using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util.Logging;
using Microsoft.Extensions.Configuration;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>StringUtils is a class for random String things, including output formatting and command line argument parsing.</summary>
	/// <remarks>
	/// StringUtils is a class for random String things, including output formatting and command line argument parsing.
	/// <p>
	/// Many of these methods will be familiar to perl users:
	/// <see cref="Join(System.Collections.Generic.IEnumerable{T})"/>
	/// ,
	/// <see cref="Split(string, string)"/>
	/// ,
	/// <see cref="Trim(string, int)"/>
	/// ,
	/// <see cref="Find(string, string)"/>
	/// ,
	/// <see cref="LookingAt(string, string)"/>
	/// , and
	/// <see cref="Matches(string, string)"/>
	/// .
	/// <p>
	/// There are also useful methods for padding Strings/Objects with spaces on the right or left for printing even-width
	/// table columns:
	/// <see cref="PadLeft(int, int)"/>
	/// ,
	/// <see cref="Pad(string, int)"/>
	/// .
	/// <p>Example: print a comma-separated list of numbers:</p>
	/// <p>
	/// <c>System.out.println(StringUtils.pad(nums, &quot;, &quot;));</c>
	/// </p>
	/// <p>Example: print a 2D array of numbers with 8-char cells:</p>
	/// <p><code>for(int i = 0; i &lt; nums.length; i++) {<br />
	/// &nbsp;&nbsp;&nbsp; for(int j = 0; j &lt; nums[i].length; j++) {<br />
	/// &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
	/// System.out.print(StringUtils.leftPad(nums[i][j], 8));<br />
	/// &nbsp;&nbsp;&nbsp; <br />
	/// &nbsp;&nbsp;&nbsp; System.out.println();<br />
	/// </code></p>
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <author>Tim Grow (grow@stanford.edu)</author>
	/// <author>Chris Cox</author>
	/// <version>2006/02/03</version>
	public class StringUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.StringUtils));

		/// <summary>Don't let anyone instantiate this class.</summary>
		private StringUtils()
		{
		}

		public static readonly string[] EmptyStringArray = new string[0];

		private const string Prop = "prop";

		private const string Props = "props";

		private const string Properties = "properties";

		private const string Args = "args";

		private const string Arguments = "arguments";

		// todo [cdm 2016]: Remove CoreMap/CoreLabel methods from this class
		// todo [cdm 2016]: Write a really good join method for this class, like William's Ruby one
		/// <summary>
		/// Say whether this regular expression can be found inside
		/// this String.
		/// </summary>
		/// <remarks>
		/// Say whether this regular expression can be found inside
		/// this String.  This method provides one of the two "missing"
		/// convenience methods for regular expressions in the String class
		/// in JDK1.4.  This is the one you'll want to use all the time if
		/// you're used to Perl.  What were they smoking?
		/// </remarks>
		/// <param name="str">String to search for match in</param>
		/// <param name="regex">String to compile as the regular expression</param>
		/// <returns>Whether the regex can be found in str</returns>
		public static bool Find(string str, string regex)
		{
			return new Regex(regex).IsMatch(str);
		}

		/// <summary>Convenience method: a case-insensitive variant of Collection.contains.</summary>
		/// <param name="c">Collection&lt;String&gt;</param>
		/// <param name="s">String</param>
		/// <returns>true if s case-insensitively matches a string in c</returns>
		public static bool ContainsIgnoreCase(ICollection<string> c, string s)
		{
			foreach (string sPrime in c)
			{
				if (string.Equals(sPrime, s, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Say whether this regular expression can be found at the beginning of
		/// this String.
		/// </summary>
		/// <remarks>
		/// Say whether this regular expression can be found at the beginning of
		/// this String.  This method provides one of the two "missing"
		/// convenience methods for regular expressions in the String class
		/// in JDK1.4.
		/// </remarks>
		/// <param name="str">String to search for match at start of</param>
		/// <param name="regex">String to compile as the regular expression</param>
		/// <returns>Whether the regex can be found at the start of str</returns>
		public static bool LookingAt(string str, string regex)
		{
			return Regex.IsMatch(str, @"\A(?:" + regex + ")");
		}

		/// <summary>
		/// Takes a string of the form "x1=y1,x2=y2,..." such
		/// that each y is an integer and each x is a key.
		/// </summary>
		/// <remarks>
		/// Takes a string of the form "x1=y1,x2=y2,..." such
		/// that each y is an integer and each x is a key.  A
		/// String[] s is returned such that s[yn]=xn.
		/// </remarks>
		/// <param name="map">
		/// A string of the form "x1=y1,x2=y2,..." such
		/// that each y is an integer and each x is a key.
		/// </param>
		/// <returns>A String[] s is returned such that s[yn]=xn</returns>
		public static string[] MapStringToArray(string map)
		{
			string[] m = map.Split(',',';');
			int maxIndex = 0;
			string[] keys = new string[m.Length];
			int[] indices = new int[m.Length];
			for (int i = 0; i < m.Length; i++)
			{
				int index = m[i].LastIndexOf('=');
				keys[i] = m[i].Substring(0, index);
				indices[i] = System.Convert.ToInt32(m[i].Substring(index + 1));
				if (indices[i] > maxIndex)
				{
					maxIndex = indices[i];
				}
			}
			string[] mapArr = new string[maxIndex + 1];
			// Arrays.fill(mapArr, null); // not needed; Java arrays zero initialized
			for (int i_1 = 0; i_1 < m.Length; i_1++)
			{
				mapArr[indices[i_1]] = keys[i_1];
			}
			return mapArr;
		}

		/// <summary>Takes a string of the form "x1=y1,x2=y2,..." and returns Map.</summary>
		/// <param name="map">A string of the form "x1=y1,x2=y2,..."</param>
		/// <returns>A Map m is returned such that m.get(xn) = yn</returns>
		public static IDictionary<string, string> MapStringToMap(string map)
		{
			string[] m = map.Split(',',';');
			IDictionary<string, string> res = new Dictionary<string,string>();
			foreach (string str in m)
			{
				int index = str.LastIndexOf('=');
				string key = str.Substring(0, index);
				string val = str.Substring( index + 1);
				res.Add(key.Trim(),val.Trim());
			}
			return res;
		}

		public static IList<Regex> RegexesToPatterns(IEnumerable<string> regexes)
		{
			IList<Regex> patterns = new List<Regex>();
			foreach (string regex in regexes)
			{
				patterns.Add(new Regex(regex, RegexOptions.Compiled));
			}
			return patterns;
		}

		/// <summary>
		/// Given a pattern, which contains one or more capturing groups, and a String,
		/// returns a list with the values of the
		/// captured groups in the pattern.
		/// </summary>
		/// <remarks>
		/// Given a pattern, which contains one or more capturing groups, and a String,
		/// returns a list with the values of the
		/// captured groups in the pattern. If the pattern does not match, returns
		/// null. Note that this uses Matcher.find() rather than Matcher.matches().
		/// If str is null, returns null.
		/// </remarks>
		public static IList<string> RegexGroups(Regex regex, string str)
		{
			if (str == null)
			{
				return null;
			}
			Match matcher = regex.Match(str);
			if (!matcher.Success)
			{
				return null;
			}
			IList<string> groups = new List<string>(matcher.Groups.Count);
			for (int index = 1; index <= matcher.Groups.Count; index++)
			{
				groups.Add(matcher.Groups[index].Value);
			}
			return groups;
		}

		/// <summary>
		/// Say whether this regular expression matches
		/// this String.
		/// </summary>
		/// <remarks>
		/// Say whether this regular expression matches
		/// this String.  This method is the same as the String.matches() method,
		/// and is included just to give a call that is parallel to the other
		/// static regex methods in this class.
		/// </remarks>
		/// <param name="str">String to search for match at start of</param>
		/// <param name="regex">String to compile as the regular expression</param>
		/// <returns>Whether the regex matches the whole of this str</returns>
		public static bool Matches(string str, string regex)
		{
			return new Regex(@"\A(?:" + regex + @")\z").IsMatch(str);
		}

		public static ICollection<string> StringToSet(string str, string delimiter)
		{
			ICollection<string> ret = null;
			if (str != null)
			{
				string[] fields = Regex.Split(str,delimiter);
				ret = new HashSet<string>();
				foreach (string field in fields)
				{
					ret.Add(field.Trim());
				}
			}
			return ret;
		}

		public static string JoinWords<_T0>(IEnumerable<_T0> l, string glue)
			where _T0 : IHasWord
		{
			StringBuilder sb = new StringBuilder(l is ICollection ? ((ICollection)l).Count : 64);
			bool first = true;
			foreach (IHasWord o in l)
			{
				if (!first)
				{
					sb.Append(glue);
				}
				else
				{
					first = false;
				}
				sb.Append(o.Word());
			}
			return sb.ToString();
		}

		public static string Join<E>(IList<E> l, string glue, Func<E, string> toStringFunc, int start, int end)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			start = System.Math.Max(start, 0);
			end = System.Math.Min(end, l.Count);
			for (int i = start; i < end; i++)
			{
				if (!first)
				{
					sb.Append(glue);
				}
				else
				{
					first = false;
				}
				sb.Append(toStringFunc(l[i]));
			}
			return sb.ToString();
		}

		public static string JoinWords<W>(IList<W> l, string glue, int start, int end)
			where W : IHasWord
		{
			return Join<W>(l, glue, null, start, end);
		}

		private static readonly Func<object, string> DefaultTostring = null;

		public static string JoinFields<M>(IList<M> l, Type field, string defaultFieldValue, string glue, int start, int end, Func<object, string> toStringFunc)
			where M : ICoreMap
		{
			return Join(l, glue, (Func<M, string>)null, start, end);
		}

		public static string JoinFields<_T0>(IList<_T0> l, Type field, string defaultFieldValue, string glue, int start, int end)
			where _T0 : ICoreMap
		{
			return JoinFields(l, field, defaultFieldValue, glue, start, end, DefaultTostring);
		}

		public static string JoinFields<_T0>(IList<_T0> l, Type field, Func<object, string> toStringFunc)
			where _T0 : ICoreMap
		{
			return JoinFields(l, field, "-", " ", 0, l.Count, toStringFunc);
		}

		public static string JoinFields<_T0>(IList<_T0> l, Type field)
			where _T0 : ICoreMap
		{
			return JoinFields(l, field, "-", " ", 0, l.Count);
		}

		public static string JoinMultipleFields<T>(IList<T> l, Type[] fields, string defaultFieldValue, string fieldGlue, string glue, int start, int end, Func<object, string> toStringFunc)
			where T : ICoreMap
		{
			return Join(l, glue, (Func<T, string>)null, start, end);
		}

		public static string JoinMultipleFields<_T0>(IList<_T0> l, Type[] fields, Func<object, string> toStringFunc)
			where _T0 : ICoreMap
		{
			return JoinMultipleFields(l, fields, "-", "/", " ", 0, l.Count, toStringFunc);
		}

		public static string JoinMultipleFields<_T0>(IList<_T0> l, Type[] fields, string defaultFieldValue, string fieldGlue, string glue, int start, int end)
			where _T0 : ICoreMap
		{
			return JoinMultipleFields(l, fields, defaultFieldValue, fieldGlue, glue, start, end, DefaultTostring);
		}

		public static string JoinMultipleFields<_T0>(IList<_T0> l, Type[] fields)
			where _T0 : ICoreMap
		{
			return JoinMultipleFields(l, fields, "-", "/", " ", 0, l.Count);
		}

		/// <summary>Joins all the tokens together (more or less) according to their original whitespace.</summary>
		/// <remarks>
		/// Joins all the tokens together (more or less) according to their original whitespace.
		/// It assumes all whitespace was " ".
		/// </remarks>
		/// <param name="tokens">
		/// list of tokens which implement
		/// <see cref="Edu.Stanford.Nlp.Ling.IHasOffset"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Ling.IHasWord"/>
		/// </param>
		/// <returns>a string of the tokens with the appropriate amount of spacing</returns>
		public static string JoinWithOriginalWhiteSpace(IList<CoreLabel> tokens)
		{
			if (!tokens.Any())
			{
				return string.Empty;
			}
			CoreLabel lastToken = tokens[0];
			StringBuilder buffer = new StringBuilder(lastToken.Word());
			for (int i = 1; i < tokens.Count; i++)
			{
				CoreLabel currentToken = tokens[i];
				int numSpaces = currentToken.BeginPosition() - lastToken.EndPosition();
				if (numSpaces < 0)
				{
					numSpaces = 0;
				}
				buffer.Append(Repeat(' ', numSpaces)).Append(currentToken.Word());
				lastToken = currentToken;
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Joins each elem in the
		/// <see cref="System.Collections.IEnumerable{T}"/>
		/// with the given glue.
		/// For example, given a list of
		/// <c>Integers</c>
		/// , you can create
		/// a comma-separated list by calling
		/// <c>join(numbers, ", ")</c>
		/// .
		/// </summary>
		/// <seealso cref="Join{X}(Java.Util.Stream.IStream{T}, string)"/>
		public static string Join<X>(IEnumerable<X> l, string glue)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (X o in l)
			{
				if (!first)
				{
					sb.Append(glue);
				}
				else
				{
					first = false;
				}
				sb.Append(o);
			}
			return sb.ToString();
		}

		/// <summary>Joins each elem in the array with the given glue.</summary>
		/// <remarks>
		/// Joins each elem in the array with the given glue. For example, given a
		/// list of ints, you can create a comma-separated list by calling
		/// <c>join(numbers, ", ")</c>
		/// .
		/// </remarks>
		public static string Join(object[] elements, string glue)
		{
			return (Join(elements, glue));
		}

		/// <summary>Joins an array of elements in a given span.</summary>
		/// <param name="elements">The elements to join.</param>
		/// <param name="start">The start index to join from.</param>
		/// <param name="end">The end (non-inclusive) to join until.</param>
		/// <param name="glue">The glue to hold together the elements.</param>
		/// <returns>The string form of the sub-array, joined on the given glue.</returns>
		public static string Join(object[] elements, int start, int end, string glue)
		{
			StringBuilder b = new StringBuilder(127);
			bool isFirst = true;
			for (int i = start; i < end; ++i)
			{
				if (isFirst)
				{
					b.Append(elements[i]);
					isFirst = false;
				}
				else
				{
					b.Append(glue).Append(elements[i]);
				}
			}
			return b.ToString();
		}

		/// <summary>Joins each element in the given array with the given glue.</summary>
		/// <remarks>
		/// Joins each element in the given array with the given glue. For example,
		/// given an array of Integers, you can create a comma-separated list by calling
		/// <c>join(numbers, ", ")</c>
		/// .
		/// </remarks>
		public static string Join(string[] items, string glue)
		{
			return Join(items, glue);
		}

		/// <summary>Joins elements with a space.</summary>
		public static string Join<_T0>(IEnumerable<_T0> l)
		{
			return Join(l, " ");
		}

		/// <summary>Joins elements with a space.</summary>
		public static string Join(object[] elements)
		{
			return (Join(elements, " "));
		}

		/// <summary>Splits on whitespace (\\s+).</summary>
		/// <param name="s">String to split</param>
		/// <returns>List<String> of split strings</returns>
		public static IList<string> Split(string s)
		{
			return Split(s, "\\s+");
		}

		/// <summary>Splits the given string using the given regex as delimiters.</summary>
		/// <remarks>
		/// Splits the given string using the given regex as delimiters.
		/// This method is the same as the String.split() method (except it throws
		/// the results in a List),
		/// and is included just to give a call that is parallel to the other
		/// static regex methods in this class.
		/// </remarks>
		/// <param name="str">String to split up</param>
		/// <param name="regex">String to compile as the regular expression</param>
		/// <returns>List of Strings resulting from splitting on the regex</returns>
		public static IList<string> Split(string str, string regex)
		{
			return Regex.Split(str,regex);
		}

		/// <summary>Split a string on a given single character.</summary>
		/// <remarks>
		/// Split a string on a given single character.
		/// This method is often faster than the regular split() method.
		/// </remarks>
		/// <param name="input">The input to split.</param>
		/// <param name="delimiter">The character to split on.</param>
		/// <returns>An array of Strings corresponding to the original input split on the delimiter character.</returns>
		public static string[] SplitOnChar(string input, char delimiter)
		{
			// State
			string[] @out = new string[input.Length + 1];
			int nextIndex = 0;
			int lastDelimiterIndex = -1;
			char[] chars = input.ToCharArray();
			// Split
			for (int i = 0; i <= chars.Length; ++i)
			{
				if (i >= chars.Length || chars[i] == delimiter)
				{
					char[] tokenChars = new char[i - (lastDelimiterIndex + 1)];
					System.Array.Copy(chars, lastDelimiterIndex + 1, tokenChars, 0, tokenChars.Length);
					@out[nextIndex] = new string(tokenChars);
					nextIndex += 1;
					lastDelimiterIndex = i;
				}
			}
			// Clean Result
			string[] trimmedOut = new string[nextIndex];
			System.Array.Copy(@out, 0, trimmedOut, 0, trimmedOut.Length);
			return trimmedOut;
		}

		/// <summary>Splits a string into whitespace tokenized fields based on a delimiter and then whitespace.</summary>
		/// <remarks>
		/// Splits a string into whitespace tokenized fields based on a delimiter and then whitespace.
		/// For example, "aa bb | bb cc | ccc ddd" would be split into "[aa,bb],[bb,cc],[ccc,ddd]" based on
		/// the delimiter "|". This method uses the old StringTokenizer class, which is up to
		/// 3x faster than the regex-based "split()" methods.
		/// </remarks>
		/// <param name="delimiter">String to split on</param>
		/// <returns>List of lists of strings.</returns>
		public static IList<IList<string>> SplitFieldsFast(string str, string delimiter)
		{
			IList<IList<string>> fields = new List<IList<string>>();
			string[] tokens = Regex.Split(str.Trim(), @"[\t\n\r\f]");
			IList<string> currentField = new List<string>();
			foreach (string token in tokens)
			{
				if (token.Equals(delimiter))
				{
					fields.Add(currentField);
					currentField = new List<string>();
				}
				else
				{
					currentField.Add(token.Trim());
				}
			}
			if (currentField.Count > 0)
			{
				fields.Add(currentField);
			}
			return fields;
		}

		/// <summary>Split on a given character, filling out the fields in the output array.</summary>
		/// <remarks>
		/// Split on a given character, filling out the fields in the output array.
		/// This is suitable for, e.g., splitting a TSV file of known column count.
		/// </remarks>
		/// <param name="out">The output array to fill</param>
		/// <param name="input">The input to split</param>
		/// <param name="delimiter">The delimiter to split on.</param>
		public static void SplitOnChar(string[] @out, string input, char delimiter)
		{
			int lastSplit = 0;
			int outI = 0;
			char[] chars = input.ToCharArray();
			for (int i = 0; i < chars.Length; ++i)
			{
				if (chars[i] == delimiter)
				{
					@out[outI] = new string(chars, lastSplit, i - lastSplit);
					outI += 1;
					lastSplit = i + 1;
				}
			}
			if (outI < @out.Length)
			{
				@out[outI] = input.Substring(lastSplit);
			}
		}

		/// <summary>Split a string into tokens.</summary>
		/// <remarks>
		/// Split a string into tokens.  Because there is a tokenRegex as well as a
		/// separatorRegex (unlike for the conventional split), you can do things
		/// like correctly split quoted strings or parenthesized arguments.
		/// However, it doesn't do the unquoting of quoted Strings for you.
		/// An empty String argument is returned at the beginning, if valueRegex
		/// accepts the empty String and str begins with separatorRegex.
		/// But str can end with either valueRegex or separatorRegex and this does
		/// not generate an empty String at the end (indeed, valueRegex need not
		/// even accept the empty String in this case.  However, if it does accept
		/// the empty String and there are multiple trailing separators, then
		/// empty values will be returned.
		/// </remarks>
		/// <param name="str">The String to split</param>
		/// <param name="valueRegex">Must match a token. You may wish to let it match the empty String</param>
		/// <param name="separatorRegex">Must match a separator</param>
		/// <returns>The List of tokens</returns>
		/// <exception cref="System.ArgumentException">if str cannot be tokenized by the two regex</exception>
		public static IList<string> ValueSplit(string str, string valueRegex, string separatorRegex)
		{
			Regex vPat = new Regex(valueRegex);
			Regex sPat = new Regex(separatorRegex);
			IList<string> ret = new List<string>();
			while (!string.IsNullOrEmpty(str))
			{
				Match vm = vPat.Match(str);
				if (vm.Success)
				{
					ret.Add(vm.Value);
					str = str.Substring(vm.Index + vm.Length);
				}
				else
				{
					// String got = vm.group();
					// log.info("vmatched " + got + "; now str is " + str);
					throw new ArgumentException("valueSplit: " + valueRegex + " doesn't match " + str);
				}
				if (!string.IsNullOrEmpty(str))
				{
					Match sm = sPat.Match(str);
					if (sm.Success)
					{
						str = str.Substring(sm.Index + sm.Length);
					}
					else
					{
						// String got = sm.group();
						// log.info("smatched " + got + "; now str is " + str);
						throw new ArgumentException("valueSplit: " + separatorRegex + " doesn't match " + str);
					}
				}
			}
			// end while
			return ret;
		}

		/// <summary>
		/// Return a String of length a minimum of totalChars characters by
		/// padding the input String str at the right end with spaces.
		/// </summary>
		/// <remarks>
		/// Return a String of length a minimum of totalChars characters by
		/// padding the input String str at the right end with spaces.
		/// If str is already longer
		/// than totalChars, it is returned unchanged.
		/// </remarks>
		public static string Pad(string str, int totalChars)
		{
			return Pad(str, totalChars, ' ');
		}

		/// <summary>
		/// Return a String of length a minimum of totalChars characters by
		/// padding the input String str at the right end with spaces.
		/// </summary>
		/// <remarks>
		/// Return a String of length a minimum of totalChars characters by
		/// padding the input String str at the right end with spaces.
		/// If str is already longer
		/// than totalChars, it is returned unchanged.
		/// </remarks>
		public static string Pad(string str, int totalChars, char pad)
		{
			if (str == null)
			{
				str = "null";
			}
			int slen = str.Length;
			StringBuilder sb = new StringBuilder(str);
			for (int i = 0; i < totalChars - slen; i++)
			{
				sb.Append(pad);
			}
			return sb.ToString();
		}

		/// <summary>Pads the toString value of the given Object.</summary>
		public static string Pad(object obj, int totalChars)
		{
			return Pad(obj.ToString(), totalChars);
		}

		/// <summary>Pad or trim so as to produce a string of exactly a certain length.</summary>
		/// <param name="str">The String to be padded or truncated</param>
		/// <param name="num">The desired length</param>
		public static string PadOrTrim(string str, int num)
		{
			if (str == null)
			{
				str = "null";
			}
			int leng = str.Length;
			if (leng < num)
			{
				StringBuilder sb = new StringBuilder(str);
				for (int i = 0; i < num - leng; i++)
				{
					sb.Append(' ');
				}
				return sb.ToString();
			}
			else
			{
				if (leng > num)
				{
					return str.Substring(0, num);
				}
				else
				{
					return str;
				}
			}
		}

		/// <summary>Pad or trim so as to produce a string of exactly a certain length.</summary>
		/// <param name="str">The String to be padded or truncated</param>
		/// <param name="num">The desired length</param>
		public static string PadLeftOrTrim(string str, int num)
		{
			if (str == null)
			{
				str = "null";
			}
			int leng = str.Length;
			if (leng < num)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < num - leng; i++)
				{
					sb.Append(' ');
				}
				sb.Append(str);
				return sb.ToString();
			}
			else
			{
				if (leng > num)
				{
					return str.Substring(str.Length - num);
				}
				else
				{
					return str;
				}
			}
		}

		/// <summary>Pad or trim the toString value of the given Object.</summary>
		public static string PadOrTrim(object obj, int totalChars)
		{
			return PadOrTrim(obj.ToString(), totalChars);
		}

		/// <summary>
		/// Pads the given String to the left with the given character ch to ensure that
		/// it's at least totalChars long.
		/// </summary>
		public static string PadLeft(string str, int totalChars, char ch)
		{
			if (str == null)
			{
				str = "null";
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0, num = totalChars - str.Length; i < num; i++)
			{
				sb.Append(ch);
			}
			sb.Append(str);
			return sb.ToString();
		}

		/// <summary>
		/// Pads the given String to the left with spaces to ensure that it's
		/// at least totalChars long.
		/// </summary>
		public static string PadLeft(string str, int totalChars)
		{
			return PadLeft(str, totalChars, ' ');
		}

		public static string PadLeft(object obj, int totalChars)
		{
			return PadLeft(obj.ToString(), totalChars);
		}

		public static string PadLeft(int i, int totalChars)
		{
			return PadLeft(i, totalChars);
		}

		public static string PadLeft(double d, int totalChars)
		{
			return PadLeft(d, totalChars);
		}

		/// <summary>Returns s if it's at most maxWidth chars, otherwise chops right side to fit.</summary>
		public static string Trim(string s, int maxWidth)
		{
			if (s.Length <= maxWidth)
			{
				return (s);
			}
			return s.Substring(0, maxWidth);
		}

		public static string Trim(object obj, int maxWidth)
		{
			return Trim(obj.ToString(), maxWidth);
		}

		public static string TrimWithEllipsis(string s, int width)
		{
			if (s.Length > width)
			{
				s = s.Substring(0, width - 3) + "...";
			}
			return s;
		}

		public static string TrimWithEllipsis(object o, int width)
		{
			return TrimWithEllipsis(o.ToString(), width);
		}

		public static string Repeat(string s, int times)
		{
			if (times == 0)
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder(times * s.Length);
			for (int i = 0; i < times; i++)
			{
				sb.Append(s);
			}
			return sb.ToString();
		}

		public static string Repeat(char ch, int times)
		{
			if (times == 0)
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder(times);
			for (int i = 0; i < times; i++)
			{
				sb.Append(ch);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns a "clean" version of the given filename in which spaces have
		/// been converted to dashes and all non-alphanumeric chars are underscores.
		/// </summary>
		public static string FileNameClean(string s)
		{
			char[] chars = s.ToCharArray();
			StringBuilder sb = new StringBuilder();
			foreach (char c in chars)
			{
				if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || (c == '_'))
				{
					sb.Append(c);
				}
				else
				{
					if (c == ' ' || c == '-')
					{
						sb.Append('_');
					}
					else
					{
						sb.Append('x').Append((int)c).Append('x');
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns the index of the <i>n</i>th occurrence of ch in s, or -1
		/// if there are less than n occurrences of ch.
		/// </summary>
		public static int NthIndex(string s, char ch, int n)
		{
			int index = 0;
			for (int i = 0; i < n; i++)
			{
				// if we're already at the end of the string,
				// and we need to find another ch, return -1
				if (index == s.Length - 1)
				{
					return -1;
				}
				index = s.IndexOf(ch, index + 1);
				if (index == -1)
				{
					return (-1);
				}
			}
			return index;
		}

		/// <summary>
		/// This returns a string from decimal digit smallestDigit to decimal digit
		/// biggest digit.
		/// </summary>
		/// <remarks>
		/// This returns a string from decimal digit smallestDigit to decimal digit
		/// biggest digit. Smallest digit is labeled 1, and the limits are
		/// inclusive.
		/// </remarks>
		public static string Truncate(int n, int smallestDigit, int biggestDigit)
		{
			int numDigits = biggestDigit - smallestDigit + 1;
			char[] result = new char[numDigits];
			for (int j = 1; j < smallestDigit; j++)
			{
				n = n / 10;
			}
			for (int j_1 = numDigits - 1; j_1 >= 0; j_1--)
			{
				result[j_1] = (char)('0' + (n % 10));
				n = n / 10;
			}
			return new string(result);
		}

		/// <summary>Parses command line arguments into a Map.</summary>
		/// <remarks>
		/// Parses command line arguments into a Map. Arguments of the form
		/// <p/>
		/// <c>-flag1 arg1a arg1b ... arg1m -flag2 -flag3 arg3a ... arg3n</c>
		/// <p/>
		/// will be parsed so that the flag is a key in the Map (including
		/// the hyphen) and its value will be a
		/// <see cref="string"/>
		/// [] containing
		/// the optional arguments (if present).  The non-flag values not
		/// captured as flag arguments are collected into a String[] array
		/// and returned as the value of
		/// <see langword="null"/>
		/// in the Map.  In
		/// this invocation, flags cannot take arguments, so all the
		/// <see cref="string"/>
		/// array values other than the value for
		/// <see langword="null"/>
		/// will be zero-length.
		/// </remarks>
		/// <param name="args">A command-line arguments array</param>
		/// <returns>
		/// a
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// of flag names to flag argument
		/// <see cref="string"/>
		/// arrays.
		/// </returns>
		public static IDictionary<string, string[]> ArgsToMap(string[] args)
		{
			return ArgsToMap(args, new Dictionary<string,int>());
		}

		/// <summary>Parses command line arguments into a Map.</summary>
		/// <remarks>
		/// Parses command line arguments into a Map. Arguments of the form
		/// <p/>
		/// <c>-flag1 arg1a arg1b ... arg1m -flag2 -flag3 arg3a ... arg3n</c>
		/// <p/>
		/// will be parsed so that the flag is a key in the Map (including
		/// the hyphen) and its value will be a
		/// <see cref="string"/>
		/// [] containing
		/// the optional arguments (if present).  The non-flag values not
		/// captured as flag arguments are collected into a String[] array
		/// and returned as the value of
		/// <see langword="null"/>
		/// in the Map.  In
		/// this invocation, the maximum number of arguments for each flag
		/// can be specified as an
		/// <see cref="int"/>
		/// value of the appropriate
		/// flag key in the
		/// <paramref name="flagsToNumArgs"/>
		/// 
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// argument. (By default, flags cannot take arguments.)
		/// <p/>
		/// Example of usage:
		/// <p/>
		/// <code>
		/// Map flagsToNumArgs = new HashMap();
		/// flagsToNumArgs.put("-x",new Integer(2));
		/// flagsToNumArgs.put("-d",new Integer(1));
		/// Map result = argsToMap(args,flagsToNumArgs);
		/// </code>
		/// <p/>
		/// If a given flag appears more than once, the extra args are appended to
		/// the String[] value for that flag.
		/// </remarks>
		/// <param name="args">the argument array to be parsed</param>
		/// <param name="flagsToNumArgs">
		/// a
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// of flag names to
		/// <see cref="int"/>
		/// values specifying the number of arguments
		/// for that flag (default min 0, max 1).
		/// </param>
		/// <returns>
		/// a
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// of flag names to flag argument
		/// <see cref="string"/>
		/// </returns>
		public static IDictionary<string, string[]> ArgsToMap(string[] args, IDictionary<string, int> flagsToNumArgs)
		{
			IDictionary<string, string[]> result = new Dictionary<string, string[]>();
			IList<string> remainingArgs = new List<string>();
			for (int i = 0; i < args.Length; i++)
			{
				string key = args[i];
				if (key[0] == '-')
				{
					// found a flag
					int? numFlagArgs = flagsToNumArgs.ContainsKey(key) ? flagsToNumArgs[key] : (int?)null;
					int max = numFlagArgs.GetValueOrDefault(1);
					int min = numFlagArgs.GetValueOrDefault(0);
					IList<string> flagArgs = new List<string>();
					for (int j = 0; j < max && i + 1 < args.Length && (j < min || string.IsNullOrEmpty(args[i + 1]) || args[i + 1][0] != '-'); i++, j++)
					{
						flagArgs.Add(args[i + 1]);
					}
					if (result.ContainsKey(key))
					{
						// append the second specification into the args.
						result[key] = result[key].Concat(flagArgs).ToArray();
					}
					else
					{
						result.Add(key, flagArgs.ToArray());
					}
				}
				else
				{
					remainingArgs.Add(args[i]);
				}
			}
			result[null] = remainingArgs.ToArray();
			return result;
		}

		/// <summary>In this version each flag has zero or one argument.</summary>
		/// <remarks>
		/// In this version each flag has zero or one argument. It has one argument
		/// if there is a thing following a flag that does not begin with '-'.  See
		/// <see cref="ArgsToProperties(string[], System.Collections.Generic.IDictionary{K, V})"/>
		/// for full documentation.
		/// </remarks>
		/// <param name="args">Command line arguments</param>
		/// <returns>A Properties object representing the arguments.</returns>
		public static IConfiguration ArgsToProperties(params string[] args)
		{
			return ArgsToProperties(args, new Dictionary<string, int>());
		}

		/// <summary>
		/// Analogous to
		/// <see cref="ArgsToMap(string[])"/>
		/// .  However, there are several key differences between this method and
		/// <see cref="ArgsToMap(string[])"/>
		/// :
		/// <ul>
		/// <li> Hyphens are stripped from flag names </li>
		/// <li> Since Properties objects are String to String mappings, the default number of arguments to a flag is
		/// assumed to be 1 and not 0. </li>
		/// <li> Furthermore, the list of arguments not bound to a flag is mapped to the "" property, not null </li>
		/// <li> The special flags "-prop", "-props", "-properties", "-args", or "-arguments" will load the property file
		/// specified by its argument. </li>
		/// <li> The value for flags without arguments is set to "true" </li>
		/// <li> If a flag has multiple arguments, the value of the property is all
		/// of the arguments joined together with a space (" ") character between them.</li>
		/// <li> The value strings are trimmed so trailing spaces do not stop you from loading a file.</li>
		/// </ul>
		/// Properties are read from left to right, and later properties will override earlier ones with the same name.
		/// Properties loaded from a Properties file with the special args are defaults that can be overridden by command line
		/// flags (or earlier Properties files if there is nested usage of the special args.
		/// </summary>
		/// <param name="args">Command line arguments</param>
		/// <param name="flagsToNumArgs">Map of how many arguments flags should have. The keys are without the minus signs.</param>
		/// <returns>A Properties object representing the arguments.</returns>
		public static IConfiguration ArgsToProperties(string[] args, IDictionary<string, int> flagsToNumArgs)
		{
			var builder = new ConfigurationBuilder();
			IList<string> remainingArgs = new List<string>();
			IDictionary<string,string> result = new Dictionary<string,string>();
			for (int i = 0; i < args.Length; i++)
			{
				string key = args[i];
				if (!string.IsNullOrEmpty(key) && key[0] == '-')
				{
					// found a flag
					if (key.Length > 1 && key[1] == '-')
					{
						key = key.Substring(2);
					}
					else
					{
						// strip off 2 hyphens
						key = key.Substring(1);
					}
					// strip off the hyphen
					int? maxFlagArgs = flagsToNumArgs.ContainsKey(key) ? flagsToNumArgs[key] : (int?)null;
					int max = maxFlagArgs.GetValueOrDefault(1);
					int min = maxFlagArgs.GetValueOrDefault(0);
					if (maxFlagArgs != null && maxFlagArgs == 0 && i < args.Length - 1 && 
						(string.Equals("true", args[i + 1], StringComparison.OrdinalIgnoreCase) || string.Equals("false", args[i + 1],StringComparison.OrdinalIgnoreCase)))
					{
						max = 1;
					}
					// case: we're reading a boolean flag. TODO(gabor) there's gotta be a better way...
					IList<string> flagArgs = new List<string>();
					// cdm oct 2007: add length check to allow for empty string argument!
					for (int j = 0; j < max && i + 1 < args.Length && (j < min || string.IsNullOrEmpty(args[i + 1]) || args[i + 1][0] != '-'); i++, j++)
					{
						flagArgs.Add(args[i + 1]);
					}
					string value;
					if (!flagArgs.Any())
					{
						value = "true";
					}
					else
					{
						value = Join(flagArgs, " ");
					}
					if (string.Equals(key, Prop, StringComparison.OrdinalIgnoreCase) || string.Equals(key, Props, StringComparison.OrdinalIgnoreCase) || string.Equals(key, Properties, StringComparison.OrdinalIgnoreCase) || string.Equals(key, Arguments, StringComparison.OrdinalIgnoreCase) || string.Equals(key, Args, StringComparison.OrdinalIgnoreCase))
					{
						result.Add(Properties, value);
					}
					else
					{
						result.Add(key, value);
					}
				}
				else
				{
					remainingArgs.Add(args[i]);
				}
			}
			if (remainingArgs.Any())
			{
				result.Add(string.Empty, Join(remainingArgs, " "));
			}
			/* Processing in reverse order, add properties that aren't present only. Thus, later ones override earlier ones. */
			while (result.ContainsKey(Properties))
			{
				string file = result[Properties];
				builder.AddIniFile(file);
				result.Remove(Properties);
				// Properties toAdd = new Properties();
				// BufferedReader reader = null;
				// try
				// {
				// 	reader = IOUtils.ReaderFromString(file);
				// 	toAdd.Load(reader);
				// 	// trim all values
				// 	foreach (string propKey in toAdd.StringPropertyNames())
				// 	{
				// 		string newVal = toAdd.GetProperty(propKey);
				// 		toAdd.SetProperty(propKey, newVal.Trim());
				// 	}
				// }
				// catch (IOException e)
				// {
				// 	string msg = "argsToProperties could not read properties file: " + file;
				// 	throw new RuntimeIOException(msg, e);
				// }
				// finally
				// {
				// 	IOUtils.CloseIgnoringExceptions(reader);
				// }
				// foreach (string key in toAdd.StringPropertyNames())
				// {
				// 	string val = toAdd.GetProperty(key);
				// 	if (!result.Contains(key))
				// 	{
				// 		result.SetProperty(key, val);
				// 	}
				// }
			}
			return 	builder.AddInMemoryCollection(result).Build();
;
		}

		/// <summary>This method reads in properties listed in a file in the format prop=value, one property per line.</summary>
		/// <remarks>
		/// This method reads in properties listed in a file in the format prop=value, one property per line.
		/// Although
		/// <c>Properties.load(InputStream)</c>
		/// exists, I implemented this method to trim the lines,
		/// something not implemented in the
		/// <c>load()</c>
		/// method.
		/// </remarks>
		/// <param name="filename">A properties file to read</param>
		/// <returns>The corresponding Properties object</returns>
		public static IConfiguration PropFileToProperties(string filename)
		{
			try
			{
				var builder = new ConfigurationBuilder();
				builder.AddIniFile(filename);
				IConfiguration result = builder.Build();
				return result;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("propFileToProperties could not read properties file: " + filename, e);
			}
		}

		/// <summary>
		/// This method converts a comma-separated String (with whitespace
		/// optionally allowed after the comma) representing properties
		/// to a Properties object.
		/// </summary>
		/// <remarks>
		/// This method converts a comma-separated String (with whitespace
		/// optionally allowed after the comma) representing properties
		/// to a Properties object.  Each property is "property=value".  The value
		/// for properties without an explicitly given value is set to "true". This can be used for a 2nd level
		/// of properties, for example, when you have a commandline argument like "-outputOptions style=xml,tags".
		/// </remarks>
		public static IConfiguration StringToProperties(string str)
		{
			var builder = new ConfigurationBuilder();
			builder.AddInMemoryCollection();
			var result = builder.Build();
			return StringToProperties(str, result);
		}

		/// <summary>
		/// This method updates a Properties object based on
		/// a comma-separated String (with whitespace
		/// optionally allowed after the comma) representing properties
		/// to a Properties object.
		/// </summary>
		/// <remarks>
		/// This method updates a Properties object based on
		/// a comma-separated String (with whitespace
		/// optionally allowed after the comma) representing properties
		/// to a Properties object.  Each property is "property=value".  The value
		/// for properties without an explicitly given value is set to "true".
		/// </remarks>
		public static IConfiguration StringToProperties(string str, IConfiguration props)
		{
			string[] propsStr = Regex.Split(str.Trim(),",\\s*");
			foreach (string term in propsStr)
			{
				int divLoc = term.IndexOf('=');
				string key;
				string value;
				if (divLoc >= 0)
				{
					key = term.Substring(0, divLoc).Trim();
					value = term.Substring(divLoc + 1).Trim();
				}
				else
				{
					key = term.Trim();
					value = "true";
				}
				props[key] = value;
			}
			return props;
		}

		/// <summary>
		/// If any of the given list of properties are not found, returns the
		/// name of that property.
		/// </summary>
		/// <remarks>
		/// If any of the given list of properties are not found, returns the
		/// name of that property.  Otherwise, returns null.
		/// </remarks>
		public static string CheckRequiredProperties(IConfiguration props, params string[] requiredProps)
		{
			foreach (string required in requiredProps)
			{
				if (props[required] == null)
				{
					return required;
				}
			}
			return null;
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file already exists, appends if
		/// <c>append=true</c>
		/// , and overwrites if
		/// <c>append=false</c>
		/// .
		/// </remarks>
		public static void PrintToFile(FileInfo file, string message, bool append, bool printLn, string encoding)
		{
			StreamWriter fw = null;
			try
			{
				if (encoding != null)
				{
					fw = new StreamWriter(file.Open(append ? FileMode.Append : FileMode.Open), Encoding.GetEncoding(encoding));
				}
				else
				{
					fw = new StreamWriter(file.Open(append ? FileMode.Append : FileMode.Open));
				}
				if (printLn)
				{
					fw.WriteLine(message);
				}
				else
				{
					fw.Write(message);
				}
			}
			catch (Exception e)
			{
				log.Warn("Exception: in printToFile " + file.FullName);
				log.Warn(e);
			}
			finally
			{
				if (fw != null)
				{
					fw.Flush();
					fw.Close();
				}
			}
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file already exists, appends if
		/// <c>append=true</c>
		/// , and overwrites if
		/// <c>append=false</c>
		/// .
		/// </remarks>
		public static void PrintToFileLn(FileInfo file, string message, bool append)
		{
			TextWriter fw = null;
			try
			{
				fw = new StreamWriter(file.Open(append ? FileMode.Append : FileMode.Open));
				fw.WriteLine(message);
			}
			catch (Exception e)
			{
				log.Warn("Exception: in printToFileLn " + file.FullName + ' ' + message);
				log.Warn(e);
			}
			finally
			{
				if (fw != null)
				{
					fw.Flush();
					fw.Close();
				}
			}
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file already exists, appends if
		/// <c>append=true</c>
		/// , and overwrites if
		/// <c>append=false</c>
		/// .
		/// </remarks>
		public static void PrintToFile(FileInfo file, string message, bool append)
		{
			StreamWriter pw = null;
			try
			{
				pw = new StreamWriter(file.Open(append ? FileMode.Append : FileMode.Open));
				pw.Write(message);
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Exception in printToFile " + file.FullName, e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(pw);
			}
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file does not exist, rewrites the file;
		/// does not append.
		/// </remarks>
		public static void PrintToFile(FileInfo file, string message)
		{
			PrintToFile(file, message, false);
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file already exists, appends if
		/// <c>append=true</c>
		/// , and overwrites if
		/// <c>append=false</c>
		/// .
		/// </remarks>
		public static void PrintToFile(string filename, string message, bool append)
		{
			PrintToFile(new FileInfo(filename), message, append);
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file already exists, appends if
		/// <c>append=true</c>
		/// , and overwrites if
		/// <c>append=false</c>
		/// .
		/// </remarks>
		public static void PrintToFileLn(string filename, string message, bool append)
		{
			PrintToFileLn(new FileInfo(filename), message, append);
		}

		/// <summary>Prints to a file.</summary>
		/// <remarks>
		/// Prints to a file.  If the file does not exist, rewrites the file;
		/// does not append.
		/// </remarks>
		public static void PrintToFile(string filename, string message)
		{
			PrintToFile(new FileInfo(filename), message, false);
		}

		/// <summary>A simpler form of command line argument parsing.</summary>
		/// <remarks>
		/// A simpler form of command line argument parsing.
		/// Dan thinks this is highly superior to the overly complexified code that
		/// comes before it.
		/// Parses command line arguments into a Map. Arguments of the form
		/// -flag1 arg1 -flag2 -flag3 arg3
		/// will be parsed so that the flag is a key in the Map (including the hyphen)
		/// and the
		/// optional argument will be its value (if present).
		/// </remarks>
		/// <returns>A Map from keys to possible values (String or null)</returns>
		public static IDictionary<string, string> ParseCommandLineArguments(string[] args)
		{
			return (IDictionary<string,string>)ParseCommandLineArguments(args, false);
		}

		/// <summary>A simpler form of command line argument parsing.</summary>
		/// <remarks>
		/// A simpler form of command line argument parsing.
		/// Dan thinks this is highly superior to the overly complexified code that
		/// comes before it.
		/// Parses command line arguments into a Map. Arguments of the form
		/// -flag1 arg1 -flag2 -flag3 arg3
		/// will be parsed so that the flag is a key in the Map (including the hyphen)
		/// and the
		/// optional argument will be its value (if present).
		/// In this version, if the argument is numeric, it will be a Double value
		/// in the map, not a String.
		/// </remarks>
		/// <returns>A Map from keys to possible values (String or null)</returns>
		public static IDictionary<string, object> ParseCommandLineArguments(string[] args, bool parseNumbers)
		{
			IDictionary<string, object> result = Generics.NewHashMap();
			for (int i = 0; i < args.Length; i++)
			{
				string key = args[i];
				if (key[0] == '-')
				{
					if (i + 1 < args.Length)
					{
						string value = args[i + 1];
						if (value[0] != '-')
						{
							if (parseNumbers)
							{
								object numericValue = value;
								try
								{
									numericValue = double.Parse(value);
								}
								catch (NumberFormatException)
								{
								}
								// ignore
								result[key] = numericValue;
							}
							else
							{
								result[key] = value;
							}
							i++;
						}
						else
						{
							result[key] = null;
						}
					}
					else
					{
						result[key] = null;
					}
				}
			}
			return result;
		}

		public static string StripNonAlphaNumerics(string orig)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < orig.Length; i++)
			{
				char c = orig[i];
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		public static string StripSGML(string orig)
		{
			Regex sgmlPattern = new Regex("<.*?>", RegexOptions.Singleline);
			Match sgmlMatcher = sgmlPattern.Match(orig);
			return sgmlMatcher.ReplaceAll(string.Empty);
		}

		public static void PrintStringOneCharPerLine(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				int c = s[i];
				System.Console.Out.WriteLine(c + " \'" + (char)c + "\' ");
			}
		}

		public static string EscapeString(string s, char[] charsToEscape, char escapeChar)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == escapeChar)
				{
					result.Append(escapeChar);
				}
				else
				{
					foreach (char charToEscape in charsToEscape)
					{
						if (c == charToEscape)
						{
							result.Append(escapeChar);
							break;
						}
					}
				}
				result.Append(c);
			}
			return result.ToString();
		}

		/// <summary>
		/// This function splits the String s into multiple Strings using the
		/// splitChar.
		/// </summary>
		/// <remarks>
		/// This function splits the String s into multiple Strings using the
		/// splitChar.  However, it provides a quoting facility: it is possible to
		/// quote strings with the quoteChar.
		/// If the quoteChar occurs within the quotedExpression, it must be prefaced
		/// by the escapeChar.
		/// This routine can be useful for processing a line of a CSV file.
		/// </remarks>
		/// <param name="s">The String to split into fields. Cannot be null.</param>
		/// <param name="splitChar">The character to split on</param>
		/// <param name="quoteChar">The character to quote items with</param>
		/// <param name="escapeChar">The character to escape the quoteChar with</param>
		/// <returns>An array of Strings that s is split into</returns>
		public static string[] SplitOnCharWithQuoting(string s, char splitChar, char quoteChar, char escapeChar)
		{
			// todo [cdm 2018]: rewrite this to use code points so can work with any Unicode characters
			IList<string> result = new List<string>();
			int i = 0;
			int length = s.Length;
			StringBuilder b = new StringBuilder();
			while (i < length)
			{
				char curr = s[i];
				if (curr == splitChar)
				{
					// add last buffer
					// cdm 2014: Do this even if the field is empty!
					// if (b.length() > 0) {
					result.Add(b.ToString());
					b = new StringBuilder();
					// }
					i++;
				}
				else
				{
					if (curr == quoteChar)
					{
						// find next instance of quoteChar
						i++;
						while (i < length)
						{
							curr = s[i];
							// mrsmith: changed this condition from
							// if (curr == escapeChar) {
							if ((curr == escapeChar) && (i + 1 < length) && (s[i + 1] == quoteChar))
							{
								b.Append(s[i + 1]);
								i += 2;
							}
							else
							{
								if (curr == quoteChar)
								{
									i++;
									break;
								}
								else
								{
									// break this loop
									b.Append(s[i]);
									i++;
								}
							}
						}
					}
					else
					{
						b.Append(curr);
						i++;
					}
				}
			}
			// RFC 4180 disallows final comma. At any rate, don't produce a field after it unless non-empty
			if (b.Length > 0)
			{
				result.Add(b.ToString());
			}
			return result.ToArray();
		}

		/// <summary>Computes the longest common substring of s and t.</summary>
		/// <remarks>
		/// Computes the longest common substring of s and t.
		/// The longest common substring of a and b is the longest run of
		/// characters that appear in order inside both a and b. Both a and b
		/// may have other extraneous characters along the way. This is like
		/// edit distance but with no substitution and a higher number means
		/// more similar. For example, the LCS of "abcD" and "aXbc" is 3 (abc).
		/// </remarks>
		public static int LongestCommonSubstring(string s, string t)
		{
			int[][] d;
			// matrix
			int n;
			// length of s
			int m;
			// length of t
			int i;
			// iterates through s
			int j;
			// iterates through t
			// int cost; // cost
			// Step 1
			n = s.Length;
			m = t.Length;
			if (n == 0)
			{
				return 0;
			}
			if (m == 0)
			{
				return 0;
			}
			d = new int[][] {  };
			// Step 2
			for (i = 0; i <= n; i++)
			{
				d[i][0] = 0;
			}
			for (j = 0; j <= m; j++)
			{
				d[0][j] = 0;
			}
			// Step 3
			for (i = 1; i <= n; i++)
			{
				char s_i = s[i - 1];
				// ith character of s
				// Step 4
				for (j = 1; j <= m; j++)
				{
					char t_j = t[j - 1];
					// jth character of t
					// Step 5
					// js: if the chars match, you can get an extra point
					// otherwise you have to skip an insertion or deletion (no subs)
					if (s_i == t_j)
					{
						d[i][j] = SloppyMath.Max(d[i - 1][j], d[i][j - 1], d[i - 1][j - 1] + 1);
					}
					else
					{
						d[i][j] = System.Math.Max(d[i - 1][j], d[i][j - 1]);
					}
				}
			}
			/* ----
			// num chars needed to display longest num
			int numChars = (int) Math.ceil(Math.log(d[n][m]) / Math.log(10));
			for (i = 0; i < numChars + 3; i++) {
			log.info(' ');
			}
			for (j = 0; j < m; j++) {
			log.info(t.charAt(j) + " ");
			}
			log.info();
			for (i = 0; i <= n; i++) {
			log.info((i == 0 ? ' ' : s.charAt(i - 1)) + " ");
			for (j = 0; j <= m; j++) {
			log.info(d[i][j] + " ");
			}
			log.info();
			}
			---- */
			// Step 7
			return d[n][m];
		}

		/// <summary>Computes the longest common contiguous substring of s and t.</summary>
		/// <remarks>
		/// Computes the longest common contiguous substring of s and t.
		/// The LCCS is the longest run of characters that appear consecutively in
		/// both s and t. For instance, the LCCS of "color" and "colour" is 4, because
		/// of "colo".
		/// </remarks>
		public static int LongestCommonContiguousSubstring(string s, string t)
		{
			if (s.IsEmpty() || t.IsEmpty())
			{
				return 0;
			}
			int M = s.Length;
			int N = t.Length;
			int[][] d = new int[][] {  };
			for (int j = 0; j <= N; j++)
			{
				d[0][j] = 0;
			}
			for (int i = 0; i <= M; i++)
			{
				d[i][0] = 0;
			}
			int max = 0;
			for (int i_1 = 1; i_1 <= M; i_1++)
			{
				for (int j_1 = 1; j_1 <= N; j_1++)
				{
					if (s[i_1 - 1] == t[j_1 - 1])
					{
						d[i_1][j_1] = d[i_1 - 1][j_1 - 1] + 1;
					}
					else
					{
						d[i_1][j_1] = 0;
					}
					if (d[i_1][j_1] > max)
					{
						max = d[i_1][j_1];
					}
				}
			}
			// log.info("LCCS(" + s + "," + t + ") = " + max);
			return max;
		}

		/// <summary>Computes the Levenshtein (edit) distance of the two given Strings.</summary>
		/// <remarks>
		/// Computes the Levenshtein (edit) distance of the two given Strings.
		/// This method doesn't allow transposition, so one character transposed between two strings has a cost of 2 (one insertion, one deletion).
		/// The EditDistance class also implements the Levenshtein distance, but does allow transposition.
		/// </remarks>
		public static int EditDistance(string s, string t)
		{
			// Step 1
			int n = s.Length;
			// length of s
			int m = t.Length;
			// length of t
			if (n == 0)
			{
				return m;
			}
			if (m == 0)
			{
				return n;
			}
			int[][] d = new int[][] {  };
			// matrix
			// Step 2
			for (int i = 0; i <= n; i++)
			{
				d[i][0] = i;
			}
			for (int j = 0; j <= m; j++)
			{
				d[0][j] = j;
			}
			// Step 3
			for (int i_1 = 1; i_1 <= n; i_1++)
			{
				char s_i = s[i_1 - 1];
				// ith character of s
				// Step 4
				for (int j_1 = 1; j_1 <= m; j_1++)
				{
					char t_j = t[j_1 - 1];
					// jth character of t
					// Step 5
					int cost;
					// cost
					if (s_i == t_j)
					{
						cost = 0;
					}
					else
					{
						cost = 1;
					}
					// Step 6
					d[i_1][j_1] = SloppyMath.Min(d[i_1 - 1][j_1] + 1, d[i_1][j_1 - 1] + 1, d[i_1 - 1][j_1 - 1] + cost);
				}
			}
			// Step 7
			return d[n][m];
		}

		/// <summary>Computes the WordNet 2.0 POS tag corresponding to the PTB POS tag s.</summary>
		/// <param name="s">a Penn TreeBank POS tag.</param>
		public static string PennPOSToWordnetPOS(string s)
		{
			if (s.Matches("NN|NNP|NNS|NNPS"))
			{
				return "noun";
			}
			if (s.Matches("VB|VBD|VBG|VBN|VBZ|VBP|MD"))
			{
				return "verb";
			}
			if (s.Matches("JJ|JJR|JJS|CD"))
			{
				return "adjective";
			}
			if (s.Matches("RB|RBR|RBS|RP|WRB"))
			{
				return "adverb";
			}
			return null;
		}

		/// <summary>Returns a short class name for an object.</summary>
		/// <remarks>
		/// Returns a short class name for an object.
		/// This is the class name stripped of any package name.
		/// </remarks>
		/// <returns>
		/// The name of the class minus a package name, for example
		/// <c>ArrayList</c>
		/// </returns>
		public static string GetShortClassName(object o)
		{
			if (o == null)
			{
				return "null";
			}
			string name = o.GetType().FullName;
			int index = name.LastIndexOf('.');
			if (index >= 0)
			{
				name = Sharpen.Runtime.Substring(name, index + 1);
			}
			return name;
		}

		/// <summary>
		/// Converts a tab delimited string into an object with given fields
		/// Requires the object has setXxx functions for the specified fields
		/// </summary>
		/// <param name="objClass">Class of object to be created</param>
		/// <param name="str">string to convert</param>
		/// <param name="delimiterRegex">delimiter regular expression</param>
		/// <param name="fieldNames">fieldnames</param>
		/// <?/>
		/// <returns>Object created from string</returns>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="Java.Lang.NoSuchFieldException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public static T ColumnStringToObject<T>(Type objClass, string str, string delimiterRegex, string[] fieldNames)
		{
			Regex delimiterPattern = new Regex(delimiterRegex, RegexOptions.Compiled);
			return Edu.Stanford.Nlp.Util.StringUtils.ColumnStringToObject<T>(objClass, str, delimiterPattern, fieldNames);
		}

		/// <summary>
		/// Converts a tab delimited string into an object with given fields
		/// Requires the object has public access for the specified fields
		/// </summary>
		/// <param name="objClass">Class of object to be created</param>
		/// <param name="str">string to convert</param>
		/// <param name="delimiterPattern">delimiter</param>
		/// <param name="fieldNames">fieldnames</param>
		/// <?/>
		/// <returns>Object created from string</returns>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="Java.Lang.NoSuchFieldException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public static T ColumnStringToObject<T>(Type objClass, string str, Regex delimiterPattern, string[] fieldNames)
		{
			string[] fields = delimiterPattern.Split(str);
			T item = (T)System.Activator.CreateInstance(objClass);
			for (int i = 0; i < fields.Length; i++)
			{
				try
				{
					FieldInfo field = objClass.GetField(fieldNames[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					field.SetValue(item, fields[i]);
				}
				catch (MemberAccessException)
				{
					MethodInfo method = objClass.GetMethod("set" + Edu.Stanford.Nlp.Util.StringUtils.Capitalize(fieldNames[i]), new [] { typeof(string) });
					method.Invoke(item, new [] { fields[i] });
				}
			}
			return item;
		}

		/// <summary>
		/// Converts an object into a tab delimited string with given fields
		/// Requires the object has public access for the specified fields
		/// </summary>
		/// <param name="object">Object to convert</param>
		/// <param name="delimiter">delimiter</param>
		/// <param name="fieldNames">fieldnames</param>
		/// <returns>String representing object</returns>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="Java.Lang.NoSuchFieldException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public static string ObjectToColumnString(object @object, string delimiter, string[] fieldNames)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string fieldName in fieldNames)
			{
				if (sb.Length > 0)
				{
					sb.Append(delimiter);
				}
				try
				{
					FieldInfo field = Sharpen.Runtime.GetDeclaredField(@object.GetType(), fieldName);
					sb.Append(field.GetValue(@object));
				}
				catch (MemberAccessException)
				{
					MethodInfo method = Sharpen.Runtime.GetDeclaredMethod(@object.GetType(), "get" + Edu.Stanford.Nlp.Util.StringUtils.Capitalize(fieldName));
					sb.Append(method.Invoke(@object));
				}
			}
			return sb.ToString();
		}

		/// <summary>Uppercases the first character of a string.</summary>
		/// <param name="s">a string to capitalize</param>
		/// <returns>a capitalized version of the string</returns>
		public static string Capitalize(string s)
		{
			if (char.IsLowerCase(s[0]))
			{
				return char.ToUpperCase(s[0]) + Sharpen.Runtime.Substring(s, 1);
			}
			else
			{
				return s;
			}
		}

		/// <summary>Check if a string begins with an uppercase.</summary>
		/// <param name="s">a string</param>
		/// <returns>
		/// true if the string is capitalized
		/// false otherwise
		/// </returns>
		public static bool IsCapitalized(string s)
		{
			return (char.IsUpperCase(s[0]));
		}

		public static string SearchAndReplace(string text, string from, string to)
		{
			from = EscapeString(from, new char[] { '.', '[', ']', '\\' }, '\\');
			// special chars in regex
			Regex p = new Regex(from);
			Match m = p.Match(text);
			return m.ReplaceAll(to);
		}

		/// <summary>Returns an HTML table containing the matrix of Strings passed in.</summary>
		/// <remarks>
		/// Returns an HTML table containing the matrix of Strings passed in.
		/// The first dimension of the matrix should represent the rows, and the
		/// second dimension the columns.
		/// </remarks>
		public static string MakeHTMLTable(string[][] table, string[] rowLabels, string[] colLabels)
		{
			StringBuilder buff = new StringBuilder();
			buff.Append("<table class=\"auto\" border=\"1\" cellspacing=\"0\">\n");
			// top row
			buff.Append("<tr>\n");
			buff.Append("<td></td>\n");
			// the top left cell
			for (int j = 0; j < table[0].Length; j++)
			{
				// assume table is a rectangular matrix
				buff.Append("<td class=\"label\">").Append(colLabels[j]).Append("</td>\n");
			}
			buff.Append("</tr>\n");
			// all other rows
			for (int i = 0; i < table.Length; i++)
			{
				// one row
				buff.Append("<tr>\n");
				buff.Append("<td class=\"label\">").Append(rowLabels[i]).Append("</td>\n");
				for (int j_1 = 0; j_1 < table[i].Length; j_1++)
				{
					buff.Append("<td class=\"data\">");
					buff.Append(((table[i][j_1] != null) ? table[i][j_1] : string.Empty));
					buff.Append("</td>\n");
				}
				buff.Append("</tr>\n");
			}
			buff.Append("</table>");
			return buff.ToString();
		}

		/// <summary>Returns a text table containing the matrix of objects passed in.</summary>
		/// <remarks>
		/// Returns a text table containing the matrix of objects passed in.
		/// The first dimension of the matrix should represent the rows, and the
		/// second dimension the columns. Each object is printed in a cell with toString().
		/// The printing may be padded with spaces on the left and then on the right to
		/// ensure that the String form is of length at least padLeft or padRight.
		/// If tsv is true, a tab is put between columns.
		/// </remarks>
		/// <returns>A String form of the table</returns>
		public static string MakeTextTable(object[][] table, object[] rowLabels, object[] colLabels, int padLeft, int padRight, bool tsv)
		{
			StringBuilder buff = new StringBuilder();
			if (colLabels != null)
			{
				// top row
				buff.Append(MakeAsciiTableCell(string.Empty, padLeft, padRight, tsv));
				// the top left cell
				for (int j = 0; j < table[0].Length; j++)
				{
					// assume table is a rectangular matrix
					buff.Append(MakeAsciiTableCell(colLabels[j], padLeft, padRight, (j != table[0].Length - 1) && tsv));
				}
				buff.Append('\n');
			}
			// all other rows
			for (int i = 0; i < table.Length; i++)
			{
				// one row
				if (rowLabels != null)
				{
					buff.Append(MakeAsciiTableCell(rowLabels[i], padLeft, padRight, tsv));
				}
				for (int j = 0; j < table[i].Length; j++)
				{
					buff.Append(MakeAsciiTableCell(table[i][j], padLeft, padRight, (j != table[0].Length - 1) && tsv));
				}
				buff.Append('\n');
			}
			return buff.ToString();
		}

		/// <summary>The cell String is the string representation of the object.</summary>
		/// <remarks>
		/// The cell String is the string representation of the object.
		/// If padLeft is greater than 0, it is padded. Ditto right
		/// </remarks>
		private static string MakeAsciiTableCell(object obj, int padLeft, int padRight, bool tsv)
		{
			string result = obj.ToString();
			if (padLeft > 0)
			{
				result = PadLeft(result, padLeft);
			}
			if (padRight > 0)
			{
				result = Pad(result, padRight);
			}
			if (tsv)
			{
				result = result + '\t';
			}
			return result;
		}

		/// <summary>Tests the string edit distance function.</summary>
		public static void Main(string[] args)
		{
			string[] s = new string[] { "there once was a man", "this one is a manic", "hey there", "there once was a mane", "once in a manger.", "where is one match?", "Jo3seph Smarr!", "Joseph R Smarr" };
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					System.Console.Out.WriteLine("s1: " + s[i]);
					System.Console.Out.WriteLine("s2: " + s[j]);
					System.Console.Out.WriteLine("edit distance: " + EditDistance(s[i], s[j]));
					System.Console.Out.WriteLine("LCS:           " + LongestCommonSubstring(s[i], s[j]));
					System.Console.Out.WriteLine("LCCS:          " + LongestCommonContiguousSubstring(s[i], s[j]));
					System.Console.Out.WriteLine();
				}
			}
		}

		public static string ToAscii(string s)
		{
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c > 127)
				{
					string result = "?";
					if (c >= unchecked((int)(0x00c0)) && c <= unchecked((int)(0x00c5)))
					{
						result = "A";
					}
					else
					{
						if (c == unchecked((int)(0x00c6)))
						{
							result = "AE";
						}
						else
						{
							if (c == unchecked((int)(0x00c7)))
							{
								result = "C";
							}
							else
							{
								if (c >= unchecked((int)(0x00c8)) && c <= unchecked((int)(0x00cb)))
								{
									result = "E";
								}
								else
								{
									if (c >= unchecked((int)(0x00cc)) && c <= unchecked((int)(0x00cf)))
									{
										result = "F";
									}
									else
									{
										if (c == unchecked((int)(0x00d0)))
										{
											result = "D";
										}
										else
										{
											if (c == unchecked((int)(0x00d1)))
											{
												result = "N";
											}
											else
											{
												if (c >= unchecked((int)(0x00d2)) && c <= unchecked((int)(0x00d6)))
												{
													result = "O";
												}
												else
												{
													if (c == unchecked((int)(0x00d7)))
													{
														result = "x";
													}
													else
													{
														if (c == unchecked((int)(0x00d8)))
														{
															result = "O";
														}
														else
														{
															if (c >= unchecked((int)(0x00d9)) && c <= unchecked((int)(0x00dc)))
															{
																result = "U";
															}
															else
															{
																if (c == unchecked((int)(0x00dd)))
																{
																	result = "Y";
																}
																else
																{
																	if (c >= unchecked((int)(0x00e0)) && c <= unchecked((int)(0x00e5)))
																	{
																		result = "a";
																	}
																	else
																	{
																		if (c == unchecked((int)(0x00e6)))
																		{
																			result = "ae";
																		}
																		else
																		{
																			if (c == unchecked((int)(0x00e7)))
																			{
																				result = "c";
																			}
																			else
																			{
																				if (c >= unchecked((int)(0x00e8)) && c <= unchecked((int)(0x00eb)))
																				{
																					result = "e";
																				}
																				else
																				{
																					if (c >= unchecked((int)(0x00ec)) && c <= unchecked((int)(0x00ef)))
																					{
																						result = "i";
																					}
																					else
																					{
																						if (c == unchecked((int)(0x00f1)))
																						{
																							result = "n";
																						}
																						else
																						{
																							if (c >= unchecked((int)(0x00f2)) && c <= unchecked((int)(0x00f8)))
																							{
																								result = "o";
																							}
																							else
																							{
																								if (c >= unchecked((int)(0x00f9)) && c <= unchecked((int)(0x00fc)))
																								{
																									result = "u";
																								}
																								else
																								{
																									if (c >= unchecked((int)(0x00fd)) && c <= unchecked((int)(0x00ff)))
																									{
																										result = "y";
																									}
																									else
																									{
																										if (c >= unchecked((int)(0x2018)) && c <= unchecked((int)(0x2019)))
																										{
																											result = "\'";
																										}
																										else
																										{
																											if (c >= unchecked((int)(0x201c)) && c <= unchecked((int)(0x201e)))
																											{
																												result = "\"";
																											}
																											else
																											{
																												if (c >= unchecked((int)(0x0213)) && c <= unchecked((int)(0x2014)))
																												{
																													result = "-";
																												}
																												else
																												{
																													if (c >= unchecked((int)(0x00A2)) && c <= unchecked((int)(0x00A5)))
																													{
																														result = "$";
																													}
																													else
																													{
																														if (c == unchecked((int)(0x2026)))
																														{
																															result = ".";
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
											}
										}
									}
								}
							}
						}
					}
					b.Append(result);
				}
				else
				{
					b.Append(c);
				}
			}
			return b.ToString();
		}

		public static string ToCSVString(string[] fields)
		{
			StringBuilder b = new StringBuilder();
			foreach (string fld in fields)
			{
				if (b.Length > 0)
				{
					b.Append(',');
				}
				string field = EscapeString(fld, new char[] { '\"' }, '\"');
				// escape quotes with double quotes
				b.Append('\"').Append(field).Append('\"');
			}
			return b.ToString();
		}

		/// <summary>
		/// Swap any occurrences of any characters in the from String in the input String with
		/// the corresponding character from the to String.
		/// </summary>
		/// <remarks>
		/// Swap any occurrences of any characters in the from String in the input String with
		/// the corresponding character from the to String.  As Perl tr, for example,
		/// tr("chris", "irs", "mop").equals("chomp"), except it does not
		/// support regular expression character ranges.
		/// <p>
		/// <i>Note:</i> This is now optimized to not allocate any objects if the
		/// input is returned unchanged.
		/// </remarks>
		public static string Tr(string input, string from, string to)
		{
			System.Diagnostics.Debug.Assert(from.Length == to.Length);
			StringBuilder sb = null;
			int len = input.Length;
			for (int i = 0; i < len; i++)
			{
				int ind = from.IndexOf(input[i]);
				if (ind >= 0)
				{
					if (sb == null)
					{
						sb = new StringBuilder(input);
					}
					Sharpen.Runtime.SetCharAt(sb, i, to[ind]);
				}
			}
			if (sb == null)
			{
				return input;
			}
			else
			{
				return sb.ToString();
			}
		}

		/// <summary>Returns the supplied string with any trailing '\n' or '\r\n' removed.</summary>
		public static string Chomp(string s)
		{
			if (s == null)
			{
				return null;
			}
			int l_1 = s.Length - 1;
			if (l_1 >= 0 && s[l_1] == '\n')
			{
				int l_2 = l_1 - 1;
				if (l_2 >= 0 && s[l_2] == '\r')
				{
					return Sharpen.Runtime.Substring(s, 0, l_2);
				}
				else
				{
					return Sharpen.Runtime.Substring(s, 0, l_1);
				}
			}
			else
			{
				return s;
			}
		}

		/// <summary>
		/// Returns the result of calling toString() on the supplied Object, but with
		/// any trailing '\n' or '\r\n' removed.
		/// </summary>
		public static string Chomp(object o)
		{
			return Chomp(o.ToString());
		}

		/// <summary>Strip directory from filename.</summary>
		/// <remarks>
		/// Strip directory from filename.  Like Unix 'basename'. <p/>
		/// Example:
		/// <c>getBaseName("/u/wcmac/foo.txt") ==&gt; "foo.txt"</c>
		/// </remarks>
		public static string GetBaseName(string fileName)
		{
			return GetBaseName(fileName, string.Empty);
		}

		/// <summary>Strip directory and suffix from filename.</summary>
		/// <remarks>
		/// Strip directory and suffix from filename.  Like Unix 'basename'.
		/// Example:
		/// <c>getBaseName("/u/wcmac/foo.txt", "") ==&gt; "foo.txt"</c>
		/// <br/>
		/// Example:
		/// <c>getBaseName("/u/wcmac/foo.txt", ".txt") ==&gt; "foo"</c>
		/// <br/>
		/// Example:
		/// <c>getBaseName("/u/wcmac/foo.txt", ".pdf") ==&gt; "foo.txt"</c>
		/// <br/>
		/// </remarks>
		public static string GetBaseName(string fileName, string suffix)
		{
			return GetBaseName(fileName, suffix, "/");
		}

		/// <summary>Strip directory and suffix from the given name.</summary>
		/// <remarks>
		/// Strip directory and suffix from the given name.  Like Unix 'basename'.
		/// Example:
		/// <c>getBaseName("/tmp/foo/bar/foo", "", "/") ==&gt; "foo"</c>
		/// <br/>
		/// Example:
		/// <c>getBaseName("edu.stanford.nlp", "", "\\.") ==&gt; "nlp"</c>
		/// <br/>
		/// </remarks>
		public static string GetBaseName(string fileName, string suffix, string sep)
		{
			string[] elts = fileName.Split(sep);
			if (elts.Length == 0)
			{
				return string.Empty;
			}
			string lastElt = elts[elts.Length - 1];
			if (lastElt.EndsWith(suffix))
			{
				lastElt = Sharpen.Runtime.Substring(lastElt, 0, lastElt.Length - suffix.Length);
			}
			return lastElt;
		}

		/// <summary>Given a String the method uses Regex to check if the String only contains alphabet characters</summary>
		/// <param name="s">a String to check using regex</param>
		/// <returns>true if the String is valid</returns>
		public static bool IsAlpha(string s)
		{
			Regex p = new Regex("^[\\p{Alpha}\\s]+$");
			Match m = p.Match(s);
			return m.Matches();
		}

		/// <summary>Given a String the method uses Regex to check if the String only contains numeric characters</summary>
		/// <param name="s">a String to check using regex</param>
		/// <returns>true if the String is valid</returns>
		public static bool IsNumeric(string s)
		{
			Regex p = new Regex("^[\\p{Digit}\\s\\.]+$");
			Match m = p.Match(s);
			return m.Matches();
		}

		/// <summary>Given a String the method uses Regex to check if the String only contains alphanumeric characters</summary>
		/// <param name="s">a String to check using regex</param>
		/// <returns>true if the String is valid</returns>
		public static bool IsAlphanumeric(string s)
		{
			Regex p = new Regex("^[\\p{Alnum}\\s\\.]+$");
			Match m = p.Match(s);
			return m.Matches();
		}

		/// <summary>Given a String the method uses Regex to check if the String only contains punctuation characters</summary>
		/// <param name="s">a String to check using regex</param>
		/// <returns>true if the String is valid</returns>
		public static bool IsPunct(string s)
		{
			Regex p = new Regex("^[\\p{Punct}]+$");
			Match m = p.Match(s);
			return m.Matches();
		}

		/// <summary>Given a String the method uses Regex to check if the String looks like an acronym</summary>
		/// <param name="s">a String to check using regex</param>
		/// <returns>true if the String is valid</returns>
		public static bool IsAcronym(string s)
		{
			Regex p = new Regex("^[\\p{Upper}]+$");
			Match m = p.Match(s);
			return m.Matches();
		}

		public static string GetNotNullString(string s)
		{
			if (s == null)
			{
				return string.Empty;
			}
			else
			{
				return s;
			}
		}

		/// <summary>Returns whether a String is either null or empty.</summary>
		/// <remarks>
		/// Returns whether a String is either null or empty.
		/// (Copies the Guava method for this.)
		/// </remarks>
		/// <param name="str">The String to test</param>
		/// <returns>Whether the String is either null or empty</returns>
		public static bool IsNullOrEmpty(string str)
		{
			return str == null || str.IsEmpty();
		}

		/// <summary>Resolve variable.</summary>
		/// <remarks>
		/// Resolve variable. If it is the props file, then substitute that variable with
		/// the value mentioned in the props file, otherwise look for the variable in the environment variables.
		/// If the variable is not found then substitute it for empty string.
		/// </remarks>
		public static string ResolveVars(string str, IDictionary props)
		{
			if (str == null)
			{
				return null;
			}
			// ${VAR_NAME} or $VAR_NAME
			Regex p = new Regex("\\$\\{(\\w+)\\}");
			Match m = p.Match(str);
			StringBuilder sb = new StringBuilder();
			while (m.Find())
			{
				string varName = null == m.Group(1) ? m.Group(2) : m.Group(1);
				string vrValue;
				//either in the props file
				if (props.Contains(varName))
				{
					vrValue = ((string)props[varName]);
				}
				else
				{
					//or as the environment variable
					vrValue = Runtime.Getenv(varName);
				}
				m.AppendReplacement(sb, null == vrValue ? string.Empty : vrValue);
			}
			m.AppendTail(sb);
			return sb.ToString();
		}

		/// <summary>convert args to properties with variable names resolved.</summary>
		/// <remarks>
		/// convert args to properties with variable names resolved. for each value
		/// having a ${VAR} or $VAR, its value is first resolved using the variables
		/// listed in the props file, and if not found then using the environment
		/// variables. if the variable is not found then substitute it for empty string
		/// </remarks>
		public static Properties ArgsToPropertiesWithResolve(string[] args)
		{
			LinkedHashMap<string, string> result = new LinkedHashMap<string, string>();
			IDictionary<string, string> existingArgs = new LinkedHashMap<string, string>();
			for (int i = 0; i < args.Length; i++)
			{
				string key = args[i];
				if (key.Length > 0 && key[0] == '-')
				{
					// found a flag
					if (key.Length > 1 && key[1] == '-')
					{
						key = Sharpen.Runtime.Substring(key, 2);
					}
					else
					{
						// strip off 2 hyphens
						key = Sharpen.Runtime.Substring(key, 1);
					}
					// strip off the hyphen
					int max = 1;
					int min = 0;
					IList<string> flagArgs = new List<string>();
					// cdm oct 2007: add length check to allow for empty string argument!
					for (int j = 0; j < max && i + 1 < args.Length && (j < min || args[i + 1].IsEmpty() || args[i + 1][0] != '-'); i++, j++)
					{
						flagArgs.Add(args[i + 1]);
					}
					if (flagArgs.IsEmpty())
					{
						existingArgs[key] = "true";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(key, Prop) || Sharpen.Runtime.EqualsIgnoreCase(key, Props) || Sharpen.Runtime.EqualsIgnoreCase(key, Properties) || Sharpen.Runtime.EqualsIgnoreCase(key, Arguments) || Sharpen.Runtime.EqualsIgnoreCase(key, 
							Args))
						{
							foreach (string flagArg in flagArgs)
							{
								result.PutAll(PropFileToLinkedHashMap(flagArg, existingArgs));
							}
							existingArgs.Clear();
						}
						else
						{
							existingArgs[key] = Join(flagArgs, " ");
						}
					}
				}
			}
			result.PutAll(existingArgs);
			foreach (KeyValuePair<string, string> o in result)
			{
				string val = ResolveVars(o.Value, result);
				result[o.Key] = val;
			}
			Properties props = new Properties();
			props.PutAll(result);
			return props;
		}

		/// <summary>
		/// This method reads in properties listed in a file in the format prop=value,
		/// one property per line.
		/// </summary>
		/// <remarks>
		/// This method reads in properties listed in a file in the format prop=value,
		/// one property per line. and reads them into a LinkedHashMap (insertion order preserving)
		/// Flags not having any arguments is set to "true".
		/// </remarks>
		/// <param name="filename">A properties file to read</param>
		/// <returns>
		/// The corresponding LinkedHashMap where the ordering is the same as in the
		/// props file
		/// </returns>
		public static LinkedHashMap<string, string> PropFileToLinkedHashMap(string filename, IDictionary<string, string> existingArgs)
		{
			LinkedHashMap<string, string> result = new LinkedHashMap<string, string>(existingArgs);
			foreach (string l in IOUtils.ReadLines(filename))
			{
				l = l.Trim();
				if (l.IsEmpty() || l.StartsWith("#"))
				{
					continue;
				}
				int index = l.IndexOf('=');
				if (index == -1)
				{
					result[l] = "true";
				}
				else
				{
					result[Sharpen.Runtime.Substring(l, 0, index).Trim()] = Sharpen.Runtime.Substring(l, index + 1).Trim();
				}
			}
			return result;
		}

		/// <summary>n grams for already split string.</summary>
		/// <remarks>n grams for already split string. the ngrams are joined with a single space</remarks>
		public static ICollection<string> GetNgrams(IList<string> words, int minSize, int maxSize)
		{
			IList<IList<string>> ng = CollectionUtils.GetNGrams(words, minSize, maxSize);
			ICollection<string> ngrams = new List<string>();
			foreach (IList<string> n in ng)
			{
				ngrams.Add(Edu.Stanford.Nlp.Util.StringUtils.Join(n, " "));
			}
			return ngrams;
		}

		/// <summary>n grams for already split string.</summary>
		/// <remarks>n grams for already split string. the ngrams are joined with a single space</remarks>
		public static ICollection<string> GetNgramsFromTokens(IList<CoreLabel> words, int minSize, int maxSize)
		{
			IList<string> wordsStr = new List<string>();
			foreach (CoreLabel l in words)
			{
				wordsStr.Add(l.Word());
			}
			IList<IList<string>> ng = CollectionUtils.GetNGrams(wordsStr, minSize, maxSize);
			ICollection<string> ngrams = new List<string>();
			foreach (IList<string> n in ng)
			{
				ngrams.Add(Edu.Stanford.Nlp.Util.StringUtils.Join(n, " "));
			}
			return ngrams;
		}

		/// <summary>The string is split on whitespace and the ngrams are joined with a single space</summary>
		public static ICollection<string> GetNgramsString(string s, int minSize, int maxSize)
		{
			return GetNgrams(Arrays.AsList(s.Split("\\s+")), minSize, maxSize);
		}

		/// <summary>Build a list of character-based ngrams from the given string.</summary>
		public static ICollection<string> GetCharacterNgrams(string s, int minSize, int maxSize)
		{
			ICollection<string> ngrams = new List<string>();
			int len = s.Length;
			for (int i = 0; i < len; i++)
			{
				for (int ngramSize = minSize; ngramSize > 0 && ngramSize <= maxSize && i + ngramSize <= len; ngramSize++)
				{
					ngrams.Add(Sharpen.Runtime.Substring(s, i, i + ngramSize));
				}
			}
			return ngrams;
		}

		private static Regex diacriticalMarksPattern = new Regex("\\p{InCombiningDiacriticalMarks}");

		public static string Normalize(string s)
		{
			// Normalizes string and strips diacritics (map to ascii) by
			// 1. taking the NFKD (compatibility decomposition -
			//   in compatibility equivalence, formatting such as subscripting is lost -
			//   see http://unicode.org/reports/tr15/)
			// 2. Removing diacriticals
			// 3. Recombining into NFKC form (compatibility composition)
			// This process may be slow.
			//
			// The main purpose of the function is to remove diacritics for asciis,
			//  but it may normalize other stuff as well.
			// A more conservative approach is to do explicit folding just for ascii character
			//   (see RuleBasedNameMatcher.normalize)
			string d = Normalizer.Normalize(s, Normalizer.Form.Nfkd);
			d = diacriticalMarksPattern.Match(d).ReplaceAll(string.Empty);
			return Normalizer.Normalize(d, Normalizer.Form.Nfkc);
		}

		/// <summary>Convert a list of labels into a string, by simply joining them with spaces.</summary>
		/// <param name="words">The words to join.</param>
		/// <returns>A string representation of the sentence, tokenized by a single space.</returns>
		public static string ToString(IList<CoreLabel> words)
		{
			return Join(words.Stream().Map(null), " ");
		}

		/// <summary>Convert a CoreMap representing a sentence into a string, by simply joining them with spaces.</summary>
		/// <param name="sentence">The sentence to stringify.</param>
		/// <returns>A string representation of the sentence, tokenized by a single space.</returns>
		public static string ToString(ICoreMap sentence)
		{
			return ToString(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
		}

		/// <summary>I shamefully stole this from: http://rosettacode.org/wiki/Levenshtein_distance#Java --Gabor</summary>
		public static int LevenshteinDistance(string s1, string s2)
		{
			s1 = s1.ToLower();
			s2 = s2.ToLower();
			int[] costs = new int[s2.Length + 1];
			for (int i = 0; i <= s1.Length; i++)
			{
				int lastValue = i;
				for (int j = 0; j <= s2.Length; j++)
				{
					if (i == 0)
					{
						costs[j] = j;
					}
					else
					{
						if (j > 0)
						{
							int newValue = costs[j - 1];
							if (s1[i - 1] != s2[j - 1])
							{
								newValue = System.Math.Min(System.Math.Min(newValue, lastValue), costs[j]) + 1;
							}
							costs[j - 1] = lastValue;
							lastValue = newValue;
						}
					}
				}
				if (i > 0)
				{
					costs[s2.Length] = lastValue;
				}
			}
			return costs[s2.Length];
		}

		/// <summary>I shamefully stole this from: http://rosettacode.org/wiki/Levenshtein_distance#Java --Gabor</summary>
		public static int LevenshteinDistance<E>(E[] s1, E[] s2)
		{
			int[] costs = new int[s2.Length + 1];
			for (int i = 0; i <= s1.Length; i++)
			{
				int lastValue = i;
				for (int j = 0; j <= s2.Length; j++)
				{
					if (i == 0)
					{
						costs[j] = j;
					}
					else
					{
						if (j > 0)
						{
							int newValue = costs[j - 1];
							if (!s1[i - 1].Equals(s2[j - 1]))
							{
								newValue = System.Math.Min(System.Math.Min(newValue, lastValue), costs[j]) + 1;
							}
							costs[j - 1] = lastValue;
							lastValue = newValue;
						}
					}
				}
				if (i > 0)
				{
					costs[s2.Length] = lastValue;
				}
			}
			return costs[s2.Length];
		}

		/// <summary>Unescape an HTML string.</summary>
		/// <remarks>
		/// Unescape an HTML string.
		/// Taken from: http://stackoverflow.com/questions/994331/java-how-to-decode-html-character-entities-in-java-like-httputility-htmldecode
		/// </remarks>
		/// <param name="input">The string to unescape</param>
		/// <returns>The unescaped String</returns>
		public static string UnescapeHtml3(string input)
		{
			StringWriter writer = null;
			int len = input.Length;
			int i = 1;
			int st = 0;
			while (true)
			{
				// look for '&'
				while (i < len && input[i - 1] != '&')
				{
					i++;
				}
				if (i >= len)
				{
					break;
				}
				// found '&', look for ';'
				int j = i;
				while (j < len && j < i + 6 + 1 && input[j] != ';')
				{
					j++;
				}
				if (j == len || j < i + 2 || j == i + 6 + 1)
				{
					i++;
					continue;
				}
				// found escape
				if (input[i] == '#')
				{
					// numeric escape
					int k = i + 1;
					int radix = 10;
					char firstChar = input[k];
					if (firstChar == 'x' || firstChar == 'X')
					{
						k++;
						radix = 16;
					}
					try
					{
						int entityValue = System.Convert.ToInt32(Sharpen.Runtime.Substring(input, k, j), radix);
						if (writer == null)
						{
							writer = new StringWriter(input.Length);
						}
						writer.Append(Sharpen.Runtime.Substring(input, st, i - 1));
						if (entityValue > unchecked((int)(0xFFFF)))
						{
							char[] chrs = char.ToChars(entityValue);
							writer.Write(chrs[0]);
							writer.Write(chrs[1]);
						}
						else
						{
							writer.Write(entityValue);
						}
					}
					catch (NumberFormatException)
					{
						i++;
						continue;
					}
				}
				else
				{
					// named escape
					ICharSequence value = htmlUnescapeLookupMap[Sharpen.Runtime.Substring(input, i, j)];
					if (value == null)
					{
						i++;
						continue;
					}
					if (writer == null)
					{
						writer = new StringWriter(input.Length);
					}
					writer.Append(input.Substring(st, i - 1));
					writer.Append(value);
				}
				// skip escape
				st = j + 1;
				i = st;
			}
			if (writer != null)
			{
				writer.Append(Sharpen.Runtime.Substring(input, st, len));
				return writer.ToString();
			}
			return input;
		}

		private static readonly string[][] HtmlEscapes = new string[][] { new string[] { "\"", "quot" }, new string[] { "&", "amp" }, new string[] { "<", "lt" }, new string[] { ">", "gt" }, new string[] { "-", "ndash" }, new string[] { "\u00A0", "nbsp"
			 }, new string[] { "\u00A1", "iexcl" }, new string[] { "\u00A2", "cent" }, new string[] { "\u00A3", "pound" }, new string[] { "\u00A4", "curren" }, new string[] { "\u00A5", "yen" }, new string[] { "\u00A6", "brvbar" }, new string[] { "\u00A7"
			, "sect" }, new string[] { "\u00A8", "uml" }, new string[] { "\u00A9", "copy" }, new string[] { "\u00AA", "ordf" }, new string[] { "\u00AB", "laquo" }, new string[] { "\u00AC", "not" }, new string[] { "\u00AD", "shy" }, new string[] { "\u00AE"
			, "reg" }, new string[] { "\u00AF", "macr" }, new string[] { "\u00B0", "deg" }, new string[] { "\u00B1", "plusmn" }, new string[] { "\u00B2", "sup2" }, new string[] { "\u00B3", "sup3" }, new string[] { "\u00B4", "acute" }, new string[] { "\u00B5"
			, "micro" }, new string[] { "\u00B6", "para" }, new string[] { "\u00B7", "middot" }, new string[] { "\u00B8", "cedil" }, new string[] { "\u00B9", "sup1" }, new string[] { "\u00BA", "ordm" }, new string[] { "\u00BB", "raquo" }, new string[] 
			{ "\u00BC", "frac14" }, new string[] { "\u00BD", "frac12" }, new string[] { "\u00BE", "frac34" }, new string[] { "\u00BF", "iquest" }, new string[] { "\u00C0", "Agrave" }, new string[] { "\u00C1", "Aacute" }, new string[] { "\u00C2", "Acirc"
			 }, new string[] { "\u00C3", "Atilde" }, new string[] { "\u00C4", "Auml" }, new string[] { "\u00C5", "Aring" }, new string[] { "\u00C6", "AElig" }, new string[] { "\u00C7", "Ccedil" }, new string[] { "\u00C8", "Egrave" }, new string[] { "\u00C9"
			, "Eacute" }, new string[] { "\u00CA", "Ecirc" }, new string[] { "\u00CB", "Euml" }, new string[] { "\u00CC", "Igrave" }, new string[] { "\u00CD", "Iacute" }, new string[] { "\u00CE", "Icirc" }, new string[] { "\u00CF", "Iuml" }, new string
			[] { "\u00D0", "ETH" }, new string[] { "\u00D1", "Ntilde" }, new string[] { "\u00D2", "Ograve" }, new string[] { "\u00D3", "Oacute" }, new string[] { "\u00D4", "Ocirc" }, new string[] { "\u00D5", "Otilde" }, new string[] { "\u00D6", "Ouml" }
			, new string[] { "\u00D7", "times" }, new string[] { "\u00D8", "Oslash" }, new string[] { "\u00D9", "Ugrave" }, new string[] { "\u00DA", "Uacute" }, new string[] { "\u00DB", "Ucirc" }, new string[] { "\u00DC", "Uuml" }, new string[] { "\u00DD"
			, "Yacute" }, new string[] { "\u00DE", "THORN" }, new string[] { "\u00DF", "szlig" }, new string[] { "\u00E0", "agrave" }, new string[] { "\u00E1", "aacute" }, new string[] { "\u00E2", "acirc" }, new string[] { "\u00E3", "atilde" }, new string
			[] { "\u00E4", "auml" }, new string[] { "\u00E5", "aring" }, new string[] { "\u00E6", "aelig" }, new string[] { "\u00E7", "ccedil" }, new string[] { "\u00E8", "egrave" }, new string[] { "\u00E9", "eacute" }, new string[] { "\u00EA", "ecirc"
			 }, new string[] { "\u00EB", "euml" }, new string[] { "\u00EC", "igrave" }, new string[] { "\u00ED", "iacute" }, new string[] { "\u00EE", "icirc" }, new string[] { "\u00EF", "iuml" }, new string[] { "\u00F0", "eth" }, new string[] { "\u00F1"
			, "ntilde" }, new string[] { "\u00F2", "ograve" }, new string[] { "\u00F3", "oacute" }, new string[] { "\u00F4", "ocirc" }, new string[] { "\u00F5", "otilde" }, new string[] { "\u00F6", "ouml" }, new string[] { "\u00F7", "divide" }, new string
			[] { "\u00F8", "oslash" }, new string[] { "\u00F9", "ugrave" }, new string[] { "\u00FA", "uacute" }, new string[] { "\u00FB", "ucirc" }, new string[] { "\u00FC", "uuml" }, new string[] { "\u00FD", "yacute" }, new string[] { "\u00FE", "thorn"
			 }, new string[] { "\u00FF", "yuml" } };

		private static readonly Dictionary<string, ICharSequence> htmlUnescapeLookupMap;

		static StringUtils()
		{
			// " - double-quote
			// & - ampersand
			// < - less-than
			// > - greater-than
			// - - dash
			// Mapping to escape ISO-8859-1 characters to their named HTML 3.x equivalents.
			// non-breaking space
			// inverted exclamation mark
			// cent sign
			// pound sign
			// currency sign
			// yen sign = yuan sign
			// broken bar = broken vertical bar
			// section sign
			// diaeresis = spacing diaeresis
			// © - copyright sign
			// feminine ordinal indicator
			// left-pointing double angle quotation mark = left pointing guillemet
			// not sign
			// soft hyphen = discretionary hyphen
			// ® - registered trademark sign
			// macron = spacing macron = overline = APL overbar
			// degree sign
			// plus-minus sign = plus-or-minus sign
			// superscript two = superscript digit two = squared
			// superscript three = superscript digit three = cubed
			// acute accent = spacing acute
			// micro sign
			// pilcrow sign = paragraph sign
			// middle dot = Georgian comma = Greek middle dot
			// cedilla = spacing cedilla
			// superscript one = superscript digit one
			// masculine ordinal indicator
			// right-pointing double angle quotation mark = right pointing guillemet
			// vulgar fraction one quarter = fraction one quarter
			// vulgar fraction one half = fraction one half
			// vulgar fraction three quarters = fraction three quarters
			// inverted question mark = turned question mark
			// А - uppercase A, grave accent
			// Б - uppercase A, acute accent
			// В - uppercase A, circumflex accent
			// Г - uppercase A, tilde
			// Д - uppercase A, umlaut
			// Е - uppercase A, ring
			// Ж - uppercase AE
			// З - uppercase C, cedilla
			// И - uppercase E, grave accent
			// Й - uppercase E, acute accent
			// К - uppercase E, circumflex accent
			// Л - uppercase E, umlaut
			// М - uppercase I, grave accent
			// Н - uppercase I, acute accent
			// О - uppercase I, circumflex accent
			// П - uppercase I, umlaut
			// Р - uppercase Eth, Icelandic
			// С - uppercase N, tilde
			// Т - uppercase O, grave accent
			// У - uppercase O, acute accent
			// Ф - uppercase O, circumflex accent
			// Х - uppercase O, tilde
			// Ц - uppercase O, umlaut
			// multiplication sign
			// Ш - uppercase O, slash
			// Щ - uppercase U, grave accent
			// Ъ - uppercase U, acute accent
			// Ы - uppercase U, circumflex accent
			// Ь - uppercase U, umlaut
			// Э - uppercase Y, acute accent
			// Ю - uppercase THORN, Icelandic
			// Я - lowercase sharps, German
			// а - lowercase a, grave accent
			// б - lowercase a, acute accent
			// в - lowercase a, circumflex accent
			// г - lowercase a, tilde
			// д - lowercase a, umlaut
			// е - lowercase a, ring
			// ж - lowercase ae
			// з - lowercase c, cedilla
			// и - lowercase e, grave accent
			// й - lowercase e, acute accent
			// к - lowercase e, circumflex accent
			// л - lowercase e, umlaut
			// м - lowercase i, grave accent
			// н - lowercase i, acute accent
			// о - lowercase i, circumflex accent
			// п - lowercase i, umlaut
			// р - lowercase eth, Icelandic
			// с - lowercase n, tilde
			// т - lowercase o, grave accent
			// у - lowercase o, acute accent
			// ф - lowercase o, circumflex accent
			// х - lowercase o, tilde
			// ц - lowercase o, umlaut
			// division sign
			// ш - lowercase o, slash
			// щ - lowercase u, grave accent
			// ъ - lowercase u, acute accent
			// ы - lowercase u, circumflex accent
			// ь - lowercase u, umlaut
			// э - lowercase y, acute accent
			// ю - lowercase thorn, Icelandic
			// я - lowercase y, umlaut
			htmlUnescapeLookupMap = new Dictionary<string, ICharSequence>();
			foreach (ICharSequence[] seq in HtmlEscapes)
			{
				htmlUnescapeLookupMap[seq[1].ToString()] = seq[0];
			}
		}

		/// <summary>Decode an array encoded as a String.</summary>
		/// <remarks>
		/// Decode an array encoded as a String. This entails a comma separated value enclosed in brackets
		/// or parentheses.
		/// </remarks>
		/// <param name="encoded">The String encoding an array</param>
		/// <returns>A String array corresponding to the encoded array</returns>
		public static string[] DecodeArray(string encoded)
		{
			if (encoded.IsEmpty())
			{
				return EmptyStringArray;
			}
			char[] chars = encoded.Trim().ToCharArray();
			//--Parse the String
			// (state)
			char quoteCloseChar = (char)0;
			IList<string> terms = new List<string>();
			StringBuilder current = new StringBuilder();
			//(start/stop overhead)
			int start = 0;
			int end = chars.Length;
			if (chars[0] == '(')
			{
				start += 1;
				end -= 1;
				if (chars[end] != ')')
				{
					throw new ArgumentException("Unclosed paren in encoded array: " + encoded);
				}
			}
			if (chars[0] == '[')
			{
				start += 1;
				end -= 1;
				if (chars[end] != ']')
				{
					throw new ArgumentException("Unclosed bracket in encoded array: " + encoded);
				}
			}
			if (chars[0] == '{')
			{
				start += 1;
				end -= 1;
				if (chars[end] != '}')
				{
					throw new ArgumentException("Unclosed bracket in encoded array: " + encoded);
				}
			}
			// (finite state automaton)
			for (int i = start; i < end; i++)
			{
				if (chars[i] == '\r')
				{
					// Ignore funny windows carriage return
					continue;
				}
				else
				{
					if (quoteCloseChar != 0)
					{
						//(case: in quotes)
						if (chars[i] == quoteCloseChar)
						{
							quoteCloseChar = (char)0;
						}
						else
						{
							current.Append(chars[i]);
						}
					}
					else
					{
						if (chars[i] == '\\')
						{
							//(case: escaped character)
							if (i == chars.Length - 1)
							{
								throw new ArgumentException("Last character of encoded array is escape character: " + encoded);
							}
							current.Append(chars[i + 1]);
							i += 1;
						}
						else
						{
							//(case: normal)
							if (chars[i] == '"')
							{
								quoteCloseChar = '"';
							}
							else
							{
								if (chars[i] == '\'')
								{
									quoteCloseChar = '\'';
								}
								else
								{
									if (chars[i] == ',' || chars[i] == ';' || chars[i] == ' ' || chars[i] == '\t' || chars[i] == '\n')
									{
										//break
										if (current.Length > 0)
										{
											terms.Add(current.ToString().Trim());
										}
										current = new StringBuilder();
									}
									else
									{
										current.Append(chars[i]);
									}
								}
							}
						}
					}
				}
			}
			//--Return
			if (current.Length > 0)
			{
				terms.Add(current.ToString().Trim());
			}
			return Sharpen.Collections.ToArray(terms, EmptyStringArray);
		}

		/// <summary>Decode a map encoded as a string.</summary>
		/// <param name="encoded">The String encoded map</param>
		/// <returns>A String map corresponding to the encoded map</returns>
		public static IDictionary<string, string> DecodeMap(string encoded)
		{
			if (encoded.IsEmpty())
			{
				return new Dictionary<string, string>();
			}
			char[] chars = encoded.Trim().ToCharArray();
			//--Parse the String
			//(state)
			char quoteCloseChar = (char)0;
			IDictionary<string, string> map = new Dictionary<string, string>();
			string key = string.Empty;
			string value = string.Empty;
			bool onKey = true;
			StringBuilder current = new StringBuilder();
			//(start/stop overhead)
			int start = 0;
			int end = chars.Length;
			if (chars[0] == '(')
			{
				start += 1;
				end -= 1;
				if (chars[end] != ')')
				{
					throw new ArgumentException("Unclosed paren in encoded map: " + encoded);
				}
			}
			if (chars[0] == '[')
			{
				start += 1;
				end -= 1;
				if (chars[end] != ']')
				{
					throw new ArgumentException("Unclosed bracket in encoded map: " + encoded);
				}
			}
			if (chars[0] == '{')
			{
				start += 1;
				end -= 1;
				if (chars[end] != '}')
				{
					throw new ArgumentException("Unclosed bracket in encoded map: " + encoded);
				}
			}
			//(finite state automata)
			for (int i = start; i < end; i++)
			{
				if (chars[i] == '\r')
				{
					// Ignore funny windows carriage return
					continue;
				}
				else
				{
					if (quoteCloseChar != 0)
					{
						//(case: in quotes)
						if (chars[i] == quoteCloseChar)
						{
							quoteCloseChar = (char)0;
						}
						else
						{
							current.Append(chars[i]);
						}
					}
					else
					{
						if (chars[i] == '\\')
						{
							//(case: escaped character)
							if (i == chars.Length - 1)
							{
								throw new ArgumentException("Last character of encoded pair is escape character: " + encoded);
							}
							current.Append(chars[i + 1]);
							i += 1;
						}
						else
						{
							//(case: normal)
							if (chars[i] == '"')
							{
								quoteCloseChar = '"';
							}
							else
							{
								if (chars[i] == '\'')
								{
									quoteCloseChar = '\'';
								}
								else
								{
									if (chars[i] == '\n' && current.Length == 0)
									{
										current.Append(string.Empty);
									}
									else
									{
										// do nothing
										if (chars[i] == ',' || chars[i] == ';' || chars[i] == '\t' || chars[i] == '\n')
										{
											// case: end a value
											if (onKey)
											{
												throw new ArgumentException("Encountered key without value");
											}
											if (current.Length > 0)
											{
												value = current.ToString().Trim();
											}
											current = new StringBuilder();
											onKey = true;
											map[key] = value;
										}
										else
										{
											// <- add value
											if ((chars[i] == '-' || chars[i] == '=') && (i < chars.Length - 1 && chars[i + 1] == '>'))
											{
												// case: end a key
												if (!onKey)
												{
													throw new ArgumentException("Encountered a value without a key");
												}
												if (current.Length > 0)
												{
													key = current.ToString().Trim();
												}
												current = new StringBuilder();
												onKey = false;
												i += 1;
											}
											else
											{
												// skip '>' character
												if (chars[i] == ':')
												{
													// case: end a key
													if (!onKey)
													{
														throw new ArgumentException("Encountered a value without a key");
													}
													if (current.Length > 0)
													{
														key = current.ToString().Trim();
													}
													current = new StringBuilder();
													onKey = false;
												}
												else
												{
													current.Append(chars[i]);
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
			//--Return
			if (current.ToString().Trim().Length > 0 && !onKey)
			{
				map[key.Trim()] = current.ToString().Trim();
			}
			return map;
		}

		/// <summary>
		/// Takes an input String, and replaces any bash-style variables (e.g., $VAR_NAME)
		/// with its actual environment variable from the passed environment specification.
		/// </summary>
		/// <param name="raw">The raw String to replace variables in.</param>
		/// <param name="env">
		/// The environment specification; e.g.,
		/// <see cref="Sharpen.Runtime.Getenv()"/>
		/// .
		/// </param>
		/// <returns>The input String, but with all variables replaced.</returns>
		public static string ExpandEnvironmentVariables(string raw, IDictionary<string, string> env)
		{
			string pattern = "\\$\\{?([a-zA-Z_]+[a-zA-Z0-9_]*)\\}?";
			Regex expr = new Regex(pattern);
			string text = raw;
			Match matcher = expr.Match(text);
			while (matcher.Find())
			{
				string envValue = env[matcher.Group(1)];
				if (envValue == null)
				{
					envValue = string.Empty;
				}
				else
				{
					envValue = envValue.Replace("\\", "\\\\");
				}
				Regex subexpr = new Regex(Regex.Escape(matcher.Group(0)));
				text = subexpr.Match(text).ReplaceAll(envValue);
			}
			return text;
		}

		/// <summary>
		/// Takes an input String, and replaces any bash-style variables (e.g., $VAR_NAME)
		/// with its actual environment variable from
		/// <see cref="Sharpen.Runtime.Getenv()"/>
		/// .
		/// </summary>
		/// <param name="raw">The raw String to replace variables in.</param>
		/// <returns>The input String, but with all variables replaced.</returns>
		public static string ExpandEnvironmentVariables(string raw)
		{
			var env = new Dictionary<string,string>();
			foreach(DictionaryEntry var in Environment.GetEnvironmentVariables()) env.Add((string)var.Key, (string)var.Value);
			return ExpandEnvironmentVariables(raw, env);
		}

		/// <summary>Logs the command line arguments to Redwood on the given channels.</summary>
		/// <remarks>
		/// Logs the command line arguments to Redwood on the given channels.
		/// The logger should be a RedwoodChannels of a single channel: the main class.
		/// </remarks>
		/// <param name="logger">The redwood logger to log to.</param>
		/// <param name="args">The command-line arguments to log.</param>
		public static void LogInvocationString(Redwood.RedwoodChannels logger, string[] args)
		{
			StringBuilder sb = new StringBuilder("Invoked on ");
			sb.Append(new DateTime());
			sb.Append(" with arguments:");
			foreach (string arg in args)
			{
				sb.Append(' ').Append(arg);
			}
			logger.Info(sb.ToString());
		}

		private static bool ContainsJsonEscape(string str)
		{
			for (int i = 0; i < str.Length; i++)
			{
				char ch = str[i];
				if (ch < '\u0020' || ch == '\\' || ch == '"')
				{
					return true;
				}
			}
			return false;
		}

		private const string escapeLetters = "btnvfr";

		public static string EscapeJsonString(string str)
		{
			// check if there are any, else return same str without allocation
			if (!ContainsJsonEscape(str))
			{
				return str;
			}
			StringBuilder sb = new StringBuilder(str.Length * 2);
			for (int i = 0; i < str.Length; i++)
			{
				char ch = str[i];
				if (ch == '\\')
				{
					sb.Append("\\\\");
				}
				else
				{
					if (ch == '"')
					{
						sb.Append("\\\"");
					}
					else
					{
						if (ch < '\u0020')
						{
							if (ch >= '\b' && ch <= '\r' && ch != '\u000B')
							{
								sb.Append('\\');
								sb.Append(escapeLetters[ch - '\b']);
							}
							else
							{
								sb.Append("\\u00");
								sb.Append(string.Format("%02X", (int)ch));
							}
						}
						else
						{
							sb.Append(ch);
						}
					}
				}
			}
			return sb.ToString();
		}
	}
}
