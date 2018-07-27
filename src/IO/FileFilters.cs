using System.IO;
using System.Text.RegularExpressions;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Provides some static methods for combination file filters.</summary>
	/// <author>Christopher Manning</author>
	public class FileFilters
	{
		private FileFilters()
		{
		}

		public static IFileFilter ConjunctionFileFilter(IFileFilter a, IFileFilter b)
		{
			return new FileFilters.ConjunctionFileFilterImpl(a, b);
		}

		public static IFileFilter NegationFileFilter(IFileFilter a)
		{
			return new FileFilters.NegationFileFilterImpl(a);
		}

		public static IFileFilter FindRegexFileFilter(string regex)
		{
			return new FileFilters.FindRegexFileFilterImpl(regex);
		}

		/// <summary>Implements a conjunction file filter.</summary>
		private class ConjunctionFileFilterImpl : IFileFilter
		{
			private readonly IFileFilter f1;

			private readonly IFileFilter f2;

			/// <summary>Sets up file filter.</summary>
			/// <param name="a">One file filter</param>
			/// <param name="b">The other file filter</param>
			public ConjunctionFileFilterImpl(IFileFilter a, IFileFilter b)
			{
				f1 = a;
				f2 = b;
			}

			/// <summary>Checks whether a file satisfies the selection filter.</summary>
			/// <param name="file">The file</param>
			/// <returns>true if the file is acceptable</returns>
			public virtual bool Accept(FileInfo file)
			{
				return f1.Accept(file) && f2.Accept(file);
			}
		}

		/// <summary>Implements a negation file filter.</summary>
		private class NegationFileFilterImpl : IFileFilter
		{
			private readonly IFileFilter f1;

			/// <summary>Sets up file filter.</summary>
			/// <param name="a">A file filter</param>
			public NegationFileFilterImpl(IFileFilter a)
			{
				f1 = a;
			}

			/// <summary>Checks whether a file satisfies the selection filter.</summary>
			/// <param name="file">The file</param>
			/// <returns>true if the file is acceptable</returns>
			public virtual bool Accept(FileInfo file)
			{
				return !f1.Accept(file);
			}
		}

		/// <summary>Implements a conjunction file filter.</summary>
		private class FindRegexFileFilterImpl : IFileFilter
		{
			private readonly Regex p;

			/// <summary>Sets up file filter.</summary>
			/// <param name="regex">The pattern to match (as find()</param>
			public FindRegexFileFilterImpl(string regex)
			{
				p = new Regex(regex, RegexOptions.Compiled);
			}

			/// <summary>Checks whether a file satisfies the selection filter.</summary>
			/// <param name="file">The file</param>
			/// <returns>true if the file is acceptable</returns>
			public virtual bool Accept(FileInfo file)
			{
				return p.IsMatch(file.Name);
			}
		}
	}
}
