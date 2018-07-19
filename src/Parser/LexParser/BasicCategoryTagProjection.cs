using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein</author>
	[System.Serializable]
	public class BasicCategoryTagProjection : ITagProjection
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.BasicCategoryTagProjection));

		private const long serialVersionUID = -2322431101811335089L;

		internal ITreebankLanguagePack tlp;

		public BasicCategoryTagProjection(ITreebankLanguagePack tlp)
		{
			this.tlp = tlp;
		}

		public virtual string Project(string tagStr)
		{
			// return tagStr;
			string ret = tlp.BasicCategory(tagStr);
			// log.info("BCTP mapped " + tagStr + " to " + ret);
			return ret;
		}
	}
}
