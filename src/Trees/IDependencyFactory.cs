using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A factory for dependencies of a certain type.</summary>
	/// <author>Christopher Manning</author>
	public interface IDependencyFactory
	{
		IDependency NewDependency(ILabel regent, ILabel dependent);

		IDependency NewDependency(ILabel regent, ILabel dependent, object name);
	}
}
