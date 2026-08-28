using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Interfaces.Repositories
{
    public interface IStrategyExecuteResultRepository
    {
        Task AddAsync(List<StrategyExecuteResult> strategyExecuteResults);
        Task<List<StrategyExecuteResult>> GetFilteredAsync();
        Task<List<StrategyExecuteResult>> GetAsync(string portfolioName, string strategyName, string processName);
        Task DeleteAsync(string portfolioName, string processName);
    }
}
