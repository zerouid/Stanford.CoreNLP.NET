using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Common
{
	public interface IParserQuery
	{
		bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord;

		bool ParseAndReport<_T0>(IList<_T0> sentence, PrintWriter pwErr)
			where _T0 : IHasWord;

		double GetPCFGScore();

		Tree GetBestParse();

		IList<ScoredObject<Tree>> GetKBestParses(int k);

		double GetBestScore();

		Tree GetBestPCFGParse();

		Tree GetBestDependencyParse(bool debinarize);

		Tree GetBestFactoredParse();

		IList<ScoredObject<Tree>> GetBestPCFGParses();

		void RestoreOriginalWords(Tree tree);

		bool HasFactoredParse();

		IList<ScoredObject<Tree>> GetKBestPCFGParses(int kbestPCFG);

		IList<ScoredObject<Tree>> GetKGoodFactoredParses(int kbest);

		IKBestViterbiParser GetPCFGParser();

		IKBestViterbiParser GetFactoredParser();

		IKBestViterbiParser GetDependencyParser();

		void SetConstraints(IList<ParserConstraint> constraints);

		bool SaidMemMessage();

		/// <summary>Parsing succeeded without any horrible errors or fallback</summary>
		bool ParseSucceeded();

		/// <summary>The sentence was skipped, probably because it was too long or of length 0</summary>
		bool ParseSkipped();

		/// <summary>The model had to fall back to a simpler model on the previous parse</summary>
		bool ParseFallback();

		/// <summary>The model ran out of memory on the most recent parse</summary>
		bool ParseNoMemory();

		/// <summary>The model could not parse the most recent sentence for some reason</summary>
		bool ParseUnparsable();

		IList<IHasWord> OriginalSentence();
	}
}
