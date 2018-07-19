using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceRelationMentionArgument : AceMentionArgument
	{
		public AceRelationMentionArgument(string role, AceEntityMention content)
			: base(role, content, "relation")
		{
		}
	}
}
