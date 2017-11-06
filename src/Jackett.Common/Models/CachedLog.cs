using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class CachedLog
    {
        public string Level { set; get; }
        public string Message { set; get; }
        public DateTime When { set; get; }
    }
}
