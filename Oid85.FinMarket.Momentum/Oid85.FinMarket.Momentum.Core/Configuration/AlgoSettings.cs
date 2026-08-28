namespace Oid85.FinMarket.Algo.Core.Configuration
{
    public class AlgoSettings
    {
        public BacktestSettings BacktestSettings { get; set; }
        public StrategyExecuteResultFilterSettings StrategyExecuteResultFilter { get; set; }
        public List<PortfolioSettings> Portfolios { get; set; }
        public List<TickerListSettings> TickerLists { get; set; }
    }

    public class BacktestSettings
    {
        public int StabilizationPeriodInCandles { get; set; }
        public int OptimizationWindowInDays { get; set; }
        public int BacktestWindowInDays { get; set; }
        public int BacktestShiftInDays { get; set; }
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

    public class PortfolioSettings
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Money { get; set; }
        public string TickerList { get; set; }
        public List<PortfolioStrategySettings> PortfolioStrategies { get; set; }
    }

    public class PortfolioStrategySettings
    {
        public string Name { get; set; }
        public bool Enable { get; set; }
    }

    public class TickerListSettings
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tickers { get; set; }
    }
}
