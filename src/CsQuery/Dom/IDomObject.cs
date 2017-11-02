using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;

namespace CsQuery
{
    /// <summary>
    /// An node that appears directly in the DOM. This is essentially synonymous with a Node, but it does
    /// not include attributes.
    /// 
    /// All properties of Element nodes are implemented in IDomObject even though many are only applicable to
    /// Elements. Attempting to read a property that doesn't exist on the node type will generally return 'null'
    /// whereas attempting to write will throw an exception. This is intended to make coding against this model
    /// the same as coding against the actual DOM, where accessing nonexistent properties is acceptable. Because
    /// some javascript code actually uses this in logic we allow the same kind of access. It also eliminates the
    /// need to cast frequently, for example, when accessing the results of a jQuery object by index.
    /// </summary>
    public interface IDomObject : IDomNode, IComparable<IDomObject>
    {
        // To simulate the way the real DOM works, most properties/methods of things directly in the DOM
        // are part of a common interface, even if they do not apply.

        /// <summary>
        /// The HTML document to which this element belongs
        /// </summary>
        IDomDocument Document { get; }

        /// <summary>
        /// The direct parent of this node
        /// </summary>
        IDomContainer ParentNode { get; }

        /// <summary>
        /// Returns all of the ancestors of the given node, in descending order of their depth from the root node.
        /// </summary>
        /// <returns>The ancestors.</returns>
        IEnumerable<IDomContainer> GetAncestors();

        /// <summary>
        /// Returns all of the descendents of the given node, in pre-order depth first order.
        /// </summary>
        /// <returns>The descendents.</returns>
        IEnumerable<IDomObject> GetDescendents();

        /// <summary>
        /// Returns all IDomElement descendents of the given node, in pre-order depth first order.
        /// </summary>
        /// <returns>The descendents.</returns>
        IEnumerable<IDomElement> GetDescendentElements();

        /// <summary>
        /// The child node at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the child node to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject, the element at the specified index within this node's children.
        /// </returns>

        IDomObject this[int index] { get; }

        /// <summary>
        /// Get or set the value of the named attribute on this element.
        /// </summary>
        ///
        /// <param name="attribute">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// An attribute value.
        /// </returns>

        string this[string attribute] { get; set; }

        /// <summary>
        /// Get or set value of the id attribute.
        /// </summary>

        string Id { get; set; }

        /// <summary>
        /// An interface to access the attributes collection of this element.
        /// </summary>

        IAttributeCollection Attributes { get; }

        /// <summary>
        /// An object encapsulating the Styles associated with this element.
        /// </summary>

        CSSStyleDeclaration Style { get; set; }

        /// <summary>
        /// gets and sets the value of the class attribute of the specified element.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.className
        /// </url>

        string ClassName { get; set; }

        /// <summary>
        /// All the unique class names applied to this object.
        /// </summary>
        /// <value>
        /// A sequence of strings	   
        /// </value>

        IEnumerable<string> Classes { get; }

        /// <summary>
        /// For input elements, the "value" property of this element. Returns null for other element
        /// types.
        /// </summary>

        string Value { get; set; }

        /// <summary>
        /// The value of an input element, or the text of a textarea element.
        /// </summary>

        string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets or gets the HTML of an elements descendants.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.innerHTML
        /// </url>

        string InnerHTML { get; set; }

        /// <summary>
        /// Gets or sets the outer HTML.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en-US/docs/DOM/element.outerHTML
        /// </url>

        string OuterHTML { get; set; }

        /// <summary>
        /// Gets or sets the text content of a node and its descendants, formatted like Chrome (a new
        /// line for each text node, a space between inline elements, a new line for block elements).
        /// Unlike browsers, the contents of hidden elements are included, since we cannot determine
        /// conclusively what is hidden.
        /// 
        /// The contents of comments, CDATA nodes, SCRIPT, STYLE and TEXTAREA nodes are ignored. Note:
        /// this is an IE property; there is no standard. The way CsQuery formats using InnerText is
        /// roughly like Chrome but may not match exactly.
        /// </summary>
        ///
        /// <url>
        /// http://msdn.microsoft.com/en-us/library/ms533899%28v=VS.85%29.aspx
        /// </url>

        string InnerText { get; set; }

        /// <summary>
        /// Gets or sets the text content of a node and its descendants, including all whitespace.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.textContent
        /// </url>

        string TextContent { get; set; }

        /// <summary>
        /// Adds a node to the end of the list of children of a specified parent node. If the node
        /// already exists it is removed from current parent node, then added to new parent node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to append.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.appendChild
        /// </url>

        void AppendChild(IDomObject element);

        /// <summary>
        /// Removes a child node from the DOM. Returns removed node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to remove.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/En/DOM/Node.removeChild
        /// </url>

