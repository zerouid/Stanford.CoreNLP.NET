using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Interval with value</summary>
	/// <author>Angel Chang</author>
	public class ValuedInterval<T, E> : IHasInterval<E>
		where E : IComparable<E>
	{
		internal T value;

		internal Interval<E> interval;

		public ValuedInterval(T value, Interval<E> interval)
		{
			this.value = value;
			this.interval = interval;
		}

		public virtual T GetValue()
		{
			return value;
		}

		public virtual Interval<E> GetInterval()
		{
			return interval;
		}
	}
}
