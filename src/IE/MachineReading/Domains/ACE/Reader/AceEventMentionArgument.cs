

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceEventMentionArgument : AceMentionArgument
	{
		public AceEventMentionArgument(string role, AceEntityMention content)
			: base(role, content, "event")
		{
		}
	}
}
