using System;
using System.Linq;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML SELECT element.
    /// </summary>

    public class HTMLSelectElement : FormSubmittableElement, IHTMLSelectElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLSelectElement()
            : base(HtmlData.tagSELECT)
        {
        }

        /// <summary>
        /// A collection of HTML option elements (in document order)
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        public IHTMLOptionsCollection Options
        {
            get
            {
                return SelectOptions();
            }
        }

        /// <summary>
        /// The number OPTION elements contained by this SELECT
        /// </summary>

        public int Length
        {
            get
            {
                return SelectOptions().Count();
            }
        }

        /// <summary>
        /// The type string for this SELECT group.
        /// </summary>


        public override string Type
        {
            get
            {
                return Multiple ? "select-multiple" : "select-one";
            }
            set
            {
                throw new InvalidOperationException("You can't set the type for a SELECT element.");
            }
        }

        private HTMLOptionsCollection SelectOptions()
        {
            if (this.NodeNameID != HtmlData.tagSELECT)
            {
                throw new InvalidOperationException("This property is only applicable to SELECT elements.");
            }
            return new HTMLOptionsCollection((DomElement)this);
        }


        /// <summary>
        /// This Boolean attribute indicates that multiple options can be selected in the list. If it is
        /// not specified, then only one option can be selected at a time.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/HTML/Element/select
        /// </url>

        public bool Multiple
        {
            get
            {
                return ((DomElement)this).HasAttribute(HtmlData.attrMULTIPLE);
            }
            set
            {
                ((DomElement)this).SetAttribute(HtmlData.attrMULTIPLE);
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
                return SelectOptions().SelectedIndex;
            }
            set
            {
                SelectOptions().SelectedIndex = value;
            }
        }

        /// <summary>
        /// Holds the currently selected item. If no item is currently selected, this value will be null.
        /// You can select an item by setting this value. A select event will be sent to the container
        /// (i.e. the listbox, richlistbox, etc., not the list item that was selected) when it is changed
        /// either via this property, the selectedIndex property, or changed by the user.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/selectedItem
        /// </url>

        public IDomElement SelectedItem
        {
            get
            {
                return SelectOptions().SelectedItem;
            }
            set
            {
                SelectOptions().SelectedItem = value;
            }
        }

        /// <summary>
        /// Get or set the value of the selected item for this Select list. When setting, if the value
        /// cannot be matched to an option, no index will be selected.
        /// </summary>

        public override string Value
        {
            get
            {
                var item = SelectedItem;
                return item == null ? "" : item.Value;
            }
            set
            {
                foreach (var item in Options)
                {
                    if (item.Value == value)
                    {
                        SelectedIndex = item.Index;
                        return;
                    }
                }
                SelectedIndex = -1;
            }
        }
    }
}
