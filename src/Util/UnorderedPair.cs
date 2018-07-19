using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Holds an unordered pair of objects.</summary>
	/// <author>Dan Klein</author>
	/// <version>2/7/01</version>
	[System.Serializable]
	public class UnorderedPair<T1, T2> : Pair<T1, T2>
	{
		private const long serialVersionUID = 1L;

		public override string ToString()
		{
			return "{" + first + "," + second + "}";
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Util.UnorderedPair)
			{
				Edu.Stanford.Nlp.Util.UnorderedPair p = (Edu.Stanford.Nlp.Util.UnorderedPair)o;
				return (((first == null ? p.first == null : first.Equals(p.first)) && (second == null ? p.second == null : second.Equals(p.second))) || ((first == null ? p.second == null : first.Equals(p.second)) && (second == null ? p.first == null : second
					.Equals(p.first))));
			}
			return false;
		}

		public override int GetHashCode()
		{
			int firstHashCode = (first == null ? 0 : first.GetHashCode());
			int secondHashCode = (second == null ? 0 : second.GetHashCode());
			if (firstHashCode != secondHashCode)
			{
				return (((firstHashCode & secondHashCode) << 16) ^ ((firstHashCode | secondHashCode)));
			}
			else
			{
				return firstHashCode;
			}
		}

		public override int CompareTo(Pair<T1, T2> o)
		{
			Edu.Stanford.Nlp.Util.UnorderedPair other = (Edu.Stanford.Nlp.Util.UnorderedPair)o;
			// get canonical order of this and other
			object this1 = first;
			object this2 = second;
			int thisC = ((IComparable)first).CompareTo(second);
			if (thisC < 0)
			{
				// switch em
				this1 = second;
				this2 = first;
			}
			object other1 = first;
			object other2 = second;
			int otherC = ((IComparable)other.first).CompareTo(other.second);
			if (otherC < 0)
			{
				// switch em
				other1 = second;
				other2 = first;
			}
			int c1 = ((IComparable)this1).CompareTo(other1);
			if (c1 != 0)
			{
				return c1;
			}
			// base it on the first
			int c2 = ((IComparable)this2).CompareTo(other2);
			if (c2 != 0)
			{
				return c1;
			}
			// base it on the second
			return 0;
		}

		public UnorderedPair()
		{
			// must be equal
			first = null;
			second = null;
		}

		public UnorderedPair(T1 first, T2 second)
		{
			this.first = first;
			this.second = second;
		}
	}
}
