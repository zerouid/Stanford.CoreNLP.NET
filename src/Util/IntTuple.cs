using System;
using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A tuple of int.</summary>
	/// <remarks>
	/// A tuple of int. There are special classes for IntUni, IntPair, IntTriple
	/// and IntQuadruple. The motivation for that was the different hashCode
	/// implementations.
	/// By using the static IntTuple.getIntTuple(numElements) one can obtain an
	/// instance of the appropriate sub-class.
	/// </remarks>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	[System.Serializable]
	public class IntTuple : IComparable<Edu.Stanford.Nlp.Util.IntTuple>
	{
		internal readonly int[] elements;

		private const long serialVersionUID = 7266305463893511982L;

		public IntTuple(int[] arr)
		{
			elements = arr;
		}

		public IntTuple(int num)
		{
			elements = new int[num];
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Util.IntTuple o)
		{
			int commonLen = Math.Min(o.Length(), Length());
			for (int i = 0; i < commonLen; i++)
			{
				int a = Get(i);
				int b = o.Get(i);
				if (a < b)
				{
					return -1;
				}
				if (b < a)
				{
					return 1;
				}
			}
			if (o.Length() == Length())
			{
				return 0;
			}
			else
			{
				return (Length() < o.Length()) ? -1 : 1;
			}
		}

		public virtual int Get(int num)
		{
			return elements[num];
		}

		public virtual void Set(int num, int val)
		{
			elements[num] = val;
		}

		public virtual void ShiftLeft()
		{
			System.Array.Copy(elements, 1, elements, 0, elements.Length - 1);
			// the API does guarantee that this works when src and dest overlap, as here
			elements[elements.Length - 1] = 0;
		}

		public virtual Edu.Stanford.Nlp.Util.IntTuple GetCopy()
		{
			Edu.Stanford.Nlp.Util.IntTuple copy = Edu.Stanford.Nlp.Util.IntTuple.GetIntTuple(elements.Length);
			//new IntTuple(numElements);
			System.Array.Copy(elements, 0, copy.elements, 0, elements.Length);
			return copy;
		}

		public virtual int[] Elems()
		{
			return elements;
		}

		public override bool Equals(object iO)
		{
			if (!(iO is Edu.Stanford.Nlp.Util.IntTuple))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.IntTuple i = (Edu.Stanford.Nlp.Util.IntTuple)iO;
			if (i.elements.Length != elements.Length)
			{
				return false;
			}
			for (int j = 0; j < elements.Length; j++)
			{
				if (elements[j] != i.Get(j))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int sum = 0;
			foreach (int element in elements)
			{
				sum = sum * 17 + element;
			}
			return sum;
		}

		public virtual int Length()
		{
			return elements.Length;
		}

		public static Edu.Stanford.Nlp.Util.IntTuple GetIntTuple(int num)
		{
			if (num == 1)
			{
				return new IntUni();
			}
			if ((num == 2))
			{
				return new IntPair();
			}
			if (num == 3)
			{
				return new IntTriple();
			}
			if (num == 4)
			{
				return new IntQuadruple();
			}
			else
			{
				return new Edu.Stanford.Nlp.Util.IntTuple(num);
			}
		}

		public static Edu.Stanford.Nlp.Util.IntTuple GetIntTuple(IList<int> integers)
		{
			Edu.Stanford.Nlp.Util.IntTuple t = Edu.Stanford.Nlp.Util.IntTuple.GetIntTuple(integers.Count);
			for (int i = 0; i < t.Length(); i++)
			{
				t.Set(i, integers[i]);
			}
			return t;
		}

		public override string ToString()
		{
			StringBuilder name = new StringBuilder();
			for (int i = 0; i < elements.Length; i++)
			{
				name.Append(Get(i));
				if (i < elements.Length - 1)
				{
					name.Append(' ');
				}
			}
			return name.ToString();
		}

		public static Edu.Stanford.Nlp.Util.IntTuple Concat(Edu.Stanford.Nlp.Util.IntTuple t1, Edu.Stanford.Nlp.Util.IntTuple t2)
		{
			int n1 = t1.Length();
			int n2 = t2.Length();
			Edu.Stanford.Nlp.Util.IntTuple res = Edu.Stanford.Nlp.Util.IntTuple.GetIntTuple(n1 + n2);
			for (int j = 0; j < n1; j++)
			{
				res.Set(j, t1.Get(j));
			}
			for (int i = 0; i < n2; i++)
			{
				res.Set(n1 + i, t2.Get(i));
			}
			return res;
		}

		public virtual void Print()
		{
			string s = ToString();
			System.Console.Out.Write(s);
		}
	}
}
