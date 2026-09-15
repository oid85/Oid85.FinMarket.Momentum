using System.Globalization;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Models;
using static Oid85.FinMarket.Momentum.Common.KnownConstants.KnownTickers;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumStrategy
    {
        public virtual string Name { get; set; } = string.Empty;

        public int Period { get; set; }

        public int CounTopTickers { get; set; }

        public List<int> RebalanceDays { get; set; } = [];

        public virtual List<string> GetDescription() => [];

        public DateOnly From { get; set; } = new DateOnly(2021, 1, 1);

        public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public List<DateOnly> Dates => DateUtils.GetDates(From, To);

        public double TotalSumLife { get; set; } = 0.0;

        public double StartMoneySum { get; set; } = 0.0;

        public double EndMoneySum => EquitySeries.Data is [] ? StartMoneySum : EquitySeries.Data.Last().Value ?? StartMoneySum;

        public TradingProcessor TradingProcessor { get; set; } = new();

        public Dictionary<string, PositionData> Data { get; set; } = [];

        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

        public List<MomentumMessage> Messages { get; set; } = [];

        public DiagramSeries EquitySeries { get; set; } = new() { Name = "Капитал", Color = KnownColors.Green, ColorFill = KnownColors.Green };

        public DiagramSeries MoneySeries { get; set; } = new() { Name = "Фонд ликвидности", Color = KnownColors.LightBlue, ColorFill = KnownColors.LightBlue };

        public DiagramSeries ShortEquitySeries => DiagramSeriesHelper.GetShortEquitySeries(EquitySeries);

        public DiagramSeries DrawdownSeries => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries, false);

        public DiagramSeries DrawdownSeriesPercent => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries, true);

        public double MaxDrawdownPercent => DrawdownSeriesPercent.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(2);

        public double MaxDrawdown => DrawdownSeries.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(2);

        public double RecoveryFactor => MaxDrawdown == 0.0 ? double.PositiveInfinity : Math.Abs(NetProfit / MaxDrawdown).RoundTo(2);

        public double NetProfit => EndMoneySum > StartMoneySum ? (EndMoneySum - StartMoneySum).RoundTo(2) : 0.0;
        
        public double TotalReturn => EndMoneySum > StartMoneySum ? ((EndMoneySum - StartMoneySum) / StartMoneySum * 100.0).RoundTo(2) : 0.0;

        public double AnnualYieldReturn => EndMoneySum > StartMoneySum ? (TotalReturn / ((To.DayNumber - From.DayNumber) / 365.0)).RoundTo(2) : 0.0;

        public List<string> Tickers => [.. Data.Keys];

        public List<string> TopTickers { get; set; } = [];

        public List<string> PortfolioTickers => [.. Data.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> PortfolioWithoutMonTickers => [.. Data.Values.Where(x => x.Ticker != MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public double WeightSum => Data.Values.Sum(x => x.Weight);        

        public bool IsRebalance => RebalanceDays.Contains(CurrentDate.Day);

        public DateOnly CurrentDate { get; set; } = DateOnly.MinValue;

        public virtual void Execute()
        {

        }

        public virtual void InitMonitorParameters()
        {

        }

        public void AddMessage(string ticker, string message, string colorFill) => Messages.Add(new() { Date = CurrentDate, Ticker = ticker, Message = message, ColorFill = colorFill });

        public void AddRebalanceMessage()
        {
            var nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = " "
            };

            foreach (var ticker in PortfolioWithoutMonTickers)
                AddMessage(ticker, $"Ребалансировка моментума. Позиция {ticker}, {TradingProcessor.BalanceData[ticker].Size.ToString("N0", nfi)} шт., {TradingProcessor.BalanceData[ticker].Cost.RoundTo(2).ToString("N", nfi)} руб., СЛ {Data[ticker].StopPrice.RoundTo(2).ToString("N", nfi)} руб.", KnownColors.LightGreen);
        }

        public void ClearMessages() => Messages.Clear();

        public Candle? GetCandle(string ticker) => CandleData[ticker].FindLast(x => x.Date <= CurrentDate);

        public void SetTopTickers() => TopTickers = [.. MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate, Period, CounTopTickers), MON];

        public void SetWeights()
        {
            foreach (var ticker in Tickers) 
                Data[ticker].Weight = 0.0;

            foreach (var ticker in TopTickers) 
                Data[ticker].Weight = 1.0;

            Data[MON].Weight = CounTopTickers - TopTickers.Count(x => x != MON);
        }

        public void UpdatePrices() => TradingProcessor.UpdatePrices();

        public void UpdateCandles()
        {            
            foreach (var ticker in PortfolioTickers)
                Data[ticker].Candle = GetCandle(ticker) ?? new Candle();
        }

        public void SetAverageCandleBodies()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)            
                Data[ticker].AverageCandleBody = CandleData[ticker].Where(x => x.Date >= CurrentDate.AddDays(-1 * Period) && x.Date <= CurrentDate).Average(x => Math.Abs(x.Close - x.Open));
        }

        public void SetStops()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
            {
                Data[ticker].StopPrice = Data[ticker].Candle.Close - 2.0 * Data[ticker].AverageCandleBody;
                Data[ticker].IsBreakEvenStop = false;
            }
        }

        public void SetEntryPrices()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                Data[ticker].EntryPrice = Data[ticker].Candle.Close;
        }

        public void CloseAllPositions() => TradingProcessor.CloseAllPositions();

        public void OpenPositionsByWeights()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                Data[ticker].CountBuy++;

            TradingProcessor.OpenPositionsByWeights(Data.ToDictionary(x => x.Key, x => x.Value.Weight), true);
        }

        public void MoveStopsToBreakEven()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (!Data[ticker].IsBreakEvenStop)
                    if (Data[ticker].Candle.Close >= Data[ticker].EntryPrice + 4.0 * Data[ticker].AverageCandleBody)
                    {
                        Data[ticker].StopPrice = Data[ticker].Candle.Close - 2.0 * Data[ticker].AverageCandleBody;
                        Data[ticker].IsBreakEvenStop = true;
                    }
        }

        public void TrailStops()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (Data[ticker].Candle.Close >= Data[ticker].StopPrice + 2.0 * Data[ticker].AverageCandleBody)
                    Data[ticker].StopPrice = Data[ticker].Candle.Close - 2.0 * Data[ticker].AverageCandleBody;
        }

        public void CheckStopsWithClosePosition()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (Data[ticker].Candle.Low < Data[ticker].StopPrice)
                {
                    Data[ticker].CountTriggerStop++;
                    ClosePosition(ticker);
                }
        }

        public void ClosePosition(string ticker)
        {
            // Скорректируем веса портфеля
            Data[ticker].Weight = 0.0;
            Data[MON].Weight += 1.0;

            TradingProcessor.ClosePosition(ticker, true);

            AddMessage(ticker, $"Стоп-лосс. Закрыта позиция по {ticker}", KnownColors.LightRed);
            AddMessage(MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
        }

        public void UpdateEquitySeries() => 
            EquitySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = TradingProcessor.TotalSum.RoundTo(2)
                });

        public void UpdateMoneySeries() => 
            MoneySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = (TradingProcessor.BalanceData[RUB].Cost + TradingProcessor.BalanceData[MON].Cost).RoundTo(2)
                });

        public double GetCurrentDrawdown()
        {
            var equityValues = EquitySeries.Data.Select(x => x.Value ?? 0.0).ToList();

            if (equityValues.Count == 0) return 0.0;

            double lastEquity = equityValues.Last();
            double maxEquity = equityValues.Max();

            if (maxEquity == 0.0) return 0.0;

            return -1 * Math.Abs((maxEquity - lastEquity) / maxEquity * 100.0).RoundTo(2);
        }

        public List<CurrentPosition> GetCurrentPositions()
        {
            var currentPositions = new List<CurrentPosition>();

            foreach (var ticker in PortfolioTickers)
            {
                var candle = CandleData[ticker].FindLast(x => x.Date <= Dates.Last());
                double tickerCost = TotalSumLife / WeightSum * Data[ticker].Weight.RoundTo(2);
                int tickerSize = Convert.ToInt32(Math.Truncate(tickerCost / candle!.Close / Data[ticker].Lot) * Data[ticker].Lot);
                double stopPrice = ticker == MON ? 0.0 : Data[ticker].StopPrice;
                double currentStopSizePercent = ticker == MON ? 0.0 : Data[ticker].CurrentStopSizePercent;
                double profitPercent = ticker == MON ? 0.0 : Data[ticker].ProfitPercent;

                currentPositions.Add(
                    new CurrentPosition
                    {
                        Ticker = ticker,
                        Weight = Data[ticker].Weight,
                        Size = tickerSize,
                        Cost = tickerCost,
                        StopPrice = stopPrice,
                        CurrentStopSizePercent = currentStopSizePercent,
                        ProfitPercent = profitPercent
                    });
            }

            List<CurrentPosition> orderedCurrentPositions = [
                    .. currentPositions.Where(x => x.Ticker != MON).OrderBy(x => x.Ticker),
                    .. currentPositions.Where(x => x.Ticker == MON)
                    ];

            int number = 1;
            foreach (var currentPosition in orderedCurrentPositions)
                currentPosition.Number = number++;

            return orderedCurrentPositions;
        }

        public List<DiagramSeries> GetPriceDynamicSeries()
        {
            var priceDynamicSeries = new List<DiagramSeries>();            

            var from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1 * Period);
            var to = DateOnly.FromDateTime(DateTime.Today);

            foreach (var (ticker, candleList) in CandleData.Where(x => x.Key != MON))
            {
                var candlesByDates = candleList.Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                string color = PortfolioTickers.Contains(ticker)
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

            return priceDynamicSeries;
        }

        public List<DiagramSeries> GetPriceSeries()
        {
            var series = new List<DiagramSeries>();

            var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
            var to = DateOnly.FromDateTime(DateTime.Today);

            List<string> tickers = [
                .. PortfolioTickers.Where(x => x != MON).OrderBy(x => x),
                .. PortfolioTickers.Where(x => x == MON)
                ];

            foreach (var ticker in tickers)
            {
                var candles = CandleData[ticker].Where(x => x.Date >= from && x.Date <= to).ToList();
                
                series.Add(
                    new DiagramSeries
                    {
                        Name = ticker,
                        Color = KnownColors.Blue,
                        ColorFill = KnownColors.LightBlue,
                        Data = [.. candles
                        .Select(x =>
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = x.Close.RoundTo(4)
                        })]
                    });
            }

            return series;
        }

        public List<List<DiagramSeries>> GetPriceWithStopSeries()
        {
            var series = new List<List<DiagramSeries>>();

            var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-15));
            var to = DateOnly.FromDateTime(DateTime.Today);

            List<string> tickers = [
                .. PortfolioTickers.Where(x => x != MON).OrderBy(x => x),
                .. PortfolioTickers.Where(x => x == MON)
                ];

            foreach (var ticker in tickers)
            {
                var candles = CandleData[ticker].Where(x => x.Date >= from && x.Date <= to).ToList();
                double stop = Data[ticker].StopPrice;

                series.Add([
                    new DiagramSeries
                    {
                        Name = $"Цена '{ticker}'",
                        Color = KnownColors.Blue,
                        ColorFill = KnownColors.LightBlue,
                        Data = [.. candles
                        .Select(x =>
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = x.Close.RoundTo(4)
                        })]
                    },
                    new DiagramSeries
                    {
                        Name = $" SL '{ticker}'",
                        Color = KnownColors.Red,
                        ColorFill = KnownColors.Red,
                        Data = [.. candles
                        .Select(x =>
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = stop.RoundTo(4)
                        })]
                    }
                    ]);
            }

            return series;
        }

        public List<TickerStatistic> GetTickerStatistic()
        {
            var tickerStatistics = new List<TickerStatistic>();

            foreach (var (ticker, data) in Data)
            {
                tickerStatistics.Add(
                    new TickerStatistic
                    {
                        Ticker = ticker,
                        CountBuy = data.CountBuy,
                        CountTriggerStop = data.CountTriggerStop,
                        CountTriggerStopPercent = data.CountTriggerStop == 0 ? 0.0 : (Convert.ToDouble(data.CountTriggerStop) / Convert.ToDouble(data.CountBuy) * 100.0).RoundTo(2)
                    });
            }

            var orderedTickerStatistics = tickerStatistics
                .OrderBy(x => x.Ticker)
                .Where(x => x.Ticker != MON)
                .ToList();

            int number = 1;
            foreach (var orderedTickerStatistic in orderedTickerStatistics)
                orderedTickerStatistic.Number = number++;

            return orderedTickerStatistics;
        }
    }
}
