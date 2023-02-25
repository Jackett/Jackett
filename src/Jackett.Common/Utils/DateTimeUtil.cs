using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Extensions;

namespace Jackett.Common.Utils
{
    /// <summary>
    /// All functions MUST return local time (local time zone)
    /// </summary>
    public static class DateTimeUtil
    {
        public const string Rfc1123ZPattern = "ddd, dd MMM yyyy HH':'mm':'ss z";

        private static readonly Regex _TimeAgoRegexp = new Regex(@"(?i)\bago", RegexOptions.Compiled);
        private static readonly Regex _TodayRegexp = new Regex(@"(?i)\btoday(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _TomorrowRegexp = new Regex(@"(?i)\btomorrow(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _YesterdayRegexp = new Regex(@"(?i)\byesterday(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
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
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks).ToLocalTime();
        }

        public static double DateTimeToUnixTimestamp(DateTime dt)
        {
            var date = dt.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        // ex: "2 hours 1 day"
        public static DateTime FromTimeAgo(string str, DateTime? relativeFrom = null)
        {
            str = str.ToLowerInvariant();
            var now = relativeFrom ?? DateTime.Now;

            if (str.Contains("now"))
                return DateTime.SpecifyKind(now, DateTimeKind.Local);

            str = str.Replace(",", "");
            str = str.Replace("ago", "");
            str = str.Replace("and", "");

            var timeAgo = TimeSpan.Zero;
            var timeAgoRegex = new Regex(@"\s*?([\d\.]+)\s*?([^\d\s\.]+)\s*?");
            var timeAgoMatches = timeAgoRegex.Match(str);

            while (timeAgoMatches.Success)
            {
                var val = ParseUtil.CoerceFloat(timeAgoMatches.Groups[1].Value);
                var unit = timeAgoMatches.Groups[2].Value;
                timeAgoMatches = timeAgoMatches.NextMatch();

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

            return DateTime.SpecifyKind(now - timeAgo, DateTimeKind.Local);
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

        private static DateTime FromFuzzyPastTime(string str, string format, DateTime now)
        {
            var result = FromFuzzyTime(str, format);
            if (result > now)
                result = result.AddYears(-1);
            return result;
        }

        public static DateTime FromUnknown(string str, string format = null, DateTime? relativeFrom = null)
        {
            try
            {
                str = ParseUtil.NormalizeSpace(str);

                if (DateTime.TryParseExact(str, Rfc1123ZPattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    return parsedDate;

                var now = relativeFrom ?? DateTime.Now;

                // try parsing the str as an unix timestamp
                if (str.IsAllDigits() && long.TryParse(str, out var unixTimeStamp))
                    return UnixTimestampToDateTime(unixTimeStamp);

                if (str.ToLower().Contains("now"))
                    return now;

                // ... ago
                var match = _TimeAgoRegexp.Match(str);
                if (match.Success)
                {
                    var timeAgo = str;
                    return FromTimeAgo(timeAgo);
                }

                // Today ...
                match = _TodayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    return dt;
                }

                // Yesterday ...
                match = _YesterdayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt -= TimeSpan.FromDays(1);
                    return dt;
                }

                // Tomorrow ...
                match = _TomorrowRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt += TimeSpan.FromDays(1);
                    return dt;
                }

                // [day of the week] at ... (eg: Saturday at 14:22)
                match = _DaysOfWeekRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified);
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

                // add missing year
                match = _MissingYearRegexp.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var newDate = now.Year + "-" + date;
                    str = str.Replace(date, newDate);
                    return FromFuzzyPastTime(str, format, now);
                }

                // add missing year 2
                match = _MissingYearRegexp2.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var time = match.Groups[2].Value;
                    str = date + " " + now.Year + " " + time;
                    return FromFuzzyPastTime(str, format, now);
                }

                return FromFuzzyTime(str, format);
            }
            catch (Exception ex)
            {
                throw new Exception($"DateTime parsing failed for \"{str}\": {ex}");
            }
        }

        // converts a date/time string to a DateTime object using a GoLang layout
        public static DateTime ParseDateTimeGoLang(string date, string layout, DateTime? relativeFrom = null)
        {
            var now = relativeFrom ?? DateTime.Now;

            date = ParseUtil.NormalizeSpace(date);

            var commonStandardFormats = new[] { "y", "h", "d" };

            if (commonStandardFormats.Any(layout.ContainsIgnoreCase) && DateTime.TryParseExact(date, layout, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return parsedDate;

            var format = layout

                // year
                .Replace("2006", "yyyy")
                .Replace("06", "yy")

                // month
                .Replace("January", "MMMM")
                .Replace("Jan", "MMM")
                .Replace("01", "MM")

                // day
                .Replace("Monday", "dddd")
                .Replace("Mon", "ddd")
                .Replace("02", "dd")
                //pattern = pattern.Replace("_2", "") // space padding not supported nativly by C#?
                .Replace("2", "d")

                // hours/minutes/seconds
                .Replace("05", "ss")

                .Replace("15", "HH")
                .Replace("03", "hh")
                .Replace("3", "h")

                .Replace("04", "mm")
                .Replace("4", "m")

                .Replace("5", "s")

                // month again
                .Replace("1", "M")

                // fractional seconds
                .Replace(".0000", "ffff")
                .Replace(".000", "fff")
                .Replace(".00", "ff")
                .Replace(".0", "f")

                .Replace(".9999", "FFFF")
                .Replace(".999", "FFF")
                .Replace(".99", "FF")
                .Replace(".9", "F")

                // AM/PM
                .Replace("PM", "tt")
                .Replace("pm", "tt") // not sure if this works

                // timezones
                // these might need further tuning
                //pattern = pattern.Replace("MST", "")
                //pattern = pattern.Replace("Z07:00:00", "")
                .Replace("Z07:00", "'Z'zzz")
                .Replace("Z07", "'Z'zz")
                //pattern = pattern.Replace("Z070000", "")
                //pattern = pattern.Replace("Z0700", "")
                .Replace("Z07:00", "'Z'zzz")
                .Replace("Z07", "'Z'zz")
                //pattern = pattern.Replace("-07:00:00", "")
                .Replace("-07:00", "zzz")
                //pattern = pattern.Replace("-0700", "zz")
                .Replace("-07", "zz");

            try
            {
                var dateTime = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);

                if (!format.Contains("yy") && dateTime > now)
                    dateTime = dateTime.AddYears(-1);

                return dateTime;
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Error while parsing DateTime \"{date}\", using layout \"{layout}\" ({format}): {ex.Message}", ex);
            }
        }

        private static TimeSpan ParseTimeSpan(string time) =>
            string.IsNullOrWhiteSpace(time)
                ? TimeSpan.Zero
                : DateTime.Parse(time).TimeOfDay;
    }
}
