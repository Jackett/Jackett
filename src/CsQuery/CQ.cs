using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.IO;
using CsQuery.ExtensionMethods;
using CsQuery.HtmlParser;
using CsQuery.StringScanner;
using CsQuery.Implementation;
using CsQuery.Engine;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;

namespace CsQuery
{
    /// <summary>
    /// The CQ object is analogus to the basic jQuery object. It has instance methods that mirror the
    /// methods of a jQuery object, and static methods that mirror utility methods such as "$.map".
    /// 
    /// Most methods return a new jQuery object that is bound to the same document, but a different
    /// selection set. In a web browser, you genally only have a single context (the browser DOM).
    /// Here, you could have many, though most of the time you will only be working with one.
    /// </summary>
    ///
    /// <remarks>
    /// Document is an IDomDocument object, referred to sometimes as the "DOM", and represents the
    /// DOM that this CsQuery objects applies to. When CQ methods are run, the resulting CQ object
    /// will refer to the same Document as the original. Selectors always run against this DOM.
    /// 
    /// Creating a CQ object from something that is not bound to a DOM (such as an HTML string, or an
    /// unbound IDomObject or IDomElement object) will result in a new Document being created, that
    /// is unrelated to any other active objects you may have. Adding unbound elements using methods
    /// such as Append will cause them to become part of the target DOM. They will be removed from
    /// whatever DOM they previously belonged to. (Elements cannot be part of more than one DOM). If
    /// you don't want to remove something while adding to a CQ object from a different DOM, then you
    /// should clone the elements.
    /// 
    /// Selection is a set of DOM nodes matching the selector.
    /// 
    /// Elements is a set of IDomElement nodes matching the selector. This is a subset of Selection -
    /// it excludes non-Element nodes.
    /// 
    /// The static Create() methods create new DOMs. To create a CsQuery object based on an existing
    /// dom, use new CQ() (similar to jQuery() methods).
    /// </remarks>
    ///
    /// <implementation>
    /// Most of the jQuery methods are implemented in separate files under the "CQ_jQuery" folder. 
    /// Methods which are not part of the jQuery API are found under the "CQ_CsQuery" folder.
    /// </implementation>

    public partial class CQ : IEnumerable<IDomObject>
    {

        #region private properties

        private Selector _Selector;
        private IDomDocument _Document;

        #endregion

        #region public properties

        /// <summary>
        /// The number of elements in the CQ object.
        /// </summary>
        ///
        /// <url>
        /// http://api.jquery.com/length/
        /// </url>

        public int Length
        {
            get
            {
                return SelectionSet.Count;
            }
        }

        /// <summary>
        /// Represents the full, parsed DOM for an object created with an HTML parameter. The Document is
        /// the equivalent of the "document" in a browser. The Document node for a complete HTML document
        /// should have only two children, the DocType node and the HTML node.
        /// </summary>
        ///
        /// <value>
        /// Returns the Document for this CQ object. This can also be an IDomFragment type, which is a
        /// derived type of IDomDocument. This is mostly a useful distinction to determine
        /// programatically how the CQ object was created and whether it's intended to represent a
        /// complete HTML document, or only a partial fragment.
        /// </value>

        public IDomDocument Document
        {
            get
            {
                if (_Document == null)
                {
                    CreateNewFragment();
                }
                return _Document;
            }
            protected set
            {
                _Document = value;
            }
        }

        /// <summary>
        /// The selector (parsed) used to create this instance.
        /// </summary>
        ///
        /// <remarks>
        /// This is not guaranteed to have useful data, since CQ objects can be created indirectly and
        /// not represent a selector. If this object was created directly from a selector, this will
        /// contain the Selector object. The ToString() overload will show how the selector was parsed.
        /// </remarks>

        public Selector Selector
        {
            get
            {
                return _Selector;
            }
            protected set
            {
                _Selector = value;
            }
        }

        /// <summary>
        /// The entire selection set as a sequence of elements. This is the default enumerator for a CQ
        /// object as well.
        /// </summary>

