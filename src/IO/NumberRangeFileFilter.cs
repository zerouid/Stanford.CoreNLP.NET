


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
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/29</version>
	public class NumberRangeFileFilter : IFileFilter
	{
		private int minimum;

		private int maximum;

		private bool recursively;

		/// <summary>
		/// Sets up a NumberRangeFileFilter by specifying the range of numbers
		/// to accept, and whether to also traverse
		/// folders for recursive search.
		/// </summary>
		/// <param name="min">The minimum number file to accept (checks &ge; this one)</param>
		/// <param name="max">The maximum number file to accept (checks &le; this one)</param>
		/// <param name="recurse">go into folders</param>
		public NumberRangeFileFilter(int min, int max, bool recurse)
		{
			minimum = min;
			maximum = max;
			recursively = recurse;
		}

		/// <summary>Checks whether a file satisfies the number range selection filter.</summary>
		/// <param name="file">The file</param>
		/// <returns>true if the file is within the range filtered for</returns>
		public virtual bool Accept(File file)
		{
			if (file.IsDirectory())
			{
				return recursively;
			}
			else
			{
				string filename = file.GetName();
				int k = filename.Length - 1;
				char c = filename[k];
				while (k >= 0 && (c < '0' || c > '9'))
				{
					k--;
					if (k >= 0)
					{
						c = filename[k];
					}
				}
				if (k < 0)
				{
					return false;
				}
				int j = k;
				c = filename[j];
				while (j >= 0 && (c >= '0' && c <= '9'))
				{
					j--;
					if (j >= 0)
					{
						c = filename[j];
					}
				}
				j++;
				k++;
				string theNumber = Sharpen.Runtime.Substring(filename, j, k);
				int number = System.Convert.ToInt32(theNumber);
				return (number >= minimum) && (number <= maximum);
			}
		}
	}
}