        void RemoveChild(IDomObject element);

        /// <summary>
        /// Inserts the specified node before a reference element as a child of the current node.
        /// </summary>
        ///
        /// <param name="newNode">
        /// The node to insert.
        /// </param>
        /// <param name="referenceNode">
        /// The node before which the new node will be inserted.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.insertBefore
        /// </url>

        void InsertBefore(IDomObject newNode, IDomObject referenceNode);

        /// <summary>
        /// Inserts the specified node after a reference element as a child of the current node.
        /// </summary>
        ///
        /// <remarks>
        /// This is a CsQuery extension.
        /// </remarks>
        ///
        /// <param name="newNode">
        /// The new node to be inserted.
        /// </param>
        /// <param name="referenceNode">
        /// The node after which the new node will be inserted.
        /// </param>

        void InsertAfter(IDomObject newNode, IDomObject referenceNode);

        /// <summary>
        /// Returns the node's first child in the tree, or null if the node is childless. If the node is a Document, it returns the first node in the list of its direct children.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.firstChild
        /// </url>

        IDomObject FirstChild { get; }

        /// <summary>
        /// Returns the element's first child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.firstElementChild
        /// </url>

        IDomElement FirstElementChild { get; }

        /// <summary>
        /// Returns the last child of a node.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.lastChild
        /// </url>

        IDomObject LastChild { get; }

        /// <summary>
        /// Returns the element's last child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.lastElementChild
        /// </url>

        IDomElement LastElementChild { get; }

        /// <summary>
        /// Returns the node immediately following the specified one in its parent's childNodes list, or
        /// null if the specified node is the last node in that list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.nextSibling
        /// </url>

        IDomObject NextSibling { get; }

        /// <summary>
        /// Returns the node immediately preceding the specified one in its parent's childNodes list,
        /// null if the specified node is the first in that list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.previousSibling
        /// </url>

        IDomObject PreviousSibling { get; }

        /// <summary>
        /// Returns the element immediately following the specified one in its parent's children list,
        /// or null if the specified element is the last one in the list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.nextElementSibling
        /// </url>

        IDomElement NextElementSibling { get; }

        /// <summary>
        /// Returns the element immediately prior to the specified one in its parent's children list, or
        /// null if the specified element is the first one in the list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.previousElementSibling
        /// </url>

        IDomElement PreviousElementSibling { get; }

        /// <summary>
        /// Adds a new boolean attribute or sets its value to true.
        /// </summary>
        ///
        /// <remarks>
        /// In HTML, some element attributes can be specified without a value, such as "checked" or
        /// "multiple." These are not really attributes but rather the default values for element boolean
        /// properties. CsQuery does not distinguish between properties and attributes since the DOM is
        /// stateless, it only reflects the actual markup it represents. The real DOM, to the contrary,
        /// can be changed through javascript. It would be possible for an element's property to be
        /// different from the default value that is specified by its markup.
        /// 
        /// Because of this, we treat properties and attributes the same. A property is simply an
        /// attribute with no specific value, it either exists or does not exist. This overload of
        /// SetAttribute allows you to set a boolean attribute. You can use RemoveAttribute to unset it.
        /// 
        /// It is also possible to set an attribute to an empty string, e.g. with markup like  
        ///     &lt;div someAttr=""&gt;
        ///     
        /// 
        /// </remarks>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>

        void SetAttribute(string name);

        /// <summary>
        /// Adds a new attribute or changes the value of an existing attribute on the specified element.
        /// </summary>
        ///
        /// <remarks>
        /// Setting an attribute to null is the equivalent of using RemoveAttribute. Setting an attribute
        /// to an empty string will cause it to be rendered as an empty value, e.g.
        /// 
        ///     &lt;div someAttr=""&gt;
        /// 
        /// If you want to set a boolean attribute that renders just as the attribute name, use
        /// SetAttribute(name) overload. When using GetAttribute to inspect an attribute value, note that
        /// both boolean and empty-string attributes will return an empty string. There is no way to determine
        /// using GetAttribute if the atttribute was set as a boolean property, or an empty string.
        /// </remarks>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="value">
        /// For input elements, the "value" property of this element. Returns null for other element
        /// types.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.setAttribute
        /// </url>

        void SetAttribute(string name, string value);

