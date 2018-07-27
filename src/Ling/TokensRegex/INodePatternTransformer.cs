

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>
	/// Interface to transform a node pattern from a
	/// <c>NodePattern&lt;T1&gt;</c>
	/// into a
	/// <c>NodePattern &lt;T2&gt;</c>
	/// .
	/// </summary>
	/// <author>Angel Chang</author>
	public interface INodePatternTransformer<T1, T2>
	{
		NodePattern<T2> Transform(NodePattern<T1> n1);

		MultiNodePattern<T2> Transform(MultiNodePattern<T1> n1);
	}
}
