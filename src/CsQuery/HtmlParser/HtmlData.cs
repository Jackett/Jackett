using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CsQuery.StringScanner;

namespace CsQuery.HtmlParser
{
    /// <summary>
    /// Reference data about HTML tags and attributes;
    /// methods to test tokens for certain properties;
    /// and the tokenizer.
    /// </summary>

    public class HtmlData
    {
        #region constants and data

        /// <summary>
        /// Indicates whether this has been compiled in debug mode. When true, DOM index paths will be
        /// stored internally in extended human-readable format.
        /// </summary>

        public static bool Debug = false;
        
        /// <summary>
        /// Length of each node's path ID (in characters), sets a limit on the number of child nodes before a reindex
        /// is required. For most cases, a small number will yield better performance. In production we probably can get
        /// away with just 1 (meaning a char=65k possible values). 
        /// 
        /// NOTE: At present PathID has been optimized as a ushort data type. You CANNOT just change this.
        /// </summary>
        
        public const int pathIdLength = 1;
        
        /// <summary>
        /// The character used to separate the unique part of an index entry from its path. When debugging
        /// it is useful to have a printable character. Otherwise we want something that is guaranteed to be
        /// a unique stop character.
        /// </summary>
        
        //public const char indexSeparator = (char)1;
        public const ushort indexSeparator = 65535;

        // Hardcode some tokens to improve performance when referring to them often

        /// <summary>
        /// Special token meaning "do nothing"
        /// </summary>

        public const ushort tagActionNothing = 0;

        /// <summary>
        /// Special token meaning "close the parent tag before opening the next one"
        /// </summary>

        public const ushort tagActionClose = 1;

        /// <summary>
        /// Identifier for the Class attribute.
        /// </summary>

        public const ushort ClassAttrId = 3;

        /// <summary>
        /// Identifier for the Value attribute.
        /// </summary>

        public const ushort ValueAttrId = 4;

        /// <summary>
        /// Identifier for the ID attribute.
        /// </summary>

        public const ushort IDAttrId = 5;

        /// <summary>
        /// Identifier for the selected attribute.
        /// </summary>

        public const ushort SelectedAttrId = 6;

        /// <summary>
        /// Identifier for the readonly attribute.
        /// </summary>

        public const ushort ReadonlyAttrId = 7;

        /// <summary>
        /// Identifier for the checked attribute.
        /// </summary>

        public const ushort CheckedAttrId = 8;

        /// <summary>
        /// The INPUT tag.
        /// </summary>

        public const ushort tagINPUT = 9;

        /// <summary>
        /// The SELECT tag.
        /// </summary>

        public const ushort tagSELECT = 10;

        /// <summary>
        /// The OPTION tag.
        /// </summary>

        public const ushort tagOPTION = 11;

        /// <summary>
        /// The P tag.
        /// </summary>

        public const ushort tagP = 12;

        /// <summary>
        /// The TR tag.
        /// </summary>

        public const ushort tagTR = 13;

        /// <summary>
        /// The TD tag.
        /// </summary>

        public const ushort tagTD = 14;

        /// <summary>
        /// The TH tag.
        /// </summary>

        public const ushort tagTH = 15;

        /// <summary>
        /// The HEAD tag.
        /// </summary>

        public const ushort tagHEAD = 16;

        /// <summary>
        /// The BODY tag.
        /// </summary>

        public const ushort tagBODY = 17;

        /// <summary>
        /// The DT tag
        /// </summary>

        public const ushort tagDT = 18;

        /// <summary>
        /// The COLGROUP tag.
        /// </summary>

        public const ushort tagCOLGROUP = 19;

        /// <summary>
        /// The DD tag
        /// </summary>

        public const ushort tagDD = 20;

        /// <summary>
        /// The LI tag
        /// </summary>

        public const ushort tagLI = 21;

        /// <summary>
        /// The DL tag
        /// </summary>

        public const ushort tagDL = 22;

        /// <summary>
        /// The TABLE tag.
        /// </summary>

        public const ushort tagTABLE = 23;

        /// <summary>
        /// The OPTGROUP tag.
        /// </summary>

        public const ushort tagOPTGROUP = 24;

        /// <summary>
        /// The UL tag.
        /// </summary>

        public const ushort tagUL = 25;

        /// <summary>
        /// The OL tag.
        /// </summary>

        public const ushort tagOL = 26;

        /// <summary>
        /// The TBODY tag
        /// </summary>

        public const ushort tagTBODY = 27;

        /// <summary>
        /// The TFOOT tag.
        /// </summary>

        public const ushort tagTFOOT = 28;

        /// <summary>
        /// The THEAD tag.
        /// </summary>

        public const ushort tagTHEAD = 29;

        /// <summary>
        /// The RT tag.
        /// </summary>

        public const ushort tagRT = 30;

