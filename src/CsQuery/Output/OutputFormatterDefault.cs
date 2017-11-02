using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.HtmlParser;

namespace CsQuery.Output
{
    /// <summary>
    /// Default output formatter.
    /// </summary>

    public class FormatDefault: IOutputFormatter
    {
        #region constructors

        /// <summary>
        /// Abstract base class constructor.
        /// </summary>
        ///
        /// <param name="options">
        /// Options for controlling the operation.
        /// </param>
        /// <param name="encoder">
        /// The encoder.
        /// </param>

        public FormatDefault(DomRenderingOptions options, IHtmlEncoder encoder)
        {
            DomRenderingOptions = options;
            MergeDefaultOptions();
            HtmlEncoder = encoder ?? HtmlEncoders.Default;
        }

        /// <summary>
        /// Creates the default OutputFormatter using default DomRenderingOption values and default HtmlEncoder
        /// </summary>

        public FormatDefault()
            : this(DomRenderingOptions.Default, HtmlEncoders.Default)
        { }

        #endregion

        #region private properties

        private DomRenderingOptions DomRenderingOptions;
        private IHtmlEncoder HtmlEncoder;
        private Stack<NodeStackElement> _OutputStack;
        private bool IsXHTML;

        /// <summary>
        /// Stack of the output tree
        /// </summary>

