/*
 * Copyright (c) 2008-2009 Mozilla Foundation
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

using System;
using System.Text;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	public sealed class NCName
	{
		// [NOCPP[

		private const int SURROGATE_OFFSET = 0x10000 - (0xD800 << 10) - 0xDC00;

		private static readonly char[] HEX_TABLE = "0123456789ABCDEF".ToCharArray();

		public static bool IsNCNameStart(char c)
		{
			return ((c >= '\u0041' && c <= '\u005A')
					|| (c >= '\u0061' && c <= '\u007A')
					|| (c >= '\u00C0' && c <= '\u00D6')
					|| (c >= '\u00D8' && c <= '\u00F6')
					|| (c >= '\u00F8' && c <= '\u00FF')
					|| (c >= '\u0100' && c <= '\u0131')
					|| (c >= '\u0134' && c <= '\u013E')
					|| (c >= '\u0141' && c <= '\u0148')
					|| (c >= '\u014A' && c <= '\u017E')
					|| (c >= '\u0180' && c <= '\u01C3')
					|| (c >= '\u01CD' && c <= '\u01F0')
					|| (c >= '\u01F4' && c <= '\u01F5')
					|| (c >= '\u01FA' && c <= '\u0217')
					|| (c >= '\u0250' && c <= '\u02A8')
					|| (c >= '\u02BB' && c <= '\u02C1') || (c == '\u0386')
					|| (c >= '\u0388' && c <= '\u038A') || (c == '\u038C')
					|| (c >= '\u038E' && c <= '\u03A1')
					|| (c >= '\u03A3' && c <= '\u03CE')
					|| (c >= '\u03D0' && c <= '\u03D6') || (c == '\u03DA')
					|| (c == '\u03DC') || (c == '\u03DE') || (c == '\u03E0')
					|| (c >= '\u03E2' && c <= '\u03F3')
					|| (c >= '\u0401' && c <= '\u040C')
					|| (c >= '\u040E' && c <= '\u044F')
					|| (c >= '\u0451' && c <= '\u045C')
					|| (c >= '\u045E' && c <= '\u0481')
					|| (c >= '\u0490' && c <= '\u04C4')
					|| (c >= '\u04C7' && c <= '\u04C8')
					|| (c >= '\u04CB' && c <= '\u04CC')
					|| (c >= '\u04D0' && c <= '\u04EB')
					|| (c >= '\u04EE' && c <= '\u04F5')
					|| (c >= '\u04F8' && c <= '\u04F9')
					|| (c >= '\u0531' && c <= '\u0556') || (c == '\u0559')
					|| (c >= '\u0561' && c <= '\u0586')
					|| (c >= '\u05D0' && c <= '\u05EA')
					|| (c >= '\u05F0' && c <= '\u05F2')
					|| (c >= '\u0621' && c <= '\u063A')
					|| (c >= '\u0641' && c <= '\u064A')
					|| (c >= '\u0671' && c <= '\u06B7')
					|| (c >= '\u06BA' && c <= '\u06BE')
					|| (c >= '\u06C0' && c <= '\u06CE')
					|| (c >= '\u06D0' && c <= '\u06D3') || (c == '\u06D5')
					|| (c >= '\u06E5' && c <= '\u06E6')
					|| (c >= '\u0905' && c <= '\u0939') || (c == '\u093D')
					|| (c >= '\u0958' && c <= '\u0961')
					|| (c >= '\u0985' && c <= '\u098C')
					|| (c >= '\u098F' && c <= '\u0990')
					|| (c >= '\u0993' && c <= '\u09A8')
					|| (c >= '\u09AA' && c <= '\u09B0') || (c == '\u09B2')
					|| (c >= '\u09B6' && c <= '\u09B9')
					|| (c >= '\u09DC' && c <= '\u09DD')
					|| (c >= '\u09DF' && c <= '\u09E1')
					|| (c >= '\u09F0' && c <= '\u09F1')
					|| (c >= '\u0A05' && c <= '\u0A0A')
					|| (c >= '\u0A0F' && c <= '\u0A10')
					|| (c >= '\u0A13' && c <= '\u0A28')
					|| (c >= '\u0A2A' && c <= '\u0A30')
					|| (c >= '\u0A32' && c <= '\u0A33')
					|| (c >= '\u0A35' && c <= '\u0A36')
					|| (c >= '\u0A38' && c <= '\u0A39')
					|| (c >= '\u0A59' && c <= '\u0A5C') || (c == '\u0A5E')
					|| (c >= '\u0A72' && c <= '\u0A74')
					|| (c >= '\u0A85' && c <= '\u0A8B') || (c == '\u0A8D')
					|| (c >= '\u0A8F' && c <= '\u0A91')
					|| (c >= '\u0A93' && c <= '\u0AA8')
					|| (c >= '\u0AAA' && c <= '\u0AB0')
					|| (c >= '\u0AB2' && c <= '\u0AB3')
					|| (c >= '\u0AB5' && c <= '\u0AB9') || (c == '\u0ABD')
					|| (c == '\u0AE0') || (c >= '\u0B05' && c <= '\u0B0C')
					|| (c >= '\u0B0F' && c <= '\u0B10')
					|| (c >= '\u0B13' && c <= '\u0B28')
					|| (c >= '\u0B2A' && c <= '\u0B30')
					|| (c >= '\u0B32' && c <= '\u0B33')
					|| (c >= '\u0B36' && c <= '\u0B39') || (c == '\u0B3D')
					|| (c >= '\u0B5C' && c <= '\u0B5D')
					|| (c >= '\u0B5F' && c <= '\u0B61')
					|| (c >= '\u0B85' && c <= '\u0B8A')
					|| (c >= '\u0B8E' && c <= '\u0B90')
					|| (c >= '\u0B92' && c <= '\u0B95')
					|| (c >= '\u0B99' && c <= '\u0B9A') || (c == '\u0B9C')
					|| (c >= '\u0B9E' && c <= '\u0B9F')
					|| (c >= '\u0BA3' && c <= '\u0BA4')
					|| (c >= '\u0BA8' && c <= '\u0BAA')
					|| (c >= '\u0BAE' && c <= '\u0BB5')
					|| (c >= '\u0BB7' && c <= '\u0BB9')
					|| (c >= '\u0C05' && c <= '\u0C0C')
					|| (c >= '\u0C0E' && c <= '\u0C10')
					|| (c >= '\u0C12' && c <= '\u0C28')
					|| (c >= '\u0C2A' && c <= '\u0C33')
					|| (c >= '\u0C35' && c <= '\u0C39')
					|| (c >= '\u0C60' && c <= '\u0C61')
					|| (c >= '\u0C85' && c <= '\u0C8C')
					|| (c >= '\u0C8E' && c <= '\u0C90')
					|| (c >= '\u0C92' && c <= '\u0CA8')
					|| (c >= '\u0CAA' && c <= '\u0CB3')
					|| (c >= '\u0CB5' && c <= '\u0CB9') || (c == '\u0CDE')
					|| (c >= '\u0CE0' && c <= '\u0CE1')
					|| (c >= '\u0D05' && c <= '\u0D0C')
					|| (c >= '\u0D0E' && c <= '\u0D10')
					|| (c >= '\u0D12' && c <= '\u0D28')
					|| (c >= '\u0D2A' && c <= '\u0D39')
					|| (c >= '\u0D60' && c <= '\u0D61')
					|| (c >= '\u0E01' && c <= '\u0E2E') || (c == '\u0E30')
					|| (c >= '\u0E32' && c <= '\u0E33')
					|| (c >= '\u0E40' && c <= '\u0E45')
					|| (c >= '\u0E81' && c <= '\u0E82') || (c == '\u0E84')
					|| (c >= '\u0E87' && c <= '\u0E88') || (c == '\u0E8A')
					|| (c == '\u0E8D') || (c >= '\u0E94' && c <= '\u0E97')
					|| (c >= '\u0E99' && c <= '\u0E9F')
					|| (c >= '\u0EA1' && c <= '\u0EA3') || (c == '\u0EA5')
					|| (c == '\u0EA7') || (c >= '\u0EAA' && c <= '\u0EAB')
					|| (c >= '\u0EAD' && c <= '\u0EAE') || (c == '\u0EB0')
					|| (c >= '\u0EB2' && c <= '\u0EB3') || (c == '\u0EBD')
					|| (c >= '\u0EC0' && c <= '\u0EC4')
					|| (c >= '\u0F40' && c <= '\u0F47')
					|| (c >= '\u0F49' && c <= '\u0F69')
					|| (c >= '\u10A0' && c <= '\u10C5')
					|| (c >= '\u10D0' && c <= '\u10F6') || (c == '\u1100')
					|| (c >= '\u1102' && c <= '\u1103')
					|| (c >= '\u1105' && c <= '\u1107') || (c == '\u1109')
					|| (c >= '\u110B' && c <= '\u110C')
					|| (c >= '\u110E' && c <= '\u1112') || (c == '\u113C')
					|| (c == '\u113E') || (c == '\u1140') || (c == '\u114C')
					|| (c == '\u114E') || (c == '\u1150')
					|| (c >= '\u1154' && c <= '\u1155') || (c == '\u1159')
					|| (c >= '\u115F' && c <= '\u1161') || (c == '\u1163')
					|| (c == '\u1165') || (c == '\u1167') || (c == '\u1169')
					|| (c >= '\u116D' && c <= '\u116E')
					|| (c >= '\u1172' && c <= '\u1173') || (c == '\u1175')
					|| (c == '\u119E') || (c == '\u11A8') || (c == '\u11AB')
					|| (c >= '\u11AE' && c <= '\u11AF')
					|| (c >= '\u11B7' && c <= '\u11B8') || (c == '\u11BA')
					|| (c >= '\u11BC' && c <= '\u11C2') || (c == '\u11EB')
					|| (c == '\u11F0') || (c == '\u11F9')
					|| (c >= '\u1E00' && c <= '\u1E9B')
					|| (c >= '\u1EA0' && c <= '\u1EF9')
					|| (c >= '\u1F00' && c <= '\u1F15')
					|| (c >= '\u1F18' && c <= '\u1F1D')
					|| (c >= '\u1F20' && c <= '\u1F45')
					|| (c >= '\u1F48' && c <= '\u1F4D')
					|| (c >= '\u1F50' && c <= '\u1F57') || (c == '\u1F59')
					|| (c == '\u1F5B') || (c == '\u1F5D')
					|| (c >= '\u1F5F' && c <= '\u1F7D')
					|| (c >= '\u1F80' && c <= '\u1FB4')
					|| (c >= '\u1FB6' && c <= '\u1FBC') || (c == '\u1FBE')
					|| (c >= '\u1FC2' && c <= '\u1FC4')
					|| (c >= '\u1FC6' && c <= '\u1FCC')
					|| (c >= '\u1FD0' && c <= '\u1FD3')
					|| (c >= '\u1FD6' && c <= '\u1FDB')
					|| (c >= '\u1FE0' && c <= '\u1FEC')
					|| (c >= '\u1FF2' && c <= '\u1FF4')
					|| (c >= '\u1FF6' && c <= '\u1FFC') || (c == '\u2126')
					|| (c >= '\u212A' && c <= '\u212B') || (c == '\u212E')
					|| (c >= '\u2180' && c <= '\u2182')
					|| (c >= '\u3041' && c <= '\u3094')
					|| (c >= '\u30A1' && c <= '\u30FA')
					|| (c >= '\u3105' && c <= '\u312C')
					|| (c >= '\uAC00' && c <= '\uD7A3')
					|| (c >= '\u4E00' && c <= '\u9FA5') || (c == '\u3007')
					|| (c >= '\u3021' && c <= '\u3029') || (c == '_'));
		}

		public static bool IsNCNameTrail(char c)
		{
			return ((c >= '\u0030' && c <= '\u0039')
					|| (c >= '\u0660' && c <= '\u0669')
					|| (c >= '\u06F0' && c <= '\u06F9')
					|| (c >= '\u0966' && c <= '\u096F')
					|| (c >= '\u09E6' && c <= '\u09EF')
					|| (c >= '\u0A66' && c <= '\u0A6F')
					|| (c >= '\u0AE6' && c <= '\u0AEF')
					|| (c >= '\u0B66' && c <= '\u0B6F')
					|| (c >= '\u0BE7' && c <= '\u0BEF')
					|| (c >= '\u0C66' && c <= '\u0C6F')
					|| (c >= '\u0CE6' && c <= '\u0CEF')
					|| (c >= '\u0D66' && c <= '\u0D6F')
					|| (c >= '\u0E50' && c <= '\u0E59')
					|| (c >= '\u0ED0' && c <= '\u0ED9')
					|| (c >= '\u0F20' && c <= '\u0F29')
					|| (c >= '\u0041' && c <= '\u005A')
					|| (c >= '\u0061' && c <= '\u007A')
					|| (c >= '\u00C0' && c <= '\u00D6')
					|| (c >= '\u00D8' && c <= '\u00F6')
					|| (c >= '\u00F8' && c <= '\u00FF')
					|| (c >= '\u0100' && c <= '\u0131')
					|| (c >= '\u0134' && c <= '\u013E')
					|| (c >= '\u0141' && c <= '\u0148')
					|| (c >= '\u014A' && c <= '\u017E')
					|| (c >= '\u0180' && c <= '\u01C3')
					|| (c >= '\u01CD' && c <= '\u01F0')
					|| (c >= '\u01F4' && c <= '\u01F5')
					|| (c >= '\u01FA' && c <= '\u0217')
					|| (c >= '\u0250' && c <= '\u02A8')
					|| (c >= '\u02BB' && c <= '\u02C1') || (c == '\u0386')
					|| (c >= '\u0388' && c <= '\u038A') || (c == '\u038C')
					|| (c >= '\u038E' && c <= '\u03A1')
					|| (c >= '\u03A3' && c <= '\u03CE')
					|| (c >= '\u03D0' && c <= '\u03D6') || (c == '\u03DA')
					|| (c == '\u03DC') || (c == '\u03DE') || (c == '\u03E0')
					|| (c >= '\u03E2' && c <= '\u03F3')
					|| (c >= '\u0401' && c <= '\u040C')
					|| (c >= '\u040E' && c <= '\u044F')
					|| (c >= '\u0451' && c <= '\u045C')
					|| (c >= '\u045E' && c <= '\u0481')
					|| (c >= '\u0490' && c <= '\u04C4')
					|| (c >= '\u04C7' && c <= '\u04C8')
					|| (c >= '\u04CB' && c <= '\u04CC')
					|| (c >= '\u04D0' && c <= '\u04EB')
					|| (c >= '\u04EE' && c <= '\u04F5')
					|| (c >= '\u04F8' && c <= '\u04F9')
					|| (c >= '\u0531' && c <= '\u0556') || (c == '\u0559')
					|| (c >= '\u0561' && c <= '\u0586')
					|| (c >= '\u05D0' && c <= '\u05EA')
					|| (c >= '\u05F0' && c <= '\u05F2')
					|| (c >= '\u0621' && c <= '\u063A')
					|| (c >= '\u0641' && c <= '\u064A')
					|| (c >= '\u0671' && c <= '\u06B7')
					|| (c >= '\u06BA' && c <= '\u06BE')
					|| (c >= '\u06C0' && c <= '\u06CE')
					|| (c >= '\u06D0' && c <= '\u06D3') || (c == '\u06D5')
					|| (c >= '\u06E5' && c <= '\u06E6')
					|| (c >= '\u0905' && c <= '\u0939') || (c == '\u093D')
					|| (c >= '\u0958' && c <= '\u0961')
					|| (c >= '\u0985' && c <= '\u098C')
					|| (c >= '\u098F' && c <= '\u0990')
					|| (c >= '\u0993' && c <= '\u09A8')
					|| (c >= '\u09AA' && c <= '\u09B0') || (c == '\u09B2')
					|| (c >= '\u09B6' && c <= '\u09B9')
					|| (c >= '\u09DC' && c <= '\u09DD')
					|| (c >= '\u09DF' && c <= '\u09E1')
					|| (c >= '\u09F0' && c <= '\u09F1')
					|| (c >= '\u0A05' && c <= '\u0A0A')
					|| (c >= '\u0A0F' && c <= '\u0A10')
					|| (c >= '\u0A13' && c <= '\u0A28')
					|| (c >= '\u0A2A' && c <= '\u0A30')
					|| (c >= '\u0A32' && c <= '\u0A33')
					|| (c >= '\u0A35' && c <= '\u0A36')
					|| (c >= '\u0A38' && c <= '\u0A39')
					|| (c >= '\u0A59' && c <= '\u0A5C') || (c == '\u0A5E')
					|| (c >= '\u0A72' && c <= '\u0A74')
					|| (c >= '\u0A85' && c <= '\u0A8B') || (c == '\u0A8D')
					|| (c >= '\u0A8F' && c <= '\u0A91')
					|| (c >= '\u0A93' && c <= '\u0AA8')
					|| (c >= '\u0AAA' && c <= '\u0AB0')
					|| (c >= '\u0AB2' && c <= '\u0AB3')
					|| (c >= '\u0AB5' && c <= '\u0AB9') || (c == '\u0ABD')
					|| (c == '\u0AE0') || (c >= '\u0B05' && c <= '\u0B0C')
					|| (c >= '\u0B0F' && c <= '\u0B10')
					|| (c >= '\u0B13' && c <= '\u0B28')
					|| (c >= '\u0B2A' && c <= '\u0B30')
					|| (c >= '\u0B32' && c <= '\u0B33')
					|| (c >= '\u0B36' && c <= '\u0B39') || (c == '\u0B3D')
					|| (c >= '\u0B5C' && c <= '\u0B5D')
					|| (c >= '\u0B5F' && c <= '\u0B61')
					|| (c >= '\u0B85' && c <= '\u0B8A')
					|| (c >= '\u0B8E' && c <= '\u0B90')
					|| (c >= '\u0B92' && c <= '\u0B95')
					|| (c >= '\u0B99' && c <= '\u0B9A') || (c == '\u0B9C')
					|| (c >= '\u0B9E' && c <= '\u0B9F')
					|| (c >= '\u0BA3' && c <= '\u0BA4')
					|| (c >= '\u0BA8' && c <= '\u0BAA')
					|| (c >= '\u0BAE' && c <= '\u0BB5')
					|| (c >= '\u0BB7' && c <= '\u0BB9')
					|| (c >= '\u0C05' && c <= '\u0C0C')
					|| (c >= '\u0C0E' && c <= '\u0C10')
					|| (c >= '\u0C12' && c <= '\u0C28')
					|| (c >= '\u0C2A' && c <= '\u0C33')
					|| (c >= '\u0C35' && c <= '\u0C39')
					|| (c >= '\u0C60' && c <= '\u0C61')
					|| (c >= '\u0C85' && c <= '\u0C8C')
					|| (c >= '\u0C8E' && c <= '\u0C90')
					|| (c >= '\u0C92' && c <= '\u0CA8')
					|| (c >= '\u0CAA' && c <= '\u0CB3')
					|| (c >= '\u0CB5' && c <= '\u0CB9') || (c == '\u0CDE')
					|| (c >= '\u0CE0' && c <= '\u0CE1')
					|| (c >= '\u0D05' && c <= '\u0D0C')
					|| (c >= '\u0D0E' && c <= '\u0D10')
					|| (c >= '\u0D12' && c <= '\u0D28')
					|| (c >= '\u0D2A' && c <= '\u0D39')
					|| (c >= '\u0D60' && c <= '\u0D61')
					|| (c >= '\u0E01' && c <= '\u0E2E') || (c == '\u0E30')
					|| (c >= '\u0E32' && c <= '\u0E33')
					|| (c >= '\u0E40' && c <= '\u0E45')
					|| (c >= '\u0E81' && c <= '\u0E82') || (c == '\u0E84')
					|| (c >= '\u0E87' && c <= '\u0E88') || (c == '\u0E8A')
					|| (c == '\u0E8D') || (c >= '\u0E94' && c <= '\u0E97')
					|| (c >= '\u0E99' && c <= '\u0E9F')
					|| (c >= '\u0EA1' && c <= '\u0EA3') || (c == '\u0EA5')
					|| (c == '\u0EA7') || (c >= '\u0EAA' && c <= '\u0EAB')
					|| (c >= '\u0EAD' && c <= '\u0EAE') || (c == '\u0EB0')
					|| (c >= '\u0EB2' && c <= '\u0EB3') || (c == '\u0EBD')
					|| (c >= '\u0EC0' && c <= '\u0EC4')
					|| (c >= '\u0F40' && c <= '\u0F47')
					|| (c >= '\u0F49' && c <= '\u0F69')
					|| (c >= '\u10A0' && c <= '\u10C5')
					|| (c >= '\u10D0' && c <= '\u10F6') || (c == '\u1100')
					|| (c >= '\u1102' && c <= '\u1103')
					|| (c >= '\u1105' && c <= '\u1107') || (c == '\u1109')
					|| (c >= '\u110B' && c <= '\u110C')
					|| (c >= '\u110E' && c <= '\u1112') || (c == '\u113C')
					|| (c == '\u113E') || (c == '\u1140') || (c == '\u114C')
					|| (c == '\u114E') || (c == '\u1150')
					|| (c >= '\u1154' && c <= '\u1155') || (c == '\u1159')
					|| (c >= '\u115F' && c <= '\u1161') || (c == '\u1163')
					|| (c == '\u1165') || (c == '\u1167') || (c == '\u1169')
					|| (c >= '\u116D' && c <= '\u116E')
					|| (c >= '\u1172' && c <= '\u1173') || (c == '\u1175')
					|| (c == '\u119E') || (c == '\u11A8') || (c == '\u11AB')
					|| (c >= '\u11AE' && c <= '\u11AF')
					|| (c >= '\u11B7' && c <= '\u11B8') || (c == '\u11BA')
					|| (c >= '\u11BC' && c <= '\u11C2') || (c == '\u11EB')
					|| (c == '\u11F0') || (c == '\u11F9')
					|| (c >= '\u1E00' && c <= '\u1E9B')
					|| (c >= '\u1EA0' && c <= '\u1EF9')
					|| (c >= '\u1F00' && c <= '\u1F15')
					|| (c >= '\u1F18' && c <= '\u1F1D')
					|| (c >= '\u1F20' && c <= '\u1F45')
					|| (c >= '\u1F48' && c <= '\u1F4D')
					|| (c >= '\u1F50' && c <= '\u1F57') || (c == '\u1F59')
					|| (c == '\u1F5B') || (c == '\u1F5D')
					|| (c >= '\u1F5F' && c <= '\u1F7D')
					|| (c >= '\u1F80' && c <= '\u1FB4')
					|| (c >= '\u1FB6' && c <= '\u1FBC') || (c == '\u1FBE')
					|| (c >= '\u1FC2' && c <= '\u1FC4')
					|| (c >= '\u1FC6' && c <= '\u1FCC')
					|| (c >= '\u1FD0' && c <= '\u1FD3')
					|| (c >= '\u1FD6' && c <= '\u1FDB')
					|| (c >= '\u1FE0' && c <= '\u1FEC')
					|| (c >= '\u1FF2' && c <= '\u1FF4')
					|| (c >= '\u1FF6' && c <= '\u1FFC') || (c == '\u2126')
					|| (c >= '\u212A' && c <= '\u212B') || (c == '\u212E')
					|| (c >= '\u2180' && c <= '\u2182')
					|| (c >= '\u3041' && c <= '\u3094')
					|| (c >= '\u30A1' && c <= '\u30FA')
					|| (c >= '\u3105' && c <= '\u312C')
					|| (c >= '\uAC00' && c <= '\uD7A3')
					|| (c >= '\u4E00' && c <= '\u9FA5') || (c == '\u3007')
					|| (c >= '\u3021' && c <= '\u3029') || (c == '_') || (c == '.')
					|| (c == '-') || (c >= '\u0300' && c <= '\u0345')
					|| (c >= '\u0360' && c <= '\u0361')
					|| (c >= '\u0483' && c <= '\u0486')
					|| (c >= '\u0591' && c <= '\u05A1')
					|| (c >= '\u05A3' && c <= '\u05B9')
					|| (c >= '\u05BB' && c <= '\u05BD') || (c == '\u05BF')
					|| (c >= '\u05C1' && c <= '\u05C2') || (c == '\u05C4')
					|| (c >= '\u064B' && c <= '\u0652') || (c == '\u0670')
					|| (c >= '\u06D6' && c <= '\u06DC')
					|| (c >= '\u06DD' && c <= '\u06DF')
					|| (c >= '\u06E0' && c <= '\u06E4')
					|| (c >= '\u06E7' && c <= '\u06E8')
					|| (c >= '\u06EA' && c <= '\u06ED')
					|| (c >= '\u0901' && c <= '\u0903') || (c == '\u093C')
					|| (c >= '\u093E' && c <= '\u094C') || (c == '\u094D')
					|| (c >= '\u0951' && c <= '\u0954')
					|| (c >= '\u0962' && c <= '\u0963')
					|| (c >= '\u0981' && c <= '\u0983') || (c == '\u09BC')
					|| (c == '\u09BE') || (c == '\u09BF')
					|| (c >= '\u09C0' && c <= '\u09C4')
					|| (c >= '\u09C7' && c <= '\u09C8')
					|| (c >= '\u09CB' && c <= '\u09CD') || (c == '\u09D7')
					|| (c >= '\u09E2' && c <= '\u09E3') || (c == '\u0A02')
					|| (c == '\u0A3C') || (c == '\u0A3E') || (c == '\u0A3F')
					|| (c >= '\u0A40' && c <= '\u0A42')
					|| (c >= '\u0A47' && c <= '\u0A48')
					|| (c >= '\u0A4B' && c <= '\u0A4D')
					|| (c >= '\u0A70' && c <= '\u0A71')
					|| (c >= '\u0A81' && c <= '\u0A83') || (c == '\u0ABC')
					|| (c >= '\u0ABE' && c <= '\u0AC5')
					|| (c >= '\u0AC7' && c <= '\u0AC9')
					|| (c >= '\u0ACB' && c <= '\u0ACD')
					|| (c >= '\u0B01' && c <= '\u0B03') || (c == '\u0B3C')
					|| (c >= '\u0B3E' && c <= '\u0B43')
					|| (c >= '\u0B47' && c <= '\u0B48')
					|| (c >= '\u0B4B' && c <= '\u0B4D')
					|| (c >= '\u0B56' && c <= '\u0B57')
					|| (c >= '\u0B82' && c <= '\u0B83')
					|| (c >= '\u0BBE' && c <= '\u0BC2')
					|| (c >= '\u0BC6' && c <= '\u0BC8')
					|| (c >= '\u0BCA' && c <= '\u0BCD') || (c == '\u0BD7')
					|| (c >= '\u0C01' && c <= '\u0C03')
					|| (c >= '\u0C3E' && c <= '\u0C44')
					|| (c >= '\u0C46' && c <= '\u0C48')
					|| (c >= '\u0C4A' && c <= '\u0C4D')
					|| (c >= '\u0C55' && c <= '\u0C56')
					|| (c >= '\u0C82' && c <= '\u0C83')
					|| (c >= '\u0CBE' && c <= '\u0CC4')
					|| (c >= '\u0CC6' && c <= '\u0CC8')
					|| (c >= '\u0CCA' && c <= '\u0CCD')
					|| (c >= '\u0CD5' && c <= '\u0CD6')
					|| (c >= '\u0D02' && c <= '\u0D03')
					|| (c >= '\u0D3E' && c <= '\u0D43')
					|| (c >= '\u0D46' && c <= '\u0D48')
					|| (c >= '\u0D4A' && c <= '\u0D4D') || (c == '\u0D57')
					|| (c == '\u0E31') || (c >= '\u0E34' && c <= '\u0E3A')
					|| (c >= '\u0E47' && c <= '\u0E4E') || (c == '\u0EB1')
					|| (c >= '\u0EB4' && c <= '\u0EB9')
					|| (c >= '\u0EBB' && c <= '\u0EBC')
					|| (c >= '\u0EC8' && c <= '\u0ECD')
					|| (c >= '\u0F18' && c <= '\u0F19') || (c == '\u0F35')
					|| (c == '\u0F37') || (c == '\u0F39') || (c == '\u0F3E')
					|| (c == '\u0F3F') || (c >= '\u0F71' && c <= '\u0F84')
					|| (c >= '\u0F86' && c <= '\u0F8B')
					|| (c >= '\u0F90' && c <= '\u0F95') || (c == '\u0F97')
					|| (c >= '\u0F99' && c <= '\u0FAD')
					|| (c >= '\u0FB1' && c <= '\u0FB7') || (c == '\u0FB9')
					|| (c >= '\u20D0' && c <= '\u20DC') || (c == '\u20E1')
					|| (c >= '\u302A' && c <= '\u302F') || (c == '\u3099')
					|| (c == '\u309A') || (c == '\u00B7') || (c == '\u02D0')
					|| (c == '\u02D1') || (c == '\u0387') || (c == '\u0640')
					|| (c == '\u0E46') || (c == '\u0EC6') || (c == '\u3005')
					|| (c >= '\u3031' && c <= '\u3035')
					|| (c >= '\u309D' && c <= '\u309E') || (c >= '\u30FC' && c <= '\u30FE'));
		}

		public static bool IsNCName(string str)
		{
			if (str == null)
			{
				return false;
			}
			else
			{
				int len = str.Length;
				switch (len)
				{
					case 0:
						return false;
					case 1:
						return NCName.IsNCNameStart(str[0]);
					default:
						if (!NCName.IsNCNameStart(str[0]))
						{
							return false;
						}
						for (int i = 1; i < len; i++)
						{
							if (!NCName.IsNCNameTrail(str[i]))
							{
								return false;
							}
						}

						return true;
				}
			}
		}

		private static void AppendUHexTo(StringBuilder sb, int c)
		{
			sb.Append('U');
			for (int i = 0; i < 6; i++)
			{
				sb.Append(HEX_TABLE[(c & 0xF00000) >> 20]);
				c <<= 4;
			}
		}

		public static string EscapeName(string str)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if ((c & 0xFC00) == 0xD800)
				{
					char next = str[++i];
					AppendUHexTo(sb, (c << 10) + next + SURROGATE_OFFSET);
				}
				else if (i == 0 && !IsNCNameStart(c))
				{
					AppendUHexTo(sb, c);
				}
				else if (i != 0 && !IsNCNameTrail(c))
				{
					AppendUHexTo(sb, c);
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}
		// ]NOCPP]
	}

}
