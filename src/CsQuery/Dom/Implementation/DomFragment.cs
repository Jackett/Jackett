using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.ExtensionMethods;
using CsQuery.HtmlParser;
using CsQuery.Engine;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An incomplete document fragment
    /// </summary>
    public class DomFragment : DomDocument, IDomFragment
    {
        /// <summary>
        /// Creates a new fragment in a given context.
        /// </summary>
        ///
        /// <param name="html">
        /// The elements.
        /// </param>
        /// <param name="context">
        /// (optional) the context. If omitted, will be automatically determined.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>
        ///
        /// <returns>
        /// A new fragment.
        /// </returns>

        public static IDomDocument Create(string html,
           string context=null,
           DocType docType = DocType.Default)
        {
            var factory = new ElementFactory();
            factory.FragmentContext = context;
            factory.HtmlParsingMode = HtmlParsingMode.Fragment;
            factory.HtmlParsingOptions = HtmlParsingOptions.AllowSelfClosingTags;
            factory.DocType = docType;

            Encoding encoding = Encoding.UTF8;
            using (var stream = new MemoryStream(encoding.GetBytes(html)))
            {
                return factory.Parse(stream, encoding);
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomFragment()
            : base()
        {
        }

        /// <summary>
        /// Create a new DomFragment using the provided DomIndex instance.
        /// </summary>
        ///
        /// <param name="domIndex">
        /// A DomIndex provider
        /// </param>

        public DomFragment(IDomIndex domIndex)
            : base(domIndex)
        {
            
        }

        /// <summary>
        /// Gets the type of the node. For DomFragment objects, this is always NodeType.DOCUMENT_FRAGMENT_NODE.
        /// </summary>

        public override NodeType NodeType
        {
            get { return  NodeType.DOCUMENT_FRAGMENT_NODE; }
        }

        /// <summary>
        /// Gets a value indicating whether this object is indexed. 
        /// </summary>

        public override bool IsIndexed
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is fragment. For DomFragment objects, this is
        /// true.
        /// </summary>

        public override bool IsFragment
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Creates a new instance of a DomFragment.
        /// </summary>
        ///
        /// <returns>
        /// The new new.
        /// </returns>

        public override IDomDocument CreateNew()
        {
            return CreateNew<IDomFragment>();
        }
    }
    
}
