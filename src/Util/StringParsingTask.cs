using System;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// An instantiation of this abstract class parses a <code>String</code> and
	/// returns an object of type <code>E</code>.
	/// </summary>
	/// <remarks>
	/// An instantiation of this abstract class parses a <code>String</code> and
	/// returns an object of type <code>E</code>.  It's called a
	/// <code>StringParsingTask</code> (rather than <code>StringParser</code>)
	/// because a new instance is constructed for each <code>String</code> to be
	/// parsed.  We do this to be thread-safe: methods in
	/// <code>StringParsingTask</code> share state information (e.g. current
	/// string index) via instance variables.
	/// </remarks>
	/// <author>Bill MacCartney</author>
	public abstract class StringParsingTask<E>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.StringParsingTask));

		protected internal string s;

		protected internal int index = 0;

		protected internal bool isEOF = false;

		/// <summary>
		/// Constructs a new <code>StringParsingTask</code> from the specified
		/// <code>String</code>.
		/// </summary>
		/// <remarks>
		/// Constructs a new <code>StringParsingTask</code> from the specified
		/// <code>String</code>.  Derived class constructors should be sure to
		/// call <code>super(s)</code>!
		/// </remarks>
		public StringParsingTask(string s)
		{
			// This class represents a parser working on a specific string.  We
			// construct from a specific string in order 
			// true if we tried to read past end
			this.s = s;
			index = 0;
		}

		/// <summary>
		/// Parses the <code>String</code> associated with this
		/// <code>StringParsingTask</code> and returns a object of type
		/// <code>E</code>.
		/// </summary>
		public abstract E Parse();

		// ---------------------------------------------------------------------
		/// <summary>
		/// Reads characters until
		/// <see cref="StringParsingTask{E}.IsWhiteSpace(char)">isWhiteSpace(ch)</see>
		/// or
		/// <see cref="StringParsingTask{E}.IsPunct(char)">isPunct(ch)</see>
		/// or
		/// <see cref="StringParsingTask{E}.IsEOF()"/>
		/// .  You may need
		/// to override the definition of
		/// <see cref="StringParsingTask{E}.IsPunct(char)">isPunct(ch)</see>
		/// to
		/// get this to work right.
		/// </summary>
		protected internal virtual string ReadName()
		{
			ReadWhiteSpace();
			StringBuilder sb = new StringBuilder();
			char ch = Read();
			while (!IsWhiteSpace(ch) && !IsPunct(ch) && !isEOF)
			{
				sb.Append(ch);
				ch = Read();
			}
			Unread();
			// log.info("Read text: ["+sb+"]");
			return string.Intern(sb.ToString());
		}

		protected internal virtual string ReadJavaIdentifier()
		{
			ReadWhiteSpace();
			StringBuilder sb = new StringBuilder();
			char ch = Read();
			if (char.IsJavaIdentifierStart(ch) && !isEOF)
			{
				sb.Append(ch);
				ch = Read();
				while (char.IsJavaIdentifierPart(ch) && !isEOF)
				{
					sb.Append(ch);
					ch = Read();
				}
			}
			Unread();
			// log.info("Read text: ["+sb+"]");
			return string.Intern(sb.ToString());
		}

		// .....................................................................
		protected internal virtual void ReadLeftParen()
		{
			// System.out.println("Read left.");
			ReadWhiteSpace();
			char ch = Read();
			if (!IsLeftParen(ch))
			{
				throw new StringParsingTask.ParserException("Expected left paren!");
			}
		}

		protected internal virtual void ReadRightParen()
		{
			// System.out.println("Read right.");
			ReadWhiteSpace();
			char ch = Read();
			if (!IsRightParen(ch))
			{
				throw new StringParsingTask.ParserException("Expected right paren!");
			}
		}

		protected internal virtual void ReadDot()
		{
			ReadWhiteSpace();
			if (IsDot(Peek()))
			{
				Read();
			}
		}

		protected internal virtual void ReadWhiteSpace()
		{
			char ch = Read();
			while (IsWhiteSpace(ch) && !IsEOF())
			{
				ch = Read();
			}
			Unread();
		}

		// .....................................................................
		protected internal virtual char Read()
		{
			if (index >= s.Length || index < 0)
			{
				isEOF = true;
				return ' ';
			}
			// arbitrary
			return s[index++];
		}

		protected internal virtual void Unread()
		{
			index--;
		}

		protected internal virtual char Peek()
		{
			char ch = Read();
			Unread();
			return ch;
		}

		// -----------------------------------------------------------------------
		protected internal virtual bool IsEOF()
		{
			return isEOF;
		}

		protected internal virtual bool IsWhiteSpace(char ch)
		{
			return (ch == ' ' || ch == '\t' || ch == '\f' || ch == '\r' || ch == '\n');
		}

		protected internal virtual bool IsPunct(char ch)
		{
			return IsLeftParen(ch) || IsRightParen(ch);
		}

		protected internal virtual bool IsLeftParen(char ch)
		{
			return ch == '(';
		}

		protected internal virtual bool IsRightParen(char ch)
		{
			return ch == ')';
		}

		protected internal virtual bool IsDot(char ch)
		{
			return ch == '.';
		}

		[System.Serializable]
		public class ParserException : Exception
		{
			private const long serialVersionUID = 1L;

			public ParserException(Exception e)
				: base(e)
			{
			}

			public ParserException(string message)
				: base(message)
			{
			}
			// exception class -------------------------------------------------------
		}
	}
}
