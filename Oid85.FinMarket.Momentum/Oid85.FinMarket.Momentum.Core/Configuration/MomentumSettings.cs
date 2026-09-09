namespace Oid85.FinMarket.Momentum.Core.Configuration
{
    public class MomentumSettings
    {
        public int CheckStopsVersion { get; set; }
        public double StartMoneySum { get; set; }
        public int Period { get; set; }
        public int CountTopTickers { get; set; }
        public List<string> Tickers { get; set; }
        public List<int> RebalanceDays { get; set; }
    }
}
