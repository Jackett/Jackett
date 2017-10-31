using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using HtmlParserSharp.Core;
using HtmlParserSharp.Common;
using CsQuery;
using CsQuery.Implementation;
using CsQuery.Utility;
using CsQuery.StringScanner;
using CsQuery.HtmlParser;
using CsQuery.Engine;
using HtmlParserSharp;

namespace CsQuery.HtmlParser
{
    /// <summary>
    /// Element factory to build a CsQuery DOM using HtmlParserSharp.
    /// </summary>

    public class ElementFactory
    {
        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>

        static ElementFactory()
        {
            ConfigureDefaultContextMap();
        }

        /// <summary>
        /// Default constructor, creates a factory with the default DomIndexProvider
        /// </summary>

        public ElementFactory(): this(Config.DomIndexProvider) 
        {
        }

        /// <summary>
        /// Creates a factory using the DomIndexProvider passed by parameter
        /// </summary>
        ///
        /// <param name="domIndexProvider">
        /// The DomIndexProvider that will be used when creating new DomDocument objects from this factory.
        /// </param>

        public ElementFactory(IDomIndexProvider domIndexProvider)
        {
            DomIndexProvider = domIndexProvider;
        }

        #endregion

        #region static methods

        /// <summary>
        /// Creates a new document from a Stream of HTML using the options passed.
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML input.
        /// </param>
        /// <param name="streamEncoding">
        /// The character set encoding used by the stream. If null, the BOM will be inspected, and it
        /// will default to UTF8 if no encoding can be identified.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>
        ///
        /// <returns>
        /// A new document.
        /// </returns>

        public static IDomDocument Create(Stream html, 
            Encoding streamEncoding,
            HtmlParsingMode parsingMode = HtmlParsingMode.Auto,
            HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            
            return GetNewParser(parsingMode, parsingOptions, docType)
                .Parse(html, streamEncoding);
            
        }

        private static ElementFactory GetNewParser()
        {
            return new ElementFactory();
        }

        private static ElementFactory GetNewParser(HtmlParsingMode parsingMode, HtmlParsingOptions parsingOptions, DocType docType)
        {
            var parser = new ElementFactory();
            parser.HtmlParsingMode = parsingMode;
            parser.DocType = GetDocType(docType);
            parser.HtmlParsingOptions = MergeOptions(parsingOptions);
            return parser;
        }


        #endregion

        #region private properties

        /// <summary>
        /// Size of the blocks to read from the input stream (char[] = 2x bytes)
        /// </summary>
        private const int tokenizerBlockChars = 2048;

        /// <summary>
        /// Size of the preprocessor block; the maximum number of bytes in which the character set
        /// encoding can be changed. This must be at least as large (IN BYTES!) as the tokenizer block or the
        /// tokenizer won't quit before moving outside the preprocessor block.
        /// </summary>

        private const int preprocessorBlockBytes = 4096;

        private static IDictionary<string, string> DefaultContext;
        private Tokenizer tokenizer;
        private IDomIndexProvider DomIndexProvider;
        private CsQueryTreeBuilder treeBuilder;

        private enum ReEncodeAction
        {
            None = 0,
            ReEncode = 1,
            /// <summary>
            /// The encoding was set from a META tag, allow it to be changed.
            /// </summary>
            ChangeEncoding = 2
            

        }

        /// <summary>
        /// When true, the document's character set encoding has changed due to a meta http-equiv
        /// directive. This can only happen once. After this we will change the encoding of the stream
        /// from that point forward only.
        /// </summary>

        private bool AlreadyReEncoded;

        /// <summary>
        /// This flag can be set during parsing if the character set encoding found in a meta tag is
        /// different than the stream's current encoding.
        /// </summary>

        private ReEncodeAction ReEncode;

        /// <summary>
        /// The active stream.
        /// </summary>

        private Stream ActiveStream;

        /// <summary>
        /// The active stream reader.
        /// </summary>

        private TextReader ActiveStreamReader;

        /// <summary>
        /// The active encoding.
        /// </summary>

        private Encoding ActiveEncoding;

        private int ActiveStreamOffset;

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the HTML parsing mode.
        /// </summary>

        public HtmlParsingMode HtmlParsingMode
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the HTML parsing mode.
        /// </summary>

        public HtmlParsingOptions HtmlParsingOptions
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the type of the document.
        /// </summary>

        public DocType DocType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a context for the fragment, e.g. a tag name
        /// </summary>

