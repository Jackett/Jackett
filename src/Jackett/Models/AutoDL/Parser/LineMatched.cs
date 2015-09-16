using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public class LineMatched : BaseParserCommand, IParserCommand
    {
        public bool Execute(ParserState state)
        {
            foreach (var action in base.Children)
            {
                if (!action.Execute(state))
                    return false;
            }

            return true;
        }
    }
}
