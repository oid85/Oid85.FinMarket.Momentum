namespace Oid85.FinMarket.Algo.Core.Requests
{
    public class GetBacktestResultRequest
    {
        public string PortfolioName { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public string StrategyParamsHash { get; set; } = string.Empty;
    }
}
