using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using CsQuery.Engine;
using CsQuery.Output;
using CsQuery.Implementation;
using System.Net;
using System.Reflection;

namespace CsQuery
{
    /// <summary>
    /// Global configuration and defaults
    /// </summary>

    public class CsQueryConfig
    {
        #region constructor 

        /// <summary>
        /// Default constructor; populates the CsQueryConfig object with system default options.
        /// </summary>

        public CsQueryConfig()
        {
            DynamicObjectType = typeof(JsObject);
            DomRenderingOptions =   DomRenderingOptions.QuoteAllAttributes;
            HtmlParsingOptions = HtmlParsingOptions.None;
            HtmlEncoder = HtmlEncoders.Basic;
            DocType = DocType.HTML5;
            GetOutputFormatter = GetDefaultOutputFormatter;
            DomIndexProvider = DomIndexProviders.Ranged;

        }

        #endregion

        #region private properties

        private DocType _DocType;
        private DomRenderingOptions _DomRenderingOptions;
        private HtmlParsingOptions _HtmlParsingOptions;
        private IOutputFormatter _OutputFormatter;
        private Func<IOutputFormatter>  _GetOutputFormatter;

        /// <summary>
        /// Internal to avoid Obsolete warning from DomRenderingOptions until we remove it
        /// </summary>

        private Type _DynamicObjectType;

        /// <summary>
        /// Creates an OutputFormatter using the default options &amp; encoder.
        /// </summary>
        ///
        /// <returns>
        /// The default output formatter.
        /// </returns>

        private IOutputFormatter GetDefaultOutputFormatter()
        {
            return OutputFormatters.Create(DomRenderingOptions, HtmlEncoder);
        }

        private IDomIndex GetDomIndex
        {
            get
            {
                return DomIndexProvider.GetDomIndex();
            }
        }


        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the DomIndexProvider, that creates instances of the DomIndex used for new documents.
        /// </summary>

        public IDomIndexProvider DomIndexProvider
        {
            get;
            set;
        }


        /// <summary>
        /// The default rendering options. These will be used when configuring a default OutputFormatter.
        /// Note that if the default OutputFormatter has been changed, this setting is not guaranteed to
        /// have any effect on output.
        /// </summary>

        public DomRenderingOptions DomRenderingOptions  {
            get {
                return _DomRenderingOptions;   
            }
            set {
                if (value.HasFlag(DomRenderingOptions.Default))
                {
                    throw new InvalidOperationException("The default DomRenderingOptions cannot contain DomRenderingOptions.Default");
                }
                _DomRenderingOptions = value;
            }
        }

        /// <summary>
        /// The default HTML parsing options. These will be used when parsing HTML without specifying any options. 
        /// </summary>

        public HtmlParsingOptions HtmlParsingOptions
        {
            get
            {
                return _HtmlParsingOptions;
            }
            set
            {
                if (value.HasFlag(HtmlParsingOptions.Default))
                {
                    throw new InvalidOperationException("The default HtmlParsingOptions cannot contain HtmlParsingOptions.Default");
                }
                _HtmlParsingOptions = value;
            }
        }

        /// <summary>
        /// The default HTML encoder.
        /// </summary>

        public IHtmlEncoder HtmlEncoder
        {
            get;
            set;
        }

        

        /// <summary>
        /// The default OutputFormatter. The GetOutputFormatter property can also be used to provide a
        /// new instance whenever a default OutputFormatter is requested; setting that property will
        /// supersede any existing value of this property.
        /// </summary>

        public IOutputFormatter OutputFormatter {
            get
            {
                if (GetOutputFormatter != null)
                {
                    return GetOutputFormatter();
                }
                else
                {
                    return _OutputFormatter;
                }
            }
            set
            {
                _OutputFormatter = value;
                _GetOutputFormatter = null;
            }
        }

        /// <summary>
        /// A delegate that returns a new instance of the default output formatter to use for rendering.
        /// The OutputFormatter property can also be used to return a single instance of a reusable
        /// IOutputFormatter object; setting that property will supersede any existing value of this
        /// property.
        /// </summary>

        public Func<IOutputFormatter> GetOutputFormatter
        {
            get
            {
                return _GetOutputFormatter;
            }
            set
            {
                _GetOutputFormatter = value;
                _OutputFormatter = null;
            }
        }

        /// <summary>
        /// Default document type. This is the parsing mode that will be used when creating documents
        /// that have no DocType and no mode is explicitly defined.
        /// </summary>

        public DocType DocType
        {
            get
            {
                return _DocType;
            }
            set
            {
                if (value == DocType.Default)
                {
                    throw new InvalidOperationException("The default DocType cannot be DocType.Default");
                }
                _DocType = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the default dynamic object type. This is the type of object used by default when
        /// parsing JSON into an unspecified type.
        /// </summary>

        public Type DynamicObjectType
        {
            get
            {
                return _DynamicObjectType;
            }
            set
            {
                if (value.GetTypeInfo().GetInterfaces()
                    .Count(item => item == typeof(IDynamicMetaObjectProvider) ||
                    item == typeof(IDictionary<string, object>)) == 2)
                {
                    _DynamicObjectType = value;
                }
                else
                {
                    throw new ArgumentException("The DynamicObjectType must inherit IDynamicMetaObjectProvider and IDictionary<string,object>. Example: ExpandoObject, or the built-in JsObject.");
                }
            }
        }


        #endregion

    }
}
