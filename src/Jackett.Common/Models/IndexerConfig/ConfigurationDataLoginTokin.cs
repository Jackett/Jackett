using System;
using System.Globalization;
using Jackett.Common.Utils;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataLoginTokin : ConfigurationDataBasicLogin
    {
        public HiddenItem ApiToken { get; private set; }
        public HiddenItem LastTokenFetchDate { get; private set; }

        public DateTime LastTokenFetchDateTime
        {
            get
            {
                return DateTimeUtil.UnixTimestampToDateTime(ParseUtil.CoerceDouble(LastTokenFetchDate.Value));
            }
            set
            {
                LastTokenFetchDate.Value = DateTimeUtil.DateTimeToUnixTimestamp(value).ToString(CultureInfo.InvariantCulture);
            }
        }

        public ConfigurationDataLoginTokin() : base()
        {
            ApiToken = new HiddenItem { Name = "ApiToken" };
            LastTokenFetchDate = new HiddenItem { Name = "LastTokenFetchDate" };
            LastTokenFetchDateTime = DateTime.MinValue;
        }
    }
}