        /// <summary>
        /// The RP tag.
        /// </summary>

        public const ushort tagRP = 31;

        /// <summary>
        /// The SCRIPT tag.
        /// </summary>

        public const ushort tagSCRIPT = 32;

        /// <summary>
        /// The TEXTAREA tag.
        /// </summary>

        public const ushort tagTEXTAREA = 33;

        /// <summary>
        /// The STYLE tag.
        /// </summary>

        public const ushort tagSTYLE = 34;

        /// <summary>
        /// The COL tag.
        /// </summary>

        public const ushort tagCOL = 35;

        /// <summary>
        /// The HTML tag.
        /// </summary>

        public const ushort tagHTML = 36;

        /// <summary>
        /// The BUTTON tag.
        /// </summary>

        public const ushort tagBUTTON = 37;

        /// <summary>
        /// The BUTTON tag.
        /// </summary>

        public const ushort attrMULTIPLE = 38;

        /// <summary>
        /// the A tag
        /// </summary>
        
        public const ushort tagA = 39;

        /// <summary>
        /// the SPAN tag
        /// </summary>

        public const ushort tagSPAN = 40;

        /// <summary>
        /// the SPAN tag
        /// </summary>

        public const ushort tagFORM = 41;

        /// <summary>
        /// The REQUIRED attribute.
        /// </summary>

        public const ushort attrREQUIRED = 42;


        /// <summary>
        /// The REQUIRED attribute.
        /// </summary>

        public const ushort attrAUTOFOCUS = 43;

        /// <summary>
        /// The TYPE attribute.
        /// </summary>

        public const ushort attrTYPE = 44;

        /// <summary>
        /// The PROGRESS element
        /// </summary>

        public const ushort tagPROGRESS = 45;

        /// <summary>
        /// The LABEL element
        /// </summary>

        public const ushort tagLABEL = 46;


        /// <summary>
        /// The DISABLED attribute
        /// </summary>

        public const ushort attrDISABLED = 47;

        /// <summary>
        /// The METER element
        /// </summary>

        public const ushort tagMETER = 48;

        /// <summary>
        /// The IMG element
        /// </summary>

        public const ushort tagIMG = 49;

        /// <summary>
        /// The IMG element
        /// </summary>

        public const ushort tagLINK = 50;


        // WHEN CHANGING THIS, YOU MUST UPDATE THE "hardcoded" ARRAY BELOW.

        /// <summary>
        /// should match final tag above; for self-checking.
        /// </summary>
        
        private const ushort maxHardcodedTokenId = 50;

        // Unquoted attribute value syntax: http://dev.w3.org/html5/spec-LC/syntax.html#attributes-0
        // 
        // U+0022 QUOTATION MARK characters (")
        // U+0027 APOSTROPHE characters (')
        // U+003D EQUALS SIGN characters (=)
        // U+003C LESS-THAN SIGN characters (<)
        // U+003E GREATER-THAN SIGN characters (>)
        // or U+0060 GRAVE ACCENT characters (`),
        // and must not be the empty string.}

        private static char[] MustBeQuoted = new char[] { '/', '\x0022', '\x0027', '\x003D', '\x003C', '\x003E', '\x0060' };
        private static char[] MustBeQuotedAll;

        /// <summary>
        /// Things that can be in a CSS number
        /// </summary>

        public static HashSet<char> NumberChars = new HashSet<char>("-+0123456789.,");

        /// <summary>
        /// The units that are allowable unit strings in a CSS style..
        /// </summary>
        /// <url>
        /// http://www.w3.org/TR/css3-values/#relative-lengths
        /// </url>

        public static HashSet<string> Units = new HashSet<string>(new string[] { 
            "%", "cm", "mm", "in", "px", "pc", "pt",  
            "em",  "ex",  "vmin", "vw", "rem","vh",
            "deg", "rad", "grad", "turn",
            "s", "ms",
            "Hz", "KHz",
            "dpi","dpcm","dppx"
        });

        /// <summary>
        /// Fields used internally
        /// </summary>

        private static ushort nextID = 2;
        private static List<string> Tokens = new List<string>();
        private static Dictionary<string, ushort> TokenIDs;
        private static object locker = new Object();

        // Constants for path encoding functions

        private static string defaultPadding;

        // This will be a lookup table where each value contains binary flags indicating what
        // properties are true for that value. We fix a size that's at an even binary boundary
        // so we can mask it for fast comparisons. If the number of tags with data exceeded 64
        // this can just increase; anything above the last used slot will just be 0.

        private static ushort[] TokenMetadata = new ushort[256];

        // (256 * 256) & ~256
        // this is the mask to test if an ID is in outside short list
        private const ushort NonSpecialTokenMask = (ushort)65280;

        #endregion

        #region constructor

