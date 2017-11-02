using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.HtmlParser;

namespace CsQuery.Engine
{
    internal class SelectorEngine
    {
        #region constructor
        
        public SelectorEngine(IDomDocument document, Selector selector)
        {
            Document = document;
            Selector = selector;
        }

        #endregion

        #region private properties

        //private static OutputSetComparer outputSetComparer = new OutputSetComparer();
        
        private List<SelectorClause> ActiveSelectors;
        private int activeSelectorId;

        private enum IndexMode
        {
            None = 0,
            Basic  = 1,
            Subselect = 2
        }

        #endregion

        #region public properties
        /// <summary>
        /// The current selection list being acted on
        /// </summary>

        public Selector Selector { get; protected set; }

        /// <summary>
        /// The Document bound to this engine instance
        /// </summary>

        public IDomDocument Document { get; protected set; }

        #endregion

        #region public methods

        /// <summary>
        /// Select implementation. The public method automatically remaps a selector with the knowledge
        /// that the context is external (and not part of a chain)
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are null.
        /// </exception>
        ///
        /// <param name="context">
        /// The context in which the selector applies. If null, the selector is run against the entire
        /// Document. If not, the selector is run against this sequence of elements.
        /// </param>
        ///
        /// <returns>
        /// A list of elements. This method returns a list (rather than a sequence) because the sequence
        /// must be enumerated to ensure that end-users don't cause the selector to be rerun repeatedly,
        /// and that the values are not mutable (e.g. if the underlying source changes).
        /// </returns>

