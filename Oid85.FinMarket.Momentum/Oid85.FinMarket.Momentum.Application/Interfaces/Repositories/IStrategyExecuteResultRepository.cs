using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.Repositories
{
    public interface IStrategyExecuteResultRepository
    {
        Task AddAsync(List<StrategyExecuteResult> strategyExecuteResults);
        Task<List<StrategyExecuteResult>> GetFilteredAsync();
        Task<List<StrategyExecuteResult>> GetAsync();
        Task DeleteAsync();
    }
}
