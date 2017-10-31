using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {
        #region public methods

        /// <summary>
        /// Get the combined text contents of each element in the set of matched elements, including
        /// their descendants.
        /// </summary>
        ///
        /// <returns>
        /// A string containing the text contents of the selection.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/text/#text1
        /// </url>

        public string Text()
        {

            StringBuilder sb = new StringBuilder();

            AddTextToStringBuilder(sb, Selection);

            return sb.ToString();
        }



        /// <summary>
        /// Set the content of each element in the set of matched elements to the specified text.
        /// </summary>
        ///
        /// <param name="value">
        /// A string of text.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/text/#text2
        /// </url>

        public CQ Text(string value)
        {
            foreach (IDomElement obj in Elements)
            {
                SetChildText(obj, value);
            }
            return this;
        }

        /// <summary>
        /// Set the content of each element in the set of matched elements to the text returned by the
        /// specified function delegate.
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate to a function that returns an HTML string to insert at the end of each element in
        /// the set of matched elements. Receives the index position of the element in the set and the
        /// old HTML value of the element as arguments. The function can return any data type, if it is not
        /// a string, it's ToString() method will be used to convert it to a string.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/text/#text2
        /// </url>

        public CQ Text(Func<int, string, object> func)
        {

            int count = 0;
            
            foreach (IDomElement obj in Elements)
            {
                var inner = obj.TextContent;
                string newText = func(count, inner).ToString();
                if (newText != inner)
                {
                    SetChildText(obj, newText);
                }
                count++;
            }

            return this;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Helper to add the text contents of a sequence of nodes to the StringBuilder
        /// </summary>
        ///
        /// <param name="sb">
        /// The target
        /// </param>
        /// <param name="nodes">
        /// The nodes to add
        /// </param>

        private void AddTextToStringBuilder(StringBuilder sb, IEnumerable<IDomObject> nodes)
        {
            foreach (var item in nodes)
            {

                switch (item.NodeType)
                {
                    case NodeType.TEXT_NODE:
                        sb.Append(item.NodeValue);
                        break;
                    case NodeType.DOCUMENT_NODE:
                    case NodeType.DOCUMENT_FRAGMENT_NODE:
                        AddTextToStringBuilder(sb, item.ChildNodes);
                        break;
                    case NodeType.ELEMENT_NODE:
                        sb.Append(item.TextContent);
                        break;
                    default:
                        break;
                }

            }
        }
        

        /// <summary>
        /// Sets a child text for this element, using the text node type appropriate for this element's type
        /// </summary>
        ///
        /// <param name="el">
        /// The element to add text to
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>

        private void SetChildText(IDomElement el, string text)
        {
            if (el.ChildrenAllowed)
            {
                el.ChildNodes.Clear();

                IDomText textEl = new DomText(text);
                el.ChildNodes.Add(textEl);
            }
        }

        #endregion

    }
}
