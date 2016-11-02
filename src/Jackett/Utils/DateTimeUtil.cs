using Cliver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public static class DateTimeUtil
    {
        public static string RFC1123ZPattern = "ddd, dd MMM yyyy HH':'mm':'ss z";

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

            str = str.Replace(",", "");
            str = str.Replace("ago", "");
            str = str.Replace("and", "");

            TimeSpan timeAgo = TimeSpan.Zero;
            Regex TimeagoRegex = new Regex(@"\s*?([\d\.]+)\s*?([^\d\s\.]+)\s*?");
            var TimeagoMatches = TimeagoRegex.Match(str);

            while (TimeagoMatches.Success)
            {
                string expanded = string.Empty;

                var val = ParseUtil.CoerceFloat(TimeagoMatches.Groups[1].Value);
                var unit = TimeagoMatches.Groups[2].Value;
                TimeagoMatches = TimeagoMatches.NextMatch();

                if (unit.Contains("sec") || unit == "s")
                    timeAgo += TimeSpan.FromSeconds(val);
                else if (unit.Contains("min") || unit == "m")
                    timeAgo += TimeSpan.FromMinutes(val);
                else if (unit.Contains("hour") || unit.Contains("hr") || unit == "h")
                    timeAgo += TimeSpan.FromHours(val);
                else if (unit.Contains("day") ||unit == "d")
                    timeAgo += TimeSpan.FromDays(val);
                else if (unit.Contains("week") || unit.Contains("wk") || unit == "w")
                    timeAgo += TimeSpan.FromDays(val * 7);
                else if (unit.Contains("month") || unit == "mo")
                    timeAgo += TimeSpan.FromDays(val * 30);
                else if (unit.Contains("year") || unit == "y")
                    timeAgo += TimeSpan.FromDays(val * 365);
                else
                {
                    throw new Exception("TimeAgo parsing failed, unknown unit: "+unit);
                }
            }

            return DateTime.SpecifyKind(DateTime.Now - timeAgo, DateTimeKind.Local);
        }

        public static TimeSpan ParseTimeSpan(string time)
        {
            TimeSpan offset = TimeSpan.Zero;
            if (time.EndsWith("AM"))
            {
                time = time.Substring(0, time.Length - 2);
            }
            else if (time.EndsWith("PM"))
            {
                time = time.Substring(0, time.Length - 2);
                offset = TimeSpan.FromHours(12);
            }

            var ts = TimeSpan.Parse(time);
            ts += offset;
            return ts;
        }

        // Uses the DateTimeRoutines library to parse the date
        // http://www.codeproject.com/Articles/33298/C-Date-Time-Parser
        public static DateTime FromFuzzyTime(string str, DateTimeRoutines.DateTimeFormat format = DateTimeRoutines.DateTimeFormat.USA_DATE)
        {
            DateTimeRoutines.ParsedDateTime dt;
            if (DateTimeRoutines.TryParseDateOrTime(str, format, out dt))
            {
                return dt.DateTime;
            }
            throw new Exception("FromFuzzyTime parsing failed");
        }

        public static Regex timeAgoRegexp = new Regex(@"(?i)\bago", RegexOptions.Compiled);
        public static Regex todayRegexp = new Regex(@"(?i)\btoday([\s,]+|$)", RegexOptions.Compiled);
        public static Regex tomorrowRegexp = new Regex(@"(?i)\btomorrow([\s,]+|$)", RegexOptions.Compiled);
        public static Regex yesterdayRegexp = new Regex(@"(?i)\byesterday([\s,]+|$)", RegexOptions.Compiled);
        public static Regex missingYearRegexp = new Regex(@"^\d{1,2}-\d{1,2}\b", RegexOptions.Compiled);

        public static DateTime FromUnknown(string str)
        {
            str = ParseUtil.NormalizeSpace(str);
            Match match;

            // ... ago
            match = timeAgoRegexp.Match(str);
            if (match.Success)
            {
                var timeago = str;
                return FromTimeAgo(timeago);
            }

            // Today ...
            match = todayRegexp.Match(str);
            if (match.Success)
            {
                var time = str.Replace(match.Groups[0].Value, "");
                DateTime dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                dt += ParseTimeSpan(time);
                return dt;
            }

            // Yesterday ...
            match = yesterdayRegexp.Match(str);
            if (match.Success)
            {
                var time = str.Replace(match.Groups[0].Value, "");
                DateTime dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                dt += ParseTimeSpan(time);
                dt -= TimeSpan.FromDays(1);
                return dt;
            }

            // Tomorrow ...
            match = tomorrowRegexp.Match(str);
            if (match.Success)
            {
                var time = str.Replace(match.Groups[0].Value, "");
                DateTime dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                dt += ParseTimeSpan(time);
                dt += TimeSpan.FromDays(1);
                return dt;
            }

            // add missing year
            match = missingYearRegexp.Match(str);
            if (match.Success)
            {
                var date = match.Groups[0].Value;
                string newDate = date+"-"+DateTime.Now.Year.ToString();
                str = str.Replace(date, newDate);
            }
            return FromFuzzyTime(str);
        }
    }
}
