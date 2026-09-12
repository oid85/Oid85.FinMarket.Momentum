using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.Services
{
    public interface IBacktestService
    {
        Task<BacktestResponse> BacktestAsync(BacktestRequest request);
        Task<BacktestResultResponse> BacktestResultAsync(BacktestResultRequest request);
    }
}
