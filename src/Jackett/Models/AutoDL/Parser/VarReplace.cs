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
    public class VarReplace : IParserCommand
    {
        string name;
        string srcvar;
        System.Text.RegularExpressions.Regex regex;
        string replace;

        public VarReplace(XElement x)
        {
            name = x.AttributeString("name");
            srcvar = x.AttributeString("srcvar");
            regex = new System.Text.RegularExpressions.Regex(x.AttributeString("regex"));
            replace = x.AttributeString("replace");

            Condition.Requires(name).IsNotNullOrWhiteSpace();
            Condition.Requires(srcvar).IsNotNullOrWhiteSpace();
            Condition.Requires(regex).IsNotNull();
            Condition.Requires(replace).IsNotNull();
        }

        public bool Execute(ParserState state)
        {
            var src = state.Variables[srcvar];
            if (src == null)
            {
                state.Logger.Error($"{state.Tracker} VarReplace encountered null src '{srcvar}'");
                return false;
            }

            state.Variables[name] = regex.Replace(src, replace);
            state.Logger.Debug($"{state.Tracker} VarReplace from '{srcvar}' to '{name}' = '{src}' to '{state.Variables[name]}'");
            return true;
        }
    }
}
