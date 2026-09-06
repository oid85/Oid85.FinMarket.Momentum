using Oid85.FinMarket.Momentum.Application.Models;

namespace Oid85.FinMarket.Momentum.Application.Extensions
{
    public static class MomentumContextExtentions
    {
        public static Dictionary<string, MomentumTickerContext> SetDate(this Dictionary<string, MomentumTickerContext> context, DateOnly date)
        {
            foreach (var (key, _) in context)
                context[key].Date = date;

            return context;
        }
    }
}