        protected Stack<NodeStackElement> OutputStack
        {
            get
            {
                if (_OutputStack == null)
                {
                    _OutputStack = new Stack<NodeStackElement>();
                }
                return _OutputStack;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Renders the object to the textwriter.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        public void Render(IDomObject node, TextWriter writer)
        {
            SetDocType(node);
            RenderInternal(node, writer);
        }

        /// <summary>
        /// Renders the object to a string.
        /// </summary>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        ///
        /// <returns>
        /// A string.
        /// </returns>

        public string Render(IDomObject node)
        {
            SetDocType(node);

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            if (node is IDomDocument)
            {
                RenderChildrenInternal(node,sw);
            }
            else
            {
                Render(node, sw);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the HTML representation of this element and its children.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>
        /// <param name="includeChildren">
        /// true to include, false to exclude the children.
        /// </param>

        public virtual void RenderElement(IDomObject element, TextWriter writer, bool includeChildren)
        {
            SetDocType(element);

            RenderElementInternal(element, writer, includeChildren);
            RenderStack(writer);
        }
       
        /// <summary>
        /// Renders the children of this element.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        public void RenderChildren(IDomObject element, TextWriter writer)
        {
            SetDocType(element);
            RenderChildrenInternal(element, writer);
        }

        #endregion

        #region private methods


        private void RenderInternal(IDomObject node, TextWriter writer)
        {
            OutputStack.Push(new NodeStackElement(node, false, false));
            RenderStack(writer);
        }


        private void RenderChildrenInternal(IDomObject element, TextWriter writer)
        {
            if (element.HasChildren)
            {
                ParseChildren(element);
            }
            else
            {
                OutputStack.Push(new NodeStackElement(element, false, false));
            }

            RenderStack(writer);

        }

        /// <summary>
        /// Gets the HTML representation of this element and its children. (This is the implementation -
        /// it will not flush the stack)
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>
        /// <param name="includeChildren">
        /// true to include, false to exclude the children.
        /// </param>

        protected virtual void RenderElementInternal(IDomObject element, TextWriter writer, bool includeChildren)
        {
            bool quoteAll = DomRenderingOptions.HasFlag(DomRenderingOptions.QuoteAllAttributes);

            writer.Write("<");
            writer.Write(element.NodeName.ToLower());

            if (element.HasAttributes)
            {
                foreach (var kvp in element.Attributes)
                {
                    writer.Write(" ");
                    RenderAttribute(writer, kvp.Key, kvp.Value, quoteAll);
                }
            }

            if (element.InnerHtmlAllowed || element.InnerTextAllowed)
            {
                writer.Write(">");

                EndElement(element);

                if (includeChildren)
                {
                    ParseChildren(element);
                }
                else
                {
                    writer.Write(element.HasChildren ?
                            "..." :
                            String.Empty);
                }

            }
            else
            {
                writer.Write(
                    IsXHTML ?
                    " />" :
                    ">"
                    );
              
            }
        }

        /// <summary>
        /// Adds the element close tag to the output stack.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        protected virtual void EndElement(IDomObject element)
        {
            OutputStack.Push(new NodeStackElement(element, false, true));
        }

        /// <summary>
        /// Process the output stack.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>

        protected void RenderStack(TextWriter writer)
        {

            while (OutputStack.Count > 0)
            {
                var nodeStackEl = OutputStack.Pop();
                var el = nodeStackEl.Element;

                if (nodeStackEl.IsClose)
                {
                    RenderElementCloseTag(el, writer);
                }
                else
                {
                    switch (el.NodeType)
                    {
                        case NodeType.ELEMENT_NODE:
                            RenderElementInternal(el, writer, true);
                            break;
                        case NodeType.DOCUMENT_FRAGMENT_NODE:
                        case NodeType.DOCUMENT_NODE:
                            RenderElements(el.ChildNodes, writer);
                            break;
                        case NodeType.TEXT_NODE:
                            RenderTextNode(el, writer, nodeStackEl.IsRaw);
                            break;
                        case NodeType.CDATA_SECTION_NODE:
                            RenderCdataNode(el, writer);
                            break;
                        case NodeType.COMMENT_NODE:
                            RenderCommentNode(el, writer);
                            break;
                        case NodeType.DOCUMENT_TYPE_NODE:
                            RenderDocTypeNode(el, writer);
                            break;
                        default:
                            throw new NotImplementedException("An unknown node type was found while rendering the CsQuery document.");
                    }
                }
            }
        }

      

        /// <summary>
        /// Renders a sequence of elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        protected void RenderElements(IEnumerable<IDomObject> elements,TextWriter writer)
        {
            foreach (var item in elements)
            {
                Render(item, writer);
            }
        }


        /// <summary>
        /// Renders the element close tag.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        protected virtual void RenderElementCloseTag(IDomObject element, TextWriter writer)
        {

            writer.Write("</");
            writer.Write(element.NodeName.ToLower());
            writer.Write(">");
        }

        /// <summary>
        /// Renders all the children of the passed node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        protected virtual void ParseChildren(IDomObject element)
        {
            if (element.HasChildren)
            {
                foreach (IDomObject el in element.ChildNodes.Reverse())
                {
                    NodeStackElement nodeStackEl = new NodeStackElement(el, el.NodeType == NodeType.TEXT_NODE && HtmlData.HtmlChildrenNotAllowed(element.NodeNameID), false);
                    OutputStack.Push(nodeStackEl);
                }
            }
        }

        /// <summary>
        /// Renders the text node.
        /// </summary>
        ///
        /// <param name="textNode">
        /// The text node.
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>
        /// <param name="raw">
        /// true to raw.
        /// </param>

        protected virtual void RenderTextNode(IDomObject textNode, TextWriter writer, bool raw)
        {
            if (raw)
            {
                writer.Write(textNode.NodeValue);
            }
            else
            {
                HtmlEncoder.Encode(textNode.NodeValue, writer);
            }
        }

        /// <summary>
        /// Renders a CDATA node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        protected void RenderCdataNode(IDomObject element, TextWriter writer)
        {
            writer.Write("<![CDATA[" + element.NodeValue + ">");
        }

        /// <summary>
        /// Renders the comment node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        protected void RenderCommentNode(IDomObject element, TextWriter writer)
        {
            if (DomRenderingOptions.HasFlag(DomRenderingOptions.RemoveComments))
            {
                return;
            }
            else
            {
                writer.Write("<!--" + element.NodeValue + "-->");
            }
        }

        /// <summary>
        /// Renders the document type node.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render
        /// </param>
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>

        protected void RenderDocTypeNode(IDomObject element, TextWriter writer)
        {

            writer.Write("<!DOCTYPE " + ((IDomSpecialElement)element).NonAttributeData + ">");
        }

        /// <summary>
        /// Render an attribute.
        /// </summary>
        ///
        /// <param name="writer">
        /// The writer to which output is written.
        /// </param>
        /// <param name="name">
        /// The name of the attribute.
        /// </param>
        /// <param name="value">
        /// The attribute value.
        /// </param>
        /// <param name="quoteAll">
        /// true to require quotes around the attribute value, false to use quotes only if needed.
        /// </param>

        protected void RenderAttribute(TextWriter writer, string name, string value, bool quoteAll)
        {
            // validator.nu: as it turns out "" and missing are synonymous
            // don't ever render attr=""

            if (value != null && value != "")
            {
                string quoteChar;
                string attrText = HtmlData.AttributeEncode(value,
                    quoteAll,
                    out quoteChar);
                writer.Write(name.ToLower());
                writer.Write("=");
                writer.Write(quoteChar);
                writer.Write(attrText);
                writer.Write(quoteChar);
            }
            else
            {
                writer.Write(name);
            }
        }

        /// <summary>
        /// Merge options with defaults when needed.
        /// </summary>

        protected void MergeDefaultOptions()
        {
            if (DomRenderingOptions.HasFlag(DomRenderingOptions.Default))
            {
                DomRenderingOptions = CsQuery.Config.DomRenderingOptions | DomRenderingOptions & ~(DomRenderingOptions.Default);
            }
        }

        /// <summary>
        /// Sets document type.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to render.
        /// </param>

        protected void SetDocType(IDomObject element)
        {
            var docType = element.Document == null ? 
                CsQuery.Config.DocType : element.Document.DocType;
            IsXHTML = docType == DocType.XHTML || docType == DocType.XHTMLStrict;
        }

        #endregion

        /// <summary>
        /// An element that captures the state of a element on the output stack.
        /// </summary>

        protected class NodeStackElement
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            ///
            /// <param name="element">
            /// The element.
            /// </param>
            /// <param name="isRaw">
            /// true if this object is raw.
            /// </param>
            /// <param name="isClose">
            /// true if this object is close.
            /// </param>

            public NodeStackElement(IDomObject element, bool isRaw, bool isClose)
            {
                Element = element;
                IsRaw = isRaw;
                IsClose = isClose;
            }

            /// <summary>
            /// The element.
            /// </summary>

            public IDomObject Element;

            /// <summary>
            /// The text node should be output as raw (un-encoded) text.
            /// </summary>

            public bool IsRaw;

            /// <summary>
            /// The is a closing tag only.
            /// </summary>

            public bool IsClose;
        }

    }
}
