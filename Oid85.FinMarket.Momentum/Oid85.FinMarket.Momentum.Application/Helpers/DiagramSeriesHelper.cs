using Oid85.FinMarket.Momentum.Common.Extensions;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Core.Models;

namespace Oid85.FinMarket.Momentum.Application.Helpers
{
    public class DiagramSeriesHelper
    {
        public static double GetAnnualPercentageYield(DiagramSeries equitySeries, int year = 0)
        {
            var dataValues = year != 0
                ? equitySeries.Data.Where(x => x.Date.Year == year)
                : equitySeries.Data;

            double firstValue = dataValues.First().Value ?? 0.0;
            double lastValue = dataValues.Last().Value ?? 0.0;

            var firstDate = dataValues.First().Date.ToDateTime(TimeOnly.MinValue);
            var lastDate = dataValues.Last().Date.ToDateTime(TimeOnly.MaxValue);

            if (lastValue == 0.0) return 0.0;

            var years = (lastDate - firstDate).TotalDays / 365.0;

            return ((lastValue - firstValue) / firstValue * 100.0 / years).RoundTo(1);
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
    }
}
