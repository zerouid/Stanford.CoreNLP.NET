using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Rerank trees from the ParserQuery based on scores from a Reranker.</summary>
	/// <remarks>
	/// Rerank trees from the ParserQuery based on scores from a Reranker.
	/// <br />
	/// TODO: should handle Factored parsers as well
	/// </remarks>
	/// <author>John Bauer</author>
	public class RerankingParserQuery : IParserQuery
	{
		private readonly Options op;

		private readonly IParserQuery parserQuery;

		private readonly IReranker reranker;

		private readonly int rerankerKBest;

		private IList<ScoredObject<Tree>> scoredTrees;

		/// <summary>
		/// Data for this particular query stored by the Reranker will be
		/// stored in this object
		/// </summary>
		private IRerankerQuery rerankerQuery;

		public RerankingParserQuery(Options op, IParserQuery parserQuery, IReranker reranker)
		{
			this.op = op;
			this.parserQuery = parserQuery;
			this.reranker = reranker;
			this.rerankerKBest = op.rerankerKBest;
		}

		public virtual bool SaidMemMessage()
		{
			return parserQuery.SaidMemMessage();
		}

		public virtual void SetConstraints(IList<ParserConstraint> constraints)
		{
			parserQuery.SetConstraints(constraints);
		}

		public virtual bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			bool success = parserQuery.Parse(sentence);
			if (!success)
			{
				return false;
			}
			IList<ScoredObject<Tree>> bestKParses = parserQuery.GetKBestPCFGParses(rerankerKBest);
			if (bestKParses.IsEmpty())
			{
				return false;
			}
			scoredTrees = Rerank(sentence, bestKParses);
			return true;
		}

		public virtual bool ParseAndReport<_T0>(IList<_T0> sentence, PrintWriter pwErr)
			where _T0 : IHasWord
		{
			bool success = parserQuery.ParseAndReport(sentence, pwErr);
			if (!success)
			{
				return false;
			}
			IList<ScoredObject<Tree>> bestKParses = parserQuery.GetKBestPCFGParses(rerankerKBest);
			if (bestKParses.IsEmpty())
			{
				return false;
			}
			scoredTrees = Rerank(sentence, bestKParses);
			return true;
		}

		internal virtual IList<ScoredObject<Tree>> Rerank<_T0>(IList<_T0> sentence, IList<ScoredObject<Tree>> bestKParses)
			where _T0 : IHasWord
		{
			this.rerankerQuery = reranker.Process(sentence);
			IList<ScoredObject<Tree>> reranked = new List<ScoredObject<Tree>>();
			foreach (ScoredObject<Tree> scoredTree in bestKParses)
			{
				double score = scoredTree.Score();
				try
				{
					score = op.baseParserWeight * score + rerankerQuery.Score(scoredTree.Object());
				}
				catch (NoSuchParseException)
				{
					score = double.NegativeInfinity;
				}
				reranked.Add(new ScoredObject<Tree>(scoredTree.Object(), score));
			}
			reranked.Sort(ScoredComparator.DescendingComparator);
			return reranked;
		}

		public virtual Tree GetBestParse()
		{
			if (scoredTrees == null || scoredTrees.IsEmpty())
			{
				return null;
			}
			return scoredTrees[0].Object();
		}

		public virtual IList<ScoredObject<Tree>> GetKBestParses(int k)
		{
			return this.GetKBestPCFGParses(k);
		}

		public virtual double GetBestScore()
		{
			return this.GetPCFGScore();
		}

		public virtual Tree GetBestPCFGParse()
		{
			return GetBestParse();
		}

		public virtual double GetPCFGScore()
		{
			if (scoredTrees == null || scoredTrees.IsEmpty())
			{
				throw new AssertionError();
			}
			return scoredTrees[0].Score();
		}

		public virtual Tree GetBestDependencyParse(bool debinarize)
		{
			// TODO: barf?
			return null;
		}

		public virtual Tree GetBestFactoredParse()
		{
			// TODO: barf?
			return null;
		}

		public virtual IList<ScoredObject<Tree>> GetBestPCFGParses()
		{
			if (scoredTrees == null || scoredTrees.IsEmpty())
			{
				throw new AssertionError();
			}
			IList<ScoredObject<Tree>> equalTrees = Generics.NewArrayList();
			double score = scoredTrees[0].Score();
			int treePos = 0;
			while (treePos < scoredTrees.Count && scoredTrees[treePos].Score() == score)
			{
				equalTrees.Add(scoredTrees[treePos]);
			}
			return equalTrees;
		}

		public virtual void RestoreOriginalWords(Tree tree)
		{
			parserQuery.RestoreOriginalWords(tree);
		}

		public virtual bool HasFactoredParse()
		{
			return false;
		}

		public virtual IList<ScoredObject<Tree>> GetKBestPCFGParses(int kbestPCFG)
		{
			IList<ScoredObject<Tree>> trees = Generics.NewArrayList();
			for (int treePos = 0; treePos < scoredTrees.Count && treePos < kbestPCFG; ++treePos)
			{
				trees.Add(scoredTrees[treePos]);
			}
			return trees;
		}

		public virtual IList<ScoredObject<Tree>> GetKGoodFactoredParses(int kbest)
		{
			// TODO: barf?
			return null;
		}

		public virtual IKBestViterbiParser GetPCFGParser()
		{
			return null;
		}

		public virtual IKBestViterbiParser GetFactoredParser()
		{
			return null;
		}

		public virtual IKBestViterbiParser GetDependencyParser()
		{
			return null;
		}

		/// <summary>Parsing succeeded without any horrible errors or fallback</summary>
		public virtual bool ParseSucceeded()
		{
			return parserQuery.ParseSucceeded();
		}

		/// <summary>The sentence was skipped, probably because it was too long or of length 0</summary>
		public virtual bool ParseSkipped()
		{
			return parserQuery.ParseSkipped();
		}

		/// <summary>The model had to fall back to a simpler model on the previous parse</summary>
		public virtual bool ParseFallback()
		{
			return parserQuery.ParseFallback();
		}

		/// <summary>The model ran out of memory on the most recent parse</summary>
		public virtual bool ParseNoMemory()
		{
			return parserQuery.ParseNoMemory();
		}

		/// <summary>The model could not parse the most recent sentence for some reason</summary>
		public virtual bool ParseUnparsable()
		{
			return parserQuery.ParseUnparsable();
		}

		public virtual IList<IHasWord> OriginalSentence()
		{
			return parserQuery.OriginalSentence();
		}

		public virtual IRerankerQuery RerankerQuery()
		{
			return rerankerQuery;
		}
	}
}
