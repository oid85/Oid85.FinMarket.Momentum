using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Helpers
{
    public class DiagramSeriesHelper
    {
        public static double GetYearPercentageYield(DiagramSeries equitySeries, int year)
        {
            var dataValues = equitySeries.Data.Where(x => x.Date.Year == year);

            double firstValue = dataValues.First().Value ?? 0.0;
            double lastValue = dataValues.Last().Value ?? 0.0;

            if (firstValue == 0.0) return 0.0;

            return ((lastValue - firstValue) / firstValue * 100.0).RoundTo(2);
        }

        public static double GetPercentageYield(DiagramSeries equitySeries, int days)
        {
            var from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1 * days);
            var to = DateOnly.FromDateTime(DateTime.Today);

            var dataValues = equitySeries.Data.Where(x => x.Date >= from && x.Date <= to);

            double firstValue = dataValues.First().Value ?? 0.0;
            double lastValue = dataValues.Last().Value ?? 0.0;

            if (firstValue == 0.0) return 0.0;

            return ((lastValue - firstValue) / firstValue * 100.0).RoundTo(2);
        }

        public static DiagramSeries GetDrawdownSeries(DiagramSeries equitySeries, bool inPercent = false)
        {
            var drawdownSeries = new DiagramSeries
            {
                Name = "Просадка",
                Color = KnownColors.Red,
                ColorFill = KnownColors.Red
            };

            for (int i = 0; i < equitySeries.Data.Count; i++)
            {
                if (i == 0)
                    drawdownSeries.Data.Add(
                        new DateValue<double?>
                        {
                            Date = equitySeries.Data[i].Date,
                            Value = 0.0
                        });

                else
                {
                    var maxEquity = equitySeries.Data.Take(i).Max(x => x.Value);

                    var dateValue = new DateValue<double?>
                    {
                        Date = equitySeries.Data[i].Date,
                        Value = 0.0
                    };

                    if (equitySeries.Data[i].Value <= maxEquity)
                        dateValue.Value = inPercent
                            ? ((equitySeries.Data[i].Value - maxEquity) / maxEquity * 100.0).RoundTo(2)
                            : (equitySeries.Data[i].Value - maxEquity).RoundTo(2);                    

                    drawdownSeries.Data.Add(dateValue);
                }
            }

            return drawdownSeries;
        }

        public static DiagramSeries GetShortEquitySeries(DiagramSeries equitySeries)
        {
            var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
            var to = DateOnly.FromDateTime(DateTime.Today);

            return new DiagramSeries()
            {
                Name = "Капитал",
                Color = KnownColors.DarkGreen,
                ColorFill = KnownColors.Green,
                Data = [.. equitySeries.Data.Where(x => x.Date >= from && x.Date <= to)],
            };
        }

        public static List<DiagramSeries> GetPriceSeries(
            List<string> portfolioTickers, 
            Dictionary<string, List<Candle>> candleData)
        {
            var series = new List<DiagramSeries>();

            var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
            var to = DateOnly.FromDateTime(DateTime.Today);

            List<string> tickers = [
                .. portfolioTickers.Where(x => x != KnownTickers.MON).OrderBy(x => x),
                .. portfolioTickers.Where(x => x == KnownTickers.MON)
                ];

            foreach (var ticker in tickers)
            {
                var candles = candleData[ticker].Where(x => x.Date >= from && x.Date <= to).ToList();

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

        public static List<DiagramSeries> GetPriceDynamicSeries(
            List<string> portfolioTickers, 
            Dictionary<string, List<Candle>> candleData, 
            int period)
        {
            var priceDynamicSeries = new List<DiagramSeries>();

            var from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1 * period);
            var to = DateOnly.FromDateTime(DateTime.Today);

            foreach (var (ticker, candleList) in candleData.Where(x => x.Key != KnownTickers.MON))
            {
                var candlesByDates = candleList.Where(x => x.Date >= from && x.Date <= to).ToList();
                double firstPrice = candlesByDates.First().Close;

                string color = portfolioTickers.Contains(ticker)
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

        public static List<List<DiagramSeries>> GetPriceWithStopSeries(
            List<string> portfolioTickers,
            Dictionary<string, List<Candle>> candleData,
            Dictionary<string, PositionData> positionData)
        {
            var series = new List<List<DiagramSeries>>();

            var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-15));
            var to = DateOnly.FromDateTime(DateTime.Today);

            List<string> tickers = [
                .. portfolioTickers.Where(x => x != KnownTickers.MON).OrderBy(x => x),
                .. portfolioTickers.Where(x => x == KnownTickers.MON)
                ];

            foreach (var ticker in tickers)
            {
                var candles = candleData[ticker].Where(x => x.Date >= from && x.Date <= to).ToList();
                double stop = positionData[ticker].StopPrice;
                double entryPrice = positionData[ticker].EntryPrice;

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
                    },
                    new DiagramSeries
                    {
                        Name = $" PEn '{ticker}'",
                        Color = KnownColors.Blue,
                        ColorFill = KnownColors.Blue,
                        Data = [.. candles
                        .Select(x =>
                        new DateValue<double?>
                        {
                            Date = x.Date,
                            Value = entryPrice.RoundTo(4)
                        })]
                    }
                    ]);
            }

            return series;
        }
    }
}
