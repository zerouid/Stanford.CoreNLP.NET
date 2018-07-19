using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// Function for stochastic calculations that does update in place
	/// (instead of maintaining and returning the derivative).
	/// </summary>
	/// <remarks>
	/// Function for stochastic calculations that does update in place
	/// (instead of maintaining and returning the derivative).
	/// Weights are represented by an array of doubles and a scalar
	/// that indicates how much to scale all weights by.
	/// This allows all weights to be scaled by just modifying the scalar.
	/// </remarks>
	/// <author>Angel Chang</author>
	public abstract class AbstractStochasticCachingDiffUpdateFunction : AbstractStochasticCachingDiffFunction
	{
		protected internal bool skipValCalc = false;

		/// <summary>Gets a random sample (this is sampling with replacement).</summary>
		/// <param name="sampleSize">number of samples to generate</param>
		/// <returns>array of indices for random sample of sampleSize</returns>
		public virtual int[] GetSample(int sampleSize)
		{
			int[] sample = new int[sampleSize];
			for (int i = 0; i < sampleSize; i++)
			{
				sample[i] = randGenerator.NextInt(this.DataDimension());
			}
			// Just generate a random index
			return sample;
		}

		/// <summary>
		/// Computes value of function for specified value of x (scaled by xScale)
		/// only over samples indexed by batch.
		/// </summary>
		/// <param name="x">unscaled weights</param>
		/// <param name="xScale">how much to scale x by when performing calculations</param>
		/// <param name="batch">indices of which samples to compute function over</param>
		/// <returns>value of function at specified x (scaled by xScale) for samples</returns>
		public abstract double ValueAt(double[] x, double xScale, int[] batch);

		public virtual double ValueAt(double[] x, double xScale, int batchSize)
		{
			GetBatch(batchSize);
			return ValueAt(x, xScale, thisBatch);
		}

		/// <summary>
		/// Performs stochastic update of weights x (scaled by xScale) based
		/// on samples indexed by batch.
		/// </summary>
		/// <param name="x">unscaled weights</param>
		/// <param name="xScale">how much to scale x by when performing calculations</param>
		/// <param name="batch">indices of which samples to compute function over</param>
		/// <param name="gain">how much to scale adjustments to x</param>
		/// <returns>value of function at specified x (scaled by xScale) for samples</returns>
		public abstract double CalculateStochasticUpdate(double[] x, double xScale, int[] batch, double gain);

		/// <summary>
		/// Performs stochastic update of weights x (scaled by xScale) based
		/// on next batch of batchSize.
		/// </summary>
		/// <param name="x">unscaled weights</param>
		/// <param name="xScale">how much to scale x by when performing calculations</param>
		/// <param name="batchSize">number of samples to pick next</param>
		/// <param name="gain">how much to scale adjustments to x</param>
		/// <returns>value of function at specified x (scaled by xScale) for samples</returns>
		public virtual double CalculateStochasticUpdate(double[] x, double xScale, int batchSize, double gain)
		{
			GetBatch(batchSize);
			return CalculateStochasticUpdate(x, xScale, thisBatch, gain);
		}

		/// <summary>
		/// Performs stochastic gradient calculation based
		/// on samples indexed by batch and does not apply regularization.
		/// </summary>
		/// <remarks>
		/// Performs stochastic gradient calculation based
		/// on samples indexed by batch and does not apply regularization.
		/// Does not update the parameter values.
		/// Typically stores derivative information for later access.
		/// </remarks>
		/// <param name="x">Unscaled weights</param>
		/// <param name="batch">Indices of which samples to compute function over</param>
		public abstract void CalculateStochasticGradient(double[] x, int[] batch);

		/// <summary>
		/// Performs stochastic gradient updates based
		/// on samples indexed by batch and do not apply regularization.
		/// </summary>
		/// <param name="x">unscaled weights</param>
		/// <param name="batchSize">number of samples to pick next</param>
		public virtual void CalculateStochasticGradient(double[] x, int batchSize)
		{
			GetBatch(batchSize);
			CalculateStochasticGradient(x, thisBatch);
		}
	}
}
