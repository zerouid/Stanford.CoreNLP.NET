using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A generified interface for making some kind of dependency object
	/// between a head and dependent.
	/// </summary>
	/// <author>Roger Levy</author>
	public interface IDependencyTyper<T>
	{
		/// <summary>
		/// Make a dependency given the Tree that is the head and the dependent,
		/// both of which are contained within root.
		/// </summary>
		T MakeDependency(Tree head, Tree dep, Tree root);
	}
}
