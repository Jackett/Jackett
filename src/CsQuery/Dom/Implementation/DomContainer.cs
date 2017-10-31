using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.ExtensionMethods;

namespace CsQuery.Implementation
{
    

    /// <summary>
    /// Base class for Dom object that contain other elements
    /// </summary>
    public abstract class DomContainer<T> : DomObject<T>, IDomContainer where T : IDomObject, IDomContainer, new()
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomContainer()
        {
            
        }

        /// <summary>
        /// Constructor that populates the container with the passed elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements.
        /// </param>

        public DomContainer(IEnumerable<IDomObject> elements): base()
        {
            ChildNodesInternal.AddRange(elements);
        }


        /// <summary>
        /// Returns all children (including inner HTML as objects);
        /// </summary>
        public override INodeList ChildNodes
        {
            get
            {
                return ChildNodesInternal;
            }
        }

        /// <summary>
        /// The child nodes as a concete object.
        /// </summary>

        protected ChildNodeList ChildNodesInternal
        {
            get
            {
                if (_ChildNodes == null)
                {
                    _ChildNodes = new ChildNodeList(this);
                }
                return _ChildNodes;
            }
        }

        private ChildNodeList _ChildNodes;

        /// <summary>
        /// Gets a value indicating whether this object has children.
        /// </summary>

        public override bool HasChildren
        {
            get
            {
                return ChildNodesInternal != null && ChildNodes.Count > 0;
            }
        }

        /// <summary>
        /// Returns the node's first child in the tree, or null if the node is childless. If the node is
        /// a Document, it returns the first node in the list of its direct children.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.firstChild
        /// </url>

        public override IDomObject FirstChild
        {
            get
            {
                if (HasChildren)
                {
                    return ChildNodes[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the element's first child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.firstElementChild
        /// </url>

        public override IDomElement FirstElementChild
        {
            get
            {
                if (HasChildren)
                {
                    int index=0;
                    while (index < ChildNodes.Count && ChildNodes[index].NodeType != NodeType.ELEMENT_NODE)
                    {
                        index++;
                    }
                    if (index < ChildNodes.Count)
                    {
                        return (IDomElement)ChildNodes[index];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the last child of a node.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.lastChild
        /// </url>

        public override IDomObject LastChild
        {
            get
            {
                return HasChildren ?
                    ChildNodes[ChildNodes.Count - 1] :
                    null;
            }
        }

        /// <summary>
        /// Returns the element's last child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.lastElementChild
        /// </url>

        public override IDomElement LastElementChild
        {
            get
            {
                if (HasChildren)
                {
                    int index = ChildNodes.Count-1;
                    while (index >=0 && ChildNodes[index].NodeType != NodeType.ELEMENT_NODE)
                    {
                        index--;
                    }
                    if (index >=0)
                    {
                        return (IDomElement)ChildNodes[index];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Appends a child.
        /// </summary>
        ///
        /// <param name="item">
        /// The element to append.
        /// </param>

        public override void AppendChild(IDomObject item)
        {
            ChildNodes.Add(item);
        }

        /// <summary>
        /// Appends a child without checking if it already exists. This should only be used during DOM
        /// construction.
        /// </summary>
        ///
        /// <param name="item">
        /// The element to append.
        /// </param>

        internal override void AppendChildUnsafe(IDomObject item)
        {
            ChildNodesInternal.AddAlways(item);
        }

        /// <summary>
        /// Removes the child.
        /// </summary>
        ///
        /// <param name="item">
        /// The element to remove.
        /// </param>

        public override void RemoveChild(IDomObject item)
        {
            ChildNodes.Remove(item);
        }

        /// <summary>
        /// Inserts the new node before a reference node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the reference node isn't a child of this node.
        /// </exception>
        ///
        /// <param name="newNode">
        /// The new node.
        /// </param>
        /// <param name="referenceNode">
        /// The reference node.
        /// </param>

        public override void InsertBefore(IDomObject newNode, IDomObject referenceNode)
        {
            if (referenceNode.ParentNode != this)
            {
                throw new InvalidOperationException("The reference node is not a child of this node");
            }
            ChildNodes.Insert(referenceNode.Index, newNode);
        }

        /// <summary>
        /// Inserts a new node after a reference node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the reference node isn't a child of this node.
        /// </exception>
        ///
        /// <param name="newNode">
        /// The new node.
        /// </param>
        /// <param name="referenceNode">
        /// The reference node.
        /// </param>

        public override void InsertAfter(IDomObject newNode, IDomObject referenceNode)
        {
            if (referenceNode.ParentNode != this)
            {
                throw new InvalidOperationException("The reference node is not a child of this node");
            }
            ChildNodes.Insert(referenceNode.Index + 1, newNode);
        }

        /// <summary>
        /// Get all child elements
        /// </summary>

        public override IEnumerable<IDomElement> ChildElements
        {
            get
            {
                if (HasChildren)
                {
                    foreach (IDomObject obj in ChildNodes)
                    {
                        var elm = obj as IDomElement;
                        if (elm != null)
                        {
                            yield return elm;
                        }
                    }
                }
                yield break;
            }
        }

        /// <summary>
        /// Gets the number of descendants of this element.
        /// </summary>
        ///
        /// <returns>
        /// An integer.
        /// </returns>

        public override int DescendantCount()
        {
            int count = 0;
            if (HasChildren)
            {
                foreach (IDomObject obj in ChildNodes)
                {
                    count += 1 + obj.DescendantCount();
                }
            }
            return count;
        }


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

        public override IDomObject this[int index]
        {
            get
            {
                return ChildNodes[index];
            }
        }

        #region interface members
        IDomObject IDomObject.Clone()
        {
            return Clone();
        }

        IDomNode IDomNode.Clone()
        {
            return Clone();
        }

        #endregion
    }

}
