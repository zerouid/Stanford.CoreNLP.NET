

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An interface for treebank vendors.</summary>
	/// <author>Roger Levy</author>
	public interface ITreebankFactory
	{
		/// <summary>Returns a treebank instance</summary>
		Edu.Stanford.Nlp.Trees.Treebank Treebank();
	}
}
