

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A mix-in interface for HeadFinders which support the
	/// makesCopulaHead method, which says how the HeadFinder in question
	/// handles "to be" verbs.
	/// </summary>
	public interface ICopulaHeadFinder
	{
		bool MakesCopulaHead();
	}
}
