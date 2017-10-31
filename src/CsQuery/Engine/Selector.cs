using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;


namespace CsQuery.Engine
{
    /// <summary>
    /// A parsed selector, consisting of one or more SelectorClauses.
    /// </summary>

    public class Selector : IEnumerable<SelectorClause>
    {
        #region constructors

        /// <summary>
        /// Creates an empty selector
        /// </summary>

        public Selector()
        {

        }

        /// <summary>
        /// Create a new selector from a single selector clause
        /// </summary>
        ///
        /// <param name="clause">
        /// The clause
        /// </param>

        public Selector(SelectorClause clause)
        {
            Clauses.Add(clause);
        }

        /// <summary>
        /// Create a new selector from a sequence of selector clauses.
        /// </summary>
        ///
        /// <param name="clauses">
        /// A sequence of clauses to build this selector
        /// </param>

        public Selector(IEnumerable<SelectorClause> clauses)
        {
            Clauses.AddRange(clauses);
        }

        /// <summary>
        /// Create a new selector from any string.
        /// </summary>
        ///
        /// <param name="selector">
        /// The CSS selector string, or a string of HTML.
        /// </param>

        public Selector(string selector)   
        {
            var parser = new SelectorParser();
            Clauses.AddRange(parser.Parse(selector));
        }

        /// <summary>
        /// Create a new selector from DOM elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of elements.
        /// </param>

        public Selector(IEnumerable<IDomObject> elements ) {

            SelectorClause sel = new SelectorClause();
            sel.SelectorType = SelectorType.Elements;
            sel.SelectElements = elements;
            Clauses.Add(sel);
        }

        /// <summary>
        /// Create a new selector from a single element.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>

        public Selector(IDomObject element)
        {

            SelectorClause sel = new SelectorClause();
            sel.SelectorType = SelectorType.Elements;
            sel.SelectElements = new List<IDomObject>();
            ((List<IDomObject>)sel.SelectElements).Add(element);
            Clauses.Add(sel);
        }

        #endregion

        #region public properties

        /// <summary>
        /// The number of clauses in this selector
        /// </summary>

        public int Count
        {
            get
            {
                return Clauses.Count;
            }
        }

        /// <summary>
        /// Indexer to get clauses of this selector by index.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the entry to access.
        /// </param>
        ///
        /// <returns>
        /// The selector clause at the index specified
        /// </returns>

