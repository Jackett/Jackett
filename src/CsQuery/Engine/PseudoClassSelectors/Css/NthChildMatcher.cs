using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods;
using CsQuery.EquationParser;
using CsQuery.HtmlParser;

namespace CsQuery.Engine
{

    /// <summary>
    /// Figure out if an index matches an Nth Child, or return a list of all matching elements from a list.
    /// </summary>
    public class NthChildMatcher
    {
        #region private properties

        /// <summary>
        /// A structure to keep information about what has been calculated so far for a given equation string.
        /// NthChild is expensive so we cache a list of matching element IDs for a given equation along with the 
        /// last index this list represents and the iteration. The next time it's called we can either reference
        /// the list of matches so far, or update it only from the point where we stopped last time.
        /// </summary>
        
        protected class CacheInfo
        {
            /// <summary>
            /// Default constructor.
            /// </summary>

            public CacheInfo()
            {
                MatchingIndices=new HashSet<int>();
            }

            /// <summary>
            /// The equation.
            /// </summary>

            public IEquation<int> Equation;

            /// <summary>
            /// The indices which match the equation. This may be incomplete, as it may only have been calculated up to the number
            /// of children present in the prior use. <seealso cref="CacheInfo.MaxIndex"/>
            /// </summary>

            public HashSet<int> MatchingIndices;

            /// <summary>
            /// The next iterator value, used to resume calculations where it was left off.
            /// </summary>

            public int NextIterator;

            /// <summary>
            /// The maximum target index value calculated so far
            /// </summary>

            public int MaxIndex;
        }

        // A cache of the set of indices matching a particular nth-child equation, 
        // so we don't have to do an expensive calculation if it's used again.
        // We probably need to add a bit of logic to flush this, though it really shouldn't
        // grow very large unless you've got massive documents and tons of different equations.

        private static ConcurrentDictionary<string, CacheInfo> ParsedEquationCache =
            new ConcurrentDictionary<string, CacheInfo>();

        private CacheInfo cacheInfo;
        private int MatchOnlyIndex;      
        private IEquation<int> Equation;
        private bool _IsJustNumber;
        private string _Text;
        private string _OnlyNodeName;
        
        // Count results from the last child instead of the first
        private bool FromLast;

        // Delegates to the functions used for matching

        private Func<int, bool> IndexMatchesImpl;
        private Func<IDomElement, IEnumerable<IDomObject>> GetMatchingChildrenImpl;

        /// <summary>
        /// When true, the current equation is just a number, and the MatchOnlyIndex value should be used directly
        /// </summary>
        protected bool IsJustNumber
        {
            get
            {
                return _IsJustNumber;
            }
            set
            {
                _IsJustNumber = value;
                IndexMatchesImpl = value ?
                    (Func<int,bool>)IndexMatchesNumber :
                    (Func<int, bool>)IndexMatchesFormula;
                GetMatchingChildrenImpl = value ?
                    (Func<IDomElement, IEnumerable<IDomObject>>)GetMatchingChildrenNumber :
                    (Func<IDomElement, IEnumerable<IDomObject>>)GetMatchingChildrenFormula;
            }


        }

        /// <summary>
        /// Only nodes with this name will be included in the count to determine if an index matches the equation
        /// </summary>
        protected string OnlyNodeName
        {
            get
            {
                return _OnlyNodeName;
            }
            set
            {
                _OnlyNodeName = !String.IsNullOrEmpty(value) ?
                    value.ToUpper() :
                    null;
            }

        }

        /// <summary>
        /// The formula for this nth child selector
        /// </summary>
        protected string Text
        {
            get
            {
                return _Text;
            }
            set
            {
                _Text = value;
                ParseEquation(value);
            }
        }

        #endregion

        #region public properties/methods

        /// <summary>
        /// Test if an element is the nth-child matching the output of a formula
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test
        /// </param>
        /// <param name="formula">
        /// The formula.
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// true if nth child of type implementation, false if not.
        /// </returns>

        public bool IsNthChildOfType(IDomElement element, string formula, bool fromLast = false)
        {
            return IndexMatches(IndexOf(element,true, fromLast), formula, fromLast);
        }

        /// <summary>
        /// Test if an element is the nth-child matching the output of a formula
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test
        /// </param>
        /// <param name="formula">
        /// The formula.
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// true if nth child, false if not.
        /// </returns>

        public bool IsNthChild(IDomElement element, string formula, bool fromLast = false)
        {
            return IndexMatches(IndexOf(element, false, fromLast), formula,fromLast);
        }

