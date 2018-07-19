using Edu.Stanford.Nlp.Classify;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <author>Jenny Finkel</author>
	public interface IScorer<L>
	{
		double Score<F>(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data);

		string GetDescription(int numDigits);
	}
}
