using CuttingEdge.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Jackett.Models.AutoDL.Parser
{
    class VarEnc : BaseParserCommand, IParserCommand
    {
        string name;

        public VarEnc(XElement x)
        {
            name = x.AttributeString("name");
            Condition.Requires(name).IsNotNullOrWhiteSpace();
        }

        public bool Execute(ParserState state)
        {
            state.CurrentValue = HttpUtility.UrlEncode(state.Variables[name]);
            state.Logger.Debug($"{state.Tracker} VarEnc returning {state.CurrentValue}.");
            return true;
        }
    }
}
