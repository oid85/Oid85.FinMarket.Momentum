using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumContext
    {
        public Dictionary<string, MomentumTickerData> TickerData { get; set; } = [];

        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

        public List<ProtocolMessage> ProtocolMessages { get; set; } = [];

        public List<string> TopTickers { get; set; } = [];
        
        public void SetDate(DateOnly date)
        {
            foreach (var (ticker, _) in TickerData) 
                TickerData[ticker].Date = date;
        }

        public List<string> GetPortfolioNoMonTickers() => [.. TickerData.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> GetPortfolioTickers() => [.. TickerData.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> GetAllTickers() => [.. TickerData.Keys];

        public double GetWeightSum() => TickerData.Values.Sum(x => x.Weight);

        public double GetCostSum() => TickerData.Values.Sum(x => x.Cost);

        public void AddMessage(DateOnly date, string ticker, string message, string colorFill) =>
            ProtocolMessages.Add(new () { Date = date, Ticker = ticker, Message = message, ColorFill = colorFill });

        public void SetTopTickers(DateOnly date, int period, int count)
        {
            TopTickers = MomentumHelper.GetMomentumTopTickers(CandleData, date, period, count);
            TopTickers.Add(KnownTickers.MON);
        }

        public void SetWeight(int count)
        {
            foreach (var ticker in GetAllTickers()) TickerData[ticker].Weight = 0.0;
            foreach (var ticker in TopTickers) TickerData[ticker].Weight = 1.0;
            TickerData[KnownTickers.MON].Weight = count - TopTickers.Count(x => x != KnownTickers.MON);
        }

        public void UpdateCandles(DateOnly date)
        {
            foreach (var ticker in GetPortfolioTickers())
                TickerData[ticker].Candle = GetCandle(ticker, date) ?? new Candle();
        }

        public Candle? GetCandle(string ticker, DateOnly date)
        {
            if (CandleData is null) return null;
            var candles = CandleData[ticker];
            if (candles is null) return null;
            var candle = candles.FindLast(x => x.Date <= date);
            if (candle is null) return null;
            return candle;
        }

        public void SetStops(DateOnly date, int period)
        {
            foreach (var ticker in GetPortfolioNoMonTickers())
                TickerData[ticker].Stop = MomentumHelper.GetStopPrice(CandleData[ticker], TickerData[ticker].Candle.Close, date, period);
        }
    }
}