        static HtmlData()
        {
            // For path encoding - when in production mode use a single character value for each path ID. This lets us avoid doing 
            // a division for indexing to determine path depth, and is just faster for a lot of reasons. This should be plenty
            // of tokens: things that are tokenized are tag names style names, class names, attribute names (not values), and ID 
            // values. You'd be hard pressed to exceed this limit (65k for a single level) on one single web page. 
            // (Famous last words right?)

            defaultPadding = "";
            for (int i = 1; i < pathIdLength; i++)
            {
                defaultPadding = defaultPadding + "0";
            }

            MustBeQuotedAll = new char[CharacterData.charsHtmlSpaceArray.Length + MustBeQuoted.Length];
            MustBeQuoted.CopyTo(MustBeQuotedAll, 0);
            CharacterData.charsHtmlSpaceArray.CopyTo(MustBeQuotedAll, MustBeQuoted.Length);

            // these elements can never have html children.

            string[] noChildHtmlAllowed = new string[] {
                // may have text content

                "SCRIPT","TEXTAREA","STYLE"

            };

            // "void elements - these elements can't have any children ever

            string[] voidElements = new string[] {
                "BASE","BASEFONT","FRAME","LINK","META","AREA","COL","HR","PARAM",
                "IMG","INPUT","BR", "!DOCTYPE","!--", "COMMAND", "EMBED","KEYGEN","SOURCE","TRACK","WBR"
            };


            // these elements will cause certain tags to be closed automatically; 
            // this is very important for layout.

            // 6-19-2012: removed "object" - object is inline.

            string[] blockElements = new string[]{
                "BODY","BR","ADDRESS","BLOCKQUOTE","CENTER","DIV","DIR","FORM","FRAMESET",
                "H1","H2","H3","H4","H5","H6","HR",
                "ISINDEX","LI","NOFRAMES","NOSCRIPT",
                "OL","P","PRE","TABLE","TR","TEXTAREA","UL",
                
                // html5 additions
                "ARTICLE","ASIDE","BUTTON","CANVAS","CAPTION","COL","COLGROUP","DD","DL","DT","EMBED",
                "FIELDSET","FIGCAPTION","FIGURE","FOOTER","HEADER","HGROUP","PROGRESS","SECTION",
                "TBODY","THEAD","TFOOT","VIDEO",
                
                // really old
                "APPLET","LAYER","LEGEND"
            };


            string[] paraClosers = new string[]{
                 "ADDRESS","ARTICLE", "ASIDE", "BLOCKQUOTE", "DIR", "DIV", "DL", "FIELDSET", "FOOTER", "FORM",
                 "H1", "H2", "H3", "H4", "H5", "H6", "HEADER", "HGROUP", "HR", "MENU", "NAV", "OL", "P", "PRE", "SECTION", "TABLE","UL"
            };

            // these elements are boolean; they do not have a value other than present or missing. They
            // are really "properties" but we don't have a distinction between properties, and a rendered
            // attribute. (It makes no sense in CsQuery; the only thing that matters when the DOM is
            // rendered is whether the attribute is present. This could change if the DOM were used with
            // a javascript engine, though, e.g. to simulate a browser)

            string[] booleanAttributes = new string[] {
                "AUTOBUFFER", "AUTOFOCUS", "AUTOPLAY", "ASYNC", "CHECKED", "COMPACT", "CONTROLS", 
                "DECLARE", "DEFAULTMUTED", "DEFAULTSELECTED", "DEFER", "DISABLED", "DRAGGABLE", 
                "FORMNOVALIDATE", "HIDDEN", "INDETERMINATE", "ISMAP", "ITEMSCOPE","LOOP", "MULTIPLE",
                "MUTED", "NOHREF", "NORESIZE", "NOSHADE", "NOWRAP", "NOVALIDATE", "OPEN", "PUBDATE", 
                "READONLY", "REQUIRED", "REVERSED", "SCOPED", "SEAMLESS", "SELECTED", "SPELLCHECK", 
                "TRUESPEED"," VISIBLE"
            };


            // these tags may be closed automatically

            string[] autoOpenOrClose = new string[] {
                "P","LI","TR","TD","TH","THEAD","TBODY","TFOOT","OPTION","HEAD","DT","DD","COLGROUP","OPTGROUP",

                // elements that precede things that can be opened automatically

                "TABLE","HTML"
            };

            // only these can appear in HEAD

            string[] metaDataTags = new string[] {
                "BASE","COMMAND","LINK","META","NOSCRIPT","SCRIPT","STYLE","TITLE"
            };


            string[] caseInsensitiveValues = new string[] {
                "type","target"
            };

            // see http://dev.w3.org/html5/spec/single-page.html#elements-1 when interfaces and overridden
            // DOMElement implementations are developed for all of these elements, this check will no
            // longer be needed. 
            
            string[] hasValueAttribute = new string[] {
                "input","select","option","param","button","progress","output","meter","script"
            };

            string[] isFormControl = new string[] {
                "input","select","button","textarea"
            };

            // consider using reflection to add all the hardcoded values instead of this error-prone method

            string[] hardcoded = new string[] {
                "unused","class","value","id","selected","readonly","checked","input","select","option","p","tr",
                "td","th","head","body","dt","colgroup","dd","li","dl","table","optgroup","ul","ol","tbody","tfoot","thead","rt",
                "rp","script","textarea","style","col","html","button","multiple","a","span","form","required","autofocus",
                "type","progress","label","disabled","meter","img","link"
            };

            TokenIDs = new Dictionary<string, ushort>();

            foreach (var item in hardcoded) {
                Tokenize(item);
            }

            // all this hardcoding makes me nervous, sanity check
            
            if (nextID != maxHardcodedTokenId + 1)
            {
                throw new InvalidOperationException("Something went wrong with the constant map in DomData");
            }

            // create the binary lookup table of tag metadata

            PopulateTokenHashset(noChildHtmlAllowed);
            PopulateTokenHashset(voidElements);
            PopulateTokenHashset(blockElements);
            PopulateTokenHashset(paraClosers);
            PopulateTokenHashset(booleanAttributes);
            PopulateTokenHashset(autoOpenOrClose);
            PopulateTokenHashset(metaDataTags);
            PopulateTokenHashset(caseInsensitiveValues);
            PopulateTokenHashset(hasValueAttribute);

            // Fill out the list of tokens to the boundary of the metadata array so the indices align

            while (nextID < (ushort)TokenMetadata.Length)
            {
                Tokens.Add(null);
                nextID++;
            }

            // no element children allowed but text children are

            setBit(noChildHtmlAllowed, TokenProperties.HtmlChildrenNotAllowed);

            // no children whatsoever

            setBit(voidElements, TokenProperties.ChildrenNotAllowed | TokenProperties.HtmlChildrenNotAllowed);

            setBit(autoOpenOrClose, TokenProperties.AutoOpenOrClose);
            setBit(blockElements, TokenProperties.BlockElement);
            setBit(booleanAttributes, TokenProperties.BooleanProperty);
            setBit(paraClosers, TokenProperties.ParagraphCloser);
            setBit(metaDataTags, TokenProperties.MetaDataTags);
            setBit(caseInsensitiveValues, TokenProperties.CaseInsensitiveValues);
            setBit(hasValueAttribute, TokenProperties.HasValue);
            setBit(isFormControl, TokenProperties.FormInputControl);
        }

