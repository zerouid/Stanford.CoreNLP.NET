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
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Dcoref
{
	[System.Serializable]
	public class Document
	{
		private const long serialVersionUID = -4139866807494603953L;

		public enum DocType
		{
			Conversation,
			Article
		}

		/// <summary>The type of document: conversational or article</summary>
		public Document.DocType docType;

		/// <summary>Document annotation</summary>
		public Annotation annotation;

		/// <summary>for conll shared task 2011</summary>
		public CoNLL2011DocumentReader.Document conllDoc;

		/// <summary>The list of gold mentions</summary>
		public IList<IList<Mention>> goldOrderedMentionsBySentence;

		/// <summary>The list of predicted mentions</summary>
		public IList<IList<Mention>> predictedOrderedMentionsBySentence;

		/// <summary>return the list of predicted mentions</summary>
		public virtual IList<IList<Mention>> GetOrderedMentions()
		{
			return predictedOrderedMentionsBySentence;
		}

		/// <summary>Clusters for coreferent mentions</summary>
		public IDictionary<int, CorefCluster> corefClusters;

		/// <summary>Gold Clusters for coreferent mentions</summary>
		public IDictionary<int, CorefCluster> goldCorefClusters;

		/// <summary>For all mentions in a document, map mentionID to mention.</summary>
		public IDictionary<int, Mention> allPredictedMentions;

		public IDictionary<int, Mention> allGoldMentions;

		/// <summary>Set of roles (in role apposition) in a document</summary>
		public ICollection<Mention> roleSet;

		/// <summary>
		/// Position of each mention in the input matrix
		/// Each mention occurrence with sentence # and position within sentence
		/// (Nth mention, not Nth token)
		/// </summary>
		public IDictionary<Mention, IntTuple> positions;

		public IDictionary<Mention, IntTuple> allPositions;

		public readonly IDictionary<IntTuple, Mention> mentionheadPositions;

		/// <summary>List of gold links in a document by positions</summary>
		private IList<Pair<IntTuple, IntTuple>> goldLinks;

		/// <summary>Map UtteranceAnnotation to String (speaker): mention ID or speaker string</summary>
		public IDictionary<int, string> speakers;

		/// <summary>Pair of mention id, and the mention's speaker id</summary>
		public ICollection<Pair<int, int>> speakerPairs;

		public int maxUtter;

		public int numParagraph;

		public int numSentences;

		/// <summary>Set of incompatible clusters pairs</summary>
		private TwoDimensionalSet<int, int> incompatibles;

		private TwoDimensionalSet<int, int> incompatibleClusters;

		protected internal TwoDimensionalMap<int, int, bool> acronymCache;

		/// <summary>Map of speaker name/id to speaker info</summary>
		[System.NonSerialized]
		private IDictionary<string, SpeakerInfo> speakerInfoMap = Generics.NewHashMap();

		public Document()
		{
			// mentions may be removed from this due to post processing
			// all mentions (mentions will not be removed from this)
			positions = Generics.NewHashMap();
			mentionheadPositions = Generics.NewHashMap();
			roleSet = Generics.NewHashSet();
			corefClusters = Generics.NewHashMap();
			goldCorefClusters = null;
			allPredictedMentions = Generics.NewHashMap();
			allGoldMentions = Generics.NewHashMap();
			speakers = Generics.NewHashMap();
			speakerPairs = Generics.NewHashSet();
			incompatibles = TwoDimensionalSet.HashSet();
			incompatibleClusters = TwoDimensionalSet.HashSet();
			acronymCache = TwoDimensionalMap.HashMap();
		}

		public Document(Annotation anno, IList<IList<Mention>> predictedMentions, IList<IList<Mention>> goldMentions, Dictionaries dict)
			: this()
		{
			annotation = anno;
			numSentences = anno.Get(typeof(CoreAnnotations.SentencesAnnotation)).Count;
			predictedOrderedMentionsBySentence = predictedMentions;
			goldOrderedMentionsBySentence = goldMentions;
			if (goldMentions != null)
			{
				FindTwinMentions(true);
				// fill allGoldMentions
				foreach (IList<Mention> l in goldOrderedMentionsBySentence)
				{
					foreach (Mention g in l)
					{
						allGoldMentions[g.mentionID] = g;
					}
				}
			}
			// set original ID, initial coref clusters, paragraph annotation, mention positions
			Initialize();
			ProcessDiscourse(dict);
			PrintMentionDetection();
		}

		/// <summary>Process discourse information</summary>
		protected internal virtual void ProcessDiscourse(Dictionaries dict)
		{
			docType = FindDocType(dict);
			MarkQuotations(this.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)), false);
			FindSpeakers(dict);
			// find 'speaker mention' for each mention
			foreach (Mention m in allPredictedMentions.Values)
			{
				int utter = m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation));
				string speaker = m.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				if (speaker != null)
				{
					// Populate speaker info
					SpeakerInfo speakerInfo = speakerInfoMap[speaker];
					if (speakerInfo == null)
					{
						speakerInfoMap[speaker] = speakerInfo = new SpeakerInfo(speaker);
						// span indicates this is the speaker
						if (Rules.MentionMatchesSpeaker(m, speakerInfo, true))
						{
							m.speakerInfo = speakerInfo;
						}
					}
					if (NumberMatchingRegex.IsDecimalInteger(speaker))
					{
						try
						{
							int speakerMentionID = System.Convert.ToInt32(speaker);
							if (utter != 0)
							{
								// Add pairs of mention id and the mention id of the speaker
								speakerPairs.Add(new Pair<int, int>(m.mentionID, speakerMentionID));
							}
						}
						catch (Exception)
						{
						}
					}
				}
				//              speakerPairs.add(new Pair<Integer, Integer>(speakerMentionID, m.mentionID));
				// no mention found for the speaker
				// nothing to do
				// set generic 'you' : e.g., you know in conversation
				if (docType != Document.DocType.Article && m.person == Dictionaries.Person.You && m.endIndex < m.sentenceWords.Count - 1 && Sharpen.Runtime.EqualsIgnoreCase(m.sentenceWords[m.endIndex].Get(typeof(CoreAnnotations.TextAnnotation)), "know"))
				{
					m.generic = true;
				}
			}
			// now that we have identified the speakers, first pass to check if mentions should cluster with the speakers
			foreach (Mention m_1 in allPredictedMentions.Values)
			{
				if (m_1.speakerInfo == null)
				{
					foreach (SpeakerInfo speakerInfo in speakerInfoMap.Values)
					{
						if (speakerInfo.HasRealSpeakerName())
						{
							// do loose match - assumes that there isn't that many speakers....
							if (Rules.MentionMatchesSpeaker(m_1, speakerInfo, false))
							{
								m_1.speakerInfo = speakerInfo;
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>Document initialize</summary>
		protected internal virtual void Initialize()
		{
			if (goldOrderedMentionsBySentence == null)
			{
				AssignOriginalID();
			}
			SetParagraphAnnotation();
			InitializeCorefCluster();
			this.allPositions = Generics.NewHashMap(this.positions);
		}

		/// <summary>initialize positions and corefClusters (put each mention in each CorefCluster)</summary>
		private void InitializeCorefCluster()
		{
			for (int i = 0; i < predictedOrderedMentionsBySentence.Count; i++)
			{
				for (int j = 0; j < predictedOrderedMentionsBySentence[i].Count; j++)
				{
					Mention m = predictedOrderedMentionsBySentence[i][j];
					if (allPredictedMentions.Contains(m.mentionID))
					{
						SieveCoreferenceSystem.logger.Warning("WARNING: Already contain mention " + m.mentionID);
						Mention m1 = allPredictedMentions[m.mentionID];
						SieveCoreferenceSystem.logger.Warning("OLD mention: " + m1.SpanToString() + "[" + m1.startIndex + "," + m1.endIndex + "]");
						SieveCoreferenceSystem.logger.Warning("NEW mention: " + m.SpanToString() + "[" + m.startIndex + "," + m.endIndex + "]");
					}
					//          SieveCoreferenceSystem.debugPrintMentions(System.err, "PREDICTED ORDERED", predictedOrderedMentionsBySentence);
					//          SieveCoreferenceSystem.debugPrintMentions(System.err, "GOLD ORDERED", goldOrderedMentionsBySentence);
					System.Diagnostics.Debug.Assert((!allPredictedMentions.Contains(m.mentionID)));
					allPredictedMentions[m.mentionID] = m;
					IntTuple pos = new IntTuple(2);
					pos.Set(0, i);
					pos.Set(1, j);
					positions[m] = pos;
					m.sentNum = i;
					System.Diagnostics.Debug.Assert((!corefClusters.Contains(m.mentionID)));
					corefClusters[m.mentionID] = new CorefCluster(m.mentionID, Generics.NewHashSet(Java.Util.Collections.SingletonList(m)));
					m.corefClusterID = m.mentionID;
					IntTuple headPosition = new IntTuple(2);
					headPosition.Set(0, i);
					headPosition.Set(1, m.headIndex);
					mentionheadPositions[headPosition] = m;
				}
			}
		}

		public virtual bool IsIncompatible(CorefCluster c1, CorefCluster c2)
		{
			// Was any of the pairs of mentions marked as incompatible
			int cid1 = System.Math.Min(c1.clusterID, c2.clusterID);
			int cid2 = System.Math.Max(c1.clusterID, c2.clusterID);
			return incompatibleClusters.Contains(cid1, cid2);
		}

		// Update incompatibles for two clusters that are about to be merged
		public virtual void MergeIncompatibles(CorefCluster to, CorefCluster from)
		{
			IList<Pair<Pair<int, int>, Pair<int, int>>> replacements = new List<Pair<Pair<int, int>, Pair<int, int>>>();
			foreach (Pair<int, int> p in incompatibleClusters)
			{
				int other = null;
				if (p.first == from.clusterID)
				{
					other = p.second;
				}
				else
				{
					if (p.second == from.clusterID)
					{
						other = p.first;
					}
				}
				if (other != null && other != to.clusterID)
				{
					int cid1 = System.Math.Min(other, to.clusterID);
					int cid2 = System.Math.Max(other, to.clusterID);
					replacements.Add(Pair.MakePair(p, Pair.MakePair(cid1, cid2)));
				}
			}
			foreach (Pair<Pair<int, int>, Pair<int, int>> r in replacements)
			{
				incompatibleClusters.Remove(r.first.First(), r.first.Second());
				incompatibleClusters.Add(r.second.First(), r.second.Second());
			}
		}

		public virtual void MergeAcronymCache(CorefCluster to, CorefCluster from)
		{
			TwoDimensionalSet<int, int> replacements = TwoDimensionalSet.HashSet();
			foreach (int first in acronymCache.FirstKeySet())
			{
				foreach (int second in acronymCache.Get(first).Keys)
				{
					if (acronymCache.Get(first, second))
					{
						int other = null;
						if (first == from.clusterID)
						{
							other = second;
						}
						else
						{
							if (second == from.clusterID)
							{
								other = first;
							}
						}
						if (other != null && other != to.clusterID)
						{
							int cid1 = System.Math.Min(other, to.clusterID);
							int cid2 = System.Math.Max(other, to.clusterID);
							replacements.Add(cid1, cid2);
						}
					}
				}
			}
			foreach (int first_1 in replacements.FirstKeySet())
			{
				foreach (int second in replacements.SecondKeySet(first_1))
				{
					acronymCache.Put(first_1, second, true);
				}
			}
		}

		public virtual bool IsIncompatible(Mention m1, Mention m2)
		{
			int mid1 = System.Math.Min(m1.mentionID, m2.mentionID);
			int mid2 = System.Math.Max(m1.mentionID, m2.mentionID);
			return incompatibles.Contains(mid1, mid2);
		}

		public virtual void AddIncompatible(Mention m1, Mention m2)
		{
			int mid1 = System.Math.Min(m1.mentionID, m2.mentionID);
			int mid2 = System.Math.Max(m1.mentionID, m2.mentionID);
			incompatibles.Add(mid1, mid2);
			int cid1 = System.Math.Min(m1.corefClusterID, m2.corefClusterID);
			int cid2 = System.Math.Max(m1.corefClusterID, m2.corefClusterID);
			incompatibleClusters.Add(cid1, cid2);
		}

		/// <summary>Mark twin mentions in gold and predicted mentions</summary>
		protected internal virtual void FindTwinMentions(bool strict)
		{
			if (strict)
			{
				FindTwinMentionsStrict();
			}
			else
			{
				FindTwinMentionsRelaxed();
			}
		}

		/// <summary>Mark twin mentions: All mention boundaries should be matched</summary>
		private void FindTwinMentionsStrict()
		{
			for (int sentNum = 0; sentNum < goldOrderedMentionsBySentence.Count; sentNum++)
			{
				IList<Mention> golds = goldOrderedMentionsBySentence[sentNum];
				IList<Mention> predicts = predictedOrderedMentionsBySentence[sentNum];
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
						SieveCoreferenceSystem.logger.Warning("WARNING: gold mentions with the same offsets: " + ip + " mentions=" + g.mentionID + "," + existingMentions + ", " + g.SpanToString());
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
						Mention g_1 = cm.GetEnumerator().Current;
						cm.Remove(g_1);
						p.mentionID = g_1.mentionID;
						p.twinless = false;
						g_1.twinless = false;
					}
				}
				// temp: for making easy to recognize twinless mention
				foreach (Mention p_1 in predicts)
				{
					if (p_1.twinless)
					{
						p_1.mentionID += 10000;
					}
				}
			}
		}

		/// <summary>Mark twin mentions: heads of the mentions are matched</summary>
		private void FindTwinMentionsRelaxed()
		{
			for (int sentNum = 0; sentNum < goldOrderedMentionsBySentence.Count; sentNum++)
			{
				IList<Mention> golds = goldOrderedMentionsBySentence[sentNum];
				IList<Mention> predicts = predictedOrderedMentionsBySentence[sentNum];
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
						p.twinless = false;
						g_1.twinless = false;
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
						r.twinless = false;
						g_1.twinless = false;
						if (goldMentionHeadPositions[g_1.headIndex].IsEmpty())
						{
							Sharpen.Collections.Remove(goldMentionHeadPositions, g_1.headIndex);
						}
					}
				}
			}
		}

		/// <summary>Set paragraph index</summary>
		private void SetParagraphAnnotation()
		{
			int paragraphIndex = 0;
			int previousOffset = -10;
			foreach (ICoreMap sent in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
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
			foreach (IList<Mention> l in predictedOrderedMentionsBySentence)
			{
				foreach (Mention m in l)
				{
					m.paragraph = m.headWord.Get(typeof(CoreAnnotations.ParagraphAnnotation));
				}
			}
			numParagraph = paragraphIndex;
		}

		/// <summary>Find document type: Conversation or article</summary>
		private Document.DocType FindDocType(Dictionaries dict)
		{
			bool speakerChange = false;
			ICollection<int> discourseWithIorYou = Generics.NewHashSet();
			foreach (ICoreMap sent in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
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
					if (dict.firstPersonPronouns.Contains(w.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower()) || dict.secondPersonPronouns.Contains(w.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower()))
					{
						discourseWithIorYou.Add(utterIndex);
					}
					if (maxUtter < utterIndex)
					{
						maxUtter = utterIndex;
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
		/// <summary>When there is no mentionID information (without gold annotation), assign mention IDs</summary>
		protected internal virtual void AssignOriginalID()
		{
			IList<IList<Mention>> orderedMentionsBySentence = this.GetOrderedMentions();
			bool hasOriginalID = true;
			foreach (IList<Mention> l in orderedMentionsBySentence)
			{
				if (l.Count == 0)
				{
					continue;
				}
				foreach (Mention m in l)
				{
					if (m.mentionID == -1)
					{
						hasOriginalID = false;
					}
				}
			}
			if (!hasOriginalID)
			{
				int id = 0;
				foreach (IList<Mention> l_1 in orderedMentionsBySentence)
				{
					foreach (Mention m in l_1)
					{
						m.mentionID = id++;
					}
				}
			}
		}

		/// <summary>Extract gold coref cluster information.</summary>
		public virtual void ExtractGoldCorefClusters()
		{
			goldCorefClusters = Generics.NewHashMap();
			foreach (IList<Mention> mentions in goldOrderedMentionsBySentence)
			{
				foreach (Mention m in mentions)
				{
					int id = m.goldCorefClusterID;
					if (id == -1)
					{
						throw new Exception("No gold info");
					}
					CorefCluster c = goldCorefClusters[id];
					if (c == null)
					{
						c = new CorefCluster(id);
						goldCorefClusters[id] = c;
					}
					c.corefMentions.Add(m);
				}
			}
		}

		protected internal virtual IList<Pair<IntTuple, IntTuple>> GetGoldLinks()
		{
			if (goldLinks == null)
			{
				this.ExtractGoldLinks();
			}
			return goldLinks;
		}

		/// <summary>Extract gold coref link information</summary>
		protected internal virtual void ExtractGoldLinks()
		{
			//    List<List<Mention>> orderedMentionsBySentence = this.getOrderedMentions();
			IList<Pair<IntTuple, IntTuple>> links = new List<Pair<IntTuple, IntTuple>>();
			// position of each mention in the input matrix, by id
			IDictionary<int, IntTuple> positions = Generics.NewHashMap();
			// positions of antecedents
			IDictionary<int, IList<IntTuple>> antecedents = Generics.NewHashMap();
			for (int i = 0; i < goldOrderedMentionsBySentence.Count; i++)
			{
				for (int j = 0; j < goldOrderedMentionsBySentence[i].Count; j++)
				{
					Mention m = goldOrderedMentionsBySentence[i][j];
					int id = m.mentionID;
					IntTuple pos = new IntTuple(2);
					pos.Set(0, i);
					pos.Set(1, j);
					positions[id] = pos;
					antecedents[id] = new List<IntTuple>();
				}
			}
			//    SieveCoreferenceSystem.debugPrintMentions(System.err, "", goldOrderedMentionsBySentence);
			foreach (IList<Mention> mentions in goldOrderedMentionsBySentence)
			{
				foreach (Mention m in mentions)
				{
					int id = m.mentionID;
					IntTuple src = positions[id];
					System.Diagnostics.Debug.Assert((src != null));
					if (m.originalRef >= 0)
					{
						IntTuple dst = positions[m.originalRef];
						if (dst == null)
						{
							throw new Exception("Cannot find gold mention with ID=" + m.originalRef);
						}
						// to deal with cataphoric annotation
						while (dst.Get(0) > src.Get(0) || (dst.Get(0) == src.Get(0) && dst.Get(1) > src.Get(1)))
						{
							Mention dstMention = goldOrderedMentionsBySentence[dst.Get(0)][dst.Get(1)];
							m.originalRef = dstMention.originalRef;
							dstMention.originalRef = id;
							if (m.originalRef < 0)
							{
								break;
							}
							dst = positions[m.originalRef];
						}
						if (m.originalRef < 0)
						{
							continue;
						}
						// A B C: if A<-B, A<-C => make a link B<-C
						for (int k = dst.Get(0); k <= src.Get(0); k++)
						{
							for (int l = 0; l < goldOrderedMentionsBySentence[k].Count; l++)
							{
								if (k == dst.Get(0) && l < dst.Get(1))
								{
									continue;
								}
								if (k == src.Get(0) && l > src.Get(1))
								{
									break;
								}
								IntTuple missed = new IntTuple(2);
								missed.Set(0, k);
								missed.Set(1, l);
								if (links.Contains(new Pair<IntTuple, IntTuple>(missed, dst)))
								{
									antecedents[id].Add(missed);
									links.Add(new Pair<IntTuple, IntTuple>(src, missed));
								}
							}
						}
						links.Add(new Pair<IntTuple, IntTuple>(src, dst));
						System.Diagnostics.Debug.Assert((antecedents[id] != null));
						antecedents[id].Add(dst);
						IList<IntTuple> ants = antecedents[m.originalRef];
						System.Diagnostics.Debug.Assert((ants != null));
						foreach (IntTuple ant in ants)
						{
							antecedents[id].Add(ant);
							links.Add(new Pair<IntTuple, IntTuple>(src, ant));
						}
					}
				}
			}
			goldLinks = links;
		}

		/// <summary>set UtteranceAnnotation for quotations: default UtteranceAnnotation = 0 is given</summary>
		private void MarkQuotations(IList<ICoreMap> results, bool normalQuotationType)
		{
			bool insideQuotation = false;
			foreach (ICoreMap m in results)
			{
				foreach (CoreLabel l in m.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					string w = l.Get(typeof(CoreAnnotations.TextAnnotation));
					bool noSpeakerInfo = !l.ContainsKey(typeof(CoreAnnotations.SpeakerAnnotation)) || l.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals(string.Empty) || l.Get(typeof(CoreAnnotations.SpeakerAnnotation)).StartsWith("PER");
					if (w.Equals("``") || (!insideQuotation && normalQuotationType && w.Equals("\"")))
					{
						insideQuotation = true;
						maxUtter++;
						continue;
					}
					else
					{
						if (w.Equals("''") || (insideQuotation && normalQuotationType && w.Equals("\"")))
						{
							insideQuotation = false;
						}
					}
					if (insideQuotation)
					{
						l.Set(typeof(CoreAnnotations.UtteranceAnnotation), maxUtter);
					}
					if (noSpeakerInfo)
					{
						l.Set(typeof(CoreAnnotations.SpeakerAnnotation), "PER" + l.Get(typeof(CoreAnnotations.UtteranceAnnotation)));
					}
				}
			}
			if (maxUtter == 0 && !normalQuotationType)
			{
				MarkQuotations(results, true);
			}
		}

		/// <summary>Speaker extraction</summary>
		private void FindSpeakers(Dictionaries dict)
		{
			bool useMarkedDiscourseBoolean = annotation.Get(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation));
			bool useMarkedDiscourse = (useMarkedDiscourseBoolean != null) ? useMarkedDiscourseBoolean : false;
			if (Constants.UseGoldSpeakerTags || useMarkedDiscourse)
			{
				foreach (ICoreMap sent in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					foreach (CoreLabel w in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						int utterIndex = w.Get(typeof(CoreAnnotations.UtteranceAnnotation));
						speakers[utterIndex] = w.Get(typeof(CoreAnnotations.SpeakerAnnotation));
					}
				}
			}
			else
			{
				if (docType == Document.DocType.Conversation)
				{
					FindSpeakersInConversation(dict);
				}
				else
				{
					if (docType == Document.DocType.Article)
					{
						FindSpeakersInArticle(dict);
					}
				}
				// set speaker info to annotation
				foreach (ICoreMap sent in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					foreach (CoreLabel w in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						int utterIndex = w.Get(typeof(CoreAnnotations.UtteranceAnnotation));
						if (speakers.Contains(utterIndex))
						{
							w.Set(typeof(CoreAnnotations.SpeakerAnnotation), speakers[utterIndex]);
						}
					}
				}
			}
		}

		private void FindSpeakersInArticle(Dictionaries dict)
		{
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			Pair<int, int> beginQuotation = new Pair<int, int>();
			Pair<int, int> endQuotation = new Pair<int, int>();
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
						beginQuotation.SetFirst(i);
						beginQuotation.SetSecond(j);
					}
					else
					{
						if (utterIndex == 0 && insideQuotation)
						{
							insideQuotation = false;
							endQuotation.SetFirst(i);
							endQuotation.SetSecond(j);
							FindQuotationSpeaker(utterNum, sentences, beginQuotation, endQuotation, dict);
						}
					}
				}
			}
		}

		private void FindQuotationSpeaker(int utterNum, IList<ICoreMap> sentences, Pair<int, int> beginQuotation, Pair<int, int> endQuotation, Dictionaries dict)
		{
			if (FindSpeaker(utterNum, beginQuotation.First(), sentences, 0, beginQuotation.Second(), dict))
			{
				return;
			}
			if (FindSpeaker(utterNum, endQuotation.First(), sentences, endQuotation.Second(), sentences[endQuotation.First()].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
			{
				return;
			}
			if (beginQuotation.Second() <= 1 && beginQuotation.First() > 0)
			{
				if (FindSpeaker(utterNum, beginQuotation.First() - 1, sentences, 0, sentences[beginQuotation.First() - 1].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
				{
					return;
				}
			}
			if (endQuotation.Second() == sentences[endQuotation.First()].Size() - 1 && sentences.Count > endQuotation.First() + 1)
			{
				if (FindSpeaker(utterNum, endQuotation.First() + 1, sentences, 0, sentences[endQuotation.First() + 1].Get(typeof(CoreAnnotations.TokensAnnotation)).Count, dict))
				{
					return;
				}
			}
		}

		private bool FindSpeaker(int utterNum, int sentNum, IList<ICoreMap> sentences, int startIndex, int endIndex, Dictionaries dict)
		{
			IList<CoreLabel> sent = sentences[sentNum].Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = startIndex; i < endIndex; i++)
			{
				if (sent[i].Get(typeof(CoreAnnotations.UtteranceAnnotation)) != 0)
				{
					continue;
				}
				string lemma = sent[i].Get(typeof(CoreAnnotations.LemmaAnnotation));
				string word = sent[i].Get(typeof(CoreAnnotations.TextAnnotation));
				if (dict.reportVerb.Contains(lemma))
				{
					// find subject
					SemanticGraph dependency = sentences[sentNum].Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					IndexedWord w = dependency.GetNodeByWordPattern(word);
					if (w != null)
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
								if (mentionheadPositions.Contains(headPosition))
								{
									speaker = int.ToString(mentionheadPositions[headPosition].mentionID);
								}
								else
								{
									speaker = subjectString;
								}
								speakers[utterNum] = speaker;
								return true;
							}
						}
					}
					else
					{
						SieveCoreferenceSystem.logger.Warning("Cannot find node in dependency for word " + word);
					}
				}
			}
			return false;
		}

		private void FindSpeakersInConversation(Dictionaries dict)
		{
			foreach (IList<Mention> l in predictedOrderedMentionsBySentence)
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
							speakers[m.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation))] = int.ToString(m.mentionID);
						}
					}
				}
			}
			IList<ICoreMap> paragraph = new List<ICoreMap>();
			int paragraphUtterIndex = 0;
			string nextParagraphSpeaker = string.Empty;
			int paragraphOffset = 0;
			foreach (ICoreMap sent in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				int currentUtter = sent.Get(typeof(CoreAnnotations.TokensAnnotation))[0].Get(typeof(CoreAnnotations.UtteranceAnnotation));
				if (paragraphUtterIndex != currentUtter)
				{
					nextParagraphSpeaker = FindParagraphSpeaker(paragraph, paragraphUtterIndex, nextParagraphSpeaker, paragraphOffset, dict);
					paragraphUtterIndex = currentUtter;
					paragraphOffset += paragraph.Count;
					paragraph = new List<ICoreMap>();
				}
				paragraph.Add(sent);
			}
			FindParagraphSpeaker(paragraph, paragraphUtterIndex, nextParagraphSpeaker, paragraphOffset, dict);
		}

		private string FindParagraphSpeaker(IList<ICoreMap> paragraph, int paragraphUtterIndex, string nextParagraphSpeaker, int paragraphOffset, Dictionaries dict)
		{
			if (!speakers.Contains(paragraphUtterIndex))
			{
				if (!nextParagraphSpeaker.Equals(string.Empty))
				{
					speakers[paragraphUtterIndex] = nextParagraphSpeaker;
				}
				else
				{
					// find the speaker of this paragraph (John, nbc news)
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
							if (mentionheadPositions.Contains(headPosition))
							{
								speaker = int.ToString(mentionheadPositions[headPosition].mentionID);
							}
						}
					}
					if (!hasVerb && !speaker.Equals(string.Empty))
					{
						speakers[paragraphUtterIndex] = speaker;
					}
				}
			}
			return FindNextParagraphSpeaker(paragraph, paragraphOffset, dict);
		}

		private string FindNextParagraphSpeaker(IList<ICoreMap> paragraph, int paragraphOffset, Dictionaries dict)
		{
			ICoreMap lastSent = paragraph[paragraph.Count - 1];
			string speaker = string.Empty;
			foreach (CoreLabel w in lastSent.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (w.Get(typeof(CoreAnnotations.LemmaAnnotation)).Equals("report") || w.Get(typeof(CoreAnnotations.LemmaAnnotation)).Equals("say"))
				{
					string word = w.Get(typeof(CoreAnnotations.TextAnnotation));
					SemanticGraph dependency = lastSent.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
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
							if (mentionheadPositions.Contains(headPosition) && mentionheadPositions[headPosition].nerString.StartsWith("PER"))
							{
								speaker = int.ToString(mentionheadPositions[headPosition].mentionID);
							}
						}
					}
				}
			}
			return speaker;
		}

		public virtual SpeakerInfo GetSpeakerInfo(string speaker)
		{
			return speakerInfoMap[speaker];
		}

		public virtual int NumberOfSpeakers()
		{
			return speakerInfoMap.Count;
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
			IndexedWord w = m.dependency.GetNodeByWordPattern(m.sentenceWords[m.headIndex].Get(typeof(CoreAnnotations.TextAnnotation)));
			if (w == null)
			{
				return false;
			}
			foreach (Pair<GrammaticalRelation, IndexedWord> parent in m.dependency.ParentPairs(w))
			{
				if (parent.First().GetShortName().Equals("nsubj") && dict.reportVerb.Contains(parent.Second().Get(typeof(CoreAnnotations.LemmaAnnotation))))
				{
					return true;
				}
			}
			return false;
		}

		protected internal virtual void PrintMentionDetection()
		{
			int foundGoldCount = 0;
			foreach (Mention g in allGoldMentions.Values)
			{
				if (!g.twinless)
				{
					foundGoldCount++;
				}
			}
			SieveCoreferenceSystem.logger.Fine("# of found gold mentions: " + foundGoldCount + " / # of gold mentions: " + allGoldMentions.Count);
			SieveCoreferenceSystem.logger.Fine("gold mentions == ");
		}
	}
}
