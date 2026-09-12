using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Core.Responses
{
    public class BacktestResultResponse
    {
        public List<DiagramSeries> EquitySeries { get; set; } = [];
        public List<BacktestResult> BacktestResults { get; set; } = [];
    }

    public class BacktestResult
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование стратегии
        /// </summary>
        public string StrategyName { get; set; }

        /// <summary>
        /// Параметры стратегии
        /// </summary>
        public string StrategyParams { get; set; }

        /// <summary>
        /// Recovery Factor
        /// </summary>
        public double RecoveryFactor { get; set; }

        /// <summary>
        /// Net Profit
        /// </summary>
        public double NetProfit { get; set; }

        /// <summary>
        /// Доходность годовая, %;
        /// </summary>
        public double AnnualYieldReturn { get; set; }
    }
}
