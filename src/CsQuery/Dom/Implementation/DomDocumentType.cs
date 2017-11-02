using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsQuery.Implementation
{

    /// <summary>
    /// A special type for the DOCTYPE node
    /// </summary>

    public class DomDocumentType : DomObject<DomDocumentType>, IDomDocumentType
    {

        #region constructors

        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomDocumentType()
            : base()
        {

        }

        /// <summary>
        /// Constructor to create based on one of several common predefined types.
        /// </summary>
        ///
        /// <param name="docType">
        /// Type of the document.
        /// </param>

        public DomDocumentType(DocType docType)
            : base()
        {
            SetDocType(docType);
        }

        /// <summary>
        /// Constructor to create a specific document type node.
        /// </summary>
        ///
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="access">
        /// PUBLIC or SYSTEM
        /// </param>
        /// <param name="FPI">
        /// Identifier for the system.
        /// </param>
        /// <param name="URI">
        /// URI of the document.
        /// </param>

        public DomDocumentType(string type, string access, string FPI, string URI)
            : base()
        {

            SetDocType(type, access, FPI, URI);
        }
        #endregion

        #region private properties

        private static Regex DocTypeRegex = new Regex(
                @"^\s*([a-zA-Z0-9]+)\s+[a-zA-Z]+(\s+""(.*?)"")*\s*$", 
            RegexOptions.IgnoreCase);
        private string DocTypeName { get; set; }
        private string Access {get; set;}
        private string FPI { get; set; }
        private string URI { get; set; }

        #endregion

        /// <summary>
        /// Gets the type of the node.
        /// </summary>

        public override NodeType NodeType
        {
            get { return NodeType.DOCUMENT_TYPE_NODE; }
        }

        /// <summary>
        /// The node (tag) name, in upper case. For DOC_TYPE nodes, this is always "DOCTYPE".
        /// </summary>

        public override string NodeName
        {
            get
            {
                return "DOCTYPE";
            }
        }

        /// <summary>
        /// Gets or sets the type of the document.
        /// </summary>

        public DocType DocType
        {
            get
            {
                if (_DocType == 0)
                {
                    throw new InvalidOperationException("The doc type has not been set.");
                }

                return _DocType;
            }
            protected set
            {
                _DocType = value;
            }
        }

        private DocType _DocType = 0;

        /// <summary>
        /// Gets or sets the information describing the content found in the tag that is not in standard
        /// attribute format.
        /// </summary>

        public string NonAttributeData
        {
            get
            {
                return DocTypeName +
                    (!String.IsNullOrEmpty(Access) ? " "+Access  : "") +
                    (!String.IsNullOrEmpty(FPI) ? " \"" + FPI + "\"" : "") +
                    (!String.IsNullOrEmpty(URI) ? " \"" + URI + "\"" : "");

            }
            set
            {
                string docTypeName="";
                string fpi="";
                string access="";
                string uri = "";

                MatchCollection matches = DocTypeRegex.Matches(value);
                if (matches.Count > 0)
                {
                    docTypeName = matches[0].Groups[1].Value;
                    if (matches[0].Groups.Count ==4 )
                    {
                        var grp = matches[0].Groups[3];
                        access = grp.Captures[0].Value;
                        if (grp.Captures.Count > 1)
                        {
                            fpi = grp.Captures[1].Value;
                            uri= grp.Captures[2].Value;
                        }
                    }
                }
              
                SetDocType(docTypeName,access,fpi,uri);
            }
        }

        private void SetDocType(string type, string access, string fpi, string uri)
        {
            DocTypeName = type.ToLower();
            Access = access == null ? "" : access.ToUpper();
            FPI = fpi ?? "";
            URI = uri ?? "";

            if (DocTypeName == null || DocTypeName != "html")
            {
                DocType = DocType.Unknown;
                return;
            }
            if (fpi == "" && uri=="")
            {
                Access = "";
                DocType = DocType.HTML5;
                return;
            }
            else if (FPI.IndexOf("html 4", StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                if (FPI.IndexOf("strict", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    DocType = DocType.HTML4Strict;
                }
                else
                {
                    DocType = DocType.HTML4;
                }
            }
            else if (FPI.IndexOf("xhtml", StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                if (FPI.IndexOf("strict", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    DocType = DocType.XHTMLStrict;
                }
                else
                {
                    DocType = DocType.XHTML;
                }
            }
            else
            {
                DocType = DocType.Unknown;
            }
        }

        /// <summary>
        /// Sets document type data values from a doc type
        /// </summary>

        private void SetDocType(DocType type)
        {
            _DocType = type;
            switch (type)
            {
                case DocType.Unknown:

                case DocType.HTML5:
                    DocTypeName = "html";
                    Access = null;
                    FPI= null;
                    URI = null;
                    break;
                case DocType.XHTML:
                    DocTypeName = "html";
                    Access= "PUBLIC";
                    FPI="-//W3C//DTD XHTML 1.0 Frameset//EN" ;
                    URI="http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd" ;
                    break;
                case DocType.HTML4:
                    DocTypeName = "html";
                    Access = "PUBLIC";
                    FPI="-//W3C//DTD HTML 4.01 Frameset//EN";
                    URI="http://www.w3.org/TR/html4/frameset.dtd";
                    break;
                case DocType.HTML4Strict:
                    DocTypeName="html";
                    Access="PUBLIC";
                    FPI="-//W3C//DTD HTML 4.01//EN" ;
                    URI="http://www.w3.org/TR/html4/strict.dtd";
                    break;
                case DocType.XHTMLStrict:
                    DocTypeName = "html";
                    Access= "PUBLIC";
                    FPI = "-//W3C//DTD XHTML 1.0 Strict//EN";
                    URI = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd";
                    break;
                default:
                    throw new NotImplementedException("Unimplemented doctype");
            }
        }

        private string _NonAttributeData = String.Empty;

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. It is possible
        /// for this value to be false but InnerTextAllowed to be true for elements which can have inner
        /// content, but no child HTML markup, such as &lt;textarea&gt; and &lt;script&gt;
        /// </summary>

        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this object has children.
        /// </summary>

        public override bool HasChildren
        {
            get { return false; }
        }

        #region interface Members

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public override DomDocumentType Clone()
        {
            DomDocumentType clone = new DomDocumentType();
            clone.FPI = FPI;
            clone.Access = Access;
            clone.URI = URI;
            
            clone.DocTypeName = DocTypeName;
            clone.DocType = DocType;
            return clone;
        }


        IDomNode IDomNode.Clone()
        {
            return Clone();
        }
        


        #endregion
    }
}
