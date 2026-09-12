using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class BacktestResultResponse
    {
        public List<DiagramSeries> EquitySeries { get; set; } = [];
        public List<BacktestResult> BacktestResults { get; set; } = [];
    }

    public class BacktestResult
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public string StrategyName { get; set; }
        public string StrategyParams { get; set; }
        public double RecoveryFactor { get; set; }
        public double NetProfit { get; set; }
        public double AnnualYieldReturn { get; set; }
    }
}
