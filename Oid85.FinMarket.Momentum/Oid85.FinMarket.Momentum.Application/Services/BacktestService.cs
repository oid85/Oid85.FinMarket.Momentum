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
        IParameterRepository parameterRepository,
        IStrategyExecuteResultRepository strategyExecuteResultRepository,
        IServiceProvider serviceProvider) 
        : IBacktestService
    {
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

            await BacktestVersion1();

            return new ();
        }

        public async Task BacktestVersion1()
        {
            var momentumSettings = options.Value;

            var strategy = serviceProvider.GetRequiredKeyedService<MomentumStrategy>(nameof(MomentumStrategyVersion1));

            strategy.From = _from;
            strategy.To = _to;
            
            // strategy.TotalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            strategy.CandleData = _candleData;            
            strategy.PositionData = _tickers.ToDictionary(k => k, v => new PositionData { Ticker = v, Lot = _instrumentData[v].Lot ?? 1 });
            strategy.PositionData.TryAdd(KnownTickers.MON, new PositionData { Ticker = KnownTickers.MON, Lot = 1 });

            List<int> ParameterListPeriod = [10, 15, 30];
            List<int> ParameterListCounTopTickers = [8, 10];
            List<List<int>> ParameterListRebalanceDays = [[1], [1, 16], [1, 11, 21]];

            foreach (var period in ParameterListPeriod)
                foreach (var counTopTickers in ParameterListCounTopTickers)
                    foreach (var rebalanceDays in ParameterListRebalanceDays)
                    {
                        var strategyParams =
                                $"Period = {JsonSerializer.Serialize(period)}; " +
                                $"CounTopTickers = {JsonSerializer.Serialize(counTopTickers)}; " +
                                $"RebalanceDays = {JsonSerializer.Serialize(rebalanceDays)};"
                                ;

                        string strategyName = nameof(MomentumStrategyVersion1);

                        try
                        {
                            strategy.Period = period;
                            strategy.CounTopTickers = counTopTickers;
                            strategy.RebalanceDays = rebalanceDays;

                            strategy.StartMoneySum = momentumSettings.StartMoneySum;
                            strategy.Money = momentumSettings.StartMoneySum;
                            strategy.TotalSum = momentumSettings.StartMoneySum;

                            strategy.EquitySeries.Data.Clear();
                            strategy.DrawdownSeries.Data.Clear();
                            strategy.MoneySeries.Data.Clear();

                            strategy.Execute();

                            var strategyExecuteResult = ToStrategyExecuteResult(strategy);

                            strategyExecuteResult.StrategyName = strategyName;
                            strategyExecuteResult.StrategyParams = strategyParams;
                            strategyExecuteResult.ResultMessage = "OK";

                            await strategyExecuteResultRepository.AddAsync([strategyExecuteResult]);
                        }

                        catch (Exception ex)
                        {
                            var strategyExecuteResult = new StrategyExecuteResult
                            {
                                StrategyName = strategyName,
                                StrategyParams = strategyParams,
                                ResultMessage = $"Error. {ex.Message}"
                            };

                            await strategyExecuteResultRepository.AddAsync([strategyExecuteResult]);
                        }
                    }
        }
    }
}
