using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	public class UniformPriorFactory<In> : IPriorModelFactory<IN>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(UniformPriorFactory));

		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<IN> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			// log.info("Using uniform prior!");
			UniformPrior<IN> uniPrior = new UniformPrior<IN>(flags.backgroundSymbol, classIndex, document);
			return uniPrior;
		}
	}
}
