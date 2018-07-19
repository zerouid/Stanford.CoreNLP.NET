using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseWordFeatureExtractor : IWordFeatureExtractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseWordFeatureExtractor));

		private const long serialVersionUID = -4327267414095852504L;

		internal bool morpho;

		internal bool chars;

		internal bool rads;

		internal bool useLength;

		internal bool useFreq;

		internal bool bigrams;

		internal bool conjunctions;

		internal bool mildConjunctions;

		public bool turnOffWordFeatures = false;

		private IntCounter wordCounter;

		private ChineseMorphFeatureSets cmfs = null;

		private const string featureDir = "gbfeatures";

		public virtual void SetFeatureLevel(int level)
		{
			morpho = false;
			chars = false;
			rads = false;
			useLength = false;
			useFreq = false;
			bigrams = false;
			conjunctions = false;
			mildConjunctions = false;
			switch (level)
			{
				case 3:
				{
					bigrams = true;
					conjunctions = true;
					goto case 2;
				}

				case 2:
				{
					chars = true;
					goto case 1;
				}

				case 1:
				{
					morpho = true;
					mildConjunctions = true;
					LoadFeatures();
					goto case 0;
				}

				case 0:
				{
					rads = true;
					goto case -1;
				}

				case -1:
				{
					useLength = true;
					useFreq = true;
					break;
				}

				default:
				{
					log.Info("Feature level " + level + " is not supported in ChineseWordFeatureExtractor.");
					log.Info("Using level 0");
					SetFeatureLevel(0);
					break;
				}
			}
		}

		public ChineseWordFeatureExtractor(int featureLevel)
		{
			/*
			public ChineseWordFeatureExtractor() {
			this(trees, 2);
			}
			*/
			wordCounter = new IntCounter();
			SetFeatureLevel(featureLevel);
		}

		public virtual void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		public virtual void Train(Tree tree, double weight)
		{
			Train(tree.TaggedYield(), weight);
		}

		public virtual void Train(IList<TaggedWord> sentence, double weight)
		{
			foreach (TaggedWord word in sentence)
			{
				string wordString = word.Word();
				wordCounter.IncrementCount(wordString, weight);
			}
		}

		private void LoadFeatures()
		{
			if (cmfs != null)
			{
				return;
			}
			cmfs = new ChineseMorphFeatureSets(featureDir);
			log.Info("Total affix features: " + cmfs.GetAffixFeatures().Count);
		}

		private ICollection<string> threshedFeatures;

		public virtual void ApplyFeatureCountThreshold(ICollection<string> data, int thresh)
		{
			IntCounter c = new IntCounter();
			foreach (string datum in data)
			{
				foreach (string feat in MakeFeatures(datum))
				{
					c.IncrementCount(feat);
				}
			}
			threshedFeatures = c.KeysAbove(thresh);
			log.Info((c.Size() - threshedFeatures.Count) + " word features removed due to thresholding.");
		}

		public virtual ICollection<string> MakeFeatures(string word)
		{
			IList<string> features = new List<string>();
			if (morpho)
			{
				foreach (KeyValuePair<string, ICollection<char>> e in cmfs.GetSingletonFeatures())
				{
					if (e.Value.Contains(word[0]))
					{
						features.Add(e.Key + "-1");
					}
				}
				// Hooray for generics!!! :-)
				foreach (KeyValuePair<string, Pair<ICollection<char>, ICollection<char>>> e_1 in cmfs.GetAffixFeatures())
				{
					bool both = false;
					if (e_1.Value.First().Contains(word[0]))
					{
						features.Add(e_1.Key + "-P");
						both = true;
					}
					if (e_1.Value.Second().Contains(word[word.Length - 1]))
					{
						features.Add(e_1.Key + "-S");
					}
					else
					{
						both = false;
					}
					if (both && mildConjunctions && !conjunctions)
					{
						features.Add(e_1.Key + "-PS");
					}
				}
				if (conjunctions)
				{
					int max = features.Count;
					for (int i = 1; i < max; i++)
					{
						string s1 = features[i];
						for (int j = 0; j < i; j++)
						{
							string s2 = features[j];
							features.Add(s1 + "&&" + s2);
						}
					}
				}
			}
			if (!turnOffWordFeatures)
			{
				features.Add(word + "-W");
			}
			if (rads)
			{
				features.Add(RadicalMap.GetRadical(word[0]) + "-FR");
				features.Add(RadicalMap.GetRadical(word[word.Length - 1]) + "-LR");
				for (int i = 0; i < word.Length; i++)
				{
					features.Add(RadicalMap.GetRadical(word[i]) + "-CR");
				}
			}
			if (chars)
			{
				// first and last chars
				features.Add(word[0] + "-FC");
				features.Add(word[word.Length - 1] + "-LC");
				for (int i = 0; i < word.Length; i++)
				{
					features.Add(word[i] + "-CC");
				}
				if (bigrams && word.Length > 1)
				{
					features.Add(Sharpen.Runtime.Substring(word, 0, 2) + "-FB");
					features.Add(Sharpen.Runtime.Substring(word, word.Length - 2) + "-LB");
					for (int i_1 = 2; i_1 <= word.Length; i_1++)
					{
						features.Add(Sharpen.Runtime.Substring(word, i_1 - 2, i_1) + "-CB");
					}
				}
			}
			if (useLength)
			{
				int lengthBin = word.Length;
				if (lengthBin >= 5)
				{
					if (lengthBin >= 8)
					{
						lengthBin = 8;
					}
					else
					{
						lengthBin = 5;
					}
				}
				features.Add(word.Length + "-L");
			}
			if (useFreq && !turnOffWordFeatures)
			{
				int freq = wordCounter.GetIntCount(word);
				int freqBin;
				if (freq <= 1)
				{
					freqBin = 0;
				}
				else
				{
					if (freq <= 3)
					{
						freqBin = 1;
					}
					else
					{
						if (freq <= 6)
						{
							freqBin = 2;
						}
						else
						{
							if (freq <= 15)
							{
								freqBin = 3;
							}
							else
							{
								if (freq <= 50)
								{
									freqBin = 4;
								}
								else
								{
									freqBin = 5;
								}
							}
						}
					}
				}
				features.Add(freqBin + "-FQ");
			}
			features.Add("PR");
			if (threshedFeatures != null)
			{
				for (IEnumerator<string> iter = features.GetEnumerator(); iter.MoveNext(); )
				{
					string s = iter.Current;
					if (!threshedFeatures.Contains(s))
					{
						iter.Remove();
					}
				}
			}
			return features;
		}
	}
}
