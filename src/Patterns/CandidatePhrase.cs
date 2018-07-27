using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;




namespace Edu.Stanford.Nlp.Patterns
{
	/// <author>Sonal Gupta on 11/7/14.</author>
	[System.Serializable]
	public class CandidatePhrase : IComparable
	{
		private readonly string phrase;

		private string phraseLemma;

		private ICounter<string> features;

		private readonly int hashCode;

		private const long serialVersionUID = 42L;

		private static readonly ConcurrentHashMap<string, Edu.Stanford.Nlp.Patterns.CandidatePhrase> candidatePhraseMap = new ConcurrentHashMap<string, Edu.Stanford.Nlp.Patterns.CandidatePhrase>();

		// static void setCandidatePhraseMap(ConcurrentHashMap<String, CandidatePhrase> candmap){
		//  candidatePhraseMap = candmap;
		// }
		public static Edu.Stanford.Nlp.Patterns.CandidatePhrase CreateOrGet(string phrase)
		{
			phrase = phrase.Trim();
			if (candidatePhraseMap.Contains(phrase))
			{
				return candidatePhraseMap[phrase];
			}
			else
			{
				Edu.Stanford.Nlp.Patterns.CandidatePhrase p = new Edu.Stanford.Nlp.Patterns.CandidatePhrase(phrase);
				candidatePhraseMap[phrase] = p;
				return p;
			}
		}

		public static Edu.Stanford.Nlp.Patterns.CandidatePhrase CreateOrGet(string phrase, string phraseLemma)
		{
			phrase = phrase.Trim();
			if (candidatePhraseMap.Contains(phrase))
			{
				Edu.Stanford.Nlp.Patterns.CandidatePhrase p = candidatePhraseMap[phrase];
				p.phraseLemma = phraseLemma;
				return p;
			}
			else
			{
				Edu.Stanford.Nlp.Patterns.CandidatePhrase p = new Edu.Stanford.Nlp.Patterns.CandidatePhrase(phrase, phraseLemma);
				candidatePhraseMap[phrase] = p;
				return p;
			}
		}

		public static Edu.Stanford.Nlp.Patterns.CandidatePhrase CreateOrGet(string phrase, string phraseLemma, ICounter<string> features)
		{
			phrase = phrase.Trim();
			if (candidatePhraseMap.Contains(phrase))
			{
				Edu.Stanford.Nlp.Patterns.CandidatePhrase p = candidatePhraseMap[phrase];
				p.phraseLemma = phraseLemma;
				//If features are non-empty, add to the current set
				if (features != null && features.Size() > 0)
				{
					if (p.features == null)
					{
						p.features = new ClassicCounter<string>();
					}
					p.features.AddAll(features);
				}
				return p;
			}
			else
			{
				Edu.Stanford.Nlp.Patterns.CandidatePhrase p = new Edu.Stanford.Nlp.Patterns.CandidatePhrase(phrase, phraseLemma, features);
				candidatePhraseMap[phrase] = p;
				return p;
			}
		}

		private CandidatePhrase(string phrase, string lemma)
			: this(phrase, lemma, null)
		{
		}

		private CandidatePhrase(string phrase, string lemma, ICounter<string> features)
		{
			if (phrase.IsEmpty())
			{
				Sharpen.Runtime.PrintStackTrace(new Exception("Creating empty candidatePhrase"), System.Console.Out);
			}
			this.phrase = phrase;
			this.phraseLemma = lemma;
			this.features = features;
			this.hashCode = phrase.GetHashCode();
		}

		private CandidatePhrase(string w)
			: this(w, null, null)
		{
		}

		public virtual string GetPhrase()
		{
			return phrase;
		}

		public virtual string GetPhraseLemma()
		{
			return phraseLemma;
		}

		public virtual double GetFeatureValue(string feat)
		{
			return features.GetCount(feat);
		}

		public override string ToString()
		{
			return phrase;
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Patterns.CandidatePhrase))
			{
				return false;
			}
			return this.hashCode == o.GetHashCode();
		}

		public virtual int CompareTo(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Patterns.CandidatePhrase))
			{
				return -1;
			}
			else
			{
				return string.CompareOrdinal(((Edu.Stanford.Nlp.Patterns.CandidatePhrase)o).GetPhrase(), this.GetPhrase());
			}
		}

		public override int GetHashCode()
		{
			return hashCode;
		}

		public static IList<Edu.Stanford.Nlp.Patterns.CandidatePhrase> ConvertStringPhrases(ICollection<string> str)
		{
			IList<Edu.Stanford.Nlp.Patterns.CandidatePhrase> phs = new List<Edu.Stanford.Nlp.Patterns.CandidatePhrase>();
			foreach (string s in str)
			{
				phs.Add(Edu.Stanford.Nlp.Patterns.CandidatePhrase.CreateOrGet(s));
			}
			return phs;
		}

		public static IList<string> ConvertToString(ICollection<Edu.Stanford.Nlp.Patterns.CandidatePhrase> words)
		{
			IList<string> phs = new List<string>();
			foreach (Edu.Stanford.Nlp.Patterns.CandidatePhrase ph in words)
			{
				phs.Add(ph.GetPhrase());
			}
			return phs;
		}

		public virtual ICounter<string> GetFeatures()
		{
			return features;
		}

		public virtual void AddFeature(string s, double v)
		{
			if (features == null)
			{
				features = new ClassicCounter<string>();
			}
			features.SetCount(s, v);
		}

		public virtual void AddFeatures(ICollection<string> feat)
		{
			if (features == null)
			{
				features = new ClassicCounter<string>();
			}
			Counters.AddInPlace(features, feat);
		}

		public virtual void SetPhraseLemma(string phraseLemma)
		{
			this.phraseLemma = phraseLemma;
		}

		public static void DeletePhrase(Edu.Stanford.Nlp.Patterns.CandidatePhrase p)
		{
			Sharpen.Collections.Remove(candidatePhraseMap, p);
		}
	}
}
