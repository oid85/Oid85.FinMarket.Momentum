using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.Services
{
    public interface IMomentumService
    {
        Task<MonitorResponse> MonitorAsync(MonitorRequest request);
    }
}
