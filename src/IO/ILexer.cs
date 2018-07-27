


namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// A Lexer interface to be used with
	/// <see cref="Edu.Stanford.Nlp.Process.LexerTokenizer"/>
	/// .  You can put a
	/// <see cref="Java.IO.Reader"/>
	/// inside
	/// a Lexer with the
	/// <see cref="Yyreset(Java.IO.Reader)"/>
	/// method.  An easy way to build classes implementing this
	/// interface is with JFlex (http://jflex.de).  Just make sure to include the following in the
	/// JFlex source file
	/// In the <i>Options and Macros</i> section of the source file, include
	/// %class JFlexDummyLexer<br />
	/// %standalone<br />
	/// %unicode<br />
	/// %int<br />
	/// <br />
	/// %implements edu.stanford.nlp.io.Lexer<br />
	/// <br />
	/// %{<br />
	/// public void pushBack(int n) {<br />
	/// yypushback(n);<br />
	/// }<br />
	/// <br />
	/// public int getYYEOF() {<br />
	/// return YYEOF;<br />
	/// }<br />
	/// %}<br />
	/// Alternatively, you can customize your own lexer and get lots of
	/// flexibility out.
	/// </summary>
	/// <author>Roger Levy</author>
	public interface ILexer
	{
		/// <summary>
		/// Gets the next token from input and returns an integer value
		/// signalling what to do with the token.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		int Yylex();

		/// <summary>returns the matched input text region</summary>
		string Yytext();

		/// <summary>
		/// Pushes back
		/// <paramref name="length"/>
		/// character positions in the
		/// lexer.  Conventionally used to push back exactly one token.
		/// </summary>
		void PushBack(int length);

		/// <summary>returns value for YYEOF</summary>
		int GetYYEOF();

		/// <summary>
		/// put a
		/// <see cref="Java.IO.Reader"/>
		/// inside the Lexer.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		void Yyreset(Reader r);
	}

	public static class LexerConstants
	{
		public const int Accept = 1;

		public const int Ignore = 0;
	}
}
