using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Class representing an ordered triple of objects, possibly typed.</summary>
	/// <remarks>
	/// Class representing an ordered triple of objects, possibly typed.
	/// Useful when you'd like a method to return three objects, or would like to put
	/// triples of objects in a Collection or Map. equals() and hashcode() should
	/// work properly.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	[System.Serializable]
	public class Triple<T1, T2, T3> : IComparable<Edu.Stanford.Nlp.Util.Triple<T1, T2, T3>>, IPrettyLoggable
	{
		private const long serialVersionUID = -4182871682751645440L;

		public T1 first;

		public T2 second;

		public T3 third;

		public Triple(T1 first, T2 second, T3 third)
		{
			this.first = first;
			this.second = second;
			this.third = third;
		}

		public virtual T1 First()
		{
			return first;
		}

		public virtual T2 Second()
		{
			return second;
		}

		public virtual T3 Third()
		{
			return third;
		}

		public virtual void SetFirst(T1 o)
		{
			first = o;
		}

		public virtual void SetSecond(T2 o)
		{
			second = o;
		}

		public virtual void SetThird(T3 o)
		{
			third = o;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.Triple))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.Triple triple = (Edu.Stanford.Nlp.Util.Triple)o;
			if (first != null ? !first.Equals(triple.first) : triple.first != null)
			{
				return false;
			}
			if (second != null ? !second.Equals(triple.second) : triple.second != null)
			{
				return false;
			}
			if (third != null ? !third.Equals(triple.third) : triple.third != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result;
			result = (first != null ? first.GetHashCode() : 0);
			result = 29 * result + (second != null ? second.GetHashCode() : 0);
			result = 29 * result + (third != null ? third.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "(" + first + "," + second + "," + third + ")";
		}

		public virtual IList<object> AsList()
		{
			return CollectionUtils.MakeList(first, second, third);
		}

		/// <summary>Returns a Triple constructed from X, Y, and Z.</summary>
		/// <remarks>
		/// Returns a Triple constructed from X, Y, and Z. Convenience method; the
		/// compiler will disambiguate the classes used for you so that you don't have
		/// to write out potentially long class names.
		/// </remarks>
		public static Edu.Stanford.Nlp.Util.Triple<X, Y, Z> MakeTriple<X, Y, Z>(X x, Y y, Z z)
		{
			return new Edu.Stanford.Nlp.Util.Triple<X, Y, Z>(x, y, z);
		}

		/// <summary><inheritDoc/></summary>
		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, this.AsList());
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Util.Triple<T1, T2, T3> another)
		{
			int comp = ((IComparable<T1>)First()).CompareTo(another.First());
			if (comp != 0)
			{
				return comp;
			}
			else
			{
				comp = ((IComparable<T2>)Second()).CompareTo(another.Second());
				if (comp != 0)
				{
					return comp;
				}
				else
				{
					return ((IComparable<T3>)Third()).CompareTo(another.Third());
				}
			}
		}
	}
}
