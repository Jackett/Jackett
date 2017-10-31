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
using CsQuery.HtmlParser;

namespace CsQuery
{
    public partial class CQ
    {
        #region public methods

        /// <summary>
        /// Insert content, specified by the parameter, to the end of each element in the set of matched
        /// elements.
        /// </summary>
        ///
        /// <param name="content">
        /// One or more HTML strings to append.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(params string[] content)
        {
            return Append(MergeContent(content));
        }

        /// <summary>
        /// Insert the element, specified by the parameter, to the end of each element in the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to exclude.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(IDomObject element)
        {
            return Append(Objects.Enumerate(element));
        }

        /// <summary>
        /// Insert the sequence of elements, specified by the parameter, to the end of each element in
        /// the set of matched elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be excluded.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(IEnumerable<IDomObject> elements)
        {
            CQ ignoredOutput;
            return Append(elements, out ignoredOutput);
        }

        /// <summary>
        /// Appends a func.
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate to a function that returns an HTML string to insert at the end
        /// of each element in the set of matched elements. Receives the index position of the element in
        /// the set and the old HTML value of the element as arguments. Within the function, this refers
        /// to the current element in the set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(Func<int, string, string> func)
        {
            int index = 0;
            foreach (DomElement obj in Elements)
            {

                string val = func(index, obj.InnerHTML);
                obj.Cq().Append((string)val);
                index++;
            }
            return this;
        }

        /// <summary>
        /// Insert content, specified by the parameter, to the end of each element in the set of matched
        /// elements.
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate to a function that returns an IDomElement to insert at the end of each element in
        /// the set of matched elements. Receives the index position of the element in the set and the
        /// old HTML value of the element as arguments. Within the function, this refers to the current
        /// element in the set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(Func<int, string, IDomElement> func)
        {
            int index = 0;
            foreach (IDomElement obj in Elements)
            {
                IDomElement clientValue = func(index, obj.InnerHTML);
                obj.Cq().Append(clientValue);
                index++;
            }
            return this;
        }

        /// <summary>
        /// Insert content, specified by the parameter, to the end of each element in the set of matched
        /// elements.
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate to a function that returns a sequence of IDomElement objects to insert at the end
        /// of each element in the set of matched elements. Receives the index position of the element in
        /// the set and the old HTML value of the element as arguments. Within the function, this refers
        /// to the current element in the set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/append/
        /// </url>

        public CQ Append(Func<int, string, IEnumerable<IDomElement>> func)
        {
            int index = 0;
            foreach (IDomElement obj in Elements)
            {
                IEnumerable<IDomElement> val = func(index, obj.InnerHTML);
                obj.Cq().Append(val);
                index++;
            }
            return this;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Append each element passed by parameter to each element in the selection set. The inserted
        /// elements are returned.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be excluded.
        /// </param>
        /// <param name="insertedElements">
        /// A CQ object containing all the elements added.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        private CQ Append(IEnumerable<IDomObject> elements, out CQ insertedElements)
        {
            insertedElements = NewCqInDomain();
            bool first = true;

            // must copy the enumerable first, since this can cause
            // els to be removed from it if they move across a document boundary

            List<IDomObject> list = new List<IDomObject>(elements);

            foreach (var obj in Elements)
            {
                // Make sure they didn't really mean to add to a tbody or something
                IDomElement target = GetTrueTarget(obj);


                foreach (var e in list)
                {
                    IDomObject toInsert = first ? e : e.Clone();
                    target.AppendChild(toInsert);
                    insertedElements.SelectionSet.Add(toInsert);
                }
                first = false;
            }
            return this;
        }

        /// <summary>
        /// Deals with tbody as the target of appends.
        /// </summary>
        ///
        /// <param name="target">
        /// The true target.
        /// </param>
        ///
        /// <returns>
        /// Either the element itself, or the TBODY element if the target was a TABLE
        /// </returns>
        
        private IDomElement GetTrueTarget(IDomElement target)
        {
            //Special handling for tables: make sure we add to the TBODY
            IDomElement element = target;
            if (target.NodeNameID == HtmlData.tagTABLE)
            {
                bool addBody = false;
                if (target.HasChildren)
                {
                    IDomElement body = target.ChildElements.FirstOrDefault(item => item.NodeNameID == HtmlData.tagTBODY);
                    if (body != null)
                    {
                        element = body;
                    }
                    else if (target.FirstElementChild == null)
                    {
                        // Add a body if there are no elements in this table yet
                        addBody = true;
                    }
                    // default = leave it alone, they've already added elements. don't worry whether it's valid or not, 
                    // assume they know what they're doing.
                }
                else
                {
                    addBody = true;
                }
                if (addBody)
                {
                    element = Document.CreateElement("tbody");
                    target.AppendChild(element);
                }
            }
            return element;
        }

        #endregion
    }
}
