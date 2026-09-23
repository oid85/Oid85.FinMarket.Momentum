using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;
using static Oid85.FinMarket.Momentum.Application.Mapping.ApplicationMapper;
using Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients;

namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class MomentumService(
        IOptions<MomentumSettings> options,
        IDataService dataService,
        IParameterRepository parameterRepository,
        ITraderFinamApiClient traderFinamApiClient,
        IServiceProvider serviceProvider)
        : IMomentumService
    {
        public async Task<MonitorResponse> MonitorVersionAsync(MonitorRequest request)
        {
            var momentumSettings = options.Value;

            var tickers = momentumSettings.Tickers;

            var strategy = serviceProvider.GetRequiredKeyedService<MomentumStrategy>($"MomentumStrategy{request.MomentumVersion}");

            strategy.InitMonitorParameters();

            strategy.StartMoneySum = momentumSettings.StartMoneySum;         

            var from = new DateOnly(2021, 2, 1);
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
            
            strategy.TradingProcessor = new TradingProcessor
            {
                CandleData = candleData,
                InstrumentData = instrumentData,
                Tickers = tickers,
                StartMoneySum = momentumSettings.StartMoneySum
            };

            strategy.TradingProcessor.Reset();

            strategy.Execute();

            return ToMonitorResponse(strategy);
        }

        public async Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request)
        {
            await parameterRepository.SetParameterValueAsync($"TotalSum:Momentum", request.TotalSum.ToString("N0"));
            return new();
        }

        public async Task<TerminalResponse> TerminalAsync(TerminalRequest request)
        {
            var monitorResponse = MonitorVersionAsync(new MonitorRequest { MomentumVersion = request.MomentumVersion });



            return new();
        }
    }
}
