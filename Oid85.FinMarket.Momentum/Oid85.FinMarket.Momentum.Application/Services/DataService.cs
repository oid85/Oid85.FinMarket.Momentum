using Microsoft.Extensions.Caching.Memory;
using Oid85.FinMarket.Algo.Application.Interfaces.ApiClients;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Services
{
    public class DataService(
        IStorageApiClient storageApiClient,
        IMemoryCache memoryCache) 
        : IDataService
    {
        private Dictionary<string, List<Candle>>? _candleData = null;
        private Dictionary<string, Instrument>? _instrumentData = null;

        public async Task<Dictionary<string, List<Candle>>> GetCandleDataAsync(List<string> tickers)
        {
            if (_candleData is not null)
            {
                foreach (var ticker in tickers)                
                    if (!_candleData.ContainsKey(ticker))
                        _candleData.Add(ticker, await GetCandlesByTickerAsync(ticker));
                
                return _candleData;
            }

            _candleData = [];

            List<string> tickerList = [.. tickers, KnownTickers.TMON];

            foreach (var ticker in tickerList.Distinct()) 
                _candleData.Add(ticker, await GetCandlesByTickerAsync(ticker));

            return _candleData;
        }

        public async Task<Dictionary<string, Instrument>> GetInstrumentDataAsync(List<string> tickers)
        {
            if (_instrumentData is not null)
                if (tickers.All(x => _instrumentData.ContainsKey(x)))
                    return _instrumentData;

            _instrumentData = (await storageApiClient.GetInstrumentListAsync(new())).Result.Instruments
                .Where(x => tickers.Distinct().Contains(x.Ticker))
                .ToDictionary(
                k => k.Ticker,
                v => new Instrument
                {
                    Ticker = v.Ticker,
                    Name = v.Name,
                    Type = v.Type,
                    Lot = v.Lot
                });

            return _instrumentData;
        }

        public List<string> GetMomentumTopTickers(Dictionary<string, List<Candle>> candleData, DateOnly date, int period, int percent)
        {
            string key = $"GetMomentumTopTickers:{date}:{period}:{percent}";

            if (memoryCache.TryGetValue(key, out List<string>? cacheTopTickers))
                return cacheTopTickers ?? [];

            DateOnly from = date.AddDays(-1 * period);
            DateOnly to = date;

            int count = Convert.ToInt32(Math.Truncate(candleData.Count * percent / 100.0));

            var topTickers = candleData
                .ToDictionary(k => k.Key, v => GetMomentumPercent(v.Value, from, to))
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)
                .ToList();

            memoryCache.Set(key, topTickers, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(60)));

            return topTickers ?? [];

            double GetMomentumPercent(List<Candle> candles, DateOnly from, DateOnly to)
            {
                var filteredCandles = candles.Where(x => x.Date >= from).Where(x => x.Date <= to).ToList();

                if (filteredCandles is []) return 0.0;

                var prices = filteredCandles.Select(x => x.Close).ToList();

                double firstPrice = prices[0];
                double lastPrice = prices[^1];

                if (firstPrice == 0.0) return 0.0;
                if (lastPrice == 0.0) return 0.0;

                return (lastPrice - firstPrice) / firstPrice * 100.0;
            }
        }

        public List<string> GetNormalizedMomentumTopTickers(Dictionary<string, List<Candle>> candleData, DateOnly date, int period, int percent)
        {
            string key = $"GetNormalizedMomentumTopTickers:{date}:{period}:{percent}";

            if (memoryCache.TryGetValue(key, out List<string>? cacheTopTickers))
                return cacheTopTickers ?? [];

            DateOnly from = date.AddDays(-1 * period);
            DateOnly to = date;

            int count = Convert.ToInt32(Math.Truncate(candleData.Count * percent / 100.0));

            var topTickers = candleData
                .ToDictionary(k => k.Key, v => GetNormalizedMomentumPercent(v.Value, from, to))
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)
                .ToList();

            memoryCache.Set(key, topTickers, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(60)));

            return topTickers ?? [];

            double GetNormalizedMomentumPercent(List<Candle> candles, DateOnly from, DateOnly to)
            {
                var filteredCandles = candles.Where(x => x.Date >= from).Where(x => x.Date <= to).ToList();

                if (filteredCandles is []) return 0.0;

                var prices = filteredCandles.Select(x => x.Close).ToList();

                double firstPrice = prices[0];
                double lastPrice = prices[^1];

                if (firstPrice == 0.0) return 0.0;
                if (lastPrice == 0.0) return 0.0;

                double momentum = lastPrice - firstPrice;
                double stdDev = prices.StdDev();

                return momentum / stdDev;
            }
        }

        public double? GetPrice(string ticker, DateOnly date)
        {
            if (_candleData is null) return null;

            var candles = _candleData[ticker];

            if (candles is null) return null;

            var candle = candles.FindLast(x => x.Date <= date);

            if (candle is null) return null;

            return candle.Close;
        }

        private async Task<List<Candle>> GetCandlesByTickerAsync(string ticker)
        {
            var from = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
            var to = DateOnly.FromDateTime(DateTime.Today);

            var response = await storageApiClient.GetCandleListAsync(
                new()
                {
                    From = from,
                    To = to,
                    Ticker = ticker
                });

            var candles =  response.Result.Candles
                .Select(x =>
                new Candle
                {
                    Open = x.Open,
                    Close = x.Close,
                    Low = x.Low,
                    High = x.High,
                    Volume = x.Volume,
                    Date = x.Date
                })
                .OrderBy(x => x.Date)
                .ToList();

            for (int i = 0; i < candles.Count; i++) candles[i].Index = i;

            return candles;
        }
    }
}
