using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Abstract class for parse items.</summary>
	/// <author>Dan Klein</author>
	public abstract class Item : IScored
	{
		public int start;

		public int end;

		public int state;

		public int head;

		public int tag;

		public Edge backEdge;

		public double iScore = double.NegativeInfinity;

		public double oScore = double.NegativeInfinity;

		private readonly bool exhaustiveTest;

		public Item(bool exhaustiveTest)
		{
			this.exhaustiveTest = exhaustiveTest;
		}

		public Item(Edu.Stanford.Nlp.Parser.Lexparser.Item item)
		{
			start = item.start;
			end = item.end;
			state = item.state;
			head = item.head;
			tag = item.tag;
			backEdge = item.backEdge;
			iScore = item.iScore;
			oScore = item.oScore;
			this.exhaustiveTest = item.exhaustiveTest;
		}

		public virtual double Score()
		{
			if (exhaustiveTest)
			{
				return iScore;
			}
			else
			{
				return iScore + oScore;
			}
		}

		public virtual bool IsEdge()
		{
			return false;
		}

		public virtual bool IsPreHook()
		{
			return false;
		}

		public virtual bool IsPostHook()
		{
			return false;
		}
	}
}
