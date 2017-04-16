using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.GitHub
{
    public class Release
    {
        public string Name { set; get; }
        public DateTime Created_at { get; set; }
        public bool Prerelease { get; set; }
        public List<Asset> Assets { set; get; } = new List<Asset>();
    }
}
