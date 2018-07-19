using Edu.Stanford.Nlp.Loglinear.Model;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/26/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Handles optimizing an AbstractDifferentiableFunction through AdaGrad guarded by backtracking.
	/// </author>
	public class BacktrackingAdaGradOptimizer : AbstractBatchOptimizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(BacktrackingAdaGradOptimizer));

		internal const double alpha = 0.1;

		// this magic number was arrived at with relation to the CoNLL benchmark, and tinkering
		public override bool UpdateWeights(ConcatVector weights, ConcatVector gradient, double logLikelihood, AbstractBatchOptimizer.OptimizationState optimizationState, bool quiet)
		{
			BacktrackingAdaGradOptimizer.AdaGradOptimizationState s = (BacktrackingAdaGradOptimizer.AdaGradOptimizationState)optimizationState;
			double logLikelihoodChange = logLikelihood - s.lastLogLikelihood;
			if (logLikelihoodChange == 0)
			{
				if (!quiet)
				{
					log.Info("\tlogLikelihood improvement = 0: quitting");
				}
				return true;
			}
			else
			{
				// Check if we should backtrack
				if (logLikelihoodChange < 0)
				{
					// If we should, move the weights back by half, and cut the lastDerivative by half
					s.lastDerivative.MapInPlace(null);
					weights.AddVectorInPlace(s.lastDerivative, -1.0);
					if (!quiet)
					{
						log.Info("\tBACKTRACK...");
					}
					// if the lastDerivative norm falls below a threshold, it means we've converged
					if (s.lastDerivative.DotProduct(s.lastDerivative) < 1.0e-10)
					{
						if (!quiet)
						{
							log.Info("\tBacktracking derivative norm " + s.lastDerivative.DotProduct(s.lastDerivative) + " < 1.0e-9: quitting");
						}
						return true;
					}
				}
				else
				{
					// Apply AdaGrad
					ConcatVector squared = gradient.DeepClone();
					squared.MapInPlace(null);
					s.adagradAccumulator.AddVectorInPlace(squared, 1.0);
					ConcatVector sqrt = s.adagradAccumulator.DeepClone();
					sqrt.MapInPlace(null);
					gradient.ElementwiseProductInPlace(sqrt);
					weights.AddVectorInPlace(gradient, 1.0);
					// Setup for backtracking, in case necessary
					s.lastDerivative = gradient;
					s.lastLogLikelihood = logLikelihood;
					if (!quiet)
					{
						log.Info("\tLL: " + logLikelihood);
					}
				}
			}
			return false;
		}

		protected internal class AdaGradOptimizationState : AbstractBatchOptimizer.OptimizationState
		{
			internal ConcatVector lastDerivative = new ConcatVector(0);

			internal ConcatVector adagradAccumulator = new ConcatVector(0);

			internal double lastLogLikelihood = double.NegativeInfinity;

			internal AdaGradOptimizationState(BacktrackingAdaGradOptimizer _enclosing)
				: base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly BacktrackingAdaGradOptimizer _enclosing;
		}

		protected internal override AbstractBatchOptimizer.OptimizationState GetFreshOptimizationState(ConcatVector initialWeights)
		{
			return new BacktrackingAdaGradOptimizer.AdaGradOptimizationState(this);
		}
	}
}
