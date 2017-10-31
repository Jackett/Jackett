using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Bitfield of flags for specifying characteristics of a single character
    /// </summary>

    [Flags]
    public enum CharacterType
    {
        /// <summary>
        /// Whitespace
        /// </summary>

        Whitespace=0x01,
        /// <summary>
        /// Alpha charactersonly
        /// </summary>
        Alpha=0x02,
        /// <summary>
        /// Numeric characters only
        /// </summary>
        Number=0x04,
        /// <summary>
        /// Numbers plus non-numeric characters that can be part of a number
        /// </summary>
        NumberPart=0x08,
        
        /// <summary>
        /// Lowercase only
        /// </summary>
        Lower=0x10,
        /// <summary>
        /// Uppercase only.
        /// </summary>
        Upper=0x20,
        /// <summary>
        /// A mathematical operator; something that can be part of a math formiul;a.
        /// </summary>
        Operator=0x40,
        /// <summary>
        /// A character that has a mate, such as ( or ].
        /// </summary>
        Enclosing=0x80,

        /// <summary>
        /// A single or double quote.
        /// </summary>
        Quote=0x100,
        /// <summary>
        /// Backslash.
        /// </summary>
        Escape=0x200,
        /// <summary>
        /// Comma, space or pipe.
        /// </summary>
        Separator=0x400,
        /// <summary>
        /// ISO10646 character set excluding numbers
        /// </summary>
        AlphaISO10646 = 0x800,

        /// <summary>
        /// Something that can be the first character of an HTML tag selector (not tag name).
        /// </summary>
        HtmlTagSelectorStart=0x1000,
        /// <summary>
        /// Something that can be anthing other than the 1st character of an HTML tag selector.
        /// </summary>
        HtmlTagSelectorExceptStart=0x2000,
        
        /// <summary>
        /// A character that marks the end of an HTML tag opener (e.g. the end of the entire tag, or
        /// the beginning of the attribute section)
        /// </summary>
        /// 
        HtmlTagOpenerEnd=0x4000,
       
        /// <summary>
        /// &lt;, &gt;, or / -- any character that's part of the construct of an html tag; 
        /// finding one of these while seeking attribute names means the tag was closed.
        /// </summary>
        HtmlTagAny=0x8000,

        /// <summary>
        /// Something that can be the first character of a legal HTML tag name.
        /// </summary>
        HtmlTagNameStart = 0x10000,
        /// <summary>
        /// Something that can be anything other than the 1st character of a legal  HTML tag name.
        /// </summary>
        HtmlTagNameExceptStart = 0x20000,
        /// <summary>
        /// Something that can be a character of a legal HTML ID value.
        /// </summary>
        HtmlAttributeName = 0x40000,


        /// <summary>
        /// Terminates a selector or part of a selector
        /// </summary>
        SelectorTerminator = 0x80000,

        // an HTML "space" is actually different from "white space" which is defined in the HTML5 spec
        // as UNICODE whitespace and is a lot of characters. But we are generally only concerned with
        // "space" characters which delimit parts of tags and so on.

        /// <summary>
        /// An HTML "space" is actually different from "white space" which is defined in the HTML5 spec
        /// as UNICODE whitespace and is a lot of characters. But we are generally only concerned with
        /// "space" characters which delimit parts of tags and so on.
        /// </summary>
        HtmlSpace = 0x100000,


        /// <summary>
        /// A character that must be HTML encoded to create valid HTML
        /// </summary>
        
        HtmlMustBeEncoded = 0x200000,

        /// <summary>
        /// A character that will terminate an unquoted HTML attribute value.
        /// </summary>
        HtmlAttributeValueTerminator = 0x400000,

        /// <summary>
        /// Part of a hex string
        /// </summary>
        Hexadecimal = 0x800000
    }
}
