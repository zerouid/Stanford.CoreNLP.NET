using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	public interface IDependencyPrinter
	{
		string DependenciesToString(GrammaticalStructure gs, ICollection<TypedDependency> deps, Tree tree);
	}
}
