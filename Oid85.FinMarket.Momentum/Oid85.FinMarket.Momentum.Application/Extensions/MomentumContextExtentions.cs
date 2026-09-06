using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Common.KnownConstants;

namespace Oid85.FinMarket.Momentum.Application.Extensions
{
    public static class MomentumContextExtentions
    {
        public static Dictionary<string, MomentumTickerContext> SetDate(this Dictionary<string, MomentumTickerContext> context, DateOnly date)
        {
            foreach (var (ticker, _) in context)
                context[ticker].Date = date;

            return context;
        }

        public static List<string> GetPortfolioNoMonTickers(this Dictionary<string, MomentumTickerContext> context) =>
            [.. context.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public static List<string> GetPortfolioTickers(this Dictionary<string, MomentumTickerContext> context) =>
            [.. context.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public static List<string> GetTickers(this Dictionary<string, MomentumTickerContext> context) => 
            [.. context.Keys];

        public static double GetWeightSum(this Dictionary<string, MomentumTickerContext> context) => 
            context.Values.Sum(x => x.Weight);

        public static double GetCostSum(this Dictionary<string, MomentumTickerContext> context) =>
            context.Values.Sum(x => x.Cost);
    }
}
