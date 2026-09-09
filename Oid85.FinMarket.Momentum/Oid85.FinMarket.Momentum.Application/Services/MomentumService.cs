using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;
using static Oid85.FinMarket.Momentum.Application.Mapping.ApplicationMapper;

namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class MomentumService(
        IOptions<MomentumSettings> options,
        IDataService dataService,
        IParameterRepository parameterRepository,
        IServiceProvider serviceProvider)
        : IMomentumService
    {
        public async Task<MonitorResponse> MonitorVersionAsync(MonitorRequest request)
        {
            var momentumSettings = options.Value;

            var tickers = momentumSettings.Tickers;

            var strategy = serviceProvider.GetRequiredKeyedService<MomentumStrategy>($"MomentumStrategyVersion{request.MomentumVersion}");

            strategy.StartMoneySum = momentumSettings.StartMoneySum;
            strategy.Money = momentumSettings.StartMoneySum;
            strategy.TotalSum = momentumSettings.StartMoneySum;
            strategy.Period = momentumSettings.Period;
            strategy.CounTopTickers = momentumSettings.CountTopTickers;
            strategy.RebalanceDays = momentumSettings.RebalanceDays;

            var from = new DateOnly(2021, 1, 1);
            var to = DateOnly.FromDateTime(DateTime.Today);

            strategy.From = from;
            strategy.To = to;

            var candleData = await dataService.GetCandleDataAsync(tickers);
            candleData.TryAdd(MON, await dataService.GetMoneyEquivalentCandlesAsync(from, to));
            strategy.CandleData = candleData;
            
            var instrumentData = await dataService.GetInstrumentDataAsync(tickers);
            strategy.PositionData = tickers.ToDictionary(k => k, v => new PositionData { Ticker = v, Lot = instrumentData[v].Lot ?? 1 });
            strategy.PositionData.TryAdd(MON, new PositionData { Ticker = MON, Lot = 1 });

            strategy.TotalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            strategy.Execute();

            return Map(strategy);
        }

        public async Task<BacktestResponse> BacktestAsync(BacktestRequest request)
        {
            var momentumSettings = options.Value;

            var from = new DateOnly(2021, 1, 1);
            var to = DateOnly.FromDateTime(DateTime.Today);



            var response = new BacktestResponse();



            return response;
        }

        public async Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request)
        {
            await parameterRepository.SetParameterValueAsync($"TotalSum:Momentum", request.TotalSum.ToString("N0"));
            return new();
        }
    }
}
