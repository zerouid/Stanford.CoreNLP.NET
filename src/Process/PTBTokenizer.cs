using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// A fast, rule-based tokenizer implementation, which produces Penn Treebank
	/// style tokenization of English text.
	/// </summary>
	/// <remarks>
	/// A fast, rule-based tokenizer implementation, which produces Penn Treebank
	/// style tokenization of English text. It was initially written to conform
	/// to Penn Treebank tokenization conventions over ASCII text, but now provides
	/// a range of tokenization options over a broader space of Unicode text.
	/// It reads raw text and outputs
	/// tokens of classes that implement edu.stanford.nlp.trees.HasWord
	/// (typically a Word or a CoreLabel). It can
	/// optionally return end-of-line as a token.
	/// <p>
	/// New code is encouraged to use the
	/// <see cref="PTBTokenizer{T}.PTBTokenizer(Java.IO.Reader, ILexedTokenFactory{T}, string)"/>
	/// constructor. The other constructors are historical.
	/// You specify the type of result tokens with a
	/// LexedTokenFactory, and can specify the treatment of tokens by mainly boolean
	/// options given in a comma separated String options
	/// (e.g., "invertible,normalizeParentheses=true").
	/// If the String is
	/// <see langword="null"/>
	/// or empty, you get the traditional
	/// PTB3 normalization behaviour (i.e., you get ptb3Escaping=true).  If you
	/// want no normalization, then you should pass in the String
	/// "ptb3Escaping=false".  The known option names are:
	/// <ol>
	/// <li>invertible: Store enough information about the original form of the
	/// token and the whitespace around it that a list of tokens can be
	/// faithfully converted back to the original String.  Valid only if the
	/// LexedTokenFactory is an instance of CoreLabelTokenFactory.  The
	/// keys used in it are: TextAnnotation for the tokenized form,
	/// OriginalTextAnnotation for the original string, BeforeAnnotation and
	/// AfterAnnotation for the whitespace before and after a token, and
	/// perhaps CharacterOffsetBeginAnnotation and CharacterOffsetEndAnnotation to record
	/// token begin/after end character offsets, if they were specified to be recorded
	/// in TokenFactory construction.  (Like the String class, begin and end
	/// are done so end - begin gives the token length.) Default is false.
	/// <li>tokenizeNLs: Whether end-of-lines should become tokens (or just
	/// be treated as part of whitespace). Default is false.
	/// <li>tokenizePerLine: Run the tokenizer separately on each line of a file.
	/// This has the following consequences: (i) A token (currently only SGML tokens)
	/// cannot span multiple lines of the original input, and (ii) The tokenizer will not
	/// examine/wait for input from the next line before deciding tokenization decisions on
	/// this line. The latter property affects treating periods by acronyms as end-of-sentence
	/// markers. Having this true is necessary to stop the tokenizer blocking and waiting
	/// for input after a newline is seen when the previous line ends with an abbreviation. </li>
	/// <li>ptb3Escaping: Enable all traditional PTB3 token transforms
	/// (like parentheses becoming -LRB-, -RRB-).  This is a macro flag that
	/// sets or clears all the options below. (Default setting of the various
	/// properties below that this flag controls is equivalent to it being set
	/// to true.)
	/// <li>americanize: Whether to rewrite common British English spellings
	/// as American English spellings. (This is useful if your training
	/// material uses American English spelling, such as the Penn Treebank.)
	/// Default is true.
	/// <li>normalizeSpace: Whether any spaces in tokens (phone numbers, fractions
	/// get turned into U+00A0 (non-breaking space).  It's dangerous to turn
	/// this off for most of our Stanford NLP software, which assumes no
	/// spaces in tokens. Default is true.
	/// <li>normalizeAmpersandEntity: Whether to map the XML
	/// <c>&amp;</c>
	/// to an
	/// ampersand. Default is true.
	/// <li>normalizeCurrency: Whether to do some awful lossy currency mappings
	/// to turn common currency characters into $, #, or "cents", reflecting
	/// the fact that nothing else appears in the old PTB3 WSJ.  (No Euro!)
	/// Default is false. (Note: The default was true through CoreNLP v3.8.0, but we're
	/// gradually inching our way towards the modern world!)
	/// <li>normalizeFractions: Whether to map certain common composed
	/// fraction characters to spelled out letter forms like "1/2".
	/// Default is true.
	/// <li>normalizeParentheses: Whether to map round parentheses to -LRB-,
	/// -RRB-, as in the Penn Treebank. Default is true.
	/// <li>normalizeOtherBrackets: Whether to map other common bracket characters
	/// to -LCB-, -LRB-, -RCB-, -RRB-, roughly as in the Penn Treebank.
	/// Default is true.
	/// <li>asciiQuotes: Whether to map all quote characters to the traditional ' and ".
	/// Default is false.
	/// <li>latexQuotes: Whether to map quotes to ``, `, ', '', as in Latex
	/// and the PTB3 WSJ (though this is now heavily frowned on in Unicode).
	/// If true, this takes precedence over the setting of unicodeQuotes;
	/// if both are false, no mapping is done.  Default is true.
	/// <li>unicodeQuotes: Whether to map quotes to the range U+2018 to U+201D,
	/// the preferred unicode encoding of single and double quotes.
	/// Default is false.
	/// <li>ptb3Ellipsis: Whether to map ellipses to three dots (...), the
	/// old PTB3 WSJ coding of an ellipsis. If true, this takes precedence
	/// over the setting of unicodeEllipsis; if both are false, no mapping
	/// is done. Default is true.
	/// <li>unicodeEllipsis: Whether to map dot and optional space sequences to
	/// U+2026, the Unicode ellipsis character. Default is false.
	/// <li>ptb3Dashes: Whether to turn various dash characters into "--",
	/// the dominant encoding of dashes in the PTB3 WSJ. Default is true.
	/// <li>keepAssimilations: true to tokenize "gonna", false to tokenize
	/// "gon na".  Default is true.
	/// <li>escapeForwardSlashAsterisk: Whether to put a backslash escape in front
	/// of / and * as the old PTB3 WSJ does for some reason (something to do
	/// with Lisp readers??). Default is true.
	/// <li>untokenizable: What to do with untokenizable characters (ones not
	/// known to the tokenizer).  Six options combining whether to log a
	/// warning for none, the first, or all, and whether to delete them or
	/// to include them as single character tokens in the output: noneDelete,
	/// firstDelete, allDelete, noneKeep, firstKeep, allKeep.
	/// The default is "firstDelete".
	/// <li>strictTreebank3: PTBTokenizer deliberately deviates from strict PTB3
	/// WSJ tokenization in two cases.  Setting this improves compatibility
	/// for those cases.  They are: (i) When an acronym is followed by a
	/// sentence end, such as "U.K." at the end of a sentence, the PTB3
	/// has tokens of "Corp" and ".", while by default PTBTokenizer duplicates
	/// the period returning tokens of "Corp." and ".", and (ii) PTBTokenizer
	/// will return numbers with a whole number and a fractional part like
	/// "5 7/8" as a single token, with a non-breaking space in the middle,
	/// while the PTB3 separates them into two tokens "5" and "7/8".
	/// (Exception: for only "U.S." the treebank does have the two tokens
	/// "U.S." and "." like our default; strictTreebank3 now does that too.)
	/// The default is false.
	/// <li>splitHyphenated: whether or not to tokenize segments of hyphenated words
	/// separately ("school" "-" "aged", "frog" "-" "lipped"), keeping the exceptions
	/// in Supplementary Guidelines for ETTB 2.0 by Justin Mott, Colin Warner, Ann Bies,
	/// Ann Taylor and CLEAR guidelines (Bracketing Biomedical Text) by Colin Warner et al. (2012).
	/// Default is false, which maintains old treebank tokenizer behavior.
	/// </ol>
	/// <p>
	/// A single instance of a PTBTokenizer is not thread safe, as it uses
	/// a non-threadsafe JFlex object to do the processing.  Multiple
	/// instances can be created safely, though.  A single instance of a
	/// PTBTokenizerFactory is also not thread safe, as it keeps its
	/// options in a local variable.
	/// </p>
	/// </remarks>
	/// <author>
	/// Tim Grow (his tokenizer is a Java implementation of Professor
	/// Chris Manning's Flex tokenizer, pgtt-treebank.l)
	/// </author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Jenny Finkel (integrating in invertible PTB tokenizer)</author>
	/// <author>Christopher Manning (redid API, added many options, maintenance)</author>
	public class PTBTokenizer<T> : AbstractTokenizer<T>
		where T : IHasWord
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.PTBTokenizer));

		private readonly PTBLexer lexer;

		// Stanford English Tokenizer -- a deterministic, fast high-quality tokenizer
		// Copyright (c) 2002-2016 The Board of Trustees of
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
		// For more information, bug reports, fixes, contact:
		//    Christopher Manning
		//    Dept of Computer Science, Gates 1A
		//    Stanford CA 94305-9010
		//    USA
		//    java-nlp-support@lists.stanford.edu
		//    http://nlp.stanford.edu/software/
		// the underlying lexer
		/// <summary>
		/// Constructs a new PTBTokenizer that returns Word tokens and which treats
		/// carriage returns as normal whitespace.
		/// </summary>
		/// <param name="r">The Reader whose contents will be tokenized</param>
		/// <returns>
		/// A PTBTokenizer that tokenizes a stream to objects of type
		/// <see cref="Edu.Stanford.Nlp.Ling.Word"/>
		/// </returns>
		public static Edu.Stanford.Nlp.Process.PTBTokenizer<Word> NewPTBTokenizer(Reader r)
		{
			return new Edu.Stanford.Nlp.Process.PTBTokenizer<Word>(r, new WordTokenFactory(), string.Empty);
		}

		/// <summary>Constructs a new PTBTokenizer that makes CoreLabel tokens.</summary>
		/// <remarks>
		/// Constructs a new PTBTokenizer that makes CoreLabel tokens.
		/// It optionally returns carriage returns
		/// as their own token. CRs come back as Words whose text is
		/// the value of
		/// <c>AbstractTokenizer.NEWLINE_TOKEN</c>
		/// .
		/// </remarks>
		/// <param name="r">The Reader to read tokens from</param>
		/// <param name="tokenizeNLs">
		/// Whether to return newlines as separate tokens
		/// (otherwise they normally disappear as whitespace)
		/// </param>
		/// <param name="invertible">
		/// if set to true, then will produce CoreLabels which
		/// will have fields for the string before and after, and the
		/// character offsets
		/// </param>
		/// <returns>A PTBTokenizer which returns CoreLabel objects</returns>
		public static Edu.Stanford.Nlp.Process.PTBTokenizer<CoreLabel> NewPTBTokenizer(Reader r, bool tokenizeNLs, bool invertible)
		{
			return new Edu.Stanford.Nlp.Process.PTBTokenizer<CoreLabel>(r, tokenizeNLs, invertible, false, new CoreLabelTokenFactory());
		}

		/// <summary>
		/// Constructs a new PTBTokenizer that optionally returns carriage returns
		/// as their own token, and has a custom LexedTokenFactory.
		/// </summary>
		/// <remarks>
		/// Constructs a new PTBTokenizer that optionally returns carriage returns
		/// as their own token, and has a custom LexedTokenFactory.
		/// If asked for, CRs come back as Words whose text is
		/// the value of
		/// <c>PTBLexer.cr</c>
		/// .  This constructor translates
		/// between the traditional boolean options of PTBTokenizer and the new
		/// options String.
		/// </remarks>
		/// <param name="r">The Reader to read tokens from</param>
		/// <param name="tokenizeNLs">
		/// Whether to return newlines as separate tokens
		/// (otherwise they normally disappear as whitespace)
		/// </param>
		/// <param name="invertible">
		/// if set to true, then will produce CoreLabels which
		/// will have fields for the string before and after, and the
		/// character offsets
		/// </param>
		/// <param name="suppressEscaping">
		/// If true, all the traditional Penn Treebank
		/// normalizations are turned off.  Otherwise, they all happen.
		/// </param>
		/// <param name="tokenFactory">
		/// The LexedTokenFactory to use to create
		/// tokens from the text.
		/// </param>
		private PTBTokenizer(Reader r, bool tokenizeNLs, bool invertible, bool suppressEscaping, ILexedTokenFactory<T> tokenFactory)
		{
			StringBuilder options = new StringBuilder();
			if (suppressEscaping)
			{
				options.Append("ptb3Escaping=false");
			}
			else
			{
				options.Append("ptb3Escaping=true");
			}
			// i.e., turn on all the historical PTB normalizations
			if (tokenizeNLs)
			{
				options.Append(",tokenizeNLs");
			}
			if (invertible)
			{
				options.Append(",invertible");
			}
			lexer = new PTBLexer(r, tokenFactory, options.ToString());
		}

		/// <summary>Constructs a new PTBTokenizer with a custom LexedTokenFactory.</summary>
		/// <remarks>
		/// Constructs a new PTBTokenizer with a custom LexedTokenFactory.
		/// Many options for tokenization and what is returned can be set via
		/// the options String. See the class documentation for details on
		/// the options String.  This is the new recommended constructor!
		/// </remarks>
		/// <param name="r">The Reader to read tokens from.</param>
		/// <param name="tokenFactory">
		/// The LexedTokenFactory to use to create
		/// tokens from the text.
		/// </param>
		/// <param name="options">
		/// Options to the lexer.  See the extensive documentation
		/// in the class javadoc.  The String may be null or empty,
		/// which means that all traditional PTB normalizations are
		/// done.  You can pass in "ptb3Escaping=false" and have no
		/// normalizations done (that is, the behavior of the old
		/// suppressEscaping=true option).
		/// </param>
		public PTBTokenizer(Reader r, ILexedTokenFactory<T> tokenFactory, string options)
		{
			lexer = new PTBLexer(r, tokenFactory, options);
		}

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal override T GetNext()
		{
			// if (lexer == null) {
			//   return null;
			// }
			try
			{
				return (T)lexer.Next();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		// cdm 2007: this shouldn't be necessary: PTBLexer decides for itself whether to return CRs based on the same flag!
		// get rid of CRs if necessary
		// while (!tokenizeNLs && PTBLexer.cr.equals(((HasWord) token).word())) {
		//   token = (T)lexer.next();
		// }
		// horatio: we used to catch exceptions here, which led to broken
		// behavior and made it very difficult to debug whatever the
		// problem was.
		/// <summary>
		/// Returns the string literal inserted for newlines when the -tokenizeNLs
		/// options is set.
		/// </summary>
		/// <returns>string literal inserted for "\n".</returns>
		public static string GetNewlineToken()
		{
			return NewlineToken;
		}

		/// <summary>Returns a presentable version of the given PTB-tokenized text.</summary>
		/// <remarks>
		/// Returns a presentable version of the given PTB-tokenized text.
		/// PTB tokenization splits up punctuation and does various other things
		/// that makes simply joining the tokens with spaces look bad. So join
		/// the tokens with space and run it through this method to produce nice
		/// looking text. It's not perfect, but it works pretty well.
		/// <p>
		/// <b>Note:</b> If your tokens have maintained the OriginalTextAnnotation and
		/// the BeforeAnnotation and the AfterAnnotation, then rather than doing
		/// this you can actually precisely reconstruct the text they were made
		/// from!
		/// </remarks>
		/// <param name="ptbText">A String in PTB3-escaped form</param>
		/// <returns>An approximation to the original String</returns>
		public static string Ptb2Text(string ptbText)
		{
			StringBuilder sb = new StringBuilder(ptbText.Length);
			// probably an overestimate
			PTB2TextLexer lexer = new PTB2TextLexer(new StringReader(ptbText));
			try
			{
				for (string token; (token = lexer.Next()) != null; )
				{
					sb.Append(token);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return sb.ToString();
		}

		/// <summary>Returns a presentable version of a given PTB token.</summary>
		/// <remarks>
		/// Returns a presentable version of a given PTB token. For instance,
		/// it transforms -LRB- into (.
		/// </remarks>
		public static string PtbToken2Text(string ptbText)
		{
			return Ptb2Text(' ' + ptbText + ' ').Trim();
		}

		/// <summary>Writes a presentable version of the given PTB-tokenized text.</summary>
		/// <remarks>
		/// Writes a presentable version of the given PTB-tokenized text.
		/// PTB tokenization splits up punctuation and does various other things
		/// that makes simply joining the tokens with spaces look bad. So join
		/// the tokens with space and run it through this method to produce nice
		/// looking text. It's not perfect, but it works pretty well.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static int Ptb2Text(Reader ptbText, TextWriter w)
		{
			int numTokens = 0;
			PTB2TextLexer lexer = new PTB2TextLexer(ptbText);
			for (string token; (token = lexer.Next()) != null; )
			{
				numTokens++;
				w.Write(token);
			}
			return numTokens;
		}

		/// <exception cref="System.IO.IOException"/>
		private static void Untok(IList<string> inputFileList, IList<string> outputFileList, string charset)
		{
			long start = Runtime.NanoTime();
			int numTokens = 0;
			int sz = inputFileList.Count;
			if (sz == 0)
			{
				Reader r = new InputStreamReader(Runtime.@in, charset);
				BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(System.Console.Out, charset));
				numTokens = Ptb2Text(r, writer);
				writer.Close();
			}
			else
			{
				for (int j = 0; j < sz; j++)
				{
					using (Reader r = IOUtils.ReaderFromString(inputFileList[j], charset))
					{
						BufferedWriter writer;
						if (outputFileList == null)
						{
							writer = new BufferedWriter(new OutputStreamWriter(System.Console.Out, charset));
						}
						else
						{
							writer = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outputFileList[j]), charset));
						}
						try
						{
							numTokens += Ptb2Text(r, writer);
						}
						finally
						{
							writer.Close();
						}
					}
				}
			}
			long duration = Runtime.NanoTime() - start;
			double wordsPerSec = (double)numTokens / ((double)duration / 1000000000.0);
			System.Console.Error.Printf("PTBTokenizer untokenized %d tokens at %.2f tokens per second.%n", numTokens, wordsPerSec);
		}

		/// <summary>Returns a presentable version of the given PTB-tokenized words.</summary>
		/// <remarks>
		/// Returns a presentable version of the given PTB-tokenized words.
		/// Pass in a List of Strings and this method will
		/// join the words with spaces and call
		/// <see cref="PTBTokenizer{T}.Ptb2Text(string)"/>
		/// on the
		/// output.
		/// </remarks>
		/// <param name="ptbWords">A list of String</param>
		/// <returns>A presentable version of the given PTB-tokenized words</returns>
		public static string Ptb2Text(IList<string> ptbWords)
		{
			return Ptb2Text(StringUtils.Join(ptbWords));
		}

		/// <summary>Returns a presentable version of the given PTB-tokenized words.</summary>
		/// <remarks>
		/// Returns a presentable version of the given PTB-tokenized words.
		/// Pass in a List of Words or a Document and this method will
		/// take the word() values (to prevent additional text from creeping in, e.g., POS tags),
		/// and call
		/// <see cref="PTBTokenizer{T}.Ptb2Text(string)"/>
		/// on the output.
		/// </remarks>
		/// <param name="ptbWords">A list of HasWord objects</param>
		/// <returns>A presentable version of the given PTB-tokenized words</returns>
		public static string LabelList2Text<_T0>(IList<_T0> ptbWords)
			where _T0 : IHasWord
		{
			IList<string> words = new List<string>();
			foreach (IHasWord hw in ptbWords)
			{
				words.Add(hw.Word());
			}
			return Ptb2Text(words);
		}

		/// <exception cref="System.IO.IOException"/>
		private static void Tok(IList<string> inputFileList, IList<string> outputFileList, string charset, Pattern parseInsidePattern, Pattern filterPattern, string options, bool preserveLines, bool oneLinePerElement, bool dump, bool lowerCase)
		{
			long start = Runtime.NanoTime();
			long numTokens = 0;
			int numFiles = inputFileList.Count;
			if (numFiles == 0)
			{
				Reader stdin = IOUtils.ReaderFromStdin(charset);
				BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(System.Console.Out, charset));
				numTokens += TokReader(stdin, writer, parseInsidePattern, filterPattern, options, preserveLines, oneLinePerElement, dump, lowerCase);
				IOUtils.CloseIgnoringExceptions(writer);
			}
			else
			{
				BufferedWriter @out = null;
				if (outputFileList == null)
				{
					@out = new BufferedWriter(new OutputStreamWriter(System.Console.Out, charset));
				}
				for (int j = 0; j < numFiles; j++)
				{
					using (Reader r = IOUtils.ReaderFromString(inputFileList[j], charset))
					{
						if (outputFileList != null)
						{
							@out = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outputFileList[j]), charset));
						}
						numTokens += TokReader(r, @out, parseInsidePattern, filterPattern, options, preserveLines, oneLinePerElement, dump, lowerCase);
					}
					if (outputFileList != null)
					{
						IOUtils.CloseIgnoringExceptions(@out);
					}
				}
				// end for j going through inputFileList
				if (outputFileList == null)
				{
					IOUtils.CloseIgnoringExceptions(@out);
				}
			}
			long duration = Runtime.NanoTime() - start;
			double wordsPerSec = (double)numTokens / ((double)duration / 1000000000.0);
			System.Console.Error.Printf("PTBTokenizer tokenized %d tokens at %.2f tokens per second.%n", numTokens, wordsPerSec);
		}

		/// <exception cref="System.IO.IOException"/>
		private static int TokReader(Reader r, BufferedWriter writer, Pattern parseInsidePattern, Pattern filterPattern, string options, bool preserveLines, bool oneLinePerElement, bool dump, bool lowerCase)
		{
			int numTokens = 0;
			bool beginLine = true;
			bool printing = (parseInsidePattern == null);
			// start off printing, unless you're looking for a start entity
			Matcher m = null;
			if (parseInsidePattern != null)
			{
				m = parseInsidePattern.Matcher(string.Empty);
			}
			// create once as performance hack
			// System.err.printf("parseInsidePattern is: |%s|%n", parseInsidePattern);
			for (Edu.Stanford.Nlp.Process.PTBTokenizer<CoreLabel> tokenizer = new Edu.Stanford.Nlp.Process.PTBTokenizer<CoreLabel>(r, new CoreLabelTokenFactory(), options); tokenizer.MoveNext(); )
			{
				CoreLabel obj = tokenizer.Current;
				// String origStr = obj.get(CoreAnnotations.TextAnnotation.class).replaceFirst("\n+$", ""); // DanC added this to fix a lexer bug, hopefully now corrected
				string origStr = obj.Get(typeof(CoreAnnotations.TextAnnotation));
				string str;
				if (lowerCase)
				{
					str = origStr.ToLower(Locale.English);
					obj.Set(typeof(CoreAnnotations.TextAnnotation), str);
				}
				else
				{
					str = origStr;
				}
				if (m != null && m.Reset(origStr).Matches())
				{
					printing = m.Group(1).IsEmpty();
					// turn on printing if no end element slash, turn it off it there is
					// System.err.printf("parseInsidePattern matched against: |%s|, printing is %b.%n", origStr, printing);
					if (!printing)
					{
						// true only if matched a stop
						beginLine = true;
						if (oneLinePerElement)
						{
							writer.NewLine();
						}
					}
				}
				else
				{
					if (printing)
					{
						if (dump)
						{
							// after having checked for tags, change str to be exhaustive
							str = obj.ToShorterString();
						}
						if (filterPattern != null && filterPattern.Matcher(origStr).Matches())
						{
						}
						else
						{
							// skip
							if (preserveLines)
							{
								if (NewlineToken.Equals(origStr))
								{
									beginLine = true;
									writer.NewLine();
								}
								else
								{
									if (!beginLine)
									{
										writer.Write(' ');
									}
									else
									{
										beginLine = false;
									}
									// writer.write(str.replace("\n", ""));
									writer.Write(str);
								}
							}
							else
							{
								if (oneLinePerElement)
								{
									if (!beginLine)
									{
										writer.Write(' ');
									}
									else
									{
										beginLine = false;
									}
									writer.Write(str);
								}
								else
								{
									writer.Write(str);
									writer.NewLine();
								}
							}
						}
					}
				}
				numTokens++;
			}
			return numTokens;
		}

		/// <returns>A PTBTokenizerFactory that vends Word tokens.</returns>
		public static ITokenizerFactory<Word> Factory()
		{
			return PTBTokenizer.PTBTokenizerFactory.NewTokenizerFactory();
		}

		/// <returns>A PTBTokenizerFactory that vends CoreLabel tokens.</returns>
		public static ITokenizerFactory<CoreLabel> Factory(bool tokenizeNLs, bool invertible)
		{
			return PTBTokenizer.PTBTokenizerFactory.NewPTBTokenizerFactory(tokenizeNLs, invertible);
		}

		/// <returns>A PTBTokenizerFactory that vends CoreLabel tokens with default tokenization.</returns>
		public static ITokenizerFactory<CoreLabel> CoreLabelFactory()
		{
			return CoreLabelFactory(string.Empty);
		}

		/// <returns>A PTBTokenizerFactory that vends CoreLabel tokens with default tokenization.</returns>
		public static ITokenizerFactory<CoreLabel> CoreLabelFactory(string options)
		{
			return PTBTokenizer.PTBTokenizerFactory.NewPTBTokenizerFactory(new CoreLabelTokenFactory(), options);
		}

		/// <summary>Get a TokenizerFactory that does Penn Treebank tokenization.</summary>
		/// <remarks>
		/// Get a TokenizerFactory that does Penn Treebank tokenization.
		/// This is now the recommended factory method to use.
		/// </remarks>
		/// <param name="factory">A TokenFactory that determines what form of token is returned by the Tokenizer</param>
		/// <param name="options">A String specifying options (see the class javadoc for details)</param>
		/// <?/>
		/// <returns>A TokenizerFactory that does Penn Treebank tokenization</returns>
		public static ITokenizerFactory<T> Factory<T>(ILexedTokenFactory<T> factory, string options)
			where T : IHasWord
		{
			return new PTBTokenizer.PTBTokenizerFactory<T>(factory, options);
		}

		/// <summary>
		/// This class provides a factory which will vend instances of PTBTokenizer
		/// which wrap a provided Reader.
		/// </summary>
		/// <remarks>
		/// This class provides a factory which will vend instances of PTBTokenizer
		/// which wrap a provided Reader.  See the documentation for
		/// <see cref="PTBTokenizer{T}"/>
		/// for details of the parameters and options.
		/// </remarks>
		/// <seealso cref="PTBTokenizer{T}"/>
		/// <?/>
		[System.Serializable]
		public class PTBTokenizerFactory<T> : ITokenizerFactory<T>
			where T : IHasWord
		{
			private const long serialVersionUID = -8859638719818931606L;

			protected internal readonly ILexedTokenFactory<T> factory;

			protected internal string options;

			/// <summary>
			/// Constructs a new TokenizerFactory that returns Word objects and
			/// treats carriage returns as normal whitespace.
			/// </summary>
			/// <remarks>
			/// Constructs a new TokenizerFactory that returns Word objects and
			/// treats carriage returns as normal whitespace.
			/// THIS METHOD IS INVOKED BY REFLECTION BY SOME OF THE JAVANLP
			/// CODE TO LOAD A TOKENIZER FACTORY.  IT SHOULD BE PRESENT IN A
			/// TokenizerFactory.
			/// </remarks>
			/// <returns>A TokenizerFactory that returns Word objects</returns>
			public static ITokenizerFactory<Word> NewTokenizerFactory()
			{
				return NewPTBTokenizerFactory(new WordTokenFactory(), string.Empty);
			}

			/// <summary>
			/// Constructs a new PTBTokenizer that returns Word objects and
			/// uses the options passed in.
			/// </summary>
			/// <remarks>
			/// Constructs a new PTBTokenizer that returns Word objects and
			/// uses the options passed in.
			/// THIS METHOD IS INVOKED BY REFLECTION BY SOME OF THE JAVANLP
			/// CODE TO LOAD A TOKENIZER FACTORY.  IT SHOULD BE PRESENT IN A
			/// TokenizerFactory.
			/// </remarks>
			/// <param name="options">A String of options</param>
			/// <returns>A TokenizerFactory that returns Word objects</returns>
			public static PTBTokenizer.PTBTokenizerFactory<Word> NewWordTokenizerFactory(string options)
			{
				return new PTBTokenizer.PTBTokenizerFactory<Word>(new WordTokenFactory(), options);
			}

			/// <summary>
			/// Constructs a new PTBTokenizer that returns CoreLabel objects and
			/// uses the options passed in.
			/// </summary>
			/// <param name="options">
			/// A String of options. For the default, recommended
			/// options for PTB-style tokenization compatibility, pass
			/// in an empty String.
			/// </param>
			/// <returns>A TokenizerFactory that returns CoreLabel objects o</returns>
			public static PTBTokenizer.PTBTokenizerFactory<CoreLabel> NewCoreLabelTokenizerFactory(string options)
			{
				return new PTBTokenizer.PTBTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory(), options);
			}

			/// <summary>
			/// Constructs a new PTBTokenizer that uses the LexedTokenFactory and
			/// options passed in.
			/// </summary>
			/// <param name="tokenFactory">The LexedTokenFactory</param>
			/// <param name="options">A String of options</param>
			/// <returns>
			/// A TokenizerFactory that returns objects of the type of the
			/// LexedTokenFactory
			/// </returns>
			public static PTBTokenizer.PTBTokenizerFactory<T> NewPTBTokenizerFactory<T>(ILexedTokenFactory<T> tokenFactory, string options)
				where T : IHasWord
			{
				return new PTBTokenizer.PTBTokenizerFactory<T>(tokenFactory, options);
			}

			public static PTBTokenizer.PTBTokenizerFactory<CoreLabel> NewPTBTokenizerFactory(bool tokenizeNLs, bool invertible)
			{
				return new PTBTokenizer.PTBTokenizerFactory<CoreLabel>(tokenizeNLs, invertible, false, new CoreLabelTokenFactory());
			}

			private PTBTokenizerFactory(bool tokenizeNLs, bool invertible, bool suppressEscaping, ILexedTokenFactory<T> factory)
			{
				// Constructors
				// This one is historical
				this.factory = factory;
				StringBuilder optionsSB = new StringBuilder();
				if (suppressEscaping)
				{
					optionsSB.Append("ptb3Escaping=false");
				}
				else
				{
					optionsSB.Append("ptb3Escaping=true");
				}
				// i.e., turn on all the historical PTB normalizations
				if (tokenizeNLs)
				{
					optionsSB.Append(",tokenizeNLs");
				}
				if (invertible)
				{
					optionsSB.Append(",invertible");
				}
				this.options = optionsSB.ToString();
			}

			/// <summary>Make a factory for PTBTokenizers.</summary>
			/// <param name="tokenFactory">A factory for the token type that the tokenizer will return</param>
			/// <param name="options">Options to the tokenizer (see the class documentation for details)</param>
			private PTBTokenizerFactory(ILexedTokenFactory<T> tokenFactory, string options)
			{
				this.factory = tokenFactory;
				this.options = options;
			}

			/// <summary>Returns a tokenizer wrapping the given Reader.</summary>
			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			/// <summary>Returns a tokenizer wrapping the given Reader.</summary>
			public virtual ITokenizer<T> GetTokenizer(Reader r)
			{
				return new PTBTokenizer<T>(r, factory, options);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r, string extraOptions)
			{
				if (options == null || options.IsEmpty())
				{
					return new PTBTokenizer<T>(r, factory, extraOptions);
				}
				else
				{
					return new PTBTokenizer<T>(r, factory, options + ',' + extraOptions);
				}
			}

			public virtual void SetOptions(string options)
			{
				this.options = options;
			}
		}

		// end static class PTBTokenizerFactory
		/// <summary>Command-line option specification.</summary>
		private static IDictionary<string, int> OptionArgDefs()
		{
			IDictionary<string, int> optionArgDefs = Generics.NewHashMap();
			optionArgDefs["options"] = 1;
			optionArgDefs["ioFileList"] = 0;
			optionArgDefs["fileList"] = 0;
			optionArgDefs["lowerCase"] = 0;
			optionArgDefs["dump"] = 0;
			optionArgDefs["untok"] = 0;
			optionArgDefs["encoding"] = 1;
			optionArgDefs["parseInside"] = 1;
			optionArgDefs["filter"] = 1;
			optionArgDefs["preserveLines"] = 0;
			optionArgDefs["oneLinePerElement"] = 0;
			return optionArgDefs;
		}

		/// <summary>
		/// Reads files given as arguments and print their tokens, by default as
		/// one per line.
		/// </summary>
		/// <remarks>
		/// Reads files given as arguments and print their tokens, by default as
		/// one per line.  This is useful either for testing or to run
		/// standalone to turn a corpus into a one-token-per-line file of tokens.
		/// This main method assumes that the input file is in utf-8 encoding,
		/// unless an encoding is specified.
		/// <p>
		/// Usage:
		/// <c>java edu.stanford.nlp.process.PTBTokenizer [options] filename+</c>
		/// <p>
		/// Options:
		/// <ul>
		/// <li> -options options Set various tokenization options
		/// (see the documentation in the class javadoc).
		/// <li> -preserveLines Produce space-separated tokens, except
		/// when the original had a line break, not one-token-per-line.
		/// <li> -oneLinePerElement Print the tokens of an element space-separated on one line.
		/// An "element" is either a file or one of the elements matched by the
		/// parseInside regex. </li>
		/// <li> -filter regex Delete any token that matches() (in its entirety) the given regex. </li>
		/// <li> -encoding encoding Specifies a character encoding. If you do not
		/// specify one, the default is utf-8 (not the platform default).
		/// <li> -lowerCase Lowercase all tokens (on tokenization).
		/// <li> -parseInside regex Names an XML-style element or a regular expression
		/// over such elements.  The tokenizer will only tokenize inside elements
		/// that match this regex.  (This is done by regex matching, not an XML
		/// parser, but works well for simple XML documents, or other SGML-style
		/// documents, such as Linguistic Data Consortium releases, which adopt
		/// the convention that a line of a file is either XML markup or
		/// character data but never both.)
		/// <li> -ioFileList file* The remaining command-line arguments are treated as
		/// filenames that themselves contain lists of pairs of input-output
		/// filenames (2 column, whitespace separated). Alternatively, if there is only
		/// one filename per line, the output filename is the input filename with ".tok" appended.
		/// <li> -fileList file* The remaining command-line arguments are treated as
		/// filenames that contain filenames, one per line. The output of tokenization is sent to
		/// stdout.
		/// <li> -dump Print the whole of each CoreLabel, not just the value (word).
		/// <li> -untok Heuristically untokenize tokenized text.
		/// <li> -h, -help Print usage info.
		/// </ul>
		/// <p>
		/// A note on
		/// <c>-preserveLines</c>
		/// : Basically, if you use this option, your output file should have
		/// the same number of lines as your input file. If not, there is a bug. But the truth of this statement
		/// depends on how you count lines…. Unicode includes "line separator" and "paragraph separator" characters
		/// and Unicode says that you should accept them. See e.g., http://unicode.org/standard/reports/tr13/tr13-5.html
		/// <p>
		/// However, Unix, Linux utilities, etc. don't recognize them and count only the traditional \n|\r|\r\n.
		/// And PTBTokenizer does normalize line separation. Hence, if your input text contains, say U+2028 Line Separator
		/// characters, the Unix wc utility will report more lines after tokenization than before,
		/// even though line breaks have been preserved, according to Unicode. It may be useful to compare results with the
		/// Perl uniwc script from https://raw.githubusercontent.com/briandfoy/Unicode-Tussle/master/script/uniwc
		/// <p>
		/// If it reports the same number of input and output lines, then this difference is your problem,
		/// and in a certain Unicode sense, our tokenizer did indeed preserve the line count.
		/// If not, please send us a bug report. At present there is no way to disable this process of Unicode separator
		/// characters. If you don't want this anomaly, you'll need to either delete these two characters or to map them
		/// to conventional Unix newline characters. Or to some other weirdo character.
		/// </remarks>
		/// <param name="args">Command line arguments</param>
		/// <exception cref="System.IO.IOException">If any file I/O problem</exception>
		public static void Main(string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args, OptionArgDefs());
			bool showHelp = PropertiesUtils.GetBool(options, "help", false);
			showHelp = PropertiesUtils.GetBool(options, "h", showHelp);
			if (showHelp)
			{
				log.Info("Usage: java edu.stanford.nlp.process.PTBTokenizer [options]* filename*");
				log.Info("  options: -h|-help|-options tokenizerOptions|-encoding encoding|-dump|");
				log.Info("           -lowerCase|-preserveLines|-oneLinePerElement|-filter regex|");
				log.Info("           -parseInside regex|-fileList|-ioFileList|-untok");
				return;
			}
			StringBuilder optionsSB = new StringBuilder();
			string tokenizerOptions = options.GetProperty("options", null);
			if (tokenizerOptions != null)
			{
				optionsSB.Append(tokenizerOptions);
			}
			bool preserveLines = PropertiesUtils.GetBool(options, "preserveLines", false);
			if (preserveLines)
			{
				optionsSB.Append(",tokenizeNLs");
			}
			bool oneLinePerElement = PropertiesUtils.GetBool(options, "oneLinePerElement", false);
			bool inputOutputFileList = PropertiesUtils.GetBool(options, "ioFileList", false);
			bool fileList = PropertiesUtils.GetBool(options, "fileList", false);
			bool lowerCase = PropertiesUtils.GetBool(options, "lowerCase", false);
			bool dump = PropertiesUtils.GetBool(options, "dump", false);
			bool untok = PropertiesUtils.GetBool(options, "untok", false);
			string charset = options.GetProperty("encoding", "utf-8");
			string parseInsideValue = options.GetProperty("parseInside", null);
			Pattern parseInsidePattern = null;
			if (parseInsideValue != null)
			{
				try
				{
					// We still allow space, but PTBTokenizer will change space to &nbsp; so need to also match it
					parseInsidePattern = Pattern.Compile("<(/?)(?:" + parseInsideValue + ")(?:(?:\\s|\u00A0)[^>]*?)?>");
				}
				catch (PatternSyntaxException)
				{
				}
			}
			// just go with null parseInsidePattern
			string filterValue = options.GetProperty("filter", null);
			Pattern filterPattern = null;
			if (filterValue != null)
			{
				try
				{
					filterPattern = Pattern.Compile(filterValue);
				}
				catch (PatternSyntaxException)
				{
				}
			}
			// just go with null filterPattern
			// Other arguments are filenames
			string parsedArgStr = options.GetProperty(string.Empty, null);
			string[] parsedArgs = (parsedArgStr == null) ? null : parsedArgStr.Split("\\s+");
			List<string> inputFileList = new List<string>();
			List<string> outputFileList = null;
			if (parsedArgs != null)
			{
				if (fileList || inputOutputFileList)
				{
					outputFileList = new List<string>();
					foreach (string fileName in parsedArgs)
					{
						BufferedReader r = IOUtils.ReaderFromString(fileName, charset);
						for (string inLine; (inLine = r.ReadLine()) != null; )
						{
							string[] fields = inLine.Split("\\s+");
							inputFileList.Add(fields[0]);
							if (fields.Length > 1)
							{
								outputFileList.Add(fields[1]);
							}
							else
							{
								outputFileList.Add(fields[0] + ".tok");
							}
						}
						r.Close();
					}
					if (fileList)
					{
						// We're not actually going to use the outputFileList!
						outputFileList = null;
					}
				}
				else
				{
					// Concatenate input files into a single output file
					Sharpen.Collections.AddAll(inputFileList, Arrays.AsList(parsedArgs));
				}
			}
			if (untok)
			{
				Untok(inputFileList, outputFileList, charset);
			}
			else
			{
				Tok(inputFileList, outputFileList, charset, parseInsidePattern, filterPattern, optionsSB.ToString(), preserveLines, oneLinePerElement, dump, lowerCase);
			}
		}
		// end main
	}
}
