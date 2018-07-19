using System;
using System.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Stores one ACE relation mention</summary>
	public class AceRelationMention : AceMention
	{
		private string mLexicalCondition;

		/// <summary>The two argument mentions</summary>
		private AceRelationMentionArgument[] mArguments;

		/// <summary>the parent event</summary>
		private AceRelation mParent;

		public AceRelationMention(string id, AceCharSeq extent, string lc)
			: base(id, extent)
		{
			mLexicalCondition = lc;
			mArguments = new AceRelationMentionArgument[2];
		}

		public virtual AceRelationMentionArgument[] GetArgs()
		{
			return mArguments;
		}

		public virtual AceEntityMention GetArg(int which)
		{
			return mArguments[which].GetContent();
		}

		public virtual void SetArg(int which, AceEntityMention em, string role)
		{
			mArguments[which] = new AceRelationMentionArgument(role, em);
		}

		/// <summary>Retrieves the argument that appears *first* in the sentence</summary>
		public virtual AceEntityMention GetFirstArg()
		{
			if (GetArg(0).GetHead().GetTokenStart() <= GetArg(1).GetHead().GetTokenStart())
			{
				return GetArg(0);
			}
			return GetArg(1);
		}

		/// <summary>Retrieves the argument that appears *last* in the sentence</summary>
		public virtual AceEntityMention GetLastArg()
		{
			if (GetArg(0).GetHead().GetTokenStart() > GetArg(1).GetHead().GetTokenStart())
			{
				return GetArg(0);
			}
			return GetArg(1);
		}

		public virtual void SetParent(AceRelation e)
		{
			mParent = e;
		}

		public virtual AceRelation GetParent()
		{
			return mParent;
		}

		public virtual string GetLexicalCondition()
		{
			return mLexicalCondition;
		}

		/// <summary>Fetches the id of the sentence that contains this mention</summary>
		public virtual int GetSentence(AceDocument doc)
		{
			return doc.GetToken(GetArg(0).GetHead().GetTokenStart()).GetSentence();
		}

		/// <summary>Returns the smallest start of the two args heads</summary>
		public virtual int GetMinTokenStart()
		{
			int s1 = GetArg(0).GetHead().GetTokenStart();
			int s2 = GetArg(1).GetHead().GetTokenStart();
			return Math.Min(s1, s2);
		}

		/// <summary>Returns the largest end of the two args heads</summary>
		public virtual int GetMaxTokenEnd()
		{
			int s1 = GetArg(0).GetHead().GetTokenEnd();
			int s2 = GetArg(1).GetHead().GetTokenEnd();
			return Math.Max(s1, s2);
		}

		public override string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AppendOffset(buffer, offset);
			buffer.Append("<relation_mention ID=\"" + GetId() + "\"");
			if (mLexicalCondition != null)
			{
				buffer.Append(" LEXICALCONDITION=\"" + mLexicalCondition + "\"");
			}
			buffer.Append(">\n");
			buffer.Append(mExtent.ToXml("extent", offset + 2));
			buffer.Append("\n");
			AceRelationMentionArgument arg1 = GetArgs()[0];
			AceRelationMentionArgument arg2 = GetArgs()[1];
			if (arg1.GetRole().Equals("Arg-1"))
			{
				// left to right
				buffer.Append(arg1.ToXml(offset + 2) + "\n");
				buffer.Append(arg2.ToXml(offset + 2) + "\n");
			}
			else
			{
				// right to left
				buffer.Append(arg2.ToXml(offset + 2) + "\n");
				buffer.Append(arg1.ToXml(offset + 2) + "\n");
			}
			AppendOffset(buffer, offset);
			buffer.Append("</relation_mention>");
			return buffer.ToString();
		}
	}
}
