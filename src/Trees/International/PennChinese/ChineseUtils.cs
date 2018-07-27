using System;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// This class contains a few String constants and
	/// static methods for dealing with Chinese text.
	/// </summary>
	/// <remarks>
	/// This class contains a few String constants and
	/// static methods for dealing with Chinese text.
	/// <p/>
	/// <b>Warning:</b> The code contains a version that uses codePoint methods
	/// to handle full Unicode.  But it seems to tickle some bugs in
	/// Sun's JDK 1.5.  It works correctly with JDK 1.6+.  By default it is
	/// enabled. The version that only handles BMP characters can be used by editing the code.  The
	/// latter prints a warning message if it sees a high-surrogate character.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class ChineseUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseUtils));

		/// <summary>Whether to only support BMP character normalization.</summary>
		/// <remarks>
		/// Whether to only support BMP character normalization.
		/// If set to true, this is more limited, but avoids bugs in JDK 1.5.
		/// </remarks>
		private const bool OnlyBmp = false;

		public const string Onewhite = "[\\s\\p{Zs}]";

		public const string White = Onewhite + "*";

		public const string Whiteplus = Onewhite + "+";

		public const string Numbers = "[\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341]";

		public const string MidDotRegexStr = "[\u00B7\u0387\u2022\u2024\u2027\u2219\u22C5\u30FB]";

		public const int Leave = 0;

		public const int Ascii = 1;

		public const int Normalize = 1;

		public const int Fullwidth = 2;

		public const int Delete = 3;

		public const int DeleteExceptBetweenAscii = 4;

		public const int MaxLegal = 4;

		private ChineseUtils()
		{
		}

		// These are good Unicode whitespace regexes for any language!
		// Chinese numbers 1-10
		// List of characters similar to \u00B7 listed in the Unicode 5.0 manual
		// These are the constants for the normalize method
		// Unicode normalization moves to low
		// private int[] puaChars = { 0xE005 };
		// private int[] uniChars = { 0x42B5 };
		// not instantiable
		public static bool IsNumber(char c)
		{
			return (StringUtils.Matches(c.ToString(), Numbers) || char.IsDigit(c));
		}

		public static string Normalize(string @in)
		{
			return Normalize(@in, Fullwidth, Ascii);
		}

		public static string Normalize(string @in, int ascii, int spaceChar)
		{
			return Normalize(@in, ascii, spaceChar, Leave);
		}

		/// <summary>This will normalize a Unicode String in various ways.</summary>
		/// <remarks>
		/// This will normalize a Unicode String in various ways.  This routine
		/// correctly handles characters outside the basic multilingual plane.
		/// </remarks>
		/// <param name="in">The String to be normalized</param>
		/// <param name="ascii">
		/// For characters conceptually in the ASCII range of
		/// ! through ~ (U+0021 through U+007E or U+FF01 through U+FF5E),
		/// if this is ChineseUtils.LEAVE, then do nothing,
		/// if it is ASCII then map them from the Chinese Full Width range
		/// to ASCII values, and if it is FULLWIDTH then do the reverse.
		/// </param>
		/// <param name="spaceChar">
		/// For characters that satisfy Character.isSpaceChar(),
		/// if this is ChineseUtils.LEAVE, then do nothing,
		/// if it is ASCII then map them to the space character U+0020, and
		/// if it is FULLWIDTH then map them to U+3000.
		/// </param>
		/// <param name="midDot">
		/// For a set of 7 characters that are roughly middle dot characters,
		/// if this is ChineseUtils.LEAVE, then do nothing,
		/// if it is NORMALIZE then map them to the extended Latin character U+00B7, and
		/// if it is FULLWIDTH then map them to U+30FB.
		/// </param>
		/// <returns>The in String normalized according to the other arguments.</returns>
		public static string Normalize(string @in, int ascii, int spaceChar, int midDot)
		{
			if (ascii < 0 || ascii > MaxLegal || spaceChar < 0 || spaceChar > MaxLegal)
			{
				throw new ArgumentException("ChineseUtils: Unknown parameter option");
			}
			return NormalizeUnicode(@in, ascii, spaceChar, midDot);
		}

		private static string NormalizeBMP(string @in, int ascii, int spaceChar, int midDot)
		{
			StringBuilder @out = new StringBuilder();
			int len = @in.Length;
			for (int i = 0; i < len; i++)
			{
				char cp = @in[i];
				if (char.IsHighSurrogate(cp))
				{
					if (i + 1 < len)
					{
						log.Warn("ChineseUtils.normalize warning: non-BMP codepoint U+" + int.ToHexString(char.CodePointAt(@in, i)) + " in " + @in);
					}
					else
					{
						log.Warn("ChineseUtils.normalize warning: unmatched high surrogate character U+" + int.ToHexString(char.CodePointAt(@in, i)) + " in " + @in);
					}
				}
				Character.UnicodeBlock cub = Character.UnicodeBlock.Of(cp);
				if (cub == Character.UnicodeBlock.PrivateUseArea || cub == Character.UnicodeBlock.SupplementaryPrivateUseAreaA || cub == Character.UnicodeBlock.SupplementaryPrivateUseAreaB)
				{
					EncodingPrintWriter.Err.Println("ChineseUtils.normalize warning: private use area codepoint U+" + int.ToHexString(cp) + " in " + @in);
				}
				bool delete = false;
				switch (ascii)
				{
					case Leave:
					{
						break;
					}

					case Ascii:
					{
						if (cp >= '\uFF01' && cp <= '\uFF5E')
						{
							cp -= (char)(unchecked((int)(0xFF00)) - unchecked((int)(0x0020)));
						}
						break;
					}

					case Fullwidth:
					{
						if (cp >= '\u0021' && cp <= '\u007E')
						{
							cp += (char)(unchecked((int)(0xFF00)) - unchecked((int)(0x0020)));
						}
						break;
					}

					default:
					{
						throw new ArgumentException("ChineseUtils: Unsupported parameter option: ascii=" + ascii);
					}
				}
				switch (spaceChar)
				{
					case Leave:
					{
						break;
					}

					case Ascii:
					{
						if (char.IsSpaceChar(cp))
						{
							cp = ' ';
						}
						break;
					}

					case Fullwidth:
					{
						if (char.IsSpaceChar(cp))
						{
							cp = '\u3000';
						}
						break;
					}

					case Delete:
					{
						if (char.IsSpaceChar(cp))
						{
							delete = true;
						}
						break;
					}

					case DeleteExceptBetweenAscii:
					{
						char cpp = 0;
						if (i > 0)
						{
							cpp = @in[i - 1];
						}
						char cpn = 0;
						if (i < (len - 1))
						{
							cpn = @in[i + 1];
						}
						// EncodingPrintWriter.out.println("cp: " + cp + "; cpp: " + cpp + "cpn: " + cpn +
						//      "; isSpace: " + Character.isSpaceChar(cp) + "; isAsciiLHL: " + isAsciiLowHigh(cpp) +
						//      "; isAsciiLHR: " + isAsciiLowHigh(cpn), "UTF-8");
						if (char.IsSpaceChar(cp) && !(IsAsciiLowHigh(cpp) && IsAsciiLowHigh(cpn)))
						{
							delete = true;
						}
						break;
					}
				}
				switch (midDot)
				{
					case Leave:
					{
						break;
					}

					case Normalize:
					{
						if (IsMidDot(cp))
						{
							cp = '\u00B7';
						}
						break;
					}

					case Fullwidth:
					{
						if (IsMidDot(cp))
						{
							cp = '\u30FB';
						}
						break;
					}

					case Delete:
					{
						if (IsMidDot(cp))
						{
							delete = true;
						}
						break;
					}

					default:
					{
						throw new ArgumentException("ChineseUtils: Unsupported parameter option: midDot=" + midDot);
					}
				}
				if (!delete)
				{
					@out.Append(cp);
				}
			}
			// end for
			return @out.ToString();
		}

		private static string NormalizeUnicode(string @in, int ascii, int spaceChar, int midDot)
		{
			StringBuilder @out = new StringBuilder();
			int len = @in.Length;
			// Do it properly with codepoints, for non-BMP Unicode as well
			// int numCP = in.codePointCount(0, len);
			int cpp = 0;
			// previous codepoint
			for (int offset = 0; offset < len; offset += char.CharCount(cp))
			{
				// int offset = in.offsetByCodePoints(0, offset);
				cp = @in.CodePointAt(offset);
				Character.UnicodeBlock cub = Character.UnicodeBlock.Of(cp);
				if (cub == Character.UnicodeBlock.PrivateUseArea || cub == Character.UnicodeBlock.SupplementaryPrivateUseAreaA || cub == Character.UnicodeBlock.SupplementaryPrivateUseAreaB)
				{
					EncodingPrintWriter.Err.Println("ChineseUtils.normalize warning: private use area codepoint U+" + int.ToHexString(cp) + " in " + @in);
				}
				bool delete = false;
				switch (ascii)
				{
					case Leave:
					{
						break;
					}

					case Ascii:
					{
						if (cp >= '\uFF01' && cp <= '\uFF5E')
						{
							cp -= (unchecked((int)(0xFF00)) - unchecked((int)(0x0020)));
						}
						break;
					}

					case Fullwidth:
					{
						if (cp >= '\u0021' && cp <= '\u007E')
						{
							cp += (unchecked((int)(0xFF00)) - unchecked((int)(0x0020)));
						}
						break;
					}

					default:
					{
						throw new ArgumentException("ChineseUtils: Unsupported parameter option: ascii=" + ascii);
					}
				}
				switch (spaceChar)
				{
					case Leave:
					{
						break;
					}

					case Ascii:
					{
						if (char.IsSpaceChar(cp))
						{
							cp = ' ';
						}
						break;
					}

					case Fullwidth:
					{
						if (char.IsSpaceChar(cp))
						{
							cp = '\u3000';
						}
						break;
					}

					case Delete:
					{
						if (char.IsSpaceChar(cp))
						{
							delete = true;
						}
						break;
					}

					case DeleteExceptBetweenAscii:
					{
						int nextOffset = offset + char.CharCount(cp);
						int cpn = 0;
						if (nextOffset < len)
						{
							cpn = @in.CodePointAt(nextOffset);
						}
						if (char.IsSpaceChar(cp) && !(IsAsciiLowHigh(cpp) && IsAsciiLowHigh(cpn)))
						{
							delete = true;
						}
						break;
					}
				}
				switch (midDot)
				{
					case Leave:
					{
						break;
					}

					case Normalize:
					{
						if (IsMidDot(cp))
						{
							cp = '\u00B7';
						}
						break;
					}

					case Fullwidth:
					{
						if (IsMidDot(cp))
						{
							cp = '\u30FB';
						}
						break;
					}

					case Delete:
					{
						if (IsMidDot(cp))
						{
							delete = true;
						}
						break;
					}

					default:
					{
						throw new ArgumentException("ChineseUtils: Unsupported parameter option: midDot=" + midDot);
					}
				}
				if (!delete)
				{
					@out.AppendCodePoint(cp);
				}
				cpp = cp;
			}
			// end for
			return @out.ToString();
		}

		private static bool IsMidDot(int cp)
		{
			return cp == '\u00B7' || cp == '\u0387' || cp == '\u2022' || cp == '\u2024' || cp == '\u2027' || cp == '\u2219' || cp == '\u22C5' || cp == '\u30FB';
		}

		private static bool IsAsciiLowHigh(int cp)
		{
			return cp >= '\uFF01' && cp <= '\uFF5E' || cp >= '\u0021' && cp <= '\u007E';
		}

		/// <summary>Mainly for testing.</summary>
		/// <remarks>
		/// Mainly for testing.  Usage:
		/// <c>ChineseUtils ascii spaceChar word*</c>
		/// ascii and spaceChar are integers: 0 = leave, 1 = ascii, 2 = fullwidth.
		/// The words listed are then normalized and sent to stdout.
		/// If no words are given, the program reads from and normalizes stdin.
		/// Input is assumed to be in UTF-8.
		/// </remarks>
		/// <param name="args">Command line arguments as above</param>
		/// <exception cref="System.IO.IOException">If any problems accessing command-line files</exception>
		public static void Main(string[] args)
		{
			if (args.Length < 3)
			{
				log.Info("usage: ChineseUtils ascii space midDot word*");
				log.Info("  First 3 args are int flags; a filter or maps args as words; assumes UTF-8");
				return;
			}
			int i = System.Convert.ToInt32(args[0]);
			int j = System.Convert.ToInt32(args[1]);
			int midDot = System.Convert.ToInt32(args[2]);
			if (args.Length > 3)
			{
				for (int k = 3; k < args.Length; k++)
				{
					EncodingPrintWriter.Out.Println(Normalize(args[k], i, j, midDot));
				}
			}
			else
			{
				BufferedReader r = IOUtils.ReaderFromStdin("UTF-8");
				for (string line; (line = r.ReadLine()) != null; )
				{
					EncodingPrintWriter.Out.Println(Normalize(line, i, j, midDot));
				}
			}
		}

		private static readonly Pattern dateChars = Pattern.Compile("[\u5E74\u6708\u65E5]+");

		private static readonly Pattern dateCharsPlus = Pattern.Compile("[\u5E74\u6708\u65E5\u53f7]+");

		private static readonly Pattern numberChars = Pattern.Compile("[0-9\uff10-\uff19" + "\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4E5D\u5341" + "\u96F6\u3007\u767E\u5343\u4E07\u4ebf\u5169\u25cb\u25ef\u3021-\u3029\u3038-\u303A]+");

		private static readonly Pattern letterChars = Pattern.Compile("[A-Za-z\uFF21-\uFF3A\uFF41-\uFF5A]+");

		private static readonly Pattern periodChars = Pattern.Compile("[\ufe52\u2027\uff0e.\u70B9]+");

		private static readonly Pattern separatingPuncChars = Pattern.Compile("[]!\"(),;:<=>?\\[\\\\`{|}~^\u3001-\u3003\u3008-\u3011\u3014-\u301F\u3030" + "\uff3d\uff01\uff02\uff08\uff09\uff0c\uff1b\uff1a\uff1c\uff1d\uff1e\uff1f" + "\uff3b\uff3c\uff40\uff5b\uff5c\uff5d\uff5e\uff3e]+"
			);

		private static readonly Pattern ambiguousPuncChars = Pattern.Compile("[-#$%&'*+/@_\uff0d\uff03\uff04\uff05\uff06\uff07\uff0a\uff0b\uff0f\uff20\uff3f]+");

		private static readonly Pattern midDotPattern = Pattern.Compile(Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseUtils.MidDotRegexStr + "+");

		// year, month, day chars.  Sometime try adding \u53f7 and see if it helps...
		// year, month, day chars.  Adding \u53F7 and seeing if it helps...
		// number chars (Chinese and Western).
		// You get U+25CB circle masquerading as zero in mt data - or even in Sighan 2003 ctb
		// add U+25EF for good measure (larger geometric circle)
		// private static final Pattern numberChars = Pattern.compile("[0-9０-９" +
		//      "一二三四五六七八九十" +
		//      "零〇百千万亿兩○◯〡-〩〸-〺]");
		// A-Za-z, narrow and full width
		// two punctuation classes for Low and Ng style features.
		public static string ShapeOf(ICharSequence input, bool augmentedDateChars, bool useMidDotShape)
		{
			string shape;
			if (augmentedDateChars && dateCharsPlus.Matcher(input).Matches())
			{
				shape = "D";
			}
			else
			{
				if (input[0] == '第')
				{
					return "o";
				}
				else
				{
					// detect those Chinese ordinals!
					if (dateChars.Matcher(input).Matches())
					{
						shape = "D";
					}
					else
					{
						if (numberChars.Matcher(input).Matches())
						{
							shape = "N";
						}
						else
						{
							if (letterChars.Matcher(input).Matches())
							{
								shape = "L";
							}
							else
							{
								if (periodChars.Matcher(input).Matches())
								{
									shape = "P";
								}
								else
								{
									if (separatingPuncChars.Matcher(input).Matches())
									{
										shape = "S";
									}
									else
									{
										if (ambiguousPuncChars.Matcher(input).Matches())
										{
											shape = "A";
										}
										else
										{
											if (useMidDotShape && midDotPattern.Matcher(input).Matches())
											{
												shape = "M";
											}
											else
											{
												shape = "C";
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return shape;
		}
	}
}
