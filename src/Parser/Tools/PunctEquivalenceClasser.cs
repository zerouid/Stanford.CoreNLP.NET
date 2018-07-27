using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Performs equivalence classing of punctuation per PTB guidelines.</summary>
	/// <remarks>
	/// Performs equivalence classing of punctuation per PTB guidelines. Many of the multilingual
	/// treebanks mark all punctuation with a single POS tag, which is bad for parsing.
	/// <p>
	/// PTB punctuation POS tag set (12 tags):
	/// 37. #  Pound sign
	/// 38. $  Dollar sign
	/// 39. .  Sentence-final punctuation
	/// 40. ,  Comma
	/// 41. :  Colon, semi-colon
	/// 42. (  Left bracket character
	/// 43. )  Right bracket character
	/// 44. "  Straight double quote
	/// 45. `  Left open single quote
	/// 46. "  Left open double quote
	/// 47. '  Right close single quote
	/// 48. "  Right close double quote
	/// <p>
	/// See http://www.ldc.upenn.edu/Catalog/docs/LDC95T7/cl93.html
	/// </remarks>
	/// <author>Spence Green</author>
	public class PunctEquivalenceClasser
	{
		private static readonly string[] eolClassRaw = new string[] { ".", "?", "!" };

		private static readonly ICollection<string> sfClass = Generics.NewHashSet(Arrays.AsList(eolClassRaw));

		private static readonly string[] colonClassRaw = new string[] { ":", ";", "-", "_" };

		private static readonly ICollection<string> colonClass = Generics.NewHashSet(Arrays.AsList(colonClassRaw));

		private static readonly string[] commaClassRaw = new string[] { ",", "ر" };

		private static readonly ICollection<string> commaClass = Generics.NewHashSet(Arrays.AsList(commaClassRaw));

		private static readonly string[] currencyClassRaw = new string[] { "$", "#", "=" };

		private static readonly ICollection<string> currencyClass = Generics.NewHashSet(Arrays.AsList(currencyClassRaw));

		private static readonly Pattern pEllipsis = Pattern.Compile("\\.\\.+");

		private static readonly string[] slashClassRaw = new string[] { "/", "\\" };

		private static readonly ICollection<string> slashClass = Generics.NewHashSet(Arrays.AsList(slashClassRaw));

		private static readonly string[] lBracketClassRaw = new string[] { "-LRB-", "(", "[", "<" };

		private static readonly ICollection<string> lBracketClass = Generics.NewHashSet(Arrays.AsList(lBracketClassRaw));

		private static readonly string[] rBracketClassRaw = new string[] { "-RRB-", ")", "]", ">" };

		private static readonly ICollection<string> rBracketClass = Generics.NewHashSet(Arrays.AsList(rBracketClassRaw));

		private static readonly string[] quoteClassRaw = new string[] { "\"", "``", "''", "'", "`" };

		private static readonly ICollection<string> quoteClass = Generics.NewHashSet(Arrays.AsList(quoteClassRaw));

		/// <summary>Return the equivalence class of the argument.</summary>
		/// <remarks>
		/// Return the equivalence class of the argument. If the argument is not contained in
		/// and equivalence class, then an empty string is returned.
		/// </remarks>
		/// <param name="punc"/>
		/// <returns>The class name if found. Otherwise, an empty string.</returns>
		public static string GetPunctClass(string punc)
		{
			if (punc.Equals("%") || punc.Equals("-PLUS-"))
			{
				//-PLUS- is an escape for "+" in the ATB
				return "perc";
			}
			else
			{
				if (punc.StartsWith("*"))
				{
					return "bullet";
				}
				else
				{
					if (sfClass.Contains(punc))
					{
						return "sf";
					}
					else
					{
						if (colonClass.Contains(punc) || pEllipsis.Matcher(punc).Matches())
						{
							return "colon";
						}
						else
						{
							if (commaClass.Contains(punc))
							{
								return "comma";
							}
							else
							{
								if (currencyClass.Contains(punc))
								{
									return "curr";
								}
								else
								{
									if (slashClass.Contains(punc))
									{
										return "slash";
									}
									else
									{
										if (lBracketClass.Contains(punc))
										{
											return "lrb";
										}
										else
										{
											if (rBracketClass.Contains(punc))
											{
												return "rrb";
											}
											else
											{
												if (quoteClass.Contains(punc))
												{
													return "quote";
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
			return string.Empty;
		}
	}
}
