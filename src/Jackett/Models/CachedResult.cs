using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class CachedResult
    {
        public ReleaseInfo Result
        {
            set; get;
        }

        public DateTime Created
        {
            set; get;
        }
    }
}
