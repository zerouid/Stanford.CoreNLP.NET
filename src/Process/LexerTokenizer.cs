using System;
using System.IO;
using Edu.Stanford.Nlp.IO;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// An implementation of
	/// <see cref="ITokenizer{T}"/>
	/// designed to work with
	/// <see cref="Edu.Stanford.Nlp.IO.ILexer"/>
	/// implementing classes.  Throw in a
	/// <see cref="Edu.Stanford.Nlp.IO.ILexer"/>
	/// on
	/// construction and you get a
	/// <see cref="ITokenizer{T}"/>
	/// .
	/// </summary>
	/// <author>Roger Levy</author>
	public class LexerTokenizer : AbstractTokenizer<string>
	{
		private readonly ILexer lexer;

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal override string GetNext()
		{
			string token = null;
			try
			{
				int a = LexerConstants.Ignore;
				while (a == LexerConstants.Ignore)
				{
					a = lexer.Yylex();
				}
				// skip tokens to be ignored
				if (a != lexer.GetYYEOF())
				{
					token = lexer.Yytext();
				}
			}
			catch (IOException)
			{
			}
			// else token remains null
			// do nothing, return null
			return token;
		}

		/// <summary>
		/// Constructs a tokenizer from a
		/// <see cref="Edu.Stanford.Nlp.IO.ILexer"/>
		/// .
		/// </summary>
		public LexerTokenizer(ILexer l)
		{
			if (l == null)
			{
				throw new ArgumentException("You can't make a Tokenizer out of a null Lexer!");
			}
			else
			{
				this.lexer = l;
			}
		}

		/// <summary>
		/// Constructs a tokenizer from a
		/// <see cref="Edu.Stanford.Nlp.IO.ILexer"/>
		/// and makes a
		/// <see cref="Java.IO.Reader"/>
		/// the active input stream for the tokenizer.
		/// </summary>
		public LexerTokenizer(ILexer l, Reader r)
			: this(l)
		{
			try
			{
				l.Yyreset(r);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e.Message);
			}
			GetNext();
		}

		/// <summary>For testing only.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			ITokenizer<string> t = new Edu.Stanford.Nlp.Process.LexerTokenizer(new JFlexDummyLexer((Reader)null), new BufferedReader(new FileReader(args[0])));
			while (t.MoveNext())
			{
				System.Console.Out.WriteLine("token " + t.Current);
			}
		}
	}
}
