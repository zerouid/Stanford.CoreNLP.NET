using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>Basic implementation of Document that should be suitable for most needs.</summary>
	/// <remarks>
	/// Basic implementation of Document that should be suitable for most needs.
	/// BasicDocument is an ArrayList for storing words and performs tokenization
	/// during construction. Override
	/// <see cref="BasicDocument{L}.Parse(string)"/>
	/// to provide support
	/// for custom
	/// document formats or to do a custom job of tokenization. BasicDocument should
	/// only be used for documents that are small enough to store in memory.
	/// The easiest way to use BasicDocuments is to construct them and call an init
	/// method in the same line (we use init methods instead of constructors because
	/// they're inherited and allow subclasses to have other more specific constructors).
	/// For example, to read in a file <tt>file</tt> and tokenize it, you can call
	/// <pre>Document doc=new BasicDocument().init(file);</pre>.
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	[System.Serializable]
	public class BasicDocument<L> : List<Word>, IDocument<L, Word, Word>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Ling.BasicDocument));

		/// <summary>title of this document (never null).</summary>
		protected internal string title = string.Empty;

		/// <summary>original text of this document (may be null).</summary>
		protected internal string originalText;

		/// <summary>Label(s) for this document.</summary>
		protected internal readonly IList<L> labels = new List<L>();

		/// <summary>
		/// TokenizerFactory used to convert the text into words inside
		/// <see cref="BasicDocument{L}.Parse(string)"/>
		/// .
		/// </summary>
		protected internal ITokenizerFactory<Word> tokenizerFactory;

		/// <summary>
		/// Constructs a new (empty) BasicDocument using a
		/// <see cref="Edu.Stanford.Nlp.Process.PTBTokenizer{T}"/>
		/// .
		/// Call one of the <tt>init</tt> * methods to populate the document
		/// from a desired source.
		/// </summary>
		public BasicDocument()
			: this(PTBTokenizer.Factory())
		{
		}

		/// <summary>Constructs a new (empty) BasicDocument using the given tokenizer.</summary>
		/// <remarks>
		/// Constructs a new (empty) BasicDocument using the given tokenizer.
		/// Call one of the <tt>init</tt> * methods to populate the document
		/// from a desired source.
		/// </remarks>
		public BasicDocument(ITokenizerFactory<Word> tokenizerFactory)
		{
			SetTokenizerFactory(tokenizerFactory);
		}

		public BasicDocument(IDocument<L, Word, Word> d)
			: this((ICollection<Word>)d)
		{
		}

		public BasicDocument(ICollection<Word> d)
			: this()
		{
			Sharpen.Collections.AddAll(this, d);
		}

		/// <summary>Inits a new BasicDocument with the given text contents and title.</summary>
		/// <remarks>
		/// Inits a new BasicDocument with the given text contents and title.
		/// The text is tokenized using
		/// <see cref="BasicDocument{L}.Parse(string)"/>
		/// to populate the list of words
		/// ("" is used if text is null). If specified, a reference to the
		/// original text is also maintained so that the text() method returns the
		/// text given to this constructor. Returns a reference to this
		/// BasicDocument
		/// for convenience (so it's more like a constructor, but inherited).
		/// </remarks>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>(string text, string title, bool keepOriginalText)
		{
			Edu.Stanford.Nlp.Ling.BasicDocument<L> basicDocument = new Edu.Stanford.Nlp.Ling.BasicDocument<L>();
			// initializes the List of labels and sets the title
			basicDocument.SetTitle(title);
			// stores the original text as specified
			if (keepOriginalText)
			{
				basicDocument.originalText = text;
			}
			else
			{
				basicDocument.originalText = null;
			}
			// populates the words by parsing the text
			basicDocument.Parse(text == null ? string.Empty : text);
			return basicDocument;
		}

		/// <summary>Calls init(text,title,true)</summary>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>(string text, string title)
		{
			return Init(text, title, true);
		}

		/// <summary>Calls init(text,null,keepOriginalText)</summary>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>(string text, bool keepOriginalText)
		{
			return Init(text, null, keepOriginalText);
		}

		/// <summary>Calls init(text,null,true)</summary>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>(string text)
		{
			return Init(text, null, true);
		}

		/// <summary>Calls init((String)null,null,true)</summary>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>()
		{
			return Init((string)null, null, true);
		}

		/// <summary>Inits a new BasicDocument by reading in the text from the given Reader.</summary>
		/// <seealso cref="BasicDocument{L}.Init{L}(string, string, bool)"/>
		/// <exception cref="System.IO.IOException"/>
		public static Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<L>(Reader textReader, string title, bool keepOriginalText)
		{
			return Init(DocumentReader.ReadText(textReader), title, keepOriginalText);
		}

		/// <summary>Calls init(textReader,title,true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(Reader textReader, string title)
		{
			return Init(textReader, title, true);
		}

		/// <summary>Calls init(textReader,null,keepOriginalText)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(Reader textReader, bool keepOriginalText)
		{
			return Init(textReader, null, keepOriginalText);
		}

		/// <summary>Calls init(textReader,null,true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(Reader textReader)
		{
			return Init(textReader, null, true);
		}

		/// <summary>Inits a new BasicDocument by reading in the text from the given File.</summary>
		/// <seealso cref="BasicDocument{L}.Init{L}(string, string, bool)"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(File textFile, string title, bool keepOriginalText)
		{
			Reader @in = DocumentReader.GetReader(textFile);
			Edu.Stanford.Nlp.Ling.BasicDocument<L> bd = Init(@in, title, keepOriginalText);
			@in.Close();
			return bd;
		}

		/// <summary>Calls init(textFile,title,true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(File textFile, string title)
		{
			return Init(textFile, title, true);
		}

		/// <summary>Calls init(textFile,textFile.getCanonicalPath(),keepOriginalText)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(File textFile, bool keepOriginalText)
		{
			return Init(textFile, textFile.GetCanonicalPath(), keepOriginalText);
		}

		/// <summary>Calls init(textFile,textFile.getCanonicalPath(),true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(File textFile)
		{
			return Init(textFile, textFile.GetCanonicalPath(), true);
		}

		/// <summary>Constructs a new BasicDocument by reading in the text from the given URL.</summary>
		/// <seealso cref="BasicDocument{L}.Init{L}(string, string, bool)"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(URL textURL, string title, bool keepOriginalText)
		{
			return Init(DocumentReader.GetReader(textURL), title, keepOriginalText);
		}

		/// <summary>Calls init(textURL,title,true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(URL textURL, string title)
		{
			return Init(textURL, title, true);
		}

		/// <summary>Calls init(textURL,textFile.toExternalForm(),keepOriginalText)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(URL textURL, bool keepOriginalText)
		{
			return Init(textURL, textURL.ToExternalForm(), keepOriginalText);
		}

		/// <summary>Calls init(textURL,textURL.toExternalForm(),true)</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init(URL textURL)
		{
			return Init(textURL, textURL.ToExternalForm(), true);
		}

		/// <summary>Initializes a new BasicDocument with the given list of words and title.</summary>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<_T0>(IList<_T0> words, string title)
			where _T0 : Word
		{
			// initializes the List of labels and sets the title
			SetTitle(title);
			// no original text
			originalText = null;
			// adds all of the given words to the list maintained by this document
			Sharpen.Collections.AddAll(this, words);
			return (this);
		}

		/// <summary>Calls init(words,null)</summary>
		public virtual Edu.Stanford.Nlp.Ling.BasicDocument<L> Init<_T0>(IList<_T0> words)
			where _T0 : Word
		{
			return Init(words, null);
		}

		/// <summary>
		/// Tokenizes the given text to populate the list of words this Document
		/// represents.
		/// </summary>
		/// <remarks>
		/// Tokenizes the given text to populate the list of words this Document
		/// represents. The default implementation uses the current tokenizer and tokenizes
		/// the entirety of the text into words. Subclasses should override this method
		/// to parse documents in non-standard formats, and/or to pull the title of the
		/// document from the text. The given text may be empty ("") but will never
		/// be null. Subclasses may want to do additional processing and then just
		/// call super.parse.
		/// </remarks>
		/// <seealso cref="BasicDocument{L}.SetTokenizerFactory(Edu.Stanford.Nlp.Process.ITokenizerFactory{T})"/>
		protected internal virtual void Parse(string text)
		{
			ITokenizer<Word> toke = tokenizerFactory.GetTokenizer(new StringReader(text));
			Sharpen.Collections.AddAll(this, toke.Tokenize());
		}

		/// <summary>Returns <tt>this</tt> (the features are the list of words).</summary>
		public virtual ICollection<Word> AsFeatures()
		{
			return this;
		}

		/// <summary>
		/// Returns the first label for this Document, or null if none have been
		/// set.
		/// </summary>
		public virtual L Label()
		{
			return (labels.Count > 0) ? labels[0] : null;
		}

		/// <summary>Returns the complete List of labels for this Document.</summary>
		/// <remarks>
		/// Returns the complete List of labels for this Document.
		/// This is an empty collection if none have been set.
		/// </remarks>
		public virtual ICollection<L> Labels()
		{
			return labels;
		}

		/// <summary>
		/// Removes all currently assigned labels for this Document then adds
		/// the given label.
		/// </summary>
		/// <remarks>
		/// Removes all currently assigned labels for this Document then adds
		/// the given label.
		/// Calling <tt>setLabel(null)</tt> effectively clears all labels.
		/// </remarks>
		public virtual void SetLabel(L label)
		{
			labels.Clear();
			AddLabel(label);
		}

		/// <summary>
		/// Removes all currently assigned labels for this Document then adds all
		/// of the given labels.
		/// </summary>
		public virtual void SetLabels(ICollection<L> labels)
		{
			this.labels.Clear();
			if (labels != null)
			{
				Sharpen.Collections.AddAll(this.labels, labels);
			}
		}

		/// <summary>Adds the given label to the List of labels for this Document if it is not null.</summary>
		public virtual void AddLabel(L label)
		{
			if (label != null)
			{
				labels.Add(label);
			}
		}

		/// <summary>Returns the title of this document.</summary>
		/// <remarks>
		/// Returns the title of this document. The title may be empty ("") but will
		/// never be null.
		/// </remarks>
		public virtual string Title()
		{
			return (title);
		}

		/// <summary>Sets the title of this Document to the given title.</summary>
		/// <remarks>
		/// Sets the title of this Document to the given title. If the given title
		/// is null, sets the title to "".
		/// </remarks>
		public virtual void SetTitle(string title)
		{
			if (title == null)
			{
				this.title = string.Empty;
			}
			else
			{
				this.title = title;
			}
		}

		/// <summary>
		/// Returns the current TokenizerFactory used by
		/// <see cref="BasicDocument{L}.Parse(string)"/>
		/// .
		/// </summary>
		public virtual ITokenizerFactory<Word> TokenizerFactory()
		{
			return (tokenizerFactory);
		}

		/// <summary>
		/// Sets the tokenizerFactory to be used by
		/// <see cref="BasicDocument{L}.Parse(string)"/>
		/// .
		/// Set this tokenizer before calling one of the <tt>init</tt> methods
		/// because
		/// it will probably call parse. Note that the tokenizer can equivalently be
		/// passed in to the constructor.
		/// </summary>
		/// <seealso cref="BasicDocument{L}.BasicDocument(Edu.Stanford.Nlp.Process.ITokenizerFactory{T})"/>
		public virtual void SetTokenizerFactory(ITokenizerFactory<Word> tokenizerFactory)
		{
			this.tokenizerFactory = tokenizerFactory;
		}

		/// <summary>
		/// Returns a new empty BasicDocument with the same title, labels, and
		/// tokenizer as this Document.
		/// </summary>
		/// <remarks>
		/// Returns a new empty BasicDocument with the same title, labels, and
		/// tokenizer as this Document. This is useful when you want to make a
		/// new Document that's like the old document but
		/// can be filled with new text (e.g. if you're transforming
		/// the contents non-destructively).
		/// Subclasses that want to preserve extra state should
		/// override this method and add the extra state to the new document before
		/// returning it. The new BasicDocument is created by calling
		/// <tt>getClass().newInstance()</tt> so it should be of the correct subclass,
		/// and thus you should be able to cast it down and add extra meta data directly.
		/// Note however that in the event an Exception is thrown on instantiation
		/// (e.g. if your subclass doesn't have a public empty constructor--it should btw!)
		/// then a new <tt>BasicDocument</tt> is used instead. Thus if you want to be paranoid
		/// (or some would say "correct") you should check that your instance is of
		/// the correct sub-type as follows (this example assumes the subclass is called
		/// <tt>NumberedDocument</tt> and it has the additional <tt>number</tt>property):
		/// <pre>Document blankDocument=super.blankDocument();
		/// if(blankDocument instanceof NumberedDocument) {
		/// ((NumberedDocument)blankDocument).setNumber(getNumber());</pre>
		/// </remarks>
		public virtual IDocument<L, Word, OUT> BlankDocument<Out>()
		{
			Edu.Stanford.Nlp.Ling.BasicDocument<L> bd;
			// tries to instantiate by reflection, settles for direct instantiation
			try
			{
				bd = ErasureUtils.UncheckedCast<Edu.Stanford.Nlp.Ling.BasicDocument<L>>(System.Activator.CreateInstance(GetType()));
			}
			catch (Exception)
			{
				bd = new Edu.Stanford.Nlp.Ling.BasicDocument<L>();
			}
			// copies over basic meta-data
			bd.SetTitle(Title());
			bd.SetLabels(Labels());
			bd.SetTokenizerFactory(tokenizerFactory);
			// cast to the new output type
			return ErasureUtils.UncheckedCast<IDocument<L, Word, OUT>>(bd);
		}

		/// <summary>
		/// Returns the text originally used to construct this document, or null if
		/// there was no original text.
		/// </summary>
		public virtual string OriginalText()
		{
			return (originalText);
		}

		/// <summary>
		/// Returns a "pretty" version of the words in this Document suitable for
		/// display.
		/// </summary>
		/// <remarks>
		/// Returns a "pretty" version of the words in this Document suitable for
		/// display. The default implementation returns each of the words in
		/// this Document separated
		/// by spaces. Specifically, each element that implements
		/// <see cref="IHasWord"/>
		/// has its
		/// <see cref="IHasWord.Word()"/>
		/// printed, and other elements are skipped.
		/// Subclasses that maintain additional information may which to
		/// override this method.
		/// </remarks>
		public virtual string PresentableText()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Word cur in this)
			{
				if (sb.Length > 0)
				{
					sb.Append(' ');
				}
				sb.Append(cur.Word());
			}
			return (sb.ToString());
		}

		/// <summary>For internal debugging purposes only.</summary>
		/// <remarks>
		/// For internal debugging purposes only. Creates and tests various instances
		/// of BasicDocument.
		/// </remarks>
		public static void Main(string[] args)
		{
			try
			{
				PrintState(Edu.Stanford.Nlp.Ling.BasicDocument.Init("this is the text", "this is the title [String]", true));
				PrintState(Edu.Stanford.Nlp.Ling.BasicDocument.Init(new StringReader("this is the text"), "this is the title [Reader]", true));
				File f = File.CreateTempFile("BasicDocumentTestFile", null);
				f.DeleteOnExit();
				PrintWriter @out = new PrintWriter(new FileWriter(f));
				@out.Print("this is the text");
				@out.Flush();
				@out.Close();
				PrintState(new Edu.Stanford.Nlp.Ling.BasicDocument<string>().Init(f, "this is the title [File]", true));
				PrintState(new Edu.Stanford.Nlp.Ling.BasicDocument<string>().Init(new URL("http://www.stanford.edu/~jsmarr/BasicDocumentTestFile.txt"), "this is the title [URL]", true));
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>For internal debugging purposes only.</summary>
		/// <remarks>
		/// For internal debugging purposes only.
		/// Prints the state of the given BasicDocument to stderr.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static void PrintState<L>(Edu.Stanford.Nlp.Ling.BasicDocument<L> bd)
		{
			log.Info("BasicDocument:");
			log.Info("\tTitle: " + bd.Title());
			log.Info("\tLabels: " + bd.Labels());
			log.Info("\tOriginalText: " + bd.OriginalText());
			log.Info("\tWords: " + bd);
			log.Info();
		}

		private const long serialVersionUID = -24171720584352262L;
	}
}
