namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class ProtocolMessage
    {
        public DateOnly Date { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
