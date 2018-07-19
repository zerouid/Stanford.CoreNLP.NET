using Java.IO;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// Implements a file filter that filters based on a passed in
	/// <see cref="Java.Util.Regex.Pattern"/>
	/// .
	/// Preciesly, it will accept exactly those
	/// <see cref="Java.IO.File"/>
	/// s for which
	/// the matches() method of the Pattern returns true on the output of the getName()
	/// method of the File.
	/// </summary>
	/// <author>Jenny Finkel</author>
	public class RegExFileFilter : IFileFilter
	{
		private Pattern pattern = null;

		/// <summary>
		/// Sets up a RegExFileFilter which checks if the file name (not the
		/// entire path) matches the passed in
		/// <see cref="Java.Util.Regex.Pattern"/>
		/// .
		/// </summary>
		public RegExFileFilter(Pattern pattern)
		{
			this.pattern = pattern;
		}

		/// <summary>Checks whether a file satisfies the selection filter.</summary>
		/// <param name="file">The file</param>
		/// <returns>true if the file is acceptable</returns>
		public virtual bool Accept(File file)
		{
			Matcher m = pattern.Matcher(file.GetName());
			return m.Matches();
		}
	}
}
