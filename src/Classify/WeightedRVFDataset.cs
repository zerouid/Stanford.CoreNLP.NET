using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A weighted version of the RVF dataset.</summary>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class WeightedRVFDataset<L, F> : RVFDataset<L, F>
	{
		private const long serialVersionUID = 1L;

		internal float[] weights = new float[16];

		public WeightedRVFDataset()
			: base()
		{
		}

		protected internal WeightedRVFDataset(IIndex<L> labelIndex, int[] trainLabels, IIndex<F> featureIndex, int[][] trainData, double[][] trainValues, float[] trainWeights)
			: base(labelIndex, trainLabels, featureIndex, trainData, trainValues)
		{
			this.weights = trainWeights;
		}

		private float[] TrimToSize(float[] i)
		{
			float[] newI = new float[size];
			lock (typeof(Runtime))
			{
				System.Array.Copy(i, 0, newI, 0, size);
			}
			return newI;
		}

		/// <summary>Get the weight array for this dataset.</summary>
		/// <remarks>
		/// Get the weight array for this dataset.
		/// Used in, e.g.,
		/// <see cref="LogConditionalObjectiveFunction{L, F}"/>
		/// .
		/// </remarks>
		/// <returns>A float array of the weights of this dataset's datums.</returns>
		public virtual float[] GetWeights()
		{
			if (weights.Length != size)
			{
				weights = TrimToSize(weights);
			}
			return weights;
		}

		/// <summary>Register a weight in the weights array.</summary>
		/// <remarks>
		/// Register a weight in the weights array.
		/// This must be called before the superclass' methods.
		/// </remarks>
		/// <param name="weight">The weight to register.</param>
		private void AddWeight(float weight)
		{
			if (weights.Length == size)
			{
				float[] newWeights = new float[size * 2];
				lock (typeof(Runtime))
				{
					System.Array.Copy(weights, 0, newWeights, 0, size);
				}
				weights = newWeights;
			}
			weights[size] = weight;
		}

		// note: don't increment size!
		/// <summary>Add a datum, with a given weight.</summary>
		/// <param name="d">The datum to add.</param>
		/// <param name="weight">The weight of this datum.</param>
		public virtual void Add(RVFDatum<L, F> d, float weight)
		{
			AddWeight(weight);
			base.Add(d);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void Add(IDatum<L, F> d)
		{
			AddWeight(1.0f);
			base.Add(d);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void Add(IDatum<L, F> d, string src, string id)
		{
			AddWeight(1.0f);
			base.Add(d, src, id);
		}
	}
}
