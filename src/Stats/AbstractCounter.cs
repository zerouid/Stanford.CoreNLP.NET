using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Default implementations of all the convenience methods provided by
	/// <see cref="ICounter{E}"/>
	/// .
	/// </summary>
	/// <author>dramage</author>
	public abstract class AbstractCounter<E> : ICounter<E>
	{
		public virtual double LogIncrementCount(E key, double amount)
		{
			double count = SloppyMath.LogAdd(GetCount(key), amount);
			SetCount(key, count);
			return GetCount(key);
		}

		public virtual double IncrementCount(E key, double amount)
		{
			double count = GetCount(key) + amount;
			SetCount(key, count);
			// get the value just to make sure it agrees with what is in the counter
			// (in case it's a float or int)
			return GetCount(key);
		}

		public virtual double IncrementCount(E key)
		{
			return IncrementCount(key, 1.0);
		}

		public virtual double DecrementCount(E key, double amount)
		{
			return IncrementCount(key, -amount);
		}

		public virtual double DecrementCount(E key)
		{
			return IncrementCount(key, -1.0);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void AddAll(ICounter<E> counter)
		{
			Counters.AddInPlace(this, counter);
		}

		public abstract void PrettyLog(Redwood.RedwoodChannels arg1, string arg2);

		public abstract void Clear();

		public abstract bool ContainsKey(E arg1);

		public abstract double DefaultReturnValue();

		public abstract ICollection<KeyValuePair<E, double>> EntrySet();

		public abstract double GetCount(object arg1);

		public abstract IFactory<ICounter<E>> GetFactory();

		public abstract ICollection<E> KeySet();

		public abstract double Remove(E arg1);

		public abstract void SetCount(E arg1, double arg2);

		public abstract void SetDefaultReturnValue(double arg1);

		public abstract int Size();

		public abstract double TotalCount();

		public abstract ICollection<double> Values();
	}
}
