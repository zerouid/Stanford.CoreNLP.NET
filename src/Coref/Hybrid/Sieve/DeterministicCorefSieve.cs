//
// StanfordCoreNLP -- a suite of NLP tools
// Copyright (c) 2009-2010 The Board of Trustees of
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
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	/// <summary>Base class for a Coref Sieve.</summary>
	/// <remarks>
	/// Base class for a Coref Sieve.
	/// Each sieve extends this class, and set flags for its own options in the constructor.
	/// </remarks>
	/// <author>heeyoung</author>
	/// <author>mihais</author>
	[System.Serializable]
	public abstract class DeterministicCorefSieve : Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Hybrid.Sieve.DeterministicCorefSieve));

		public readonly DcorefSieveOptions flags;

		public DeterministicCorefSieve()
			: base()
		{
			this.classifierType = Sieve.ClassifierType.Rule;
			flags = new DcorefSieveOptions();
		}

		public DeterministicCorefSieve(Properties props)
			: base(props)
		{
			this.classifierType = Sieve.ClassifierType.Rule;
			flags = new DcorefSieveOptions();
		}

		/// <exception cref="System.Exception"/>
		public override void FindCoreferentAntecedent(Mention m, int mIdx, Document document, Dictionaries dict, Properties props, StringBuilder sbLog)
		{
			// check for skip: first mention only, discourse salience
			if (!this.flags.UseSpeakermatch && !this.flags.UseDiscoursematch && !this.flags.UseApposition && !this.flags.UsePredicatenominatives && this.SkipThisMention(document, m, document.corefClusters[m.corefClusterID], dict))
			{
				return;
			}
			ICollection<Mention> roleSet = document.roleSet;
			for (int sentJ = m.sentNum; sentJ >= 0; sentJ--)
			{
				IList<Mention> l = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.GetOrderedAntecedents(m, sentJ, mIdx, document.predictedMentions, dict);
				if (maxSentDist != -1 && m.sentNum - sentJ > maxSentDist)
				{
					continue;
				}
				// TODO: do we need this?
				// Sort mentions by length whenever we have two mentions beginning at the same position and having the same head
				for (int i = 0; i < l.Count; i++)
				{
					for (int j = 0; j < l.Count; j++)
					{
						if (l[i].headString.Equals(l[j].headString) && l[i].startIndex == l[j].startIndex && l[i].SameSentence(l[j]) && j > i && l[i].SpanToString().Length > l[j].SpanToString().Length)
						{
							l.Set(j, l.Set(i, l[j]));
						}
					}
				}
				//              log.info("antecedent ordering changed!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
				foreach (Mention ant in l)
				{
					if (SkipForAnalysis(ant, m, props))
					{
						continue;
					}
					// m2 - antecedent of m1
					// Skip singletons according to the singleton predictor
					// (only for non-NE mentions)
					// Recasens, de Marneffe, and Potts (NAACL 2013)
					if (m.isSingleton && m.mentionType != Dictionaries.MentionType.Proper && ant.isSingleton && ant.mentionType != Dictionaries.MentionType.Proper)
					{
						continue;
					}
					if (m.corefClusterID == ant.corefClusterID)
					{
						continue;
					}
					if (!mType.Contains(m.mentionType) || !aType.Contains(ant.mentionType))
					{
						continue;
					}
					if (m.mentionType == Dictionaries.MentionType.Pronominal)
					{
						if (!MatchedMentionType(m, mTypeStr))
						{
							continue;
						}
						if (!MatchedMentionType(ant, aTypeStr))
						{
							continue;
						}
					}
					CorefCluster c1 = document.corefClusters[m.corefClusterID];
					CorefCluster c2 = document.corefClusters[ant.corefClusterID];
					System.Diagnostics.Debug.Assert((c1 != null));
					System.Diagnostics.Debug.Assert((c2 != null));
					if (this.UseRoleSkip())
					{
						if (m.IsRoleAppositive(ant, dict))
						{
							roleSet.Add(m);
						}
						else
						{
							if (ant.IsRoleAppositive(m, dict))
							{
								roleSet.Add(ant);
							}
						}
						continue;
					}
					if (this.Coreferent(document, c1, c2, m, ant, dict, roleSet))
					{
						// print logs for analysis
						//            if (doScore()) {
						//              printLogs(c1, c2, m1, m2, document, currentSieve);
						//            }
						// print dcoref log
						if (HybridCorefProperties.Debug(props))
						{
							sbLog.Append(HybridCorefPrinter.PrintErrorLogDcoref(m, ant, document, dict, mIdx, this.GetType().FullName));
						}
						int removeID = c1.clusterID;
						//          log.info("Merging ant "+c2+" with "+c1);
						CorefCluster.MergeClusters(c2, c1);
						document.MergeIncompatibles(c2, c1);
						document.MergeAcronymCache(c2, c1);
						//            log.warning("Removing cluster " + removeID + ", merged with " + c2.getClusterID());
						Sharpen.Collections.Remove(document.corefClusters, removeID);
						return;
					}
				}
			}
		}

		// End of "LOOP"
		public virtual string FlagsToString()
		{
			return flags.ToString();
		}

		public virtual bool UseRoleSkip()
		{
			return flags.UseRoleSkip;
		}

		/// <summary>Skip this mention? (search pruning)</summary>
		public virtual bool SkipThisMention(Document document, Mention m1, CorefCluster c, Dictionaries dict)
		{
			bool skip = false;
			// only do for the first mention in its cluster
			//    if(!flags.USE_EXACTSTRINGMATCH && !flags.USE_ROLEAPPOSITION && !flags.USE_PREDICATENOMINATIVES
			if (!flags.UseRoleapposition && !flags.UsePredicatenominatives && !flags.UseAcronym && !flags.UseApposition && !flags.UseRelativepronoun && !c.GetFirstMention().Equals(m1))
			{
				// CHINESE CHANGE
				return true;
			}
			if (m1.appositions == null && m1.predicateNominatives == null && (m1.LowercaseNormalizedSpanString().StartsWith("a ") || m1.LowercaseNormalizedSpanString().StartsWith("an ")) && !flags.UseExactstringmatch)
			{
				skip = true;
			}
			// A noun phrase starting with an indefinite article - unlikely to have an antecedent (e.g. "A commission" was set up to .... )
			if (dict.indefinitePronouns.Contains(m1.LowercaseNormalizedSpanString()))
			{
				skip = true;
			}
			// An indefinite pronoun - unlikely to have an antecedent (e.g. "Some" say that... )
			foreach (string indef in dict.indefinitePronouns)
			{
				if (m1.LowercaseNormalizedSpanString().StartsWith(indef + " "))
				{
					skip = true;
					// A noun phrase starting with an indefinite adjective - unlikely to have an antecedent (e.g. "Another opinion" on the topic is...)
					break;
				}
			}
			return skip;
		}

		public virtual bool CheckEntityMatch(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict, ICollection<Mention> roleSet)
		{
			return false;
		}

		/// <summary>Checks if two clusters are coreferent according to our sieve pass constraints</summary>
		/// <param name="document"/>
		/// <exception cref="System.Exception"/>
		public virtual bool Coreferent(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention mention2, Mention ant, Dictionaries dict, ICollection<Mention> roleSet)
		{
			bool ret = false;
			Mention mention = mentionCluster.GetRepresentativeMention();
			if (flags.UseIncompatibles)
			{
				// Check our list of incompatible mentions and don't cluster them together
				// Allows definite no's from previous sieves to propagate down
				if (document.IsIncompatible(mentionCluster, potentialAntecedent))
				{
					return false;
				}
			}
			if (flags.DoPronoun && Math.Abs(mention2.sentNum - ant.sentNum) > 3 && mention2.person != Dictionaries.Person.I && mention2.person != Dictionaries.Person.You)
			{
				return false;
			}
			if (mention2.LowercaseNormalizedSpanString().Equals("this") && Math.Abs(mention2.sentNum - ant.sentNum) > 3)
			{
				return false;
			}
			if (mention2.person == Dictionaries.Person.You && document.docType == Document.DocType.Article && mention2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals("PER0"))
			{
				return false;
			}
			if (document.conllDoc != null)
			{
				if (ant.generic && ant.person == Dictionaries.Person.You)
				{
					return false;
				}
				if (mention2.generic)
				{
					return false;
				}
			}
			// chinese newswire contains coref nested NPs with shared headword  Chen & Ng
			if (lang != Locale.Chinese || document.docInfo == null || !document.docInfo.GetOrDefault("DOC_ID", string.Empty).Contains("nw"))
			{
				if (mention2.InsideIn(ant) || ant.InsideIn(mention2))
				{
					return false;
				}
			}
			if (flags.UseSpeakermatch)
			{
				string mSpeaker = mention2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				string aSpeaker = ant.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				// <I> from same speaker
				if (mention2.person == Dictionaries.Person.I && ant.person == Dictionaries.Person.I)
				{
					return (mSpeaker.Equals(aSpeaker));
				}
				// <I> - speaker
				if ((mention2.person == Dictionaries.Person.I && mSpeaker.Equals(int.ToString(ant.mentionID))) || (ant.person == Dictionaries.Person.I && aSpeaker.Equals(int.ToString(mention2.mentionID))))
				{
					return true;
				}
			}
			if (flags.UseDiscoursematch)
			{
				string mString = mention.LowercaseNormalizedSpanString();
				string antString = ant.LowercaseNormalizedSpanString();
				// mention and ant both belong to the same speaker cluster
				if (mention.speakerInfo != null && mention.speakerInfo == ant.speakerInfo)
				{
					return true;
				}
				// (I - I) in the same speaker's quotation.
				if (mention.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString) && ant.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString) && CorefRules.EntitySameSpeaker(document, mention, 
					ant))
				{
					return true;
				}
				// (speaker - I)
				if ((mention.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString)) && CorefRules.AntecedentIsMentionSpeaker(document, mention, ant, dict))
				{
					if (mention.speakerInfo == null && ant.speakerInfo != null)
					{
						mention.speakerInfo = ant.speakerInfo;
					}
					return true;
				}
				// (I - speaker)
				if ((ant.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString)) && CorefRules.AntecedentIsMentionSpeaker(document, ant, mention, dict))
				{
					if (ant.speakerInfo == null && mention.speakerInfo != null)
					{
						ant.speakerInfo = mention.speakerInfo;
					}
					return true;
				}
				// Can be iffy if more than two speakers... but still should be okay most of the time
				if (dict.secondPersonPronouns.Contains(mString) && dict.secondPersonPronouns.Contains(antString) && CorefRules.EntitySameSpeaker(document, mention, ant))
				{
					return true;
				}
				// previous I - you or previous you - I in two person conversation
				if (((mention.person == Dictionaries.Person.I && ant.person == Dictionaries.Person.You || (mention.person == Dictionaries.Person.You && ant.person == Dictionaries.Person.I)) && (mention.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation
					)) - ant.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) == 1) && document.docType == Document.DocType.Conversation))
				{
					return true;
				}
				if (dict.reflexivePronouns.Contains(mention.headString) && CorefRules.EntitySubjectObject(mention, ant))
				{
					return true;
				}
			}
			if (!flags.UseExactstringmatch && !flags.UseRelaxedExactstringmatch && !flags.UseApposition && !flags.UseWordsInclusion)
			{
				foreach (Mention m in mentionCluster.GetCorefMentions())
				{
					foreach (Mention a in potentialAntecedent.GetCorefMentions())
					{
						// angelx - not sure about the logic here, disable (code was also refactored from original)
						// vv gabor - re-enabled code (seems to improve performance) vv
						if (m.person != Dictionaries.Person.I && a.person != Dictionaries.Person.I && (CorefRules.AntecedentIsMentionSpeaker(document, m, a, dict) || CorefRules.AntecedentIsMentionSpeaker(document, a, m, dict)))
						{
							document.AddIncompatible(m, a);
							return false;
						}
						// ^^ end block of code in question ^^
						int dist = Math.Abs(m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) - a.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)));
						if (document.docType != Document.DocType.Article && dist == 1 && !CorefRules.EntitySameSpeaker(document, m, a))
						{
							string mSpeaker = document.speakers[m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))];
							string aSpeaker = document.speakers[a.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))];
							if (m.person == Dictionaries.Person.I && a.person == Dictionaries.Person.I)
							{
								document.AddIncompatible(m, a);
								return false;
							}
							if (m.person == Dictionaries.Person.You && a.person == Dictionaries.Person.You)
							{
								document.AddIncompatible(m, a);
								return false;
							}
							// This is weak since we can refer to both speakers
							if (m.person == Dictionaries.Person.We && a.person == Dictionaries.Person.We)
							{
								document.AddIncompatible(m, a);
								return false;
							}
						}
					}
				}
				if (document.docType == Document.DocType.Article)
				{
					foreach (Mention m_1 in mentionCluster.GetCorefMentions())
					{
						foreach (Mention a in potentialAntecedent.GetCorefMentions())
						{
							if (CorefRules.EntitySubjectObject(m_1, a))
							{
								document.AddIncompatible(m_1, a);
								return false;
							}
						}
					}
				}
			}
			// Incompatibility constraints - do before match checks
			if (flags.USE_iwithini && CorefRules.EntityIWithinI(mention, ant, dict))
			{
				document.AddIncompatible(mention, ant);
				return false;
			}
			// Match checks
			if (flags.UseExactstringmatch && CorefRules.EntityExactStringMatch(mention, ant, dict, roleSet))
			{
				return true;
			}
			//    if(flags.USE_EXACTSTRINGMATCH && Rules.entityExactStringMatch(mentionCluster, potentialAntecedent, dict, roleSet)){
			//      return true;
			//    }
			if (flags.UseNameMatch && CheckEntityMatch(document, mentionCluster, potentialAntecedent, dict, roleSet))
			{
				ret = true;
			}
			if (flags.UseRelaxedExactstringmatch && CorefRules.EntityRelaxedExactStringMatch(mentionCluster, potentialAntecedent, mention, ant, dict, roleSet))
			{
				return true;
			}
			if (flags.UseApposition && CorefRules.EntityIsApposition(mentionCluster, potentialAntecedent, mention, ant))
			{
				return true;
			}
			if (flags.UsePredicatenominatives && CorefRules.EntityIsPredicateNominatives(mentionCluster, potentialAntecedent, mention, ant))
			{
				return true;
			}
			if (flags.UseAcronym && CorefRules.EntityIsAcronym(document, mentionCluster, potentialAntecedent))
			{
				return true;
			}
			if (flags.UseRelativepronoun && CorefRules.EntityIsRelativePronoun(mention, ant))
			{
				return true;
			}
			if (flags.UseDemonym && mention.IsDemonym(ant, dict))
			{
				return true;
			}
			if (flags.UseRoleapposition)
			{
				if (lang == Locale.Chinese)
				{
					ret = false;
				}
				else
				{
					if (CorefRules.EntityIsRoleAppositive(mentionCluster, potentialAntecedent, mention, ant, dict))
					{
						ret = true;
					}
				}
			}
			if (flags.UseInclusionHeadmatch && CorefRules.EntityHeadsAgree(mentionCluster, potentialAntecedent, mention, ant, dict))
			{
				ret = true;
			}
			if (flags.UseRelaxedHeadmatch && CorefRules.EntityRelaxedHeadsAgreeBetweenMentions(mentionCluster, potentialAntecedent, mention, ant))
			{
				ret = true;
			}
			if (flags.UseWordsInclusion && ret && !CorefRules.EntityWordsIncluded(mentionCluster, potentialAntecedent, mention, ant))
			{
				return false;
			}
			if (flags.UseIncompatibleModifier && ret && CorefRules.EntityHaveIncompatibleModifier(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if (flags.UseProperheadAtLast && ret && !CorefRules.EntitySameProperHeadLastWord(mentionCluster, potentialAntecedent, mention, ant))
			{
				return false;
			}
			if (flags.UseAttributesAgree && !CorefRules.EntityAttributesAgree(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if (flags.UseDifferentLocation && CorefRules.EntityHaveDifferentLocation(mention, ant, dict))
			{
				if (flags.UseProperheadAtLast && ret && mention.goldCorefClusterID != ant.goldCorefClusterID)
				{
				}
				return false;
			}
			if (flags.UseNumberInMention && CorefRules.EntityNumberInLaterMention(mention, ant))
			{
				if (flags.UseProperheadAtLast && ret && mention.goldCorefClusterID != ant.goldCorefClusterID)
				{
				}
				return false;
			}
			if (flags.UseDistance && CorefRules.EntityTokenDistance(mention2, ant))
			{
				return false;
			}
			if (flags.UseCorefDict)
			{
				// Head match
				if (ant.headWord.Lemma().Equals(mention2.headWord.Lemma()))
				{
					return false;
				}
				// Constraint: ignore pairs commonNoun - properNoun
				if (ant.mentionType != Dictionaries.MentionType.Proper && (mention2.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP") || !Sharpen.Runtime.Substring(mention2.headWord.Word(), 1).Equals(Sharpen.Runtime.Substring(mention2
					.headWord.Word(), 1).ToLower())))
				{
					return false;
				}
				// Constraint: ignore plurals
				if (ant.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("NNS") && mention2.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("NNS"))
				{
					return false;
				}
				// Constraint: ignore mentions with indefinite determiners
				if (dict.indefinitePronouns.Contains(ant.originalSpan[0].Lemma()) || dict.indefinitePronouns.Contains(mention2.originalSpan[0].Lemma()))
				{
					return false;
				}
				// Constraint: ignore coordinated mentions
				if (ant.IsCoordinated() || mention2.IsCoordinated())
				{
					return false;
				}
				// Constraint: context incompatibility
				if (CorefRules.ContextIncompatible(mention2, ant, dict))
				{
					return false;
				}
				// Constraint: sentence context incompatibility when the mentions are common nouns
				if (CorefRules.SentenceContextIncompatible(mention2, ant, dict))
				{
					return false;
				}
				if (CorefRules.EntityClusterAllCorefDictionary(mentionCluster, potentialAntecedent, dict, 1, 8))
				{
					return true;
				}
				if (CorefRules.EntityCorefDictionary(mention, ant, dict, 2, 2))
				{
					return true;
				}
				if (CorefRules.EntityCorefDictionary(mention, ant, dict, 3, 2))
				{
					return true;
				}
				if (CorefRules.EntityCorefDictionary(mention, ant, dict, 4, 2))
				{
					return true;
				}
			}
			if (flags.DoPronoun)
			{
				Mention m;
				if (mention.predicateNominatives != null && mention.predicateNominatives.Contains(mention2))
				{
					m = mention2;
				}
				else
				{
					m = mention;
				}
				bool mIsPronoun = (m.IsPronominal() || dict.allPronouns.Contains(m.ToString()));
				bool attrAgree = HybridCorefProperties.UseDefaultPronounAgreement(props) ? CorefRules.EntityAttributesAgree(mentionCluster, potentialAntecedent) : CorefRules.EntityAttributesAgree(mentionCluster, potentialAntecedent, lang);
				if (mIsPronoun && attrAgree)
				{
					if (dict.demonymSet.Contains(ant.LowercaseNormalizedSpanString()) && dict.notOrganizationPRP.Contains(m.headString))
					{
						document.AddIncompatible(m, ant);
						return false;
					}
					if (CorefRules.EntityPersonDisagree(document, mentionCluster, potentialAntecedent, dict))
					{
						document.AddIncompatible(m, ant);
						return false;
					}
					return true;
				}
			}
			if (flags.UseChineseHeadMatch)
			{
				if (mention2.headWord == ant.headWord && mention2.InsideIn(ant))
				{
					if (!document.IsCoref(mention2, ant))
					{
					}
					// TODO: exclude conjunction
					// log.info("error in chinese head match: "+mention2.spanToString()+"\t"+ant.spanToString());
					return true;
				}
			}
			return ret;
		}

		/// <summary>Orders the antecedents for the given mention (m1)</summary>
		/// <param name="antecedentSentence"/>
		/// <param name="mySentence"/>
		/// <param name="orderedMentions"/>
		/// <param name="orderedMentionsBySentence"/>
		/// <param name="m1"/>
		/// <param name="m1Position"/>
		/// <param name="corefClusters"/>
		/// <param name="dict"/>
		/// <returns>An ordering of potential antecedents depending on same/different sentence, etc.</returns>
		public virtual IList<Mention> GetOrderedAntecedents(int antecedentSentence, int mySentence, IList<Mention> orderedMentions, IList<IList<Mention>> orderedMentionsBySentence, Mention m1, int m1Position, IDictionary<int, CorefCluster> corefClusters
			, Dictionaries dict)
		{
			IList<Mention> orderedAntecedents = new List<Mention>();
			// ordering antecedents
			if (antecedentSentence == mySentence)
			{
				// same sentence
				Sharpen.Collections.AddAll(orderedAntecedents, orderedMentions.SubList(0, m1Position));
				if (flags.DoPronoun && m1.IsPronominal())
				{
					// TODO
					orderedAntecedents = SortMentionsForPronoun(orderedAntecedents, m1);
				}
				if (dict.relativePronouns.Contains(m1.SpanToString()))
				{
					Java.Util.Collections.Reverse(orderedAntecedents);
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
		private static IList<Mention> SortMentionsForPronoun(IList<Mention> l, Mention m1)
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
				if (current.Label().Value().StartsWith("S"))
				{
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
			if (l.Count != sorted.Count)
			{
				sorted = l;
			}
			else
			{
				if (!l.Equals(sorted))
				{
					for (int i = 0; i < l.Count; i++)
					{
						Mention ml = l[i];
						Mention msorted = sorted[i];
					}
				}
			}
			return sorted;
		}
	}
}
