using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the <code>HasCategory</code> interface
	/// knows about categories.
	/// </summary>
	/// <author>Christopher Manning</author>
	public interface IHasCategory
	{
		/// <summary>Return the category value of the label (or null if none).</summary>
		/// <returns>String the category value for the label</returns>
		string Category();

		/// <summary>Set the category value for the label (if one is stored).</summary>
		/// <param name="category">The category value for the label</param>
		void SetCategory(string category);
	}
}