        private static HashSet<ushort> PopulateTokenHashset(IEnumerable<string> tokens)
        {
            var set = new HashSet<ushort>();
            foreach (var item in tokens)
            {
                set.Add(Tokenize(item));
            }
            return set;
        }

        private static void Touch()
        {
            var x =  nextID;
        }

        #endregion

        #region public methods

        /// <summary>
        /// A list of all keys (tokens) created.
        /// </summary>

        public static IEnumerable<string> Keys
        {
            get
            {
                return Tokens;
            }
        }

        /// <summary>
        /// This type does not allow HTML children. Some of these types may allow text but not HTML.
        /// </summary>
        ///
        /// <param name="nodeId">
        /// The token ID
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool HtmlChildrenNotAllowed(ushort nodeId)
        {
            return (nodeId & NonSpecialTokenMask) == 0 &&
                (TokenMetadata[nodeId] & (ushort)TokenProperties.HtmlChildrenNotAllowed) > 0;

        }

        /// <summary>
        /// This type does not allow HTML children. Some of these types may allow text but not HTML.
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The node name to test.
        /// </param>
        ///
        /// <returns>
        /// true if HTML nodes are not allowed as childredn, false if they are.
        /// </returns>

        public static bool HtmlChildrenNotAllowed(string nodeName)
        {
            return HtmlChildrenNotAllowed(Tokenize(nodeName));
        }

        /// <summary>
        /// Test whether this element may have children.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token ID.
        /// </param>
        ///
        /// <returns>
        /// When false, this element type may never have children.
        /// </returns>

        public static bool ChildrenAllowed(ushort tokenId)
        {
            // nodeId & NonSpecialTokenMask returns zero for tokens that are in the short list.
            // anything outside the short list (or not matching special properties) os ok - 
            // innertextallowed is the default

            return (tokenId & NonSpecialTokenMask) != 0 ||
                (TokenMetadata[tokenId] & (ushort)TokenProperties.ChildrenNotAllowed) == 0;
        }

        /// <summary>
        /// Test whether this element can have children.
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The node name to test.
        /// </param>
        ///
        /// <returns>
        /// When false, this element type may never have children.
        /// </returns>

        public static bool ChildrenAllowed(string nodeName)
        {
            return ChildrenAllowed(Tokenize(nodeName));
        }

