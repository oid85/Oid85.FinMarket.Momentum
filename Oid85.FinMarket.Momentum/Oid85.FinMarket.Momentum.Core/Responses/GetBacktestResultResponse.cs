using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Core.Responses
{
    public class GetBacktestResultResponse
    {
        public List<BacktestResultSeries> PricePanel { get; set; } = [];
        public List<BacktestResultSeries> Equity { get; set; } = [];
    }

    public class BacktestResultSeries
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ColorFill { get; set; } = string.Empty;
        public List<DateValue<double?>> Data { get; set; } = [];
    }
}
