using Edu.Stanford.Nlp.Ling.Tokensregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>This interface represents an expression that can be evaluated to obtain a value.</summary>
	/// <author>Angel Chang</author>
	public interface IExpression
	{
		/// <summary>Returns tags associated with this expression.</summary>
		/// <returns>Tags associated with this expression</returns>
		Tags GetTags();

		/// <summary>Set the tags associated with this expression.</summary>
		/// <param name="tags">Tags to associate with this expression</param>
		void SetTags(Tags tags);

		/// <summary>Returns a string indicating the type of this expression.</summary>
		/// <returns>type of this expressions</returns>
		string GetType();

		/// <summary>Simplifies the expression using the specified environment.</summary>
		/// <param name="env">Environment to simplify with respect to</param>
		/// <returns>Simplified expressions</returns>
		IExpression Simplify(Env env);

		/// <summary>
		/// Evaluates the expression using the specified environment and
		/// arguments.
		/// </summary>
		/// <remarks>
		/// Evaluates the expression using the specified environment and
		/// arguments.  Arguments are additional context not provided
		/// by the environment.
		/// </remarks>
		/// <param name="env">Environment</param>
		/// <param name="args">Arguments</param>
		/// <returns>Evaluated value</returns>
		IValue Evaluate(Env env, params object[] args);

		/// <summary>
		/// Returns whether the expression has already been evaluated to
		/// a Value
		/// </summary>
		/// <returns>true if the expression is already evaluated</returns>
		bool HasValue();
	}
}
