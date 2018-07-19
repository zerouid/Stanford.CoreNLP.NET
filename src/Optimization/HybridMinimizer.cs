using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Hybrid Minimizer is set up as a combination of two minimizers.</summary>
	/// <remarks>
	/// Hybrid Minimizer is set up as a combination of two minimizers.  The first minimizer will ideally
	/// quickly converge regardless of proximity to the true minimum, while the second minimizer would
	/// generally be a quadratic method, that is only fully quadratic near the solution.
	/// If you read this, send me an e-mail saying, "Alex!  You should finish adding the description to
	/// the Hybrid Minimizer!"
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class HybridMinimizer : IMinimizer<IDiffFunction>, IHasEvaluators
	{
		private readonly IMinimizer<IDiffFunction> firstMinimizer;

		private readonly IMinimizer<IDiffFunction> secondMinimizer;

		private readonly int iterationCutoff;

		public HybridMinimizer(IMinimizer<IDiffFunction> minimizerOne, IMinimizer<IDiffFunction> minimizerTwo, int iterationCutoff)
		{
			// = new SMDMinimizer<DiffFunction>();
			// = new QNMinimizer(15);
			// = 1000;
			this.firstMinimizer = minimizerOne;
			this.secondMinimizer = minimizerTwo;
			this.iterationCutoff = iterationCutoff;
		}

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			if (firstMinimizer is IHasEvaluators)
			{
				((IHasEvaluators)firstMinimizer).SetEvaluators(iters, evaluators);
			}
			if (secondMinimizer is IHasEvaluators)
			{
				((IHasEvaluators)secondMinimizer).SetEvaluators(iters, evaluators);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial, int maxIterations)
		{
			double[] x = firstMinimizer.Minimize(function, functionTolerance, initial, iterationCutoff);
			return secondMinimizer.Minimize(function, functionTolerance, x, maxIterations);
		}
	}
}
