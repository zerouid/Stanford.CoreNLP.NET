using System.Collections.Generic;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	public class UniformPriorFactory<In> : IPriorModelFactory<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(UniformPriorFactory));

		public virtual IListeningSequenceModel GetInstance(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<In> document, Pair<double[][], double[][]> entityMatrices, SeqClassifierFlags flags)
		{
			// log.info("Using uniform prior!");
			UniformPrior<In> uniPrior = new UniformPrior<In>(flags.backgroundSymbol, classIndex, document);
			return uniPrior;
		}
	}
}
