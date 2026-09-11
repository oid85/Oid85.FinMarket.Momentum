using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;

namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class BacktestService(
        IOptions<MomentumSettings> options) 
        : IBacktestService
    {
        public async Task<BacktestResponse> BacktestAsync(BacktestRequest request)
        {
            var momentumSettings = options.Value;

            var from = new DateOnly(2021, 1, 1);
            var to = DateOnly.FromDateTime(DateTime.Today);



            var response = new BacktestResponse();



            return response;
        }
    }
}
