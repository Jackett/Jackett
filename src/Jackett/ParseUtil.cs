using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public static class ParseUtil
    {
        public static float CoerceFloat(string str)
        {
            return float.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static int CoerceInt(string str)
        {
            return int.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static long CoerceLong(string str)
        {
            return long.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

    }
}
