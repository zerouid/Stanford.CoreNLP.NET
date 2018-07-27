using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid.Sieve;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Coref.Hybrid
{
	/// <summary>
	/// Prints CoNLL-style output from a
	/// <see cref="Edu.Stanford.Nlp.Coref.Data.Document"/>
	/// </summary>
	/// <author>heeyoung</author>
	public class HybridCorefPrinter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(HybridCorefPrinter));

		public static readonly DecimalFormat df = new DecimalFormat("#.####");

		public static readonly SpeakerMatch dcorefSpeaker = new SpeakerMatch();

		public static readonly DiscourseMatch dcorefDiscourse = new DiscourseMatch();

		public static readonly ExactStringMatch dcorefExactString = new ExactStringMatch();

		public static readonly RelaxedExactStringMatch dcorefRelaxedExactString = new RelaxedExactStringMatch();

		public static readonly PreciseConstructs dcorefPreciseConstructs = new PreciseConstructs();

		public static readonly StrictHeadMatch1 dcorefHead1 = new StrictHeadMatch1();

		public static readonly StrictHeadMatch2 dcorefHead2 = new StrictHeadMatch2();

		public static readonly StrictHeadMatch3 dcorefHead3 = new StrictHeadMatch3();

		public static readonly StrictHeadMatch4 dcorefHead4 = new StrictHeadMatch4();

		public static readonly RelaxedHeadMatch dcorefRelaxedHead = new RelaxedHeadMatch();

		public static readonly PronounMatch dcorefPronounSieve = new PronounMatch();

		// for debug
		//  public static final ChineseHeadMatch dcorefChineseHeadMatch = new ChineseHeadMatch(StringUtils.argsToProperties(new String[]{"-coref.language", "zh"}));
		/// <summary>Print raw document for analysis</summary>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static string PrintRawDoc(Document document, bool gold, bool printClusterID)
		{
			StringBuilder sb = new StringBuilder();
			IList<ICoreMap> sentences = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			StringBuilder doc = new StringBuilder();
			for (int i = 0; i < sentences.Count; i++)
			{
				doc.Append(SentenceStringWithMention(i, document, gold, printClusterID));
				doc.Append("\n");
			}
			sb.Append("PRINT RAW DOC START\n");
			sb.Append(document.annotation.Get(typeof(CoreAnnotations.DocIDAnnotation))).Append("\n");
			if (gold)
			{
				sb.Append("New DOC: (GOLD MENTIONS) ==================================================\n");
			}
			else
			{
				sb.Append("New DOC: (Predicted Mentions) ==================================================\n");
			}
			sb.Append(doc.ToString()).Append("\n");
			sb.Append("PRINT RAW DOC END").Append("\n");
			return sb.ToString();
		}

		/// <exception cref="System.Exception"/>
		public static string PrintErrorLog(Mention m, Document document, ICounter<int> probs, int mIdx, Dictionaries dict, RFSieve sieve)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\nERROR START-----------------------------------------------------------------------\n");
			sb.Append("RESOLVER TYPE: mType: " + sieve.mType + ", aType: " + sieve.aType).Append("\n");
			sb.Append("DOCUMENT: " + document.docInfo["DOC_ID"] + ", " + document.docInfo["DOC_PART"]).Append("\n");
			IList<Mention> orderedAnts = new List<Mention>();
			sb.Append("\nGOLD CLUSTER ID\n");
			for (int sentDist = m.sentNum; sentDist >= 0; sentDist--)
			{
				if (sentDist == sieve.maxSentDist)
				{
					sb.Append("\tstart compare from here-------------\n");
				}
				int sentIdx = m.sentNum - sentDist;
				sb.Append("\tSENT " + sentIdx + "\t" + SentenceStringWithMention(sentIdx, document, true, true)).Append("\n");
			}
			sb.Append("\nMENTION ID\n");
			for (int sentDist_1 = m.sentNum; sentDist_1 >= 0; sentDist_1--)
			{
				if (sentDist_1 == sieve.maxSentDist)
				{
					sb.Append("\tstart compare from here-------------\n");
				}
				int sentIdx = m.sentNum - sentDist_1;
				sb.Append("\tSENT " + sentIdx + "\t" + SentenceStringWithMention(sentIdx, document, false, false)).Append("\n");
			}
			// get dcoref antecedents ordering
			for (int sentDist_2 = 0; sentDist_2 <= Math.Min(sieve.maxSentDist, m.sentNum); sentDist_2++)
			{
				int sentIdx = m.sentNum - sentDist_2;
				Sharpen.Collections.AddAll(orderedAnts, Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.GetOrderedAntecedents(m, sentIdx, mIdx, document.predictedMentions, dict));
			}
			IDictionary<int, int> orders = Generics.NewHashMap();
			for (int i = 0; i < orderedAnts.Count; i++)
			{
				Mention ant = orderedAnts[i];
				orders[ant.mentionID] = i;
			}
			CorefCluster mC = document.corefClusters[m.corefClusterID];
			bool isFirstMention = IsFirstMention(m, document);
			bool foundCorefAnt = (probs.Size() > 0 && Counters.Max(probs) > sieve.thresMerge);
			bool correctDecision = ((isFirstMention && !foundCorefAnt) || (foundCorefAnt && Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.IsReallyCoref(document, m.mentionID, Counters.Argmax(probs))));
			bool barePlural = (m.originalSpan.Count == 1 && m.headWord.Tag().Equals("NNS"));
			if (correctDecision)
			{
				return string.Empty;
			}
			sb.Append("\nMENTION: " + m.SpanToString() + " (" + m.mentionID + ")\tperson: " + m.person + "\tsingleton? " + (!m.hasTwin) + "\t\tisFirstMention? " + isFirstMention + "\t\tfoundAnt? " + foundCorefAnt + "\t\tcorrectDecision? " + correctDecision
				 + "\tbarePlural? " + barePlural);
			sb.Append("\n\ttype: " + m.mentionType + "\tHeadword: " + m.headWord.Word() + "\tNEtype: " + m.nerString + "\tnumber: " + m.number + "\tgender: " + m.gender + "\tanimacy: " + m.animacy).Append("\n");
			if (m.contextParseTree != null)
			{
				sb.Append(m.contextParseTree.PennString());
			}
			sb.Append("\n\n\t\tOracle\t\tDcoref\t\t\tRF\t\tAntecedent\n");
			foreach (int antID in Counters.ToSortedList(probs))
			{
				Mention ant = document.predictedMentionsByID[antID];
				CorefCluster aC = document.corefClusters[ant.corefClusterID];
				bool oracle = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.IsReallyCoref(document, m.mentionID, antID);
				double prob = probs.GetCount(antID);
				int order = orders[antID];
				string oracleStr = (oracle) ? "coref   " : "notcoref";
				//      String dcorefStr = (dcoref)? "coref   " : "notcoref";
				string dcorefStr = "notcoref";
				if (dcorefDiscourse.Coreferent(document, mC, aC, m, ant, dict, null))
				{
					dcorefStr = "coref-discourse";
				}
				else
				{
					//      else if(dcorefChineseHeadMatch.coreferent(document, mC, aC, m, ant, dict, null)) dcorefStr = "coref-chineseHeadMatch";
					if (dcorefExactString.Coreferent(document, mC, aC, m, ant, dict, null))
					{
						dcorefStr = "coref-exactString";
					}
					else
					{
						if (dcorefRelaxedExactString.Coreferent(document, mC, aC, m, ant, dict, null))
						{
							dcorefStr = "coref-relaxedExact";
						}
						else
						{
							if (dcorefPreciseConstructs.Coreferent(document, mC, aC, m, ant, dict, null))
							{
								dcorefStr = "coref-preciseConstruct";
							}
							else
							{
								if (dcorefHead1.Coreferent(document, mC, aC, m, ant, dict, null))
								{
									dcorefStr = "coref-head1";
								}
								else
								{
									if (dcorefHead2.Coreferent(document, mC, aC, m, ant, dict, null))
									{
										dcorefStr = "coref-head2";
									}
									else
									{
										if (dcorefHead3.Coreferent(document, mC, aC, m, ant, dict, null))
										{
											dcorefStr = "coref-head3";
										}
										else
										{
											if (dcorefHead4.Coreferent(document, mC, aC, m, ant, dict, null))
											{
												dcorefStr = "coref-head4";
											}
											else
											{
												if (dcorefRelaxedHead.Coreferent(document, mC, aC, m, ant, dict, null))
												{
													dcorefStr = "coref-relaxedHead";
												}
												else
												{
													if (dcorefPronounSieve.Coreferent(document, mC, aC, m, ant, dict, null))
													{
														dcorefStr = "coref-pronounSieve";
													}
													else
													{
														if (dcorefSpeaker.Coreferent(document, mC, aC, m, ant, dict, null))
														{
															dcorefStr = "coref-speaker";
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				dcorefStr += "\t" + order.ToString();
				string probStr = df.Format(prob);
				sb.Append("\t\t" + oracleStr + "\t" + dcorefStr + "\t" + probStr + "\t\t" + ant.SpanToString() + " (" + ant.mentionID + ")\n");
			}
			sb.Append("ERROR END -----------------------------------------------------------------------\n");
			return sb.ToString();
		}

		internal static bool IsFirstMention(Mention m, Document document)
		{
			if (!m.hasTwin)
			{
				return true;
			}
			Mention twinGold = document.goldMentionsByID[m.mentionID];
			foreach (Mention coref in document.goldCorefClusters[twinGold.goldCorefClusterID].GetCorefMentions())
			{
				if (coref == twinGold)
				{
					continue;
				}
				if (coref.AppearEarlierThan(twinGold))
				{
					return false;
				}
			}
			return true;
		}

		public static string SentenceStringWithMention(int i, Document document, bool gold, bool printClusterID)
		{
			StringBuilder sentStr = new StringBuilder();
			IList<ICoreMap> sentences = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<IList<Mention>> allMentions;
			if (gold)
			{
				allMentions = document.goldMentions;
			}
			else
			{
				allMentions = document.predictedMentions;
			}
			//    String filename = document.annotation.get()
			int previousOffset = 0;
			ICoreMap sentence = sentences[i];
			IList<Mention> mentions = allMentions[i];
			IList<CoreLabel> t = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			string speaker = t[0].Get(typeof(CoreAnnotations.SpeakerAnnotation));
			if (NumberMatchingRegex.IsDecimalInteger(speaker))
			{
				speaker = speaker + ": " + document.predictedMentionsByID[System.Convert.ToInt32(speaker)].SpanToString();
			}
			sentStr.Append("\tspeaker: " + speaker + " (" + t[0].Get(typeof(CoreAnnotations.UtteranceAnnotation)) + ") ");
			string[] tokens = new string[t.Count];
			foreach (CoreLabel c in t)
			{
				tokens[c.Index() - 1] = c.Word();
			}
			//    if(previousOffset+2 < t.get(0).get(CoreAnnotations.CharacterOffsetBeginAnnotation.class) && printClusterID) {
			//      sentStr.append("\n");
			//    }
			previousOffset = t[t.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			ICounter<int> startCounts = new ClassicCounter<int>();
			ICounter<int> endCounts = new ClassicCounter<int>();
			IDictionary<int, IDeque<Mention>> endMentions = Generics.NewHashMap();
			foreach (Mention m in mentions)
			{
				//      if(!gold && (document.corefClusters.get(m.corefClusterID)==null || document.corefClusters.get(m.corefClusterID).getCorefMentions().size()<=1)) {
				//        continue;
				//      }
				startCounts.IncrementCount(m.startIndex);
				endCounts.IncrementCount(m.endIndex);
				if (!endMentions.Contains(m.endIndex))
				{
					endMentions[m.endIndex] = new ArrayDeque<Mention>();
				}
				endMentions[m.endIndex].Push(m);
			}
			for (int j = 0; j < tokens.Length; j++)
			{
				if (endMentions.Contains(j))
				{
					foreach (Mention m_1 in endMentions[j])
					{
						int id = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
						id = (printClusterID) ? id : m_1.mentionID;
						sentStr.Append("]_").Append(id);
					}
				}
				for (int k = 0; k < startCounts.GetCount(j); k++)
				{
					if (sentStr.Length > 0 && sentStr[sentStr.Length - 1] != '[')
					{
						sentStr.Append(" ");
					}
					sentStr.Append("[");
				}
				if (sentStr.Length > 0 && sentStr[sentStr.Length - 1] != '[')
				{
					sentStr.Append(" ");
				}
				sentStr.Append(tokens[j]);
			}
			if (endMentions.Contains(tokens.Length))
			{
				foreach (Mention m_1 in endMentions[tokens.Length])
				{
					int id = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
					id = (printClusterID) ? id : m_1.mentionID;
					sentStr.Append("]_").Append(id);
				}
			}
			//append("_").append(m.mentionID);
			//    sentStr.append("\n");
			return sentStr.ToString();
		}

		public static string PrintMentionDetectionLog(Document document)
		{
			StringBuilder sbLog = new StringBuilder();
			IList<ICoreMap> sentences = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			sbLog.Append("\nERROR START-----------------------------------------------------------------------\n");
			for (int i = 0; i < sentences.Count; i++)
			{
				sbLog.Append("\nSENT ").Append(i).Append(" GOLD   : ").Append(HybridCorefPrinter.SentenceStringWithMention(i, document, true, false)).Append("\n");
				sbLog.Append("SENT ").Append(i).Append(" PREDICT: ").Append(HybridCorefPrinter.SentenceStringWithMention(i, document, false, false)).Append("\n");
				//      for(CoreLabel cl : sentences.get(i).get(TokensAnnotation.class)) {
				//        sbLog.append(cl.word()).append("-").append(cl.get(UtteranceAnnotation.class)).append("-").append(cl.get(SpeakerAnnotation.class)).append(" ");
				//      }
				foreach (Mention p in document.predictedMentions[i])
				{
					sbLog.Append("\n");
					if (!p.hasTwin)
					{
						sbLog.Append("\tSPURIOUS");
					}
					sbLog.Append("\tmention: ").Append(p.SpanToString()).Append("\t\t\theadword: ").Append(p.headString).Append("\tPOS: ").Append(p.headWord.Tag()).Append("\tmentiontype: ").Append(p.mentionType).Append("\tnumber: ").Append(p.number).Append("\tgender: "
						).Append(p.gender).Append("\tanimacy: ").Append(p.animacy).Append("\tperson: ").Append(p.person).Append("\tNE: ").Append(p.nerString);
				}
				sbLog.Append("\n");
				foreach (Mention g in document.goldMentions[i])
				{
					if (!g.hasTwin)
					{
						sbLog.Append("\tmissed gold: ").Append(g.SpanToString()).Append("\tPOS: ").Append(g.headWord.Tag()).Append("\tmentiontype: ").Append(g.mentionType).Append("\theadword: ").Append(g.headString).Append("\tnumber: ").Append(g.number).Append("\tgender: "
							).Append(g.gender).Append("\tanimacy: ").Append(g.animacy).Append("\tperson: ").Append(g.person).Append("\tNE: ").Append(g.nerString).Append("\n");
						if (g.sentenceWords != null)
						{
							if (g.sentenceWords.Count > g.endIndex)
							{
								sbLog.Append("\tnextword: ").Append(g.sentenceWords[g.endIndex]).Append("\t").Append(g.sentenceWords[g.endIndex].Tag()).Append("\n");
							}
						}
						if (g.contextParseTree != null)
						{
							sbLog.Append(g.contextParseTree.PennString()).Append("\n\n");
						}
						else
						{
							sbLog.Append("\n\n");
						}
					}
				}
				if (sentences[i].Get(typeof(TreeCoreAnnotations.TreeAnnotation)) != null)
				{
					sbLog.Append("\n\tparse: \n").Append(sentences[i].Get(typeof(TreeCoreAnnotations.TreeAnnotation)).PennString());
				}
				sbLog.Append("\n\tcollapsedDependency: \n").Append(sentences[i].Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)));
			}
			sbLog.Append("ERROR END -----------------------------------------------------------------------\n");
			return sbLog.ToString();
		}

		/// <exception cref="System.Exception"/>
		public static string PrintErrorLogDcoref(Mention m, Mention found, Document document, Dictionaries dict, int mIdx, string whichResolver)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\nERROR START-----------------------------------------------------------------------\n");
			sb.Append("RESOLVER TYPE: ").Append(whichResolver).Append("\n");
			sb.Append("DOCUMENT: " + document.docInfo["DOC_ID"] + ", " + document.docInfo["DOC_PART"]).Append("\n");
			IList<Mention> orderedAnts = new List<Mention>();
			sb.Append("\nGOLD CLUSTER ID\n");
			for (int sentDist = m.sentNum; sentDist >= 0; sentDist--)
			{
				int sentIdx = m.sentNum - sentDist;
				sb.Append("\tSENT " + sentIdx + "\t" + SentenceStringWithMention(sentIdx, document, true, true)).Append("\n");
			}
			sb.Append("\nMENTION ID\n");
			for (int sentDist_1 = m.sentNum; sentDist_1 >= 0; sentDist_1--)
			{
				int sentIdx = m.sentNum - sentDist_1;
				sb.Append("\tSENT " + sentIdx + "\t" + SentenceStringWithMention(sentIdx, document, false, false)).Append("\n");
			}
			// get dcoref antecedents ordering
			for (int sentDist_2 = 0; sentDist_2 <= m.sentNum; sentDist_2++)
			{
				int sentIdx = m.sentNum - sentDist_2;
				Sharpen.Collections.AddAll(orderedAnts, Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.GetOrderedAntecedents(m, sentIdx, mIdx, document.predictedMentions, dict));
			}
			IDictionary<int, int> orders = Generics.NewHashMap();
			for (int i = 0; i < orderedAnts.Count; i++)
			{
				Mention ant = orderedAnts[i];
				orders[ant.mentionID] = i;
			}
			CorefCluster mC = document.corefClusters[m.corefClusterID];
			bool isFirstMention = IsFirstMention(m, document);
			bool foundCorefAnt = true;
			// we're printing only mentions that found coref antecedent
			bool correctDecision = document.IsCoref(m, found);
			if (correctDecision)
			{
				return string.Empty;
			}
			sb.Append("\nMENTION: " + m.SpanToString() + " (" + m.mentionID + ")\tperson: " + m.person + "\tsingleton? " + (!m.hasTwin) + "\t\tisFirstMention? " + isFirstMention + "\t\tfoundAnt? " + foundCorefAnt + "\t\tcorrectDecision? " + correctDecision
				);
			sb.Append("\n\ttype: " + m.mentionType + "\tHeadword: " + m.headWord.Word() + "\tNEtype: " + m.nerString + "\tnumber: " + m.number + "\tgender: " + m.gender + "\tanimacy: " + m.animacy).Append("\n");
			if (m.contextParseTree != null)
			{
				sb.Append(m.contextParseTree.PennString());
			}
			sb.Append("\n\n\t\tOracle\t\tDcoref\t\t\tRF\t\tAntecedent\n");
			foreach (Mention ant_1 in orderedAnts)
			{
				int antID = ant_1.mentionID;
				CorefCluster aC = document.corefClusters[ant_1.corefClusterID];
				bool oracle = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.IsReallyCoref(document, m.mentionID, antID);
				int order = orders[antID];
				string oracleStr = (oracle) ? "coref   " : "notcoref";
				//      String dcorefStr = (dcoref)? "coref   " : "notcoref";
				string dcorefStr = "notcoref";
				if (dcorefSpeaker.Coreferent(document, mC, aC, m, ant_1, dict, null))
				{
					dcorefStr = "coref-speaker";
				}
				else
				{
					//      else if(dcorefChineseHeadMatch.coreferent(document, mC, aC, m, ant, dict, null)) dcorefStr = "coref-chineseHeadMatch";
					if (dcorefDiscourse.Coreferent(document, mC, aC, m, ant_1, dict, null))
					{
						dcorefStr = "coref-discourse";
					}
					else
					{
						if (dcorefExactString.Coreferent(document, mC, aC, m, ant_1, dict, null))
						{
							dcorefStr = "coref-exactString";
						}
						else
						{
							if (dcorefRelaxedExactString.Coreferent(document, mC, aC, m, ant_1, dict, null))
							{
								dcorefStr = "coref-relaxedExact";
							}
							else
							{
								if (dcorefPreciseConstructs.Coreferent(document, mC, aC, m, ant_1, dict, null))
								{
									dcorefStr = "coref-preciseConstruct";
								}
								else
								{
									if (dcorefHead1.Coreferent(document, mC, aC, m, ant_1, dict, null))
									{
										dcorefStr = "coref-head1";
									}
									else
									{
										if (dcorefHead2.Coreferent(document, mC, aC, m, ant_1, dict, null))
										{
											dcorefStr = "coref-head2";
										}
										else
										{
											if (dcorefHead3.Coreferent(document, mC, aC, m, ant_1, dict, null))
											{
												dcorefStr = "coref-head3";
											}
											else
											{
												if (dcorefHead4.Coreferent(document, mC, aC, m, ant_1, dict, null))
												{
													dcorefStr = "coref-head4";
												}
												else
												{
													if (dcorefRelaxedHead.Coreferent(document, mC, aC, m, ant_1, dict, null))
													{
														dcorefStr = "coref-relaxedHead";
													}
													else
													{
														if (dcorefPronounSieve.Coreferent(document, mC, aC, m, ant_1, dict, null))
														{
															dcorefStr = "coref-pronounSieve";
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				dcorefStr += "\t" + order.ToString();
				sb.Append("\t\t" + oracleStr + "\t" + dcorefStr + "\t\t" + ant_1.SpanToString() + " (" + ant_1.mentionID + ")\n");
			}
			sb.Append("ERROR END -----------------------------------------------------------------------\n");
			return sb.ToString();
		}

		/// <exception cref="System.Exception"/>
		public static void LinkDistanceAnalysis(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			HybridCorefSystem cs = new HybridCorefSystem(props);
			cs.docMaker.ResetDocs();
			ICounter<int> proper = new ClassicCounter<int>();
			ICounter<int> common = new ClassicCounter<int>();
			ICounter<int> pronoun = new ClassicCounter<int>();
			ICounter<int> list = new ClassicCounter<int>();
			while (true)
			{
				Document document = cs.docMaker.NextDoc();
				if (document == null)
				{
					break;
				}
				for (int sentIdx = 0; sentIdx < document.predictedMentions.Count; sentIdx++)
				{
					IList<Mention> predictedInSent = document.predictedMentions[sentIdx];
					for (int mIdx = 0; mIdx < predictedInSent.Count; mIdx++)
					{
						Mention m = predictedInSent[mIdx];
						for (int distance = 0; distance <= sentIdx; distance++)
						{
							IList<Mention> candidates = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.GetOrderedAntecedents(m, sentIdx - distance, mIdx, document.predictedMentions, cs.dictionaries);
							foreach (Mention candidate in candidates)
							{
								if (candidate == m)
								{
									continue;
								}
								if (distance == 0 && m.AppearEarlierThan(candidate))
								{
									continue;
								}
								// ignore cataphora
								if (candidate.goldCorefClusterID == m.goldCorefClusterID)
								{
									switch (m.mentionType)
									{
										case Dictionaries.MentionType.Nominal:
										{
											if (candidate.mentionType == Dictionaries.MentionType.Nominal || candidate.mentionType == Dictionaries.MentionType.Proper)
											{
												common.IncrementCount(distance);
												goto loop_break;
											}
											break;
										}

										case Dictionaries.MentionType.Proper:
										{
											if (candidate.mentionType == Dictionaries.MentionType.Proper)
											{
												proper.IncrementCount(distance);
												goto loop_break;
											}
											break;
										}

										case Dictionaries.MentionType.Pronominal:
										{
											pronoun.IncrementCount(distance);
											goto loop_break;
										}

										case Dictionaries.MentionType.List:
										{
											if (candidate.mentionType == Dictionaries.MentionType.List)
											{
												list.IncrementCount(distance);
												goto loop_break;
											}
											break;
										}

										default:
										{
											break;
										}
									}
								}
							}
loop_continue: ;
						}
loop_break: ;
					}
				}
			}
			System.Console.Out.WriteLine("PROPER -------------------------------------------");
			Counters.PrintCounterSortedByKeys(proper);
			System.Console.Out.WriteLine("COMMON -------------------------------------------");
			Counters.PrintCounterSortedByKeys(common);
			System.Console.Out.WriteLine("PRONOUN -------------------------------------------");
			Counters.PrintCounterSortedByKeys(pronoun);
			System.Console.Out.WriteLine("LIST -------------------------------------------");
			Counters.PrintCounterSortedByKeys(list);
			log.Info();
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			LinkDistanceAnalysis(args);
		}
	}
}
