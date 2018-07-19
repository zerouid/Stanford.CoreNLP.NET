using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// An interface for a factory that builds a GrammaticalStructure from
	/// a list of TypedDependencies and a TreeGraphNode.
	/// </summary>
	/// <remarks>
	/// An interface for a factory that builds a GrammaticalStructure from
	/// a list of TypedDependencies and a TreeGraphNode.  This is useful
	/// when building a GrammaticalStructure from a conll data file, for example.
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IGrammaticalStructureFromDependenciesFactory
	{
		GrammaticalStructure Build(IList<TypedDependency> projectiveDependencies, TreeGraphNode root);
	}
}
