using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Extensions;
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

namespace Oid85.FinMarket.Momentum.Application.Services
{
    public class MomentumService(
        IOptions<MomentumSettings> options,
        IDataService dataService,
        IParameterRepository parameterRepository)
        : IMomentumService
    {
        public async Task<MonitorResponse> MonitorAsync(MonitorRequest request)
        {
            var momentumSettings = options.Value;
            
            var from = new DateOnly(2021, 1, 1);
            var to = DateOnly.FromDateTime(DateTime.Today);
            var dates = DateUtils.GetDates(from, to);

            var tickers = momentumSettings.Tickers;
            var instrumentData = await dataService.GetInstrumentDataAsync(tickers);

            var candleData = await dataService.GetCandleDataAsync(tickers);
            candleData.TryAdd(MON, await dataService.GetMoneyEquivalentCandlesAsync(from, to));

            var context = new MomentumContext
            {
                PeriodInDays = momentumSettings.PeriodInDays,
                CountBestTickers = momentumSettings.CountBestTickers,
                Money = momentumSettings.StartMoneySum,
                TotalSum = momentumSettings.StartMoneySum,
                CandleData = candleData,
                TickerData = tickers.ToDictionary(k => k, v => new MomentumTickerData { Ticker = v, Lot = instrumentData[v].Lot ?? 1 })
            };
            context.TickerData.TryAdd(MON, new MomentumTickerData { Ticker = MON, Lot = 1 });                                   

            var equitySeries = new DiagramSeries
            {
                Name = "Капитал",
                Color = KnownColors.Green,
                ColorFill = KnownColors.Green
            };

            var moneySeries = new DiagramSeries
            {
                Name = "Фонд ликвидности",
                Color = KnownColors.LightBlue,
                ColorFill = KnownColors.LightBlue
            };                       

            foreach (var date in dates)
            {
                context.SetDate(date);

                if (momentumSettings.RebalanceDays.Contains(date.Day))
                {
                    context.ProtocolMessages.Clear();

                    context.SetTopTickers();
                    context.SetWeights();
                    context.UpdateCandles();
                    context.SetStops();
                    context.SetSizes();
                    context.UpdateCosts();
                    context.UpdateMoney();
                    context.UpdateTotalSum();
                    
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        context.AddMessage(ticker, $"Ребалансировка моментума. Позиция {ticker}", KnownColors.LightGreen);
                }

                else
                {
                    context.UpdateCandles();
                    context.UpdateCosts();
                    if (momentumSettings.CheckStopsVersion == 1) context.CheckStopsVersion1();
                    if (momentumSettings.CheckStopsVersion == 2) context.CheckStopsVersion2();
                    context.UpdateTotalSum();
                }

                if (CurrentDrawdown() >= 15.0)
                    foreach (var ticker in context.GetPortfolioNoMonTickers())
                        context.ClosePosition(ticker);

                equitySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = (context.TotalSum / 1000.0).RoundTo(2)
                    });

                moneySeries.Data.Add(
                    new()
                    {
                        Date = date,
                        Value = ((context.Money + context.TickerData[MON].Cost) / 1000.0).RoundTo(2)
                    });
            }

            #region CurrentPositions

            double totalSumLife = Convert.ToDouble(((await parameterRepository.GetParameterValueAsync("TotalSum:Momentum")) ?? "0").Replace(" ", "").Trim());

            var currentPositions = new List<PortfolioPosition>();
            
            foreach (var ticker in context.GetPortfolioTickers())
            {
                var candle = dataService.GetCandle(ticker, dates.Last());
                var lot = context.TickerData[ticker].Lot;

                double baseUnit = totalSumLife / context.GetWeightSum();
                double tickerCost = baseUnit * context.TickerData[ticker].Weight;
                double tickerSize = tickerCost / candle!.Close;
                tickerSize /= lot;
                tickerSize = Math.Truncate(tickerSize);
                tickerSize *= lot;

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Ticker = ticker,
                        Weight = context.TickerData[ticker].Weight,
                        Size = Convert.ToInt32(tickerSize),
                        Cost = tickerCost.RoundTo(2),
                        StopPrice = ticker == MON ? 0.0 : context.TickerData[ticker].Stop.RoundTo(4)
                    });
            }

            List<PortfolioPosition> orderedCurrentPositions = [
                    .. currentPositions.Where(x => x.Ticker != MON).OrderBy(x => x.Ticker),
                    .. currentPositions.Where(x => x.Ticker == MON)
                    ];

            int number = 1;
            foreach (var currentPosition in orderedCurrentPositions)
                currentPosition.Number = number++;

            #endregion

            #region PriceDynamicSeries

            var currentTopTickers = MomentumHelper.GetMomentumTopTickers(candleData, DateOnly.FromDateTime(DateTime.Today), momentumSettings.PeriodInDays, momentumSettings.CountBestTickers);

            from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1 * momentumSettings.PeriodInDays);
            to = DateOnly.FromDateTime(DateTime.Today);

            var priceDynamicSeries = new List<DiagramSeries>();

            foreach (var (ticker, candleList) in candleData.Where(x => x.Key != MON))
            {
                var candlesByDates = candleList.Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                string color = context.GetPortfolioTickers().Contains(ticker)
                    ? KnownColors.Green 
                    : KnownColors.LightBlue;

                priceDynamicSeries.Add(
                    new DiagramSeries
                    {
                        Name = ticker,
                        Color = color,
                        ColorFill = color,
                        Data = [.. candlesByDates
                        .Select(x => 
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = (x.Close / firstPrice).RoundTo(4)
                        })]
                    });
            }

            #endregion

            var drawdownSeries = DiagramSeriesHelper.GetDrawdownSeries(equitySeries);
            var drawdownPercentSeries = DiagramSeriesHelper.GetDrawdownSeries(equitySeries, true);

            double maxDrawdown = drawdownPercentSeries.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1);
            double currentDrawdown = drawdownPercentSeries.Data.Last(x => x.Value.HasValue).Value!.Value.RoundTo(1);

            return new MonitorResponse
            {
                ProtocolMessages = [.. context.ProtocolMessages.OrderByDescending(x => x.Date)],
                TotalSumLife = totalSumLife,
                BacktestSeries = [equitySeries, moneySeries, drawdownSeries],
                PriceDynamicSeries = priceDynamicSeries,
                CurrentPositions = orderedCurrentPositions,
                Yield = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries),
                Yield2021 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2021),
                Yield2022 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2022),
                Yield2023 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2023),
                Yield2024 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2024),
                Yield2025 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2025),
                Yield2026 = DiagramSeriesHelper.GetAnnualPercentageYield(equitySeries, 2026),
                MaxDrawdown = maxDrawdown,
                CurrentDrawdown = currentDrawdown
            };

            double CurrentDrawdown()
            {
                var equityValues = equitySeries.Data.Select(x => x.Value ?? 0.0).ToList();

                if (equityValues.Count == 0) return 0.0;

                double lastEquity = equityValues.Last();
                double maxEquity = equityValues.Max();

                if (maxEquity == 0.0) return 0.0;

                return Math.Abs((maxEquity - lastEquity) / maxEquity * 100.0);
            }
        }

        public async Task<EditPortfolioTotalSumResponse> EditPortfolioTotalSumAsync(EditPortfolioTotalSumRequest request)
        {
            await parameterRepository.SetParameterValueAsync($"TotalSum:Momentum", request.TotalSum.ToString("N0"));
            return new();
        }
    }
}
