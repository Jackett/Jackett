using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public static class DateTimeUtil
    {
        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

    }
}
