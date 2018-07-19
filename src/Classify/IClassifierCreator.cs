using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Creates a classifier with given weights</summary>
	/// <author>Angel Chang</author>
	public interface IClassifierCreator<L, F>
	{
		IClassifier<L, F> CreateClassifier(double[] weights);
	}
}