        /// <summary>
        /// Test whether the node is a block-type element.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token ID of the node
        /// </param>
        ///
        /// <returns>
        /// true if the token ID represents a block type element, false if not.
        /// </returns>

        public static bool IsBlock(ushort tokenId)
        {

            return (tokenId & NonSpecialTokenMask) == 0 &&
                (TokenMetadata[tokenId] & (ushort)TokenProperties.BlockElement) != 0;
        }

        /// <summary>
        /// Test whether the node is a block-type element
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The node name to test.
        /// </param>
        ///
        /// <returns>
        /// true if a block type, false if not.
        /// </returns>

        public static bool IsBlock(string nodeName)
        {
            return IsBlock(Tokenize(nodeName));
        }

        /// <summary>
        /// Test whether the attribute is a boolean type.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token ID
        /// </param>
        ///
        /// <returns>
        /// true if boolean, false if not.
        /// </returns>

        public static bool IsBoolean(ushort tokenId)
        {
            return (tokenId & NonSpecialTokenMask) == 0 &&
                  (TokenMetadata[tokenId] & (ushort)TokenProperties.BooleanProperty) != 0;
        }

        /// <summary>
        /// Test whether the attribute is a boolean type.
        /// </summary>
        ///
        /// <param name="propertyName">
        /// The attribute or property name
        /// </param>
        ///
        /// <returns>
        /// true if boolean, false if not.
        /// </returns>

        public static bool IsBoolean(string propertyName)
        {
            return IsBoolean(Tokenize(propertyName));
        }

        /// <summary>
        /// Test whether an attribute has case-insensitive values (for selection purposes)
        /// </summary>
        ///
        /// <param name="attributeName">
        /// Name of the attribute.
        /// </param>
        ///
        /// <returns>
        /// true if the values are case insensitive, false if not.
        /// </returns>

        public static bool IsCaseInsensitiveValues(string attributeName)
        {
            return IsCaseInsensitiveValues(Tokenize(attributeName));
        }

        /// <summary>
        /// Test whether an attribute has case-insensitive values (for selection purposes)
        /// </summary>
        ///
        /// <param name="attributeToken">
        /// Token ID of the attribute.
        /// </param>
        ///
        /// <returns>
        /// true if the values are case insensitive, false if not.
        /// </returns>

        public static bool IsCaseInsensitiveValues(ushort attributeToken)
        {
            return (attributeToken & NonSpecialTokenMask) == 0 &&
                 (TokenMetadata[attributeToken] & (ushort)TokenProperties.CaseInsensitiveValues) != 0;

        }

        /// <summary>
        /// Test if a node type has a VALUE property.
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The node name token.
        /// </param>
        ///
        /// <returns>
        /// true if it has a VALUE property, false if not.
        /// </returns>

        public static bool HasValueProperty(string nodeName)
        {
            return HasValueProperty(Tokenize(nodeName));
        }

        /// <summary>
        /// Test if a node type has a VALUE property.
        /// </summary>
        ///
        /// <param name="nodeNameToken">
        /// Token ID of the node name.
        /// </param>
        ///
        /// <returns>
        /// true if it has a VALUE property, false if not.
        /// </returns>

        public static bool HasValueProperty(ushort nodeNameToken)
        {
            return (nodeNameToken & NonSpecialTokenMask) == 0 &&
                 (TokenMetadata[nodeNameToken] & (ushort)TokenProperties.HasValue) != 0;
        }

        /// <summary>
        /// Test if the node name is a form input control.
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The node name to test.
        /// </param>
        ///
        /// <returns>
        /// true if a form input control, false if not.
        /// </returns>

        public static bool IsFormInputControl(string nodeName)
        {
            return IsFormInputControl(Tokenize(nodeName));
        }

        /// <summary>
        /// Test if the node name is a form input control
        /// </summary>
        ///
        /// <param name="nodeNameToken">
        /// The node name token.
        /// </param>
        ///
        /// <returns>
        /// true if a form input control, false if not.
        /// </returns>

        public static bool IsFormInputControl(ushort nodeNameToken)
        {
            return (nodeNameToken & NonSpecialTokenMask) == 0 &&
                 (TokenMetadata[nodeNameToken] & (ushort)TokenProperties.HasValue) != 0;
        }

        /// <summary>
        /// Return a token for a name
        /// </summary>
        ///
        /// <param name="name">
        /// The name to tokenize.
        /// </param>
        ///
        /// <returns>
        /// The token
        /// </returns>

        public static ushort Tokenize(string name)
        {

            if (String.IsNullOrEmpty(name))
            {
                return 0;
            }
            return TokenizeImpl(name.ToLower());

        }

