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
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>One cluster for the SieveCoreferenceSystem.</summary>
	/// <author>Heeyoung Lee</author>
	[System.Serializable]
	public class CorefCluster
	{
		private const long serialVersionUID = 8655265337578515592L;

		public readonly ICollection<Mention> corefMentions;

		public readonly int clusterID;

		public readonly ICollection<Dictionaries.Number> numbers;

		public readonly ICollection<Dictionaries.Gender> genders;

		public readonly ICollection<Dictionaries.Animacy> animacies;

		public readonly ICollection<string> nerStrings;

		public readonly ICollection<string> heads;

		/// <summary>All words in this cluster - for word inclusion feature</summary>
		public readonly ICollection<string> words;

		/// <summary>The first mention in this cluster</summary>
		protected internal Mention firstMention;

		/// <summary>Return the most representative mention in the chain.</summary>
		/// <remarks>
		/// Return the most representative mention in the chain.
		/// A proper noun mention or a mention with more pre-modifiers is preferred.
		/// </remarks>
		public Mention representative;

		// Attributes for cluster - can include multiple attribute e.g., {singular, plural}
		public virtual int GetClusterID()
		{
			return clusterID;
		}

		public virtual ICollection<Mention> GetCorefMentions()
		{
			return corefMentions;
		}

		public virtual int Size()
		{
			return corefMentions.Count;
		}

		public virtual Mention GetFirstMention()
		{
			return firstMention;
		}

		public virtual Mention GetRepresentativeMention()
		{
			return representative;
		}

		public CorefCluster(int Id)
		{
			clusterID = Id;
			corefMentions = Generics.NewHashSet();
			numbers = EnumSet.NoneOf<Dictionaries.Number>();
			genders = EnumSet.NoneOf<Dictionaries.Gender>();
			animacies = EnumSet.NoneOf<Dictionaries.Animacy>();
			nerStrings = Generics.NewHashSet();
			heads = Generics.NewHashSet();
			words = Generics.NewHashSet();
			firstMention = null;
			representative = null;
		}

		public CorefCluster(int Id, ICollection<Mention> mentions)
			: this(Id)
		{
			// Register mentions
			Sharpen.Collections.AddAll(corefMentions, mentions);
			// Get list of mentions in textual order
			IList<Mention> sortedMentions = new List<Mention>(mentions.Count);
			Sharpen.Collections.AddAll(sortedMentions, mentions);
			sortedMentions.Sort(new CorefChain.MentionComparator());
			// Set default for first / representative mention
			if (sortedMentions.Count > 0)
			{
				firstMention = sortedMentions[0];
				representative = sortedMentions[0];
			}
			// will be updated below
			foreach (Mention m in sortedMentions)
			{
				// Add various information about mentions to cluster
				animacies.Add(m.animacy);
				genders.Add(m.gender);
				numbers.Add(m.number);
				nerStrings.Add(m.nerString);
				heads.Add(m.headString);
				if (!m.IsPronominal())
				{
					foreach (CoreLabel w in m.originalSpan)
					{
						words.Add(w.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower());
					}
				}
				// Update representative mention, if appropriate
				if (m != representative && m.MoreRepresentativeThan(representative))
				{
					System.Diagnostics.Debug.Assert(!representative.MoreRepresentativeThan(m));
					representative = m;
				}
			}
		}

		/// <summary>merge 2 clusters: to = to + from</summary>
		public static void MergeClusters(Edu.Stanford.Nlp.Coref.Data.CorefCluster to, Edu.Stanford.Nlp.Coref.Data.CorefCluster from)
		{
			int toID = to.clusterID;
			foreach (Mention m in from.corefMentions)
			{
				m.corefClusterID = toID;
			}
			Sharpen.Collections.AddAll(to.numbers, from.numbers);
			if (to.numbers.Count > 1 && to.numbers.Contains(Dictionaries.Number.Unknown))
			{
				to.numbers.Remove(Dictionaries.Number.Unknown);
			}
			Sharpen.Collections.AddAll(to.genders, from.genders);
			if (to.genders.Count > 1 && to.genders.Contains(Dictionaries.Gender.Unknown))
			{
				to.genders.Remove(Dictionaries.Gender.Unknown);
			}
			Sharpen.Collections.AddAll(to.animacies, from.animacies);
			if (to.animacies.Count > 1 && to.animacies.Contains(Dictionaries.Animacy.Unknown))
			{
				to.animacies.Remove(Dictionaries.Animacy.Unknown);
			}
			Sharpen.Collections.AddAll(to.nerStrings, from.nerStrings);
			if (to.nerStrings.Count > 1 && to.nerStrings.Contains("O"))
			{
				to.nerStrings.Remove("O");
			}
			if (to.nerStrings.Count > 1 && to.nerStrings.Contains("MISC"))
			{
				to.nerStrings.Remove("MISC");
			}
			Sharpen.Collections.AddAll(to.heads, from.heads);
			Sharpen.Collections.AddAll(to.corefMentions, from.corefMentions);
			Sharpen.Collections.AddAll(to.words, from.words);
			if (from.firstMention.AppearEarlierThan(to.firstMention) && !from.firstMention.IsPronominal())
			{
				System.Diagnostics.Debug.Assert(!to.firstMention.AppearEarlierThan(from.firstMention));
				to.firstMention = from.firstMention;
			}
			if (from.representative.MoreRepresentativeThan(to.representative))
			{
				to.representative = from.representative;
			}
		}

		//Redwood.log("debug-cluster", "merged clusters: "+toID+" += "+from.clusterID);
		//to.printCorefCluster();
		//from.printCorefCluster();
		/// <summary>Print cluster information</summary>
		public virtual void PrintCorefCluster()
		{
			Redwood.Log("debug-cluster", "Cluster ID: " + clusterID + "\tNumbers: " + numbers + "\tGenders: " + genders + "\tanimacies: " + animacies);
			Redwood.Log("debug-cluster", "NE: " + nerStrings + "\tfirst Mention's ID: " + firstMention.mentionID + "\tHeads: " + heads + "\twords: " + words);
			SortedDictionary<int, Mention> forSortedPrint = new SortedDictionary<int, Mention>();
			foreach (Mention m in this.corefMentions)
			{
				forSortedPrint[m.mentionID] = m;
			}
			foreach (Mention m_1 in forSortedPrint.Values)
			{
				string rep = (representative == m_1) ? "*" : string.Empty;
				if (m_1.goldCorefClusterID == -1)
				{
					Redwood.Log("debug-cluster", rep + "mention-> id:" + m_1.mentionID + "\toriginalRef: " + m_1.originalRef + "\t" + m_1.SpanToString() + "\tsentNum: " + m_1.sentNum + "\tstartIndex: " + m_1.startIndex + "\tType: " + m_1.mentionType + "\tNER: "
						 + m_1.nerString);
				}
				else
				{
					Redwood.Log("debug-cluster", rep + "mention-> id:" + m_1.mentionID + "\toriginalClusterID: " + m_1.goldCorefClusterID + "\t" + m_1.SpanToString() + "\tsentNum: " + m_1.sentNum + "\tstartIndex: " + m_1.startIndex + "\toriginalRef: " + m_1.originalRef
						 + "\tType: " + m_1.mentionType + "\tNER: " + m_1.nerString);
				}
			}
		}

		public virtual bool IsSinglePronounCluster(Dictionaries dict)
		{
			if (this.corefMentions.Count > 1)
			{
				return false;
			}
			foreach (Mention m in this.corefMentions)
			{
				if (m.IsPronominal() || dict.allPronouns.Contains(m.SpanToString().ToLower()))
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			return corefMentions.ToString() + "=" + clusterID;
		}
	}
}
