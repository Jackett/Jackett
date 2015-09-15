using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public class ParseInfo
    {
        public List<LinePatterns> SingleLineMatches { get; set; } = new List<LinePatterns>();
        public List<LineMatched> MatchParsers { get; set; } = new List<LineMatched>();
        public List<Ignore> IgnoreMatches { get; set; } = new List<Ignore>();
    }
}
