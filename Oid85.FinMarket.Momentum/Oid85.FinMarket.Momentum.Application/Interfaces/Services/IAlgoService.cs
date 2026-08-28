using Oid85.FinMarket.Algo.Core.Requests;
using Oid85.FinMarket.Algo.Core.Responses;

namespace Oid85.FinMarket.Algo.Application.Interfaces.Services
{
    public interface IAlgoService
    {
        /// <summary>
        /// Бэктест стратегий портфеля
        /// </summary>
        Task<BacktestResponse> BacktestAsync(BacktestRequest request);

        /// <summary>
        /// Оптимизация стратегий портфеля
        /// </summary>
        Task<OptimizationResponse> OptimizationAsync(OptimizationRequest request);

        /// <summary>
        /// Мониторинг стратегий
        /// </summary>
        Task<MonitorResponse> MonitorAsync(MonitorRequest request);
        
        /// <summary>
        /// Список портфелей
        /// </summary>
        Task<PortfolioListResponse> PortfolioListAsync(PortfolioListRequest request);

        /// <summary>
        /// Список стратегий
        /// </summary>
        Task<StrategyListResponse> StrategyListAsync(StrategyListRequest request);

        /// <summary>
        /// Получить сумму портфеля
        /// </summary>
        Task<GetPortfolioTotalSumResponse> GetPortfolioTotalSumAsync(GetPortfolioTotalSumRequest request);

        /// <summary>
        /// Редактировать сумму портфеля
        /// </summary>
        Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request);

        /// <summary>
        /// Получить результаты бэктеста
        /// </summary>
        Task<GetBacktestResultListResponse> GetBacktestResultListAsync(GetBacktestResultListRequest request);

        /// <summary>
        /// Получить диаграмму бэктеста
        /// </summary>
        Task<GetBacktestResultResponse> GetBacktestResultDiagramAsync(GetBacktestResultRequest request);
    }
}