        public IEnumerable<IDomObject> Selection
        {
            get
            {
                return SelectionSet;
            }
        }

        /// <summary>
        /// Returns only IDomElement objects from the current selection.
        /// </summary>

        public IEnumerable<IDomElement> Elements
        {
            get
            {
                return OnlyElements(SelectionSet);
            }
        }

        /// <summary>
        /// Gets or sets the order in which the selection set is returned. Usually, this is the order
        /// that elements appear in the DOM. Some operations could result in a selection set that's in an
        /// arbitrary order, though.
        /// </summary>

        public SelectionSetOrder Order
        {
            get
            {
                return SelectionSet.OutputOrder;
            }
            set
            {
                SelectionSet.OutputOrder= value;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Returns the HTML of each selected element in order. <see cref="CQ.SelectionHtml()"/>
        /// </summary>
        ///
        /// <returns>
        /// A string of HTML
        /// </returns>

        public override string ToString()
        {
            return SelectionHtml();
        }

        #endregion

        #region interface members

        /// <summary>
        /// Returns an enumeration of the current selection set for this CQ object
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<IDomObject> GetEnumerator()
        {
            return SelectionSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return SelectionSet.GetEnumerator();
        }

        #endregion

        #region private properties

        private CQ _CsQueryParent;
        private SelectionSet<IDomObject> _Selection;

        /// <summary>
        /// The object from which this CsQuery was created.
        /// </summary>

        protected CQ CsQueryParent
        {
            get
            {

                {
                    return _CsQueryParent;
                }
            }
            set
            {
                _CsQueryParent = value;
                
                // only rebind Document when it's missing; e.g. upon creating a new document.
                
                if (value != null && _Document==null)
                {
                    Document = value.Document;
                }

                //ClearSelections();
            }
        }

        /// <summary>
        /// The current selection set including all node types.
        /// </summary>

        protected SelectionSet<IDomObject> SelectionSet
        {
            get
            {
                if (_Selection == null)
                {
                    _Selection = new SelectionSet<IDomObject>(SelectionSetOrder.OrderAdded);
                }
                return _Selection;
            }
            set
            {
                _Selection = value;
            }
        }

        #endregion

        #region private methods


        /// <summary>
        /// Clear the entire object.
        /// </summary>

        protected void Clear()
        {
            CsQueryParent = null;
            Document = null;
            ClearSelections();
        }

        /// <summary>
        /// Clears the current selection set.
        /// </summary>

        protected void ClearSelections()
        {
            SelectionSet.Clear();
        }

        /// <summary>
        /// Sets the selection set for this object, and asserts that the order in which it as assigned is
        /// the order passed. This allows most operations to return the original set directly; if it is
        /// requested in a different order then it will be sorted.
        /// </summary>
        ///
        /// <param name="selectionSet">
        /// The current selection set including all node types.
        /// </param>
        /// <param name="inputOrder">
        /// The order in which the elements appear in selectionSet. If omitted, Ascending is the default.
        /// </param>
        /// <param name="outputOrder">
        /// The default output order, if different from the inputOrder. If omitted, the same as the input
        /// order is the default.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        protected CQ SetSelection(IEnumerable<IDomObject> selectionSet, 
            SelectionSetOrder inputOrder = SelectionSetOrder.Ascending, 
            SelectionSetOrder outputOrder=0)
        {
            SelectionSet = new SelectionSet<IDomObject>(selectionSet, inputOrder, outputOrder);
            return this;
        }

        /// <summary>
        /// Sets the selection set for this object to a single element..
        /// </summary>
        ///
        /// <param name="element">
        /// The element to add.
        /// </param>
        /// <param name="outputOrder">
        /// The default output order. If omitted, Ascending (DOM) order is the default.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        protected CQ SetSelection(IDomObject element,
            SelectionSetOrder outputOrder = SelectionSetOrder.Ascending)
        {
            SelectionSet = new SelectionSet<IDomObject>(Objects.Enumerate(element), outputOrder,outputOrder);
            return this;
        }