        public IList<IDomObject> Select(IEnumerable<IDomObject> context)
        {
            // this holds the final output

            HashSet<IDomObject> output = new HashSet<IDomObject>();

            if (Selector == null )
            {
                throw new ArgumentNullException("The selector cannot be null.");
            }

            if (Selector.Count == 0)
            {
                return EmptyEnumerable().ToList();
            }

            ActiveSelectors = new List<SelectorClause>(Selector);

            // First just check if we ended up here with an HTML selector; if so, hand it off.
            
            var firstSelector = ActiveSelectors[0];
            if (firstSelector.SelectorType == SelectorType.HTML)
            {
                return CsQuery.Implementation.
                    DomDocument.Create(firstSelector.Html, HtmlParsingMode.Fragment)
                        .ChildNodes
                        .ToList();
            } 

            // this holds any results that carried over from the previous loop for chaining

            IEnumerable<IDomObject> lastResult = null;

            // this is the source from which selections are made in a given iteration; it could be the DOM
            // root, a context, or the previous result set. 
            
            IEnumerable<IDomObject> selectionSource=null;

            // Disable the index if there is no context (e.g. disconnected elements)
            // or if the first element is not indexed, or the context is not from the same document as this
            // selector is bound. Determine which features can be used for this query by casting the index
            // to the known interfaces. 

            
            bool useIndex;
            if (context.IsNullOrEmpty()) {
                useIndex = true;
            } else {
                IDomObject first = context.First();
                useIndex = !first.IsDisconnected && first.IsIndexed && first.Document==Document;
            }

            IDomIndexRanged rangedIndex=null;
            IDomIndexSimple simpleIndex=null;

            if (useIndex)
            {
                rangedIndex = Document.DocumentIndex as IDomIndexRanged;
                simpleIndex = Document.DocumentIndex as IDomIndexSimple;
            }

            for (activeSelectorId = 0; activeSelectorId < ActiveSelectors.Count; activeSelectorId++)
            {

                var selector = ActiveSelectors[activeSelectorId].Clone();

                if (lastResult != null && 
                    (selector.CombinatorType == CombinatorType.Root || selector.CombinatorType == CombinatorType.Context))
                {
                    // we will alter the selector during each iteration to remove the parts that have already been
                    // parsed, so use a copy. This is a selector that was chained with the selector grouping
                    // combinator "," -- we always output the results so far when beginning a new group. 
                    
                    output.AddRange(lastResult);
                    lastResult = null;
                }

                // For "and" combinator types, we want to leave everything as it was -- the results of this
                // selector should compound with the prior. This is not an actual CSS combinator, this is the
                // equivalent of grouping parenthesis. That is, in CSS there's no way to say "(input[submit],
                // button):visible" - that is group the results on selector part and apply a filter to it. But
                // we need to do exactly this for certain selector types (for example the jQuery :button
                // selector). 

                if (selector.CombinatorType != CombinatorType.Grouped)
                {

                    selectionSource = GetSelectionSource(selector, context, lastResult);
                    lastResult = null;
                }
                
                List<ushort> key = new List<ushort>();
                SelectorType removeSelectorType = 0;

                // determine the type of traversal & depth for this selector

                int depth = 0;
                bool descendants = true;
                    
                switch (selector.TraversalType)
                {
                    case TraversalType.Child:
                        depth = selector.ChildDepth;
                        descendants = false;
                        break;
                    case TraversalType.Filter:
                    case TraversalType.Adjacent:
                    case TraversalType.Sibling:
                        depth = 0;
                        descendants = false;
                        break;
                    case TraversalType.Descendent:
                        depth = 1;
                        descendants = true;
                        break;
                    // default: fall through with default values set above.
                }

                bool canUseBasicIndex = (selectionSource == null)
                    && descendants
                    && depth == 0;


                // build index keys when possible for the active index type

                if (rangedIndex != null ||
                    (simpleIndex != null && canUseBasicIndex)
                    && !selector.NoIndex)
                {

                    // We don't want to use the index for "NotEquals" selectors because a missing attribute
                    // is considered a valid match
                    
                    if (selector.SelectorType.HasFlag(SelectorType.AttributeValue)
                        && selector.AttributeSelectorType != AttributeSelectorType.NotExists
                        && selector.AttributeSelectorType != AttributeSelectorType.NotEquals)
                    {
                        key.Add('!');
                        key.Add(HtmlData.Tokenize(selector.AttributeName));

                        // AttributeValue must still be matched manually - so remove this flag only if the
                        // selector is conclusive without further checking
                        
                        if (selector.AttributeSelectorType == AttributeSelectorType.Exists)
                        {
                            removeSelectorType = SelectorType.AttributeValue;
                        }
                    }
                    else if (selector.SelectorType.HasFlag(SelectorType.Tag))
                    {
                        key.Add('+');
                        key.Add(HtmlData.Tokenize(selector.Tag));
                        removeSelectorType=SelectorType.Tag;
                    }
                    else if (selector.SelectorType.HasFlag(SelectorType.ID))
                    {
                        key.Add('#');
                        key.Add(HtmlData.TokenizeCaseSensitive(selector.ID));
                        removeSelectorType=SelectorType.ID;
                    }
                    else if (selector.SelectorType.HasFlag(SelectorType.Class))
                    {
                        key.Add('.');
                        key.Add(HtmlData.TokenizeCaseSensitive(selector.Class));
                        removeSelectorType=SelectorType.Class;
                    }
                }

                // If part of the selector was indexed, key will not be empty. Return initial set from the
                // index. If any selectors remain after this they will be searched the hard way.

                IEnumerable<IDomObject> result = null;

                if (key.Count>0)
                {
                    // This is the main index access point: if we have an index key, we'll get as much as we can from the index.
                    // Anything else will be handled manually.

                  
       
                    if (selectionSource == null)
                    {
                        // we don't need to test for index features at this point; if canUseBasicIndex = false and we
                        // are here, then the prior logic dictates that the ranged index is available. But always use
                        // the simple index if that's all we need because it could be faster. 
                        
                        result = simpleIndex.QueryIndex(key.ToArray());
                    }
                    else
                    {
                        HashSet<IDomObject> elementMatches = new HashSet<IDomObject>();
                        result = elementMatches;
                        
                        foreach (IDomObject obj in selectionSource)
                        {

                            var subKey = key.Concat(HtmlData.indexSeparator).Concat(obj.NodePath).ToArray();
                            
                            var matches = rangedIndex.QueryIndex(subKey,depth, descendants);

                            elementMatches.AddRange(matches);
                        }
                    }

                    selector.SelectorType &= ~removeSelectorType;

                    // Special case for attribute selectors: when Attribute Value attribute selector is present, we
                    // still need to filter for the correct value afterwards. But we need to change the traversal
                    // type because any nodes with the correct attribute type have already been selected. 
                    
                    if (selector.SelectorType.HasFlag(SelectorType.AttributeValue))
                    {
                        selector.TraversalType = TraversalType.Filter;
                    }
                    
                }
             

                // If any selectors were not handled via the index, match them manually
                
                if (selector.SelectorType != 0)
                {
      
                    // if there are no temporary results (b/c there was no indexed selector) then use selection
                    // source instead (e.g. start from the same point that the index would have) 

                    result = GetMatches(result ?? selectionSource ?? Document.ChildElements, selector);
                }
                
                lastResult = lastResult == null ?
                    result : lastResult.Concat(result); 
                
            }

            // After the loop has finished, output any results from the last iteration.

            output.AddRange(lastResult);

            // Return the results as a list so that any user will not cause the selector to be run again

            return output.OrderBy(item => item.NodePath, Implementation.PathKeyComparer.Comparer).ToList();

        }

