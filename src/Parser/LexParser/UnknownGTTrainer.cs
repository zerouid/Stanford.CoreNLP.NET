using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class trains a Good-Turing model for unknown words from a
	/// collection of trees.
	/// </summary>
	/// <remarks>
	/// This class trains a Good-Turing model for unknown words from a
	/// collection of trees.  It builds up a map of statistics which can be
	/// used by any UnknownWordModel which wants to use the GT model.
	/// Authors:
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Greg Donaker (corrections and modeling improvements)</author>
	/// <author>Christopher Manning (generalized and improved what Greg did)</author>
	/// <author>Anna Rafferty</author>
	/// <author>John Bauer (refactored into a separate training class)</author>
	public class UnknownGTTrainer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(UnknownGTTrainer));

		internal ClassicCounter<Pair<string, string>> wtCount = new ClassicCounter<Pair<string, string>>();

		internal ClassicCounter<string> tagCount = new ClassicCounter<string>();

		internal ClassicCounter<string> r1 = new ClassicCounter<string>();

		internal ClassicCounter<string> r0 = new ClassicCounter<string>();

		internal ICollection<string> seenWords = Generics.NewHashSet();

		internal double tokens = 0;

		internal IDictionary<string, float> unknownGT = Generics.NewHashMap();

		// for each tag, # of words seen once
		// for each tag, # of words not seen
		public virtual void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree t in trees)
			{
				Train(t, weight);
			}
		}

		public virtual void Train(Tree tree, double weight)
		{
			/* get TaggedWord and total tag counts, and get set of all
			* words attested in training
			*/
			foreach (TaggedWord word in tree.TaggedYield())
			{
				Train(word, weight);
			}
		}

		public virtual void Train(TaggedWord tw, double weight)
		{
			tokens = tokens + weight;
			string word = tw.Word();
			string tag = tw.Tag();
			// TaggedWord has crummy equality conditions
			Pair<string, string> wt = new Pair<string, string>(word, tag);
			wtCount.IncrementCount(wt, weight);
			tagCount.IncrementCount(tag, weight);
			seenWords.Add(word);
		}

		public virtual void FinishTraining()
		{
			// testing: get some stats here
			log.Info("Total tokens: " + tokens);
			log.Info("Total WordTag types: " + wtCount.KeySet().Count);
			log.Info("Total tag types: " + tagCount.KeySet().Count);
			log.Info("Total word types: " + seenWords.Count);
			/* find # of once-seen words for each tag */
			foreach (Pair<string, string> wt in wtCount.KeySet())
			{
				if (wtCount.GetCount(wt) == 1)
				{
					r1.IncrementCount(wt.Second());
				}
			}
			/* find # of unseen words for each tag */
			foreach (string tag in tagCount.KeySet())
			{
				foreach (string word in seenWords)
				{
					Pair<string, string> wt_1 = new Pair<string, string>(word, tag);
					if (!(wtCount.KeySet().Contains(wt_1)))
					{
						r0.IncrementCount(tag);
					}
				}
			}
			/* set unseen word probability for each tag */
			foreach (string tag_1 in tagCount.KeySet())
			{
				float logprob = (float)Math.Log(r1.GetCount(tag_1) / (tagCount.GetCount(tag_1) * r0.GetCount(tag_1)));
				unknownGT[tag_1] = float.ValueOf(logprob);
			}
		}
	}
}
