


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A class for Integer objects that you can change.</summary>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public sealed class MutableInteger : Number, IComparable<Edu.Stanford.Nlp.Util.MutableInteger>
	{
		private int i;

		// Mutable
		public void Set(int i)
		{
			this.i = i;
		}

		public override int GetHashCode()
		{
			return i;
		}

		/// <summary>Compares this object to the specified object.</summary>
		/// <remarks>
		/// Compares this object to the specified object.  The result is
		/// <code>true</code> if and only if the argument is not
		/// <code>null</code> and is an <code>MutableInteger</code> object that
		/// contains the same <code>int</code> value as this object.
		/// Note that a MutableInteger isn't and can't be equal to an Integer.
		/// </remarks>
		/// <param name="obj">the object to compare with.</param>
		/// <returns>
		/// <code>true</code> if the objects are the same;
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj is Edu.Stanford.Nlp.Util.MutableInteger)
			{
				return i == ((Edu.Stanford.Nlp.Util.MutableInteger)obj).i;
			}
			return false;
		}

		public override string ToString()
		{
			return int.ToString(i);
		}

		// Comparable interface
		/// <summary>Compares two <code>MutableInteger</code> objects numerically.</summary>
		/// <param name="anotherMutableInteger">
		/// the <code>MutableInteger</code> to be
		/// compared.
		/// </param>
		/// <returns>
		/// The value <code>0</code> if this <code>MutableInteger</code> is
		/// equal to the argument <code>MutableInteger</code>; a value less than
		/// <code>0</code> if this <code>MutableInteger</code> is numerically less
		/// than the argument <code>MutableInteger</code>; and a value greater
		/// than <code>0</code> if this <code>MutableInteger</code> is numerically
		/// greater than the argument <code>MutableInteger</code> (signed
		/// comparison).
		/// </returns>
		public int CompareTo(Edu.Stanford.Nlp.Util.MutableInteger anotherMutableInteger)
		{
			int thisVal = this.i;
			int anotherVal = anotherMutableInteger.i;
			return (thisVal < anotherVal ? -1 : (thisVal == anotherVal ? 0 : 1));
		}

		// Number interface
		public override int IntValue()
		{
			return i;
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

		/// <summary>Add the argument to the value of this integer.</summary>
		/// <remarks>Add the argument to the value of this integer.  A convenience method.</remarks>
		/// <param name="val">Value to be added to this integer</param>
		public void IncValue(int val)
		{
			i += val;
		}

		public MutableInteger()
			: this(0)
		{
		}

		public MutableInteger(int i)
		{
			this.i = i;
		}

		private const long serialVersionUID = 624465615824626762L;
	}
}
