using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CsQuery.Implementation;

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.ExtensionMethods.Xml
{
    /// <summary>
    /// Cq XML node. This is not implemented completely. Not suggested that you use it.
    /// </summary>

    public class CqXmlNode: XmlElement 
    {
        public CqXmlNode(XmlDocument xmlDocument, IDomObject element): base("",GetNodeName(element),"",xmlDocument)
        {
            Element = element;
            XmlDocument = xmlDocument;
        }

        private IDomObject Element;
        private XmlDocument XmlDocument;
        private XmlNodeList InnerChildNodes;
        private bool IsAttributesCreated;
        private bool IsChildListCreated;

        public override XmlNode CloneNode(bool deep)
        {
            throw new NotImplementedException();
        }

        public override string LocalName
        {
            get { return base.LocalName; }
        }

        public override string Name
        {
            get { return base.Name; }
        }

        public override XmlNodeType NodeType
        {
            get { return NodeTypeMap(Element.NodeType); }
        }

      
        public override XmlAttributeCollection Attributes
        {
            get
            {
                if (!IsAttributesCreated)
                {
                    if (Element.HasAttributes)
                    {
                        foreach (var attr in Element.Attributes)
                        {
                            var xmlAttr = XmlDocument.CreateAttribute(attr.Key);
                            xmlAttr.Value = attr.Value;

                            base.Attributes.Append(xmlAttr);
                        }
                    }
                    IsAttributesCreated = true;
                }

                return base.Attributes;
            }
        }


        public override XmlNodeList ChildNodes
        {
            get
            {
                if (!IsChildListCreated)
                {
                    InnerChildNodes = new CqXmlNodeList(XmlDocument, Element.ChildNodes);
                    IsChildListCreated = true;
                }
                return InnerChildNodes;
            }
        }


        protected XmlNodeType NodeTypeMap(NodeType type)
        {
            switch (type)
            {
                //case CsQuery.NodeType.ATTRIBUTE_NODE:
                //    return XmlNodeType.Attribute;
                case CsQuery.NodeType.CDATA_SECTION_NODE:
                    return XmlNodeType.CDATA;
                case CsQuery.NodeType.COMMENT_NODE:
                    return XmlNodeType.Comment;
                case CsQuery.NodeType.DOCUMENT_FRAGMENT_NODE:
                    return XmlNodeType.DocumentFragment;
                case CsQuery.NodeType.DOCUMENT_NODE:
                    //return XmlNodeType.Document;
                     return XmlNodeType.Element;
                case CsQuery.NodeType.DOCUMENT_TYPE_NODE:
                    return XmlNodeType.DocumentType;
                case CsQuery.NodeType.ELEMENT_NODE:
                    return XmlNodeType.Element;
                case CsQuery.NodeType.TEXT_NODE:
                    return XmlNodeType.Text;
                default:
                    throw new NotImplementedException("Unknown node type");

            }
        }
        
        private static string GetNodeName(IDomObject element)
        {
            if (!String.IsNullOrEmpty(element.NodeName))
            {
                return CleanXmlNodeName(element.NodeName);
            } else {
                if (element is IDomFragment)
                {
                    return "ROOT";
                }
                else if (element is IDomDocument)
                {
                    return "ROOT";
                } else {
                    return "UNKNOWN";
                }
            }
        }

        /// <summary>
        /// Clean an XML node name. Since the only problematic node names should be like "#text" we just
        /// look for a # and strip it.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        ///
        /// <returns>
        /// A string that's acceptable as an XML node name.
        /// </returns>

        private static string CleanXmlNodeName(string name)
        {

            if (name[0] == '#')
            {
                return name.Substring(1);
            }
            else
            {
                return name;
            }

        }
    }
}
