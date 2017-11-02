using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using CsQuery.Engine;
using CsQuery.Output;
using CsQuery.Implementation;
using System.Net;

namespace CsQuery
{
    /// <summary>
    /// Global configuration and defaults
    /// </summary>

    public static class Config
    {
        #region constructor 
    
        static Config()
        {
            DefaultConfig = new CsQueryConfig();
        }

        #endregion

        #region private properties

        private static CsQueryConfig DefaultConfig;

        #endregion

        #region public properties

        /// <summary>
        /// The default startup options. These are flags. 
        /// </summary>

        public static StartupOptions StartupOptions = StartupOptions.LookForExtensions;

        /// <summary>
        /// Provides access to the PseudoSelectors object, which allows registering new filters and
        /// accessing information and instances about existing filters.
        /// </summary>
        ///
        /// <value>
        /// The pseudo PseudoSelectors configuration object.
        /// </value>

        public static PseudoSelectors PseudoClassFilters
        {
            get {
                return PseudoSelectors.Items;
            }
        }
        
        /// <summary>
        /// The default rendering options. These will be used when configuring a default OutputFormatter.
        /// Note that if the default OutputFormatter has been changed, this setting is not guaranteed to
        /// have any effect on output.
        /// </summary>

        public static DomRenderingOptions DomRenderingOptions  {
            get {
                return DefaultConfig.DomRenderingOptions;
            }
            set {
                DefaultConfig.DomRenderingOptions = value;
            }
        }

        /// <summary>
        /// The default HTML parsing options. These will be used when parsing HTML without specifying any options. 
        /// </summary>

        public static HtmlParsingOptions HtmlParsingOptions
        {
            get
            {
                return DefaultConfig.HtmlParsingOptions;
            }
            set
            {
                DefaultConfig.HtmlParsingOptions = value;
            }
        }

        /// <summary>
        /// The default HTML encoder.
        /// </summary>

        public static IHtmlEncoder HtmlEncoder
        {
            get
            {
                return DefaultConfig.HtmlEncoder;
            }
            set
            {
                DefaultConfig.HtmlEncoder = value;
            }
        }

        

        /// <summary>
        /// The default OutputFormatter. The GetOutputFormatter property can also be used to provide a
        /// new instance whenever a default OutputFormatter is requested; setting that property will
        /// supersede any existing value of this property.
        /// </summary>

        public static IOutputFormatter OutputFormatter {
            get
            {
                return DefaultConfig.OutputFormatter;
            }
            set
            {
                DefaultConfig.OutputFormatter = value;
            }
        }

        /// <summary>
        /// A delegate that returns a new instance of the default output formatter to use for rendering.
        /// The OutputFormatter property can also be used to return a single instance of a reusable
        /// IOutputFormatter object; setting that property will supersede any existing value of this
        /// property.
        /// </summary>

        public static Func<IOutputFormatter> GetOutputFormatter
        {
            get
            {
                return DefaultConfig.GetOutputFormatter;
            }
            set
            {
                DefaultConfig.GetOutputFormatter = value;
            }
        }


        /// <summary>
        /// Default document type. This is the parsing mode that will be used when creating documents
        /// that have no DocType and no mode is explicitly defined.
        /// </summary>

        public static DocType DocType
        {
            get
            {
                return DefaultConfig.DocType;
            }
            set
            {
                DefaultConfig.DocType = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the default dynamic object type. This is the type of object used by default when
        /// parsing JSON into an unspecified type.
        /// </summary>

        public static Type DynamicObjectType
        {
            get
            {
                return DefaultConfig.DynamicObjectType;
            }
            set
            {
                DefaultConfig.DynamicObjectType = value;
            }
        }

        /// <summary>
        /// Gets or sets the default DomIndexProvider, which returns an instance of a DomIndex that
        /// defines the indexing strategy for new documents.
        /// </summary>

        public static IDomIndexProvider DomIndexProvider
        {
            get
            {
                return DefaultConfig.DomIndexProvider;
            }
            set
            {
                DefaultConfig.DomIndexProvider = value;
            }
        }
        #endregion

    }
}
