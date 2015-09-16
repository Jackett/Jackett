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
    public class Vars : BaseParserCommand, IParserCommand
    {
        public Vars(XElement x)
        {
            
        }

        public bool Execute(ParserState state)
        {
            state.Logger.Debug($"{state.Tracker} Vars");
            for (int i = 0; i < state.TempVariables.Count; i++)
            {
                if (i >= base.Children.Count)
                {
                    state.Logger.Debug($"{state.Tracker} Vars more variables than children!");
                }
                else
                {
                    var originalItem = state.CurrentItem;
                    state.CurrentValue = state.TempVariables[i];
                    base.Children[i].Execute(state);
                }
            }

            return true;
        }
    }
}
