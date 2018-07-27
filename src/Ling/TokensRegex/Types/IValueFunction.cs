using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>
	/// A function that takes as input an environment (Env) and a list of values
	/// (
	/// <c>List&lt;Value&gt;</c>
	/// ) and returns a Value.
	/// </summary>
	/// <author>Angel Chang</author>
	public interface IValueFunction
	{
		/// <summary>Checks if the arguments are valid.</summary>
		/// <param name="in">The input arguments</param>
		/// <returns>true if the arguments are valid (false otherwise)</returns>
		bool CheckArgs(IList<IValue> @in);

		/// <summary>
		/// Applies the function to the list values using the environment as context
		/// and returns the evaluated value.
		/// </summary>
		/// <param name="env">The environment to use</param>
		/// <param name="in">The input arguments</param>
		/// <returns>Value indicating the value of the function</returns>
		IValue Apply(Env env, IList<IValue> @in);

		/// <summary>Returns a string describing what this function does.</summary>
		/// <returns>String describing the function</returns>
		string GetDescription();
	}
}
