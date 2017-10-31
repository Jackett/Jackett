using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CsQuery.ExtensionMethods.Xml
{
    /// <summary>
    /// An adapter to map an INodeList to an XmlNodeList. NOT IMPLEMENTED.
    /// </summary>

    public class CqXmlNodeList: XmlNodeList
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="xmlDocument">
        /// The XML document.
        /// </param>
        /// <param name="nodeList">
        /// List of nodes.
        /// </param>

        public CqXmlNodeList(XmlDocument xmlDocument, INodeList nodeList)
        {
            NodeList = nodeList;
            XmlDocument = xmlDocument;
        }

        private INodeList NodeList;
        private XmlDocument XmlDocument;

        /// <summary>
        /// The number of nodes in this list
        /// </summary>

        public override int Count
        {
            get { return NodeList.Count; }
        }

        /// <summary>
        /// An enumerator for the node list.
        /// </summary>
        ///
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" />.
        /// </returns>

        public override System.Collections.IEnumerator GetEnumerator()
        {

            return Nodes().GetEnumerator();
        }

        private IEnumerable<XmlNode> Nodes()
        {
            foreach (var node in NodeList)
            {
                yield return node.ToXml(XmlDocument);
            }
        }

        /// <summary>
        /// Retrieves a node at the given index.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index into the list of nodes.
        /// </param>
        ///
        /// <returns>
        /// The <see cref="T:System.Xml.XmlNode" /> in the collection. If <paramref name="index" /> is
        /// greater than or equal to the number of nodes in the list, this returns null.
        /// </returns>

        public override XmlNode Item(int index)
        {
            return NodeList[index].ToXml();
        }
    }
}
