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
    public class SetVarIf :IParserCommand
    {
        string varname;
        string value;
        System.Text.RegularExpressions.Regex regex;
        string newValue;


        public SetVarIf(XElement x)
        {
            varname = x.AttributeString("varName");
            Condition.Requires(varname).IsNotNullOrWhiteSpace();

            var regexStr = x.AttributeString("regex");

            if (regexStr != null)
            {
                regex = new System.Text.RegularExpressions.Regex(regexStr);
            }
            else
            {
                value = x.AttributeString("value");
                newValue = x.AttributeString("newValue");
            }
        }

        public bool Execute(ParserState state)
        {
            if (regex == null)
            {
                var ok = string.Equals(state.Variables[varname], value, StringComparison.InvariantCultureIgnoreCase);
                if (ok)
                {
                    state.Variables[varname] = newValue;
                }

                state.Logger.Debug($"{state.Tracker} SetVarIf eq ok {ok} var {varname} = {newValue}.");
            }
            else
            {
                var match = regex.Match(state.Variables[varname]);
                if (match.Success)
                    state.Variables[varname] = match.Groups[0].Value;

                state.Logger.Debug($"{state.Tracker} SetVarIf regex ok {match.Success} var {varname} = {match.Value}.");
            }

            return true;
        }
    }
}
