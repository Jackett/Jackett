using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class ChannelInfo
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Uri Link { get; set; }
        public string Language { get; set; }
        public string Category { get; set; }
        public Uri ImageUrl { get; set; }
        public string ImageTitle { get; set; }
        public Uri ImageLink { get; set; }
        public string ImageDescription { get; set; }

        public ChannelInfo()
        {
            // Set default values
            Language = "en-us";
            Category = "search";
        }
    }
}
