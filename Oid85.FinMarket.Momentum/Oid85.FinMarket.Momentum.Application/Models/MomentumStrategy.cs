using System.Globalization;
using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumStrategy
    {
        public virtual List<string> GetDescription() => [];

        public DateOnly From { get; set; } = new DateOnly(2021, 1, 1);

        public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public List<DateOnly> Dates => DateUtils.GetDates(From, To);

        public int Period { get; set; }

        public int CounTopTickers { get; set; }

        public double TotalSumLife { get; set; } = 0.0;

        public double TotalSum { get; set; } = 0.0;

        public double StartMoneySum { get; set; } = 0.0;

        public double Money { get; set; } = 0.0;

        public List<int> RebalanceDays { get; set; } = [];

        public Dictionary<string, PositionData> PositionData { get; set; } = [];

        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

        public List<ProtocolMessage> ProtocolMessages { get; set; } = [];

        public DiagramSeries EquitySeries { get; set; } = new() { Name = "Капитал", Color = KnownColors.Green, ColorFill = KnownColors.Green };

        public DiagramSeries MoneySeries { get; set; } = new() { Name = "Фонд ликвидности", Color = KnownColors.LightBlue, ColorFill = KnownColors.LightBlue };

        public DiagramSeries ShortEquitySeries => DiagramSeriesHelper.GetShortEquitySeries(EquitySeries);

        public DiagramSeries DrawdownSeries => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries);

        public DiagramSeries DrawdownSeriesPercent => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries, true);

        public double MaxDrawdown => DrawdownSeriesPercent.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1);

        public List<string> Tickers => [.. PositionData.Keys];

        public List<string> TopTickers { get; set; } = [];

        public List<string> PortfolioTickers => [.. PositionData.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> PortfolioWithoutMonTickers => [.. PositionData.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public double WeightSum => PositionData.Values.Sum(x => x.Weight);

        public double CostSum => PositionData.Values.Sum(x => x.Cost);

        public bool IsRebalance => RebalanceDays.Contains(CurrentDate.Day);

        public DateOnly CurrentDate { get; set; } = DateOnly.MinValue;

        public virtual void Execute()
        {

        }

        public void AddMessage(string ticker, string message, string colorFill) => ProtocolMessages.Add(new() { Date = CurrentDate, Ticker = ticker, Message = message, ColorFill = colorFill });

        public void AddRebalanceMessage()
        {
            var nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ",";
            nfi.NumberGroupSeparator = " ";

            foreach (var ticker in PortfolioWithoutMonTickers)
                AddMessage(ticker, $"Ребалансировка моментума. Позиция {ticker}, {PositionData[ticker].Size.ToString("N0", nfi)} шт., {PositionData[ticker].Cost.RoundTo(2).ToString("N", nfi)} руб., СЛ {PositionData[ticker].Stop.RoundTo(2).ToString("N", nfi)} руб.", KnownColors.LightGreen);
        }

        public void ClearMessages() => ProtocolMessages.Clear();

        public Candle? GetCandle(string ticker) => CandleData[ticker].FindLast(x => x.Date <= CurrentDate);

        public void SetTopTickers() => TopTickers = [.. MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate, Period, CounTopTickers), KnownTickers.MON];

        public void SetWeights()
        {
            foreach (var ticker in Tickers) 
                PositionData[ticker].Weight = 0.0;

            foreach (var ticker in TopTickers) 
                PositionData[ticker].Weight = 1.0;

            PositionData[KnownTickers.MON].Weight = CounTopTickers - TopTickers.Count(x => x != KnownTickers.MON);
        }

        public void UpdateCandles()
        {
            foreach (var ticker in PortfolioTickers)
                PositionData[ticker].Candle = GetCandle(ticker) ?? new Candle();
        }

        public void SetStops()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                PositionData[ticker].Stop = MomentumHelper.GetStopPrice(CandleData[ticker], PositionData[ticker].Candle.Close, CurrentDate, Period);
        }

        public void SetSizes()
        {
            foreach (var ticker in Tickers)
                PositionData[ticker].Size = 0.0;

            double baseUnit = TotalSum / WeightSum;

            foreach (var ticker in PortfolioTickers)
            {
                if (PositionData[ticker].Candle.Close == 0.0)
                {
                    PositionData[ticker].Cost = 0.0;
                    continue;
                }

                PositionData[ticker].Size = Math.Truncate(baseUnit * PositionData[ticker].Weight / PositionData[ticker].Candle.Close / PositionData[ticker].Lot) * PositionData[ticker].Lot;
                PositionData[ticker].CountBuy++;
            }
        }

        public void UpdateCosts()
        {
            foreach (var ticker in Tickers) 
                PositionData[ticker].Cost = 0.0;

            foreach (var ticker in PortfolioTickers) 
                PositionData[ticker].Cost = PositionData[ticker].Candle.Close * PositionData[ticker].Size;
        }

        public void UpdateTotalSum()
        {
            TotalSum = CostSum + Money;
        }

        public void UpdateMoney()
        {
            Money = TotalSum - CostSum;
        }

        public void CheckStopsWithClosePosition()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (PositionData[ticker].Candle.Low < PositionData[ticker].Stop)
                {
                    PositionData[ticker].CountTriggerStop++;
                    ClosePosition(ticker);
                }
        }

        public void CheckStopsChangePosition()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (PositionData[ticker].Candle.Low < PositionData[ticker].Stop)
                {
                    PositionData[ticker].CountTriggerStop++;
                    ChangePosition(ticker);
                }
        }

        public void ClosePosition(string ticker)
        {
            // Продаем актив
            PositionData[ticker].Weight = 0.0;
            PositionData[ticker].Size = 0.0;
            Money += PositionData[ticker].Cost;
            PositionData[ticker].Cost = 0.0;
            
            // Покупаем фонд ликвидности
            PositionData[KnownTickers.MON].Weight += 1.0;
            double monSize = Math.Truncate(Money / PositionData[KnownTickers.MON].Candle.Close);
            double monCost = monSize * PositionData[KnownTickers.MON].Candle.Close;
            Money -= monCost;

            PositionData[KnownTickers.MON].Size += monSize;
            PositionData[KnownTickers.MON].Cost += monCost;

            AddMessage(ticker, $"Стоп-лосс. Закрыта позиция по {ticker}", KnownColors.LightRed);
            AddMessage(KnownTickers.MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
        }

        public void ChangePosition(string ticker)
        {
            string tickerForRemove = ticker;
            var currentTickers = PortfolioTickers;

            // Продаем актив
            PositionData[tickerForRemove].Weight = 0.0;
            PositionData[tickerForRemove].Size = 0.0;
            Money += PositionData[tickerForRemove].Cost;
            PositionData[tickerForRemove].Cost = 0.0;

            // Определяем новых лидеров
            var newTopTickers = MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate, Period, CounTopTickers)
                .Where(x => !currentTickers.Contains(x)).Where(x => x != KnownTickers.MON).ToList();

            var tickerForAdd = newTopTickers.Count == 0
                ? KnownTickers.MON
                : newTopTickers.First();

            AddMessage(ticker, $"Стоп-лосс. Закрыта позиция по {ticker}", KnownColors.LightRed);

            if (tickerForAdd == KnownTickers.MON)
            {
                // Покупаем фонд ликвидности
                PositionData[KnownTickers.MON].Weight += 1.0;
                double monSize = Math.Truncate(Money / PositionData[KnownTickers.MON].Candle.Close);
                double monCost = monSize * PositionData[KnownTickers.MON].Candle.Close;
                Money -= monCost;

                PositionData[KnownTickers.MON].Size += monSize;
                PositionData[KnownTickers.MON].Cost += monCost;

                AddMessage(KnownTickers.MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
            }

            else
            {
                // Покупаем другой актив
                PositionData[tickerForAdd].Weight = 1.0;
                PositionData[tickerForAdd].Candle = GetCandle(tickerForAdd) ?? new Candle();
                PositionData[tickerForAdd].Size = Math.Truncate(Money / PositionData[tickerForAdd].Candle.Close / PositionData[tickerForAdd].Lot) * PositionData[tickerForAdd].Lot;
                PositionData[tickerForAdd].Cost = PositionData[tickerForAdd].Candle.Close * PositionData[tickerForAdd].Size;
                PositionData[tickerForAdd].CountBuy++;

                Money -= PositionData[tickerForAdd].Cost;

                AddMessage(tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}", KnownColors.LightGreen);
            }
        }

        public void UpdateEquitySeries() => 
            EquitySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = (TotalSum / 1000.0).RoundTo(2)
                });

        public void UpdateMoneySeries() => 
            MoneySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = ((Money + PositionData[KnownTickers.MON].Cost) / 1000.0).RoundTo(2)
                });

        public double GetCurrentDrawdown()
        {
            var equityValues = EquitySeries.Data.Select(x => x.Value ?? 0.0).ToList();

            if (equityValues.Count == 0) return 0.0;

            double lastEquity = equityValues.Last();
            double maxEquity = equityValues.Max();

            if (maxEquity == 0.0) return 0.0;

            return -1 * Math.Abs((maxEquity - lastEquity) / maxEquity * 100.0).RoundTo(1);
        }

        public List<PortfolioPosition> GetCurrentPositions()
        {
            var currentPositions = new List<PortfolioPosition>();

            foreach (var ticker in PortfolioTickers)
            {
                var candle = CandleData[ticker].FindLast(x => x.Date <= Dates.Last());
                var lot = PositionData[ticker].Lot;
                double baseUnit = TotalSumLife / WeightSum;
                double tickerCost = baseUnit * PositionData[ticker].Weight;
                int tickerSize = Convert.ToInt32(Math.Truncate(tickerCost / candle!.Close / lot) * lot);

                currentPositions.Add(
                    new PortfolioPosition
                    {
                        Ticker = ticker,
                        Weight = PositionData[ticker].Weight,
                        Size = tickerSize,
                        Cost = tickerCost.RoundTo(2),
                        StopPrice = ticker == KnownTickers.MON ? 0.0 : PositionData[ticker].Stop.RoundTo(4)
                    });
            }

            List<PortfolioPosition> orderedCurrentPositions = [
                    .. currentPositions.Where(x => x.Ticker != KnownTickers.MON).OrderBy(x => x.Ticker),
                    .. currentPositions.Where(x => x.Ticker == KnownTickers.MON)
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

            foreach (var (ticker, candleList) in CandleData.Where(x => x.Key != KnownTickers.MON))
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
            var priceSeries = new List<DiagramSeries>();

            var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
            var to = DateOnly.FromDateTime(DateTime.Today);

            List<string> tickers = [
                .. PortfolioTickers.Where(x => x != KnownTickers.MON).OrderBy(x => x),
                .. PortfolioTickers.Where(x => x == KnownTickers.MON)
                ];

            foreach (var ticker in tickers)
            {
                var candlesByDates = CandleData[ticker].Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                priceSeries.Add(
                    new DiagramSeries
                    {
                        Name = ticker,
                        Color = KnownColors.Blue,
                        ColorFill = KnownColors.LightBlue,
                        Data = [.. candlesByDates
                        .Select(x =>
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = x.Close.RoundTo(4)
                        })]
                    });
            }

            return priceSeries;
        }

        public List<TickerStatistic> GetTickerStatistic() =>
            [.. PositionData
                .Select(x =>
                    new TickerStatistic
                    {
                        Ticker = x.Key,
                        CountBuy = x.Value.CountBuy,
                        CountTriggerStop = x.Value.CountTriggerStop,
                        CountTriggerStopPercent = x.Value.CountTriggerStop == 0 ? 0.0 : (Convert.ToDouble(x.Value.CountTriggerStop) / Convert.ToDouble(x.Value.CountBuy) * 100.0).RoundTo(1)
                    })
                .Where(x => x.Ticker != KnownTickers.MON)];
    }
}
