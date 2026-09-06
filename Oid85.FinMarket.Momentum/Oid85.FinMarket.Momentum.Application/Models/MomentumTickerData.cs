using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumTickerData
    {
        public DateOnly Date { get; set; } = DateOnly.MinValue;
        public string Ticker { get; set; } = string.Empty;
        public Candle Candle { get; set; } = new Candle();
        public int Lot { get; set; } = 1;
        public double Weight { get; set; } = 0.0;
        public double Cost { get; set; } = 0.0;
        public double Size { get; set; } = 0.0;
        public double Stop { get; set; } = 0.0;
    }
}
