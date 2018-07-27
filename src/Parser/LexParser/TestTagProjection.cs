using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein</author>
	[System.Serializable]
	public class TestTagProjection : ITagProjection
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TestTagProjection));

		private const long serialVersionUID = 9161675508802284114L;

		// Looks like the intended behavior of TestTagProjection is:
		// 1) Include the basic category (everything before a '-' or '^' annotation)
		// 2) Include any annotation introduced with '-'
		// 3) Exclude any annotation introduced with '^'
		// 4) Annotations introduced with other characters will be included or excluded
		//    as determined by the previous annotation or basic category.
		//
		// This seems awfully haphazard :(
		//
		//  Roger
		public virtual string Project(string tagStr)
		{
			StringBuilder sb = new StringBuilder();
			bool good = true;
			for (int pos = 0; pos < len; pos++)
			{
				char c = tagStr[pos];
				if (c == '-')
				{
					good = true;
				}
				else
				{
					if (c == '^')
					{
						good = false;
					}
				}
				if (good)
				{
					sb.Append(c);
				}
			}
			string ret = sb.ToString();
			// log.info("TTP mapped " + tagStr + " to " + ret);
			return ret;
		}
	}
}
