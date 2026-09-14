using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Application.Strategies;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;
using static Oid85.FinMarket.Momentum.Application.Mapping.ApplicationMapper;


namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class BacktestService(
        IOptions<MomentumSettings> options,
        IDataService dataService,
        IStrategyExecuteResultRepository strategyExecuteResultRepository,
        IServiceProvider serviceProvider) 
        : IBacktestService
    {
        private readonly List<int> PARAMETER_LIST_PERIOD = [10, 15, 30];
        private readonly List<int> PARAMETER_LIST_COUNT_TOP_TICKRES = [8, 10];
        private readonly List<List<int>> PARAMETER_LIST_REBALANCE_DAYS = [[1], [1, 16], [1, 11, 21]];

        private readonly DateOnly _from = new DateOnly(2021, 1, 1);
        private readonly DateOnly _to = DateOnly.FromDateTime(DateTime.Today);
        private List<string> _tickers = [];
        private Dictionary<string, List<Candle>> _candleData = [];
        private Dictionary<string, Instrument> _instrumentData = [];

        public async Task<BacktestResponse> BacktestAsync(BacktestRequest request)
        {
            var momentumSettings = options.Value;

            _tickers = momentumSettings.Tickers;

            _candleData = await dataService.GetCandleDataAsync(_tickers);
            _candleData.TryAdd(KnownTickers.MON, await dataService.GetMoneyEquivalentCandlesAsync(_from, _to));
            _instrumentData = await dataService.GetInstrumentDataAsync(_tickers);

            await strategyExecuteResultRepository.DeleteAsync();

            await BacktestByStrategyName(nameof(MomentumStrategyVersion1));
            await BacktestByStrategyName(nameof(MomentumStrategyVersion2));
            await BacktestByStrategyName(nameof(MomentumStrategyVersion3));

            return new ();
        }

        private async Task BacktestByStrategyName(string strategyName)
        {
            var momentumSettings = options.Value;

            var strategy = serviceProvider.GetRequiredKeyedService<MomentumStrategy>(strategyName);

            strategy.Name = strategyName;

            List<int> ParameterListPeriod = PARAMETER_LIST_PERIOD;
            List<int> ParameterListCounTopTickers = PARAMETER_LIST_COUNT_TOP_TICKRES;
            List<List<int>> ParameterListRebalanceDays = PARAMETER_LIST_REBALANCE_DAYS;            

            strategy.From = _from;
            strategy.To = _to;

            strategy.CandleData = _candleData;            
            strategy.Data = _tickers.ToDictionary(k => k, v => new PositionData { Ticker = v, Lot = _instrumentData[v].Lot ?? 1 });
            strategy.Data.TryAdd(KnownTickers.MON, new PositionData { Ticker = KnownTickers.MON, Lot = 1 });

            strategy.BalanceProcessor = new BalanceProcessor
            {
                CandleData = _candleData,
                InstrumentData = _instrumentData,
                Tickers = _tickers,
                StartMoneySum = momentumSettings.StartMoneySum
            };

            foreach (var period in ParameterListPeriod)
                foreach (var counTopTickers in ParameterListCounTopTickers)
                    foreach (var rebalanceDays in ParameterListRebalanceDays)
                    {
                        var strategyParams =
                                $"Period = {JsonSerializer.Serialize(period)}; " +
                                $"CounTopTickers = {JsonSerializer.Serialize(counTopTickers)}; " +
                                $"RebalanceDays = {JsonSerializer.Serialize(rebalanceDays)};"
                                ;                        

                        try
                        {
                            strategy.Period = period;
                            strategy.CounTopTickers = counTopTickers;
                            strategy.RebalanceDays = rebalanceDays;

                            strategy.StartMoneySum = momentumSettings.StartMoneySum;

                            strategy.EquitySeries.Data.Clear();
                            strategy.DrawdownSeries.Data.Clear();
                            strategy.MoneySeries.Data.Clear();

                            strategy.BalanceProcessor.Reset();

                            strategy.Execute();

                            var strategyExecuteResult = ToStrategyExecuteResult(strategy);

                            strategyExecuteResult.StrategyParams = strategyParams;
                            strategyExecuteResult.ResultMessage = "OK";

                            await strategyExecuteResultRepository.AddAsync([strategyExecuteResult]);
                        }

                        catch (Exception ex)
                        {
                            var strategyExecuteResult = new StrategyExecuteResult
                            {
                                StrategyParams = strategyParams,
                                ResultMessage = $"Error. {ex.Message}"
                            };

                            await strategyExecuteResultRepository.AddAsync([strategyExecuteResult]);
                        }
                    }
        }

        public async Task<BacktestResultResponse> BacktestResultAsync(BacktestResultRequest request)
        {
            const int countLimit = 30;

            var strategyExecuteResults = await strategyExecuteResultRepository.GetFilteredAsync();

            var orderedBacktestResults = strategyExecuteResults
                    .Select(x =>
                    new BacktestResult
                    {
                        Id = x.Id,
                        StrategyName = x.StrategyName,
                        StrategyParams = x.StrategyParams,
                        RecoveryFactor = x.RecoveryFactor,
                        NetProfit = x.NetProfit,
                        AnnualYieldReturn = x.AnnualYieldReturn,
                    })
                    .OrderByDescending(x => x.AnnualYieldReturn)
                    .Take(countLimit)
                    .ToList();

            int number = 1;
            foreach (var orderedBacktestResult in orderedBacktestResults)
                orderedBacktestResult.Number = number++;

            return new BacktestResultResponse
            {
                BacktestResults = orderedBacktestResults,

                EquitySeries = [.. orderedBacktestResults
                    .Select(x =>
                    new DiagramSeries
                    {
                        Name = $"{x.Number}. {x.StrategyName}: {x.StrategyParams}",
                        Color = KnownColors.DarkBlue,
                        ColorFill = KnownColors.DarkBlue,
                        Data = strategyExecuteResults.Find(xx => xx.Id == x.Id)?.EquityCurve ?? []
                    })]
            };
        }
    }
}
