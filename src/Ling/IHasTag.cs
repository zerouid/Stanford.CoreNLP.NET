using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the <code>HasTag</code> interface
	/// knows about part-of-speech tags.
	/// </summary>
	/// <author>Christopher Manning</author>
	public interface IHasTag
	{
		/// <summary>Return the tag value of the label (or null if none).</summary>
		/// <returns>String the tag value for the label</returns>
		string Tag();

		/// <summary>Set the tag value for the label (if one is stored).</summary>
		/// <param name="tag">The tag value for the label</param>
		void SetTag(string tag);
	}
}
