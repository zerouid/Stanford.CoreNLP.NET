

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>This interface represents an expression that can be assigned to.</summary>
	/// <author>Angel Chang</author>
	public interface IAssignableExpression : IExpression
	{
		IExpression Assign(IExpression expr);
	}
}