        #endregion

        #region selection matching main code

        /// <summary>
        /// Get the sequence that is the source for the current clause, based on the selector, prior
        /// results, and context.
        /// </summary>
        ///
        /// <remarks>
        /// Notes from refactoring this on 10/14/2012: At issue is selectors like ":not(.sel1 .sel2,
        /// :first) where the subselector has filters that apply to just the context, versus selectors
        /// like ":has(.sel1 .sel2, :first) where the subselector needs to apply to the results of a
        /// selection against the DOM
        /// 
        /// case1: $('.sel','.context-sel') means that ".sel" is actually applied against .context-sel.
        /// it's like .find.
        /// 
        /// totally different from a subselector -- but the subselector still needs a context to apply
        /// filters, even though the selectors theselves are run against the whole doc.
        /// 
        /// so we need to set up selectors before running against the context so each subselector is IDd
        /// as either "context" or "root" in addition to its traversal type to eliminate ambiguity of
        /// intent. a subselector for :not should have "root+descendant" for the first part and
        /// "context+filter" for the 2nd. For regular context type filters, it should be
        /// "context+descendant" (same as find). FOr complex context/find filters chained with a comma,
        /// the stuff after the comma should also be in context though jquery seems inconsistent with
        /// this.
        /// 
        /// This code here should then use the new info to select the correct sleection source. Think we
        /// should be rid of traversaltype.subselect. Think traversaltype.all should really mean "include
        /// the context items" instead of "Descendant" as it does now.
        /// </remarks>
        ///
        /// <param name="clause">
        /// The current selector clause.
        /// </param>
        /// <param name="context">
        /// The context passed initially to this Select operation.
        /// </param>
        /// <param name="lastResult">
        /// The result of the prior clause. Can be null.
        /// </param>
        ///
        /// <returns>
        /// The sequence that should source the current clause's context.
        /// </returns>

        protected IEnumerable<IDomObject> GetSelectionSource(SelectorClause clause,
            IEnumerable<IDomObject> context, IEnumerable<IDomObject> lastResult)
        {
         
            IEnumerable<IDomObject> selectionSource=null;
            IEnumerable<IDomObject> interimSelectionSource = null;

            if (clause.CombinatorType != CombinatorType.Chained)
            {
                interimSelectionSource = clause.CombinatorType == CombinatorType.Context ?
                    context : null;
            }
            else
            {
                interimSelectionSource = lastResult;
            }

            // If the selector used the adjacent combinator, grab the next element for each
            
            if (interimSelectionSource != null)
            {
                if (clause.TraversalType == TraversalType.Adjacent || clause.TraversalType == TraversalType.Sibling)
                {
                    selectionSource = GetAdjacentOrSiblings(clause.TraversalType, interimSelectionSource);
                    clause.TraversalType = TraversalType.Filter;
                }
                else
                {
                    selectionSource = interimSelectionSource;
                }
            }

            return selectionSource;
        }

