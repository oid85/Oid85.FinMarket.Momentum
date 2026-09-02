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
            candleData.TryAdd(KnownTickers.MON, await dataService.GetMoneyEquivalentDataAsync(from, to));

            var prices = tickers.ToDictionary(k => k, v => 0.0);
            prices.TryAdd(KnownTickers.MON, 0.0);

            var lots = instrumentData.ToDictionary(k => k.Key, v => v.Value.Lot ?? 1);
            lots.TryAdd(KnownTickers.MON, 1);

            var weights = new Dictionary<string, double>();
            var costs = new Dictionary<string, double>();
            var sizes = new Dictionary<string, double>();
            var stops = new Dictionary<string, double>();

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

            foreach (var date in dates)
            {                
                if (momentumSettings.RebalanceDays.Contains(date.Day))
                {
                    SetTickers();
                    SetWeight();
                    UpdatePrices();
                    SetStops();
                    SetSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();
                }

                else
                {
                    UpdatePrices();
                    UpdateCosts();
                    CheckStops();                    
                    UpdateTotalSum();
                }

                equitySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = totalSum.RoundTo(2)
                    });

                moneySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = (money + costs[KnownTickers.MON]).RoundTo(2)
                    });

                void SetTickers()
                {
                    tickers = MomentumHelper.GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
                    tickers.Add(KnownTickers.MON);
                }

                void SetWeight()
                {
                    weights = tickers.ToDictionary(k => k, v => 1.0);
                    weights[KnownTickers.MON] = momentumSettings.CountBestTickers - tickers.Count(x => x != KnownTickers.MON);
                }

                void UpdatePrices()
                {
                    foreach (var ticker in weights.Keys)
                        prices[ticker] = dataService.GetPrice(ticker, date) ?? 0.0;
                }

                void SetStops()
                {
                    foreach (var ticker in weights.Keys.Where(x => x != KnownTickers.MON))
                        stops[ticker] = MomentumHelper.GetStopPrice(candleData[ticker], prices[ticker], date, momentumSettings.PeriodInDays);
                }

                void CheckStops()
                {
                    foreach (var ticker in weights.Keys.Where(x => x != KnownTickers.MON))
                    {
                        if (prices[ticker] < stops[ticker])
                        {
                            // Корректируем веса
                            weights.Remove(ticker);
                            weights[KnownTickers.MON] += 1.0;

                            // Продаем актив
                            sizes[ticker] = 0.0;
                            money += costs[ticker];
                            costs[ticker] = 0.0;

                            // Покупаем фонд ликвидности
                            double monSize = Math.Truncate(money / prices[KnownTickers.MON]);
                            double monCost = monSize * prices[KnownTickers.MON];                            
                            money -= monCost;

                            sizes[KnownTickers.MON] += monSize;
                            costs[KnownTickers.MON] += monCost;
                        }
                    }
                }

                void SetSizes()
                {
                    sizes.Clear();

                    double baseUnit = totalSum / weights.Values.Sum();

                    foreach (var ticker in weights.Keys)
                    {
                        if (prices[ticker] == 0.0)
                        {
                            costs[ticker] = 0.0;
                            continue;
                        }

                        double tickerCost = baseUnit * weights[ticker];
                        double tickerSize = tickerCost / prices[ticker];
                        tickerSize /= lots[ticker];
                        tickerSize = Math.Truncate(tickerSize);
                        tickerSize *= lots[ticker];
                        
                        sizes.TryAdd(ticker, Convert.ToInt32(tickerSize));
                    }
                }

                void UpdateCosts()
                {
                    costs.Clear();

                    foreach (var ticker in weights.Keys)
                        costs.TryAdd(ticker, prices[ticker] * sizes[ticker]);
                }

                void UpdateTotalSum()
                {
                    totalSum = costs.Values.Sum() + money;
                }

                void UpdateMoney()
                {
                    money = totalSum - costs.Values.Sum();
                }
            }

            var drawdownSeries = DiagramSeriesHelper.GetDrawdownSeries(equitySeries);
            var drawdownPercentSeries = DiagramSeriesHelper.GetDrawdownPercentSeries(equitySeries);

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();

            int number = 0;

            foreach (var (ticker, weight) in weights)
            {
                var price = dataService.GetPrice(ticker, dates.Last());
                var lot = lots[ticker];

                double baseUnit = totalSumLife / weights.Values.Sum();
                double tickerCost = baseUnit * weight;
                double tickerSize = tickerCost / price!.Value;
                tickerSize /= lot;
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lot;

                number++;

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Number = number,
                        Ticker = ticker,
                        Weight = weight,
                        Size = Convert.ToInt32(tickerSize),
                        Cost = tickerCost.RoundTo(2),
                        StopPrice = ticker == KnownTickers.MON ? 0.0 : stops[ticker].RoundTo(4)
                    });
            }

            return new MonitorResponse
            {
                Series = [equitySeries, moneySeries, drawdownSeries],
                CurrentPositions = [.. currentPositions.OrderByDescending(x => x.Cost)],
                Yield = DiagramSeriesHelper.GetAverageYearYieldPercent(equitySeries),
                Yield2021 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2021),
                Yield2022 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2022),
                Yield2023 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2023),
                Yield2024 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2024),
                Yield2025 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2025),
                Yield2026 = DiagramSeriesHelper.GetYearYieldPercent(equitySeries, 2026),
                MaxDrawdown = drawdownPercentSeries.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1),
                CurrentDrawdown = drawdownPercentSeries.Data.Last(x => x.Value.HasValue).Value!.Value.RoundTo(1)
            };
        }
    }
}
