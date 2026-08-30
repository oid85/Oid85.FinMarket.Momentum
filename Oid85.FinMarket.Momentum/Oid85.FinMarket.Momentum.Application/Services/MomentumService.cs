using System.Diagnostics;
using Microsoft.Extensions.Options;
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
        IDataService dataService)
        : IMomentumService
    {
        public async Task<MonitorResponse> MonitorAsync(MonitorRequest request)
        {
            var momentumSettings = options.Value;

            double startMoneySum = 1_000_000.0;
            double money = startMoneySum;
            double totalSum = startMoneySum;

            var dt = DateOnly.FromDateTime(DateTime.Today.AddYears(-5));
            var from = new DateOnly(dt.Year, dt.Month, 1);
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

            var equitySeries = new DiagramSeries
            {
                Name = "Капитал",
                Color = KnownColors.Green,
                ColorFill = KnownColors.Green
            };

            var moneySeries = new DiagramSeries
            {
                Name = "Ден. ср-ва и экв.",
                Color = KnownColors.Blue,
                ColorFill = KnownColors.Blue
            };

            foreach (var date in dates)
            {                
                if (date.Day == 1)
                {
                    UpdateTickers();
                    UpdateWeight();
                    UpdatePrices();
                    UpdateSizes();
                    UpdateCosts();
                    UpdateMoney();
                    UpdateTotalSum();
                }

                else
                {
                    UpdatePrices();
                    UpdateCosts();
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

                void UpdateTickers()
                {
                    tickers = GetMomentumTopTickers(candleData, date, momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);
                    tickers.Add(KnownTickers.MON);
                }

                void UpdateWeight()
                {
                    weights = tickers.ToDictionary(k => k, v => 1.0);
                    weights[KnownTickers.MON] = momentumSettings.CountBestTickers - tickers.Count(x => x != KnownTickers.MON);
                }

                void UpdatePrices()
                {
                    foreach (var ticker in weights.Keys)
                        prices[ticker] = dataService.GetPrice(ticker, date) ?? 0.0;
                }

                void UpdateSizes()
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

            return new MonitorResponse
            {
                Series = [equitySeries, moneySeries]
            };
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

            var topTickers = candleData
                .Where(x => x.Key != KnownTickers.MON)
                .ToDictionary(k => k.Key, v => GetDeltaPricePercent(v.Value, from, to))
                .Where(x => x.Value > 0.0)
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)                
                .ToList();

            return topTickers ?? [];
        }

        private static double GetDeltaPricePercent(List<Candle> candles, DateOnly from, DateOnly to)
        {
            var filteredCandles = candles.Where(x => x.Date >= from).Where(x => x.Date <= to).ToList();

            if (filteredCandles is []) return 0.0;

            var prices = filteredCandles.Select(x => x.Close).ToList();

            double firstPrice = prices.First();
            double lastPrice = prices.Last();

            if (firstPrice == 0.0) return 0.0;
            if (lastPrice == 0.0) return 0.0;

            return (lastPrice - firstPrice) / firstPrice * 100.0;
        }
    }
}
