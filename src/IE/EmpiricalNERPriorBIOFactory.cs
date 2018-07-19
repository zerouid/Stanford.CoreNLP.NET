using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	public class EmpiricalNERPriorBIOFactory<In> : IPriorModelFactory<IN>
		where In : ICoreMap
	{
		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<IN> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			EntityCachingAbstractSequencePriorBIO<IN> prior = new EmpiricalNERPriorBIO<IN>(flags.backgroundSymbol, classIndex, tagIndex, document, entityMatrices, flags);
			return prior;
		}
	}
}