        public SelectorClause this[int index]
        {
            get
            {
                return Clauses[index];
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is an HTML selector (e.g. it's not really a
        /// selector, but should return a new HTML fragment).
        /// </summary>

        public bool IsHtml
        {
            get
            {
                return Count == 1 && Clauses[0].SelectorType == SelectorType.HTML;
            }
        }
        #endregion

        #region public methods

        /// <summary>
        /// Adds a clause to this selector.
        /// </summary>
        ///
        /// <param name="clause">
        /// The clause to insert.
        /// </param>

        public void Add(SelectorClause clause)
        {

            // TODO: We'd like to prevent duplicate clauses, but in order to do so, they need to be combined into 
            // complete selectors (e.g. sets bounded by CombinatorType.Root). That really should be the definition of a 
            // selector, e.g. each part separated by a comma

            //if (clause.CombinatorType == CombinatorType.Root && Clauses.Contains(clause))
            //{
            //    return;
            //}
            Clauses.Add(clause);

        }

        /// <summary>
        /// Convert this selector to a context filter, meaning any open :filter type selectors will be
        /// applied against the context instead of the root. This differs from a Context selector in that
        /// non-filter selectors are still run against the document root, whereas in a Context selector,
        /// they are run against the context itself. This type is used for filters and "Is" and "Not",
        /// the Context type is used for "Find" and objects created with context.
        /// </summary>
        ///
        /// <returns>
        /// The context.
        /// </returns>

        public Selector ToFilterSelector()
        {
            var filter = Clone();
            // convert :filters to map to the context
            foreach (var sel in filter.Clauses)
            {

                if (sel.CombinatorType == CombinatorType.Root &&
                    sel.SelectorType == SelectorType.PseudoClass)
                {
                    sel.TraversalType = TraversalType.Filter;
                    sel.CombinatorType = CombinatorType.Context;
                }
            }

            return filter;
        }

        /// <summary>
        /// Convert this selector to apply the context only: changes Root selectors to be applied to
        /// Context+Descendant traversal type. This is used to create selectors for use with "Find"
        /// </summary>
        ///
        /// <returns>
        /// A new selector.
        /// </returns>

        public Selector ToContextSelector()
        {
            var filter = Clone();
            
            foreach (var sel in filter.Clauses)
            {

                if (sel.CombinatorType == CombinatorType.Root)
                {
                    sel.CombinatorType = CombinatorType.Context;
                    if (sel.TraversalType == TraversalType.All)
                    {
                        sel.TraversalType = TraversalType.Descendent;
                    }

                }
            }

            return filter;
        }

     
        #endregion

        #region private properties

        private List<SelectorClause> _Clauses;

        /// <summary>
        /// Gets a new selection engine for this selector
        /// </summary>
        ///
        /// <param name="document">
        /// The document that's the root for the selector engine
        /// </param>
        ///
        /// <returns>
        /// The new engine.
        /// </returns>

        private SelectorEngine GetEngine(IDomDocument document)
        {
            
            var engine = new SelectorEngine(document,this);
            return engine;
        }

        /// <summary>
        /// Gets a list of clauses in this selector
        /// </summary>

        protected List<SelectorClause> Clauses
        {
            get
            {
                if (_Clauses == null)
                {
                    _Clauses = new List<SelectorClause>();
                }
                return _Clauses;
            }
        } 

        /// <summary>
        /// Gets a clone of the list of member clauses in this selector
        /// </summary>

        protected IEnumerable<SelectorClause> ClausesClone
        {
            get
            {
                if (Count > 0)
                {
                    foreach (var clause in Clauses)
                    {
                        yield return clause.Clone();
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Insert a selector clause at the specified position.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown if the selector is not valid to insert at this position.
        /// </exception>
        ///
        /// <param name="index">
        /// The position in the selector chain to insert this clause
        /// </param>
        /// <param name="clause">
        /// The clause to insert
        /// </param>
        /// <param name="combinatorType">
        /// (optional) type of the combinator.
        /// </param>

        public void Insert(int index, SelectorClause clause, CombinatorType combinatorType = CombinatorType.Chained)
        {
            if (combinatorType == CombinatorType.Root && Clauses.Count!=0) {
                throw new ArgumentException("Combinator type can only be root if there are no other selectors.");
            }

            if (Clauses.Count > 0 && index == 0)
            {
                Clauses[0].CombinatorType = combinatorType;
                clause.CombinatorType = CombinatorType.Root;
                clause.TraversalType = TraversalType.All;

            }
            Clauses.Insert(index, clause);

        }

        /// <summary>
        /// Return the elements of document that match this selector
        /// </summary>
        ///
        /// <param name="document">
        /// The document against which to select
        /// </param>
        ///
        /// <returns>
        /// The sequence of matching elements
        /// </returns>

        public IList<IDomObject> Select(IDomDocument document)
        {
            return Select(document, (IEnumerable<IDomObject>)null);
        }

        /// <summary>
        /// Return the elements of document that match this selector within a context. 
        /// </summary>
        ///
        /// <param name="document">
        /// The document against which to select.
        /// </param>
        /// <param name="context">
        /// The context to select against. Context should be contained within document.
        /// </param>
        ///
        /// <returns>
        /// The sequence of matching elements.
        /// </returns>

        public IList<IDomObject> Select(IDomDocument document, IDomObject context)
        {
            return Select(document, Objects.Enumerate(context));
        }

        /// <summary>
        /// Return the elements of document that match this selector within a context.
        /// </summary>
        ///
        /// <param name="document">
        /// The document against which to select.
        /// </param>
        /// <param name="context">
        /// The context to select against. Context should be contained within document.
        /// </param>
        ///
        /// <returns>
        /// The sequence of matching elements.
        /// </returns>

        public IList<IDomObject> Select(IDomDocument document, IEnumerable<IDomObject> context)
        {
            
            return GetEngine(document).Select(context);
        }

        /// <summary>
        /// Return only elements of sequence that match this selector.
        /// </summary>
        ///
        /// <param name="document">
        /// The DOM to which the members of the sequence belong.
        /// </param>
        /// <param name="sequence">
        /// The sequence to filter. 
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements, which is a subset of the original sequence.
        /// </returns>

        public IEnumerable<IDomObject> Filter(IDomDocument document, IEnumerable<IDomObject> sequence)
        {
            // This needs to be two steps - returning the selection set directly will cause the sequence
            // to be ordered in DOM order, and not its original order.

            HashSet<IDomObject> matches = new HashSet<IDomObject>(ToFilterSelector().Select(document, sequence));
            
            foreach (var item in sequence) {
                if (matches.Contains(item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Test if a single element matches this selector.
        /// </summary>
        ///
        /// <param name="document">
        /// The document context
        /// </param>
        /// <param name="element">
        /// The element to test
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Matches(IDomDocument document, IDomObject element)
        {
            return ToFilterSelector().Select(document, element).Any();
        }

        /// <summary>
        /// Return only elements from the sequence that do not match this selector.
        /// </summary>
        ///
        /// <param name="document">
        /// The document context.
        /// </param>
        /// <param name="sequence">
        /// The source sequence.
        /// </param>
        ///
        /// <returns>
        /// The elements from the source sequence that do not match this selector.
        /// </returns>
        
        public IEnumerable<IDomObject> Except(IDomDocument document, IEnumerable<IDomObject> sequence)
        {
            return sequence.Except(Select(document));
            //HashSet<IDomObject> matches = new HashSet<IDomObject>(GetFilterSelector().Select(document, sequence));

            //foreach (var item in sequence)
            //{
            //    if (!matches.Contains(item))
            //    {
            //        yield return item;
            //    }
            //}
        }

       
        /// <summary>
        /// Return a clone of this selector.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public Selector Clone()
        {
            Selector clone = new Selector(ClausesClone);
            return clone;
        }

        /// <summary>
        /// Returns CSS selector string of this Selector. This may not exactly match the input clause since
        /// it has been regenerated.
        /// </summary>
        ///
        /// <returns>
        /// A CSS selector.
        /// </returns>

        public override string ToString()
        {
            string output = "";
            bool first=true;
            foreach (var selector in this)
            {
                if (!first) {
                    if (selector.CombinatorType == CombinatorType.Root)
                    {
                        output += ",";
                    }
                    else if (selector.CombinatorType == CombinatorType.Grouped)
                    {
                        output += "&";
                    }
                }
                output+=selector.ToString();
                first = false;
            }
            return output;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        ///
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object" />.
        /// </returns>

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object" /> is equal to the current
        /// <see cref="T:System.Object" />.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        ///
        /// <returns>
        /// true if the specified <see cref="T:System.Object" /> is equal to the current
        /// <see cref="T:System.Object" />; otherwise, false.
        /// </returns>

        public override bool Equals(object obj)
        {
            return ToString().Equals(obj);
        }

        #endregion

        #region interface members

        /// <summary>
        /// An enumerator to iterate over each clause in this selector
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<SelectorClause> GetEnumerator()
        {
            return Clauses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Clauses.GetEnumerator();
        }

        #endregion
    }
}
