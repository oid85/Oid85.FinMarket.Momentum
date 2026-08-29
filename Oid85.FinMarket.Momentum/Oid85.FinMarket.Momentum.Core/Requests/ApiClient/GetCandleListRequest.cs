namespace Oid85.FinMarket.Momentum.Core.Requests.ApiClient
{
    public class GetCandleListRequest
    {
        public string Ticker { get; set; }
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
    }
}
