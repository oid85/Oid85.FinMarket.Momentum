using Microsoft.Extensions.Caching.Memory;
using Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Services
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

            foreach (var ticker in tickers.Distinct()) 
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

        public async Task<List<Candle>> GetMoneyEquivalentDataAsync(DateOnly from, DateOnly to)
        {
            var dates = DateUtils.GetDates(from, to);

            double price = 100.0;

            var candles = new List<Candle>
            {
                new() {
                    Date = dates[0],
                    Open = price,
                    Close = price,
                    High = price,
                    Low = price,
                    Volume = 0
                }
            };

            double keyRate = 12.0;
            double dayPercentRate = keyRate / 365.0;
            double multipleCoefficient = 1.0 + dayPercentRate / 100.0;

            for (int i = 1; i < dates.Count; i++)
            {
                candles.Add(
                    new()
                    {
                        Date = dates[i],
                        Open = candles[i - 1].Open * multipleCoefficient,
                        Close = candles[i - 1].Close * multipleCoefficient,
                        High = candles[i - 1].High * multipleCoefficient,
                        Low = candles[i - 1].Low * multipleCoefficient,
                        Volume = 0
                    });
            }

            return candles;
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
