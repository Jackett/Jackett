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

    public class HtmlOptionsCollection: IHtmlOptionsCollection
    {
        #region constructor

        public HtmlOptionsCollection(IDomElement parent)
        {
            if (parent.NodeNameID != HtmlData.tagOPTION)
            {
                throw new ArgumentException("The parent node for an HtmlOptionsCollection must be an Option node.");
            }

            Parent = (IDomElementSelect)parent;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the parent element for this collection
        /// </summary>

        public IDomElementSelect Parent { get; protected set; }

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

        /// <summary>
        /// Returns the index of the currently selected item. You may select an item by assigning its
        /// index to this property. By assigning -1 to this property, all items will be deselected.
        /// Returns -1 if no items are selected.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/selectedIndex.
        /// </url>

        public int SelectedIndex
        {
            get
            {
                var index = this.IndexOf(item => item.Selected);

                return Parent.Multiple ?
                    index :
                    index == -1 ? 0 : index;
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

        public IDomElement SelectedItem
        {
            get
            {
                throw new NotImplementedException();
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
        /// <param name="parent">
        /// The parent.
        /// </param>
        ///
        /// <returns>
        /// An enumerator.
        /// </returns>

        protected IEnumerable<DomElement> Children()
        {
            return Children(Parent);
        }

        /// <summary>
        /// Implementation for Children.
        /// </summary>

        private IEnumerable<DomElement> Children(IDomElement parent)
        {
            foreach (var item in parent.ChildNodes)
            {
                switch (item.NodeNameID)
                {
                    case HtmlData.tagOPTION:
                        yield return (DomElement)item;
                        break;
                    case HtmlData.tagOPTGROUP:
                        foreach (var child in Children((IDomElement)item))
                        {
                            yield return child;
                        }
                        break;
                }
            }
        }

        #endregion

    }
}
