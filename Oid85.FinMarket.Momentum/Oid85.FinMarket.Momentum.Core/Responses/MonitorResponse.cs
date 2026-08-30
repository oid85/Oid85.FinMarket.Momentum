using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class MonitorResponse
    {
        public List<DiagramSeries> Series { get; set; } = [];
        public List<PortfolioPosition> CurrentPositions { get; set; } = [];        
        public double Yield { get; set; }
        public double MaxDrawdown { get; set; }
        public double CurrentDrawdown { get; set; }
    }
}
