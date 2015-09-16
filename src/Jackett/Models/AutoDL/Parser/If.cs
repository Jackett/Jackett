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
    public class If : BaseParserCommand, IParserCommand
    {
        string srcVar;
        System.Text.RegularExpressions.Regex regex;


        public If(XElement x)
        {
            srcVar = x.AttributeString("srcvar");
            var regexStr = x.AttributeString("regex");
            Condition.Requires(srcVar).IsNotNullOrWhiteSpace();
            Condition.Requires(regexStr).IsNotNullOrWhiteSpace();
            regex = new System.Text.RegularExpressions.Regex(regexStr);
        }

        public bool Execute(ParserState state)
        {
            if (regex.Match(state.Variables[srcVar]).Success)
            {
                foreach(var subAction in this.Children)
                {
                    if (!subAction.Execute(state))
                        return false;
                }

                return true;
            }
            else
            {
                return true;
            }
        }
    }
}