        /// <summary>
        /// Returns the value of the named attribute on the specified element. If the named attribute
        /// does not exist, the value returned will be null. The empty string is returned for values that
        /// exist but have no value.
        /// </summary>
        ///
        /// <remarks>
        /// If an attribute does not exist, this returns null. If an attribute was set as a boolean
        /// property attribute, or the attribute has an empty string value, an empty string will be
        /// returned. Note that an empty-string value for GetAttribute could result in an attribute
        /// rendering as either a property, or an empty string value, e.g.
        /// 
        /// &amp;ltdiv someAttr&gt;
        /// &amp;ltdiv someAttr=""&gt;
        /// 
        /// There is no way to determine whether an attribute was set as a property or empty string other
        /// than rendering. The internal data will match the way it was parsed from HTML, or the way it
        /// was set. When set using  <code>SetAttribute(name)</code> it will be displayed as a boolean
        /// property; when set using <code>SetAttribute(name,"")</code> it will be displayed as an empty
        /// string.
        /// </remarks>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// The attribute value string.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.getAttribute
        /// </url>

        string GetAttribute(string name);

        /// <summary>
        /// Returns the value of the named attribute on the specified element. If the named attribute
        /// does not exist, the value returned will be the provide "defaultValue".
        /// </summary>
        ///
        /// <remarks>
        /// This overload is a CsQuery extension.
        /// </remarks>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="defaultValue">
        /// A string to return if the attribute does not exist.
        /// </param>
        ///
        /// <returns>
        /// The attribute value string.
        /// </returns>
        ///
        /// <seealso cref="T:CsQuery.IDomObject.GetAttribute"/>

        string GetAttribute(string name, string defaultValue);

        /// <summary>
        /// Try to get a named attribute.
        /// </summary>
        ///
        /// <remarks>
        /// This overload is a CsQuery extension.
        /// </remarks>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="value">
        /// The attribute value, or null if the named attribute does not exist.
        /// </param>
        ///
        /// <returns>
        /// true if the attribute exists, false if it does not.
        /// </returns>

        bool TryGetAttribute(string name, out string value);

        /// <summary>
        /// Returns a boolean value indicating whether the specified element has the specified attribute or not.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if the named attribute exists, false if not.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.hasAttribute
        /// </url>

        bool HasAttribute(string name);

        /// <summary>
        /// Removes an attribute from the specified element.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it the attribute exists, false if the attribute did not exist. If the attribute
        /// exists it will always be removed, that is, it is not possible for this method to fail unless
        /// the attribute does not exist.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.removeAttribute
        /// </url>

        bool RemoveAttribute(string name);

        /// <summary>
        /// Returns a boolean value indicating whether the named class exists on this element.
        /// </summary>
        ///
        /// <param name="className">
        /// The class name for which to test.
        /// </param>
        ///
        /// <returns>
        /// true if the class is a member of this elements classes, false if not.
        /// </returns>
        /// <remarks>This is a CsQuery extension.</remarks>

        bool HasClass(string className);

        /// <summary>
        /// Adds the class.
        /// </summary>
        ///
        /// <param name="className">
        /// The class name for which to test.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool AddClass(string className);

        /// <summary>
        /// Removes the named class from the classes defined for this element.
        /// </summary>
        ///
        /// <remarks>
        /// This method is a CsQuery extension.
        /// </remarks>
        ///
        /// <param name="className">
        /// The class name to remove.
        /// </param>
        ///
        /// <returns>
        /// true if the class exists and was removed from this element, false if the class did not exist.
        /// If the class exists it will always be removed, that is, it is not possible for this method to
        /// fail if the class exists.
        /// </returns>

        bool RemoveClass(string className);

        /// <summary>
        /// Returns a boolean value indicating whether the named style is defined in the styles for this
        /// element.
        /// </summary>
        ///
        /// <param name="styleName">
        /// Name of the style to test.
        /// </param>
        ///
        /// <returns>
        /// true if the style is explicitly defined on this element, false if not.
        /// </returns>

        bool HasStyle(string styleName);

        /// <summary>
        /// Adds a style descriptor to this element, validating the style name and value against the CSS3
        /// ruleset. The string should be of the form "styleName: styleDef;", e.g.
        /// 
        ///     "width: 10px;"
        /// 
        /// The trailing semicolon is optional.
        /// 
        /// </summary>
        ///
        /// <param name="styleString">
        /// The style string.
        /// </param>

        void AddStyle(string styleString);

        /// <summary>
        /// Adds a style descriptor to this element, optionally validating against the CSS3 ruleset. The
        /// default method always validates; this overload should be used if validation is not desired.
        /// </summary>
        ///
        /// <param name="style">
        /// An object encapsulating the Styles associated with this element.
        /// </param>
        /// <param name="strict">
        /// true to enforce validation of CSS3 styles.
        /// </param>

        void AddStyle(string style, bool strict);

        /// <summary>
        /// Removes the named style from this element.
        /// </summary>
        ///
        /// <param name="name">
        /// The style name.
        /// </param>
        ///
        /// <returns>
        /// true if the style exists and is removed, false if the style did not exist.
        /// </returns>

