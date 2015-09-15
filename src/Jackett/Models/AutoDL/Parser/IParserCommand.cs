using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public interface IParserCommand
    {
        bool Execute(ParserState state);
    }
}
