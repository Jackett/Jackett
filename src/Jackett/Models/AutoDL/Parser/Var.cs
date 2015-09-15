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
    public class Var : BaseParserCommand, IParserCommand
    {
        string name;

        public Var(XElement x)
        {
            name = x.AttributeString("name");
            Condition.Requires(name).IsNotNullOrWhiteSpace();
        }

        public bool Execute(ParserState state)
        {
            if (state.TempVariables.Count == 0)
            {
                state.Logger.Debug($"{state.Tracker} Var setting: {name} to {state.CurrentItem}");
                state.Variables[name] = state.CurrentItem;
                return true;
            }
            else if (base.Children.Count > 0)
            {
                // Build value from other actions
                var builder = new StringBuilder();

                for(var i=0;i<base.Children.Count;i++)
                {
                    var action = base.Children[i];
                    if (!action.Execute(state))
                    {
                        state.Logger.Debug($"{state.Tracker} Var sub {i} action failed.");
                        return false;
                    }
                    else
                    {
                        builder.Append(state.CurrentValue);
                    }
                }

                var value = builder.ToString();
                state.Logger.Debug($"{state.Tracker} Var composite setting {name} to {value}.");
                state.Variables[name] = value;
                return true;
            } else
            {
                // Write variable
                state.CurrentValue = state.Variables[name];
                state.Logger.Debug($"{state.Tracker} Var returning {state.Variables[name]} from {name}.");
                return true;
            }
        }
    }
}
