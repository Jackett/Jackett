using AutoMapper;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public class ParserState
    {
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> HTTPHeaders { get; set; } = new Dictionary<string, string>();
        public List<string> TempVariables { get; set; } = new List<string>();
        public Logger Logger { get; set; }
        public string Tracker { get; set; }

        public string CurrentItem { get; set; }
        public string CurrentValue { get; set; }

        static ParserState()
        {
            Mapper.CreateMap<ParserState, ParserState>();
        }

        public ParserState Clone()
        {
            return Mapper.Map<ParserState>(this);
        }
    }
}
