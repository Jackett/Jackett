using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Website.Models
{
    public class Release
    {
        public int Id { get; set; }
        public DateTime When { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
    }
}
