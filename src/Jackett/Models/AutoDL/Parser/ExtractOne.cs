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
    public class ExtractOne : BaseParserCommand, IParserCommand
    {
        public ExtractOne(XElement x)
        {
        }

        public bool Execute(ParserState state)
        {
            state.Logger.Debug($"{state.Tracker} ExtractOne has {base.Children.Count} child actions.");
            foreach(var item in base.Children)
            {
               if(item.Execute(state))
                    return true;
            }
            return true;
        }
    }
}
