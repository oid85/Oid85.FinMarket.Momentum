using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Helpers
{
    public class MomentumHelper
    {
        public static double GetStopPrice(List<Candle> candles, double price, DateOnly date, int period)
        {
            if (candles is []) return 0.0;

            DateOnly from = date.AddDays(-1 * period);
            DateOnly to = date;

            var averageRange = candles
                .Where(x => x.Date >= from && x.Date <= to)
                .Average(x => Math.Abs(x.Close - x.Open));

            return price - averageRange * 2.0;
        }

        public static List<string> GetMomentumTopTickers(
            Dictionary<string, List<Candle>> candleData, DateOnly date, int period, int count)
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

        public static double GetDeltaPricePercent(List<Candle> candles)
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
