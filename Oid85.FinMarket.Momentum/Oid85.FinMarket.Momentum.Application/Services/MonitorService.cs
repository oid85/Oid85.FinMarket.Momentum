using Microsoft.Extensions.Options;
using Oid85.FinMarket.Algo.Application.Helpers;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Common.Utils;
using Oid85.FinMarket.Algo.Core.Configuration;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Services
{
    public class MonitorService(
        IOptions<AlgoSettings> options,
        IDataService dataService) 
        : IMonitorService
    {
        public async Task<PortfolioData> GetPortfolioDataAsync(string portfolioName, List<StrategyExecuteResult> strategyExecuteResults)
        {
            var algoSettings = options.Value;
            var portfolioSettings = algoSettings.Portfolios.Find(x => x.Name == portfolioName);

            if (strategyExecuteResults is [])
                return new();

            double startMoney = portfolioSettings!.Money;

            var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-1 * 365));
            var to = DateOnly.FromDateTime(DateTime.Today);
            List<DateOnly> dates = DateUtils.GetDates(from, to);

            double money = startMoney;
            double totalSum = startMoney;

            List<string> tickers = [.. strategyExecuteResults.Select(x => x.Ticker).Distinct(), KnownTickers.TMON];

            var candleData = await dataService.GetCandleDataAsync(tickers);
            var instrumentData = await dataService.GetInstrumentDataAsync(tickers);
            
            var weights = tickers.ToDictionary(k => k, v => 0);
            var sizes = tickers.ToDictionary(k => k, v => 0);
            var costs = tickers.ToDictionary(k => k, v => 0.0);
            var prices = tickers.ToDictionary(k => k, v => 0.0);
            var lots = instrumentData.ToDictionary(k => k.Key, v => v.Value.Lot ?? 1);

            var positionWeightData = MonitorHelper.GetPositionWeightData(strategyExecuteResults, tickers, dates);

            var portfolioData = new PortfolioData { PositionWeightData = positionWeightData };

            foreach (var date in dates)
            {
                UpdateWeights();
                UpdatePrices();
                UpdateCosts();
                UpdateTotalSum();
                Rebalance();

                portfolioData.EqiutyCurve.Add(new DateValue<double> { Date = date, Value = totalSum });
                portfolioData.MoneyCurve.Add(new DateValue<double> { Date = date, Value = costs[KnownTickers.TMON] });

                if (portfolioData.EqiutyCurve.Count > 0)
                {
                    var maxEquity = portfolioData.EqiutyCurve.MaxBy(x => x.Value);
                    var currentEquity = portfolioData.EqiutyCurve[^1];
                    var currentDrawdown = -1 * (maxEquity!.Value - currentEquity.Value);

                    portfolioData.DrawdownCurve.Add(new DateValue<double> { Date = currentEquity.Date, Value = currentDrawdown });
                }
                
                void UpdateWeights()
                {
                    var positionWeightDataByDate = MonitorHelper.GetPositionWeightDataByDate(positionWeightData, date);
                    
                    weights = tickers.ToDictionary(k => k, v => positionWeightDataByDate.Where(x => x.Ticker == v).Sum(x => x.Weight));
                    int count = weights.Where(x => x.Key != KnownTickers.TMON).ToDictionary().Values.Sum();
                    weights[KnownTickers.TMON] = strategyExecuteResults.Count - count;
                }

                void UpdatePrices()
                {
                    foreach (var ticker in tickers)
                        prices[ticker] = dataService.GetPrice(ticker, date) ?? 0.0;
                }

                void UpdateCosts()
                {
                    foreach (var ticker in tickers)
                        costs[ticker] = prices[ticker] * sizes[ticker];
                }

                void UpdateSizes()
                {
                    double baseUnit = totalSum / weights.Values.Sum();

                    foreach (var ticker in tickers)
                    {
                        if (prices[ticker] == 0.0)
                        {
                            costs[ticker] = 0.0;
                            continue;
                        }

                        double tickerCost = baseUnit * weights[ticker];
                        double tickerSize = tickerCost / prices[ticker];
                        tickerSize /= lots[ticker];
                        tickerSize = Math.Truncate(tickerSize);
                        tickerSize *= lots[ticker];
                        sizes[ticker] = Convert.ToInt32(tickerSize);
                    }
                }

                void UpdateTotalSum()
                {
                    totalSum = costs.Values.Sum() + money;
                }

                void Rebalance()
                {
                    UpdateSizes();
                    UpdateCosts();
                    money = totalSum - costs.Values.Sum();
                }
            }

            return portfolioData;
        }
    }
}
