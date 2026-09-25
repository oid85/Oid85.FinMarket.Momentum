namespace Oid85.FinMarket.Momentum.Core.Responses.ApiClient
{
    public class PortfolioInfoResponse
    {
        public PortfolioInfoResult Result { get; set; } = new();
    }

    public class PortfolioInfoResult
    {
        public decimal TotalSum { get; set; }
        public decimal Money { get; set; }
        public decimal TotalDailyPnl { get; set; }
        public List<PositionDataItem> Positions { get; set; } = [];
    }

    public class PositionDataItem
    {
        public string Ticker { get; set; } = string.Empty;
        public int Size { get; set; }
        public decimal Cost { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal DailyPnl { get; set; }
    }
}
