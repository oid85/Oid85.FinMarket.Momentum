using System.Globalization;

namespace Oid85.FinMarket.Algo.Common.Utils
{
    public static class DateUtils
    {
        public static List<DateOnly> GetDates(DateOnly from, DateOnly to)
        {
            var curDate = from;
            var dates = new List<DateOnly>();

            while (curDate <= to)
            {
                dates.Add(curDate);
                curDate = curDate.AddDays(1);
            }

            return dates;
        }

        public static List<DateOnly> GetMonthDates(DateOnly from, DateOnly to)
        {
            var curDate = new DateOnly(from.Year, from.Month, 1);
            var dates = new List<DateOnly>();

            while (curDate <= to)
            {
                dates.Add(curDate);
                curDate = curDate.AddMonths(1);
            }

            return dates;
        }

        public static List<(int WeekNumber, DateOnly WeekStartDay, DateOnly WeekEndDay)> GetWeeks(DateOnly from, DateOnly to)
        {
            var result = new List<(int WeekNumber, DateOnly WeekStartDay, DateOnly WeekEndDay)>();

            var startDate = from.ToDateTime(TimeOnly.MinValue);
            var endDate = to.ToDateTime(TimeOnly.MinValue);

            // Обходим каждую неделю начиная с начальной даты
            while (startDate <= endDate)
            {
                int weekNumber = GetIso8601WeekOfYear(startDate); // Номер недели ISO

                // Определяем первый и последний дни недели
                DayOfWeek firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                var monday = startDate.AddDays((firstDayOfWeek - startDate.DayOfWeek + 7) % 7);
                var sunday = monday.AddDays(6);

                result.Add(new(weekNumber, DateOnly.FromDateTime(monday), DateOnly.FromDateTime(sunday)));

                // Проверяем, входит ли воскресенье в диапазон дат
                if (sunday > endDate)
                    break;

                // Переход к следующей неделе
                startDate = sunday.AddDays(1);
            }

            return result;
        }

        private static int GetIso8601WeekOfYear(DateTime dateTime)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(dateTime);

            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                dateTime = dateTime.AddDays(3);

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