        public string FragmentContext
        {
            get;
            set;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Given a TextReader, create a new IDomDocument from the input.
        /// </summary>
        ///
        /// <exception cref="InvalidDataException">
        /// Thrown when an invalid data error condition occurs.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="inputStream">
        /// The HTML input.
        /// </param>
        /// <param name="encoding">
        /// The encoding.
        /// </param>
        ///
        /// <returns>
        /// A populated IDomDocument.
        /// </returns>

        public IDomDocument Parse(Stream inputStream, Encoding encoding)
        {
            ActiveStream = inputStream;
            ActiveEncoding = encoding;

           // split into two streams so we can restart if needed
           // without having to re-parse the entire stream.

            byte[] part1bytes = new byte[preprocessorBlockBytes];
            int part1size = inputStream.Read(part1bytes, 0, preprocessorBlockBytes);

            MemoryStream part1stream = new MemoryStream(part1bytes, 0, part1size);
                 
            if (part1stream.Length==0)
            {
                return new DomFragment();
            }


        
            // create a combined stream from the pre-fetched part, and the remainder (whose position
            // will be wherever it was left after reading the part 1 block).
            
            Stream stream;

            // The official order of precedence for character set processing is as follows:
            //
            // HTTP Content-Type header
            // byte-order mark (BOM)
            // XML declaration
            // meta element
            // link charset attribute
            // 
            // http://www.w3.org/International/questions/qa-html-encoding-declarations#precedence
            //
            // Chrome does this:
            // 
            // A UTF-16 or UTF-8 BOM overrides the HTTP declaration for Internet Explorer, Safari and Chrome browsers.
            //
            // We act like chrome.

            
            var bomReader = new BOMReader(part1stream);
            
            if (bomReader.IsBOM) {
                
                // if there is a BOM encoding, and there's either no active encoding specified already, or it's utf-8/utf-16
                // then use it.

                var bomEncoding = bomReader.Encoding;

                if (ActiveEncoding == null ||
                    (bomReader.Encoding != null && 
                        (bomReader.Encoding.WebName == "utf-8" || bomReader.Encoding.WebName == "utf-16")
                    )
                )
                {
                    ActiveEncoding = bomReader.Encoding;
                }
                
                // either way strip the BOM.
                
                stream = new CombinedStream(bomReader.StreamWithoutBOM, inputStream);
            }
            else
            {
                // no BOM, just reset the input stream
                
                part1stream.Position = 0;
                stream = new CombinedStream(part1stream, inputStream);
            }

            ActiveStreamReader = new StreamReader(stream, ActiveEncoding ?? Encoding.UTF8, false);

            if (HtmlParsingMode == HtmlParsingMode.Auto || 
                ((HtmlParsingMode == HtmlParsingMode.Fragment )
                    && String.IsNullOrEmpty(FragmentContext)))
            {

                string ctx;
                ActiveStreamReader = GetContextFromStream(ActiveStreamReader, out ctx);

                if (HtmlParsingMode == HtmlParsingMode.Auto)
                {
                    switch (ctx)
                    {
                        case "document":
                            HtmlParsingMode = HtmlParsingMode.Document;
                            ctx = "";
                            break;
                        case "html":
                            HtmlParsingMode = HtmlParsingMode.Content;
                            break;
                        default:
                            HtmlParsingMode = HtmlParsingMode.Fragment;
                            HtmlParsingOptions = HtmlParsingOptions.AllowSelfClosingTags;
                            break;
                    }
                }

                if (HtmlParsingMode == HtmlParsingMode.Fragment) 
                {
                    FragmentContext = ctx;
                }
            }

            Reset();

            Tokenize();

            // If the character set was declared within the first block

            if (ReEncode == ReEncodeAction.ReEncode)
            {

                AlreadyReEncoded = true;

                if (ActiveStreamOffset >= preprocessorBlockBytes)
                {
                    // this should never happen, since we test this when accepting an alternate encoding and should
                    // have already decided to change the encoding midstream instead of restart. But as a failsafe
                    // in case there's some part of the parser abort sequence I don't understand, just switch
                    // midstream if we end up here for some reason to keep things going. 
                    
                    ActiveStreamReader = new StreamReader(ActiveStream, ActiveEncoding);
                }
                else
                {

                    part1stream = new MemoryStream(part1bytes);

                    // if the 2nd stream has already been closed, then the whole thing is less than the
                    // preprocessor block size; just restart the cached stream..

                    if (inputStream.CanRead)
                    {
                        stream = new CombinedStream(part1stream, inputStream);
                    }
                    else
                    {
                        stream = part1stream;
                    }

                    // assign the re-mapped stream to the source and start again
                    ActiveStreamReader = new StreamReader(stream, ActiveEncoding);
                }

                Reset();
                Tokenize();

            }

            // set this before returning document to the client to improve performance during DOM alteration

            IDomIndexQueue indexQueue = treeBuilder.Document.DocumentIndex as IDomIndexQueue;
            if (indexQueue!=null)
            {
                indexQueue.QueueChanges = true;
            }
            

            return treeBuilder.Document;
        }


        #endregion

        #region private methods

        private static HtmlParsingOptions MergeOptions(HtmlParsingOptions options)
        {
            if (options.HasFlag(HtmlParsingOptions.Default))
            {
                return CsQuery.Config.HtmlParsingOptions | options & ~(HtmlParsingOptions.Default);
            }
            else
            {
                return options;
            }
        }
        private static DocType GetDocType(DocType docType)
        {
            return docType == DocType.Default ? Config.DocType : docType;
        }

        private void ConfigureTreeBuilderForParsingMode()
        {
            
            switch (HtmlParsingMode)
            {

                case HtmlParsingMode.Document:
                    treeBuilder.DoctypeExpectation = DoctypeExpectation.Auto;
                    break;
                case HtmlParsingMode.Content:
                    treeBuilder.SetFragmentContext("body");
                    treeBuilder.DoctypeExpectation = DoctypeExpectation.Html;
                    break;
                case HtmlParsingMode.Fragment:
                    treeBuilder.DoctypeExpectation = DoctypeExpectation.Html;
                    treeBuilder.SetFragmentContext(FragmentContext);
                    HtmlParsingMode = HtmlParsingMode.Auto;
                    break;
            }

            
        }

        private static void SetDefaultContext(string tags, string context)
        {
            var tagList = tags.Split(',');
            foreach (var tag in tagList)
            {
                DefaultContext[tag.Trim()] = context;
            }
        }

        /// <summary>
        /// Gets a default context for a tag
        /// </summary>
        ///
        /// <param name="tag">
        /// The tag.
        /// </param>
        ///
        /// <returns>
        /// The context.
        /// </returns>

        private string GetContext(string tag)
        {
            string context;
            if (DefaultContext.TryGetValue(tag, out context))
            {
                return context;
            }
            else
            {
                return "body";
            }
        }

        /// <summary>
        /// Gets a context by inspecting the beginning of a stream. Will restore the stream to its
        /// unaltered state.
        /// </summary>
        ///
        /// <param name="reader">
        /// The HTML input.
        /// </param>
        /// <param name="context">
        /// [out] The context (e.g. the valid parent of the first tag name found).
        /// </param>
        ///
        /// <returns>
        /// The a new TextReader which is a clone of the original.
        /// </returns>

        private TextReader GetContextFromStream(TextReader reader, out string context)
        {
            
            int pos = 0;
            string tag = "";
            string readSoFar = "";
            int mode=0;
            char[] buf = new char[1];
            bool finished = false;

            while (!finished && reader.Read(buf,0,1)>0)
            {
                
                char cur = buf[0];
                readSoFar += cur;

                switch(mode) {
                    case 0:
                        if (cur=='<') {
                            mode=1;
                        }
                        break;
                    case 1:
                        if (CharacterData.IsType(cur, CharacterType.HtmlTagOpenerEnd))
                        {
                            finished = true;
                            break;
                        }
                        tag += cur;
                        break;
                }
                pos++;
            }
            context = GetContext(tag);
            return new CombinedTextReader(new StringReader(readSoFar), reader);
        }

        private void InitializeTreeBuilder()
        {
            treeBuilder = new CsQueryTreeBuilder(DomIndexProvider);

            treeBuilder.NamePolicy = XmlViolationPolicy.Allow;
            treeBuilder.WantsComments = !HtmlParsingOptions.HasFlag(HtmlParsingOptions.IgnoreComments);
            treeBuilder.AllowSelfClosingTags = HtmlParsingOptions.HasFlag(HtmlParsingOptions.AllowSelfClosingTags);

            // DocTypeExpectation should be set later depending on fragment/content/document selection


        }
        private void Reset()
        {
            InitializeTreeBuilder();

            tokenizer = new Tokenizer(treeBuilder, false);
            tokenizer.EncodingDeclared += tokenizer_EncodingDeclared;
            ReEncode = ReEncodeAction.None;

            // optionally: report errors and more

            //treeBuilder.ErrorEvent +=
            //    (sender, a) =>
            //    {
            //        ILocator loc = tokenizer as ILocator;
            //        Console.WriteLine("{0}: {1} (Line: {2})", a.IsWarning ? "Warning" : "Error", a.Message, loc.LineNumber);
            //    };
            //treeBuilder.DocumentModeDetected += (sender, a) => Console.WriteLine("Document mode: " + a.Mode.ToString());
            //tokenizer.EncodingDeclared += (sender, a) => Console.WriteLine("Encoding: " + a.Encoding + " (currently ignored)");
        }


        /// <summary>
        /// Event is called by the tokenizer when a content-encoding meta tag is found. We should just always return true.
        /// </summary>
        ///
        /// <param name="sender">
        /// The tokenizer
        /// </param>
        /// <param name="e">
        /// Encoding detected event information.
        /// </param>

        private void tokenizer_EncodingDeclared(object sender, EncodingDetectedEventArgs e)
        {



            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(e.Encoding);
            }
            catch
            {
                // when an invalid encoding is detected just ignore.
                return;
            }


            bool accept = false;

            // only accept new encodings from meta if there has been no encoding identified already

            if (encoding != null && 
                ActiveEncoding==null)
            {
                accept = true;
                ActiveEncoding = encoding;

                // when CanRead & CanSeek then it means we can and should restart the stream. If outside of 1K
                // then it's illegal, don't actually restart, just change encoding midstream. 

                if (!AlreadyReEncoded && 
                     ActiveStreamOffset < preprocessorBlockBytes)
                {
                    ReEncode = ReEncodeAction.ReEncode;
                    accept = true;
                }
                else
                {
                    ReEncode = ReEncodeAction.ChangeEncoding;
                    accept = false;
                }
            }
            e.AcceptEncoding = accept;
        }
        
