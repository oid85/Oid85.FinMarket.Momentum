namespace Oid85.FinMarket.Algo.Common.Extensions
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

        public static double? Div(this double? value, double? arg)
        {
            if (!value.HasValue) return null;
            if (!arg.HasValue) return null;
            if (arg.Value == 0.0) return null;

            return value.Value / arg.Value;
        }

        public static double? Div(this double value, double? arg)
        {
            if (!arg.HasValue) return null;
            if (arg.Value == 0.0) return null;

            return value / arg.Value;
        }

        public static double? Div(this double value, double arg)
        {
            if (arg == 0.0) return null;

            return value / arg;
        }

        public static double? Mult(this double? value, double? arg)
        {
            if (!value.HasValue) return null;
            if (!arg.HasValue) return null;

            return value.Value * arg.Value;
        }

        public static double? Mult(this double value, double? arg)
        {
            if (!arg.HasValue) return null;

            return value * arg.Value;
        }
    }
}
