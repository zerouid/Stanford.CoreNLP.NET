using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// This enumeratin was created to organize the selection of different methods for stochastic
	/// calculations.
	/// </summary>
	/// <remarks>
	/// This enumeratin was created to organize the selection of different methods for stochastic
	/// calculations.  It was also created for use with Stochastic Meta Descent (SMDMinimizer) due
	/// to the need for Hessian Vector Products, and the inefficiency of continuing to calculate these
	/// vector products in other minimization methods like Stochastic Gradient Descent (SGDMinimizer)
	/// </remarks>
	/// <author>Alex Kleeman (akleeman@stanford.edu)</author>
	[System.Serializable]
	public sealed class StochasticCalculateMethods
	{
		public static readonly Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods NoneSpecified = new Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods(false);

		public static readonly Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods GradientOnly = new Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods(false);

		public static readonly Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods AlgorithmicDifferentiation = new Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods(true);

		public static readonly Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods IncorporatedFiniteDifference = new Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods(true);

		public static readonly Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods ExternalFiniteDifference = new Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods(false);

		private bool objFuncCalculatesHdotV;

		internal StochasticCalculateMethods(bool ObjectiveFunctionCalculatesHdotV)
		{
			/*  Used for procedures like Stochastic Gradient Descent */
			/*  This is used with the Objective Function can handle calculations using Algorithmic Differentiation*/
			/*  It is often more efficient to calculate the Finite difference within one single for loop,
			if the objective function can handle this, this method should be used instead of
			ExternalFiniteDifference
			*/
			/*  ExternalFiniteDifference uses two calls to the objective function to come up with an approximation of
			the H.v
			*/
			/*
			*This boolean is true if the Objective Function is required to calculate the hessian vector product
			*   In the case of ExternalFiniteDifference this is false since two calls are made to the objective
			*   function.
			*/
			this.objFuncCalculatesHdotV = ObjectiveFunctionCalculatesHdotV;
		}

		public bool CalculatesHessianVectorProduct()
		{
			return Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods.objFuncCalculatesHdotV;
		}

		public static Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods ParseMethod(string method)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(method, "AlgorithmicDifferentiation"))
			{
				return Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods.AlgorithmicDifferentiation;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(method, "IncorporatedFiniteDifference"))
				{
					return Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods.IncorporatedFiniteDifference;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(method, "ExternalFinitedifference"))
					{
						return Edu.Stanford.Nlp.Optimization.StochasticCalculateMethods.ExternalFiniteDifference;
					}
					else
					{
						return null;
					}
				}
			}
		}
	}
}
