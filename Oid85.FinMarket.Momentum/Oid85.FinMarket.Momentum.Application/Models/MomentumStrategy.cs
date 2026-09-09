using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Oid85.FinMarket.Momentum.Application.Models
{
    public class MomentumStrategy
    {
        public DateOnly From { get; set; } = new DateOnly(2021, 1, 1);

        public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public List<DateOnly> Dates => DateUtils.GetDates(From, To);

        public int Period { get; set; }

        public int CountTopTickers { get; set; }

        public double TotalSum { get; set; } = 0.0;

        public double StartMoneySum { get; set; } = 0.0;

        public double Money { get; set; } = 0.0;

        public List<int> RebalanceDays { get; set; } = [];

        public Dictionary<string, PositionData> PositionData { get; set; } = [];

        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

        public List<ProtocolMessage> ProtocolMessages { get; set; } = [];

        public DiagramSeries EquitySeries { get; set; } = new() { Name = "Капитал", Color = KnownColors.Green, ColorFill = KnownColors.Green };

        public DiagramSeries MoneySeries { get; set; } = new() { Name = "Фонд ликвидности", Color = KnownColors.LightBlue, ColorFill = KnownColors.LightBlue };

        public DiagramSeries DrawdownSeries => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries);

        public DiagramSeries DrawdownSeriesPercent => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries, true);

        public double MaxDrawdown => DrawdownSeriesPercent.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1);

        public double CurrentDrawdown => DrawdownSeriesPercent.Data.Last(x => x.Value.HasValue).Value!.Value.RoundTo(1);

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
            foreach (var ticker in PortfolioWithoutMonTickers)
                AddMessage(ticker, $"Ребалансировка моментума. Позиция {ticker}", KnownColors.LightGreen);
        }

        public void ClearMessages() => ProtocolMessages.Clear();

        public Candle? GetCandle(string ticker) => CandleData[ticker].FindLast(x => x.Date <= CurrentDate);

        public void SetTopTickers() => TopTickers = [.. MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate, Period, CountTopTickers), KnownTickers.MON];

        public void SetWeights()
        {
            foreach (var ticker in Tickers) 
                PositionData[ticker].Weight = 0.0;

            foreach (var ticker in TopTickers) 
                PositionData[ticker].Weight = 1.0;

            PositionData[KnownTickers.MON].Weight = CountTopTickers - TopTickers.Count(x => x != KnownTickers.MON);
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
                    ClosePosition(ticker);
        }

        public void CheckStopsChangePosition()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (PositionData[ticker].Candle.Low < PositionData[ticker].Stop)
                    ChangePosition(ticker);
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

            AddMessage(ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);
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
            var newTopTickers = MomentumHelper.GetMomentumTopTickers(CandleData, CurrentDate, Period, CountTopTickers)
                .Where(x => !currentTickers.Contains(x)).Where(x => x != KnownTickers.MON).ToList();

            var tickerForAdd = newTopTickers.Count == 0
                ? KnownTickers.MON
                : newTopTickers.First();

            AddMessage(ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);

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

                Money -= PositionData[tickerForAdd].Cost;

                AddMessage(tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}", KnownColors.LightGreen);
            }
        }

        public void UpdateEquitySeries()
        {
            EquitySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = (TotalSum / 1000.0).RoundTo(2)
                });
        }

        public void UpdateMoneySeries()
        {
            MoneySeries.Data.Add(
                new()
                {
                    Date = CurrentDate,
                    Value = ((Money + PositionData[KnownTickers.MON].Cost) / 1000.0).RoundTo(2)
                });
        }
    }
}
