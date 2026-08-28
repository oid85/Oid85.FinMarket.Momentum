using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Algo.Application.Interfaces.Repositories;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Application.Mapping;
using Oid85.FinMarket.Algo.Application.Strategies;
using Oid85.FinMarket.Algo.Common.Extensions;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Common.Utils;
using Oid85.FinMarket.Algo.Core.Configuration;
using Oid85.FinMarket.Algo.Core.Models;
using Oid85.FinMarket.Algo.Core.Requests;
using Oid85.FinMarket.Algo.Core.Responses;

namespace Oid85.FinMarket.Algo.Application.Services
{
    public class AlgoService(
        IDataService dataService,
        IMonitorService monitorService,
        IOptions<AlgoSettings> options,
        IStrategyExecuteResultRepository strategyExecuteResultRepository,
        IParameterRepository parameterRepository,
        IServiceProvider serviceProvider)
        : IAlgoService
    {
        /// <inheritdoc />
        public async Task<BacktestResponse> BacktestAsync(BacktestRequest request)
        {
            var algoSettings = options.Value;
            var portfolioSettingsList = algoSettings.Portfolios;

            if (!string.IsNullOrEmpty(request.PortfolioName))
                portfolioSettingsList = [.. portfolioSettingsList.Where(x => x.Name == request.PortfolioName)];

            foreach (var portfolioSetting in portfolioSettingsList)
            {
                string processName = KnownProcessNames.Backtest;

                await strategyExecuteResultRepository.DeleteAsync(portfolioSetting.Name, processName);

                var strategyExecuteResults = await ExecuteAsync(
                    new()
                    {
                        PortfolioName = portfolioSetting.Name,
                        IsOptimization = false,
                        ProcessName = processName
                    });

                await strategyExecuteResultRepository.AddAsync(strategyExecuteResults);
            }

            return new();
        }

        /// <inheritdoc />
        public async Task<OptimizationResponse> OptimizationAsync(OptimizationRequest request)
        {
            var algoSettings = options.Value;
            var portfolioSettingsList = algoSettings.Portfolios;

            if (!string.IsNullOrEmpty(request.PortfolioName))
                portfolioSettingsList = [.. portfolioSettingsList.Where(x => x.Name == request.PortfolioName)];

            foreach (var portfolioSetting in portfolioSettingsList)
            {
                string processName = KnownProcessNames.Optimization;

                await strategyExecuteResultRepository.DeleteAsync(portfolioSetting.Name, processName);

                var strategyExecuteResults = await ExecuteAsync(
                    new()
                    {
                        PortfolioName = portfolioSetting.Name,
                        IsOptimization = true,
                        ProcessName = processName
                    });

                await strategyExecuteResultRepository.AddAsync(strategyExecuteResults);
            }

            return new();
        }

        /// <inheritdoc />
        public async Task<MonitorResponse> MonitorAsync(MonitorRequest request)
        {
            var algoSettings = options.Value;

            if (string.IsNullOrEmpty(request.PortfolioName))
                request.PortfolioName = algoSettings.Portfolios.First().Name;

            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == request.PortfolioName);
            var enabledStrategyNames = portfolioSettings!.PortfolioStrategies.Where(x => x.Enable).Select(x => x.Name).ToList();

            var strategyExecuteResults = (await ExecuteAsync(
                new()
                {
                    PortfolioName = request.PortfolioName,
                    IsOptimization = false,
                    ProcessName = KnownProcessNames.Backtest
                }))
                .Where(x => enabledStrategyNames.Contains(x.StrategyName))
                .ToList();

            var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-1 * 365));
            var to = DateOnly.FromDateTime(DateTime.Today);
            var dates = DateUtils.GetDates(from, to);
                        
            var response = new MonitorResponse { Dates = dates };

            var portfolioData = await monitorService.GetPortfolioDataAsync(request.PortfolioName, strategyExecuteResults);

            response.Series = 
                [
                    GetPortfolioBacktestSeries(portfolioData.EqiutyCurve, "Капитал, тыс. руб.", KnownColors.Green),
                    GetPortfolioBacktestSeries(portfolioData.DrawdownCurve, "Просадка, тыс. руб.", KnownColors.Red),
                    GetPortfolioBacktestSeries(portfolioData.MoneyCurve, "Ден. средства, тыс. руб.", KnownColors.LightBlue)
                ];

            response.Dates = dates;

            response.PositionWeightData = GetPositionWeightData(portfolioData.PositionWeightData);

            var tickers = algoSettings.TickerLists.Find(x => x.Name == portfolioSettings!.TickerList)!.Tickers;
            var instrumentData = await dataService.GetInstrumentDataAsync(tickers);
            var lots = instrumentData.ToDictionary(k => k.Key, v => v.Value.Lot ?? 1);

            var totalSumResponse = await GetPortfolioTotalSumAsync(new() { PortfolioName = request.PortfolioName });

            response.CurrentPositions = GetCurrentPositions(
                portfolioData.PositionWeightData,
                totalSumResponse.TotalSum, 
                strategyExecuteResults.Count, 
                lots);

            response.Count = strategyExecuteResults.Count;

            response.Yield = GetAverageYearYieldPercent(response.Series[0]);

            var drawdownValues = GetDrawdownValues(response.Series[0]);

            response.MaxDrawdown = drawdownValues.Min();
            response.CurrentDrawdown = drawdownValues.Last();

            return response;
        }

        /// <inheritdoc />
        public async Task<GetBacktestResultListResponse> GetBacktestResultListAsync(GetBacktestResultListRequest request)
        {
            var algoSettings = options.Value;

            if (string.IsNullOrEmpty(request.PortfolioName))
                request.PortfolioName = algoSettings.Portfolios.First().Name;

            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == request.PortfolioName);

            if (string.IsNullOrEmpty(request.StrategyName))
                request.StrategyName = portfolioSettings!.PortfolioStrategies.First().Name;

            var strategyExecuteResults = await strategyExecuteResultRepository.GetAsync(
                request.PortfolioName, request.StrategyName, KnownProcessNames.Backtest);

            return new GetBacktestResultListResponse
            {
                Items = strategyExecuteResults
                .Select(x =>
                new BacktestResultItem
                {
                    Ticker = x.Ticker,
                    PortfolioName = x.PortfolioName,
                    StrategyName = x.StrategyName,
                    StrategyParams = x.StrategyParams,
                    StrategyParamsHash = x.StrategyParamsHash,
                    ProfitFactor = new ColorValue<double>
                    {
                        Value = x.ProfitFactor.RoundTo(2),
                        Color = x.ProfitFactor switch
                        {
                            > 1.0 => KnownColors.Green,
                            _ => KnownColors.White
                        }
                    },
                    RecoveryFactor = new ColorValue<double>
                    {
                        Value = x.RecoveryFactor.RoundTo(2),
                        Color = x.RecoveryFactor switch
                        {
                            > 1.0 => KnownColors.Green,
                            _ => KnownColors.White
                        }
                    },
                    AnnualYieldReturn = new ColorValue<double>
                    {
                        Value = x.AnnualYieldReturn.RoundTo(2),
                        Color = x.AnnualYieldReturn switch
                        {
                            > 15.0 => KnownColors.Green,
                            _ => KnownColors.White
                        }
                    },
                    AverageNetProfitPercent = new ColorValue<double>
                    {
                        Value = x.AverageNetProfitPercent.RoundTo(2),
                        Color = x.AverageNetProfitPercent switch
                        {
                            > 0.0 => KnownColors.Green,
                            _ => KnownColors.White
                        }
                    }
                })
                .OrderBy(x => x.Ticker)
                .ToList()
            };
        }

        /// <inheritdoc />
        public async Task<GetBacktestResultResponse> GetBacktestResultDiagramAsync(GetBacktestResultRequest request)
        {
            var algoSettings = options.Value;

            if (string.IsNullOrEmpty(request.PortfolioName)) return new ();
            if (string.IsNullOrEmpty(request.StrategyName)) return new();
            if (string.IsNullOrEmpty(request.Ticker)) return new();
            if (string.IsNullOrEmpty(request.StrategyParamsHash)) return new();

            var strategyExecuteResults = await ExecuteAsync(
                new()
                {
                    PortfolioName = request.PortfolioName,
                    IsOptimization = false,
                    ProcessName = KnownProcessNames.Backtest
                },
                request.StrategyName,
                request.Ticker);

            var strategyExecuteResult = strategyExecuteResults.Find(x => x.StrategyParamsHash == request.StrategyParamsHash);

            if (strategyExecuteResult is null) return new();

            var from = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
            var to = DateOnly.FromDateTime(DateTime.Today);

            var response = new GetBacktestResultResponse
            {
                PricePanel = [
                    new BacktestResultSeries
                    {
                        Name = "Цена",
                        Color = KnownColors.Blue,
                        ColorFill = KnownColors.Blue,
                        Data = [.. strategyExecuteResult
                            .DiagramPoints                            
                            .Select(x => new DateValue<double?> { Date = x.Date, Value = x.Price })
                            .Where(x => x.Date >= from && x.Date <= to)]
                    },

                    new BacktestResultSeries
                    {
                        Name = "Лонг",
                        Color = KnownColors.Green,
                        ColorFill = KnownColors.Green,
                        Data = [.. strategyExecuteResult
                            .DiagramPoints
                            .Select(x => new DateValue<double?> { Date = x.Date, Value = x.LongPositionIndicator })
                            .Where(x => x.Date >= from && x.Date <= to)]
                    }
                ],

                Equity = [
                    new BacktestResultSeries
                    {
                        Name = "Капитал",
                        Color = KnownColors.Green,
                        ColorFill = KnownColors.Green,
                        Data = [.. strategyExecuteResult
                            .EqiutyCurve                            
                            .Expand(strategyExecuteResult.StartDate, strategyExecuteResult.EndDate)                            
                            .Select(x => new DateValue<double?> { Date = x.Key, Value = x.Value })
                            .Where(x => x.Date >= from && x.Date <= to)]
                    },

                    new BacktestResultSeries
                    {
                        Name = "Просадка",
                        Color = KnownColors.Red,
                        ColorFill = KnownColors.Red,
                        Data = [.. strategyExecuteResult
                            .DrawdownCurve
                            .Expand(strategyExecuteResult.StartDate, strategyExecuteResult.EndDate)
                            .Select(x => new DateValue<double?> { Date = x.Key, Value = -1 * x.Value })
                            .Where(x => x.Date >= from && x.Date <= to)]
                    }
                ]
            };

            return response;
        }

        private static double GetAverageYearYieldPercent(PortfolioBacktestSeries series)
        {
            double first = series.Data.First().Value ?? 0.0;
            double last = series.Data.Last().Value ?? 0.0;

            if (last == 0.0) return 0.0;

            var startDate = series.Data.First().Date;
            var endDate = series.Data.Last().Date;

            var years = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MaxValue)).TotalDays / 365.0;

            return ((last - first) / first * 100.0 / years).RoundTo(2);
        }

        private static List<double> GetDrawdownValues(PortfolioBacktestSeries series)
        {
            List<double> equity = [.. series.Data.Select(x => x.Value ?? 0.0)];
            List<double> drawdown = [];

            for (int i = 0; i < equity.Count; i++)
            {
                if (i == 0)
                    drawdown.Add(0.0);

                else
                {
                    var maxEquity = equity.Take(i).Max();
                    drawdown.Add(equity[i] >= maxEquity ? 0.0 : ((equity[i] - maxEquity) / maxEquity * 100.0).RoundTo(2));
                }
            }

            return drawdown;
        }

        /// <inheritdoc />
        public async Task<GetPortfolioTotalSumResponse> GetPortfolioTotalSumAsync(GetPortfolioTotalSumRequest request)
        {
            double totalSum = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync($"TotalSum:{request.PortfolioName}")) ?? "0").Replace(" ", "").Trim());
            return new() { PortfolioName = request.PortfolioName , TotalSum = totalSum };
        }

        /// <inheritdoc />
        public async Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request)
        {
            await parameterRepository.SetParameterValueAsync($"TotalSum:{request.PortfolioName}", request.TotalSum.ToString("N0"));
            return new();
        }

        private static List<PositionWeightData> GetPositionWeightData(List<(string Ticker, List<DateWeight> WeightData)> positionWeightData) =>
            [.. positionWeightData
                .Where(x => x.Ticker != KnownTickers.TMON)
                .Select(x => new PositionWeightData
                {
                    Ticker = x.Ticker,
                    PositionWeightItems = [.. x.WeightData
                        .Select(xx => new PositionWeightItem
                        {
                            Date = xx.Date,
                            Weight = xx.Weight,
                            ColorFill = xx.Weight > 0 
                                ? KnownColors.Green 
                                : KnownColors.White
                        })]
                })];

        private List<PositionItem> GetCurrentPositions(
            List<(string Ticker, List<DateWeight> WeightData)> positionWeightData, 
            double money,
            int totalUnits,
            Dictionary<string, int> lots)
        {
            List<(string Ticker, DateWeight Weight)> lastPositionWeight = 
                [.. positionWeightData
                    .Where(x => x.Ticker != KnownTickers.TMON)
                    .Select(x => (x.Ticker, x.WeightData.Last()))];

            var baseUnit = money / totalUnits;

            var result = new List<PositionItem>();

            foreach (var item in lastPositionWeight)
            {
                var price = dataService.GetPrice(item.Ticker, item.Weight.Date)!.Value;
                double tickerCost = baseUnit * item.Weight.Weight;
                double tickerSize = tickerCost / price;
                tickerSize /= lots[item.Ticker];
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lots[item.Ticker];
                int size = Convert.ToInt32(tickerSize);

                result.Add(
                    new() 
                    {
                        Date = item.Weight.Date, 
                        Ticker = item.Ticker,
                        Weight = item.Weight.Weight,
                        Size = size,
                        Cost = tickerCost.RoundTo(2)
                    });
            }

            double sumPositions = result.Sum(x => x.Cost);

            result.Add(
                new()
                {
                    Ticker = KnownTickers.TMON,
                    Cost = (money - sumPositions).RoundTo(2)
                });

            return result;
        }

        private static PortfolioBacktestSeries GetPortfolioBacktestSeries(List<DateValue<double>> dateValues, string description, string color) => 
            new()
            {
                Name = $"{description}",
                Color = color,
                ColorFill = color,
                Data = [.. dateValues.Select(x => new PortfolioBacktestSeriesItem { Date = x.Date, Value = (x.Value / 1000.0).RoundTo(4) })]
            };

        /// <inheritdoc />
        public async Task<PortfolioListResponse> PortfolioListAsync(PortfolioListRequest request)
        {
            var algoSettings = options.Value;

            return new PortfolioListResponse
            {
                Items = [.. algoSettings.Portfolios.Select(x => new PortfolioListItem { Name = x.Name, Description = x.Description })]
            };
        }

        /// <inheritdoc />
        public async Task<StrategyListResponse> StrategyListAsync(StrategyListRequest request)
        {
            var algoSettings = options.Value;

            if (string.IsNullOrEmpty(request.PortfolioName))
                request.PortfolioName = algoSettings.Portfolios.First().Name;

            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == request.PortfolioName);

            return new StrategyListResponse
            {
                Items = [.. portfolioSettings!.PortfolioStrategies
                    .Where(x => x.Enable)
                    .Select(x => new StrategyListItem { Name = x.Name })]
            };
        }

        /// <summary>
        /// Выполнить стратегии портфеля
        /// </summary>
        private async Task<List<StrategyExecuteResult>> ExecuteAsync(StrategyExecuteRequest request)
        {
            var strategyExecuteResults = new List<StrategyExecuteResult>();

            var algoSettings = options.Value;
            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == request.PortfolioName);
            var tickers = algoSettings.TickerLists.Find(x => x.Name == portfolioSettings!.TickerList)!.Tickers;
            var candleData = await GetCandleDataAsync(request.IsOptimization, tickers);
            var strategyData = GetStrategyData();

            foreach (var portfolioStrategySettings in portfolioSettings!.PortfolioStrategies)
            {
                var strategy = strategyData[portfolioStrategySettings.Name];

                foreach (var ticker in tickers)
                {
                    strategy.Ticker = ticker;
                    strategy.CandleData = candleData;
                    strategy.PortfolioName = portfolioSettings.Name;
                    strategy.StabilizationPeriod = algoSettings.BacktestSettings.StabilizationPeriodInCandles;
                    strategy.ProcessName = request.ProcessName!;

                    if (strategy.Candles is []) continue;

                    var parameterSets = request.IsOptimization
                        ? GetParameterSets(strategy.StrategyParameters)
                        : await GetParameterSets(portfolioSettings.Name, strategy.StrategyName, ticker);

                    var results = Execute(strategy, parameterSets);

                    strategyExecuteResults.AddRange(results);
                }
            }

            return strategyExecuteResults;
        }

        /// <summary>
        /// Выполнить стратегии портфеля
        /// </summary>
        private async Task<List<StrategyExecuteResult>> ExecuteAsync(StrategyExecuteRequest request, string strategyName, string tickerName)
        {
            var strategyExecuteResults = new List<StrategyExecuteResult>();

            var algoSettings = options.Value;
            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == request.PortfolioName);
            var tickers = algoSettings.TickerLists.Find(x => x.Name == portfolioSettings!.TickerList)!.Tickers;
            var candleData = await GetCandleDataAsync(request.IsOptimization, tickers);
            var strategyData = GetStrategyData();

            foreach (var portfolioStrategySettings in portfolioSettings!.PortfolioStrategies.Where(x => x.Name == strategyName))
            {
                var strategy = strategyData[portfolioStrategySettings.Name];

                foreach (var ticker in tickers.Where(x => x == tickerName))
                {
                    strategy.Ticker = ticker;
                    strategy.CandleData = candleData;
                    strategy.PortfolioName = portfolioSettings.Name;
                    strategy.StabilizationPeriod = algoSettings.BacktestSettings.StabilizationPeriodInCandles;
                    strategy.ProcessName = request.ProcessName!;

                    if (strategy.Candles is []) continue;

                    var parameterSets = request.IsOptimization
                        ? GetParameterSets(strategy.StrategyParameters)
                        : await GetParameterSets(portfolioSettings.Name, strategy.StrategyName, ticker);

                    var results = Execute(strategy, parameterSets);

                    strategyExecuteResults.AddRange(results);
                }
            }

            return strategyExecuteResults;
        }

        /// <summary>
        /// Выполнить стратегию на наборах параметров
        /// </summary>
        private List<StrategyExecuteResult> Execute(Strategy strategy, List<Dictionary<string, int>> parameterSets)
        {
            var results = new List<StrategyExecuteResult>();

            foreach (var parameterSet in parameterSets)
            {
                var result = Execute(strategy, parameterSet);

                if (result is not null)
                    results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Выполнить стратегию на наборе параметров
        /// </summary>
        private StrategyExecuteResult? Execute(Strategy strategy, Dictionary<string, int> parameterSet)
        {
            var algoSettings = options.Value;
            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == strategy.PortfolioName);

            StrategyExecuteResult result;

            try
            {
                if (parameterSet.Count == 0) return null;

                strategy.Init(parameterSet, portfolioSettings!.Money);
                strategy.Execute();
                result = ApplicationMapper.MapToStrategyExecuteResult(strategy);
                result.ResultMessage = "Success";
            }

            catch (Exception exception)
            {
                result = ApplicationMapper.MapToStrategyExecuteResult(strategy);
                result.ResultMessage = $"Error. {exception.Message}";
            }

            return result;
        }

        /// <summary>
        /// Получить стратегии
        /// </summary>
        private Dictionary<string, Strategy> GetStrategyData()
        {
            var algoSettings = options.Value;

            var strategyNames = algoSettings
                .Portfolios
                .SelectMany(x => x.PortfolioStrategies.Select(xx => xx.Name))
                .Distinct()
                .ToList();

            var strategyDictionary = new Dictionary<string, Strategy>();

            foreach (var strategyName in strategyNames)
            {
                var strategy = serviceProvider.GetRequiredKeyedService<Strategy>(strategyName);

                strategyDictionary.TryAdd(strategyName, strategy);
            }

            return strategyDictionary;
        }

        /// <summary>
        /// Получение свечей
        /// </summary>
        private async Task<Dictionary<string, List<Candle>>> GetCandleDataAsync(bool isOptimization, List<string> tickers)
        {
            var dateRange = isOptimization ? GetOptimizationDates() : GetBacktestDates();

            var result = new Dictionary<string, List<Candle>>();

            var candleData = await dataService.GetCandleDataAsync(tickers);

            foreach (string ticker in tickers)
            {
                var candles = candleData[ticker]
                    .Where(x => x.Date >= dateRange.From)
                    .Where(x => x.Date <= dateRange.To)
                    .ToList();

                if (candles.Count == 0)
                    continue;

                for (int i = 0; i < candles.Count; i++)
                    candles[i].Index = i;

                result.TryAdd(ticker, candles);
            }

            return result;
        }

        /// <summary>
        /// Получить даты для оптимизации
        /// </summary>
        private (DateOnly From, DateOnly To) GetOptimizationDates()
        {
            var algoSettings = options.Value;

            var today = DateOnly.FromDateTime(DateTime.Today);

            var from = today
                .AddDays(-1 * algoSettings.BacktestSettings.BacktestWindowInDays)
                .AddDays(-1 * algoSettings.BacktestSettings.StabilizationPeriodInCandles)
                .AddDays(-1 * algoSettings.BacktestSettings.BacktestShiftInDays);

            var to = today.AddDays(-1 * algoSettings.BacktestSettings.BacktestShiftInDays);

            return (from, to);
        }

        /// <summary>
        /// Получить даты для бэктеста
        /// </summary>
        private (DateOnly From, DateOnly To) GetBacktestDates()
        {
            var algoSettings = options.Value;

            var today = DateOnly.FromDateTime(DateTime.Today);

            var from = today
                .AddDays(-1 * algoSettings.BacktestSettings.BacktestWindowInDays)
                .AddDays(-1 * algoSettings.BacktestSettings.StabilizationPeriodInCandles);

            var to = today;

            return (from, to);
        }

        /// <summary>
        /// Получить параметры стратегии для оптимизации
        /// </summary>
        private static List<Dictionary<string, int>> GetParameterSets(List<StrategyParameter> strategyParams)
        {
            var result = new List<Dictionary<string, int>>();

            switch (strategyParams.Count)
            {
                case 1:
                    for (int paramValue1 = strategyParams[0].Min; paramValue1 <= strategyParams[0].Max; paramValue1 += strategyParams[0].Step)
                        result.Add(
                            new Dictionary<string, int>
                            {
                                [strategyParams[0].Name] = paramValue1
                            });

                    return result;

                case 2:
                    for (int paramValue1 = strategyParams[0].Min; paramValue1 <= strategyParams[0].Max; paramValue1 += strategyParams[0].Step)
                        for (int paramValue2 = strategyParams[1].Min; paramValue2 <= strategyParams[1].Max; paramValue2 += strategyParams[1].Step)
                            result.Add(
                                new Dictionary<string, int>
                                {
                                    [strategyParams[0].Name] = paramValue1,
                                    [strategyParams[1].Name] = paramValue2
                                });

                    return result;

                case 3:
                    for (int paramValue1 = strategyParams[0].Min; paramValue1 <= strategyParams[0].Max; paramValue1 += strategyParams[0].Step)
                        for (int paramValue2 = strategyParams[1].Min; paramValue2 <= strategyParams[1].Max; paramValue2 += strategyParams[1].Step)
                            for (int paramValue3 = strategyParams[2].Min; paramValue3 <= strategyParams[2].Max; paramValue3 += strategyParams[2].Step)
                                result.Add(
                                    new Dictionary<string, int>
                                    {
                                        [strategyParams[0].Name] = paramValue1,
                                        [strategyParams[1].Name] = paramValue2,
                                        [strategyParams[2].Name] = paramValue3
                                    });

                    return result;
            }

            throw new Exception("Количество параметров не может быть больше трёх");
        }

        /// <summary>
        /// Получить параметры стратегии для бэктеста
        /// </summary>
        private async Task<List<Dictionary<string, int>>> GetParameterSets(string portfolioName, string strategyName, string ticker)
        {
            var strategyExecuteResults = (await strategyExecuteResultRepository.GetFilteredAsync())
                .Where(x => x.PortfolioName == portfolioName)
                .Where(x => x.StrategyName == strategyName)
                .Where(x => x.ProcessName == KnownProcessNames.Optimization)
                .ToList();

            if (strategyExecuteResults is []) return [];

            var strategyExecuteResultsByTicker = strategyExecuteResults
                .Where(x => x.Ticker == ticker)
                .ToList();

            if (strategyExecuteResultsByTicker is []) return [];

            double tickerPercentLimit = 3.0;

            double tickerPercent = Convert.ToDouble(strategyExecuteResultsByTicker.Count) / Convert.ToDouble(strategyExecuteResults.Count) * 100.0;

            // Если по тикеру результатов мало (менее tickerPercentLimit %), то пропускаем эти результаты
            if (tickerPercent < tickerPercentLimit) return [];

            var parameterSets = strategyExecuteResultsByTicker
                .Select(x => JsonSerializer.Deserialize<Dictionary<string, int>>(x.StrategyParams) ?? [])
                .ToList();

            return parameterSets ?? [];
        }
    }
}