        /// <summary>
        /// Enumerates nth children of the same type as the parent.
        /// </summary>
        ///
        /// <remarks>
        /// This could be implemented more efficiently, but it's a bit complicated because we need to keep track of n 
        /// for each type of element		 
        /// </remarks>
        /// <param name="element">
        /// The parent element.
        /// </param>
        /// <param name="formula">
        /// The formula for determining n.
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements
        /// </returns>

        public IEnumerable<IDomObject> NthChildsOfType(IDomContainer element, string formula, bool fromLast = false)
        {
            return element.ChildElements
                .Where(item=>IsNthChildOfType(item,formula,fromLast));
        }

        /// <summary>
        /// Enumerates nth children in this collection.
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        /// <param name="formula">
        /// The formula for determining n.
        /// </param>
        /// <param name="fromLast">
        /// When true, count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process nth childs in this collection.
        /// </returns>

        public IEnumerable<IDomObject> NthChilds(IDomContainer element, string formula, bool fromLast = false)
        {
            return GetMatchingChildren(element, formula, null, fromLast);
        }

        /// <summary>
        /// Return the index of obj within its siblings, including only elements with the same node name.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to seek
        /// </param>
        /// <param name="onlyOfSameType">
        /// true to only objects of the same NodeName should be considered
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of obj within its siblings (or its siblings of the same type)
        /// </returns>

