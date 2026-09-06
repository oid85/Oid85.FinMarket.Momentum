using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumContext
    {
        public Dictionary<string, MomentumTickerContext> Data { get; set; } = [];

        public List<ProtocolMessage> ProtocolMessages { get; set; } = [];

        public void SetDate(DateOnly date)
        {
            foreach (var (ticker, _) in Data)
                Data[ticker].Date = date;
        }

        public List<string> GetPortfolioNoMonTickers() => [.. Data.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> GetPortfolioTickers() => [.. Data.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> GetTickers() => [.. Data.Keys];

        public double GetWeightSum() => Data.Values.Sum(x => x.Weight);

        public double GetCostSum() => Data.Values.Sum(x => x.Cost);

        public void AddMessage(DateOnly date, string ticker, string message, string colorFill) =>
            ProtocolMessages.Add(
                new ProtocolMessage()
                {
                    Date = date,
                    Ticker = ticker,
                    Message = message,
                    ColorFill = colorFill
                });
    }
}
