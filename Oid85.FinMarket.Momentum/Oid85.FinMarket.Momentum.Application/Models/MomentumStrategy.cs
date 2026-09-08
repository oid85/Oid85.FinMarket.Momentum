using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Common.Utils;
using Oid85.FinMarket.Momentum.Core.Models;

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

        public Dictionary<string, MomentumTickerData> TickerData { get; set; } = [];

        public Dictionary<string, List<Candle>> CandleData { get; set; } = [];

        public List<ProtocolMessage> ProtocolMessages { get; set; } = [];

        public DiagramSeries EquitySeries { get; set; } = new() { Name = "Капитал", Color = KnownColors.Green, ColorFill = KnownColors.Green };

        public DiagramSeries MoneySeries { get; set; } = new() { Name = "Фонд ликвидности", Color = KnownColors.LightBlue, ColorFill = KnownColors.LightBlue };

        public DiagramSeries DrawdownSeries => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries);

        public DiagramSeries DrawdownSeriesPercent => DiagramSeriesHelper.GetDrawdownSeries(EquitySeries, true);

        public double MaxDrawdown => DrawdownSeriesPercent.Data.Where(x => x.Value.HasValue).Min(x => x.Value!.Value).RoundTo(1);

        public double CurrentDrawdown => DrawdownSeriesPercent.Data.Last(x => x.Value.HasValue).Value!.Value.RoundTo(1);

        public List<string> Tickers => [.. TickerData.Keys];

        public List<string> TopTickers { get; set; } = [];

        public List<string> PortfolioTickers => [.. TickerData.Values.Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public List<string> PortfolioWithoutMonTickers => [.. TickerData.Values.Where(x => x.Ticker != KnownTickers.MON).Where(x => x.Weight > 0.0).Select(x => x.Ticker)];

        public double WeightSum => TickerData.Values.Sum(x => x.Weight);

        public double CostSum => TickerData.Values.Sum(x => x.Cost);

        public DateOnly Date { get; set; } = DateOnly.MinValue;

        public virtual void Execute()
        {

        }

        public void AddMessage(string ticker, string message, string colorFill) => ProtocolMessages.Add(new() { Date = Date, Ticker = ticker, Message = message, ColorFill = colorFill });

        public Candle? GetCandle(string ticker) => CandleData[ticker].FindLast(x => x.Date <= Date);

        public void ClearSizes()
        {
            foreach (var (ticker, _) in TickerData)
                TickerData[ticker].Size = 0.0;
        }

        public void ClearCosts()
        {
            foreach (var (ticker, _) in TickerData)
                TickerData[ticker].Cost = 0.0;
        }

        public void SetTopTickers()
        {
            TopTickers = MomentumHelper.GetMomentumTopTickers(CandleData, Date, Period, CountTopTickers);
            TopTickers.Add(KnownTickers.MON);
        }

        public void SetWeights()
        {
            foreach (var ticker in Tickers) TickerData[ticker].Weight = 0.0;
            foreach (var ticker in TopTickers) TickerData[ticker].Weight = 1.0;
            TickerData[KnownTickers.MON].Weight = CountTopTickers - TopTickers.Count(x => x != KnownTickers.MON);
        }

        public void UpdateCandles()
        {
            foreach (var ticker in PortfolioTickers)
                TickerData[ticker].Candle = GetCandle(ticker) ?? new Candle();
        }

        public void SetStops()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                TickerData[ticker].Stop = MomentumHelper.GetStopPrice(CandleData[ticker], TickerData[ticker].Candle.Close, Date, Period);
        }

        public void SetSizes()
        {
            ClearSizes();

            double baseUnit = TotalSum / WeightSum;

            foreach (var ticker in PortfolioTickers)
            {
                if (TickerData[ticker].Candle.Close == 0.0)
                {
                    TickerData[ticker].Cost = 0.0;
                    continue;
                }

                TickerData[ticker].Size = Math.Truncate(baseUnit * TickerData[ticker].Weight / TickerData[ticker].Candle.Close / TickerData[ticker].Lot) * TickerData[ticker].Lot;
            }
        }

        public void UpdateCosts()
        {
            ClearCosts();

            foreach (var ticker in PortfolioTickers)
                TickerData[ticker].Cost = TickerData[ticker].Candle.Close * TickerData[ticker].Size;
        }

        public void UpdateTotalSum()
        {
            TotalSum = CostSum + Money;
        }

        public void UpdateMoney()
        {
            Money = TotalSum - CostSum;
        }

        public void CheckStopsVersion1()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (TickerData[ticker].Candle.Low < TickerData[ticker].Stop)
                    ClosePosition(ticker);
        }

        public void CheckStopsVersion2()
        {
            foreach (var ticker in PortfolioWithoutMonTickers)
                if (TickerData[ticker].Candle.Low < TickerData[ticker].Stop)
                    ChangePosition(ticker);
        }

        public void ClosePosition(string ticker)
        {
            // Продаем актив
            TickerData[ticker].Weight = 0.0;
            TickerData[ticker].Size = 0.0;
            Money += TickerData[ticker].Cost;
            TickerData[ticker].Cost = 0.0;

            // Покупаем фонд ликвидности
            TickerData[KnownTickers.MON].Weight += 1.0;
            double monSize = Math.Truncate(Money / TickerData[KnownTickers.MON].Candle.Close);
            double monCost = monSize * TickerData[KnownTickers.MON].Candle.Close;
            Money -= monCost;

            TickerData[KnownTickers.MON].Size += monSize;
            TickerData[KnownTickers.MON].Cost += monCost;

            AddMessage(ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);
            AddMessage(KnownTickers.MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
        }

        public void ChangePosition(string ticker)
        {
            string tickerForRemove = ticker;
            var currentTickers = PortfolioTickers;

            // Продаем актив
            TickerData[tickerForRemove].Weight = 0.0;
            TickerData[tickerForRemove].Size = 0.0;
            Money += TickerData[tickerForRemove].Cost;
            TickerData[tickerForRemove].Cost = 0.0;

            // Определяем новых лидеров
            var newTopTickers = MomentumHelper.GetMomentumTopTickers(CandleData, Date, Period, CountTopTickers)
                .Where(x => !currentTickers.Contains(x)).Where(x => x != KnownTickers.MON).ToList();

            var tickerForAdd = newTopTickers.Count == 0
                ? KnownTickers.MON
                : newTopTickers.First();

            AddMessage(ticker, $"Стоп-лосс. Удален {ticker}", KnownColors.LightRed);

            if (tickerForAdd == KnownTickers.MON)
            {
                // Покупаем фонд ликвидности
                TickerData[KnownTickers.MON].Weight += 1.0;
                double monSize = Math.Truncate(Money / TickerData[KnownTickers.MON].Candle.Close);
                double monCost = monSize * TickerData[KnownTickers.MON].Candle.Close;
                Money -= monCost;

                TickerData[KnownTickers.MON].Size += monSize;
                TickerData[KnownTickers.MON].Cost += monCost;

                AddMessage(KnownTickers.MON, $"Увеличена доля фонда ликвидности", KnownColors.LightGreen);
            }

            else
            {
                // Покупаем другой актив
                TickerData[tickerForAdd].Weight = 1.0;
                TickerData[tickerForAdd].Candle = GetCandle(tickerForAdd) ?? new Candle();
                TickerData[tickerForAdd].Size = Math.Truncate(Money / TickerData[tickerForAdd].Candle.Close / TickerData[tickerForAdd].Lot) * TickerData[tickerForAdd].Lot;
                TickerData[tickerForAdd].Cost = TickerData[tickerForAdd].Candle.Close * TickerData[tickerForAdd].Size;

                Money -= TickerData[tickerForAdd].Cost;

                AddMessage(tickerForAdd, $"Замена актива. Добавлен {tickerForAdd}", KnownColors.LightGreen);
            }
        }
    }
}
