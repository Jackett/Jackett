using System.Collections.Generic;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML input element.
    /// </summary>

    public class HTMLInputElement : FormSubmittableElement, IHTMLInputElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLInputElement()
            : base(HtmlData.tagINPUT)
        {
        }

        /// <summary>
        /// A URL that provides the destination of the hyperlink. If the href attribute is not specified,
        /// the element represents a placeholder hyperlink.
        /// </summary>

        public bool Autofocus
        {
            get
            {
                return HasAttribute(HtmlData.attrAUTOFOCUS);
            }
            set
            {
                SetProp(HtmlData.attrAUTOFOCUS, value);
            }
        }

        /// <summary>
        /// Specifies that the element is a required part of form submission.
        /// </summary>

        public bool Required
        {
            get
            {
                return HasAttribute(HtmlData.attrREQUIRED);
            }
            set
            {
                SetProp(HtmlData.attrREQUIRED, value);
            }
        }

        /// <summary>
        /// The value of the "type" attribute. For input elements, this property always returns a
        /// lowercase value and defaults to "text" if there is no type attribute. For other element types,
        /// it simply returns the value of the "type" attribute.
        /// </summary>
        ///
        /// <remarks>
        /// TODO: in HTML5 type can be used on OL attributes (and maybe others?) and its value is case
        /// sensitive. The Type of input elements is always lower case, though. This behavior needs to be
        /// verified against the spec.
        /// </remarks>
        ///
        /// <value>
        /// The type.
        /// </value>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/type
        /// </url>

        public override string Type
        {
            get
            {
                return GetAttribute(HtmlData.attrTYPE, "text").ToLower();
            }
            set
            {
                base.Type = value;
            }
        }

        /// <summary>
        /// Returns all the keys that should be in the index for this item (keys for class, tag,
        /// attributes, and id)
        /// </summary>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process index keys in this collection.
        /// </returns>

        public override IEnumerable<ushort[]> IndexKeysRanged()
        {
            return base.IndexKeysRanged();
        }

        /// <summary>
        /// Gets a value indicating whether this object has children. For input elements, this is always false.
        /// </summary>

        public override bool HasChildren
        {
            get
            {
                return false;
            }
        }

        ///// <summary>
        ///// Gets an attribute value for matching, accounting for default values of special attribute
        ///// types.
        ///// </summary>
        /////
        ///// <param name="attributeId">
        ///// Identifier for the attribute.
        ///// </param>
        ///// <param name="value">
        ///// [out] The value.
        ///// </param>
        /////
        ///// <returns>
        ///// The attribute for matching.
        ///// </returns>

        //internal override bool TryGetAttributeForMatching(ushort attributeId, out string value)
        //{
        //    if (attributeId == HtmlData.attrTYPE)
        //    {
        //        if (!TryGetAttribute(attributeId, out value))
        //        {
        //            value = "text";
        //        }
        //        return true;
        //    }
        //    else
        //    {
        //        return base.TryGetAttributeForMatching(attributeId, out value);
        //    }
        //}

        //protected override IEnumerable<ushort> IndexAttributesTokens()
        //{
        //    if (!HasAttribute(HtmlData.attrTYPE)) {
        //        yield return HtmlData.attrTYPE;
        //    }
        //    foreach (var item in base.IndexAttributesTokens())
        //    {
        //        yield return item;
        //    }
        //}
    }
}
