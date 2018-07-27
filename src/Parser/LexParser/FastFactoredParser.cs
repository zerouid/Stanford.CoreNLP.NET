using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Provides a much faster way to realize the factored
	/// parsing idea, including easily returning "k good" results
	/// at the expense of optimality.
	/// </summary>
	/// <remarks>
	/// Provides a much faster way to realize the factored
	/// parsing idea, including easily returning "k good" results
	/// at the expense of optimality.  Exploiting the k best functionality
	/// of the ExhaustivePCFGParser, this model simply gets more than
	/// k best PCFG parsers, scores them according to the dependency
	/// grammar, and returns them in terms of their product score.
	/// No actual parsing is done.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class FastFactoredParser : IKBestViterbiParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.FastFactoredParser));

		protected internal const bool Verbose = false;

		protected internal ExhaustivePCFGParser pparser;

		protected internal IGrammarProjection projection;

		protected internal MLEDependencyGrammar dg;

		protected internal Options op;

		private int numToFind;

		private readonly IIndex<string> wordIndex;

		private readonly IIndex<string> tagIndex;

		// TODO Regression tests
		// TODO Set dependency tuning and test whether useful
		// TODO Validate and up the Arabic numbers
		// TODO Make the printing options for k good/best sane
		// TODO Check parsing of a List<String>.  Change defaultSentence() to be List<HasWord>
		protected internal virtual int Project(int state)
		{
			return projection.Project(state);
		}

		/// <summary>Return the best parse of the sentence most recently parsed.</summary>
		/// <returns>The best (highest score) tree</returns>
		public virtual Tree GetBestParse()
		{
			return nGoodTrees[0].Object();
		}

		public virtual double GetBestScore()
		{
			return nGoodTrees[0].Score();
		}

		public virtual bool HasParse()
		{
			return !nGoodTrees.IsEmpty();
		}

		private IList<ScoredObject<Tree>> nGoodTrees = new List<ScoredObject<Tree>>();

		/// <summary>Return the list of N "good" parses of the sentence most recently parsed.</summary>
		/// <remarks>
		/// Return the list of N "good" parses of the sentence most recently parsed.
		/// (The first is guaranteed to be the best, but later ones are only
		/// guaranteed the best subject to the possibilities that disappear because
		/// the PCFG/Dep charts only store the best over each span.)
		/// </remarks>
		/// <returns>The list of N best trees</returns>
		public virtual IList<ScoredObject<Tree>> GetKGoodParses(int k)
		{
			if (k <= nGoodTrees.Count)
			{
				return nGoodTrees.SubList(0, k);
			}
			else
			{
				throw new NotSupportedException("FastFactoredParser: cannot provide " + k + " good parses.");
			}
		}

		/// <summary>Use the DependencyGrammar to score the tree.</summary>
		/// <param name="tr">A binarized tree (as returned by the PCFG parser</param>
		/// <returns>The score for the tree according to the grammar</returns>
		private double DepScoreTree(Tree tr)
		{
			// log.info("Here's our tree:");
			// tr.pennPrint();
			// log.info(Trees.toDebugStructureString(tr));
			Tree cwtTree = tr.DeepCopy(new LabeledScoredTreeFactory(), new CategoryWordTagFactory());
			cwtTree.PercolateHeads(binHeadFinder);
			// log.info("Here's what it went to:");
			// cwtTree.pennPrint();
			IList<IntDependency> deps = MLEDependencyGrammar.TreeToDependencyList(cwtTree, wordIndex, tagIndex);
			// log.info("Here's the deps:\n" + deps);
			return dg.ScoreAll(deps);
		}

		private readonly IHeadFinder binHeadFinder = new BinaryHeadFinder();

		/// <summary>Parse a Sentence.</summary>
		/// <remarks>
		/// Parse a Sentence.  It is assumed that when this is called, the pparser
		/// has already been called to parse the sentence.
		/// </remarks>
		/// <param name="words">The list of words to parse.</param>
		/// <returns>true iff it could be parsed</returns>
		public virtual bool Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			nGoodTrees.Clear();
			int numParsesToConsider = numToFind * op.testOptions.fastFactoredCandidateMultiplier + op.testOptions.fastFactoredCandidateAddend;
			if (pparser.HasParse())
			{
				IList<ScoredObject<Tree>> pcfgBest = pparser.GetKBestParses(numParsesToConsider);
				Beam<ScoredObject<Tree>> goodParses = new Beam<ScoredObject<Tree>>(numToFind);
				foreach (ScoredObject<Tree> candidate in pcfgBest)
				{
					if (Thread.Interrupted())
					{
						throw new RuntimeInterruptedException();
					}
					double depScore = DepScoreTree(candidate.Object());
					ScoredObject<Tree> x = new ScoredObject<Tree>(candidate.Object(), candidate.Score() + depScore);
					goodParses.Add(x);
				}
				nGoodTrees = goodParses.AsSortedList();
			}
			return !nGoodTrees.IsEmpty();
		}

		/// <summary>Get the exact k best parses for the sentence.</summary>
		/// <param name="k">The number of best parses to return</param>
		/// <returns>
		/// The exact k best parses for the sentence, with
		/// each accompanied by its score (typically a
		/// negative log probability).
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetKBestParses(int k)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Get a complete set of the maximally scoring parses for a sentence,
		/// rather than one chosen at random.
		/// </summary>
		/// <remarks>
		/// Get a complete set of the maximally scoring parses for a sentence,
		/// rather than one chosen at random.  This set may be of size 1 or larger.
		/// </remarks>
		/// <returns>
		/// All the equal best parses for a sentence, with each
		/// accompanied by its score
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetBestParses()
		{
			throw new NotSupportedException();
		}

		/// <summary>Get k parse samples for the sentence.</summary>
		/// <remarks>
		/// Get k parse samples for the sentence.  It is expected that the
		/// parses are sampled based on their relative probability.
		/// </remarks>
		/// <param name="k">The number of sampled parses to return</param>
		/// <returns>
		/// A list of k parse samples for the sentence, with
		/// each accompanied by its score
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetKSampledParses(int k)
		{
			throw new NotSupportedException();
		}

		internal FastFactoredParser(ExhaustivePCFGParser pparser, MLEDependencyGrammar dg, Options op, int numToFind, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(pparser, dg, op, numToFind, new NullGrammarProjection(null, null), wordIndex, tagIndex)
		{
		}

		internal FastFactoredParser(ExhaustivePCFGParser pparser, MLEDependencyGrammar dg, Options op, int numToFind, IGrammarProjection projection, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			this.pparser = pparser;
			this.projection = projection;
			this.dg = dg;
			this.op = op;
			this.numToFind = numToFind;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
		}
	}
}
