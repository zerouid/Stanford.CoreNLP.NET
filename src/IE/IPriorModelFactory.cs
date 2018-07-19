using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	public interface IPriorModelFactory<In>
		where In : ICoreMap
	{
		IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<IN> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags);
	}
}
