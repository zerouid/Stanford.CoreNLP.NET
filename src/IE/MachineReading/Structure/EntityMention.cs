using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Each entity mention is described by a type (possibly subtype) and a span of text</summary>
	/// <author>Andrey Gusev</author>
	/// <author>Mihai</author>
	[System.Serializable]
	public class EntityMention : ExtractionObject
	{
		private const long serialVersionUID = -2745903102654191527L;

		/// <summary>Mention type, if available, e.g., nominal</summary>
		private readonly string mentionType;

		private string corefID = "-1";

		/// <summary>
		/// Offsets the head span, e.g., "George Bush" in the extent "the president George Bush"
		/// The offsets are relative to the sentence containing this mention
		/// </summary>
		private Span headTokenSpan;

		/// <summary>
		/// Position of the syntactic head word of this mention, e.g., "Bush" for the head span "George Bush"
		/// The offset is relative the sentence containing this mention
		/// Note: use headTokenSpan when sequence tagging entity mentions not this.
		/// </summary>
		/// <remarks>
		/// Position of the syntactic head word of this mention, e.g., "Bush" for the head span "George Bush"
		/// The offset is relative the sentence containing this mention
		/// Note: use headTokenSpan when sequence tagging entity mentions not this.
		/// This is meant to be used only for event/relation feature extraction!
		/// </remarks>
		private int syntacticHeadTokenPosition;

		private string normalizedName;

		public EntityMention(string objectId, ICoreMap sentence, Span extentSpan, Span headSpan, string type, string subtype, string mentionType)
			: base(objectId, sentence, extentSpan, type, subtype)
		{
			this.mentionType = (mentionType != null ? string.Intern(mentionType) : null);
			this.headTokenSpan = headSpan;
			this.syntacticHeadTokenPosition = -1;
			this.normalizedName = null;
		}

		public virtual string GetCorefID()
		{
			return corefID;
		}

		public virtual void SetCorefID(string id)
		{
			this.corefID = id;
		}

		public virtual string GetMentionType()
		{
			return mentionType;
		}

		public virtual Span GetHead()
		{
			return headTokenSpan;
		}

		public virtual int GetHeadTokenStart()
		{
			return headTokenSpan.Start();
		}

		public virtual int GetHeadTokenEnd()
		{
			return headTokenSpan.End();
		}

		public virtual void SetHeadTokenSpan(Span s)
		{
			headTokenSpan = s;
		}

		public virtual void SetHeadTokenPosition(int i)
		{
			this.syntacticHeadTokenPosition = i;
		}

		public virtual int GetSyntacticHeadTokenPosition()
		{
			return this.syntacticHeadTokenPosition;
		}

		public virtual CoreLabel GetSyntacticHeadToken()
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			return tokens[syntacticHeadTokenPosition];
		}

		public virtual Tree GetSyntacticHeadTree()
		{
			Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			return tree.GetLeaves()[syntacticHeadTokenPosition];
		}

		public virtual string GetNormalizedName()
		{
			return normalizedName;
		}

		public virtual void SetNormalizedName(string n)
		{
			normalizedName = n;
		}

		/*
		@Override
		public boolean equals(Object other) {
		if(! (other instanceof EntityMention)) return false;
		ExtractionObject o = (ExtractionObject) other;
		if(o.objectId.equals(objectId) && o.sentence == sentence) return true;
		return false;
		}
		*/
		public override bool Equals(object other)
		{
			if (!(other is Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention otherEnt = (Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention)other;
			return Equals(otherEnt, true);
		}

		public virtual bool HeadIncludes(Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention otherEnt, bool useSubType)
		{
			return otherEnt.GetSyntacticHeadTokenPosition() >= GetHeadTokenStart() && otherEnt.GetSyntacticHeadTokenPosition() < GetHeadTokenEnd() && ((type != null && otherEnt.type != null && type.Equals(otherEnt.type)) || (type == null && otherEnt.type
				 == null)) && (!useSubType || ((subType != null && otherEnt.subType != null && subType.Equals(otherEnt.subType)) || (subType == null && otherEnt.subType == null)));
		}

		public virtual bool Equals(Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention otherEnt, bool useSubType)
		{
			//
			// two mentions are equal if they are over the same sentence,
			// have the same head span, the same type/subtype, and the same text.
			// We need this for scoring NER, and in various places in KBP
			//
			if (sentence.Get(typeof(CoreAnnotations.TextAnnotation)).Equals(otherEnt.sentence.Get(typeof(CoreAnnotations.TextAnnotation))) && TextEquals(otherEnt) && LabelEquals(otherEnt, useSubType))
			{
				return true;
			}
			/*
			if(((headTokenSpan != null && headTokenSpan.equals(otherEnt.headTokenSpan)) ||
			(extentTokenSpan != null && extentTokenSpan.equals(otherEnt.extentTokenSpan))) &&
			((type != null && otherEnt.type != null && type.equals(otherEnt.type)) || (type == null && otherEnt.type == null)) &&
			(! useSubType || ((subType != null && otherEnt.subType != null && subType.equals(otherEnt.subType)) || (subType == null && otherEnt.subType == null))) &&
			AnnotationUtils.getTextContent(sentence, headTokenSpan).equals(AnnotationUtils.getTextContent(otherEnt.getSentence(), otherEnt.headTokenSpan))){
			return true;
			}
			*/
			return false;
		}

		/// <summary>Compares the labels of the two mentions</summary>
		/// <param name="otherEnt"/>
		/// <param name="useSubType"/>
		public virtual bool LabelEquals(Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention otherEnt, bool useSubType)
		{
			if (((type != null && otherEnt.type != null && type.Equals(otherEnt.type)) || (type == null && otherEnt.type == null)) && (!useSubType || ((subType != null && otherEnt.subType != null && subType.Equals(otherEnt.subType)) || (subType == null 
				&& otherEnt.subType == null))))
			{
				return true;
			}
			return false;
		}

		/// <summary>Compares the text spans of the two entity mentions.</summary>
		/// <param name="otherEnt"/>
		public virtual bool TextEquals(Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention otherEnt)
		{
			//
			// we attempt three comparisons:
			// a) if syntactic heads are defined we consider two texts similar if they have the same syntactic head
			//    (this is necessary because in NFL we compare entities with different spans but same heads, e.g. "49ers" vs "San Francisco 49ers"
			// b) if head spans are defined we consider two texts similar if they have the same head span
			// c) if extent spans are defined we consider two texts similar if they have the same extent span
			//
			if (syntacticHeadTokenPosition != -1 && otherEnt.syntacticHeadTokenPosition != -1)
			{
				if (syntacticHeadTokenPosition == otherEnt.syntacticHeadTokenPosition)
				{
					return true;
				}
				return false;
			}
			if (headTokenSpan != null && otherEnt.headTokenSpan != null)
			{
				if (headTokenSpan.Equals(otherEnt.headTokenSpan))
				{
					return true;
				}
				return false;
			}
			if (extentTokenSpan != null && otherEnt.extentTokenSpan != null)
			{
				if (extentTokenSpan.Equals(otherEnt.extentTokenSpan))
				{
					return true;
				}
				return false;
			}
			if (!this.GetExtentString().Equals(otherEnt.GetExtentString()))
			{
				return false;
			}
			return false;
		}

		/// <summary>Get the text value of this entity.</summary>
		/// <remarks>
		/// Get the text value of this entity.
		/// The headTokenSpan MUST be set before calling this method!
		/// </remarks>
		public override string GetValue()
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			// int lastEnd = -1;
			StringBuilder sb = new StringBuilder();
			for (int i = headTokenSpan.Start(); i < headTokenSpan.End(); i++)
			{
				CoreLabel token = tokens[i];
				// we are not guaranteed to have CharacterOffsets so we can't use them...
				/*
				Integer start = token.get(CoreAnnotations.CharacterOffsetBeginAnnotation.class);
				Integer end = token.get(CoreAnnotations.CharacterOffsetEndAnnotation.class);
				
				if (start != null && end != null) {
				if (lastEnd != -1 && !start.equals(lastEnd)) {
				sb.append(StringUtils.repeat(" ", start - lastEnd));
				lastEnd = end;
				}
				} else {
				if (lastEnd != -1) sb.append(" ");
				lastEnd = 0;
				}
				*/
				if (i > headTokenSpan.Start())
				{
					sb.Append(" ");
				}
				sb.Append(token.Word());
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			return "EntityMention [type=" + type + (subType != null ? ", subType=" + subType : string.Empty) + (mentionType != null ? ", mentionType=" + mentionType : string.Empty) + (objectId != null ? ", objectId=" + objectId : string.Empty) + (headTokenSpan
				 != null ? ", hstart=" + headTokenSpan.Start() + ", hend=" + headTokenSpan.End() : string.Empty) + (extentTokenSpan != null ? ", estart=" + extentTokenSpan.Start() + ", eend=" + extentTokenSpan.End() : string.Empty) + (syntacticHeadTokenPosition
				 >= 0 ? ", headPosition=" + syntacticHeadTokenPosition : string.Empty) + (headTokenSpan != null ? ", value=\"" + GetValue() + "\"" : string.Empty) + (normalizedName != null ? ", normalizedName=\"" + normalizedName + "\"" : string.Empty) + ", corefID="
				 + corefID + (typeProbabilities != null ? ", probs=" + ProbsToString() : string.Empty) + "]";
		}

		public override int GetHashCode()
		{
			int result = mentionType != null ? mentionType.GetHashCode() : 0;
			result = 31 * result + (headTokenSpan != null ? headTokenSpan.GetHashCode() : 0);
			result = 31 * result + (normalizedName != null ? normalizedName.GetHashCode() : 0);
			result = 31 * result + (extentTokenSpan != null ? extentTokenSpan.GetHashCode() : 0);
			return result;
		}

		internal class CompByHead : IComparator<EntityMention>
		{
			public virtual int Compare(EntityMention o1, EntityMention o2)
			{
				if (o1.GetHeadTokenStart() < o2.GetHeadTokenStart())
				{
					return -1;
				}
				else
				{
					if (o1.GetHeadTokenStart() > o2.GetHeadTokenStart())
					{
						return 1;
					}
					else
					{
						if (o1.GetHeadTokenEnd() < o2.GetHeadTokenEnd())
						{
							return -1;
						}
						else
						{
							if (o1.GetHeadTokenEnd() > o2.GetHeadTokenEnd())
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

		public static void SortByHeadSpan(IList<EntityMention> mentions)
		{
			mentions.Sort(new EntityMention.CompByHead());
		}

		private static int MentionCounter = 0;

		/// <summary>Creates a new unique id for an entity mention</summary>
		/// <returns>the new id</returns>
		public static string MakeUniqueId()
		{
			lock (typeof(EntityMention))
			{
				MentionCounter++;
				return "EntityMention-" + MentionCounter;
			}
		}
	}
}
