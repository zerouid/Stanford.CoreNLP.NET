



namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Implements a file filter that uses file extensions to filter files.</summary>
	/// <author>cmanning 2000/01/24</author>
	public class ExtensionFileFilter : FileFilter, IFileFilter
	{
		private string extension;

		private bool recursively;

		/// <summary>
		/// Sets up Extension file filter by specifying an extension
		/// to accept (currently only 1) and whether to also display
		/// folders for recursive search.
		/// </summary>
		/// <remarks>
		/// Sets up Extension file filter by specifying an extension
		/// to accept (currently only 1) and whether to also display
		/// folders for recursive search.
		/// The passed extension may be null, in which case the filter
		/// will pass all files (passing an empty String does not have the same
		/// effect -- this would look for file names ending in a period).
		/// </remarks>
		/// <param name="ext">File extension (need not include period) or passing null means accepting all files</param>
		/// <param name="recurse">go into folders</param>
		public ExtensionFileFilter(string ext, bool recurse)
		{
			// = null
			if (ext != null)
			{
				if (ext.StartsWith("."))
				{
					extension = ext;
				}
				else
				{
					extension = '.' + ext;
				}
			}
			recursively = recurse;
		}

		/// <summary>Sets up an extension file filter that will recurse into sub directories.</summary>
		/// <param name="ext">The extension to accept (with or without a leading period).</param>
		public ExtensionFileFilter(string ext)
			: this(ext, true)
		{
		}

		/// <summary>Checks whether a file satisfies the selection filter.</summary>
		/// <param name="file">The file</param>
		/// <returns>true if the file is acceptable</returns>
		public override bool Accept(File file)
		{
			if (file.IsDirectory())
			{
				return recursively;
			}
			else
			{
				if (extension == null)
				{
					return true;
				}
				else
				{
					return file.GetName().EndsWith(extension);
				}
			}
		}

		/// <summary>Returns a description of what extension is being used (for file choosers).</summary>
		/// <remarks>
		/// Returns a description of what extension is being used (for file choosers).
		/// For example, if the suffix is "xml", the description will be
		/// "XML Files (*.xml)".
		/// </remarks>
		/// <returns>description of this file filter</returns>
		public override string GetDescription()
		{
			string ucExt = Sharpen.Runtime.Substring(extension, 1).ToUpper();
			return ucExt + " Files (*" + extension + ')';
		}
	}
}