        bool RemoveStyle(string name);

        /// <summary>
        /// Returns true if this node has any attributes.
        /// </summary>

        bool HasAttributes { get; }

        /// <summary>
        /// Returns true if this node has CSS classes.
        /// </summary>

        bool HasClasses { get; }

        /// <summary>
        /// Returns true if this node has any styles defined.
        /// </summary>

        bool HasStyles { get; }

        /// <summary>
        /// Indicates whether the element is selected or not. This value is read-only. To change the
        /// selection, set either the selectedIndex or selectedItem property of the containing element.
        /// </summary>
        ///
        /// <remarks>
        /// In CsQuery, this property simply indicates the presence of a "selected" attribute. The
        /// accompanying "SelectedIndex" and "SelectedItem" properties have not been implemented as of
        /// this writing.
        /// </remarks>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Attribute/selected
        /// </url>

        bool Selected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the element is checked.
        /// </summary>
        ///
        /// <remarks>
        /// In CsQuery, this property simply indicates the presence of a "checked" attribute.
        /// </remarks>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/checked
        /// </url>

        bool Checked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the element is disabled.
        /// </summary>
        ///
        /// <remarks>
        /// In CsQuery, this property simply indicates the presence of a "disabled" attribute.
        /// </remarks>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/disabled
        /// </url>

        bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the only should be read.
        /// </summary>
        ///
        /// <remarks>
        /// In CsQuery, this property simply indicates the presence of a "readonly" attribute.
        /// </remarks>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/readOnly
        /// </url>

        bool ReadOnly { get; set; }

        /// <summary>
        /// The value of the "type" attribute. For input elements, this property always returns a
        /// lowercase value and defaults to "text" if there is no type attribute. For other element types,
        /// it simply returns the value of the "type" attribute.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/type
        /// </url>

        string Type { get; set; }

        /// <summary>
        /// Gets or sets the name attribute of an DOM object, it only applies to the following elements:
        /// &lt;a&gt; , &lt;applet&gt; , &lt;form&gt; , &lt;frame&gt; , &lt;iframe&gt; , &lt;img&gt; ,
        /// &lt;input&gt; , &lt;map&gt; , &lt;meta&gt; , &lt;object&gt; , &lt;option&gt; , &lt;param&gt; ,
        /// &lt;select&gt; , and &lt;textarea&gt; .
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.name
        /// </url>

        string Name { get; set; }

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. It is possible
        /// for this value to be false but InnerTextAllowed to be true for elements which can have inner
        /// content, but no child HTML markup, such as &lt;textarea&gt; and &lt;script&gt;
        /// </summary>

        bool InnerHtmlAllowed { get; }

        /// <summary>
        /// Gets a value indicating whether text content is allowed as a child of this element. 
        /// DEPRECATED 7-1-2012, PLEASE USE ChildrenAllowed(). This will be removed in a future release.
        /// </summary>

        bool InnerTextAllowed { get; }

        /// <summary>
        /// Gets a value indicating whether this element may have children. When false, it means this is
        /// a void element.
        /// </summary>

        bool ChildrenAllowed { get; }

        /// <summary>
        /// Return the total number of descendants of this element
        /// </summary>
        ///
        /// <returns>
        /// int, the total number of descendants
        /// </returns>

        int DescendantCount();

        /// <summary>
        /// Gets the depth of this node relative to the Document node, which has depth zero.
        /// </summary>

        int Depth { get; }

        /// <summary>
        /// Gets a unique ID for this element among its siblings
        /// </summary>

        [Obsolete]
        char PathID { get; }

        /// <summary>
        /// Gets the unique path to this element from the root of the heirarchy. This is generally only
        /// used for internal purposes.
        /// </summary>

        [Obsolete]
        string Path { get; }

        /// <summary>
        /// Gets the identifier of this node in the index. This isn't used right now in the index. It is
        /// intended that this will become distinct from Index so the index can be sparse (e.g. we don't
        /// have to reindex when removing things)
        /// </summary>

        ushort NodePathID { get; }

        /// <summary>
        /// Gets the full pathname of the node file.
        /// </summary>

        ushort[] NodePath { get; }

        /// <summary>
        /// Wrap this element in a CQ object. This is the CsQuery equivalent of the common jQuery
        /// construct $(el). Since there is no default method in C# that we can use to create a similar
        /// syntax, this method serves the same purpose.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object wrapping this element.
        /// </returns>

        CQ Cq();

        /// <summary>
        /// Clone this element.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this element that is not bound to the original.
        /// </returns>

        new IDomObject Clone();

        /// <summary>
        /// The internal token ID for this element's node name. 
        /// </summary>

        ushort NodeNameID { get; }
    }
}
