// MaxentTagger -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2016 Leland Stanford Junior University
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
// For more information, bug reports, fixes, contact:
// Christopher Manning
// Dept of Computer Science, Gates 2A
// Stanford CA 94305-9020
// USA
// Support/Questions: stanford-nlp on SO or java-nlp-user@lists.stanford.edu
// Licensing: java-nlp-support@lists.stanford.edu
// http://nlp.stanford.edu/software/tagger.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <author>Kristina Toutanova</author>
	/// <author>Michel Galley</author>
	/// <version>1.0</version>
	public class TestSentence : ISequenceModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.TestSentence));

		protected internal readonly bool Verbose;

		protected internal const string naTag = "NA";

		private static readonly string[] naTagArr = new string[] { naTag };

		protected internal const bool Dbg = false;

		protected internal const int kBestSize = 1;

		protected internal readonly string tagSeparator;

		protected internal readonly string encoding;

		protected internal readonly PairsHolder pairs = new PairsHolder();

		protected internal IList<string> sent;

		private IList<string> originalTags;

		protected internal IList<IHasWord> origWords;

		protected internal int size;

		private string[] correctTags;

		protected internal string[] finalTags;

		internal int numRight;

		internal int numWrong;

		internal int numUnknown;

		internal int numWrongUnknown;

		private int endSizePairs;

		private volatile History history;

		private volatile IDictionary<string, double[]> localScores = Generics.NewHashMap();

		private volatile double[][] localContextScores;

		protected internal readonly MaxentTagger maxentTagger;

		public TestSentence(MaxentTagger maxentTagger)
		{
			// origWords is only set when run with a list of HasWords; when run
			// with a list of strings, this will be null
			// TODO this always has the value of sent.size(). Remove it? [cdm 2008]
			// protected double[][][] probabilities;
			// = 0;
			System.Diagnostics.Debug.Assert((maxentTagger != null));
			System.Diagnostics.Debug.Assert((maxentTagger.GetLambdaSolve() != null));
			this.maxentTagger = maxentTagger;
			if (maxentTagger.config != null)
			{
				tagSeparator = maxentTagger.config.GetTagSeparator();
				encoding = maxentTagger.config.GetEncoding();
				Verbose = maxentTagger.config.GetVerbose();
			}
			else
			{
				tagSeparator = TaggerConfig.GetDefaultTagSeparator();
				encoding = "utf-8";
				Verbose = false;
			}
			history = new History(pairs, maxentTagger.extractors);
		}

		public virtual void SetCorrectTags<_T0>(IList<_T0> sentence)
			where _T0 : IHasTag
		{
			int len = sentence.Count;
			correctTags = new string[len];
			for (int i = 0; i < len; i++)
			{
				correctTags[i] = sentence[i].Tag();
			}
		}

		/// <summary>Tags the sentence s by running maxent model.</summary>
		/// <remarks>
		/// Tags the sentence s by running maxent model.  Returns a sentence (List) of
		/// TaggedWord objects.
		/// </remarks>
		/// <param name="s">Input sentence (List).  This isn't changed.</param>
		/// <returns>Tagged sentence</returns>
		public virtual List<TaggedWord> TagSentence<_T0>(IList<_T0> s, bool reuseTags)
			where _T0 : IHasWord
		{
			this.origWords = new List<IHasWord>(s);
			int sz = s.Count;
			this.sent = new List<string>(sz + 1);
			foreach (IHasWord value1 in s)
			{
				if (maxentTagger.wordFunction != null)
				{
					sent.Add(maxentTagger.wordFunction.Apply(value1.Word()));
				}
				else
				{
					sent.Add(value1.Word());
				}
			}
			sent.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosWord);
			if (reuseTags)
			{
				this.originalTags = new List<string>(sz + 1);
				foreach (IHasWord value in s)
				{
					if (value is IHasTag)
					{
						originalTags.Add(((IHasTag)value).Tag());
					}
					else
					{
						originalTags.Add(null);
					}
				}
				originalTags.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
			}
			size = sz + 1;
			if (Verbose)
			{
				log.Info("Sentence is " + SentenceUtils.ListToString(sent, false, tagSeparator));
			}
			Init();
			List<TaggedWord> result = TestTagInference();
			if (maxentTagger.wordFunction != null)
			{
				for (int j = 0; j < sz; ++j)
				{
					result[j].SetWord(s[j].Word());
				}
			}
			return result;
		}

		protected internal virtual void Revert(int prevSize)
		{
			endSizePairs = prevSize;
		}

		protected internal virtual void Init()
		{
			//the eos are assumed already there
			localContextScores = new double[size][];
			for (int i = 0; i < size - 1; i++)
			{
				if (maxentTagger.dict.IsUnknown(sent[i]))
				{
					numUnknown++;
				}
			}
		}

		/// <summary>Returns a string representation of the sentence.</summary>
		/// <returns>tagged sentence</returns>
		internal virtual string GetTaggedNice()
		{
			StringBuilder sb = new StringBuilder();
			// size - 1 means to exclude the EOS (end of string) symbol
			for (int i = 0; i < size - 1; i++)
			{
				sb.Append(ToNice(sent[i])).Append(tagSeparator).Append(ToNice(finalTags[i]));
				sb.Append(' ');
			}
			return sb.ToString();
		}

		internal virtual List<TaggedWord> GetTaggedSentence()
		{
			bool hasOffset;
			hasOffset = origWords != null && origWords.Count > 0 && (origWords[0] is IHasOffset);
			List<TaggedWord> taggedSentence = new List<TaggedWord>();
			for (int j = 0; j < size - 1; j++)
			{
				string tag = finalTags[j];
				TaggedWord w = new TaggedWord(sent[j], tag);
				if (hasOffset)
				{
					IHasOffset offset = (IHasOffset)origWords[j];
					w.SetBeginPosition(offset.BeginPosition());
					w.SetEndPosition(offset.EndPosition());
				}
				taggedSentence.Add(w);
			}
			return taggedSentence;
		}

		internal static string ToNice(string s)
		{
			if (s == null)
			{
				return naTag;
			}
			else
			{
				return s;
			}
		}

		/// <summary>calculateProbs puts log probs of taggings in the probabilities array.</summary>
		/// <param name="probabilities">Array with indices sent size, k best size, numTags</param>
		protected internal virtual void CalculateProbs(double[][][] probabilities)
		{
			ArrayUtils.Fill(probabilities, double.NegativeInfinity);
			for (int hyp = 0; hyp < kBestSize; hyp++)
			{
				// put the whole thing in pairs, give its beginning and end
				pairs.SetSize(size);
				for (int i = 0; i < size; i++)
				{
					pairs.SetWord(i, sent[i]);
					pairs.SetTag(i, finalTags[i]);
				}
				//pairs.add(new WordTag(sent.get(i),finalTags[i]));
				// TODO: if kBestSize > 1, use KBestSequenceFinder and save
				// k-best hypotheses into finalTags:
				//pairs.setTag(i,finalTags[i]);
				int start = endSizePairs;
				int end = endSizePairs + size - 1;
				endSizePairs = endSizePairs + size;
				// iterate over the sentence
				for (int current = 0; current < size; current++)
				{
					History h = new History(start, end, current + start, pairs, maxentTagger.extractors);
					string[] tags = StringTagsAt(h.current - h.start + LeftWindow());
					double[] probs = GetHistories(tags, h);
					ArrayMath.LogNormalize(probs);
					// log.info("word: " + pairs.getWord(current));
					// log.info("tags: " + Arrays.asList(tags));
					// log.info("probs: " + ArrayMath.toString(probs));
					for (int j = 0; j < tags.Length; j++)
					{
						// score the j-th tag
						string tag = tags[j];
						bool approximate = maxentTagger.HasApproximateScoring();
						int tagindex = approximate ? maxentTagger.tags.GetIndex(tag) : j;
						// log.info("Mapped from j="+ j + " " + tag + " to " + tagindex);
						probabilities[current][hyp][tagindex] = probs[j];
					}
				}
			}
			// for current
			// for hyp
			// clean up the stuff in PairsHolder (added by cdm in Aug 2008)
			Revert(0);
		}

		// end calculateProbs()
		/// <summary>
		/// Write the tagging and note any errors (if pf != null) and accumulate
		/// global statistics.
		/// </summary>
		/// <param name="finalTags">Chosen tags for sentence</param>
		/// <param name="pf">
		/// File to write tagged output to (can be null, then no output;
		/// at present it is non-null iff the debug property is set)
		/// </param>
		protected internal virtual void WriteTagsAndErrors(string[] finalTags, PrintFile pf, bool verboseResults)
		{
			StringWriter sw = new StringWriter(200);
			for (int i = 0; i < correctTags.Length; i++)
			{
				sw.Write(ToNice(sent[i]));
				sw.Write(tagSeparator);
				sw.Write(finalTags[i]);
				sw.Write(' ');
				if (pf != null)
				{
					pf.Write(ToNice(sent[i]));
					pf.Write(tagSeparator);
					pf.Write(finalTags[i]);
				}
				if ((correctTags[i]).Equals(finalTags[i]))
				{
					numRight++;
				}
				else
				{
					numWrong++;
					if (pf != null)
					{
						pf.Write('|' + correctTags[i]);
					}
					if (verboseResults)
					{
						EncodingPrintWriter.Err.Println((maxentTagger.dict.IsUnknown(sent[i]) ? "Unk" : string.Empty) + "Word: " + sent[i] + "; correct: " + correctTags[i] + "; guessed: " + finalTags[i], encoding);
					}
					if (maxentTagger.dict.IsUnknown(sent[i]))
					{
						numWrongUnknown++;
						if (pf != null)
						{
							pf.Write("*");
						}
					}
				}
				// if
				// else
				if (pf != null)
				{
					pf.Write(' ');
				}
			}
			// for
			if (pf != null)
			{
				pf.WriteLine();
			}
			if (verboseResults)
			{
				PrintWriter pw;
				try
				{
					pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
				}
				catch (UnsupportedEncodingException)
				{
					pw = new PrintWriter(new OutputStreamWriter(System.Console.Out), true);
				}
				pw.Println(sw);
			}
		}

		/// <summary>Update a confusion matrix with the errors from this sentence.</summary>
		/// <param name="finalTags">Chosen tags for sentence</param>
		/// <param name="confusionMatrix">Confusion matrix to write to</param>
		protected internal virtual void UpdateConfusionMatrix(string[] finalTags, ConfusionMatrix<string> confusionMatrix)
		{
			for (int i = 0; i < correctTags.Length; i++)
			{
				confusionMatrix.Add(finalTags[i], correctTags[i]);
			}
		}

		/// <summary>Test using (exact Viterbi) TagInference.</summary>
		/// <returns>The tagged sentence</returns>
		private List<TaggedWord> TestTagInference()
		{
			RunTagInference();
			return GetTaggedSentence();
		}

		private void RunTagInference()
		{
			this.InitializeScorer();
			if (Thread.Interrupted())
			{
				// Allow interrupting
				throw new RuntimeInterruptedException();
			}
			IBestSequenceFinder ti = new ExactBestSequenceFinder();
			//new BeamBestSequenceFinder(50);
			//new KBestSequenceFinder()
			int[] bestTags = ti.BestSequence(this);
			finalTags = new string[bestTags.Length];
			for (int j = 0; j < size; j++)
			{
				finalTags[j] = maxentTagger.tags.GetTag(bestTags[j + LeftWindow()]);
			}
			if (Thread.Interrupted())
			{
				// Allow interrupting
				throw new RuntimeInterruptedException();
			}
			CleanUpScorer();
		}

		// This is used for Dan's tag inference methods.
		// current is the actual word number + leftW
		private void SetHistory(int current, History h, int[] tags)
		{
			//writes over the tags in the last thing in pairs
			int left = LeftWindow();
			int right = RightWindow();
			for (int j = current - left; j <= current + right; j++)
			{
				if (j < left)
				{
					continue;
				}
				//but shouldn't happen
				if (j >= size + left)
				{
					break;
				}
				//but shouldn't happen
				h.SetTag(j - left, maxentTagger.tags.GetTag(tags[j]));
			}
		}

		// do initializations for the TagScorer interface
		protected internal virtual void InitializeScorer()
		{
			pairs.SetSize(size);
			for (int i = 0; i < size; i++)
			{
				pairs.SetWord(i, sent[i]);
			}
			endSizePairs += size;
		}

		/// <summary>clean-up after the scorer</summary>
		protected internal virtual void CleanUpScorer()
		{
			Revert(0);
		}

		// This scores the current assignment in PairsHolder at
		// current position h.current (returns normalized scores)
		private double[] GetScores(History h)
		{
			if (maxentTagger.HasApproximateScoring())
			{
				return GetApproximateScores(h);
			}
			return GetExactScores(h);
		}

		private double[] GetExactScores(History h)
		{
			string[] tags = StringTagsAt(h.current - h.start + LeftWindow());
			double[] histories = GetHistories(tags, h);
			// log score for each tag
			ArrayMath.LogNormalize(histories);
			double[] scores = new double[tags.Length];
			for (int j = 0; j < tags.Length; j++)
			{
				// score the j-th tag
				string tag = tags[j];
				int tagindex = maxentTagger.tags.GetIndex(tag);
				scores[j] = histories[tagindex];
			}
			return scores;
		}

		// In this method, each tag that is incompatible with the current word
		// (e.g., apple_CC) gets a default (constant) score instead of its exact score.
		// The scores of all other tags are computed exactly.
		private double[] GetApproximateScores(History h)
		{
			string[] tags = StringTagsAt(h.current - h.start + LeftWindow());
			double[] scores = GetHistories(tags, h);
			// log score for each active tag, unnormalized
			// Number of tags that get assigned a default score:
			int nDefault = maxentTagger.ySize - tags.Length;
			double logScore = ArrayMath.LogSum(scores);
			double logScoreInactiveTags = maxentTagger.GetInactiveTagDefaultScore(nDefault);
			double logTotal = SloppyMath.LogAdd(logScore, logScoreInactiveTags);
			ArrayMath.AddInPlace(scores, -logTotal);
			return scores;
		}

		// This precomputes scores of local features (localScores).
		protected internal virtual double[] GetHistories(string[] tags, History h)
		{
			bool rare = maxentTagger.IsRare(ExtractorFrames.cWord.Extract(h));
			Extractors ex = maxentTagger.extractors;
			Extractors exR = maxentTagger.extractorsRare;
			string w = pairs.GetWord(h.current);
			double[] lS;
			double[] lcS;
			lS = localScores[w];
			if (lS == null)
			{
				lS = GetHistories(tags, h, ex.local, rare ? exR.local : null);
				localScores[w] = lS;
			}
			else
			{
				if (lS.Length != tags.Length)
				{
					// This case can occur when a word was given a specific forced
					// tag, and then later it shows up without the forced tag.
					// TODO: if a word is given a forced tag, we should always get
					// its features rather than use the cache, just in case the tag
					// given is not the same tag as before
					lS = GetHistories(tags, h, ex.local, rare ? exR.local : null);
					if (tags.Length > 1)
					{
						localScores[w] = lS;
					}
				}
			}
			if ((lcS = localContextScores[h.current]) == null)
			{
				lcS = GetHistories(tags, h, ex.localContext, rare ? exR.localContext : null);
				localContextScores[h.current] = lcS;
				ArrayMath.PairwiseAddInPlace(lcS, lS);
			}
			double[] totalS = GetHistories(tags, h, ex.dynamic, rare ? exR.dynamic : null);
			ArrayMath.PairwiseAddInPlace(totalS, lcS);
			return totalS;
		}

		private double[] GetHistories(string[] tags, History h, IList<Pair<int, Extractor>> extractors, IList<Pair<int, Extractor>> extractorsRare)
		{
			if (maxentTagger.HasApproximateScoring())
			{
				return GetApproximateHistories(tags, h, extractors, extractorsRare);
			}
			return GetExactHistories(h, extractors, extractorsRare);
		}

		private double[] GetExactHistories(History h, IList<Pair<int, Extractor>> extractors, IList<Pair<int, Extractor>> extractorsRare)
		{
			double[] scores = new double[maxentTagger.ySize];
			int szCommon = maxentTagger.extractors.Size();
			foreach (Pair<int, Extractor> e in extractors)
			{
				int kf = e.First();
				Extractor ex = e.Second();
				string val = ex.Extract(h);
				int[] fAssociations = maxentTagger.fAssociations[kf][val];
				if (fAssociations != null)
				{
					for (int i = 0; i < maxentTagger.ySize; i++)
					{
						int fNum = fAssociations[i];
						if (fNum > -1)
						{
							scores[i] += maxentTagger.GetLambdaSolve().lambda[fNum];
						}
					}
				}
			}
			if (extractorsRare != null)
			{
				foreach (Pair<int, Extractor> e_1 in extractorsRare)
				{
					int kf = e_1.First();
					Extractor ex = e_1.Second();
					string val = ex.Extract(h);
					int[] fAssociations = maxentTagger.fAssociations[kf + szCommon][val];
					if (fAssociations != null)
					{
						for (int i = 0; i < maxentTagger.ySize; i++)
						{
							int fNum = fAssociations[i];
							if (fNum > -1)
							{
								scores[i] += maxentTagger.GetLambdaSolve().lambda[fNum];
							}
						}
					}
				}
			}
			return scores;
		}

		// todo [cdm 2016]: Could this be sped up a bit by caching lambda array, extracting method for shared code?
		// todo [cdm 2016]: Also it's allocating java.util.ArrayList$Itr for for loop - why can't it just random access array?
		/// <summary>Returns an unnormalized score (in log space) for each tag.</summary>
		private double[] GetApproximateHistories(string[] tags, History h, IList<Pair<int, Extractor>> extractors, IList<Pair<int, Extractor>> extractorsRare)
		{
			double[] scores = new double[tags.Length];
			int szCommon = maxentTagger.extractors.Size();
			foreach (Pair<int, Extractor> e in extractors)
			{
				int kf = e.First();
				Extractor ex = e.Second();
				string val = ex.Extract(h);
				int[] fAssociations = maxentTagger.fAssociations[kf][val];
				if (fAssociations != null)
				{
					for (int j = 0; j < tags.Length; j++)
					{
						string tag = tags[j];
						int tagIndex = maxentTagger.tags.GetIndex(tag);
						int fNum = fAssociations[tagIndex];
						if (fNum > -1)
						{
							scores[j] += maxentTagger.GetLambdaSolve().lambda[fNum];
						}
					}
				}
			}
			if (extractorsRare != null)
			{
				foreach (Pair<int, Extractor> e_1 in extractorsRare)
				{
					int kf = e_1.First();
					Extractor ex = e_1.Second();
					string val = ex.Extract(h);
					int[] fAssociations = maxentTagger.fAssociations[szCommon + kf][val];
					if (fAssociations != null)
					{
						for (int j = 0; j < tags.Length; j++)
						{
							string tag = tags[j];
							int tagIndex = maxentTagger.tags.GetIndex(tag);
							int fNum = fAssociations[tagIndex];
							if (fNum > -1)
							{
								scores[j] += maxentTagger.GetLambdaSolve().lambda[fNum];
							}
						}
					}
				}
			}
			return scores;
		}

		/// <summary>This method should be called after the sentence has been tagged.</summary>
		/// <remarks>
		/// This method should be called after the sentence has been tagged.
		/// For every unknown word, this method prints the 3 most probable tags
		/// to the file pfu.
		/// </remarks>
		/// <param name="numSent">The sentence number</param>
		/// <param name="pfu">The file to print the probable tags to</param>
		internal virtual void PrintUnknown(int numSent, PrintFile pfu)
		{
			NumberFormat nf = new DecimalFormat("0.0000");
			int numTags = maxentTagger.NumTags();
			double[][][] probabilities = new double[size][][];
			CalculateProbs(probabilities);
			for (int current = 0; current < size; current++)
			{
				if (maxentTagger.dict.IsUnknown(sent[current]))
				{
					pfu.Write(sent[current]);
					pfu.Write(':');
					pfu.Write(numSent);
					double[] probs = new double[3];
					string[] tag3 = new string[3];
					GetTop3(probabilities, current, probs, tag3);
					for (int i = 0; i < 3; i++)
					{
						if (probs[i] > double.NegativeInfinity)
						{
							pfu.Write('\t');
							pfu.Write(tag3[i]);
							pfu.Write(' ');
							pfu.Write(nf.Format(System.Math.Exp(probs[i])));
						}
					}
					int rank;
					string correctTag = ToNice(this.correctTags[current]);
					for (rank = 0; rank < 3; rank++)
					{
						if (correctTag.Equals(tag3[rank]))
						{
							break;
						}
					}
					//if
					pfu.Write('\t');
					switch (rank)
					{
						case 0:
						{
							pfu.Write("Correct");
							break;
						}

						case 1:
						{
							pfu.Write("2nd");
							break;
						}

						case 2:
						{
							pfu.Write("3rd");
							break;
						}

						default:
						{
							pfu.Write("Not top 3");
							break;
						}
					}
					pfu.WriteLine();
				}
			}
		}

		// if
		// for
		// This method should be called after a sentence has been tagged.
		// For every word token, this method prints the 3 most probable tags
		// to the file pfu except for
		internal virtual void PrintTop(PrintFile pfu)
		{
			NumberFormat nf = new DecimalFormat("0.0000");
			int numTags = maxentTagger.NumTags();
			double[][][] probabilities = new double[size][][];
			CalculateProbs(probabilities);
			for (int current = 0; current < correctTags.Length; current++)
			{
				pfu.Write(sent[current]);
				double[] probs = new double[3];
				string[] tag3 = new string[3];
				GetTop3(probabilities, current, probs, tag3);
				for (int i = 0; i < 3; i++)
				{
					if (probs[i] > double.NegativeInfinity)
					{
						pfu.Write('\t');
						pfu.Write(tag3[i]);
						pfu.Write(' ');
						pfu.Write(nf.Format(System.Math.Exp(probs[i])));
					}
				}
				int rank;
				string correctTag = ToNice(this.correctTags[current]);
				for (rank = 0; rank < 3; rank++)
				{
					if (correctTag.Equals(tag3[rank]))
					{
						break;
					}
				}
				//if
				pfu.Write('\t');
				switch (rank)
				{
					case 0:
					{
						pfu.Write("Correct");
						break;
					}

					case 1:
					{
						pfu.Write("2nd");
						break;
					}

					case 2:
					{
						pfu.Write("3rd");
						break;
					}

					default:
					{
						pfu.Write("Not top 3");
						break;
					}
				}
				pfu.WriteLine();
			}
		}

		// for
		/// <summary>
		/// probs and tags should be passed in as arrays of size 3!
		/// If probs[i] == Double.NEGATIVE_INFINITY, then the entry should be ignored.
		/// </summary>
		private void GetTop3(double[][][] probabilities, int current, double[] probs, string[] tags)
		{
			int[] topIds = new int[3];
			double[] probTags = probabilities[current][0];
			Arrays.Fill(probs, double.NegativeInfinity);
			for (int i = 0; i < probTags.Length; i++)
			{
				if (probTags[i] > probs[0])
				{
					probs[2] = probs[1];
					probs[1] = probs[0];
					probs[0] = probTags[i];
					topIds[2] = topIds[1];
					topIds[1] = topIds[0];
					topIds[0] = i;
				}
				else
				{
					if (probTags[i] > probs[1])
					{
						probs[2] = probs[1];
						probs[1] = probTags[i];
						topIds[2] = topIds[1];
						topIds[1] = i;
					}
					else
					{
						if (probTags[i] > probs[2])
						{
							probs[2] = probTags[i];
							topIds[2] = i;
						}
					}
				}
			}
			for (int j = 0; j < 3; j++)
			{
				tags[j] = ToNice(maxentTagger.tags.GetTag(topIds[j]));
			}
		}

		/*
		* Implementation of the TagScorer interface follows
		*/
		public virtual int Length()
		{
			return sent.Count;
		}

		public virtual int LeftWindow()
		{
			return maxentTagger.leftContext;
		}

		//hard-code for now
		public virtual int RightWindow()
		{
			return maxentTagger.rightContext;
		}

		//hard code for now
		public virtual int[] GetPossibleValues(int pos)
		{
			string[] arr1 = StringTagsAt(pos);
			int[] arr = new int[arr1.Length];
			for (int i = 0; i < arr.Length; i++)
			{
				arr[i] = maxentTagger.tags.GetIndex(arr1[i]);
			}
			return arr;
		}

		public virtual double ScoreOf(int[] tags, int pos)
		{
			double[] scores = ScoresOf(tags, pos);
			double score = double.NegativeInfinity;
			int[] pv = GetPossibleValues(pos);
			for (int i = 0; i < scores.Length; i++)
			{
				if (pv[i] == tags[pos])
				{
					score = scores[i];
				}
			}
			return score;
		}

		public virtual double ScoreOf(int[] sequence)
		{
			throw new NotSupportedException();
		}

		public virtual double[] ScoresOf(int[] tags, int pos)
		{
			history.Init(endSizePairs - size, endSizePairs - 1, endSizePairs - size + pos - LeftWindow());
			SetHistory(pos, history, tags);
			return GetScores(history);
		}

		// todo [cdm 2013]: Tagging could be sped up quite a bit here if we cached int arrays of tags by index, not Strings
		protected internal virtual string[] StringTagsAt(int pos)
		{
			if ((pos < LeftWindow()) || (pos >= size + LeftWindow()))
			{
				return naTagArr;
			}
			string[] arr1;
			if (originalTags != null && originalTags[pos - LeftWindow()] != null)
			{
				arr1 = new string[1];
				arr1[0] = originalTags[pos - LeftWindow()];
				return arr1;
			}
			string word = sent[pos - LeftWindow()];
			if (maxentTagger.dict.IsUnknown(word))
			{
				ICollection<string> open = maxentTagger.tags.GetOpenTags();
				// todo: really want array of String or int here
				arr1 = Sharpen.Collections.ToArray(open, new string[open.Count]);
			}
			else
			{
				arr1 = maxentTagger.dict.GetTags(word);
			}
			arr1 = maxentTagger.tags.DeterministicallyExpandTags(arr1);
			return arr1;
		}
	}
}
