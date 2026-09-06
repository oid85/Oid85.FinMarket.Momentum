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

        public static List<string> GetPortfolioNoMonTickers(this Dictionary<string, MomentumTickerContext> context)
        {
            List<string> tickers = [];

            foreach (var (ticker, item) in context)
                if (ticker != KnownTickers.MON)
                    if (item.Weight > 0.0)
                        tickers.Add(ticker);

            return tickers;
        }

        public static List<string> GetPortfolioTickers(this Dictionary<string, MomentumTickerContext> context)
        {
            List<string> tickers = [];

            foreach (var (ticker, item) in context)                
                if (item.Weight > 0.0)
                    tickers.Add(ticker);

            return tickers;
        }

        public static List<string> GetTickers(this Dictionary<string, MomentumTickerContext> context)
        {
            List<string> tickers = [];

            foreach (var (ticker, _) in context)
                tickers.Add(ticker);

            return tickers;
        }

        public static double GetWeightSum(this Dictionary<string, MomentumTickerContext> context)
        {
            double sum = 0.0;

            foreach (var (ticker, item) in context)
                sum += item.Weight;

            return sum;
        }

        public static double GetCostSum(this Dictionary<string, MomentumTickerContext> context)
        {
            double sum = 0.0;

            foreach (var (ticker, item) in context)
                sum += item.Cost;

            return sum;
        }
    }
}
