namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class PositionData
    {
        public string Ticker { get; set; } = string.Empty;
        public Candle Candle { get; set; } = new Candle();
        public int Lot { get; set; } = 1;
        public double Weight { get; set; } = 0.0;
        public double Cost { get; set; } = 0.0;
        public double Size { get; set; } = 0.0;
        public double Stop { get; set; } = 0.0;
        public int CountBuy { get; set; } = 0;
        public int CountTriggerStop { get; set; } = 0;
    }
}
