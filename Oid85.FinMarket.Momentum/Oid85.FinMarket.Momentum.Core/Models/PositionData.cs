using System.Diagnostics;
using Oid85.FinMarket.Momentum.Common.Extensions;

namespace Oid85.FinMarket.Momentum.Core.Models
{
    [DebuggerDisplay("Weight: {Weight}, Lot: {Lot}")]
    public class PositionData
    {
        public string Ticker { get; set; } = string.Empty;
        public Candle Candle { get; set; } = new Candle();
        public double AverageCandleBody { get; set; } = 0.0;
        public int Lot { get; set; } = 1;
        public double Weight { get; set; } = 0.0;
        public double StopPrice { get; set; } = 0.0;        
        public double CurrentStopSize => Candle.Close == 0.0 || StopPrice == 0.0 ? 0.0 : Math.Abs(Candle.Close - StopPrice).RoundTo(4);
        public double CurrentStopSizePercent => Candle.Close == 0.0 || StopPrice == 0.0 ? 0.0 : (CurrentStopSize / Candle.Close * 100.0).RoundTo(1);
        public bool IsBreakEvenStop { get; set; } = false;
        public double EntryPrice { get; set; } = 0.0;
        public double ProfitPercent => Candle.Close == 0.0 || EntryPrice == 0.0 ? 0.0 : ((Candle.Close - EntryPrice) / Candle.Close * 100.0).RoundTo(1);
        public int CountBuy { get; set; } = 0;
        public int CountTriggerStop { get; set; } = 0;
    }
}
