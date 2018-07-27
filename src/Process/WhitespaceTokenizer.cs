using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// A WhitespaceTokenizer is a tokenizer that splits on and discards only
	/// whitespace characters.
	/// </summary>
	/// <remarks>
	/// A WhitespaceTokenizer is a tokenizer that splits on and discards only
	/// whitespace characters.
	/// This implementation can return Word, CoreLabel or other LexedToken objects. It has a parameter
	/// for whether to make EOL a token or whether to treat EOL characters as whitespace.
	/// If an EOL is a token, the class returns it as a Word with String value "\n".
	/// <i>Implementation note:</i> This was rewritten in Apr 2006 to discard the old StreamTokenizer-based
	/// implementation and to replace it with a Unicode compliant JFlex-based version.
	/// This tokenizer treats as Whitespace almost exactly the same characters deemed Whitespace by the
	/// Java function
	/// <see cref="char.IsWhiteSpace(int)">isWhitespace</see>
	/// . That is, a whitespace
	/// is a Unicode SPACE_SEPARATOR, LINE_SEPARATOR or PARAGRAPH_SEPARATOR, or one of the control characters
	/// U+0009-U+000D or U+001C-U+001F <i>except</i> the non-breaking space characters. The one addition is
	/// to also allow U+0085 as a line ending character, for compatibility with certain IBM systems.
	/// For including "spaces" in tokens, it is recommended that you represent them as the non-break space
	/// character U+00A0.
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	public class WhitespaceTokenizer<T> : AbstractTokenizer<T>
		where T : IHasWord
	{
		private WhitespaceLexer lexer;

		private readonly bool eolIsSignificant;

		/// <summary>A factory which vends WhitespaceTokenizers.</summary>
		/// <author>Christopher Manning</author>
		[System.Serializable]
		public class WhitespaceTokenizerFactory<T> : ITokenizerFactory<T>
			where T : IHasWord
		{
			private const long serialVersionUID = -5438594683910349897L;

			private bool tokenizeNLs;

			private readonly ILexedTokenFactory<T> factory;

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
				return new WhitespaceTokenizer.WhitespaceTokenizerFactory<Word>(new WordTokenFactory(), false);
			}

			public WhitespaceTokenizerFactory(ILexedTokenFactory<T> factory)
				: this(factory, false)
			{
			}

			public WhitespaceTokenizerFactory(ILexedTokenFactory<T> factory, string options)
			{
				this.factory = factory;
				Properties prop = StringUtils.StringToProperties(options);
				this.tokenizeNLs = PropertiesUtils.GetBool(prop, "tokenizeNLs", false);
			}

			public WhitespaceTokenizerFactory(ILexedTokenFactory<T> factory, bool tokenizeNLs)
			{
				this.factory = factory;
				this.tokenizeNLs = tokenizeNLs;
			}

			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r)
			{
				return new WhitespaceTokenizer<T>(factory, r, tokenizeNLs);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r, string extraOptions)
			{
				Properties prop = StringUtils.StringToProperties(extraOptions);
				bool tokenizeNewlines = PropertiesUtils.GetBool(prop, "tokenizeNLs", this.tokenizeNLs);
				return new WhitespaceTokenizer<T>(factory, r, tokenizeNewlines);
			}

			public virtual void SetOptions(string options)
			{
				Properties prop = StringUtils.StringToProperties(options);
				tokenizeNLs = PropertiesUtils.GetBool(prop, "tokenizeNLs", tokenizeNLs);
			}
		}

		// end class WhitespaceTokenizerFactory
		public static WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel> NewCoreLabelTokenizerFactory(string options)
		{
			return new WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory(), options);
		}

		public static WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel> NewCoreLabelTokenizerFactory()
		{
			return new WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory());
		}

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal override T GetNext()
		{
			if (lexer == null)
			{
				return null;
			}
			try
			{
				T token = (T)lexer.Next();
				while (token != null && token.Word().Equals(WhitespaceLexer.Newline))
				{
					if (eolIsSignificant)
					{
						return token;
					}
					else
					{
						token = (T)lexer.Next();
					}
				}
				return token;
			}
			catch (IOException)
			{
				return null;
			}
		}

		/// <summary>Constructs a new WhitespaceTokenizer.</summary>
		/// <param name="r">The Reader that is its source.</param>
		/// <param name="eolIsSignificant">Whether eol tokens should be returned.</param>
		public WhitespaceTokenizer(ILexedTokenFactory factory, Reader r, bool eolIsSignificant)
		{
			this.eolIsSignificant = eolIsSignificant;
			// The conditional below is perhaps currently needed in LexicalizedParser, since
			// it passes in a null arg while doing type-checking for sentence escaping
			// but StreamTokenizer barfs on that.  But maybe shouldn't be here.
			if (r != null)
			{
				lexer = new WhitespaceLexer(r, factory);
			}
		}

		public static WhitespaceTokenizer<CoreLabel> NewCoreLabelWhitespaceTokenizer(Reader r)
		{
			return new WhitespaceTokenizer<CoreLabel>(new CoreLabelTokenFactory(), r, false);
		}

		public static WhitespaceTokenizer<CoreLabel> NewCoreLabelWhitespaceTokenizer(Reader r, bool tokenizeNLs)
		{
			return new WhitespaceTokenizer<CoreLabel>(new CoreLabelTokenFactory(), r, tokenizeNLs);
		}

		public static WhitespaceTokenizer<Word> NewWordWhitespaceTokenizer(Reader r)
		{
			return NewWordWhitespaceTokenizer(r, false);
		}

		public static WhitespaceTokenizer<Word> NewWordWhitespaceTokenizer(Reader r, bool eolIsSignificant)
		{
			return new WhitespaceTokenizer<Word>(new WordTokenFactory(), r, eolIsSignificant);
		}

		/* ----
		* Sets the source of this Tokenizer to be the Reader r.
		
		private void setSource(Reader r) {
		lexer = new WhitespaceLexer(r);
		}
		---- */
		public static ITokenizerFactory<Word> Factory()
		{
			return new WhitespaceTokenizer.WhitespaceTokenizerFactory<Word>(new WordTokenFactory(), false);
		}

		public static ITokenizerFactory<Word> Factory(bool eolIsSignificant)
		{
			return new WhitespaceTokenizer.WhitespaceTokenizerFactory<Word>(new WordTokenFactory(), eolIsSignificant);
		}

		/// <summary>Reads a file from the argument and prints its tokens one per line.</summary>
		/// <remarks>
		/// Reads a file from the argument and prints its tokens one per line.
		/// This is mainly as a testing aid, but it can also be quite useful
		/// standalone to turn a corpus into a one token per line file of tokens.
		/// Usage:
		/// <c>java edu.stanford.nlp.process.WhitespaceTokenizer filename</c>
		/// </remarks>
		/// <param name="args">Command line arguments</param>
		/// <exception cref="System.IO.IOException">If can't open files, etc.</exception>
		public static void Main(string[] args)
		{
			bool eolIsSignificant = (args.Length > 0 && args[0].Equals("-cr"));
			Reader reader = ((args.Length > 0 && !args[args.Length - 1].Equals("-cr")) ? new InputStreamReader(new FileInputStream(args[args.Length - 1]), "UTF-8") : new InputStreamReader(Runtime.@in, "UTF-8"));
			WhitespaceTokenizer<Word> tokenizer = new WhitespaceTokenizer<Word>(new WordTokenFactory(), reader, eolIsSignificant);
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, "UTF-8"), true);
			while (tokenizer.MoveNext())
			{
				Word w = tokenizer.Current;
				if (w.Value().Equals(WhitespaceLexer.Newline))
				{
					pw.Println("***CR***");
				}
				else
				{
					pw.Println(w);
				}
			}
		}
	}
}
