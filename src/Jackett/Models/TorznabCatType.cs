using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    class TorznabCatType
    {
        private static Dictionary<int, string> cats = new Dictionary<int, string>();

        static TorznabCatType()
        {
            cats.Add(5000, "TV");
            cats.Add(5030, "TV/SD");
            cats.Add(5040, "TV/HD");
            cats.Add(5070, "TV/Anime");
            cats.Add(8000, "Books");
            cats.Add(8020, "Books/Comics");
            cats.Add(4000, "PC");
            cats.Add(3030, "Audio/Audiobook");
            cats.Add(2000, "Movies");
            cats.Add(2040, "Movies/HD");
            cats.Add(2030, "Movies/SD");
            cats.Add(2010, "Movies/Foreign");
            cats.Add(3000, "Audio");
            cats.Add(3040, "Audio/Lossless");
            cats.Add(3010, "Audio/MP3");
            cats.Add(6000, "XXX");
            cats.Add(6040, "XXX/x264");
            cats.Add(6010, "XXX/DVD");
            cats.Add(6060, "XXX/Imageset");
        }

        public static bool QueryContainsParentCategory(int[] queryCats, int releaseCat)
        {
            if (cats.ContainsKey(releaseCat) && queryCats!=null)
            {
                var ncab = cats[releaseCat];
                var split = ncab.IndexOf("/");
                if (split > -1)
                {
                    string parentCatName = ncab.Substring(0,split);
                    if (cats.ContainsValue(parentCatName))
                    {
                        var parentCat = cats.Where(c => c.Value == parentCatName).First().Key;
                        return queryCats.Contains(parentCat);
                    }
                }
            }

            return false;
        }

        public static string GetCatDesc(int newznabcat)
        {
            if (cats.ContainsKey(newznabcat))
            {
                return cats[newznabcat];
            }

            return string.Empty;
        }

        private static TorznabCategory GetCat(int id)
        {
            return new TorznabCategory()
            {
                ID = id,
                Name = cats[id]
            };
        }

        public static TorznabCategory Anime
        {
            get { return GetCat(5070); }
        }

        public static TorznabCategory TV
        {
            get { return GetCat(5000); }
        }

        public static TorznabCategory TVSD
        {
            get { return GetCat(5030); }
        }

        public static TorznabCategory TVHD
        {
            get { return GetCat(5040); }
        }

        public static TorznabCategory Books
        {
            get { return GetCat(8000); }
        }

        public static TorznabCategory Comic
        {
            get { return GetCat(8020); }
        }

        public static TorznabCategory Apps
        {
            get { return GetCat(4000); }
        }

        public static TorznabCategory AudioBooks
        {
            get { return GetCat(3030); }
        }

        public static TorznabCategory Movies
        {
            get { return GetCat(2000); }
        }

        public static TorznabCategory MoviesHD
        {
            get { return GetCat(2040); }
        }

        public static TorznabCategory MoviesSD
        {
            get { return GetCat(2030); }
        }

        public static TorznabCategory MoviesForeign
        {
            get { return GetCat(2040); }
        }

        public static TorznabCategory Audio
        {
            get { return GetCat(3000); }
        }

        public static TorznabCategory AudioLossless
        {
            get { return GetCat(3040); }
        }

        public static TorznabCategory AudioLossy
        {
            get { return GetCat(3010); }
        }

        public static TorznabCategory XXX
        {
            get { return GetCat(6000); }
        }

        public static TorznabCategory XXXHD
        {
            get { return GetCat(6040); }
        }

        public static TorznabCategory XXXSD
        {
            get { return GetCat(6010); }
        }

        public static TorznabCategory XXXImg
        {
            get { return GetCat(6060); }
        }
    }
}
