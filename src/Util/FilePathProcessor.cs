using System;
using Edu.Stanford.Nlp.IO;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// The <code>FilePathProcessor</code> traverses a directory structure and
	/// applies the <code>processFile</code> method to files meeting some
	/// criterion.
	/// </summary>
	/// <remarks>
	/// The <code>FilePathProcessor</code> traverses a directory structure and
	/// applies the <code>processFile</code> method to files meeting some
	/// criterion.  It is implemented as static methods, not as an extension of
	/// <code>File</code>.
	/// <p>
	/// <i>Note:</i> This is used in our old code in ling/trees, but newer code
	/// should probably use io.FileSequentialCollection
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class FilePathProcessor
	{
		private FilePathProcessor()
		{
		}

		/// <summary>
		/// Apply a method to the files under a given directory and
		/// perhaps its subdirectories.
		/// </summary>
		/// <param name="pathStr">file or directory to load from as a String</param>
		/// <param name="suffix">suffix (normally "File extension") of files to load</param>
		/// <param name="recursively">true means descend into subdirectories as well</param>
		/// <param name="processor">
		/// The <code>FileProcessor</code> to apply to each
		/// <code>File</code>
		/// </param>
		public static void ProcessPath(string pathStr, string suffix, bool recursively, IFileProcessor processor)
		{
			ProcessPath(new File(pathStr), new ExtensionFileFilter(suffix, recursively), processor);
		}

		/// <summary>
		/// Apply a method to the files under a given directory and
		/// perhaps its subdirectories.
		/// </summary>
		/// <param name="path">file or directory to load from</param>
		/// <param name="suffix">suffix (normally "File extension") of files to load</param>
		/// <param name="recursively">true means descend into subdirectories as well</param>
		/// <param name="processor">
		/// The <code>FileProcessor</code> to apply to each
		/// <code>File</code>
		/// </param>
		public static void ProcessPath(File path, string suffix, bool recursively, IFileProcessor processor)
		{
			ProcessPath(path, new ExtensionFileFilter(suffix, recursively), processor);
		}

		/// <summary>
		/// Apply a function to the files under a given directory and
		/// perhaps its subdirectories.
		/// </summary>
		/// <remarks>
		/// Apply a function to the files under a given directory and
		/// perhaps its subdirectories.  If the path is a directory then only
		/// files within the directory (perhaps recursively) that satisfy the
		/// filter are processed.  If the <code>path</code>is a file, then
		/// that file is processed regardless of whether it satisfies the
		/// filter.  (This semantics was adopted, since otherwise there was no
		/// easy way to go through all the files in a directory without
		/// descending recursively via the specification of a
		/// <code>FileFilter</code>.)
		/// </remarks>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filter">
		/// a FileFilter of files to load.  The filter may be null,
		/// and then all files are processed.
		/// </param>
		/// <param name="processor">
		/// The <code>FileProcessor</code> to apply to each
		/// <code>File</code>
		/// </param>
		public static void ProcessPath(File path, IFileFilter filter, IFileProcessor processor)
		{
			if (path.IsDirectory())
			{
				// if path is a directory, look into it
				File[] directoryListing = path.ListFiles(filter);
				if (directoryListing == null)
				{
					throw new ArgumentException("Directory access problem for: " + path);
				}
				foreach (File file in directoryListing)
				{
					ProcessPath(file, filter, processor);
				}
			}
			else
			{
				// it's already passed the filter or was uniquely specified
				// if (filter.accept(path))
				processor.ProcessFile(path);
			}
		}
	}
}
