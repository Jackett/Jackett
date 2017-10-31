/*
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

#pragma warning disable 1591

namespace HtmlParserSharp.Common
{
	// This file contains the attributes that correspond to the annotations
	// @NsUri, @Prefix and @Local in the Java code. Probably we can safely remove these.

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method)]
	public class NsUriAttribute : Attribute	{ }

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method)]
	public class PrefixAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method)]
	public class LocalAttribute : Attribute { }
}
