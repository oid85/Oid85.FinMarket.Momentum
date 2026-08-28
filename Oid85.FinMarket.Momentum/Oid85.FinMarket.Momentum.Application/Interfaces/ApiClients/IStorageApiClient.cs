using Oid85.FinMarket.Algo.Core.Requests.ApiClient;
using Oid85.FinMarket.Algo.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Algo.Application.Interfaces.ApiClients
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
