using System.Diagnostics;
using Microsoft.Extensions.Options;
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

            double startMoneySum = 1_000_000.0;
            double money = startMoneySum;
            double totalSum = startMoneySum;
            
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

            var weights = new Dictionary<string, double>() { { KnownTickers.MON, (double) momentumSettings.CountBestTickers } };
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
                if (date.Day == 1)
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
                    tickers = GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
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
                        stops[ticker] = GetStopPrice(candleData[ticker], prices[ticker], date, momentumSettings.PeriodInDays);
                }

                void CheckStops()
                {
                    foreach (var ticker in weights.Keys.Where(x => x != KnownTickers.MON))
                    {
                        if (prices[ticker] < stops[ticker])
                        {
                            weights.Remove(ticker);
                            weights[KnownTickers.MON] += 1.0;
                            sizes[ticker] = 0.0;
                            money += costs[ticker];
                            costs[ticker] = 0.0;

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
                    sizes = [];

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
                    costs = [];

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

            var drawdownSeries = GetDrawdownSeries(equitySeries);
            var drawdownPercentSeries = GetDrawdownPercentSeries(equitySeries);

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();

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

                currentPositions.Add(
                    new PortfolioPosition
                    {
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
                Yield = GetAverageYearYieldPercent(equitySeries),
                Yield2021 = GetYearYieldPercent(equitySeries, 2021),
                Yield2022 = GetYearYieldPercent(equitySeries, 2022),
                Yield2023 = GetYearYieldPercent(equitySeries, 2023),
                Yield2024 = GetYearYieldPercent(equitySeries, 2024),
                Yield2025 = GetYearYieldPercent(equitySeries, 2025),
                Yield2026 = GetYearYieldPercent(equitySeries, 2026),
                MaxDrawdown = drawdownPercentSeries.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value),
                CurrentDrawdown = drawdownPercentSeries.Data.Last(x => x.Value.HasValue).Value!.Value
            };
        }

        private static double GetYearYieldPercent(DiagramSeries equitySeries, int year)
        {
            var dataValues = equitySeries.Data.Where(x => x.Date.Year == year);

            double firstValue = dataValues.First().Value ?? 0.0;
            double lastValue = dataValues.Last().Value ?? 0.0;

            var firstDate = dataValues.First().Date.ToDateTime(TimeOnly.MinValue);
            var lastDate = dataValues.Last().Date.ToDateTime(TimeOnly.MaxValue);

            if (lastValue == 0.0) return 0.0;

            var years = (lastDate - firstDate).TotalDays / 365.0;

            return ((lastValue - firstValue) / firstValue * 100.0 / years).RoundTo(2);
        }

        private static double GetAverageYearYieldPercent(DiagramSeries equitySeries)
        {
            double firstValue = equitySeries.Data.First().Value ?? 0.0;
            double lastValue = equitySeries.Data.Last().Value ?? 0.0;

            var firstDate = equitySeries.Data.First().Date.ToDateTime(TimeOnly.MinValue);
            var lastDate = equitySeries.Data.Last().Date.ToDateTime(TimeOnly.MaxValue);

            if (lastValue == 0.0) return 0.0;

            var years = (lastDate - firstDate).TotalDays / 365.0;

            return ((lastValue - firstValue) / firstValue * 100.0 / years).RoundTo(2);
        }

        private static DiagramSeries GetDrawdownSeries(DiagramSeries equitySeries)
        {
            var drawdownSeries = new DiagramSeries
            {
                Name = "Просадка",
                Color = KnownColors.Red,
                ColorFill = KnownColors.Red
            };

            for (int i = 0; i < equitySeries.Data.Count; i++)
            {
                if (i == 0)
                    drawdownSeries.Data.Add(
                        new DateValue<double?>
                        {
                            Date = equitySeries.Data[i].Date,
                            Value = 0.0
                        });

                else
                {
                    var maxEquity = equitySeries.Data.Take(i).Max(x => x.Value);

                    var dateValue = new DateValue<double?>
                    {
                        Date = equitySeries.Data[i].Date,
                        Value = 0.0
                    };

                    if (equitySeries.Data[i].Value <= maxEquity)
                        dateValue.Value = (equitySeries.Data[i].Value - maxEquity).RoundTo(2);

                    drawdownSeries.Data.Add(dateValue);
                }
            }

            return drawdownSeries;
        }

        private static DiagramSeries GetDrawdownPercentSeries(DiagramSeries equitySeries)
        {
            var drawdownSeries = new DiagramSeries
            {
                Name = "Просадка, %",
                Color = KnownColors.Red,
                ColorFill = KnownColors.Red
            };

            for (int i = 0; i < equitySeries.Data.Count; i++)
            {
                if (i == 0)
                    drawdownSeries.Data.Add(
                        new DateValue<double?>
                        {
                            Date = equitySeries.Data[i].Date,
                            Value = 0.0
                        });

                else
                {
                    var maxEquity = equitySeries.Data.Take(i).Max(x => x.Value);

                    var dateValue = new DateValue<double?>
                    {
                        Date = equitySeries.Data[i].Date,
                        Value = 0.0
                    };

                    if (equitySeries.Data[i].Value <= maxEquity)
                        dateValue.Value = ((equitySeries.Data[i].Value - maxEquity) / maxEquity * 100.0).RoundTo(2);

                    drawdownSeries.Data.Add(dateValue);
                }
            }

            return drawdownSeries;
        }

        private static double GetStopPrice(List<Candle> candles, double price, DateOnly date, int period)
        {
            if (candles is []) return 0.0;

            DateOnly from = date.AddDays(-1 * period);
            DateOnly to = date;

            var averageRange = candles
                .Where(x => x.Date >= from && x.Date <= to)
                .Average(x => Math.Abs(x.Close - x.Open));

            return price - averageRange;
        }

        private static List<string> GetMomentumTopTickers(
            Dictionary<string, List<Candle>> candleData,
            DateOnly date, 
            int period, 
            int count)
        {
            if (candleData is null) return [];

            DateOnly from = date.AddDays(-1 * period);
            DateOnly to = date;

            var tickers = candleData
                .Where(x => x.Key != KnownTickers.MON)
                .ToDictionary(
                    k => k.Key, 
                    v => GetDeltaPricePercent([.. v.Value.Where(x => x.Date >= from && x.Date <= to)]))
                .Where(x => x.Value > 0.0)
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)                
                .ToList();

            return tickers ?? [];
        }

        private static double GetDeltaPricePercent(List<Candle> candles)
        {
            if (candles is []) return 0.0;

            double firstPrice = candles.First().Close;
            double lastPrice = candles.Last().Close;

            if (firstPrice == 0.0) return 0.0;
            if (lastPrice == 0.0) return 0.0;

            return (lastPrice - firstPrice) / firstPrice * 100.0;
        }
    }
}
