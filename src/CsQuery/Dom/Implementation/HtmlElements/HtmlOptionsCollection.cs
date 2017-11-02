using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using CsQuery.ExtensionMethods;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A collection of HTML options.
    /// </summary>
    ///
    /// <url>
    /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
    /// </url>

    public class HTMLOptionsCollection: IHTMLOptionsCollection
    {
        #region constructor

        /// <summary>
        /// Creates an HTMLOptionsCollection from the children of a Select element.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        ///
        /// <param name="parent">
        /// The parent element for this collection.
        /// </param>

        public HTMLOptionsCollection(IDomElement parent)
        {
            if (parent.NodeNameID != HtmlData.tagSELECT)
            {
                throw new ArgumentException("The parent node for an HtmlOptionsCollection must be a SELECT node.");
            }

            Parent = (IHTMLSelectElement)parent;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the parent element for this collection
        /// </summary>

        public IHTMLSelectElement Parent { get; protected set; }

        /// <summary>
        /// Returns the specific node at the given zero-based index (gives null if out of range)
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        public IDomElement Item(int index)
        {
            return Children().ElementAt(index);
        }

        /// <summary>
        /// Returns the specific node at the given zero-based index (gives null if out of range)
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection.
        /// </url>
        
        [IndexerName("Indexer")]
        public IDomElement this[int index]
        {
            get { return Item(index); }
        }

        /// <summary>
        /// Returns the specific node with the given DOMString (i.e., string) id. Returns null if no such
        /// named node exists.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="name">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element, or null if the named element does not exist.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        public IDomElement NamedItem(string name)
        {
            return Children().Where(item => item.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Returns the specific node at the given zero-based index (gives null if out of range)
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the option element
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        [IndexerName("Indexer")]
        public IDomElement this[string name]
        {
            get {
                return NamedItem(name);            
            }
        }

        #endregion

        #region internal properties

        // These properties are the implementations of methods exposed on IDomElementSelect

        /// <summary>
        /// Logic: if nothing specifically selected, find the first enabled option, otherwise, the first disabled option.
        /// </summary>

        internal int SelectedIndex
        {
            get
            {
                OptionElement el;
                var index = GetSelectedItem(out el);
                return index;
            }
            set
            {
                Children().ForEach((item, i) =>
                {
                    if (i == value)
                    {
                        item.SetAttribute(HtmlData.SelectedAttrId);
                    }
                    else if (item.Selected)
                    {
                        item.RemoveAttribute(HtmlData.SelectedAttrId);
                    }
                });
            }
        }

        private int GetSelectedItem(out OptionElement el)
        {
            // HTML5 says the last selected one is it
            var index = Children(Parent).LastIndexOf(item =>
                   item.Element.HasAttribute("selected"), out el);

            if (Parent.Multiple || index >= 0)
            {
                return index;
            }
            else
            {
                return Children(Parent).IndexOf(item => !item.Disabled, out el);
            }
        }

        internal IDomElement SelectedItem
        {
            get
            {

                OptionElement el;
                GetSelectedItem(out el);
                return el.Element;
            }
            set
            {
                Children().ForEach((item) =>
                {
                    if (item == value)
                    {
                        item.SetAttribute(HtmlData.SelectedAttrId);
                    }
                    else if (item.Selected)
                    {
                        item.RemoveAttribute(HtmlData.SelectedAttrId);
                    }
                });
            }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<IDomObject> GetEnumerator()
        {
            return Children().GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates the element children of a node.
        /// </summary>
        ///
        /// <returns>
        /// An sequence of elements
        /// </returns>

        protected IEnumerable<DomElement> Children()
        {
            return Children(Parent).Select(item => item.Element);
        }

        /// <summary>
        /// Implementation for Children. The bool part of the tuple indicates if the element inherits a
        /// "disabled" property.
        /// </summary>
        ///
        /// <param name="parent">
        /// The parent element for this collection.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process children in this collection.
        /// </returns>

        private IEnumerable<OptionElement> Children(IDomElement parent)
        {
            return Children(parent, false);
        }

        private IEnumerable<OptionElement> Children(IDomElement parent, bool disabled)
        {
            foreach (var item in parent.ChildElements)
            {
                switch (item.NodeNameID)
                {
                    case HtmlData.tagOPTION:
                        yield return new OptionElement {
                             Element = (DomElement)item,
                             Disabled = disabled || item.HasAttribute("disabled")
                        };
                        break;

                    case HtmlData.tagOPTGROUP:
                        foreach (var child in Children(item, disabled || item.HasAttribute("disabled")))
                        {
                            yield return child;
                        }
                        break;
                }
            }
        }

        private struct OptionElement
        {
            public DomElement Element;
            public bool Disabled;

        }

        #endregion

    }
}
