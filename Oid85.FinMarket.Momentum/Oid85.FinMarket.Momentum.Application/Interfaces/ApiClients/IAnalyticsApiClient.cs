using Oid85.FinMarket.Momentum.Core.Requests.ApiClient;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients
{
    /// <summary>
    /// Клиент сервиса FinMarket.Analytics
    /// </summary>
    public interface IAnalyticsApiClient
    {
        /// <summary>
        /// Получить рейтинг по фундаментальным данным
        /// </summary>
        Task<GetFundamentalRatingShortListResponse> GetFundamentalRatingShortListAsync(GetFundamentalRatingShortListRequest request);
    }
}
