using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jackett.Common.Utils
{
    public static class DateTimeUtil
    {
        public const string Rfc1123ZPattern = "ddd, dd MMM yyyy HH':'mm':'ss z";

        private static readonly Regex _TimeAgoRegexp = new Regex(@"(?i)\bago", RegexOptions.Compiled);
        private static readonly Regex _TodayRegexp = new Regex(@"(?i)\btoday([\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _TomorrowRegexp = new Regex(@"(?i)\btomorrow([\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _YesterdayRegexp = new Regex(@"(?i)\byesterday([\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _DaysOfWeekRegexp = new Regex(@"(?i)\b(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\s+at\s+", RegexOptions.Compiled);
        private static readonly Regex _MissingYearRegexp = new Regex(@"^(\d{1,2}-\d{1,2})(\s|$)", RegexOptions.Compiled);
        private static readonly Regex _MissingYearRegexp2 = new Regex(@"^(\d{1,2}\s+\w{3})\s+(\d{1,2}\:\d{1,2}.*)$", RegexOptions.Compiled); // 1 Jan 10:30

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(unixTime).ToLocalTime();
            return dt;
        }

        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
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
                return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

            str = str.Replace(",", "");
            str = str.Replace("ago", "");
            str = str.Replace("and", "");

            var timeAgo = TimeSpan.Zero;
            var timeagoRegex = new Regex(@"\s*?([\d\.]+)\s*?([^\d\s\.]+)\s*?");
            var timeagoMatches = timeagoRegex.Match(str);

            while (timeagoMatches.Success)
            {
                var val = ParseUtil.CoerceFloat(timeagoMatches.Groups[1].Value);
                var unit = timeagoMatches.Groups[2].Value;
                timeagoMatches = timeagoMatches.NextMatch();

                if (unit.Contains("sec") || unit == "s")
                    timeAgo += TimeSpan.FromSeconds(val);
                else if (unit.Contains("min") || unit == "m")
                    timeAgo += TimeSpan.FromMinutes(val);
                else if (unit.Contains("hour") || unit.Contains("hr") || unit == "h")
                    timeAgo += TimeSpan.FromHours(val);
                else if (unit.Contains("day") || unit == "d")
                    timeAgo += TimeSpan.FromDays(val);
                else if (unit.Contains("week") || unit.Contains("wk") || unit == "w")
                    timeAgo += TimeSpan.FromDays(val * 7);
                else if (unit.Contains("month") || unit == "mo")
                    timeAgo += TimeSpan.FromDays(val * 30);
                else if (unit.Contains("year") || unit == "y")
                    timeAgo += TimeSpan.FromDays(val * 365);
                else
                    throw new Exception("TimeAgo parsing failed, unknown unit: " + unit);
            }

            return DateTime.SpecifyKind(DateTime.Now - timeAgo, DateTimeKind.Local);
        }

        // Uses the DateTimeRoutines library to parse the date
        // http://www.codeproject.com/Articles/33298/C-Date-Time-Parser
        public static DateTime FromFuzzyTime(string str, string format = null)
        {
            var dtFormat = format == "UK" ?
                DateTimeRoutines.DateTimeRoutines.DateTimeFormat.UkDate :
                DateTimeRoutines.DateTimeRoutines.DateTimeFormat.UsaDate;

            if (DateTimeRoutines.DateTimeRoutines.TryParseDateOrTime(
                str, dtFormat, out DateTimeRoutines.DateTimeRoutines.ParsedDateTime dt))
                return dt.DateTime;

            throw new Exception("FromFuzzyTime parsing failed");
        }

        public static DateTime FromUnknown(string str, string format = null)
        {
            try
            {
                str = ParseUtil.NormalizeSpace(str);
                if (str.ToLower().Contains("now"))
                    return DateTime.UtcNow;

                // ... ago
                var match = _TimeAgoRegexp.Match(str);
                if (match.Success)
                {
                    var timeago = str;
                    return FromTimeAgo(timeago);
                }

                // Today ...
                match = _TodayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    return dt;
                }

                // Yesterday ...
                match = _YesterdayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt -= TimeSpan.FromDays(1);
                    return dt;
                }

                // Tomorrow ...
                match = _TomorrowRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt += TimeSpan.FromDays(1);
                    return dt;
                }

                // [day of the week] at ... (eg: Saturday at 14:22)
                match = _DaysOfWeekRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);

                    DayOfWeek dow;
                    var groupMatchLower = match.Groups[1].Value.ToLower();
                    if (groupMatchLower.StartsWith("monday"))
                        dow = DayOfWeek.Monday;
                    else if (groupMatchLower.StartsWith("tuesday"))
                        dow = DayOfWeek.Tuesday;
                    else if (groupMatchLower.StartsWith("wednesday"))
                        dow = DayOfWeek.Wednesday;
                    else if (groupMatchLower.StartsWith("thursday"))
                        dow = DayOfWeek.Thursday;
                    else if (groupMatchLower.StartsWith("friday"))
                        dow = DayOfWeek.Friday;
                    else if (groupMatchLower.StartsWith("saturday"))
                        dow = DayOfWeek.Saturday;
                    else
                        dow = DayOfWeek.Sunday;

                    while (dt.DayOfWeek != dow)
                        dt = dt.AddDays(-1);
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
                match = _MissingYearRegexp.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var newDate = DateTime.Now.Year + "-" + date;
                    str = str.Replace(date, newDate);
                }

                // add missing year 2
                match = _MissingYearRegexp2.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var time = match.Groups[2].Value;
                    str = date + " " + DateTime.Now.Year + " " + time;
                }

                return FromFuzzyTime(str, format);
            }
            catch (Exception ex)
            {
                throw new Exception($"DateTime parsing failed for \"{str}\": {ex}");
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
                throw new FormatException($"Error while parsing DateTime \"{date}\", using layout \"{layout}\" ({pattern}): {ex.Message}");
            }
        }

        private static TimeSpan ParseTimeSpan(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return TimeSpan.Zero;

            var offset = TimeSpan.Zero;
            if (time.EndsWith("AM"))
            {
                time = time.Substring(0, time.Length - 2);
                if (time.StartsWith("12")) // 12:15 AM becomes 00:15
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

    }
}
