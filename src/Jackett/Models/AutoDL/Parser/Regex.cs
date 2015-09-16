using CuttingEdge.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Models.AutoDL.Parser
{
    public class Regex : IParserCommand
    {
        System.Text.RegularExpressions.Regex regex;

        public Regex(XElement x)
        {
            var value = x.AttributeString("value");
            Condition.Requires(value).IsNotNull();
            regex = new System.Text.RegularExpressions.Regex(value);
        }

        public bool Execute(ParserState state)
        {
            var matches = regex.Matches(state.CurrentItem);

            state.Logger.Debug($"{state.Tracker} Regex matches: {matches.Count}");
            if (matches.Count == 0)
            {
                return false;
            }

            state.TempVariables = new List<string>();

            foreach(var match in matches.Cast<Match>().Where(m => m.Success))
            {
                foreach(var group in match.Groups.Cast<Group>().Skip(1))
                {
                    state.TempVariables.Add(group.Value);
                }
            }

            return true;
        }
    }
}
