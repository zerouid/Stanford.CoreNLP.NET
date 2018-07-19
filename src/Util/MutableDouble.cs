using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A class for Double objects that you can change.</summary>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public sealed class MutableDouble : Number, IComparable<Edu.Stanford.Nlp.Util.MutableDouble>
	{
		private double d;

		// Mutable
		public void Set(double d)
		{
			this.d = d;
		}

		public override int GetHashCode()
		{
			long bits = double.DoubleToLongBits(d);
			return (int)(bits ^ ((long)(((ulong)bits) >> 32)));
		}

		/// <summary>Compares this object to the specified object.</summary>
		/// <remarks>
		/// Compares this object to the specified object.  The result is
		/// <code>true</code> if and only if the argument is not
		/// <code>null</code> and is an <code>MutableDouble</code> object that
		/// contains the same <code>double</code> value as this object.
		/// Note that a MutableDouble isn't and can't be equal to an Double.
		/// </remarks>
		/// <param name="obj">the object to compare with.</param>
		/// <returns>
		/// <code>true</code> if the objects are the same;
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool Equals(object obj)
		{
			return obj is Edu.Stanford.Nlp.Util.MutableDouble && d == ((Edu.Stanford.Nlp.Util.MutableDouble)obj).d;
		}

		public override string ToString()
		{
			return double.ToString(d);
		}

		// Comparable interface
		/// <summary>Compares two <code>MutableDouble</code> objects numerically.</summary>
		/// <param name="anotherMutableDouble">
		/// the <code>MutableDouble</code> to be
		/// compared.
		/// </param>
		/// <returns>
		/// Tthe value <code>0</code> if this <code>MutableDouble</code> is
		/// equal to the argument <code>MutableDouble</code>; a value less than
		/// <code>0</code> if this <code>MutableDouble</code> is numerically less
		/// than the argument <code>MutableDouble</code>; and a value greater
		/// than <code>0</code> if this <code>MutableDouble</code> is numerically
		/// greater than the argument <code>MutableDouble</code> (signed
		/// comparison).
		/// </returns>
		public int CompareTo(Edu.Stanford.Nlp.Util.MutableDouble anotherMutableDouble)
		{
			double thisVal = this.d;
			double anotherVal = anotherMutableDouble.d;
			return (thisVal < anotherVal ? -1 : (thisVal == anotherVal ? 0 : 1));
		}

		// Number interface
		public override int IntValue()
		{
			return (int)d;
		}

		public override long LongValue()
		{
			return (long)d;
		}

		public override short ShortValue()
		{
			return (short)d;
		}

		public override byte ByteValue()
		{
			return unchecked((byte)d);
		}

		public override float FloatValue()
		{
			return (float)d;
		}

		public override double DoubleValue()
		{
			return d;
		}

		public MutableDouble()
			: this(0.0)
		{
		}

		public MutableDouble(double d)
		{
			this.d = d;
		}

		public MutableDouble(Number num)
		{
			this.d = num;
		}

		private const long serialVersionUID = 624465615824626762L;
	}
}
