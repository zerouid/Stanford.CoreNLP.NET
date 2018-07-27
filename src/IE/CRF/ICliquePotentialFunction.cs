

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public interface ICliquePotentialFunction
	{
		/// <param name="cliqueSize">1 if node clique, 2 if edge clique, etc</param>
		/// <param name="labelIndex">the index of the output class label</param>
		/// <param name="cliqueFeatures">an int array containing the feature indices that are active in this clique</param>
		/// <param name="featureVal">a double array containing the feature values corresponding to feature indices in cliqueFeatures</param>
		/// <returns>clique potential value</returns>
		double ComputeCliquePotential(int cliqueSize, int labelIndex, int[] cliqueFeatures, double[] featureVal, int posInSent);
	}
}
