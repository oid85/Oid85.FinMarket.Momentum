using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumTickerContext
    {
        public DateOnly Date { get; set; } = DateOnly.MinValue;
        public string Ticker { get; set; } = string.Empty;
        public Candle Candle { get; set; } = new Candle();
        public int Lot { get; set; } = 1;
        public double Weight { get; set; } = 0.0;
    }
}
