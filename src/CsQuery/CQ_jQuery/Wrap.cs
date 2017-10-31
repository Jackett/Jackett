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
        /// Wrap an HTML structure around each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrappingSelector">
        /// A string that is either a selector or a string of HTML that defines the structure to wrap
        /// around the set of matched elements.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrap/
        /// </url>

        public CQ Wrap(string wrappingSelector)
        {
            return Wrap(Select(wrappingSelector));
        }

        /// <summary>
        /// Wrap an HTML structure around each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="element">
        /// An element which is the structure to wrap around the selection set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrap/
        /// </url>

        public CQ Wrap(IDomObject element)
        {
            return Wrap(Objects.Enumerate(element));
        }

        /// <summary>
        /// Wrap an HTML structure around each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrapper">
        /// A sequence of elements that is the structure to wrap around the selection set. There may be
        /// multiple elements but there should be only one innermost element in the sequence.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrap/
        /// </url>

        public CQ Wrap(IEnumerable<IDomObject> wrapper)
        {
            return Wrap(wrapper, false);
        }

        /// <summary>
        /// Wrap an HTML structure around all elements in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrappingSelector">
        /// A string that is either a selector or a string of HTML that defines the structure to wrap
        /// around the set of matched elements.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapall/
        /// </url>

        public CQ WrapAll(string wrappingSelector)
        {
            return WrapAll(Select(wrappingSelector));
        }

        /// <summary>
        /// Wrap an HTML structure around all elements in the set of matched elements.
        /// </summary>
        ///
        /// <param name="element">
        /// An element which is the structure to wrap around the selection set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapall/
        /// </url>

        public CQ WrapAll(IDomObject element)
        {
            return WrapAll(Objects.Enumerate(element));
        }

        /// <summary>
        /// Wrap an HTML structure around all elements in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrapper">
        /// A sequence of elements that is the structure to wrap around each element in the selection
        /// set. There may be multiple elements but there should be only one innermost element in the
        /// sequence.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapall/
        /// </url>

        public CQ WrapAll(IEnumerable<IDomObject> wrapper)
        {
            return Wrap(wrapper, true);
        }
        /// <summary>
        /// Remove the parents of the set of matched elements from the DOM, leaving the matched elements
        /// in their place.
        /// </summary>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/unwrap/
        /// </url>

        public CQ Unwrap()
        {
            HashSet<IDomObject> parents = new HashSet<IDomObject>();

            // Start with a unique list of parents instead of working with the siblings
            // to avoid repetition and unwrapping more than once for multiple siblings from
            // a single parent

            foreach (IDomObject obj in SelectionSet)
            {
                if (obj.ParentNode != null)
                {
                    parents.Add(obj.ParentNode);
                }
            }
            foreach (IDomObject obj in parents)
            {
                var csq = obj.Cq();
                csq.ReplaceWith(csq.Contents());

            }
            //Order = SelectionSetOrder.Ascending;
            return this;
        }

        /// <summary>
        /// Wrap an HTML structure around the content of each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="selector">
        /// An HTML snippet or elector expression specifying the structure to wrap around the content of
        /// the matched elements.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapinner/
        /// </url>

        public CQ WrapInner(string selector)
        {
            return WrapInner(Select(selector));
        }

        /// <summary>
        /// Wrap an HTML structure around the content of each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrapper">
        /// A sequence of elements that is the structure to wrap around the content of the selection set.
        /// There may be multiple elements but there should be only one innermost element in the sequence.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapinner/
        /// </url>

        public CQ WrapInner(IDomObject wrapper)
        {
            return WrapInner(Objects.Enumerate(wrapper));
        }

        /// <summary>
        /// Wrap an HTML structure around the content of each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="wrapper">
        /// A sequence of elements that is the structure to wrap around the content of the selection set.
        /// There may be multiple elements but there should be only one innermost element in the sequence.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/wrapinner/
        /// </url>

        public CQ WrapInner(IEnumerable<IDomObject> wrapper)
        {
            foreach (var el in Elements)
            {
                var self = el.Cq();
                var contents = self.Contents();
                if (contents.Length > 0)
                {
                    contents.WrapAll(wrapper);
                }
                else
                {
                    self.Append(wrapper);
                }
            }
            return this;
        }

        #endregion

        #region private methods

        private CQ Wrap(IEnumerable<IDomObject> wrapper, bool keepSiblingsTogether)
        {
            // get innermost structure
            CQ wrapperTemplate = EnsureCsQuery(wrapper);
            IDomElement wrappingEl = null;
            IDomElement wrappingElRoot = null;

            int depth = GetInnermostContainer(wrapperTemplate.Elements, out wrappingEl, out wrappingElRoot);

            if (wrappingEl != null)
            {
                IDomObject nextEl = null;
                IDomElement innerEl = null;
                IDomElement innerElRoot = null;

                foreach (IDomObject el in SelectionSet)
                {

                    if (nextEl == null
                        || (!ReferenceEquals(nextEl, el)) &&
                            !keepSiblingsTogether)
                    {
                        var template = wrappingElRoot.Cq().Clone();
                        if (el.ParentNode != null)
                        {
                            template.InsertBefore(el);
                        }
                        // This will always succceed because we tested before this loop. But we need
                        // to run it again b/c it's a clone now
                        GetInnermostContainer(template.Elements, out innerEl, out innerElRoot);
                    }
                    nextEl = el.NextSibling;
                    innerEl.AppendChild(el);

                }
            }
            return this;
        }

        /// <summary>
        /// Ouptuts the deepest-nested object, it's root element from the list of elements passed, and
        /// returns the depth, given a structure. Helper method for Wrap.
        /// </summary>
        ///
        /// <param name="elements">
        /// The sequence to analyze
        /// </param>
        /// <param name="element">
        /// [ouy] The innermost element container
        /// </param>
        /// <param name="rootElement">
        /// [out] The root element.
        /// </param>
        ///
        /// <returns>
        /// The innermost container.
        /// </returns>

        protected int GetInnermostContainer(IEnumerable<IDomElement> elements,
            out IDomElement element,
            out IDomElement rootElement)
        {
            int depth = 0;
            element = null;
            rootElement = null;
            foreach (IDomElement el in elements)
            {
                if (el.HasChildren)
                {
                    IDomElement innerEl;
                    IDomElement root;
                    int innerDepth = GetInnermostContainer(el.ChildElements,
                        out innerEl,
                        out root);
                    if (innerDepth > depth)
                    {
                        depth = innerDepth + 1;
                        element = innerEl;
                        rootElement = el;
                    }
                }
                if (depth == 0)
                {
                    depth = 1;
                    element = el;
                    rootElement = el;
                }
            }
            return depth;
        }
        #endregion

    }
}
