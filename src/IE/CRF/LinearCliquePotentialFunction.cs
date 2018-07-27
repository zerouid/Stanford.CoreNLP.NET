

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class LinearCliquePotentialFunction : ICliquePotentialFunction
	{
		private readonly double[][] weights;

		internal LinearCliquePotentialFunction(double[][] weights)
		{
			this.weights = weights;
		}

		public virtual double ComputeCliquePotential(int cliqueSize, int labelIndex, int[] cliqueFeatures, double[] featureVal, int posInSent)
		{
			double output = 0.0;
			for (int m = 0; m < cliqueFeatures.Length; m++)
			{
				double dotProd = weights[cliqueFeatures[m]][labelIndex];
				if (featureVal != null)
				{
					dotProd *= featureVal[m];
				}
				output += dotProd;
			}
			return output;
		}
	}
}
