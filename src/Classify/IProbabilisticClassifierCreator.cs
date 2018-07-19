using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Creates a probablic classifier with given weights</summary>
	/// <author>Angel Chang</author>
	public interface IProbabilisticClassifierCreator<L, F>
	{
		IProbabilisticClassifier<L, F> CreateProbabilisticClassifier(double[] weights);
	}
}
