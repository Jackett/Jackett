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

/*
 * The comments following this one that use the same comment syntax as this 
 * comment are quotes from the WHATWG HTML 5 spec as of 27 June 2007 
 * amended as of June 28 2007.
 * That document came with this statement:
 * © Copyright 2004-2007 Apple Computer, Inc., Mozilla Foundation, and 
 * Opera Software ASA. You are granted a license to use, reproduce and 
 * create derivative works of this document."
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using HtmlParserSharp.Common;
using System.Xml;
using System.Text;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	public abstract class TreeBuilder<T> : ITokenHandler, ITreeBuilderState<T> where T : class
	{
		private InsertionMode mode = InsertionMode.INITIAL;

		private InsertionMode originalMode = InsertionMode.INITIAL;

		/// <summary>
		/// Used only when moving back to IN_BODY.
		/// </summary>
		private bool framesetOk = true;

		protected Tokenizer tokenizer;

		// [NOCPP[

		public event EventHandler<DocumentModeEventArgs> DocumentModeDetected;

		public DoctypeExpectation DoctypeExpectation { get; set; }

		// ]NOCPP]

		public bool IsScriptingEnabled { get; set; }

		private bool needToDropLF;

		private bool fragment;

		[Local]
		private string contextName;

		[NsUri]
		private string contextNamespace;

		private T contextNode;

		private StackNode<T>[] stack;

		private int currentPtr = -1;

		private StackNode<T>[] listOfActiveFormattingElements;

		private int listPtr = -1;

		private T formPointer;

		private T headPointer;

		/**
		 * Used to work around Gecko limitations. Not used in Java.
		 */
		private T deepTreeSurrogateParent;

		//protected char[] charBuffer;
        protected StringBuilder charBuffer;

		protected int charBufferLen 
        { 
            get {
                return charBuffer.Length;
            } 
        }

		private bool quirks = false;

		// [NOCPP[

		public bool IsReportingDoctype { get; set; }

		public XmlViolationPolicy NamePolicy { get; set; }

		// stores the first occurrences of IDs
		private readonly Dictionary<string, Locator> idLocations = new Dictionary<string, Locator>();

		private bool html4;

		// ]NOCPP]

		protected TreeBuilder()
		{
			fragment = false;
			IsReportingDoctype = true;
			DoctypeExpectation = DoctypeExpectation.Html;
			NamePolicy = XmlViolationPolicy.AlterInfoset;
			IsScriptingEnabled = false;
		}

		/// <summary>
		/// Reports an condition that would make the infoset incompatible with XML
		/// 1.0 as fatal.
		/// </summary>
		protected void Fatal()
		{
			// TODO: why is this empty (in original code)?
		}

		// [NOCPP[

		protected void Fatal(Exception e)
		{
			//SAXParseException spe = new SAXParseException(e.getMessage(),
			//        tokenizer, e);
			//if (ErrorEvent != null) {
			//    errorHandler.fatalError(spe);
			//}
			//throw spe;

			throw e; // TODO
		}

		internal void Fatal(string s)
		{
			//SAXParseException spe = new SAXParseException(s, tokenizer);
			//if (ErrorEvent != null) {
			//    errorHandler.fatalError(spe);
			//}
			//throw spe;

			throw new Exception(s); // TODO
		}

		public event EventHandler<ParserErrorEventArgs> ErrorEvent;

		/// <summary>
		/// Reports a Parse Error.
		/// </summary>
		/// <param name="message">The message.</param>
		void Err(string message)
		{
			if (ErrorEvent != null)
			{
				ErrNoCheck(message);
			}
		}

		/// <summary>
		/// Reports a Parse Error without checking if an error handler is present.
		/// </summary>
		/// <param name="message">The message.</param>
		void ErrNoCheck(string message)
		{
			ErrorEvent(this, new ParserErrorEventArgs(message, false));
		}

		/// <summary>
		/// Reports a stray start tag.
		/// </summary>
		/// <param name="name">The name of the stray tag.</param>
		private void ErrStrayStartTag(string name)
		{
			Err("Stray end tag \u201C" + name + "\u201D.");
		}

		/// <summary>
		/// Reports a stray end tag.
		/// </summary>
		/// <param name="name">The name of the stray tag.</param>
		private void ErrStrayEndTag(string name)
		{
			Err("Stray end tag \u201C" + name + "\u201D.");
		}

		/// <summary>
		/// Reports a state when elements expected to be closed were not.
		/// </summary>
		/// <param name="eltPos">The position of the start tag on the stack of the element
		/// being closed.</param>
		/// <param name="name">The name of the end tag.</param>
		private void ErrUnclosedElements(int eltPos, string name)
		{
			Err("End tag \u201C" + name + "\u201D seen, but there were open elements.");
			ErrListUnclosedStartTags(eltPos);
		}

		/// <summary>
		/// Reports a state when elements expected to be closed ahead of an implied
		/// end tag but were not.
		/// </summary>
		/// <param name="eltPos">The position of the start tag on the stack of the element
		/// being closed.</param>
		/// <param name="name">The name of the end tag.</param>
		private void ErrUnclosedElementsImplied(int eltPos, string name)
		{
			Err("End tag \u201C" + name + "\u201D implied, but there were open elements.");
			ErrListUnclosedStartTags(eltPos);
		}

		/// <summary>
		/// Reports a state when elements expected to be closed ahead of an implied
		/// table cell close.
		/// </summary>
		/// <param name="eltPos">The position of the start tag on the stack of the element
		/// being closed.</param>
		private void ErrUnclosedElementsCell(int eltPos)
		{
			Err("A table cell was implicitly closed, but there were open elements.");
			ErrListUnclosedStartTags(eltPos);
		}

		private void ErrListUnclosedStartTags(int eltPos)
		{
			if (currentPtr != -1)
			{
				for (int i = currentPtr; i > eltPos; i--)
				{
					ReportUnclosedElementNameAndLocation(i);
				}
			}
		}

		/// <summary>
		/// Reports arriving at/near end of document with unclosed elements remaining.
		/// </summary>
		/// <param name="message">The message.</param>
		private void ErrEndWithUnclosedElements(string message)
		{
			if (ErrorEvent == null)
			{
				return;
			}
			ErrNoCheck(message);
			// just report all remaining unclosed elements
			ErrListUnclosedStartTags(0);
		}

		/// <summary>
		/// Reports the name and location of an unclosed element.
		/// </summary>
		/// <param name="pos">The position.</param>
		private void ReportUnclosedElementNameAndLocation(int pos)
		{
			StackNode<T> node = stack[pos];
			if (node.IsOptionalEndTag)
			{
				return;
			}
			TaintableLocator locator = node.Locator;
			if (locator.IsTainted)
			{
				return;
			}
			locator.MarkTainted();
			//SAXParseException spe = new SAXParseException(
			//        "Unclosed element \u201C" + node.popName + "\u201D.", locator);
			//errorHandler.error(spe);
			ErrNoCheck("Unclosed element \u201C" + node.popName + "\u201D.");
		}

		/// <summary>
		/// Reports a warning
		/// </summary>
		/// <param name="message">The message.</param>
		internal void Warn(string message)
		{
			if (ErrorEvent != null)
			{
				//SAXParseException spe = new SAXParseException(message, tokenizer);
				//errorHandler.warning(spe);
				ErrorEvent(this, new ParserErrorEventArgs(message, true));
			}
		}

		// ]NOCPP]

		public void StartTokenization(Tokenizer self)
		{
			tokenizer = self;
			stack = new StackNode<T>[64];
			listOfActiveFormattingElements = new StackNode<T>[64];
			needToDropLF = false;
			originalMode = InsertionMode.INITIAL;
			currentPtr = -1;
			listPtr = -1;
			formPointer = null;
			headPointer = null;
			deepTreeSurrogateParent = null;
			// [NOCPP[
			html4 = false;
			idLocations.Clear();
            // removed - this overrides end-user settings, resuling in no comments always
			//wantingComments = false;
			// ]NOCPP]
			Start(fragment);
            //charBuffer = new StringBuilder(10240);
            charBuffer = new StringBuilder();
            charBuffer.Clear();
			framesetOk = true;
			if (fragment)
			{
				T elt;
				if (contextNode != null)
				{
					elt = contextNode;
				}
				else
				{
					elt = CreateHtmlElementSetAsRoot(tokenizer.EmptyAttributes());
				}
				StackNode<T> node = new StackNode<T>(ElementName.HTML, elt
					// [NOCPP[
						, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
					// ]NOCPP]
				);
				currentPtr++;
				stack[currentPtr] = node;
				ResetTheInsertionMode();
				if ("title" == contextName || "textarea" == contextName)
				{
                    tokenizer.SetStateAndEndTagExpectation(TokenizerState.RCDATA, contextName);
				}
				else if ("style" == contextName || "xmp" == contextName
					  || "iframe" == contextName || "noembed" == contextName
					  || "noframes" == contextName
					  || (IsScriptingEnabled && "noscript" == contextName))
				{
                    tokenizer.SetStateAndEndTagExpectation(TokenizerState.RAWTEXT, contextName);
				}
				else if ("plaintext" == contextName)
				{
                    tokenizer.SetStateAndEndTagExpectation(TokenizerState.PLAINTEXT, contextName);
				}
				else if ("script" == contextName)
				{
					tokenizer.SetStateAndEndTagExpectation(TokenizerState.SCRIPT_DATA,contextName);
				}
				else
				{
                    tokenizer.SetStateAndEndTagExpectation(TokenizerState.DATA, contextName);
				}
				contextName = null;
				contextNode = null;
			}
			else
			{
				mode = InsertionMode.INITIAL;
			}
		}

		public void Doctype([Local] string name, string publicIdentifier, string systemIdentifier, bool forceQuirks)
		{
			needToDropLF = false;
			if (!IsInForeign)
			{
				switch (mode)
				{
					case InsertionMode.INITIAL:
						// [NOCPP[
						if (IsReportingDoctype)
						{
							// ]NOCPP]
							AppendDoctypeToDocument(name == null ? "" : name,
									publicIdentifier == null ? String.Empty
											: publicIdentifier,
									systemIdentifier == null ? String.Empty
											: systemIdentifier);
							// [NOCPP[
						}
						switch (DoctypeExpectation)
						{
							case DoctypeExpectation.Html:
								// ]NOCPP]
								if (IsQuirky(name, publicIdentifier,
										systemIdentifier, forceQuirks))
								{
									Err("Quirky doctype. Expected \u201C<!DOCTYPE html>\u201D.");
									DocumentModeInternal(DocumentMode.QuirksMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								else if (IsAlmostStandards(publicIdentifier,
									  systemIdentifier))
								{
									Err("Almost standards mode doctype. Expected \u201C<!DOCTYPE html>\u201D.");
									DocumentModeInternal(
											DocumentMode.AlmostStandardsMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								else
								{
									// [NOCPP[
									if (("-//W3C//DTD HTML 4.0//EN" == publicIdentifier &&
										(systemIdentifier == null || "http://www.w3.org/TR/REC-html40/strict.dtd" == systemIdentifier))
											|| ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier &&
												(systemIdentifier == null || "http://www.w3.org/TR/html4/strict.dtd" == systemIdentifier))
											|| ("-//W3C//DTD XHTML 1.0 Strict//EN" == publicIdentifier && 
													"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd" == systemIdentifier)
											|| ("-//W3C//DTD XHTML 1.1//EN" == publicIdentifier &&
													"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd" == systemIdentifier)

									)
									{
										Warn("Obsolete doctype. Expected \u201C<!DOCTYPE html>\u201D.");
									}
									else if (!((systemIdentifier == null || "about:legacy-compat" == systemIdentifier) &&
										publicIdentifier == null))
									{
										Err("Legacy doctype. Expected \u201C<!DOCTYPE html>\u201D.");
									}
									// ]NOCPP]
									DocumentModeInternal(
											DocumentMode.StandardsMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								// [NOCPP[
								break;
							case DoctypeExpectation.Html401Strict:
								html4 = true;
								tokenizer.TurnOnAdditionalHtml4Errors();
								if (IsQuirky(name, publicIdentifier,
										systemIdentifier, forceQuirks))
								{
									Err("Quirky doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
									DocumentModeInternal(DocumentMode.QuirksMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								else if (IsAlmostStandards(publicIdentifier,
									  systemIdentifier))
								{
									Err("Almost standards mode doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
									DocumentModeInternal(
											DocumentMode.AlmostStandardsMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								else
								{
									if ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier)
									{
										if ("http://www.w3.org/TR/html4/strict.dtd" != systemIdentifier)
										{
											Warn("The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
										}
									}
									else
									{
										Err("The doctype was not the HTML 4.01 Strict doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
									}
									DocumentModeInternal(
											DocumentMode.StandardsMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								break;
							case DoctypeExpectation.Html401Transitional:
								html4 = true;
								tokenizer.TurnOnAdditionalHtml4Errors();
								if (IsQuirky(name, publicIdentifier,
										systemIdentifier, forceQuirks))
								{
									Err("Quirky doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
									DocumentModeInternal(DocumentMode.QuirksMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								else if (IsAlmostStandards(publicIdentifier,
									  systemIdentifier))
								{
									if ("-//W3C//DTD HTML 4.01 Transitional//EN" == publicIdentifier
											&& systemIdentifier != null)
									{
										if ("http://www.w3.org/TR/html4/loose.dtd" != systemIdentifier)
										{
											Warn("The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
										}
									}
									else
									{
										Err("The doctype was not a non-quirky HTML 4.01 Transitional doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
									}
									DocumentModeInternal(
											DocumentMode.AlmostStandardsMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								else
								{
									Err("The doctype was not the HTML 4.01 Transitional doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
									DocumentModeInternal(
											DocumentMode.StandardsMode,
											publicIdentifier, systemIdentifier,
											true);
								}
								break;
							case DoctypeExpectation.Auto:
								html4 = IsHtml4Doctype(publicIdentifier);
								if (html4)
								{
									tokenizer.TurnOnAdditionalHtml4Errors();
								}
								if (IsQuirky(name, publicIdentifier,
										systemIdentifier, forceQuirks))
								{
									Err("Quirky doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
									DocumentModeInternal(DocumentMode.QuirksMode,
											publicIdentifier, systemIdentifier,
											html4);
								}
								else if (IsAlmostStandards(publicIdentifier,
									  systemIdentifier))
								{
									if ("-//W3C//DTD HTML 4.01 Transitional//EN" == publicIdentifier)
									{
										if ("http://www.w3.org/TR/html4/loose.dtd" != systemIdentifier)
										{
											Warn("The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
										}
									}
									else
									{
										Err("Almost standards mode doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
									}
									DocumentModeInternal(
											DocumentMode.AlmostStandardsMode,
											publicIdentifier, systemIdentifier,
											html4);
								}
								else
								{
									if ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier)
									{
										if ("http://www.w3.org/TR/html4/strict.dtd" != systemIdentifier)
										{
											Warn("The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
										}
									}
									else
									{
										if (!(publicIdentifier == null && systemIdentifier == null))
										{
											Err("Legacy doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
										}
									}
									DocumentModeInternal(
											DocumentMode.StandardsMode,
											publicIdentifier, systemIdentifier,
											html4);
								}
								break;
							case DoctypeExpectation.NoDoctypeErrors:
								if (IsQuirky(name, publicIdentifier,
										systemIdentifier, forceQuirks))
								{
									DocumentModeInternal(DocumentMode.QuirksMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								else if (IsAlmostStandards(publicIdentifier,
									  systemIdentifier))
								{
									DocumentModeInternal(
											DocumentMode.AlmostStandardsMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								else
								{
									DocumentModeInternal(
											DocumentMode.StandardsMode,
											publicIdentifier, systemIdentifier,
											false);
								}
								break;
						}
						// ]NOCPP]

						/*
						 * 
						 * Then, switch to the root element mode of the tree
						 * construction stage.
						 */
						mode = InsertionMode.BEFORE_HTML;
						return;
					default:
						break;
				}
			}
			/*
			 * A DOCTYPE token Parse error.
			 */
			Err("Stray doctype.");
			/*
			 * Ignore the token.
			 */
			return;
		}

		// [NOCPP[

		private bool IsHtml4Doctype(string publicIdentifier)
		{
			if (publicIdentifier != null
					&& (Array.BinarySearch<string>(TreeBuilderConstants.HTML4_PUBLIC_IDS,
							publicIdentifier) > -1))
			{
				return true;
			}
			return false;
		}

		// ]NOCPP]

		/// <summary>
		/// Receive a comment token. The data is junk if the<code>wantsComments()</code>
		/// returned <code>false</code>.
		/// </summary>
		/// <param name="buf">The buffer holding the data.</param>
		/// <param name="start">The offset into the buffer.</param>
		/// <param name="length">The number of code units to read.</param>
		public void Comment(char[] buf, int start, int length)
		{
			needToDropLF = false;
			// [NOCPP[
			if (!WantsComments)
			{
				return;
			}
			// ]NOCPP]
			if (!IsInForeign)
			{
				switch (mode)
				{
					case InsertionMode.INITIAL:
					case InsertionMode.BEFORE_HTML:
					case InsertionMode.AFTER_AFTER_BODY:
					case InsertionMode.AFTER_AFTER_FRAMESET:
						/*
						 * A comment token Append a Comment node to the Document
						 * object with the data attribute set to the data given in
						 * the comment token.
						 */
						AppendCommentToDocument(buf, start, length);
						return;
					case InsertionMode.AFTER_BODY:
						/*
						 * A comment token Append a Comment node to the first
						 * element in the stack of open elements (the html element),
						 * with the data attribute set to the data given in the
						 * comment token.
						 */
						FlushCharacters();
						AppendComment(stack[0].node, buf, start, length);
						return;
					default:
						break;
				}
			}
			/*
			 * A comment token Append a Comment node to the current node with the
			 * data attribute set to the data given in the comment token.
			 */
			FlushCharacters();
			AppendComment(stack[currentPtr].node, buf, start, length);
			return;
		}

		/// <summary>
		/// Receive character tokens. This method has the same semantics as the SAX
		/// method of the same name.
		/// </summary>
		/// <param name="buf">A buffer holding the data.</param>
		/// <param name="start">The offset into the buffer.</param>
		/// <param name="length">The number of code units to read.</param>
		public void Characters(char[] buf, int start, int length)
		{
			if (needToDropLF)
			{
				needToDropLF = false;
				if (buf[start] == '\n')
				{
					start++;
					length--;
					if (length == 0)
					{
						return;
					}
				}
			}

			// optimize the most common case
			switch (mode)
			{
				case InsertionMode.IN_BODY:
				case InsertionMode.IN_CELL:
				case InsertionMode.IN_CAPTION:
                    if (!IsInForeignButNotHtmlOrMathTextIntegrationPoint)
					{
						ReconstructTheActiveFormattingElements();
					}
					// fall through
					goto case InsertionMode.TEXT;
				case InsertionMode.TEXT:
					AccumulateCharacters(buf, start, length);
					return;
				case InsertionMode.IN_TABLE:
				case InsertionMode.IN_TABLE_BODY:
				case InsertionMode.IN_ROW:
					AccumulateCharactersForced(buf, start, length);
					return;
				default:
					int end = start + length;
					/*charactersloop:*/
					for (int i = start; i < end; i++)
					{
						switch (buf[i])
						{
							case ' ':
							case '\t':
							case '\n':
							case '\r':
							case '\u000C':
								/*
								 * A character token that is one of one of U+0009
								 * CHARACTER TABULATION, U+000A LINE FEED (LF),
								 * U+000C FORM FEED (FF), or U+0020 SPACE
								 */
								switch (mode)
								{
									case InsertionMode.INITIAL:
									case InsertionMode.BEFORE_HTML:
									case InsertionMode.BEFORE_HEAD:
										/*
										 * Ignore the token.
										 */
										start = i + 1;
										continue;
									case InsertionMode.IN_HEAD:
									case InsertionMode.IN_HEAD_NOSCRIPT:
									case InsertionMode.AFTER_HEAD:
									case InsertionMode.IN_COLUMN_GROUP:
									case InsertionMode.IN_FRAMESET:
									case InsertionMode.AFTER_FRAMESET:
										/*
										 * Append the character to the current node.
										 */
										continue;
									case InsertionMode.FRAMESET_OK:
									case InsertionMode.IN_BODY:
									case InsertionMode.IN_CELL:
									case InsertionMode.IN_CAPTION:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}

										/*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
                                        if (!IsInForeignButNotHtmlOrMathTextIntegrationPoint)
										{
											FlushCharacters();
											ReconstructTheActiveFormattingElements();
										}
										/*
										 * Append the token's character to the
										 * current node.
										 */
										goto continueCharactersloop;
									case InsertionMode.IN_SELECT:
									case InsertionMode.IN_SELECT_IN_TABLE:
										goto continueCharactersloop;
									case InsertionMode.IN_TABLE:
									case InsertionMode.IN_TABLE_BODY:
									case InsertionMode.IN_ROW:
										AccumulateCharactersForced(buf, i, 1);
										start = i + 1;
										continue;
									case InsertionMode.AFTER_BODY:
									case InsertionMode.AFTER_AFTER_BODY:
									case InsertionMode.AFTER_AFTER_FRAMESET:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
										FlushCharacters();
										ReconstructTheActiveFormattingElements();
										/*
										 * Append the token's character to the
										 * current node.
										 */
										continue;
								}
								goto default;
							default:
								/*
								 * A character token that is not one of one of
								 * U+0009 CHARACTER TABULATION, U+000A LINE FEED
								 * (LF), U+000C FORM FEED (FF), or U+0020 SPACE
								 */
								switch (mode)
								{
									case InsertionMode.INITIAL:
										/*
										 * Parse error.
										 */
										// [NOCPP[
										switch (DoctypeExpectation)
										{
											case DoctypeExpectation.Auto:
												Err("Non-space characters found without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
												break;
											case DoctypeExpectation.Html:
												Err("Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
												break;
											case DoctypeExpectation.Html401Strict:
												Err("Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
												break;
											case DoctypeExpectation.Html401Transitional:
												Err("Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
												break;
											case DoctypeExpectation.NoDoctypeErrors:
												break;
										}
										// ]NOCPP]
										/*
										 * 
										 * Set the document to quirks mode.
										 */
										DocumentModeInternal(
												DocumentMode.QuirksMode, null,
												null, false);
										/*
										 * Then, switch to the root element mode of
										 * the tree construction stage
										 */
										mode = InsertionMode.BEFORE_HTML;
										/*
										 * and reprocess the current token.
										 */
										i--;
										continue;
									case InsertionMode.BEFORE_HTML:
										/*
										 * Create an HTMLElement node with the tag
										 * name html, in the HTML namespace. Append
										 * it to the Document object.
										 */
										// No need to flush characters here,
										// because there's nothing to flush.
										AppendHtmlElementToDocumentAndPush();
										/* Switch to the main mode */
										mode = InsertionMode.BEFORE_HEAD;
										/*
										 * reprocess the current token.
										 */
										i--;
										continue;
									case InsertionMode.BEFORE_HEAD:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * /Act as if a start tag token with the tag
										 * name "head" and no attributes had been
										 * seen,
										 */
										FlushCharacters();
										AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
										mode = InsertionMode.IN_HEAD;
										/*
										 * then reprocess the current token.
										 * 
										 * This will result in an empty head element
										 * being generated, with the current token
										 * being reprocessed in the "after head"
										 * insertion mode.
										 */
										i--;
										continue;
									case InsertionMode.IN_HEAD:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Act as if an end tag token with the tag
										 * name "head" had been seen,
										 */
										FlushCharacters();
										Pop();
										mode = InsertionMode.AFTER_HEAD;
										/*
										 * and reprocess the current token.
										 */
										i--;
										continue;
									case InsertionMode.IN_HEAD_NOSCRIPT:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Parse error. Act as if an end tag with
										 * the tag name "noscript" had been seen
										 */
										Err("Non-space character inside \u201Cnoscript\u201D inside \u201Chead\u201D.");
										FlushCharacters();
										Pop();
										mode = InsertionMode.IN_HEAD;
										/*
										 * and reprocess the current token.
										 */
										i--;
										continue;
									case InsertionMode.AFTER_HEAD:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Act as if a start tag token with the tag
										 * name "body" and no attributes had been
										 * seen,
										 */
										FlushCharacters();
										AppendToCurrentNodeAndPushBodyElement();
										mode = InsertionMode.FRAMESET_OK;
										/*
										 * and then reprocess the current token.
										 */
										i--;
										continue;
									case InsertionMode.FRAMESET_OK:
										framesetOk = false;
										mode = InsertionMode.IN_BODY;
										i--;
										continue;
									case InsertionMode.IN_BODY:
									case InsertionMode.IN_CELL:
									case InsertionMode.IN_CAPTION:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
                                        if (!IsInForeignButNotHtmlOrMathTextIntegrationPoint)
										{
											FlushCharacters();
											ReconstructTheActiveFormattingElements();
										}
										/*
										 * Append the token's character to the
										 * current node.
										 */
										goto continueCharactersloop;
									case InsertionMode.IN_TABLE:
									case InsertionMode.IN_TABLE_BODY:
									case InsertionMode.IN_ROW:
										AccumulateCharactersForced(buf, i, 1);
										start = i + 1;
										continue;
									case InsertionMode.IN_COLUMN_GROUP:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Act as if an end tag with the tag name
										 * "colgroup" had been seen, and then, if
										 * that token wasn't ignored, reprocess the
										 * current token.
										 */
										if (currentPtr == 0)
										{
											Err("Non-space in \u201Ccolgroup\u201D when parsing fragment.");
											start = i + 1;
											continue;
										}
										FlushCharacters();
										Pop();
										mode = InsertionMode.IN_TABLE;
										i--;
										continue;
									case InsertionMode.IN_SELECT:
									case InsertionMode.IN_SELECT_IN_TABLE:
										goto continueCharactersloop;
									case InsertionMode.AFTER_BODY:
										Err("Non-space character after body.");
										Fatal();
										mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
										i--;
										continue;
									case InsertionMode.IN_FRAMESET:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Parse error.
										 */
										Err("Non-space in \u201Cframeset\u201D.");
										/*
										 * Ignore the token.
										 */
										start = i + 1;
										continue;
									case InsertionMode.AFTER_FRAMESET:
										if (start < i)
										{
											AccumulateCharacters(buf, start, i
													- start);
											start = i;
										}
										/*
										 * Parse error.
										 */
										Err("Non-space after \u201Cframeset\u201D.");
										/*
										 * Ignore the token.
										 */
										start = i + 1;
										continue;
									case InsertionMode.AFTER_AFTER_BODY:
										/*
										 * Parse error.
										 */
										Err("Non-space character in page trailer.");
										/*
										 * Switch back to the main mode and
										 * reprocess the token.
										 */
										mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
										i--;
										continue;
									case InsertionMode.AFTER_AFTER_FRAMESET:
										/*
										 * Parse error.
										 */
										Err("Non-space character in page trailer.");
										/*
										 * Switch back to the main mode and
										 * reprocess the token.
										 */
										mode = InsertionMode.IN_FRAMESET;
										i--;
										continue;
								}
								break;
						}

					continueCharactersloop:
						continue;
					}
					if (start < end)
					{
						AccumulateCharacters(buf, start, end - start);
					}
					break;
			}
		}

		/// <summary>
		/// Reports a U+0000 that's being turned into a U+FFFD.
		/// </summary>
		public void ZeroOriginatingReplacementCharacter()
		{
			if (mode == InsertionMode.TEXT)
			{
				AccumulateCharacters(TreeBuilderConstants.REPLACEMENT_CHARACTER, 0, 1);
				return;
			}
            if (currentPtr >= 0)
            {
                if (IsSpecialParentInForeign(stack[currentPtr]))
                {
                    return;
                }
                //if (stackNode.ns == "http://www.w3.org/1998/Math/MathML"
                //        && stackNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT)
                //{
                //    return;
                //}
                AccumulateCharacters(TreeBuilderConstants.REPLACEMENT_CHARACTER, 0, 1);
            }
		}

		/// <summary>
		/// The end-of-file token.
		/// </summary>
		public void Eof()
		{
			FlushCharacters();
			/*eofloop:*/
			for (; ; )
			{
				if (IsInForeign)
				{
					Err("End of file in a foreign namespace context.");
					goto continueEofloop; // TODO: endless loop???
				}
				switch (mode)
				{
					case InsertionMode.INITIAL:
						/*
						 * Parse error.
						 */
						// [NOCPP[
						switch (DoctypeExpectation)
						{
							case DoctypeExpectation.Auto:
								Err("End of file seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html:
								Err("End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html401Strict:
								Err("End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
								break;
							case DoctypeExpectation.Html401Transitional:
								Err("End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
								break;
							case DoctypeExpectation.NoDoctypeErrors:
								break;
						}
						// ]NOCPP]
						/*
						 * 
						 * Set the document to quirks mode.
						 */
						DocumentModeInternal(DocumentMode.QuirksMode, null, null,
								false);
						/*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
						mode = InsertionMode.BEFORE_HTML;
						/*
						 * and reprocess the current token.
						 */
						continue;
					case InsertionMode.BEFORE_HTML:
						/*
						 * Create an HTMLElement node with the tag name html, in the
						 * HTML namespace. Append it to the Document object.
						 */
						AppendHtmlElementToDocumentAndPush();
						// XXX application cache manifest
						/* Switch to the main mode */
						mode = InsertionMode.BEFORE_HEAD;
						/*
						 * reprocess the current token.
						 */
						continue;
					case InsertionMode.BEFORE_HEAD:
						AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
						mode = InsertionMode.IN_HEAD;
						continue;
					case InsertionMode.IN_HEAD:
						if (ErrorEvent != null && currentPtr > 1)
						{
							ErrEndWithUnclosedElements("End of file seen and there were open elements.");
						}
						while (currentPtr > 0)
						{
							PopOnEof();
						}
						mode = InsertionMode.AFTER_HEAD;
						continue;
					case InsertionMode.IN_HEAD_NOSCRIPT:
						ErrEndWithUnclosedElements("End of file seen and there were open elements.");
						while (currentPtr > 1)
						{
							PopOnEof();
						}
						mode = InsertionMode.IN_HEAD;
						continue;
					case InsertionMode.AFTER_HEAD:
						AppendToCurrentNodeAndPushBodyElement();
						mode = InsertionMode.IN_BODY;
						continue;
					case InsertionMode.IN_COLUMN_GROUP:
						if (currentPtr == 0)
						{
							Debug.Assert(fragment);
							goto breakEofloop;
						}
						else
						{
							PopOnEof();
							mode = InsertionMode.IN_TABLE;
							continue;
						}
					case InsertionMode.FRAMESET_OK:
					case InsertionMode.IN_CAPTION:
					case InsertionMode.IN_CELL:
					case InsertionMode.IN_BODY:
						// [NOCPP[
						/*openelementloop:*/
						for (int i = currentPtr; i >= 0; i--)
						{
							DispatchGroup group = stack[i].Group;
							switch (group)
							{
								case DispatchGroup.DD_OR_DT:
								case DispatchGroup.LI:
								case DispatchGroup.P:
								case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
								case DispatchGroup.TD_OR_TH:
								case DispatchGroup.BODY:
								case DispatchGroup.HTML:
									break;
								default:
									ErrEndWithUnclosedElements("End of file seen and there were open elements.");
									goto breakOpenelementloop;
							}
						}

					breakOpenelementloop:
						// ]NOCPP]
						goto breakEofloop;
					case InsertionMode.TEXT:
						if (ErrorEvent != null)
						{
							Err("End of file seen when expecting text or an end tag.");
							ErrListUnclosedStartTags(0);
						}
						// XXX mark script as already executed
						if (originalMode == InsertionMode.AFTER_HEAD)
						{
							PopOnEof();
						}
						PopOnEof();
						mode = originalMode;
						continue;
					case InsertionMode.IN_TABLE_BODY:
					case InsertionMode.IN_ROW:
					case InsertionMode.IN_TABLE:
					case InsertionMode.IN_SELECT:
					case InsertionMode.IN_SELECT_IN_TABLE:
					case InsertionMode.IN_FRAMESET:
						if (ErrorEvent != null && currentPtr > 0)
						{
							ErrEndWithUnclosedElements("End of file seen and there were open elements.");
						}
						goto breakEofloop;
					case InsertionMode.AFTER_BODY:
					case InsertionMode.AFTER_FRAMESET:
					case InsertionMode.AFTER_AFTER_BODY:
					case InsertionMode.AFTER_AFTER_FRAMESET:
					default:
						// [NOCPP[
						//if (currentPtr == 0) { // This silliness is here to poison
						//    // buggy compiler optimizations in
						//    // GWT
						//    System.currentTimeMillis();
						//}
						// ]NOCPP]
						goto breakEofloop;
				}

			continueEofloop:
				continue;
			}

		breakEofloop:

			while (currentPtr > 0)
			{
				PopOnEof();
			}
			if (!fragment)
			{
				PopOnEof();
			}
			/* Stop parsing. */
		}

		/// <summary>
		/// The perform final cleanup.
		/// </summary>
		public void EndTokenization()
		{
			formPointer = null;
			headPointer = null;
			deepTreeSurrogateParent = null;
			if (stack != null)
			{
				while (currentPtr > -1)
				{
					currentPtr--;
				}
				stack = null;
			}
			if (listOfActiveFormattingElements != null)
			{
				while (listPtr > -1)
				{
					//if (listOfActiveFormattingElements[listPtr] != null) {
					//    listOfActiveFormattingElements[listPtr].Release();
					//}
					listPtr--;
				}
				listOfActiveFormattingElements = null;
			}
			// [NOCPP[
			idLocations.Clear();
			// ]NOCPP]
			charBuffer = null;
			End();
		}

		public void StartTag(ElementName elementName, HtmlAttributes attributes, bool selfClosing)
		{
			FlushCharacters();

			// [NOCPP[
			if (ErrorEvent != null)
			{
				// ID uniqueness
				string id = attributes.Id;
				if (id != null)
				{
					Locator oldLoc;
					bool success = idLocations.TryGetValue(id, out oldLoc);
					if (success)
					{
						Err("Duplicate ID \u201C" + id + "\u201D.");
						//errorHandler.warning(new SAXParseException(
						//        "The first occurrence of ID \u201C" + id
						//        + "\u201D was here.", oldLoc));
						Warn("The first occurrence of ID \u201C" + id + "\u201D was here.");
					}
					else
					{
						idLocations[id] = new Locator(tokenizer);
					}
				}
			}
			// ]NOCPP]

			int eltPos;
			needToDropLF = false;
			/*starttagloop:*/
			for (; ; )
			{
				DispatchGroup group = elementName.Group;
				/*[Local]*/
				string name = elementName.name;
				if (IsInForeign)
				{
					StackNode<T> currentNode = stack[currentPtr];
					/*[NsUri]*/
					string currNs = currentNode.ns;
					if (!(currentNode.IsHtmlIntegrationPoint || (currNs == "http://www.w3.org/1998/Math/MathML" &&
						((currentNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT && group != DispatchGroup.MGLYPH_OR_MALIGNMARK) ||
						(currentNode.Group == DispatchGroup.ANNOTATION_XML && group == DispatchGroup.SVG)))))
					{
						switch (group)
						{
							case DispatchGroup.B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
							case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
							case DispatchGroup.BODY:
							case DispatchGroup.BR:
							case DispatchGroup.RUBY_OR_SPAN_OR_SUB_OR_SUP_OR_VAR:
							case DispatchGroup.DD_OR_DT:
							case DispatchGroup.UL_OR_OL_OR_DL:
							case DispatchGroup.EMBED_OR_IMG:
							case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
							case DispatchGroup.HEAD:
							case DispatchGroup.HR:
							case DispatchGroup.LI:
							case DispatchGroup.META:
							case DispatchGroup.NOBR:
							case DispatchGroup.P:
							case DispatchGroup.PRE_OR_LISTING:
							case DispatchGroup.TABLE:
								Err("HTML start tag \u201C"
										+ name
										+ "\u201D in a foreign namespace context.");
								while (!IsSpecialParentInForeign(stack[currentPtr]))
								{
									Pop();
								}
								goto continueStarttagloop;
							case DispatchGroup.FONT:
								if (attributes.Contains(AttributeName.COLOR)
										|| attributes.Contains(AttributeName.FACE)
										|| attributes.Contains(AttributeName.SIZE))
								{
									Err("HTML start tag \u201C"
											+ name
											+ "\u201D in a foreign namespace context.");
									while (!IsSpecialParentInForeign(stack[currentPtr]))
									{
										Pop();
									}
									goto continueStarttagloop;
								}
								else
								{
									// else fall through
									goto default;
								}
							default:
								if ("http://www.w3.org/2000/svg" == currNs)
								{
									attributes.AdjustForSvg();
									if (selfClosing)
									{
										AppendVoidElementToCurrentMayFosterSVG(
												elementName, attributes);
										selfClosing = false;
									}
									else
									{
										AppendToCurrentNodeAndPushElementMayFosterSVG(
												elementName, attributes);
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								}
								else
								{
									attributes.AdjustForMath();
									if (selfClosing)
									{
										AppendVoidElementToCurrentMayFosterMathML(
												elementName, attributes);
										selfClosing = false;
									}
									else
									{
										AppendToCurrentNodeAndPushElementMayFosterMathML(
												elementName, attributes);
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								}
						} // switch
					} // foreignObject / annotation-xml
				}
				switch (mode)
				{
					case InsertionMode.IN_TABLE_BODY:
						switch (group)
						{
							case DispatchGroup.TR:
								ClearStackBackTo(FindLastInTableScopeOrRootTbodyTheadTfoot());
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								mode = InsertionMode.IN_ROW;
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.TD_OR_TH:
								Err("\u201C" + name
										+ "\u201D start tag in table body.");
								ClearStackBackTo(FindLastInTableScopeOrRootTbodyTheadTfoot());
								AppendToCurrentNodeAndPushElement(
										ElementName.TR,
										HtmlAttributes.EMPTY_ATTRIBUTES);
								mode = InsertionMode.IN_ROW;
								continue;
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
								eltPos = FindLastInTableScopeOrRootTbodyTheadTfoot();
								if (eltPos == 0)
								{
									ErrStrayStartTag(name);
									goto breakStarttagloop;
								}
								else
								{
									ClearStackBackTo(eltPos);
									Pop();
									mode = InsertionMode.IN_TABLE;
									continue;
								}
							default:
								// fall through to IN_TABLE (TODO: IN_ROW?)
								break;
						}
						goto case InsertionMode.IN_ROW;
					case InsertionMode.IN_ROW:
						switch (group)
						{
							case DispatchGroup.TD_OR_TH:
								ClearStackBackTo(FindLastOrRoot(DispatchGroup.TR));
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								mode = InsertionMode.IN_CELL;
								InsertMarker();
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
								eltPos = FindLastOrRoot(DispatchGroup.TR);
								if (eltPos == 0)
								{
									Debug.Assert(fragment);
									Err("No table row to close.");
									goto breakStarttagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE_BODY;
								continue;
							default:
								// fall through to IN_TABLE
								break;
						}
						goto case InsertionMode.IN_TABLE;
					case InsertionMode.IN_TABLE:
						/*intableloop:*/
						for (; ; )
						{
							switch (group)
							{
								case DispatchGroup.CAPTION:
									ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
									InsertMarker();
									AppendToCurrentNodeAndPushElement(
											elementName,
											attributes);
									mode = InsertionMode.IN_CAPTION;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.COLGROUP:
									ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
									AppendToCurrentNodeAndPushElement(
											elementName,
											attributes);
									mode = InsertionMode.IN_COLUMN_GROUP;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.COL:
									ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
									AppendToCurrentNodeAndPushElement(
											ElementName.COLGROUP,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									mode = InsertionMode.IN_COLUMN_GROUP;
									goto continueStarttagloop;
								case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
									ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
									AppendToCurrentNodeAndPushElement(
											elementName,
											attributes);
									mode = InsertionMode.IN_TABLE_BODY;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.TR:
								case DispatchGroup.TD_OR_TH:
									ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
									AppendToCurrentNodeAndPushElement(
											ElementName.TBODY,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									mode = InsertionMode.IN_TABLE_BODY;
									goto continueStarttagloop;
								case DispatchGroup.TABLE:
									Err("Start tag for \u201Ctable\u201D seen but the previous \u201Ctable\u201D is still open.");
									eltPos = FindLastInTableScope(name);
									if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
									{
										Debug.Assert(fragment);
										goto breakStarttagloop;
									}
									GenerateImpliedEndTags();
									// XXX is the next if dead code?
									if (ErrorEvent != null && !IsCurrent("table"))
									{
										Err("Unclosed elements on stack.");
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
									ResetTheInsertionMode();
									goto continueStarttagloop;
								case DispatchGroup.SCRIPT:
									// XXX need to manage much more stuff
									// here if
									// supporting
									// document.write()
									AppendToCurrentNodeAndPushElement(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.SCRIPT_DATA, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.STYLE:
									AppendToCurrentNodeAndPushElement(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RAWTEXT, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.INPUT:
									if (!Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
											"hidden",
											attributes.GetValue(AttributeName.TYPE)))
									{
										goto breakIntableloop;
									}
									AppendVoidElementToCurrent(
											name, attributes,
											formPointer);
									selfClosing = false;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.FORM:
									if (formPointer != null)
									{
										Err("Saw a \u201Cform\u201D start tag, but there was already an active \u201Cform\u201D element. Nested forms are not allowed. Ignoring the tag.");
										goto breakStarttagloop;
									}
									else
									{
										Err("Start tag \u201Cform\u201D seen in \u201Ctable\u201D.");
										AppendVoidFormToCurrent(attributes);
										attributes = null; // CPP
										goto breakStarttagloop;
									}
								default:
									Err("Start tag \u201C" + name
											+ "\u201D seen in \u201Ctable\u201D.");
									// fall through to IN_BODY (TODO: IN_CAPTION?)
									goto breakIntableloop;
							}
						}

					breakIntableloop:
						goto case InsertionMode.IN_CAPTION;

					case InsertionMode.IN_CAPTION:
						switch (group)
						{
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
							case DispatchGroup.TD_OR_TH:
								ErrStrayStartTag(name);
								eltPos = FindLastInTableScope("caption");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									goto breakStarttagloop;
								}
								GenerateImpliedEndTags();
								if (ErrorEvent != null && currentPtr != eltPos)
								{
									Err("Unclosed elements on stack.");
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
								mode = InsertionMode.IN_TABLE;
								continue;
							default:
								// fall through to IN_BODY (TODO: IN_CELL?)
								break;
						}
						goto case InsertionMode.IN_CELL;
					case InsertionMode.IN_CELL:
						switch (group)
						{
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
							case DispatchGroup.TD_OR_TH:
								eltPos = FindLastInTableScopeTdTh();
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Err("No cell to close.");
									goto breakStarttagloop;
								}
								else
								{
									CloseTheCell(eltPos);
									continue;
								}
							default:
								// fall through to IN_BODY (TODO: FRAMESET_OK?)
								break;
						}
						goto case InsertionMode.FRAMESET_OK;
					case InsertionMode.FRAMESET_OK:
						switch (group)
						{
							case DispatchGroup.FRAMESET:
								if (mode == InsertionMode.FRAMESET_OK)
								{
									if (currentPtr == 0 || stack[1].Group != DispatchGroup.BODY)
									{
										Debug.Assert(fragment);
										ErrStrayStartTag(name);
										goto breakStarttagloop;
									}
									else
									{
										Err("\u201Cframeset\u201D start tag seen.");
										DetachFromParent(stack[1].node);
										while (currentPtr > 0)
										{
											Pop();
										}
										AppendToCurrentNodeAndPushElement(
												elementName,
												attributes);
										mode = InsertionMode.IN_FRAMESET;
										attributes = null; // CPP
										goto breakStarttagloop;
									}
								}
								else
								{
									ErrStrayStartTag(name);
									goto breakStarttagloop;
								}
							// NOT falling through!
							case DispatchGroup.PRE_OR_LISTING:
							case DispatchGroup.LI:
							case DispatchGroup.DD_OR_DT:
							case DispatchGroup.BUTTON:
							case DispatchGroup.MARQUEE_OR_APPLET:
							case DispatchGroup.OBJECT:
							case DispatchGroup.TABLE:
							case DispatchGroup.AREA_OR_WBR:
							case DispatchGroup.BR:
							case DispatchGroup.EMBED_OR_IMG:
							case DispatchGroup.INPUT:
							case DispatchGroup.KEYGEN:
							case DispatchGroup.HR:
							case DispatchGroup.TEXTAREA:
							case DispatchGroup.XMP:
							case DispatchGroup.IFRAME:
							case DispatchGroup.SELECT:
								if (mode == InsertionMode.FRAMESET_OK
										&& !(group == DispatchGroup.INPUT && Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
												"hidden",
												attributes.GetValue(AttributeName.TYPE))))
								{
									framesetOk = false;
									mode = InsertionMode.IN_BODY;
								}
								// fall through to IN_BODY
								break;
							default:
								// fall through to IN_BODY
								break;
						}
						goto case InsertionMode.IN_BODY;
					case InsertionMode.IN_BODY:
						/*inbodyloop:*/
						for (; ; )
						{
							switch (group)
							{
								case DispatchGroup.HTML:
									ErrStrayStartTag(name);
									if (!fragment)
									{
										AddAttributesToHtml(attributes);
										attributes = null; // CPP
									}
									goto breakStarttagloop;
								case DispatchGroup.BASE:
								case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
								case DispatchGroup.META:
								case DispatchGroup.STYLE:
								case DispatchGroup.SCRIPT:
								case DispatchGroup.TITLE:
								case DispatchGroup.COMMAND:
									// Fall through to IN_HEAD
									goto breakInbodyloop;
								case DispatchGroup.BODY:
									if (currentPtr == 0
											|| stack[1].Group != DispatchGroup.BODY)
									{
										Debug.Assert(fragment);
										ErrStrayStartTag(name);
										goto breakStarttagloop;
									}
									Err("\u201Cbody\u201D start tag found but the \u201Cbody\u201D element is already open.");
									framesetOk = false;
									if (mode == InsertionMode.FRAMESET_OK)
									{
										mode = InsertionMode.IN_BODY;
									}
									if (AddAttributesToBody(attributes))
									{
										attributes = null; // CPP
									}
									goto breakStarttagloop;
								case DispatchGroup.P:
								case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
								case DispatchGroup.UL_OR_OL_OR_DL:
								case DispatchGroup.ADDRESS_OR_ARTICLE_OR_ASIDE_OR_DETAILS_OR_DIR_OR_FIGCAPTION_OR_FIGURE_OR_FOOTER_OR_HEADER_OR_HGROUP_OR_NAV_OR_SECTION_OR_SUMMARY:
									ImplicitlyCloseP();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
									ImplicitlyCloseP();
									if (stack[currentPtr].Group == DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6)
									{
										Err("Heading cannot be a child of another heading.");
										Pop();
									}
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.FIELDSET:
									ImplicitlyCloseP();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes, formPointer);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.PRE_OR_LISTING:
									ImplicitlyCloseP();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									needToDropLF = true;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.FORM:
									if (formPointer != null)
									{
										Err("Saw a \u201Cform\u201D start tag, but there was already an active \u201Cform\u201D element. Nested forms are not allowed. Ignoring the tag.");
										goto breakStarttagloop;
									}
									else
									{
										ImplicitlyCloseP();
										AppendToCurrentNodeAndPushFormElementMayFoster(attributes);
										attributes = null; // CPP
										goto breakStarttagloop;
									}
								case DispatchGroup.LI:
								case DispatchGroup.DD_OR_DT:
									eltPos = currentPtr;
									for (; ; )
									{
										StackNode<T> node = stack[eltPos]; // weak
										// ref
										if (node.Group == group)
										{ // LI or
											// DD_OR_DT
											GenerateImpliedEndTagsExceptFor(node.name);
											if (ErrorEvent != null
													&& eltPos != currentPtr)
											{
												ErrUnclosedElementsImplied(eltPos, name);
											}
											while (currentPtr >= eltPos)
											{
												Pop();
											}
											break;
										}
										else if (node.IsScoping
											  || (node.IsSpecial
													  && node.name != "p"
													  && node.name != "address" && node.name != "div"))
										{
											break;
										}
										eltPos--;
									}
									ImplicitlyCloseP();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.PLAINTEXT:
									ImplicitlyCloseP();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.PLAINTEXT, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.A:
									int activeAPos = FindInListOfActiveFormattingElementsContainsBetweenEndAndLastMarker("a");
									if (activeAPos != -1)
									{
										Err("An \u201Ca\u201D start tag seen with already an active \u201Ca\u201D element.");
										StackNode<T> activeA = listOfActiveFormattingElements[activeAPos];
										activeA.Retain();
										AdoptionAgencyEndTag("a");
										RemoveFromStack(activeA);
										activeAPos = FindInListOfActiveFormattingElements(activeA);
										if (activeAPos != -1)
										{
											RemoveFromListOfActiveFormattingElements(activeAPos);
										}
										activeA.Release();
									}
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushFormattingElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
								case DispatchGroup.FONT:
									ReconstructTheActiveFormattingElements();
									MaybeForgetEarlierDuplicateFormattingElement(elementName.name, attributes);
									AppendToCurrentNodeAndPushFormattingElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.NOBR:
									ReconstructTheActiveFormattingElements();
									if (TreeBuilderConstants.NOT_FOUND_ON_STACK != FindLastInScope("nobr"))
									{
										Err("\u201Cnobr\u201D start tag seen when there was an open \u201Cnobr\u201D element in scope.");
										AdoptionAgencyEndTag("nobr");
										ReconstructTheActiveFormattingElements();
									}
									AppendToCurrentNodeAndPushFormattingElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.BUTTON:
									eltPos = FindLastInScope(name);
									if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
									{
										Err("\u201Cbutton\u201D start tag seen when there was an open \u201Cbutton\u201D element in scope.");

										GenerateImpliedEndTags();
										if (ErrorEvent != null && !IsCurrent(name))
										{
											ErrUnclosedElementsImplied(eltPos, name);
										}
										while (currentPtr >= eltPos)
										{
											Pop();
										}
										goto continueStarttagloop;
									}
									else
									{
										ReconstructTheActiveFormattingElements();
										AppendToCurrentNodeAndPushElementMayFoster(
												elementName,
												attributes, formPointer);
										attributes = null; // CPP
										goto breakStarttagloop;
									}
								case DispatchGroup.OBJECT:
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes, formPointer);
									InsertMarker();
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.MARQUEE_OR_APPLET:
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									InsertMarker();
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.TABLE:
									// The only quirk. Blame Hixie and
									// Acid2.
									if (!quirks)
									{
										ImplicitlyCloseP();
									}
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									mode = InsertionMode.IN_TABLE;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.BR:
								case DispatchGroup.EMBED_OR_IMG:
								case DispatchGroup.AREA_OR_WBR:
									ReconstructTheActiveFormattingElements();
									// FALL THROUGH to PARAM_OR_SOURCE_OR_TRACK
									goto case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK;
								case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK:
									AppendVoidElementToCurrentMayFoster(
											elementName,
											attributes);
									selfClosing = false;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.HR:
									ImplicitlyCloseP();
									AppendVoidElementToCurrentMayFoster(
											elementName,
											attributes);
									selfClosing = false;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.IMAGE:
									Err("Saw a start tag \u201Cimage\u201D.");
									elementName = ElementName.IMG;
									goto continueStarttagloop;
								case DispatchGroup.KEYGEN:
								case DispatchGroup.INPUT:
									ReconstructTheActiveFormattingElements();
									AppendVoidElementToCurrentMayFoster(
											name, attributes,
											formPointer);
									selfClosing = false;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.ISINDEX:
									Err("\u201Cisindex\u201D seen.");
									if (formPointer != null)
									{
										goto breakStarttagloop;
									}
									ImplicitlyCloseP();
									HtmlAttributes formAttrs = new HtmlAttributes(0);
									int actionIndex = attributes.GetIndex(AttributeName.ACTION);
									if (actionIndex > -1)
									{
										formAttrs.AddAttribute(
												AttributeName.ACTION,
												attributes.GetValue(actionIndex)
											// [NOCPP[
												, XmlViolationPolicy.Allow
											// ]NOCPP]
										);
									}
									AppendToCurrentNodeAndPushFormElementMayFoster(formAttrs);
									AppendVoidElementToCurrentMayFoster(
											ElementName.HR,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									AppendToCurrentNodeAndPushElementMayFoster(
											ElementName.LABEL,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									int promptIndex = attributes.GetIndex(AttributeName.PROMPT);
									if (promptIndex > -1)
									{
										char[] prompt = attributes.GetValue(promptIndex).ToCharArray();
										AppendCharacters(stack[currentPtr].node,
												prompt, 0, prompt.Length);
									}
									else
									{
										AppendIsindexPrompt(stack[currentPtr].node);
									}
									HtmlAttributes inputAttributes = new HtmlAttributes(
											0);
									inputAttributes.AddAttribute(
											AttributeName.NAME,
											"isindex"
										// [NOCPP[
											, XmlViolationPolicy.Allow
										// ]NOCPP]
									);
									for (int i = 0; i < attributes.Length; i++)
									{
										AttributeName attributeQName = attributes.GetAttributeName(i);
										if (AttributeName.NAME == attributeQName
												|| AttributeName.PROMPT == attributeQName)
										{
											//attributes.ReleaseValue(i);
										}
										else if (AttributeName.ACTION != attributeQName)
										{
											inputAttributes.AddAttribute(
													attributeQName,
													attributes.GetValue(i)
												// [NOCPP[
													, XmlViolationPolicy.Allow
												// ]NOCPP]

											);
										}
									}
									attributes.ClearWithoutReleasingContents();
									AppendVoidElementToCurrentMayFoster(
											"input",
											inputAttributes, formPointer);
									Pop(); // label
									AppendVoidElementToCurrentMayFoster(
											ElementName.HR,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									Pop(); // form
									selfClosing = false;
									// Portability.delete(formAttrs);
									// Portability.delete(inputAttributes);
									// Don't delete attributes, they are deleted
									// later
									goto breakStarttagloop;
								case DispatchGroup.TEXTAREA:
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes, formPointer);
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RCDATA, elementName);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									needToDropLF = true;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.XMP:
									ImplicitlyCloseP();
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RAWTEXT, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.NOSCRIPT:
									if (!IsScriptingEnabled)
									{
										ReconstructTheActiveFormattingElements();
										AppendToCurrentNodeAndPushElementMayFoster(
												elementName,
												attributes);
										attributes = null; // CPP
										goto breakStarttagloop;
									}
									else
									{
										// fall through
										goto case DispatchGroup.NOFRAMES;
									}
								case DispatchGroup.NOFRAMES:
								case DispatchGroup.IFRAME:
								case DispatchGroup.NOEMBED:
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RAWTEXT, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.SELECT:
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes, formPointer);
									switch (mode)
									{
										case InsertionMode.IN_TABLE:
										case InsertionMode.IN_CAPTION:
										case InsertionMode.IN_COLUMN_GROUP:
										case InsertionMode.IN_TABLE_BODY:
										case InsertionMode.IN_ROW:
										case InsertionMode.IN_CELL:
											mode = InsertionMode.IN_SELECT_IN_TABLE;
											break;
										default:
											mode = InsertionMode.IN_SELECT;
											break;
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.OPTGROUP:
								case DispatchGroup.OPTION:
									if (IsCurrent("option"))
									{
										Pop();
									}
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.RT_OR_RP:
		
									eltPos = FindLastInScope("ruby");
									if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
									{
										GenerateImpliedEndTags();
									}
									if (eltPos != currentPtr)
									{
										if (ErrorEvent != null)
										{
											if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
											{

												Err("Start tag \u201C"
														+ name
														+ "\u201D seen without a \u201Cruby\u201D element being open.");
											}
											else
											{
												Err("Unclosed children in \u201Cruby\u201D.");
											}
										}
									}
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.MATH:
									ReconstructTheActiveFormattingElements();
									attributes.AdjustForMath();
									if (selfClosing)
									{
										AppendVoidElementToCurrentMayFosterMathML(
												elementName, attributes);
										selfClosing = false;
									}
									else
									{
										AppendToCurrentNodeAndPushElementMayFosterMathML(
												elementName, attributes);
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.SVG:
									ReconstructTheActiveFormattingElements();
									attributes.AdjustForSvg();
									if (selfClosing)
									{
										AppendVoidElementToCurrentMayFosterSVG(
												elementName,
												attributes);
										selfClosing = false;
									}
									else
									{
										AppendToCurrentNodeAndPushElementMayFosterSVG(
												elementName, attributes);
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.CAPTION:
								case DispatchGroup.COL:
								case DispatchGroup.COLGROUP:
								case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
								case DispatchGroup.TR:
								case DispatchGroup.TD_OR_TH:
								case DispatchGroup.FRAME:
								case DispatchGroup.FRAMESET:
								case DispatchGroup.HEAD:
									ErrStrayStartTag(name);
									goto breakStarttagloop;
								case DispatchGroup.OUTPUT_OR_LABEL:
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes, formPointer);
									attributes = null; // CPP
									goto breakStarttagloop;
								default:
									ReconstructTheActiveFormattingElements();
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									attributes = null; // CPP
									goto breakStarttagloop;
							}
						}

					breakInbodyloop:
						goto case InsertionMode.IN_HEAD;

					case InsertionMode.IN_HEAD:
						/*inheadloop:*/
						for (; ; )
						{
							switch (group)
							{
								case DispatchGroup.HTML:
									ErrStrayStartTag(name);
									if (!fragment)
									{
										AddAttributesToHtml(attributes);
										attributes = null; // CPP
									}
									goto breakStarttagloop;
								case DispatchGroup.BASE:
								case DispatchGroup.COMMAND:
									AppendVoidElementToCurrentMayFoster(
											elementName,
											attributes);
									selfClosing = false;
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.META:
								case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
									// Fall through to IN_HEAD_NOSCRIPT
									goto breakInheadloop;
								case DispatchGroup.TITLE:
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RCDATA, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.NOSCRIPT:
									if (IsScriptingEnabled)
									{
										AppendToCurrentNodeAndPushElement(
												elementName,
												attributes);
										originalMode = mode;
										mode = InsertionMode.TEXT;
										tokenizer.SetStateAndEndTagExpectation(
                                                TokenizerState.RAWTEXT, elementName);
									}
									else
									{
										AppendToCurrentNodeAndPushElementMayFoster(
												elementName,
												attributes);
										mode = InsertionMode.IN_HEAD_NOSCRIPT;
									}
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.SCRIPT:
									// XXX need to manage much more stuff
									// here if
									// supporting
									// document.write()
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.SCRIPT_DATA, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.STYLE:
								case DispatchGroup.NOFRAMES:
									AppendToCurrentNodeAndPushElementMayFoster(
											elementName,
											attributes);
									originalMode = mode;
									mode = InsertionMode.TEXT;
									tokenizer.SetStateAndEndTagExpectation(
                                            TokenizerState.RAWTEXT, elementName);
									attributes = null; // CPP
									goto breakStarttagloop;
								case DispatchGroup.HEAD:
									/* Parse error. */
									Err("Start tag for \u201Chead\u201D seen when \u201Chead\u201D was already open.");
									/* Ignore the token. */
									goto breakStarttagloop;
								default:
									Pop();
									mode = InsertionMode.AFTER_HEAD;
									goto continueStarttagloop;
							}
						}

					breakInheadloop:
						goto case InsertionMode.IN_HEAD_NOSCRIPT;

					case InsertionMode.IN_HEAD_NOSCRIPT:
						switch (group)
						{
							case DispatchGroup.HTML:
								// XXX did Hixie really mean to omit "base"
								// here?
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.META:
								CheckMetaCharset(attributes);
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.STYLE:
							case DispatchGroup.NOFRAMES:
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.RAWTEXT, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.HEAD:
								Err("Start tag for \u201Chead\u201D seen when \u201Chead\u201D was already open.");
								goto breakStarttagloop;
							case DispatchGroup.NOSCRIPT:
								Err("Start tag for \u201Cnoscript\u201D seen when \u201Cnoscript\u201D was already open.");
								goto breakStarttagloop;
							default:
								Err("Bad start tag in \u201C" + name
										+ "\u201D in \u201Chead\u201D.");
								Pop();
								mode = InsertionMode.IN_HEAD;
								continue;
						}
					case InsertionMode.IN_COLUMN_GROUP:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.COL:
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								if (currentPtr == 0)
								{
									Debug.Assert(fragment);
									Err("Garbage in \u201Ccolgroup\u201D fragment.");
									goto breakStarttagloop;
								}
								Pop();
								mode = InsertionMode.IN_TABLE;
								continue;
						}
					case InsertionMode.IN_SELECT_IN_TABLE:
						switch (group)
						{
							case DispatchGroup.CAPTION:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
							case DispatchGroup.TD_OR_TH:
							case DispatchGroup.TABLE:
								Err("\u201C"
										+ name
										+ "\u201D start tag with \u201Cselect\u201D open.");
								eltPos = FindLastInTableScope("select");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Debug.Assert(fragment);
									goto breakStarttagloop; // http://www.w3.org/Bugs/Public/show_bug.cgi?id=8375
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ResetTheInsertionMode();
								continue;
							default:
								// fall through to IN_SELECT
								break;
						}
						goto case InsertionMode.IN_SELECT;
					case InsertionMode.IN_SELECT:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.OPTION:
								if (IsCurrent("option"))
								{
									Pop();
								}
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.OPTGROUP:
								if (IsCurrent("option"))
								{
									Pop();
								}
								if (IsCurrent("optgroup"))
								{
									Pop();
								}
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.SELECT:
								Err("\u201Cselect\u201D start tag where end tag expected.");
								eltPos = FindLastInTableScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Debug.Assert(fragment);
									Err("No \u201Cselect\u201D in table scope.");
									goto breakStarttagloop;
								}
								else
								{
									while (currentPtr >= eltPos)
									{
										Pop();
									}
									ResetTheInsertionMode();
									goto breakStarttagloop;
								}
							case DispatchGroup.INPUT:
							case DispatchGroup.TEXTAREA:
							case DispatchGroup.KEYGEN:
								Err("\u201C"
										+ name
										+ "\u201D start tag seen in \u201Cselect\u2201D.");
								eltPos = FindLastInTableScope("select");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Debug.Assert(fragment);
									goto breakStarttagloop;
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ResetTheInsertionMode();
								continue;
							case DispatchGroup.SCRIPT:
								// XXX need to manage much more stuff
								// here if
								// supporting
								// document.write()
								AppendToCurrentNodeAndPushElementMayFoster(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.SCRIPT_DATA, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								ErrStrayStartTag(name);
								goto breakStarttagloop;
						}
					case InsertionMode.AFTER_BODY:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							default:
								ErrStrayStartTag(name);
								mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
								continue;
						}
					case InsertionMode.IN_FRAMESET:
						switch (group)
						{
							case DispatchGroup.FRAMESET:
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.FRAME:
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								// fall through to AFTER_FRAMESET
								break;
						}
						goto case InsertionMode.AFTER_FRAMESET;
					case InsertionMode.AFTER_FRAMESET:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.NOFRAMES:
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.RAWTEXT, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								ErrStrayStartTag(name);
								goto breakStarttagloop;
						}
					case InsertionMode.INITIAL:
						/*
						 * Parse error.
						 */
						// [NOCPP[
						switch (DoctypeExpectation)
						{
							case DoctypeExpectation.Auto:
								Err("Start tag seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html:
								Err("Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html401Strict:
								Err("Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
								break;
							case DoctypeExpectation.Html401Transitional:
								Err("Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
								break;
							case DoctypeExpectation.NoDoctypeErrors:
								break;
						}
						// ]NOCPP]
						/*
						 * 
						 * Set the document to quirks mode.
						 */
						DocumentModeInternal(DocumentMode.QuirksMode, null, null,
								false);
						/*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
						mode = InsertionMode.BEFORE_HTML;
						/*
						 * and reprocess the current token.
						 */
						continue;
					case InsertionMode.BEFORE_HTML:
						switch (group)
						{
							case DispatchGroup.HTML:
								// optimize error check and streaming SAX by
								// hoisting
								// "html" handling here.
								if (attributes == HtmlAttributes.EMPTY_ATTRIBUTES)
								{
									// This has the right magic side effect
									// that
									// it
									// makes attributes in SAX Tree mutable.
									AppendHtmlElementToDocumentAndPush();
								}
								else
								{
									AppendHtmlElementToDocumentAndPush(attributes);
								}
								// XXX application cache should fire here
								mode = InsertionMode.BEFORE_HEAD;
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								/*
								 * Create an HTMLElement node with the tag name
								 * html, in the HTML namespace. Append it to the
								 * Document object.
								 */
								AppendHtmlElementToDocumentAndPush();
								/* Switch to the main mode */
								mode = InsertionMode.BEFORE_HEAD;
								/*
								 * reprocess the current token.
								 */
								continue;
						}
					case InsertionMode.BEFORE_HEAD:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.HEAD:
								/*
								 * A start tag whose tag name is "head"
								 * 
								 * Create an element for the token.
								 * 
								 * Set the head element pointer to this new element
								 * node.
								 * 
								 * Append the new element to the current node and
								 * push it onto the stack of open elements.
								 */
								AppendToCurrentNodeAndPushHeadElement(attributes);
								/*
								 * Change the insertion mode to "in head".
								 */
								mode = InsertionMode.IN_HEAD;
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								/*
								 * Any other start tag token
								 * 
								 * Act as if a start tag token with the tag name
								 * "head" and no attributes had been seen,
								 */
								AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
								mode = InsertionMode.IN_HEAD;
								/*
								 * then reprocess the current token.
								 * 
								 * This will result in an empty head element being
								 * generated, with the current token being
								 * reprocessed in the "after head" insertion mode.
								 */
								continue;
						}
					case InsertionMode.AFTER_HEAD:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.BODY:
								if (attributes.Length == 0)
								{
									// This has the right magic side effect
									// that
									// it
									// makes attributes in SAX Tree mutable.
									AppendToCurrentNodeAndPushBodyElement();
								}
								else
								{
									AppendToCurrentNodeAndPushBodyElement(attributes);
								}
								framesetOk = false;
								mode = InsertionMode.IN_BODY;
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.FRAMESET:
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								mode = InsertionMode.IN_FRAMESET;
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.BASE:
								Err("\u201Cbase\u201D element outside \u201Chead\u201D.");
								PushHeadPointerOntoStack();
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								Pop(); // head
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
								Err("\u201Clink\u201D element outside \u201Chead\u201D.");
								PushHeadPointerOntoStack();
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								Pop(); // head
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.META:
								Err("\u201Cmeta\u201D element outside \u201Chead\u201D.");
								CheckMetaCharset(attributes);
								PushHeadPointerOntoStack();
								AppendVoidElementToCurrentMayFoster(
										elementName,
										attributes);
								selfClosing = false;
								Pop(); // head
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.SCRIPT:
								Err("\u201Cscript\u201D element between \u201Chead\u201D and \u201Cbody\u201D.");
								PushHeadPointerOntoStack();
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.SCRIPT_DATA, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.STYLE:
							case DispatchGroup.NOFRAMES:
								Err("\u201C"
										+ name
										+ "\u201D element between \u201Chead\u201D and \u201Cbody\u201D.");
								PushHeadPointerOntoStack();
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.RAWTEXT, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.TITLE:
								Err("\u201Ctitle\u201D element outside \u201Chead\u201D.");
								PushHeadPointerOntoStack();
								AppendToCurrentNodeAndPushElement(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.RCDATA, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							case DispatchGroup.HEAD:
								ErrStrayStartTag(name);
								goto breakStarttagloop;
							default:
								AppendToCurrentNodeAndPushBodyElement();
								mode = InsertionMode.FRAMESET_OK;
								continue;
						}
					case InsertionMode.AFTER_AFTER_BODY:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							default:
								ErrStrayStartTag(name);
								Fatal();
								mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
								continue;
						}
					case InsertionMode.AFTER_AFTER_FRAMESET:
						switch (group)
						{
							case DispatchGroup.HTML:
								ErrStrayStartTag(name);
								if (!fragment)
								{
									AddAttributesToHtml(attributes);
									attributes = null; // CPP
								}
								goto breakStarttagloop;
							case DispatchGroup.NOFRAMES:
								AppendToCurrentNodeAndPushElementMayFoster(
										elementName,
										attributes);
								originalMode = mode;
								mode = InsertionMode.TEXT;
								tokenizer.SetStateAndEndTagExpectation(
                                        TokenizerState.SCRIPT_DATA, elementName);
								attributes = null; // CPP
								goto breakStarttagloop;
							default:
								ErrStrayStartTag(name);
								goto breakStarttagloop;
						}
					case InsertionMode.TEXT:
						Debug.Assert(false);
						goto breakStarttagloop; // Avoid infinite loop if the assertion fails (TODO: check)
				}

			continueStarttagloop:
				continue;
			}

		breakStarttagloop:
            if (selfClosing) {
                if (AllowSelfClosingTags) {
                    EndTag(elementName);
                } else if (ErrorEvent != null) {
                    Err("Self-closing syntax (\u201C/>\u201D) used on a non-void HTML element. Ignoring the slash and treating as a start tag.");
                }
			}
		}

		private bool IsSpecialParentInForeign(StackNode<T> stackNode)
		{
			/*[NsUri]*/
			string ns = stackNode.ns;
			return ("http://www.w3.org/1999/xhtml" == ns)
					|| (stackNode.IsHtmlIntegrationPoint)
					|| (("http://www.w3.org/1998/Math/MathML" == ns) && (stackNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT));
		}

		public static string ExtractCharsetFromContent(string attributeValue)
		{
			// This is a bit ugly. Converting the string to char array in order to
			// make the portability layer smaller.
			CharsetState charsetState = CharsetState.CHARSET_INITIAL;
			int start = -1;
			int end = -1;
			char[] buffer = attributeValue.ToCharArray();

			/*charsetloop:*/
			for (int i = 0; i < buffer.Length; i++)
			{
				char c = buffer[i];
				switch (charsetState)
				{
					case CharsetState.CHARSET_INITIAL:
						switch (c)
						{
							case 'c':
							case 'C':
								charsetState = CharsetState.CHARSET_C;
								continue;
							default:
								continue;
						}
					case CharsetState.CHARSET_C:
						switch (c)
						{
							case 'h':
							case 'H':
								charsetState = CharsetState.CHARSET_H;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_H:
						switch (c)
						{
							case 'a':
							case 'A':
								charsetState = CharsetState.CHARSET_A;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_A:
						switch (c)
						{
							case 'r':
							case 'R':
								charsetState = CharsetState.CHARSET_R;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_R:
						switch (c)
						{
							case 's':
							case 'S':
								charsetState = CharsetState.CHARSET_S;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_S:
						switch (c)
						{
							case 'e':
							case 'E':
								charsetState = CharsetState.CHARSET_E;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_E:
						switch (c)
						{
							case 't':
							case 'T':
								charsetState = CharsetState.CHARSET_T;
								continue;
							default:
								charsetState = CharsetState.CHARSET_INITIAL;
								continue;
						}
					case CharsetState.CHARSET_T:
						switch (c)
						{
							case '\t':
							case '\n':
							case '\u000C':
							case '\r':
							case ' ':
								continue;
							case '=':
								charsetState = CharsetState.CHARSET_EQUALS;
								continue;
							default:
								return null;
						}
					case CharsetState.CHARSET_EQUALS:
						switch (c)
						{
							case '\t':
							case '\n':
							case '\u000C':
							case '\r':
							case ' ':
								continue;
							case '\'':
								start = i + 1;
								charsetState = CharsetState.CHARSET_SINGLE_QUOTED;
								continue;
							case '\"':
								start = i + 1;
								charsetState = CharsetState.CHARSET_DOUBLE_QUOTED;
								continue;
							default:
								start = i;
								charsetState = CharsetState.CHARSET_UNQUOTED;
								continue;
						}
					case CharsetState.CHARSET_SINGLE_QUOTED:
						switch (c)
						{
							case '\'':
								end = i;
								goto breakCharsetloop;
							default:
								continue;
						}
					case CharsetState.CHARSET_DOUBLE_QUOTED:
						switch (c)
						{
							case '\"':
								end = i;
								goto breakCharsetloop;
							default:
								continue;
						}
					case CharsetState.CHARSET_UNQUOTED:
						switch (c)
						{
							case '\t':
							case '\n':
							case '\u000C':
							case '\r':
							case ' ':
							case ';':
								end = i;
								goto breakCharsetloop;
							default:
								continue;
						}
				}
			}
		breakCharsetloop:

			string charset = null;
			if (start != -1)
			{
				if (end == -1)
				{
					end = buffer.Length;
				}
				charset = new String(buffer, start, end - start);
			}
			return charset;
		}

		private void CheckMetaCharset(HtmlAttributes attributes)
		{
			string charset = attributes.GetValue(AttributeName.CHARSET);
			if (charset != null)
			{
				if (tokenizer.InternalEncodingDeclaration(charset))
				{
					RequestSuspension();
					return;
				}
				return;
			}
			if (!Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
					"content-type",
					attributes.GetValue(AttributeName.HTTP_EQUIV)))
			{
				return;
			}
			string content = attributes.GetValue(AttributeName.CONTENT);
			if (content != null)
			{
				string extract = ExtractCharsetFromContent(content);
				// remember not to return early without releasing the string
				if (extract != null)
				{
					if (tokenizer.InternalEncodingDeclaration(extract))
					{
						RequestSuspension();
					}
				}
			}
		}

		public void EndTag(ElementName elementName)
		{
			FlushCharacters();
			needToDropLF = false;
			int eltPos;
			DispatchGroup group = elementName.Group;
			/*[Local]*/
			string name = elementName.name;
			/*endtagloop:*/
			for (; ; )
			{
				if (IsInForeign)
				{
					if (ErrorEvent != null && stack[currentPtr].name != name)
					{
						Err("End tag \u201C"
								+ name
								+ "\u201D did not match the name of the current open element (\u201C"
								+ stack[currentPtr].popName + "\u201D).");
					}
					eltPos = currentPtr;
					for (; ; )
					{
						if (stack[eltPos].name == name)
						{
							while (currentPtr >= eltPos)
							{
								Pop();
							}
							goto breakEndtagloop;
						}
						if (stack[--eltPos].ns == "http://www.w3.org/1999/xhtml")
						{
							break;
						}
					}
				}
				switch (mode)
				{
					case InsertionMode.IN_ROW:
						switch (group)
						{
							case DispatchGroup.TR:
								eltPos = FindLastOrRoot(DispatchGroup.TR);
								if (eltPos == 0)
								{
									Debug.Assert(fragment);
									Err("No table row to close.");
									goto breakEndtagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE_BODY;
								goto breakEndtagloop;
							case DispatchGroup.TABLE:
								eltPos = FindLastOrRoot(DispatchGroup.TR);
								if (eltPos == 0)
								{
									Debug.Assert(fragment);
									Err("No table row to close.");
									goto breakEndtagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE_BODY;
								continue;
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
								if (FindLastInTableScope(name) == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								eltPos = FindLastOrRoot(DispatchGroup.TR);
								if (eltPos == 0)
								{
									Debug.Assert(fragment);
									Err("No table row to close.");
									goto breakEndtagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE_BODY;
								continue;
							case DispatchGroup.BODY:
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.HTML:
							case DispatchGroup.TD_OR_TH:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								// fall through to IN_TABLE (TODO: IN_TABLE_BODY?)
								break;
						}

						goto case InsertionMode.IN_TABLE_BODY;
					case InsertionMode.IN_TABLE_BODY:
						switch (group)
						{
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
								eltPos = FindLastOrRoot(name);
								if (eltPos == 0)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE;
								goto breakEndtagloop;
							case DispatchGroup.TABLE:
								eltPos = FindLastInTableScopeOrRootTbodyTheadTfoot();
								if (eltPos == 0)
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								ClearStackBackTo(eltPos);
								Pop();
								mode = InsertionMode.IN_TABLE;
								continue;
							case DispatchGroup.BODY:
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.HTML:
							case DispatchGroup.TD_OR_TH:
							case DispatchGroup.TR:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								// fall through to IN_TABLE
								break;
						}
						goto case InsertionMode.IN_TABLE;
					case InsertionMode.IN_TABLE:
						switch (group)
						{
							case DispatchGroup.TABLE:
								eltPos = FindLast("table");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ResetTheInsertionMode();
								goto breakEndtagloop;
							case DispatchGroup.BODY:
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.HTML:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TD_OR_TH:
							case DispatchGroup.TR:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								ErrStrayEndTag(name);
								// fall through to IN_BODY (TODO: IN_CAPTION?)
								break;
						}
						goto case InsertionMode.IN_CAPTION;
					case InsertionMode.IN_CAPTION:
						switch (group)
						{
							case DispatchGroup.CAPTION:
								eltPos = FindLastInTableScope("caption");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									goto breakEndtagloop;
								}
								GenerateImpliedEndTags();
								if (ErrorEvent != null && currentPtr != eltPos)
								{
									ErrUnclosedElements(eltPos, name);
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
								mode = InsertionMode.IN_TABLE;
								goto breakEndtagloop;
							case DispatchGroup.TABLE:
								Err("\u201Ctable\u201D closed but \u201Ccaption\u201D was still open.");
								eltPos = FindLastInTableScope("caption");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									goto breakEndtagloop;
								}
								GenerateImpliedEndTags();
								if (ErrorEvent != null && currentPtr != eltPos)
								{
									ErrUnclosedElements(eltPos, name);
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
								mode = InsertionMode.IN_TABLE;
								continue;
							case DispatchGroup.BODY:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.HTML:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TD_OR_TH:
							case DispatchGroup.TR:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								// fall through to IN_BODY (TODO: IN_CELL?)
								break;
						}
						goto case InsertionMode.IN_CELL;
					case InsertionMode.IN_CELL:
						switch (group)
						{
							case DispatchGroup.TD_OR_TH:
								eltPos = FindLastInTableScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								GenerateImpliedEndTags();
								if (ErrorEvent != null && !IsCurrent(name))
								{
									ErrUnclosedElements(eltPos, name);
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
								mode = InsertionMode.IN_ROW;
								goto breakEndtagloop;
							case DispatchGroup.TABLE:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
								if (FindLastInTableScope(name) == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								CloseTheCell(FindLastInTableScopeTdTh());
								continue;
							case DispatchGroup.BODY:
							case DispatchGroup.CAPTION:
							case DispatchGroup.COL:
							case DispatchGroup.COLGROUP:
							case DispatchGroup.HTML:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								// fall through to IN_BODY
								break;
						}
						goto case InsertionMode.IN_BODY;
					case InsertionMode.FRAMESET_OK:
					case InsertionMode.IN_BODY:
						switch (group)
						{
							case DispatchGroup.BODY:
								if (!IsSecondOnStackBody())
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								Debug.Assert(currentPtr >= 1);
								if (ErrorEvent != null)
								{
									/*uncloseloop1:*/
									for (int i = 2; i <= currentPtr; i++)
									{
										switch (stack[i].Group)
										{
											case DispatchGroup.DD_OR_DT:
											case DispatchGroup.LI:
											case DispatchGroup.OPTGROUP:
											case DispatchGroup.OPTION: // is this possible?
											case DispatchGroup.P:
											case DispatchGroup.RT_OR_RP:
											case DispatchGroup.TD_OR_TH:
											case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
												break;
											default:
												ErrEndWithUnclosedElements("End tag for \u201Cbody\u201D seen but there were unclosed elements.");
												goto breakUncloseloop1;
										}
									}
								}
							breakUncloseloop1:
								mode = InsertionMode.AFTER_BODY;
								goto breakEndtagloop;
							case DispatchGroup.HTML:
								if (!IsSecondOnStackBody())
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								if (ErrorEvent != null)
								{
									/*uncloseloop2:*/
									for (int i = 0; i <= currentPtr; i++)
									{
										switch (stack[i].Group)
										{
											case DispatchGroup.DD_OR_DT:
											case DispatchGroup.LI:
											case DispatchGroup.P:
											case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
											case DispatchGroup.TD_OR_TH:
											case DispatchGroup.BODY:
											case DispatchGroup.HTML:
												break;
											default:
												ErrEndWithUnclosedElements("End tag for \u201Chtml\u201D seen but there were unclosed elements.");
												goto breakUncloseloop2;
										}
									}
								}

							breakUncloseloop2:
								mode = InsertionMode.AFTER_BODY;
								continue;
							case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
							case DispatchGroup.UL_OR_OL_OR_DL:
							case DispatchGroup.PRE_OR_LISTING:
							case DispatchGroup.FIELDSET:
							case DispatchGroup.BUTTON:
							case DispatchGroup.ADDRESS_OR_ARTICLE_OR_ASIDE_OR_DETAILS_OR_DIR_OR_FIGCAPTION_OR_FIGURE_OR_FOOTER_OR_HEADER_OR_HGROUP_OR_NAV_OR_SECTION_OR_SUMMARY:
								eltPos = FindLastInScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
								}
								else
								{
									GenerateImpliedEndTags();
									if (ErrorEvent != null && !IsCurrent(name))
									{
										ErrUnclosedElements(eltPos, name);
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
								}
								goto breakEndtagloop;
							case DispatchGroup.FORM:
								if (formPointer == null)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								formPointer = null;
								eltPos = FindLastInScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								GenerateImpliedEndTags();
								if (ErrorEvent != null && !IsCurrent(name))
								{
									ErrUnclosedElements(eltPos, name);
								}
								RemoveFromStack(eltPos);
								goto breakEndtagloop;
							case DispatchGroup.P:
								eltPos = FindLastInButtonScope("p");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Err("No \u201Cp\u201D element in scope but a \u201Cp\u201D end tag seen.");
									// XXX Can the 'in foreign' case happen anymore?
									if (IsInForeign)
									{
										Err("HTML start tag \u201C"
												+ name
												+ "\u201D in a foreign namespace context.");
										while (stack[currentPtr].ns != "http://www.w3.org/1999/xhtml")
										{
											Pop();
										}
									}
									AppendVoidElementToCurrentMayFoster(
											elementName,
											HtmlAttributes.EMPTY_ATTRIBUTES);
									goto breakEndtagloop;
								}
								GenerateImpliedEndTagsExceptFor("p");
								Debug.Assert(eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK);
								if (ErrorEvent != null && eltPos != currentPtr)
								{
									ErrUnclosedElements(eltPos, name);
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								goto breakEndtagloop;
							case DispatchGroup.LI:
								eltPos = FindLastInListScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Err("No \u201Cli\u201D element in list scope but a \u201Cli\u201D end tag seen.");
								}
								else
								{
									GenerateImpliedEndTagsExceptFor(name);
									if (ErrorEvent != null && eltPos != currentPtr)
									{
										ErrUnclosedElements(eltPos, name);
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
								}
								goto breakEndtagloop;
							case DispatchGroup.DD_OR_DT:
								eltPos = FindLastInScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Err("No \u201C"
											+ name
											+ "\u201D element in scope but a \u201C"
											+ name + "\u201D end tag seen.");
								}
								else
								{
									GenerateImpliedEndTagsExceptFor(name);
									if (ErrorEvent != null
											&& eltPos != currentPtr)
									{
										ErrUnclosedElements(eltPos, name);
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
								}
								goto breakEndtagloop;
							case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
								eltPos = FindLastInScopeHn();
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
								}
								else
								{
									GenerateImpliedEndTags();
									if (ErrorEvent != null && !IsCurrent(name))
									{
										ErrUnclosedElements(eltPos, name);
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
								}
								goto breakEndtagloop;
							case DispatchGroup.OBJECT:
							case DispatchGroup.MARQUEE_OR_APPLET:
								eltPos = FindLastInScope(name);
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									ErrStrayEndTag(name);
								}
								else
								{
									GenerateImpliedEndTags();
									if (ErrorEvent != null && !IsCurrent(name))
									{
										ErrUnclosedElements(eltPos, name);
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
									ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
								}
								goto breakEndtagloop;
							case DispatchGroup.BR:
								Err("End tag \u201Cbr\u201D.");
								if (IsInForeign)
								{
									Err("HTML start tag \u201C"
											+ name
											+ "\u201D in a foreign namespace context.");
									while (stack[currentPtr].ns != "http://www.w3.org/1999/xhtml")
									{
										Pop();
									}
								}
								ReconstructTheActiveFormattingElements();
								AppendVoidElementToCurrentMayFoster(
										elementName,
										HtmlAttributes.EMPTY_ATTRIBUTES);
								goto breakEndtagloop;
							case DispatchGroup.AREA_OR_WBR:
							case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK:
							case DispatchGroup.EMBED_OR_IMG:
							case DispatchGroup.IMAGE:
							case DispatchGroup.INPUT:
							case DispatchGroup.KEYGEN: // XXX??
							case DispatchGroup.HR:
							case DispatchGroup.ISINDEX:
							case DispatchGroup.IFRAME:
							case DispatchGroup.NOEMBED: // XXX???
							case DispatchGroup.NOFRAMES: // XXX??
							case DispatchGroup.SELECT:
							case DispatchGroup.TABLE:
							case DispatchGroup.TEXTAREA: // XXX??
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							case DispatchGroup.NOSCRIPT:
								if (IsScriptingEnabled)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								else
								{
									// fall through
									goto case DispatchGroup.A;
								}
							case DispatchGroup.A:
							case DispatchGroup.B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
							case DispatchGroup.FONT:
							case DispatchGroup.NOBR:
								if (AdoptionAgencyEndTag(name))
								{
									goto breakEndtagloop;
								}
								else
								{
									// else handle like any other tag
									goto default;
								}
							default:
								if (IsCurrent(name))
								{
									Pop();
									goto breakEndtagloop;
								}

								eltPos = currentPtr;
								for (; ; )
								{
									StackNode<T> node = stack[eltPos];
									if (node.name == name)
									{
										GenerateImpliedEndTags();
										if (ErrorEvent != null
												&& !IsCurrent(name))
										{
											ErrUnclosedElements(eltPos, name);
										}
										while (currentPtr >= eltPos)
										{
											Pop();
										}
										goto breakEndtagloop;
									}
									else if (node.IsSpecial)
									{
										ErrStrayEndTag(name);
										goto breakEndtagloop;
									}
									eltPos--;
								}
						}
					case InsertionMode.IN_COLUMN_GROUP:
						switch (group)
						{
							case DispatchGroup.COLGROUP:
								if (currentPtr == 0)
								{
									Debug.Assert(fragment);
									Err("Garbage in \u201Ccolgroup\u201D fragment.");
									goto breakEndtagloop;
								}
								Pop();
								mode = InsertionMode.IN_TABLE;
								goto breakEndtagloop;
							case DispatchGroup.COL:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
							default:
								if (currentPtr == 0)
								{
									Debug.Assert(fragment);
									Err("Garbage in \u201Ccolgroup\u201D fragment.");
									goto breakEndtagloop;
								}
								Pop();
								mode = InsertionMode.IN_TABLE;
								continue;
						}
					case InsertionMode.IN_SELECT_IN_TABLE:
						switch (group)
						{
							case DispatchGroup.CAPTION:
							case DispatchGroup.TABLE:
							case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
							case DispatchGroup.TR:
							case DispatchGroup.TD_OR_TH:
								Err("\u201C"
										+ name
										+ "\u201D end tag with \u201Cselect\u201D open.");
								if (FindLastInTableScope(name) != TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									eltPos = FindLastInTableScope("select");
									if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
									{
										Debug.Assert(fragment);
										goto breakEndtagloop; // http://www.w3.org/Bugs/Public/show_bug.cgi?id=8375
									}
									while (currentPtr >= eltPos)
									{
										Pop();
									}
									ResetTheInsertionMode();
									continue;
								}
								else
								{
									goto breakEndtagloop;
								}
							default:
								break;
							// fall through to IN_SELECT
						}
						goto case InsertionMode.IN_SELECT;
					case InsertionMode.IN_SELECT:
						switch (group)
						{
							case DispatchGroup.OPTION:
								if (IsCurrent("option"))
								{
									Pop();
									goto breakEndtagloop;
								}
								else
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
							case DispatchGroup.OPTGROUP:
								if (IsCurrent("option")
										&& "optgroup" == stack[currentPtr - 1].name)
								{
									Pop();
								}
								if (IsCurrent("optgroup"))
								{
									Pop();
								}
								else
								{
									ErrStrayEndTag(name);
								}
								goto breakEndtagloop;
							case DispatchGroup.SELECT:
								eltPos = FindLastInTableScope("select");
								if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								while (currentPtr >= eltPos)
								{
									Pop();
								}
								ResetTheInsertionMode();
								goto breakEndtagloop;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.AFTER_BODY:
						switch (group)
						{
							case DispatchGroup.HTML:
								if (fragment)
								{
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								else
								{
									mode = InsertionMode.AFTER_AFTER_BODY;
									goto breakEndtagloop;
								}
							default:
								Err("Saw an end tag after \u201Cbody\u201D had been closed.");
								mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
								continue;
						}
					case InsertionMode.IN_FRAMESET:
						switch (group)
						{
							case DispatchGroup.FRAMESET:
								if (currentPtr == 0)
								{
									Debug.Assert(fragment);
									ErrStrayEndTag(name);
									goto breakEndtagloop;
								}
								Pop();
								if ((!fragment) && !IsCurrent("frameset"))
								{
									mode = InsertionMode.AFTER_FRAMESET;
								}
								goto breakEndtagloop;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.AFTER_FRAMESET:
						switch (group)
						{
							case DispatchGroup.HTML:
								mode = InsertionMode.AFTER_AFTER_FRAMESET;
								goto breakEndtagloop;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.INITIAL:
						/*
						 * Parse error.
						 */
						// [NOCPP[
						switch (DoctypeExpectation)
						{
							case DoctypeExpectation.Auto:
								Err("End tag seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html:
								Err("End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
								break;
							case DoctypeExpectation.Html401Strict:
								Err("End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
								break;
							case DoctypeExpectation.Html401Transitional:
								Err("End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
								break;
							case DoctypeExpectation.NoDoctypeErrors:
								break;
						}
						// ]NOCPP]
						/*
						 * 
						 * Set the document to quirks mode.
						 */
						DocumentModeInternal(DocumentMode.QuirksMode, null, null,
								false);
						/*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
						mode = InsertionMode.BEFORE_HTML;
						/*
						 * and reprocess the current token.
						 */
						continue;
					case InsertionMode.BEFORE_HTML:
						switch (group)
						{
							case DispatchGroup.HEAD:
							case DispatchGroup.BR:
							case DispatchGroup.HTML:
							case DispatchGroup.BODY:
								/*
								 * Create an HTMLElement node with the tag name
								 * html, in the HTML namespace. Append it to the
								 * Document object.
								 */
								AppendHtmlElementToDocumentAndPush();
								/* Switch to the main mode */
								mode = InsertionMode.BEFORE_HEAD;
								/*
								 * reprocess the current token.
								 */
								continue;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.BEFORE_HEAD:
						switch (group)
						{
							case DispatchGroup.HEAD:
							case DispatchGroup.BR:
							case DispatchGroup.HTML:
							case DispatchGroup.BODY:
								AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
								mode = InsertionMode.IN_HEAD;
								continue;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.IN_HEAD:
						switch (group)
						{
							case DispatchGroup.HEAD:
								Pop();
								mode = InsertionMode.AFTER_HEAD;
								goto breakEndtagloop;
							case DispatchGroup.BR:
							case DispatchGroup.HTML:
							case DispatchGroup.BODY:
								Pop();
								mode = InsertionMode.AFTER_HEAD;
								continue;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.IN_HEAD_NOSCRIPT:
						switch (group)
						{
							case DispatchGroup.NOSCRIPT:
								Pop();
								mode = InsertionMode.IN_HEAD;
								goto breakEndtagloop;
							case DispatchGroup.BR:
								ErrStrayEndTag(name);
								Pop();
								mode = InsertionMode.IN_HEAD;
								continue;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.AFTER_HEAD:
						switch (group)
						{
							case DispatchGroup.HTML:
							case DispatchGroup.BODY:
							case DispatchGroup.BR:
								AppendToCurrentNodeAndPushBodyElement();
								mode = InsertionMode.FRAMESET_OK;
								continue;
							default:
								ErrStrayEndTag(name);
								goto breakEndtagloop;
						}
					case InsertionMode.AFTER_AFTER_BODY:
						ErrStrayEndTag(name);
						mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
						continue;
					case InsertionMode.AFTER_AFTER_FRAMESET:
						ErrStrayEndTag(name);
						mode = InsertionMode.IN_FRAMESET;
						continue;
					case InsertionMode.TEXT:
						// XXX need to manage insertion point here
						Pop();
						if (originalMode == InsertionMode.AFTER_HEAD)
						{
							SilentPop();
						}
						mode = originalMode;
						goto breakEndtagloop;
				}
			} // endtagloop

			breakEndtagloop:
			return;
		}

		private int FindLastInTableScopeOrRootTbodyTheadTfoot()
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].Group == DispatchGroup.TBODY_OR_THEAD_OR_TFOOT)
				{
					return i;
				}
			}
			return 0;
		}

		private int FindLast([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private int FindLastInTableScope([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
				else if (stack[i].name == "table")
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private int FindLastInButtonScope([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
				else if (stack[i].IsScoping || stack[i].name == "button")
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private int FindLastInScope([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
				else if (stack[i].IsScoping)
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private int FindLastInListScope([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
				else if (stack[i].IsScoping || stack[i].name == "ul" || stack[i].name == "ol")
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private int FindLastInScopeHn()
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].Group == DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6)
				{
					return i;
				}
				else if (stack[i].IsScoping)
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private void GenerateImpliedEndTagsExceptFor([Local] string name)
		{
			for (; ; )
			{
				StackNode<T> node = stack[currentPtr];
				switch (node.Group)
				{
					case DispatchGroup.P:
					case DispatchGroup.LI:
					case DispatchGroup.DD_OR_DT:
					case DispatchGroup.OPTION:
					case DispatchGroup.OPTGROUP:
					case DispatchGroup.RT_OR_RP:
						if (node.name == name)
						{
							return;
						}
						Pop();
						continue;
					default:
						return;
				}
			}
		}

		private void GenerateImpliedEndTags()
		{
			for (; ; )
			{
				switch (stack[currentPtr].Group)
				{
					case DispatchGroup.P:
					case DispatchGroup.LI:
					case DispatchGroup.DD_OR_DT:
					case DispatchGroup.OPTION:
					case DispatchGroup.OPTGROUP:
					case DispatchGroup.RT_OR_RP:
						Pop();
						continue;
					default:
						return;
				}
			}
		}

		private bool IsSecondOnStackBody()
		{
			return currentPtr >= 1 && stack[1].Group == DispatchGroup.BODY;
		}

		private void DocumentModeInternal(DocumentMode m, string publicIdentifier,
				string systemIdentifier, bool html4SpecificAdditionalErrorChecks)
		{
			quirks = (m == DocumentMode.QuirksMode);
			if (DocumentModeDetected != null)
			{
				DocumentModeDetected(this, new DocumentModeEventArgs(
						m
					// [NOCPP[
						, publicIdentifier, systemIdentifier,
						html4SpecificAdditionalErrorChecks
					// ]NOCPP]
				));
			}
			// [NOCPP[
			ReceiveDocumentMode(m, publicIdentifier, systemIdentifier,
					html4SpecificAdditionalErrorChecks);
			// ]NOCPP]
		}

		private bool IsAlmostStandards(string publicIdentifier, string systemIdentifier)
		{
			if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
					"-//w3c//dtd xhtml 1.0 transitional//en", publicIdentifier))
			{
				return true;
			}
			if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
					"-//w3c//dtd xhtml 1.0 frameset//en", publicIdentifier))
			{
				return true;
			}
			if (systemIdentifier != null)
			{
				if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
						"-//w3c//dtd html 4.01 transitional//en", publicIdentifier))
				{
					return true;
				}
				if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
						"-//w3c//dtd html 4.01 frameset//en", publicIdentifier))
				{
					return true;
				}
			}
			return false;
		}

		private bool IsQuirky([Local] string name, string publicIdentifier, string systemIdentifier, bool forceQuirks)
		{
			if (forceQuirks)
			{
				return true;
			}
			if (name != TreeBuilderConstants.HTML_LOCAL)
			{
				return true;
			}
			if (publicIdentifier != null)
			{
				for (int i = 0; i < TreeBuilderConstants.QUIRKY_PUBLIC_IDS.Length; i++)
				{
					if (Portability.LowerCaseLiteralIsPrefixOfIgnoreAsciiCaseString(
							TreeBuilderConstants.QUIRKY_PUBLIC_IDS[i], publicIdentifier))
					{
						return true;
					}
				}
				if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
						"-//w3o//dtd w3 html strict 3.0//en//", publicIdentifier)
						|| Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
								"-/w3c/dtd html 4.0 transitional/en",
								publicIdentifier)
						|| Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
								"html", publicIdentifier))
				{
					return true;
				}
			}
			if (systemIdentifier == null)
			{
				if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
						"-//w3c//dtd html 4.01 transitional//en", publicIdentifier))
				{
					return true;
				}
				else if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
					  "-//w3c//dtd html 4.01 frameset//en", publicIdentifier))
				{
					return true;
				}
			}
			else if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
				  "http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd",
				  systemIdentifier))
			{
				return true;
			}
			return false;
		}

		private void CloseTheCell(int eltPos)
		{
			GenerateImpliedEndTags();
			if (ErrorEvent != null && eltPos != currentPtr)
			{
				ErrUnclosedElementsCell(eltPos);
			}
			while (currentPtr >= eltPos)
			{
				Pop();
			}
			ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
			mode = InsertionMode.IN_ROW;
			return;
		}

		private int FindLastInTableScopeTdTh()
		{
			for (int i = currentPtr; i > 0; i--)
			{
				/*[Local]*/
				string name = stack[i].name;
				if ("td" == name || "th" == name)
				{
					return i;
				}
				else if (name == "table")
				{
					return TreeBuilderConstants.NOT_FOUND_ON_STACK;
				}
			}
			return TreeBuilderConstants.NOT_FOUND_ON_STACK;
		}

		private void ClearStackBackTo(int eltPos)
		{
			while (currentPtr > eltPos)
			{ // > not >= intentional
				Pop();
			}
		}

		private void ResetTheInsertionMode()
		{
			StackNode<T> node;
			/*[Local]*/
			string name;
			/*[NsUri]*/
			string ns;
			for (int i = currentPtr; i >= 0; i--)
			{
				node = stack[i];
				name = node.name;
				ns = node.ns;
				if (i == 0)
				{
					if (!(contextNamespace == "http://www.w3.org/1999/xhtml" && (contextName == "td" || contextName == "th")))
					{
						name = contextName;
						ns = contextNamespace;
					}
					else
					{
						mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY; // XXX from Hixie's email
						return;
					}
				}
				if ("select" == name)
				{
					mode = InsertionMode.IN_SELECT;
					return;
				}
				else if ("td" == name || "th" == name)
				{
					mode = InsertionMode.IN_CELL;
					return;
				}
				else if ("tr" == name)
				{
					mode = InsertionMode.IN_ROW;
					return;
				}
				else if ("tbody" == name || "thead" == name || "tfoot" == name)
				{
					mode = InsertionMode.IN_TABLE_BODY;
					return;
				}
				else if ("caption" == name)
				{
					mode = InsertionMode.IN_CAPTION;
					return;
				}
				else if ("colgroup" == name)
				{
					mode = InsertionMode.IN_COLUMN_GROUP;
					return;
				}
				else if ("table" == name)
				{
					mode = InsertionMode.IN_TABLE;
					return;
				}
				else if ("http://www.w3.org/1999/xhtml" != ns)
				{
					mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
					return;
				}
				else if ("head" == name)
				{
					mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY; // really
					return;
				}
				else if ("body" == name)
				{
					mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
					return;
				}
				else if ("frameset" == name)
				{
					mode = InsertionMode.IN_FRAMESET;
					return;
				}
				else if ("html" == name)
				{
					if (headPointer == null)
					{
						mode = InsertionMode.BEFORE_HEAD;
					}
					else
					{
						mode = InsertionMode.AFTER_HEAD;
					}
					return;
				}
				else if (i == 0)
				{
					mode = framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
					return;
				}
			}
		}

		private void ImplicitlyCloseP()
		{
			int eltPos = FindLastInButtonScope("p");
			if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
			{
				return;
			}
			GenerateImpliedEndTagsExceptFor("p");
			if (ErrorEvent != null && eltPos != currentPtr)
			{
				ErrUnclosedElementsImplied(eltPos, "p");
			}
			while (currentPtr >= eltPos)
			{
				Pop();
			}
		}

		private bool ClearLastStackSlot()
		{
			stack[currentPtr] = null;
			return true;
		}

		private bool ClearLastListSlot()
		{
			listOfActiveFormattingElements[listPtr] = null;
			return true;
		}

		private void Push(StackNode<T> node)
		{
			currentPtr++;
			if (currentPtr == stack.Length)
			{
				StackNode<T>[] newStack = new StackNode<T>[stack.Length + 64];
				Array.Copy(stack, newStack, stack.Length);
				stack = newStack;
			}
			stack[currentPtr] = node;
			ElementPushed(node.ns, node.popName, node.node);
		}

		private void SilentPush(StackNode<T> node)
		{
			currentPtr++;
			if (currentPtr == stack.Length)
			{
				StackNode<T>[] newStack = new StackNode<T>[stack.Length + 64];
				Array.Copy(stack, newStack, stack.Length);
				stack = newStack;
			}
			stack[currentPtr] = node;
		}

		private void Append(StackNode<T> node)
		{
			listPtr++;
			if (listPtr == listOfActiveFormattingElements.Length)
			{
				StackNode<T>[] newList = new StackNode<T>[listOfActiveFormattingElements.Length + 64];
				Array.Copy(listOfActiveFormattingElements, newList, listOfActiveFormattingElements.Length);
				listOfActiveFormattingElements = newList;
			}
			listOfActiveFormattingElements[listPtr] = node;
		}

		private void InsertMarker()
		{
			Append(null);
		}

		private void ClearTheListOfActiveFormattingElementsUpToTheLastMarker()
		{
			while (listPtr > -1)
			{
				if (listOfActiveFormattingElements[listPtr] == null)
				{
					--listPtr;
					return;
				}
				listOfActiveFormattingElements[listPtr].Release();
				--listPtr;
			}
		}

		private bool IsCurrent([Local] string name)
		{
			return name == stack[currentPtr].name;
		}

		private void RemoveFromStack(int pos)
		{
			if (currentPtr == pos)
			{
				Pop();
			}
			else
			{
				Fatal();
				stack[pos].Release();
				Array.Copy(stack, pos + 1, stack, pos, currentPtr - pos);
				Debug.Assert(ClearLastStackSlot());
				currentPtr--;
			}
		}

		private void RemoveFromStack(StackNode<T> node)
		{
			if (stack[currentPtr] == node)
			{
				Pop();
			}
			else
			{
				int pos = currentPtr - 1;
				while (pos >= 0 && stack[pos] != node)
				{
					pos--;
				}
				if (pos == -1)
				{
					// dead code?
					return;
				}
				Fatal();
				node.Release();
				Array.Copy(stack, pos + 1, stack, pos, currentPtr - pos);
				currentPtr--;
			}
		}

		private void RemoveFromListOfActiveFormattingElements(int pos)
		{
			Debug.Assert(listOfActiveFormattingElements[pos] != null);
			listOfActiveFormattingElements[pos].Release();
			if (pos == listPtr)
			{
				Debug.Assert(ClearLastListSlot());
				listPtr--;
				return;
			}
			Debug.Assert(pos < listPtr);
			Array.Copy(listOfActiveFormattingElements, pos + 1, listOfActiveFormattingElements, pos, listPtr - pos);
			Debug.Assert(ClearLastListSlot());
			listPtr--;
		}

		private bool AdoptionAgencyEndTag([Local] string name)
		{
			// If you crash around here, perhaps some stack node variable claimed to
			// be a weak ref isn't.
			for (int i = 0; i < 8; ++i)
			{
				int formattingEltListPos = listPtr;
				while (formattingEltListPos > -1)
				{
					StackNode<T> listNode = listOfActiveFormattingElements[formattingEltListPos]; // weak
					// ref
					if (listNode == null)
					{
						formattingEltListPos = -1;
						break;
					}
					else if (listNode.name == name)
					{
						break;
					}
					formattingEltListPos--;
				}
				if (formattingEltListPos == -1)
				{
					return false;
				}
				StackNode<T> formattingElt = listOfActiveFormattingElements[formattingEltListPos]; // this
				// *looks*
				// like
				// a
				// weak
				// ref
				// to
				// the
				// list
				// of
				// formatting
				// elements
				int formattingEltStackPos = currentPtr;
				bool inScope = true;
				while (formattingEltStackPos > -1)
				{
					StackNode<T> node = stack[formattingEltStackPos]; // weak ref
					if (node == formattingElt)
					{
						break;
					}
					else if (node.IsScoping)
					{
						inScope = false;
					}
					formattingEltStackPos--;
				}
				if (formattingEltStackPos == -1)
				{
					Err("No element \u201C" + name + "\u201D to close.");
					RemoveFromListOfActiveFormattingElements(formattingEltListPos);
					return true;
				}
				if (!inScope)
				{
					Err("No element \u201C" + name + "\u201D to close.");
					return true;
				}
				// stackPos now points to the formatting element and it is in scope
				if (ErrorEvent != null && formattingEltStackPos != currentPtr)
				{
					Err("End tag \u201C" + name + "\u201D violates nesting rules.");
				}
				int furthestBlockPos = formattingEltStackPos + 1;
				while (furthestBlockPos <= currentPtr)
				{
					StackNode<T> node = stack[furthestBlockPos]; // weak ref
					if (node.IsSpecial)
					{
						break;
					}
					furthestBlockPos++;
				}
				if (furthestBlockPos > currentPtr)
				{
					// no furthest block
					while (currentPtr >= formattingEltStackPos)
					{
						Pop();
					}
					RemoveFromListOfActiveFormattingElements(formattingEltListPos);
					return true;
				}
				StackNode<T> commonAncestor = stack[formattingEltStackPos - 1]; // weak
				// ref
				StackNode<T> furthestBlock = stack[furthestBlockPos]; // weak ref
				// detachFromParent(furthestBlock.node); XXX AAA CHANGE
				int bookmark = formattingEltListPos;
				int nodePos = furthestBlockPos;
				StackNode<T> lastNode = furthestBlock; // weak ref
				for (int j = 0; j < 3; ++j)
				{
					nodePos--;
					StackNode<T> node = stack[nodePos]; // weak ref
					int nodeListPos = FindInListOfActiveFormattingElements(node);
					if (nodeListPos == -1)
					{
						Debug.Assert(formattingEltStackPos < nodePos);
						Debug.Assert(bookmark < nodePos);
						Debug.Assert(furthestBlockPos > nodePos);
						RemoveFromStack(nodePos); // node is now a bad pointer in
						// C++
						furthestBlockPos--;
						continue;
					}
					// now node is both on stack and in the list
					if (nodePos == formattingEltStackPos)
					{
						break;
					}
					if (nodePos == furthestBlockPos)
					{
						bookmark = nodeListPos + 1;
					}
					// if (hasChildren(node.node)) { XXX AAA CHANGE
					Debug.Assert(node == listOfActiveFormattingElements[nodeListPos]);
					Debug.Assert(node == stack[nodePos]);
					T clone = CreateElement("http://www.w3.org/1999/xhtml",
							node.name, node.attributes.CloneAttributes());
					StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
							node.name, clone, node.popName, node.attributes
						// [NOCPP[
							, node.Locator
						// ]NOCPP]       
					); // creation
					// ownership
					// goes
					// to
					// stack
					node.DropAttributes(); // adopt ownership to newNode
					stack[nodePos] = newNode;
					newNode.Retain(); // retain for list
					listOfActiveFormattingElements[nodeListPos] = newNode;
					node.Release(); // release from stack
					node.Release(); // release from list
					node = newNode;
					// } XXX AAA CHANGE
					DetachFromParent(lastNode.node);
					AppendElement(lastNode.node, node.node);
					lastNode = node;
				}
				if (commonAncestor.IsFosterParenting)
				{
					Fatal();
					DetachFromParent(lastNode.node);
					InsertIntoFosterParent(lastNode.node);
				}
				else
				{
					DetachFromParent(lastNode.node);
					AppendElement(lastNode.node, commonAncestor.node);
				}
				T clone2 = CreateElement("http://www.w3.org/1999/xhtml",
						formattingElt.name,
						formattingElt.attributes.CloneAttributes());
				StackNode<T> formattingClone = new StackNode<T>(
						formattingElt.Flags, formattingElt.ns,
						formattingElt.name, clone2, formattingElt.popName,
						formattingElt.attributes
					// [NOCPP[
						, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
					// ]NOCPP]
				); // Ownership
				// transfers
				// to
				// stack
				// below
				formattingElt.DropAttributes(); // transfer ownership to formattingClone
				AppendChildrenToNewParent(furthestBlock.node, clone2);
				AppendElement(clone2, furthestBlock.node);
				RemoveFromListOfActiveFormattingElements(formattingEltListPos);
				InsertIntoListOfActiveFormattingElements(formattingClone, bookmark);
				Debug.Assert(formattingEltStackPos < furthestBlockPos);
				RemoveFromStack(formattingEltStackPos);
				// furthestBlockPos is now off by one and points to the slot after it
				InsertIntoStack(formattingClone, furthestBlockPos);
			}
			return true;
		}

		private void InsertIntoStack(StackNode<T> node, int position)
		{
			Debug.Assert(currentPtr + 1 < stack.Length);
			Debug.Assert(position <= currentPtr + 1);
			if (position == currentPtr + 1)
			{
				Push(node);
			}
			else
			{
				Array.Copy(stack, position, stack, position + 1,
						(currentPtr - position) + 1);
				currentPtr++;
				stack[position] = node;
			}
		}

		private void InsertIntoListOfActiveFormattingElements(
				StackNode<T> formattingClone, int bookmark)
		{
			formattingClone.Retain();
			Debug.Assert(listPtr + 1 < listOfActiveFormattingElements.Length);
			if (bookmark <= listPtr)
			{
				Array.Copy(listOfActiveFormattingElements, bookmark,
						listOfActiveFormattingElements, bookmark + 1,
						(listPtr - bookmark) + 1);
			}
			listPtr++;
			listOfActiveFormattingElements[bookmark] = formattingClone;
		}

		private int FindInListOfActiveFormattingElements(StackNode<T> node)
		{
			for (int i = listPtr; i >= 0; i--)
			{
				if (node == listOfActiveFormattingElements[i])
				{
					return i;
				}
			}
			return -1;
		}

		private int FindInListOfActiveFormattingElementsContainsBetweenEndAndLastMarker([Local] string name)
		{
			for (int i = listPtr; i >= 0; i--)
			{
				StackNode<T> node = listOfActiveFormattingElements[i];
				if (node == null)
				{
					return -1;
				}
				else if (node.name == name)
				{
					return i;
				}
			}
			return -1;
		}


		private void MaybeForgetEarlierDuplicateFormattingElement([Local] string name, HtmlAttributes attributes)
		{
			int candidate = -1;
			int count = 0;
			for (int i = listPtr; i >= 0; i--)
			{
				StackNode<T> node = listOfActiveFormattingElements[i];
				if (node == null)
				{
					break;
				}
				if (node.name == name && node.attributes.Equals(attributes))
				{
					candidate = i;
					++count;
				}
			}
			if (count >= 3)
			{
				RemoveFromListOfActiveFormattingElements(candidate);
			}
		}

		private int FindLastOrRoot([Local] string name)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].name == name)
				{
					return i;
				}
			}
			return 0;
		}

		private int FindLastOrRoot(DispatchGroup group)
		{
			for (int i = currentPtr; i > 0; i--)
			{
				if (stack[i].Group == group)
				{
					return i;
				}
			}
			return 0;
		}

		/// <summary>
		/// Attempt to add attribute to the body element.
		/// </summary>
		/// <param name="attributes">The attributes.</param>
		/// <returns><c>true</c> if the attributes were added</returns>
		private bool AddAttributesToBody(HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			if (currentPtr >= 1)
			{
				StackNode<T> body = stack[1];
				if (body.Group == DispatchGroup.BODY)
				{
					AddAttributesToElement(body.node, attributes);
					return true;
				}
			}
			return false;
		}

		private void AddAttributesToHtml(HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			AddAttributesToElement(stack[0].node, attributes);
		}

		private void PushHeadPointerOntoStack()
		{
			Debug.Assert(headPointer != null);
			Debug.Assert(!fragment);
			Debug.Assert(mode == InsertionMode.AFTER_HEAD);
			Fatal();
			SilentPush(new StackNode<T>(ElementName.HEAD, headPointer
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			));
		}

		private void ReconstructTheActiveFormattingElements()
		{
			if (listPtr == -1)
			{
				return;
			}
			StackNode<T> mostRecent = listOfActiveFormattingElements[listPtr];
			if (mostRecent == null || IsInStack(mostRecent))
			{
				return;
			}
			int entryPos = listPtr;
			for (; ; )
			{
				entryPos--;
				if (entryPos == -1)
				{
					break;
				}
				if (listOfActiveFormattingElements[entryPos] == null)
				{
					break;
				}
				if (IsInStack(listOfActiveFormattingElements[entryPos]))
				{
					break;
				}
			}
			while (entryPos < listPtr)
			{
				entryPos++;
				StackNode<T> entry = listOfActiveFormattingElements[entryPos];
				T clone = CreateElement("http://www.w3.org/1999/xhtml", entry.name,
						entry.attributes.CloneAttributes());
				StackNode<T> entryClone = new StackNode<T>(entry.Flags,
						entry.ns, entry.name, clone, entry.popName,
						entry.attributes
					// [NOCPP[
						, entry.Locator
					// ]NOCPP]
				);
				entry.DropAttributes(); // transfer ownership to entryClone
				StackNode<T> currentNode = stack[currentPtr];
				if (currentNode.IsFosterParenting)
				{
					InsertIntoFosterParent(clone);
				}
				else
				{
					AppendElement(clone, currentNode.node);
				}
				Push(entryClone);
				// stack takes ownership of the local variable
				listOfActiveFormattingElements[entryPos] = entryClone;
				// overwriting the old entry on the list, so release & retain
				entry.Release();
				entryClone.Retain();
			}
		}

		private void InsertIntoFosterParent(T child)
		{
			int eltPos = FindLastOrRoot(DispatchGroup.TABLE);
			StackNode<T> node = stack[eltPos];
			T elt = node.node;
			if (eltPos == 0)
			{
				AppendElement(child, elt);
				return;
			}
			InsertFosterParentedChild(child, elt, stack[eltPos - 1].node);
		}

		private bool IsInStack(StackNode<T> node)
		{
			for (int i = currentPtr; i >= 0; i--)
			{
				if (stack[i] == node)
				{
					return true;
				}
			}
			return false;
		}

		private void Pop()
		{
			StackNode<T> node = stack[currentPtr];
			Debug.Assert(ClearLastStackSlot());
			currentPtr--;
			ElementPopped(node.ns, node.popName, node.node);
			node.Release();
		}

		private void SilentPop()
		{
			StackNode<T> node = stack[currentPtr];
			Debug.Assert(ClearLastStackSlot());
			currentPtr--;
			node.Release();
		}

		private void PopOnEof()
		{
			StackNode<T> node = stack[currentPtr];
			Debug.Assert(ClearLastStackSlot());
			currentPtr--;
			MarkMalformedIfScript(node.node);
			ElementPopped(node.ns, node.popName, node.node);
			node.Release();
		}

		// [NOCPP[
		private void CheckAttributes(HtmlAttributes attributes, [NsUri] string ns)
		{
			if (ErrorEvent != null)
			{
				int len = attributes.XmlnsLength;
				for (int i = 0; i < len; i++)
				{
					AttributeName name = attributes.GetXmlnsAttributeName(i);
					if (name == AttributeName.XMLNS)
					{
						if (html4)
						{
							Err("Attribute \u201Cxmlns\u201D not allowed here. (HTML4-only error.)");
						}
						else
						{
							string xmlns = attributes.GetXmlnsValue(i);
							if (ns != xmlns)
							{
								Err("Bad value \u201C"
										+ xmlns
										+ "\u201D for the attribute \u201Cxmlns\u201D (only \u201C"
										+ ns + "\u201D permitted here).");
								switch (NamePolicy)
								{
									case XmlViolationPolicy.AlterInfoset:
									// fall through
									case XmlViolationPolicy.Allow:
										Warn("Attribute \u201Cxmlns\u201D is not serializable as XML 1.0.");
										break;
									case XmlViolationPolicy.Fatal:
										Fatal("Attribute \u201Cxmlns\u201D is not serializable as XML 1.0.");
										break;
								}
							}
						}
					}
					else if (ns != "http://www.w3.org/1999/xhtml"
						  && name == AttributeName.XMLNS_XLINK)
					{
						string xmlns = attributes.GetXmlnsValue(i);
						if ("http://www.w3.org/1999/xlink" != xmlns)
						{
							Err("Bad value \u201C"
									+ xmlns
									+ "\u201D for the attribute \u201Cxmlns:link\u201D (only \u201Chttp://www.w3.org/1999/xlink\u201D permitted here).");
							switch (NamePolicy)
							{
								case XmlViolationPolicy.AlterInfoset:
								// fall through
								case XmlViolationPolicy.Allow:
									Warn("Attribute \u201Cxmlns:xlink\u201D with a value other than \u201Chttp://www.w3.org/1999/xlink\u201D is not serializable as XML 1.0 without changing document semantics.");
									break;
								case XmlViolationPolicy.Fatal:
									Fatal("Attribute \u201Cxmlns:xlink\u201D with a value other than \u201Chttp://www.w3.org/1999/xlink\u201D is not serializable as XML 1.0 without changing document semantics.");
									break;
							}
						}
					}
					else
					{
						Err("Attribute \u201C" + attributes.GetXmlnsLocalName(i)
								+ "\u201D not allowed here.");
						switch (NamePolicy)
						{
							case XmlViolationPolicy.AlterInfoset:
							// fall through
							case XmlViolationPolicy.Allow:
								Warn("Attribute with the local name \u201C"
										+ attributes.GetXmlnsLocalName(i)
										+ "\u201D is not serializable as XML 1.0.");
								break;
							case XmlViolationPolicy.Fatal:
								Fatal("Attribute with the local name \u201C"
										+ attributes.GetXmlnsLocalName(i)
										+ "\u201D is not serializable as XML 1.0.");
								break;
						}
					}
				}
			}
			attributes.ProcessNonNcNames(this, NamePolicy);
		}

		private string CheckPopName([Local] string name)
		{
			if (NCName.IsNCName(name))
			{
				return name;
			}
			else
			{
				switch (NamePolicy)
				{
					case XmlViolationPolicy.Allow:
						Warn("Element name \u201C" + name
								+ "\u201D cannot be represented as XML 1.0.");
						return name;
					case XmlViolationPolicy.AlterInfoset:
						Warn("Element name \u201C" + name
								+ "\u201D cannot be represented as XML 1.0.");
						return NCName.EscapeName(name);
					case XmlViolationPolicy.Fatal:
						Fatal("Element name \u201C" + name
								+ "\u201D cannot be represented as XML 1.0.");
						break;
				}
			}
			return null; // keep compiler happy
		}

		// ]NOCPP]

		private void AppendHtmlElementToDocumentAndPush(HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			T elt = CreateHtmlElementSetAsRoot(attributes);
			StackNode<T> node = new StackNode<T>(ElementName.HTML,
					elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendHtmlElementToDocumentAndPush()
		{
			AppendHtmlElementToDocumentAndPush(tokenizer.EmptyAttributes());
		}

		private void AppendToCurrentNodeAndPushHeadElement(HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1999/xhtml", "head",
					attributes);
			AppendElement(elt, stack[currentPtr].node);
			headPointer = elt;
			StackNode<T> node = new StackNode<T>(ElementName.HEAD,
					elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendToCurrentNodeAndPushBodyElement(HtmlAttributes attributes)
		{
			AppendToCurrentNodeAndPushElement(ElementName.BODY,
					attributes);
		}

		private void AppendToCurrentNodeAndPushBodyElement()
		{
			AppendToCurrentNodeAndPushBodyElement(tokenizer.EmptyAttributes());
		}

		private void AppendToCurrentNodeAndPushFormElementMayFoster(
				HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1999/xhtml", "form",
					attributes);
			formPointer = elt;
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			StackNode<T> node = new StackNode<T>(ElementName.FORM,
					elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendToCurrentNodeAndPushFormattingElementMayFoster(ElementName elementName, HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			// This method can't be called for custom elements
			T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			StackNode<T> node = new StackNode<T>(elementName, elt, attributes.CloneAttributes()
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
			Append(node);
			node.Retain(); // append doesn't retain itself
		}

		private void AppendToCurrentNodeAndPushElement(ElementName elementName, HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			// This method can't be called for custom elements
			T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes);
			AppendElement(elt, stack[currentPtr].node);
			StackNode<T> node = new StackNode<T>(elementName, elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendToCurrentNodeAndPushElementMayFoster(ElementName elementName,
				HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.name;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1999/xhtml", popName, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			StackNode<T> node = new StackNode<T>(elementName, elt, popName
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendToCurrentNodeAndPushElementMayFosterMathML(
				ElementName elementName, HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.name;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1998/Math/MathML");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1998/Math/MathML", popName,
					attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			bool markAsHtmlIntegrationPoint = false;
			if (ElementName.ANNOTATION_XML == elementName
					&& AnnotationXmlEncodingPermitsHtml(attributes))
			{
				markAsHtmlIntegrationPoint = true;
			}
			StackNode<T> node = new StackNode<T>(elementName, elt, popName,
					markAsHtmlIntegrationPoint
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private bool AnnotationXmlEncodingPermitsHtml(HtmlAttributes attributes)
		{
			string encoding = attributes.GetValue(AttributeName.ENCODING);
			if (encoding == null)
			{
				return false;
			}
			return Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
					"application/xhtml+xml", encoding)
					|| Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
							"text/html", encoding);
		}

		private void AppendToCurrentNodeAndPushElementMayFosterSVG(
				ElementName elementName, HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.camelCaseName;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/2000/svg");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/2000/svg", popName, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			StackNode<T> node = new StackNode<T>(elementName, popName, elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendToCurrentNodeAndPushElementMayFoster(ElementName elementName, HtmlAttributes attributes, T form)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			// Can't be called for custom elements
			T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes, fragment ? null
					: form);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			StackNode<T> node = new StackNode<T>(elementName, elt
				// [NOCPP[
					, ErrorEvent == null ? null : new TaintableLocator(tokenizer)
				// ]NOCPP]
			);
			Push(node);
		}

		private void AppendVoidElementToCurrentMayFoster(
				[Local] string name, HtmlAttributes attributes, T form)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			// Can't be called for custom elements
			T elt = CreateElement("http://www.w3.org/1999/xhtml", name, attributes, fragment ? null : form);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			ElementPushed("http://www.w3.org/1999/xhtml", name, elt);
			ElementPopped("http://www.w3.org/1999/xhtml", name, elt);
		}

		private void AppendVoidElementToCurrentMayFoster(
				ElementName elementName, HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.name;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1999/xhtml", popName, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			ElementPushed("http://www.w3.org/1999/xhtml", popName, elt);
			ElementPopped("http://www.w3.org/1999/xhtml", popName, elt);
		}

		private void AppendVoidElementToCurrentMayFosterSVG(
				ElementName elementName, HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.camelCaseName;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/2000/svg");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/2000/svg", popName, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			ElementPushed("http://www.w3.org/2000/svg", popName, elt);
			ElementPopped("http://www.w3.org/2000/svg", popName, elt);
		}

		private void AppendVoidElementToCurrentMayFosterMathML(ElementName elementName, HtmlAttributes attributes)
		{
			/*[Local]*/
			string popName = elementName.name;
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1998/Math/MathML");
			if (elementName.IsCustom)
			{
				popName = CheckPopName(popName);
			}
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1998/Math/MathML", popName, attributes);
			StackNode<T> current = stack[currentPtr];
			if (current.IsFosterParenting)
			{
				Fatal();
				InsertIntoFosterParent(elt);
			}
			else
			{
				AppendElement(elt, current.node);
			}
			ElementPushed("http://www.w3.org/1998/Math/MathML", popName, elt);
			ElementPopped("http://www.w3.org/1998/Math/MathML", popName, elt);
		}

		private void AppendVoidElementToCurrent(
				[Local] string name, HtmlAttributes attributes, T form)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			// Can't be called for custom elements
			T elt = CreateElement("http://www.w3.org/1999/xhtml", name, attributes, fragment ? null : form);
			StackNode<T> current = stack[currentPtr];
			AppendElement(elt, current.node);
			ElementPushed("http://www.w3.org/1999/xhtml", name, elt);
			ElementPopped("http://www.w3.org/1999/xhtml", name, elt);
		}

		private void AppendVoidFormToCurrent(HtmlAttributes attributes)
		{
			// [NOCPP[
			CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
			// ]NOCPP]
			T elt = CreateElement("http://www.w3.org/1999/xhtml", "form",
					attributes);
			formPointer = elt;
			// ownership transferred to form pointer
			StackNode<T> current = stack[currentPtr];
			AppendElement(elt, current.node);
			ElementPushed("http://www.w3.org/1999/xhtml", "form", elt);
			ElementPopped("http://www.w3.org/1999/xhtml", "form", elt);
		}

		// [NOCPP[

		private void AccumulateCharactersForced(char[] buf, int start, int length)
		{
            charBuffer.Append(buf, start, length);
		}

		// ]NOCPP]

		protected virtual void AccumulateCharacters(char[] buf, int start, int length)
		{
			AppendCharacters(stack[currentPtr].node, buf, start, length);
		}

		// ------------------------------- //

		protected void RequestSuspension()
		{
			tokenizer.RequestSuspension();
		}

		protected abstract T CreateElement([NsUri] string ns, [Local] string name,
				HtmlAttributes attributes);

		protected virtual T CreateElement([NsUri] string ns, [Local] string name,
				HtmlAttributes attributes, T form)
		{
			return CreateElement("http://www.w3.org/1999/xhtml", name, attributes);
		}

		protected abstract T CreateHtmlElementSetAsRoot(HtmlAttributes attributes);

		protected abstract void DetachFromParent(T element);

		protected abstract bool HasChildren(T element);

		protected abstract void AppendElement(T child, T newParent);

		protected abstract void AppendChildrenToNewParent(T oldParent, T newParent);

		protected abstract void InsertFosterParentedChild(T child, T table, T stackParent);

        protected abstract void InsertFosterParentedCharacters(
                StringBuilder sb, T table, T stackParent);

		protected abstract void AppendCharacters(T parent, char[] buf,
				int start, int length);
        
        protected abstract void AppendCharacters(T parent, StringBuilder sb);

		protected abstract void AppendIsindexPrompt(T parent);

		protected abstract void AppendComment(T parent, char[] buf, int start, int length);

		protected abstract void AppendCommentToDocument(char[] buf, int start, int length);

		protected abstract void AddAttributesToElement(T element, HtmlAttributes attributes);

		protected void MarkMalformedIfScript(T elt)
		{

		}

		protected virtual void Start(bool fragmentMode)
		{

		}

		protected virtual void End()
		{

		}

		protected virtual void AppendDoctypeToDocument([Local] string name,
				string publicIdentifier, string systemIdentifier)
		{

		}

		protected virtual void ElementPushed([NsUri] string ns, [Local] string name, T node)
		{

		}

		protected virtual void ElementPopped([NsUri] string ns, [Local] string name, T node)
		{

		}

		// [NOCPP[

		protected virtual void ReceiveDocumentMode(DocumentMode m, string publicIdentifier,
				string systemIdentifier, bool html4SpecificAdditionalErrorChecks)
		{
			// is overridden is subclasses
		}

		/// <summary>
		/// If this handler implementation cares about comments, return <code>true</code>.
		/// If not, return <code>false</code>
		/// </summary>
		/// <returns>
		/// Whether this handler wants comments
		/// </returns>
		public bool WantsComments
		{
			get; set;
		}

        public bool AllowSelfClosingTags { get; set; }


		/**
		 * The argument MUST be an interned string or <code>null</code>.
		 * 
		 * @param context
		 */
		public void SetFragmentContext([Local] string context)
		{
			this.contextName = context;
			this.contextNamespace = "http://www.w3.org/1999/xhtml";
			this.contextNode = null;
			this.fragment = (contextName != null);
			this.quirks = false;
		}

		// ]NOCPP]

		/// <summary>
		/// Checks if the CDATA sections are allowed.
		/// </summary>
		/// <returns>
		///   <c>true</c> if CDATA sections are allowed
		/// </returns>
		public bool IsCDataSectionAllowed
		{
			get
			{
				return IsInForeign;
			}
		}

		private bool IsInForeign
		{
			get
			{
				return currentPtr >= 0 && stack[currentPtr].ns != "http://www.w3.org/1999/xhtml";
			}
		}


        private bool IsInForeignButNotHtmlOrMathTextIntegrationPoint 
        {
            get {
                if (currentPtr < 0) {
                    return false;
                }
                return !IsSpecialParentInForeign(stack[currentPtr]);
            }
        }
		/**
		 * The argument MUST be an interned string or <code>null</code>.
		 * 
		 * @param context
		 */
		public void SetFragmentContext([Local] string context,
				[NsUri] string ns, T node, bool quirks)
		{
			this.contextName = context;
			this.contextNamespace = ns;
			this.contextNode = node;
			this.fragment = (contextName != null);
			this.quirks = quirks;
		}

		protected T CurrentNode()
		{
			return stack[currentPtr].node;
		}

		/// <summary>
		/// Flushes the pending characters. Public for document.write use cases only.
		/// </summary>
		public void FlushCharacters()
		{
			if (charBufferLen > 0)
			{
				if ((mode == InsertionMode.IN_TABLE || mode == InsertionMode.IN_TABLE_BODY || mode == InsertionMode.IN_ROW)
						&& CharBufferContainsNonWhitespace())
				{
					Err("Misplaced non-space characters insided a table.");
					ReconstructTheActiveFormattingElements();
					if (!stack[currentPtr].IsFosterParenting)
					{
						// reconstructing gave us a new current node
						AppendCharacters(CurrentNode(), charBuffer);
                        charBuffer.Clear();
						return;
					}
					int eltPos = FindLastOrRoot(DispatchGroup.TABLE);
					StackNode<T> node = stack[eltPos];
					T elt = node.node;
					if (eltPos == 0)
					{
						AppendCharacters(elt, charBuffer);
                        charBuffer.Clear();
						return;
					}
					InsertFosterParentedCharacters(charBuffer,elt, stack[eltPos - 1].node);
                    charBuffer.Clear();
					return;
				}
				AppendCharacters(CurrentNode(), charBuffer);
                charBuffer.Clear();
			}
		}

		private bool CharBufferContainsNonWhitespace()
		{
			for (int i = 0; i < charBufferLen; i++)
			{
				switch (charBuffer[i])
				{
					case ' ':
					case '\t':
					case '\n':
					case '\r':
					case '\u000C':
						continue;
					default:
						return true;
				}
			}
			return false;
		}

		#region Snapshots

		/// <summary>
		/// Creates a comparable snapshot of the tree builder state. Snapshot
		/// creation is only supported immediately after a script end tag has been
		/// processed. In C++ the caller is responsible for calling
		/// <code>delete</code> on the returned object.
		/// </summary>
		/// <returns>A snapshot</returns>
		public ITreeBuilderState<T> NewSnapshot()
		{
			StackNode<T>[] listCopy = new StackNode<T>[listPtr + 1];
			for (int i = 0; i < listCopy.Length; i++)
			{
				StackNode<T> node = listOfActiveFormattingElements[i];
				if (node != null)
				{
					StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
							node.name, node.node, node.popName,
							node.attributes.CloneAttributes()
						// [NOCPP[
							, node.Locator
						// ]NOCPP]
					);
					listCopy[i] = newNode;
				}
				else
				{
					listCopy[i] = null;
				}
			}
			StackNode<T>[] stackCopy = new StackNode<T>[currentPtr + 1];
			for (int i = 0; i < stackCopy.Length; i++)
			{
				StackNode<T> node = stack[i];
				int listIndex = FindInListOfActiveFormattingElements(node);
				if (listIndex == -1)
				{
					StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
							node.name, node.node, node.popName,
							null
						// [NOCPP[
							, node.Locator
						// ]NOCPP]
					);
					stackCopy[i] = newNode;
				}
				else
				{
					stackCopy[i] = listCopy[listIndex];
					stackCopy[i].Retain();
				}
			}
			return new StateSnapshot<T>(stackCopy, listCopy, formPointer, headPointer, deepTreeSurrogateParent, mode, originalMode, framesetOk, needToDropLF, quirks);
		}

		public bool SnapshotMatches(ITreeBuilderState<T> snapshot)
		{
			StackNode<T>[] stackCopy = snapshot.Stack;
			int stackLen = snapshot.Stack.Length;
			StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
			int listLen = snapshot.ListOfActiveFormattingElements.Length;

			if (stackLen != currentPtr + 1
					|| listLen != listPtr + 1
					|| formPointer != snapshot.FormPointer
					|| headPointer != snapshot.HeadPointer
					|| deepTreeSurrogateParent != snapshot.DeepTreeSurrogateParent
					|| mode != snapshot.Mode
					|| originalMode != snapshot.OriginalMode
					|| framesetOk != snapshot.IsFramesetOk
					|| needToDropLF != snapshot.IsNeedToDropLF
					|| quirks != snapshot.IsQuirks)
			{ // maybe just assert quirks
				return false;
			}
			for (int i = listLen - 1; i >= 0; i--)
			{
				if (listCopy[i] == null
						&& listOfActiveFormattingElements[i] == null)
				{
					continue;
				}
				else if (listCopy[i] == null
					  || listOfActiveFormattingElements[i] == null)
				{
					return false;
				}
				if (listCopy[i].node != listOfActiveFormattingElements[i].node)
				{
					return false; // it's possible that this condition is overly
					// strict
				}
			}
			for (int i = stackLen - 1; i >= 0; i--)
			{
				if (stackCopy[i].node != stack[i].node)
				{
					return false;
				}
			}
			return true;
		}

		public void LoadState(ITreeBuilderState<T> snapshot)
		{
			StackNode<T>[] stackCopy = snapshot.Stack;
			int stackLen = snapshot.Stack.Length;
			StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
			int listLen = snapshot.ListOfActiveFormattingElements.Length;

			for (int i = 0; i <= listPtr; i++)
			{
				if (listOfActiveFormattingElements[i] != null)
				{
					listOfActiveFormattingElements[i].Release();
				}
			}
			if (listOfActiveFormattingElements.Length < listLen)
			{
				listOfActiveFormattingElements = new StackNode<T>[listLen];
			}
			listPtr = listLen - 1;

			for (int i = 0; i <= currentPtr; i++)
			{
				stack[i].Release();
			}
			if (stack.Length < stackLen)
			{
				stack = new StackNode<T>[stackLen];
			}
			currentPtr = stackLen - 1;

			for (int i = 0; i < listLen; i++)
			{
				StackNode<T> node = listCopy[i];
				if (node != null)
				{
					StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
							node.name, node.node,
							node.popName,
							node.attributes.CloneAttributes()
						// [NOCPP[
							, node.Locator
						// ]NOCPP]
					);
					listOfActiveFormattingElements[i] = newNode;
				}
				else
				{
					listOfActiveFormattingElements[i] = null;
				}
			}
			for (int i = 0; i < stackLen; i++)
			{
				StackNode<T> node = stackCopy[i];
				int listIndex = FindInArray(node, listCopy);
				if (listIndex == -1)
				{
					StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
							node.name, node.node,
							node.popName,
							null
						// [NOCPP[
							, node.Locator
						// ]NOCPP]       
					);
					stack[i] = newNode;
				}
				else
				{
					stack[i] = listOfActiveFormattingElements[listIndex];
					stack[i].Retain();
				}
			}
			formPointer = snapshot.FormPointer;
			headPointer = snapshot.HeadPointer;
			deepTreeSurrogateParent = snapshot.DeepTreeSurrogateParent;
			mode = snapshot.Mode;
			originalMode = snapshot.OriginalMode;
			framesetOk = snapshot.IsFramesetOk;
			needToDropLF = snapshot.IsNeedToDropLF;
			quirks = snapshot.IsQuirks;
		}

		private int FindInArray(StackNode<T> node, StackNode<T>[] arr)
		{
			for (int i = listPtr; i >= 0; i--)
			{
				if (node == arr[i])
				{
					return i;
				}
			}
			return -1;
		}

		public T FormPointer
		{
			get
			{
				return formPointer;
			}
		}

		public T HeadPointer
		{
			get
			{
				return headPointer;
			}
		}

		public T DeepTreeSurrogateParent
		{
			get
			{
				return deepTreeSurrogateParent;
			}
		}

		/// <summary>
		/// Gets the list of active formatting elements.
		/// </summary>
		public StackNode<T>[] ListOfActiveFormattingElements
		{
			get
			{
				return listOfActiveFormattingElements;
			}
		}

		/// <summary>
		/// Gets the stack.
		/// </summary>
		public StackNode<T>[] Stack
		{
			get
			{
				return stack;
			}
		}

		public InsertionMode Mode
		{
			get
			{
				return mode;
			}
		}

		public InsertionMode OriginalMode
		{
			get
			{
				return originalMode;
			}
		}

		public bool IsFramesetOk
		{
			get
			{
				return framesetOk;
			}
		}

		public bool IsNeedToDropLF
		{
			get
			{
				return needToDropLF;
			}
		}

		public bool IsQuirks
		{
			get
			{
				return quirks;
			}
		}

		#endregion
	}

}
