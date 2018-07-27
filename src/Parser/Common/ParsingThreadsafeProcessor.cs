using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Concurrent;



namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>Takes a sentence and returns a ParserQuery with the given sentence parsed.</summary>
	/// <remarks>
	/// Takes a sentence and returns a ParserQuery with the given sentence parsed.
	/// Can be used in a MulticoreWrapper.
	/// </remarks>
	/// <author>John Bauer</author>
	public class ParsingThreadsafeProcessor : IThreadsafeProcessor<IList<IHasWord>, IParserQuery>
	{
		internal ParserGrammar pqFactory;

		internal PrintWriter pwErr;

		public ParsingThreadsafeProcessor(ParserGrammar pqFactory)
			: this(pqFactory, null)
		{
		}

		public ParsingThreadsafeProcessor(ParserGrammar pqFactory, PrintWriter pwErr)
		{
			this.pqFactory = pqFactory;
			this.pwErr = pwErr;
		}

		public virtual IParserQuery Process<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			IParserQuery pq = pqFactory.ParserQuery();
			if (pwErr != null)
			{
				pq.ParseAndReport(sentence, pwErr);
			}
			else
			{
				pq.Parse(sentence);
			}
			return pq;
		}

		public virtual IThreadsafeProcessor<IList<IHasWord>, IParserQuery> NewInstance()
		{
			// ParserQueryFactories should be threadsafe
			return this;
		}
	}
}
