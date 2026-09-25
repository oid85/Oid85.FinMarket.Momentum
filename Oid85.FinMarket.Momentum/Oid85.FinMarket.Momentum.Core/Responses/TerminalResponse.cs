namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class TerminalResponse
    {
        public double TotalSum { get; set; }
        public List<TerminalRow> Items { get; set; } = [];
    }

    public class TerminalRow
    {
        public string Ticker { get; set; } = string.Empty;
    }
}
