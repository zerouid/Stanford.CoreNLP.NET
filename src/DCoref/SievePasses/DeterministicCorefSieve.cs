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
using System.Reflection;
using Edu.Stanford.Nlp.Dcoref;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	/// <summary>Base class for a Coref Sieve.</summary>
	/// <remarks>
	/// Base class for a Coref Sieve.
	/// Each sieve extends this class, and set flags for its own options in the constructor.
	/// </remarks>
	/// <author>heeyoung</author>
	/// <author>mihais</author>
	public abstract class DeterministicCorefSieve
	{
		public readonly SieveOptions flags;

		protected internal Locale lang;

		/// <summary>Initialize flagSet</summary>
		public DeterministicCorefSieve()
		{
			flags = new SieveOptions();
		}

		public virtual void Init(Properties props)
		{
			lang = Locale.ForLanguageTag(props.GetProperty(Constants.LanguageProp, "en"));
		}

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
			if (!flags.UseExactstringmatch && !flags.UseRoleapposition && !flags.UsePredicatenominatives && !flags.UseAcronym && !flags.UseApposition && !flags.UseRelativepronoun && !c.GetFirstMention().Equals(m1))
			{
				return true;
			}
			SieveCoreferenceSystem.logger.Finest("DOING COREF FOR:\t" + m1.SpanToString());
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
			if (skip)
			{
				SieveCoreferenceSystem.logger.Finest("MENTION SKIPPED:\t" + m1.SpanToString() + "(" + m1.sentNum + ")" + "\toriginalRef: " + m1.originalRef + " in discourse " + m1.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)));
			}
			return skip;
		}

		public virtual bool CheckEntityMatch(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict, ICollection<Mention> roleSet)
		{
			return false;
		}

		/// <summary>Checks if two clusters are coreferent according to our sieve pass constraints.</summary>
		/// <param name="document"/>
		/// <exception cref="System.Exception"/>
		public virtual bool Coreferent(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention mention2, Mention ant, Dictionaries dict, ICollection<Mention> roleSet, Semantics semantics)
		{
			bool ret = false;
			Mention mention = mentionCluster.GetRepresentativeMention();
			if (flags.UseIncompatibles)
			{
				// Check our list of incompatible mentions and don't cluster them together
				// Allows definite no's from previous sieves to propagate down
				if (document.IsIncompatible(mentionCluster, potentialAntecedent))
				{
					SieveCoreferenceSystem.logger.Finest("INCOMPATIBLE clusters: not match: " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID != ant.goldCorefClusterID
						));
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
			if (mention2.InsideIn(ant) || ant.InsideIn(mention2))
			{
				return false;
			}
			if (flags.UseDiscoursematch)
			{
				string mString = mention.LowercaseNormalizedSpanString();
				string antString = ant.LowercaseNormalizedSpanString();
				// mention and ant both belong to the same speaker cluster
				if (mention.speakerInfo != null && mention.speakerInfo == ant.speakerInfo)
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: maps to same speaker: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					return true;
				}
				// (I - I) in the same speaker's quotation.
				if (mention.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString) && ant.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString) && Rules.EntitySameSpeaker(document, mention, ant))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: 1st person same speaker: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					return true;
				}
				// (speaker - I)
				if ((mention.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString)) && Rules.AntecedentIsMentionSpeaker(document, mention, ant, dict))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: 1st person mention speaker match antecedent: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					if (mention.speakerInfo == null && ant.speakerInfo != null)
					{
						mention.speakerInfo = ant.speakerInfo;
					}
					return true;
				}
				// (I - speaker)
				if ((ant.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString)) && Rules.AntecedentIsMentionSpeaker(document, ant, mention, dict))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: 1st person antecedent speaker match mention: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					if (ant.speakerInfo == null && mention.speakerInfo != null)
					{
						ant.speakerInfo = mention.speakerInfo;
					}
					return true;
				}
				// Can be iffy if more than two speakers... but still should be okay most of the time
				if (dict.secondPersonPronouns.Contains(mString) && dict.secondPersonPronouns.Contains(antString) && Rules.EntitySameSpeaker(document, mention, ant))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: 2nd person same speaker: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					return true;
				}
				// previous I - you or previous you - I in two person conversation
				if (((mention.person == Dictionaries.Person.I && ant.person == Dictionaries.Person.You || (mention.person == Dictionaries.Person.You && ant.person == Dictionaries.Person.I)) && (mention.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation
					)) - ant.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) == 1) && document.docType == Document.DocType.Conversation))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: between two person: " + mention.SpanToString() + "\tmatched\t" + ant.SpanToString());
					return true;
				}
				if (dict.reflexivePronouns.Contains(mention.headString) && Rules.EntitySubjectObject(mention, ant))
				{
					SieveCoreferenceSystem.logger.Finest("discourse match: reflexive pronoun: " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID == ant.goldCorefClusterID
						));
					return true;
				}
			}
			if (Constants.UseDiscourseConstraints && !flags.UseExactstringmatch && !flags.UseRelaxedExactstringmatch && !flags.UseApposition && !flags.UseWordsInclusion)
			{
				foreach (Mention m in mentionCluster.GetCorefMentions())
				{
					foreach (Mention a in potentialAntecedent.GetCorefMentions())
					{
						// angelx - not sure about the logic here, disable (code was also refactored from original)
						// vv gabor - re-enabled code (seems to improve performance) vv
						if (m.person != Dictionaries.Person.I && a.person != Dictionaries.Person.I && (Rules.AntecedentIsMentionSpeaker(document, m, a, dict) || Rules.AntecedentIsMentionSpeaker(document, a, m, dict)))
						{
							SieveCoreferenceSystem.logger.Finest("Incompatibles: not match(speaker): " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID != ant.goldCorefClusterID
								));
							document.AddIncompatible(m, a);
							return false;
						}
						// ^^ end block of code in question ^^
						int dist = Math.Abs(m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) - a.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)));
						if (document.docType != Document.DocType.Article && dist == 1 && !Rules.EntitySameSpeaker(document, m, a))
						{
							string mSpeaker = document.speakers[m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))];
							string aSpeaker = document.speakers[a.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))];
							if (m.person == Dictionaries.Person.I && a.person == Dictionaries.Person.I)
							{
								SieveCoreferenceSystem.logger.Finest("Incompatibles: neighbor I: " + ant.SpanToString() + "(" + ant.mentionID + "," + aSpeaker + ") :: " + mention.SpanToString() + "(" + mention.mentionID + "," + mSpeaker + ") -> " + (mention.goldCorefClusterID
									 != ant.goldCorefClusterID));
								document.AddIncompatible(m, a);
								return false;
							}
							if (m.person == Dictionaries.Person.You && a.person == Dictionaries.Person.You)
							{
								SieveCoreferenceSystem.logger.Finest("Incompatibles: neighbor YOU: " + ant.SpanToString() + "(" + ant.mentionID + "," + aSpeaker + ") :: " + mention.SpanToString() + "(" + mention.mentionID + "," + mSpeaker + ") -> " + (mention.goldCorefClusterID
									 != ant.goldCorefClusterID));
								document.AddIncompatible(m, a);
								return false;
							}
							// This is weak since we can refer to both speakers
							if (m.person == Dictionaries.Person.We && a.person == Dictionaries.Person.We)
							{
								SieveCoreferenceSystem.logger.Finest("Incompatibles: neighbor WE: " + ant.SpanToString() + "(" + ant.mentionID + "," + aSpeaker + ") :: " + mention.SpanToString() + "(" + mention.mentionID + "," + mSpeaker + ") -> " + (mention.goldCorefClusterID
									 != ant.goldCorefClusterID));
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
							if (Rules.EntitySubjectObject(m_1, a))
							{
								SieveCoreferenceSystem.logger.Finest("Incompatibles: subject-object: " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID != ant.goldCorefClusterID)
									);
								document.AddIncompatible(m_1, a);
								return false;
							}
						}
					}
				}
			}
			// Incompatibility constraints - do before match checks
			if (flags.USE_iwithini && Rules.EntityIWithinI(mention, ant, dict))
			{
				SieveCoreferenceSystem.logger.Finest("Incompatibles: iwithini: " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID != ant.goldCorefClusterID));
				document.AddIncompatible(mention, ant);
				return false;
			}
			// Match checks
			if (flags.UseExactstringmatch && Rules.EntityExactStringMatch(mentionCluster, potentialAntecedent, dict, roleSet))
			{
				return true;
			}
			if (flags.UseNameMatch && CheckEntityMatch(document, mentionCluster, potentialAntecedent, dict, roleSet))
			{
				ret = true;
			}
			if (flags.UseRelaxedExactstringmatch && Rules.EntityRelaxedExactStringMatch(mentionCluster, potentialAntecedent, mention, ant, dict, roleSet))
			{
				return true;
			}
			if (flags.UseApposition && Rules.EntityIsApposition(mentionCluster, potentialAntecedent, mention, ant))
			{
				SieveCoreferenceSystem.logger.Finest("Apposition: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				return true;
			}
			if (flags.UsePredicatenominatives && Rules.EntityIsPredicateNominatives(mentionCluster, potentialAntecedent, mention, ant))
			{
				SieveCoreferenceSystem.logger.Finest("Predicate nominatives: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				return true;
			}
			if (flags.UseAcronym && Rules.EntityIsAcronym(document, mentionCluster, potentialAntecedent))
			{
				SieveCoreferenceSystem.logger.Finest("Acronym: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				return true;
			}
			if (flags.UseRelativepronoun && Rules.EntityIsRelativePronoun(mention, ant))
			{
				SieveCoreferenceSystem.logger.Finest("Relative pronoun: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				return true;
			}
			if (flags.UseDemonym && mention.IsDemonym(ant, dict))
			{
				SieveCoreferenceSystem.logger.Finest("Demonym: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				return true;
			}
			if (flags.UseRoleapposition && lang != Locale.Chinese && Rules.EntityIsRoleAppositive(mentionCluster, potentialAntecedent, mention, ant, dict))
			{
				SieveCoreferenceSystem.logger.Finest("Role Appositive: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				ret = true;
			}
			if (flags.UseInclusionHeadmatch && Rules.EntityHeadsAgree(mentionCluster, potentialAntecedent, mention, ant, dict))
			{
				SieveCoreferenceSystem.logger.Finest("Entity heads agree: " + mention.SpanToString() + "\tvs\t" + ant.SpanToString());
				ret = true;
			}
			if (flags.UseRelaxedHeadmatch && Rules.EntityRelaxedHeadsAgreeBetweenMentions(mentionCluster, potentialAntecedent, mention, ant))
			{
				ret = true;
			}
			if (flags.UseWordsInclusion && ret && !Rules.EntityWordsIncluded(mentionCluster, potentialAntecedent, mention, ant))
			{
				return false;
			}
			if (flags.UseIncompatibleModifier && ret && Rules.EntityHaveIncompatibleModifier(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if (flags.UseProperheadAtLast && ret && !Rules.EntitySameProperHeadLastWord(mentionCluster, potentialAntecedent, mention, ant))
			{
				return false;
			}
			if (flags.UseAttributesAgree && !Rules.EntityAttributesAgree(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if (flags.UseDifferentLocation && Rules.EntityHaveDifferentLocation(mention, ant, dict))
			{
				if (flags.UseProperheadAtLast && ret && mention.goldCorefClusterID != ant.goldCorefClusterID)
				{
					SieveCoreferenceSystem.logger.Finest("DIFFERENT LOCATION: " + ant.SpanToString() + " :: " + mention.SpanToString());
				}
				return false;
			}
			if (flags.UseNumberInMention && Rules.EntityNumberInLaterMention(mention, ant))
			{
				if (flags.UseProperheadAtLast && ret && mention.goldCorefClusterID != ant.goldCorefClusterID)
				{
					SieveCoreferenceSystem.logger.Finest("NEW NUMBER : " + ant.SpanToString() + " :: " + mention.SpanToString());
				}
				return false;
			}
			if (flags.UseWnHypernym)
			{
				MethodInfo meth = semantics.wordnet.GetType().GetMethod("checkHypernym", typeof(CorefCluster), typeof(CorefCluster), typeof(Mention), typeof(Mention));
				if ((bool)meth.Invoke(semantics.wordnet, mentionCluster, potentialAntecedent, mention, ant))
				{
					ret = true;
				}
				else
				{
					if (mention.goldCorefClusterID == ant.goldCorefClusterID && !mention.IsPronominal() && !ant.IsPronominal())
					{
						SieveCoreferenceSystem.logger.Finest("not hypernym in WN");
						SieveCoreferenceSystem.logger.Finest("False Negatives:: " + ant.SpanToString() + " <= " + mention.SpanToString());
					}
				}
			}
			if (flags.UseWnSynonym)
			{
				MethodInfo meth = semantics.wordnet.GetType().GetMethod("checkSynonym", new Type[] { typeof(Mention), typeof(Mention) });
				if ((bool)meth.Invoke(semantics.wordnet, mention, ant))
				{
					ret = true;
				}
				else
				{
					if (mention.goldCorefClusterID == ant.goldCorefClusterID && !mention.IsPronominal() && !ant.IsPronominal())
					{
						SieveCoreferenceSystem.logger.Finest("not synonym in WN");
						SieveCoreferenceSystem.logger.Finest("False Negatives:: " + ant.SpanToString() + " <= " + mention.SpanToString());
					}
				}
			}
			try
			{
				if (flags.UseAlias && Rules.EntityAlias(mentionCluster, potentialAntecedent, semantics, dict))
				{
					return true;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			if (flags.UseDistance && Rules.EntityTokenDistance(mention2, ant))
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
				if (Rules.ContextIncompatible(mention2, ant, dict))
				{
					return false;
				}
				// Constraint: sentence context incompatibility when the mentions are common nouns
				if (Rules.SentenceContextIncompatible(mention2, ant, dict))
				{
					return false;
				}
				if (Rules.EntityClusterAllCorefDictionary(mentionCluster, potentialAntecedent, dict, 1, 8))
				{
					return true;
				}
				if (Rules.EntityCorefDictionary(mention, ant, dict, 2, 2))
				{
					return true;
				}
				if (Rules.EntityCorefDictionary(mention, ant, dict, 3, 2))
				{
					return true;
				}
				if (Rules.EntityCorefDictionary(mention, ant, dict, 4, 2))
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
				if ((m.IsPronominal() || dict.allPronouns.Contains(m.ToString())) && Rules.EntityAttributesAgree(mentionCluster, potentialAntecedent))
				{
					if (dict.demonymSet.Contains(ant.LowercaseNormalizedSpanString()) && dict.notOrganizationPRP.Contains(m.headString))
					{
						document.AddIncompatible(m, ant);
						return false;
					}
					if (Constants.UseDiscourseConstraints && Rules.EntityPersonDisagree(document, mentionCluster, potentialAntecedent, dict))
					{
						SieveCoreferenceSystem.logger.Finest("Incompatibles: Person Disagree: " + ant.SpanToString() + "(" + ant.mentionID + ") :: " + mention.SpanToString() + "(" + mention.mentionID + ") -> " + (mention.goldCorefClusterID != ant.goldCorefClusterID
							));
						document.AddIncompatible(m, ant);
						return false;
					}
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
				if (flags.DoPronoun && corefClusters[m1.corefClusterID].IsSinglePronounCluster(dict))
				{
					orderedAntecedents = SortMentionsForPronoun(orderedAntecedents, m1, true);
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

		/// <summary>Divides a sentence into clauses and sorts the antecedents for pronoun matching.</summary>
		private static IList<Mention> SortMentionsForPronoun(IList<Mention> l, Mention m1, bool sameSentence)
		{
			IList<Mention> sorted = new List<Mention>();
			if (sameSentence)
			{
				Tree tree = m1.contextParseTree;
				Tree current = m1.mentionSubTree;
				current = current.Parent(tree);
				while (current != null)
				{
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
					current = current.Parent(tree);
				}
				if (SieveCoreferenceSystem.logger.IsLoggable(Level.Finest))
				{
					if (l.Count != sorted.Count)
					{
						SieveCoreferenceSystem.logger.Finest("sorting failed!!! -> parser error?? \tmentionID: " + m1.mentionID + " " + m1.SpanToString());
						sorted = l;
					}
					else
					{
						if (!l.Equals(sorted))
						{
							SieveCoreferenceSystem.logger.Finest("sorting succeeded & changed !! \tmentionID: " + m1.mentionID + " " + m1.SpanToString());
							for (int i = 0; i < l.Count; i++)
							{
								Mention ml = l[i];
								Mention msorted = sorted[i];
								SieveCoreferenceSystem.logger.Finest("\t[" + ml.SpanToString() + "]\t[" + msorted.SpanToString() + "]");
							}
						}
						else
						{
							SieveCoreferenceSystem.logger.Finest("no changed !! \tmentionID: " + m1.mentionID + " " + m1.SpanToString());
						}
					}
				}
			}
			return sorted;
		}
	}
}
