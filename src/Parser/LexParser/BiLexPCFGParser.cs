using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Implements Eisner and Satta style algorithms for bilexical
	/// PCFG parsing.
	/// </summary>
	/// <remarks>
	/// Implements Eisner and Satta style algorithms for bilexical
	/// PCFG parsing.  The basic class provides O(n<sup>4</sup>)
	/// parsing, with the passed in PCFG and dependency parsers
	/// providing outside scores in an efficient A* search.
	/// </remarks>
	/// <author>Dan Klein</author>
	public class BiLexPCFGParser : IKBestViterbiParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.BiLexPCFGParser));

		protected internal const bool Verbose = false;

		protected internal const bool VeryVerbose = false;

		protected internal HookChart chart;

		protected internal IHeap<Item> agenda;

		protected internal int length;

		protected internal int[] words;

		protected internal Edge goal;

		protected internal Interner interner;

		protected internal IScorer scorer;

		protected internal ExhaustivePCFGParser fscorer;

		protected internal ExhaustiveDependencyParser dparser;

		protected internal IGrammarProjection projection;

		protected internal BinaryGrammar bg;

		protected internal UnaryGrammar ug;

		protected internal IDependencyGrammar dg;

		protected internal ILexicon lex;

		protected internal Options op;

		protected internal IList<IntTaggedWord>[] taggedWordList;

		protected internal readonly IIndex<string> wordIndex;

		protected internal readonly IIndex<string> tagIndex;

		protected internal readonly IIndex<string> stateIndex;

		protected internal CoreLabel[] originalLabels;

		protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

		protected internal long relaxHook1 = 0;

		protected internal long relaxHook2 = 0;

		protected internal long relaxHook3 = 0;

		protected internal long relaxHook4 = 0;

		protected internal long builtHooks = 0;

		protected internal long builtEdges = 0;

		protected internal long extractedHooks = 0;

		protected internal long extractedEdges = 0;

		private const double Tol = 1e-10;

		//pair dep scores
		// temp
		protected internal static bool Better(double x, double y)
		{
			return ((x - y) / (Math.Abs(x) + Math.Abs(y) + 1e-100) > Tol);
		}

		public virtual double GetBestScore()
		{
			if (goal == null)
			{
				return double.NegativeInfinity;
			}
			else
			{
				return goal.Score();
			}
		}

		protected internal virtual Tree ExtractParse(Edge edge)
		{
			string head = wordIndex.Get(words[edge.head]);
			string tag = tagIndex.Get(edge.tag);
			string state = stateIndex.Get(edge.state);
			ILabel label = new CategoryWordTag(state, head, tag);
			if (edge.backEdge == null && edge.backHook == null)
			{
				// leaf, but needs word terminal
				Tree leaf;
				if (originalLabels[edge.head] != null)
				{
					leaf = tf.NewLeaf(originalLabels[edge.head]);
				}
				else
				{
					leaf = tf.NewLeaf(head);
				}
				IList<Tree> childList = Java.Util.Collections.SingletonList(leaf);
				return tf.NewTreeNode(label, childList);
			}
			if (edge.backHook == null)
			{
				// unary
				IList<Tree> childList = Java.Util.Collections.SingletonList(ExtractParse(edge.backEdge));
				return tf.NewTreeNode(label, childList);
			}
			// binary
			IList<Tree> children = new List<Tree>();
			if (edge.backHook.IsPreHook())
			{
				children.Add(ExtractParse(edge.backEdge));
				children.Add(ExtractParse(edge.backHook.backEdge));
			}
			else
			{
				children.Add(ExtractParse(edge.backHook.backEdge));
				children.Add(ExtractParse(edge.backEdge));
			}
			return tf.NewTreeNode(label, children);
		}

		/// <summary>Return the best parse of the sentence most recently parsed.</summary>
		/// <returns>The best (highest score) tree</returns>
		public virtual Tree GetBestParse()
		{
			return ExtractParse(goal);
		}

		public virtual bool HasParse()
		{
			return goal != null && goal.iScore != double.NegativeInfinity;
		}

		protected internal IList<Edge> nGoodTrees = new LinkedList<Edge>();

		// Added by Dan Zeman to store the list of N best trees.
		/// <summary>Return the list of k "good" parses of the sentence most recently parsed.</summary>
		/// <remarks>
		/// Return the list of k "good" parses of the sentence most recently parsed.
		/// (The first is guaranteed to be the best, but later ones are only
		/// guaranteed the best subject to the possibilities that disappear because
		/// the PCFG/Dep charts only store the best over each span.)
		/// </remarks>
		/// <returns>The list of k best trees</returns>
		public virtual IList<ScoredObject<Tree>> GetKGoodParses(int k)
		{
			IList<ScoredObject<Tree>> nGoodTreesList = new List<ScoredObject<Tree>>(op.testOptions.printFactoredKGood);
			foreach (Edge e in nGoodTrees)
			{
				nGoodTreesList.Add(new ScoredObject<Tree>(ExtractParse(e), e.iScore));
			}
			return nGoodTreesList;
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
			throw new NotSupportedException("BiLexPCFGParser doesn't support k best parses");
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
			throw new NotSupportedException("BiLexPCFGParser doesn't support best parses");
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
			throw new NotSupportedException("BiLexPCFGParser doesn't support k sampled parses");
		}

		protected internal Edge tempEdge;

		protected internal virtual void Combine(Edge edge, Hook hook)
		{
			// make result edge
			if (hook.IsPreHook())
			{
				tempEdge.start = edge.start;
				tempEdge.end = hook.end;
			}
			else
			{
				tempEdge.start = hook.start;
				tempEdge.end = edge.end;
			}
			tempEdge.state = hook.state;
			tempEdge.head = hook.head;
			tempEdge.tag = hook.tag;
			tempEdge.iScore = hook.iScore + edge.iScore;
			tempEdge.backEdge = edge;
			tempEdge.backHook = hook;
			RelaxTempEdge();
		}

		protected internal virtual void RelaxTempEdge()
		{
			// if (tempEdge.iScore > scorer.iScore(tempEdge)+1e-4) {
			//   log.info(tempEdge+" has i "+tempEdge.iScore+" iE "+scorer.iScore(tempEdge));
			// }
			Edge resultEdge = (Edge)interner.Intern(tempEdge);
			if (resultEdge == tempEdge)
			{
				tempEdge = new Edge(op.testOptions.exhaustiveTest);
				DiscoverEdge(resultEdge);
			}
			else
			{
				if (Better(tempEdge.iScore, resultEdge.iScore) && resultEdge.oScore > double.NegativeInfinity)
				{
					// we've found a better way of making an edge that may make a parse
					double back = resultEdge.iScore;
					Edge backE = resultEdge.backEdge;
					Hook backH = resultEdge.backHook;
					resultEdge.iScore = tempEdge.iScore;
					resultEdge.backEdge = tempEdge.backEdge;
					resultEdge.backHook = tempEdge.backHook;
					try
					{
						agenda.DecreaseKey(resultEdge);
					}
					catch (ArgumentNullException)
					{
						if (false)
						{
							log.Info(string.Empty);
							log.Info("Old backEdge: " + backE + " i " + backE.iScore + " o " + backE.oScore + " s " + backE.Score());
							log.Info("Old backEdge: " + backE + " iE " + scorer.IScore(backE));
							log.Info("Old backHook: " + backH + " i " + backH.iScore + " o " + backH.oScore + " s " + backH.Score());
							log.Info("New backEdge: " + tempEdge.backEdge + " i " + tempEdge.backEdge.iScore + " o " + tempEdge.backEdge.oScore + " s " + tempEdge.backEdge.Score());
							log.Info("New backEdge: " + tempEdge.backEdge + " iE " + scorer.IScore(tempEdge.backEdge));
							log.Info("New backHook: " + tempEdge.backHook + " i " + tempEdge.backHook.iScore + " o " + tempEdge.backHook.oScore + " s " + tempEdge.backHook.Score());
							log.Error("Formed " + resultEdge + " i " + tempEdge.iScore + " o " + resultEdge.oScore + " s " + resultEdge.Score());
							log.Error("Formed " + resultEdge + " " + (resultEdge == tempEdge ? "new" : "old") + " " + tempEdge.iScore + " was " + back + " better? " + Better(tempEdge.iScore, back));
						}
					}
				}
			}
		}

		protected internal virtual void DiscoverEdge(Edge edge)
		{
			// create new edge
			edge.oScore = scorer.OScore(edge);
			agenda.Add(edge);
			builtEdges++;
		}

		protected internal virtual void DiscoverHook(Hook hook)
		{
			hook.oScore = BuildOScore(hook);
			if (hook.oScore == double.NegativeInfinity)
			{
				relaxHook4++;
			}
			builtHooks++;
			agenda.Add(hook);
		}

		protected internal virtual double BuildOScore(Hook hook)
		{
			double bestOScore = double.NegativeInfinity;
			Edge iTemp = new Edge(op.testOptions.exhaustiveTest);
			Edge oTemp = new Edge(op.testOptions.exhaustiveTest);
			iTemp.head = hook.head;
			iTemp.tag = hook.tag;
			iTemp.state = hook.subState;
			oTemp.head = hook.head;
			oTemp.tag = hook.tag;
			oTemp.state = hook.state;
			if (hook.IsPreHook())
			{
				iTemp.end = hook.start;
				oTemp.end = hook.end;
				for (int start = 0; start <= hook.head; start++)
				{
					iTemp.start = start;
					oTemp.start = start;
					double oScore = scorer.OScore(oTemp) + scorer.IScore(iTemp);
					//log.info("Score for "+hook+" is i "+iTemp+" ("+scorer.iScore(iTemp)+") o "+oTemp+" ("+scorer.oScore(oTemp)+")");
					bestOScore = SloppyMath.Max(bestOScore, oScore);
				}
			}
			else
			{
				iTemp.start = hook.end;
				oTemp.start = hook.start;
				for (int end = hook.head + 1; end <= length; end++)
				{
					iTemp.end = end;
					oTemp.end = end;
					double oScore = scorer.OScore(oTemp) + scorer.IScore(iTemp);
					bestOScore = SloppyMath.Max(bestOScore, oScore);
				}
			}
			return bestOScore;
		}

		protected internal Hook tempHook;

		protected internal virtual void ProjectHooks(Edge edge)
		{
			// form hooks
			// POST HOOKS
			//for (Iterator rI = bg.ruleIteratorByLeftChild(edge.state);
			//      rI.hasNext(); ) {
			IList<BinaryRule> ruleList = bg.RuleListByLeftChild(edge.state);
			foreach (BinaryRule br in ruleList)
			{
				//BinaryRule br = rI.next();
				if (scorer is ILatticeScorer)
				{
					ILatticeScorer lscorer = (ILatticeScorer)scorer;
					Edge latEdge = (Edge)lscorer.ConvertItemSpan(new Edge(edge));
					if (!fscorer.OPossibleL(Project(br.parent), latEdge.start) || !fscorer.IPossibleL(Project(br.rightChild), latEdge.end))
					{
						continue;
					}
				}
				else
				{
					if (!fscorer.OPossibleL(Project(br.parent), edge.start) || !fscorer.IPossibleL(Project(br.rightChild), edge.end))
					{
						continue;
					}
				}
				for (int head = edge.end; head < length; head++)
				{
					// cdm Apr 2006: avoid Iterator allocation
					// for (Iterator iTWI = taggedWordList[head].iterator(); iTWI.hasNext();) {
					// IntTaggedWord iTW = (IntTaggedWord) iTWI.next();
					for (int hdi = 0; hdi < sz; hdi++)
					{
						IntTaggedWord iTW = taggedWordList[head][hdi];
						int tag = iTW.tag;
						tempHook.start = edge.start;
						tempHook.end = edge.end;
						tempHook.head = head;
						tempHook.tag = tag;
						tempHook.state = br.parent;
						tempHook.subState = br.rightChild;
						if (!chart.IsBuiltL(tempHook.subState, tempHook.end, tempHook.head, tempHook.tag))
						{
							continue;
						}
						tempHook.iScore = edge.iScore + br.score + dparser.headScore[dparser.binDistance[head][edge.end]][head][dg.TagBin(tag)][edge.head][dg.TagBin(edge.tag)] + dparser.headStop[edge.head][dg.TagBin(edge.tag)][edge.start] + dparser.headStop[edge.head
							][dg.TagBin(edge.tag)][edge.end];
						tempHook.backEdge = edge;
						RelaxTempHook();
					}
				}
			}
			// PRE HOOKS
			//for (Iterator<BinaryRule> rI = bg.ruleIteratorByRightChild(edge.state);
			//     rI.hasNext(); ) {
			ruleList = bg.RuleListByRightChild(edge.state);
			foreach (BinaryRule br_1 in ruleList)
			{
				//BinaryRule br = rI.next();
				if (scorer is ILatticeScorer)
				{
					ILatticeScorer lscorer = (ILatticeScorer)scorer;
					Edge latEdge = (Edge)lscorer.ConvertItemSpan(new Edge(edge));
					if (!fscorer.OPossibleR(Project(br_1.parent), latEdge.end) || !fscorer.IPossibleR(Project(br_1.leftChild), latEdge.start))
					{
						continue;
					}
				}
				else
				{
					if (!fscorer.OPossibleR(Project(br_1.parent), edge.end) || !fscorer.IPossibleR(Project(br_1.leftChild), edge.start))
					{
						continue;
					}
				}
				for (int head = 0; head < edge.start; head++)
				{
					// cdm Apr 2006: avoid Iterator allocation
					// for (Iterator iTWI = taggedWordList[head].iterator(); iTWI.hasNext();) {
					//IntTaggedWord iTW = (IntTaggedWord) iTWI.next();
					for (int hdi = 0; hdi < sz; hdi++)
					{
						IntTaggedWord iTW = taggedWordList[head][hdi];
						int tag = iTW.tag;
						tempHook.start = edge.start;
						tempHook.end = edge.end;
						tempHook.head = head;
						tempHook.tag = tag;
						tempHook.state = br_1.parent;
						tempHook.subState = br_1.leftChild;
						if (!chart.IsBuiltR(tempHook.subState, tempHook.start, tempHook.head, tempHook.tag))
						{
							continue;
						}
						tempHook.iScore = edge.iScore + br_1.score + dparser.headScore[dparser.binDistance[head][edge.start]][head][dg.TagBin(tag)][edge.head][dg.TagBin(edge.tag)] + dparser.headStop[edge.head][dg.TagBin(edge.tag)][edge.start] + dparser.headStop[edge
							.head][dg.TagBin(edge.tag)][edge.end];
						tempHook.backEdge = edge;
						RelaxTempHook();
					}
				}
			}
		}

		protected internal virtual void RegisterReal(Edge real)
		{
			chart.RegisterRealEdge(real);
		}

		protected internal virtual void TriggerHooks(Edge edge)
		{
			// we might have built a synth edge, enabling some old real edges to project hooks (the difference between this method and triggerAllHooks is that here we look only at realEdges)
			bool newL = !chart.IsBuiltL(edge.state, edge.start, edge.head, edge.tag);
			bool newR = !chart.IsBuiltR(edge.state, edge.end, edge.head, edge.tag);
			chart.RegisterEdgeIndexes(edge);
			if (newR)
			{
				// PRE HOOKS
				BinaryRule[] rules = bg.SplitRulesWithLC(edge.state);
				foreach (BinaryRule br in rules)
				{
					ICollection<Edge> realEdges = chart.GetRealEdgesWithL(br.rightChild, edge.end);
					foreach (Edge real in realEdges)
					{
						tempHook.start = real.start;
						tempHook.end = real.end;
						tempHook.state = br.parent;
						tempHook.subState = br.leftChild;
						tempHook.head = edge.head;
						tempHook.tag = edge.tag;
						tempHook.backEdge = real;
						tempHook.iScore = real.iScore + br.score + dparser.headScore[dparser.binDistance[edge.head][edge.end]][edge.head][dg.TagBin(edge.tag)][real.head][dg.TagBin(real.tag)] + dparser.headStop[real.head][dg.TagBin(real.tag)][real.start] + dparser.headStop
							[real.head][dg.TagBin(real.tag)][real.end];
						RelaxTempHook();
					}
				}
			}
			if (newL)
			{
				// POST HOOKS
				BinaryRule[] rules = bg.SplitRulesWithRC(edge.state);
				foreach (BinaryRule br in rules)
				{
					ICollection<Edge> realEdges = chart.GetRealEdgesWithR(br.leftChild, edge.start);
					foreach (Edge real in realEdges)
					{
						tempHook.start = real.start;
						tempHook.end = real.end;
						tempHook.state = br.parent;
						tempHook.subState = br.rightChild;
						tempHook.head = edge.head;
						tempHook.tag = edge.tag;
						tempHook.backEdge = real;
						tempHook.iScore = real.iScore + br.score + dparser.headScore[dparser.binDistance[edge.head][edge.start]][edge.head][dg.TagBin(edge.tag)][real.head][dg.TagBin(real.tag)] + dparser.headStop[real.head][dg.TagBin(real.tag)][real.start] + dparser
							.headStop[real.head][dg.TagBin(real.tag)][real.end];
						RelaxTempHook();
					}
				}
			}
		}

		protected internal virtual void TriggerAllHooks(Edge edge)
		{
			// we might have built a new edge, enabling some old edges to project hooks
			bool newL = !chart.IsBuiltL(edge.state, edge.start, edge.head, edge.tag);
			bool newR = !chart.IsBuiltR(edge.state, edge.end, edge.head, edge.tag);
			chart.RegisterEdgeIndexes(edge);
			if (newR)
			{
				// PRE HOOKS
				for (IEnumerator<BinaryRule> rI = bg.RuleIteratorByLeftChild(edge.state); rI.MoveNext(); )
				{
					BinaryRule br = rI.Current;
					ICollection<Edge> edges = chart.GetRealEdgesWithL(br.rightChild, edge.end);
					foreach (Edge real in edges)
					{
						tempHook.start = real.start;
						tempHook.end = real.end;
						tempHook.state = br.parent;
						tempHook.subState = br.leftChild;
						tempHook.head = edge.head;
						tempHook.tag = edge.tag;
						tempHook.backEdge = real;
						tempHook.iScore = real.iScore + br.score + dparser.headScore[dparser.binDistance[edge.head][edge.end]][edge.head][dg.TagBin(edge.tag)][real.head][dg.TagBin(real.tag)] + dparser.headStop[real.head][dg.TagBin(real.tag)][real.start] + dparser.headStop
							[real.head][dg.TagBin(real.tag)][real.end];
						RelaxTempHook();
					}
				}
			}
			if (newL)
			{
				// POST HOOKS
				for (IEnumerator rI = bg.RuleIteratorByRightChild(edge.state); rI.MoveNext(); )
				{
					BinaryRule br = (BinaryRule)rI.Current;
					ICollection<Edge> edges = chart.GetRealEdgesWithR(br.leftChild, edge.start);
					foreach (Edge real in edges)
					{
						tempHook.start = real.start;
						tempHook.end = real.end;
						tempHook.state = br.parent;
						tempHook.subState = br.rightChild;
						tempHook.head = edge.head;
						tempHook.tag = edge.tag;
						tempHook.backEdge = real;
						tempHook.iScore = real.iScore + br.score + dparser.headScore[dparser.binDistance[edge.head][edge.start]][edge.head][dg.TagBin(edge.tag)][real.head][dg.TagBin(real.tag)] + dparser.headStop[real.head][dg.TagBin(real.tag)][real.start] + dparser
							.headStop[real.head][dg.TagBin(real.tag)][real.end];
						RelaxTempHook();
					}
				}
			}
		}

		protected internal virtual void RelaxTempHook()
		{
			relaxHook1++;
			if (!scorer.OPossible(tempHook) || !scorer.IPossible(tempHook))
			{
				return;
			}
			relaxHook2++;
			Hook resultHook = (Hook)interner.Intern(tempHook);
			if (resultHook == tempHook)
			{
				relaxHook3++;
				tempHook = new Hook(op.testOptions.exhaustiveTest);
				DiscoverHook(resultHook);
			}
			if (Better(tempHook.iScore, resultHook.iScore))
			{
				resultHook.iScore = tempHook.iScore;
				resultHook.backEdge = tempHook.backEdge;
				try
				{
					agenda.DecreaseKey(resultHook);
				}
				catch (ArgumentNullException)
				{
				}
			}
		}

		protected internal virtual void ProjectUnaries(Edge edge)
		{
			for (IEnumerator rI = ug.RuleIteratorByChild(edge.state); rI.MoveNext(); )
			{
				UnaryRule ur = (UnaryRule)rI.Current;
				if (ur.child == ur.parent)
				{
					continue;
				}
				tempEdge.start = edge.start;
				tempEdge.end = edge.end;
				tempEdge.head = edge.head;
				tempEdge.tag = edge.tag;
				tempEdge.state = ur.parent;
				tempEdge.backEdge = edge;
				tempEdge.backHook = null;
				tempEdge.iScore = edge.iScore + ur.score;
				RelaxTempEdge();
			}
		}

		protected internal virtual void ProcessEdge(Edge edge)
		{
			// add to chart
			chart.AddEdge(edge);
			// fetch existing hooks that can combine with it and combine them
			foreach (Hook hook in chart.GetPreHooks(edge))
			{
				Combine(edge, hook);
			}
			foreach (Hook hook_1 in chart.GetPostHooks(edge))
			{
				Combine(edge, hook_1);
			}
			// do projections
			//if (VERBOSE) log.info("Projecting: "+edge);
			ProjectUnaries(edge);
			if (!bg.IsSynthetic(edge.state) && !op.freeDependencies)
			{
				ProjectHooks(edge);
				RegisterReal(edge);
			}
			if (op.freeDependencies)
			{
				ProjectHooks(edge);
				RegisterReal(edge);
				TriggerAllHooks(edge);
			}
			else
			{
				TriggerHooks(edge);
			}
		}

		protected internal virtual void ProcessHook(Hook hook)
		{
			// add to chart
			//if (VERBOSE) log.info("Adding to chart: "+hook);
			chart.AddHook(hook);
			ICollection<Edge> edges = chart.GetEdges(hook);
			foreach (Edge edge in edges)
			{
				Combine(edge, hook);
			}
		}

		protected internal virtual void ProcessItem(Item item)
		{
			if (item.IsEdge())
			{
				ProcessEdge((Edge)item);
			}
			else
			{
				ProcessHook((Hook)item);
			}
		}

		protected internal virtual void DiscoverItem(Item item)
		{
			if (item.IsEdge())
			{
				DiscoverEdge((Edge)item);
			}
			else
			{
				DiscoverHook((Hook)item);
			}
		}

		protected internal virtual Item MakeInitialItem(int pos, int tag, int state, double iScore)
		{
			Edge edge = new Edge(op.testOptions.exhaustiveTest);
			edge.start = pos;
			edge.end = pos + 1;
			edge.state = state;
			edge.head = pos;
			edge.tag = tag;
			edge.iScore = iScore;
			return edge;
		}

		protected internal virtual IList<Item> MakeInitialItems<_T0>(IList<_T0> wordList)
			where _T0 : IHasWord
		{
			IList<Item> itemList = new List<Item>();
			int length = wordList.Count;
			int numTags = tagIndex.Size();
			words = new int[length];
			taggedWordList = new IList[length];
			int terminalCount = 0;
			originalLabels = new CoreLabel[wordList.Count];
			for (int i = 0; i < length; i++)
			{
				taggedWordList[i] = new List<IntTaggedWord>(numTags);
				IHasWord wordObject = wordList[i];
				if (wordObject is CoreLabel)
				{
					originalLabels[i] = (CoreLabel)wordObject;
				}
				string wordStr = wordObject.Word();
				//Word context (e.g., morphosyntactic info)
				string wordContextStr = null;
				if (wordObject is IHasContext)
				{
					wordContextStr = ((IHasContext)wordObject).OriginalText();
					if (string.Empty.Equals(wordContextStr))
					{
						wordContextStr = null;
					}
				}
				if (!wordIndex.Contains(wordStr))
				{
					wordStr = LexiconConstants.UnknownWord;
				}
				int word = wordIndex.IndexOf(wordStr);
				words[i] = word;
				for (IEnumerator<IntTaggedWord> tagI = lex.RuleIteratorByWord(word, i, wordContextStr); tagI.MoveNext(); )
				{
					IntTaggedWord tagging = tagI.Current;
					int tag = tagging.tag;
					//String curTagStr = tagIndex.get(tag);
					//if (!tagStr.equals("") && !tagStr.equals(curTagStr))
					//  continue;
					int state = stateIndex.IndexOf(tagIndex.Get(tag));
					//itemList.add(makeInitialItem(i,tag,state,1.0*tagging.score));
					// THIS WILL CAUSE BUGS!!!  Don't use with another A* scorer
					tempEdge.state = state;
					tempEdge.head = i;
					tempEdge.start = i;
					tempEdge.end = i + 1;
					tempEdge.tag = tag;
					itemList.Add(MakeInitialItem(i, tag, state, scorer.IScore(tempEdge)));
					terminalCount++;
					taggedWordList[i].Add(new IntTaggedWord(word, tag));
				}
			}
			if (op.testOptions.verbose)
			{
				log.Info("Terminals (# of tag edges in chart): " + terminalCount);
			}
			return itemList;
		}

		protected internal virtual void ScoreDependencies()
		{
		}

		// just leach it off the dparser for now...
		/*
		IntDependency dependency = new IntDependency();
		for (int head = 0; head < words.length; head++) {
		for (int hTag = 0; hTag < tagIndex.size(); hTag++) {
		for (int arg = 0; arg < words.length; arg++) {
		for (int aTag = 0; aTag < tagIndex.size(); aTag++) {
		Arrays.fill(depScore[head][hTag][arg][aTag],Float.NEGATIVE_INFINITY);
		}
		}
		}
		}
		for (int head = 0; head < words.length; head++) {
		for (int arg = 0; arg < words.length; arg++) {
		if (head == arg)
		continue;
		for (Iterator<IntTaggedWord> headTWI=taggedWordList[head].iterator(); headTWI.hasNext();) {
		IntTaggedWord headTW = headTWI.next();
		for (Iterator<IntTaggedWord> argTWI=taggedWordList[arg].iterator(); argTWI.hasNext();) {
		IntTaggedWord argTW = argTWI.next();
		dependency.head = headTW;
		dependency.arg = argTW;
		dependency.leftHeaded = (head < arg);
		dependency.distance = Math.abs(head-arg);
		depScore[head][headTW.tag][arg][argTW.tag] =
		dg.score(dependency);
		if (false && depScore[head][headTW.tag][arg][argTW.tag] > -100)
		log.info(wordIndex.get(headTW.word)+"/"+tagIndex.get(headTW.tag)+" -> "+wordIndex.get(argTW.word)+"/"+tagIndex.get(argTW.tag)+" score "+depScore[head][headTW.tag][arg][argTW.tag]);
		}
		}
		}
		}
		*/
		protected internal virtual void SetGoal(int length)
		{
			goal = new Edge(op.testOptions.exhaustiveTest);
			goal.start = 0;
			goal.end = length;
			goal.state = stateIndex.IndexOf(op.Langpack().StartSymbol());
			goal.tag = tagIndex.IndexOf(LexiconConstants.BoundaryTag);
			goal.head = length - 1;
		}

		//goal = (Edge)interner.intern(goal);
		protected internal virtual void Initialize<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			length = words.Count;
			interner = new Interner();
			agenda = new ArrayHeap<Item>(ScoredComparator.DescendingComparator);
			chart = new HookChart();
			SetGoal(length);
			IList<Item> initialItems = MakeInitialItems(words);
			//    scoreDependencies();
			foreach (Item item in initialItems)
			{
				item = (Item)interner.Intern(item);
				//if (VERBOSE) log.info("Initial: "+item);
				DiscoverItem(item);
			}
		}

		/// <summary>Parse a Sentence.</summary>
		/// <returns>true iff it could be parsed</returns>
		public virtual bool Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			int nGoodRemaining = 0;
			if (op.testOptions.printFactoredKGood > 0)
			{
				nGoodRemaining = op.testOptions.printFactoredKGood;
				nGoodTrees.Clear();
			}
			int spanFound = 0;
			long last = 0;
			int exHook = 0;
			relaxHook1 = 0;
			relaxHook2 = 0;
			relaxHook3 = 0;
			relaxHook4 = 0;
			builtHooks = 0;
			builtEdges = 0;
			extractedHooks = 0;
			extractedEdges = 0;
			if (op.testOptions.verbose)
			{
				Timing.Tick("Starting combined parse.");
			}
			dparser.binDistance = dparser.binDistance;
			// THIS IS TERRIBLE, BUT SAVES MEMORY
			Initialize(words);
			while (!agenda.IsEmpty())
			{
				Item item = agenda.ExtractMin();
				if (!item.IsEdge())
				{
					exHook++;
					extractedHooks++;
				}
				else
				{
					extractedEdges++;
				}
				if (relaxHook1 > last + 1000000)
				{
					last = relaxHook1;
					if (op.testOptions.verbose)
					{
						log.Info("Proposed hooks:   " + relaxHook1);
						log.Info("Unfiltered hooks: " + relaxHook2);
						log.Info("Built hooks:      " + relaxHook3);
						log.Info("Waste hooks:      " + relaxHook4);
						log.Info("Extracted hooks:  " + exHook);
					}
				}
				if (item.end - item.start > spanFound)
				{
					spanFound = item.end - item.start;
					if (op.testOptions.verbose)
					{
						log.Info(spanFound + " ");
					}
				}
				//if (item.end < 5) log.info("Extracted: "+item+" iScore "+item.iScore+" oScore "+item.oScore+" score "+item.score());
				if (item.Equals(goal))
				{
					if (op.testOptions.verbose)
					{
						log.Info("Found goal!");
						log.Info("Comb iScore " + item.iScore);
						// was goal.iScore
						Timing.Tick("Done, parse found.");
						log.Info("Built items:      " + (builtEdges + builtHooks));
						log.Info("Built hooks:      " + builtHooks);
						log.Info("Built edges:      " + builtEdges);
						log.Info("Extracted items:  " + (extractedEdges + extractedHooks));
						log.Info("Extracted hooks:  " + extractedHooks);
						log.Info("Extracted edges:  " + extractedEdges);
					}
					//postMortem();
					if (op.testOptions.printFactoredKGood <= 0)
					{
						goal = (Edge)item;
						interner = null;
						agenda = null;
						return true;
					}
					else
					{
						// Store the parse
						goal = (Edge)item;
						nGoodTrees.Add(goal);
						nGoodRemaining--;
						if (nGoodRemaining > 0)
						{
						}
						else
						{
							interner = null;
							agenda = null;
							return true;
						}
					}
				}
				// Is the currently best item acceptable at all?
				if (item.Score() == double.NegativeInfinity)
				{
					// Do not report failure in nGood mode if we found something earlier.
					if (nGoodTrees.Count > 0)
					{
						goal = nGoodTrees[0];
						interner = null;
						agenda = null;
						return true;
					}
					log.Info("FactoredParser: no consistent parse [hit A*-blocked edges, aborting].");
					if (op.testOptions.verbose)
					{
						Timing.Tick("FactoredParser: no consistent parse [hit A*-blocked edges, aborting].");
					}
					return false;
				}
				// Keep the number of items from getting too large
				if (op.testOptions.MaxItems > 0 && (builtEdges + builtHooks) >= op.testOptions.MaxItems)
				{
					// Do not report failure in kGood mode if we found something earlier.
					if (nGoodTrees.Count > 0)
					{
						log.Info("DEBUG: aborting search because of reaching the MAX_ITEMS work limit [" + op.testOptions.MaxItems + " items]");
						goal = nGoodTrees[0];
						interner = null;
						agenda = null;
						return true;
					}
					log.Info("FactoredParser: exceeded MAX_ITEMS work limit [" + op.testOptions.MaxItems + " items]; aborting.");
					if (op.testOptions.verbose)
					{
						Timing.Tick("FactoredParser: exceeded MAX_ITEMS work limit [" + op.testOptions.MaxItems + " items]; aborting.");
					}
					return false;
				}
				if (Verbose && item.Score() != double.NegativeInfinity)
				{
					System.Console.Error.Printf("Removing from agenda: %s score i %.2f + o %.2f = %.2f\n", item, item.iScore, item.oScore, item.Score());
					if (item.backEdge != null)
					{
						log.Info("  Backtrace: " + item.backEdge.ToString() + " " + (item.IsEdge() ? (((Edge)item).backHook != null ? ((Edge)item).backHook.ToString() : string.Empty) : string.Empty));
					}
				}
				ProcessItem(item);
			}
			// end while agenda is not empty
			// If we are here, the agenda is empty.
			// Do not report failure if we found something earlier.
			if (nGoodTrees.Count > 0)
			{
				log.Info("DEBUG: aborting search because of empty agenda");
				goal = nGoodTrees[0];
				interner = null;
				agenda = null;
				return true;
			}
			log.Info("FactoredParser: emptied agenda, no parse found!");
			if (op.testOptions.verbose)
			{
				Timing.Tick("FactoredParser: emptied agenda, no parse found!");
			}
			return false;
		}

		protected internal virtual void PostMortem()
		{
			int numHooks = 0;
			int numEdges = 0;
			int numUnmatchedHooks = 0;
			int total = agenda.Size();
			int done = 0;
			while (!agenda.IsEmpty())
			{
				Item item = agenda.ExtractMin();
				done++;
				//if(done % (total/10) == 0)
				//        log.info("Scanning: "+100*done/total);
				if (item.IsEdge())
				{
					numEdges++;
				}
				else
				{
					numHooks++;
					ICollection edges = chart.GetEdges((Hook)item);
					if (edges.Count == 0)
					{
						numUnmatchedHooks++;
					}
				}
			}
			log.Info("--- Agenda Post-Mortem ---");
			log.Info("Edges:           " + numEdges);
			log.Info("Hooks:           " + numHooks);
			log.Info("Unmatched Hooks: " + numUnmatchedHooks);
		}

		protected internal virtual int Project(int state)
		{
			return projection.Project(state);
		}

		public BiLexPCFGParser(IScorer scorer, ExhaustivePCFGParser fscorer, ExhaustiveDependencyParser dparser, BinaryGrammar bg, UnaryGrammar ug, IDependencyGrammar dg, ILexicon lex, Options op, IIndex<string> stateIndex, IIndex<string> wordIndex, 
			IIndex<string> tagIndex)
			: this(scorer, fscorer, dparser, bg, ug, dg, lex, op, new NullGrammarProjection(bg, ug), stateIndex, wordIndex, tagIndex)
		{
		}

		internal BiLexPCFGParser(IScorer scorer, ExhaustivePCFGParser fscorer, ExhaustiveDependencyParser dparser, BinaryGrammar bg, UnaryGrammar ug, IDependencyGrammar dg, ILexicon lex, Options op, IGrammarProjection projection, IIndex<string> stateIndex
			, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			this.fscorer = fscorer;
			this.projection = projection;
			this.dparser = dparser;
			this.scorer = scorer;
			this.bg = bg;
			this.ug = ug;
			this.dg = dg;
			this.lex = lex;
			this.op = op;
			this.stateIndex = stateIndex;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			tempEdge = new Edge(op.testOptions.exhaustiveTest);
			tempHook = new Hook(op.testOptions.exhaustiveTest);
		}

		public class N5BiLexPCFGParser : BiLexPCFGParser
		{
			protected internal override void RelaxTempHook()
			{
				relaxHook1++;
				if (!scorer.OPossible(tempHook) || !scorer.IPossible(tempHook))
				{
					return;
				}
				relaxHook2++;
				Hook resultHook = tempHook;
				//Hook resultHook = (Hook)interner.intern(tempHook);
				if (resultHook == tempHook)
				{
					relaxHook3++;
					tempHook = new Hook(op.testOptions.exhaustiveTest);
					ProcessHook(resultHook);
					builtHooks++;
				}
			}

			internal N5BiLexPCFGParser(IScorer scorer, ExhaustivePCFGParser fscorer, ExhaustiveDependencyParser leach, BinaryGrammar bg, UnaryGrammar ug, IDependencyGrammar dg, ILexicon lex, Options op, IIndex<string> stateIndex, IIndex<string> wordIndex
				, IIndex<string> tagIndex)
				: base(scorer, fscorer, leach, bg, ug, dg, lex, op, new NullGrammarProjection(bg, ug), stateIndex, wordIndex, tagIndex)
			{
			}

			internal N5BiLexPCFGParser(IScorer scorer, ExhaustivePCFGParser fscorer, ExhaustiveDependencyParser leach, BinaryGrammar bg, UnaryGrammar ug, IDependencyGrammar dg, ILexicon lex, Options op, IGrammarProjection proj, IIndex<string> stateIndex
				, IIndex<string> wordIndex, IIndex<string> tagIndex)
				: base(scorer, fscorer, leach, bg, ug, dg, lex, op, proj, stateIndex, wordIndex, tagIndex)
			{
			}
		}
		// end class N5BiLexPCFGParser
	}
}
