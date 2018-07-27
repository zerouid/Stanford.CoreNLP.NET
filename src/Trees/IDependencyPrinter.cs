using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Trees
{
	public interface IDependencyPrinter
	{
		string DependenciesToString(GrammaticalStructure gs, ICollection<TypedDependency> deps, Tree tree);
	}
}
