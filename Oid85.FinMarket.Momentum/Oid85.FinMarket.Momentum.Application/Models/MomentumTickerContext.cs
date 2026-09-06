namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumTickerContext
    {
        public DateOnly Date { get; set; }
        public string Ticker { get; set; } = string.Empty;
    }
}
