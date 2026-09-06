using Oid85.FinMarket.Momentum.Common.KnownConstants;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumContext
    {
        public Dictionary<string, MomentumTickerContext> Data { get; set; } = [];

        public void SetDate(DateOnly date)
        {
            foreach (var (ticker, _) in Data)
                Data[ticker].Date = date;
        }

        public List<string> GetPortfolioNoMonTickers() =>
            [.. Data.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];
    }
}