        /// <summary>
        /// Return a token for a name, adding to the index if it doesn't exist. When indexing tags and
        /// attributes, TokenID(tokenName) should be used.
        /// </summary>
        ///
        /// <param name="name">
        /// The name to tokenize
        /// </param>
        ///
        /// <returns>
        /// A token representation of the string
        /// </returns>

        public static ushort TokenizeCaseSensitive(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return 0;
            }
            return TokenizeImpl(name);
        }

        /// <summary>
        /// Return a token ID for a name, adding to the index if it doesn't exist. When indexing tags and
        /// attributes, ignoreCase should be used.
        /// </summary>
        ///
        /// <param name="tokenName">
        /// The token name
        /// </param>
        ///
        /// <returns>
        /// A token
        /// </returns>

        private static ushort TokenizeImpl(string tokenName)
        {
            ushort id;

            if (!TokenIDs.TryGetValue(tokenName, out id))
            {

                lock (locker)
                {
                    if (!TokenIDs.TryGetValue(tokenName, out id))
                    {
                        Tokens.Add(tokenName);
                        TokenIDs.Add(tokenName, nextID);
                        // if for some reason we go over 65,535, will overflow and crash. no need 
                        // to check
                        id = nextID++;
                    }
                }
            }
            return id;
        }

        /// <summary>
        /// Return a token name for an ID.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token ID
        /// </param>
        ///
        /// <returns>
        /// The string, or an empty string if the token ID was not found
        /// </returns>

        public static string TokenName(ushort tokenId)
        {
            return tokenId <= 0 ? "" : Tokens[tokenId - 2];
        }

        /// <summary>
        /// HtmlEncode a string, except for double-quotes, so it can be enclosed in single-quotes.
        /// </summary>
        ///
        /// <param name="text">
        /// The text to encode
        /// </param>
        /// <param name="alwaysQuote">
        /// When true, the attribute value will be quoted even if quotes are not required by the value.
        /// </param>
        /// <param name="quoteChar">
        /// [out] The quote character.
        /// </param>
        ///
        /// <returns>
        /// The encoded string
        /// </returns>

        public static string AttributeEncode(string text, bool alwaysQuote, out string quoteChar)
        {
            if (text == "")
            {
                quoteChar = "\"";
                return "";
            }

            bool hasQuotes = text.IndexOf("\"") >= 0;
            bool hasSingleQuotes = text.IndexOf("'") >= 0;
            string result = text;
            if (hasQuotes || hasSingleQuotes)
            {

                //un-encode quotes or single-quotes when possible. When writing the attribute it will use the right one
                if (hasQuotes && hasSingleQuotes)
                {
                    result = result.Replace("'", "&#39;");
                    quoteChar = "\'";
                }
                else if (hasQuotes)
                {
                    quoteChar = "'";
                }
                else
                {
                    quoteChar = "\"";
                }
            }
            else
            {
                if (alwaysQuote)
                {
                    quoteChar = "\"";
                }
                else
                {
                    quoteChar = result.IndexOfAny(HtmlParser.HtmlData.MustBeQuotedAll) >= 0 ? "\"" : "";
                }
            }

            return result;
        }

        /// <summary>
        /// Decode HTML-encoded text.
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML.
        /// </param>
        ///
        /// <returns>
        /// Decoded html.
        /// </returns>

