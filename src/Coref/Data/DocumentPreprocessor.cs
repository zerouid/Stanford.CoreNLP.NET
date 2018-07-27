using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>Coref document preprocessor.</summary>
	/// <author>Heeyoung Lee</author>
	/// <author>Kevin Clark</author>
	public class DocumentPreprocessor
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Data.DocumentPreprocessor));

		private DocumentPreprocessor()
		{
		}

		/// <summary>Fill missing information in document including mention ID, mention attributes, syntactic relation, etc.</summary>
		/// <exception cref="System.Exception"/>
		public static void Preprocess(Document doc, Dictionaries dict, LogisticClassifier<string, string> singletonPredictor, IHeadFinder headFinder)
		{
			// assign mention IDs, find twin mentions, fill mention positions, sentNum, headpositions
			InitializeMentions(doc, dict, singletonPredictor, headFinder);
			// mention reordering
			MentionReordering(doc, headFinder);
			// find syntactic information
			FillSyntacticInfo(doc);
			// process discourse (speaker info etc)
			SetParagraphAnnotation(doc);
			ProcessDiscourse(doc, dict);
			// initialize cluster info
			InitializeClusters(doc);
			// extract gold clusters if we have
			if (doc.goldMentions != null)
			{
				ExtractGoldClusters(doc);
				int foundGoldCount = 0;
				foreach (Mention g in doc.goldMentionsByID.Values)
				{
					if (g.hasTwin)
					{
						foundGoldCount++;
					}
				}
				Redwood.Log("debug-md", "# of found gold mentions: " + foundGoldCount + " / # of gold mentions: " + doc.goldMentionsByID.Count);
			}
			// assign mention numbers
			AssignMentionNumbers(doc);
		}

		/// <summary>Extract gold coref cluster information.</summary>
		public static void ExtractGoldClusters(Document doc)
		{
			doc.goldCorefClusters = Generics.NewHashMap();
			foreach (IList<Mention> mentions in doc.goldMentions)
			{
				foreach (Mention m in mentions)
				{
					int id = m.goldCorefClusterID;
					if (id == -1)
					{
						throw new Exception("No gold info");
					}
					CorefCluster c = doc.goldCorefClusters[id];
					if (c == null)
					{
						c = new CorefCluster(id);
						doc.goldCorefClusters[id] = c;
					}
					c.corefMentions.Add(m);
				}
			}
		}

		private static void AssignMentionNumbers(Document document)
		{
			IList<Mention> mentionsList = CorefUtils.GetSortedMentions(document);
			for (int i = 0; i < mentionsList.Count; i++)
			{
				mentionsList[i].mentionNum = i;
			}
		}

		/// <exception cref="System.Exception"/>
		private static void MentionReordering(Document doc, IHeadFinder headFinder)
		{
			IList<IList<Mention>> mentions = doc.predictedMentions;
			IList<ICoreMap> sentences = doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < sentences.Count; i++)
			{
				IList<Mention> mentionsInSent = mentions[i];
				mentions.Set(i, MentionReorderingBySpan(mentionsInSent));
			}
		}

		protected internal static int GetHeadIndex(Tree t, IHeadFinder headFinder)
		{
			// The trees passed in do not have the CoordinationTransformer
			// applied, but that just means the SemanticHeadFinder results are
			// slightly worse.
			Tree ht = t.HeadTerminal(headFinder);
			if (ht == null)
			{
				return -1;
			}
			// temporary: a key which is matched to nothing
			CoreLabel l = (CoreLabel)ht.Label();
			return l.Get(typeof(CoreAnnotations.IndexAnnotation));
		}

		private static IList<Mention> MentionReorderingBySpan(IList<Mention> mentionsInSent)
		{
			TreeSet<Mention> ordering = new TreeSet<Mention>(new _IComparator_141());
			Sharpen.Collections.AddAll(ordering, mentionsInSent);
			IList<Mention> orderedMentions = Generics.NewArrayList(ordering);
			return orderedMentions;
		}

		private sealed class _IComparator_141 : IComparator<Mention>
		{
			public _IComparator_141()
			{
			}

			public int Compare(Mention m1, Mention m2)
			{
				return (m1.AppearEarlierThan(m2)) ? -1 : (m2.AppearEarlierThan(m1)) ? 1 : 0;
			}
		}

		private static void FillSyntacticInfo(Document doc)
		{
			IList<IList<Mention>> mentions = doc.predictedMentions;
			IList<ICoreMap> sentences = doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < sentences.Count; i++)
			{
				IList<Mention> mentionsInSent = mentions[i];
				FindSyntacticRelationsFromDependency(mentionsInSent);
			}
		}

		/// <summary>assign mention IDs, find twin mentions, fill mention positions, initialize coref clusters, etc</summary>
		/// <exception cref="System.Exception"></exception>
		private static void InitializeMentions(Document doc, Dictionaries dict, LogisticClassifier<string, string> singletonPredictor, IHeadFinder headFinder)
		{
			bool hasGold = (doc.goldMentions != null);
			AssignMentionIDs(doc);
			if (hasGold)
			{
				FindTwinMentions(doc, true);
			}
			FillMentionInfo(doc, dict, singletonPredictor, headFinder);
			doc.allPositions = Generics.NewHashMap(doc.positions);
		}

		// allPositions retain all mentions even after postprocessing
		private static void AssignMentionIDs(Document doc)
		{
			bool hasGold = (doc.goldMentions != null);
			int maxID = 0;
			if (hasGold)
			{
				foreach (IList<Mention> golds in doc.goldMentions)
				{
					foreach (Mention g in golds)
					{
						g.mentionID = maxID++;
					}
				}
			}
			foreach (IList<Mention> predicted in doc.predictedMentions)
			{
				foreach (Mention p in predicted)
				{
					p.mentionID = maxID++;
				}
			}
		}

		/// <summary>Mark twin mentions in gold and predicted mentions</summary>
		protected internal static void FindTwinMentions(Document doc, bool strict)
		{
			if (strict)
			{
				FindTwinMentionsStrict(doc);
			}
			else
			{
				FindTwinMentionsRelaxed(doc);
			}
		}

		/// <summary>Mark twin mentions: All mention boundaries should be matched</summary>
		private static void FindTwinMentionsStrict(Document doc)
		{
			for (int sentNum = 0; sentNum < doc.goldMentions.Count; sentNum++)
			{
				IList<Mention> golds = doc.goldMentions[sentNum];
				IList<Mention> predicts = doc.predictedMentions[sentNum];
				// For CoNLL training there are some documents with gold mentions with the same position offsets
				// See /scr/nlp/data/conll-2011/v2/data/train/data/english/annotations/nw/wsj/09/wsj_0990.v2_auto_conll
				//  (Packwood - Roth)
				CollectionValuedMap<IntPair, Mention> goldMentionPositions = new CollectionValuedMap<IntPair, Mention>();
				foreach (Mention g in golds)
				{
					IntPair ip = new IntPair(g.startIndex, g.endIndex);
					if (goldMentionPositions.Contains(ip))
					{
						StringBuilder existingMentions = new StringBuilder();
						foreach (Mention eg in goldMentionPositions[ip])
						{
							if (existingMentions.Length > 0)
							{
								existingMentions.Append(",");
							}
							existingMentions.Append(eg.mentionID);
						}
						Redwood.Log("debug-preprocessor", "WARNING: gold mentions with the same offsets: " + ip + " mentions=" + g.mentionID + "," + existingMentions + ", " + g.SpanToString());
					}
					//assert(!goldMentionPositions.containsKey(ip));
					goldMentionPositions.Add(new IntPair(g.startIndex, g.endIndex), g);
				}
				foreach (Mention p in predicts)
				{
					IntPair pos = new IntPair(p.startIndex, p.endIndex);
					if (goldMentionPositions.Contains(pos))
					{
						ICollection<Mention> cm = goldMentionPositions[pos];
						int minId = int.MaxValue;
						Mention g_1 = null;
						foreach (Mention m in cm)
						{
							if (m.mentionID < minId)
							{
								g_1 = m;
								minId = m.mentionID;
							}
						}
						cm.Remove(g_1);
						p.mentionID = g_1.mentionID;
						p.hasTwin = true;
						g_1.hasTwin = true;
					}
				}
			}
		}

		/// <summary>Mark twin mentions: heads of the mentions are matched</summary>
		private static void FindTwinMentionsRelaxed(Document doc)
		{
			for (int sentNum = 0; sentNum < doc.goldMentions.Count; sentNum++)
			{
				IList<Mention> golds = doc.goldMentions[sentNum];
				IList<Mention> predicts = doc.predictedMentions[sentNum];
				IDictionary<IntPair, Mention> goldMentionPositions = Generics.NewHashMap();
				IDictionary<int, LinkedList<Mention>> goldMentionHeadPositions = Generics.NewHashMap();
				foreach (Mention g in golds)
				{
					goldMentionPositions[new IntPair(g.startIndex, g.endIndex)] = g;
					if (!goldMentionHeadPositions.Contains(g.headIndex))
					{
						goldMentionHeadPositions[g.headIndex] = new LinkedList<Mention>();
					}
					goldMentionHeadPositions[g.headIndex].Add(g);
				}
				IList<Mention> remains = new List<Mention>();
				foreach (Mention p in predicts)
				{
					IntPair pos = new IntPair(p.startIndex, p.endIndex);
					if (goldMentionPositions.Contains(pos))
					{
						Mention g_1 = goldMentionPositions[pos];
						p.mentionID = g_1.mentionID;
						p.hasTwin = true;
						g_1.hasTwin = true;
						goldMentionHeadPositions[g_1.headIndex].Remove(g_1);
						if (goldMentionHeadPositions[g_1.headIndex].IsEmpty())
						{
							Sharpen.Collections.Remove(goldMentionHeadPositions, g_1.headIndex);
						}
					}
					else
					{
						remains.Add(p);
					}
				}
				foreach (Mention r in remains)
				{
					if (goldMentionHeadPositions.Contains(r.headIndex))
					{
						Mention g_1 = goldMentionHeadPositions[r.headIndex].Poll();
						r.mentionID = g_1.mentionID;
						r.hasTwin = true;
						g_1.hasTwin = true;
						if (goldMentionHeadPositions[g_1.headIndex].IsEmpty())
						{
							Sharpen.Collections.Remove(goldMentionHeadPositions, g_1.headIndex);
						}
					}
				}
			}
		}

		/// <summary>initialize several variables for mentions</summary>
		/// <exception cref="System.Exception"/>
		private static void FillMentionInfo(Document doc, Dictionaries dict, LogisticClassifier<string, string> singletonPredictor, IHeadFinder headFinder)
		{
			IList<ICoreMap> sentences = doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < doc.predictedMentions.Count; i++)
			{
				ICoreMap sentence = sentences[i];
				for (int j = 0; j < doc.predictedMentions[i].Count; j++)
				{
					Mention m = doc.predictedMentions[i][j];
					doc.predictedMentionsByID[m.mentionID] = m;
					// mentionsByID
					IntTuple pos = new IntTuple(2);
					pos.Set(0, i);
					pos.Set(1, j);
					doc.positions[m] = pos;
					// positions
					m.sentNum = i;
					// sentNum
					IntTuple headPosition = new IntTuple(2);
					headPosition.Set(0, i);
					headPosition.Set(1, m.headIndex);
					doc.mentionheadPositions[headPosition] = m;
					// headPositions
					m.contextParseTree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					//        m.sentenceWords = sentence.get(TokensAnnotation.class);
					m.basicDependency = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					m.enhancedDependency = sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					if (m.enhancedDependency == null)
					{
						m.enhancedDependency = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					}
					// mentionSubTree (highest NP that has the same head) if constituency tree available
					if (m.contextParseTree != null)
					{
						Tree headTree = m.contextParseTree.GetLeaves()[m.headIndex];
						if (headTree == null)
						{
							throw new Exception("Missing head tree for a mention!");
						}
						Tree t = headTree;
						while ((t = t.Parent(m.contextParseTree)) != null)
						{
							if (t.HeadTerminal(headFinder) == headTree && t.Value().Equals("NP"))
							{
								m.mentionSubTree = t;
							}
							else
							{
								if (m.mentionSubTree != null)
								{
									break;
								}
							}
						}
						if (m.mentionSubTree == null)
						{
							m.mentionSubTree = headTree;
						}
					}
					m.Process(dict, null, singletonPredictor);
				}
			}
			bool hasGold = (doc.goldMentions != null);
			if (hasGold)
			{
				doc.goldMentionsByID = Generics.NewHashMap();
				int sentNum = 0;
				foreach (IList<Mention> golds in doc.goldMentions)
				{
					foreach (Mention g in golds)
					{
						doc.goldMentionsByID[g.mentionID] = g;
						g.sentNum = sentNum;
					}
					sentNum++;
				}
			}
		}

		private static void FindSyntacticRelationsFromDependency(IList<Mention> orderedMentions)
		{
			if (orderedMentions.Count == 0)
			{
				return;
			}
			MarkListMemberRelation(orderedMentions);
			SemanticGraph dependency = orderedMentions[0].enhancedDependency;
			// apposition
			ICollection<Pair<int, int>> appos = Generics.NewHashSet();
			IList<SemanticGraphEdge> appositions = dependency.FindAllRelns(UniversalEnglishGrammaticalRelations.AppositionalModifier);
			foreach (SemanticGraphEdge edge in appositions)
			{
				int sIdx = edge.GetSource().Index() - 1;
				int tIdx = edge.GetTarget().Index() - 1;
				appos.Add(Pair.MakePair(sIdx, tIdx));
			}
			MarkMentionRelation(orderedMentions, appos, "APPOSITION");
			// predicate nominatives
			ICollection<Pair<int, int>> preNomi = Generics.NewHashSet();
			IList<SemanticGraphEdge> copula = dependency.FindAllRelns(UniversalEnglishGrammaticalRelations.Copula);
			foreach (SemanticGraphEdge edge_1 in copula)
			{
				IndexedWord source = edge_1.GetSource();
				IndexedWord target = dependency.GetChildWithReln(source, UniversalEnglishGrammaticalRelations.NominalSubject);
				if (target == null)
				{
					target = dependency.GetChildWithReln(source, UniversalEnglishGrammaticalRelations.ClausalSubject);
				}
				// TODO
				if (target == null)
				{
					continue;
				}
				// to handle relative clause: e.g., Tim who is a student,
				if (target.Tag().StartsWith("W"))
				{
					IndexedWord parent = dependency.GetParent(source);
					if (parent != null && dependency.Reln(parent, source).Equals(UniversalEnglishGrammaticalRelations.RelativeClauseModifier))
					{
						target = parent;
					}
				}
				int sIdx = source.Index() - 1;
				int tIdx = target.Index() - 1;
				preNomi.Add(Pair.MakePair(tIdx, sIdx));
			}
			MarkMentionRelation(orderedMentions, preNomi, "PREDICATE_NOMINATIVE");
			// relative pronouns  TODO
			ICollection<Pair<int, int>> relativePronounPairs = Generics.NewHashSet();
			MarkMentionRelation(orderedMentions, relativePronounPairs, "RELATIVE_PRONOUN");
		}

		private static void InitializeClusters(Document doc)
		{
			foreach (IList<Mention> predicted in doc.predictedMentions)
			{
				foreach (Mention p in predicted)
				{
					doc.corefClusters[p.mentionID] = new CorefCluster(p.mentionID, Generics.NewHashSet(Arrays.AsList(p)));
					p.corefClusterID = p.mentionID;
				}
			}
			bool hasGold = (doc.goldMentions != null);
			if (hasGold)
			{
				foreach (IList<Mention> golds in doc.goldMentions)
				{
					foreach (Mention g in golds)
					{
						doc.goldMentionsByID[g.mentionID] = g;
					}
				}
			}
		}

		/// <summary>Find document type: Conversation or article</summary>
		private static Document.DocType FindDocType(Document doc)
		{
			bool speakerChange = false;
			foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel w in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					int utterIndex = w.Get(typeof(CoreAnnotations.UtteranceAnnotation));
					if (utterIndex != 0)
					{
						speakerChange = true;
					}
					if (speakerChange && utterIndex == 0)
					{
						return Document.DocType.Article;
					}
					if (doc.maxUtter < utterIndex)
					{
						doc.maxUtter = utterIndex;
					}
				}
			}
			if (!speakerChange)
			{
				return Document.DocType.Article;
			}
			return Document.DocType.Conversation;
		}

		// in conversation, utter index keep increasing.
		/// <summary>Set paragraph index</summary>
		private static void SetParagraphAnnotation(Document doc)
		{
			int paragraphIndex = 0;
			int previousOffset = -10;
			foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel w in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					if (w.ContainsKey(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
					{
						if (w.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) > previousOffset + 2)
						{
							paragraphIndex++;
						}
						w.Set(typeof(CoreAnnotations.ParagraphAnnotation), paragraphIndex);
						previousOffset = w.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					}
					else
					{
						w.Set(typeof(CoreAnnotations.ParagraphAnnotation), -1);
					}
				}
			}
			foreach (IList<Mention> l in doc.predictedMentions)
			{
				foreach (Mention m in l)
				{
					m.paragraph = m.headWord.Get(typeof(CoreAnnotations.ParagraphAnnotation));
				}
			}
			doc.numParagraph = paragraphIndex;
		}

		/// <summary>Process discourse information</summary>
		protected internal static void ProcessDiscourse(Document doc, Dictionaries dict)
		{
			bool useMarkedDiscourse = doc.annotation.Get(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation));
			if (useMarkedDiscourse == null || !useMarkedDiscourse)
			{
				foreach (CoreLabel l in doc.annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					l.Remove(typeof(CoreAnnotations.SpeakerAnnotation));
					l.Remove(typeof(CoreAnnotations.UtteranceAnnotation));
				}
			}
			SetUtteranceAndSpeakerAnnotation(doc);
			//    markQuotations(this.annotation.get(CoreAnnotations.SentencesAnnotation.class), false);
			// mention utter setting
			foreach (Mention m in doc.predictedMentionsByID.Values)
			{
				m.utter = m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation));
			}
			doc.docType = FindDocType(doc);
			FindSpeakers(doc, dict);
			bool debug = false;
			if (debug)
			{
				foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					foreach (CoreLabel cl in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						log.Info("   " + cl.Word() + "-" + cl.Get(typeof(CoreAnnotations.UtteranceAnnotation)) + "-" + cl.Get(typeof(CoreAnnotations.SpeakerAnnotation)));
					}
				}
				foreach (int utter in doc.speakers.Keys)
				{
					string speakerID = doc.speakers[utter];
					log.Info("utterance: " + utter);
					log.Info("speakers value: " + speakerID);
					log.Info("mention for it: " + ((NumberMatchingRegex.IsDecimalInteger(speakerID)) ? doc.predictedMentionsByID[System.Convert.ToInt32(doc.speakers[utter])] : "no mention for this speaker yet"));
				}
				log.Info("AA SPEAKERS: " + doc.speakers);
			}
			// build 'speakerInfo' from 'speakers'
			foreach (int utter_1 in doc.speakers.Keys)
			{
				string speaker = doc.speakers[utter_1];
				SpeakerInfo speakerInfo = doc.speakerInfoMap[speaker];
				if (speakerInfo == null)
				{
					doc.speakerInfoMap[speaker] = speakerInfo = new SpeakerInfo(speaker);
				}
			}
			if (debug)
			{
				log.Info("BB SPEAKER INFO MAP: " + doc.speakerInfoMap);
			}
			// mention -> to its speakerID: m.headWord.get(SpeakerAnnotation.class)
			// speakerID -> more info: speakerInfoMap.get(speakerID)
			// if exists, set(mentionID, its speakerID pair): speakerPairs
			// for speakerInfo with real speaker name, find corresponding mention by strict/loose matching
			IDictionary<string, int> speakerConversion = Generics.NewHashMap();
			foreach (string speaker_1 in doc.speakerInfoMap.Keys)
			{
				SpeakerInfo speakerInfo = doc.speakerInfoMap[speaker_1];
				if (speakerInfo.HasRealSpeakerName())
				{
					// do only for real name speaker, not mention ID
					bool found = false;
					foreach (Mention m_1 in doc.predictedMentionsByID.Values)
					{
						if (CorefRules.MentionMatchesSpeaker(m_1, speakerInfo, true))
						{
							speakerConversion[speaker_1] = m_1.mentionID;
							found = true;
							break;
						}
					}
					if (!found)
					{
						foreach (Mention m_2 in doc.predictedMentionsByID.Values)
						{
							if (CorefRules.MentionMatchesSpeaker(m_2, speakerInfo, false))
							{
								speakerConversion[speaker_1] = m_2.mentionID;
								break;
							}
						}
					}
				}
			}
			if (debug)
			{
				log.Info("CC speaker conversion: " + speakerConversion);
			}
			// convert real name speaker to speaker mention id
			foreach (int utter_2 in doc.speakers.Keys)
			{
				string speaker = doc.speakers[utter_2];
				if (speakerConversion.Contains(speaker_1))
				{
					int speakerID = speakerConversion[speaker_1];
					doc.speakers[utter_2] = int.ToString(speakerID);
				}
			}
			foreach (string speaker_2 in speakerConversion.Keys)
			{
				doc.speakerInfoMap[int.ToString(speakerConversion[speaker_2])] = doc.speakerInfoMap[speaker_2];
				Sharpen.Collections.Remove(doc.speakerInfoMap, speaker_2);
			}
			// fix SpeakerAnnotation
			foreach (CoreLabel cl_1 in doc.annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				int utter = cl_1.Get(typeof(CoreAnnotations.UtteranceAnnotation));
				if (doc.speakers.Contains(utter_2))
				{
					cl_1.Set(typeof(CoreAnnotations.SpeakerAnnotation), doc.speakers[utter_2]);
				}
			}
			// find speakerPairs
			foreach (Mention m_3 in doc.predictedMentionsByID.Values)
			{
				string speaker = m_3.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				if (debug)
				{
					log.Info("DD: " + speaker_2);
				}
				// if this is not a CoNLL doc, don't treat a number username as a speakerMentionID
				// conllDoc == null indicates not a CoNLL doc
				if (doc.conllDoc != null)
				{
					if (NumberMatchingRegex.IsDecimalInteger(speaker_2))
					{
						int speakerMentionID = System.Convert.ToInt32(speaker_2);
						doc.speakerPairs.Add(new Pair<int, int>(m_3.mentionID, speakerMentionID));
					}
				}
			}
			if (debug)
			{
				log.Info("==========================================================================");
				foreach (int utter in doc.speakers.Keys)
				{
					string speakerID = doc.speakers[utter_2];
					log.Info("utterance: " + utter_2);
					log.Info("speakers value: " + speakerID);
					log.Info("mention for it: " + ((NumberMatchingRegex.IsDecimalInteger(speakerID)) ? doc.predictedMentionsByID[System.Convert.ToInt32(doc.speakers[utter_2])] : "no mention for this speaker yet"));
				}
				log.Info(doc.speakers);
			}
		}

		private static void SetUtteranceAndSpeakerAnnotation(Document doc)
		{
			doc.speakerInfoGiven = false;
			int utterance = 0;
			int outsideQuoteUtterance = 0;
			// the utterance of outside of quotation
			bool insideQuotation = false;
			IList<CoreLabel> tokens = doc.annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			string preSpeaker = (tokens.Count > 0) ? tokens[0].Get(typeof(CoreAnnotations.SpeakerAnnotation)) : null;
			foreach (CoreLabel l in tokens)
			{
				string curSpeaker = l.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				string w = l.Get(typeof(CoreAnnotations.TextAnnotation));
				if (curSpeaker != null && !curSpeaker.Equals("-"))
				{
					doc.speakerInfoGiven = true;
				}
				bool speakerChange = doc.speakerInfoGiven && curSpeaker != null && !curSpeaker.Equals(preSpeaker);
				bool quoteStart = w.Equals("``") || (!insideQuotation && w.Equals("\""));
				bool quoteEnd = w.Equals("''") || (insideQuotation && w.Equals("\""));
				if (speakerChange)
				{
					if (quoteStart)
					{
						utterance = doc.maxUtter + 1;
						outsideQuoteUtterance = utterance + 1;
					}
					else
					{
						utterance = doc.maxUtter + 1;
						outsideQuoteUtterance = utterance;
					}
					preSpeaker = curSpeaker;
				}
				else
				{
					if (quoteStart)
					{
						utterance = doc.maxUtter + 1;
					}
				}
				if (quoteEnd)
				{
					utterance = outsideQuoteUtterance;
					insideQuotation = false;
				}
				if (doc.maxUtter < utterance)
				{
					doc.maxUtter = utterance;
				}
				l.Set(typeof(CoreAnnotations.UtteranceAnnotation), utterance);
				if (quoteStart)
				{
					l.Set(typeof(CoreAnnotations.UtteranceAnnotation), outsideQuoteUtterance);
				}
				// quote start got outside utterance idx
				bool noSpeakerInfo = !l.ContainsKey(typeof(CoreAnnotations.SpeakerAnnotation)) || l.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals(string.Empty) || l.Get(typeof(CoreAnnotations.SpeakerAnnotation)).StartsWith("PER");
				if (noSpeakerInfo || insideQuotation)
				{
					l.Set(typeof(CoreAnnotations.SpeakerAnnotation), "PER" + utterance);
				}
				if (quoteStart)
				{
					insideQuotation = true;
				}
			}
		}

		/// <summary>Speaker extraction</summary>
		private static void FindSpeakers(Document doc, Dictionaries dict)
		{
			bool useMarkedDiscourseBoolean = doc.annotation.Get(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation));
			bool useMarkedDiscourse = (useMarkedDiscourseBoolean != null) ? useMarkedDiscourseBoolean : false;
			if (!useMarkedDiscourse)
			{
				if (doc.docType == Document.DocType.Conversation)
				{
					FindSpeakersInConversation(doc, dict);
				}
				else
				{
					if (doc.docType == Document.DocType.Article)
					{
						FindSpeakersInArticle(doc, dict);
					}
				}
			}
			foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel w in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					int utterIndex = w.Get(typeof(CoreAnnotations.UtteranceAnnotation));
					if (!doc.speakers.Contains(utterIndex))
					{
						doc.speakers[utterIndex] = w.Get(typeof(CoreAnnotations.SpeakerAnnotation));
					}
				}
			}
		}

		private static void FindSpeakersInArticle(Document doc, Dictionaries dict)
		{
			IList<ICoreMap> sentences = doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IntPair beginQuotation = null;
			IntPair endQuotation = null;
			bool insideQuotation = false;
			int utterNum = -1;
			for (int i = 0; i < sentences.Count; i++)
			{
				IList<CoreLabel> sent = sentences[i].Get(typeof(CoreAnnotations.TokensAnnotation));
				for (int j = 0; j < sent.Count; j++)
				{
					int utterIndex = sent[j].Get(typeof(CoreAnnotations.UtteranceAnnotation));
					if (utterIndex != 0 && !insideQuotation)
					{
						utterNum = utterIndex;
						insideQuotation = true;
						beginQuotation = new IntPair(i, j);
					}
					else
					{
						if (utterIndex == 0 && insideQuotation)
						{
							insideQuotation = false;
							endQuotation = new IntPair(i, j);
							FindQuotationSpeaker(doc, utterNum, sentences, beginQuotation, endQuotation, dict);
						}
					}
				}
			}
			if (insideQuotation)
			{
				endQuotation = new IntPair(sentences.Count - 1, sentences[sentences.Count - 1].Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 1);
				FindQuotationSpeaker(doc, utterNum, sentences, beginQuotation, endQuotation, dict);
			}
		}

		private static void FindQuotationSpeaker(Document doc, int utterNum, IList<ICoreMap> sentences, IntPair beginQuotation, IntPair endQuotation, Dictionaries dict)
		{
			if (FindSpeaker(doc, utterNum, beginQuotation.Get(0), sentences, 0, beginQuotation.Get(1), dict))
			{
				return;
			}
			if (FindSpeaker(doc, utterNum, endQuotation.Get(0), sentences, endQuotation.Get(1), sentences[endQuotation.Get(0)].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
			{
				return;
			}
			if (beginQuotation.Get(1) <= 1 && beginQuotation.Get(0) > 0)
			{
				if (FindSpeaker(doc, utterNum, beginQuotation.Get(0) - 1, sentences, 0, sentences[beginQuotation.Get(0) - 1].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
				{
					return;
				}
			}
			if (endQuotation.Get(1) >= sentences[endQuotation.Get(0)].Size() - 2 && sentences.Count > endQuotation.Get(0) + 1)
			{
				if (FindSpeaker(doc, utterNum, endQuotation.Get(0) + 1, sentences, 0, sentences[endQuotation.Get(0) + 1].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
				{
					return;
				}
			}
		}

		private static bool FindSpeaker(Document doc, int utterNum, int sentNum, IList<ICoreMap> sentences, int startIndex, int endIndex, Dictionaries dict)
		{
			IList<CoreLabel> sent = sentences[sentNum].Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = startIndex; i < endIndex; i++)
			{
				CoreLabel cl = sent[i];
				if (cl.Get(typeof(CoreAnnotations.UtteranceAnnotation)) != 0)
				{
					continue;
				}
				string lemma = cl.Lemma();
				string word = cl.Word();
				if (dict.reportVerb.Contains(lemma) && cl.Tag().StartsWith("V"))
				{
					// find subject
					SemanticGraph dependency = sentences[sentNum].Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					if (dependency == null)
					{
						dependency = sentences[sentNum].Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					}
					IndexedWord w = dependency.GetNodeByWordPattern(word);
					if (w != null)
					{
						if (FindSubject(doc, dependency, w, sentNum, utterNum))
						{
							return true;
						}
						foreach (IndexedWord p in dependency.GetPathToRoot(w))
						{
							if (!p.Tag().StartsWith("V") && !p.Tag().StartsWith("MD"))
							{
								break;
							}
							if (FindSubject(doc, dependency, p, sentNum, utterNum))
							{
								return true;
							}
						}
					}
					else
					{
						// handling something like "was talking", "can tell"
						Redwood.Log("debug-preprocessor", "Cannot find node in dependency for word " + word);
					}
				}
			}
			return false;
		}

		private static bool FindSubject(Document doc, SemanticGraph dependency, IndexedWord w, int sentNum, int utterNum)
		{
			foreach (Pair<GrammaticalRelation, IndexedWord> child in dependency.ChildPairs(w))
			{
				if (child.First().GetShortName().Equals("nsubj"))
				{
					string subjectString = child.Second().Word();
					int subjectIndex = child.Second().Index();
					// start from 1
					IntTuple headPosition = new IntTuple(2);
					headPosition.Set(0, sentNum);
					headPosition.Set(1, subjectIndex - 1);
					string speaker;
					if (doc.mentionheadPositions.Contains(headPosition))
					{
						speaker = int.ToString(doc.mentionheadPositions[headPosition].mentionID);
					}
					else
					{
						speaker = subjectString;
					}
					doc.speakers[utterNum] = speaker;
					return true;
				}
			}
			return false;
		}

		private static void FindSpeakersInConversation(Document doc, Dictionaries dict)
		{
			foreach (IList<Mention> l in doc.predictedMentions)
			{
				foreach (Mention m in l)
				{
					if (m.predicateNominatives == null)
					{
						continue;
					}
					foreach (Mention a in m.predicateNominatives)
					{
						if (a.SpanToString().ToLower().Equals("i"))
						{
							doc.speakers[m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))] = int.ToString(m.mentionID);
						}
					}
				}
			}
			IList<ICoreMap> paragraph = new List<ICoreMap>();
			int paragraphUtterIndex = 0;
			string nextParagraphSpeaker = string.Empty;
			int paragraphOffset = 0;
			foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				paragraph.Add(sent);
				int currentUtter = sent.Get(typeof(CoreAnnotations.TokensAnnotation))[0].Get(typeof(CoreAnnotations.UtteranceAnnotation));
				if (paragraphUtterIndex != currentUtter)
				{
					nextParagraphSpeaker = FindParagraphSpeaker(doc, paragraph, paragraphUtterIndex, nextParagraphSpeaker, paragraphOffset, dict);
					paragraphUtterIndex = currentUtter;
					paragraphOffset += paragraph.Count;
					paragraph = new List<ICoreMap>();
				}
			}
			FindParagraphSpeaker(doc, paragraph, paragraphUtterIndex, nextParagraphSpeaker, paragraphOffset, dict);
		}

		private static string FindParagraphSpeaker(Document doc, IList<ICoreMap> paragraph, int paragraphUtterIndex, string nextParagraphSpeaker, int paragraphOffset, Dictionaries dict)
		{
			if (!doc.speakers.Contains(paragraphUtterIndex))
			{
				if (!nextParagraphSpeaker.IsEmpty())
				{
					doc.speakers[paragraphUtterIndex] = nextParagraphSpeaker;
				}
				else
				{
					// find the speaker of this paragraph (John, nbc news)
					// cdm [Sept 2015] added this check to try to avoid crash
					if (paragraph.IsEmpty())
					{
						Redwood.Log("debug-preprocessor", "Empty paragraph; skipping findParagraphSpeaker");
						return string.Empty;
					}
					ICoreMap lastSent = paragraph[paragraph.Count - 1];
					string speaker = string.Empty;
					bool hasVerb = false;
					for (int i = 0; i < lastSent.Get(typeof(CoreAnnotations.TokensAnnotation)).Count; i++)
					{
						CoreLabel w = lastSent.Get(typeof(CoreAnnotations.TokensAnnotation))[i];
						string pos = w.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
						string ner = w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
						if (pos.StartsWith("V"))
						{
							hasVerb = true;
							break;
						}
						if (ner.StartsWith("PER"))
						{
							IntTuple headPosition = new IntTuple(2);
							headPosition.Set(0, paragraph.Count - 1 + paragraphOffset);
							headPosition.Set(1, i);
							if (doc.mentionheadPositions.Contains(headPosition))
							{
								speaker = int.ToString(doc.mentionheadPositions[headPosition].mentionID);
							}
						}
					}
					if (!hasVerb && !speaker.Equals(string.Empty))
					{
						doc.speakers[paragraphUtterIndex] = speaker;
					}
				}
			}
			return FindNextParagraphSpeaker(doc, paragraph, paragraphOffset, dict);
		}

		private static string FindNextParagraphSpeaker(Document doc, IList<ICoreMap> paragraph, int paragraphOffset, Dictionaries dict)
		{
			if (paragraph.IsEmpty())
			{
				return string.Empty;
			}
			ICoreMap lastSent = paragraph[paragraph.Count - 1];
			string speaker = string.Empty;
			foreach (CoreLabel w in lastSent.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (w.Get(typeof(CoreAnnotations.LemmaAnnotation)).Equals("report") || w.Get(typeof(CoreAnnotations.LemmaAnnotation)).Equals("say"))
				{
					string word = w.Get(typeof(CoreAnnotations.TextAnnotation));
					SemanticGraph dependency = lastSent.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					if (dependency == null)
					{
						dependency = lastSent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					}
					IndexedWord t = dependency.GetNodeByWordPattern(word);
					foreach (Pair<GrammaticalRelation, IndexedWord> child in dependency.ChildPairs(t))
					{
						if (child.First().GetShortName().Equals("nsubj"))
						{
							int subjectIndex = child.Second().Index();
							// start from 1
							IntTuple headPosition = new IntTuple(2);
							headPosition.Set(0, paragraph.Count - 1 + paragraphOffset);
							headPosition.Set(1, subjectIndex - 1);
							if (doc.mentionheadPositions.Contains(headPosition) && doc.mentionheadPositions[headPosition].nerString.StartsWith("PER"))
							{
								speaker = int.ToString(doc.mentionheadPositions[headPosition].mentionID);
							}
						}
					}
				}
			}
			return speaker;
		}

		/// <summary>Check one mention is the speaker of the other mention</summary>
		public static bool IsSpeaker(Mention m, Mention ant, Dictionaries dict)
		{
			if (!dict.firstPersonPronouns.Contains(ant.SpanToString().ToLower()) || ant.number == Dictionaries.Number.Plural || ant.sentNum != m.sentNum)
			{
				return false;
			}
			int countQuotationMark = 0;
			for (int i = System.Math.Min(m.headIndex, ant.headIndex) + 1; i < System.Math.Max(m.headIndex, ant.headIndex); i++)
			{
				string word = m.sentenceWords[i].Get(typeof(CoreAnnotations.TextAnnotation));
				if (word.Equals("``") || word.Equals("''"))
				{
					countQuotationMark++;
				}
			}
			if (countQuotationMark != 1)
			{
				return false;
			}
			IndexedWord w = m.enhancedDependency.GetNodeByWordPattern(m.sentenceWords[m.headIndex].Get(typeof(CoreAnnotations.TextAnnotation)));
			if (w == null)
			{
				return false;
			}
			foreach (Pair<GrammaticalRelation, IndexedWord> parent in m.enhancedDependency.ParentPairs(w))
			{
				if (parent.First().GetShortName().Equals("nsubj") && dict.reportVerb.Contains(parent.Second().Get(typeof(CoreAnnotations.LemmaAnnotation))))
				{
					return true;
				}
			}
			return false;
		}

		private static void MarkListMemberRelation(IList<Mention> orderedMentions)
		{
			foreach (Mention m1 in orderedMentions)
			{
				foreach (Mention m2 in orderedMentions)
				{
					// Mark if m2 and m1 are in list relationship
					if (m1.IsListMemberOf(m2))
					{
						m2.AddListMember(m1);
						m1.AddBelongsToList(m2);
					}
					else
					{
						if (m2.IsListMemberOf(m1))
						{
							m1.AddListMember(m2);
							m2.AddBelongsToList(m1);
						}
					}
				}
			}
		}

		private static void MarkMentionRelation(IList<Mention> orderedMentions, ICollection<Pair<int, int>> foundPairs, string flag)
		{
			foreach (Mention m1 in orderedMentions)
			{
				foreach (Mention m2 in orderedMentions)
				{
					if (m1 == m2)
					{
						continue;
					}
					// Ignore if m2 and m1 are in list relationship
					if (m1.IsListMemberOf(m2) || m2.IsListMemberOf(m1) || m1.IsMemberOfSameList(m2))
					{
						//Redwood.log("debug-preprocessor", "Not checking '" + m1 + "' and '" + m2 + "' for " + flag + ": in list relationship");
						continue;
					}
					foreach (Pair<int, int> foundPair in foundPairs)
					{
						if (foundPair.First() == m1.headIndex && foundPair.Second() == m2.headIndex)
						{
							if (flag.Equals("APPOSITION"))
							{
								if (!foundPair.First().Equals(foundPair.Second()) || m2.InsideIn(m1))
								{
									m2.AddApposition(m1);
								}
							}
							else
							{
								if (flag.Equals("PREDICATE_NOMINATIVE"))
								{
									m2.AddPredicateNominatives(m1);
								}
								else
								{
									if (flag.Equals("RELATIVE_PRONOUN"))
									{
										m2.AddRelativePronoun(m1);
									}
									else
									{
										throw new Exception("check flag in markMentionRelation (dcoref/MentionExtractor.java)");
									}
								}
							}
						}
					}
				}
			}
		}
		//  private static final TregexPattern relativePronounPattern = TregexPattern.compile("NP < (NP=m1 $.. (SBAR < (WHNP < WP|WDT=m2)))");
		//  private static void findRelativePronouns(Tree tree, Set<Pair<Integer, Integer>> relativePronounPairs) {
		//    findTreePattern(tree, relativePronounPattern, relativePronounPairs);
		//  }
	}
}
