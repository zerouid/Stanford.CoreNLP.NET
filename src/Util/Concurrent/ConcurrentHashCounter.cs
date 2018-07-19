using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>
	/// A threadsafe counter implemented as a lightweight wrapper around a
	/// ConcurrentHashMap.
	/// </summary>
	/// <author>Spence Green</author>
	/// <?/>
	[System.Serializable]
	public class ConcurrentHashCounter<E> : ICounter<E>, IEnumerable<E>
	{
		private const long serialVersionUID = -8077192206562696111L;

		private const int DefaultCapacity = 100;

		private readonly IConcurrentMap<E, AtomicDouble> map;

		private readonly AtomicDouble totalCount;

		private double defaultReturnValue = 0.0;

		public ConcurrentHashCounter()
			: this(DefaultCapacity)
		{
		}

		public ConcurrentHashCounter(int initialCapacity)
		{
			map = new ConcurrentHashMap<E, AtomicDouble>(initialCapacity);
			totalCount = new AtomicDouble();
		}

		public virtual IEnumerator<E> GetEnumerator()
		{
			return KeySet().GetEnumerator();
		}

		public virtual IFactory<ICounter<E>> GetFactory()
		{
			return new _IFactory_55();
		}

		private sealed class _IFactory_55 : IFactory<ICounter<E>>
		{
			public _IFactory_55()
			{
				this.serialVersionUID = 6076144467752914760L;
			}

			private const long serialVersionUID;

			public ICounter<E> Create()
			{
				return new Edu.Stanford.Nlp.Util.Concurrent.ConcurrentHashCounter<E>();
			}
		}

		public virtual void SetDefaultReturnValue(double value)
		{
			defaultReturnValue = value;
		}

		public virtual double DefaultReturnValue()
		{
			return defaultReturnValue;
		}

		public virtual double GetCount(object key)
		{
			AtomicDouble v = map[key];
			return v == null ? defaultReturnValue : v.Get();
		}

		public virtual void SetCount(E key, double value)
		{
			// TODO Inspired by Guava.AtomicLongMap
			// Modify for our use?
			for (; ; )
			{
				AtomicDouble atomic = map[key];
				if (atomic == null)
				{
					atomic = map.PutIfAbsent(key, new AtomicDouble(value));
					if (atomic == null)
					{
						totalCount.AddAndGet(value);
						return;
					}
				}
				for (; ; )
				{
					double oldValue = atomic.Get();
					if (oldValue == 0.0)
					{
						// don't compareAndSet a zero
						if (map.Replace(key, atomic, new AtomicDouble(value)))
						{
							totalCount.AddAndGet(value);
							return;
						}
						goto outer_continue;
					}
					if (atomic.CompareAndSet(oldValue, value))
					{
						totalCount.AddAndGet(value - oldValue);
						return;
					}
				}
outer_continue: ;
			}
outer_break: ;
		}

		public virtual double IncrementCount(E key, double value)
		{
			// TODO Inspired by Guava.AtomicLongMap
			// Modify for our use?
			for (; ; )
			{
				AtomicDouble atomic = map[key];
				if (atomic == null)
				{
					atomic = map.PutIfAbsent(key, new AtomicDouble(value));
					if (atomic == null)
					{
						totalCount.AddAndGet(value);
						return value;
					}
				}
				for (; ; )
				{
					double oldValue = atomic.Get();
					if (oldValue == 0.0)
					{
						// don't compareAndSet a zero
						if (map.Replace(key, atomic, new AtomicDouble(value)))
						{
							totalCount.AddAndGet(value);
							return value;
						}
						goto outer_continue;
					}
					double newValue = oldValue + value;
					if (atomic.CompareAndSet(oldValue, newValue))
					{
						totalCount.AddAndGet(value);
						return newValue;
					}
				}
outer_continue: ;
			}
outer_break: ;
		}

		public virtual double IncrementCount(E key)
		{
			return IncrementCount(key, 1.0);
		}

		public virtual double DecrementCount(E key, double value)
		{
			return IncrementCount(key, -value);
		}

		public virtual double DecrementCount(E key)
		{
			return IncrementCount(key, -1.0);
		}

		public virtual double LogIncrementCount(E key, double value)
		{
			// TODO Inspired by Guava.AtomicLongMap
			// Modify for our use?
			for (; ; )
			{
				AtomicDouble atomic = map[key];
				if (atomic == null)
				{
					atomic = map.PutIfAbsent(key, new AtomicDouble(value));
					if (atomic == null)
					{
						totalCount.AddAndGet(value);
						return value;
					}
				}
				for (; ; )
				{
					double oldValue = atomic.Get();
					if (oldValue == 0.0)
					{
						// don't compareAndSet a zero
						if (map.Replace(key, atomic, new AtomicDouble(value)))
						{
							totalCount.AddAndGet(value);
							return value;
						}
						goto outer_continue;
					}
					double newValue = SloppyMath.LogAdd(oldValue, value);
					if (atomic.CompareAndSet(oldValue, newValue))
					{
						totalCount.AddAndGet(value);
						return newValue;
					}
				}
outer_continue: ;
			}
outer_break: ;
		}

		public virtual void AddAll(ICounter<E> counter)
		{
			Counters.AddInPlace(this, counter);
		}

		public virtual double Remove(E key)
		{
			AtomicDouble atomic = map[key];
			if (atomic == null)
			{
				return defaultReturnValue;
			}
			for (; ; )
			{
				double oldValue = atomic.Get();
				if (oldValue == 0.0 || atomic.CompareAndSet(oldValue, 0.0))
				{
					// only remove after setting to zero, to avoid concurrent updates
					Sharpen.Collections.Remove(map, key, atomic);
					// succeed even if the remove fails, since the value was already adjusted
					totalCount.AddAndGet(-1.0 * oldValue);
					return oldValue;
				}
			}
		}

		public virtual bool ContainsKey(E key)
		{
			return map.Contains(key);
		}

		public virtual ICollection<E> KeySet()
		{
			return Java.Util.Collections.UnmodifiableSet(map.Keys);
		}

		public virtual ICollection<double> Values()
		{
			return new _ICollection_232(this);
		}

		private sealed class _ICollection_232 : ICollection<double>
		{
			public _ICollection_232(ConcurrentHashCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			public bool IsEmpty()
			{
				return this._enclosing.map.Count == 0;
			}

			public bool Contains(object o)
			{
				if (o is double)
				{
					double value = (double)o;
					foreach (AtomicDouble atomic in this._enclosing.map.Values)
					{
						if (atomic.Get() == value)
						{
							return true;
						}
					}
				}
				return false;
			}

			public IEnumerator<double> GetEnumerator()
			{
				return new _IEnumerator_255(this);
			}

			private sealed class _IEnumerator_255 : IEnumerator<double>
			{
				public _IEnumerator_255()
				{
					this.iterator = this._enclosing._enclosing.map.Values.GetEnumerator();
				}

				internal IEnumerator<AtomicDouble> iterator;

				public bool MoveNext()
				{
					return this.iterator.MoveNext();
				}

				public double Current
				{
					get
					{
						return this.iterator.Current.Get();
					}
				}

				public void Remove()
				{
					this.iterator.Remove();
				}
			}

			public object[] ToArray()
			{
				return Sharpen.Collections.ToArray(this._enclosing.map.Values);
			}

			public T[] ToArray<T>(T[] a)
			{
				return Sharpen.Collections.ToArray(this._enclosing.map.Values, a);
			}

			public bool Add(double e)
			{
				throw new NotSupportedException();
			}

			public bool Remove(object o)
			{
				throw new NotSupportedException();
			}

			public bool ContainsAll<_T0>(ICollection<_T0> c)
			{
				throw new NotSupportedException();
			}

			public bool AddAll<_T0>(ICollection<_T0> c)
				where _T0 : double
			{
				throw new NotSupportedException();
			}

			public bool RemoveAll<_T0>(ICollection<_T0> c)
			{
				throw new NotSupportedException();
			}

			public bool RetainAll<_T0>(ICollection<_T0> c)
			{
				throw new NotSupportedException();
			}

			public void Clear()
			{
				throw new NotSupportedException();
			}

			private readonly ConcurrentHashCounter<E> _enclosing;
		}

		public virtual ICollection<KeyValuePair<E, double>> EntrySet()
		{
			return new _AbstractSet_312(this);
		}

		private sealed class _AbstractSet_312 : AbstractSet<KeyValuePair<E, double>>
		{
			public _AbstractSet_312(ConcurrentHashCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<KeyValuePair<E, double>> GetEnumerator()
			{
				return new _IEnumerator_315(this);
			}

			private sealed class _IEnumerator_315 : IEnumerator<KeyValuePair<E, double>>
			{
				public _IEnumerator_315(_AbstractSet_312 _enclosing)
				{
					this._enclosing = _enclosing;
					this.inner = this._enclosing._enclosing.map.GetEnumerator();
				}

				internal readonly IEnumerator<KeyValuePair<E, AtomicDouble>> inner;

				public bool MoveNext()
				{
					return this.inner.MoveNext();
				}

				public KeyValuePair<E, double> Current
				{
					get
					{
						return new _KeyValuePair_325(this);
					}
				}

				private sealed class _KeyValuePair_325 : KeyValuePair<E, double>
				{
					public _KeyValuePair_325(_IEnumerator_315 _enclosing)
					{
						this._enclosing = _enclosing;
						this.e = this._enclosing.inner.Current;
					}

					internal readonly KeyValuePair<E, AtomicDouble> e;

					public E Key
					{
						get
						{
							return this.e.Key;
						}
					}

					public double Value
					{
						get
						{
							return this.e.Value.Get();
						}
					}

					public double SetValue(double value)
					{
						double old = this.e.Value.Get();
						this._enclosing._enclosing._enclosing.SetCount(this.e.Key, value);
						this.e.Value.Set(value);
						return old;
					}

					private readonly _IEnumerator_315 _enclosing;
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly _AbstractSet_312 _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			private readonly ConcurrentHashCounter<E> _enclosing;
		}

		public virtual void Clear()
		{
			for (; ; )
			{
				totalCount.Set(0.0);
				if (totalCount.Get() == 0.0)
				{
					map.Clear();
					return;
				}
			}
		}

		public virtual int Size()
		{
			return map.Count;
		}

		public virtual double TotalCount()
		{
			return totalCount.Get();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (!(o is Edu.Stanford.Nlp.Util.Concurrent.ConcurrentHashCounter))
				{
					return false;
				}
				else
				{
					Edu.Stanford.Nlp.Util.Concurrent.ConcurrentHashCounter<E> other = (Edu.Stanford.Nlp.Util.Concurrent.ConcurrentHashCounter<E>)o;
					return totalCount.Get() == other.totalCount.Get() && map.Equals(other.map);
				}
			}
		}

		/// <summary>Returns a hashCode which is the underlying Map's hashCode.</summary>
		/// <returns>A hashCode.</returns>
		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		/// <summary>
		/// Returns a String representation of the Counter, as formatted by
		/// the underlying Map.
		/// </summary>
		/// <returns>A String representation of the Counter.</returns>
		public override string ToString()
		{
			return map.ToString();
		}

		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, map);
		}
	}
}