        public static string HtmlDecode(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);

        }
        /// <summary>
        /// For testing only - the production code never uses this version.
        /// </summary>
        ///
        /// <param name="tag">
        /// .
        /// </param>
        /// <param name="newTag">
        /// .
        /// </param>
        /// <param name="isDocument">
        /// .
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public static ushort SpecialTagAction(string tag, string newTag, bool isDocument = true)
        {
            return isDocument ?
                SpecialTagActionForDocument(Tokenize(tag), Tokenize(newTag)) :
                SpecialTagAction(Tokenize(tag), Tokenize(newTag));
        }


        /// <summary>
        /// Determine a course of action given a new tag, its parent, and whether or not to treat this as
        /// a document. Return 1 to close, 0 to do nothing, or an ID to generate.
        /// </summary>
        ///
        /// <param name="parentTagId">
        /// The parent tag ID
        /// </param>
        /// <param name="newTagId">
        /// The new tag ID found
        /// </param>
        ///
        /// <returns>
        /// A tokenId representing an action or a new tag to generate
        /// </returns>

        public static ushort SpecialTagActionForDocument(ushort parentTagId, ushort newTagId)
        {
            if (parentTagId == HtmlData.tagHTML)
            {

                // [html5] An html element's start tag may be omitted if the first thing inside the html element is not a comment.
                // [html5] A body element's start tag may be omitted if the element is empty, or if the first thing inside the body
                //         element is not a space character or a comment, except if the first thing inside the body element is a 
                //         script or style element.
                // [html5] A head element's start tag may be omitted if the element is empty, or if the first thing inside the head element is an element.

                // [csquery] When a metadata tag appears, we start a head. Otherwise, we start a body. If a body later appears it will be ignored.

                    return (newTagId & NonSpecialTokenMask) == 0 && 
                        (TokenMetadata[newTagId] & (ushort)TokenProperties.MetaDataTags) != 0 ?
                        HtmlData.tagHEAD :
                            newTagId != HtmlData.tagBODY && 
                            newTagId != HtmlData.tagHEAD ?
                                HtmlData.tagBODY : tagActionNothing;
            } else {
                return SpecialTagAction(parentTagId,newTagId);
            }
        }

        /// <summary>
        /// Return the type of action that should be performed given a tag, and a new tag found as a
        /// child of that tag.
        /// </summary>
        ///
        /// <remarks>
        /// Some tags have inner HTML but are often not closed properly. There are two possible
        /// situations. A tag may not have a nested instance of itself, and therefore any recurrence of
        /// that tag implies the previous one is closed. Other tag closings are simply optional, but are
        /// not repeater tags (e.g. body, html). These should be handled automatically by the logic that
        /// bubbles any closing tag to its parent if it doesn't match the current tag. The exception is
        /// &lt;head&gt; which technically does not require a close, but we would not expect to find
        /// another close tag Complete list of optional closing tags: HTML, HEAD, BODY, P, DT, DD, LI,
        /// OPTION, THEAD, TH, TBODY, TR, TD, TFOOT, COLGROUP
        /// 
        ///  body, html will be closed automatically at the end of parsing and are also not required.
        /// </remarks>
        ///
        /// <param name="parentTagId">
        /// The parent tag's token.
        /// </param>
        /// <param name="newTagId">
        /// The new child tag's token.
        /// </param>
        ///
        /// <returns>
        /// A tag action code indicating that nothing special should happen or the parent tag should be
        /// closed; or alternatively the token for a tag that should be generated in place before the new
        /// tag is opened.
        /// </returns>

        public static ushort SpecialTagAction(ushort parentTagId, ushort newTagId)
        {

            if ((parentTagId & NonSpecialTokenMask) != 0)
            {
                return HtmlData.tagActionNothing;
            }

            switch(parentTagId) {
                case HtmlData.tagHEAD:
                    return  (newTagId & NonSpecialTokenMask) == 0 &&
                            (TokenMetadata[newTagId] & (ushort)TokenProperties.MetaDataTags) == 0 ?
                                tagActionClose : tagActionNothing;
      
                // [html5] An li element's end tag may be omitted if the li element is immediately followed by another li element 
                //         or if there is no more content in the parent element.

                case HtmlData.tagLI:
                    return newTagId == HtmlData.tagLI ?
                       tagActionClose : tagActionNothing;

                // [html5] A dt element's end tag may be omitted if the dt element is immediately followed by another dt element or a dd element.
                // [html5] A dd element's end tag may be omitted if the dd element is immediately followed by another dd element or a dt element, or if there is no more content in the parent element.
                // [csquery] we more liberally interpret the appearance of a DL as an omitted 

                case HtmlData.tagDT:
                case HtmlData.tagDD:
                    return newTagId == HtmlData.tagDT || newTagId == HtmlData.tagDD
                        ? tagActionClose : tagActionNothing;

                // [html5] A p element's end tag may be omitted if the p element is immediately followed by an 
                //     - address, article, aside, blockquote, dir, div, dl, fieldset, footer, form, h1, h2, h3, h4, h5, h6, header,
                //     - group, hr, menu, nav, ol, p, pre, section, table, or ul, 
                //   element, or if there is no more content in the parent element and the parent element is not an a element.
                // [csquery] I have no idea what "the parent element is not an element" means. Closing an open p whenever we hit one of these elements seems to work.

                case HtmlData.tagP:
                    return (newTagId & NonSpecialTokenMask) == 0
                        && (TokenMetadata[newTagId] & (ushort)TokenProperties.ParagraphCloser) != 0
                            ? tagActionClose : tagActionNothing;


                // [html5] An rt element's end tag may be omitted if the rt element is immediately followed by an rt or rp element, or if there is no more content in the parent element.
                // [html5] An rp element's end tag may be omitted if the rp element is immediately followed by an rt or rp element, or if there is no more content in the parent element.

                case HtmlData.tagRT:
                case HtmlData.tagRP:
                    return newTagId == HtmlData.tagRT || newTagId == HtmlData.tagRP
                        ? tagActionClose : tagActionNothing;

                // [html5] An optgroup element's end tag may be omitted if the optgroup element is immediately followed by another 
                //    optgroup element, or if there is no more content in the parent element.

                case HtmlData.tagOPTGROUP:
                    return newTagId == HtmlData.tagOPTGROUP
                        ? tagActionClose : tagActionNothing;

                // [html5] An option element's end tag may be omitted if the option element is immediately
                // followed by another option element, or if it is immediately followed by an optgroup element,
                // or if there is no more content in the parent element. 

                case HtmlData.tagOPTION:
                    return newTagId == HtmlData.tagOPTION
                        ? tagActionClose : tagActionNothing;

                // [html5] A colgroup element's start tag may be omitted if the first thing inside the colgroup
                // element is a col element, and if the element is not immediately preceded by another colgroup
                // element whose end tag has been omitted. (It can't be omitted if the element is empty.) 

                // [csquery] This logic is beyond the capability of the parser right now. We close colgroup if
                // we hit something else in the table. In practice this should make no difference. 

                case HtmlData.tagCOLGROUP:
                    return newTagId == HtmlData.tagCOLGROUP || newTagId == HtmlData.tagTR || newTagId == HtmlData.tagTABLE
                        || newTagId == HtmlData.tagTHEAD || newTagId == HtmlData.tagTBODY || newTagId == HtmlData.tagTFOOT
                        ? tagActionClose : tagActionNothing;

                // [html5]  A tr element's end tag may be omitted if the tr element is immediately followed by another tr element, or if there is no more content in the parent element.
                // [csquery] just close it if it's an
                
                case HtmlData.tagTR:
                    return newTagId == HtmlData.tagTR || newTagId == HtmlData.tagTBODY || newTagId == HtmlData.tagTFOOT
                        ? tagActionClose : tagActionNothing;

                case HtmlData.tagTD:

                // [html5] A th element's end tag may be omitted if the th element is immediately followed by a td or th element, or if there is no more content in the parent element.
                // [csquery] we evaluate "no more content" by trying to open another tag type in the table. This can return both a close & create 
                
                case HtmlData.tagTH:
                    return newTagId == HtmlData.tagTBODY || newTagId == HtmlData.tagTFOOT ||
                            newTagId == HtmlData.tagTH || newTagId == HtmlData.tagTD || newTagId == HtmlData.tagTR
                        ? tagActionClose : tagActionNothing;

                // simple case: repeater-like tags should be closed by another occurence of itself

                // [html5] A thead element's end tag may be omitted if the thead element is immediately followed by a tbody or tfoot element.
                //         A tbody element's end tag may be omitted if the tbody element is immediately followed by a tbody or tfoot element, or if there is no more content in the parent element.

                case HtmlData.tagTHEAD:
                case HtmlData.tagTBODY:
                    return newTagId == HtmlData.tagTBODY || newTagId == HtmlData.tagTFOOT
                        ? tagActionClose : tagActionNothing;

                // [html5] A tfoot element's end tag may be omitted if the tfoot element is immediately followed by a tbody element, or if there is no more content in the parent element.
                // [csquery] can't think of any reason not to include THEAD as a closer if they put them in wrong order

                case HtmlData.tagTFOOT:
                    return newTagId == HtmlData.tagBODY || newTagId == HtmlData.tagTHEAD
                        ? tagActionClose : tagActionNothing;

                // AUTO CREATION

                // [html5] A tbody element's start tag may be omitted if the first thing inside the tbody element is a tr element, and if the element is not immediately 
                //        preceded by a tbody, thead, or tfoot element whose end tag has been omitted. (It can't be omitted if the element is empty.)

                case HtmlData.tagTABLE:
                    //return newTagId == HtmlData.tagCOL ?
                    return newTagId == HtmlData.tagTR ?
                        HtmlData.tagTBODY : tagActionNothing;

                default:
                    return tagActionNothing;

            }

        }

        #endregion

        #region private methods

      
       

        /// <summary>
        /// For each value in "tokens" (ignoring case) sets the specified bit in the reference table.
        /// </summary>
        ///
        /// <param name="tokens">
        /// A sequence of tokens
        /// </param>
        /// <param name="bit">
        /// The bitflag to set
        /// </param>

        private static void setBit(IEnumerable<string> tokens, TokenProperties bit)
        {
            foreach (var token in tokens)
            {
                setBit(Tokenize(token), bit);
            }

        }

        /// <summary>
        /// For each value in "tokens" sets the specified bit in the reference table.
        /// </summary>
        ///
        /// <param name="tokens">
        /// The sequence of tokens
        /// </param>
        /// <param name="bit">
        /// The bitflag to set
        /// </param>

        private static void setBit(IEnumerable<ushort> tokens, TokenProperties bit)
        {
            foreach (var token in tokens)
            {
                setBit(token, bit);
            }
        }

        /// <summary>
        /// Set the specified bit in the reference table for "token".
        /// </summary>
        ///
        /// <param name="token">
        /// The token
        /// </param>
        /// <param name="bit">
        /// The bit to set
        /// </param>

        private static void setBit(ushort token, TokenProperties bit)
        {
            TokenMetadata[token] |= (ushort)bit;
        }

        #endregion
    }
}
