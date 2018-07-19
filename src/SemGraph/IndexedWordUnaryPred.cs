using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// Represents an extensible class that can be extended and passed around
	/// to perform
	/// </summary>
	/// <author>Eric Yeh</author>
	public class IndexedWordUnaryPred
	{
		public virtual bool Test(IndexedWord node)
		{
			return true;
		}

		public virtual bool Test(IndexedWord node, SemanticGraph sg)
		{
			return Test(node);
		}
	}
}
