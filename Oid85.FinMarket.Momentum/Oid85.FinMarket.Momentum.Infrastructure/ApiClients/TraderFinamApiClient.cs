using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Exceptions;
using Oid85.FinMarket.Momentum.Core.Requests.ApiClient;
using Oid85.FinMarket.Momentum.Core.Responses.ApiClient;

namespace Oid85.FinMarket.Momentum.Infrastructure.ApiClients
{
    /// <inheritdoc />
    public class TraderFinamApiClient(
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory)
        : ITraderFinamApiClient
    {
        /// <inheritdoc />
        public async Task<PortfolioInfoResponse> GetPortfolioInfoAsync(PortfolioInfoRequest request) =>
            await GetResponseAsync<PortfolioInfoRequest, PortfolioInfoResponse>("/api/trader-finam/portfolio-info", request);

        private async Task<TResponse> GetCachedDataAsync<TRequest, TResponse>(string url, TRequest request) where TResponse : new()
        {
            string key = StringUtils.GetMd5($"{nameof(TRequest)}_{JsonSerializer.Serialize(request)}");

            if (memoryCache.TryGetValue(key, out TResponse? cacheResponse))
                return cacheResponse;

            else
            {
                var response = await GetResponseAsync<TRequest, TResponse>(url, request);
                memoryCache.Set(key, response, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(60)));

                return response;
            }
        }

        private async Task<TResponse> GetResponseAsync<TRequest, TResponse>(string url, TRequest request) where TResponse : new()
        {
            try
            {
                var content = JsonContent.Create(request);
                using var httpResponse = await SendPostRequestAsync(url, content);
                var data = await httpResponse.Content.ReadFromJsonAsync<TResponse>();
                return data ?? new TResponse();
            }

            catch (Exception exception)
            {
                throw new CustomBusinessException("500", "Ошибка при выполнении запроса", exception);
            }
        }

        private async Task<HttpResponseMessage> SendPostRequestAsync(string url, HttpContent content)
        {
            using var httpClient = httpClientFactory.CreateClient(KnownHttpClients.FinMarketTraderFinamServiceApiClient);
            return await httpClient.PostAsync(url, content);
        }
    }
}
