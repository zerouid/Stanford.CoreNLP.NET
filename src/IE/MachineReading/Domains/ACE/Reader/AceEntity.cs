using System.Collections.Generic;
using System.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>
	/// Implements the ACE
	/// <literal><entity></literal>
	/// construct.
	/// </summary>
	/// <author>David McClosky</author>
	public class AceEntity : AceElement
	{
		private string mType;

		private string mSubtype;

		private string mClass;

		private IList<AceEntityMention> mMentions;

		public AceEntity(string id, string type, string subtype, string cls)
			: base(id)
		{
			mType = type;
			mSubtype = subtype;
			mClass = cls;
			mMentions = new List<AceEntityMention>();
		}

		public virtual void AddMention(AceEntityMention m)
		{
			mMentions.Add(m);
			m.SetParent(this);
		}

		public virtual IList<AceEntityMention> GetMentions()
		{
			return mMentions;
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

		public virtual void SetClass(string s)
		{
			mClass = s;
		}

		public virtual string GetClasss()
		{
			return mClass;
		}

		public virtual string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AppendOffset(buffer, offset);
			buffer.Append("<entity ID=\"" + GetId() + "\" TYPE =\"" + AceToken.Others.Get(mType) + "\" SUBTYPE=\"" + AceToken.Others.Get(mSubtype) + "\" CLASS=\"" + AceToken.Others.Get(mClass) + "\">\n");
			foreach (AceEntityMention m in mMentions)
			{
				buffer.Append(m.ToXml(offset + 2));
				buffer.Append("\n");
			}
			AppendOffset(buffer, offset);
			buffer.Append("</entity>");
			return buffer.ToString();
		}
	}
}
