using Oid85.FinMarket.Algo.Common.Utils;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Helpers
{
    public class MonitorHelper
    {
        public static List<(string Ticker, List<DateWeight> WeightData)> GetPositionWeightData(
            List<StrategyExecuteResult> strategyExecuteResults,
            List<string> tickers,
            List<DateOnly> dates)
        {
            var result = new List<(string Ticker, List<DateWeight> WeightData)>();

            foreach (var ticker in tickers)
            {
                var data = strategyExecuteResults
                    .Where(x => x.Ticker == ticker)
                    .Select(x => Map(x.Positions, dates))
                    .ToList();

                result.Add(new (ticker, [.. Merge(data, dates).Select(x => new DateWeight { Date = x.Date, Weight = x.Value})]));
            }

            return [.. result.OrderBy(x => x.Ticker)];
        }

        public static List<TickerWeight> GetPositionWeightDataByDate(
            List<(string Ticker, List<DateWeight> WeightData)> weightData,
            DateOnly date)
        {
            var result = new List<TickerWeight>();

            foreach (var (ticker, weight) in weightData)
                result.Add(
                    new()
                    {
                        Ticker = ticker,
                        Weight = weight.Find(x => x.Date == date)?.Weight ?? 0
                    });

            return result;
        }

        public static List<DateValue<int>> Merge(List<List<DateValue<int>>> data, List<DateOnly> dates)
        {
            var result = new List<DateValue<int>>();

            var combineData = data.SelectMany(x => x).ToList();

            foreach (var date in dates)
                result.Add(
                    new()
                    {
                        Date = date,
                        Value = combineData.Where(x => x.Date == date).Sum(x => x.Value)
                    });

            return [.. result.OrderBy(x => x.Date)];
        }

        public static List<DateValue<int>> Map(SortedDictionary<DateOnly, Position> positions, List<DateOnly> dates)
        {
            var dictionary = dates.ToDictionary(k => k, v => 0);

            foreach (var position in positions)
            {
                var positionDates = position.Value.ExitDate.HasValue
                    ? DateUtils.GetDates(position.Value.EntryDate, position.Value.ExitDate.Value)
                    : DateUtils.GetDates(position.Value.EntryDate, dates.Last());

                foreach (var date in positionDates)
                {
                    if (position.Value.IsLong) dictionary[date] = 1;
                    if (position.Value.IsShort) dictionary[date] = -1;
                }
            }

            return [.. dictionary.Select(x => new DateValue<int> { Date = x.Key, Value = x.Value}).OrderBy(x => x.Date)];
        }
    }
}
