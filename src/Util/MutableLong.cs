using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A class for Long objects that you can change.</summary>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public sealed class MutableLong : Number, IComparable<Edu.Stanford.Nlp.Util.MutableLong>
	{
		private long i;

		// Mutable
		public void Set(long i)
		{
			this.i = i;
		}

		public override int GetHashCode()
		{
			return (int)(i ^ ((long)(((ulong)i) >> 32)));
		}

		/// <summary>Compares this object to the specified object.</summary>
		/// <remarks>
		/// Compares this object to the specified object.  The result is
		/// <see langword="true"/>
		/// if and only if the argument is not
		/// <see langword="null"/>
		/// and is an
		/// <c>MutableLong</c>
		/// object that
		/// contains the same
		/// <c>long</c>
		/// value as this object.
		/// Note that a MutableLong isn't and can't be equal to an Long.
		/// </remarks>
		/// <param name="obj">the object to compare with.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if the objects are the same;
		/// <see langword="false"/>
		/// otherwise.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			else
			{
				if (obj is Edu.Stanford.Nlp.Util.MutableLong)
				{
					return i == ((Edu.Stanford.Nlp.Util.MutableLong)obj).i;
				}
			}
			return false;
		}

		public override string ToString()
		{
			return System.Convert.ToString(i);
		}

		// Comparable interface
		/// <summary>Compares two <code>MutableLong</code> objects numerically.</summary>
		/// <param name="anotherMutableLong">
		/// the <code>MutableLong</code> to be
		/// compared.
		/// </param>
		/// <returns>
		/// The value <code>0</code> if this <code>MutableLong</code> is
		/// equal to the argument <code>MutableLong</code>; a value less than
		/// <code>0</code> if this <code>MutableLong</code> is numerically less
		/// than the argument <code>MutableLong</code>; and a value greater
		/// than <code>0</code> if this <code>MutableLong</code> is numerically
		/// greater than the argument <code>MutableLong</code> (signed
		/// comparison).
		/// </returns>
		public int CompareTo(Edu.Stanford.Nlp.Util.MutableLong anotherMutableLong)
		{
			long thisVal = this.i;
			long anotherVal = anotherMutableLong.i;
			return (thisVal < anotherVal ? -1 : (thisVal == anotherVal ? 0 : 1));
		}

		// Number interface
		public override int IntValue()
		{
			return (int)i;
		}

		public override long LongValue()
		{
			return i;
		}

		public override short ShortValue()
		{
			return (short)i;
		}

		public override byte ByteValue()
		{
			return unchecked((byte)i);
		}

		public override float FloatValue()
		{
			return i;
		}

		public override double DoubleValue()
		{
			return i;
		}

		/// <summary>Add the argument to the value of this long.</summary>
		/// <remarks>Add the argument to the value of this long.  A convenience method.</remarks>
		/// <param name="val">Value to be added to this long</param>
		public void IncValue(long val)
		{
			i += val;
		}

		public MutableLong()
			: this(0)
		{
		}

		public MutableLong(long i)
		{
			this.i = i;
		}

		private const long serialVersionUID = 624465615824626762L;
	}
}
