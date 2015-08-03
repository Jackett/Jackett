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
        public static double CoerceDouble(string str)
        {
            return double.Parse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static float CoerceFloat(string str)
        {
            return float.Parse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static int CoerceInt(string str)
        {
            return int.Parse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static long CoerceLong(string str)
        {
            return long.Parse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }


        public static bool TryCoerceDouble(string str, out double result)
        {
            return double.TryParse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceFloat(string str, out float result)
        {
            return float.TryParse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceInt(string str, out int result)
        {
            return int.TryParse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryCoerceLong(string str, out long result)
        {
            return long.TryParse(str.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

    }
}
