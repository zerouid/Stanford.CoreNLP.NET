using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An annotator which picks quotations out of the given text.</summary>
	/// <remarks>
	/// An annotator which picks quotations out of the given text. Allows
	/// for embedded quotations so long as they are either directed unicode quotes or are
	/// of a different type of quote than the outer quotations
	/// (e.g. "'Gadzooks' is what he said to me" is legal whereas
	/// "They called me "Danger" when I was..." is illegal).
	/// Uses regular-expression-like rules to find quotes and does not
	/// depend on the tokenizer, which allows quotes like ''Tis true!' to be
	/// correctly identified.
	/// <p>
	/// Considers regular ascii ("", '', ``'', and `') as well as "smart" and
	/// international quotation marks as follows:
	/// “”,‘’, «», ‹›, 「」, 『』, „”, and ‚’.
	/// <p>
	/// Note: extracts everything within these pairs as a whole quote segment, which may or may
	/// not be the desired behaviour for texts that use different formatting styles than
	/// standard english ones.
	/// <p>
	/// There are a number of options that can be passed to the quote annotator to
	/// customize its' behaviour:
	/// <ul>
	/// <li>singleQuotes: "true" or "false", indicating whether or not to consider ' tokens
	/// to be quotation marks (default=false).</li>
	/// <li>maxLength: maximum character length of quotes to consider (default=-1).</li>
	/// <li>asciiQuotes: "true" or "false", indicating whether or not to convert all quotes
	/// to ascii quotes before processing (can help when there are errors in quote directionality)
	/// (default=false).</li>
	/// <li>allowEmbeddedSame: "true" or "false" indicating whether or not to allow smart/directed
	/// (everything except " and ') quotes of the same kind to be embedded within one another
	/// (default=false).</li>
	/// <li>extractUnclosedQuotes: "true" or "false" indicating whether or not to extract unclosed
	/// quotes. If "true", an UnclosedQuotationsAnnotation that is structured exactly the same as
	/// the QuotationsAnnotation will be added to the document. Any nested unclosed quotations will be
	/// contained in nested UnclosedQuotationsAnnotation on the target unclosed quotation
	/// (default=false).</li>
	/// </ul>
	/// The annotator adds a QuotationsAnnotation to the Annotation
	/// which returns a List<CoreMap> that
	/// contain the following information:
	/// <ul>
	/// <li>CharacterOffsetBeginAnnotation</li>
	/// <li>CharacterOffsetEndAnnotation</li>
	/// <li>QuotationIndexAnnotation</li>
	/// <li>QuotationsAnnotation (if there are embedded quotes)</li>
	/// <li>TokensAnnotation (if the tokenizer is run before the quote annotator)</li>
	/// <li>TokenBeginAnnotation (if the tokenizer is run before the quote annotator)</li>
	/// <li>TokenEndAnnotation (if the tokenizer is run before the quote annotator)</li>
	/// <li>SentenceBeginAnnotation (if the sentence splitter has bee run before the quote annotator)</li>
	/// <li>SentenceEndAnnotation (if the sentence splitter has bee run before the quote annotator)</li>
	/// </ul>
	/// </remarks>
	/// <author>Grace Muzny</author>
	public class QuoteAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.QuoteAnnotator));

		private readonly bool Verbose;

		private readonly bool Debug = false;

		public bool UseSingle = false;

		public int MaxLength = -1;

		public bool AsciiQuotes = false;

		public bool AllowEmbeddedSame = false;

		public bool SmartQuotes = false;

		public bool ExtractUnclosed = false;

		public bool AttributeQuotes = true;

		public QuoteAttributionAnnotator quoteAttributionAnnotator;

		public static readonly IDictionary<string, string> DirectedQuotes;

		static QuoteAnnotator()
		{
			// whether or not to consider single single quotes as quote-marking
			// max length to consider for quotes
			// whether to convert unicode quotes to non-unicode " and '
			// before processing
			// Whether or not to allow quotes of the same type embedded inside of each other
			// Whether or not to allow quotes of the same type embedded inside of each other
			// Whether or not to extract unclosed quotes
			// Whether or not to perform quote attribution
			// A quote attribution annotator this annotator may use
			//TODO: add directed quote/unicode quote understanding capabilities.
			// will need substantial logic, probably, as quotation mark conventions
			// vary widely.
			IDictionary<string, string> tmp = Generics.NewHashMap();
			tmp["“"] = "”";
			// directed double inward
			tmp["‘"] = "’";
			// directed single inward
			tmp["«"] = "»";
			// guillemets
			tmp["‹"] = "›";
			// single guillemets
			tmp["「"] = "」";
			// cjk brackets
			tmp["『"] = "』";
			// cjk brackets
			tmp["„"] = "”";
			// directed double down/up left pointing
			tmp["‚"] = "’";
			// directed single down/up left pointing
			tmp["``"] = "''";
			// double latex -- single latex quotes don't belong here!
			DirectedQuotes = Java.Util.Collections.UnmodifiableMap(tmp);
		}

		/// <summary>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and '.
		/// </summary>
		/// <remarks>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and '. If an unclosed quote appears, by default,
		/// this quote will not be counted as a quote.
		/// </remarks>
		/// <param name="name">
		/// String that is ignored but allows for creation of the
		/// QuoteAnnotator via a customAnnotatorClass
		/// </param>
		/// <param name="props">
		/// Properties object that contains the customizable properties
		/// attributes.
		/// </param>
		public QuoteAnnotator(string name, Properties props)
			: this(name, props, false)
		{
		}

		/// <summary>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and ' as well as a variety of smart and international quotes.
		/// </summary>
		/// <remarks>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and ' as well as a variety of smart and international quotes.
		/// If an unclosed quote appears, by default, this quote will not be counted as a quote.
		/// </remarks>
		/// <param name="props">
		/// Properties object that contains the customizable properties
		/// attributes.
		/// </param>
		public QuoteAnnotator(Properties props)
			: this("quote", props, false)
		{
		}

		/// <summary>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and '.
		/// </summary>
		/// <remarks>
		/// Return a QuoteAnnotator that isolates quotes denoted by the
		/// ASCII characters " and '. If an unclosed quote appears, by default,
		/// this quote will not be counted as a quote.
		/// </remarks>
		/// <param name="props">
		/// Properties object that contains the customizable properties
		/// attributes.
		/// </param>
		/// <param name="verbose">whether or not to output verbose information.</param>
		public QuoteAnnotator(string name, Properties props, bool verbose)
		{
			UseSingle = bool.ParseBoolean(props.GetProperty(name + "." + "singleQuotes", "false"));
			MaxLength = System.Convert.ToInt32(props.GetProperty(name + "." + "maxLength", "-1"));
			AsciiQuotes = bool.ParseBoolean(props.GetProperty(name + "." + "asciiQuotes", "false"));
			AllowEmbeddedSame = bool.ParseBoolean(props.GetProperty(name + "." + "allowEmbeddedSame", "false"));
			SmartQuotes = bool.ParseBoolean(props.GetProperty(name + "." + "smartQuotes", "false"));
			ExtractUnclosed = bool.ParseBoolean(props.GetProperty(name + "." + "extractUnclosedQuotes", "false"));
			AttributeQuotes = bool.ParseBoolean(props.GetProperty(name + "." + "attributeQuotes", "true"));
			Verbose = verbose;
			Timing timer = null;
			if (Verbose)
			{
				timer = new Timing();
				log.Info("Preparing quote annotator...");
			}
			if (AttributeQuotes)
			{
				quoteAttributionAnnotator = new QuoteAttributionAnnotator(props);
			}
			if (Verbose)
			{
				timer.Stop("done.");
			}
		}

		/// <summary>helper method for creating version of document text without xml.</summary>
		public static string XmlFreeText(string documentText, Annotation annotation)
		{
			int firstTokenCharIndex = annotation.Get(typeof(CoreAnnotations.TokensAnnotation))[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			// add white space for all text before first token
			string cleanedText = Sharpen.Runtime.Substring(documentText, 0, firstTokenCharIndex).ReplaceAll("\\S", " ");
			int tokenIndex = 0;
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			foreach (CoreLabel token in tokens)
			{
				// add the current token's text
				cleanedText += token.OriginalText();
				// add whitespace for non-tokens and xml in between these tokens
				tokenIndex += 1;
				if (tokenIndex < tokens.Count)
				{
					CoreLabel nextToken = tokens[tokenIndex];
					int inBetweenStart = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					int inBetweenEnd = nextToken.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					string inBetweenTokenText = Sharpen.Runtime.Substring(documentText, inBetweenStart, inBetweenEnd);
					inBetweenTokenText = inBetweenTokenText.ReplaceAll("\\S", " ");
					cleanedText += inBetweenTokenText;
				}
			}
			// add white space for all non-token content after last token
			cleanedText += Sharpen.Runtime.Substring(documentText, cleanedText.Length, documentText.Length).ReplaceAll("\\S", " ");
			return cleanedText;
		}

		public virtual void Annotate(Annotation annotation)
		{
			string text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			// clear out xml content from text
			text = XmlFreeText(text, annotation);
			// TODO: the following, if you want the quote annotator to get these truly correct
			// Pre-process to make word terminal apostrophes specially encoded (Jones' dog)
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			string quotesFrom = text;
			if (SmartQuotes)
			{
				// we're just going to try a bunch of different things and pick
				// whichever results in the most total quotes
				// try unicode
				Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> overall = GetQuotes(quotesFrom);
				string docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
				IList<ICoreMap> cmQuotesUnicode = GetCoreMapQuotes(overall.First(), tokens, sentences, text, docID, false);
				IList<ICoreMap> cmUnclosedUnicode = null;
				if (ExtractUnclosed)
				{
					cmUnclosedUnicode = GetCoreMapQuotes(overall.Second(), tokens, sentences, text, docID, true);
				}
				int numUnicode = CountQuotes(cmQuotesUnicode);
				// try ascii
				if (AsciiQuotes)
				{
					quotesFrom = ReplaceUnicode(text);
				}
				overall = GetQuotes(quotesFrom);
				docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
				IList<ICoreMap> cmQuotesAscii = GetCoreMapQuotes(overall.First(), tokens, sentences, text, docID, false);
				IList<ICoreMap> cmUnclosedAscii = null;
				if (ExtractUnclosed)
				{
					cmUnclosedAscii = GetCoreMapQuotes(overall.Second(), tokens, sentences, text, docID, true);
				}
				int numAsciiSingle = CountQuotes(cmQuotesAscii);
				// don't allow single quotes
				UseSingle = false;
				overall = GetQuotes(quotesFrom);
				docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
				IList<ICoreMap> cmQuotesAsciiNoSingle = GetCoreMapQuotes(overall.First(), tokens, sentences, text, docID, false);
				IList<ICoreMap> cmUnclosedAsciiNoSingle = null;
				if (ExtractUnclosed)
				{
					cmUnclosedAsciiNoSingle = GetCoreMapQuotes(overall.Second(), tokens, sentences, text, docID, true);
				}
				int numAsciiNoSingle = CountQuotes(cmQuotesAsciiNoSingle);
				log.Info("Number of quotes + unicode - single : " + numUnicode);
				log.Info("Number of quotes + ascii - single : " + numAsciiNoSingle);
				log.Info("Number of quotes + ascii + single : " + numAsciiSingle);
				if (numUnicode >= numAsciiNoSingle && numUnicode > (numAsciiSingle / 2))
				{
					SetAnnotations(annotation, cmQuotesUnicode, cmUnclosedUnicode, "Using unicode quotes.");
				}
				else
				{
					if (numAsciiSingle > (numAsciiNoSingle / 2))
					{
						SetAnnotations(annotation, cmQuotesAscii, cmUnclosedAscii, "Using ascii quotes.");
					}
					else
					{
						SetAnnotations(annotation, cmQuotesAsciiNoSingle, cmUnclosedAsciiNoSingle, "Using ascii quotes with no single quotes.");
					}
				}
			}
			else
			{
				if (AsciiQuotes)
				{
					quotesFrom = ReplaceUnicode(text);
				}
				Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> overall = GetQuotes(quotesFrom);
				string docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
				IList<ICoreMap> cmQuotes = GetCoreMapQuotes(overall.First(), tokens, sentences, text, docID, false);
				IList<ICoreMap> cmQuotesUnclosed = GetCoreMapQuotes(overall.Second(), tokens, sentences, text, docID, true);
				// add quotes to document
				SetAnnotations(annotation, cmQuotes, cmQuotesUnclosed, "Setting quotes.");
			}
			// if quote attribution is activated, run the quoteAttributionAnnotator
			if (AttributeQuotes)
			{
				quoteAttributionAnnotator.Annotate(annotation);
			}
		}

		private void SetAnnotations(Annotation annotation, IList<ICoreMap> quotes, IList<ICoreMap> unclosed, string message)
		{
			annotation.Set(typeof(CoreAnnotations.QuotationsAnnotation), quotes);
			log.Info(message);
			if (ExtractUnclosed)
			{
				annotation.Set(typeof(CoreAnnotations.UnclosedQuotationsAnnotation), unclosed);
			}
		}

		//TODO: update this so that it goes more than 1 layer deep
		private static int CountQuotes(IList<ICoreMap> quotes)
		{
			int total = quotes.Count;
			foreach (ICoreMap quote in quotes)
			{
				IList<ICoreMap> innerQuotes = quote.Get(typeof(CoreAnnotations.QuotationsAnnotation));
				if (innerQuotes != null)
				{
					total += innerQuotes.Count;
				}
			}
			return total;
		}

		private static readonly Pattern asciiSingleQuote = Pattern.Compile("&apos;|[\u0091\u2018\u0092\u2019\u201A\u201B\u2039\u203A']");

		private static readonly Pattern asciiDoubleQuote = Pattern.Compile("&quot;|[\u0093\u201C\u0094\u201D\u201E\u00AB\u00BB\"]");

		// Stolen from PTBLexer
		private static string AsciiQuotes(string @in)
		{
			string s1 = @in;
			s1 = asciiSingleQuote.Matcher(s1).ReplaceAll("'");
			s1 = asciiDoubleQuote.Matcher(s1).ReplaceAll("\"");
			return s1;
		}

		public static string ReplaceUnicode(string text)
		{
			return AsciiQuotes(text);
		}

		public static IComparator<ICoreMap> GetQuoteComparator()
		{
			return new _IComparator_330();
		}

		private sealed class _IComparator_330 : IComparator<ICoreMap>
		{
			public _IComparator_330()
			{
			}

			public int Compare(ICoreMap o1, ICoreMap o2)
			{
				int s1 = o1.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int s2 = o2.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				return s1 - s2;
			}
		}

		public static IList<ICoreMap> GetCoreMapQuotes(IList<Pair<int, int>> quotes, IList<CoreLabel> tokens, IList<ICoreMap> sentences, string text, string docID, bool unclosed)
		{
			IList<ICoreMap> cmQuotes = Generics.NewArrayList();
			foreach (Pair<int, int> p in quotes)
			{
				int begin = p.First();
				int end = p.Second();
				// find the tokens for this quote
				IList<CoreLabel> quoteTokens = new List<CoreLabel>();
				int tokenOffset = -1;
				if (tokens != null)
				{
					int currTok = 0;
					while (currTok < tokens.Count && tokens[currTok].BeginPosition() < begin)
					{
						currTok++;
					}
					int i = currTok;
					tokenOffset = i;
					while (i < tokens.Count && tokens[i].EndPosition() <= end)
					{
						quoteTokens.Add(tokens[i]);
						i++;
					}
				}
				// find the sentences for this quote
				int beginSentence = -1;
				int endSentence = -1;
				if (sentences != null)
				{
					foreach (ICoreMap sentence in sentences)
					{
						int sentBegin = sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
						int sentEnd = sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
						int sentIndex = sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
						if (sentBegin <= begin)
						{
							beginSentence = sentIndex;
						}
						if (sentEnd >= end && endSentence < 0)
						{
							endSentence = sentIndex;
						}
					}
				}
				// create a quote annotation with text and token offsets
				Annotation quote = MakeQuote(Sharpen.Runtime.Substring(text, begin, end), begin, end, quoteTokens, tokenOffset, beginSentence, endSentence, docID);
				// add quote in and filter
				// filter: quoteTokens.size() != 0
				// filter: endSentence == -1
				if (quoteTokens.Count != 0 && endSentence > -1)
				{
					cmQuotes.Add(quote);
				}
			}
			// sort quotes by beginning index
			IComparator<ICoreMap> quoteComparator = GetQuoteComparator();
			cmQuotes.Sort(quoteComparator);
			// embed quotes
			IList<ICoreMap> toRemove = new List<ICoreMap>();
			foreach (ICoreMap cmQuote in cmQuotes)
			{
				int start = cmQuote.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int end = cmQuote.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				// See if we need to embed a quote
				IList<ICoreMap> embeddedQuotes = new List<ICoreMap>();
				foreach (ICoreMap cmQuoteComp in cmQuotes)
				{
					int startComp = cmQuoteComp.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int endComp = cmQuoteComp.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					if (start < startComp && end >= endComp)
					{
						// p contains comp
						embeddedQuotes.Add(cmQuoteComp);
						// now we want to remove it from the top-level quote list
						toRemove.Add(cmQuoteComp);
					}
				}
				if (!unclosed)
				{
					cmQuote.Set(typeof(CoreAnnotations.QuotationsAnnotation), embeddedQuotes);
				}
				else
				{
					cmQuote.Set(typeof(CoreAnnotations.UnclosedQuotationsAnnotation), embeddedQuotes);
				}
			}
			// Remove all the quotes that we want to.
			foreach (ICoreMap r in toRemove)
			{
				// remove that quote from the overall list
				cmQuotes.Remove(r);
			}
			// Set the quote index annotations properly
			SetQuoteIndices(cmQuotes, unclosed);
			return cmQuotes;
		}

		private static void SetQuoteIndices(IList<ICoreMap> topLevel, bool unclosed)
		{
			IList<ICoreMap> level = topLevel;
			int index = 0;
			while (!level.IsEmpty())
			{
				IList<ICoreMap> nextLevel = Generics.NewArrayList();
				foreach (ICoreMap quote in level)
				{
					quote.Set(typeof(CoreAnnotations.QuotationIndexAnnotation), index);
					IList<CoreLabel> quoteTokens = quote.Get(typeof(CoreAnnotations.TokensAnnotation));
					if (quoteTokens != null)
					{
						foreach (CoreLabel qt in quoteTokens)
						{
							qt.Set(typeof(CoreAnnotations.QuotationIndexAnnotation), index);
						}
					}
					index++;
					IList<ICoreMap> key = quote.Get(typeof(CoreAnnotations.QuotationsAnnotation));
					if (unclosed)
					{
						key = quote.Get(typeof(CoreAnnotations.UnclosedQuotationsAnnotation));
					}
					if (key != null)
					{
						if (!unclosed)
						{
							Sharpen.Collections.AddAll(nextLevel, quote.Get(typeof(CoreAnnotations.QuotationsAnnotation)));
						}
						else
						{
							Sharpen.Collections.AddAll(nextLevel, quote.Get(typeof(CoreAnnotations.UnclosedQuotationsAnnotation)));
						}
					}
				}
				level = nextLevel;
			}
		}

		public static Annotation MakeQuote(string surfaceForm, int begin, int end, IList<CoreLabel> quoteTokens, int tokenOffset, int sentenceBeginIndex, int sentenceEndIndex, string docID)
		{
			Annotation quote = new Annotation(surfaceForm);
			// create a quote annotation with text and token offsets
			quote.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
			quote.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
			if (docID != null)
			{
				quote.Set(typeof(CoreAnnotations.DocIDAnnotation), docID);
			}
			if (quoteTokens != null)
			{
				quote.Set(typeof(CoreAnnotations.TokensAnnotation), quoteTokens);
				quote.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenOffset);
				quote.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenOffset + quoteTokens.Count - 1);
			}
			quote.Set(typeof(CoreAnnotations.SentenceBeginAnnotation), sentenceBeginIndex);
			quote.Set(typeof(CoreAnnotations.SentenceEndAnnotation), sentenceEndIndex);
			return quote;
		}

		public virtual Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> GetQuotes(string text)
		{
			return RecursiveQuotes(text, 0, null);
		}

		public virtual Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> RecursiveQuotes(string text, int offset, string prevQuote)
		{
			IDictionary<string, IList<Pair<int, int>>> quotesMap = new Dictionary<string, IList<Pair<int, int>>>();
			int start = -1;
			int end = -1;
			string quote = null;
			int directed = 0;
			for (int i = 0; i < text.Length; i++)
			{
				// Either I'm not in any quote or this one matches
				// the kind that I am.
				string c = Sharpen.Runtime.Substring(text, i, i + 1);
				if (c.Equals("`") && i < text.Length - 1 && text[i + 1] == '`')
				{
					c += text[i + 1];
				}
				else
				{
					if (c.Equals("'") && (quote != null && (quote.Equals("``") || quote.Equals("`"))))
					{
						// we want to ignore it if unless is is the beginning of the
						// last set of ' of the proper length
						int curr = i;
						while (curr < text.Length && text[curr] == '\'')
						{
							curr++;
						}
						if (i == curr - quote.Length || (directed > 0 && i == curr - (directed * quote.Length)))
						{
							for (int a = i + 1; a < i + quote.Length; a++)
							{
								c += text[a];
							}
						}
						else
						{
							continue;
						}
					}
				}
				if (DirectedQuotes.Contains(quote) && DirectedQuotes[quote].Equals(c))
				{
					if (c.Equals("’"))
					{
						if ((i == text.Length - 1 || IsSingleQuoteEnd(text, i)))
						{
							// check to make sure that this isn't an apostrophe..
							directed--;
						}
					}
					else
					{
						// closing
						directed--;
					}
				}
				// opening
				if ((start < 0) && !MatchesPrevQuote(c, prevQuote) && (((IsSingleQuoteWithUse(c) || c.Equals("`")) && IsSingleQuoteStart(text, i)) || (c.Equals("\"") || DirectedQuotes.Contains(c))))
				{
					start = i;
					quote = c;
				}
				else
				{
					// closing
					if ((start >= 0 && end < 0) && ((c.Equals(quote) && (((c.Equals("'") || c.Equals("`")) && IsSingleQuoteEnd(text, i)) || (c.Equals("\"") && IsDoubleQuoteEnd(text, i)))) || (c.Equals("'") && quote.Equals("`") && IsSingleQuoteEnd(text, i)) || (
						DirectedQuotes.Contains(quote) && DirectedQuotes[quote].Equals(c) && directed == 0)))
					{
						// latex quotes are kind of problematic
						end = i + c.Length;
					}
				}
				if (DirectedQuotes.Contains(c) && c.Equals(quote))
				{
					// opening of this kind of directed quote
					directed++;
				}
				if (start >= 0 && end > 0)
				{
					if (!quotesMap.Contains(quote))
					{
						quotesMap[quote] = new List<Pair<int, int>>();
					}
					quotesMap[quote].Add(new Pair<int, int>(start, end));
					start = -1;
					end = -1;
					quote = null;
				}
				if (c.Length > 1)
				{
					i += c.Length - 1;
				}
				// forget about this quote
				if (MaxLength > 0 && start >= 0 && i - start > MaxLength)
				{
					// go back to the right index after start
					i = start + quote.Length;
					start = -1;
					end = -1;
					quote = null;
				}
			}
			// TODO: determine if we want to be more strict w/ single quotes than double
			// answer: we do want to.
			if (start >= 0 && start < text.Length - 3)
			{
				string warning = text;
				if (text.Length > 150)
				{
					warning = Sharpen.Runtime.Substring(text, 0, 150) + "...";
				}
				log.Info("WARNING: unmatched quote of type " + quote + " found at index " + start + " in text segment: " + warning);
			}
			// recursively look for embedded quotes in these ones
			IList<Pair<int, int>> quotes = Generics.NewArrayList();
			IList<Pair<int, int>> unclosedQuotes = Generics.NewArrayList();
			// If I didn't find any quotes, but did find a quote-beginning, try again,
			// but without the part of the text before the single quote
			// really this test should be whether or not start is mapped to in quotesMap
			if (!IsAQuoteMapStarter(start, quotesMap) && start >= 0 && start < text.Length - 3)
			{
				if (ExtractUnclosed)
				{
					unclosedQuotes.Add(new Pair<int, int>(start, text.Length));
				}
				string toPass = Sharpen.Runtime.Substring(text, start + quote.Length, text.Length);
				Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> embedded = RecursiveQuotes(toPass, offset, null);
				// these are the good quotes
				foreach (Pair<int, int> e in embedded.First())
				{
					quotes.Add(new Pair<int, int>(e.First() + start + quote.Length, e.Second() + start + 1));
				}
				if (ExtractUnclosed)
				{
					// these are the unclosed quotes
					foreach (Pair<int, int> e_1 in embedded.Second())
					{
						unclosedQuotes.Add(new Pair<int, int>(e_1.First() + start + quote.Length, e_1.Second() + start + 1));
					}
				}
			}
			// Now take care of the good quotes that we found
			foreach (string qKind in quotesMap.Keys)
			{
				foreach (Pair<int, int> q in quotesMap[qKind])
				{
					if (q.Second() - q.First() >= qKind.Length * 2)
					{
						string toPass = Sharpen.Runtime.Substring(text, q.First() + qKind.Length, q.Second() - qKind.Length);
						string qKindToPass = null;
						if (!(DirectedQuotes.Contains(qKind) || qKind.Equals("`")) || !AllowEmbeddedSame)
						{
							qKindToPass = qKind;
						}
						Pair<IList<Pair<int, int>>, IList<Pair<int, int>>> embedded = RecursiveQuotes(toPass, q.First() + qKind.Length + offset, qKindToPass);
						// good quotes
						foreach (Pair<int, int> e in embedded.First())
						{
							// don't add offset here because the
							// recursive method already added it
							if (e.Second() - e.First() > 2)
							{
								quotes.Add(new Pair<int, int>(e.First(), e.Second()));
							}
						}
						// unclosed quotes
						if (ExtractUnclosed)
						{
							// these are the unclosed quotes
							foreach (Pair<int, int> e_1 in embedded.Second())
							{
								unclosedQuotes.Add(new Pair<int, int>(e_1.First(), e_1.Second()));
							}
						}
					}
					quotes.Add(new Pair<int, int>(q.First() + offset, q.Second() + offset));
				}
			}
			return new Pair<IList<Pair<int, int>>, IList<Pair<int, int>>>(quotes, unclosedQuotes);
		}

		private static bool IsAQuoteMapStarter(int target, IDictionary<string, IList<Pair<int, int>>> quotesMap)
		{
			foreach (string k in quotesMap.Keys)
			{
				foreach (Pair<int, int> pair in quotesMap[k])
				{
					if (pair.First() == target)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsSingleQuoteWithUse(string c)
		{
			return c.Equals("'") && UseSingle;
		}

		private static bool MatchesPrevQuote(string c, string prev)
		{
			return prev != null && prev.Equals(c);
		}

		private static bool IsSingleQuoteStart(string text, int i)
		{
			if (i == 0)
			{
				return true;
			}
			string prev = Sharpen.Runtime.Substring(text, i - 1, i);
			return IsWhitespaceOrPunct(prev);
		}

		private static bool IsSingleQuoteEnd(string text, int i)
		{
			if (i == text.Length - 1)
			{
				return true;
			}
			string next = Sharpen.Runtime.Substring(text, i + 1, i + 2);
			return IsWhitespaceOrPunct(next);
		}

		private static bool IsDoubleQuoteEnd(string text, int i)
		{
			if (i == text.Length - 1)
			{
				return true;
			}
			string next = Sharpen.Runtime.Substring(text, i + 1, i + 2);
			if (i == text.Length - 2 && IsWhitespaceOrPunct(next))
			{
				return true;
			}
			string nextNext = Sharpen.Runtime.Substring(text, i + 2, i + 3);
			return ((IsWhitespaceOrPunct(next) && !IsSingleQuote(next)) || (IsSingleQuote(next) && IsWhitespaceOrPunct(nextNext)));
		}

		public static bool IsWhitespaceOrPunct(string c)
		{
			Pattern punctOrWhite = Pattern.Compile("[\\s\\p{Punct}]", Pattern.UnicodeCharacterClass);
			Matcher m = punctOrWhite.Matcher(c);
			return m.Matches();
		}

		public static bool IsSingleQuote(string c)
		{
			return c.Equals("'");
		}

		public virtual ICollection<Type> Requires()
		{
			// set base requirements
			ICollection<Type> baseRequirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation
				), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.IsNewlineAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation)));
			// add extra quote attribution requirements if necessary
			if (AttributeQuotes)
			{
				HashSet<Type> attributionRequirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation), typeof(CoreAnnotations.MentionsAnnotation), typeof(CoreAnnotations.TokenEndAnnotation
					), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CorefCoreAnnotations.CorefChainAnnotation), typeof(CoreAnnotations.MentionsAnnotation
					), typeof(CoreAnnotations.EntityMentionIndexAnnotation), typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation)));
				Sharpen.Collections.AddAll(baseRequirements, attributionRequirements);
			}
			return baseRequirements;
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			if (AttributeQuotes)
			{
				return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.QuotationsAnnotation), typeof(CoreAnnotations.QuotationIndexAnnotation), typeof(QuoteAttributionAnnotator.MentionAnnotation), typeof(QuoteAttributionAnnotator.MentionBeginAnnotation
					), typeof(QuoteAttributionAnnotator.MentionEndAnnotation), typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), typeof(QuoteAttributionAnnotator.MentionSieveAnnotation), typeof(QuoteAttributionAnnotator.SpeakerAnnotation), typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation
					), typeof(CoreAnnotations.ParagraphIndexAnnotation)));
			}
			else
			{
				return Java.Util.Collections.Singleton(typeof(CoreAnnotations.QuotationsAnnotation));
			}
		}

		// helper method to recursively gather all embedded quotes
		public static IList<ICoreMap> GatherQuotes(ICoreMap curr)
		{
			IList<ICoreMap> embedded = curr.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			if (embedded != null)
			{
				IList<ICoreMap> extended = Generics.NewArrayList();
				foreach (ICoreMap quote in embedded)
				{
					Sharpen.Collections.AddAll(extended, GatherQuotes(quote));
				}
				Sharpen.Collections.AddAll(extended, embedded);
				return extended;
			}
			else
			{
				return Generics.NewArrayList();
			}
		}
	}
}
