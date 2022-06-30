using System;

namespace Jackett.Common.Models
{
    public class CachedLog
    {
        public string Level { set; get; }
        public string Message { set; get; }
        public DateTime When { set; get; }
    }
}
