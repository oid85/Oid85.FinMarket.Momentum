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

            var context = tickers.ToDictionary(k => k, v => new MomentumTickerContext { Ticker = v, Lot = instrumentData[v].Lot ?? 1 }); 
            context.TryAdd(MON, new MomentumTickerContext { Ticker = MON, Lot = 1 });
                        
            var stops = tickers.ToDictionary(k => k, v => 0.0); stops.TryAdd(MON, 0.0);

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

            List<ProtocolMessage> protocolMessages = [];

            foreach (var date in dates)
            {
                // Устанавливаем текущую дату для контекста
                context.SetDate(date);

                if (momentumSettings.RebalanceDays.Contains(date.Day))
                {
                    protocolMessages.Clear();

                    SetTickers();
                    SetWeight();
                    UpdateCandles();
                    SetStops();
                    SetSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();

                    

                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        AddMessage(date, ticker, $"Ребалансировка моментума. Позиция {ticker}", KnownColors.LightGreen);
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
                        Value = ((money + context[MON].Cost) / 1000.0).RoundTo(2)
                    });

                void SetTickers()
                {
                    tickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
                    tickers.Add(MON);
                }

                void SetWeight()
                {
                    foreach (var ticker in context.GetTickers())
                        context[ticker].Weight = 0.0;

                    foreach (var ticker in tickers)
                        context[ticker].Weight = 1.0;

                    context[MON].Weight = momentumSettings.CountBestTickers - tickers.Count(x => x != MON);
                }

                void UpdateCandles()
                {
                    foreach (var ticker in context.GetPortfolioTickers()) 
                        context[ticker].Candle = dataService.GetCandle(ticker, date) ?? new Candle();
                }

                void SetStops()
                {
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        stops[ticker] = MomentumHelper.GetStopPrice(candleData[ticker], context[ticker].Candle.Close, date, momentumSettings.PeriodInDays);
                }

                void CheckStops()
                {
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        if (context[ticker].Candle.Low < stops[ticker])
                            ClosePosition(ticker);
                }

                void ClosePosition(string ticker)
                {
                    // Продаем актив
                    context[ticker].Weight = 0.0;
                    context[ticker].Size = 0.0;
                    money += context[ticker].Cost;
                    context[ticker].Cost = 0.0;

                    // Покупаем фонд ликвидности
                    context[MON].Weight += 1.0;
                    double monSize = Math.Truncate(money / context[MON].Candle.Close);
                    double monCost = monSize * context[MON].Candle.Close;
                    money -= monCost;

                    context[MON].Size += monSize;
                    context[MON].Cost += monCost;

                    AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);
                    AddMessage(date, MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
                }

                void ChangePosition(string ticker)
                {
                    string tickerForRemove = ticker;
                    var currentTickers = context.GetPortfolioTickers();

                    // Продаем актив
                    context[tickerForRemove].Weight = 0.0;
                    context[tickerForRemove].Size = 0.0;
                    money += context[tickerForRemove].Cost;
                    context[tickerForRemove].Cost = 0.0;

                    // Определяем новых лидеров
                    var newTopTickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers)
                        .Where(x => !currentTickers.Contains(x)).Where(x => x != MON).ToList();

                    var tickerForAdd = newTopTickers.Count == 0 
                        ? MON 
                        : newTopTickers.First();

                    AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);

                    if (tickerForAdd == MON)
                    {
                        // Покупаем фонд ликвидности
                        context[MON].Weight += 1.0;
                        double monSize = Math.Truncate(money / context[MON].Candle.Close);
                        double monCost = monSize * context[MON].Candle.Close;
                        money -= monCost;

                        context[MON].Size += monSize;
                        context[MON].Cost += monCost;

                        AddMessage(date, MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
                    }

                    else
                    {
                        // Покупаем другой актив
                        context[tickerForAdd].Weight = 1.0;
                        context[tickerForAdd].Candle = dataService.GetCandle(tickerForAdd, date) ?? new Candle();
                        context[tickerForAdd].Size = Math.Truncate(money / context[tickerForAdd].Candle.Close / context[tickerForAdd].Lot) * context[tickerForAdd].Lot;
                        context[tickerForAdd].Cost = context[tickerForAdd].Candle.Close * context[tickerForAdd].Size;

                        money -= context[tickerForAdd].Cost;

                        AddMessage(date, tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}", KnownColors.LightGreen);
                    }
                }

                void SetSizes()
                {
                    ClearSizes();

                    double baseUnit = totalSum / context.GetWeightSum();

                    foreach (var ticker in context.GetPortfolioTickers())
                    {
                        if (context[ticker].Candle.Close == 0.0)
                        {
                            context[ticker].Cost = 0.0;
                            continue;
                        }

                        context[ticker].Size = Math.Truncate(baseUnit * context[ticker].Weight / context[ticker].Candle.Close / context[ticker].Lot) * context[ticker].Lot;
                    }
                }

                void ClearSizes()
                {
                    foreach (var (ticker, item) in context)
                        context[ticker].Size = 0.0;
                }

                void UpdateCosts()
                {
                    ClearCosts();

                    foreach (var ticker in context.GetPortfolioTickers())
                        context[ticker].Cost = context[ticker].Candle.Close * context[ticker].Size;
                }

                void ClearCosts()
                {
                    foreach (var (ticker, item) in context)
                        context[ticker].Cost = 0.0;
                }

                void UpdateTotalSum()
                {
                    totalSum = context.GetCostSum() + money;
                }

                void UpdateMoney()
                {
                    money = totalSum - context.GetCostSum();
                }

                void AddMessage(DateOnly date, string ticker, string message, string colorFill) => 
                    protocolMessages.Add(
                        new ProtocolMessage()
                        {
                            Date = date,
                            Ticker = ticker,
                            Message = message,
                            ColorFill = colorFill
                        });
            }

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();
            
            foreach (var ticker in context.GetPortfolioTickers())
            {
                var candle = dataService.GetCandle(ticker, dates.Last());
                var lot = context[ticker].Lot;

                double baseUnit = totalSumLife / context.GetWeightSum();
                double tickerCost = baseUnit * context[ticker].Weight;
                double tickerSize = tickerCost / candle!.Close;
                tickerSize /= lot;
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lot;

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Ticker = ticker,
                        Weight = context[ticker].Weight,
                        Size = Convert.ToInt32(tickerSize),
                        Cost = tickerCost.RoundTo(2),
                        StopPrice = ticker == MON ? 0.0 : stops[ticker].RoundTo(4)
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
                ProtocolMessages = [.. protocolMessages.OrderByDescending(x => x.Date)],
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
