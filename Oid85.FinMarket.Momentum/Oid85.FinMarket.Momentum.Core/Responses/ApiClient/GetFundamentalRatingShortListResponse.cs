namespace Oid85.FinMarket.Momentum.Core.Responses.ApiClient
{
    public class GetFundamentalRatingShortListResponse
    {
        public GetFundamentalRatingShortListResult Result { get; set; } = new();
    }

    public class GetFundamentalRatingShortListResult
    {
        public List<FundamentalRatingShortListItem> Items { get; set; } = [];
    }

    public class FundamentalRatingShortListItem
    {
        public string Ticker { get; set; } = string.Empty;
        public double Score { get; set; }
        public double DividendYieldRatio { get; set; }
        public double DividendAristocratRatio { get; set; }
    }
}
