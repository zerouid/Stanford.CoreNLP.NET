using System.Collections.Generic;
using System.Text;


namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Stores one ACE relation</summary>
	public class AceRelation : AceElement
	{
		private string mType;

		private string mSubtype;

		private string mModality;

		private string mTense;

		/// <summary>The list of mentions for this event</summary>
		private IList<AceRelationMention> mMentions;

		public const string NilLabel = "nil";

		public AceRelation(string id, string type, string subtype, string modality, string tense)
			: base(id)
		{
			mType = type;
			mSubtype = subtype;
			mModality = modality;
			mTense = tense;
			mMentions = new List<AceRelationMention>();
		}

		public virtual void AddMention(AceRelationMention m)
		{
			mMentions.Add(m);
			m.SetParent(this);
		}

		public virtual AceRelationMention GetMention(int which)
		{
			return mMentions[which];
		}

		public virtual int GetMentionCount()
		{
			return mMentions.Count;
		}

		public virtual string GetType()
		{
			return mType;
		}

		public virtual void SetType(string s)
		{
			mType = s;
		}

		public virtual string GetSubtype()
		{
			return mSubtype;
		}

		public virtual void SetSubtype(string s)
		{
			mSubtype = s;
		}

		public virtual string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AppendOffset(buffer, offset);
			buffer.Append("<relation ID=\"" + GetId() + "\" TYPE =\"" + mType + "\" SUBTYPE=\"" + mSubtype + "\" MODALITY=\"" + mModality + "\" TENSE=\"" + mTense + "\">\n");
			AceRelationMentionArgument arg1 = mMentions[0].GetArgs()[0];
			AceRelationMentionArgument arg2 = mMentions[0].GetArgs()[1];
			if (arg1.GetRole().Equals("Arg-1"))
			{
				// left to right
				buffer.Append(arg1.ToXmlShort(offset + 2) + "\n");
				buffer.Append(arg2.ToXmlShort(offset + 2) + "\n");
			}
			else
			{
				// right to left
				buffer.Append(arg2.ToXmlShort(offset + 2) + "\n");
				buffer.Append(arg1.ToXmlShort(offset + 2) + "\n");
			}
			foreach (AceRelationMention m in mMentions)
			{
				buffer.Append(m.ToXml(offset + 2));
				buffer.Append("\n");
			}
			AppendOffset(buffer, offset);
			buffer.Append("</relation>");
			return buffer.ToString();
		}
	}
}
