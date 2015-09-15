using CuttingEdge.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Models.AutoDL.Parser
{
    public class Http : BaseParserCommand, IParserCommand
    {
        string name;

        public Http(XElement x)
        {
            name = x.AttributeString("name");
            Condition.Requires(name).IsNotNullOrWhiteSpace();
        }

        public bool Execute(ParserState state)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < base.Children.Count; i++)
            {
                var action = base.Children[i];
                var subState = state.Clone();
                subState.CurrentItem = string.Empty;
                if (!action.Execute(subState))
                {
                    state.Logger.Debug($"{state.Tracker} Http sub {i} action failed.");
                    return false;
                }
                else
                {
                    builder.Append(state.CurrentItem);
                }
            }

            var value = builder.ToString();
            state.Logger.Debug($"{state.Tracker} Http setting {name} to {value}.");
            state.HTTPHeaders[name] = value;
            return true;
        }
    }
}
