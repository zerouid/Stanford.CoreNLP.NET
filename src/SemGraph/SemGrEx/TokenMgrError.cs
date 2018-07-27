/* Generated By:JavaCC: Do not edit this line. TokenMgrError.java Version 5.0 */
/* JavaCCOptions: */
using System;
using System.Text;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>Token Manager Error.</summary>
	[System.Serializable]
	internal class TokenMgrError : Exception
	{
		/// <summary>The version identifier for this Serializable class.</summary>
		/// <remarks>
		/// The version identifier for this Serializable class.
		/// Increment only if the <i>serialized</i> form of the
		/// class changes.
		/// </remarks>
		private const long serialVersionUID = 1L;

		/// <summary>Lexical error occurred.</summary>
		internal const int LexicalError = 0;

		/// <summary>An attempt was made to create a second instance of a static token manager.</summary>
		internal const int StaticLexerError = 1;

		/// <summary>Tried to change to an invalid lexical state.</summary>
		internal const int InvalidLexicalState = 2;

		/// <summary>Detected (and bailed out of) an infinite loop in the token manager.</summary>
		internal const int LoopDetected = 3;

		/// <summary>Indicates the reason why the exception is thrown.</summary>
		/// <remarks>
		/// Indicates the reason why the exception is thrown. It will have
		/// one of the above 4 values.
		/// </remarks>
		internal int errorCode;

		/*
		* Ordinals for various reasons why an Error of this type can be thrown.
		*/
		/// <summary>
		/// Replaces unprintable characters by their escaped (or unicode escaped)
		/// equivalents in the given string
		/// </summary>
		protected internal static string AddEscapes(string str)
		{
			StringBuilder retval = new StringBuilder();
			char ch;
			for (int i = 0; i < str.Length; i++)
			{
				switch (str[i])
				{
					case 0:
					{
						continue;
					}

					case '\b':
					{
						retval.Append("\\b");
						continue;
					}

					case '\t':
					{
						retval.Append("\\t");
						continue;
					}

					case '\n':
					{
						retval.Append("\\n");
						continue;
					}

					case '\f':
					{
						retval.Append("\\f");
						continue;
					}

					case '\r':
					{
						retval.Append("\\r");
						continue;
					}

					case '\"':
					{
						retval.Append("\\\"");
						continue;
					}

					case '\'':
					{
						retval.Append("\\\'");
						continue;
					}

					case '\\':
					{
						retval.Append("\\\\");
						continue;
					}

					default:
					{
						if ((ch = str[i]) < unchecked((int)(0x20)) || ch > unchecked((int)(0x7e)))
						{
							string s = "0000" + int.ToString(ch, 16);
							retval.Append("\\u" + Sharpen.Runtime.Substring(s, s.Length - 4, s.Length));
						}
						else
						{
							retval.Append(ch);
						}
						continue;
					}
				}
			}
			return retval.ToString();
		}

		/// <summary>
		/// Returns a detailed message for the Error when it is thrown by the
		/// token manager to indicate a lexical error.
		/// </summary>
		/// <remarks>
		/// Returns a detailed message for the Error when it is thrown by the
		/// token manager to indicate a lexical error.
		/// Parameters :
		/// EOFSeen     : indicates if EOF caused the lexical error
		/// curLexState : lexical state in which this error occurred
		/// errorLine   : line number when the error occurred
		/// errorColumn : column number when the error occurred
		/// errorAfter  : prefix that was seen before this error occurred
		/// curchar     : the offending character
		/// Note: You can customize the lexical error message by modifying this method.
		/// </remarks>
		protected internal static string LexicalError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar)
		{
			return ("Lexical error at line " + errorLine + ", column " + errorColumn + ".  Encountered: " + (EOFSeen ? "<EOF> " : ("\"" + AddEscapes(curChar.ToString()) + "\"") + " (" + (int)curChar + "), ") + "after : \"" + AddEscapes(errorAfter) + "\""
				);
		}

		/// <summary>You can also modify the body of this method to customize your error messages.</summary>
		/// <remarks>
		/// You can also modify the body of this method to customize your error messages.
		/// For example, cases like LOOP_DETECTED and INVALID_LEXICAL_STATE are not
		/// of end-users concern, so you can return something like :
		/// "Internal Error : Please file a bug report .... "
		/// from this method for such cases in the release version of your parser.
		/// </remarks>
		public override string Message
		{
			get
			{
				return base.Message;
			}
		}

		/// <summary>No arg constructor.</summary>
		public TokenMgrError()
		{
		}

		/// <summary>Constructor with message and reason.</summary>
		public TokenMgrError(string message, int reason)
			: base(message)
		{
			/*
			* Constructors of various flavors follow.
			*/
			errorCode = reason;
		}

		/// <summary>Full Constructor.</summary>
		public TokenMgrError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar, int reason)
			: this(LexicalError(EOFSeen, lexState, errorLine, errorColumn, errorAfter, curChar), reason)
		{
		}
	}
}
