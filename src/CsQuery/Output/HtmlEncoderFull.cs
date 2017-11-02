using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Output
{

    /// <summary>
    /// Full HTML encoder. All entities with known HTML codes are parsed; everything above 160
    /// becomes an HTML numeric-coded entity.
    /// </summary>

    public class HtmlEncoderFull: HtmlEncoderBasic
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HtmlEncoderFull(): base()
        {


        }

        static HtmlEncoderFull() {
            HtmlEntityMap = new Dictionary<char, string>();
            PopulateHtmlEntityMap();
        }

        static IDictionary<char, string> HtmlEntityMap;

        static ushort[] codedValues = new ushort[] {
            34,
            38,39,
            60,61,
            160,161,162,163,164,165,166,167,168,169,
            170,171,172,173,174,175,176,177,178,179,
            180,181,182,183,184,185,186,187,188,189,
            190,191,192,193,194,195,196,197,198,199,
            200,201,202,203,204,205,206,207,208,209,
            210,211,212,213,214,
            216,217,218,219,
            220,221,222,223,224,225,226,227,228,229,
            230,231,232,233,234,235,236,237,238,239,
            240,241,242,243,244,245,246,
            248,249,
            250,251,252,253,254,255,
            338,339,
            352,353,
            376,
            710,
            732,
            8194,8195,
            8201,
            8204,8205,8206,8207,
            8211,8212,
            8216,8217,8218,
            8220,8221,8222,
            8224,8225,
            8230,
            8240,
            8249,8250,
            8364,
            8482

        };

        static string[] codedEntities = new string[] {
            /* 34 */  "quot",
            /* 38 */  "amp","apos",
            /* 60 */  "lt","gt",
            /* 160 */ "nbsp","iexcl","cent","pound","curren","yen","brvbar","sect","uml","copy",
            /* 170 */ "ordf","laquo","not","shy","reg","macr","deg","plusmn","sup2","sup3",
            /* 180 */ "acute","micro","para","middot","cedil","sup1","ordm","raquo","frac14","frac12",
            /* 190 */ "frac45","iquest","Agrave","Aacute","Acirc","Atilde","Auml","Aring","AElig","Ccedil",
            /* 200 */ "Egrave","Eacute","Ecirc","Euml","Igrave","Iacute","Icirc","Iuml","ETH","Ntilde",
            /* 210 */ "Ograve","Oacute","Ocirc","Otilde","Ouml",
            /* 216 */ "Oslash","Ugrave","Uacute","Ucirc",
            /* 220 */ "Uuml","Yacute","THORN","szlig","agrave","aacute","acirc","atilde","auml","aring",
            /* 230 */ "aelig","ccedil","egrave","eacute","ecirc","euml","igrave","iacute","icirc","iuml",
            /* 240 */ "eth","ntilde","ograve","oacute","ocirc","otilde","ouml",
            /* 248 */ "oslash","ugrave",
            /* 250 */ "uacute","ucirc","uuml","yacute","thorn","yuml",
            /* 338 */ "OElig","oelig",
            /* 353 */ "Scaron","scaron",
            /* 376 */ "Yuml",
            /* 710 */ "circ",
            /* 732 */ "tilde",
            /* 8194 */ "ensp","emsp",
            /* 8201 */ "thinsp",
            /* 8204 */ "zwnj","zwj","lrm","rlm",
            /* 8211 */ "ndash","mdash",
            /* 8216 */ "lsquo","rsquo","sbquo",
            /* 8220 */ "ldquo","rdquo","bdquo",
            /* 8224 */ "dagger", "Dagger",
            /* 8230 */ "hellip",
            /* 8240 */ "permil",
            /* 8249 */ "lsaquo","rsaquo",
            /* 8364 */ "euro",
            /* 8482 */ "trade"
        };

        private static void PopulateHtmlEntityMap()
        {
            if (codedEntities.Length != codedValues.Length)
            {
                throw new InvalidDataException("The codedValues array must be the same length as the codedEntities array.");
            }

            for (int i = 0; i < codedValues.Length; i++)
            {
                HtmlEntityMap.Add((char)codedValues[i], "&"+codedEntities[i]+";");
            }

        }
        /// <summary>
        /// Determines of a character must be encoded; if so, encodes it as the output parameter and
        /// returns true; if not, returns false.
        /// </summary>
        ///
        /// <param name="c">
        /// The text string to encode.
        /// </param>
        /// <param name="encoded">
        /// [out] The encoded string.
        /// </param>
        ///
        /// <returns>
        /// True if the character was encoded.
        /// </returns>

        protected override bool TryEncode(char c, out string encoded)
        {
            if (c>=160) {
                bool found = HtmlEntityMap.TryGetValue(c, out encoded);
                if (found) {
                    return true;
                }
            } 

            // fall through - default handling for anything we did not process
            
            return base.TryEncode(c, out encoded);
            
        }

    }
}
