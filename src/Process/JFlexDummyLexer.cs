/* The following code was generated by JFlex 1.6.1 */
using System;
using System.IO;
using Edu.Stanford.Nlp.IO;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// This class is a scanner generated by
	/// <a href="http://www.jflex.de/">JFlex</a> 1.6.1
	/// from the specification file <tt>/Users/manning/git/javanlp/projects/core/src/edu/stanford/nlp/process/JFlexDummyLexer.flex</tt>
	/// </summary>
	internal class JFlexDummyLexer : ILexer
	{
		/// <summary>This character denotes the end of file</summary>
		public const int Yyeof = -1;

		/// <summary>initial size of the lookahead buffer</summary>
		private const int ZzBuffersize = 16384;

		/// <summary>lexical states</summary>
		public const int Yyinitial = 0;

		/// <summary>
		/// ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
		/// ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
		/// at the beginning of a line
		/// l is of the form l = 2*k, k a non negative integer
		/// </summary>
		private static readonly int[] ZzLexstate = new int[] { 0, 0 };

		/// <summary>Translates characters to character classes</summary>
		private const string ZzCmapPacked = "\xb\x0\x5\x1\x16\x0\x1\x1\x90\x0\x1\x1\u1fa2\x0\x2\x1\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\uffff\x0\udfe6\x0";

		/// <summary>Translates characters to character classes</summary>
		private static readonly char[] ZzCmap = ZzUnpackCMap(ZzCmapPacked);

		/// <summary>Translates DFA states to action switch labels.</summary>
		private static readonly int[] ZzAction = ZzUnpackAction();

		private const string ZzActionPacked0 = "\x1\x0\x1\x1\x1\x2";

		private static int[] ZzUnpackAction()
		{
			int[] result = new int[3];
			int offset = 0;
			offset = ZzUnpackAction(ZzActionPacked0, offset, result);
			return result;
		}

		private static int ZzUnpackAction(string packed, int offset, int[] result)
		{
			int i = 0;
			/* index in packed string  */
			int j = offset;
			/* index in unpacked array */
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		/// <summary>Translates a state to a row index in the transition table</summary>
		private static readonly int[] ZzRowmap = ZzUnpackRowMap();

		private const string ZzRowmapPacked0 = "\x0\x0\x0\x2\x0\x4";

		private static int[] ZzUnpackRowMap()
		{
			int[] result = new int[3];
			int offset = 0;
			offset = ZzUnpackRowMap(ZzRowmapPacked0, offset, result);
			return result;
		}

		private static int ZzUnpackRowMap(string packed, int offset, int[] result)
		{
			int i = 0;
			/* index in packed string  */
			int j = offset;
			/* index in unpacked array */
			int l = packed.Length;
			while (i < l)
			{
				int high = packed[i++] << 16;
				result[j++] = high | packed[i++];
			}
			return j;
		}

		/// <summary>The transition table of the DFA</summary>
		private static readonly int[] ZzTrans = ZzUnpackTrans();

		private const string ZzTransPacked0 = "\x1\x2\x1\x3\x1\x2\x2\x0\x1\x3";

		private static int[] ZzUnpackTrans()
		{
			int[] result = new int[6];
			int offset = 0;
			offset = ZzUnpackTrans(ZzTransPacked0, offset, result);
			return result;
		}

		private static int ZzUnpackTrans(string packed, int offset, int[] result)
		{
			int i = 0;
			/* index in packed string  */
			int j = offset;
			/* index in unpacked array */
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				value--;
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		private const int ZzUnknownError = 0;

		private const int ZzNoMatch = 1;

		private const int ZzPushback2big = 2;

		private static readonly string[] ZzErrorMsg = new string[] { "Unknown internal scanner error", "Error: could not match input", "Error: pushback value was too large" };

		/// <summary>ZZ_ATTRIBUTE[aState] contains the attributes of state <code>aState</code></summary>
		private static readonly int[] ZzAttribute = ZzUnpackAttribute();

		private const string ZzAttributePacked0 = "\x1\x0\x2\x1";

		/* error codes */
		/* error messages for the codes above */
		private static int[] ZzUnpackAttribute()
		{
			int[] result = new int[3];
			int offset = 0;
			offset = ZzUnpackAttribute(ZzAttributePacked0, offset, result);
			return result;
		}

		private static int ZzUnpackAttribute(string packed, int offset, int[] result)
		{
			int i = 0;
			/* index in packed string  */
			int j = offset;
			/* index in unpacked array */
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		/// <summary>the input device</summary>
		private Reader zzReader;

		/// <summary>the current state of the DFA</summary>
		private int zzState;

		/// <summary>the current lexical state</summary>
		private int zzLexicalState = Yyinitial;

		/// <summary>
		/// this buffer contains the current text to be matched and is
		/// the source of the yytext() string
		/// </summary>
		private char[] zzBuffer = new char[ZzBuffersize];

		/// <summary>the textposition at the last accepting state</summary>
		private int zzMarkedPos;

		/// <summary>the current text position in the buffer</summary>
		private int zzCurrentPos;

		/// <summary>startRead marks the beginning of the yytext() string in the buffer</summary>
		private int zzStartRead;

		/// <summary>
		/// endRead marks the last character in the buffer, that has been read
		/// from input
		/// </summary>
		private int zzEndRead;

		/// <summary>number of newlines encountered up to the start of the matched text</summary>
		private int yyline;

		/// <summary>the number of characters up to the start of the matched text</summary>
		private int yychar;

		/// <summary>
		/// the number of characters from the last newline up to the start of the
		/// matched text
		/// </summary>
		private int yycolumn;

		/// <summary>zzAtBOL == true <=> the scanner is currently at the beginning of a line</summary>
		private bool zzAtBOL = true;

		/// <summary>zzAtEOF == true <=> the scanner is at the EOF</summary>
		private bool zzAtEOF;

		/// <summary>denotes if the user-EOF-code has already been executed</summary>
		private bool zzEOFDone;

		/// <summary>The number of occupied positions in zzBuffer beyond zzEndRead.</summary>
		/// <remarks>
		/// The number of occupied positions in zzBuffer beyond zzEndRead.
		/// When a lead/high surrogate has been read from the input stream
		/// into the final zzBuffer position, this will have a value of 1;
		/// otherwise, it will have a value of 0.
		/// </remarks>
		private int zzFinalHighSurrogate = 0;

		/* user code: */
		public virtual void PushBack(int n)
		{
			Yypushback(n);
		}

		public virtual int GetYYEOF()
		{
			return Yyeof;
		}

		/// <summary>Creates a new scanner</summary>
		/// <param name="in">the java.io.Reader to read input from.</param>
		internal JFlexDummyLexer(Reader @in)
		{
			this.zzReader = @in;
		}

		/// <summary>Unpacks the compressed character translation table.</summary>
		/// <param name="packed">the packed character translation table</param>
		/// <returns>the unpacked character translation table</returns>
		private static char[] ZzUnpackCMap(string packed)
		{
			char[] map = new char[unchecked((int)(0x110000))];
			int i = 0;
			/* index in packed string  */
			int j = 0;
			/* index in unpacked array */
			while (i < 50)
			{
				int count = packed[i++];
				char value = packed[i++];
				do
				{
					map[j++] = value;
				}
				while (--count > 0);
			}
			return map;
		}

		/// <summary>Refills the input buffer.</summary>
		/// <returns><code>false</code>, iff there was new input.</returns>
		/// <exception>
		/// java.io.IOException
		/// if any I/O-Error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		private bool ZzRefill()
		{
			/* first: make room (if you can) */
			if (zzStartRead > 0)
			{
				zzEndRead += zzFinalHighSurrogate;
				zzFinalHighSurrogate = 0;
				System.Array.Copy(zzBuffer, zzStartRead, zzBuffer, 0, zzEndRead - zzStartRead);
				/* translate stored positions */
				zzEndRead -= zzStartRead;
				zzCurrentPos -= zzStartRead;
				zzMarkedPos -= zzStartRead;
				zzStartRead = 0;
			}
			/* is the buffer big enough? */
			if (zzCurrentPos >= zzBuffer.Length - zzFinalHighSurrogate)
			{
				/* if not: blow it up */
				char[] newBuffer = new char[zzBuffer.Length * 2];
				System.Array.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
				zzBuffer = newBuffer;
				zzEndRead += zzFinalHighSurrogate;
				zzFinalHighSurrogate = 0;
			}
			/* fill the buffer with new input */
			int requested = zzBuffer.Length - zzEndRead;
			int numRead = zzReader.Read(zzBuffer, zzEndRead, requested);
			/* not supposed to occur according to specification of java.io.Reader */
			if (numRead == 0)
			{
				throw new IOException("Reader returned 0 characters. See JFlex examples for workaround.");
			}
			if (numRead > 0)
			{
				zzEndRead += numRead;
				/* If numRead == requested, we might have requested to few chars to
				encode a full Unicode character. We assume that a Reader would
				otherwise never return half characters. */
				if (numRead == requested)
				{
					if (char.IsHighSurrogate(zzBuffer[zzEndRead - 1]))
					{
						--zzEndRead;
						zzFinalHighSurrogate = 1;
					}
				}
				/* potentially more input available */
				return false;
			}
			/* numRead < 0 ==> end of stream */
			return true;
		}

		/// <summary>Closes the input stream.</summary>
		/// <exception cref="System.IO.IOException"/>
		public void Yyclose()
		{
			zzAtEOF = true;
			/* indicate end of file */
			zzEndRead = zzStartRead;
			/* invalidate buffer    */
			if (zzReader != null)
			{
				zzReader.Close();
			}
		}

		/// <summary>Resets the scanner to read from a new input stream.</summary>
		/// <remarks>
		/// Resets the scanner to read from a new input stream.
		/// Does not close the old reader.
		/// All internal variables are reset, the old input stream
		/// <b>cannot</b> be reused (internal buffer is discarded and lost).
		/// Lexical state is set to <tt>ZZ_INITIAL</tt>.
		/// Internal scan buffer is resized down to its initial length, if it has grown.
		/// </remarks>
		/// <param name="reader">the new input stream</param>
		public void Yyreset(Reader reader)
		{
			zzReader = reader;
			zzAtBOL = true;
			zzAtEOF = false;
			zzEOFDone = false;
			zzEndRead = zzStartRead = 0;
			zzCurrentPos = zzMarkedPos = 0;
			zzFinalHighSurrogate = 0;
			yyline = yychar = yycolumn = 0;
			zzLexicalState = Yyinitial;
			if (zzBuffer.Length > ZzBuffersize)
			{
				zzBuffer = new char[ZzBuffersize];
			}
		}

		/// <summary>Returns the current lexical state.</summary>
		public int Yystate()
		{
			return zzLexicalState;
		}

		/// <summary>Enters a new lexical state</summary>
		/// <param name="newState">the new lexical state</param>
		public void Yybegin(int newState)
		{
			zzLexicalState = newState;
		}

		/// <summary>Returns the text matched by the current regular expression.</summary>
		public string Yytext()
		{
			return new string(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
		}

		/// <summary>
		/// Returns the character at position <tt>pos</tt> from the
		/// matched text.
		/// </summary>
		/// <remarks>
		/// Returns the character at position <tt>pos</tt> from the
		/// matched text.
		/// It is equivalent to yytext().charAt(pos), but faster
		/// </remarks>
		/// <param name="pos">
		/// the position of the character to fetch.
		/// A value from 0 to yylength()-1.
		/// </param>
		/// <returns>the character at position pos</returns>
		public char Yycharat(int pos)
		{
			return zzBuffer[zzStartRead + pos];
		}

		/// <summary>Returns the length of the matched text region.</summary>
		public int Yylength()
		{
			return zzMarkedPos - zzStartRead;
		}

		/// <summary>Reports an error that occured while scanning.</summary>
		/// <remarks>
		/// Reports an error that occured while scanning.
		/// In a wellformed scanner (no or only correct usage of
		/// yypushback(int) and a match-all fallback rule) this method
		/// will only be called with things that "Can't Possibly Happen".
		/// If this method is called, something is seriously wrong
		/// (e.g. a JFlex bug producing a faulty scanner etc.).
		/// Usual syntax/scanner level error handling should be done
		/// in error fallback rules.
		/// </remarks>
		/// <param name="errorCode">the code of the errormessage to display</param>
		private void ZzScanError(int errorCode)
		{
			string message;
			try
			{
				message = ZzErrorMsg[errorCode];
			}
			catch (IndexOutOfRangeException)
			{
				message = ZzErrorMsg[ZzUnknownError];
			}
			throw new Exception(message);
		}

		/// <summary>Pushes the specified amount of characters back into the input stream.</summary>
		/// <remarks>
		/// Pushes the specified amount of characters back into the input stream.
		/// They will be read again by then next call of the scanning method
		/// </remarks>
		/// <param name="number">
		/// the number of characters to be read again.
		/// This number must not be greater than yylength()!
		/// </param>
		public virtual void Yypushback(int number)
		{
			if (number > Yylength())
			{
				ZzScanError(ZzPushback2big);
			}
			zzMarkedPos -= number;
		}

		/// <summary>
		/// Resumes scanning until the next regular expression is matched,
		/// the end of input is encountered or an I/O-Error occurs.
		/// </summary>
		/// <returns>the next token</returns>
		/// <exception>
		/// java.io.IOException
		/// if any I/O-Error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"/>
		public virtual int Yylex()
		{
			int zzInput;
			int zzAction;
			// cached fields:
			int zzCurrentPosL;
			int zzMarkedPosL;
			int zzEndReadL = zzEndRead;
			char[] zzBufferL = zzBuffer;
			char[] zzCMapL = ZzCmap;
			int[] zzTransL = ZzTrans;
			int[] zzRowMapL = ZzRowmap;
			int[] zzAttrL = ZzAttribute;
			while (true)
			{
				zzMarkedPosL = zzMarkedPos;
				zzAction = -1;
				zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;
				zzState = ZzLexstate[zzLexicalState];
				// set up zzAction for empty match case:
				int zzAttributes = zzAttrL[zzState];
				if ((zzAttributes & 1) == 1)
				{
					zzAction = zzState;
				}
				while (true)
				{
					if (zzCurrentPosL < zzEndReadL)
					{
						zzInput = char.CodePointAt(zzBufferL, zzCurrentPosL, zzEndReadL);
						zzCurrentPosL += char.CharCount(zzInput);
					}
					else
					{
						if (zzAtEOF)
						{
							zzInput = Yyeof;
							goto zzForAction_break;
						}
						else
						{
							// store back cached positions
							zzCurrentPos = zzCurrentPosL;
							zzMarkedPos = zzMarkedPosL;
							bool eof = ZzRefill();
							// get translated positions and possibly new buffer
							zzCurrentPosL = zzCurrentPos;
							zzMarkedPosL = zzMarkedPos;
							zzBufferL = zzBuffer;
							zzEndReadL = zzEndRead;
							if (eof)
							{
								zzInput = Yyeof;
								goto zzForAction_break;
							}
							else
							{
								zzInput = char.CodePointAt(zzBufferL, zzCurrentPosL, zzEndReadL);
								zzCurrentPosL += char.CharCount(zzInput);
							}
						}
					}
					int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
					if (zzNext == -1)
					{
						goto zzForAction_break;
					}
					zzState = zzNext;
					zzAttributes = zzAttrL[zzState];
					if ((zzAttributes & 1) == 1)
					{
						zzAction = zzState;
						zzMarkedPosL = zzCurrentPosL;
						if ((zzAttributes & 8) == 8)
						{
							goto zzForAction_break;
						}
					}
				}
zzForAction_break: ;
				// store back cached position
				zzMarkedPos = zzMarkedPosL;
				if (zzInput == Yyeof && zzStartRead == zzCurrentPos)
				{
					zzAtEOF = true;
					return Yyeof;
				}
				else
				{
					switch (zzAction < 0 ? zzAction : ZzAction[zzAction])
					{
						case 1:
						{
							return LexerConstants.Accept;
						}

						case 3:
						{
							break;
						}

						case 2:
						{
							goto case 4;
						}

						case 4:
						{
							break;
						}

						default:
						{
							ZzScanError(ZzNoMatch);
							break;
						}
					}
				}
			}
		}
	}
}
