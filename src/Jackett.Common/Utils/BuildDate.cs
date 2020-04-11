using System;
using System.Globalization;
using System.Reflection;

namespace Jackett.Common.Utils
{
    public static class BuildDate
    {
        public static DateTime GetBuildDateTime()
        {
            var commonAssembly = Assembly.GetExecutingAssembly();
            var attribute = commonAssembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime ?? default(DateTime);
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value) => DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        public DateTime DateTime { get; }
    }
}