        /// <summary>
        /// Return all elements matching a selector, within a list of elements. This function will
        /// traverse children, but it is expected that the source list at the current depth (e.g. from an
        /// Adjacent or Sibling selector) is already processed.
        /// </summary>
        ///
        /// <param name="source">
        /// The sequence of elements to filter.
        /// </param>
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// The sequence of elements matching the selector.
        /// </returns>

        protected IEnumerable<IDomObject> GetMatches(IEnumerable<IDomObject> source, SelectorClause selector)
        {
            // Maintain a hashset of every element already searched. Since result sets frequently contain items which are
            // children of other items in the list, we would end up searching the tree repeatedly
            
            HashSet<IDomObject> uniqueElements = null;

            // The processing stack
            
            Stack<MatchElement> stack = null;

            // The source list for the current iteration

            IEnumerable<IDomObject> curList = source;
            
            // the results obtained so far in this iteration

            HashSet<IDomObject> temporaryResults = new HashSet<IDomObject>();

            // The unique list has to be reset for each sub-selector
            
            uniqueElements = new HashSet<IDomObject>();


            if (selector.SelectorType.HasFlag(SelectorType.Elements))
            {

                var set = GetAllChildOrDescendants(selector.TraversalType, source);

                return set.Intersect(selector.SelectElements);
            }

            // For the jQuery extensions (which are mapped to the position in the output, not the DOM) we have to enumerate
            // the results first, rather than targeting specific child elements. Handle it here,

            else if (selector.SelectorType.HasFlag(SelectorType.PseudoClass))
            {
                if (selector.IsResultListPosition) {
                    return GetResultPositionMatches(curList, selector);
                } 
                
            }
            else if (selector.SelectorType.HasFlag(SelectorType.All))
            {
                return GetAllChildOrDescendants(selector.TraversalType, curList);
            } 

            // Otherwise, try to match each element individually
            
            stack = new Stack<MatchElement>();

            foreach (var obj in curList)
            {
                // We must check everything again when looking for specific depth of children
                // otherwise - no point - skip em
                
                IDomElement el = obj as IDomElement;
                if (el == null || selector.TraversalType != TraversalType.Child && uniqueElements.Contains(el))
                {
                    continue;
                }
                
                stack.Push(new MatchElement(el, 0));
                
                int matchIndex = 0;
                
                while (stack.Count != 0)
                {
                    var current = stack.Pop();

                    if (Matches(selector, current.Element, current.Depth))
                    {
                        temporaryResults.Add(current.Element);
                        matchIndex++;
                    }
                    // Add children to stack (in reverse order, so they are processed in the correct order when popped)

                    // Don't keep going to children if the target depth is < the depth. Though the match would still fail,
                    // stuff would end up the unique list which we might need to test later if it appears directly in the source list
                    // causing it to be ignored.

                    if (selector.TraversalType != TraversalType.Filter &&
                        (selector.TraversalType != TraversalType.Child || selector.ChildDepth > current.Depth))
                    {
                        SelectorType selectorType = selector.SelectorType;
                        IDomElement elm = current.Element;

                        if (selector.IsDomPositionPseudoSelector &&
                            ((selector.TraversalType == TraversalType.All) ||
                            (selector.TraversalType == TraversalType.Child && selector.ChildDepth == current.Depth + 1) ||
                            (selector.TraversalType == TraversalType.Descendent && selector.ChildDepth <= current.Depth + 1))) 
                        {
                            temporaryResults.AddRange(GetPseudoClassMatches(elm, selector));
                            selectorType &= ~SelectorType.PseudoClass;
                        }

                        if (selectorType == 0)
                        {
                            continue;
                        }

                        for (int j = elm.ChildNodes.Count - 1; j >= 0; j--)
                        {
                            IDomElement child = elm[j] as IDomElement;

                            if (child==null || !uniqueElements.Add(child))
                            {
                                continue;
                            }
                            if (child.NodeType == NodeType.ELEMENT_NODE)
                            {
                                stack.Push(new MatchElement(child, current.Depth + 1));
                            }
                        }
                    }
                }
            }

            return temporaryResults;
        }


