using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// A SELECT element
    /// </summary>

    public interface IHTMLSelectElement : IDomElement, IFormSubmittableElement, IFormReassociateableElement
    {
        /// <summary>
        /// A collection of HTML option elements (in document order)
        /// </summary>
        /// <url>https://developer.mozilla.org/en/DOM/HTMLOptionsCollection</url>

        IHTMLOptionsCollection Options { get; }

        /// <summary>
        /// Returns the index of the currently selected item. You may select an item by assigning its
        /// index to this property. By assigning -1 to this property, all items will be deselected.
        /// Returns -1 if no items are selected.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/selectedIndex.
        /// </url>

        int SelectedIndex { get; set; }

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

        IDomElement SelectedItem { get; set; }

        /// <summary>
        /// This Boolean attribute indicates that multiple options can be selected in the list. If it is
        /// not specified, then only one option can be selected at a time.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/HTML/Element/select
        /// </url>

        bool Multiple { get; set; }

        /// <summary>
        /// Gets the number of options in the select
        /// </summary>

        int Length { get; }
    }
}
