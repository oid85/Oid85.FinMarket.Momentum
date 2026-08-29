namespace Oid85.FinMarket.Momentum.Core.Configuration
{
    public class MomentumSettings
    {
        public int PeriodInDays { get; set; }
        public int CountBestTickers { get; set; }
        public List<string> Tickers { get; set; }
    }
}
