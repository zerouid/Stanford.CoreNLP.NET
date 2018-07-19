using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>
	/// Rules for coref system (mention detection, entity coref, event coref)
	/// The name of the method for mention detection starts with detection,
	/// for entity coref starts with entity, and for event coref starts with event.
	/// </summary>
	/// <author>heeyoung, recasens</author>
	public class CorefRules
	{
		public static bool EntityBothHaveProper(CorefCluster mentionCluster, CorefCluster potentialAntecedent)
		{
			bool mentionClusterHaveProper = false;
			bool potentialAntecedentHaveProper = false;
			foreach (Mention m in mentionCluster.corefMentions)
			{
				if (m.mentionType == Dictionaries.MentionType.Proper)
				{
					mentionClusterHaveProper = true;
					break;
				}
			}
			foreach (Mention a in potentialAntecedent.corefMentions)
			{
				if (a.mentionType == Dictionaries.MentionType.Proper)
				{
					potentialAntecedentHaveProper = true;
					break;
				}
			}
			return (mentionClusterHaveProper && potentialAntecedentHaveProper);
		}

		public static bool EntitySameProperHeadLastWord(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention mention, Mention ant)
		{
			foreach (Mention m in mentionCluster.GetCorefMentions())
			{
				foreach (Mention a in potentialAntecedent.GetCorefMentions())
				{
					if (EntitySameProperHeadLastWord(m, a))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <exception cref="System.Exception"/>
		public static bool EntityAlias(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Semantics semantics, Dictionaries dict)
		{
			Mention mention = mentionCluster.GetRepresentativeMention();
			Mention antecedent = potentialAntecedent.GetRepresentativeMention();
			if (mention.mentionType != Dictionaries.MentionType.Proper || antecedent.mentionType != Dictionaries.MentionType.Proper)
			{
				return false;
			}
			MethodInfo meth = semantics.wordnet.GetType().GetMethod("alias", new Type[] { typeof(Mention), typeof(Mention) });
			if ((bool)meth.Invoke(semantics.wordnet, new object[] { mention, antecedent }))
			{
				return true;
			}
			return false;
		}

		public static bool EntityIWithinI(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict)
		{
			foreach (Mention m in mentionCluster.GetCorefMentions())
			{
				foreach (Mention a in potentialAntecedent.GetCorefMentions())
				{
					if (EntityIWithinI(m, a, dict))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool EntityPersonDisagree(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict)
		{
			bool disagree = false;
			foreach (Mention m in mentionCluster.GetCorefMentions())
			{
				foreach (Mention ant in potentialAntecedent.GetCorefMentions())
				{
					if (EntityPersonDisagree(document, m, ant, dict))
					{
						disagree = true;
						break;
					}
				}
			}
			if (disagree)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private static readonly IList<string> entityWordsToExclude = Arrays.AsList(new string[] { "the", "this", "mr.", "miss", "mrs.", "dr.", "ms.", "inc.", "ltd.", "corp.", "'s" });

		/// <summary>Word inclusion except stop words</summary>
		public static bool EntityWordsIncluded(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention mention, Mention ant)
		{
			ICollection<string> wordsExceptStopWords = Generics.NewHashSet(mentionCluster.words);
			wordsExceptStopWords.RemoveAll(entityWordsToExclude);
			wordsExceptStopWords.Remove(mention.headString.ToLower());
			if (potentialAntecedent.words.ContainsAll(wordsExceptStopWords))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>Compatible modifier only</summary>
		public static bool EntityHaveIncompatibleModifier(CorefCluster mentionCluster, CorefCluster potentialAntecedent)
		{
			foreach (Mention m in mentionCluster.corefMentions)
			{
				foreach (Mention ant in potentialAntecedent.corefMentions)
				{
					if (EntityHaveIncompatibleModifier(m, ant))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool EntityIsRoleAppositive(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention m1, Mention m2, Dictionaries dict)
		{
			if (!EntityAttributesAgree(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			return m1.IsRoleAppositive(m2, dict) || m2.IsRoleAppositive(m1, dict);
		}

		public static bool EntityIsRelativePronoun(Mention m1, Mention m2)
		{
			return m1.IsRelativePronoun(m2) || m2.IsRelativePronoun(m1);
		}

		public static bool EntityIsAcronym(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent)
		{
			Pair<int, int> idPair = Pair.MakePair(Math.Min(mentionCluster.clusterID, potentialAntecedent.clusterID), Math.Max(mentionCluster.clusterID, potentialAntecedent.clusterID));
			if (!document.acronymCache.Contains(idPair))
			{
				bool isAcronym = false;
				foreach (Mention m in mentionCluster.corefMentions)
				{
					if (m.IsPronominal())
					{
						continue;
					}
					foreach (Mention ant in potentialAntecedent.corefMentions)
					{
						if (IsAcronym(m.originalSpan, ant.originalSpan))
						{
							isAcronym = true;
						}
					}
				}
				document.acronymCache[idPair] = isAcronym;
			}
			return document.acronymCache[idPair];
		}

		public static bool IsAcronym(IList<CoreLabel> first, IList<CoreLabel> second)
		{
			if (first.Count > 1 && second.Count > 1)
			{
				return false;
			}
			if (first.Count == 0 && second.Count == 0)
			{
				return false;
			}
			IList<CoreLabel> longer;
			IList<CoreLabel> shorter;
			if (first.Count == second.Count)
			{
				string firstWord = first[0].Get(typeof(CoreAnnotations.TextAnnotation));
				string secondWord = second[0].Get(typeof(CoreAnnotations.TextAnnotation));
				longer = (firstWord.Length > secondWord.Length) ? first : second;
				shorter = (firstWord.Length > secondWord.Length) ? second : first;
			}
			else
			{
				longer = (first.Count > 0 && first.Count > second.Count) ? first : second;
				shorter = (second.Count > 0 && first.Count > second.Count) ? second : first;
			}
			string acronym = shorter.Count > 0 ? shorter[0].Get(typeof(CoreAnnotations.TextAnnotation)) : "<UNK>";
			// This check is not strictly necessary, but it saves a chunk of
			// time iterating through the text of the longer mention
			for (int acronymPos = 0; acronymPos < acronym.Length; ++acronymPos)
			{
				if (acronym[acronymPos] < 'A' || acronym[acronymPos] > 'Z')
				{
					return false;
				}
			}
			int acronymPos_1 = 0;
			foreach (CoreLabel aLonger1 in longer)
			{
				string word = aLonger1.Get(typeof(CoreAnnotations.TextAnnotation));
				for (int charNum = 0; charNum < word.Length; ++charNum)
				{
					if (word[charNum] >= 'A' && word[charNum] <= 'Z')
					{
						// This triggers if there were more "acronym" characters in
						// the longer mention than in the shorter mention
						if (acronymPos_1 >= acronym.Length)
						{
							return false;
						}
						if (acronym[acronymPos_1] != word[charNum])
						{
							return false;
						}
						++acronymPos_1;
					}
				}
			}
			if (acronymPos_1 != acronym.Length)
			{
				return false;
			}
			foreach (CoreLabel aLonger in longer)
			{
				if (aLonger.Get(typeof(CoreAnnotations.TextAnnotation)).Contains(acronym))
				{
					return false;
				}
			}
			return true;
		}

		public static bool EntityIsPredicateNominatives(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention m1, Mention m2)
		{
			if (!EntityAttributesAgree(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if ((m1.startIndex <= m2.startIndex && m1.endIndex >= m2.endIndex) || (m1.startIndex >= m2.startIndex && m1.endIndex <= m2.endIndex))
			{
				return false;
			}
			return m1.IsPredicateNominatives(m2) || m2.IsPredicateNominatives(m1);
		}

		public static bool EntityIsApposition(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention m1, Mention m2)
		{
			if (!EntityAttributesAgree(mentionCluster, potentialAntecedent))
			{
				return false;
			}
			if (m1.mentionType == Dictionaries.MentionType.Proper && m2.mentionType == Dictionaries.MentionType.Proper)
			{
				return false;
			}
			if (m1.nerString.Equals("LOCATION"))
			{
				return false;
			}
			return m1.IsApposition(m2) || m2.IsApposition(m1);
		}

		public static bool EntityAttributesAgree(CorefCluster mentionCluster, CorefCluster potentialAntecedent)
		{
			return EntityAttributesAgree(mentionCluster, potentialAntecedent, false);
		}

		public static bool EntityAttributesAgree(CorefCluster mentionCluster, CorefCluster potentialAntecedent, bool ignoreGender)
		{
			bool hasExtraAnt = false;
			bool hasExtraThis = false;
			// number
			if (!mentionCluster.numbers.Contains(Dictionaries.Number.Unknown))
			{
				foreach (Dictionaries.Number n in potentialAntecedent.numbers)
				{
					if (n != Dictionaries.Number.Unknown && !mentionCluster.numbers.Contains(n))
					{
						hasExtraAnt = true;
						break;
					}
				}
			}
			if (!potentialAntecedent.numbers.Contains(Dictionaries.Number.Unknown))
			{
				foreach (Dictionaries.Number n in mentionCluster.numbers)
				{
					if (n != Dictionaries.Number.Unknown && !potentialAntecedent.numbers.Contains(n))
					{
						hasExtraThis = true;
						break;
					}
				}
			}
			if (hasExtraAnt && hasExtraThis)
			{
				return false;
			}
			// gender
			hasExtraAnt = false;
			hasExtraThis = false;
			if (!ignoreGender)
			{
				if (!mentionCluster.genders.Contains(Dictionaries.Gender.Unknown))
				{
					foreach (Dictionaries.Gender g in potentialAntecedent.genders)
					{
						if (g != Dictionaries.Gender.Unknown && !mentionCluster.genders.Contains(g))
						{
							hasExtraAnt = true;
							break;
						}
					}
				}
				if (!potentialAntecedent.genders.Contains(Dictionaries.Gender.Unknown))
				{
					foreach (Dictionaries.Gender g in mentionCluster.genders)
					{
						if (g != Dictionaries.Gender.Unknown && !potentialAntecedent.genders.Contains(g))
						{
							hasExtraThis = true;
							break;
						}
					}
				}
			}
			if (hasExtraAnt && hasExtraThis)
			{
				return false;
			}
			// animacy
			hasExtraAnt = false;
			hasExtraThis = false;
			if (!mentionCluster.animacies.Contains(Dictionaries.Animacy.Unknown))
			{
				foreach (Dictionaries.Animacy a in potentialAntecedent.animacies)
				{
					if (a != Dictionaries.Animacy.Unknown && !mentionCluster.animacies.Contains(a))
					{
						hasExtraAnt = true;
						break;
					}
				}
			}
			if (!potentialAntecedent.animacies.Contains(Dictionaries.Animacy.Unknown))
			{
				foreach (Dictionaries.Animacy a in mentionCluster.animacies)
				{
					if (a != Dictionaries.Animacy.Unknown && !potentialAntecedent.animacies.Contains(a))
					{
						hasExtraThis = true;
						break;
					}
				}
			}
			if (hasExtraAnt && hasExtraThis)
			{
				return false;
			}
			// NE type
			hasExtraAnt = false;
			hasExtraThis = false;
			if (!mentionCluster.nerStrings.Contains("O") && !mentionCluster.nerStrings.Contains("MISC"))
			{
				foreach (string ne in potentialAntecedent.nerStrings)
				{
					if (!ne.Equals("O") && !ne.Equals("MISC") && !mentionCluster.nerStrings.Contains(ne))
					{
						hasExtraAnt = true;
						break;
					}
				}
			}
			if (!potentialAntecedent.nerStrings.Contains("O") && !potentialAntecedent.nerStrings.Contains("MISC"))
			{
				foreach (string ne in mentionCluster.nerStrings)
				{
					if (!ne.Equals("O") && !ne.Equals("MISC") && !potentialAntecedent.nerStrings.Contains(ne))
					{
						hasExtraThis = true;
						break;
					}
				}
			}
			return !(hasExtraAnt && hasExtraThis);
		}

		private static bool AttributeSetDisagree<E>(ICollection<E> s1, ICollection<E> s2)
		{
			int minSize = Math.Min(s1.Count, s2.Count);
			// intersection being smaller than the smaller set means both sets
			// have extra elements
			if (minSize > Sets.Intersection(s1, s2).Count)
			{
				return true;
			}
			return false;
		}

		private static void PruneAttributes<E>(ICollection<E> attrs, ICollection<E> unknown)
		{
			if (attrs.Count > unknown.Count)
			{
				attrs.RemoveAll(unknown);
			}
		}

		private static void PruneAttributes<E>(ICollection<E> attrs, E unknown)
		{
			if (attrs.Count > 1)
			{
				attrs.Remove(unknown);
			}
		}

		private static readonly ICollection<string> UnknownNer = new HashSet<string>(Arrays.AsList("MISC", "O"));

		private static bool EntityAttributesAgreeChinese(CorefCluster mentionCluster, CorefCluster potentialAntecedent)
		{
			PruneAttributes(mentionCluster.numbers, Dictionaries.Number.Unknown);
			PruneAttributes(mentionCluster.genders, Dictionaries.Gender.Unknown);
			PruneAttributes(mentionCluster.animacies, Dictionaries.Animacy.Unknown);
			PruneAttributes(mentionCluster.nerStrings, UnknownNer);
			PruneAttributes(potentialAntecedent.numbers, Dictionaries.Number.Unknown);
			PruneAttributes(potentialAntecedent.genders, Dictionaries.Gender.Unknown);
			PruneAttributes(potentialAntecedent.animacies, Dictionaries.Animacy.Unknown);
			PruneAttributes(potentialAntecedent.nerStrings, UnknownNer);
			if (AttributeSetDisagree(mentionCluster.numbers, potentialAntecedent.numbers) || AttributeSetDisagree(mentionCluster.genders, potentialAntecedent.genders) || AttributeSetDisagree(mentionCluster.animacies, potentialAntecedent.animacies) || AttributeSetDisagree
				(mentionCluster.nerStrings, potentialAntecedent.nerStrings))
			{
				return false;
			}
			return true;
		}

		public static bool EntityAttributesAgree(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Locale lang)
		{
			if (lang == Locale.Chinese)
			{
				return EntityAttributesAgreeChinese(mentionCluster, potentialAntecedent);
			}
			return EntityAttributesAgree(mentionCluster, potentialAntecedent);
		}

		public static bool EntityRelaxedHeadsAgreeBetweenMentions(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention m, Mention ant)
		{
			if (m.IsPronominal() || ant.IsPronominal())
			{
				return false;
			}
			if (m.HeadsAgree(ant))
			{
				return true;
			}
			return false;
		}

		public static bool EntityHeadsAgree(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention m, Mention ant, Dictionaries dict)
		{
			bool headAgree = false;
			if (m.IsPronominal() || ant.IsPronominal() || dict.allPronouns.Contains(m.LowercaseNormalizedSpanString()) || dict.allPronouns.Contains(ant.LowercaseNormalizedSpanString()))
			{
				return false;
			}
			foreach (Mention a in potentialAntecedent.corefMentions)
			{
				if (a.headString.Equals(m.headString))
				{
					headAgree = true;
				}
			}
			return headAgree;
		}

		public static bool EntityExactStringMatch(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict, ICollection<Mention> roleSet)
		{
			bool matched = false;
			foreach (Mention m in mentionCluster.corefMentions)
			{
				if (roleSet != null && roleSet.Contains(m))
				{
					return false;
				}
				if (m.IsPronominal())
				{
					continue;
				}
				string mSpan = m.LowercaseNormalizedSpanString();
				if (dict.allPronouns.Contains(mSpan))
				{
					continue;
				}
				foreach (Mention ant in potentialAntecedent.corefMentions)
				{
					if (ant.IsPronominal())
					{
						continue;
					}
					string antSpan = ant.LowercaseNormalizedSpanString();
					if (dict.allPronouns.Contains(antSpan))
					{
						continue;
					}
					if (mSpan.Equals(antSpan))
					{
						matched = true;
					}
					if (mSpan.Equals(antSpan + " 's") || antSpan.Equals(mSpan + " 's"))
					{
						matched = true;
					}
				}
			}
			return matched;
		}

		public static bool EntityExactStringMatch(Mention m, Mention ant, Dictionaries dict, ICollection<Mention> roleSet)
		{
			bool matched = false;
			if (roleSet != null && roleSet.Contains(m))
			{
				return false;
			}
			if (m.IsPronominal() || ant.IsPronominal())
			{
				return false;
			}
			string mSpan = m.LowercaseNormalizedSpanString();
			if (dict.allPronouns.Contains(mSpan))
			{
				return false;
			}
			string antSpan = ant.LowercaseNormalizedSpanString();
			if (dict.allPronouns.Contains(antSpan))
			{
				return false;
			}
			if (mSpan.Equals(antSpan))
			{
				matched = true;
			}
			if (mSpan.Equals(antSpan + " 's") || antSpan.Equals(mSpan + " 's"))
			{
				matched = true;
			}
			return matched;
		}

		/// <summary>
		/// Exact string match except phrase after head (only for proper noun):
		/// For dealing with a error like
		/// <literal>"[Mr. Bickford] &lt;- [Mr. Bickford , an 18-year mediation veteran]</literal>
		/// "
		/// </summary>
		public static bool EntityRelaxedExactStringMatch(CorefCluster mentionCluster, CorefCluster potentialAntecedent, Mention mention, Mention ant, Dictionaries dict, ICollection<Mention> roleSet)
		{
			if (roleSet != null && roleSet.Contains(mention))
			{
				return false;
			}
			if (mention.mentionType == Dictionaries.MentionType.List || ant.mentionType == Dictionaries.MentionType.List)
			{
				return false;
			}
			if (mention.IsPronominal() || ant.IsPronominal() || dict.allPronouns.Contains(mention.LowercaseNormalizedSpanString()) || dict.allPronouns.Contains(ant.LowercaseNormalizedSpanString()))
			{
				return false;
			}
			string mentionSpan = mention.RemovePhraseAfterHead();
			string antSpan = ant.RemovePhraseAfterHead();
			if (mentionSpan.Equals(string.Empty) || antSpan.Equals(string.Empty))
			{
				return false;
			}
			if (mentionSpan.Equals(antSpan) || mentionSpan.Equals(antSpan + " 's") || antSpan.Equals(mentionSpan + " 's"))
			{
				return true;
			}
			return false;
		}

		/// <summary>Check whether two mentions are in i-within-i relation (Chomsky, 1981)</summary>
		public static bool EntityIWithinI(Mention m1, Mention m2, Dictionaries dict)
		{
			// check for nesting: i-within-i
			if (!m1.IsApposition(m2) && !m2.IsApposition(m1) && !m1.IsRelativePronoun(m2) && !m2.IsRelativePronoun(m1) && !m1.IsRoleAppositive(m2, dict) && !m2.IsRoleAppositive(m1, dict))
			{
				if (m1.IncludedIn(m2) || m2.IncludedIn(m1))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Check whether later mention has incompatible modifier</summary>
		public static bool EntityHaveIncompatibleModifier(Mention m, Mention ant)
		{
			if (!Sharpen.Runtime.EqualsIgnoreCase(ant.headString, m.headString))
			{
				return false;
			}
			// only apply to same head mentions
			bool thisHasExtra = false;
			int lengthThis = m.originalSpan.Count;
			int lengthM = ant.originalSpan.Count;
			ICollection<string> thisWordSet = Generics.NewHashSet();
			ICollection<string> antWordSet = Generics.NewHashSet();
			ICollection<string> locationModifier = Generics.NewHashSet(Arrays.AsList("east", "west", "north", "south", "eastern", "western", "northern", "southern", "upper", "lower"));
			for (int i = 0; i < lengthThis; i++)
			{
				string w1 = m.originalSpan[i].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower();
				string pos1 = m.originalSpan[i].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				if (!(pos1.StartsWith("N") || pos1.StartsWith("JJ") || pos1.Equals("CD") || pos1.StartsWith("V")) || Sharpen.Runtime.EqualsIgnoreCase(w1, m.headString))
				{
					continue;
				}
				thisWordSet.Add(w1);
			}
			for (int j = 0; j < lengthM; j++)
			{
				string w2 = ant.originalSpan[j].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower();
				antWordSet.Add(w2);
			}
			foreach (string w in thisWordSet)
			{
				if (!antWordSet.Contains(w))
				{
					thisHasExtra = true;
					break;
				}
			}
			bool hasLocationModifier = false;
			foreach (string l in locationModifier)
			{
				if (antWordSet.Contains(l) && !thisWordSet.Contains(l))
				{
					hasLocationModifier = true;
					break;
				}
			}
			return (thisHasExtra || hasLocationModifier);
		}

		/// <summary>Check whether two mentions have different locations</summary>
		private static readonly ICollection<string> locationModifier = Generics.NewHashSet(Arrays.AsList("east", "west", "north", "south", "eastern", "western", "northern", "southern", "northwestern", "southwestern", "northeastern", "southeastern", 
			"upper", "lower"));

		public static bool EntityHaveDifferentLocation(Mention m, Mention a, Dictionaries dict)
		{
			// state and country cannot be coref
			if ((dict.statesAbbreviation.Contains(a.SpanToString()) || dict.statesAbbreviation.ContainsValue(a.SpanToString())) && (Sharpen.Runtime.EqualsIgnoreCase(m.headString, "country") || Sharpen.Runtime.EqualsIgnoreCase(m.headString, "nation")))
			{
				return true;
			}
			ICollection<string> locationM = Generics.NewHashSet();
			ICollection<string> locationA = Generics.NewHashSet();
			string mString = m.LowercaseNormalizedSpanString();
			string aString = a.LowercaseNormalizedSpanString();
			foreach (CoreLabel w in m.originalSpan)
			{
				string text = w.Get(typeof(CoreAnnotations.TextAnnotation));
				string lowercased = text.ToLower();
				if (locationModifier.Contains(lowercased))
				{
					return true;
				}
				if (w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("LOCATION"))
				{
					string loc = text;
					if (dict.statesAbbreviation.Contains(loc))
					{
						loc = dict.statesAbbreviation[loc];
					}
					locationM.Add(lowercased);
				}
			}
			foreach (CoreLabel w_1 in a.originalSpan)
			{
				string text = w_1.Get(typeof(CoreAnnotations.TextAnnotation));
				string lowercased = text.ToLower();
				if (locationModifier.Contains(lowercased))
				{
					return true;
				}
				if (w_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("LOCATION"))
				{
					string loc = text;
					if (dict.statesAbbreviation.Contains(loc))
					{
						loc = dict.statesAbbreviation[loc];
					}
					locationA.Add(lowercased);
				}
			}
			bool mHasExtra = false;
			bool aHasExtra = false;
			foreach (string s in locationM)
			{
				if (!aString.Contains(s))
				{
					mHasExtra = true;
					break;
				}
			}
			foreach (string s_1 in locationA)
			{
				if (!mString.Contains(s_1))
				{
					aHasExtra = true;
					break;
				}
			}
			if (mHasExtra && aHasExtra)
			{
				return true;
			}
			return false;
		}

		/// <summary>Check whether two mentions have the same proper head words</summary>
		public static bool EntitySameProperHeadLastWord(Mention m, Mention a)
		{
			if (!Sharpen.Runtime.EqualsIgnoreCase(m.headString, a.headString) || !m.sentenceWords[m.headIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP") || !a.sentenceWords[a.headIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation
				)).StartsWith("NNP"))
			{
				return false;
			}
			if (!m.RemovePhraseAfterHead().ToLower().EndsWith(m.headString) || !a.RemovePhraseAfterHead().ToLower().EndsWith(a.headString))
			{
				return false;
			}
			ICollection<string> mProperNouns = Generics.NewHashSet();
			ICollection<string> aProperNouns = Generics.NewHashSet();
			foreach (CoreLabel w in m.sentenceWords.SubList(m.startIndex, m.headIndex))
			{
				if (w.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP"))
				{
					mProperNouns.Add(w.Get(typeof(CoreAnnotations.TextAnnotation)));
				}
			}
			foreach (CoreLabel w_1 in a.sentenceWords.SubList(a.startIndex, a.headIndex))
			{
				if (w_1.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP"))
				{
					aProperNouns.Add(w_1.Get(typeof(CoreAnnotations.TextAnnotation)));
				}
			}
			bool mHasExtra = false;
			bool aHasExtra = false;
			foreach (string s in mProperNouns)
			{
				if (!aProperNouns.Contains(s))
				{
					mHasExtra = true;
					break;
				}
			}
			foreach (string s_1 in aProperNouns)
			{
				if (!mProperNouns.Contains(s_1))
				{
					aHasExtra = true;
					break;
				}
			}
			if (mHasExtra && aHasExtra)
			{
				return false;
			}
			return true;
		}

		private static readonly ICollection<string> Numbers = Generics.NewHashSet(Arrays.AsList(new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundred", "thousand", "million", "billion" }));

		/// <summary>Check whether there is a new number in later mention</summary>
		public static bool EntityNumberInLaterMention(Mention mention, Mention ant)
		{
			ICollection<string> antecedentWords = Generics.NewHashSet();
			foreach (CoreLabel w in ant.originalSpan)
			{
				antecedentWords.Add(w.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			foreach (CoreLabel w_1 in mention.originalSpan)
			{
				string word = w_1.Get(typeof(CoreAnnotations.TextAnnotation));
				// Note: this is locale specific for English and ascii numerals
				if (NumberMatchingRegex.IsDouble(word))
				{
					if (!antecedentWords.Contains(word))
					{
						return true;
					}
				}
				else
				{
					if (Numbers.Contains(word.ToLower()) && !antecedentWords.Contains(word))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Have extra proper noun except strings involved in semantic match</summary>
		public static bool EntityHaveExtraProperNoun(Mention m, Mention a, ICollection<string> exceptWords)
		{
			ICollection<string> mProper = Generics.NewHashSet();
			ICollection<string> aProper = Generics.NewHashSet();
			string mString = m.SpanToString();
			string aString = a.SpanToString();
			foreach (CoreLabel w in m.originalSpan)
			{
				if (w.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP"))
				{
					mProper.Add(w.Get(typeof(CoreAnnotations.TextAnnotation)));
				}
			}
			foreach (CoreLabel w_1 in a.originalSpan)
			{
				if (w_1.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP"))
				{
					aProper.Add(w_1.Get(typeof(CoreAnnotations.TextAnnotation)));
				}
			}
			bool mHasExtra = false;
			bool aHasExtra = false;
			foreach (string s in mProper)
			{
				if (!aString.Contains(s) && !exceptWords.Contains(s.ToLower()))
				{
					mHasExtra = true;
					break;
				}
			}
			foreach (string s_1 in aProper)
			{
				if (!mString.Contains(s_1) && !exceptWords.Contains(s_1.ToLower()))
				{
					aHasExtra = true;
					break;
				}
			}
			if (mHasExtra && aHasExtra)
			{
				return true;
			}
			return false;
		}

		/// <summary>Is the speaker for mention the same entity as the ant entity?</summary>
		public static bool AntecedentIsMentionSpeaker(Document document, Mention mention, Mention ant, Dictionaries dict)
		{
			if (document.speakerPairs.Contains(new Pair<int, int>(mention.mentionID, ant.mentionID)))
			{
				return true;
			}
			if (AntecedentMatchesMentionSpeakerAnnotation(mention, ant, document))
			{
				return true;
			}
			return false;
		}

		public static readonly Pattern WhitespacePattern = Pattern.Compile(" +");

		/// <summary>The antecedent matches the speaker annotation found in the mention</summary>
		public static bool AntecedentMatchesMentionSpeakerAnnotation(Mention mention, Mention ant, Document document)
		{
			if (mention.headWord == null)
			{
				return false;
			}
			string speaker = mention.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
			if (speaker == null)
			{
				return false;
			}
			SpeakerInfo speakerInfo = (document != null) ? document.GetSpeakerInfo(speaker) : null;
			if (speakerInfo != null)
			{
				return (MentionMatchesSpeaker(ant, speakerInfo, false));
			}
			// CAN'T get speaker info - take alternate path
			// We optimize a little here: if the name has no spaces, which is
			// the common case, then it is unnecessarily expensive to call
			// regex split
			if (speaker.IndexOf(" ") >= 0)
			{
				// Perhaps we could optimize this, too, but that would be trickier
				foreach (string s in WhitespacePattern.Split(speaker))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(ant.headString, s))
					{
						return true;
					}
				}
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(ant.headString, speaker))
				{
					return true;
				}
			}
			return false;
		}

		public static bool MentionMatchesSpeaker(Mention mention, SpeakerInfo speakerInfo, bool strictMatch)
		{
			// Got info about this speaker
			if (mention.speakerInfo != null)
			{
				if (mention.speakerInfo == speakerInfo)
				{
					return true;
				}
			}
			if (speakerInfo.ContainsMention(mention))
			{
				return true;
			}
			if (strictMatch)
			{
				string spkstr = SpeakerInfo.WhitespacePattern.Matcher(speakerInfo.GetSpeakerName()).ReplaceAll(string.Empty);
				string mstr = SpeakerInfo.WhitespacePattern.Matcher(mention.SpanToString()).ReplaceAll(string.Empty);
				if (Sharpen.Runtime.EqualsIgnoreCase(spkstr, mstr))
				{
					speakerInfo.AddMention(mention);
					return true;
				}
			}
			else
			{
				// speaker strings are pre-split
				if (!mention.headWord.Tag().StartsWith("NNP"))
				{
					return false;
				}
				foreach (string s in speakerInfo.GetSpeakerNameStrings())
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(mention.headString, s))
					{
						speakerInfo.AddMention(mention);
						return true;
					}
				}
				if (speakerInfo.GetSpeakerDesc() != null)
				{
					string spkDescStr = SpeakerInfo.WhitespacePattern.Matcher(speakerInfo.GetSpeakerDesc()).ReplaceAll(string.Empty);
					string mstr = SpeakerInfo.WhitespacePattern.Matcher(mention.SpanToString()).ReplaceAll(string.Empty);
					if (Sharpen.Runtime.EqualsIgnoreCase(spkDescStr, mstr))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool EntityPersonDisagree(Document document, Mention m, Mention ant, Dictionaries dict)
		{
			bool sameSpeaker = EntitySameSpeaker(document, m, ant);
			if (sameSpeaker && m.person != ant.person)
			{
				if ((m.person == Dictionaries.Person.It && ant.person == Dictionaries.Person.They) || (m.person == Dictionaries.Person.They && ant.person == Dictionaries.Person.It) || (m.person == Dictionaries.Person.They && ant.person == Dictionaries.Person
					.They))
				{
					return false;
				}
				else
				{
					if (m.person != Dictionaries.Person.Unknown && ant.person != Dictionaries.Person.Unknown)
					{
						return true;
					}
				}
			}
			if (sameSpeaker)
			{
				if (!ant.IsPronominal())
				{
					if (m.person == Dictionaries.Person.I || m.person == Dictionaries.Person.We || m.person == Dictionaries.Person.You)
					{
						return true;
					}
				}
				else
				{
					if (!m.IsPronominal())
					{
						if (ant.person == Dictionaries.Person.I || ant.person == Dictionaries.Person.We || ant.person == Dictionaries.Person.You)
						{
							return true;
						}
					}
				}
			}
			if (m.person == Dictionaries.Person.You && m != ant && ant.AppearEarlierThan(m))
			{
				System.Diagnostics.Debug.Assert(!m.AppearEarlierThan(ant));
				int mUtter = m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation));
				if (document.speakers.Contains(mUtter - 1))
				{
					string previousSpeaker = document.speakers[mUtter - 1];
					int previousSpeakerCorefClusterID = GetSpeakerClusterId(document, previousSpeaker);
					if (previousSpeakerCorefClusterID < 0)
					{
						return true;
					}
					if (ant.corefClusterID != previousSpeakerCorefClusterID && ant.person != Dictionaries.Person.I)
					{
						return true;
					}
				}
				else
				{
					return true;
				}
			}
			else
			{
				if (ant.person == Dictionaries.Person.You && m != ant && m.AppearEarlierThan(ant))
				{
					System.Diagnostics.Debug.Assert(!(ant.AppearEarlierThan(m)));
					int aUtter = ant.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation));
					if (document.speakers.Contains(aUtter - 1))
					{
						string previousSpeaker = document.speakers[aUtter - 1];
						int previousSpeakerCorefClusterID = GetSpeakerClusterId(document, previousSpeaker);
						if (previousSpeakerCorefClusterID < 0)
						{
							return true;
						}
						if (m.corefClusterID != previousSpeakerCorefClusterID && m.person != Dictionaries.Person.I)
						{
							return true;
						}
					}
					else
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Do the mentions share the same speaker?</summary>
		public static bool EntitySameSpeaker(Document document, Mention m, Mention ant)
		{
			string mSpeakerStr = m.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
			if (mSpeakerStr == null)
			{
				return false;
			}
			string antSpeakerStr = ant.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
			if (antSpeakerStr == null)
			{
				return false;
			}
			// Speakers are the same if the speaker strings are the same (most common case?)
			if (mSpeakerStr.Equals(antSpeakerStr))
			{
				return true;
			}
			else
			{
				// Speakers are also the same if they map to the same cluster id...
				int mSpeakerClusterID = GetSpeakerClusterId(document, mSpeakerStr);
				int antSpeakerClusterID = GetSpeakerClusterId(document, antSpeakerStr);
				if (mSpeakerClusterID >= 0 && antSpeakerClusterID >= 0)
				{
					return (mSpeakerClusterID == antSpeakerClusterID);
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>Given the name of a speaker, returns the coref cluster id it belongs to (-1 if no cluster)</summary>
		/// <param name="document">The document to search in</param>
		/// <param name="speakerString">The name to search for</param>
		/// <returns>cluster id</returns>
		public static int GetSpeakerClusterId(Document document, string speakerString)
		{
			int speakerClusterId = -1;
			// try looking up cluster id from speaker info
			SpeakerInfo speakerInfo = null;
			if (speakerString != null)
			{
				speakerInfo = document.GetSpeakerInfo(speakerString);
				if (speakerInfo != null)
				{
					speakerClusterId = speakerInfo.GetCorefClusterId();
				}
			}
			if (speakerClusterId < 0 && speakerString != null && NumberMatchingRegex.IsDecimalInteger(speakerString))
			{
				// speakerString is number so is mention id
				try
				{
					int speakerMentionId = System.Convert.ToInt32(speakerString);
					Mention mention = document.predictedMentionsByID[speakerMentionId];
					if (mention != null)
					{
						speakerClusterId = mention.corefClusterID;
						if (speakerInfo != null)
						{
							speakerInfo.AddMention(mention);
						}
					}
				}
				catch (Exception)
				{
				}
			}
			return speakerClusterId;
		}

		public static bool EntitySubjectObject(Mention m1, Mention m2)
		{
			if (m1.sentNum != m2.sentNum)
			{
				return false;
			}
			if (m1.dependingVerb == null || m2.dependingVerb == null)
			{
				return false;
			}
			if (m1.dependingVerb == m2.dependingVerb && ((m1.isSubject && (m2.isDirectObject || m2.isIndirectObject || m2.isPrepositionObject)) || (m2.isSubject && (m1.isDirectObject || m1.isIndirectObject || m1.isPrepositionObject))))
			{
				return true;
			}
			return false;
		}

		// Return true if the two mentions are less than n mentions apart in the same sent
		public static bool EntityTokenDistance(Mention m1, Mention m2)
		{
			if ((m2.sentNum == m1.sentNum) && (m1.startIndex - m2.startIndex < 6))
			{
				return true;
			}
			return false;
		}

		// COREF_DICT strict: all the mention pairs between the two clusters must match in the dict
		public static bool EntityClusterAllCorefDictionary(CorefCluster menCluster, CorefCluster antCluster, Dictionaries dict, int dictColumn, int freq)
		{
			bool ret = false;
			foreach (Mention men in menCluster.GetCorefMentions())
			{
				if (men.IsPronominal())
				{
					continue;
				}
				foreach (Mention ant in antCluster.GetCorefMentions())
				{
					if (ant.IsPronominal() || men.headWord.Lemma().Equals(ant.headWord.Lemma()))
					{
						continue;
					}
					if (EntityCorefDictionary(men, ant, dict, dictColumn, freq))
					{
						ret = true;
					}
					else
					{
						return false;
					}
				}
			}
			return ret;
		}

		// COREF_DICT pairwise: the two mentions match in the dict
		public static bool EntityCorefDictionary(Mention men, Mention ant, Dictionaries dict, int dictVersion, int freq)
		{
			Pair<string, string> mention_pair = new Pair<string, string>(men.GetSplitPattern()[dictVersion - 1].ToLower(), ant.GetSplitPattern()[dictVersion - 1].ToLower());
			int high_freq = -1;
			if (dictVersion == 1)
			{
				high_freq = 75;
			}
			else
			{
				if (dictVersion == 2)
				{
					high_freq = 16;
				}
				else
				{
					if (dictVersion == 3)
					{
						high_freq = 16;
					}
					else
					{
						if (dictVersion == 4)
						{
							high_freq = 16;
						}
					}
				}
			}
			if (dict.corefDict[dictVersion - 1].GetCount(mention_pair) > high_freq)
			{
				return true;
			}
			if (dict.corefDict[dictVersion - 1].GetCount(mention_pair) > freq)
			{
				if (dict.corefDictPMI.GetCount(mention_pair) > 0.18)
				{
					return true;
				}
				if (!dict.corefDictPMI.ContainsKey(mention_pair))
				{
					return true;
				}
			}
			return false;
		}

		public static bool ContextIncompatible(Mention men, Mention ant, Dictionaries dict)
		{
			string antHead = ant.headWord.Word();
			if ((ant.mentionType == Dictionaries.MentionType.Proper) && ant.sentNum != men.sentNum && !IsContextOverlapping(ant, men) && dict.NE_signatures.Contains(antHead))
			{
				IntCounter<string> ranks = Counters.ToRankCounter(dict.NE_signatures[antHead]);
				IList<string> context;
				if (!men.GetPremodifierContext().IsEmpty())
				{
					context = men.GetPremodifierContext();
				}
				else
				{
					context = men.GetContext();
				}
				if (!context.IsEmpty())
				{
					int highestRank = 100000;
					foreach (string w in context)
					{
						if (ranks.ContainsKey(w) && ranks.GetIntCount(w) < highestRank)
						{
							highestRank = ranks.GetIntCount(w);
						}
						// check in the other direction
						if (dict.NE_signatures.Contains(w))
						{
							IntCounter<string> reverseRanks = Counters.ToRankCounter(dict.NE_signatures[w]);
							if (reverseRanks.ContainsKey(antHead) && reverseRanks.GetIntCount(antHead) < highestRank)
							{
								highestRank = reverseRanks.GetIntCount(antHead);
							}
						}
					}
					if (highestRank > 10)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool SentenceContextIncompatible(Mention men, Mention ant, Dictionaries dict)
		{
			if ((ant.mentionType != Dictionaries.MentionType.Proper) && (ant.sentNum != men.sentNum) && (men.mentionType != Dictionaries.MentionType.Proper) && !IsContextOverlapping(ant, men))
			{
				IList<string> context1 = !ant.GetPremodifierContext().IsEmpty() ? ant.GetPremodifierContext() : ant.GetContext();
				IList<string> context2 = !men.GetPremodifierContext().IsEmpty() ? men.GetPremodifierContext() : men.GetContext();
				if (!context1.IsEmpty() && !context2.IsEmpty())
				{
					int highestRank = 100000;
					foreach (string w1 in context1)
					{
						foreach (string w2 in context2)
						{
							// check the forward direction
							if (dict.NE_signatures.Contains(w1))
							{
								IntCounter<string> ranks = Counters.ToRankCounter(dict.NE_signatures[w1]);
								if (ranks.ContainsKey(w2) && ranks.GetIntCount(w2) < highestRank)
								{
									highestRank = ranks.GetIntCount(w2);
								}
							}
							// check in the other direction
							if (dict.NE_signatures.Contains(w2))
							{
								IntCounter<string> reverseRanks = Counters.ToRankCounter(dict.NE_signatures[w2]);
								if (reverseRanks.ContainsKey(w1) && reverseRanks.GetIntCount(w1) < highestRank)
								{
									highestRank = reverseRanks.GetIntCount(w1);
								}
							}
						}
					}
					if (highestRank > 10)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsContextOverlapping(Mention m1, Mention m2)
		{
			ICollection<string> context1 = Generics.NewHashSet();
			ICollection<string> context2 = Generics.NewHashSet();
			Sharpen.Collections.AddAll(context1, m1.GetContext());
			Sharpen.Collections.AddAll(context2, m2.GetContext());
			return Sets.Intersects(context1, context2);
		}
	}
}
