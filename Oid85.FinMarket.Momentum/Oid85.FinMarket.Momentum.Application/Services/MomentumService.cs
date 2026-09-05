using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
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
            candleData.TryAdd(MON, await dataService.GetMoneyEquivalentDataAsync(from, to));

            var prices = tickers.ToDictionary(k => k, v => 0.0); prices.TryAdd(MON, 0.0);
            var lowPrices = tickers.ToDictionary(k => k, v => 0.0); lowPrices.TryAdd(MON, 0.0);
            var lots = instrumentData.ToDictionary(k => k.Key, v => v.Value.Lot ?? 1); lots.TryAdd(MON, 1);
            var weights = tickers.ToDictionary(k => k, v => 0.0); weights.TryAdd(MON, 0.0);
            var costs = tickers.ToDictionary(k => k, v => 0.0); costs.TryAdd(MON, 0.0);
            var sizes = tickers.ToDictionary(k => k, v => 0.0); sizes.TryAdd(MON, 0.0);
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
                if (momentumSettings.RebalanceDays.Contains(date.Day))
                {
                    protocolMessages.Clear();

                    SetTickers();
                    SetWeight();
                    UpdatePrices();
                    SetStops();
                    SetSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();

                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys.Where(x => x != MON))
                        AddMessage(date, ticker, $"Ребалансировка моментума. Добавлен {ticker}");
                }

                else
                {
                    UpdatePrices();
                    UpdateCosts();
                    CheckStops();                    
                    UpdateTotalSum();
                }

                if (CurrentDrawdown() >= 15.0)
                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys.Where(x => x != MON))
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
                        Value = ((money + costs[MON]) / 1000.0).RoundTo(2)
                    });

                void SetTickers()
                {
                    tickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
                    tickers.Add(MON);
                }

                void SetWeight()
                {
                    foreach (var ticker in weights.Keys) 
                        weights[ticker] = 0.0;

                    foreach (var ticker in tickers) 
                        weights[ticker] = 1.0;
                    
                    weights[MON] = momentumSettings.CountBestTickers - tickers.Count(x => x != MON);
                }

                void UpdatePrices()
                {
                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys) 
                        prices[ticker] = dataService.GetPrice(ticker, date) ?? 0.0;

                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys) 
                        lowPrices[ticker] = dataService.GetLowPrice(ticker, date) ?? 0.0;
                }

                void SetStops()
                {
                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys.Where(x => x != MON))
                        stops[ticker] = MomentumHelper.GetStopPrice(candleData[ticker], prices[ticker], date, momentumSettings.PeriodInDays);
                }

                void CheckStops()
                {
                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys.Where(x => x != MON))
                        if (lowPrices[ticker] < stops[ticker])
                            ClosePosition(ticker);
                }

                void ClosePosition(string ticker)
                {
                    // Продаем актив
                    weights[ticker] = 0.0;
                    sizes[ticker] = 0.0;
                    money += costs[ticker];
                    costs[ticker] = 0.0;

                    // Покупаем фонд ликвидности
                    weights[MON] += 1.0;
                    double monSize = Math.Truncate(money / prices[MON]);
                    double monCost = monSize * prices[MON];
                    money -= monCost;

                    sizes[MON] += monSize;
                    costs[MON] += monCost;

                    AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}");
                    AddMessage(date, MON, $"Увеличена доля фонда ликвидности");
                }

                void ChangePosition(string ticker)
                {
                    string tickerForRemove = ticker;
                    var currentTickers = weights.Where(x => x.Value > 0.0).ToDictionary().Keys.ToList();

                    // Продаем актив
                    weights[tickerForRemove] = 0.0;
                    sizes[tickerForRemove] = 0.0;
                    money += costs[tickerForRemove];
                    costs[tickerForRemove] = 0.0;

                    // Определяем новых лидеров
                    var newTopTickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers)
                        .Where(x => !currentTickers.Contains(x)).Where(x => x != MON).ToList();

                    var tickerForAdd = newTopTickers.Count == 0 
                        ? MON 
                        : newTopTickers.First();

                    AddMessage(date, ticker, $"Стоп-лосс. Удален {ticker}");

                    if (tickerForAdd == MON)
                    {
                        // Покупаем фонд ликвидности
                        weights[MON] += 1.0;
                        double monSize = Math.Truncate(money / prices[MON]);
                        double monCost = monSize * prices[MON];
                        money -= monCost;

                        sizes[MON] += monSize;
                        costs[MON] += monCost;

                        AddMessage(date, MON, $"Увеличена доля фонда ликвидности");
                    }

                    else
                    {
                        // Покупаем другой актив
                        weights[tickerForAdd] = 1.0;

                        prices[tickerForAdd] = dataService.GetPrice(tickerForAdd, date) ?? 0.0;
                        lowPrices[tickerForAdd] = dataService.GetLowPrice(tickerForAdd, date) ?? 0.0;

                        sizes[tickerForAdd] = Math.Truncate(money / prices[tickerForAdd] / lots[tickerForAdd]) * lots[tickerForAdd];
                        costs[tickerForAdd] = prices[tickerForAdd] * sizes[tickerForAdd];

                        money -= costs[tickerForAdd];

                        AddMessage(date, tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}");
                    }
                }

                void SetSizes()
                {
                    ClearSizes();

                    double baseUnit = totalSum / weights.Values.Sum();

                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys)
                    {
                        if (prices[ticker] == 0.0)
                        {
                            costs[ticker] = 0.0;
                            continue;
                        }
                                
                        sizes[ticker] = Math.Truncate(baseUnit * weights[ticker] / prices[ticker] / lots[ticker]) * lots[ticker];
                    }
                }

                void ClearSizes()
                {
                    foreach (var ticker in sizes.Keys)
                        sizes[ticker] = 0.0;
                }

                void UpdateCosts()
                {
                    ClearCosts();

                    foreach (var ticker in weights.Where(x => x.Value > 0.0).ToDictionary().Keys)
                        costs[ticker] = prices[ticker] * sizes[ticker];
                }

                void ClearCosts()
                {
                    foreach (var ticker in costs.Keys)
                        costs[ticker] = 0.0;
                }

                void UpdateTotalSum()
                {
                    totalSum = costs.Values.Sum() + money;
                }

                void UpdateMoney()
                {
                    money = totalSum - costs.Values.Sum();
                }

                void AddMessage(DateOnly date, string ticker, string message)
                {
                    protocolMessages.Add(
                        new ProtocolMessage()
                        {
                            Date = date,
                            Ticker = ticker,
                            Message = message
                        });
                }
            }

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();
            
            foreach (var (ticker, weight) in weights.Where(x => x.Value > 0).ToDictionary())
            {
                var price = dataService.GetPrice(ticker, dates.Last());
                var lot = lots[ticker];

                double baseUnit = totalSumLife / weights.Values.Sum();
                double tickerCost = baseUnit * weight;
                double tickerSize = tickerCost / price!.Value;
                tickerSize /= lot;
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lot;

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Ticker = ticker,
                        Weight = weight,
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

            foreach (var (ticker, candles) in candleData.Where(x => x.Key != MON))
            {
                var candlesByDates = candles.Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                string color = weights.Where(x => x.Value > 0).ToDictionary().ContainsKey(ticker)
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
