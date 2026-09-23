using Oid85.FinMarket.Momentum.Core.Requests.ApiClient;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients
{
    /// <summary>
    /// Клиент сервиса FinMarket.TraderFinam
    /// </summary>
    public interface ITraderFinamApiClient
    {
        /// <summary>
        /// Получить информацию о портфеле
        /// </summary>
        Task<PortfolioInfoResponse> GetPortfolioInfoAsync(PortfolioInfoRequest request);
    }
}
