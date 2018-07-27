using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.Coref.Hybrid.RF;
using Edu.Stanford.Nlp.Coref.MD;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class RFSieve : Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Hybrid.Sieve.RFSieve));

		private const long serialVersionUID = -4090017054885920527L;

		public RandomForest rf;

		/// <summary>the probability threshold for merging two mentions</summary>
		public double thresMerge;

		public RFSieve(RandomForest rf, Properties props, string sievename)
			: base(props, sievename)
		{
			// for RF sieve
			// constructor for RF sieve
			this.rf = rf;
			this.props = props;
			this.classifierType = Sieve.ClassifierType.Rf;
		}

		/// <exception cref="System.Exception"/>
		public override void FindCoreferentAntecedent(Mention m, int mIdx, Document document, Dictionaries dict, Properties props, StringBuilder sbLog)
		{
			int sentIdx = m.sentNum;
			ICounter<int> probs = new ClassicCounter<int>();
			int mentionDist = 0;
			for (int sentDist = 0; sentDist <= Math.Min(this.maxSentDist, sentIdx); sentDist++)
			{
				IList<Mention> candidates = GetOrderedAntecedents(m, sentIdx - sentDist, mIdx, document.predictedMentions, dict);
				foreach (Mention candidate in candidates)
				{
					if (SkipForAnalysis(candidate, m, props))
					{
						continue;
					}
					if (candidate == m)
					{
						continue;
					}
					if (!aType.Contains(candidate.mentionType))
					{
						continue;
					}
					if (m.mentionType == Dictionaries.MentionType.Pronominal)
					{
						if (!MatchedMentionType(m, mTypeStr))
						{
							continue;
						}
						if (!MatchedMentionType(candidate, aTypeStr))
						{
							continue;
						}
					}
					if (sentDist == 0 && m.AppearEarlierThan(candidate))
					{
						continue;
					}
					// ignore cataphora
					mentionDist++;
					RVFDatum<bool, string> datum = ExtractDatum(m, candidate, document, mentionDist, dict, props, sievename);
					double probTrue = 0;
					if (this.classifierType == Sieve.ClassifierType.Rf)
					{
						probTrue = this.rf.ProbabilityOfTrue(datum);
					}
					probs.SetCount(candidate.mentionID, probTrue);
				}
			}
			if (HybridCorefProperties.Debug(props))
			{
				sbLog.Append(HybridCorefPrinter.PrintErrorLog(m, document, probs, mIdx, dict, this));
			}
			if (probs.Size() > 0 && Counters.Max(probs) > this.thresMerge)
			{
				// merge highest prob candidate
				int antID = Counters.Argmax(probs);
				Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.Merge(document, m.mentionID, antID);
			}
		}

		public static RVFDatum<bool, string> ExtractDatum(Mention m, Mention candidate, Document document, int mentionDist, Dictionaries dict, Properties props, string sievename)
		{
			try
			{
				bool label = (document.goldMentions == null) ? false : document.IsCoref(m, candidate);
				ICounter<string> features = new ClassicCounter<string>();
				CorefCluster mC = document.corefClusters[m.corefClusterID];
				CorefCluster aC = document.corefClusters[candidate.corefClusterID];
				CoreLabel mFirst = m.sentenceWords[m.startIndex];
				CoreLabel mLast = m.sentenceWords[m.endIndex - 1];
				CoreLabel mPreceding = (m.startIndex > 0) ? m.sentenceWords[m.startIndex - 1] : null;
				CoreLabel mFollowing = (m.endIndex < m.sentenceWords.Count) ? m.sentenceWords[m.endIndex] : null;
				CoreLabel aFirst = candidate.sentenceWords[candidate.startIndex];
				CoreLabel aLast = candidate.sentenceWords[candidate.endIndex - 1];
				CoreLabel aPreceding = (candidate.startIndex > 0) ? candidate.sentenceWords[candidate.startIndex - 1] : null;
				CoreLabel aFollowing = (candidate.endIndex < candidate.sentenceWords.Count) ? candidate.sentenceWords[candidate.endIndex] : null;
				////////////////////////////////////////////////////////////////////////////////
				///////    basic features: distance, doctype, mention length, roles ////////////
				////////////////////////////////////////////////////////////////////////////////
				if (HybridCorefProperties.UseBasicFeatures(props, sievename))
				{
					int sentDist = m.sentNum - candidate.sentNum;
					features.IncrementCount("SENTDIST", sentDist);
					features.IncrementCount("MENTIONDIST", mentionDist);
					int minSentDist = sentDist;
					foreach (Mention a in aC.corefMentions)
					{
						minSentDist = Math.Min(minSentDist, Math.Abs(m.sentNum - a.sentNum));
					}
					features.IncrementCount("MINSENTDIST", minSentDist);
					// When they are in the same sentence, divides a sentence into clauses and add such feature
					if (CorefProperties.UseConstituencyParse(props))
					{
						if (m.sentNum == candidate.sentNum)
						{
							int clauseCount = 0;
							Tree tree = m.contextParseTree;
							Tree current = m.mentionSubTree;
							while (true)
							{
								current = current.Ancestor(1, tree);
								if (current.Label().Value().StartsWith("S"))
								{
									clauseCount++;
								}
								if (current.Dominates(candidate.mentionSubTree))
								{
									break;
								}
								if (current.Label().Value().Equals("ROOT") || current.Ancestor(1, tree) == null)
								{
									break;
								}
							}
							features.IncrementCount("CLAUSECOUNT", clauseCount);
						}
					}
					if (document.docType == Document.DocType.Conversation)
					{
						features.IncrementCount("B-DOCTYPE-" + document.docType);
					}
					if (Sharpen.Runtime.EqualsIgnoreCase(m.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)), "PER0"))
					{
						features.IncrementCount("B-SPEAKER-PER0");
					}
					if (document.docInfo != null && document.docInfo.Contains("DOC_ID"))
					{
						features.IncrementCount("B-DOCSOURCE-" + document.docInfo["DOC_ID"].Split("/")[1]);
					}
					features.IncrementCount("M-LENGTH", m.originalSpan.Count);
					features.IncrementCount("A-LENGTH", candidate.originalSpan.Count);
					if (m.originalSpan.Count < candidate.originalSpan.Count)
					{
						features.IncrementCount("B-A-ISLONGER");
					}
					features.IncrementCount("A-SIZE", aC.GetCorefMentions().Count);
					features.IncrementCount("M-SIZE", mC.GetCorefMentions().Count);
					string antRole = "A-NOROLE";
					string mRole = "M-NOROLE";
					if (m.isSubject)
					{
						mRole = "M-SUBJ";
					}
					if (m.isDirectObject)
					{
						mRole = "M-DOBJ";
					}
					if (m.isIndirectObject)
					{
						mRole = "M-IOBJ";
					}
					if (m.isPrepositionObject)
					{
						mRole = "M-POBJ";
					}
					if (candidate.isSubject)
					{
						antRole = "A-SUBJ";
					}
					if (candidate.isDirectObject)
					{
						antRole = "A-DOBJ";
					}
					if (candidate.isIndirectObject)
					{
						antRole = "A-IOBJ";
					}
					if (candidate.isPrepositionObject)
					{
						antRole = "A-POBJ";
					}
					features.IncrementCount("B-" + mRole);
					features.IncrementCount("B-" + antRole);
					features.IncrementCount("B-" + antRole + "-" + mRole);
					if (HybridCorefProperties.CombineObjectRoles(props, sievename))
					{
						// combine all objects
						if (m.isDirectObject || m.isIndirectObject || m.isPrepositionObject || candidate.isDirectObject || candidate.isIndirectObject || candidate.isPrepositionObject)
						{
							if (m.isDirectObject || m.isIndirectObject || m.isPrepositionObject)
							{
								mRole = "M-OBJ";
								features.IncrementCount("B-M-OBJ");
							}
							if (candidate.isDirectObject || candidate.isIndirectObject || candidate.isPrepositionObject)
							{
								antRole = "A-OBJ";
								features.IncrementCount("B-A-OBJ");
							}
							features.IncrementCount("B-" + antRole + "-" + mRole);
						}
					}
					if (mFirst.Word().ToLower().Matches("a|an"))
					{
						features.IncrementCount("B-M-START-WITH-INDEFINITE");
					}
					if (aFirst.Word().ToLower().Matches("a|an"))
					{
						features.IncrementCount("B-A-START-WITH-INDEFINITE");
					}
					if (Sharpen.Runtime.EqualsIgnoreCase(mFirst.Word(), "the"))
					{
						features.IncrementCount("B-M-START-WITH-DEFINITE");
					}
					if (Sharpen.Runtime.EqualsIgnoreCase(aFirst.Word(), "the"))
					{
						features.IncrementCount("B-A-START-WITH-DEFINITE");
					}
					if (dict.indefinitePronouns.Contains(m.LowercaseNormalizedSpanString()))
					{
						features.IncrementCount("B-M-INDEFINITE-PRONOUN");
					}
					if (dict.indefinitePronouns.Contains(candidate.LowercaseNormalizedSpanString()))
					{
						features.IncrementCount("B-A-INDEFINITE-PRONOUN");
					}
					if (dict.indefinitePronouns.Contains(mFirst.Word().ToLower()))
					{
						features.IncrementCount("B-M-INDEFINITE-ADJ");
					}
					if (dict.indefinitePronouns.Contains(aFirst.Word().ToLower()))
					{
						features.IncrementCount("B-A-INDEFINITE-ADJ");
					}
					if (dict.reflexivePronouns.Contains(m.headString))
					{
						features.IncrementCount("B-M-REFLEXIVE");
					}
					if (dict.reflexivePronouns.Contains(candidate.headString))
					{
						features.IncrementCount("B-A-REFLEXIVE");
					}
					if (m.headIndex == m.endIndex - 1)
					{
						features.IncrementCount("B-M-HEADEND");
					}
					if (m.headIndex < m.endIndex - 1)
					{
						CoreLabel headnext = m.sentenceWords[m.headIndex + 1];
						if (headnext.Word().Matches("that|,") || headnext.Tag().StartsWith("W"))
						{
							features.IncrementCount("B-M-HASPOSTPHRASE");
							if (mFirst.Tag().Equals("DT") && mFirst.Word().ToLower().Matches("the|this|these|those"))
							{
								features.IncrementCount("B-M-THE-HASPOSTPHRASE");
							}
							else
							{
								if (mFirst.Word().ToLower().Matches("a|an"))
								{
									features.IncrementCount("B-M-INDEFINITE-HASPOSTPHRASE");
								}
							}
						}
					}
					// shape feature from Bjorkelund & Kuhn
					StringBuilder sb = new StringBuilder();
					IList<Mention> sortedMentions = new List<Mention>(aC.corefMentions.Count);
					Sharpen.Collections.AddAll(sortedMentions, aC.corefMentions);
					sortedMentions.Sort(new CorefChain.MentionComparator());
					foreach (Mention a_1 in sortedMentions)
					{
						sb.Append(a_1.mentionType).Append("-");
					}
					features.IncrementCount("B-A-SHAPE-" + sb);
					sb = new StringBuilder();
					sortedMentions = new List<Mention>(mC.corefMentions.Count);
					Sharpen.Collections.AddAll(sortedMentions, mC.corefMentions);
					sortedMentions.Sort(new CorefChain.MentionComparator());
					foreach (Mention men in sortedMentions)
					{
						sb.Append(men.mentionType).Append("-");
					}
					features.IncrementCount("B-M-SHAPE-" + sb);
					if (CorefProperties.UseConstituencyParse(props))
					{
						sb = new StringBuilder();
						Tree mTree = m.contextParseTree;
						Tree mHead = mTree.GetLeaves()[m.headIndex].Ancestor(1, mTree);
						foreach (Tree node in mTree.PathNodeToNode(mHead, mTree))
						{
							sb.Append(node.Value()).Append("-");
							if (node.Value().Equals("S"))
							{
								break;
							}
						}
						features.IncrementCount("B-M-SYNPATH-" + sb);
						sb = new StringBuilder();
						Tree aTree = candidate.contextParseTree;
						Tree aHead = aTree.GetLeaves()[candidate.headIndex].Ancestor(1, aTree);
						foreach (Tree node_1 in aTree.PathNodeToNode(aHead, aTree))
						{
							sb.Append(node_1.Value()).Append("-");
							if (node_1.Value().Equals("S"))
							{
								break;
							}
						}
						features.IncrementCount("B-A-SYNPATH-" + sb);
					}
					features.IncrementCount("A-FIRSTAPPEAR", aC.representative.sentNum);
					features.IncrementCount("M-FIRSTAPPEAR", mC.representative.sentNum);
					int docSize = document.predictedMentions.Count;
					// document size in # of sentences
					features.IncrementCount("A-FIRSTAPPEAR-NORMALIZED", aC.representative.sentNum / docSize);
					features.IncrementCount("M-FIRSTAPPEAR-NORMALIZED", mC.representative.sentNum / docSize);
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    mention detection features                               ////////////
				////////////////////////////////////////////////////////////////////////////////
				if (HybridCorefProperties.UseMentionDetectionFeatures(props, sievename))
				{
					// bare plurals
					if (m.originalSpan.Count == 1 && m.headWord.Tag().Equals("NNS"))
					{
						features.IncrementCount("B-M-BAREPLURAL");
					}
					if (candidate.originalSpan.Count == 1 && candidate.headWord.Tag().Equals("NNS"))
					{
						features.IncrementCount("B-A-BAREPLURAL");
					}
					// pleonastic it
					if (CorefProperties.UseConstituencyParse(props))
					{
						if (RuleBasedCorefMentionFinder.IsPleonastic(m, m.contextParseTree) || RuleBasedCorefMentionFinder.IsPleonastic(candidate, candidate.contextParseTree))
						{
							features.IncrementCount("B-PLEONASTICIT");
						}
					}
					// quantRule
					if (dict.quantifiers.Contains(mFirst.Word().ToLower(Locale.English)))
					{
						features.IncrementCount("B-M-QUANTIFIER");
					}
					if (dict.quantifiers.Contains(aFirst.Word().ToLower(Locale.English)))
					{
						features.IncrementCount("B-A-QUANTIFIER");
					}
					// starts with negation
					if (mFirst.Word().ToLower(Locale.English).Matches("none|no|nothing|not") || aFirst.Word().ToLower(Locale.English).Matches("none|no|nothing|not"))
					{
						features.IncrementCount("B-NEGATIVE-START");
					}
					// parititive rule
					if (RuleBasedCorefMentionFinder.PartitiveRule(m, m.sentenceWords, dict))
					{
						features.IncrementCount("B-M-PARTITIVE");
					}
					if (RuleBasedCorefMentionFinder.PartitiveRule(candidate, candidate.sentenceWords, dict))
					{
						features.IncrementCount("B-A-PARTITIVE");
					}
					// %
					if (m.headString.Equals("%"))
					{
						features.IncrementCount("B-M-HEAD%");
					}
					if (candidate.headString.Equals("%"))
					{
						features.IncrementCount("B-A-HEAD%");
					}
					// adjective form of nations
					if (dict.IsAdjectivalDemonym(m.SpanToString()))
					{
						features.IncrementCount("B-M-ADJ-DEMONYM");
					}
					if (dict.IsAdjectivalDemonym(candidate.SpanToString()))
					{
						features.IncrementCount("B-A-ADJ-DEMONYM");
					}
					// ends with "etc."
					if (m.LowercaseNormalizedSpanString().EndsWith("etc."))
					{
						features.IncrementCount("B-M-ETC-END");
					}
					if (candidate.LowercaseNormalizedSpanString().EndsWith("etc."))
					{
						features.IncrementCount("B-A-ETC-END");
					}
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    attributes, attributes agree                             ////////////
				////////////////////////////////////////////////////////////////////////////////
				features.IncrementCount("B-M-NUMBER-" + m.number);
				features.IncrementCount("B-A-NUMBER-" + candidate.number);
				features.IncrementCount("B-M-GENDER-" + m.gender);
				features.IncrementCount("B-A-GENDER-" + candidate.gender);
				features.IncrementCount("B-M-ANIMACY-" + m.animacy);
				features.IncrementCount("B-A-ANIMACY-" + candidate.animacy);
				features.IncrementCount("B-M-PERSON-" + m.person);
				features.IncrementCount("B-A-PERSON-" + candidate.person);
				features.IncrementCount("B-M-NETYPE-" + m.nerString);
				features.IncrementCount("B-A-NETYPE-" + candidate.nerString);
				features.IncrementCount("B-BOTH-NUMBER-" + candidate.number + "-" + m.number);
				features.IncrementCount("B-BOTH-GENDER-" + candidate.gender + "-" + m.gender);
				features.IncrementCount("B-BOTH-ANIMACY-" + candidate.animacy + "-" + m.animacy);
				features.IncrementCount("B-BOTH-PERSON-" + candidate.person + "-" + m.person);
				features.IncrementCount("B-BOTH-NETYPE-" + candidate.nerString + "-" + m.nerString);
				ICollection<Dictionaries.Number> mcNumber = Generics.NewHashSet();
				foreach (Dictionaries.Number n in mC.numbers)
				{
					features.IncrementCount("B-MC-NUMBER-" + n);
					mcNumber.Add(n);
				}
				if (mcNumber.Count == 1)
				{
					features.IncrementCount("B-MC-CLUSTERNUMBER-" + mcNumber.GetEnumerator().Current);
				}
				else
				{
					mcNumber.Remove(Dictionaries.Number.Unknown);
					if (mcNumber.Count == 1)
					{
						features.IncrementCount("B-MC-CLUSTERNUMBER-" + mcNumber.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-MC-CLUSTERNUMBER-CONFLICT");
					}
				}
				ICollection<Dictionaries.Gender> mcGender = Generics.NewHashSet();
				foreach (Dictionaries.Gender g in mC.genders)
				{
					features.IncrementCount("B-MC-GENDER-" + g);
					mcGender.Add(g);
				}
				if (mcGender.Count == 1)
				{
					features.IncrementCount("B-MC-CLUSTERGENDER-" + mcGender.GetEnumerator().Current);
				}
				else
				{
					mcGender.Remove(Dictionaries.Gender.Unknown);
					if (mcGender.Count == 1)
					{
						features.IncrementCount("B-MC-CLUSTERGENDER-" + mcGender.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-MC-CLUSTERGENDER-CONFLICT");
					}
				}
				ICollection<Dictionaries.Animacy> mcAnimacy = Generics.NewHashSet();
				foreach (Dictionaries.Animacy a_2 in mC.animacies)
				{
					features.IncrementCount("B-MC-ANIMACY-" + a_2);
					mcAnimacy.Add(a_2);
				}
				if (mcAnimacy.Count == 1)
				{
					features.IncrementCount("B-MC-CLUSTERANIMACY-" + mcAnimacy.GetEnumerator().Current);
				}
				else
				{
					mcAnimacy.Remove(Dictionaries.Animacy.Unknown);
					if (mcAnimacy.Count == 1)
					{
						features.IncrementCount("B-MC-CLUSTERANIMACY-" + mcAnimacy.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-MC-CLUSTERANIMACY-CONFLICT");
					}
				}
				ICollection<string> mcNER = Generics.NewHashSet();
				foreach (string t in mC.nerStrings)
				{
					features.IncrementCount("B-MC-NETYPE-" + t);
					mcNER.Add(t);
				}
				if (mcNER.Count == 1)
				{
					features.IncrementCount("B-MC-CLUSTERNETYPE-" + mcNER.GetEnumerator().Current);
				}
				else
				{
					mcNER.Remove("O");
					if (mcNER.Count == 1)
					{
						features.IncrementCount("B-MC-CLUSTERNETYPE-" + mcNER.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-MC-CLUSTERNETYPE-CONFLICT");
					}
				}
				ICollection<Dictionaries.Number> acNumber = Generics.NewHashSet();
				foreach (Dictionaries.Number n_1 in aC.numbers)
				{
					features.IncrementCount("B-AC-NUMBER-" + n_1);
					acNumber.Add(n_1);
				}
				if (acNumber.Count == 1)
				{
					features.IncrementCount("B-AC-CLUSTERNUMBER-" + acNumber.GetEnumerator().Current);
				}
				else
				{
					acNumber.Remove(Dictionaries.Number.Unknown);
					if (acNumber.Count == 1)
					{
						features.IncrementCount("B-AC-CLUSTERNUMBER-" + acNumber.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-AC-CLUSTERNUMBER-CONFLICT");
					}
				}
				ICollection<Dictionaries.Gender> acGender = Generics.NewHashSet();
				foreach (Dictionaries.Gender g_1 in aC.genders)
				{
					features.IncrementCount("B-AC-GENDER-" + g_1);
					acGender.Add(g_1);
				}
				if (acGender.Count == 1)
				{
					features.IncrementCount("B-AC-CLUSTERGENDER-" + acGender.GetEnumerator().Current);
				}
				else
				{
					acGender.Remove(Dictionaries.Gender.Unknown);
					if (acGender.Count == 1)
					{
						features.IncrementCount("B-AC-CLUSTERGENDER-" + acGender.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-AC-CLUSTERGENDER-CONFLICT");
					}
				}
				ICollection<Dictionaries.Animacy> acAnimacy = Generics.NewHashSet();
				foreach (Dictionaries.Animacy a_3 in aC.animacies)
				{
					features.IncrementCount("B-AC-ANIMACY-" + a_3);
					acAnimacy.Add(a_3);
				}
				if (acAnimacy.Count == 1)
				{
					features.IncrementCount("B-AC-CLUSTERANIMACY-" + acAnimacy.GetEnumerator().Current);
				}
				else
				{
					acAnimacy.Remove(Dictionaries.Animacy.Unknown);
					if (acAnimacy.Count == 1)
					{
						features.IncrementCount("B-AC-CLUSTERANIMACY-" + acAnimacy.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-AC-CLUSTERANIMACY-CONFLICT");
					}
				}
				ICollection<string> acNER = Generics.NewHashSet();
				foreach (string t_1 in aC.nerStrings)
				{
					features.IncrementCount("B-AC-NETYPE-" + t_1);
					acNER.Add(t_1);
				}
				if (acNER.Count == 1)
				{
					features.IncrementCount("B-AC-CLUSTERNETYPE-" + acNER.GetEnumerator().Current);
				}
				else
				{
					acNER.Remove("O");
					if (acNER.Count == 1)
					{
						features.IncrementCount("B-AC-CLUSTERNETYPE-" + acNER.GetEnumerator().Current);
					}
					else
					{
						features.IncrementCount("B-AC-CLUSTERNETYPE-CONFLICT");
					}
				}
				if (m.NumbersAgree(candidate))
				{
					features.IncrementCount("B-NUMBER-AGREE");
				}
				if (m.GendersAgree(candidate))
				{
					features.IncrementCount("B-GENDER-AGREE");
				}
				if (m.AnimaciesAgree(candidate))
				{
					features.IncrementCount("B-ANIMACY-AGREE");
				}
				if (CorefRules.EntityAttributesAgree(mC, aC))
				{
					features.IncrementCount("B-ATTRIBUTES-AGREE");
				}
				if (CorefRules.EntityPersonDisagree(document, m, candidate, dict))
				{
					features.IncrementCount("B-PERSON-DISAGREE");
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    dcoref rules                                             ////////////
				////////////////////////////////////////////////////////////////////////////////
				if (HybridCorefProperties.UseDcorefRules(props, sievename))
				{
					if (CorefRules.EntityIWithinI(m, candidate, dict))
					{
						features.IncrementCount("B-i-within-i");
					}
					if (CorefRules.AntecedentIsMentionSpeaker(document, m, candidate, dict))
					{
						features.IncrementCount("B-ANT-IS-SPEAKER");
					}
					if (CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-SAME-SPEAKER");
					}
					if (CorefRules.EntitySubjectObject(m, candidate))
					{
						features.IncrementCount("B-SUBJ-OBJ");
					}
					foreach (Mention a in aC.corefMentions)
					{
						if (CorefRules.EntitySubjectObject(m, a_3))
						{
							features.IncrementCount("B-CLUSTER-SUBJ-OBJ");
						}
					}
					if (CorefRules.EntityPersonDisagree(document, m, candidate, dict) && CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-PERSON-DISAGREE-SAME-SPEAKER");
					}
					if (CorefRules.EntityIWithinI(mC, aC, dict))
					{
						features.IncrementCount("B-ENTITY-IWITHINI");
					}
					if (CorefRules.AntecedentMatchesMentionSpeakerAnnotation(m, candidate, document))
					{
						features.IncrementCount("B-ANT-IS-SPEAKER-OF-MENTION");
					}
					ICollection<Dictionaries.MentionType> mType = HybridCorefProperties.GetMentionType(props, sievename);
					if (mType.Contains(Dictionaries.MentionType.Proper) || mType.Contains(Dictionaries.MentionType.Nominal))
					{
						if (m.headString.Equals(candidate.headString))
						{
							features.IncrementCount("B-HEADMATCH");
						}
						if (CorefRules.EntityHeadsAgree(mC, aC, m, candidate, dict))
						{
							features.IncrementCount("B-HEADSAGREE");
						}
						if (CorefRules.EntityExactStringMatch(mC, aC, dict, document.roleSet))
						{
							features.IncrementCount("B-EXACTSTRINGMATCH");
						}
						if (CorefRules.EntityHaveExtraProperNoun(m, candidate, new HashSet<string>()))
						{
							features.IncrementCount("B-HAVE-EXTRA-PROPER-NOUN");
						}
						if (CorefRules.EntityBothHaveProper(mC, aC))
						{
							features.IncrementCount("B-BOTH-HAVE-PROPER");
						}
						if (CorefRules.EntityHaveDifferentLocation(m, candidate, dict))
						{
							features.IncrementCount("B-HAVE-DIFF-LOC");
						}
						if (CorefRules.EntityHaveIncompatibleModifier(mC, aC))
						{
							features.IncrementCount("B-HAVE-INCOMPATIBLE-MODIFIER");
						}
						if (CorefRules.EntityIsAcronym(document, mC, aC))
						{
							features.IncrementCount("B-IS-ACRONYM");
						}
						if (CorefRules.EntityIsApposition(mC, aC, m, candidate))
						{
							features.IncrementCount("B-IS-APPOSITION");
						}
						if (CorefRules.EntityIsPredicateNominatives(mC, aC, m, candidate))
						{
							features.IncrementCount("B-IS-PREDICATE-NOMINATIVES");
						}
						if (CorefRules.EntityIsRoleAppositive(mC, aC, m, candidate, dict))
						{
							features.IncrementCount("B-IS-ROLE-APPOSITIVE");
						}
						if (CorefRules.EntityNumberInLaterMention(m, candidate))
						{
							features.IncrementCount("B-NUMBER-IN-LATER");
						}
						if (CorefRules.EntityRelaxedExactStringMatch(mC, aC, m, candidate, dict, document.roleSet))
						{
							features.IncrementCount("B-RELAXED-EXACT-STRING-MATCH");
						}
						if (CorefRules.EntityRelaxedHeadsAgreeBetweenMentions(mC, aC, m, candidate))
						{
							features.IncrementCount("B-RELAXED-HEAD-AGREE");
						}
						if (CorefRules.EntitySameProperHeadLastWord(m, candidate))
						{
							features.IncrementCount("B-SAME-PROPER-HEAD");
						}
						if (CorefRules.EntitySameProperHeadLastWord(mC, aC, m, candidate))
						{
							features.IncrementCount("B-CLUSTER-SAME-PROPER-HEAD");
						}
						if (CorefRules.EntityWordsIncluded(mC, aC, m, candidate))
						{
							features.IncrementCount("B-WORD-INCLUSION");
						}
					}
					if (mType.Contains(Dictionaries.MentionType.List))
					{
						features.IncrementCount("NUM-LIST-", NumEntitiesInList(m));
						if (m.SpanToString().Contains("two") || m.SpanToString().Contains("2") || m.SpanToString().Contains("both"))
						{
							features.IncrementCount("LIST-M-TWO");
						}
						if (m.SpanToString().Contains("three") || m.SpanToString().Contains("3"))
						{
							features.IncrementCount("LIST-M-THREE");
						}
						if (candidate.SpanToString().Contains("two") || candidate.SpanToString().Contains("2") || candidate.SpanToString().Contains("both"))
						{
							features.IncrementCount("B-LIST-A-TWO");
						}
						if (candidate.SpanToString().Contains("three") || candidate.SpanToString().Contains("3"))
						{
							features.IncrementCount("B-LIST-A-THREE");
						}
					}
					if (mType.Contains(Dictionaries.MentionType.Pronominal))
					{
						if (dict.firstPersonPronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-I");
						}
						if (dict.secondPersonPronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-YOU");
						}
						if (dict.thirdPersonPronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-3RDPERSON");
						}
						if (dict.possessivePronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-POSSESSIVE");
						}
						if (dict.neutralPronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-NEUTRAL");
						}
						if (dict.malePronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-MALE");
						}
						if (dict.femalePronouns.Contains(m.headString))
						{
							features.IncrementCount("B-M-FEMALE");
						}
						if (dict.firstPersonPronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-I");
						}
						if (dict.secondPersonPronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-YOU");
						}
						if (dict.thirdPersonPronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-3RDPERSON");
						}
						if (dict.possessivePronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-POSSESSIVE");
						}
						if (dict.neutralPronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-NEUTRAL");
						}
						if (dict.malePronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-MALE");
						}
						if (dict.femalePronouns.Contains(candidate.headString))
						{
							features.IncrementCount("B-A-FEMALE");
						}
						features.IncrementCount("B-M-GENERIC-" + m.generic);
						features.IncrementCount("B-A-GENERIC-" + candidate.generic);
						if (HybridCorefPrinter.dcorefPronounSieve.SkipThisMention(document, m, mC, dict))
						{
							features.IncrementCount("B-SKIPTHISMENTION-true");
						}
						if (Sharpen.Runtime.EqualsIgnoreCase(m.SpanToString(), "you") && mFollowing != null && Sharpen.Runtime.EqualsIgnoreCase(mFollowing.Word(), "know"))
						{
							features.IncrementCount("B-YOUKNOW-PRECEDING-POS-" + ((mPreceding == null) ? "NULL" : mPreceding.Tag()));
							features.IncrementCount("B-YOUKNOW-PRECEDING-WORD-" + ((mPreceding == null) ? "NULL" : mPreceding.Word().ToLower()));
							CoreLabel nextword = (m.endIndex + 1 < m.sentenceWords.Count) ? m.sentenceWords[m.endIndex + 1] : null;
							features.IncrementCount("B-YOUKNOW-FOLLOWING-POS-" + ((nextword == null) ? "NULL" : nextword.Tag()));
							features.IncrementCount("B-YOUKNOW-FOLLOWING-WORD-" + ((nextword == null) ? "NULL" : nextword.Word().ToLower()));
						}
						if (Sharpen.Runtime.EqualsIgnoreCase(candidate.SpanToString(), "you") && aFollowing != null && Sharpen.Runtime.EqualsIgnoreCase(aFollowing.Word(), "know"))
						{
							features.IncrementCount("B-YOUKNOW-PRECEDING-POS-" + ((aPreceding == null) ? "NULL" : aPreceding.Tag()));
							features.IncrementCount("B-YOUKNOW-PRECEDING-WORD-" + ((aPreceding == null) ? "NULL" : aPreceding.Word().ToLower()));
							CoreLabel nextword = (candidate.endIndex + 1 < candidate.sentenceWords.Count) ? candidate.sentenceWords[candidate.endIndex + 1] : null;
							features.IncrementCount("B-YOUKNOW-FOLLOWING-POS-" + ((nextword == null) ? "NULL" : nextword.Tag()));
							features.IncrementCount("B-YOUKNOW-FOLLOWING-WORD-" + ((nextword == null) ? "NULL" : nextword.Word().ToLower()));
						}
					}
					// discourse match features
					if (m.person == Dictionaries.Person.You && document.docType == Document.DocType.Article && m.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals("PER0"))
					{
						features.IncrementCount("B-DISCOURSE-M-YOU-GENERIC?");
					}
					if (candidate.generic && candidate.person == Dictionaries.Person.You)
					{
						features.IncrementCount("B-DISCOURSE-A-YOU-GENERIC?");
					}
					string mString = m.LowercaseNormalizedSpanString();
					string antString = candidate.LowercaseNormalizedSpanString();
					// I-I
					if (m.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString) && candidate.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString) && CorefRules.EntitySameSpeaker(document, m, candidate
						))
					{
						features.IncrementCount("B-DISCOURSE-I-I-SAMESPEAKER");
					}
					// (speaker - I)
					if ((m.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(mString)) && CorefRules.AntecedentIsMentionSpeaker(document, m, candidate, dict))
					{
						features.IncrementCount("B-DISCOURSE-SPEAKER-I");
					}
					// (I - speaker)
					if ((candidate.number == Dictionaries.Number.Singular && dict.firstPersonPronouns.Contains(antString)) && CorefRules.AntecedentIsMentionSpeaker(document, candidate, m, dict))
					{
						features.IncrementCount("B-DISCOURSE-I-SPEAKER");
					}
					// Can be iffy if more than two speakers... but still should be okay most of the time
					if (dict.secondPersonPronouns.Contains(mString) && dict.secondPersonPronouns.Contains(antString) && CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-DISCOURSE-BOTH-YOU");
					}
					// previous I - you or previous you - I in two person conversation
					if (((m.person == Dictionaries.Person.I && candidate.person == Dictionaries.Person.You || (m.person == Dictionaries.Person.You && candidate.person == Dictionaries.Person.I)) && (m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) - candidate
						.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) == 1) && document.docType == Document.DocType.Conversation))
					{
						features.IncrementCount("B-DISCOURSE-I-YOU");
					}
					if (dict.reflexivePronouns.Contains(m.headString) && CorefRules.EntitySubjectObject(m, candidate))
					{
						features.IncrementCount("B-DISCOURSE-REFLEXIVE");
					}
					if (m.person == Dictionaries.Person.I && candidate.person == Dictionaries.Person.I && !CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-DISCOURSE-I-I-DIFFSPEAKER");
					}
					if (m.person == Dictionaries.Person.You && candidate.person == Dictionaries.Person.You && !CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-DISCOURSE-YOU-YOU-DIFFSPEAKER");
					}
					if (m.person == Dictionaries.Person.We && candidate.person == Dictionaries.Person.We && !CorefRules.EntitySameSpeaker(document, m, candidate))
					{
						features.IncrementCount("B-DISCOURSE-WE-WE-DIFFSPEAKER");
					}
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    POS features                                             ////////////
				////////////////////////////////////////////////////////////////////////////////
				if (HybridCorefProperties.UsePOSFeatures(props, sievename))
				{
					features.IncrementCount("B-LEXICAL-M-HEADPOS-" + m.headWord.Tag());
					features.IncrementCount("B-LEXICAL-A-HEADPOS-" + candidate.headWord.Tag());
					features.IncrementCount("B-LEXICAL-M-FIRSTPOS-" + mFirst.Tag());
					features.IncrementCount("B-LEXICAL-A-FIRSTPOS-" + aFirst.Tag());
					features.IncrementCount("B-LEXICAL-M-LASTPOS-" + mLast.Tag());
					features.IncrementCount("B-LEXICAL-A-LASTPOS-" + aLast.Tag());
					features.IncrementCount("B-LEXICAL-M-PRECEDINGPOS-" + ((mPreceding == null) ? "NULL" : mPreceding.Tag()));
					features.IncrementCount("B-LEXICAL-A-PRECEDINGPOS-" + ((aPreceding == null) ? "NULL" : aPreceding.Tag()));
					features.IncrementCount("B-LEXICAL-M-FOLLOWINGPOS-" + ((mFollowing == null) ? "NULL" : mFollowing.Tag()));
					features.IncrementCount("B-LEXICAL-A-FOLLOWINGPOS-" + ((aFollowing == null) ? "NULL" : aFollowing.Tag()));
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    lexical features                                         ////////////
				////////////////////////////////////////////////////////////////////////////////
				if (HybridCorefProperties.UseLexicalFeatures(props, sievename))
				{
					features.IncrementCount("B-LEXICAL-M-HEADWORD-" + m.headString.ToLower());
					features.IncrementCount("B-LEXICAL-A-HEADWORD-" + candidate.headString.ToLower());
					features.IncrementCount("B-LEXICAL-M-FIRSTWORD-" + mFirst.Word().ToLower());
					features.IncrementCount("B-LEXICAL-A-FIRSTWORD-" + aFirst.Word().ToLower());
					features.IncrementCount("B-LEXICAL-M-LASTWORD-" + mLast.Word().ToLower());
					features.IncrementCount("B-LEXICAL-A-LASTWORD-" + aLast.Word().ToLower());
					features.IncrementCount("B-LEXICAL-M-PRECEDINGWORD-" + ((mPreceding == null) ? "NULL" : mPreceding.Word().ToLower()));
					features.IncrementCount("B-LEXICAL-A-PRECEDINGWORD-" + ((aPreceding == null) ? "NULL" : aPreceding.Word().ToLower()));
					features.IncrementCount("B-LEXICAL-M-FOLLOWINGWORD-" + ((mFollowing == null) ? "NULL" : mFollowing.Word().ToLower()));
					features.IncrementCount("B-LEXICAL-A-FOLLOWINGWORD-" + ((aFollowing == null) ? "NULL" : aFollowing.Word().ToLower()));
					//extra headword, modifiers lexical features
					foreach (string mHead in mC.heads)
					{
						if (!aC.heads.Contains(mHead))
						{
							features.IncrementCount("B-LEXICAL-MC-EXTRAHEAD-" + mHead);
						}
					}
					foreach (string mWord in mC.words)
					{
						if (!aC.words.Contains(mWord))
						{
							features.IncrementCount("B-LEXICAL-MC-EXTRAWORD-" + mWord);
						}
					}
				}
				////////////////////////////////////////////////////////////////////////////////
				///////    word vector features                                     ////////////
				////////////////////////////////////////////////////////////////////////////////
				// cosine
				if (HybridCorefProperties.UseWordEmbedding(props, sievename))
				{
					// dimension
					int dim = dict.vectors.GetEnumerator().Current.Value.Length;
					// distance between headword
					float[] mV = dict.vectors[m.headString.ToLower()];
					float[] aV = dict.vectors[candidate.headString.ToLower()];
					if (mV != null && aV != null)
					{
						features.IncrementCount("WORDVECTOR-DIFF-HEADWORD", Cosine(mV, aV));
					}
					mV = dict.vectors[mFirst.Word().ToLower()];
					aV = dict.vectors[aFirst.Word().ToLower()];
					if (mV != null && aV != null)
					{
						features.IncrementCount("WORDVECTOR-DIFF-FIRSTWORD", Cosine(mV, aV));
					}
					mV = dict.vectors[mLast.Word().ToLower()];
					aV = dict.vectors[aLast.Word().ToLower()];
					if (mV != null && aV != null)
					{
						features.IncrementCount("WORDVECTOR-DIFF-LASTWORD", Cosine(mV, aV));
					}
					if (mPreceding != null && aPreceding != null)
					{
						mV = dict.vectors[mPreceding.Word().ToLower()];
						aV = dict.vectors[aPreceding.Word().ToLower()];
						if (mV != null && aV != null)
						{
							features.IncrementCount("WORDVECTOR-DIFF-PRECEDINGWORD", Cosine(mV, aV));
						}
					}
					if (mFollowing != null && aFollowing != null)
					{
						mV = dict.vectors[mFollowing.Word().ToLower()];
						aV = dict.vectors[aFollowing.Word().ToLower()];
						if (mV != null && aV != null)
						{
							features.IncrementCount("WORDVECTOR-DIFF-FOLLOWINGWORD", Cosine(mV, aV));
						}
					}
					float[] aggreM = new float[dim];
					float[] aggreA = new float[dim];
					foreach (CoreLabel cl in m.originalSpan)
					{
						float[] v = dict.vectors[cl.Word().ToLower()];
						if (v == null)
						{
							continue;
						}
						ArrayMath.PairwiseAddInPlace(aggreM, v);
					}
					foreach (CoreLabel cl_1 in candidate.originalSpan)
					{
						float[] v = dict.vectors[cl_1.Word().ToLower()];
						if (v == null)
						{
							continue;
						}
						ArrayMath.PairwiseAddInPlace(aggreA, v);
					}
					if (ArrayMath.L2Norm(aggreM) != 0 && ArrayMath.L2Norm(aggreA) != 0)
					{
						features.IncrementCount("WORDVECTOR-AGGREGATE-DIFF", Cosine(aggreM, aggreA));
					}
					int cnt = 0;
					double dist = 0;
					foreach (CoreLabel mcl in m.originalSpan)
					{
						foreach (CoreLabel acl in candidate.originalSpan)
						{
							mV = dict.vectors[mcl.Word().ToLower()];
							aV = dict.vectors[acl.Word().ToLower()];
							if (mV == null || aV == null)
							{
								continue;
							}
							cnt++;
							dist += Cosine(mV, aV);
						}
					}
					features.IncrementCount("WORDVECTOR-AVG-DIFF", dist / cnt);
				}
				return new RVFDatum<bool, string>(features, label);
			}
			catch (Exception e)
			{
				log.Info("Datum Extraction failed in Sieve.java while processing document: " + document.docInfo["DOC_ID"] + " part: " + document.docInfo["DOC_PART"]);
				throw new Exception(e);
			}
		}

		// assume the input vectors are normalized
		private static double Cosine(float[] normalizedVector1, float[] normalizedVector2)
		{
			double inner = ArrayMath.InnerProduct(normalizedVector1, normalizedVector2);
			return inner;
		}

		private static int NumEntitiesInList(Mention m)
		{
			int num = 0;
			for (int i = 1; i < m.originalSpan.Count; i++)
			{
				CoreLabel cl = m.originalSpan[i];
				if (cl.Word().Equals(","))
				{
					num++;
				}
				if ((Sharpen.Runtime.EqualsIgnoreCase(cl.Word(), "and") || Sharpen.Runtime.EqualsIgnoreCase(cl.Word(), "or")) && !m.originalSpan[i - 1].Word().Equals(","))
				{
					num++;
				}
			}
			return num;
		}
	}
}
