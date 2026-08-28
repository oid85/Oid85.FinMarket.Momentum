namespace Oid85.FinMarket.Algo.Core.Responses
{
    public class StrategyListResponse
    {
        public List<StrategyListItem> Items { get; set; } = [];
    }

    public class StrategyListItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
