

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Just a single integer</summary>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	[System.Serializable]
	public class IntUni : IntTuple
	{
		public IntUni()
			: base(1)
		{
		}

		public IntUni(int src)
			: base(1)
		{
			elements[0] = src;
		}

		public virtual int GetSource()
		{
			return elements[0];
		}

		public virtual void SetSource(int src)
		{
			elements[0] = src;
		}

		public override IntTuple GetCopy()
		{
			Edu.Stanford.Nlp.Util.IntUni nT = new Edu.Stanford.Nlp.Util.IntUni(elements[0]);
			return nT;
		}

		public virtual void Add(int val)
		{
			elements[0] += val;
		}

		private const long serialVersionUID = -7182556672628741200L;
	}
}
