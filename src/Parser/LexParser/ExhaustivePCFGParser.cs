// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002, 2003, 2004, 2005 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An exhaustive generalized CKY PCFG parser.</summary>
	/// <remarks>
	/// An exhaustive generalized CKY PCFG parser.
	/// Fairly carefully optimized to be fast.
	/// If reusing this object for multiple parses, remember to correctly
	/// set any options such as the constraints field.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning (I seem to maintain it....)</author>
	/// <author>Jenny Finkel (N-best and sampling code, former from Liang/Chiang)</author>
	public class ExhaustivePCFGParser : IScorer, IKBestViterbiParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ExhaustivePCFGParser));

		protected internal readonly string goalStr;

		protected internal readonly IIndex<string> stateIndex;

		protected internal readonly IIndex<string> wordIndex;

		protected internal readonly IIndex<string> tagIndex;

		protected internal readonly ITreeFactory tf;

		protected internal readonly BinaryGrammar bg;

		protected internal readonly UnaryGrammar ug;

		protected internal readonly ILexicon lex;

		protected internal readonly Options op;

		protected internal readonly ITreebankLanguagePack tlp;

		protected internal OutsideRuleFilter orf;

		protected internal float[][][] iScore;

		protected internal float[][][] oScore;

		protected internal float bestScore;

		protected internal int[][][] wordsInSpan;

		protected internal bool[][] oFilteredStart;

		protected internal bool[][] oFilteredEnd;

		protected internal bool[][] iPossibleByL;

		protected internal bool[][] iPossibleByR;

		protected internal bool[][] oPossibleByL;

		protected internal bool[][] oPossibleByR;

		protected internal int[] words;

		private int[] beginOffsets;

		private int[] endOffsets;

		private CoreLabel[] originalCoreLabels;

		private IHasTag[] originalTags;

		protected internal int length;

		protected internal bool[][] tags;

		protected internal int myMaxLength = -unchecked((int)(0xDEADBEEF));

		protected internal readonly int numStates;

		protected internal int arraySize = 0;

		/// <summary>
		/// When you want to force the parser to parse a particular
		/// subsequence into a particular state.
		/// </summary>
		/// <remarks>
		/// When you want to force the parser to parse a particular
		/// subsequence into a particular state.  Parses will only be made
		/// where there is a constituent over the given span which matches
		/// (as regular expression) the state Pattern given.  See the
		/// documentation of the ParserConstraint class for information on
		/// specifying a ParserConstraint.
		/// Implementation note: It would be cleaner to make this a
		/// Collections.emptyList, but that actually significantly slows down
		/// the processing in the case of empty lists.  Checking for null
		/// saves quite a bit of time.
		/// </remarks>
		protected internal IList<ParserConstraint> constraints = null;

		// public static long insideTime = 0;  // for profiling
		// public static long outsideTime = 0;
		// inside scores
		// start idx, end idx, state -> logProb (ragged; null for end <= start)
		// outside scores
		// start idx, end idx, state -> logProb
		// number of words in span with this state
		// [start][state]; only used by unused outsideRuleFilter
		// [end][state]; only used by unused outsideRuleFilter
		// [start][state]
		// [end][state]
		// [start][state]
		// [end][state]
		// words of sentence being parsed as word Numberer ints
		// one larger than true length of sentence; includes boundary symbol in count
		private CoreLabel GetCoreLabel(int labelIndex)
		{
			if (originalCoreLabels[labelIndex] != null)
			{
				CoreLabel terminalLabel = originalCoreLabels[labelIndex];
				if (terminalLabel.Value() == null && terminalLabel.Word() != null)
				{
					terminalLabel.SetValue(terminalLabel.Word());
				}
				return terminalLabel;
			}
			string wordStr = wordIndex.Get(words[labelIndex]);
			CoreLabel terminalLabel_1 = new CoreLabel();
			terminalLabel_1.SetValue(wordStr);
			terminalLabel_1.SetWord(wordStr);
			terminalLabel_1.SetBeginPosition(beginOffsets[labelIndex]);
			terminalLabel_1.SetEndPosition(endOffsets[labelIndex]);
			if (originalTags[labelIndex] != null)
			{
				terminalLabel_1.SetTag(originalTags[labelIndex].Tag());
			}
			return terminalLabel_1;
		}

		public virtual double OScore(Edge edge)
		{
			double oS = oScore[edge.start][edge.end][edge.state];
			return oS;
		}

		public virtual double IScore(Edge edge)
		{
			return iScore[edge.start][edge.end][edge.state];
		}

		public virtual bool OPossible(Hook hook)
		{
			return (hook.IsPreHook() ? oPossibleByR[hook.end][hook.state] : oPossibleByL[hook.start][hook.state]);
		}

		public virtual bool IPossible(Hook hook)
		{
			return (hook.IsPreHook() ? iPossibleByR[hook.start][hook.subState] : iPossibleByL[hook.end][hook.subState]);
		}

		public virtual bool OPossibleL(int state, int start)
		{
			return oPossibleByL[start][state];
		}

		public virtual bool OPossibleR(int state, int end)
		{
			return oPossibleByR[end][state];
		}

		public virtual bool IPossibleL(int state, int start)
		{
			return iPossibleByL[start][state];
		}

		public virtual bool IPossibleR(int state, int end)
		{
			return iPossibleByR[end][state];
		}

		protected internal virtual void BuildOFilter()
		{
			oFilteredStart = new bool[length][];
			oFilteredEnd = new bool[][] {  };
			orf.Init();
			for (int start = 0; start < length; start++)
			{
				orf.LeftAccepting(oFilteredStart[start]);
				orf.AdvanceRight(tags[start]);
			}
			for (int end = length; end > 0; end--)
			{
				orf.RightAccepting(oFilteredEnd[end]);
				orf.AdvanceLeft(tags[end - 1]);
			}
		}

		public virtual double ValidateBinarizedTree(Tree tree, int start)
		{
			if (tree.IsLeaf())
			{
				return 0.0;
			}
			float epsilon = 0.0001f;
			if (tree.IsPreTerminal())
			{
				string wordStr = tree.Children()[0].Label().Value();
				int tag = tagIndex.IndexOf(tree.Label().Value());
				int word = wordIndex.IndexOf(wordStr);
				IntTaggedWord iTW = new IntTaggedWord(word, tag);
				float score = lex.Score(iTW, start, wordStr, null);
				float bound = iScore[start][start + 1][stateIndex.IndexOf(tree.Label().Value())];
				if (score > bound + epsilon)
				{
					System.Console.Out.WriteLine("Invalid tagging:");
					System.Console.Out.WriteLine("  Tag: " + tree.Label().Value());
					System.Console.Out.WriteLine("  Word: " + tree.Children()[0].Label().Value());
					System.Console.Out.WriteLine("  Score: " + score);
					System.Console.Out.WriteLine("  Bound: " + bound);
				}
				return score;
			}
			int parent = stateIndex.IndexOf(tree.Label().Value());
			int firstChild = stateIndex.IndexOf(tree.Children()[0].Label().Value());
			if (tree.NumChildren() == 1)
			{
				UnaryRule ur = new UnaryRule(parent, firstChild);
				double score = SloppyMath.Max(ug.ScoreRule(ur), -10000.0) + ValidateBinarizedTree(tree.Children()[0], start);
				double bound = iScore[start][start + tree.Yield().Count][parent];
				if (score > bound + epsilon)
				{
					System.Console.Out.WriteLine("Invalid unary:");
					System.Console.Out.WriteLine("  Parent: " + tree.Label().Value());
					System.Console.Out.WriteLine("  Child: " + tree.Children()[0].Label().Value());
					System.Console.Out.WriteLine("  Start: " + start);
					System.Console.Out.WriteLine("  End: " + (start + tree.Yield().Count));
					System.Console.Out.WriteLine("  Score: " + score);
					System.Console.Out.WriteLine("  Bound: " + bound);
				}
				return score;
			}
			int secondChild = stateIndex.IndexOf(tree.Children()[1].Label().Value());
			BinaryRule br = new BinaryRule(parent, firstChild, secondChild);
			double score_1 = SloppyMath.Max(bg.ScoreRule(br), -10000.0) + ValidateBinarizedTree(tree.Children()[0], start) + ValidateBinarizedTree(tree.Children()[1], start + tree.Children()[0].Yield().Count);
			double bound_1 = iScore[start][start + tree.Yield().Count][parent];
			if (score_1 > bound_1 + epsilon)
			{
				System.Console.Out.WriteLine("Invalid binary:");
				System.Console.Out.WriteLine("  Parent: " + tree.Label().Value());
				System.Console.Out.WriteLine("  LChild: " + tree.Children()[0].Label().Value());
				System.Console.Out.WriteLine("  RChild: " + tree.Children()[1].Label().Value());
				System.Console.Out.WriteLine("  Start: " + start);
				System.Console.Out.WriteLine("  End: " + (start + tree.Yield().Count));
				System.Console.Out.WriteLine("  Score: " + score_1);
				System.Console.Out.WriteLine("  Bound: " + bound_1);
			}
			return score_1;
		}

		// needs to be set up so that uses same Train options...
		public virtual Tree ScoreNonBinarizedTree(Tree tree)
		{
			TreeAnnotatorAndBinarizer binarizer = new TreeAnnotatorAndBinarizer(op.tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			tree = binarizer.TransformTree(tree);
			ScoreBinarizedTree(tree, 0);
			return op.tlpParams.SubcategoryStripper().TransformTree(new Debinarizer(op.forceCNF).TransformTree(tree));
		}

		//    return debinarizer.transformTree(t);
		//
		public virtual double ScoreBinarizedTree(Tree tree, int start)
		{
			if (tree.IsLeaf())
			{
				return 0.0;
			}
			if (tree.IsPreTerminal())
			{
				string wordStr = tree.Children()[0].Label().Value();
				int tag = tagIndex.IndexOf(tree.Label().Value());
				int word = wordIndex.IndexOf(wordStr);
				IntTaggedWord iTW = new IntTaggedWord(word, tag);
				// if (lex.score(iTW,(leftmost ? 0 : 1)) == Double.NEGATIVE_INFINITY) {
				//   System.out.println("NO SCORE FOR: "+iTW);
				// }
				float score = lex.Score(iTW, start, wordStr, null);
				tree.SetScore(score);
				return score;
			}
			int parent = stateIndex.IndexOf(tree.Label().Value());
			int firstChild = stateIndex.IndexOf(tree.Children()[0].Label().Value());
			if (tree.NumChildren() == 1)
			{
				UnaryRule ur = new UnaryRule(parent, firstChild);
				//+ DEBUG
				// if (ug.scoreRule(ur) < -10000) {
				//        System.out.println("Grammar doesn't have rule: " + ur);
				// }
				//      return SloppyMath.max(ug.scoreRule(ur), -10000.0) + scoreBinarizedTree(tree.children()[0], leftmost);
				double score = ug.ScoreRule(ur) + ScoreBinarizedTree(tree.Children()[0], start);
				tree.SetScore(score);
				return score;
			}
			int secondChild = stateIndex.IndexOf(tree.Children()[1].Label().Value());
			BinaryRule br = new BinaryRule(parent, firstChild, secondChild);
			//+ DEBUG
			// if (bg.scoreRule(br) < -10000) {
			//  System.out.println("Grammar doesn't have rule: " + br);
			// }
			//    return SloppyMath.max(bg.scoreRule(br), -10000.0) +
			//            scoreBinarizedTree(tree.children()[0], leftmost) +
			//            scoreBinarizedTree(tree.children()[1], false);
			double score_1 = bg.ScoreRule(br) + ScoreBinarizedTree(tree.Children()[0], start) + ScoreBinarizedTree(tree.Children()[1], start + tree.Children()[0].Yield().Count);
			tree.SetScore(score_1);
			return score_1;
		}

		internal const bool spillGuts = false;

		internal const bool dumpTagging = false;

		private long time = Runtime.CurrentTimeMillis();

		protected internal virtual void Tick(string str)
		{
			long time2 = Runtime.CurrentTimeMillis();
			long diff = time2 - time;
			time = time2;
			log.Info("done.  " + diff + "\n" + str);
		}

		protected internal bool floodTags = false;

		protected internal IList sentence = null;

		protected internal Lattice lr = null;

		protected internal int[][] narrowLExtent;

		protected internal int[][] wideLExtent;

		protected internal int[][] narrowRExtent;

		protected internal int[][] wideRExtent;

		protected internal readonly bool[] isTag;

		// = null; // [end][state]: the rightmost left extent of state s ending at position i
		// = null; // [end][state] the leftmost left extent of state s ending at position i
		// = null; // [start][state]: the leftmost right extent of state s starting at position i
		// = null; // [start][state] the rightmost right extent of state s starting at position i
		// this records whether grammar states (stateIndex) correspond to POS tags
		public virtual bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			lr = null;
			// better nullPointer exception than silent error
			//System.out.println("is it a taggedword?" + (sentence.get(0) instanceof TaggedWord)); //debugging
			if (sentence != this.sentence)
			{
				this.sentence = sentence;
				floodTags = false;
			}
			if (op.testOptions.verbose)
			{
				Timing.Tick("Starting pcfg parse.");
			}
			length = sentence.Count;
			if (length > arraySize)
			{
				ConsiderCreatingArrays(length);
			}
			int goal = stateIndex.IndexOf(goalStr);
			if (op.testOptions.verbose)
			{
				// System.out.println(numStates + " states, " + goal + " is the goal state.");
				// log.info(new ArrayList(ug.coreRules.keySet()));
				log.Info("Initializing PCFG...");
			}
			// map input words to words array (wordIndex ints)
			words = new int[length];
			beginOffsets = new int[length];
			endOffsets = new int[length];
			originalCoreLabels = new CoreLabel[length];
			originalTags = new IHasTag[length];
			int unk = 0;
			StringBuilder unkWords = new StringBuilder("[");
			// int unkIndex = wordIndex.size();
			for (int i = 0; i < length; i++)
			{
				string s = sentence[i].Word();
				if (sentence[i] is IHasOffset)
				{
					IHasOffset word = (IHasOffset)sentence[i];
					beginOffsets[i] = word.BeginPosition();
					endOffsets[i] = word.EndPosition();
				}
				else
				{
					//Storing the positions of the word interstices
					//Account for single space between words
					beginOffsets[i] = ((i == 0) ? 0 : endOffsets[i - 1] + 1);
					endOffsets[i] = beginOffsets[i] + s.Length;
				}
				if (sentence[i] is CoreLabel)
				{
					originalCoreLabels[i] = (CoreLabel)sentence[i];
				}
				if (sentence[i] is IHasTag)
				{
					IHasTag tag = (IHasTag)sentence[i];
					if (tag.Tag() != null)
					{
						originalTags[i] = tag;
					}
				}
				if (op.testOptions.verbose && (!wordIndex.Contains(s) || !lex.IsKnown(wordIndex.IndexOf(s))))
				{
					unk++;
					unkWords.Append(' ');
					unkWords.Append(s);
					unkWords.Append(" { ");
					for (int jj = 0; jj < s.Length; jj++)
					{
						char ch = s[jj];
						unkWords.Append(char.GetType(ch)).Append(" ");
					}
					unkWords.Append("}");
				}
				// TODO: really, add a new word?
				//words[i] = wordIndex.indexOf(s, unkIndex);
				//if (words[i] == unkIndex) {
				//  ++unkIndex;
				//}
				words[i] = wordIndex.AddToIndex(s);
			}
			//if (wordIndex.contains(s)) {
			//  words[i] = wordIndex.indexOf(s);
			//} else {
			//  words[i] = wordIndex.indexOf(Lexicon.UNKNOWN_WORD);
			//}
			// initialize inside and outside score arrays
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			for (int start = 0; start < length; start++)
			{
				for (int end = start + 1; end <= length; end++)
				{
					Arrays.Fill(iScore[start][end], float.NegativeInfinity);
					if (op.doDep && !op.testOptions.useFastFactored)
					{
						Arrays.Fill(oScore[start][end], float.NegativeInfinity);
					}
					if (op.testOptions.lengthNormalization)
					{
						Arrays.Fill(wordsInSpan[start][end], 1);
					}
				}
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			for (int loc = 0; loc <= length; loc++)
			{
				Arrays.Fill(narrowLExtent[loc], -1);
				// the rightmost left with state s ending at i that we can get is the beginning
				Arrays.Fill(wideLExtent[loc], length + 1);
			}
			// the leftmost left with state s ending at i that we can get is the end
			for (int loc_1 = 0; loc_1 < length; loc_1++)
			{
				Arrays.Fill(narrowRExtent[loc_1], length + 1);
				// the leftmost right with state s starting at i that we can get is the end
				Arrays.Fill(wideRExtent[loc_1], -1);
			}
			// the rightmost right with state s starting at i that we can get is the beginning
			// int puncTag = stateIndex.indexOf(".");
			// boolean lastIsPunc = false;
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				unkWords.Append(" ]");
				op.tlpParams.Pw(System.Console.Error).Println("Unknown words: " + unk + " " + unkWords);
				log.Info("Starting filters...");
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			// do tags
			InitializeChart(sentence);
			//if (op.testOptions.outsideFilter)
			// buildOFilter();
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				log.Info("Starting insides...");
			}
			// do the inside probabilities
			DoInsideScores();
			if (op.testOptions.verbose)
			{
				// insideTime += Timing.tick("done.");
				Timing.Tick("done.");
				System.Console.Out.WriteLine("PCFG parsing " + length + " words (incl. stop): insideScore = " + iScore[0][length][goal]);
			}
			bestScore = iScore[0][length][goal];
			bool succeeded = HasParse();
			if (op.testOptions.doRecovery && !succeeded && !floodTags)
			{
				floodTags = true;
				// sentence will try to reparse
				// ms: disabled message. this is annoying and it doesn't really provide much information
				//log.info("Trying recovery parse...");
				return Parse(sentence);
			}
			if (!op.doDep || op.testOptions.useFastFactored)
			{
				return succeeded;
			}
			if (op.testOptions.verbose)
			{
				log.Info("Starting outsides...");
			}
			// outside scores
			oScore[0][length][goal] = 0.0f;
			DoOutsideScores();
			//System.out.println("State rate: "+((int)(1000*ohits/otries))/10.0);
			//System.out.println("Traversals: "+ohits);
			if (op.testOptions.verbose)
			{
				// outsideTime += Timing.tick("Done.");
				Timing.Tick("done.");
			}
			if (op.doDep)
			{
				InitializePossibles();
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			return succeeded;
		}

		public virtual bool Parse(HTKLatticeReader lr)
		{
			//TODO wsg 20-jan-2010
			// There are presently 2 issues with HTK lattice parsing:
			//   (1) The initializeChart() method present in rev. 19820 did not properly initialize
			//         lattices (or sub-lattices) like this (where A,B,C are nodes, and NN is the POS tag arc label):
			//
			//              --NN--> B --NN--
			//             /                \
			//            A ------NN-------> C
			//
			//   (2) extractBestParse() was not implemented properly.
			//
			//   To re-implement support for HTKLatticeReader it is necessary to create an interface
			//   for the two different lattice implementations and then modify initializeChart() and
			//   extractBestParse() as appropriate. Another solution would be to duplicate these two
			//   methods and make the necessary changes for HTKLatticeReader. In both cases, the
			//   acoustic model score provided by the HTK lattices should be included in the weighting.
			//
			//   Note that I never actually tested HTKLatticeReader, so I am uncertain if this facility
			//   actually worked in the first place.
			//
			System.Console.Error.Printf("%s: HTK lattice parsing presently disabled.\n", this.GetType().FullName);
			return false;
		}

		public virtual bool Parse(Lattice lr)
		{
			sentence = null;
			// better nullPointer exception than silent error
			if (lr != this.lr)
			{
				this.lr = lr;
				floodTags = false;
			}
			if (op.testOptions.verbose)
			{
				Timing.Tick("Doing lattice PCFG parse...");
			}
			// The number of whitespace nodes in the lattice
			length = lr.GetNumNodes() - 1;
			//Subtract 1 since considerCreatingArrays will add the final interstice
			if (length > arraySize)
			{
				ConsiderCreatingArrays(length);
			}
			int goal = stateIndex.IndexOf(goalStr);
			//    if (op.testOptions.verbose) {
			//      log.info("Unaries: " + ug.rules());
			//      log.info("Binaries: " + bg.rules());
			//      log.info("Initializing PCFG...");
			//      log.info("   " + numStates + " states, " + goal + " is the goal state.");
			//    }
			//    log.info("Tagging states");
			//    for(int i = 0; i < numStates; i++) {
			//      if(isTag[i]) {
			//        int tagId = Numberer.translate(stateSpace, "tags", i);
			//        String tag = (String) tagNumberer.object(tagId);
			//        System.err.printf(" %d: %s\n",i,tag);
			//      }
			//    }
			// Create a map of all words in the lattice
			//
			//    int numEdges = lr.getNumEdges();
			//    words = new int[numEdges];
			//    offsets = new IntPair[numEdges];
			//
			//    int unk = 0;
			//    int i = 0;
			//    StringBuilder unkWords = new StringBuilder("[");
			//    for (LatticeEdge edge : lr) {
			//      String s = edge.word;
			//      if (op.testOptions.verbose && !lex.isKnown(wordNumberer.number(s))) {
			//        unk++;
			//        unkWords.append(" " + s);
			//      }
			//      words[i++] = wordNumberer.number(s);
			//    }
			for (int start = 0; start < length; start++)
			{
				for (int end = start + 1; end <= length; end++)
				{
					Arrays.Fill(iScore[start][end], float.NegativeInfinity);
					if (op.doDep)
					{
						Arrays.Fill(oScore[start][end], float.NegativeInfinity);
					}
				}
			}
			for (int loc = 0; loc <= length; loc++)
			{
				Arrays.Fill(narrowLExtent[loc], -1);
				// the rightmost left with state s ending at i that we can get is the beginning
				Arrays.Fill(wideLExtent[loc], length + 1);
			}
			// the leftmost left with state s ending at i that we can get is the end
			for (int loc_1 = 0; loc_1 < length; loc_1++)
			{
				Arrays.Fill(narrowRExtent[loc_1], length + 1);
				// the leftmost right with state s starting at i that we can get is the end
				Arrays.Fill(wideRExtent[loc_1], -1);
			}
			// the rightmost right with state s starting at i that we can get is the beginning
			InitializeChart(lr);
			DoInsideScores();
			bestScore = iScore[0][length][goal];
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				log.Info("PCFG " + length + " words (incl. stop) iScore " + bestScore);
			}
			bool succeeded = HasParse();
			// Try a recovery parse
			if (!succeeded && op.testOptions.doRecovery && !floodTags)
			{
				floodTags = true;
				System.Console.Error.Printf(this.GetType().FullName + ": Parse failed. Trying recovery parse...");
				succeeded = Parse(lr);
				if (!succeeded)
				{
					return false;
				}
			}
			oScore[0][length][goal] = 0.0f;
			DoOutsideScores();
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
			}
			if (op.doDep)
			{
				InitializePossibles();
			}
			return succeeded;
		}

		/// <summary>These arrays are used by the factored parser (only) during edge combination.</summary>
		/// <remarks>
		/// These arrays are used by the factored parser (only) during edge combination.
		/// The method assumes that the iScore and oScore arrays have been initialized.
		/// </remarks>
		protected internal virtual void InitializePossibles()
		{
			for (int loc = 0; loc < length; loc++)
			{
				Arrays.Fill(iPossibleByL[loc], false);
				Arrays.Fill(oPossibleByL[loc], false);
			}
			for (int loc_1 = 0; loc_1 <= length; loc_1++)
			{
				Arrays.Fill(iPossibleByR[loc_1], false);
				Arrays.Fill(oPossibleByR[loc_1], false);
			}
			for (int start = 0; start < length; start++)
			{
				for (int end = start + 1; end <= length; end++)
				{
					for (int state = 0; state < numStates; state++)
					{
						if (iScore[start][end][state] > float.NegativeInfinity && oScore[start][end][state] > float.NegativeInfinity)
						{
							iPossibleByL[start][state] = true;
							iPossibleByR[end][state] = true;
							oPossibleByL[start][state] = true;
							oPossibleByR[end][state] = true;
						}
					}
				}
			}
		}

		private void DoOutsideScores()
		{
			for (int diff = length; diff >= 1; diff--)
			{
				if (Thread.Interrupted())
				{
					throw new RuntimeInterruptedException();
				}
				for (int start = 0; start + diff <= length; start++)
				{
					int end = start + diff;
					// do unaries
					for (int s = 0; s < numStates; s++)
					{
						float oS = oScore[start][end][s];
						if (oS == float.NegativeInfinity)
						{
							continue;
						}
						UnaryRule[] rules = ug.ClosedRulesByParent(s);
						foreach (UnaryRule ur in rules)
						{
							float pS = ur.score;
							float tot = oS + pS;
							if (tot > oScore[start][end][ur.child] && iScore[start][end][ur.child] > float.NegativeInfinity)
							{
								oScore[start][end][ur.child] = tot;
							}
						}
					}
					// do binaries
					for (int s_1 = 0; s_1 < numStates; s_1++)
					{
						int min1 = narrowRExtent[start][s_1];
						if (end < min1)
						{
							continue;
						}
						BinaryRule[] rules = bg.SplitRulesWithLC(s_1);
						foreach (BinaryRule br in rules)
						{
							float oS = oScore[start][end][br.parent];
							if (oS == float.NegativeInfinity)
							{
								continue;
							}
							int max1 = narrowLExtent[end][br.rightChild];
							if (max1 < min1)
							{
								continue;
							}
							int min = min1;
							int max = max1;
							if (max - min > 2)
							{
								int min2 = wideLExtent[end][br.rightChild];
								min = (min1 > min2 ? min1 : min2);
								if (max1 < min)
								{
									continue;
								}
								int max2 = wideRExtent[start][br.leftChild];
								max = (max1 < max2 ? max1 : max2);
								if (max < min)
								{
									continue;
								}
							}
							float pS = br.score;
							for (int split = min; split <= max; split++)
							{
								float lS = iScore[start][split][br.leftChild];
								if (lS == float.NegativeInfinity)
								{
									continue;
								}
								float rS = iScore[split][end][br.rightChild];
								if (rS == float.NegativeInfinity)
								{
									continue;
								}
								float totL = pS + rS + oS;
								if (totL > oScore[start][split][br.leftChild])
								{
									oScore[start][split][br.leftChild] = totL;
								}
								float totR = pS + lS + oS;
								if (totR > oScore[split][end][br.rightChild])
								{
									oScore[split][end][br.rightChild] = totR;
								}
							}
						}
					}
					for (int s_2 = 0; s_2 < numStates; s_2++)
					{
						int max1 = narrowLExtent[end][s_2];
						if (max1 < start)
						{
							continue;
						}
						BinaryRule[] rules = bg.SplitRulesWithRC(s_2);
						foreach (BinaryRule br in rules)
						{
							float oS = oScore[start][end][br.parent];
							if (oS == float.NegativeInfinity)
							{
								continue;
							}
							int min1 = narrowRExtent[start][br.leftChild];
							if (max1 < min1)
							{
								continue;
							}
							int min = min1;
							int max = max1;
							if (max - min > 2)
							{
								int min2 = wideLExtent[end][br.rightChild];
								min = (min1 > min2 ? min1 : min2);
								if (max1 < min)
								{
									continue;
								}
								int max2 = wideRExtent[start][br.leftChild];
								max = (max1 < max2 ? max1 : max2);
								if (max < min)
								{
									continue;
								}
							}
							float pS = br.score;
							for (int split = min; split <= max; split++)
							{
								float lS = iScore[start][split][br.leftChild];
								if (lS == float.NegativeInfinity)
								{
									continue;
								}
								float rS = iScore[split][end][br.rightChild];
								if (rS == float.NegativeInfinity)
								{
									continue;
								}
								float totL = pS + rS + oS;
								if (totL > oScore[start][split][br.leftChild])
								{
									oScore[start][split][br.leftChild] = totL;
								}
								float totR = pS + lS + oS;
								if (totR > oScore[split][end][br.rightChild])
								{
									oScore[split][end][br.rightChild] = totR;
								}
							}
						}
					}
				}
			}
		}

		/*
		for (int s = 0; s < numStates; s++) {
		float oS = oScore[start][end][s];
		//if (iScore[start][end][s] == Float.NEGATIVE_INFINITY ||
		//             oS == Float.NEGATIVE_INFINITY)
		if (oS == Float.NEGATIVE_INFINITY)
		continue;
		BinaryRule[] rules = bg.splitRulesWithParent(s);
		for (int r=0; r<rules.length; r++) {
		BinaryRule br = rules[r];
		int min1 = narrowRExtent[start][br.leftChild];
		if (end < min1)
		continue;
		int max1 = narrowLExtent[end][br.rightChild];
		if (max1 < min1)
		continue;
		int min2 = wideLExtent[end][br.rightChild];
		int min = (min1 > min2 ? min1 : min2);
		if (max1 < min)
		continue;
		int max2 = wideRExtent[start][br.leftChild];
		int max = (max1 < max2 ? max1 : max2);
		if (max < min)
		continue;
		float pS = (float) br.score;
		for (int split = min; split <= max; split++) {
		float lS = iScore[start][split][br.leftChild];
		if (lS == Float.NEGATIVE_INFINITY)
		continue;
		float rS = iScore[split][end][br.rightChild];
		if (rS == Float.NEGATIVE_INFINITY)
		continue;
		float totL = pS+rS+oS;
		if (totL > oScore[start][split][br.leftChild]) {
		oScore[start][split][br.leftChild] = totL;
		}
		float totR = pS+lS+oS;
		if (totR > oScore[split][end][br.rightChild]) {
		oScore[split][end][br.rightChild] = totR;
		}
		}
		}
		}
		*/
		/// <summary>
		/// Fills in the iScore array of each category over each span
		/// of length 2 or more.
		/// </summary>
		internal virtual void DoInsideScores()
		{
			for (int diff = 2; diff <= length; diff++)
			{
				if (Thread.Interrupted())
				{
					throw new RuntimeInterruptedException();
				}
				// usually stop one short because boundary symbol only combines
				// with whole sentence span. So for 3 word sentence + boundary = 4,
				// length == 4, and do [0,2], [1,3]; [0,3]; [0,4]
				for (int start = 0; start < ((diff == length) ? 1 : length - diff); start++)
				{
					DoInsideChartCell(diff, start);
				}
			}
		}

		// for start
		// for diff (i.e., span)
		// end doInsideScores()
		private void DoInsideChartCell(int diff, int start)
		{
			bool lengthNormalization = op.testOptions.lengthNormalization;
			int end = start + diff;
			IList<ParserConstraint> constraints = GetConstraints();
			if (constraints != null)
			{
				foreach (ParserConstraint c in constraints)
				{
					if ((start > c.start && start < c.end && end > c.end) || (end > c.start && end < c.end && start < c.start))
					{
						return;
					}
				}
			}
			// 2011-11-26 jdk1.6: caching/hoisting a bunch of variables gives you about 15% speed up!
			// caching this saves a bit of time in the inner loop, maybe 1.8%
			int[] narrowRExtent_start = narrowRExtent[start];
			// caching this saved 2% in the inner loop
			int[] wideRExtent_start = wideRExtent[start];
			int[] narrowLExtent_end = narrowLExtent[end];
			int[] wideLExtent_end = wideLExtent[end];
			float[][] iScore_start = iScore[start];
			float[] iScore_start_end = iScore_start[end];
			for (int leftState = 0; leftState < numStates; leftState++)
			{
				int narrowR = narrowRExtent_start[leftState];
				if (narrowR >= end)
				{
					// can this left constituent leave space for a right constituent?
					continue;
				}
				BinaryRule[] leftRules = bg.SplitRulesWithLC(leftState);
				//      if (spillGuts) System.out.println("Found " + leftRules.length + " left rules for state " + stateIndex.get(leftState));
				foreach (BinaryRule rule in leftRules)
				{
					int rightChild = rule.rightChild;
					int narrowL = narrowLExtent_end[rightChild];
					if (narrowL < narrowR)
					{
						// can this right constituent fit next to the left constituent?
						continue;
					}
					int min2 = wideLExtent_end[rightChild];
					int min = (narrowR > min2 ? narrowR : min2);
					// Erik Frey 2009-12-17: This is unnecessary: narrowR is <= narrowL (established in previous check) and wideLExtent[e][r] is always <= narrowLExtent[e][r] by design, so the check will never evaluate true.
					// if (min > narrowL) { // can this right constituent stretch far enough to reach the left constituent?
					//   continue;
					// }
					int max1 = wideRExtent_start[leftState];
					int max = (max1 < narrowL ? max1 : narrowL);
					if (min > max)
					{
						// can this left constituent stretch far enough to reach the right constituent?
						continue;
					}
					float pS = rule.score;
					int parentState = rule.parent;
					float oldIScore = iScore_start_end[parentState];
					float bestIScore = oldIScore;
					bool foundBetter;
					// always set below for this rule
					//System.out.println("Min "+min+" max "+max+" start "+start+" end "+end);
					if (!lengthNormalization)
					{
						// find the split that can use this rule to make the max score
						for (int split = min; split <= max; split++)
						{
							if (constraints != null)
							{
								bool skip = false;
								foreach (ParserConstraint c in constraints)
								{
									if (((start < c.start && end >= c.end) || (start <= c.start && end > c.end)) && split > c.start && split < c.end)
									{
										skip = true;
										break;
									}
									if ((start == c.start && split == c.end))
									{
										string tag = stateIndex.Get(leftState);
										Matcher m = c.state.Matcher(tag);
										if (!m.Matches())
										{
											skip = true;
											break;
										}
									}
									if ((split == c.start && end == c.end))
									{
										string tag = stateIndex.Get(rightChild);
										Matcher m = c.state.Matcher(tag);
										if (!m.Matches())
										{
											skip = true;
											break;
										}
									}
								}
								if (skip)
								{
									continue;
								}
							}
							float lS = iScore_start[split][leftState];
							if (lS == float.NegativeInfinity)
							{
								continue;
							}
							float rS = iScore[split][end][rightChild];
							if (rS == float.NegativeInfinity)
							{
								continue;
							}
							float tot = pS + lS + rS;
							if (tot > bestIScore)
							{
								bestIScore = tot;
							}
						}
						// for split point
						foundBetter = bestIScore > oldIScore;
					}
					else
					{
						// find split that uses this rule to make the max *length normalized* score
						int bestWordsInSpan = wordsInSpan[start][end][parentState];
						float oldNormIScore = oldIScore / bestWordsInSpan;
						float bestNormIScore = oldNormIScore;
						for (int split = min; split <= max; split++)
						{
							float lS = iScore_start[split][leftState];
							if (lS == float.NegativeInfinity)
							{
								continue;
							}
							float rS = iScore[split][end][rightChild];
							if (rS == float.NegativeInfinity)
							{
								continue;
							}
							float tot = pS + lS + rS;
							int newWordsInSpan = wordsInSpan[start][split][leftState] + wordsInSpan[split][end][rightChild];
							float normTot = tot / newWordsInSpan;
							if (normTot > bestNormIScore)
							{
								bestIScore = tot;
								bestNormIScore = normTot;
								bestWordsInSpan = newWordsInSpan;
							}
						}
						// for split point
						foundBetter = bestNormIScore > oldNormIScore;
						if (foundBetter)
						{
							wordsInSpan[start][end][parentState] = bestWordsInSpan;
						}
					}
					// fi op.testOptions.lengthNormalization
					if (foundBetter)
					{
						// this way of making "parentState" is better than previous
						iScore_start_end[parentState] = bestIScore;
						if (oldIScore == float.NegativeInfinity)
						{
							if (start > narrowLExtent_end[parentState])
							{
								narrowLExtent_end[parentState] = wideLExtent_end[parentState] = start;
							}
							else
							{
								if (start < wideLExtent_end[parentState])
								{
									wideLExtent_end[parentState] = start;
								}
							}
							if (end < narrowRExtent_start[parentState])
							{
								narrowRExtent_start[parentState] = wideRExtent_start[parentState] = end;
							}
							else
							{
								if (end > wideRExtent_start[parentState])
								{
									wideRExtent_start[parentState] = end;
								}
							}
						}
					}
				}
			}
			// end if foundBetter
			// end for leftRules
			// end for leftState
			// do right restricted rules
			for (int rightState = 0; rightState < numStates; rightState++)
			{
				int narrowL = narrowLExtent_end[rightState];
				if (narrowL <= start)
				{
					continue;
				}
				BinaryRule[] rightRules = bg.SplitRulesWithRC(rightState);
				//      if (spillGuts) System.out.println("Found " + rightRules.length + " right rules for state " + stateIndex.get(rightState));
				foreach (BinaryRule rule in rightRules)
				{
					//      if (spillGuts) System.out.println("Considering rule for " + start + " to " + end + ": " + rightRules[i]);
					int leftChild = rule.leftChild;
					int narrowR = narrowRExtent_start[leftChild];
					if (narrowR > narrowL)
					{
						continue;
					}
					int min2 = wideLExtent_end[rightState];
					int min = (narrowR > min2 ? narrowR : min2);
					// Erik Frey 2009-12-17: This is unnecessary: narrowR is <= narrowL (established in previous check) and wideLExtent[e][r] is always <= narrowLExtent[e][r] by design, so the check will never evaluate true.
					// if (min > narrowL) {
					//   continue;
					// }
					int max1 = wideRExtent_start[leftChild];
					int max = (max1 < narrowL ? max1 : narrowL);
					if (min > max)
					{
						continue;
					}
					float pS = rule.score;
					int parentState = rule.parent;
					float oldIScore = iScore_start_end[parentState];
					float bestIScore = oldIScore;
					bool foundBetter;
					// always initialized below
					//System.out.println("Start "+start+" end "+end+" min "+min+" max "+max);
					if (!lengthNormalization)
					{
						// find the split that can use this rule to make the max score
						for (int split = min; split <= max; split++)
						{
							if (constraints != null)
							{
								bool skip = false;
								foreach (ParserConstraint c in constraints)
								{
									if (((start < c.start && end >= c.end) || (start <= c.start && end > c.end)) && split > c.start && split < c.end)
									{
										skip = true;
										break;
									}
									if ((start == c.start && split == c.end))
									{
										string tag = stateIndex.Get(leftChild);
										Matcher m = c.state.Matcher(tag);
										if (!m.Matches())
										{
											//if (!tag.startsWith(c.state+"^")) {
											skip = true;
											break;
										}
									}
									if ((split == c.start && end == c.end))
									{
										string tag = stateIndex.Get(rightState);
										Matcher m = c.state.Matcher(tag);
										if (!m.Matches())
										{
											//if (!tag.startsWith(c.state+"^")) {
											skip = true;
											break;
										}
									}
								}
								if (skip)
								{
									continue;
								}
							}
							float lS = iScore_start[split][leftChild];
							// cdm [2012]: Test whether removing these 2 tests might speed things up because less branching?
							// jab [2014]: oddly enough, removing these tests helps the chinese parser but not the english parser.
							if (lS == float.NegativeInfinity)
							{
								continue;
							}
							float rS = iScore[split][end][rightState];
							if (rS == float.NegativeInfinity)
							{
								continue;
							}
							float tot = pS + lS + rS;
							if (tot > bestIScore)
							{
								bestIScore = tot;
							}
						}
						// end for split
						foundBetter = bestIScore > oldIScore;
					}
					else
					{
						// find split that uses this rule to make the max *length normalized* score
						int bestWordsInSpan = wordsInSpan[start][end][parentState];
						float oldNormIScore = oldIScore / bestWordsInSpan;
						float bestNormIScore = oldNormIScore;
						for (int split = min; split <= max; split++)
						{
							float lS = iScore_start[split][leftChild];
							if (lS == float.NegativeInfinity)
							{
								continue;
							}
							float rS = iScore[split][end][rightState];
							if (rS == float.NegativeInfinity)
							{
								continue;
							}
							float tot = pS + lS + rS;
							int newWordsInSpan = wordsInSpan[start][split][leftChild] + wordsInSpan[split][end][rightState];
							float normTot = tot / newWordsInSpan;
							if (normTot > bestNormIScore)
							{
								bestIScore = tot;
								bestNormIScore = normTot;
								bestWordsInSpan = newWordsInSpan;
							}
						}
						// end for split
						foundBetter = bestNormIScore > oldNormIScore;
						if (foundBetter)
						{
							wordsInSpan[start][end][parentState] = bestWordsInSpan;
						}
					}
					// end if lengthNormalization
					if (foundBetter)
					{
						// this way of making "parentState" is better than previous
						iScore_start_end[parentState] = bestIScore;
						if (oldIScore == float.NegativeInfinity)
						{
							if (start > narrowLExtent_end[parentState])
							{
								narrowLExtent_end[parentState] = wideLExtent_end[parentState] = start;
							}
							else
							{
								if (start < wideLExtent_end[parentState])
								{
									wideLExtent_end[parentState] = start;
								}
							}
							if (end < narrowRExtent_start[parentState])
							{
								narrowRExtent_start[parentState] = wideRExtent_start[parentState] = end;
							}
							else
							{
								if (end > wideRExtent_start[parentState])
								{
									wideRExtent_start[parentState] = end;
								}
							}
						}
					}
				}
			}
			// end if foundBetter
			// for rightRules
			// for rightState
			// do unary rules -- one could promote this loop and put start inside
			for (int state = 0; state < numStates; state++)
			{
				float iS = iScore_start_end[state];
				if (iS == float.NegativeInfinity)
				{
					continue;
				}
				UnaryRule[] unaries = ug.ClosedRulesByChild(state);
				foreach (UnaryRule ur in unaries)
				{
					if (constraints != null)
					{
						bool skip = false;
						foreach (ParserConstraint c in constraints)
						{
							if ((start == c.start && end == c.end))
							{
								string tag = stateIndex.Get(ur.parent);
								Matcher m = c.state.Matcher(tag);
								if (!m.Matches())
								{
									//if (!tag.startsWith(c.state+"^")) {
									skip = true;
									break;
								}
							}
						}
						if (skip)
						{
							continue;
						}
					}
					int parentState = ur.parent;
					float pS = ur.score;
					float tot = iS + pS;
					float cur = iScore_start_end[parentState];
					bool foundBetter;
					// always set below
					if (lengthNormalization)
					{
						int totWordsInSpan = wordsInSpan[start][end][state];
						float normTot = tot / totWordsInSpan;
						int curWordsInSpan = wordsInSpan[start][end][parentState];
						float normCur = cur / curWordsInSpan;
						foundBetter = normTot > normCur;
						if (foundBetter)
						{
							wordsInSpan[start][end][parentState] = wordsInSpan[start][end][state];
						}
					}
					else
					{
						foundBetter = (tot > cur);
					}
					if (foundBetter)
					{
						iScore_start_end[parentState] = tot;
						if (cur == float.NegativeInfinity)
						{
							if (start > narrowLExtent_end[parentState])
							{
								narrowLExtent_end[parentState] = wideLExtent_end[parentState] = start;
							}
							else
							{
								if (start < wideLExtent_end[parentState])
								{
									wideLExtent_end[parentState] = start;
								}
							}
							if (end < narrowRExtent_start[parentState])
							{
								narrowRExtent_start[parentState] = wideRExtent_start[parentState] = end;
							}
							else
							{
								if (end > wideRExtent_start[parentState])
								{
									wideRExtent_start[parentState] = end;
								}
							}
						}
					}
				}
			}
		}

		// end if foundBetter
		// for UnaryRule r
		// for unary rules
		private void InitializeChart(Lattice lr)
		{
			foreach (LatticeEdge edge in lr)
			{
				int start = edge.start;
				int end = edge.end;
				string word = edge.word;
				// Add pre-terminals, augmented with edge weights
				for (int state = 0; state < numStates; state++)
				{
					if (isTag[state])
					{
						IntTaggedWord itw = new IntTaggedWord(word, stateIndex.Get(state), wordIndex, tagIndex);
						float newScore = lex.Score(itw, start, word, null) + (float)edge.weight;
						if (newScore > iScore[start][end][state])
						{
							iScore[start][end][state] = newScore;
							narrowRExtent[start][state] = System.Math.Min(end, narrowRExtent[start][state]);
							narrowLExtent[end][state] = System.Math.Max(start, narrowLExtent[end][state]);
							wideRExtent[start][state] = System.Math.Max(end, wideRExtent[start][state]);
							wideLExtent[end][state] = System.Math.Min(start, wideLExtent[end][state]);
						}
					}
				}
				// Give scores to all tags if the parse fails (more flexible tagging)
				if (floodTags && (!op.testOptions.noRecoveryTagging))
				{
					for (int state_1 = 0; state_1 < numStates; state_1++)
					{
						float iS = iScore[start][end][state_1];
						if (isTag[state_1] && iS == float.NegativeInfinity)
						{
							iScore[start][end][state_1] = -1000.0f + (float)edge.weight;
							narrowRExtent[start][state_1] = end;
							narrowLExtent[end][state_1] = start;
							wideRExtent[start][state_1] = end;
							wideLExtent[end][state_1] = start;
						}
					}
				}
				// Add unary rules (possibly chains) that terminate in POS tags
				for (int state_2 = 0; state_2 < numStates; state_2++)
				{
					float iS = iScore[start][end][state_2];
					if (iS == float.NegativeInfinity)
					{
						continue;
					}
					UnaryRule[] unaries = ug.ClosedRulesByChild(state_2);
					foreach (UnaryRule ur in unaries)
					{
						int parentState = ur.parent;
						float pS = ur.score;
						float tot = iS + pS;
						if (tot > iScore[start][end][parentState])
						{
							iScore[start][end][parentState] = tot;
							narrowRExtent[start][parentState] = System.Math.Min(end, narrowRExtent[start][parentState]);
							narrowLExtent[end][parentState] = System.Math.Max(start, narrowLExtent[end][parentState]);
							wideRExtent[start][parentState] = System.Math.Max(end, wideRExtent[start][parentState]);
							wideLExtent[end][parentState] = System.Math.Min(start, wideLExtent[end][parentState]);
						}
					}
				}
			}
		}

		//            narrowRExtent[start][parentState] = start + 1; //end
		//            narrowLExtent[end][parentState] = end - 1; //start
		//            wideRExtent[start][parentState] = start + 1; //end
		//            wideLExtent[end][parentState] = end - 1; //start
		private void InitializeChart<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			int boundary = wordIndex.IndexOf(LexiconConstants.Boundary);
			for (int start = 0; start < length; start++)
			{
				if (op.testOptions.maxSpanForTags > 1)
				{
					// only relevant for parsing single words as multiple input tokens.
					// todo [cdm 2012]: This case seems buggy in never doing unaries over span 1 items
					// note we don't look for "words" including the end symbol!
					for (int end = start + 1; (end < length - 1 && end - start <= op.testOptions.maxSpanForTags) || (start + 1 == end); end++)
					{
						StringBuilder word = new StringBuilder();
						//wsg: Feb 2010 - Appears to support character-level parsing
						for (int i = start; i < end; i++)
						{
							if (sentence[i] is IHasWord)
							{
								IHasWord cl = sentence[i];
								word.Append(cl.Word());
							}
							else
							{
								word.Append(sentence[i].ToString());
							}
						}
						for (int state = 0; state < numStates; state++)
						{
							float iS = iScore[start][end][state];
							if (iS == float.NegativeInfinity && isTag[state])
							{
								IntTaggedWord itw = new IntTaggedWord(word.ToString(), stateIndex.Get(state), wordIndex, tagIndex);
								iScore[start][end][state] = lex.Score(itw, start, word.ToString(), null);
								if (iScore[start][end][state] > float.NegativeInfinity)
								{
									narrowRExtent[start][state] = start + 1;
									narrowLExtent[end][state] = end - 1;
									wideRExtent[start][state] = start + 1;
									wideLExtent[end][state] = end - 1;
								}
							}
						}
					}
				}
				else
				{
					// "normal" chart initialization of the [start,start+1] cell
					int word = words[start];
					int end = start + 1;
					Arrays.Fill(tags[start], false);
					float[] iScore_start_end = iScore[start][end];
					int[] narrowRExtent_start = narrowRExtent[start];
					int[] narrowLExtent_end = narrowLExtent[end];
					int[] wideRExtent_start = wideRExtent[start];
					int[] wideLExtent_end = wideLExtent[end];
					//Force tags
					string trueTagStr = null;
					if (sentence[start] is IHasTag)
					{
						trueTagStr = ((IHasTag)sentence[start]).Tag();
						if (string.Empty.Equals(trueTagStr))
						{
							trueTagStr = null;
						}
					}
					// Another option for forcing tags: supply a regex
					string candidateTagRegex = null;
					if (sentence[start] is CoreLabel)
					{
						candidateTagRegex = ((CoreLabel)sentence[start]).Get(typeof(ParserAnnotations.CandidatePartOfSpeechAnnotation));
						if (string.Empty.Equals(candidateTagRegex))
						{
							candidateTagRegex = null;
						}
					}
					//Word context (e.g., morphosyntactic info)
					string wordContextStr = null;
					if (sentence[start] is IHasContext)
					{
						wordContextStr = ((IHasContext)sentence[start]).OriginalText();
						if (string.Empty.Equals(wordContextStr))
						{
							wordContextStr = null;
						}
					}
					bool assignedSomeTag = false;
					if (!floodTags || word == boundary)
					{
						// in this case we generate the taggings in the lexicon,
						// which may itself be tagging flexibly or using a strict lexicon.
						for (IEnumerator<IntTaggedWord> taggingI = lex.RuleIteratorByWord(word, start, wordContextStr); taggingI.MoveNext(); )
						{
							IntTaggedWord tagging = taggingI.Current;
							int state = stateIndex.IndexOf(tagIndex.Get(tagging.tag));
							// if word was supplied with a POS tag, skip all taggings
							// not basicCategory() compatible with supplied tag.
							if (trueTagStr != null)
							{
								if ((!op.testOptions.forceTagBeginnings && !tlp.BasicCategory(tagging.TagString(tagIndex)).Equals(trueTagStr)) || (op.testOptions.forceTagBeginnings && !tagging.TagString(tagIndex).StartsWith(trueTagStr)))
								{
									continue;
								}
							}
							if (candidateTagRegex != null)
							{
								if ((!op.testOptions.forceTagBeginnings && !tlp.BasicCategory(tagging.TagString(tagIndex)).Matches(candidateTagRegex)) || (op.testOptions.forceTagBeginnings && !tagging.TagString(tagIndex).Matches(candidateTagRegex)))
								{
									continue;
								}
							}
							// try {
							float lexScore = lex.Score(tagging, start, wordIndex.Get(tagging.word), wordContextStr);
							// score the cell according to P(word|tag) in the lexicon
							if (lexScore > float.NegativeInfinity)
							{
								assignedSomeTag = true;
								iScore_start_end[state] = lexScore;
								narrowRExtent_start[state] = end;
								narrowLExtent_end[state] = start;
								wideRExtent_start[state] = end;
								wideLExtent_end[state] = start;
							}
							// } catch (Exception e) {
							// e.printStackTrace();
							// System.out.println("State: " + state + " tags " + Numberer.getGlobalNumberer("tags").object(tagging.tag));
							// }
							int tag = tagging.tag;
							tags[start][tag] = true;
						}
					}
					//if (start == length-2 && tagging.parent == puncTag)
					//  lastIsPunc = true;
					// end if ( ! floodTags || word == boundary)
					if (!assignedSomeTag)
					{
						// If you got here, either you were using forceTags (gold tags)
						// and the gold tag was not seen with that word in the training data
						// or we are in floodTags=true (recovery parse) mode
						// Here, we give words all tags for
						// which the lexicon score is not -Inf, not just seen or
						// specified taggings
						for (int state = 0; state < numStates; state++)
						{
							if (isTag[state] && iScore_start_end[state] == float.NegativeInfinity)
							{
								if (trueTagStr != null)
								{
									string tagString = stateIndex.Get(state);
									if (!tlp.BasicCategory(tagString).Equals(trueTagStr))
									{
										continue;
									}
								}
								float lexScore = lex.Score(new IntTaggedWord(word, tagIndex.IndexOf(stateIndex.Get(state))), start, wordIndex.Get(word), wordContextStr);
								if (candidateTagRegex != null)
								{
									string tagString = stateIndex.Get(state);
									if (!tlp.BasicCategory(tagString).Matches(candidateTagRegex))
									{
										continue;
									}
								}
								if (lexScore > float.NegativeInfinity)
								{
									iScore_start_end[state] = lexScore;
									narrowRExtent_start[state] = end;
									narrowLExtent_end[state] = start;
									wideRExtent_start[state] = end;
									wideLExtent_end[state] = start;
								}
							}
						}
					}
					// end if ! assignedSomeTag
					// tag multi-counting
					if (op.dcTags)
					{
						for (int state = 0; state < numStates; state++)
						{
							if (isTag[state])
							{
								iScore_start_end[state] *= (1.0 + op.testOptions.depWeight);
							}
						}
					}
					if (floodTags && (!op.testOptions.noRecoveryTagging) && !(word == boundary))
					{
						// if parse failed because of tag coverage, we put in all tags with
						// a score of -1000, by fiat.  You get here from the invocation of
						// parse(ls) inside parse(ls) *after* floodTags has been turned on.
						// Search above for "floodTags = true".
						for (int state = 0; state < numStates; state++)
						{
							if (isTag[state] && iScore_start_end[state] == float.NegativeInfinity)
							{
								iScore_start_end[state] = -1000.0f;
								narrowRExtent_start[state] = end;
								narrowLExtent_end[state] = start;
								wideRExtent_start[state] = end;
								wideLExtent_end[state] = start;
							}
						}
					}
					// Apply unary rules in diagonal cells of chart
					for (int state_1 = 0; state_1 < numStates; state_1++)
					{
						float iS = iScore_start_end[state_1];
						if (iS == float.NegativeInfinity)
						{
							continue;
						}
						UnaryRule[] unaries = ug.ClosedRulesByChild(state_1);
						foreach (UnaryRule ur in unaries)
						{
							int parentState = ur.parent;
							float pS = ur.score;
							float tot = iS + pS;
							if (tot > iScore_start_end[parentState])
							{
								iScore_start_end[parentState] = tot;
								narrowRExtent_start[parentState] = end;
								narrowLExtent_end[parentState] = start;
								wideRExtent_start[parentState] = end;
								wideLExtent_end[parentState] = start;
							}
						}
					}
				}
			}
		}

		// end for start
		// end initializeChart(List sentence)
		public virtual bool HasParse()
		{
			return GetBestScore() > double.NegativeInfinity;
		}

		private const double Tol = 1e-5;

		protected internal static bool Matches(double x, double y)
		{
			return (System.Math.Abs(x - y) / (System.Math.Abs(x) + System.Math.Abs(y) + 1e-10) < Tol);
		}

		public virtual double GetBestScore()
		{
			return GetBestScore(goalStr);
		}

		public virtual double GetBestScore(string stateName)
		{
			if (length > arraySize)
			{
				return double.NegativeInfinity;
			}
			if (!stateIndex.Contains(stateName))
			{
				return double.NegativeInfinity;
			}
			int goal = stateIndex.IndexOf(stateName);
			if (iScore == null || iScore.Length == 0 || iScore[0].Length <= length || iScore[0][length].Length <= goal)
			{
				return double.NegativeInfinity;
			}
			return iScore[0][length][goal];
		}

		public virtual Tree GetBestParse()
		{
			Tree internalTree = ExtractBestParse(goalStr, 0, length);
			//System.out.println("Got internal best parse...");
			if (internalTree == null)
			{
				log.Info("Warning: no parse found in ExhaustivePCFGParser.extractBestParse");
			}
			// else {
			// restoreUnaries(internalTree);
			// }
			// System.out.println("Restored unaries...");
			return internalTree;
		}

		//TreeTransformer debinarizer = BinarizerFactory.getDebinarizer();
		//return debinarizer.transformTree(internalTree);
		/// <summary>Return the best parse of some category/state over a certain span.</summary>
		protected internal virtual Tree ExtractBestParse(string goalStr, int start, int end)
		{
			return ExtractBestParse(stateIndex.IndexOf(goalStr), start, end);
		}

		private Tree ExtractBestParse(int goal, int start, int end)
		{
			// find source of inside score
			// no backtraces so we can speed up the parsing for its primary use
			double bestScore = iScore[start][end][goal];
			double normBestScore = op.testOptions.lengthNormalization ? (bestScore / wordsInSpan[start][end][goal]) : bestScore;
			string goalStr = stateIndex.Get(goal);
			// check tags
			if (end - start <= op.testOptions.maxSpanForTags && tagIndex.Contains(goalStr))
			{
				if (op.testOptions.maxSpanForTags > 1)
				{
					Tree wordNode = null;
					if (sentence != null)
					{
						StringBuilder word = new StringBuilder();
						for (int i = start; i < end; i++)
						{
							if (sentence[i] is IHasWord)
							{
								IHasWord cl = (IHasWord)sentence[i];
								word.Append(cl.Word());
							}
							else
							{
								word.Append(sentence[i].ToString());
							}
						}
						wordNode = tf.NewLeaf(word.ToString());
					}
					else
					{
						if (lr != null)
						{
							IList<LatticeEdge> latticeEdges = lr.GetEdgesOverSpan(start, end);
							foreach (LatticeEdge edge in latticeEdges)
							{
								IntTaggedWord itw = new IntTaggedWord(edge.word, stateIndex.Get(goal), wordIndex, tagIndex);
								float tagScore = (floodTags) ? -1000.0f : lex.Score(itw, start, edge.word, null);
								if (Matches(bestScore, tagScore + (float)edge.weight))
								{
									wordNode = tf.NewLeaf(edge.word);
									if (wordNode.Label() is CoreLabel)
									{
										CoreLabel cl = (CoreLabel)wordNode.Label();
										cl.SetBeginPosition(start);
										cl.SetEndPosition(end);
									}
									break;
								}
							}
							if (wordNode == null)
							{
								throw new Exception("could not find matching word from lattice in parse reconstruction");
							}
						}
						else
						{
							throw new Exception("attempt to get word when sentence and lattice are null!");
						}
					}
					Tree tagNode = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(wordNode));
					tagNode.SetScore(bestScore);
					if (originalTags[start] != null)
					{
						tagNode.Label().SetValue(originalTags[start].Tag());
					}
					return tagNode;
				}
				else
				{
					// normal lexicon is single words case
					IntTaggedWord tagging = new IntTaggedWord(words[start], tagIndex.IndexOf(goalStr));
					string contextStr = GetCoreLabel(start).OriginalText();
					float tagScore = lex.Score(tagging, start, wordIndex.Get(words[start]), contextStr);
					if (tagScore > float.NegativeInfinity || floodTags)
					{
						// return a pre-terminal tree
						CoreLabel terminalLabel = GetCoreLabel(start);
						Tree wordNode = tf.NewLeaf(terminalLabel);
						Tree tagNode = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(wordNode));
						tagNode.SetScore(bestScore);
						if (terminalLabel.Tag() != null)
						{
							tagNode.Label().SetValue(terminalLabel.Tag());
						}
						if (tagNode.Label() is IHasTag)
						{
							((IHasTag)tagNode.Label()).SetTag(tagNode.Label().Value());
						}
						return tagNode;
					}
				}
			}
			// check binaries first
			for (int split = start + 1; split < end; split++)
			{
				for (IEnumerator<BinaryRule> binaryI = bg.RuleIteratorByParent(goal); binaryI.MoveNext(); )
				{
					BinaryRule br = binaryI.Current;
					double score = br.score + iScore[start][split][br.leftChild] + iScore[split][end][br.rightChild];
					bool matches;
					if (op.testOptions.lengthNormalization)
					{
						double normScore = score / (wordsInSpan[start][split][br.leftChild] + wordsInSpan[split][end][br.rightChild]);
						matches = Matches(normScore, normBestScore);
					}
					else
					{
						matches = Matches(score, bestScore);
					}
					if (matches)
					{
						// build binary split
						Tree leftChildTree = ExtractBestParse(br.leftChild, start, split);
						Tree rightChildTree = ExtractBestParse(br.rightChild, split, end);
						IList<Tree> children = new List<Tree>();
						children.Add(leftChildTree);
						children.Add(rightChildTree);
						Tree result = tf.NewTreeNode(goalStr, children);
						result.SetScore(score);
						// log.info("    Found Binary node: "+result);
						return result;
					}
				}
			}
			// check unaries
			// note that even though we parse with the unary-closed grammar, we can
			// extract the best parse with the non-unary-closed grammar, since all
			// the intermediate states in the chain must have been built, and hence
			// we can exploit the sparser space and reconstruct the full tree as we go.
			// for (Iterator<UnaryRule> unaryI = ug.closedRuleIteratorByParent(goal); unaryI.hasNext(); ) {
			for (IEnumerator<UnaryRule> unaryI = ug.RuleIteratorByParent(goal); unaryI.MoveNext(); )
			{
				UnaryRule ur = unaryI.Current;
				// log.info("  Trying " + ur + " dtr score: " + iScore[start][end][ur.child]);
				double score = ur.score + iScore[start][end][ur.child];
				bool matches;
				if (op.testOptions.lengthNormalization)
				{
					double normScore = score / wordsInSpan[start][end][ur.child];
					matches = Matches(normScore, normBestScore);
				}
				else
				{
					matches = Matches(score, bestScore);
				}
				if (ur.child != ur.parent && matches)
				{
					// build unary
					Tree childTree = ExtractBestParse(ur.child, start, end);
					Tree result = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(childTree));
					// log.info("    Matched!  Unary node: "+result);
					result.SetScore(score);
					return result;
				}
			}
			log.Info("Warning: no parse found in ExhaustivePCFGParser.extractBestParse: failing on: [" + start + ", " + end + "] looking for " + goalStr);
			return null;
		}

		/* -----------------------
		// No longer needed: extracBestParse restores unaries as it goes
		protected void restoreUnaries(Tree t) {
		//System.out.println("In restoreUnaries...");
		for (Tree node : t) {
		log.info("Doing node: "+node.label());
		if (node.isLeaf() || node.isPreTerminal() || node.numChildren() != 1) {
		//System.out.println("Skipping node: "+node.label());
		continue;
		}
		//System.out.println("Not skipping node: "+node.label());
		Tree parent = node;
		Tree child = node.children()[0];
		List path = ug.getBestPath(stateIndex.indexOf(parent.label().value()), stateIndex.indexOf(child.label().value()));
		log.info("Got path: "+path);
		int pos = 1;
		while (pos < path.size() - 1) {
		int interState = ((Integer) path.get(pos)).intValue();
		Tree intermediate = tf.newTreeNode(new StringLabel(stateIndex.get(interState)), parent.getChildrenAsList());
		parent.setChildren(Collections.singletonList(intermediate));
		pos++;
		}
		//System.out.println("Done with node: "+node.label());
		}
		}
		---------------------- */
		/// <summary>Return all best parses (except no ties allowed on POS tags?).</summary>
		/// <remarks>
		/// Return all best parses (except no ties allowed on POS tags?).
		/// Even though we parse with the unary-closed grammar, since all the
		/// intermediate states in a chain must have been built, we can
		/// reconstruct the unary chain as we go using the non-unary-closed grammar.
		/// </remarks>
		protected internal virtual IList<Tree> ExtractBestParses(int goal, int start, int end)
		{
			// find sources of inside score
			// no backtraces so we can speed up the parsing for its primary use
			double bestScore = iScore[start][end][goal];
			string goalStr = stateIndex.Get(goal);
			//System.out.println("Searching for "+goalStr+" from "+start+" to "+end+" scored "+bestScore);
			// check tags
			if (end - start == 1 && tagIndex.Contains(goalStr))
			{
				IntTaggedWord tagging = new IntTaggedWord(words[start], tagIndex.IndexOf(goalStr));
				string contextStr = GetCoreLabel(start).OriginalText();
				float tagScore = lex.Score(tagging, start, wordIndex.Get(words[start]), contextStr);
				if (tagScore > float.NegativeInfinity || floodTags)
				{
					// return a pre-terminal tree
					string wordStr = wordIndex.Get(words[start]);
					Tree wordNode = tf.NewLeaf(wordStr);
					Tree tagNode = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(wordNode));
					if (originalTags[start] != null)
					{
						tagNode.Label().SetValue(originalTags[start].Tag());
					}
					//System.out.println("Tag node: "+tagNode);
					return Java.Util.Collections.SingletonList(tagNode);
				}
			}
			// check binaries first
			IList<Tree> bestTrees = new List<Tree>();
			for (int split = start + 1; split < end; split++)
			{
				for (IEnumerator<BinaryRule> binaryI = bg.RuleIteratorByParent(goal); binaryI.MoveNext(); )
				{
					BinaryRule br = binaryI.Current;
					double score = br.score + iScore[start][split][br.leftChild] + iScore[split][end][br.rightChild];
					if (Matches(score, bestScore))
					{
						// build binary split
						IList<Tree> leftChildTrees = ExtractBestParses(br.leftChild, start, split);
						IList<Tree> rightChildTrees = ExtractBestParses(br.rightChild, split, end);
						// System.out.println("Found a best way to build " + goalStr + "(" +
						//                 start + "," + end + ") with " +
						//                 leftChildTrees.size() + "x" +
						//                 rightChildTrees.size() + " ways to build.");
						foreach (Tree leftChildTree in leftChildTrees)
						{
							foreach (Tree rightChildTree in rightChildTrees)
							{
								IList<Tree> children = new List<Tree>();
								children.Add(leftChildTree);
								children.Add(rightChildTree);
								Tree result = tf.NewTreeNode(goalStr, children);
								//System.out.println("Binary node: "+result);
								bestTrees.Add(result);
							}
						}
					}
				}
			}
			// check unaries
			for (IEnumerator<UnaryRule> unaryI = ug.RuleIteratorByParent(goal); unaryI.MoveNext(); )
			{
				UnaryRule ur = unaryI.Current;
				double score = ur.score + iScore[start][end][ur.child];
				if (ur.child != ur.parent && Matches(score, bestScore))
				{
					// build unary
					IList<Tree> childTrees = ExtractBestParses(ur.child, start, end);
					foreach (Tree childTree in childTrees)
					{
						Tree result = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(childTree));
						//System.out.println("Unary node: "+result);
						bestTrees.Add(result);
					}
				}
			}
			if (bestTrees.IsEmpty())
			{
				log.Info("Warning: no parse found in ExhaustivePCFGParser.extractBestParse: failing on: [" + start + ", " + end + "] looking for " + goalStr);
			}
			return bestTrees;
		}

		/// <summary>Get k good parses for the sentence.</summary>
		/// <remarks>
		/// Get k good parses for the sentence.  It is expected that the
		/// parses returned approximate the k best parses, but without any
		/// guarantee that the exact list of k best parses has been produced.
		/// </remarks>
		/// <param name="k">The number of good parses to return</param>
		/// <returns>
		/// A list of k good parses for the sentence, with
		/// each accompanied by its score
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetKGoodParses(int k)
		{
			return GetKBestParses(k);
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
			throw new NotSupportedException("ExhaustivePCFGParser doesn't sample.");
		}

		//
		// BEGIN K-BEST STUFF
		// taken straight out of "Better k-best Parsing" by Liang Huang and David
		// Chiang
		//
		/// <summary>Get the exact k best parses for the sentence.</summary>
		/// <param name="k">The number of best parses to return</param>
		/// <returns>
		/// The exact k best parses for the sentence, with
		/// each accompanied by its score (typically a
		/// negative log probability).
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetKBestParses(int k)
		{
			cand = Generics.NewHashMap();
			dHat = Generics.NewHashMap();
			int start = 0;
			int end = length;
			int goal = stateIndex.IndexOf(goalStr);
			ExhaustivePCFGParser.Vertex v = new ExhaustivePCFGParser.Vertex(goal, start, end);
			IList<ScoredObject<Tree>> kBestTrees = new List<ScoredObject<Tree>>();
			for (int i = 1; i <= k; i++)
			{
				Tree internalTree = GetTree(v, i, k);
				if (internalTree == null)
				{
					break;
				}
				// restoreUnaries(internalTree);
				kBestTrees.Add(new ScoredObject<Tree>(internalTree, dHat[v][i - 1].score));
			}
			return kBestTrees;
		}

		/// <summary>Get the kth best, when calculating kPrime best (e.g.</summary>
		/// <remarks>Get the kth best, when calculating kPrime best (e.g. 2nd best of 5).</remarks>
		private Tree GetTree(ExhaustivePCFGParser.Vertex v, int k, int kPrime)
		{
			LazyKthBest(v, k, kPrime);
			string goalStr = stateIndex.Get(v.goal);
			int start = v.start;
			// int end = v.end;
			IList<ExhaustivePCFGParser.Derivation> dHatV = dHat[v];
			if (isTag[v.goal] && v.start + 1 == v.end)
			{
				IntTaggedWord tagging = new IntTaggedWord(words[start], tagIndex.IndexOf(goalStr));
				string contextStr = GetCoreLabel(start).OriginalText();
				float tagScore = lex.Score(tagging, start, wordIndex.Get(words[start]), contextStr);
				if (tagScore > float.NegativeInfinity || floodTags)
				{
					// return a pre-terminal tree
					CoreLabel terminalLabel = GetCoreLabel(start);
					Tree wordNode = tf.NewLeaf(terminalLabel);
					Tree tagNode = tf.NewTreeNode(goalStr, Java.Util.Collections.SingletonList(wordNode));
					if (originalTags[start] != null)
					{
						tagNode.Label().SetValue(originalTags[start].Tag());
					}
					if (tagNode.Label() is IHasTag)
					{
						((IHasTag)tagNode.Label()).SetTag(tagNode.Label().Value());
					}
					return tagNode;
				}
				else
				{
					System.Diagnostics.Debug.Assert(false);
				}
			}
			if (k - 1 >= dHatV.Count)
			{
				return null;
			}
			ExhaustivePCFGParser.Derivation d = dHatV[k - 1];
			IList<Tree> children = new List<Tree>();
			for (int i = 0; i < d.arc.Size(); i++)
			{
				ExhaustivePCFGParser.Vertex child = d.arc.tails[i];
				Tree t = GetTree(child, d.j[i], kPrime);
				System.Diagnostics.Debug.Assert((t != null));
				children.Add(t);
			}
			return tf.NewTreeNode(goalStr, children);
		}

		private class Vertex
		{
			public readonly int goal;

			public readonly int start;

			public readonly int end;

			public Vertex(int goal, int start, int end)
			{
				this.goal = goal;
				this.start = start;
				this.end = end;
			}

			public override bool Equals(object o)
			{
				if (!(o is ExhaustivePCFGParser.Vertex))
				{
					return false;
				}
				ExhaustivePCFGParser.Vertex v = (ExhaustivePCFGParser.Vertex)o;
				return (v.goal == goal && v.start == start && v.end == end);
			}

			private int hc = -1;

			public override int GetHashCode()
			{
				if (hc == -1)
				{
					hc = goal + (17 * (start + (17 * end)));
				}
				return hc;
			}

			public override string ToString()
			{
				return goal + "[" + start + "," + end + "]";
			}
		}

		private class Arc
		{
			public readonly IList<ExhaustivePCFGParser.Vertex> tails;

			public readonly ExhaustivePCFGParser.Vertex head;

			public readonly double ruleScore;

			public Arc(IList<ExhaustivePCFGParser.Vertex> tails, ExhaustivePCFGParser.Vertex head, double ruleScore)
			{
				// for convenience
				this.tails = Java.Util.Collections.UnmodifiableList(tails);
				this.head = head;
				this.ruleScore = ruleScore;
			}

			// TODO: add check that rule is compatible with head and tails!
			public override bool Equals(object o)
			{
				if (!(o is ExhaustivePCFGParser.Arc))
				{
					return false;
				}
				ExhaustivePCFGParser.Arc a = (ExhaustivePCFGParser.Arc)o;
				return a.head.Equals(head) && a.tails.Equals(tails);
			}

			private int hc = -1;

			public override int GetHashCode()
			{
				if (hc == -1)
				{
					hc = head.GetHashCode() + (17 * tails.GetHashCode());
				}
				return hc;
			}

			public virtual int Size()
			{
				return tails.Count;
			}
		}

		private class Derivation
		{
			public readonly ExhaustivePCFGParser.Arc arc;

			public readonly IList<int> j;

			public readonly double score;

			public readonly IList<double> childrenScores;

			public Derivation(ExhaustivePCFGParser.Arc arc, IList<int> j, double score, IList<double> childrenScores)
			{
				// score does not affect equality (?)
				this.arc = arc;
				this.j = Java.Util.Collections.UnmodifiableList(j);
				this.score = score;
				this.childrenScores = Java.Util.Collections.UnmodifiableList(childrenScores);
			}

			public override bool Equals(object o)
			{
				if (!(o is ExhaustivePCFGParser.Derivation))
				{
					return false;
				}
				ExhaustivePCFGParser.Derivation d = (ExhaustivePCFGParser.Derivation)o;
				if (arc == null && d.arc != null || arc != null && d.arc == null)
				{
					return false;
				}
				return ((arc == null && d.arc == null || d.arc.Equals(arc)) && d.j.Equals(j));
			}

			private int hc = -1;

			public override int GetHashCode()
			{
				if (hc == -1)
				{
					hc = (arc == null ? 0 : arc.GetHashCode()) + (17 * j.GetHashCode());
				}
				return hc;
			}
		}

		private IList<ExhaustivePCFGParser.Arc> GetBackwardsStar(ExhaustivePCFGParser.Vertex v)
		{
			IList<ExhaustivePCFGParser.Arc> bs = new List<ExhaustivePCFGParser.Arc>();
			// pre-terminal??
			if (isTag[v.goal] && v.start + 1 == v.end)
			{
				IList<ExhaustivePCFGParser.Vertex> tails = new List<ExhaustivePCFGParser.Vertex>();
				double score = iScore[v.start][v.end][v.goal];
				ExhaustivePCFGParser.Arc arc = new ExhaustivePCFGParser.Arc(tails, v, score);
				bs.Add(arc);
			}
			// check binaries
			for (int split = v.start + 1; split < v.end; split++)
			{
				foreach (BinaryRule br in bg.RuleListByParent(v.goal))
				{
					ExhaustivePCFGParser.Vertex lChild = new ExhaustivePCFGParser.Vertex(br.leftChild, v.start, split);
					ExhaustivePCFGParser.Vertex rChild = new ExhaustivePCFGParser.Vertex(br.rightChild, split, v.end);
					IList<ExhaustivePCFGParser.Vertex> tails = new List<ExhaustivePCFGParser.Vertex>();
					tails.Add(lChild);
					tails.Add(rChild);
					ExhaustivePCFGParser.Arc arc = new ExhaustivePCFGParser.Arc(tails, v, br.score);
					bs.Add(arc);
				}
			}
			// check unaries
			foreach (UnaryRule ur in ug.RulesByParent(v.goal))
			{
				ExhaustivePCFGParser.Vertex child = new ExhaustivePCFGParser.Vertex(ur.child, v.start, v.end);
				IList<ExhaustivePCFGParser.Vertex> tails = new List<ExhaustivePCFGParser.Vertex>();
				tails.Add(child);
				ExhaustivePCFGParser.Arc arc = new ExhaustivePCFGParser.Arc(tails, v, ur.score);
				bs.Add(arc);
			}
			return bs;
		}

		private IDictionary<ExhaustivePCFGParser.Vertex, IPriorityQueue<ExhaustivePCFGParser.Derivation>> cand = Generics.NewHashMap();

		private IDictionary<ExhaustivePCFGParser.Vertex, LinkedList<ExhaustivePCFGParser.Derivation>> dHat = Generics.NewHashMap();

		private IPriorityQueue<ExhaustivePCFGParser.Derivation> GetCandidates(ExhaustivePCFGParser.Vertex v, int k)
		{
			IPriorityQueue<ExhaustivePCFGParser.Derivation> candV = cand[v];
			if (candV == null)
			{
				candV = new BinaryHeapPriorityQueue<ExhaustivePCFGParser.Derivation>();
				IList<ExhaustivePCFGParser.Arc> bsV = GetBackwardsStar(v);
				foreach (ExhaustivePCFGParser.Arc arc in bsV)
				{
					int size = arc.Size();
					double score = arc.ruleScore;
					IList<double> childrenScores = new List<double>();
					for (int i = 0; i < size; i++)
					{
						ExhaustivePCFGParser.Vertex child = arc.tails[i];
						double s = iScore[child.start][child.end][child.goal];
						childrenScores.Add(s);
						score += s;
					}
					if (score == double.NegativeInfinity)
					{
						continue;
					}
					IList<int> j = new List<int>();
					for (int i_1 = 0; i_1 < size; i_1++)
					{
						j.Add(1);
					}
					ExhaustivePCFGParser.Derivation d = new ExhaustivePCFGParser.Derivation(arc, j, score, childrenScores);
					candV.Add(d, score);
				}
				IPriorityQueue<ExhaustivePCFGParser.Derivation> tmp = new BinaryHeapPriorityQueue<ExhaustivePCFGParser.Derivation>();
				for (int i_2 = 0; i_2 < k; i_2++)
				{
					if (candV.IsEmpty())
					{
						break;
					}
					ExhaustivePCFGParser.Derivation d = candV.RemoveFirst();
					tmp.Add(d, d.score);
				}
				candV = tmp;
				cand[v] = candV;
			}
			return candV;
		}

		// note: kPrime is the original k
		private void LazyKthBest(ExhaustivePCFGParser.Vertex v, int k, int kPrime)
		{
			IPriorityQueue<ExhaustivePCFGParser.Derivation> candV = GetCandidates(v, kPrime);
			LinkedList<ExhaustivePCFGParser.Derivation> dHatV = dHat[v];
			if (dHatV == null)
			{
				dHatV = new LinkedList<ExhaustivePCFGParser.Derivation>();
				dHat[v] = dHatV;
			}
			while (dHatV.Count < k)
			{
				if (!dHatV.IsEmpty())
				{
					ExhaustivePCFGParser.Derivation derivation = dHatV.GetLast();
					LazyNext(candV, derivation, kPrime);
				}
				if (!candV.IsEmpty())
				{
					ExhaustivePCFGParser.Derivation d = candV.RemoveFirst();
					dHatV.Add(d);
				}
				else
				{
					break;
				}
			}
		}

		private void LazyNext(IPriorityQueue<ExhaustivePCFGParser.Derivation> candV, ExhaustivePCFGParser.Derivation derivation, int kPrime)
		{
			IList<ExhaustivePCFGParser.Vertex> tails = derivation.arc.tails;
			for (int i = 0; i < sz; i++)
			{
				IList<int> j = new List<int>(derivation.j);
				j.Set(i, j[i] + 1);
				ExhaustivePCFGParser.Vertex Ti = tails[i];
				LazyKthBest(Ti, j[i], kPrime);
				LinkedList<ExhaustivePCFGParser.Derivation> dHatTi = dHat[Ti];
				// compute score for this derivation
				if (j[i] - 1 >= dHatTi.Count)
				{
					continue;
				}
				ExhaustivePCFGParser.Derivation d = dHatTi[j[i] - 1];
				double newScore = derivation.score - derivation.childrenScores[i] + d.score;
				IList<double> childrenScores = new List<double>(derivation.childrenScores);
				childrenScores.Set(i, d.score);
				ExhaustivePCFGParser.Derivation newDerivation = new ExhaustivePCFGParser.Derivation(derivation.arc, j, newScore, childrenScores);
				if (!candV.Contains(newDerivation) && newScore > double.NegativeInfinity)
				{
					candV.Add(newDerivation, newScore);
				}
			}
		}

		//
		// END K-BEST STUFF
		//
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
			int start = 0;
			int end = length;
			int goal = stateIndex.IndexOf(goalStr);
			double bestScore = iScore[start][end][goal];
			IList<Tree> internalTrees = ExtractBestParses(goal, start, end);
			//System.out.println("Got internal best parse...");
			// for (Tree internalTree : internalTrees) {
			//   restoreUnaries(internalTree);
			// }
			//System.out.println("Restored unaries...");
			IList<ScoredObject<Tree>> scoredTrees = new List<ScoredObject<Tree>>(internalTrees.Count);
			foreach (Tree tr in internalTrees)
			{
				scoredTrees.Add(new ScoredObject<Tree>(tr, bestScore));
			}
			return scoredTrees;
		}

		//TreeTransformer debinarizer = BinarizerFactory.getDebinarizer();
		//return debinarizer.transformTree(internalTree);
		protected internal virtual IList<ParserConstraint> GetConstraints()
		{
			return constraints;
		}

		internal virtual void SetConstraints(IList<ParserConstraint> constraints)
		{
			if (constraints == null)
			{
				this.constraints = Java.Util.Collections.EmptyList();
			}
			else
			{
				this.constraints = constraints;
			}
		}

		public ExhaustivePCFGParser(BinaryGrammar bg, UnaryGrammar ug, ILexicon lex, Options op, IIndex<string> stateIndex, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			//    System.out.println("ExhaustivePCFGParser constructor called.");
			this.bg = bg;
			this.ug = ug;
			this.lex = lex;
			this.op = op;
			this.tlp = op.Langpack();
			goalStr = tlp.StartSymbol();
			this.stateIndex = stateIndex;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			tf = new LabeledScoredTreeFactory();
			numStates = stateIndex.Size();
			isTag = new bool[numStates];
			// tag index is smaller, so we fill by iterating over the tag index
			// rather than over the state index
			foreach (string tag in tagIndex.ObjectsList())
			{
				int state = stateIndex.IndexOf(tag);
				if (state < 0)
				{
					continue;
				}
				isTag[state] = true;
			}
		}

		public virtual void NudgeDownArraySize()
		{
			try
			{
				if (arraySize > 2)
				{
					ConsiderCreatingArrays(arraySize - 2);
				}
			}
			catch (OutOfMemoryException oome)
			{
				Sharpen.Runtime.PrintStackTrace(oome);
			}
		}

		private void ConsiderCreatingArrays(int length)
		{
			if (length > op.testOptions.maxLength + 1 || length >= myMaxLength)
			{
				throw new OutOfMemoryException("Refusal to create such large arrays.");
			}
			else
			{
				try
				{
					CreateArrays(length + 1);
				}
				catch (OutOfMemoryException e)
				{
					myMaxLength = length;
					if (arraySize > 0)
					{
						try
						{
							CreateArrays(arraySize);
						}
						catch (OutOfMemoryException)
						{
							throw new Exception("CANNOT EVEN CREATE ARRAYS OF ORIGINAL SIZE!!");
						}
					}
					throw;
				}
				arraySize = length + 1;
				if (op.testOptions.verbose)
				{
					log.Info("Created PCFG parser arrays of size " + arraySize);
				}
			}
		}

		protected internal virtual void CreateArrays(int length)
		{
			// zero out some stuff first in case we recently ran out of memory and are reallocating
			ClearArrays();
			int numTags = tagIndex.Size();
			// allocate just the parts of iScore and oScore used (end > start, etc.)
			// todo: with some modifications to doInsideScores, we wouldn't need to allocate iScore[i,length] for i != 0 and i != length
			//    System.out.println("initializing iScore arrays with length " + length + " and numStates " + numStates);
			iScore = new float[length][][];
			for (int start = 0; start < length; start++)
			{
				for (int end = start + 1; end <= length; end++)
				{
					iScore[start][end] = new float[numStates];
				}
			}
			//    System.out.println("finished initializing iScore arrays");
			if (op.doDep && !op.testOptions.useFastFactored)
			{
				//      System.out.println("initializing oScore arrays with length " + length + " and numStates " + numStates);
				oScore = new float[length][][];
				for (int start_1 = 0; start_1 < length; start_1++)
				{
					for (int end = start_1 + 1; end <= length; end++)
					{
						oScore[start_1][end] = new float[numStates];
					}
				}
			}
			// System.out.println("finished initializing oScore arrays");
			narrowRExtent = new int[length][];
			wideRExtent = new int[length][];
			narrowLExtent = new int[][] {  };
			wideLExtent = new int[][] {  };
			if (op.doDep && !op.testOptions.useFastFactored)
			{
				iPossibleByL = new bool[length][];
				iPossibleByR = new bool[][] {  };
				oPossibleByL = new bool[length][];
				oPossibleByR = new bool[][] {  };
			}
			tags = new bool[length][];
			if (op.testOptions.lengthNormalization)
			{
				wordsInSpan = new int[length][][];
				for (int start_1 = 0; start_1 < length; start_1++)
				{
					for (int end = start_1 + 1; end <= length; end++)
					{
						wordsInSpan[start_1][end] = new int[numStates];
					}
				}
			}
		}

		//    System.out.println("ExhaustivePCFGParser constructor finished.");
		private void ClearArrays()
		{
			iScore = oScore = null;
			iPossibleByL = iPossibleByR = oPossibleByL = oPossibleByR = null;
			oFilteredEnd = oFilteredStart = null;
			tags = null;
			narrowRExtent = wideRExtent = narrowLExtent = wideLExtent = null;
		}
	}
}
