using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	public class BiesModifiedMapper : LDCPosMapper
	{
		public BiesModifiedMapper()
		{
			mapping = Pattern.Compile("(\\S+)\t(\\S+)");
		}
	}
}
