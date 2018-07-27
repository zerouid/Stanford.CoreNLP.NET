using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <summary>A generic interface loading, processing, and writing a data set.</summary>
	/// <remarks>
	/// A generic interface loading, processing, and writing a data set. Classes
	/// that implement this interface may be specified in the configuration file
	/// using the <code>TYPE</code> parameter.
	/// <see cref="TreebankPreprocessor"/>
	/// will
	/// then call
	/// <see cref="SetOptions(Java.Util.Properties)"/>
	/// ,
	/// <see cref="Build()"/>
	/// and
	/// <see cref="GetFilenames()"/>
	/// in that order.
	/// </remarks>
	/// <author>Spence Green</author>
	public interface IDataset
	{
		public enum Encoding
		{
			Buckwalter,
			Utf8
		}

		/// <summary>Sets options for a dataset.</summary>
		/// <param name="opts">
		/// A map from parameter types defined in
		/// <see cref="ConfigParser"/>
		/// to
		/// values
		/// </param>
		/// <returns>true if opts contains all required options. false, otherwise.</returns>
		bool SetOptions(Properties opts);

		/// <summary>Generic method for loading, processing, and writing a dataset.</summary>
		void Build();

		/// <summary>
		/// Returns the filenames written by
		/// <see cref="Build()"/>
		/// .
		/// </summary>
		/// <returns>A collection of filenames</returns>
		IList<string> GetFilenames();
	}

	public static class DatasetConstants
	{
	}
}
