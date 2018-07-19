using System;
using Edu.Stanford.Nlp.Optimization;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Maximizes the conditional likelihood with a given prior.</summary>
	/// <author>Jenny Finkel</author>
	/// <author>Sarah Spikes (Templatization)</author>
	/// <author>Ramesh Nallapati (Made the function more general to support other AbstractCachingDiffFunctions involving the summation of two objective functions)</author>
	public class SemiSupervisedLogConditionalObjectiveFunction : AbstractCachingDiffFunction
	{
		internal AbstractCachingDiffFunction objFunc;

		internal AbstractCachingDiffFunction biasedObjFunc;

		internal double convexComboFrac = 0.5;

		internal LogPrior prior;

		//BiasedLogConditionalObjectiveFunction biasedObjFunc;  
		public virtual void SetPrior(LogPrior prior)
		{
			this.prior = prior;
		}

		public override int DomainDimension()
		{
			return objFunc.DomainDimension();
		}

		protected internal override void Calculate(double[] x)
		{
			if (derivative == null)
			{
				derivative = new double[DomainDimension()];
			}
			value = convexComboFrac * objFunc.ValueAt(x) + (1.0 - convexComboFrac) * biasedObjFunc.ValueAt(x);
			//value = objFunc.valueAt(x) + biasedObjFunc.valueAt(x);
			double[] d1 = objFunc.DerivativeAt(x);
			double[] d2 = biasedObjFunc.DerivativeAt(x);
			for (int i = 0; i < DomainDimension(); i++)
			{
				derivative[i] = convexComboFrac * d1[i] + (1.0 - convexComboFrac) * d2[i];
			}
			//derivative[i] = d1[i] + d2[i];
			if (prior != null)
			{
				value += prior.Compute(x, derivative);
			}
		}

		public SemiSupervisedLogConditionalObjectiveFunction(AbstractCachingDiffFunction objFunc, AbstractCachingDiffFunction biasedObjFunc, LogPrior prior, double convexComboFrac)
		{
			this.objFunc = objFunc;
			this.biasedObjFunc = biasedObjFunc;
			this.prior = prior;
			this.convexComboFrac = convexComboFrac;
			if (convexComboFrac < 0 || convexComboFrac > 1.0)
			{
				throw new Exception("convexComboFrac has to lie between 0 and 1 (both inclusive).");
			}
		}

		public SemiSupervisedLogConditionalObjectiveFunction(AbstractCachingDiffFunction objFunc, AbstractCachingDiffFunction biasedObjFunc, LogPrior prior)
			: this(objFunc, biasedObjFunc, prior, 0.5)
		{
		}
		//this.objFunc = objFunc;
		//this.biasedObjFunc = biasedObjFunc;
		//this.prior = prior;
	}
}
