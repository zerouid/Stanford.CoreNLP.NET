using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Provides static methods which
	/// map any String to another String indicative of its "word shape" -- e.g.,
	/// whether capitalized, numeric, etc.
	/// </summary>
	/// <remarks>
	/// Provides static methods which
	/// map any String to another String indicative of its "word shape" -- e.g.,
	/// whether capitalized, numeric, etc.  Different implementations may
	/// implement quite different, normally language specific ideas of what
	/// word shapes are useful.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Dan Klein</author>
	public class WordShapeClassifier
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.WordShapeClassifier));

		public const int Nowordshape = -1;

		public const int Wordshapedan1 = 0;

		public const int Wordshapechris1 = 1;

		public const int Wordshapedan2 = 2;

		public const int Wordshapedan2uselc = 3;

		public const int Wordshapedan2bio = 4;

		public const int Wordshapedan2biouselc = 5;

		public const int Wordshapejenny1 = 6;

		public const int Wordshapejenny1uselc = 7;

		public const int Wordshapechris2 = 8;

		public const int Wordshapechris2uselc = 9;

		public const int Wordshapechris3 = 10;

		public const int Wordshapechris3uselc = 11;

		public const int Wordshapechris4 = 12;

		public const int Wordshapedigits = 13;

		public const int Wordshapechinese = 14;

		public const int Wordshapecluster1 = 15;

		private WordShapeClassifier()
		{
		}

		// TODO: put in a regexp for ordinals, fraction num/num and perhaps even 30-5/8
		// This class cannot be instantiated
		/// <summary>Look up a shaper by a short String name.</summary>
		/// <param name="name">
		/// Shaper name.  Known names have patterns along the lines of:
		/// dan[12](bio)?(UseLC)?, jenny1(useLC)?, chris[1234](useLC)?, cluster1.
		/// </param>
		/// <returns>An integer constant for the shaper</returns>
		public static int LookupShaper(string name)
		{
			if (name == null)
			{
				return Nowordshape;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(name, "dan1"))
				{
					return Wordshapedan1;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris1"))
					{
						return Wordshapechris1;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(name, "dan2"))
						{
							return Wordshapedan2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(name, "dan2useLC"))
							{
								return Wordshapedan2uselc;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(name, "dan2bio"))
								{
									return Wordshapedan2bio;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(name, "dan2bioUseLC"))
									{
										return Wordshapedan2biouselc;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(name, "jenny1"))
										{
											return Wordshapejenny1;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(name, "jenny1useLC"))
											{
												return Wordshapejenny1uselc;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris2"))
												{
													return Wordshapechris2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris2useLC"))
													{
														return Wordshapechris2uselc;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris3"))
														{
															return Wordshapechris3;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris3useLC"))
															{
																return Wordshapechris3uselc;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(name, "chris4"))
																{
																	return Wordshapechris4;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(name, "digits"))
																	{
																		return Wordshapedigits;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(name, "chinese"))
																		{
																			return Wordshapechinese;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(name, "cluster1"))
																			{
																				return Wordshapecluster1;
																			}
																			else
																			{
																				return Nowordshape;
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns true if the specified word shaper doesn't use
		/// known lower case words, even if a list of them is present.
		/// </summary>
		/// <remarks>
		/// Returns true if the specified word shaper doesn't use
		/// known lower case words, even if a list of them is present.
		/// This is used for backwards compatibility. It is suggested that
		/// new word shape functions are either passed a non-null list of
		/// lowercase words or not, depending on whether you want knownLC marking
		/// (if it is available in a shaper).  This is how chris4 works.
		/// </remarks>
		/// <param name="shape">One of the defined shape constants</param>
		/// <returns>
		/// true if the specified word shaper uses
		/// known lower case words.
		/// </returns>
		private static bool DontUseLC(int shape)
		{
			return shape == Wordshapedan2 || shape == Wordshapedan2bio || shape == Wordshapejenny1 || shape == Wordshapechris2 || shape == Wordshapechris3;
		}

		/// <summary>
		/// Specify the String and the int identifying which word shaper to
		/// use and this returns the result of using that wordshaper on the String.
		/// </summary>
		/// <param name="inStr">String to calculate word shape of</param>
		/// <param name="wordShaper">Constant for which shaping formula to use</param>
		/// <returns>The wordshape String</returns>
		public static string WordShape(string inStr, int wordShaper)
		{
			return WordShape(inStr, wordShaper, null);
		}

		/// <summary>
		/// Specify the string and the int identifying which word shaper to
		/// use and this returns the result of using that wordshaper on the String.
		/// </summary>
		/// <param name="inStr">String to calculate word shape of</param>
		/// <param name="wordShaper">Constant for which shaping formula to use</param>
		/// <param name="knownLCWords">
		/// A Collection of known lowercase words, which some shapers use
		/// to decide the class of capitalized words.
		/// <i>Note: while this code works with any Collection, you should
		/// provide a Set for decent performance.</i>  If this parameter is
		/// null or empty, then this option is not used (capitalized words
		/// are treated the same, regardless of whether the lowercased
		/// version of the String has been seen).
		/// </param>
		/// <returns>The wordshape String</returns>
		public static string WordShape(string inStr, int wordShaper, ICollection<string> knownLCWords)
		{
			// this first bit is for backwards compatibility with how things were first
			// implemented, where the word shaper name encodes whether to useLC.
			// If the shaper is in the old compatibility list, then a specified
			// list of knownLCwords is ignored
			if (knownLCWords != null && DontUseLC(wordShaper))
			{
				knownLCWords = null;
			}
			switch (wordShaper)
			{
				case Nowordshape:
				{
					return inStr;
				}

				case Wordshapedan1:
				{
					return WordShapeDan1(inStr);
				}

				case Wordshapechris1:
				{
					return WordShapeChris1(inStr);
				}

				case Wordshapedan2:
				{
					return WordShapeDan2(inStr, knownLCWords);
				}

				case Wordshapedan2uselc:
				{
					return WordShapeDan2(inStr, knownLCWords);
				}

				case Wordshapedan2bio:
				{
					return WordShapeDan2Bio(inStr, knownLCWords);
				}

				case Wordshapedan2biouselc:
				{
					return WordShapeDan2Bio(inStr, knownLCWords);
				}

				case Wordshapejenny1:
				{
					return WordShapeJenny1(inStr, knownLCWords);
				}

				case Wordshapejenny1uselc:
				{
					return WordShapeJenny1(inStr, knownLCWords);
				}

				case Wordshapechris2:
				{
					return WordShapeChris2(inStr, false, knownLCWords);
				}

				case Wordshapechris2uselc:
				{
					return WordShapeChris2(inStr, false, knownLCWords);
				}

				case Wordshapechris3:
				{
					return WordShapeChris2(inStr, true, knownLCWords);
				}

				case Wordshapechris3uselc:
				{
					return WordShapeChris2(inStr, true, knownLCWords);
				}

				case Wordshapechris4:
				{
					return WordShapeChris4(inStr, false, knownLCWords);
				}

				case Wordshapedigits:
				{
					return WordShapeDigits(inStr);
				}

				case Wordshapechinese:
				{
					return WordShapeChinese(inStr);
				}

				case Wordshapecluster1:
				{
					return WordShapeCluster1(inStr);
				}

				default:
				{
					throw new InvalidOperationException("Bad WordShapeClassifier");
				}
			}
		}

		/// <summary>
		/// A fairly basic 5-way classifier, that notes digits, and upper
		/// and lower case, mixed, and non-alphanumeric.
		/// </summary>
		/// <param name="s">String to find word shape of</param>
		/// <returns>Its word shape: a 5 way classification</returns>
		private static string WordShapeDan1(string s)
		{
			bool digit = true;
			bool upper = true;
			bool lower = true;
			bool mixed = true;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (!char.IsDigit(c))
				{
					digit = false;
				}
				if (!char.IsLowerCase(c))
				{
					lower = false;
				}
				if (!char.IsUpperCase(c))
				{
					upper = false;
				}
				if ((i == 0 && !char.IsUpperCase(c)) || (i >= 1 && !char.IsLowerCase(c)))
				{
					mixed = false;
				}
			}
			if (digit)
			{
				return "ALL-DIGITS";
			}
			if (upper)
			{
				return "ALL-UPPER";
			}
			if (lower)
			{
				return "ALL-LOWER";
			}
			if (mixed)
			{
				return "MIXED-CASE";
			}
			return "OTHER";
		}

		/// <summary>
		/// A fine-grained word shape classifier, that equivalence classes
		/// lower and upper case and digits, and collapses sequences of the
		/// same type, but keeps all punctuation, etc.
		/// </summary>
		/// <remarks>
		/// A fine-grained word shape classifier, that equivalence classes
		/// lower and upper case and digits, and collapses sequences of the
		/// same type, but keeps all punctuation, etc. <p>
		/// <i>Note:</i> We treat '_' as a lowercase letter, sort of like many
		/// programming languages.  We do this because we use '_' joining of
		/// tokens in some applications like RTE.
		/// </remarks>
		/// <param name="s">The String whose shape is to be returned</param>
		/// <param name="knownLCWords">
		/// If this is non-null and non-empty, mark words whose
		/// lower case form is found in the
		/// Collection of known lower case words
		/// </param>
		/// <returns>The word shape</returns>
		private static string WordShapeDan2(string s, ICollection<string> knownLCWords)
		{
			StringBuilder sb = new StringBuilder("WT-");
			char lastM = '~';
			bool nonLetters = false;
			int len = s.Length;
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				char m = c;
				if (char.IsDigit(c))
				{
					m = 'd';
				}
				else
				{
					if (char.IsLowerCase(c) || c == '_')
					{
						m = 'x';
					}
					else
					{
						if (char.IsUpperCase(c))
						{
							m = 'X';
						}
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				if (m != lastM)
				{
					sb.Append(m);
				}
				lastM = m;
			}
			if (len <= 3)
			{
				sb.Append(':').Append(len);
			}
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			// log.info("wordShapeDan2: " + s + " became " + sb);
			return sb.ToString();
		}

		private static string WordShapeJenny1(string s, ICollection<string> knownLCWords)
		{
			StringBuilder sb = new StringBuilder("WT-");
			char lastM = '~';
			bool nonLetters = false;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				char m = c;
				if (char.IsDigit(c))
				{
					m = 'd';
				}
				else
				{
					if (char.IsLowerCase(c))
					{
						m = 'x';
					}
					else
					{
						if (char.IsUpperCase(c))
						{
							m = 'X';
						}
					}
				}
				foreach (string gr in greek)
				{
					if (s.StartsWith(gr, i))
					{
						m = 'g';
						i = i + gr.Length - 1;
						//System.out.println(s + "  ::  " + s.substring(i+1));
						break;
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				if (m != lastM)
				{
					sb.Append(m);
				}
				lastM = m;
			}
			if (s.Length <= 3)
			{
				sb.Append(':').Append(s.Length);
			}
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			//System.out.println(s+" became "+sb);
			return sb.ToString();
		}

		/// <summary>
		/// Note: the optimizations in wordShapeChris2 would break if BOUNDARY_SIZE
		/// was greater than the shortest greek word, so valid values are: 0, 1, 2, 3.
		/// </summary>
		private const int BoundarySize = 2;

		/// <summary>
		/// This one picks up on Dan2 ideas, but seeks to make less distinctions
		/// mid sequence by sorting for long words, but to maintain extra
		/// distinctions for short words.
		/// </summary>
		/// <remarks>
		/// This one picks up on Dan2 ideas, but seeks to make less distinctions
		/// mid sequence by sorting for long words, but to maintain extra
		/// distinctions for short words. It exactly preserves the character shape
		/// of the first and last 2 (i.e., BOUNDARY_SIZE) characters and then
		/// will record shapes that occur between them (perhaps only if they are
		/// different)
		/// </remarks>
		/// <param name="s">The String to find the word shape of</param>
		/// <param name="omitIfInBoundary">
		/// If true, character classes present in the
		/// first or last two (i.e., BOUNDARY_SIZE) letters
		/// of the word are not also registered
		/// as classes that appear in the middle of the word.
		/// </param>
		/// <param name="knownLCWords">
		/// If non-null and non-empty, tag with a "k" suffix words
		/// that are in this list when lowercased (representing
		/// that the word is "known" as a lowercase word).
		/// </param>
		/// <returns>A word shape for the word.</returns>
		private static string WordShapeChris2(string s, bool omitIfInBoundary, ICollection<string> knownLCWords)
		{
			int len = s.Length;
			if (len <= BoundarySize * 2)
			{
				return WordShapeChris2Short(s, len, knownLCWords);
			}
			else
			{
				return WordShapeChris2Long(s, omitIfInBoundary, len, knownLCWords);
			}
		}

		// Do the simple case of words <= BOUNDARY_SIZE * 2 (i.e., 4) with only 1 object allocation!
		private static string WordShapeChris2Short(string s, int len, ICollection<string> knownLCWords)
		{
			int sbLen = (knownLCWords != null) ? len + 1 : len;
			// markKnownLC makes String 1 longer
			StringBuilder sb = new StringBuilder(sbLen);
			bool nonLetters = false;
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				char m = c;
				if (char.IsDigit(c))
				{
					m = 'd';
				}
				else
				{
					if (char.IsLowerCase(c))
					{
						m = 'x';
					}
					else
					{
						if (char.IsUpperCase(c) || char.IsTitleCase(c))
						{
							m = 'X';
						}
					}
				}
				foreach (string gr in greek)
				{
					if (s.StartsWith(gr, i))
					{
						m = 'g';
						//System.out.println(s + "  ::  " + s.substring(i+1));
						i += gr.Length - 1;
						// System.out.println("Position skips to " + i);
						break;
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				sb.Append(m);
			}
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			// System.out.println(s + " became " + sb);
			return sb.ToString();
		}

		// introduce sizes and optional allocation to reduce memory churn demands;
		// this class could blow a lot of memory if used in a tight loop,
		// as the naive version allocates lots of kind of heavyweight objects
		// endSB should be of length BOUNDARY_SIZE
		// sb is maximally of size s.length() + 1, but is usually (much) shorter. The +1 might happen if markKnownLC is true and it applies
		// boundSet is maximally of size BOUNDARY_SIZE * 2 (and is often smaller)
		// seenSet is maximally of size s.length() - BOUNDARY_SIZE * 2, but might often be of size <= 4. But it has no initial size allocation
		// But we want the initial size to be greater than BOUNDARY_SIZE * 2 * (4/3) since the default loadfactor is 3/4.
		// That is, of size 6, which become 8, since HashMaps are powers of 2.  Still, it's half the size
		private static string WordShapeChris2Long(string s, bool omitIfInBoundary, int len, ICollection<string> knownLCWords)
		{
			char[] beginChars = new char[BoundarySize];
			char[] endChars = new char[BoundarySize];
			int beginUpto = 0;
			int endUpto = 0;
			ICollection<char> seenSet = new TreeSet<char>();
			// TreeSet guarantees stable ordering; has no size parameter
			bool nonLetters = false;
			for (int i = 0; i < len; i++)
			{
				int iIncr = 0;
				char c = s[i];
				char m = c;
				if (char.IsDigit(c))
				{
					m = 'd';
				}
				else
				{
					if (char.IsLowerCase(c))
					{
						m = 'x';
					}
					else
					{
						if (char.IsUpperCase(c) || char.IsTitleCase(c))
						{
							m = 'X';
						}
					}
				}
				foreach (string gr in greek)
				{
					if (s.StartsWith(gr, i))
					{
						m = 'g';
						//System.out.println(s + "  ::  " + s.substring(i+1));
						iIncr = gr.Length - 1;
						break;
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				if (i < BoundarySize)
				{
					beginChars[beginUpto++] = m;
				}
				else
				{
					if (i < len - BoundarySize)
					{
						seenSet.Add(char.ValueOf(m));
					}
					else
					{
						endChars[endUpto++] = m;
					}
				}
				i += iIncr;
			}
			// System.out.println("Position skips to " + i);
			// Calculate size. This may be an upperbound, but is often correct
			int sbSize = beginUpto + endUpto + seenSet.Count;
			if (knownLCWords != null)
			{
				sbSize++;
			}
			StringBuilder sb = new StringBuilder(sbSize);
			// put in the beginning chars
			sb.Append(beginChars, 0, beginUpto);
			// put in the stored ones sorted
			if (omitIfInBoundary)
			{
				foreach (char chr in seenSet)
				{
					char ch = chr;
					bool insert = true;
					for (int i_1 = 0; i_1 < beginUpto; i_1++)
					{
						if (beginChars[i_1] == ch)
						{
							insert = false;
							break;
						}
					}
					for (int i_2 = 0; i_2 < endUpto; i_2++)
					{
						if (endChars[i_2] == ch)
						{
							insert = false;
							break;
						}
					}
					if (insert)
					{
						sb.Append(ch);
					}
				}
			}
			else
			{
				foreach (char chr in seenSet)
				{
					sb.Append(chr);
				}
			}
			// and add end ones
			sb.Append(endChars, 0, endUpto);
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			// System.out.println(s + " became " + sb);
			return sb.ToString();
		}

		private static char Chris4equivalenceClass(char c)
		{
			int type = char.GetType(c);
			if (char.IsDigit(c) || type == char.LetterNumber || type == char.OtherNumber || "一二三四五六七八九十零〇百千万亿兩○◯".IndexOf(c) > 0)
			{
				// include Chinese numbers that are just of unicode type OTHER_LETTER (and a couple of round symbols often used (by mistake?) for zeroes)
				return 'd';
			}
			else
			{
				if (c == '第')
				{
					return 'o';
				}
				else
				{
					// detect those Chinese ordinals!
					if (c == '年' || c == '月' || c == '日')
					{
						// || c == '号') {
						return 'D';
					}
					else
					{
						// Chinese date characters.
						if (char.IsLowerCase(c))
						{
							return 'x';
						}
						else
						{
							if (char.IsUpperCase(c) || char.IsTitleCase(c))
							{
								return 'X';
							}
							else
							{
								if (char.IsWhiteSpace(c) || char.IsSpaceChar(c))
								{
									return 's';
								}
								else
								{
									if (type == char.OtherLetter)
									{
										return 'c';
									}
									else
									{
										// Chinese characters, etc. without case
										if (type == char.CurrencySymbol)
										{
											return '$';
										}
										else
										{
											if (type == char.MathSymbol)
											{
												return '+';
											}
											else
											{
												if (type == char.OtherSymbol || c == '|')
												{
													return '|';
												}
												else
												{
													if (type == char.StartPunctuation)
													{
														return '(';
													}
													else
													{
														if (type == char.EndPunctuation)
														{
															return ')';
														}
														else
														{
															if (type == char.InitialQuotePunctuation)
															{
																return '`';
															}
															else
															{
																if (type == char.FinalQuotePunctuation || c == '\'')
																{
																	return '\'';
																}
																else
																{
																	if (c == '%')
																	{
																		return '%';
																	}
																	else
																	{
																		if (type == char.OtherPunctuation)
																		{
																			return '.';
																		}
																		else
																		{
																			if (type == char.ConnectorPunctuation)
																			{
																				return '_';
																			}
																			else
																			{
																				if (type == char.DashPunctuation)
																				{
																					return '-';
																				}
																				else
																				{
																					return 'q';
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static string WordShapeChris4(string s)
		{
			return WordShapeChris4(s, false, null);
		}

		/// <summary>
		/// This one picks up on Dan2 ideas, but seeks to make less distinctions
		/// mid sequence by sorting for long words, but to maintain extra
		/// distinctions for short words, by always recording the class of the
		/// first and last two characters of the word.
		/// </summary>
		/// <remarks>
		/// This one picks up on Dan2 ideas, but seeks to make less distinctions
		/// mid sequence by sorting for long words, but to maintain extra
		/// distinctions for short words, by always recording the class of the
		/// first and last two characters of the word.
		/// Compared to chris2 on which it is based,
		/// it uses more Unicode classes, and so collapses things like
		/// punctuation more, and might work better with real unicode.
		/// </remarks>
		/// <param name="s">The String to find the word shape of</param>
		/// <param name="omitIfInBoundary">
		/// If true, character classes present in the
		/// first or last two (i.e., BOUNDARY_SIZE) letters
		/// of the word are not also registered
		/// as classes that appear in the middle of the word.
		/// </param>
		/// <param name="knownLCWords">
		/// If non-null and non-empty, tag with a "k" suffix words
		/// that are in this list when lowercased (representing
		/// that the word is "known" as a lowercase word).
		/// </param>
		/// <returns>A word shape for the word.</returns>
		private static string WordShapeChris4(string s, bool omitIfInBoundary, ICollection<string> knownLCWords)
		{
			int len = s.Length;
			if (len <= BoundarySize * 2)
			{
				return WordShapeChris4Short(s, len, knownLCWords);
			}
			else
			{
				return WordShapeChris4Long(s, omitIfInBoundary, len, knownLCWords);
			}
		}

		// Do the simple case of words <= BOUNDARY_SIZE * 2 (i.e., 4) with only 1 object allocation!
		private static string WordShapeChris4Short(string s, int len, ICollection<string> knownLCWords)
		{
			int sbLen = (knownLCWords != null) ? len + 1 : len;
			// markKnownLC makes String 1 longer
			StringBuilder sb = new StringBuilder(sbLen);
			bool nonLetters = false;
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				char m = Chris4equivalenceClass(c);
				foreach (string gr in greek)
				{
					if (s.StartsWith(gr, i))
					{
						m = 'g';
						//System.out.println(s + "  ::  " + s.substring(i+1));
						i += gr.Length - 1;
						// System.out.println("Position skips to " + i);
						break;
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				sb.Append(m);
			}
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			// System.out.println(s + " became " + sb);
			return sb.ToString();
		}

		private static string WordShapeChris4Long(string s, bool omitIfInBoundary, int len, ICollection<string> knownLCWords)
		{
			StringBuilder sb = new StringBuilder(s.Length + 1);
			StringBuilder endSB = new StringBuilder(BoundarySize);
			ICollection<char> boundSet = Generics.NewHashSet(BoundarySize * 2);
			ICollection<char> seenSet = new TreeSet<char>();
			// TreeSet guarantees stable ordering
			bool nonLetters = false;
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				char m = Chris4equivalenceClass(c);
				int iIncr = 0;
				foreach (string gr in greek)
				{
					if (s.StartsWith(gr, i))
					{
						m = 'g';
						iIncr = gr.Length - 1;
						//System.out.println(s + "  ::  " + s.substring(i+1));
						break;
					}
				}
				if (m != 'x' && m != 'X')
				{
					nonLetters = true;
				}
				if (i < BoundarySize)
				{
					sb.Append(m);
					boundSet.Add(char.ValueOf(m));
				}
				else
				{
					if (i < len - BoundarySize)
					{
						seenSet.Add(char.ValueOf(m));
					}
					else
					{
						boundSet.Add(char.ValueOf(m));
						endSB.Append(m);
					}
				}
				// System.out.println("Position " + i + " --> " + m);
				i += iIncr;
			}
			// put in the stored ones sorted and add end ones
			foreach (char chr in seenSet)
			{
				if (!omitIfInBoundary || !boundSet.Contains(chr))
				{
					char ch = chr;
					sb.Append(ch);
				}
			}
			sb.Append(endSB);
			if (knownLCWords != null)
			{
				if (!nonLetters && knownLCWords.Contains(s.ToLower()))
				{
					sb.Append('k');
				}
			}
			// System.out.println(s + " became " + sb);
			return sb.ToString();
		}

		/// <summary>
		/// Returns a fine-grained word shape classifier, that equivalence classes
		/// lower and upper case and digits, and collapses sequences of the
		/// same type, but keeps all punctuation.
		/// </summary>
		/// <remarks>
		/// Returns a fine-grained word shape classifier, that equivalence classes
		/// lower and upper case and digits, and collapses sequences of the
		/// same type, but keeps all punctuation.  This adds an extra recognizer
		/// for a greek letter embedded in the String, which is useful for bio.
		/// </remarks>
		private static string WordShapeDan2Bio(string s, ICollection<string> knownLCWords)
		{
			if (ContainsGreekLetter(s))
			{
				return WordShapeDan2(s, knownLCWords) + "-GREEK";
			}
			else
			{
				return WordShapeDan2(s, knownLCWords);
			}
		}

		/// <summary>List of greek letters for bio.</summary>
		/// <remarks>
		/// List of greek letters for bio.  We omit eta, mu, nu, xi, phi, chi, psi.
		/// Maybe should omit rho too, but it is used in bio "Rho kinase inhibitor".
		/// </remarks>
		private static readonly string[] greek = new string[] { "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "theta", "iota", "kappa", "lambda", "omicron", "rho", "sigma", "tau", "upsilon", "omega" };

		private static readonly Pattern biogreek = Pattern.Compile("alpha|beta|gamma|delta|epsilon|zeta|theta|iota|kappa|lambda|omicron|rho|sigma|tau|upsilon|omega", Pattern.CaseInsensitive);

		/// <summary>
		/// Somewhat ad-hoc list of only greek letters that bio people use, partly
		/// to avoid false positives on short ones.
		/// </summary>
		/// <param name="s">String to check for Greek</param>
		/// <returns>true iff there is a greek lette embedded somewhere in the String</returns>
		private static bool ContainsGreekLetter(string s)
		{
			Matcher m = biogreek.Matcher(s);
			return m.Find();
		}

		/// <summary>
		/// This one equivalence classes all strings into one of 24 semantically
		/// informed classes, somewhat similarly to the function specified in the
		/// BBN Nymble NER paper (Bikel et al.
		/// </summary>
		/// <remarks>
		/// This one equivalence classes all strings into one of 24 semantically
		/// informed classes, somewhat similarly to the function specified in the
		/// BBN Nymble NER paper (Bikel et al. 1997).
		/// <p>
		/// Note that it regards caseless non-Latin letters as lowercase.
		/// </remarks>
		/// <param name="s">String to word class</param>
		/// <returns>The string's class</returns>
		private static string WordShapeChris1(string s)
		{
			int length = s.Length;
			if (length == 0)
			{
				return "SYMBOL";
			}
			// unclear if this is sensible, but it's what a length 0 String becomes....
			bool cardinal = false;
			bool number = true;
			bool seenDigit = false;
			bool seenNonDigit = false;
			for (int i = 0; i < length; i++)
			{
				char ch = s[i];
				bool digit = char.IsDigit(ch);
				if (digit)
				{
					seenDigit = true;
				}
				else
				{
					seenNonDigit = true;
				}
				// allow commas, decimals, and negative numbers
				digit = digit || ch == '.' || ch == ',' || (i == 0 && (ch == '-' || ch == '+'));
				if (!digit)
				{
					number = false;
				}
			}
			if (!seenDigit)
			{
				number = false;
			}
			else
			{
				if (!seenNonDigit)
				{
					cardinal = true;
				}
			}
			if (cardinal)
			{
				if (length < 4)
				{
					return "CARDINAL13";
				}
				else
				{
					if (length == 4)
					{
						return "CARDINAL4";
					}
					else
					{
						return "CARDINAL5PLUS";
					}
				}
			}
			else
			{
				if (number)
				{
					return "NUMBER";
				}
			}
			bool seenLower = false;
			bool seenUpper = false;
			bool allCaps = true;
			bool allLower = true;
			bool initCap = false;
			bool dash = false;
			bool period = false;
			for (int i_1 = 0; i_1 < length; i_1++)
			{
				char ch = s[i_1];
				bool up = char.IsUpperCase(ch);
				bool let = char.IsLetter(ch);
				bool tit = char.IsTitleCase(ch);
				if (ch == '-')
				{
					dash = true;
				}
				else
				{
					if (ch == '.')
					{
						period = true;
					}
				}
				if (tit)
				{
					seenUpper = true;
					allLower = false;
					seenLower = true;
					allCaps = false;
				}
				else
				{
					if (up)
					{
						seenUpper = true;
						allLower = false;
					}
					else
					{
						if (let)
						{
							seenLower = true;
							allCaps = false;
						}
					}
				}
				if (i_1 == 0 && (up || tit))
				{
					initCap = true;
				}
			}
			if (length == 2 && initCap && period)
			{
				return "ACRONYM1";
			}
			else
			{
				if (seenUpper && allCaps && !seenDigit && period)
				{
					return "ACRONYM";
				}
				else
				{
					if (seenDigit && dash && !seenUpper && !seenLower)
					{
						return "DIGIT-DASH";
					}
					else
					{
						if (initCap && seenLower && seenDigit && dash)
						{
							return "CAPITALIZED-DIGIT-DASH";
						}
						else
						{
							if (initCap && seenLower && seenDigit)
							{
								return "CAPITALIZED-DIGIT";
							}
							else
							{
								if (initCap && seenLower && dash)
								{
									return "CAPITALIZED-DASH";
								}
								else
								{
									if (initCap && seenLower)
									{
										return "CAPITALIZED";
									}
									else
									{
										if (seenUpper && allCaps && seenDigit && dash)
										{
											return "ALLCAPS-DIGIT-DASH";
										}
										else
										{
											if (seenUpper && allCaps && seenDigit)
											{
												return "ALLCAPS-DIGIT";
											}
											else
											{
												if (seenUpper && allCaps && dash)
												{
													return "ALLCAPS";
												}
												else
												{
													if (seenUpper && allCaps)
													{
														return "ALLCAPS";
													}
													else
													{
														if (seenLower && allLower && seenDigit && dash)
														{
															return "LOWERCASE-DIGIT-DASH";
														}
														else
														{
															if (seenLower && allLower && seenDigit)
															{
																return "LOWERCASE-DIGIT";
															}
															else
															{
																if (seenLower && allLower && dash)
																{
																	return "LOWERCASE-DASH";
																}
																else
																{
																	if (seenLower && allLower)
																	{
																		return "LOWERCASE";
																	}
																	else
																	{
																		if (seenLower && seenDigit)
																		{
																			return "MIXEDCASE-DIGIT";
																		}
																		else
																		{
																			if (seenLower)
																			{
																				return "MIXEDCASE";
																			}
																			else
																			{
																				if (seenDigit)
																				{
																					return "SYMBOL-DIGIT";
																				}
																				else
																				{
																					return "SYMBOL";
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Just collapses digits to 9 characters.</summary>
		/// <remarks>
		/// Just collapses digits to 9 characters.
		/// Does lazy copying of String.
		/// </remarks>
		/// <param name="s">String to find word shape of</param>
		/// <returns>The same string except digits are equivalence classed to 9.</returns>
		private static string WordShapeDigits(string s)
		{
			char[] outChars = null;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (char.IsDigit(c))
				{
					if (outChars == null)
					{
						outChars = s.ToCharArray();
					}
					outChars[i] = '9';
				}
			}
			if (outChars == null)
			{
				// no digit found
				return s;
			}
			else
			{
				return new string(outChars);
			}
		}

		/// <summary>Uses distributional similarity clusters for unknown words.</summary>
		/// <remarks>
		/// Uses distributional similarity clusters for unknown words.  Except that
		/// numbers are just turned into NUMBER.
		/// This one uses ones from a fixed file that we've used for NER.
		/// </remarks>
		/// <param name="s">String to find word shape of</param>
		/// <returns>Its word shape</returns>
		private static string WordShapeCluster1(string s)
		{
			bool digit = true;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (!(char.IsDigit(c) || c == '.' || c == ',' || (i == 0 && (c == '-' || c == '+'))))
				{
					digit = false;
				}
			}
			if (digit)
			{
				return "NUMBER";
			}
			else
			{
				string cluster = WordShapeClassifier.DistributionalClusters.cluster1[s];
				if (cluster == null)
				{
					cluster = "NULL";
				}
				return cluster;
			}
		}

		private static string WordShapeChinese(string s)
		{
			return ChineseUtils.ShapeOf(s, true, true);
		}

		private class DistributionalClusters
		{
			private DistributionalClusters()
			{
			}

			public static IDictionary<string, string> cluster1 = LoadWordClusters("/u/nlp/data/pos_tags_are_useless/egw.bnc.200", "alexClark");

			[System.Serializable]
			private class LcMap<K, V> : Dictionary<K, V>
			{
				private const long serialVersionUID = -457913281600751901L;

				public override V Get(object key)
				{
					return base.Get;
				}
			}

			public static IDictionary<string, string> LoadWordClusters(string file, string format)
			{
				Timing.StartDoing("Loading distsim lexicon from " + file);
				IDictionary<string, string> lexicon = new WordShapeClassifier.DistributionalClusters.LcMap<string, string>();
				if ("terryKoo".Equals(format))
				{
					foreach (string line in ObjectBank.GetLineIterator(file))
					{
						string[] bits = line.Split("\\t");
						string word = bits[1];
						// for now, always lowercase, but should revisit this
						word = word.ToLower();
						string wordClass = bits[0];
						lexicon[word] = wordClass;
					}
				}
				else
				{
					// "alexClark"
					foreach (string line in ObjectBank.GetLineIterator(file))
					{
						string[] bits = line.Split("\\s+");
						string word = bits[0];
						// for now, always lowercase, but should revisit this
						word = word.ToLower();
						lexicon[word] = bits[1];
					}
				}
				Timing.EndDoing();
				return lexicon;
			}
		}

		/// <summary>
		/// Usage: <code>java edu.stanford.nlp.process.WordShapeClassifier
		/// [-wordShape name] string+ </code><br />
		/// where <code>name</code> is an argument to <code>lookupShaper</code>.
		/// </summary>
		/// <remarks>
		/// Usage: <code>java edu.stanford.nlp.process.WordShapeClassifier
		/// [-wordShape name] string+ </code><br />
		/// where <code>name</code> is an argument to <code>lookupShaper</code>.
		/// Known names have patterns along the lines of: dan[12](bio)?(UseLC)?,
		/// jenny1(useLC)?, chris[1234](useLC)?, cluster1.
		/// If you don't specify a word shape function, you get chris1.
		/// </remarks>
		/// <param name="args">Command-line arguments, as above.</param>
		public static void Main(string[] args)
		{
			int i = 0;
			int classifierToUse = Wordshapechris1;
			if (args.Length == 0)
			{
				System.Console.Out.WriteLine("edu.stanford.nlp.process.WordShapeClassifier [-wordShape name] string+");
			}
			else
			{
				if (args[0][0] == '-')
				{
					if (args[0].Equals("-wordShape") && args.Length >= 2)
					{
						classifierToUse = LookupShaper(args[1]);
						i += 2;
					}
					else
					{
						log.Info("Unknown flag: " + args[0]);
						i++;
					}
				}
			}
			for (; i < args.Length; i++)
			{
				System.Console.Out.Write(args[i] + ": ");
				System.Console.Out.WriteLine(WordShape(args[i], classifierToUse));
			}
		}
	}
}
