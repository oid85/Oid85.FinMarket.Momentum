using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class MonitorResponse
    {
        public List<DiagramSeries> BacktestSeries { get; set; } = [];
        public List<DiagramSeries> PriceDynamicSeries { get; set; } = [];
        public List<PortfolioPosition> CurrentPositions { get; set; } = [];        
        public double Yield { get; set; }
        public double Yield2021 { get; set; }
        public double Yield2022 { get; set; }
        public double Yield2023 { get; set; }
        public double Yield2024 { get; set; }
        public double Yield2025 { get; set; }
        public double Yield2026 { get; set; }
        public double MaxDrawdown { get; set; }
        public double CurrentDrawdown { get; set; }
        public double TotalSumLife { get; set; }
    }
}
