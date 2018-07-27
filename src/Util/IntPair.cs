

namespace Edu.Stanford.Nlp.Util
{
	[System.Serializable]
	public class IntPair : IntTuple
	{
		private const long serialVersionUID = 1L;

		public IntPair()
			: base(2)
		{
		}

		public IntPair(int src, int trgt)
			: base(2)
		{
			elements[0] = src;
			elements[1] = trgt;
		}

		/// <summary>Return the first element of the pair</summary>
		public virtual int GetSource()
		{
			return Get(0);
		}

		/// <summary>Return the second element of the pair</summary>
		public virtual int GetTarget()
		{
			return Get(1);
		}

		public override IntTuple GetCopy()
		{
			return new Edu.Stanford.Nlp.Util.IntPair(elements[0], elements[1]);
		}

		public override bool Equals(object iO)
		{
			if (!(iO is Edu.Stanford.Nlp.Util.IntPair))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.IntPair i = (Edu.Stanford.Nlp.Util.IntPair)iO;
			return elements[0] == i.Get(0) && elements[1] == i.Get(1);
		}

		public override int GetHashCode()
		{
			return elements[0] * 17 + elements[1];
		}
	}
}
