using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Finds multi word strings in a piece of text</summary>
	/// <author>Angel Chang</author>
	public class MultiWordStringMatcher
	{
		/// <summary>
		/// if
		/// <c>matchType</c>
		/// is
		/// <c>EXCT</c>
		/// : match exact string
		/// <br />if
		/// <c>matchType</c>
		/// is
		/// <c>EXCTWS</c>
		/// : match exact string, except whitespace can match multiple whitespaces
		/// <br />if
		/// <c>matchType</c>
		/// is
		/// <c>LWS</c>
		/// : match case insensitive string, except whitespace can match multiple whitespaces
		/// <br />if
		/// <c>matchType</c>
		/// is
		/// <c>LNRM</c>
		/// : disregards punctuation, does case insensitive match
		/// <br />if
		/// <c>matchType</c>
		/// is
		/// <c>REGEX</c>
		/// : interprets string as regex already
		/// </summary>
		public enum MatchType
		{
			Exct,
			Exctws,
			Lws,
			Lnrm,
			Regex
		}

		private bool caseInsensitiveMatch = false;

		private MultiWordStringMatcher.MatchType matchType = MultiWordStringMatcher.MatchType.Exctws;

		public MultiWordStringMatcher(MultiWordStringMatcher.MatchType matchType)
		{
			SetMatchType(matchType);
		}

		public MultiWordStringMatcher(string matchTypeStr)
		{
			SetMatchType(MultiWordStringMatcher.MatchType.ValueOf(matchTypeStr));
		}

		public virtual MultiWordStringMatcher.MatchType GetMatchType()
		{
			return matchType;
		}

		public virtual void SetMatchType(MultiWordStringMatcher.MatchType matchType)
		{
			this.matchType = matchType;
			caseInsensitiveMatch = (matchType != MultiWordStringMatcher.MatchType.Exct && matchType != MultiWordStringMatcher.MatchType.Exctws);
			targetStringPatternCache.Clear();
		}

		/// <summary>Finds target string in text and put spaces around it so it will be matched with we match against tokens.</summary>
		/// <param name="text">- String in which to look for the target string</param>
		/// <param name="targetString">- Target string to look for</param>
		/// <returns>Updated text with spaces around target string</returns>
		public static string PutSpacesAroundTargetString(string text, string targetString)
		{
			return MarkTargetString(text, targetString, " ", " ", true);
		}

		protected internal static string MarkTargetString(string text, string targetString, string beginMark, string endMark, bool markOnlyIfSpace)
		{
			StringBuilder sb = new StringBuilder(text);
			int i = sb.IndexOf(targetString);
			while (i >= 0)
			{
				bool matched = true;
				bool markBefore = !markOnlyIfSpace;
				bool markAfter = !markOnlyIfSpace;
				if (i > 0)
				{
					char charBefore = sb[i - 1];
					if (char.IsLetterOrDigit(charBefore))
					{
						matched = false;
					}
					else
					{
						if (!char.IsWhiteSpace(charBefore))
						{
							markBefore = true;
						}
					}
				}
				if (i + targetString.Length < sb.Length)
				{
					char charAfter = sb[i + targetString.Length];
					if (char.IsLetterOrDigit(charAfter))
					{
						matched = false;
					}
					else
					{
						if (!char.IsWhiteSpace(charAfter))
						{
							markAfter = true;
						}
					}
				}
				if (matched)
				{
					if (markBefore)
					{
						sb.Insert(i, beginMark);
						i += beginMark.Length;
					}
					i = i + targetString.Length;
					if (markAfter)
					{
						sb.Insert(i, endMark);
						i += endMark.Length;
					}
				}
				else
				{
					i++;
				}
				i = sb.IndexOf(targetString, i);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Finds target string in text span from character start to end (exclusive) and returns offsets
		/// (does EXCT string matching).
		/// </summary>
		/// <param name="text">- String in which to look for the target string</param>
		/// <param name="targetString">- Target string to look for</param>
		/// <param name="start">- position to start search</param>
		/// <param name="end">- position to end search</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the targetString can be find
		/// </returns>
		protected internal static IList<IntPair> FindTargetStringOffsetsExct(string text, string targetString, int start, int end)
		{
			if (start > text.Length)
			{
				return null;
			}
			if (end > text.Length)
			{
				return null;
			}
			IList<IntPair> offsets = null;
			int i = text.IndexOf(targetString, start);
			if (i >= 0 && i < end)
			{
				offsets = new List<IntPair>();
			}
			while (i >= 0 && i < end)
			{
				bool matched = true;
				if (i > 0)
				{
					char charBefore = text[i - 1];
					if (char.IsLetterOrDigit(charBefore))
					{
						matched = false;
					}
				}
				if (i + targetString.Length < text.Length)
				{
					char charAfter = text[i + targetString.Length];
					if (char.IsLetterOrDigit(charAfter))
					{
						matched = false;
					}
				}
				if (matched)
				{
					offsets.Add(new IntPair(i, i + targetString.Length));
					i += targetString.Length;
				}
				else
				{
					i++;
				}
				i = text.IndexOf(targetString, i);
			}
			return offsets;
		}

		private CacheMap<string, Pattern> targetStringPatternCache = new CacheMap<string, Pattern>(5000);

		public static readonly IComparator<string> LongestStringComparator = new MultiWordStringMatcher.LongestStringComparator();

		public class LongestStringComparator : IComparator<string>
		{
			public virtual int Compare(string o1, string o2)
			{
				int l1 = o1.Length;
				int l2 = o2.Length;
				if (l1 == l2)
				{
					return string.CompareOrdinal(o1, o2);
				}
				else
				{
					return (l1 > l2) ? -1 : 1;
				}
			}
		}

		public virtual Pattern GetPattern(string[] targetStrings)
		{
			string regex = GetRegex(targetStrings);
			return Pattern.Compile(regex);
		}

		public virtual string GetRegex(string[] targetStrings)
		{
			IList<string> strings = Arrays.AsList(targetStrings);
			// Sort by longest string first
			strings.Sort(LongestStringComparator);
			StringBuilder sb = new StringBuilder();
			foreach (string s in strings)
			{
				if (sb.Length > 0)
				{
					sb.Append("|");
				}
				sb.Append(GetRegex(s));
			}
			string regex = sb.ToString();
			return regex;
		}

		public virtual Pattern GetPattern(string targetString)
		{
			Pattern pattern = targetStringPatternCache[targetString];
			if (pattern == null)
			{
				pattern = CreatePattern(targetString);
				targetStringPatternCache[targetString] = pattern;
			}
			return pattern;
		}

		public virtual Pattern CreatePattern(string targetString)
		{
			string wordRegex = GetRegex(targetString);
			return Pattern.Compile(wordRegex);
		}

		public virtual string GetRegex(string targetString)
		{
			string wordRegex;
			switch (matchType)
			{
				case MultiWordStringMatcher.MatchType.Exct:
				{
					wordRegex = Pattern.Quote(targetString);
					break;
				}

				case MultiWordStringMatcher.MatchType.Exctws:
				{
					wordRegex = GetExctWsRegex(targetString);
					break;
				}

				case MultiWordStringMatcher.MatchType.Lws:
				{
					wordRegex = GetLWsRegex(targetString);
					break;
				}

				case MultiWordStringMatcher.MatchType.Lnrm:
				{
					wordRegex = GetLnrmRegex(targetString);
					break;
				}

				case MultiWordStringMatcher.MatchType.Regex:
				{
					wordRegex = targetString;
					break;
				}

				default:
				{
					throw new NotSupportedException();
				}
			}
			return wordRegex;
		}

		private static Pattern whitespacePattern = Pattern.Compile("\\s+");

		private static readonly Pattern punctWhitespacePattern = Pattern.Compile("\\s*(\\p{Punct})\\s*");

		public static string GetExctWsRegex(string targetString)
		{
			StringBuilder sb = new StringBuilder();
			string[] fields = whitespacePattern.Split(targetString);
			foreach (string field in fields)
			{
				// require at least one whitespace if there is whitespace in target string
				if (sb.Length > 0)
				{
					sb.Append("\\s+");
				}
				// Allow any number of spaces between punctuation and text
				string tmp = punctWhitespacePattern.Matcher(field).ReplaceAll(" $1 ");
				tmp = tmp.Trim();
				string[] punctFields = whitespacePattern.Split(tmp);
				foreach (string f in punctFields)
				{
					if (sb.Length > 0)
					{
						sb.Append("\\s*");
					}
					sb.Append(Pattern.Quote(f));
				}
			}
			return sb.ToString();
		}

		public static string GetLWsRegex(string targetString)
		{
			return "(?iu)" + GetExctWsRegex(targetString);
		}

		private static readonly Pattern lnrmDelimPatternAny = Pattern.Compile("(?:\\p{Punct}|\\s)*");

		private static readonly Pattern lnrmDelimPattern = Pattern.Compile("(?:\\p{Punct}|\\s)+");

		public static string GetLnrmRegex(string targetString)
		{
			StringBuilder sb = new StringBuilder("(?iu)");
			string[] fields = lnrmDelimPattern.Split(targetString);
			bool first = true;
			foreach (string field in fields)
			{
				if (!first)
				{
					sb.Append(lnrmDelimPatternAny);
				}
				else
				{
					first = false;
				}
				sb.Append(Pattern.Quote(field));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Finds target string in text and returns offsets using regular expressions
		/// (matches based on set matchType).
		/// </summary>
		/// <param name="text">- String in which to find target string</param>
		/// <param name="targetString">- Target string to look for</param>
		/// <param name="start">- position to start search</param>
		/// <param name="end">- position to end search</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the target string can be find
		/// </returns>
		protected internal virtual IList<IntPair> FindTargetStringOffsetsRegex(string text, string targetString, int start, int end)
		{
			if (start > text.Length)
			{
				return null;
			}
			if (end > text.Length)
			{
				return null;
			}
			Pattern targetPattern = GetPattern(targetString);
			return FindOffsets(targetPattern, text, start, end);
		}

		/// <summary>Finds pattern in text and returns offsets.</summary>
		/// <param name="pattern">- pattern to look for</param>
		/// <param name="text">- String in which to look for the pattern</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the pattern can be find
		/// </returns>
		public static IList<IntPair> FindOffsets(Pattern pattern, string text)
		{
			return FindOffsets(pattern, text, 0, text.Length);
		}

		/// <summary>Finds pattern in text span from character start to end (exclusive) and returns offsets.</summary>
		/// <param name="pattern">- pattern to look for</param>
		/// <param name="text">- String in which to look for the pattern</param>
		/// <param name="start">- position to start search</param>
		/// <param name="end">- position to end search</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the pattern can be find
		/// </returns>
		public static IList<IntPair> FindOffsets(Pattern pattern, string text, int start, int end)
		{
			Matcher matcher = pattern.Matcher(text);
			IList<IntPair> offsets = null;
			matcher.Region(start, end);
			int i = (matcher.Find()) ? matcher.Start() : -1;
			if (i >= 0 && i < end)
			{
				offsets = new List<IntPair>();
			}
			while (i >= 0 && i < end)
			{
				bool matched = true;
				int matchEnd = matcher.End();
				if (i > 0)
				{
					char charBefore = text[i - 1];
					if (char.IsLetterOrDigit(charBefore))
					{
						matched = false;
					}
				}
				if (matchEnd < text.Length)
				{
					char charAfter = text[matchEnd];
					if (char.IsLetterOrDigit(charAfter))
					{
						matched = false;
					}
				}
				if (matched)
				{
					offsets.Add(new IntPair(i, matchEnd));
				}
				i = (matcher.Find()) ? matcher.Start() : -1;
			}
			return offsets;
		}

		/// <summary>
		/// Finds target string in text and returns offsets
		/// (matches based on set matchType).
		/// </summary>
		/// <param name="text">- String in which to look for the target string</param>
		/// <param name="targetString">- Target string to look for</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the target string can be find
		/// </returns>
		public virtual IList<IntPair> FindTargetStringOffsets(string text, string targetString)
		{
			return FindTargetStringOffsets(text, targetString, 0, text.Length);
		}

		/// <summary>
		/// Finds target string in text span from character start to end (exclusive) and returns offsets
		/// (matches based on set matchType).
		/// </summary>
		/// <param name="text">- String in which to look for the target string</param>
		/// <param name="targetString">- Target string to look for</param>
		/// <param name="start">- position to start search</param>
		/// <param name="end">- position to end search</param>
		/// <returns>
		/// list of integer pairs indicating the character offsets (begin, end - exclusive)
		/// at which the target string can be find
		/// </returns>
		public virtual IList<IntPair> FindTargetStringOffsets(string text, string targetString, int start, int end)
		{
			switch (matchType)
			{
				case MultiWordStringMatcher.MatchType.Exct:
				{
					return FindTargetStringOffsetsExct(text, targetString, start, end);
				}

				default:
				{
					return FindTargetStringOffsetsRegex(text, targetString, start, end);
				}
			}
		}
	}
}
