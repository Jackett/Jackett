using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class NetworkSummary
    {
        public string Name { get; set; }
        public List<string> Servers { get; set; } = new List<string>();
        public List<string> Profiles { get; set; } = new List<string>();
    }
}
