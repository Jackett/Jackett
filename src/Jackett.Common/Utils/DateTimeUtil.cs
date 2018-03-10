using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jackett.Common.Utils
{
    public static class DateTimeUtil
    {
        public static string RFC1123ZPattern = "ddd, dd MMM yyyy HH':'mm':'ss z";

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dt = dt.AddSeconds(unixTime).ToLocalTime();
            return dt;
        }

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
            if (string.IsNullOrWhiteSpace(time))
                return TimeSpan.Zero;

            TimeSpan offset = TimeSpan.Zero;
            if (time.EndsWith("AM"))
            {
                time = time.Substring(0, time.Length - 2);
                if(time.StartsWith("12")) // 12:15 AM becomes 00:15
                    time = "00" + time.Substring(2);
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
        public static DateTime FromFuzzyTime(string str, string format = null)
        {
            DateTimeRoutines.DateTimeRoutines.DateTimeFormat dt_format = DateTimeRoutines.DateTimeRoutines.DateTimeFormat.USA_DATE;
            if (format == "UK")
            {
                dt_format = DateTimeRoutines.DateTimeRoutines.DateTimeFormat.UK_DATE;
            }

            DateTimeRoutines.DateTimeRoutines.ParsedDateTime dt;
            if (DateTimeRoutines.DateTimeRoutines.TryParseDateOrTime(str, dt_format, out dt))
            {
                return dt.DateTime;
            }
            throw new Exception("FromFuzzyTime parsing failed");
        }

        public static Regex timeAgoRegexp = new Regex(@"(?i)\bago", RegexOptions.Compiled);
        public static Regex todayRegexp = new Regex(@"(?i)\btoday([\s,]*|$)", RegexOptions.Compiled);
        public static Regex tomorrowRegexp = new Regex(@"(?i)\btomorrow([\s,]*|$)", RegexOptions.Compiled);
        public static Regex yesterdayRegexp = new Regex(@"(?i)\byesterday([\s,]*|$)", RegexOptions.Compiled);
        public static Regex missingYearRegexp = new Regex(@"^(\d{1,2}-\d{1,2})(\s|$)", RegexOptions.Compiled);
        public static Regex missingYearRegexp2 = new Regex(@"^(\d{1,2}\s+\w{3})\s+(\d{1,2}\:\d{1,2}.*)$", RegexOptions.Compiled); // 1 Jan 10:30

        public static DateTime FromUnknown(string str, string format = null)
        {
            try {
                str = ParseUtil.NormalizeSpace(str);
                Match match;

                if(str.ToLower().Contains("now"))
                {
                    return DateTime.UtcNow;
                }

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

                try
                {
                    // try parsing the str as an unix timestamp
                    var unixTimeStamp = long.Parse(str);
                    return UnixTimestampToDateTime(unixTimeStamp);
                }
                catch (FormatException)
                {
                    // it wasn't a timestamp, continue....
                }

                // add missing year
                match = missingYearRegexp.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    string newDate = DateTime.Now.Year.ToString()+ "-"+date;
                    str = str.Replace(date, newDate);
                }

                // add missing year 2
                match = missingYearRegexp2.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var time = match.Groups[2].Value;
                    str = date + " " + DateTime.Now.Year.ToString() + " " + time;
                }

                return FromFuzzyTime(str, format);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DateTime parsing failed for \"{0}\": {1}", str, ex.ToString()));
            }
        }

        // converts a date/time string to a DateTime object using a GoLang layout
        public static DateTime ParseDateTimeGoLang(string date, string layout)
        {
            date = ParseUtil.NormalizeSpace(date);
            var pattern = layout;

            // year
            pattern = pattern.Replace("2006", "yyyy");
            pattern = pattern.Replace("06", "yy");

            // month
            pattern = pattern.Replace("January", "MMMM");
            pattern = pattern.Replace("Jan", "MMM");
            pattern = pattern.Replace("01", "MM");

            // day
            pattern = pattern.Replace("Monday", "dddd");
            pattern = pattern.Replace("Mon", "ddd");
            pattern = pattern.Replace("02", "dd");
            //pattern = pattern.Replace("_2", ""); // space padding not supported nativly by C#?
            pattern = pattern.Replace("2", "d");

            // hours/minutes/seconds
            pattern = pattern.Replace("05", "ss");

            pattern = pattern.Replace("15", "HH");
            pattern = pattern.Replace("03", "hh");
            pattern = pattern.Replace("3", "h");

            pattern = pattern.Replace("04", "mm");
            pattern = pattern.Replace("4", "m");

            pattern = pattern.Replace("5", "s");

            // month again
            pattern = pattern.Replace("1", "M");

            // fractional seconds
            pattern = pattern.Replace(".0000", "ffff");
            pattern = pattern.Replace(".000", "fff");
            pattern = pattern.Replace(".00", "ff");
            pattern = pattern.Replace(".0", "f");

            pattern = pattern.Replace(".9999", "FFFF");
            pattern = pattern.Replace(".999", "FFF");
            pattern = pattern.Replace(".99", "FF");
            pattern = pattern.Replace(".9", "F");

            // AM/PM
            pattern = pattern.Replace("PM", "tt");
            pattern = pattern.Replace("pm", "tt"); // not sure if this works

            // timezones
            // these might need further tuning
            //pattern = pattern.Replace("MST", "");
            //pattern = pattern.Replace("Z07:00:00", "");
            pattern = pattern.Replace("Z07:00", "'Z'zzz");
            pattern = pattern.Replace("Z07", "'Z'zz");
            //pattern = pattern.Replace("Z070000", "");
            //pattern = pattern.Replace("Z0700", "");
            pattern = pattern.Replace("Z07:00", "'Z'zzz");
            pattern = pattern.Replace("Z07", "'Z'zz");
            //pattern = pattern.Replace("-07:00:00", "");
            pattern = pattern.Replace("-07:00", "zzz");
            //pattern = pattern.Replace("-0700", "zz");
            pattern = pattern.Replace("-07", "zz");

            try
            {
                return DateTime.ParseExact(date, pattern, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                throw new FormatException(string.Format("Error while parsing DateTime \"{0}\", using layout \"{1}\" ({2}): {3}", date, layout, pattern, ex.Message));
            }
        }
    }
}
