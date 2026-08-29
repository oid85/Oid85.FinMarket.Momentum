using Oid85.FinMarket.Momentum.Common.Utils;

namespace Oid85.FinMarket.Momentum.Common.Extensions;

public static class DataValueSeriesExtensions
{
    public static Dictionary<DateOnly, double> Expand(this Dictionary<DateOnly, double> data, DateOnly from, DateOnly to)
    {
        var dates = DateUtils.GetDates(from, to);

        var result = dates.ToDictionary(date => date, _ => 0.0);

        foreach (var curveItem in data) result[curveItem.Key] = curveItem.Value;

        var keys = result.Keys.ToList();
        
        for (int i = 1; i < keys.Count; i++)
            if (result[keys[i]] == 0.0)
                result[keys[i]] = result[keys[i - 1]];
        
        return result;
    }
}