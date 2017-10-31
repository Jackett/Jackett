using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;

namespace CsQuery
{
    /// <summary>
    /// Interface defining the style declaration for a DOM element.
    /// </summary>

    public interface ICSSStyleDeclaration
    {
        /// <summary>
        /// The number of properties that have been explicitly set in this declaration block.
        /// </summary>

        int Length { get; }

        /// <summary>
        /// The parsable textual representation of the declaration block (excluding the surrounding curly
        /// braces). Setting this attribute will result in the parsing of the new value and resetting of
        /// all the properties in the declaration block including the removal or addition of properties.
        /// </summary>

        string CssText { get; set; }

        /// <summary>
        /// The CSS rule that contains this declaration block or null if this CSSStyleDeclaration is not
        /// attached to a CSSRule.
        /// </summary>

        ICSSRule ParentRule { get; }

        /// <summary>
        /// Event raised when the HasStyles attribute changes
        /// </summary>

        event EventHandler<CSSStyleChangedArgs> OnHasStylesChanged;

        // BELOW THIS IS LEGACY

        /// <summary>
        /// Test whether a named style is defined on an element.
        /// </summary>
        ///
        /// <param name="styleName">
        /// The name of the style.
        /// </param>
        ///
        /// <returns>
        /// true if the style is explicitly defined on this element, false if not.
        /// </returns>

        bool HasStyle(string styleName);

        /// <summary>
        /// Sets one or more styles on the element.
        /// </summary>
        ///
        /// <param name="styles">
        /// The semicolon-separated style definitions.
        /// </param>

        void SetStyles(string styles);

        /// <summary>
        /// Sets one or more styles on the element.
        /// </summary>
        ///
        /// <param name="styles">
        /// The semicolon-separated style definitions.
        /// </param>
        /// <param name="strict">
        /// When true, the styles will be validated for correct sytax, and an error thrown if they fail.
        /// </param>

        void SetStyles(string styles, bool strict);

        /// <summary>
        /// Sets a style identified by name to a value.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>

        void SetStyle(string name, string value);

        /// <summary>
        /// Sets a style identified by name to a value.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="strict">
        /// When true, the styles will be validated for correct sytax, and an error thrown if they fail.
        /// </param>

        void SetStyle(string name, string value, bool strict);

        /// <summary>
        /// Gets a named style.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        ///
        /// <returns>
        /// The style.
        /// </returns>

        string GetStyle(string name);

        /// <summary>
        /// Removes the style from the style descriptor for this element.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails. this can only fail if the style was not present.
        /// </returns>

        bool RemoveStyle(string name);

    }

   

    
}

