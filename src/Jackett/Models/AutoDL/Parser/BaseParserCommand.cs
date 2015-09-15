using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL.Parser
{
    public abstract class BaseParserCommand
    {
        public List<IParserCommand> Children { get; set; } = new List<IParserCommand>();
    }
}
