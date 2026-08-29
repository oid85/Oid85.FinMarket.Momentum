using Oid85.FinMarket.Momentum.Core.Requests.ApiClient;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients
{
    /// <summary>
    /// Клиент сервиса FinMarket.Storage
    /// </summary>
    public interface IStorageApiClient
    {
        /// <summary>
        /// Получить свечи
        /// </summary>
        Task<GetCandleListResponse> GetCandleListAsync(GetCandleListRequest request);

        /// <summary>
        /// Получить инструменты
        /// </summary>
        Task<GetInstrumentListResponse> GetInstrumentListAsync(GetInstrumentListRequest request);
    }
}
