namespace Oid85.FinMarket.Algo.Core.Requests
{
    public class EditPortfolioTotalSumRequest
    {
        public string PortfolioName { get; set; } = string.Empty;
        public double TotalSum { get; set; }
    }
}
