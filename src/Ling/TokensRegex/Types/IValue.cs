

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>An expression that has been evaluated to a Java object of type T.</summary>
	/// <author>Angel Chang</author>
	public interface IValue<T> : IExpression
	{
		/// <summary>The Java object representing the value of the expression.</summary>
		/// <returns>a Java object</returns>
		T Get();
	}
}
