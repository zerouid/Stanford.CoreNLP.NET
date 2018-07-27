using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// Implements a file filter that examines a number in a filename to
	/// determine acceptance.
	/// </summary>
	/// <remarks>
	/// Implements a file filter that examines a number in a filename to
	/// determine acceptance.  This is useful for wanting to process ranges
	/// of numbered files in collections where each file has some name, part
	/// of which is alphabetic and constant, and part of which is numeric.
	/// The test is evaluated based on the rightmost natural number found in
	/// the filename string.  (It only looks in the final filename, not in other
	/// components of the path.)  Number ranges are inclusive.
	/// <p>
	/// This filter can select multiple discontinuous ranges based on a format
	/// similar to page selection ranges in various formatting software, such as
	/// "34,52-65,67,93-95".  The constructor takes a String of this sort and
	/// deconstructs it into a list of ranges.  The accepted syntax is:
	/// <p>
	/// ranges = range <br />
	/// ranges = range "," ranges <br />
	/// range = integer <br />
	/// range = integer "-" integer
	/// <p>
	/// Whitespace will be ignored.  If the filter constructor is passed anything
	/// that is not a list of numeric ranges of this sort, including being passed
	/// an empty String, then an
	/// <c>IllegalArgumentException</c>
	/// will be
	/// thrown.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2003/03/31</version>
	public class NumberRangesFileFilter : IFileFilter
	{
		private IList<Pair<int, int>> ranges = new List<Pair<int, int>>();

		private bool recursively;

		/// <summary>
		/// Sets up a NumberRangesFileFilter by specifying the ranges of numbers
		/// to accept, and whether to also traverse
		/// folders for recursive search.
		/// </summary>
		/// <param name="ranges">The ranges of numbers to accept (see class documentation)</param>
		/// <param name="recurse">Whether to go into subfolders</param>
		/// <exception cref="System.ArgumentException">
		/// If the String ranges does not
		/// contain a suitable ranges format
		/// </exception>
		public NumberRangesFileFilter(string ranges, bool recurse)
		{
			recursively = recurse;
			try
			{
				string[] ra = ranges.Split(",");
				foreach (string range in ra)
				{
					string[] one = range.Split("-");
					if (one.Length > 2)
					{
						throw new ArgumentException("Constructor argument not valid list of number ranges (too many hyphens): ");
					}
					else
					{
						int low = System.Convert.ToInt32(one[0].Trim());
						int high;
						if (one.Length == 2)
						{
							high = System.Convert.ToInt32(one[1].Trim());
						}
						else
						{
							high = low;
						}
						Pair<int, int> p = new Pair<int, int>(int.Parse(low), int.Parse(high));
						this.ranges.Add(p);
					}
				}
			}
			catch (Exception e)
			{
				throw new ArgumentException("Constructor argument not valid list of number ranges: " + ranges, e);
			}
		}

		/// <summary>Checks whether a file satisfies the number range selection filter.</summary>
		/// <remarks>
		/// Checks whether a file satisfies the number range selection filter.
		/// The test is evaluated based on the rightmost natural number found in
		/// the filename string (proper, not including directories in a path).
		/// </remarks>
		/// <param name="file">The file</param>
		/// <returns>true If the file is within the ranges filtered for</returns>
		public virtual bool Accept(File file)
		{
			if (file.IsDirectory())
			{
				return recursively;
			}
			else
			{
				string filename = file.GetName();
				return Accept(filename);
			}
		}

		/// <summary>Checks whether a String satisfies the number range selection filter.</summary>
		/// <remarks>
		/// Checks whether a String satisfies the number range selection filter.
		/// The test is evaluated based on the rightmost natural number found in
		/// the String.   Note that this is just evaluated on the String as given.
		/// It is not trying to interpret it as a filename and to decide whether
		/// the file exists, is a directory or anything like that.
		/// </remarks>
		/// <param name="str">The String to check for a number in</param>
		/// <returns>true If the String is within the ranges filtered for</returns>
		public virtual bool Accept(string str)
		{
			int k = str.Length - 1;
			char c = str[k];
			while (k >= 0 && !char.IsDigit(c))
			{
				k--;
				if (k >= 0)
				{
					c = str[k];
				}
			}
			if (k < 0)
			{
				return false;
			}
			int j = k;
			c = str[j];
			while (j >= 0 && char.IsDigit(c))
			{
				j--;
				if (j >= 0)
				{
					c = str[j];
				}
			}
			j++;
			k++;
			string theNumber = Sharpen.Runtime.Substring(str, j, k);
			int number = System.Convert.ToInt32(theNumber);
			foreach (Pair<int, int> p in ranges)
			{
				int low = p.First();
				int high = p.Second();
				if (number >= low && number <= high)
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			StringBuilder sb;
			if (recursively)
			{
				sb = new StringBuilder("recursively ");
			}
			else
			{
				sb = new StringBuilder();
			}
			for (IEnumerator<Pair<int, int>> it = ranges.GetEnumerator(); it.MoveNext(); )
			{
				Pair<int, int> p = it.Current;
				int low = p.First();
				int high = p.Second();
				if (low == high)
				{
					sb.Append(low);
				}
				else
				{
					sb.Append(low);
					sb.Append('-');
					sb.Append(high);
				}
				if (it.MoveNext())
				{
					sb.Append(',');
				}
			}
			return sb.ToString();
		}
	}
}
