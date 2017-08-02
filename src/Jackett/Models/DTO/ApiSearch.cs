using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.DTO
{
    public class ApiSearch
    {
        public string Query { get; set; }
        public int Category { get; set; }
    }
}
