using System;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	[System.Serializable]
	public class DirichletProcess<E> : IProbabilityDistribution<E>
	{
		private const long serialVersionUID = -8653536087199951278L;

		private readonly IProbabilityDistribution<E> baseMeasure;

		private readonly double alpha;

		private readonly ClassicCounter<E> sampled;

		public DirichletProcess(IProbabilityDistribution<E> baseMeasure, double alpha)
		{
			this.baseMeasure = baseMeasure;
			this.alpha = alpha;
			this.sampled = new ClassicCounter<E>();
			sampled.IncrementCount(null, alpha);
		}

		public virtual E DrawSample(Random random)
		{
			E drawn = Counters.Sample(sampled);
			if (drawn == null)
			{
				drawn = baseMeasure.DrawSample(random);
			}
			sampled.IncrementCount(drawn);
			return drawn;
		}

		public virtual double NumOccurances(E @object)
		{
			if (@object == null)
			{
				throw new Exception("You cannot ask for the number of occurances of null.");
			}
			return sampled.GetCount(@object);
		}

		public virtual double ProbabilityOf(E @object)
		{
			if (@object == null)
			{
				throw new Exception("You cannot ask for the probability of null.");
			}
			if (sampled.KeySet().Contains(@object))
			{
				return sampled.GetCount(@object) / sampled.TotalCount();
			}
			else
			{
				return 0.0;
			}
		}

		public virtual double LogProbabilityOf(E @object)
		{
			return Math.Log(ProbabilityOf(@object));
		}

		public virtual double ProbabilityOfNewObject()
		{
			return alpha / sampled.TotalCount();
		}
	}
}
