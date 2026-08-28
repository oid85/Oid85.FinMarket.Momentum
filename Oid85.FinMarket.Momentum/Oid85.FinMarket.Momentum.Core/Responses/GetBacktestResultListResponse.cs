using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Core.Responses
{
    public class GetBacktestResultListResponse
    {
        public List<BacktestResultItem> Items { get; set; } = [];
    }

    public class BacktestResultItem
    {
        public string Ticker { get; set; }
        public string PortfolioName { get; set; }
        public string StrategyName { get; set; }
        public string StrategyParams { get; set; }
        public string StrategyParamsHash { get; set; }
        public ColorValue<double> ProfitFactor { get; set; }
        public ColorValue<double> RecoveryFactor { get; set; }
        public ColorValue<double> AnnualYieldReturn { get; set; }
        public ColorValue<double> AverageNetProfitPercent { get; set; }
    }
}
