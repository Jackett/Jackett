using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public static class ParseUtil
    {
        public static string NormalizeSpace(string s)
        {
            return s.Trim();
        }

        public static string NormalizeNumber(string s)
        {
            string normalized = NormalizeSpace(s);
            normalized = normalized.Replace("-", "0");
            normalized = normalized.Replace(",", "");
            return normalized;
        }

        public static double CoerceDouble(string str)
        {
            return double.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static float CoerceFloat(string str)
        {
            return float.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static int CoerceInt(string str)
        {
            return int.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static long CoerceLong(string str)
        {
            return long.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);
        }


        public static bool TryCoerceDouble(string str, out double result)
        {
            return double.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceFloat(string str, out float result)
        {
            return float.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceInt(string str, out int result)
        {
            return int.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceLong(string str, out long result)
        {
            return long.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

    }
}
