namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumContext
    {
        public Dictionary<string, MomentumTickerContext> Data { get; set; } = [];
    }
}
