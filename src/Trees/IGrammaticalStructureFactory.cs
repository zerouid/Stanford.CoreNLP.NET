

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A general factory for
	/// <see cref="GrammaticalStructure"/>
	/// objects.
	/// </summary>
	/// <author>Galen Andrew</author>
	/// <author>John Bauer</author>
	public interface IGrammaticalStructureFactory
	{
		/// <summary>
		/// Vend a new
		/// <see cref="GrammaticalStructure"/>
		/// based on the given
		/// <see cref="Tree"/>
		/// .
		/// </summary>
		/// <param name="t">the tree to analyze</param>
		/// <returns>a GrammaticalStructure based on the tree</returns>
		GrammaticalStructure NewGrammaticalStructure(Tree t);
	}
}
