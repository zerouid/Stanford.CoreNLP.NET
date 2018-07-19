using Edu.Stanford.Nlp.Maxent;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class DataWordTag : DataGeneric
	{
		private readonly History h;

		private readonly int yNum;

		private readonly string tag;

		internal DataWordTag(History h, int y, string tag)
		{
			this.h = h;
			this.yNum = y;
			this.tag = tag;
		}

		public virtual History GetHistory()
		{
			return h;
		}

		public override string GetY()
		{
			return tag;
		}

		public virtual int GetYInd()
		{
			return yNum;
		}
	}
}
