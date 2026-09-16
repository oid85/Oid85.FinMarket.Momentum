using System.Diagnostics;

namespace Oid85.FinMarket.Momentum.Core.Models
{
    [DebuggerDisplay("Price: {Price}, Size: {Size}, Cost: {Cost}")]
    public class Balance
    {
        public double Price { get; set; } = 0.0;
        public double Size { get; set; } = 0.0;
        public double Cost => Price * Size;
    }
}
