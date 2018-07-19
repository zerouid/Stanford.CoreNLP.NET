using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Used for creating an NER prior by reflection.</summary>
	/// <author>Christopher Manning</author>
	public class EmpiricalNERPriorFactory<In> : IPriorModelFactory<IN>
		where In : ICoreMap
	{
		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<IN> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			EntityCachingAbstractSequencePrior<IN> prior = new EmpiricalNERPrior<IN>(flags.backgroundSymbol, classIndex, document);
			// SamplingNERPrior prior = new SamplingNERPrior(flags.backgroundSymbol, classIndex, newDocument);
			return prior;
		}
	}
}
