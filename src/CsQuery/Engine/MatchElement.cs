using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
     ///<summary>
     /// Wrapper class used by the engine to store info on the selector stack.
     ///</summary>
    internal class MatchElement
    {
        public MatchElement(IDomElement  element)
        {
            Initialize(element, 0);
        }
        public MatchElement(IDomElement element, int depth)
        {
            Initialize(element, depth);
        }
        protected void Initialize(IDomElement element, int depth)
        {
            Depth = depth;
            Element = element;
        }
        public int Depth { get; protected set; }
        public IDomElement Element { get; protected set; }
    }

}
