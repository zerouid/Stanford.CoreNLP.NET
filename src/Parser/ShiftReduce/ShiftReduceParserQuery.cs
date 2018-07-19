using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	public class ShiftReduceParserQuery : IParserQuery
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Shiftreduce.ShiftReduceParserQuery));

		internal Debinarizer debinarizer = new Debinarizer(false);

		internal IList<IHasWord> originalSentence;

		private State initialState;

		private State finalState;

		internal Tree debinarized;

		internal bool success;

		internal bool unparsable;

		private IList<State> bestParses;

		internal readonly ShiftReduceParser parser;

		internal IList<ParserConstraint> constraints = null;

		public ShiftReduceParserQuery(ShiftReduceParser parser)
		{
			this.parser = parser;
		}

		public virtual bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			this.originalSentence = sentence;
			initialState = ShiftReduceParser.InitialStateFromTaggedSentence(sentence);
			return ParseInternal();
		}

		public virtual bool Parse(Tree tree)
		{
			this.originalSentence = tree.YieldHasWord();
			initialState = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
			return ParseInternal();
		}

		private static TregexPattern rearrangeFinalPunctuationTregex = TregexPattern.Compile("__ !> __ <- (__=top <- (__ <<- (/[.]|PU/=punc < /[.!?。！？]/ ?> (__=single <: =punc))))");

		private static TsurgeonPattern rearrangeFinalPunctuationTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[move punc >-1 top] [if exists single prune single]");

		// TODO: we are assuming that sentence final punctuation always has
		// either . or PU as the tag.
		private bool ParseInternal()
		{
			int maxBeamSize = Math.Max(parser.op.TestOptions().beamSize, 1);
			success = true;
			unparsable = false;
			PriorityQueue<State> beam = new PriorityQueue<State>(maxBeamSize + 1, ScoredComparator.AscendingComparator);
			beam.Add(initialState);
			// TODO: don't construct as many PriorityQueues
			while (beam.Count > 0)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting the parser
					throw new RuntimeInterruptedException();
				}
				// log.info("================================================");
				// log.info("Current beam:");
				// log.info(beam);
				PriorityQueue<State> oldBeam = beam;
				beam = new PriorityQueue<State>(maxBeamSize + 1, ScoredComparator.AscendingComparator);
				State bestState = null;
				foreach (State state in oldBeam)
				{
					if (Thread.Interrupted())
					{
						// Allow interrupting the parser
						throw new RuntimeInterruptedException();
					}
					ICollection<ScoredObject<int>> predictedTransitions = parser.model.FindHighestScoringTransitions(state, true, maxBeamSize, constraints);
					// log.info("Examining state: " + state);
					foreach (ScoredObject<int> predictedTransition in predictedTransitions)
					{
						ITransition transition = parser.model.transitionIndex.Get(predictedTransition.Object());
						State newState = transition.Apply(state, predictedTransition.Score());
						// log.info("  Transition: " + transition + " (" + predictedTransition.score() + ")");
						if (bestState == null || bestState.Score() < newState.Score())
						{
							bestState = newState;
						}
						beam.Add(newState);
						if (beam.Count > maxBeamSize)
						{
							beam.Poll();
						}
					}
				}
				if (beam.Count == 0)
				{
					// Oops, time for some fallback plan
					// This can happen with the set of constraints given by the original paper
					// For example, one particular French model had a situation where it would reach
					//   @Ssub @Ssub .
					// without a left(Ssub) transition, so finishing the parse was impossible.
					// This will probably result in a bad parse, but at least it
					// will result in some sort of parse.
					foreach (State state_1 in oldBeam)
					{
						ITransition transition = parser.model.FindEmergencyTransition(state_1, constraints);
						if (transition != null)
						{
							State newState = transition.Apply(state_1);
							if (bestState == null || bestState.Score() < newState.Score())
							{
								bestState = newState;
							}
							beam.Add(newState);
						}
					}
				}
				// bestState == null only happens when we have failed to make progress, so quit
				// If the bestState is finished, we are done
				if (bestState == null || bestState.IsFinished())
				{
					break;
				}
			}
			if (beam.Count == 0)
			{
				success = false;
				unparsable = true;
				debinarized = null;
				finalState = null;
				bestParses = Java.Util.Collections.EmptyList();
			}
			else
			{
				// TODO: filter out beam elements that aren't finished
				bestParses = Generics.NewArrayList(beam);
				bestParses.Sort(beam.Comparator());
				Java.Util.Collections.Reverse(bestParses);
				finalState = bestParses[0];
				debinarized = debinarizer.TransformTree(finalState.stack.Peek());
				debinarized = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(rearrangeFinalPunctuationTregex, rearrangeFinalPunctuationTsurgeon, debinarized);
			}
			return success;
		}

		/// <summary>TODO: if we add anything interesting to report, we should report it here</summary>
		public virtual bool ParseAndReport<_T0>(IList<_T0> sentence, PrintWriter pwErr)
			where _T0 : IHasWord
		{
			bool success = Parse(sentence);
			//log.info(getBestTransitionSequence());
			//log.info(getBestBinarizedParse());
			return success;
		}

		public virtual Tree GetBestBinarizedParse()
		{
			return finalState.stack.Peek();
		}

		public virtual IList<ITransition> GetBestTransitionSequence()
		{
			return finalState.transitions.AsList();
		}

		public virtual double GetPCFGScore()
		{
			return finalState.score;
		}

		public virtual Tree GetBestParse()
		{
			return debinarized;
		}

		public virtual IList<ScoredObject<Tree>> GetKBestParses(int k)
		{
			return this.GetKBestPCFGParses(k);
		}

		public virtual double GetBestScore()
		{
			return this.GetPCFGScore();
		}

		/// <summary>TODO: can we get away with not calling this PCFG?</summary>
		public virtual Tree GetBestPCFGParse()
		{
			return debinarized;
		}

		public virtual Tree GetBestDependencyParse(bool debinarize)
		{
			return null;
		}

		public virtual Tree GetBestFactoredParse()
		{
			return null;
		}

		/// <summary>TODO: if this is a beam, return all equal parses</summary>
		public virtual IList<ScoredObject<Tree>> GetBestPCFGParses()
		{
			ScoredObject<Tree> parse = new ScoredObject<Tree>(debinarized, finalState.score);
			return Java.Util.Collections.SingletonList(parse);
		}

		public virtual bool HasFactoredParse()
		{
			return false;
		}

		/// <summary>TODO: return more if this used a beam</summary>
		public virtual IList<ScoredObject<Tree>> GetKBestPCFGParses(int kbestPCFG)
		{
			ScoredObject<Tree> parse = new ScoredObject<Tree>(debinarized, finalState.score);
			return Java.Util.Collections.SingletonList(parse);
		}

		public virtual IList<ScoredObject<Tree>> GetKGoodFactoredParses(int kbest)
		{
			throw new NotSupportedException();
		}

		public virtual IKBestViterbiParser GetPCFGParser()
		{
			// TODO: find some way to treat this as a KBestViterbiParser?
			return null;
		}

		public virtual IKBestViterbiParser GetDependencyParser()
		{
			return null;
		}

		public virtual IKBestViterbiParser GetFactoredParser()
		{
			return null;
		}

		public virtual void SetConstraints(IList<ParserConstraint> constraints)
		{
			this.constraints = constraints;
		}

		public virtual bool SaidMemMessage()
		{
			return false;
		}

		public virtual bool ParseSucceeded()
		{
			return success;
		}

		/// <summary>TODO: skip sentences which are too long</summary>
		public virtual bool ParseSkipped()
		{
			return false;
		}

		public virtual bool ParseFallback()
		{
			return false;
		}

		/// <summary>TODO: add memory handling?</summary>
		public virtual bool ParseNoMemory()
		{
			return false;
		}

		public virtual bool ParseUnparsable()
		{
			return unparsable;
		}

		public virtual IList<IHasWord> OriginalSentence()
		{
			return originalSentence;
		}

		/// <summary>TODO: clearly this should be a default method in ParserQuery once Java 8 comes out</summary>
		public virtual void RestoreOriginalWords(Tree tree)
		{
			if (originalSentence == null || tree == null)
			{
				return;
			}
			IList<Tree> leaves = tree.GetLeaves();
			if (leaves.Count != originalSentence.Count)
			{
				throw new InvalidOperationException("originalWords and sentence of different sizes: " + originalSentence.Count + " vs. " + leaves.Count + "\n Orig: " + SentenceUtils.ListToString(originalSentence) + "\n Pars: " + SentenceUtils.ListToString(leaves
					));
			}
			// TODO: get rid of this cast
			IEnumerator<ILabel> wordsIterator = (IEnumerator<ILabel>)originalSentence.GetEnumerator();
			foreach (Tree leaf in leaves)
			{
				leaf.SetLabel(wordsIterator.Current);
			}
		}
	}
}
