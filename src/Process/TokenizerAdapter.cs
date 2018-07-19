using System;
using System.IO;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// This class adapts between a <code>java.io.StreamTokenizer</code>
	/// and a <code>edu.stanford.nlp.process.Tokenizer</code>.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2004/04/01</version>
	public class TokenizerAdapter : AbstractTokenizer<string>
	{
		protected internal readonly StreamTokenizer st;

		protected internal string eolString = "<EOL>";

		/// <summary>Create a new <code>TokenizerAdaptor</code>.</summary>
		/// <remarks>
		/// Create a new <code>TokenizerAdaptor</code>.  In general, it is
		/// recommended that the passed in <code>StreamTokenizer</code> should
		/// have had <code>resetSyntax()</code> done to it, so that numbers are
		/// returned as entered as tokens of type <code>String</code>, though this
		/// code will cope as best it can.
		/// </remarks>
		/// <param name="st">The internal <code>java.io.StreamTokenizer</code></param>
		public TokenizerAdapter(StreamTokenizer st)
		{
			this.st = st;
		}

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal override string GetNext()
		{
			try
			{
				int nextTok = st.NextToken();
				switch (nextTok)
				{
					case StreamTokenizer.TtEol:
					{
						return eolString;
					}

					case StreamTokenizer.TtEof:
					{
						return null;
					}

					case StreamTokenizer.TtWord:
					{
						return st.sval;
					}

					case StreamTokenizer.TtNumber:
					{
						return double.ToString(st.nval);
					}

					default:
					{
						char[] t = new char[] { (char)nextTok };
						// (array initialization)
						return new string(t);
					}
				}
			}
			catch (IOException)
			{
				// do nothing, return null
				return null;
			}
		}

		/// <summary>
		/// Set the <code>String</code> returned when the inner tokenizer
		/// returns an end-of-line token.
		/// </summary>
		/// <remarks>
		/// Set the <code>String</code> returned when the inner tokenizer
		/// returns an end-of-line token.  This will only happen if the
		/// inner tokenizer has been set to <code>eolIsSignificant(true)</code>.
		/// </remarks>
		/// <param name="eolString">
		/// The String used to represent eol.  It is not allowed
		/// to be <code>null</code> (which would confuse line ends and file end)
		/// </param>
		public virtual void SetEolString(string eolString)
		{
			if (eolString == null)
			{
				throw new ArgumentException("eolString cannot be null");
			}
			this.eolString = eolString;
		}

		/// <summary>
		/// Say whether the <code>String</code> is the end-of-line token for
		/// this tokenizer.
		/// </summary>
		/// <param name="str">The String being tested</param>
		/// <returns>Whether it is the end-of-line token</returns>
		public virtual bool IsEol(string str)
		{
			return eolString.Equals(str);
		}
	}
}
