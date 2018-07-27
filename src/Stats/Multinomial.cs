using System;



namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>a multinomial distribution.</summary>
	/// <remarks>
	/// a multinomial distribution.  pretty straightforward.  specify the parameters with
	/// a counter.  It is assumed that the Counter's keySet() contains all of the parameters (i.e., there are not other
	/// possible values which are set to 0).  It makes a copy of the Counter, so tha parameters cannot be changes,
	/// and it normalizes the values if they are not already normalized.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class Multinomial<E> : IProbabilityDistribution<E>
	{
		private const long serialVersionUID = -697457414113362926L;

		private ICounter<E> parameters;

		public Multinomial(ICounter<E> parameters)
		{
			double totalMass = parameters.TotalCount();
			if (totalMass <= 0.0)
			{
				throw new Exception("total mass must be positive!");
			}
			this.parameters = new ClassicCounter<E>();
			foreach (E @object in parameters.KeySet())
			{
				double oldCount = parameters.GetCount(@object);
				if (oldCount < 0.0)
				{
					throw new Exception("no negative parameters allowed!");
				}
				this.parameters.SetCount(@object, oldCount / totalMass);
			}
		}

		public virtual ICounter<E> GetParameters()
		{
			return new ClassicCounter<E>(parameters);
		}

		public virtual double ProbabilityOf(E @object)
		{
			if (!parameters.KeySet().Contains(@object))
			{
				throw new Exception("Not a valid object for this multinomial!");
			}
			return parameters.GetCount(@object);
		}

		public virtual double LogProbabilityOf(E @object)
		{
			if (!parameters.KeySet().Contains(@object))
			{
				throw new Exception("Not a valid object for this multinomial!");
			}
			return Math.Log(parameters.GetCount(@object));
		}

		public virtual E DrawSample(Random random)
		{
			double r = random.NextDouble();
			double sum = 0.0;
			foreach (E @object in parameters.KeySet())
			{
				sum += parameters.GetCount(@object);
				if (sum >= r)
				{
					return @object;
				}
			}
			throw new Exception("This point should never be reached");
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Stats.Multinomial))
			{
				return false;
			}
			Edu.Stanford.Nlp.Stats.Multinomial otherMultinomial = (Edu.Stanford.Nlp.Stats.Multinomial)o;
			return parameters.Equals(otherMultinomial.parameters);
		}

		private int hashCode = -1;

		public override int GetHashCode()
		{
			if (hashCode == -1)
			{
				hashCode = parameters.GetHashCode() + 17;
			}
			return hashCode;
		}
	}
}