        private int IndexOf(IDomElement obj, bool onlyOfSameType, bool fromLast = false)
        {
            // get the index just for this type
            int typeIndex = 0;
            
            var childNodes = obj.ParentNode.ChildNodes;
            int length = childNodes.Count;

            for (int index = 0; index < length; index++)
            {
                var el = NthChildMatcher.GetEffectiveChild(childNodes, index, fromLast);
                if (el.NodeType == NodeType.ELEMENT_NODE)
                {
                    if (!onlyOfSameType || el.NodeNameID == obj.NodeNameID)
                    {
                        if (ReferenceEquals(el, obj))
                        {
                            return typeIndex;
                        }
                        typeIndex++;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Return true if the index matches the formula provided.
        /// </summary>
        ///
        /// <param name="index">
        /// The index to test
        /// </param>
        /// <param name="formula">
        /// The formula
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool IndexMatches(int index, string formula, bool fromLast=false)
        {
            FromLast = fromLast;
            return IndexMatches(index, formula);
        }

        /// <summary>
        /// Return true if the index matches the formula provided.
        /// </summary>
        ///
        /// <param name="index">
        /// The index to test.
        /// </param>
        /// <param name="formula">
        /// The formula.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool IndexMatches(int index, string formula)
        {
            Text = formula;
            return IndexMatchesImpl(index);
                
        }

        /// <summary>
        /// Return each child that matches an index returned by the forumla
        /// </summary>
        ///
        /// <param name="obj">
        /// The parent object.
        /// </param>
        /// <param name="formula">
        /// The formula for determining n.
        /// </param>
        /// <param name="onlyNodeName">
        /// The type of node to match.
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process get matching children in this
        /// collection.
        /// </returns>

        public IEnumerable<IDomObject> GetMatchingChildren(IDomContainer obj, 
            string formula, 
            string onlyNodeName = null, 
            bool fromLast = false)
        {
            OnlyNodeName = onlyNodeName;
            FromLast = fromLast;
            return GetMatchingChildren(obj, formula);

        }

        /// <summary>
        /// Return each child that matches an index returned by the forumla.
        /// </summary>
        ///
        /// <param name="obj">
        /// The parent object.
        /// </param>
        /// <param name="formula">
        /// The formula for determining n.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process get matching children in this
        /// collection.
        /// </returns>

        public IEnumerable<IDomObject> GetMatchingChildren(IDomContainer obj, string formula)
        {
            Text = formula;
            return GetMatchingChildren(obj);
        }

        /// <summary>
        /// Return each child that matches an index returned by the forumla.
        /// </summary>
        ///
        /// <param name="obj">
        /// The parent object.
        /// </param>
        ///
        /// <returns>
        /// Sequence of matching children.
        /// </returns>

        public IEnumerable<IDomObject> GetMatchingChildren(IDomContainer obj)
        {
            if (!obj.HasChildren)
            {
                yield break;
            }
            else if (IsJustNumber)
            {
                IDomElement child = GetNthChild(obj,MatchOnlyIndex);

                if (child != null)
                {
                    yield return child;
                }
                else
                {
                    yield break;
                }
            }
            else
            {
                UpdateCacheInfo(obj.ChildNodes.Count);

                int elementIndex = 1;
                int newActualIndex=-1;

                IDomElement el = GetNextChild(obj, -1, out newActualIndex);
                while (newActualIndex >= 0)
                {
                    if (cacheInfo.MatchingIndices.Contains(elementIndex))
                    {
                        yield return el;
                    }
                    el = GetNextChild(obj, newActualIndex, out newActualIndex);
                    elementIndex++;
                }
            }
        }



        #endregion

        #region public static methods
        
        // These methods are exposed because they are used elsewhere

        /// <summary>
        /// Return the correct child from a list based on an index, and the fromLast setting. That is, if fromLast is
        /// true, just return the child at "index." If not, return the child starting from the end at "index"
        /// </summary>
        ///
        /// <param name="nodeList">
        /// The container to obtain children from
        /// </param>
        /// <param name="index">
        /// The index
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// The effective child.
        /// </returns>

        public static IDomObject GetEffectiveChild(INodeList nodeList, int index, bool fromLast)
        {
            if (fromLast)
            {
                return nodeList[nodeList.Count - index - 1];
            }
            else
            {
                return nodeList[index];
            }
        }

        /// <summary>
        /// Gets the true index based on an effective index. (Misnomer, consider changing, should be
        /// GetActualIndex)
        /// </summary>
        ///
        /// <param name="nodeList">
        /// The container to obtain children from
        /// </param>
        /// <param name="index">
        /// The index
        /// </param>
        /// <param name="fromLast">
        /// Count from the last element instead of the first.
        /// </param>
        ///
        /// <returns>
        /// The actual index.
        /// </returns>

        public static int GetEffectiveIndex(INodeList nodeList, int index, bool fromLast)
        {
            if (fromLast)
            {
                return nodeList.Length - index - 1;
            }
            else
            {
                return index;
            }
        }

        #endregion

        #region private methods

        private IDomElement GetNthChild(IDomContainer parent, int index)
        {
            int newActualIndex;
            int elementIndex = 1;
            IDomElement nthChild = GetNextChild(parent,-1, out newActualIndex);

            while (nthChild != null && elementIndex != index)
            {
                nthChild = GetNextChild(parent, newActualIndex, out newActualIndex);
                elementIndex++;
            }
            return nthChild;
        }

        private IDomElement GetNextChild(IDomContainer parent, int currentIndex, out int newIndex)
        {

            int index = currentIndex;

            var children = parent.ChildNodes;
            int count = children.Count;
            IDomObject effectiveNextChild = null;

            while (++index < count) {
                effectiveNextChild= GetEffectiveChild(children, index);
                if (effectiveNextChild.NodeType == NodeType.ELEMENT_NODE)
                {
                    break;
                }
            }
             

            if (index < count)
            {
                newIndex = index;
                return (IDomElement)effectiveNextChild;
            }
            else
            {
                newIndex = -1;
                return null;
            }
        }

        /// <summary>
        /// Return the correct child from a list based on an index, and the current "FromLast" setting
        /// </summary>
        /// <param name="nodeList"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private IDomObject GetEffectiveChild(INodeList nodeList, int index)
        {
            return GetEffectiveChild(nodeList, index, FromLast);
        }
       

        /// <summary>
        /// Parse the equation text into in IEquation, or obtain from the cache if available
        /// </summary>
        /// <param name="equationText"></param>
        protected void ParseEquation(string equationText)
        {
            CheckForSimpleNumber(equationText);

            if (IsJustNumber)
            {
                return;
            }

            equationText = CheckForEvenOdd(equationText);

            if (!ParsedEquationCache.TryGetValue(equationText, out cacheInfo))
            {
                cacheInfo = new CacheInfo();
                Equation = cacheInfo.Equation = GetEquation(equationText);
                ParsedEquationCache.TryAdd(equationText, cacheInfo);
            }
            else
            {
                Equation = cacheInfo.Equation;
            }          
        }

        /// <summary>
        /// Check if it was just a number passed (not an equation) and assign the correct delegates to matching
        /// </summary>
        /// <param name="equation"></param>
        protected void CheckForSimpleNumber(string equation)
        {
            int matchIndex;
            if (Int32.TryParse(equation, out matchIndex))
            {
                MatchOnlyIndex = matchIndex;
                IsJustNumber = true;
            }
            else
            {
                IsJustNumber = false;
            }
        }

        /// <summary>
        /// Returns a parsed equation from a string, validating that it appears to be a legitimate nth-child equation
        /// </summary>
        /// <param name="equationText"></param>
        /// <returns></returns>
        private IEquation<int> GetEquation(string equationText)
        {
            IEquation<int> equation;
            try
            {
                equation= Equations.CreateEquation<int>(equationText);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(String.Format("The equation {{0}} could not be parsed.", equationText), e);
            }

            IVariable variable;
            try
            {
                variable = equation.Variables.Single();
            }
            catch (InvalidOperationException e)
            {
                throw new ArgumentException(String.Format("The equation {{0}} must contain a single variable 'n'.", equation), e);
            }

            if (variable.Name != "n")
            {
                throw new ArgumentException(String.Format("The equation {{0}} does not have a variable 'n'.", equation));
            }
            return equation;

        }

        /// <summary>
        /// Replaces _Text with the correct equation for "even" and "odd".
        /// </summary>
        ///
        /// <param name="equation">
        /// The equation
        /// </param>
        ///
        /// <returns>
        /// The new equation
        /// </returns>

        protected string CheckForEvenOdd(string equation)
        {
            switch (_Text)
            {
                case "odd":
                    return "2n+1";
                case "even":
                    return "2n";
                default:
                    return equation;
            }
        }

        /// <summary>
        /// Test whether an index matches a hard index passed by the formula. (This is one of two
        /// implementations used via delegate)
        /// </summary>
        ///
        /// <param name="index">
        /// The index to test.
        /// </param>
        ///
        /// <returns>
        /// true if it matches, false if not.
        /// </returns>

        protected bool IndexMatchesNumber(int index)
        {
            return MatchOnlyIndex - 1 == index;
        }

        /// <summary>
        /// Test whether an index matches the calculated (or cached) value of a formula. (This is one of
        /// two implementations used via delegate)
        /// </summary>
        ///
        /// <param name="index">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected bool IndexMatchesFormula(int index)
        {
            var matchIndex = index += 1; // nthchild is 1 based indices
            if (index > cacheInfo.MaxIndex)
            {

                UpdateCacheInfo(matchIndex);
            }
            return cacheInfo.MatchingIndices.Contains(matchIndex);

        }

        /// <summary>
        /// Enumerates each child that matches a hard number passed as a formula (one of two
        /// implementations used via delegate)
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// An sequence of the single matching child, or an empty sequence if none match.
        /// </returns>

        public IEnumerable<IDomObject> GetMatchingChildrenNumber(IDomElement element)
        {
            if (!element.HasChildren)
            {
                yield break;
            }
            else 
            {
                IDomElement child = GetNthChild(element, MatchOnlyIndex);

                if (child != null)
                {
                    yield return child;
                }
                else
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Enumerates the child elements that match a formula (one of two implementations used via
        /// delegate)
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching children.
        /// </returns>

        public IEnumerable<IDomObject> GetMatchingChildrenFormula(IDomElement element)
        {
            if (!element.HasChildren)
            {
                yield break;
            }
            else
            {
                UpdateCacheInfo(element.ChildNodes.Count);

                int elementIndex = 1;
                int newActualIndex = -1;

                IDomElement el = GetNextChild(element, -1, out newActualIndex);
                while (newActualIndex >= 0)
                {
                    if (cacheInfo.MatchingIndices.Contains(elementIndex))
                    {
                        yield return el;
                    }
                    el = GetNextChild(element, newActualIndex, out newActualIndex);
                    elementIndex++;
                }
            }
        }

        /// <summary>
        /// Get the next matching index using the equation and add it to our cached list of equation
        /// results.
        /// </summary>
        ///
        /// <param name="lastIndex">
        /// The last index used
        /// </param>

        protected void UpdateCacheInfo(int lastIndex)
        {

            if (cacheInfo.MaxIndex >= lastIndex)
            {
                return;
            }

            int iterator = cacheInfo.NextIterator;
            int val = -1;

            // because of negative-valued n's, we may need to calculate the entire equation set down to zero
            // so we need to watch if the numbers are descending.

            int lastVal = 999999;

            // the 2nd part of the while expression ensures that we count down to zero for negative values

            while ((val < lastIndex && iterator <= lastIndex+1) ||
                (lastVal > val && val>0  ))
            {
                Equation.SetVariable("n", iterator);
                if (val > 0)
                {
                    lastVal = val;
                }
                val = Equation.Value;
                // negative results are just ignored.
                if (val > 0)
                {
                    cacheInfo.MatchingIndices.Add(val);
                }
                iterator++;
            }
            cacheInfo.MaxIndex = lastIndex;
            cacheInfo.NextIterator = iterator;
        }


        #endregion
    }
}
