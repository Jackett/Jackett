using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class TorznabCategory
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public List<TorznabCategory> SubCategories { get; private set; }

        public TorznabCategory()
        {
            SubCategories = new List<TorznabCategory>();
        }

        public static TorznabCategory Anime
        {
            get
            {
                return new TorznabCategory()
                {
                    ID = "5070",
                    Name = "TV/Anime"
                };
            }
        }

        public static TorznabCategory TV
        {
            get
            {
                return new TorznabCategory()
                {
                    ID = "5000",
                    Name = "TV"
                };
            }
        }

        public static TorznabCategory TVSD
        {
            get
            {
                return new TorznabCategory()
                {
                    ID = "5030",
                    Name = "TV/SD"
                };
            }
        }

        public static TorznabCategory TVHD
        {
            get
            {
                return new TorznabCategory()
                {
                    ID = "5040",
                    Name = "TV/HD"
                };
            }
        }
    }
}
