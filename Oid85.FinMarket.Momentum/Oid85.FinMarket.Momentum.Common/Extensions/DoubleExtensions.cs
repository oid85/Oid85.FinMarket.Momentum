namespace Oid85.FinMarket.Momentum.Common.Extensions
{
    public static class DoubleExtensions
    {
        public static double RoundTo(this double value, int decimalPlaces)
        {
            return Math.Round(value, decimalPlaces);
        }

        public static double? RoundTo(this double? value, int decimalPlaces)
        {
            if (!value.HasValue) return null;

            return Math.Round(value.Value, decimalPlaces);
        }
    }
}
