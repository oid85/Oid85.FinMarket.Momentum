namespace Oid85.FinMarket.Momentum.Core.Configuration
{
    public class MomentumSettings
    {
        public StrategyExecuteResultFilterSettings StrategyExecuteResultFilter { get; set; } = new();
        public double StartMoneySum { get; set; }
        public List<string> DividendShareTickers { get; set; } = [];
        public List<string> SevenEtfTickers { get; set; } = [];
    }

    public class StrategyExecuteResultFilterSettings
    {
        public double MinProfitFactor { get; set; }
        public double MinRecoveryFactor { get; set; }
        public double MinWinningTradesPercent { get; set; }
        public double MaxWinningTradesPercent { get; set; }
        public double MinAnnualYieldReturn { get; set; }
        public double MaxDrawdownPercent { get; set; }
    }
}
