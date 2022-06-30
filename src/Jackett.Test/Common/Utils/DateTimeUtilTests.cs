using System;
using System.Collections.Generic;
using Jackett.Common.Utils;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class DateTimeUtilTests
    {
        [Test]
        public void UnixTimestampToDateTimeTest()
        {
            const long timestampLong = 1604113707;
            const double timestampDouble = 1604113707.0;
            var expected = new DateTime(2020, 10, 31, 3, 8, 27, DateTimeKind.Utc).ToLocalTime();

            Assert.AreEqual(expected, DateTimeUtil.UnixTimestampToDateTime(timestampLong));
            Assert.AreEqual(expected, DateTimeUtil.UnixTimestampToDateTime(timestampDouble));
        }

        [Test]
        public void DateTimeToUnixTimestampTest()
        {
            var dateTime = new DateTime(2020, 10, 31, 3, 8, 27, DateTimeKind.Utc);
            const double expected = 1604113707.0;

            Assert.AreEqual(expected, DateTimeUtil.DateTimeToUnixTimestamp(dateTime));
        }

        [Test]
        public void FromTimeAgoTest()
        {
            var now = DateTime.Now;

            AssertSimilarDates(now, DateTimeUtil.FromTimeAgo("NOW")); // case insensitive
            AssertSimilarDates(now, DateTimeUtil.FromTimeAgo("now"));
            AssertSimilarDates(now.AddSeconds(-1), DateTimeUtil.FromTimeAgo("1 sec"));
            AssertSimilarDates(now.AddMinutes(-12), DateTimeUtil.FromTimeAgo("12 min ago"));
            AssertSimilarDates(now.AddHours(-20), DateTimeUtil.FromTimeAgo("20 h"));
            AssertSimilarDates(now.AddDays(-3), DateTimeUtil.FromTimeAgo("3 days"));
            AssertSimilarDates(now.AddDays(-7), DateTimeUtil.FromTimeAgo("1 week"));
            AssertSimilarDates(now.AddDays(-60), DateTimeUtil.FromTimeAgo("2 month ago"));
            AssertSimilarDates(now.AddDays(-365), DateTimeUtil.FromTimeAgo("1 year"));
            AssertSimilarDates(now.AddHours(-20).AddMinutes(-15), DateTimeUtil.FromTimeAgo("20 hours and 15 minutes ago"));
            AssertSimilarDates(now, DateTimeUtil.FromTimeAgo(""));

            // bad cases
            try
            {
                DateTimeUtil.FromTimeAgo("1 bad"); // unknown unit
                Assert.Fail();
            }
            catch
            {
                // ignored
            }
        }

        [Test]
        public void FromFuzzyTimeTest()
        {
            // there are few tests because we are using a well known library
            var now = DateTime.Now;

            Assert.AreEqual(new DateTimeOffset(2010, 6, 21, 4, 20, 19, new TimeSpan(-4, -30, 0)).DateTime,
                DateTimeUtil.FromFuzzyTime("21 Jun 2010 04:20:19 -0430 blah"));
            Assert.AreEqual(new DateTime(2005, 6, 10, 10, 30, 0),
                DateTimeUtil.FromFuzzyTime("June 10, 2005 10:30AM", "UK"));
            Assert.AreEqual(new DateTime(now.Year, now.Month, now.Day, 19, 54, 0),
                DateTimeUtil.FromFuzzyTime("today 19:54"));
            Assert.True((now - DateTimeUtil.FromFuzzyTime("Yesterday at 10:20 pm")).TotalSeconds <= 3600 * 24); // 1 day
            Assert.True((now - DateTimeUtil.FromFuzzyTime("Sunday at 14:30")).TotalSeconds <= 3600 * 24 * 7); // 1 week

            // bad cases
            try
            {
                DateTimeUtil.FromFuzzyTime("1 bad");
                Assert.Fail();
            }
            catch
            {
                // ignored
            }
        }

        [Test]
        public void FromUnknownTest()
        {
            var now = DateTime.Now;
            var today = now.ToUniversalTime().Date;
            var yesterday = today.AddDays(-1);
            var tomorrow = today.AddDays(1);
            var testCases = new Dictionary<string, DateTime>
            {
                {"today, at 1:00 PM", today.AddHours(13)},
                {"today at 12:00PM", today.AddHours(12)},
                {"Today", today},
                {"Today at 20:19:54", today.AddHours(20).AddMinutes(19).AddSeconds(54)},
                {"Today 22:29", today.AddHours(22).AddMinutes(29)},
                {"Yesterday\n 19:54:54", yesterday.AddHours(19).AddMinutes(54).AddSeconds(54)},
                {"yesterday\n 11:55 PM", yesterday.AddHours(23).AddMinutes(55)},
                {"Tomorrow\n 19:54:54", tomorrow.AddHours(19).AddMinutes(54).AddSeconds(54)},
                {"tomorrow 22:29", tomorrow.AddHours(22).AddMinutes(29)},
            };

            foreach (var testCase in testCases)
                Assert.AreEqual(testCase.Value, DateTimeUtil.FromUnknown(testCase.Key, relativeFrom: now));

            Assert.AreEqual(now, DateTimeUtil.FromUnknown("now", relativeFrom: now));
            AssertSimilarDates(now.AddHours(-3), DateTimeUtil.FromUnknown("3 hours ago", relativeFrom: now));

            Assert.True((now - DateTimeUtil.FromUnknown("monday at 10:20 am", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7); // 7 days
            Assert.True((now - DateTimeUtil.FromUnknown("Tuesday at 22:20", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);
            Assert.True((now - DateTimeUtil.FromUnknown("wednesday at \n 22:20", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);
            Assert.True((now - DateTimeUtil.FromUnknown("\n thursday \n at 22:20", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);
            Assert.True((now - DateTimeUtil.FromUnknown("friday at 22:20", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);
            Assert.True((now - DateTimeUtil.FromUnknown("Saturday at 00:20", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);
            Assert.True((now - DateTimeUtil.FromUnknown("sunday at 22:00", relativeFrom: now)).TotalSeconds <= 3600 * 24 * 7);

            Assert.AreEqual(new DateTime(2020, 10, 31, 3, 8, 27, DateTimeKind.Utc).ToLocalTime(),
                DateTimeUtil.FromUnknown("1604113707", relativeFrom: now));

            var refDate = new DateTime(2021, 03, 12, 12, 00, 00, DateTimeKind.Local);
            Assert.AreEqual(new DateTime(refDate.Year, 2, 1), DateTimeUtil.FromUnknown("02-01", relativeFrom: refDate));
            Assert.AreEqual(new DateTime(refDate.Year, 2, 1), DateTimeUtil.FromUnknown("2-1", relativeFrom: refDate));
            Assert.AreEqual(new DateTime(refDate.Year, 1, 2, 10, 30, 0), DateTimeUtil.FromUnknown("2 Jan 10:30", relativeFrom: refDate));

            Assert.AreEqual(new DateTime(2005, 6, 10, 10, 30, 0),
                DateTimeUtil.FromUnknown("June 10, 2005 10:30AM"));

            // bad cases
            try
            {
                DateTimeUtil.FromUnknown("1 bad");
                Assert.Fail();
            }
            catch
            {
                // ignored
            }

            Assert.AreEqual(new DateTime(refDate.Year - 1, 5, 2), DateTimeUtil.FromUnknown("05-02", relativeFrom: refDate));
            Assert.AreEqual(new DateTime(refDate.Year - 1, 5, 2), DateTimeUtil.FromUnknown("5-2", relativeFrom: refDate));
            Assert.AreEqual(new DateTime(refDate.Year - 1, 5, 2, 10, 30, 0), DateTimeUtil.FromUnknown("2 May 10:30", relativeFrom: refDate));

            Assert.AreEqual(new DateTime(2020, 12, 31, 23, 59, 0), DateTimeUtil.FromUnknown("12-31 23:59", relativeFrom: new DateTime(2021, 12, 31, 23, 58, 59, DateTimeKind.Local)));
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 1, 0), DateTimeUtil.FromUnknown("1-1 00:01", relativeFrom: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local)));
        }

        [Test]
        public void ParseDateTimeGoLangTest()
        {
            var now = DateTime.Now;

            Assert.AreEqual(new DateTimeOffset(2010, 6, 21, 4, 20, 19, new TimeSpan(-4, 0, 0)).ToLocalTime().DateTime,
                DateTimeUtil.ParseDateTimeGoLang("21-06-2010 04:20:19 -04:00", "02-01-2006 15:04:05 -07:00"));
            Assert.AreEqual(new DateTimeOffset(2010, 6, 21, 0, 0, 0, new TimeSpan(-5, -30, 0)).ToLocalTime().DateTime,
                DateTimeUtil.ParseDateTimeGoLang("2010-06-21 -05:30", "2006-01-02 -07:00"));
            var refDate = new DateTime(2022, 03, 12, 12, 00, 00, DateTimeKind.Local);
            Assert.AreEqual(new DateTime(refDate.Year - 1, 9, 14, 7, 0, 0),
                            DateTimeUtil.ParseDateTimeGoLang("7am Sep. 14", "3pm Jan. 2", relativeFrom: refDate));

            // bad cases
            try
            {
                DateTimeUtil.ParseDateTimeGoLang("21-06-2010 04:20:19", "02-01-2006 15:04:05 -07:00");
                Assert.Fail();
            }
            catch
            {
                // ignored
            }
        }

        private static void AssertSimilarDates(DateTime dt1, DateTime dt2, int delta = 5)
        {
            var diff = Math.Abs((dt1 - dt2).TotalSeconds);
            Assert.True(diff < delta, $"Dates are not similar. Expected: {dt1} But was: {dt2}");
        }
    }
}
