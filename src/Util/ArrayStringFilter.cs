using System;
using System.Collections.Generic;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Filters Strings based on whether they exactly match any string in
	/// the array it is initially constructed with.
	/// </summary>
	/// <remarks>
	/// Filters Strings based on whether they exactly match any string in
	/// the array it is initially constructed with.  Saves some time over
	/// using regexes if the array of strings is small enough.  No specific
	/// experiments exist for how long the array can be before performance
	/// is worse than a regex, but the English dependencies code was helped
	/// by replacing disjunction regexes of 6 words or fewer with this.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class ArrayStringFilter : IPredicate<string>
	{
		private readonly string[] words;

		private readonly int length;

		private readonly ArrayStringFilter.Mode mode;

		public enum Mode
		{
			Exact,
			Prefix,
			CaseInsensitive
		}

		public ArrayStringFilter(ArrayStringFilter.Mode mode, params string[] words)
		{
			if (mode == null)
			{
				throw new ArgumentNullException("Cannot handle null mode");
			}
			this.mode = mode;
			this.words = new string[words.Length];
			System.Array.Copy(words, 0, this.words, 0, words.Length);
			this.length = words.Length;
		}

		public virtual bool Test(string input)
		{
			switch (mode)
			{
				case ArrayStringFilter.Mode.Exact:
				{
					for (int i = 0; i < length; ++i)
					{
						if (words[i].Equals(input))
						{
							return true;
						}
					}
					return false;
				}

				case ArrayStringFilter.Mode.Prefix:
				{
					if (input == null)
					{
						return false;
					}
					for (int i_1 = 0; i_1 < length; ++i_1)
					{
						if (input.StartsWith(words[i_1]))
						{
							return true;
						}
					}
					return false;
				}

				case ArrayStringFilter.Mode.CaseInsensitive:
				{
					for (int i_2 = 0; i_2 < length; ++i_2)
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(words[i_2], input))
						{
							return true;
						}
					}
					return false;
				}

				default:
				{
					throw new ArgumentException("Unknown mode " + mode);
				}
			}
		}

		public override string ToString()
		{
			return mode.ToString() + ':' + StringUtils.Join(words, ",");
		}

		public override int GetHashCode()
		{
			int result = 1;
			foreach (string word in words)
			{
				result += word.GetHashCode();
			}
			return result;
		}

		public override bool Equals(object other)
		{
			if (other == this)
			{
				return true;
			}
			if (!(other is Edu.Stanford.Nlp.Util.ArrayStringFilter))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.ArrayStringFilter filter = (Edu.Stanford.Nlp.Util.ArrayStringFilter)other;
			if (filter.mode != this.mode || filter.length != this.length)
			{
				return false;
			}
			ICollection<string> myWords = new HashSet<string>(Arrays.AsList(this.words));
			ICollection<string> otherWords = new HashSet<string>(Arrays.AsList(filter.words));
			return myWords.Equals(otherWords);
		}

		private const long serialVersionUID = 1;
	}
}
