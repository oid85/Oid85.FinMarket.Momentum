namespace Oid85.FinMarket.Algo.Core.Responses
{
    public class MonitorResponse
    {
        public List<DateOnly> Dates { get; set; } = [];
        public List<PositionWeightData> PositionWeightData { get; set; } = [];
        public List<PositionItem> CurrentPositions { get; set; } = [];
        public List<PortfolioBacktestSeries> Series { get; set; } = [];
        public int Count { get; set; }
        public double Yield { get; set; }
        public double MaxDrawdown { get; set; }
        public double CurrentDrawdown { get; set; }
    }

    public class PositionWeightData
    {
        public string Ticker { get; set; } = string.Empty;
        public List<PositionWeightItem> PositionWeightItems { get; set; } = [];
    }

    public class PositionWeightItem
    {
        public DateOnly Date { get; set; }
        public string ColorFill { get; set; } = "#FFFFFF";
        public int? Weight { get; set; } = null;
    }

    public class PositionItem
    {
        public DateOnly Date { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int Size { get; set; }
        public double Cost { get; set; }
    }

    public class PortfolioBacktestSeries
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ColorFill { get; set; } = string.Empty;
        public List<PortfolioBacktestSeriesItem> Data { get; set; } = [];
    }

    public class PortfolioBacktestSeriesItem
    {
        public DateOnly Date { get; set; }
        public double? Value { get; set; } = null;
    }
}
