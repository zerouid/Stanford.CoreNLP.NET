// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002-2006 The Board of Trustees of
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
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// An exhaustive O(n<sup>4</sup>t<sup>2</sup>) time and O(n<sup>2</sup>t)
	/// space dependency parser.
	/// </summary>
	/// <remarks>
	/// An exhaustive O(n<sup>4</sup>t<sup>2</sup>) time and O(n<sup>2</sup>t)
	/// space dependency parser.
	/// This follows the general
	/// picture of the Eisner and Satta dependency parsing papers, but without the
	/// tricks in defining items that they use to get an O(n<sup>3</sup>)
	/// dependency parser.  The parser is as described in:
	/// <p/>
	/// Dan Klein and Christopher D. Manning. 2003. Fast Exact Inference with a
	/// Factored Model for Natural Language Parsing. In Suzanna Becker, Sebastian
	/// Thrun, and Klaus Obermayer (eds), Advances in Neural Information Processing
	/// Systems 15 (NIPS 2002). Cambridge, MA: MIT Press, pp. 3-10.
	/// http://nlp.stanford.edu/pubs/lex-parser.pdf
	/// <p/>
	/// </remarks>
	/// <author>Dan Klein</author>
	public class ExhaustiveDependencyParser : IScorer, IKBestViterbiParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ExhaustiveDependencyParser));

		private const bool Debug = false;

		private const bool DebugMore = false;

		private readonly IIndex<string> tagIndex;

		private readonly IIndex<string> wordIndex;

		private ITreeFactory tf;

		private IDependencyGrammar dg;

		private ILexicon lex;

		private Options op;

		private ITreebankLanguagePack tlp;

		private IList sentence;

		private int[] words;

		/// <summary>Max log inner probability score.</summary>
		/// <remarks>
		/// Max log inner probability score.
		/// Indices:
		/// 1. headPos - index of head word (one side of subtree)
		/// 2. headTag - which tag assigned
		/// 3. cornerPosition - other end of span, i.e. "corner" of right triangle
		/// </remarks>
		private float[][][] iScoreH;

		/// <summary>Max log outer probability score.</summary>
		/// <remarks>Max log outer probability score.  Same indices as iScoreH.</remarks>
		private float[][][] oScoreH;

		/// <summary>Total log inner probability score.</summary>
		/// <remarks>
		/// Total log inner probability score.  Same indices as iScoreH.  Designed for
		/// producing summed total probabilities.  Unfinished.
		/// </remarks>
		private float[][][] iScoreHSum;

		/// <summary>If true, compute iScoreHSum</summary>
		private const bool doiScoreHSum = false;

		private int[][] rawDistance;

		internal int[][] binDistance;

		internal float[][][][][] headScore;

		internal float[][][] headStop;

		private bool[][][] oPossibleByL;

		private bool[][][] oPossibleByR;

		private bool[][][] iPossibleByL;

		private bool[][][] iPossibleByR;

		private int arraySize = 0;

		private int myMaxLength = -unchecked((int)(0xDEADBEEF));

		// headPos, headTag, cornerPosition (non-head)
		// headPos, headTag, cornerPosition (non-head)
		// reused in other class, so can't be private
		// headPos, headTag, split
		internal virtual float OScore(int start, int end, int head, int tag)
		{
			return oScoreH[head][dg.TagBin(tag)][start] + oScoreH[head][dg.TagBin(tag)][end];
		}

		/// <summary>
		/// Probability of *most likely* parse having word (at head) with given POS
		/// tag as marker on tree over start (inclusive) ...
		/// </summary>
		/// <remarks>
		/// Probability of *most likely* parse having word (at head) with given POS
		/// tag as marker on tree over start (inclusive) ... end (exclusive).  Found
		/// by summing (product done in log space) the log probabilities in the two
		/// half-triangles.  The indices of iScoreH are: (1) head word index,
		/// (2) head tag assigned, and (3) other corner that ends span.
		/// </remarks>
		internal virtual float IScore(int start, int end, int head, int tag)
		{
			return iScoreH[head][dg.TagBin(tag)][start] + iScoreH[head][dg.TagBin(tag)][end];
		}

		/// <summary>
		/// Total probability of all parses having word (at head) with given POS tag
		/// as marker on tree over start (inclusive) ..
		/// </summary>
		/// <remarks>
		/// Total probability of all parses having word (at head) with given POS tag
		/// as marker on tree over start (inclusive) .. end (exclusive).
		/// TODO: CURRENTLY UNTESTED!
		/// </remarks>
		internal virtual float IScoreTotal(int start, int end, int head, int tag)
		{
			throw new Exception("Summed inner scores not computed");
			// log scores: so + => * and exploiting independence of left and right choices
			return iScoreHSum[head][dg.TagBin(tag)][start] + iScoreHSum[head][dg.TagBin(tag)][end];
		}

		public virtual double OScore(Edge edge)
		{
			return OScore(edge.start, edge.end, edge.head, edge.tag);
		}

		public virtual double IScore(Edge edge)
		{
			return IScore(edge.start, edge.end, edge.head, edge.tag);
		}

		public virtual bool OPossible(Hook hook)
		{
			return (hook.IsPreHook() ? oPossibleByR[hook.end][hook.head][dg.TagBin(hook.tag)] : oPossibleByL[hook.start][hook.head][dg.TagBin(hook.tag)]);
		}

		public virtual bool IPossible(Hook hook)
		{
			return (hook.IsPreHook() ? iPossibleByR[hook.start][hook.head][dg.TagBin(hook.tag)] : iPossibleByL[hook.end][hook.head][dg.TagBin(hook.tag)]);
		}

		public virtual bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			if (op.testOptions.verbose)
			{
				Timing.Tick("Starting dependency parse.");
			}
			this.sentence = sentence;
			int length = sentence.Count;
			if (length > arraySize)
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
								throw new Exception("CANNOT EVEN CREATE ARRAYS OF ORIGINAL SIZE!!! " + arraySize);
							}
						}
						throw;
					}
					arraySize = length + 1;
					if (op.testOptions.verbose)
					{
						log.Info("Created dparser arrays of size " + arraySize);
					}
				}
			}
			if (op.testOptions.verbose)
			{
				log.Info("Initializing...");
			}
			// map to words
			words = new int[length];
			int numTags = dg.NumTagBins();
			//tagIndex.size();
			//System.out.println("\nNumTags: "+numTags);
			//System.out.println(tagIndex);
			bool[][] hasTag = new bool[length][];
			for (int i = 0; i < length; i++)
			{
				//if (wordIndex.contains(sentence.get(i).toString()))
				words[i] = wordIndex.AddToIndex(sentence[i].Word());
			}
			//else
			//words[i] = wordIndex.indexOf(Lexicon.UNKNOWN_WORD);
			for (int head = 0; head < length; head++)
			{
				for (int tag = 0; tag < numTags; tag++)
				{
					Arrays.Fill(iScoreH[head][tag], float.NegativeInfinity);
					Arrays.Fill(oScoreH[head][tag], float.NegativeInfinity);
				}
			}
			for (int head_1 = 0; head_1 < length; head_1++)
			{
				for (int loc = 0; loc <= length; loc++)
				{
					rawDistance[head_1][loc] = (head_1 >= loc ? head_1 - loc : loc - head_1 - 1);
					binDistance[head_1][loc] = dg.DistanceBin(rawDistance[head_1][loc]);
				}
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			// do tags
			for (int start = 0; start + 1 <= length; start++)
			{
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
				int word = words[start];
				for (IEnumerator<IntTaggedWord> taggingI = lex.RuleIteratorByWord(word, start, wordContextStr); taggingI.MoveNext(); )
				{
					IntTaggedWord tagging = taggingI.Current;
					if (trueTagStr != null)
					{
						if (!tlp.BasicCategory(tagging.TagString(tagIndex)).Equals(trueTagStr))
						{
							continue;
						}
					}
					float score = lex.Score(tagging, start, wordIndex.Get(tagging.word), wordContextStr);
					//iScoreH[start][tag][start] = (op.dcTags ? (float)op.testOptions.depWeight*score : 0.0f);
					if (score > float.NegativeInfinity)
					{
						int tag = tagging.tag;
						iScoreH[start][dg.TagBin(tag)][start] = 0.0f;
						iScoreH[start][dg.TagBin(tag)][start + 1] = 0.0f;
					}
				}
			}
			for (int hWord = 0; hWord < length; hWord++)
			{
				for (int hTag = 0; hTag < numTags; hTag++)
				{
					hasTag[hWord][hTag] = (iScoreH[hWord][hTag][hWord] + iScoreH[hWord][hTag][hWord + 1] > float.NegativeInfinity);
					Arrays.Fill(headStop[hWord][hTag], float.NegativeInfinity);
					for (int aWord = 0; aWord < length; aWord++)
					{
						for (int dist = 0; dist < dg.NumDistBins(); dist++)
						{
							Arrays.Fill(headScore[dist][hWord][hTag][aWord], float.NegativeInfinity);
						}
					}
				}
			}
			// score and cache all pairs -- headScores and stops
			//int hit = 0;
			for (int hWord_1 = 0; hWord_1 < length; hWord_1++)
			{
				for (int hTag = 0; hTag < numTags; hTag++)
				{
					//Arrays.fill(headStopL[hWord][hTag], Float.NEGATIVE_INFINITY);
					//Arrays.fill(headStopR[hWord][hTag], Float.NEGATIVE_INFINITY);
					//Arrays.fill(headStop[hWord][hTag], Float.NEGATIVE_INFINITY);
					if (!hasTag[hWord_1][hTag])
					{
						continue;
					}
					for (int split = 0; split <= length; split++)
					{
						if (split <= hWord_1)
						{
							headStop[hWord_1][hTag][split] = (float)dg.ScoreTB(words[hWord_1], hTag, -2, -2, false, hWord_1 - split);
						}
						else
						{
							//System.out.println("headstopL " + hWord +" " + hTag + " " + split + " " + headStopL[hWord][hTag][split]); // debugging
							headStop[hWord_1][hTag][split] = (float)dg.ScoreTB(words[hWord_1], hTag, -2, -2, true, split - hWord_1 - 1);
						}
					}
					//System.out.println("headstopR " + hWord +" " + hTag + " " + split + " " + headStopR[hWord][hTag][split]); // debugging
					//hit++;
					//Timing.tick("hWord: "+hWord+" hTag: "+hTag+" piddle count: "+hit);
					for (int aWord = 0; aWord < length; aWord++)
					{
						if (aWord == hWord_1)
						{
							continue;
						}
						// can't be argument of yourself
						bool leftHeaded = hWord_1 < aWord;
						int start_1;
						int end;
						if (leftHeaded)
						{
							start_1 = hWord_1 + 1;
							end = aWord + 1;
						}
						else
						{
							start_1 = aWord + 1;
							end = hWord_1 + 1;
						}
						for (int aTag = 0; aTag < numTags; aTag++)
						{
							if (!hasTag[aWord][aTag])
							{
								continue;
							}
							for (int split_1 = start_1; split_1 < end; split_1++)
							{
								// Moved this stuff out two loops- GMA
								//              for (int split = 0; split <= length; split++) {
								// if leftHeaded, go from hWord+1 to aWord
								// else go from aWord+1 to hWord
								//              if ((leftHeaded && (split <= hWord || split > aWord)) ||
								//                      ((!leftHeaded) && (split <= aWord || split > hWord)))
								//                continue;
								int headDistance = rawDistance[hWord_1][split_1];
								int binDist = binDistance[hWord_1][split_1];
								headScore[binDist][hWord_1][hTag][aWord][aTag] = (float)dg.ScoreTB(words[hWord_1], hTag, words[aWord], aTag, leftHeaded, headDistance);
								//hit++;
								// skip other splits with same binDist
								while (split_1 + 1 < end && binDistance[hWord_1][split_1 + 1] == binDist)
								{
									split_1++;
								}
							}
						}
					}
				}
			}
			// end split
			// end aTag
			// end aWord
			// end hTag
			// end hWord
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				// displayHeadScores();
				log.Info("Starting insides...");
			}
			// do larger spans
			for (int diff = 2; diff <= length; diff++)
			{
				if (Thread.Interrupted())
				{
					throw new RuntimeInterruptedException();
				}
				for (int start_1 = 0; start_1 + diff <= length; start_1++)
				{
					int end = start_1 + diff;
					// left extension
					int endHead = end - 1;
					for (int endTag = 0; endTag < numTags; endTag++)
					{
						if (!hasTag[endHead][endTag])
						{
							continue;
						}
						// bestScore is max for iScoreH
						float bestScore = float.NegativeInfinity;
						for (int argHead = start_1; argHead < endHead; argHead++)
						{
							for (int argTag = 0; argTag < numTags; argTag++)
							{
								if (!hasTag[argHead][argTag])
								{
									continue;
								}
								float argLeftScore = iScoreH[argHead][argTag][start_1];
								if (argLeftScore == float.NegativeInfinity)
								{
									continue;
								}
								float stopLeftScore = headStop[argHead][argTag][start_1];
								if (stopLeftScore == float.NegativeInfinity)
								{
									continue;
								}
								for (int split = argHead + 1; split < end; split++)
								{
									// short circuit if dependency is impossible
									float depScore = headScore[binDistance[endHead][split]][endHead][endTag][argHead][argTag];
									if (depScore == float.NegativeInfinity)
									{
										continue;
									}
									float score = iScoreH[endHead][endTag][split] + argLeftScore + iScoreH[argHead][argTag][split] + depScore + stopLeftScore + headStop[argHead][argTag][split];
									if (score > bestScore)
									{
										bestScore = score;
									}
								}
							}
						}
						// end for split
						// sum for iScoreHSum
						// end for argTag : tags
						// end for argHead
						iScoreH[endHead][endTag][start_1] = bestScore;
					}
					// end for endTag : tags
					// right extension
					int startHead = start_1;
					for (int startTag = 0; startTag < numTags; startTag++)
					{
						if (!hasTag[startHead][startTag])
						{
							continue;
						}
						// bestScore is max for iScoreH
						float bestScore = float.NegativeInfinity;
						for (int argHead = start_1 + 1; argHead < end; argHead++)
						{
							for (int argTag = 0; argTag < numTags; argTag++)
							{
								if (!hasTag[argHead][argTag])
								{
									continue;
								}
								float argRightScore = iScoreH[argHead][argTag][end];
								if (argRightScore == float.NegativeInfinity)
								{
									continue;
								}
								float stopRightScore = headStop[argHead][argTag][end];
								if (stopRightScore == float.NegativeInfinity)
								{
									continue;
								}
								for (int split = start_1 + 1; split <= argHead; split++)
								{
									// short circuit if dependency is impossible
									float depScore = headScore[binDistance[startHead][split]][startHead][startTag][argHead][argTag];
									if (depScore == float.NegativeInfinity)
									{
										continue;
									}
									float score = iScoreH[startHead][startTag][split] + iScoreH[argHead][argTag][split] + argRightScore + depScore + stopRightScore + headStop[argHead][argTag][split];
									if (score > bestScore)
									{
										bestScore = score;
									}
								}
							}
						}
						// sum for iScoreHSum
						// end for argTag: tags
						// end for argHead
						iScoreH[startHead][startTag][end] = bestScore;
					}
				}
			}
			// end for startTag: tags
			// end for start
			// end for diff (i.e., span)
			int goalTag = dg.TagBin(tagIndex.IndexOf(LexiconConstants.BoundaryTag));
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				log.Info("Dep  parsing " + length + " words (incl. stop): insideScore " + (iScoreH[length - 1][goalTag][0] + iScoreH[length - 1][goalTag][length]));
			}
			if (!op.doPCFG)
			{
				return HasParse();
			}
			if (op.testOptions.verbose)
			{
				log.Info("Starting outsides...");
			}
			oScoreH[length - 1][goalTag][0] = 0.0f;
			oScoreH[length - 1][goalTag][length] = 0.0f;
			for (int diff_1 = length; diff_1 > 1; diff_1--)
			{
				if (Thread.Interrupted())
				{
					throw new RuntimeInterruptedException();
				}
				for (int start_1 = 0; start_1 + diff_1 <= length; start_1++)
				{
					int end = start_1 + diff_1;
					// left half
					int endHead = end - 1;
					for (int endTag = 0; endTag < numTags; endTag++)
					{
						if (!hasTag[endHead][endTag])
						{
							continue;
						}
						for (int argHead = start_1; argHead < endHead; argHead++)
						{
							for (int argTag = 0; argTag < numTags; argTag++)
							{
								if (!hasTag[argHead][argTag])
								{
									continue;
								}
								for (int split = argHead; split <= endHead; split++)
								{
									float subScore = (oScoreH[endHead][endTag][start_1] + headScore[binDistance[endHead][split]][endHead][endTag][argHead][argTag] + headStop[argHead][argTag][start_1] + headStop[argHead][argTag][split]);
									float scoreRight = (subScore + iScoreH[argHead][argTag][start_1] + iScoreH[argHead][argTag][split]);
									float scoreMid = (subScore + iScoreH[argHead][argTag][start_1] + iScoreH[endHead][endTag][split]);
									float scoreLeft = (subScore + iScoreH[argHead][argTag][split] + iScoreH[endHead][endTag][split]);
									if (scoreRight > oScoreH[endHead][endTag][split])
									{
										oScoreH[endHead][endTag][split] = scoreRight;
									}
									if (scoreMid > oScoreH[argHead][argTag][split])
									{
										oScoreH[argHead][argTag][split] = scoreMid;
									}
									if (scoreLeft > oScoreH[argHead][argTag][start_1])
									{
										oScoreH[argHead][argTag][start_1] = scoreLeft;
									}
								}
							}
						}
					}
					// right half
					int startHead = start_1;
					for (int startTag = 0; startTag < numTags; startTag++)
					{
						if (!hasTag[startHead][startTag])
						{
							continue;
						}
						for (int argHead = startHead + 1; argHead < end; argHead++)
						{
							for (int argTag = 0; argTag < numTags; argTag++)
							{
								if (!hasTag[argHead][argTag])
								{
									continue;
								}
								for (int split = startHead + 1; split <= argHead; split++)
								{
									float subScore = (oScoreH[startHead][startTag][end] + headScore[binDistance[startHead][split]][startHead][startTag][argHead][argTag] + headStop[argHead][argTag][split] + headStop[argHead][argTag][end]);
									float scoreLeft = (subScore + iScoreH[argHead][argTag][split] + iScoreH[argHead][argTag][end]);
									float scoreMid = (subScore + iScoreH[startHead][startTag][split] + iScoreH[argHead][argTag][end]);
									float scoreRight = (subScore + iScoreH[startHead][startTag][split] + iScoreH[argHead][argTag][split]);
									if (scoreLeft > oScoreH[startHead][startTag][split])
									{
										oScoreH[startHead][startTag][split] = scoreLeft;
									}
									if (scoreMid > oScoreH[argHead][argTag][split])
									{
										oScoreH[argHead][argTag][split] = scoreMid;
									}
									if (scoreRight > oScoreH[argHead][argTag][end])
									{
										oScoreH[argHead][argTag][end] = scoreRight;
									}
								}
							}
						}
					}
				}
			}
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
				log.Info("Starting half-filters...");
			}
			for (int loc_1 = 0; loc_1 <= length; loc_1++)
			{
				for (int head_2 = 0; head_2 < length; head_2++)
				{
					Arrays.Fill(iPossibleByL[loc_1][head_2], false);
					Arrays.Fill(iPossibleByR[loc_1][head_2], false);
					Arrays.Fill(oPossibleByL[loc_1][head_2], false);
					Arrays.Fill(oPossibleByR[loc_1][head_2], false);
				}
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			for (int head_3 = 0; head_3 < length; head_3++)
			{
				for (int tag = 0; tag < numTags; tag++)
				{
					if (!hasTag[head_3][tag])
					{
						continue;
					}
					for (int start_1 = 0; start_1 <= head_3; start_1++)
					{
						for (int end = head_3 + 1; end <= length; end++)
						{
							if (iScoreH[head_3][tag][start_1] + iScoreH[head_3][tag][end] > float.NegativeInfinity && oScoreH[head_3][tag][start_1] + oScoreH[head_3][tag][end] > float.NegativeInfinity)
							{
								iPossibleByR[end][head_3][tag] = true;
								iPossibleByL[start_1][head_3][tag] = true;
								oPossibleByR[end][head_3][tag] = true;
								oPossibleByL[start_1][head_3][tag] = true;
							}
						}
					}
				}
			}
			if (op.testOptions.verbose)
			{
				Timing.Tick("done.");
			}
			return HasParse();
		}

		public virtual bool HasParse()
		{
			return GetBestScore() > float.NegativeInfinity;
		}

		public virtual double GetBestScore()
		{
			int length = sentence.Count;
			if (length > arraySize)
			{
				return float.NegativeInfinity;
			}
			int goalTag = tagIndex.IndexOf(LexiconConstants.BoundaryTag);
			return IScore(0, length, length - 1, goalTag);
		}

		/// <summary>
		/// This displays a headScore matrix, which will be valid after parsing
		/// a sentence.
		/// </summary>
		/// <remarks>
		/// This displays a headScore matrix, which will be valid after parsing
		/// a sentence.  Unclear yet whether this is valid/useful [cdm].
		/// </remarks>
		public virtual void DisplayHeadScores()
		{
			int numTags = tagIndex.Size();
			System.Console.Out.WriteLine("---- headScore matrix (head x dep, best tags) ----");
			System.Console.Out.Write(StringUtils.PadOrTrim(string.Empty, 6));
			foreach (int word in words)
			{
				System.Console.Out.Write(" " + StringUtils.PadOrTrim(wordIndex.Get(word), 2));
			}
			System.Console.Out.WriteLine();
			for (int hWord = 0; hWord < words.Length; hWord++)
			{
				System.Console.Out.Write(StringUtils.PadOrTrim(wordIndex.Get(words[hWord]), 6));
				int bigBD = -1;
				int bigHTag = -1;
				int bigATag = -1;
				for (int aWord = 0; aWord < words.Length; aWord++)
				{
					// we basically just max of all the variables, but for distance > 0, we
					// include a factor for generating something at distance 0, or else
					// the result is too whacked out to be useful
					float biggest = float.NegativeInfinity;
					for (int bd = 0; bd < dg.NumDistBins(); bd++)
					{
						for (int hTag = 0; hTag < numTags; hTag++)
						{
							/*
							float penalty = 0.0f;
							if (bd != 0) {
							penalty = (float) dg.score(words[hWord], hTag, -2, -2, aWord > hWord, 0);
							penalty = (float) Math.log(1.0 - Math.exp(penalty));
							}
							for (int aTag = 0; aTag < numTags; aTag++) {
							if (headScore[bd][hWord][hTag][aWord][aTag] + penalty > biggest) {
							biggest = headScore[bd][hWord][hTag][aWord][aTag] + penalty;
							*/
							for (int aTag = 0; aTag < numTags; aTag++)
							{
								if (headScore[bd][hWord][dg.TagBin(hTag)][aWord][dg.TagBin(aTag)] > biggest)
								{
									biggest = headScore[bd][hWord][dg.TagBin(hTag)][aWord][dg.TagBin(aTag)];
									bigBD = bd;
									bigHTag = hTag;
									bigATag = aTag;
								}
							}
						}
					}
					if (float.IsInfinite(biggest))
					{
						System.Console.Out.Write(" " + StringUtils.PadOrTrim("in", 2));
					}
					else
					{
						int score = Math.Round(Math.Abs(headScore[bigBD][hWord][dg.TagBin(bigHTag)][aWord][dg.TagBin(bigATag)]));
						System.Console.Out.Write(" " + StringUtils.PadOrTrim(int.ToString(score), 2));
					}
				}
				System.Console.Out.WriteLine();
			}
		}

		private const double Tol = 1e-5;

		private static bool Matches(double x, double y)
		{
			return (Math.Abs(x - y) / (Math.Abs(x) + Math.Abs(y) + 1e-10) < Tol);
		}

		/// <summary>Find the best (partial) parse within the parameter constraints.</summary>
		/// <param name="start">Sentence index of start of span (fenceposts, from 0 up)</param>
		/// <param name="end">Sentence index of end of span (right side fencepost)</param>
		/// <param name="hWord">Sentence index of head word (left side fencepost)</param>
		/// <param name="hTag">Tag assigned to hWord</param>
		/// <returns>The best parse tree within the parameter constraints</returns>
		private Tree ExtractBestParse(int start, int end, int hWord, int hTag)
		{
			string headWordStr = wordIndex.Get(words[hWord]);
			string headTagStr = tagIndex.Get(hTag);
			ILabel headLabel = new CategoryWordTag(headWordStr, headWordStr, headTagStr);
			int numTags = tagIndex.Size();
			// deal with span 1
			if (end - start == 1)
			{
				Tree leaf = tf.NewLeaf(new Word(headWordStr));
				return tf.NewTreeNode(headLabel, Java.Util.Collections.SingletonList(leaf));
			}
			// find backtrace
			IList<Tree> children = new List<Tree>();
			double bestScore = IScore(start, end, hWord, hTag);
			for (int split = start + 1; split < end; split++)
			{
				int binD = binDistance[hWord][split];
				if (hWord < split)
				{
					for (int aWord = split; aWord < end; aWord++)
					{
						for (int aTag = 0; aTag < numTags; aTag++)
						{
							if (Matches(IScore(start, split, hWord, hTag) + IScore(split, end, aWord, aTag) + headScore[binD][hWord][dg.TagBin(hTag)][aWord][dg.TagBin(aTag)] + headStop[aWord][dg.TagBin(aTag)][split] + headStop[aWord][dg.TagBin(aTag)][end], bestScore))
							{
								// build it
								children.Add(ExtractBestParse(start, split, hWord, hTag));
								children.Add(ExtractBestParse(split, end, aWord, aTag));
								return tf.NewTreeNode(headLabel, children);
							}
						}
					}
				}
				else
				{
					for (int aWord = start; aWord < split; aWord++)
					{
						for (int aTag = 0; aTag < numTags; aTag++)
						{
							if (Matches(IScore(start, split, aWord, aTag) + IScore(split, end, hWord, hTag) + headScore[binD][hWord][dg.TagBin(hTag)][aWord][dg.TagBin(aTag)] + headStop[aWord][dg.TagBin(aTag)][start] + headStop[aWord][dg.TagBin(aTag)][split], bestScore))
							{
								children.Add(ExtractBestParse(start, split, aWord, aTag));
								children.Add(ExtractBestParse(split, end, hWord, hTag));
								// build it
								return tf.NewTreeNode(headLabel, children);
							}
						}
					}
				}
			}
			log.Info("Problem in ExhaustiveDependencyParser::extractBestParse");
			return null;
		}

		private Tree Flatten(Tree tree)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return tree;
			}
			IList<Tree> newChildren = new List<Tree>();
			Tree[] children = tree.Children();
			foreach (Tree child in children)
			{
				Tree newChild = Flatten(child);
				if (!newChild.IsPreTerminal() && newChild.Label().ToString().Equals(tree.Label().ToString()))
				{
					Sharpen.Collections.AddAll(newChildren, newChild.GetChildrenAsList());
				}
				else
				{
					newChildren.Add(newChild);
				}
			}
			return tf.NewTreeNode(tree.Label(), newChildren);
		}

		/// <summary>Return the best dependency parse for a sentence.</summary>
		/// <remarks>
		/// Return the best dependency parse for a sentence.  You must call
		/// <c>parse()</c>
		/// before a call to this method.
		/// <p>
		/// <i>Implementation note:</i> the best parse is recalculated from the chart
		/// each time this method is called.  It isn't cached.
		/// </remarks>
		/// <returns>
		/// The best dependency parse for a sentence or
		/// <see langword="null"/>
		/// .
		/// The returned tree will begin with a binary branching node, the
		/// left branch of which is the dependency tree proper, and the right
		/// side of which contains a boundary word .$. which heads the
		/// sentence.
		/// </returns>
		public virtual Tree GetBestParse()
		{
			if (!HasParse())
			{
				return null;
			}
			return Flatten(ExtractBestParse(0, words.Length, words.Length - 1, tagIndex.IndexOf(LexiconConstants.BoundaryTag)));
		}

		public ExhaustiveDependencyParser(IDependencyGrammar dg, ILexicon lex, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			this.dg = dg;
			this.lex = lex;
			this.op = op;
			this.tlp = op.Langpack();
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			tf = new LabeledScoredTreeFactory();
		}

		private void CreateArrays(int length)
		{
			iScoreH = oScoreH = headStop = iScoreHSum = null;
			iPossibleByL = iPossibleByR = oPossibleByL = oPossibleByR = null;
			headScore = null;
			rawDistance = binDistance = null;
			int tagNum = dg.NumTagBins();
			//tagIndex.size();
			iScoreH = new float[][][] {  };
			oScoreH = new float[][][] {  };
			iPossibleByL = new bool[][][] {  };
			iPossibleByR = new bool[][][] {  };
			oPossibleByL = new bool[][][] {  };
			oPossibleByR = new bool[][][] {  };
			headScore = new float[][][][][] {  };
			headStop = new float[][][] {  };
			rawDistance = new int[][] {  };
			binDistance = new int[][] {  };
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
			throw new NotSupportedException("Doesn't do k best yet");
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
			throw new NotSupportedException("Doesn't do best parses yet");
		}

		/// <summary>Get k good parses for the sentence.</summary>
		/// <remarks>
		/// Get k good parses for the sentence.  It is expected that the
		/// parses returned approximate the k best parses, but without any
		/// guarantee that the exact list of k best parses has been produced.
		/// If a class really provides k best parses functionality, it is
		/// reasonable to also return this output as the k good parses.
		/// </remarks>
		/// <param name="k">The number of good parses to return</param>
		/// <returns>
		/// A list of k good parses for the sentence, with
		/// each accompanied by its score
		/// </returns>
		public virtual IList<ScoredObject<Tree>> GetKGoodParses(int k)
		{
			throw new NotSupportedException("Doesn't do k good yet");
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
			throw new NotSupportedException("Doesn't do k sampled yet");
		}
	}
}