        /// <summary>
        /// Map a CSV or enumerable object to a hashset.
        /// </summary>
        ///
        /// <param name="value">
        /// the object or sequence to map
        /// </param>
        ///
        /// <returns>
        /// A new hashset
        /// </returns>

        protected HashSet<string> MapMultipleValues(object value)
        {
            var values = new HashSet<string>();
            if (value is string)
            {
                values.AddRange(value.ToString().Split(','));

            }
            if (value is IEnumerable)
            {
                foreach (object obj in (IEnumerable)value)
                {
                    values.Add(obj.ToString());
                }
            }

            if (values.Count == 0)
            {
                if (value != null)
                {
                    values.Add(value.ToString());
                }
            }
            return values;

        }

        /// <summary>
        /// Helper function for option groups to set multiple options when passed a CSV of values.
        /// </summary>
        ///
        /// <param name="elements">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>
        /// <param name="multiple">
        /// true to multiple.
        /// </param>

        protected void SetOptionSelected(IEnumerable<IDomElement> elements, object value, bool multiple)
        {
            HashSet<string> values = MapMultipleValues(value);
            SetOptionSelected(elements, values, multiple);
        }

        /// <summary>
        /// Helper function for option groups to set multiple options when passed a CSV of values.
        /// </summary>
        ///
        /// <param name="elements">
        /// .
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <param name="multiple">
        /// true to multiple.
        /// </param>

        protected void SetOptionSelected(IEnumerable<IDomElement> elements, HashSet<string> values, bool multiple)
        {
            bool setOne = false;
            string attribute;

            foreach (IDomElement e in elements)
            {
                attribute = String.Empty;
                switch (e.NodeNameID)
                {
                    case HtmlData.tagOPTION:
                        attribute = "selected";
                        break;
                    case HtmlData.tagINPUT:
                        switch (e["type"])
                        {
                            case "checkbox":
                            case "radio":
                                attribute = "checked";
                                break;
                        }
                        break;
                    case HtmlData.tagOPTGROUP:
                        SetOptionSelected(e.ChildElements, values, multiple);
                        break;
                }
                if (attribute != String.Empty && !setOne && values.Contains(e["value"]))
                {
                    e.SetAttribute(attribute);
                    if (!multiple)
                    {
                        setOne = true;
                    }
                }
                else
                {
                    e.RemoveAttribute(attribute);
                }

            }
        }


        /// <summary>
        /// Add an item to the list of selected elements. It should be part of this DOM.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to add
        /// </param>
        ///
        /// <returns>
        /// true if the element was added.
        /// </returns>

        protected bool AddSelection(IDomObject element)
        {
            //if (!ReferenceEquals(element.Dom, Dom))
            //{
            //    throw new InvalidOperationException("Cannot add unbound elements or elements bound to another DOM directly to a selection set.");
            //}
            return SelectionSet.Add(element);
        }

        /// <summary>
        /// Adds each element to the current selection set. 
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to add
        /// </param>
        ///
        /// <returns>
        /// true if any elements were added.
        /// </returns>

        protected bool AddSelection(IEnumerable<IDomObject> elements)
        {
            bool result = false; 
            foreach (IDomObject elm in elements)
            {
                result = true;
                AddSelection(elm);
            }
            return result;
        }

        /// <summary>
        /// Map range of elements to a new CQ object using a function delegate to populate it.
        /// </summary>
        ///
        /// <param name="source">
        /// Source elements
        /// </param>
        /// <param name="del">
        /// Delegate to the mapping function
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>

        protected CQ MapRangeToNewCQ(IEnumerable<IDomObject> source, Func<IDomObject, IEnumerable<IDomObject>> del)
        {
            CQ output = NewCqInDomain();
            foreach (var item in source)
            {
                output.SelectionSet.AddRange(del(item));
            }
            return output;
        }

