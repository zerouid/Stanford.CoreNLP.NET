

namespace Edu.Stanford.Nlp.Util
{
	[System.Serializable]
	public class IntQuadruple : IntTuple
	{
		private const long serialVersionUID = 7154973101012473479L;

		public IntQuadruple()
			: base(4)
		{
		}

		public IntQuadruple(int src, int mid, int trgt, int trgt2)
			: base(4)
		{
			elements[0] = src;
			elements[1] = mid;
			elements[2] = trgt;
			elements[3] = trgt2;
		}

		public override IntTuple GetCopy()
		{
			Edu.Stanford.Nlp.Util.IntQuadruple nT = new Edu.Stanford.Nlp.Util.IntQuadruple(elements[0], elements[1], elements[2], elements[3]);
			return nT;
		}

		public virtual int GetSource()
		{
			return Get(0);
		}

		public virtual int GetMiddle()
		{
			return Get(1);
		}

		public virtual int GetTarget()
		{
			return Get(2);
		}

		public virtual int GetTarget2()
		{
			return Get(3);
		}
	}
}
