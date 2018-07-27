using System.Text;


namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceMentionArgument
	{
		protected internal readonly string mRole;

		protected internal readonly AceEntityMention mContent;

		private readonly string mentionType;

		public AceMentionArgument(string role, AceEntityMention content, string mentionType)
		{
			// in practice, event or relation
			mRole = role;
			mContent = content;
			this.mentionType = mentionType;
		}

		public virtual AceEntityMention GetContent()
		{
			return mContent;
		}

		public virtual string GetRole()
		{
			return mRole;
		}

		public virtual string ToXml(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AceElement.AppendOffset(buffer, offset);
			buffer.Append("<" + mentionType + "_mention_argument REFID=\"" + mContent.GetId() + "\" ROLE=\"" + mRole + "\">\n");
			//buffer.append(getContent().toXml(offset + 2));
			AceCharSeq ext = GetContent().GetExtent();
			buffer.Append(ext.ToXml("extent", offset + 2));
			buffer.Append("\n");
			AceElement.AppendOffset(buffer, offset);
			buffer.Append("</" + mentionType + "_mention_argument>");
			return buffer.ToString();
		}

		public virtual string ToXmlShort(int offset)
		{
			StringBuilder buffer = new StringBuilder();
			AceElement.AppendOffset(buffer, offset);
			buffer.Append("<" + mentionType + "_argument REFID=\"" + mContent.GetParent().GetId() + "\" ROLE=\"" + mRole + "\"/>");
			return buffer.ToString();
		}
	}
}
