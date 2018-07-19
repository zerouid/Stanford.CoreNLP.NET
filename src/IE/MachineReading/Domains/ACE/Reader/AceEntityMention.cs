using System.Collections.Generic;
using System.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>
	/// Implements the ACE
	/// <c>&lt;entity_mention&gt;</c>
	/// construct.
	/// </summary>
	/// <author>David McClosky</author>
	public class AceEntityMention : AceMention
	{
		public override string ToString()
		{
			return "AceEntityMention [mHead=" + mHead + ", mLdctype=" + mLdctype + ", mType=" + mType + "]";
		}

		private string mType;

		private string mLdctype;

		private AceCharSeq mHead;

		/// <summary>Position of the head word of this mention</summary>
		private int mHeadTokenPosition;

		/// <summary>The parent entity</summary>
		private AceEntity mParent;

		/// <summary>The set of relation mentions that contain this entity mention</summary>
		private IList<AceRelationMention> mRelationMentions;

		/// <summary>The set of event mentions that contain this entity mention</summary>
		private IList<AceEventMention> mEventMentions;

		public AceEntityMention(string id, string type, string ldctype, AceCharSeq extent, AceCharSeq head)
			: base(id, extent)
		{
			mType = type;
			mLdctype = ldctype;
			mHead = head;
			mExtent = extent;
			mHeadTokenPosition = -1;
			mParent = null;
			mRelationMentions = new List<AceRelationMention>();
			mEventMentions = new List<AceEventMention>();
		}

		public virtual string GetMention()
		{
			return mType;
		}

		public virtual void SetParent(AceEntity e)
		{
			mParent = e;
		}

		public virtual AceEntity GetParent()
		{
			return mParent;
		}

		public virtual AceCharSeq GetHead()
		{
			return mHead;
		}

		public override AceCharSeq GetExtent()
		{
			return mExtent;
		}

		public virtual int GetHeadTokenPosition()
		{
			return mHeadTokenPosition;
		}

		public virtual void SetType(string s)
		{
			mType = s;
		}

		public virtual string GetType()
		{
			return mType;
		}

		public virtual void SetLdctype(string s)
		{
			mLdctype = s;
		}

		public virtual string GetLdctype()
		{
			return mLdctype;
		}

		public virtual void AddRelationMention(AceRelationMention rm)
		{
			mRelationMentions.Add(rm);
		}

		public virtual IList<AceRelationMention> GetRelationMentions()
		{
			return mRelationMentions;
		}

		public virtual void AddEventMention(AceEventMention rm)
		{
			mEventMentions.Add(rm);
		}

		public virtual IList<AceEventMention> GetEventMentions()
		{
			return mEventMentions;
		}

		public override string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			string mentionType = mType;
			AppendOffset(buffer, offset);
			buffer.Append("<entity_mention ID=\"" + GetId() + "\" TYPE =\"" + mentionType + "\" LDCTYPE=\"" + mLdctype + "\">\n");
			buffer.Append(mExtent.ToXml("extent", offset + 2));
			buffer.Append("\n");
			buffer.Append(mHead.ToXml("head", offset + 2));
			buffer.Append("\n");
			AppendOffset(buffer, offset);
			buffer.Append("</entity_mention>");
			if (mentionType.Equals("NAM"))
			{
				// XXX: <entity_attributes> should be in Entity.toXml()
				buffer.Append("\n");
				AppendOffset(buffer, offset);
				buffer.Append("<entity_attributes>\n");
				AppendOffset(buffer, offset + 2);
				buffer.Append("<name NAME=\"" + mHead.GetText() + "\">\n");
				buffer.Append(mHead.ToXml(offset + 4) + "\n");
				AppendOffset(buffer, offset + 2);
				buffer.Append("</name>\n");
				AppendOffset(buffer, offset);
				buffer.Append("</entity_attributes>");
			}
			return buffer.ToString();
		}

		private static bool Contains(List<int> set, int elem)
		{
			foreach (int aSet in set)
			{
				if (elem == aSet)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Detects the head word of this mention
		/// Heuristic:
		/// (a) the last token in mHead, if there are no prepositions
		/// (b) the last word before the first preposition
		/// Note: the mHead must be already matched against tokens!
		/// </summary>
		public virtual void DetectHeadToken(AceDocument doc)
		{
			List<int> preps = new List<int>();
			preps.Add(AceToken.Others.Get("IN"));
			for (int i = mHead.GetTokenStart(); i <= mHead.GetTokenEnd(); i++)
			{
				// found a prep
				if (Contains(preps, doc.GetToken(i).GetPos()) && i > mHead.GetTokenStart())
				{
					mHeadTokenPosition = i - 1;
					return;
				}
			}
			// set as the last word in mHead
			mHeadTokenPosition = mHead.GetTokenEnd();
		}

		/// <summary>Verifies if this mention appears before the parameter in textual order</summary>
		public virtual bool Before(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceEntityMention em)
		{
			if (mHead.GetByteEnd() < em.mHead.GetByteStart())
			{
				return true;
			}
			return false;
		}

		/// <summary>Verifies if this mention appears after the parameter in textual order</summary>
		public virtual bool After(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceEntityMention em)
		{
			if (mHead.GetByteStart() > em.mHead.GetByteEnd())
			{
				return true;
			}
			return false;
		}
	}
}
