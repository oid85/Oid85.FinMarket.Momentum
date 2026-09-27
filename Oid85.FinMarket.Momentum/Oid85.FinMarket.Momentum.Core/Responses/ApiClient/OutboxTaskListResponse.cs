namespace Oid85.FinMarket.Momentum.Core.Responses.ApiClient
{
    public class OutboxTaskListResponse
    {
        public OutboxTaskListResult Result { get; set; } = new();
    }

    public class OutboxTaskListResult
    {
        public List<OutboxTaskItem> Tasks { get; set; } = [];
    }

    public class OutboxTaskItem
    {
        public Guid Id { get; set; }
        public string? Source { get; set; } = null;
        public string? Type { get; set; } = null;
        public string? Ticker { get; set; } = null;
        public string? TargetSize { get; set; } = null;
        public string? TargetStopPrice { get; set; } = null;
        public string? State { get; set; } = null;
    }
}
