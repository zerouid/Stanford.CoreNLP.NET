using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// Interface allowing for different routines to compare for equality over SemanticGraphEdges (typed
	/// lambdas in Java?)
	/// </summary>
	/// <author>Eric Yeh</author>
	public interface IISemanticGraphEdgeEql
	{
		bool Equals(SemanticGraphEdge edge1, SemanticGraphEdge edge2, SemanticGraph sg1, SemanticGraph sg2);
	}
}
