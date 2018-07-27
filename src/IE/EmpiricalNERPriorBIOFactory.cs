using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	public class EmpiricalNERPriorBIOFactory<In> : IPriorModelFactory<In>
		where In : ICoreMap
	{
		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<In> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			EntityCachingAbstractSequencePriorBIO<In> prior = new EmpiricalNERPriorBIO<In>(flags.backgroundSymbol, classIndex, tagIndex, document, entityMatrices, flags);
			return prior;
		}
	}
}
