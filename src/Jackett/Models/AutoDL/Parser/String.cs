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
    class String : BaseParserCommand, IParserCommand
    {
        string name;
        string value;

        public String(XElement x)
        {
            name = x.AttributeString("name");
            value = x.AttributeString("value");
            //Condition.Requires(name).IsNotNullOrWhiteSpace();
        }

        public bool Execute(ParserState state)
        {
            if (string.IsNullOrEmpty(name))
            {
                state.CurrentValue = value;
                state.Logger.Debug($"{state.Tracker} String returning value {value}.");
            }
            else
            {
                state.CurrentValue = state.Variables[name];
                state.Logger.Debug($"{state.Tracker} String returning {state.CurrentValue}.");
            }
            return true;
        }
    }
}
