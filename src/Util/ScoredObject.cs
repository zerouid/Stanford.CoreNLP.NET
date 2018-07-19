using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Wrapper class for holding a scored object.</summary>
	/// <author>Dan Klein</author>
	/// <version>2/7/01</version>
	[System.Serializable]
	public class ScoredObject<T> : IScored
	{
		private double score;

		public virtual double Score()
		{
			return score;
		}

		public virtual void SetScore(double score)
		{
			this.score = score;
		}

		private T @object;

		public virtual T Object()
		{
			return @object;
		}

		public virtual void SetObject(T @object)
		{
			this.@object = @object;
		}

		public ScoredObject(T @object, double score)
		{
			this.@object = @object;
			this.score = score;
		}

		public override string ToString()
		{
			return @object + " @ " + score;
		}

		private const long serialVersionUID = 1L;
	}
}
