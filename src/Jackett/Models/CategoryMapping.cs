using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    class CategoryMapping
    {
        public CategoryMapping(string trackerCat, int newzCat)
        {
            TrackerCategory = trackerCat;
            NewzNabCategory = newzCat;
        }

        public string TrackerCategory { get; private set; }
        public int NewzNabCategory { get; private set; }
    }
}
