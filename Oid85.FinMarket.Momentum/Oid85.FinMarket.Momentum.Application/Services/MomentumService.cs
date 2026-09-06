using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Extensions;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;

namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class MomentumService(
        IOptions<MomentumSettings> options,
        IDataService dataService,
        IParameterRepository parameterRepository)
        : IMomentumService
    {
        public async Task<MonitorResponse> MonitorAsync(MonitorRequest request)
        {
            var momentumSettings = options.Value;
            
            double money = momentumSettings.StartMoneySum;
            double totalSum = momentumSettings.StartMoneySum;

            var from = new DateOnly(2021, 1, 1);
            var to = DateOnly.FromDateTime(DateTime.Today);
            var dates = DateUtils.GetDates(from, to);

            var tickers = momentumSettings.Tickers;
            var instrumentData = await dataService.GetInstrumentDataAsync(tickers);

            var candleData = await dataService.GetCandleDataAsync(tickers);
            candleData.TryAdd(MON, await dataService.GetMoneyEquivalentCandlesAsync(from, to));

            var context = new MomentumContext
            {
                Data = tickers.ToDictionary(k => k, v => new MomentumTickerContext { Ticker = v, Lot = instrumentData[v].Lot ?? 1 })
            };
            context.Data.TryAdd(MON, new MomentumTickerContext { Ticker = MON, Lot = 1 });                                   

            var equitySeries = new DiagramSeries
            {
                Name = "Капитал",
                Color = KnownColors.Green,
                ColorFill = KnownColors.Green
            };

            var moneySeries = new DiagramSeries
            {
                Name = "Фонд ликвидности",
                Color = KnownColors.LightBlue,
                ColorFill = KnownColors.LightBlue
            };            

            double CurrentDrawdown()
            {
                var equityValues = equitySeries.Data.Select(x => x.Value ?? 0.0).ToList();

                if (equityValues.Count == 0) return 0.0;

                double lastEquity = equityValues.Last();
                double maxEquity = equityValues.Max();

                if (maxEquity == 0.0) return 0.0;

                return Math.Abs((maxEquity - lastEquity) / maxEquity * 100.0);
            }            

            foreach (var date in dates)
            {
                // Устанавливаем текущую дату для контекста
                context.SetDate(date);

                if (momentumSettings.RebalanceDays.Contains(date.Day))
                {
                    context.ProtocolMessages.Clear();

                    SetTickers();
                    SetWeight();
                    UpdateCandles();
                    SetStops();
                    SetSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();
                    
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        context.AddMessage(date, ticker, $"Ребалансировка моментума. Позиция {ticker}", KnownColors.LightGreen);
                }

                else
                {
                    UpdateCandles();
                    UpdateCosts();
                    CheckStops();                    
                    UpdateTotalSum();
                }

                if (CurrentDrawdown() >= 15.0)
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        ClosePosition(ticker);

                equitySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = (totalSum / 1000.0).RoundTo(2)
                    });

                moneySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = ((money + context.Data[MON].Cost) / 1000.0).RoundTo(2)
                    });

                void SetTickers()
                {
                    tickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
                    tickers.Add(MON);
                }

                void SetWeight()
                {
                    foreach (var ticker in context.GetTickers())
                        context.Data[ticker].Weight = 0.0;

                    foreach (var ticker in tickers)
                        context.Data[ticker].Weight = 1.0;

                    context.Data[MON].Weight = momentumSettings.CountBestTickers - tickers.Count(x => x != MON);
                }

                void UpdateCandles()
                {
                    foreach (var ticker in context.GetPortfolioTickers())
                        context.Data[ticker].Candle = dataService.GetCandle(ticker, date) ?? new Candle();
                }

                void SetStops()
                {
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        context.Data[ticker].Stop = MomentumHelper.GetStopPrice(candleData[ticker], context.Data[ticker].Candle.Close, date, momentumSettings.PeriodInDays);
                }

                void CheckStops()
                {
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        if (context.Data[ticker].Candle.Low < context.Data[ticker].Stop)
                            ClosePosition(ticker);
                }

                void ClosePosition(string ticker)
                {
                    // Продаем актив
                    context.Data[ticker].Weight = 0.0;
                    context.Data[ticker].Size = 0.0;
                    money += context.Data[ticker].Cost;
                    context.Data[ticker].Cost = 0.0;

                    // Покупаем фонд ликвидности
                    context.Data[MON].Weight += 1.0;
                    double monSize = Math.Truncate(money / context.Data[MON].Candle.Close);
                    double monCost = monSize * context.Data[MON].Candle.Close;
                    money -= monCost;

                    context.Data[MON].Size += monSize;
                    context.Data[MON].Cost += monCost;

                    context.AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);
                    context.AddMessage(date, MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
                }

                void ChangePosition(string ticker)
                {
                    string tickerForRemove = ticker;
                    var currentTickers = context.GetPortfolioTickers();

                    // Продаем актив
                    context.Data[tickerForRemove].Weight = 0.0;
                    context.Data[tickerForRemove].Size = 0.0;
                    money += context.Data[tickerForRemove].Cost;
                    context.Data[tickerForRemove].Cost = 0.0;

                    // Определяем новых лидеров
                    var newTopTickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers)
                        .Where(x => !currentTickers.Contains(x)).Where(x => x != MON).ToList();

                    var tickerForAdd = newTopTickers.Count == 0 
                        ? MON 
                        : newTopTickers.First();

                    context.AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);

                    if (tickerForAdd == MON)
                    {
                        // Покупаем фонд ликвидности
                        context.Data[MON].Weight += 1.0;
                        double monSize = Math.Truncate(money / context.Data[MON].Candle.Close);
                        double monCost = monSize * context.Data[MON].Candle.Close;
                        money -= monCost;

                        context.Data[MON].Size += monSize;
                        context.Data[MON].Cost += monCost;

                        context.AddMessage(date, MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
                    }

                    else
                    {
                        // Покупаем другой актив
                        context.Data[tickerForAdd].Weight = 1.0;
                        context.Data[tickerForAdd].Candle = dataService.GetCandle(tickerForAdd, date) ?? new Candle();
                        context.Data[tickerForAdd].Size = Math.Truncate(money / context.Data[tickerForAdd].Candle.Close / context.Data[tickerForAdd].Lot) * context.Data[tickerForAdd].Lot;
                        context.Data[tickerForAdd].Cost = context.Data[tickerForAdd].Candle.Close * context.Data[tickerForAdd].Size;

                        money -= context.Data[tickerForAdd].Cost;

                        context.AddMessage(date, tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}", KnownColors.LightGreen);
                    }
                }

                void SetSizes()
                {
                    ClearSizes();

                    double baseUnit = totalSum / context.GetWeightSum();

                    foreach (var ticker in context.GetPortfolioTickers())
                    {
                        if (context.Data[ticker].Candle.Close == 0.0)
                        {
                            context.Data[ticker].Cost = 0.0;
                            continue;
                        }

                        context.Data[ticker].Size = Math.Truncate(baseUnit * context.Data[ticker].Weight / context.Data[ticker].Candle.Close / context.Data[ticker].Lot) * context.Data[ticker].Lot;
                    }
                }

                void ClearSizes()
                {
                    foreach (var (ticker, item) in context.Data)
                        context.Data[ticker].Size = 0.0;
                }

                void UpdateCosts()
                {
                    ClearCosts();

                    foreach (var ticker in context.GetPortfolioTickers())
                        context.Data[ticker].Cost = context.Data[ticker].Candle.Close * context.Data[ticker].Size;
                }

                void ClearCosts()
                {
                    foreach (var (ticker, item) in context.Data)
                        context.Data[ticker].Cost = 0.0;
                }

                void UpdateTotalSum()
                {
                    totalSum = context.GetCostSum() + money;
                }

                void UpdateMoney()
                {
                    money = totalSum - context.GetCostSum();
                }
            }

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();
            
            foreach (var ticker in context.GetPortfolioTickers())
            {
                var candle = dataService.GetCandle(ticker, dates.Last());
                var lot = context.Data[ticker].Lot;

                double baseUnit = totalSumLife / context.GetWeightSum();
                double tickerCost = baseUnit * context.Data[ticker].Weight;
                double tickerSize = tickerCost / candle!.Close;
                tickerSize /= lot;
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lot;

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Ticker = ticker,
                        Weight = context.Data[ticker].Weight,
                        Size = Convert.ToInt32(tickerSize),
                        Cost = tickerCost.RoundTo(2),
                        StopPrice = ticker == MON ? 0.0 : context.Data[ticker].Stop.RoundTo(4)
                    });
            }

            List<PortfolioPosition> orderedCurrentPositions = [
                    .. currentPositions.Where(x => x.Ticker != MON).OrderBy(x => x.Ticker),
                    .. currentPositions.Where(x => x.Ticker == MON)
                    ];

            int number = 1;
            foreach (var currentPosition in orderedCurrentPositions)
                currentPosition.Number = number++;            

            var currentTopTickers = MomentumHelper.GetMomentumTopTickers(candleData, DateOnly.FromDateTime(DateTime.Today), momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);

            from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1 * momentumSettings.PeriodInDays);
            to = DateOnly.FromDateTime(DateTime.Today);

            var priceDynamicSeries = new List<DiagramSeries>();

            foreach (var (ticker, candleList) in candleData.Where(x => x.Key != MON))
            {
                var candlesByDates = candleList.Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                string color = context.GetPortfolioTickers().Contains(ticker)
                    ? KnownColors.Green 
                    : KnownColors.LightBlue;

                priceDynamicSeries.Add(
                    new DiagramSeries
                    {
                        Name = ticker,
                        Color = color,
                        ColorFill = color,
                        Data = [.. candlesByDates
                        .Select(x => 
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = (x.Close / firstPrice).RoundTo(4)
                        })]
                    });
            }

            var drawdownSeries = DiagramSeriesHelper.GetDrawdownSeries(equitySeries);
            var drawdownPercentSeries = DiagramSeriesHelper.GetDrawdownSeries(equitySeries, true);

            double maxDrawdown = drawdownPercentSeries.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1);
            double currentDrawdown = drawdownPercentSeries.Data.Last(x => x.Value.HasValue).Value!.Value.RoundTo(1);

            return new MonitorResponse
            {
                ProtocolMessages = [.. context.ProtocolMessages.OrderByDescending(x => x.Date)],
                TotalSumLife = totalSumLife,
                BacktestSeries = [equitySeries, moneySeries, drawdownSeries],
                PriceDynamicSeries = priceDynamicSeries,
                CurrentPositions = orderedCurrentPositions,
                Yield = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries),
                Yield2021 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2021),
                Yield2022 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2022),
                Yield2023 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2023),
                Yield2024 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2024),
                Yield2025 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2025),
                Yield2026 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2026),
                MaxDrawdown = maxDrawdown,
                CurrentDrawdown = currentDrawdown
            };
        }

        public async Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request)
        {
            await parameterRepository.SetParameterValueAsync($"TotalSum:Momentum", request.TotalSum.ToString("N0"));
            return new();
        }
    }
}