        private void Tokenize()
        {
            if (ActiveStreamReader == null)
            {
                throw new ArgumentNullException("reader was null.");
            }

            ConfigureTreeBuilderForParsingMode();
            tokenizer.Start();

            bool swallowBom = true;


            try
            {
                char[] buffer = new char[tokenizerBlockChars];
                UTF16Buffer bufr = new UTF16Buffer(buffer, 0, 0);
                bool lastWasCR = false;
                int len = -1;
                if ((len = ActiveStreamReader.Read(buffer, 0, buffer.Length)) != 0)
                {
                    
                    int offset = 0;
                    int length = len;
                    if (swallowBom)
                    {
                        if (buffer[0] == '\uFEFF')
                        {
                            ActiveStreamOffset = -1;
                            offset = 1;
                            length--;
                        }
                    }
                    if (length > 0)
                    {
                        tokenizer.SetTransitionBaseOffset(ActiveStreamOffset);
                        bufr.Start = offset;
                        bufr.End = offset + length;
                        while (bufr.HasMore && !tokenizer.IsSuspended)
                        {
                            bufr.Adjust(lastWasCR);
                            lastWasCR = false;
                            if (bufr.HasMore && !tokenizer.IsSuspended)
                            {
                                lastWasCR = tokenizer.TokenizeBuffer(bufr);
                            }
                        }
                    }

                    CheckForReEncode();

                    ActiveStreamOffset = length;
                    while (!tokenizer.IsSuspended && (len = ActiveStreamReader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        tokenizer.SetTransitionBaseOffset(ActiveStreamOffset);
                        bufr.Start = 0;
                        bufr.End = len;
                        while (bufr.HasMore && !tokenizer.IsSuspended)
                        {
                            bufr.Adjust(lastWasCR);
                            lastWasCR = false;
                            if (bufr.HasMore && !tokenizer.IsSuspended)
                            {
                                lastWasCR = tokenizer.TokenizeBuffer(bufr);
                            }
                        }
                        ActiveStreamOffset += len;
                        CheckForReEncode();
                    }
                }
                if (!tokenizer.IsSuspended)
                {
                    tokenizer.Eof();
                }
            }
            finally
            {
                tokenizer.End();
            }
        }

        /// <summary>
        /// If a new character set encoding was declared and it's too late to change, switch to the new
        /// one midstream.
        /// </summary>

        private void CheckForReEncode()
        {
            if (ReEncode == ReEncodeAction.ChangeEncoding)
            {
                ActiveStreamReader = new StreamReader(ActiveStream, ActiveEncoding);
                ReEncode = ReEncodeAction.None;
            }
        }

        /// <summary>
        /// Configure default context: creates a default context for arbitrary fragments so they are valid no matter what, 
        /// so that true fragments can be created without concern for the context
        /// </summary>

        private static void ConfigureDefaultContextMap()
        {
            DefaultContext = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            SetDefaultContext("tbody,thead,tfoot,colgroup,caption", "table");
            SetDefaultContext("col", "colgroup");
            SetDefaultContext("tr", "tbody");
            SetDefaultContext("td,th", "tr");

            SetDefaultContext("option,optgroup", "select");

            SetDefaultContext("dt,dd", "dl");
            SetDefaultContext("li", "ol");

            SetDefaultContext("meta", "head");
            SetDefaultContext("title", "head");
            SetDefaultContext("head", "html");

            // pass these through; they will dictate high-level parsing mode
            
            SetDefaultContext("html", "document");
            SetDefaultContext("!doctype", "document");
            SetDefaultContext("body", "html");

        }
        #endregion

    }
}
