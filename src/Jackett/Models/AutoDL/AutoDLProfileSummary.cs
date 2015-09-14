using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class AutoDLProfileSummary
    {
        public string Type { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string SiteName { get; set; }
        public bool IsConfigured { set; get; }
        public List<ConfigOption> Options { get; set; } = new List<ConfigOption>();
    }
}
