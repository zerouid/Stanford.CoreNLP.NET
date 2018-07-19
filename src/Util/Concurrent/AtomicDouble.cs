/*
* Written by Doug Lea and Martin Buchholz with assistance from
* members of JCP JSR-166 Expert Group and released to the public
* domain, as explained at
* http://creativecommons.org/publicdomain/zero/1.0/
*
* Version: http://gee.cs.oswego.edu/cgi-bin/viewcvs.cgi/jsr166/src/jsr166e/extra/AtomicDouble.java?revision=1.19
*/
using Java.IO;
using Java.Lang;
using Java.Util.Concurrent.Atomic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>
	/// A
	/// <c>double</c>
	/// value that may be updated atomically.  See the
	/// <see cref="Java.Util.Concurrent.Atomic"/>
	/// package specification for
	/// description of the properties of atomic variables.  An
	/// <c>AtomicDouble</c>
	/// is used in applications such as atomic accumulation,
	/// and cannot be used as a replacement for a
	/// <see cref="double"/>
	/// .  However,
	/// this class does extend
	/// <c>Number</c>
	/// to allow uniform access by
	/// tools and utilities that deal with numerically-based classes.
	/// <p id="bitEquals">This class compares primitive
	/// <c>double</c>
	/// values in methods such as
	/// <see cref="CompareAndSet(double, double)"/>
	/// by comparing their
	/// bitwise representation using
	/// <see cref="double.DoubleToRawLongBits(double)"/>
	/// ,
	/// which differs from both the primitive double
	/// <c>==</c>
	/// operator
	/// and from
	/// <see cref="double.Equals(object)"/>
	/// , as if implemented by:
	/// <pre>
	/// <c>
	/// static boolean bitEquals(double x, double y)
	/// long xBits = Double.doubleToRawLongBits(x);
	/// long yBits = Double.doubleToRawLongBits(y);
	/// return xBits == yBits;
	/// </c>
	/// </pre>
	/// </summary>
	/// <seealso cref="Java.Util.Concurrent.Atomic.DoubleAdder">See jsr166e.DoubleMaxUpdater</seealso>
	/// <author>Doug Lea</author>
	/// <author>Martin Buchholz</author>
	[System.Serializable]
	public class AtomicDouble : Number
	{
		private const long serialVersionUID = -8405198993435143622L;

		private static readonly AtomicLongFieldUpdater<Edu.Stanford.Nlp.Util.Concurrent.AtomicDouble> updater = AtomicLongFieldUpdater.NewUpdater<Edu.Stanford.Nlp.Util.Concurrent.AtomicDouble>("value");

		[System.NonSerialized]
		private volatile long value;

		/// <summary>
		/// Creates a new
		/// <c>AtomicDouble</c>
		/// with the given initial value.
		/// </summary>
		/// <param name="initialValue">the initial value</param>
		public AtomicDouble(double initialValue)
		{
			/* implements java.io.Serializable */
			value = double.DoubleToRawLongBits(initialValue);
		}

		/// <summary>
		/// Creates a new
		/// <c>AtomicDouble</c>
		/// with initial value
		/// <c>0.0</c>
		/// .
		/// </summary>
		public AtomicDouble()
		{
		}

		// assert doubleToRawLongBits(0.0) == 0L;
		/// <summary>Gets the current value.</summary>
		/// <returns>the current value</returns>
		public double Get()
		{
			return double.LongBitsToDouble(value);
		}

		/// <summary>Sets to the given value.</summary>
		/// <param name="newValue">the new value</param>
		public void Set(double newValue)
		{
			long next = double.DoubleToRawLongBits(newValue);
			value = next;
		}

		/// <summary>Eventually sets to the given value.</summary>
		/// <param name="newValue">the new value</param>
		public void LazySet(double newValue)
		{
			Set(newValue);
		}

		/// <summary>Atomically sets to the given value and returns the old value.</summary>
		/// <param name="newValue">the new value</param>
		/// <returns>the previous value</returns>
		public double GetAndSet(double newValue)
		{
			long next = double.DoubleToRawLongBits(newValue);
			return double.LongBitsToDouble(updater.GetAndSet(this, next));
		}

		/// <summary>
		/// Atomically sets the value to the given updated value
		/// if the current value is <a href="#bitEquals">bitwise equal</a>
		/// to the expected value.
		/// </summary>
		/// <param name="expect">the expected value</param>
		/// <param name="update">the new value</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if successful. False return indicates that
		/// the actual value was not bitwise equal to the expected value.
		/// </returns>
		public bool CompareAndSet(double expect, double update)
		{
			return updater.CompareAndSet(this, double.DoubleToRawLongBits(expect), double.DoubleToRawLongBits(update));
		}

		/// <summary>
		/// Atomically sets the value to the given updated value
		/// if the current value is <a href="#bitEquals">bitwise equal</a>
		/// to the expected value.
		/// </summary>
		/// <remarks>
		/// Atomically sets the value to the given updated value
		/// if the current value is <a href="#bitEquals">bitwise equal</a>
		/// to the expected value.
		/// <p>&lt;a
		/// href="http://download.oracle.com/javase/7/docs/api/java/util/concurrent/atomic/package-summary.html#Spurious"&gt;
		/// May fail spuriously and does not provide ordering guarantees</a>,
		/// so is only rarely an appropriate alternative to
		/// <c>compareAndSet</c>
		/// .
		/// </remarks>
		/// <param name="expect">the expected value</param>
		/// <param name="update">the new value</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if successful
		/// </returns>
		public bool WeakCompareAndSet(double expect, double update)
		{
			return CompareAndSet(expect, update);
		}

		/// <summary>Atomically adds the given value to the current value.</summary>
		/// <param name="delta">the value to add</param>
		/// <returns>the previous value</returns>
		public double GetAndAdd(double delta)
		{
			while (true)
			{
				long current = value;
				double currentVal = double.LongBitsToDouble(current);
				double nextVal = currentVal + delta;
				long next = double.DoubleToRawLongBits(nextVal);
				if (updater.CompareAndSet(this, current, next))
				{
					return currentVal;
				}
			}
		}

		/// <summary>Atomically adds the given value to the current value.</summary>
		/// <param name="delta">the value to add</param>
		/// <returns>the updated value</returns>
		public double AddAndGet(double delta)
		{
			while (true)
			{
				long current = value;
				double currentVal = double.LongBitsToDouble(current);
				double nextVal = currentVal + delta;
				long next = double.DoubleToRawLongBits(nextVal);
				if (updater.CompareAndSet(this, current, next))
				{
					return nextVal;
				}
			}
		}

		/// <summary>Returns the String representation of the current value.</summary>
		/// <returns>the String representation of the current value</returns>
		public override string ToString()
		{
			return double.ToString(Get());
		}

		/// <summary>
		/// Returns the value of this
		/// <c>AtomicDouble</c>
		/// as an
		/// <c>int</c>
		/// after a narrowing primitive conversion.
		/// </summary>
		public override int IntValue()
		{
			return (int)Get();
		}

		/// <summary>
		/// Returns the value of this
		/// <c>AtomicDouble</c>
		/// as a
		/// <c>long</c>
		/// after a narrowing primitive conversion.
		/// </summary>
		public override long LongValue()
		{
			return (long)Get();
		}

		/// <summary>
		/// Returns the value of this
		/// <c>AtomicDouble</c>
		/// as a
		/// <c>float</c>
		/// after a narrowing primitive conversion.
		/// </summary>
		public override float FloatValue()
		{
			return (float)Get();
		}

		/// <summary>
		/// Returns the value of this
		/// <c>AtomicDouble</c>
		/// as a
		/// <c>double</c>
		/// .
		/// </summary>
		public override double DoubleValue()
		{
			return Get();
		}

		/// <summary>Saves the state to a stream (that is, serializes it).</summary>
		/// <param name="s">the stream</param>
		/// <exception cref="System.IO.IOException">if an I/O error occurs</exception>
		/// <serialData>
		/// The current value is emitted (a
		/// <c>double</c>
		/// ).
		/// </serialData>
		private void WriteObject(ObjectOutputStream s)
		{
			s.DefaultWriteObject();
			s.WriteDouble(Get());
		}

		/// <summary>Reconstitutes the instance from a stream (that is, deserializes it).</summary>
		/// <param name="s">the stream</param>
		/// <exception cref="System.TypeLoadException">
		/// if the class of a serialized object
		/// could not be found
		/// </exception>
		/// <exception cref="System.IO.IOException">if an I/O error occurs</exception>
		private void ReadObject(ObjectInputStream s)
		{
			s.DefaultReadObject();
			Set(s.ReadDouble());
		}
	}
}
