using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class AdminSearch
    {
        public string Query { get; set; }
        public string Tracker { get; set; }
        public int Category { get; set; }
    }
}
