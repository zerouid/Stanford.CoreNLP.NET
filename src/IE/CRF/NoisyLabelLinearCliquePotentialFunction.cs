using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class NoisyLabelLinearCliquePotentialFunction : ICliquePotentialFunction
	{
		private readonly double[][] weights;

		private readonly int[] docLabels;

		private readonly double[][] errorMatrix;

		public NoisyLabelLinearCliquePotentialFunction(double[][] weights, int[] docLabels, double[][] errorMatrix)
		{
			this.weights = weights;
			this.docLabels = docLabels;
			this.errorMatrix = errorMatrix;
		}

		private double G(int labelIndex, int posInSent)
		{
			if (errorMatrix == null)
			{
				return 0;
			}
			int observed = docLabels[posInSent];
			return errorMatrix[labelIndex][observed];
		}

		public virtual double ComputeCliquePotential(int cliqueSize, int labelIndex, int[] cliqueFeatures, double[] featureVal, int posInSent)
		{
			double output = 0.0;
			double dotProd = 0;
			foreach (int cliqueFeature in cliqueFeatures)
			{
				dotProd = weights[cliqueFeature][labelIndex];
				output += dotProd;
			}
			if (cliqueSize == 1)
			{
				// add the noisy label part
				output += G(labelIndex, posInSent);
			}
			return output;
		}
	}
}
