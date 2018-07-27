using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Used for creating an NER prior by reflection.</summary>
	/// <author>Christopher Manning</author>
	public class EmpiricalNERPriorFactory<In> : IPriorModelFactory<In>
		where In : ICoreMap
	{
		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<In> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			EntityCachingAbstractSequencePrior<In> prior = new EmpiricalNERPrior<In>(flags.backgroundSymbol, classIndex, document);
			// SamplingNERPrior prior = new SamplingNERPrior(flags.backgroundSymbol, classIndex, newDocument);
			return prior;
		}
	}
}
