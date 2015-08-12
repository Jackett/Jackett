using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Website.Models
{
    public class DownloadCount
    {
        public int Id { get; set; }
        public string File { get; set; }
        public string Version { get; set; }
        public int Count { get; set; }
    }
}
