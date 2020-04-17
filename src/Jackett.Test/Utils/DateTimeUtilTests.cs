using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Jackett.Common.Utils.Tests
{
    [TestFixture]
    public class DateTimeUtilTests
    {
        [Test]
        public void FromUnknownTest()
        {
            var today = DateTime.UtcNow.Date;
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
                {"Yesterday\n 11:55 PM", yesterday.AddHours(23).AddMinutes(55)}
            };

            foreach (var testCase in testCases)
                Assert.AreEqual(testCase.Value, DateTimeUtil.FromUnknown(testCase.Key));
        }
    }
}
