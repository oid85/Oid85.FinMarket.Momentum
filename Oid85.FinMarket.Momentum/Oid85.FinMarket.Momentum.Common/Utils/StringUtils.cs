using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Oid85.FinMarket.Momentum.Common.Utils;

public static class StringUtils
{
    public static string Base64Encode(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static string Base64Decode(string base64) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(base64));

    public static string GetMd5(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(bytes);
        var result = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return result;
    }

    public static double ToDouble(string? input)
    {
        if (input is null) return 0.0;

        string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        input = input.Trim();
        input = input.Replace(" ", "");
        input = input.Replace(",", separator);
        input = input.Replace(".", separator);

        var result = Convert.ToDouble(input);

        return result;
    }
}