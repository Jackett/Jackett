/*
 * The comments following this one that use the same comment syntax as this 
 * comment are quotes from the WHATWG HTML 5 spec as of 27 June 2007 
 * amended as of June 28 2007.
 * That document came with this statement:
 * © Copyright 2004-2007 Apple Computer, Inc., Mozilla Foundation, and 
 * Opera Software ASA. You are granted a license to use, reproduce and 
 * create derivative works of this document."
 */

using HtmlParserSharp.Common;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	/// <summary>
	/// Moved the constants (and pseude-enums) out of the TreeBuilder class.
	/// </summary>
	public class TreeBuilderConstants
	{
		/// <summary>
		/// Array version of U+FFFD.
		/// </summary>
		internal static readonly char[] REPLACEMENT_CHARACTER = { '\uFFFD' };

		// [NOCPP[

		internal readonly static string[] HTML4_PUBLIC_IDS = {
			"-//W3C//DTD HTML 4.0 Frameset//EN",
			"-//W3C//DTD HTML 4.0 Transitional//EN",
			"-//W3C//DTD HTML 4.0//EN", "-//W3C//DTD HTML 4.01 Frameset//EN",
			"-//W3C//DTD HTML 4.01 Transitional//EN",
			"-//W3C//DTD HTML 4.01//EN" };

		// ]NOCPP]

		internal readonly static string[] QUIRKY_PUBLIC_IDS = {
			"+//silmaril//dtd html pro v0r11 19970101//",
			"-//advasoft ltd//dtd html 3.0 aswedit + extensions//",
			"-//as//dtd html 3.0 aswedit + extensions//",
			"-//ietf//dtd html 2.0 level 1//",
			"-//ietf//dtd html 2.0 level 2//",
			"-//ietf//dtd html 2.0 strict level 1//",
			"-//ietf//dtd html 2.0 strict level 2//",
			"-//ietf//dtd html 2.0 strict//",
			"-//ietf//dtd html 2.0//",
			"-//ietf//dtd html 2.1e//",
			"-//ietf//dtd html 3.0//",
			"-//ietf//dtd html 3.2 final//",
			"-//ietf//dtd html 3.2//",
			"-//ietf//dtd html 3//",
			"-//ietf//dtd html level 0//",
			"-//ietf//dtd html level 1//",
			"-//ietf//dtd html level 2//",
			"-//ietf//dtd html level 3//",
			"-//ietf//dtd html strict level 0//",
			"-//ietf//dtd html strict level 1//",
			"-//ietf//dtd html strict level 2//",
			"-//ietf//dtd html strict level 3//",
			"-//ietf//dtd html strict//",
			"-//ietf//dtd html//",
			"-//metrius//dtd metrius presentational//",
			"-//microsoft//dtd internet explorer 2.0 html strict//",
			"-//microsoft//dtd internet explorer 2.0 html//",
			"-//microsoft//dtd internet explorer 2.0 tables//",
			"-//microsoft//dtd internet explorer 3.0 html strict//",
			"-//microsoft//dtd internet explorer 3.0 html//",
			"-//microsoft//dtd internet explorer 3.0 tables//",
			"-//netscape comm. corp.//dtd html//",
			"-//netscape comm. corp.//dtd strict html//",
			"-//o'reilly and associates//dtd html 2.0//",
			"-//o'reilly and associates//dtd html extended 1.0//",
			"-//o'reilly and associates//dtd html extended relaxed 1.0//",
			"-//softquad software//dtd hotmetal pro 6.0::19990601::extensions to html 4.0//",
			"-//softquad//dtd hotmetal pro 4.0::19971010::extensions to html 4.0//",
			"-//spyglass//dtd html 2.0 extended//",
			"-//sq//dtd html 2.0 hotmetal + extensions//",
			"-//sun microsystems corp.//dtd hotjava html//",
			"-//sun microsystems corp.//dtd hotjava strict html//",
			"-//w3c//dtd html 3 1995-03-24//", "-//w3c//dtd html 3.2 draft//",
			"-//w3c//dtd html 3.2 final//", "-//w3c//dtd html 3.2//",
			"-//w3c//dtd html 3.2s draft//", "-//w3c//dtd html 4.0 frameset//",
			"-//w3c//dtd html 4.0 transitional//",
			"-//w3c//dtd html experimental 19960712//",
			"-//w3c//dtd html experimental 970421//", "-//w3c//dtd w3 html//",
			"-//w3o//dtd w3 html 3.0//", "-//webtechs//dtd mozilla html 2.0//",
			"-//webtechs//dtd mozilla html//" };

		internal const int NOT_FOUND_ON_STACK = int.MaxValue;

		// [NOCPP[

		[Local]
		internal const string HTML_LOCAL = "html";

		// ]NOCPP]
	}
}
