using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public class LinePatterns : BaseParserCommand
    {
        public List<ParserState> Execute(ParserState state)
        {
            var matchedStates = new List<ParserState>();
            foreach(var action in base.Children)
            {
                var extract = action as Extract;
                if (extract != null)
                {
                    var subState = state.Clone();
                    if (extract.Execute(subState))
                    {
                        matchedStates.Add(subState);
                    }
                }
            }

            return matchedStates;
        }
    }
}
