using System;



namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>simple dirichlet distribution.</summary>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class Dirichlet<E> : IConjugatePrior<Multinomial<E>, E>
	{
		private const long serialVersionUID = 1L;

		private ICounter<E> parameters;

		public Dirichlet(ICounter<E> parameters)
		{
			CheckParameters(parameters);
			this.parameters = new ClassicCounter<E>(parameters);
		}

		private void CheckParameters(ICounter<E> parameters)
		{
			foreach (E o in parameters.KeySet())
			{
				if (parameters.GetCount(o) < 0.0)
				{
					throw new Exception("Parameters must be non-negative!");
				}
			}
			if (parameters.TotalCount() <= 0.0)
			{
				throw new Exception("Parameters must have positive mass!");
			}
		}

		public virtual Multinomial<E> DrawSample(Random random)
		{
			return DrawSample(random, parameters);
		}

		public static Multinomial<F> DrawSample<F>(Random random, ICounter<F> parameters)
		{
			ICounter<F> multParameters = new ClassicCounter<F>();
			double sum = 0.0;
			foreach (F o in parameters.KeySet())
			{
				double parameter = Gamma.DrawSample(random, parameters.GetCount(o));
				sum += parameter;
				multParameters.SetCount(o, parameter);
			}
			foreach (F o_1 in multParameters.KeySet())
			{
				multParameters.SetCount(o_1, multParameters.GetCount(o_1) / sum);
			}
			return new Multinomial<F>(multParameters);
		}

		// Faster sampling from a Dirichlet.
		public static double[] DrawSample(Random random, double[] parameters)
		{
			double sum = 0.0;
			double[] result = new double[parameters.Length];
			for (int i = 0; i < parameters.Length; ++i)
			{
				double parameter = Gamma.DrawSample(random, parameters[i]);
				sum += parameter;
				result[i] = parameter;
			}
			for (int i_1 = 0; i_1 < parameters.Length; ++i_1)
			{
				result[i_1] /= sum;
			}
			return result;
		}

		public static double SampleBeta(double a, double b, Random random)
		{
			ICounter<bool> c = new ClassicCounter<bool>();
			c.SetCount(true, a);
			c.SetCount(false, b);
			Multinomial<bool> beta = (new Edu.Stanford.Nlp.Stats.Dirichlet<bool>(c)).DrawSample(random);
			return beta.ProbabilityOf(true);
		}

		public virtual double GetPredictiveProbability(E @object)
		{
			return parameters.GetCount(@object) / parameters.TotalCount();
		}

		public virtual double GetPredictiveLogProbability(E @object)
		{
			return Math.Log(GetPredictiveProbability(@object));
		}

		public virtual Edu.Stanford.Nlp.Stats.Dirichlet<E> GetPosteriorDistribution(ICounter<E> counts)
		{
			ICounter<E> newParameters = new ClassicCounter<E>(parameters);
			Counters.AddInPlace(newParameters, counts);
			return new Edu.Stanford.Nlp.Stats.Dirichlet<E>(newParameters);
		}

		public virtual double GetPosteriorPredictiveProbability(ICounter<E> counts, E @object)
		{
			double numerator = parameters.GetCount(@object) + counts.GetCount(@object);
			double denominator = parameters.TotalCount() + counts.TotalCount();
			return numerator / denominator;
		}

		public virtual double GetPosteriorPredictiveLogProbability(ICounter<E> counts, E @object)
		{
			return Math.Log(GetPosteriorPredictiveProbability(counts, @object));
		}

		public virtual double ProbabilityOf(Multinomial<E> @object)
		{
			// TODO Auto-generated method stub
			return 0;
		}

		// Quick hack method for metropolis
		public static double UnnormalizedLogProbabilityOf(double[] mult, double[] @params)
		{
			double sum = 0.0;
			for (int i = 0; i < @params.Length; ++i)
			{
				if (mult[i] > 0)
				{
					sum += (@params[i] - 1) * Math.Log(mult[i]);
				}
			}
			return sum;
		}

		public virtual double LogProbabilityOf(Multinomial<E> @object)
		{
			// TODO Auto-generated method stub
			return 0;
		}

		public override string ToString()
		{
			return Counters.ToBiggestValuesFirstString(parameters, 50);
		}
	}
}
