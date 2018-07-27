using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid.RF;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Coref.MD
{
	[System.Serializable]
	public class MentionDetectionClassifier
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.MD.MentionDetectionClassifier));

		private const long serialVersionUID = -4100580709477023158L;

		public RandomForest rf;

		public MentionDetectionClassifier(RandomForest rf)
		{
			this.rf = rf;
		}

		public static ICounter<string> ExtractFeatures(Mention p, ICollection<Mention> shares, ICollection<string> neStrings, Dictionaries dict, Properties props)
		{
			ICounter<string> features = new ClassicCounter<string>();
			string span = p.LowercaseNormalizedSpanString();
			string ner = p.headWord.Ner();
			int sIdx = p.startIndex;
			int eIdx = p.endIndex;
			IList<CoreLabel> sent = p.sentenceWords;
			CoreLabel preWord = (sIdx == 0) ? null : sent[sIdx - 1];
			CoreLabel nextWord = (eIdx == sent.Count) ? null : sent[eIdx];
			CoreLabel firstWord = p.originalSpan[0];
			CoreLabel lastWord = p.originalSpan[p.originalSpan.Count - 1];
			features.IncrementCount("B-NETYPE-" + ner);
			if (neStrings.Contains(span))
			{
				features.IncrementCount("B-NE-STRING-EXIST");
				if ((preWord == null || !preWord.Ner().Equals(ner)) && (nextWord == null || !nextWord.Ner().Equals(ner)))
				{
					features.IncrementCount("B-NE-FULLSPAN");
				}
			}
			if (preWord != null)
			{
				features.IncrementCount("B-PRECEDINGWORD-" + preWord.Word());
			}
			if (nextWord != null)
			{
				features.IncrementCount("B-FOLLOWINGWORD-" + nextWord.Word());
			}
			if (preWord != null)
			{
				features.IncrementCount("B-PRECEDINGPOS-" + preWord.Tag());
			}
			if (nextWord != null)
			{
				features.IncrementCount("B-FOLLOWINGPOS-" + nextWord.Tag());
			}
			features.IncrementCount("B-FIRSTWORD-" + firstWord.Word());
			features.IncrementCount("B-FIRSTPOS-" + firstWord.Tag());
			features.IncrementCount("B-LASTWORD-" + lastWord.Word());
			features.IncrementCount("B-LASTWORD-" + lastWord.Tag());
			foreach (Mention s in shares)
			{
				if (s == p)
				{
					continue;
				}
				if (s.InsideIn(p))
				{
					features.IncrementCount("B-BIGGER-THAN-ANOTHER");
					break;
				}
			}
			foreach (Mention s_1 in shares)
			{
				if (s_1 == p)
				{
					continue;
				}
				if (p.InsideIn(s_1))
				{
					features.IncrementCount("B-SMALLER-THAN-ANOTHER");
					break;
				}
			}
			return features;
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public static Edu.Stanford.Nlp.Coref.MD.MentionDetectionClassifier LoadMentionDetectionClassifier(string filename)
		{
			log.Info("loading MentionDetectionClassifier ...");
			Edu.Stanford.Nlp.Coref.MD.MentionDetectionClassifier mdc = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(filename);
			log.Info("done");
			return mdc;
		}

		public virtual double ProbabilityOf(Mention p, ICollection<Mention> shares, ICollection<string> neStrings, Dictionaries dict, Properties props)
		{
			try
			{
				bool dummyLabel = false;
				RVFDatum<bool, string> datum = new RVFDatum<bool, string>(ExtractFeatures(p, shares, neStrings, dict, props), dummyLabel);
				return rf.ProbabilityOfTrue(datum);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public virtual void ClassifyMentions(IList<IList<Mention>> predictedMentions, Dictionaries dict, Properties props)
		{
			ICollection<string> neStrings = Generics.NewHashSet();
			foreach (IList<Mention> predictedMention in predictedMentions)
			{
				foreach (Mention m in predictedMention)
				{
					string ne = m.headWord.Ner();
					if (ne.Equals("O"))
					{
						continue;
					}
					foreach (CoreLabel cl in m.originalSpan)
					{
						if (!cl.Ner().Equals(ne))
						{
							continue;
						}
					}
					neStrings.Add(m.LowercaseNormalizedSpanString());
				}
			}
			foreach (IList<Mention> predicts in predictedMentions)
			{
				IDictionary<int, ICollection<Mention>> headPositions = Generics.NewHashMap();
				foreach (Mention p in predicts)
				{
					if (!headPositions.Contains(p.headIndex))
					{
						headPositions[p.headIndex] = Generics.NewHashSet();
					}
					headPositions[p.headIndex].Add(p);
				}
				ICollection<Mention> remove = Generics.NewHashSet();
				foreach (int hPos in headPositions.Keys)
				{
					ICollection<Mention> shares = headPositions[hPos];
					if (shares.Count > 1)
					{
						ICounter<Mention> probs = new ClassicCounter<Mention>();
						foreach (Mention p_1 in shares)
						{
							double trueProb = ProbabilityOf(p_1, shares, neStrings, dict, props);
							probs.IncrementCount(p_1, trueProb);
						}
						// add to remove
						Mention keep = Counters.Argmax(probs, null);
						probs.Remove(keep);
						Sharpen.Collections.AddAll(remove, probs.KeySet());
					}
				}
				foreach (Mention r in remove)
				{
					predicts.Remove(r);
				}
			}
		}
	}
}
