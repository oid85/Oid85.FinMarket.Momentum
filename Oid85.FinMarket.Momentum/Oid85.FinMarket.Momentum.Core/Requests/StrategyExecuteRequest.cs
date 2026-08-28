namespace Oid85.FinMarket.Algo.Core.Requests
{
    public class StrategyExecuteRequest
    {
        public string? PortfolioName { get; set; }
        public string? ProcessName { get; set; }
        public bool IsOptimization { get; set; }        
    }
}
