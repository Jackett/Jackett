using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// A pseudoselector that returns elements that are visible. Visibility is defined by CSS: a
    /// nonzero opacity, a display that is not "hidden", and the absence of zero-valued width &amp;
    /// heights. Additionally, input elements of type "hidden" are always considered not visible.
    /// </summary>

    public class Visible: PseudoSelectorFilter
    {
        /// <summary>
        /// Test whether an element is visible
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if it matches, false if not.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            return IsVisible(element);
        }

        /// <summary>
        /// Test whether the passed element is visible, based on CSS styles and height/width properties.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if visible, false if not.
        /// </returns>

        public static bool IsVisible(IDomObject element)
        {
            //ensure if a text node is passed, we start with its container.
            
            IDomObject el = element is IDomElement ? 
                element : 
                element.ParentNode;

            while (el != null && el.NodeType == NodeType.ELEMENT_NODE)
            {
                if (ElementIsItselfHidden((IDomElement)el))
                {
                    return false;
                }
                el = el.ParentNode;
            }
            return true;
        }
        
        private static bool ElementIsItselfHidden(IDomElement el)
        {
            if (el.NodeNameID == HtmlData.tagINPUT && el.Type == "hidden")
            {
                return true;
            }

            if (el.HasStyles)
            {
                if (el.Style["display"] == "none" || el.Style.NumberPart("opacity") == 0)
                {
                    return true;
                }
                double? wid = el.Style.NumberPart("width");
                double? height = el.Style.NumberPart("height");
                if (wid == 0 || height == 0)
                {
                    return true;
                }
            }
            string widthAttr, heightAttr;
            widthAttr = el.GetAttribute("width");
            heightAttr = el.GetAttribute("height");

            return widthAttr == "0" || heightAttr == "0";
        }

       
    }
}
