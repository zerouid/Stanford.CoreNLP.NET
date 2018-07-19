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
using Edu.Stanford.Nlp.Coref.Docreader;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Data
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
		public CoNLLDocumentReader.CoNLLDocument conllDoc;

		/// <summary>The list of gold mentions</summary>
		public IList<IList<Mention>> goldMentions;

		/// <summary>The list of predicted mentions</summary>
		public IList<IList<Mention>> predictedMentions;

		/// <summary>return the list of predicted mentions</summary>
		public virtual IList<IList<Mention>> GetOrderedMentions()
		{
			return predictedMentions;
		}

		/// <summary>Clusters for coreferent mentions</summary>
		public IDictionary<int, CorefCluster> corefClusters;

		/// <summary>Gold Clusters for coreferent mentions</summary>
		public IDictionary<int, CorefCluster> goldCorefClusters;

		/// <summary>
		/// All mentions in a document
		/// <literal>mentionID -&gt; mention</literal>
		/// 
		/// </summary>
		public IDictionary<int, Mention> predictedMentionsByID;

		public IDictionary<int, Mention> goldMentionsByID;

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

		/// <summary>
		/// UtteranceAnnotation
		/// <literal>-&gt;</literal>
		/// String (speaker): mention ID or speaker string
		/// e.g., the value can be "34" (mentionID), "Larry" (speaker string), or "PER3" (autoassigned speaker string)
		/// </summary>
		public IDictionary<int, string> speakers;

		/// <summary>
		/// Pair of mention id, and the mention's speaker id
		/// the second value is the "speaker mention"'s id.
		/// </summary>
		/// <remarks>
		/// Pair of mention id, and the mention's speaker id
		/// the second value is the "speaker mention"'s id.
		/// e.g., Larry said, "San Francisco is a city.": (id(Larry), id(San Francisco))
		/// </remarks>
		public ICollection<Pair<int, int>> speakerPairs;

		public bool speakerInfoGiven;

		public int maxUtter;

		public int numParagraph;

		public int numSentences;

		/// <summary>Set of incompatible clusters pairs</summary>
		private readonly ICollection<Pair<int, int>> incompatibles;

		private readonly ICollection<Pair<int, int>> incompatibleClusters;

		public IDictionary<Pair<int, int>, bool> acronymCache;

		/// <summary>
		/// Map of speaker name/id to speaker info
		/// the key is the value of the variable 'speakers'
		/// </summary>
		public IDictionary<string, SpeakerInfo> speakerInfoMap = Generics.NewHashMap();

		/// <summary>Additional information about the document.</summary>
		/// <remarks>Additional information about the document. Can be used as features</remarks>
		public IDictionary<string, string> docInfo;

		public Document()
		{
			// mentions may be removed from this due to post processing
			// all mentions (mentions will not be removed from this)
			// public Counter<String> properNouns = new ClassicCounter<>();
			// public Counter<String> phraseCounter = new ClassicCounter<>();
			// public Counter<String> headwordCounter = new ClassicCounter<>();
			positions = Generics.NewHashMap();
			mentionheadPositions = Generics.NewHashMap();
			roleSet = Generics.NewHashSet();
			corefClusters = Generics.NewHashMap();
			goldCorefClusters = null;
			predictedMentionsByID = Generics.NewHashMap();
			//    goldMentionsByID = Generics.newHashMap();
			speakers = Generics.NewHashMap();
			speakerPairs = Generics.NewHashSet();
			incompatibles = Generics.NewHashSet();
			incompatibleClusters = Generics.NewHashSet();
			acronymCache = Generics.NewHashMap();
		}

		public Document(Annotation anno, IList<IList<Mention>> predictedMentions, IList<IList<Mention>> goldMentions)
			: this()
		{
			annotation = anno;
			this.predictedMentions = predictedMentions;
			this.goldMentions = goldMentions;
		}

		public Document(InputDoc input, IList<IList<Mention>> mentions)
			: this()
		{
			this.annotation = input.annotation;
			this.predictedMentions = mentions;
			this.goldMentions = input.goldMentions;
			this.docInfo = input.docInfo;
			this.numSentences = input.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)).Count;
			this.conllDoc = input.conllDoc;
		}

		// null if it's not conll input
		public virtual bool IsIncompatible(CorefCluster c1, CorefCluster c2)
		{
			// Was any of the pairs of mentions marked as incompatible
			int cid1 = Math.Min(c1.clusterID, c2.clusterID);
			int cid2 = Math.Max(c1.clusterID, c2.clusterID);
			return incompatibleClusters.Contains(Pair.MakePair(cid1, cid2));
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
					int cid1 = Math.Min(other, to.clusterID);
					int cid2 = Math.Max(other, to.clusterID);
					replacements.Add(Pair.MakePair(p, Pair.MakePair(cid1, cid2)));
				}
			}
			foreach (Pair<Pair<int, int>, Pair<int, int>> r in replacements)
			{
				incompatibleClusters.Remove(r.first);
				incompatibleClusters.Add(r.second);
			}
		}

		public virtual void MergeAcronymCache(CorefCluster to, CorefCluster from)
		{
			IDictionary<Pair<int, int>, bool> replacements = Generics.NewHashMap();
			foreach (Pair<int, int> p in acronymCache.Keys)
			{
				if (acronymCache[p])
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
						int cid1 = Math.Min(other, to.clusterID);
						int cid2 = Math.Max(other, to.clusterID);
						replacements[Pair.MakePair(cid1, cid2)] = true;
					}
				}
			}
			foreach (Pair<int, int> p_1 in replacements.Keys)
			{
				acronymCache[p_1] = replacements[p_1];
			}
		}

		public virtual bool IsIncompatible(Mention m1, Mention m2)
		{
			int mid1 = Math.Min(m1.mentionID, m2.mentionID);
			int mid2 = Math.Max(m1.mentionID, m2.mentionID);
			return incompatibles.Contains(Pair.MakePair(mid1, mid2));
		}

		public virtual void AddIncompatible(Mention m1, Mention m2)
		{
			int mid1 = Math.Min(m1.mentionID, m2.mentionID);
			int mid2 = Math.Max(m1.mentionID, m2.mentionID);
			incompatibles.Add(Pair.MakePair(mid1, mid2));
			int cid1 = Math.Min(m1.corefClusterID, m2.corefClusterID);
			int cid2 = Math.Max(m1.corefClusterID, m2.corefClusterID);
			incompatibleClusters.Add(Pair.MakePair(cid1, cid2));
		}

		public virtual IList<Pair<IntTuple, IntTuple>> GetGoldLinks()
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
			for (int i = 0; i < goldMentions.Count; i++)
			{
				for (int j = 0; j < goldMentions[i].Count; j++)
				{
					Mention m = goldMentions[i][j];
					int id = m.mentionID;
					IntTuple pos = new IntTuple(2);
					pos.Set(0, i);
					pos.Set(1, j);
					positions[id] = pos;
					antecedents[id] = new List<IntTuple>();
				}
			}
			//    SieveCoreferenceSystem.debugPrintMentions(System.err, "", goldOrderedMentionsBySentence);
			foreach (IList<Mention> mentions in goldMentions)
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
							Mention dstMention = goldMentions[dst.Get(0)][dst.Get(1)];
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
							for (int l = 0; l < goldMentions[k].Count; l++)
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

		public virtual SpeakerInfo GetSpeakerInfo(string speaker)
		{
			return speakerInfoMap[speaker];
		}

		public virtual int NumberOfSpeakers()
		{
			return speakerInfoMap.Count;
		}

		public virtual bool IsCoref(Mention m1, Mention m2)
		{
			return this.goldMentionsByID.Contains(m1.mentionID) && this.goldMentionsByID.Contains(m2.mentionID) && this.goldMentionsByID[m1.mentionID].goldCorefClusterID == this.goldMentionsByID[m2.mentionID].goldCorefClusterID;
		}
	}
}
