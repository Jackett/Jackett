using System;
using System.Collections.Generic;

namespace Jackett.Common.Models.GitHub
{
    public class Release
    {
        public string Name { set; get; }
        public DateTime Created_at { get; set; }
        public bool Prerelease { get; set; }
        public List<Asset> Assets { set; get; } = new List<Asset>();
    }
}
