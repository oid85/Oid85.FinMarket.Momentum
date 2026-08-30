namespace Oid85.FinMarket.Momentum.Core.Models
{
    public class DiagramSeries
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ColorFill { get; set; } = string.Empty;
        public List<DateValue<double?>> Data { get; set; } = [];
    }
}
