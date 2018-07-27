

namespace Edu.Stanford.Nlp.Util
{
	[System.Serializable]
	public class IntTriple : IntTuple
	{
		private const long serialVersionUID = -3744404627253652799L;

		public IntTriple()
			: base(3)
		{
		}

		public IntTriple(int src, int mid, int trgt)
			: base(3)
		{
			elements[0] = src;
			elements[1] = mid;
			elements[2] = trgt;
		}

		public override IntTuple GetCopy()
		{
			Edu.Stanford.Nlp.Util.IntTriple nT = new Edu.Stanford.Nlp.Util.IntTriple(elements[0], elements[1], elements[2]);
			return nT;
		}

		public virtual int GetSource()
		{
			return elements[0];
		}

		public virtual int GetTarget()
		{
			return elements[2];
		}

		public virtual int GetMiddle()
		{
			return elements[1];
		}
	}
}
