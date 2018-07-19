using System.Collections;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	public class ExactGrammarCompactor : GrammarCompactor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ExactGrammarCompactor));

		internal TransducerGraph.IGraphProcessor quasiDeterminizer = new QuasiDeterminizer();

		internal IAutomatonMinimizer minimizer = new FastExactAutomatonMinimizer();

		internal TransducerGraph.INodeProcessor ntsp = new TransducerGraph.SetToStringNodeProcessor(new PennTreebankLanguagePack());

		internal TransducerGraph.INodeProcessor otsp = new TransducerGraph.ObjectToSetNodeProcessor();

		internal TransducerGraph.IArcProcessor isp = new TransducerGraph.InputSplittingProcessor();

		internal TransducerGraph.IArcProcessor ocp = new TransducerGraph.OutputCombiningProcessor();

		private bool saveGraphs;

		public ExactGrammarCompactor(Options op, bool saveGraphs, bool verbose)
			: base(op)
		{
			// = false;
			this.saveGraphs = saveGraphs;
			this.verbose = verbose;
			outputType = NormalizedLogProbabilities;
		}

		protected internal override TransducerGraph DoCompaction(TransducerGraph graph, IList l1, IList l3)
		{
			TransducerGraph result = graph;
			if (saveGraphs)
			{
				WriteFile(result, "unminimized", (string)result.GetEndNodes().GetEnumerator().Current);
			}
			result = quasiDeterminizer.ProcessGraph(result);
			result = new TransducerGraph(result, ocp);
			// combine outputs into inputs
			result = minimizer.MinimizeFA(result);
			// minimize the thing
			//result = new  TransducerGraph(graph, otsp); // for debugging
			result = new TransducerGraph(result, ntsp);
			// pull out strings from sets returned by minimizer
			result = new TransducerGraph(result, isp);
			// split outputs from inputs
			if (saveGraphs)
			{
				WriteFile(result, "exactminimized", (string)result.GetEndNodes().GetEnumerator().Current);
			}
			// for debugging do comparison of the paths accepted by graph and result
			//log.info(TransducerGraph.testGraphPaths(graph, result, 100));
			return result;
		}
	}
}
