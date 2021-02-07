using System.Collections.Generic;
using System.Globalization;

namespace Bloomberg.API
{
    public static class Utilities
    {
        public static string CleanString(this string input)
        {
            if (input == null) return null;

            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            return input;
        }

        public static int ToInt(this string input)
        {
            input = CleanString(input)?.Replace(" ", "");

            if (input == null)
            {
                return -1;
            }

            return int.Parse(input);
        }

        public static void AddRange<T>(this ICollection<T> baseCollection, IEnumerable<T> toAdd)
        {
            foreach (var x1 in toAdd)
            {
                baseCollection.Add(x1);
            }
        }

        public static decimal BloombergStringToDecimal(this string raw)
        {
            if (raw.Contains("E"))
            {
                return decimal.Parse(raw, NumberStyles.Float);
            }
            else
            {
                return decimal.Parse(raw);
            }
        }
    }
}