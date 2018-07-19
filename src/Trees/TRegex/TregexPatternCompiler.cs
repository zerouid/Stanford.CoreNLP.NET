// TregexPatternCompiler
// Copyright (c) 2004-2007 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: parser-user@lists.stanford.edu
//    Licensing: parser-support@lists.stanford.edu
//    http://www-nlp.stanford.edu/software/tregex.shtml
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>
	/// A class for compiling TregexPatterns with specific HeadFinders and or
	/// basicCategoryFunctions.
	/// </summary>
	/// <author>Galen Andrew</author>
	public class TregexPatternCompiler
	{
		internal static readonly IFunction<string, string> DefaultBasicCatFunction = new PennTreebankLanguagePack().GetBasicCategoryFunction();

		internal static readonly IHeadFinder DefaultHeadFinder = new CollinsHeadFinder();

		private readonly IFunction<string, string> basicCatFunction;

		private readonly IHeadFinder headFinder;

		private readonly IList<Pair<string, string>> macros = new List<Pair<string, string>>();

		public static readonly Edu.Stanford.Nlp.Trees.Tregex.TregexPatternCompiler defaultCompiler = new Edu.Stanford.Nlp.Trees.Tregex.TregexPatternCompiler();

		public TregexPatternCompiler()
			: this(DefaultHeadFinder, DefaultBasicCatFunction)
		{
		}

		/// <summary>A compiler that uses this basicCatFunction and the default HeadFinder.</summary>
		/// <param name="basicCatFunction">the function mapping Strings to Strings</param>
		public TregexPatternCompiler(IFunction<string, string> basicCatFunction)
			: this(DefaultHeadFinder, basicCatFunction)
		{
		}

		/// <summary>A compiler that uses this HeadFinder and the default basicCategoryFunction</summary>
		/// <param name="headFinder">the HeadFinder</param>
		public TregexPatternCompiler(IHeadFinder headFinder)
			: this(headFinder, DefaultBasicCatFunction)
		{
		}

		/// <summary>A compiler that uses this HeadFinder and this basicCategoryFunction</summary>
		/// <param name="headFinder">the HeadFinder</param>
		/// <param name="basicCatFunction">The function mapping Strings to Strings</param>
		public TregexPatternCompiler(IHeadFinder headFinder, IFunction<string, string> basicCatFunction)
		{
			this.headFinder = headFinder;
			this.basicCatFunction = basicCatFunction;
		}

		// todo [cdm 2013]: Provide an easy way to do Matcher.quoteReplacement(): This would be quite useful, since the replacement will often contain $ or \
		/// <summary>
		/// Define a macro for rewriting a pattern in any tregex expression compiled
		/// by this compiler.
		/// </summary>
		/// <remarks>
		/// Define a macro for rewriting a pattern in any tregex expression compiled
		/// by this compiler. The semantics of this is that all instances of the
		/// original in the pattern are replaced by the replacement, using exactly
		/// the semantics of String.replaceAll(original, replacement) and the
		/// result will then be compiled by the compiler. As such, note that a
		/// macro can replace any part of a tregex expression, in a syntax
		/// insensitive way.  Here's an example:
		/// <c>tpc.addMacro("FINITE_BE_AUX", "/^(?i:am|is|are|was|were)$/");</c>
		/// </remarks>
		/// <param name="original">
		/// The String to match; becomes the first argument of a
		/// String.replaceAll()
		/// </param>
		/// <param name="replacement">
		/// The replacement String; becomes the second argument
		/// of a String.replaceAll()
		/// </param>
		public virtual void AddMacro(string original, string replacement)
		{
			macros.Add(new Pair<string, string>(original, replacement));
		}

		/// <summary>
		/// Create a TregexPattern from this tregex string using the headFinder and
		/// basicCat function this TregexPatternCompiler was created with.
		/// </summary>
		/// <remarks>
		/// Create a TregexPattern from this tregex string using the headFinder and
		/// basicCat function this TregexPatternCompiler was created with.
		/// <i>Implementation note:</i> If there is an invalid token in the Tregex
		/// parser, JavaCC will throw a TokenMgrError.  This is a class
		/// that extends Error, not Exception (OMG! - bad!), and so rather than
		/// requiring clients to catch it, we wrap it in a ParseException.
		/// (The original Error's are thrown in TregexParserTokenManager.)
		/// </remarks>
		/// <param name="tregex">The pattern to parse</param>
		/// <returns>A new TregexPattern object based on this string</returns>
		/// <exception cref="TregexParseException">If the expression is syntactically invalid</exception>
		public virtual TregexPattern Compile(string tregex)
		{
			foreach (Pair<string, string> macro in macros)
			{
				tregex = tregex.ReplaceAll(macro.First(), macro.Second());
			}
			TregexPattern pattern;
			try
			{
				TregexParser parser = new TregexParser(new StringReader(tregex + '\n'), basicCatFunction, headFinder);
				pattern = parser.Root();
			}
			catch (TokenMgrError tme)
			{
				throw new TregexParseException("Could not parse " + tregex, tme);
			}
			catch (ParseException e)
			{
				throw new TregexParseException("Could not parse " + tregex, e);
			}
			pattern.SetPatternString(tregex);
			return pattern;
		}
	}
}
