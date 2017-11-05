using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    class CategoryMapping
    {
        public CategoryMapping(string trackerCat, string trackerCatDesc, int newzCat)
        {
            TrackerCategory = trackerCat;
            TrackerCategoryDesc = trackerCatDesc;
            NewzNabCategory = newzCat;
        }

        public string TrackerCategory { get; private set; }
        public string TrackerCategoryDesc { get; private set; }
        public int NewzNabCategory { get; private set; }
    }
}
