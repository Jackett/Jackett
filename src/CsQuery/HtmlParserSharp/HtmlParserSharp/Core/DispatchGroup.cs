/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2007-2011 Mozilla Foundation
 * Portions of comments Copyright 2004-2008 Apple Computer, Inc., Mozilla 
 * Foundation, and Opera Software ASA.
 * Copyright (c) 2012 Patrick Reisert
 *
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 */

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	public enum DispatchGroup
	{
		OTHER = 0,

		A = 1,

		BASE = 2,

		BODY = 3,

		BR = 4,

		BUTTON = 5,

		CAPTION = 6,

		COL = 7,

		COLGROUP = 8,

		FORM = 9,

		FRAME = 10,

		FRAMESET = 11,

		IMAGE = 12,

		INPUT = 13,

		ISINDEX = 14,

		LI = 15,

		LINK_OR_BASEFONT_OR_BGSOUND = 16,

		MATH = 17,

		META = 18,

		SVG = 19,

		HEAD = 20,

		HR = 22,

		HTML = 23,

		NOBR = 24,

		NOFRAMES = 25,

		NOSCRIPT = 26,

		OPTGROUP = 27,

		OPTION = 28,

		P = 29,

		PLAINTEXT = 30,

		SCRIPT = 31,

		SELECT = 32,

		STYLE = 33,

		TABLE = 34,

		TEXTAREA = 35,

		TITLE = 36,

		TR = 37,

		XMP = 38,

		TBODY_OR_THEAD_OR_TFOOT = 39,

		TD_OR_TH = 40,

		DD_OR_DT = 41,

		H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6 = 42,

		MARQUEE_OR_APPLET = 43,

		PRE_OR_LISTING = 44,

		B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U = 45,

		UL_OR_OL_OR_DL = 46,

		IFRAME = 47,

		EMBED_OR_IMG = 48,

		AREA_OR_WBR = 49,

		DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU = 50,

		ADDRESS_OR_ARTICLE_OR_ASIDE_OR_DETAILS_OR_DIR_OR_FIGCAPTION_OR_FIGURE_OR_FOOTER_OR_HEADER_OR_HGROUP_OR_NAV_OR_SECTION_OR_SUMMARY = 51,

		RUBY_OR_SPAN_OR_SUB_OR_SUP_OR_VAR = 52,

		RT_OR_RP = 53,

		COMMAND = 54,

		PARAM_OR_SOURCE_OR_TRACK = 55,

		MGLYPH_OR_MALIGNMARK = 56,

		MI_MO_MN_MS_MTEXT = 57,

		ANNOTATION_XML = 58,

		FOREIGNOBJECT_OR_DESC = 59,

		NOEMBED = 60,

		FIELDSET = 61,

		OUTPUT_OR_LABEL = 62,

		OBJECT = 63,

		FONT = 64,

		KEYGEN = 65,
        
        MENUITEM = 66
	}
}
