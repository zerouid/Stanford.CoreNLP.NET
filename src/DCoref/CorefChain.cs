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
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Output of (deterministic) coref system.</summary>
	/// <remarks>
	/// Output of (deterministic) coref system.  Each CorefChain represents a set
	/// of mentions in the text which should all correspond to the same actual
	/// entity.  There is a representative mention, which stores the best
	/// mention of an entity, and then there is a List of all mentions
	/// that are coreferent with that mention. The mentionMap maps from pairs of
	/// a sentence number and a head word index to a CorefMention. The chainID is
	/// an arbitrary integer for the chain number.
	/// </remarks>
	/// <author>Heeyoung Lee</author>
	[System.Serializable]
	public class CorefChain
	{
		private readonly int chainID;

		private readonly IList<CorefChain.CorefMention> mentions;

		private readonly IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentionMap;

		/// <summary>The most representative mention in this cluster</summary>
		private readonly CorefChain.CorefMention representative;

		public override bool Equals(object aThat)
		{
			if (this == aThat)
			{
				return true;
			}
			if (!(aThat is Edu.Stanford.Nlp.Dcoref.CorefChain))
			{
				return false;
			}
			Edu.Stanford.Nlp.Dcoref.CorefChain that = (Edu.Stanford.Nlp.Dcoref.CorefChain)aThat;
			if (chainID != that.chainID)
			{
				return false;
			}
			if (!mentions.Equals(that.mentions))
			{
				return false;
			}
			if (representative == null && that.representative == null)
			{
				return true;
			}
			if (representative == null || that.representative == null || !representative.Equals(that.representative))
			{
				return false;
			}
			// mentionMap is another view of mentions, so no need to compare
			// that once we've compared mentions
			return true;
		}

		public override int GetHashCode()
		{
			return mentions.GetHashCode();
		}

		/// <summary>get List of CorefMentions</summary>
		public virtual IList<CorefChain.CorefMention> GetMentionsInTextualOrder()
		{
			return mentions;
		}

		/// <summary>get CorefMentions by position (sentence number, headIndex) Can be multiple mentions sharing headword</summary>
		public virtual ICollection<CorefChain.CorefMention> GetMentionsWithSameHead(IntPair position)
		{
			return mentionMap[position];
		}

		/// <summary>get CorefMention by position</summary>
		public virtual ICollection<CorefChain.CorefMention> GetMentionsWithSameHead(int sentenceNumber, int headIndex)
		{
			return GetMentionsWithSameHead(new IntPair(sentenceNumber, headIndex));
		}

		public virtual IDictionary<IntPair, ICollection<CorefChain.CorefMention>> GetMentionMap()
		{
			return mentionMap;
		}

		/// <summary>Return the most representative mention in the chain.</summary>
		/// <remarks>
		/// Return the most representative mention in the chain.
		/// Proper mention and a mention with more pre-modifiers are preferred.
		/// </remarks>
		public virtual CorefChain.CorefMention GetRepresentativeMention()
		{
			return representative;
		}

		public virtual int GetChainID()
		{
			return chainID;
		}

		/// <summary>Mention for coref output.</summary>
		/// <remarks>
		/// Mention for coref output.  This is one instance of the entity
		/// referred to by a given CorefChain.
		/// </remarks>
		[System.Serializable]
		public class CorefMention
		{
			public readonly Dictionaries.MentionType mentionType;

			public readonly Dictionaries.Number number;

			public readonly Dictionaries.Gender gender;

			public readonly Dictionaries.Animacy animacy;

			/// <summary>Starting word number, indexed from 1</summary>
			public readonly int startIndex;

			/// <summary>One past the end word number, indexed from 1</summary>
			public readonly int endIndex;

			/// <summary>Head word of the mention</summary>
			public readonly int headIndex;

			public readonly int corefClusterID;

			public readonly int mentionID;

			/// <summary>
			/// Sentence number in the document containing this mention,
			/// indexed from 1.
			/// </summary>
			public readonly int sentNum;

			/// <summary>
			/// Position is a binary tuple of (sentence number, mention number
			/// in that sentence).
			/// </summary>
			/// <remarks>
			/// Position is a binary tuple of (sentence number, mention number
			/// in that sentence).  This is used for indexing by mention.
			/// </remarks>
			public readonly IntTuple position;

			public readonly string mentionSpan;

			/// <summary>This constructor is used to recreate a CorefMention following serialization.</summary>
			public CorefMention(Dictionaries.MentionType mentionType, Dictionaries.Number number, Dictionaries.Gender gender, Dictionaries.Animacy animacy, int startIndex, int endIndex, int headIndex, int corefClusterID, int mentionID, int sentNum, IntTuple
				 position, string mentionSpan)
			{
				this.mentionType = mentionType;
				this.number = number;
				this.gender = gender;
				this.animacy = animacy;
				this.startIndex = startIndex;
				this.endIndex = endIndex;
				this.headIndex = headIndex;
				this.corefClusterID = corefClusterID;
				this.mentionID = mentionID;
				this.sentNum = sentNum;
				this.position = position;
				this.mentionSpan = mentionSpan;
			}

			/// <summary>This constructor builds the external CorefMention class from the internal Mention.</summary>
			public CorefMention(Mention m, IntTuple pos)
			{
				mentionType = m.mentionType;
				number = m.number;
				gender = m.gender;
				animacy = m.animacy;
				startIndex = m.startIndex + 1;
				endIndex = m.endIndex + 1;
				headIndex = m.headIndex + 1;
				corefClusterID = m.corefClusterID;
				sentNum = m.sentNum + 1;
				mentionID = m.mentionID;
				mentionSpan = m.SpanToString();
				// index starts from 1
				position = new IntTuple(2);
				position.Set(0, pos.Get(0) + 1);
				position.Set(1, pos.Get(1) + 1);
				m.headWord.Set(typeof(CorefCoreAnnotations.CorefClusterIdAnnotation), corefClusterID);
			}

			public override bool Equals(object aThat)
			{
				if (this == aThat)
				{
					return true;
				}
				if (!(aThat is CorefChain.CorefMention))
				{
					return false;
				}
				CorefChain.CorefMention that = (CorefChain.CorefMention)aThat;
				if (mentionType != that.mentionType)
				{
					return false;
				}
				if (number != that.number)
				{
					return false;
				}
				if (gender != that.gender)
				{
					return false;
				}
				if (animacy != that.animacy)
				{
					return false;
				}
				if (startIndex != that.startIndex)
				{
					return false;
				}
				if (endIndex != that.endIndex)
				{
					return false;
				}
				if (headIndex != that.headIndex)
				{
					return false;
				}
				if (corefClusterID != that.corefClusterID)
				{
					return false;
				}
				if (mentionID != that.mentionID)
				{
					return false;
				}
				if (sentNum != that.sentNum)
				{
					return false;
				}
				if (!position.Equals(that.position))
				{
					return false;
				}
				// we ignore MentionSpan as it is constructed from the tokens
				// the mention is a span of, so if we know those spans are the
				// same, we should be able to ignore the actual text
				return true;
			}

			public override int GetHashCode()
			{
				return position.GetHashCode();
			}

			public override string ToString()
			{
				return '"' + mentionSpan + "\" in sentence " + sentNum;
			}

			//      return "(sentence:" + sentNum + ", startIndex:" + startIndex + "-endIndex:" + endIndex + ")";
			private bool MoreRepresentativeThan(CorefChain.CorefMention m)
			{
				if (m == null)
				{
					return true;
				}
				if (mentionType != m.mentionType)
				{
					return (mentionType == Dictionaries.MentionType.Proper) || (mentionType == Dictionaries.MentionType.Nominal && m.mentionType == Dictionaries.MentionType.Pronominal);
				}
				else
				{
					// First, check length
					if (headIndex - startIndex > m.headIndex - m.startIndex)
					{
						return true;
					}
					if (headIndex - startIndex < m.headIndex - m.startIndex)
					{
						return false;
					}
					if (endIndex - startIndex > m.endIndex - m.startIndex)
					{
						return true;
					}
					if (endIndex - startIndex < m.endIndex - m.startIndex)
					{
						return false;
					}
					// Now check relative position
					if (sentNum < m.sentNum)
					{
						return true;
					}
					if (sentNum > m.sentNum)
					{
						return false;
					}
					if (headIndex < m.headIndex)
					{
						return true;
					}
					if (headIndex > m.headIndex)
					{
						return false;
					}
					if (startIndex < m.startIndex)
					{
						return true;
					}
					if (startIndex > m.startIndex)
					{
						return false;
					}
					// At this point they're equal...
					return false;
				}
			}

			private const long serialVersionUID = 3657691243504173L;
		}

		protected internal class CorefMentionComparator : IComparator<CorefChain.CorefMention>
		{
			// end static class CorefMention
			public virtual int Compare(CorefChain.CorefMention m1, CorefChain.CorefMention m2)
			{
				if (m1.sentNum < m2.sentNum)
				{
					return -1;
				}
				else
				{
					if (m1.sentNum > m2.sentNum)
					{
						return 1;
					}
					else
					{
						if (m1.startIndex < m2.startIndex)
						{
							return -1;
						}
						else
						{
							if (m1.startIndex > m2.startIndex)
							{
								return 1;
							}
							else
							{
								if (m1.endIndex > m2.endIndex)
								{
									return -1;
								}
								else
								{
									if (m1.endIndex < m2.endIndex)
									{
										return 1;
									}
									else
									{
										return 0;
									}
								}
							}
						}
					}
				}
			}
		}

		protected internal class MentionComparator : IComparator<Mention>
		{
			public virtual int Compare(Mention m1, Mention m2)
			{
				if (m1.sentNum < m2.sentNum)
				{
					return -1;
				}
				else
				{
					if (m1.sentNum > m2.sentNum)
					{
						return 1;
					}
					else
					{
						if (m1.startIndex < m2.startIndex)
						{
							return -1;
						}
						else
						{
							if (m1.startIndex > m2.startIndex)
							{
								return 1;
							}
							else
							{
								if (m1.endIndex > m2.endIndex)
								{
									return -1;
								}
								else
								{
									if (m1.endIndex < m2.endIndex)
									{
										return 1;
									}
									else
									{
										return 0;
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Delete a mention from this coreference chain.</summary>
		/// <param name="m">The mention to delete.</param>
		public virtual void DeleteMention(CorefChain.CorefMention m)
		{
			this.mentions.Remove(m);
			IntPair position = new IntPair(m.sentNum, m.headIndex);
			Sharpen.Collections.Remove(this.mentionMap, position);
		}

		public CorefChain(CorefCluster c, IDictionary<Mention, IntTuple> positions)
		{
			chainID = c.clusterID;
			// Collect mentions
			mentions = new List<CorefChain.CorefMention>();
			mentionMap = Generics.NewHashMap();
			CorefChain.CorefMention represents = null;
			foreach (Mention m in c.GetCorefMentions())
			{
				CorefChain.CorefMention men = new CorefChain.CorefMention(m, positions[m]);
				mentions.Add(men);
			}
			mentions.Sort(new CorefChain.CorefMentionComparator());
			// Find representative mention
			foreach (CorefChain.CorefMention men_1 in mentions)
			{
				IntPair position = new IntPair(men_1.sentNum, men_1.headIndex);
				if (!mentionMap.Contains(position))
				{
					mentionMap[position] = Generics.NewHashSet<CorefChain.CorefMention>();
				}
				mentionMap[position].Add(men_1);
				if (men_1.MoreRepresentativeThan(represents))
				{
					represents = men_1;
				}
			}
			representative = represents;
		}

		/// <summary>Constructor required by CustomAnnotationSerializer</summary>
		public CorefChain(int cid, IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentionMap, CorefChain.CorefMention representative)
		{
			this.chainID = cid;
			this.representative = representative;
			this.mentionMap = mentionMap;
			this.mentions = new List<CorefChain.CorefMention>();
			foreach (ICollection<CorefChain.CorefMention> ms in mentionMap.Values)
			{
				foreach (CorefChain.CorefMention m in ms)
				{
					this.mentions.Add(m);
				}
			}
			mentions.Sort(new CorefChain.CorefMentionComparator());
		}

		public override string ToString()
		{
			return "CHAIN" + this.chainID + '-' + mentions;
		}

		private const long serialVersionUID = 3657691243506528L;
	}
}
