using CuttingEdge.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Models.AutoDL.Parser
{
    public class ExtractTags : BaseParserCommand, IParserCommand
    {
        string src;
        string splitItem;

        public ExtractTags(XElement x)
        {
            src = x.AttributeString("srcvar");
            Condition.Requires(src).IsNotNullOrWhiteSpace();
            splitItem = x.AttributeString("split");
            Condition.Requires(splitItem).IsNotNullOrEmpty();
        }

        public bool Execute(ParserState state)
        {
            var srcValue = state.Variables[src];
            if (srcValue == null)
            {
                state.Logger.Debug($"{state.Tracker} ExtractTags failed processing null {srcValue}.");
                return true;
            }

            var split = srcValue.Split(splitItem.ToCharArray());
            state.Logger.Debug($"{state.Tracker} ExtractTags split to {split.Length}.");
            for(int i = 0; i < split.Length; i++)
            {
                if (i <= base.Children.Count)
                {
                    state.CurrentValue = split[i];
                    if (!base.Children[i].Execute(state))
                        return false;
                }
            }

            state.Logger.Debug($"{state.Tracker} VarEnc returning {state.CurrentValue}.");
            return true;
        }
    }
}
