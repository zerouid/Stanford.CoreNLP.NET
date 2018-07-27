using System.Text;


namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Base class for all ACE annotation elements.</summary>
	public class AceElement
	{
		/// <summary>Unique identifier for this element/</summary>
		protected internal readonly string mId;

		public AceElement(string id)
		{
			mId = id;
		}

		public virtual string GetId()
		{
			return mId;
		}

		// todo [cdm 2014]: Change this to using StringBuilder or Appendable or similar
		public static void AppendOffset(StringBuilder buffer, int offset)
		{
			for (int i = 0; i < offset; i++)
			{
				buffer.Append(' ');
			}
		}
	}
}
