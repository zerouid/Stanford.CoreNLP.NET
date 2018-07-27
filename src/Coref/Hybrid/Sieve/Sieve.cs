using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public abstract class Sieve
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve));

		private const long serialVersionUID = 3986463332365306868L;

		public enum ClassifierType
		{
			Rule,
			Rf,
			Oracle
		}

		public Sieve.ClassifierType classifierType = null;

		protected internal Locale lang;

		public readonly string sievename;

		/// <summary>the maximum sentence distance for linking two mentions</summary>
		public int maxSentDist = -1;

		/// <summary>type of mention we want to resolve.</summary>
		/// <remarks>type of mention we want to resolve. e.g., if mType is PRONOMINAL, we only resolve pronoun mentions</remarks>
		public readonly ICollection<Dictionaries.MentionType> mType;

		/// <summary>type of mention we want to compare to.</summary>
		/// <remarks>type of mention we want to compare to. e.g., if aType is PROPER, the resolution can be done only with PROPER antecedent</remarks>
		public readonly ICollection<Dictionaries.MentionType> aType;

		public readonly ICollection<string> mTypeStr;

		public readonly ICollection<string> aTypeStr;

		public Properties props = null;

		public Sieve()
		{
			this.lang = Locale.English;
			this.sievename = this.GetType().GetSimpleName();
			this.aType = new HashSet<Dictionaries.MentionType>(Arrays.AsList(Dictionaries.MentionType.Values()));
			this.mType = new HashSet<Dictionaries.MentionType>(Arrays.AsList(Dictionaries.MentionType.Values()));
			this.maxSentDist = 1000;
			this.mTypeStr = Generics.NewHashSet();
			this.aTypeStr = Generics.NewHashSet();
		}

		public Sieve(Properties props)
		{
			this.lang = HybridCorefProperties.GetLanguage(props);
			this.sievename = this.GetType().GetSimpleName();
			this.aType = HybridCorefProperties.GetAntecedentType(props, sievename);
			this.mType = HybridCorefProperties.GetMentionType(props, sievename);
			this.maxSentDist = HybridCorefProperties.GetMaxSentDistForSieve(props, sievename);
			this.mTypeStr = HybridCorefProperties.GetMentionTypeStr(props, sievename);
			this.aTypeStr = HybridCorefProperties.GetAntecedentTypeStr(props, sievename);
		}

		public Sieve(Properties props, string sievename)
		{
			this.lang = HybridCorefProperties.GetLanguage(props);
			this.sievename = sievename;
			this.aType = HybridCorefProperties.GetAntecedentType(props, sievename);
			this.mType = HybridCorefProperties.GetMentionType(props, sievename);
			this.maxSentDist = HybridCorefProperties.GetMaxSentDistForSieve(props, sievename);
			this.mTypeStr = HybridCorefProperties.GetMentionTypeStr(props, sievename);
			this.aTypeStr = HybridCorefProperties.GetAntecedentTypeStr(props, sievename);
		}

		/// <exception cref="System.Exception"/>
		public virtual string ResolveMention(Document document, Dictionaries dict, Properties props)
		{
			StringBuilder sbLog = new StringBuilder();
			if (HybridCorefProperties.Debug(props))
			{
				sbLog.Append("=======================================================");
				sbLog.Append(HybridCorefPrinter.PrintRawDoc(document, true, true));
			}
			foreach (IList<Mention> mentionsInSent in document.predictedMentions)
			{
				for (int mIdx = 0; mIdx < mentionsInSent.Count; mIdx++)
				{
					Mention m = mentionsInSent[mIdx];
					if (SkipMentionType(m, props))
					{
						continue;
					}
					FindCoreferentAntecedent(m, mIdx, document, dict, props, sbLog);
				}
			}
			return sbLog.ToString();
		}

		/// <exception cref="System.Exception"/>
		public abstract void FindCoreferentAntecedent(Mention m, int mIdx, Document document, Dictionaries dict, Properties props, StringBuilder sbLog);

		// load sieve (from file or make a deterministic sieve)
		/// <exception cref="System.Exception"/>
		public static Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve LoadSieve(Properties props, string sievename)
		{
			switch (HybridCorefProperties.GetClassifierType(props, sievename))
			{
				case Sieve.ClassifierType.Rule:
				{
					// log.info("Loading sieve: "+sievename+" ...");
					DeterministicCorefSieve sieve = (DeterministicCorefSieve)Sharpen.Runtime.GetType("edu.stanford.nlp.coref.hybrid.sieve." + sievename).GetConstructor().NewInstance();
					sieve.props = props;
					sieve.lang = HybridCorefProperties.GetLanguage(props);
					return sieve;
				}

				case Sieve.ClassifierType.Rf:
				{
					log.Info("Loading sieve: " + sievename + " from " + HybridCorefProperties.GetPathModel(props, sievename) + " ... ");
					RFSieve rfsieve = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(HybridCorefProperties.GetPathModel(props, sievename));
					rfsieve.thresMerge = HybridCorefProperties.GetMergeThreshold(props, sievename);
					log.Info("done. Merging threshold: " + rfsieve.thresMerge);
					return rfsieve;
				}

				case Sieve.ClassifierType.Oracle:
				{
					OracleSieve oracleSieve = new OracleSieve(props, sievename);
					oracleSieve.props = props;
					return oracleSieve;
				}

				default:
				{
					throw new Exception("no sieve type specified");
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public static IList<Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve> LoadSieves(Properties props)
		{
			IList<Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve> sieves = new List<Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve>();
			string sieveProp = HybridCorefProperties.GetSieves(props);
			string currentSieveForTrain = HybridCorefProperties.GetCurrentSieveForTrain(props);
			string[] sievenames = (currentSieveForTrain == null) ? sieveProp.Trim().Split(",\\s*") : sieveProp.Split(currentSieveForTrain)[0].Trim().Split(",\\s*");
			foreach (string sievename in sievenames)
			{
				Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve sieve = LoadSieve(props, sievename);
				sieves.Add(sieve);
			}
			return sieves;
		}

		public static bool HasThat(IList<CoreLabel> words)
		{
			foreach (CoreLabel cl in words)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(cl.Word(), "that") && Sharpen.Runtime.EqualsIgnoreCase(cl.Tag(), "IN"))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasToVerb(IList<CoreLabel> words)
		{
			for (int i = 0; i < words.Count - 1; i++)
			{
				if (words[i].Tag().Equals("TO") && words[i + 1].Tag().StartsWith("V"))
				{
					return true;
				}
			}
			return false;
		}

		private bool SkipMentionType(Mention m, Properties props)
		{
			if (mType.Contains(m.mentionType))
			{
				return false;
			}
			return true;
		}

		public static void Merge(Document document, int mID, int antID)
		{
			CorefCluster c1 = document.corefClusters[document.predictedMentionsByID[mID].corefClusterID];
			CorefCluster c2 = document.corefClusters[document.predictedMentionsByID[antID].corefClusterID];
			if (c1 == c2)
			{
				return;
			}
			int removeID = c1.GetClusterID();
			CorefCluster.MergeClusters(c2, c1);
			document.MergeIncompatibles(c2, c1);
			Sharpen.Collections.Remove(document.corefClusters, removeID);
		}

		// check if two mentions are really coref in gold annotation
		public static bool IsReallyCoref(Document document, int mID, int antID)
		{
			if (!document.goldMentionsByID.Contains(mID) || !document.goldMentionsByID.Contains(antID))
			{
				return false;
			}
			int mGoldClusterID = document.goldMentionsByID[mID].goldCorefClusterID;
			int aGoldClusterID = document.goldMentionsByID[antID].goldCorefClusterID;
			return (mGoldClusterID == aGoldClusterID);
		}

		protected internal static bool SkipForAnalysis(Mention ant, Mention m, Properties props)
		{
			if (!HybridCorefProperties.DoAnalysis(props))
			{
				return false;
			}
			string skipMentionType = HybridCorefProperties.GetSkipMentionType(props);
			string skipAntType = HybridCorefProperties.GetSkipAntecedentType(props);
			return MatchedMentionType(ant, skipAntType) && MatchedMentionType(m, skipMentionType);
		}

		protected internal static bool MatchedMentionType(Mention m, ICollection<string> types)
		{
			if (types.IsEmpty())
			{
				return true;
			}
			foreach (string type in types)
			{
				if (MatchedMentionType(m, type))
				{
					return true;
				}
			}
			return false;
		}

		protected internal static bool MatchedMentionType(Mention m, string type)
		{
			if (type == null)
			{
				return false;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "all") || Sharpen.Runtime.EqualsIgnoreCase(type, m.mentionType.ToString()))
			{
				return true;
			}
			// check pronoun specific type
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "he") && m.IsPronominal() && m.person == Dictionaries.Person.He)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "she") && m.IsPronominal() && m.person == Dictionaries.Person.She)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "you") && m.IsPronominal() && m.person == Dictionaries.Person.You)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "I") && m.IsPronominal() && m.person == Dictionaries.Person.I)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "it") && m.IsPronominal() && m.person == Dictionaries.Person.It)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "they") && m.IsPronominal() && m.person == Dictionaries.Person.They)
			{
				return true;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "we") && m.IsPronominal() && m.person == Dictionaries.Person.We)
			{
				return true;
			}
			// check named entity type
			if (type.ToLower().StartsWith("ne:"))
			{
				if (Sharpen.Runtime.Substring(type.ToLower(), 3).StartsWith(Sharpen.Runtime.Substring(m.nerString.ToLower(), 0, Math.Min(3, m.nerString.Length))))
				{
					return true;
				}
			}
			return false;
		}

		public static IList<Mention> GetOrderedAntecedents(Mention m, int antecedentSentence, int mPosition, IList<IList<Mention>> orderedMentionsBySentence, Dictionaries dict)
		{
			IList<Mention> orderedAntecedents = new List<Mention>();
			// ordering antecedents
			if (antecedentSentence == m.sentNum)
			{
				// same sentence
				Sharpen.Collections.AddAll(orderedAntecedents, orderedMentionsBySentence[m.sentNum].SubList(0, mPosition));
				if (dict.relativePronouns.Contains(m.SpanToString()))
				{
					Java.Util.Collections.Reverse(orderedAntecedents);
				}
				else
				{
					orderedAntecedents = SortMentionsByClause(orderedAntecedents, m);
				}
			}
			else
			{
				// previous sentence
				Sharpen.Collections.AddAll(orderedAntecedents, orderedMentionsBySentence[antecedentSentence]);
			}
			return orderedAntecedents;
		}

		/// <summary>Divides a sentence into clauses and sort the antecedents for pronoun matching</summary>
		private static IList<Mention> SortMentionsByClause(IList<Mention> l, Mention m1)
		{
			IList<Mention> sorted = new List<Mention>();
			Tree tree = m1.contextParseTree;
			Tree current = m1.mentionSubTree;
			if (tree == null || current == null)
			{
				return l;
			}
			while (true)
			{
				current = current.Ancestor(1, tree);
				string curLabel = current.Label().Value();
				if ("TOP".Equals(curLabel) || curLabel.StartsWith("S") || curLabel.Equals("NP"))
				{
					//      if(current.label().value().startsWith("S")){
					foreach (Mention m in l)
					{
						if (!sorted.Contains(m) && current.Dominates(m.mentionSubTree))
						{
							sorted.Add(m);
						}
					}
				}
				if (current.Ancestor(1, tree) == null)
				{
					break;
				}
			}
			return sorted;
		}
	}
}