        /// <summary>
        /// Runs a set of selectors and returns the combined result as a single enumerable.
        /// </summary>
        ///
        /// <param name="selectors">
        /// A sequence of strings that area each selectors
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process merge selections in this collection.
        /// </returns>

        protected IEnumerable<IDomObject> MergeSelections(IEnumerable<string> selectors)
        {
            SelectionSet<IDomObject> allContent = new SelectionSet<IDomObject>(SelectionSetOrder.Ascending);

            Each(selectors, item => allContent.AddRange(Select(item)));
            return allContent;
        }

        /// <summary>
        /// Runs a set of HTML creation selectors and returns result as a single enumerable.
        /// </summary>
        ///
        /// <param name="content">
        /// A sequence of strings that are each valid HTML
        /// </param>
        ///
        /// <returns>
        /// A new sequence containing all the elements from all the selectors.
        /// </returns>

        protected IEnumerable<IDomObject> MergeContent(IEnumerable<string> content)
        {
            List<IDomObject> allContent = new List<IDomObject>();
            foreach (var item in content)
            {
                allContent.AddRange(CQ.Create(item));
            }
            return allContent;
        }


        /// <summary>
        /// Enumerates only the IDomElements in the sequence provided. Any other elemnent types are excluded..
        /// </summary>
        ///
        /// <param name="objects">
        /// The objects.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process only elements in this collection.
        /// </returns>

        protected IEnumerable<IDomElement> OnlyElements(IEnumerable<IDomObject> objects)
        {
            foreach (var item in objects)
            {
                IDomElement el = item as IDomElement;
                if (el != null)
                {
                    yield return el;
                }
            }
        }

        /// <summary>
        /// Filter a sequence using a selector if the selector is not empty. If it's empty, return a new
        /// CQ object containing the original list.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector.
        /// </param>
        /// <param name="list">
        /// The source sequence.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        protected CQ FilterIfSelector(string selector, IEnumerable<IDomObject> list)
        {
            return FilterIfSelector(selector, list, SelectionSetOrder.OrderAdded);
        }

        /// <summary>
        /// Filter a sequence using a selector if the selector is not empty. If it's empty, return a new CQ object
        /// containing the original list.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector.
        /// </param>
        /// <param name="list">
        /// The source sequence
        /// </param>
        /// <param name="order">
        /// The order in which the elements of the new CQ object should be returned
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>

        protected CQ FilterIfSelector(string selector, IEnumerable<IDomObject> list, SelectionSetOrder order)
        {
            CQ output;
            if (String.IsNullOrEmpty(selector))
            {
                output = NewInstance(list, this);
            }
            else
            {
                output = NewInstance(FilterElements(list, selector), this);
            }
            output.Order = order;
            return output;
        }

        /// <summary>
        /// Filter a sequence using a selector, ignoring missing selectors
        /// </summary>
        ///
        /// <param name="elements">
        /// The sequence to filter
        /// </param>
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process filter elements in this collection.
        /// </returns>

        protected IEnumerable<IDomObject> FilterElements(IEnumerable<IDomObject> elements, string selector)
        {
            return FilterElementsIgnoreNull(elements, selector ?? "");
        }

        /// <summary>
        /// Filter an element list using another selector. A null selector results in no filtering; an
        /// empty string selector results in an empty list being return.
        /// </summary>
        ///
        /// <param name="elements">
        /// The sequence to filter.
        /// </param>
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// The filtered list.
        /// </returns>

        protected IEnumerable<IDomObject> FilterElementsIgnoreNull(IEnumerable<IDomObject> elements, string selector)
        {
            if (selector == "")
            {
                return Objects.EmptyEnumerable<IDomObject>();
            }
            else if (selector == null)
            {
                return elements;
            }
            else
            {
                Selector selectors = new Selector(selector).ToFilterSelector();
                return selectors.Filter(Document, elements);
            }
        }
        

        #endregion
        
    }
}
