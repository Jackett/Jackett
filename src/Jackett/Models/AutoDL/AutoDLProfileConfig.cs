using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class AutoDLProfileConfig
    {
        public string Type { get; set; }
        public Dictionary<string, string> Options { set; get; } = new Dictionary<string, string>();
    }
}
