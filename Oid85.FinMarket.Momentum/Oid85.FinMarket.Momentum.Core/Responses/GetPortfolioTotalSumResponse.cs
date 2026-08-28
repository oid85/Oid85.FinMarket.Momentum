namespace Oid85.FinMarket.Algo.Core.Responses
{
    public class GetPortfolioTotalSumResponse
    {
        public string PortfolioName { get; set; } = string.Empty;
        public double TotalSum { get; set; }
    }
}
