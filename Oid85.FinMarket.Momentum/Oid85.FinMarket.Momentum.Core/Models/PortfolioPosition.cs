namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class PortfolioPosition
    {
        public int Number { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public double Weight { get; set; }
        public int Size { get; set; }
        public double Cost { get; set; }
        public double StopPrice { get; set; }
    }
}