        /// <summary>
        /// Return true if an object matches a specific selector. If the selector has a desecendant or child traversal type, it must also
        /// match the specificed depth.
        /// </summary>
        /// <param name="selector">The jQuery/CSS selector</param>
        /// <param name="obj">The target object</param>
        /// <param name="depth">The depth at which the target must appear for descendant or child selectors</param>
        /// <returns></returns>
        protected bool Matches(SelectorClause selector, IDomElement obj, int depth)
        {
            switch (selector.TraversalType)
            {
                case TraversalType.Child:
                    if (selector.ChildDepth != depth)
                    {
                        return false;
                    }
                    break;
                case TraversalType.Descendent:
                    // Special case because this code is jacked up: when only "AttributeValue" it's ALWAYS a filter, it means
                    // the AttributeExists was handled previously by the index.

                    // This engine at some point should be reworked so that the "And" combinator is just a subselector, this logic has 
                    // become too brittle.

                    if (depth == 0)
                    {
                        return false;
                    }
                    break;
            }

            if (selector.SelectorType.HasFlag(SelectorType.All))
            {
                return true;
            }

            if (selector.SelectorType.HasFlag(SelectorType.PseudoClass))
            {
                return MatchesPseudoClass(obj, selector);
            }

            if (obj.NodeType != NodeType.ELEMENT_NODE)
            {
                return false;
            }
            
            // Check each selector from easier/more specific to harder. e.g. ID is going to eliminate a lot of things.

            if (selector.SelectorType.HasFlag(SelectorType.ID) &&
                selector.ID != obj.Id)
            {
                return false;
            }
            if (selector.SelectorType.HasFlag(SelectorType.Class) &&
                !obj.HasClass(selector.Class))
            {
                return false;
            }

            if (selector.SelectorType.HasFlag(SelectorType.Tag) &&
                !String.Equals(obj.NodeName, selector.Tag, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            
            if ((selector.SelectorType & SelectorType.AttributeValue)>0)
            {
                return AttributeSelectors.Matches((IDomElement)obj,selector);
            }

            if (selector.SelectorType == SelectorType.None)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return all position-type matches. These are selectors that are keyed to the position within
        /// the selection set itself.
        /// </summary>
        ///
        /// <param name="list">
        /// The list of elements to filter
        /// </param>
        /// <param name="selector">
        /// The selector
        /// </param>
        ///
        /// <returns>
        /// A sequence of elements matching the filter
        /// </returns>


        protected IEnumerable<IDomObject> GetResultPositionMatches(IEnumerable<IDomObject> list, 
            SelectorClause selector)
        {
            // for sibling traversal types the mapping was done already by the Matches function

            var sourceList = GetAllChildOrDescendants(selector.TraversalType, list);

            return ((IPseudoSelectorFilter)selector.PseudoSelector).Filter(sourceList);
           
        }

        
        /// <summary>
        /// Return all child elements matching a DOM-position type selector
        /// </summary>
        /// <param name="elm"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        protected IEnumerable<IDomObject> GetPseudoClassMatches(IDomElement elm, SelectorClause selector)
        {
            IEnumerable<IDomObject> results;
           
            results = ((IPseudoSelectorChild)selector.PseudoSelector).ChildMatches(elm);

            foreach (var item in results)
            {
                yield return item;
            }

            // Traverse children if needed

            if (selector.TraversalType == TraversalType.Descendent || 
                selector.TraversalType == TraversalType.All)
            {
                foreach (var child in elm.ChildElements)
                {
                    foreach (var item in GetPseudoClassMatches(child, selector))
                    {
                        yield return item;
                    }
                }
            }
          
        }    

        /// <summary>
        /// Return true if an element matches a specific filter.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test
        /// </param>
        /// <param name="selector">
        /// A selector clause
        /// </param>
        ///
        /// <returns>
        /// true if matches pseudo class, false if not.matches the selector, false if not
        /// </returns>

        protected bool MatchesPseudoClass(IDomElement element, SelectorClause selector)
        {
            return ((IPseudoSelectorChild)selector.PseudoSelector).Matches(element);       
        }

 
        #endregion

        #region private methods

        private DomIndexFeatures GetFeatures(IDomIndex index)
        {
            return (index is IDomIndexQueue ? DomIndexFeatures.Queue : 0) |
                (index is IDomIndexRanged ? DomIndexFeatures.Range : 0);

        }

        private IEnumerable<IDomObject> EmptyEnumerable()
        {
            yield break;
        }

        /// <summary>
        /// Map a list to its siblings or adjacent elements if needed. Ignore other traversal types.
        /// </summary>
        ///
        /// <param name="traversalType">
        /// The traversal type
        /// </param>
        /// <param name="list">
        /// The source list
        /// </param>
        ///
        /// <returns>
        /// Sequence of adjacent or sibling elements.
        /// </returns>

        protected IEnumerable<IDomObject> GetAdjacentOrSiblings(TraversalType traversalType, IEnumerable<IDomObject> list)
        {
            IEnumerable<IDomObject> sourceList;
            switch (traversalType)
            {
                case TraversalType.Adjacent:
                    sourceList = GetAdjacentElements(list);
                    break;
                case TraversalType.Sibling:
                    sourceList = GetSiblings(list);
                    break;
                default:
                    sourceList = list;
                    break;
            }
            return sourceList;
        }

        protected IEnumerable<IDomObject> GetAllElements(IEnumerable<IDomObject> list)
        {
            foreach (var item in list)
            {
                yield return item;
                foreach (var descendant in GetDescendantElements(item))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Map a list to its children or descendants, if needed.
        /// </summary>
        /// <param name="traversalType"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected IEnumerable<IDomObject> GetAllChildOrDescendants(TraversalType traversalType, IEnumerable<IDomObject> list)
        {
            switch (traversalType)
            {
                case TraversalType.All:
                    return GetAllElements(list);
                case TraversalType.Child:
                    return GetChildElements(list);
                case TraversalType.Descendent:
                    return GetDescendantElements(list);
                default:
                    return list;
            }
        }


        protected IEnumerable<IDomObject> GetTraversalTargetElements(TraversalType traversalType, IEnumerable<IDomObject> list)
        {
            switch (traversalType)
            {
                case TraversalType.Filter:
                    return list;
                case TraversalType.Child:

                    return GetChildElements(list);
                case TraversalType.Adjacent:
                    return GetAdjacentElements(list);
                case TraversalType.Sibling:
                    return GetSiblings(list);
  
                case TraversalType.Descendent:
                    throw new InvalidOperationException("TraversalType.Descendant should not be found at this point.");
                case TraversalType.All:
                    throw new InvalidOperationException("TraversalType.All should not be found at this point.");
                default:
                    throw new NotImplementedException("Unimplemented traversal type.");
            }
        }

        /// <summary>
        /// Return all children of each element in the list
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        protected IEnumerable<IDomElement> GetChildElements(IEnumerable<IDomObject> list)
        {
            foreach (var item in list)
            {
                foreach (var child in item.ChildElements)
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Return all descendants of each element in the list
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<IDomElement> GetDescendantElements(IEnumerable<IDomObject> list)
        {
            foreach (var item in list)
            {
                foreach (var child in GetDescendantElements(item))
                {
                    yield return child;
                }
            }


        }

        public static IEnumerable<IDomElement> GetDescendantElements(IDomObject element)
        {
            foreach (var child in element.ChildElements)
            {
                yield return child;
                foreach (var grandChild in GetDescendantElements(child))
                {
                    yield return grandChild;
                }
            }
            
        }
        protected IEnumerable<IDomElement> GetAdjacentElements(IEnumerable<IDomObject> list)
        {
            return CQ.Map(list, item =>
            {
                return item.NextElementSibling;
            });
        }

        protected IEnumerable<IDomElement> GetSiblings(IEnumerable<IDomObject> list)
        {
            foreach (var item in list)
            {

                IDomContainer parent = item.ParentNode;
                int index = item.Index + 1;
                int length = parent.ChildNodes.Count;

                while (index < length)
                {
                    IDomElement node = parent.ChildNodes[index] as IDomElement;
                    if (node != null)
                    {
                        yield return node;
                    }
                    index++;
                }
            }
        }
        


        #endregion

    }
}
