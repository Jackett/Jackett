using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public static class DateTimeUtil
    {
        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

        public static double DateTimeToUnixTimestamp(DateTime dt)
        {
            var date = dt.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        // ex: "2 hours 1 day"
        public static DateTime FromTimeAgo(string str)
        {
            str = str.ToLowerInvariant();
            if (str.Contains("now"))
            {
                return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            }

            var dateParts = str.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            TimeSpan timeAgo = TimeSpan.Zero;
            for (var i = 0; i < dateParts.Length / 2; i++)
            {
                var val = ParseUtil.CoerceFloat(dateParts[i * 2]);
                var unit = dateParts[i * 2 + 1];
                if (unit.Contains("sec"))
                    timeAgo += TimeSpan.FromSeconds(val);
                else if (unit.Contains("min"))
                    timeAgo += TimeSpan.FromMinutes(val);
                else if (unit.Contains("hour") || unit.Contains("hr"))
                    timeAgo += TimeSpan.FromHours(val);
                else if (unit.Contains("day"))
                    timeAgo += TimeSpan.FromDays(val);
                else if (unit.Contains("week") || unit.Contains("wk"))
                    timeAgo += TimeSpan.FromDays(val * 7);
                else if (unit.Contains("month"))
                    timeAgo += TimeSpan.FromDays(val * 30);
                else if (unit.Contains("year"))
                    timeAgo += TimeSpan.FromDays(val * 365);
                else
                {
                    throw new Exception("TimeAgo parsing failed");
                }
            }

            return DateTime.SpecifyKind(DateTime.Now - timeAgo, DateTimeKind.Local);
        }
    }
}
