using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class TorrentPotatoRequest
    {
        public string username { get; set; }
        public string passkey { get; set; }
        public string imdbid { get; set; }
        public string search { get; set; }
    }
}
