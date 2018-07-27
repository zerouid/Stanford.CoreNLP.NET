

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceMention : AceElement
	{
		protected internal AceCharSeq mExtent;

		protected internal AceMention(string id, AceCharSeq extent)
			: base(id)
		{
			mExtent = extent;
		}

		public virtual AceCharSeq GetExtent()
		{
			return mExtent;
		}

		public virtual string ToXml(int offset)
		{
			return string.Empty;
		}
	}
}
