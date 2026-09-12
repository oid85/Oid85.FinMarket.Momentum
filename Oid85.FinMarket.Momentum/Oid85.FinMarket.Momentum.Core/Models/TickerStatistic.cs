namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class TickerStatistic
    {
        public int Number { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int CountBuy { get; set; } = 0;
        public int CountTriggerStop { get; set; } = 0;
        public double CountTriggerStopPercent { get; set; } = 0.0;        
    }
}
