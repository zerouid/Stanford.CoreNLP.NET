using Edu.Stanford.Nlp.Process;





namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>Basic mechanism for reading in Documents from various input sources.</summary>
	/// <remarks>
	/// Basic mechanism for reading in Documents from various input sources.
	/// This default implementation can read from strings, files, URLs, and
	/// InputStreams and can use a given Tokenizer to turn the text into words.
	/// When working with a new data format, make a new DocumentReader to parse it
	/// and then use it with the existing Document APIs (rather than having to make
	/// new Document classes). Use the protected class variables (in, tokenizer,
	/// keepOriginalText) to read text and create docs appropriately. Subclasses should
	/// ideally provide similar constructors to this class, though only the constructor
	/// that takes a Reader is required.
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - templatized</author>
	/// <?/>
	public class DocumentReader<L>
	{
		/// <summary>Reader used to read in document text.</summary>
		/// <remarks>
		/// Reader used to read in document text. In default implementation, this is
		/// guaranteed to be a BufferedReader (so cast down) but it's typed as
		/// Reader in case subclasses don't want it buffered for some reason.
		/// </remarks>
		protected internal BufferedReader @in;

		/// <summary>Tokenizer used to chop up document text into words.</summary>
		protected internal ITokenizerFactory<IHasWord> tokenizerFactory;

		/// <summary>Whether to keep source text in document along with tokenized words.</summary>
		protected internal bool keepOriginalText;

		/// <summary>Constructs a new DocumentReader without an initial input source.</summary>
		/// <remarks>
		/// Constructs a new DocumentReader without an initial input source.
		/// Must call
		/// <see cref="DocumentReader{L}.SetReader(Java.IO.Reader)"/>
		/// before trying to read any documents.
		/// Uses a PTBTokenizer and keeps original text.
		/// </remarks>
		public DocumentReader()
			: this(null)
		{
		}

		/// <summary>Constructs a new DocumentReader using a PTBTokenizerFactory and keeps the original text.</summary>
		/// <param name="in">The Reader</param>
		public DocumentReader(Reader @in)
			: this(@in, PTBTokenizer.PTBTokenizerFactory.NewTokenizerFactory(), true)
		{
		}

		/// <summary>
		/// Constructs a new DocumentReader that will read text from the given
		/// Reader and tokenize it into words using the given Tokenizer.
		/// </summary>
		/// <remarks>
		/// Constructs a new DocumentReader that will read text from the given
		/// Reader and tokenize it into words using the given Tokenizer. The default
		/// implementation will internally buffer the reader if it is not already
		/// buffered, so there is no need to pre-wrap the reader with a BufferedReader.
		/// This class provides many <tt>getReader</tt> methods for conviniently
		/// reading from many input sources.
		/// </remarks>
		public DocumentReader(Reader @in, ITokenizerFactory<IHasWord> tokenizerFactory, bool keepOriginalText)
		{
			if (@in != null)
			{
				SetReader(@in);
			}
			SetTokenizerFactory(tokenizerFactory);
			this.keepOriginalText = keepOriginalText;
		}

		/// <summary>Returns the reader for the text input source of this DocumentReader.</summary>
		public virtual Reader GetReader()
		{
			return @in;
		}

		/// <summary>Sets the reader from which to read and create documents.</summary>
		/// <remarks>
		/// Sets the reader from which to read and create documents.
		/// Default implementation automatically buffers the Reader if it's not
		/// already buffered. Subclasses that don't want buffering may want to override
		/// this method to simply set the global <tt>in</tt> directly.
		/// </remarks>
		public virtual void SetReader(Reader @in)
		{
			this.@in = GetBufferedReader(@in);
		}

		/// <summary>Returns the tokenizer used to chop up text into words for the documents.</summary>
		public virtual ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return (tokenizerFactory);
		}

		/// <summary>Sets the tokenizer used to chop up text into words for the documents.</summary>
		public virtual void SetTokenizerFactory<_T0>(ITokenizerFactory<_T0> tokenizerFactory)
			where _T0 : IHasWord
		{
			this.tokenizerFactory = tokenizerFactory;
		}

		/// <summary>Returns whether created documents will store their source text along with tokenized words.</summary>
		public virtual bool GetKeepOriginalText()
		{
			return (keepOriginalText);
		}

		/// <summary>Sets whether created documents should store their source text along with tokenized words.</summary>
		public virtual void SetKeepOriginalText(bool keepOriginalText)
		{
			this.keepOriginalText = keepOriginalText;
		}

		/// <summary>
		/// Reads the next document's worth of text from the reader and turns it into
		/// a Document.
		/// </summary>
		/// <remarks>
		/// Reads the next document's worth of text from the reader and turns it into
		/// a Document. Default implementation calls
		/// <see cref="DocumentReader{L}.ReadNextDocumentText()"/>
		/// and passes it to
		/// <see cref="DocumentReader{L}.ParseDocumentText(string)"/>
		/// to create the document.
		/// Subclasses may wish to override either or both of those methods to handle
		/// custom formats of document collections and individual documents
		/// respectively. This method can also be overridden in its entirety to
		/// provide custom reading and construction of documents from input text.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual BasicDocument<L> ReadDocument()
		{
			string text = ReadNextDocumentText();
			if (text == null)
			{
				return (null);
			}
			return ParseDocumentText(text);
		}

		/// <summary>Reads the next document's worth of text from the reader.</summary>
		/// <remarks>
		/// Reads the next document's worth of text from the reader. Default
		/// implementation reads all the text. Subclasses wishing to read multiple
		/// documents from a single input source should read until the next document
		/// delimiter and return the text so far. Returns null if there is no more
		/// text to be read.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		protected internal virtual string ReadNextDocumentText()
		{
			return ReadText(@in);
		}

		/// <summary>Creates a new Document for the given text.</summary>
		/// <remarks>
		/// Creates a new Document for the given text. Default implementation tokenizes
		/// the text using the tokenizer provided during construction and sticks the words
		/// in a new BasicDocument. The text is also stored as the original text in
		/// the BasicDocument if keepOriginalText was set in the constructor. Subclasses
		/// may wish to extract additional information from the text and/or return another
		/// document subclass with additional meta-data.
		/// </remarks>
		protected internal virtual BasicDocument<L> ParseDocumentText(string text)
		{
			new BasicDocument<L>();
			return BasicDocument.Init(text, keepOriginalText);
		}

		/// <summary>
		/// Wraps the given Reader in a BufferedReader or returns it directly if it
		/// is already a BufferedReader.
		/// </summary>
		/// <remarks>
		/// Wraps the given Reader in a BufferedReader or returns it directly if it
		/// is already a BufferedReader. Subclasses should use this method before
		/// reading from <tt>in</tt> for efficiency and/or to read entire lines at
		/// a time. Note that this should only be done once per reader because when
		/// you read from a buffered reader, it reads more than necessary and stores
		/// the rest, so if you then throw that buffered reader out and get a new one
		/// for the original reader, text will be missing. In the default DocumentReader
		/// text, the Reader passed in at construction is wrapped in a buffered reader
		/// so you can just cast <tt>in</tt> down to a BufferedReader without calling
		/// this method.
		/// </remarks>
		public static BufferedReader GetBufferedReader(Reader @in)
		{
			if (@in == null)
			{
				return (null);
			}
			if (!(@in is BufferedReader))
			{
				@in = new BufferedReader(@in);
			}
			return (BufferedReader)@in;
		}

		/// <summary>Returns everything that can be read from the given Reader as a String.</summary>
		/// <remarks>
		/// Returns everything that can be read from the given Reader as a String.
		/// Returns null if the given Reader is null.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static string ReadText(Reader @in)
		{
			// returns null if the reader is null
			if (@in == null)
			{
				return (null);
			}
			// ensures the reader is buffered
			BufferedReader br = GetBufferedReader(@in);
			// reads all the chars into a buffer
			StringBuilder sb = new StringBuilder(16000);
			// make biggish
			int c;
			while ((c = br.Read()) >= 0)
			{
				sb.Append((char)c);
			}
			return sb.ToString();
		}

		/// <summary>Returns a Reader that reads in the given text.</summary>
		public static Reader GetReader(string text)
		{
			return (new StringReader(text));
		}

		/// <summary>Returns a Reader that reads in the given file.</summary>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static Reader GetReader(File file)
		{
			return (new FileReader(file));
		}

		/// <summary>Returns a Reader that reads in the given URL.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static Reader GetReader(URL url)
		{
			return (GetReader(url.OpenStream()));
		}

		/// <summary>Returns a Reader that reads in the given InputStream.</summary>
		public static Reader GetReader(InputStream @in)
		{
			return (new InputStreamReader(@in));
		}
	}
}
