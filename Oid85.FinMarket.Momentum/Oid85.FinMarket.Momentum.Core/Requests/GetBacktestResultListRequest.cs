namespace Oid85.FinMarket.Algo.Core.Requests
{
    public class GetBacktestResultListRequest
    {
        public string PortfolioName { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
    }
}
