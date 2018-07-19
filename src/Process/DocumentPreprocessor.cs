using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Produces a list of sentences from either a plain text or XML document.</summary>
	/// <remarks>
	/// Produces a list of sentences from either a plain text or XML document.
	/// This class acts like a Reader: It allows you to make a single pass through a
	/// list of sentences in a document. If you need to pass through the document
	/// multiple times, then you need to create a second DocumentProcessor.
	/// <p>
	/// Tokenization: The default tokenizer is
	/// <see cref="PTBTokenizer{T}"/>
	/// . If null is passed
	/// to
	/// <c>setTokenizerFactory</c>
	/// , then whitespace tokenization is assumed.
	/// <p>
	/// Adding a new document type requires two steps:
	/// <ol>
	/// <li> Add a new DocType.
	/// <li> Create an iterator for the new DocType and modify the iterator()
	/// function to return the new iterator.
	/// </ol>
	/// <p>
	/// NOTE: This implementation should <em>not</em> use external libraries since it
	/// is used in the parser.
	/// </remarks>
	/// <author>Spence Green</author>
	public class DocumentPreprocessor : IEnumerable<IList<IHasWord>>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.DocumentPreprocessor));

		public enum DocType
		{
			Plain,
			Xml
		}

		private static readonly string[] DefaultSentenceDelims = new string[] { ".", "?", "!", "!!", "!!!", "??", "?!", "!?" };

		private Reader inputReader;

		private readonly DocumentPreprocessor.DocType docType;

		private ITokenizerFactory<IHasWord> tokenizerFactory = PTBTokenizer.CoreLabelFactory();

		private string[] sentenceFinalPuncWords = DefaultSentenceDelims;

		private IFunction<IList<IHasWord>, IList<IHasWord>> escaper;

		private string sentenceDelimiter;

		/// <summary>
		/// Example: if the words are already POS tagged and look like
		/// foo_VB, you want to set the tagDelimiter to "_"
		/// </summary>
		private string tagDelimiter;

		/// <summary>
		/// When doing XML parsing, only accept text in between tags that
		/// match this regular expression.
		/// </summary>
		/// <remarks>
		/// When doing XML parsing, only accept text in between tags that
		/// match this regular expression.  Defaults to everything.
		/// </remarks>
		private string elementDelimiter = ".*";

		private static readonly Pattern wsPattern = Pattern.Compile("\\s+");

		private readonly string[] sentenceFinalFollowers = new string[] { ")", "]", "\"", "\'", "''", "-RRB-", "-RSB-", "-RCB-" };

		private bool keepEmptySentences;

		/// <summary>Constructs a preprocessor from an existing input stream.</summary>
		/// <param name="input">An existing reader</param>
		public DocumentPreprocessor(Reader input)
			: this(input, DocumentPreprocessor.DocType.Plain)
		{
		}

		public DocumentPreprocessor(Reader input, DocumentPreprocessor.DocType t)
		{
			// todo [cdm 2017]: This class is used in all our parsers, but we should probably work to move over to WordToSetenceProcessor, which has been used in CoreNLP and has been developed more.
			// todo: Should probably change this to be regex, but I've added some multi-character punctuation in the meantime
			// inputReader is used in a fairly yucky way at the moment to communicate
			// from a XMLIterator across to a PlainTextIterator.  Maybe redo by making
			// the inner classes static and explicitly passing things around.
			//Configurable options
			// = null;
			// = null;
			// = null;
			//From PTB conventions
			// = false;
			if (input == null)
			{
				throw new ArgumentException("Cannot read from null object!");
			}
			docType = t;
			inputReader = input;
		}

		public DocumentPreprocessor(string docPath)
			: this(docPath, DocumentPreprocessor.DocType.Plain, "UTF-8")
		{
		}

		public DocumentPreprocessor(string docPath, DocumentPreprocessor.DocType t)
			: this(docPath, t, "UTF-8")
		{
		}

		/// <summary>
		/// Constructs a preprocessor from a file at a path, which can be either
		/// a filesystem location, a classpath entry, or a URL.
		/// </summary>
		/// <param name="docPath">The path</param>
		/// <param name="encoding">The character encoding used by Readers</param>
		public DocumentPreprocessor(string docPath, DocumentPreprocessor.DocType t, string encoding)
		{
			if (docPath == null)
			{
				throw new ArgumentException("Cannot open null document path!");
			}
			docType = t;
			try
			{
				inputReader = IOUtils.ReaderFromString(docPath, encoding);
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException(string.Format("%s: Could not open path %s", this.GetType().FullName, docPath), ioe);
			}
		}

		/// <summary>
		/// Set whether or not the tokenizer keeps empty sentences in
		/// whitespace mode.
		/// </summary>
		/// <remarks>
		/// Set whether or not the tokenizer keeps empty sentences in
		/// whitespace mode.  Useful for programs that want to echo blank
		/// lines.  Not relevant for the non-whitespace model.
		/// </remarks>
		public virtual void SetKeepEmptySentences(bool keepEmptySentences)
		{
			this.keepEmptySentences = keepEmptySentences;
		}

		/// <summary>Sets the end-of-sentence delimiters.</summary>
		/// <remarks>
		/// Sets the end-of-sentence delimiters.
		/// <p>
		/// For newline tokenization, use the argument {"\n"}.
		/// </remarks>
		/// <param name="sentenceFinalPuncWords">An array of words that count as sentence final punctuation.</param>
		public virtual void SetSentenceFinalPuncWords(string[] sentenceFinalPuncWords)
		{
			this.sentenceFinalPuncWords = sentenceFinalPuncWords;
		}

		/// <summary>
		/// Sets the factory from which to produce a
		/// <see cref="ITokenizer{T}"/>
		/// .  The default is
		/// <see cref="PTBTokenizer{T}"/>
		/// .
		/// <p>
		/// NOTE: If a null argument is used, then the document is assumed to be tokenized
		/// and DocumentPreprocessor performs no tokenization.
		/// </summary>
		public virtual void SetTokenizerFactory<_T0>(ITokenizerFactory<_T0> newTokenizerFactory)
			where _T0 : IHasWord
		{
			tokenizerFactory = newTokenizerFactory;
		}

		/// <summary>Set an escaper.</summary>
		/// <param name="e">The escaper</param>
		public virtual void SetEscaper(IFunction<IList<IHasWord>, IList<IHasWord>> e)
		{
			escaper = e;
		}

		/// <summary>
		/// Make the processor assume that the document is already delimited
		/// by the supplied parameter.
		/// </summary>
		/// <param name="s">The sentence delimiter</param>
		public virtual void SetSentenceDelimiter(string s)
		{
			sentenceDelimiter = s;
		}

		/// <summary>Split tags from tokens.</summary>
		/// <remarks>
		/// Split tags from tokens. The tag will be placed in the TagAnnotation of
		/// the returned label.
		/// <p>
		/// Note that for strings that contain two or more instances of the tag delimiter,
		/// the last instance is treated as the split point.
		/// <p>
		/// The tag delimiter should not contain any characters that must be escaped in a Java
		/// regex.
		/// </remarks>
		/// <param name="s">POS tag delimiter</param>
		public virtual void SetTagDelimiter(string s)
		{
			tagDelimiter = s;
		}

		/// <summary>Only read text from inside these XML elements if in XML mode.</summary>
		/// <remarks>
		/// Only read text from inside these XML elements if in XML mode.
		/// <i>Note:</i> This class implements an approximation to XML via regex.
		/// Otherwise, text will read from all tokens.
		/// </remarks>
		public virtual void SetElementDelimiter(string s)
		{
			elementDelimiter = s;
		}

		/// <summary>Returns sentences until the document is exhausted.</summary>
		/// <remarks>
		/// Returns sentences until the document is exhausted. Calls close() if the end of the document
		/// is reached. Otherwise, the user is required to close the stream.
		/// </remarks>
		/// <returns>
		/// An Iterator over sentences (each a List of word tokens).
		/// Although the type is given as
		/// <c>List&lt;HasWord&gt;</c>
		/// , in practice you get a List of CoreLabel,
		/// and you can cast down to that. (Someday we might manage to fix the generic typing....)
		/// </returns>
		public virtual IEnumerator<IList<IHasWord>> GetEnumerator()
		{
			// Add new document types here
			if (docType == DocumentPreprocessor.DocType.Plain)
			{
				return new DocumentPreprocessor.PlainTextIterator(this);
			}
			else
			{
				if (docType == DocumentPreprocessor.DocType.Xml)
				{
					return new DocumentPreprocessor.XMLIterator(this);
				}
				else
				{
					throw new InvalidOperationException("Someone didn't add a handler for a new docType.");
				}
			}
		}

		private class PlainTextIterator : IEnumerator<IList<IHasWord>>
		{
			private readonly ITokenizer<IHasWord> tokenizer;

			private readonly ICollection<string> sentDelims;

			private readonly ICollection<string> delimFollowers;

			private readonly IFunction<string, string[]> splitTag;

			private IList<IHasWord> nextSent;

			private readonly IList<IHasWord> nextSentCarryover = Generics.NewArrayList();

			public PlainTextIterator(DocumentPreprocessor _enclosing)
			{
				this._enclosing = _enclosing;
				// = null;
				// Establish how to find sentence boundaries
				bool eolIsSignificant = false;
				this.sentDelims = Generics.NewHashSet();
				if (this._enclosing.sentenceDelimiter == null)
				{
					if (this._enclosing.sentenceFinalPuncWords != null)
					{
						Sharpen.Collections.AddAll(this.sentDelims, Arrays.AsList(this._enclosing.sentenceFinalPuncWords));
					}
					this.delimFollowers = Generics.NewHashSet(Arrays.AsList(this._enclosing.sentenceFinalFollowers));
				}
				else
				{
					this.sentDelims.Add(this._enclosing.sentenceDelimiter);
					this.delimFollowers = Generics.NewHashSet();
					eolIsSignificant = DocumentPreprocessor.wsPattern.Matcher(this._enclosing.sentenceDelimiter).Matches();
					if (eolIsSignificant)
					{
						// For Stanford English Tokenizer
						this.sentDelims.Add(PTBTokenizer.GetNewlineToken());
					}
				}
				// Setup the tokenizer
				if (this._enclosing.tokenizerFactory == null)
				{
					eolIsSignificant = this.sentDelims.Contains(WhitespaceLexer.Newline);
					this.tokenizer = WhitespaceTokenizer.NewCoreLabelWhitespaceTokenizer(this._enclosing.inputReader, eolIsSignificant);
				}
				else
				{
					if (eolIsSignificant)
					{
						this.tokenizer = this._enclosing.tokenizerFactory.GetTokenizer(this._enclosing.inputReader, "tokenizeNLs");
					}
					else
					{
						this.tokenizer = this._enclosing.tokenizerFactory.GetTokenizer(this._enclosing.inputReader);
					}
				}
				// If tokens are tagged, then we must split them
				// Note that if the token contains two or more instances of the delimiter, then the last
				// instance is regarded as the split point.
				if (this._enclosing.tagDelimiter == null)
				{
					this.splitTag = null;
				}
				else
				{
					this.splitTag = new _IFunction_281(this);
				}
			}

			private sealed class _IFunction_281 : IFunction<string, string[]>
			{
				public _IFunction_281()
				{
					this.splitRegex = string.Format("%s(?!.*%s)", this._enclosing._enclosing.tagDelimiter, this._enclosing._enclosing.tagDelimiter);
				}

				private readonly string splitRegex;

				public string[] Apply(string @in)
				{
					string[] splits = @in.Trim().Split(this.splitRegex);
					if (splits.Length == 2)
					{
						return splits;
					}
					else
					{
						string[] oldStr = new string[] { @in };
						return oldStr;
					}
				}
			}

			private void PrimeNext()
			{
				if (this._enclosing.inputReader == null)
				{
					// we've already been out of stuff and have closed the input reader; so just return
					return;
				}
				this.nextSent = Generics.NewArrayList(this.nextSentCarryover);
				this.nextSentCarryover.Clear();
				bool seenBoundary = false;
				if (!this.tokenizer.MoveNext())
				{
					IOUtils.CloseIgnoringExceptions(this._enclosing.inputReader);
					this._enclosing.inputReader = null;
					// nextSent = null; // WRONG: There may be something in it from the nextSentCarryover
					if (this.nextSent.IsEmpty())
					{
						this.nextSent = null;
					}
					return;
				}
				do
				{
					IHasWord token = this.tokenizer.Current;
					if (this.splitTag != null)
					{
						string[] toks = this.splitTag.Apply(token.Word());
						token.SetWord(toks[0]);
						if (token is ILabel)
						{
							((ILabel)token).SetValue(toks[0]);
						}
						if (toks.Length == 2 && token is IHasTag)
						{
							//wsg2011: Some of the underlying tokenizers return old
							//JavaNLP labels.  We could convert to CoreLabel here, but
							//we choose a conservative implementation....
							((IHasTag)token).SetTag(toks[1]);
						}
					}
					if (this.sentDelims.Contains(token.Word()))
					{
						seenBoundary = true;
					}
					else
					{
						if (seenBoundary && !this.delimFollowers.Contains(token.Word()))
						{
							this.nextSentCarryover.Add(token);
							break;
						}
					}
					if (!(DocumentPreprocessor.wsPattern.Matcher(token.Word()).Matches() || token.Word().Equals(PTBTokenizer.GetNewlineToken())))
					{
						this.nextSent.Add(token);
					}
					// If there are no words that can follow a sentence delimiter,
					// then there are two cases.  In one case is we already have a
					// sentence, in which case there is no reason to look at the
					// next token, since that just causes buffering without any
					// chance of the current sentence being extended, since
					// delimFollowers = {}.  In the other case, we have an empty
					// sentence, which at this point means the sentence delimiter
					// was a whitespace token such as \n.  We might as well keep
					// going as if we had never seen anything.
					if (seenBoundary && this.delimFollowers.IsEmpty())
					{
						if (!this.nextSent.IsEmpty() || this._enclosing.keepEmptySentences)
						{
							break;
						}
						else
						{
							seenBoundary = false;
						}
					}
				}
				while (this.tokenizer.MoveNext());
				if (this.nextSent.IsEmpty() && this.nextSentCarryover.IsEmpty() && !this._enclosing.keepEmptySentences)
				{
					IOUtils.CloseIgnoringExceptions(this._enclosing.inputReader);
					this._enclosing.inputReader = null;
					this.nextSent = null;
				}
				else
				{
					if (this._enclosing.escaper != null)
					{
						this.nextSent = this._enclosing.escaper.Apply(this.nextSent);
					}
				}
			}

			public virtual bool MoveNext()
			{
				if (this.nextSent == null)
				{
					this.PrimeNext();
				}
				return this.nextSent != null;
			}

			public virtual IList<IHasWord> Current
			{
				get
				{
					if (this.nextSent == null)
					{
						this.PrimeNext();
					}
					if (this.nextSent == null)
					{
						throw new NoSuchElementException();
					}
					IList<IHasWord> thisIteration = this.nextSent;
					this.nextSent = null;
					return thisIteration;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly DocumentPreprocessor _enclosing;
		}

		private class XMLIterator : IEnumerator<IList<IHasWord>>
		{
			private readonly XMLBeginEndIterator<string> xmlItr;

			private readonly Reader originalDocReader;

			private DocumentPreprocessor.PlainTextIterator plainItr;

			private IList<IHasWord> nextSent;

			public XMLIterator(DocumentPreprocessor _enclosing)
			{
				this._enclosing = _enclosing;
				// = null;
				// = null;
				this.xmlItr = new XMLBeginEndIterator<string>(this._enclosing.inputReader, this._enclosing.elementDelimiter);
				this.originalDocReader = this._enclosing.inputReader;
				this.PrimeNext();
			}

			private void PrimeNext()
			{
				do
				{
					// It is necessary to loop because if a document has a pattern
					// that goes: <tag></tag> the xmlItr will return an empty
					// string, which the plainItr will process to null.  If we
					// didn't loop to find the next tag, the iterator would stop.
					if (this.plainItr != null && this.plainItr.MoveNext())
					{
						this.nextSent = this.plainItr.Current;
					}
					else
					{
						if (this.xmlItr.MoveNext())
						{
							string block = this.xmlItr.Current;
							this._enclosing.inputReader = new BufferedReader(new StringReader(block));
							this.plainItr = new DocumentPreprocessor.PlainTextIterator(this);
							if (this.plainItr.MoveNext())
							{
								this.nextSent = this.plainItr.Current;
							}
							else
							{
								this.nextSent = null;
							}
						}
						else
						{
							IOUtils.CloseIgnoringExceptions(this.originalDocReader);
							this.nextSent = null;
							break;
						}
					}
				}
				while (this.nextSent == null);
			}

			public virtual bool MoveNext()
			{
				return this.nextSent != null;
			}

			public virtual IList<IHasWord> Current
			{
				get
				{
					if (this.nextSent == null)
					{
						throw new NoSuchElementException();
					}
					IList<IHasWord> thisSentence = this.nextSent;
					this.PrimeNext();
					return thisSentence;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly DocumentPreprocessor _enclosing;
		}

		// end class XMLIterator
		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.LineSeparator();
			sb.Append(string.Format("Usage: java %s [OPTIONS] [file] [< file]%n%n", typeof(DocumentPreprocessor).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("-xml delim              : XML input with associated delimiter.").Append(nl);
			sb.Append("-encoding type          : Input encoding (default: UTF-8).").Append(nl);
			sb.Append("-printSentenceLengths   : ").Append(nl);
			sb.Append("-noTokenization         : Split on newline delimiters only.").Append(nl);
			sb.Append("-printOriginalText      : Print the original, not normalized form of tokens.").Append(nl);
			sb.Append("-suppressEscaping       : Suppress PTB escaping.").Append(nl);
			sb.Append("-tokenizerOptions opts  : Specify custom tokenizer options.").Append(nl);
			sb.Append("-tag delim              : Input tokens are tagged. Split tags.").Append(nl);
			sb.Append("-whitespaceTokenization : Whitespace tokenization only.").Append(nl);
			sb.Append("-sentenceDelimiter delim: Split sentences on this also (\"newline\" for \\n)").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> ArgOptionDefs()
		{
			IDictionary<string, int> argOptionDefs = Generics.NewHashMap();
			argOptionDefs["help"] = 0;
			argOptionDefs["xml"] = 1;
			argOptionDefs["encoding"] = 1;
			argOptionDefs["printSentenceLengths"] = 0;
			argOptionDefs["noTokenization"] = 0;
			argOptionDefs["suppressEscaping"] = 0;
			argOptionDefs["tag"] = 1;
			argOptionDefs["tokenizerOptions"] = 1;
			argOptionDefs["whitespaceTokenization"] = 0;
			argOptionDefs["sentenceDelimiter"] = 1;
			return argOptionDefs;
		}

		/// <summary>A simple, deterministic sentence-splitter.</summary>
		/// <remarks>
		/// A simple, deterministic sentence-splitter. This method only supports the English
		/// tokenizer, so for other languages you should run the tokenizer first and then
		/// run this sentence splitter with the "-whitespaceTokenization" option.
		/// </remarks>
		/// <param name="args">Command-line arguments</param>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args, ArgOptionDefs());
			if (options.Contains("help"))
			{
				log.Info(Usage());
				return;
			}
			// Command-line flags
			string encoding = options.GetProperty("encoding", "utf-8");
			bool printSentenceLengths = PropertiesUtils.GetBool(options, "printSentenceLengths", false);
			string xmlElementDelimiter = options.GetProperty("xml", null);
			DocumentPreprocessor.DocType docType = xmlElementDelimiter == null ? DocumentPreprocessor.DocType.Plain : DocumentPreprocessor.DocType.Xml;
			string sentenceDelimiter = options.Contains("noTokenization") ? Runtime.GetProperty("line.separator") : null;
			string sDelim = options.GetProperty("sentenceDelimiter");
			if (sDelim != null)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(sDelim, "newline"))
				{
					sentenceDelimiter = "\n";
				}
				else
				{
					sentenceDelimiter = sDelim;
				}
			}
			string tagDelimiter = options.GetProperty("tag", null);
			string[] sentenceDelims = null;
			// Setup the TokenizerFactory
			int numFactoryFlags = 0;
			bool suppressEscaping = options.Contains("suppressEscaping");
			if (suppressEscaping)
			{
				numFactoryFlags += 1;
			}
			bool customTokenizer = options.Contains("tokenizerOptions");
			if (customTokenizer)
			{
				numFactoryFlags += 1;
			}
			bool printOriginalText = options.Contains("printOriginalText");
			if (printOriginalText)
			{
				numFactoryFlags += 1;
			}
			bool whitespaceTokenization = options.Contains("whitespaceTokenization");
			if (whitespaceTokenization)
			{
				numFactoryFlags += 1;
			}
			if (numFactoryFlags > 1)
			{
				log.Info("Only one tokenizer flag allowed at a time: ");
				log.Info("  -suppressEscaping, -tokenizerOptions, -printOriginalText, -whitespaceTokenization");
				return;
			}
			ITokenizerFactory<IHasWord> tf = null;
			if (suppressEscaping)
			{
				tf = PTBTokenizer.Factory(new CoreLabelTokenFactory(), "ptb3Escaping=false");
			}
			else
			{
				if (customTokenizer)
				{
					tf = PTBTokenizer.Factory(new CoreLabelTokenFactory(), options.GetProperty("tokenizerOptions"));
				}
				else
				{
					if (printOriginalText)
					{
						tf = PTBTokenizer.Factory(new CoreLabelTokenFactory(), "invertible=true");
					}
					else
					{
						if (whitespaceTokenization)
						{
							IList<string> whitespaceDelims = new List<string>(Arrays.AsList(DocumentPreprocessor.DefaultSentenceDelims));
							whitespaceDelims.Add(WhitespaceLexer.Newline);
							sentenceDelims = Sharpen.Collections.ToArray(whitespaceDelims, new string[whitespaceDelims.Count]);
						}
						else
						{
							tf = PTBTokenizer.Factory(new CoreLabelTokenFactory(), string.Empty);
						}
					}
				}
			}
			string fileList = options.GetProperty(string.Empty, null);
			string[] files = fileList == null ? new string[1] : fileList.Split("\\s+");
			int numSents = 0;
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
			foreach (string file in files)
			{
				DocumentPreprocessor docPreprocessor;
				if (file == null || file.IsEmpty())
				{
					docPreprocessor = new DocumentPreprocessor(new InputStreamReader(Runtime.@in, encoding));
				}
				else
				{
					docPreprocessor = new DocumentPreprocessor(file, docType, encoding);
				}
				if (docType == DocumentPreprocessor.DocType.Xml)
				{
					docPreprocessor.SetElementDelimiter(xmlElementDelimiter);
				}
				docPreprocessor.SetTokenizerFactory(tf);
				if (sentenceDelimiter != null)
				{
					docPreprocessor.SetSentenceDelimiter(sentenceDelimiter);
				}
				if (tagDelimiter != null)
				{
					docPreprocessor.SetTagDelimiter(tagDelimiter);
				}
				if (sentenceDelims != null)
				{
					docPreprocessor.SetSentenceFinalPuncWords(sentenceDelims);
				}
				foreach (IList<IHasWord> sentence in docPreprocessor)
				{
					numSents++;
					if (printSentenceLengths)
					{
						System.Console.Error.Printf("Length: %d%n", sentence.Count);
					}
					bool printSpace = false;
					foreach (IHasWord word in sentence)
					{
						if (printOriginalText)
						{
							CoreLabel cl = (CoreLabel)word;
							if (!printSpace)
							{
								pw.Print(cl.Get(typeof(CoreAnnotations.BeforeAnnotation)));
								printSpace = true;
							}
							pw.Print(cl.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
							pw.Print(cl.Get(typeof(CoreAnnotations.AfterAnnotation)));
						}
						else
						{
							if (printSpace)
							{
								pw.Print(" ");
							}
							printSpace = true;
							pw.Print(word.Word());
						}
					}
					pw.Println();
				}
			}
			pw.Close();
			System.Console.Error.Printf("Read in %d sentences.%n", numSents);
		}
	}
}
