namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class PortfolioPosition
    {
        public string Ticker { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int Size { get; set; }
        public double Cost { get; set; }
    }
}
