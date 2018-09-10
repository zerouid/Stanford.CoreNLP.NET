using System;
using System.Collections.Generic;





namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Utilities for helping out with Iterables as Collections is to Collection.</summary>
	/// <remarks>
	/// Utilities for helping out with Iterables as Collections is to Collection.
	/// NB: Some Iterables returned by methods in this class return Iterators that
	/// assume a call to hasNext will precede each call to next.  While this usage
	/// is not up to the Java Iterator spec, it should work fine with
	/// e.g. the Java enhanced for-loop.
	/// Methods in Iterators are merged.
	/// </remarks>
	/// <author>dramage</author>
	/// <author>
	/// dlwh
	/// <see cref="FlatMap{T, U}(System.Collections.Generic.IEnumerable{T}, Java.Util.Function.Func{T, R})"/>
	/// </author>
	/// <author>Huy Nguyen (htnguyen@cs.stanford.edu)</author>
	public class Iterables
	{
		private Iterables()
		{
		}

		// static methods
		/// <summary>Transformed view of the given iterable.</summary>
		/// <remarks>
		/// Transformed view of the given iterable.  Returns the output
		/// of the given function when applied to each element of the
		/// iterable.
		/// </remarks>
		public static IEnumerable<V> Transform<K, V, _T2>(IEnumerable<K> iterable, Func<_T2> function)
		{
			return null;
		}

		/// <summary>Filtered view of the given iterable.</summary>
		/// <remarks>
		/// Filtered view of the given iterable.  Returns only those elements
		/// from the iterable for which the given Function returns true.
		/// </remarks>
		public static IEnumerable<T> Filter<T>(IEnumerable<T> iterable, IPredicate<T> accept)
		{
			return new _IEnumerable_68(iterable, accept);
		}

		private sealed class _IEnumerable_68 : IEnumerable<T>
		{
			public _IEnumerable_68(IEnumerable<T> iterable, IPredicate<T> accept)
			{
				this.iterable = iterable;
				this.accept = accept;
			}

			public IEnumerator<T> GetEnumerator()
			{
				return new _IEnumerator_70(iterable, accept);
			}

			private sealed class _IEnumerator_70 : IEnumerator<T>
			{
				public _IEnumerator_70(IEnumerable<T> iterable, IPredicate<T> accept)
				{
					this.iterable = iterable;
					this.accept = accept;
					this.inner = iterable.GetEnumerator();
					this.queued = false;
					this.next = null;
				}

				internal IEnumerator<T> inner;

				internal bool queued;

				internal T next;

				public bool MoveNext()
				{
					this.Prepare();
					return this.queued;
				}

				public T Current
				{
					get
					{
						this.Prepare();
						if (!this.queued)
						{
							throw new Exception("Filter .next() called with no next");
						}
						T rv = this.next;
						this.next = null;
						this.queued = false;
						return rv;
					}
				}

				public void Prepare()
				{
					if (this.queued)
					{
						return;
					}
					while (this.inner.MoveNext())
					{
						T next = this.inner.Current;
						if (accept.Test(next))
						{
							this.next = next;
							this.queued = true;
							return;
						}
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly IEnumerable<T> iterable;

				private readonly IPredicate<T> accept;
			}

			private readonly IEnumerable<T> iterable;

			private readonly IPredicate<T> accept;
		}

		/// <summary>Casts all values in the given Iterable to the given type.</summary>
		public static IEnumerable<T> Cast<T, _T1>(IEnumerable<_T1> iterable, Type type)
		{
			return null;
		}

		/// <summary>Returns a shortened view of an iterator.</summary>
		/// <remarks>Returns a shortened view of an iterator.  Returns at most <code>max</code> elements.</remarks>
		public static IEnumerable<T> Take<T>(T[] array, int max)
		{
			return Take(Arrays.AsList(array), max);
		}

		/// <summary>Returns a shortened view of an iterator.</summary>
		/// <remarks>Returns a shortened view of an iterator.  Returns at most <code>max</code> elements.</remarks>
		public static IEnumerable<T> Take<T>(IEnumerable<T> iterable, int max)
		{
			return new _IEnumerable_151(this, iterable, max);
		}

		private sealed class _IEnumerable_151 : IEnumerable<T>
		{
			public _IEnumerable_151(Iterables _enclosing, IEnumerable<T> iterable, int max)
			{
				this._enclosing = _enclosing;
				this.iterable = iterable;
				this.max = max;
				this.iterator = iterable.GetEnumerator();
			}

			internal readonly IEnumerator<T> iterator;

			// @Override
			public IEnumerator<T> GetEnumerator()
			{
				return new _IEnumerator_156(this, max);
			}

			private sealed class _IEnumerator_156 : IEnumerator<T>
			{
				public _IEnumerator_156(_IEnumerable_151 _enclosing, int max)
				{
					this._enclosing = _enclosing;
					this.max = max;
					this.i = 0;
				}

				internal int i;

				// @Override
				public bool MoveNext()
				{
					return this.i < max && this._enclosing.iterator.MoveNext();
				}

				public T Current
				{
					get
					{
						// @Override
						this.i++;
						return this._enclosing.iterator.Current;
					}
				}

				// @Override
				public void Remove()
				{
					this._enclosing.iterator.Remove();
				}

				private readonly _IEnumerable_151 _enclosing;

				private readonly int max;
			}

			private readonly Iterables _enclosing;

			private readonly IEnumerable<T> iterable;

			private readonly int max;
		}

		/// <summary>Returns a view of the given data, ignoring the first toDrop elements.</summary>
		public static IEnumerable<T> Drop<T>(T[] array, int toDrop)
		{
			return Drop(Arrays.AsList(array), toDrop);
		}

		/// <summary>Returns a view of the given data, ignoring the first toDrop elements.</summary>
		public static IEnumerable<T> Drop<T>(IEnumerable<T> iterable, int toDrop)
		{
			return new _IEnumerable_192(this, iterable, toDrop);
		}

		private sealed class _IEnumerable_192 : IEnumerable<T>
		{
			public _IEnumerable_192(Iterables _enclosing, IEnumerable<T> iterable, int toDrop)
			{
				this._enclosing = _enclosing;
				this.iterable = iterable;
				this.toDrop = toDrop;
				this.iterator = iterable.GetEnumerator();
			}

			internal readonly IEnumerator<T> iterator;

			// @Override
			public IEnumerator<T> GetEnumerator()
			{
				return new _IEnumerator_197(this, toDrop);
			}

			private sealed class _IEnumerator_197 : IEnumerator<T>
			{
				public _IEnumerator_197(_IEnumerable_192 _enclosing, int toDrop)
				{
					this._enclosing = _enclosing;
					this.toDrop = toDrop;
					this.skipped = 0;
				}

				internal int skipped;

				// @Override
				public bool MoveNext()
				{
					while (this.skipped < toDrop && this._enclosing.iterator.MoveNext())
					{
						this._enclosing.iterator.Current;
						this.skipped += 1;
					}
					return this._enclosing.iterator.MoveNext();
				}

				public T Current
				{
					get
					{
						// @Override
						while (this.skipped < toDrop && this._enclosing.iterator.MoveNext())
						{
							this._enclosing.iterator.Current;
							this.skipped += 1;
						}
						return this._enclosing.iterator.Current;
					}
				}

				// @Override
				public void Remove()
				{
					this._enclosing.iterator.Remove();
				}

				private readonly _IEnumerable_192 _enclosing;

				private readonly int toDrop;
			}

			private readonly Iterables _enclosing;

			private readonly IEnumerable<T> iterable;

			private readonly int toDrop;
		}

		/// <summary>Chains together an Iterable of Iterables after transforming each one.</summary>
		/// <remarks>
		/// Chains together an Iterable of Iterables after transforming each one.
		/// Equivalent to Iterables.transform(Iterables.chain(iterables),trans);
		/// </remarks>
		public static IEnumerable<U> FlatMap<T, U, _T2, _T3>(IEnumerable<_T2> iterables, Func<_T3> trans)
			where _T2 : IEnumerable<T>
		{
			return Transform(Chain(iterables), trans);
		}

		/// <summary>Chains together a set of Iterables of compatible types.</summary>
		/// <remarks>
		/// Chains together a set of Iterables of compatible types.  Returns all
		/// elements of the first iterable, then all of the second, then the third,
		/// etc.
		/// </remarks>
		public static IEnumerable<T> Chain<T, _T1>(IEnumerable<_T1> iterables)
			where _T1 : IEnumerable<T>
		{
			return new _IEnumerable_241(iterables);
		}

		private sealed class _IEnumerable_241 : IEnumerable<T>
		{
			public _IEnumerable_241(IEnumerable<IEnumerable<T>> iterables)
			{
				this.iterables = iterables;
			}

			public IEnumerator<T> GetEnumerator()
			{
				IEnumerator<IEnumerable<T>> iterators = iterables.GetEnumerator();
				return new _IEnumerator_245(iterators);
			}

			private sealed class _IEnumerator_245 : IEnumerator<T>
			{
				public _IEnumerator_245(IEnumerator<IEnumerable<T>> iterators)
				{
					this.iterators = iterators;
					this.current = null;
				}

				private IEnumerator<T> current;

				public bool MoveNext()
				{
					// advance current iterator if necessary, return false at end
					while (this.current == null || !this.current.MoveNext())
					{
						if (iterators.MoveNext())
						{
							this.current = iterators.Current.GetEnumerator();
						}
						else
						{
							return false;
						}
					}
					return true;
				}

				public T Current
				{
					get
					{
						return this.current.Current;
					}
				}

				public void Remove()
				{
					this.current.Remove();
				}

				private readonly IEnumerator<IEnumerable<T>> iterators;
			}

			private readonly IEnumerable<IEnumerable<T>> iterables;
		}

		/// <summary>
		/// Chains together all Iterables of type T as given in an array or
		/// varargs parameter.
		/// </summary>
		public static IEnumerable<T> Chain<T>(params IEnumerable<T>[] iterables)
		{
			return Chain(Arrays.AsList(iterables));
		}

		/// <summary>
		/// Chains together all arrays of type T[] as given in an array or
		/// varargs parameter.
		/// </summary>
		public static IEnumerable<T> Chain<T>(params T[][] arrays)
		{
			LinkedList<IEnumerable<T>> iterables = new LinkedList<IEnumerable<T>>();
			foreach (T[] array in arrays)
			{
				iterables.Add(Arrays.AsList(array));
			}
			return Chain(iterables);
		}

		/// <summary>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.
		/// </summary>
		/// <remarks>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.  Ends when the shorter
		/// iterable ends.
		/// </remarks>
		public static IEnumerable<Pair<T1, T2>> Zip<T1, T2>(IEnumerable<T1> iter1, IEnumerable<T2> iter2)
		{
			return null;
		}

		/// <summary>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.
		/// </summary>
		/// <remarks>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.  Ends when the shorter
		/// iterable ends.
		/// </remarks>
		public static IEnumerable<Pair<T1, T2>> Zip<T1, T2>(IEnumerable<T1> iter, T2[] array)
		{
			return Zip(iter, Arrays.AsList(array));
		}

		/// <summary>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.
		/// </summary>
		/// <remarks>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.  Ends when the shorter
		/// iterable ends.
		/// </remarks>
		public static IEnumerable<Pair<T1, T2>> Zip<T1, T2>(T1[] array, IEnumerable<T2> iter)
		{
			return Zip(Arrays.AsList(array), iter);
		}

		/// <summary>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.
		/// </summary>
		/// <remarks>
		/// Zips two iterables into one iterable over Pairs of corresponding
		/// elements in the two underlying iterables.  Ends when the shorter
		/// iterable ends.
		/// </remarks>
		public static IEnumerable<Pair<T1, T2>> Zip<T1, T2>(T1[] array1, T2[] array2)
		{
			return Zip(Arrays.AsList(array1), Arrays.AsList(array2));
		}

		/// <summary>
		/// Zips up two iterators into one iterator over Pairs of corresponding
		/// elements.
		/// </summary>
		/// <remarks>
		/// Zips up two iterators into one iterator over Pairs of corresponding
		/// elements.  Ends when the shorter iterator ends.
		/// </remarks>
		public static IEnumerator<Pair<T1, T2>> Zip<T1, T2>(IEnumerator<T1> iter1, IEnumerator<T2> iter2)
		{
			return new _IEnumerator_343(iter1, iter2);
		}

		private sealed class _IEnumerator_343 : IEnumerator<Pair<T1, T2>>
		{
			public _IEnumerator_343(IEnumerator<T1> iter1, IEnumerator<T2> iter2)
			{
				this.iter1 = iter1;
				this.iter2 = iter2;
			}

			public bool MoveNext()
			{
				return iter1.MoveNext() && iter2.MoveNext();
			}

			public Pair<T1, T2> Current
			{
				get
				{
					return new Pair<T1, T2>(iter1.Current, iter2.Current);
				}
			}

			public void Remove()
			{
				iter1.Remove();
				iter2.Remove();
			}

			private readonly IEnumerator<T1> iter1;

			private readonly IEnumerator<T2> iter2;
		}

		/// <summary>
		/// A comparator used by the merge functions to determine which of two
		/// iterators to increment by one of the merge functions.
		/// </summary>
		/// <?/>
		/// <?/>
		public interface IIncrementComparator<V1, V2>
		{
			/// <summary>
			/// Returns -1 if the value of a should come before the value of b,
			/// +1 if the value of b should come before the value of a, or 0 if
			/// the two should be merged together.
			/// </summary>
			int Compare(V1 a, V2 b);
		}

		/// <summary>
		/// Iterates over pairs of objects from two (sorted) iterators such that
		/// each pair a \in iter1, b \in iter2 returned has comparator.compare(a,b)==0.
		/// </summary>
		/// <remarks>
		/// Iterates over pairs of objects from two (sorted) iterators such that
		/// each pair a \in iter1, b \in iter2 returned has comparator.compare(a,b)==0.
		/// If the comparator says that a and b are not equal, we increment the
		/// iterator of the smaller value.  If the comparator says that a and b are
		/// equal, we return that pair and increment both iterators.
		/// This is used, e.g. to return lines from two input files that have
		/// the same "key" as determined by the given comparator.
		/// The comparator will always be passed elements from the first iter as
		/// the first argument.
		/// </remarks>
		public static IEnumerable<Pair<V1, V2>> Merge<V1, V2>(IEnumerable<V1> iter1, IEnumerable<V2> iter2, Iterables.IIncrementComparator<V1, V2> comparator)
		{
			return new _IEnumerable_392(this, iter1, iter2, comparator);
		}

		private sealed class _IEnumerable_392 : IEnumerable<Pair<V1, V2>>
		{
			public _IEnumerable_392(Iterables _enclosing, IEnumerable<V1> iter1, IEnumerable<V2> iter2, Iterables.IIncrementComparator<V1, V2> comparator)
			{
				this._enclosing = _enclosing;
				this.iter1 = iter1;
				this.iter2 = iter2;
				this.comparator = comparator;
				this.iterA = iter1.GetEnumerator();
				this.iterB = iter2.GetEnumerator();
			}

			internal IEnumerator<V1> iterA;

			internal IEnumerator<V2> iterB;

			public IEnumerator<Pair<V1, V2>> GetEnumerator()
			{
				return new _IEnumerator_397(this, comparator);
			}

			private sealed class _IEnumerator_397 : IEnumerator<Pair<V1, V2>>
			{
				public _IEnumerator_397(_IEnumerable_392 _enclosing, Iterables.IIncrementComparator<V1, V2> comparator)
				{
					this._enclosing = _enclosing;
					this.comparator = comparator;
					this.ready = false;
					this.pending = null;
				}

				internal bool ready;

				internal Pair<V1, V2> pending;

				public bool MoveNext()
				{
					if (!this.ready)
					{
						this.pending = this.NextPair();
						this.ready = true;
					}
					return this.pending != null;
				}

				public Pair<V1, V2> Current
				{
					get
					{
						if (!this.ready && !this.MoveNext())
						{
							throw new IllegalAccessError("Called next without hasNext");
						}
						this.ready = false;
						return this.pending;
					}
				}

				public void Remove()
				{
					throw new NotSupportedException("Cannot remove pairs " + "from a merged iterator");
				}

				private Pair<V1, V2> NextPair()
				{
					V1 nextA = null;
					V2 nextB = null;
					while (this._enclosing.iterA.MoveNext() && this._enclosing.iterB.MoveNext())
					{
						// increment iterators are null
						if (nextA == null)
						{
							nextA = this._enclosing.iterA.Current;
						}
						if (nextB == null)
						{
							nextB = this._enclosing.iterB.Current;
						}
						int cmp = comparator.Compare(nextA, nextB);
						if (cmp < 0)
						{
							// iterA too small, increment it next time around
							nextA = null;
						}
						else
						{
							if (cmp > 0)
							{
								// iterB too small, increment it next time around
								nextB = null;
							}
							else
							{
								// just right - return this pair
								return new Pair<V1, V2>(nextA, nextB);
							}
						}
					}
					return null;
				}

				private readonly _IEnumerable_392 _enclosing;

				private readonly Iterables.IIncrementComparator<V1, V2> comparator;
			}

			private readonly Iterables _enclosing;

			private readonly IEnumerable<V1> iter1;

			private readonly IEnumerable<V2> iter2;

			private readonly Iterables.IIncrementComparator<V1, V2> comparator;
		}

		/// <summary>
		/// Same as
		/// <see cref="Merge{V1, V2}(System.Collections.Generic.IEnumerable{T}, System.Collections.Generic.IEnumerable{T}, IIncrementComparator{V1, V2})"/>
		/// but using
		/// the given (symmetric) comparator.
		/// </summary>
		public static IEnumerable<Pair<V, V>> Merge<V>(IEnumerable<V> iter1, IEnumerable<V> iter2, IComparator<V> comparator)
		{
			Iterables.IIncrementComparator<V, V> inc = null;
			return Merge(iter1, iter2, inc);
		}

		/// <summary>
		/// Iterates over triples of objects from three (sorted) iterators such that
		/// for every returned triple a (from iter1), b (from iter2), c (from iter3)
		/// satisfies the constraint that <code>comparator.compare(a,b) ==
		/// comparator.compare(a,c) == 0</code>.
		/// </summary>
		/// <remarks>
		/// Iterates over triples of objects from three (sorted) iterators such that
		/// for every returned triple a (from iter1), b (from iter2), c (from iter3)
		/// satisfies the constraint that <code>comparator.compare(a,b) ==
		/// comparator.compare(a,c) == 0</code>.  Internally, this function first
		/// calls merge(iter1,iter2,comparatorA), and then merges that iterator
		/// with the iter3 by comparing based on the value returned by iter1.
		/// This is used, e.g. to return lines from three input files that have
		/// the same "key" as determined by the given comparator.
		/// </remarks>
		public static IEnumerable<Triple<V1, V2, V3>> Merge<V1, V2, V3>(IEnumerable<V1> iter1, IEnumerable<V2> iter2, IEnumerable<V3> iter3, Iterables.IIncrementComparator<V1, V2> comparatorA, Iterables.IIncrementComparator<V1, V3> comparatorB)
		{
			// partial merge on first two iterables
			IEnumerable<Pair<V1, V2>> partial = Merge(iter1, iter2, comparatorA);
			Iterables.IIncrementComparator<Pair<V1, V2>, V3> inc = new _IIncrementComparator_485(comparatorB);
			// flattens the pairs into triple
			Func<Pair<Pair<V1, V2>, V3>, Triple<V1, V2, V3>> flatten = null;
			return Transform(Merge(partial, iter3, inc), flatten);
		}

		private sealed class _IIncrementComparator_485 : Iterables.IIncrementComparator<Pair<V1, V2>, V3>
		{
			public _IIncrementComparator_485(Iterables.IIncrementComparator<V1, V3> comparatorB)
			{
				this.comparatorB = comparatorB;
			}

			public int Compare(Pair<V1, V2> a, V3 b)
			{
				return comparatorB.Compare(a.first, b);
			}

			private readonly Iterables.IIncrementComparator<V1, V3> comparatorB;
		}

		/// <summary>
		/// Same as
		/// <see cref="Merge{V1, V2, V3}(System.Collections.Generic.IEnumerable{T}, System.Collections.Generic.IEnumerable{T}, System.Collections.Generic.IEnumerable{T}, IIncrementComparator{V1, V2}, IIncrementComparator{V1, V2})"/>
		/// but using the given (symmetric) comparator.
		/// </summary>
		public static IEnumerable<Triple<V, V, V>> Merge<V>(IEnumerable<V> iter1, IEnumerable<V> iter2, IEnumerable<V> iter3, IComparator<V> comparator)
		{
			Iterables.IIncrementComparator<V, V> inc = null;
			return Merge(iter1, iter2, iter3, inc, inc);
		}

		/// <summary>
		/// Groups consecutive elements from the given iterable based on the value
		/// in the given comparator.
		/// </summary>
		/// <remarks>
		/// Groups consecutive elements from the given iterable based on the value
		/// in the given comparator.  Each inner iterable will iterate over consecutive
		/// items from the input until the comparator says that the next item is not
		/// equal to the previous.
		/// </remarks>
		public static IEnumerable<IEnumerable<V>> Group<V>(IEnumerable<V> iterable, IComparator<V> comparator)
		{
			return new _IEnumerable_520(iterable);
		}

		private sealed class _IEnumerable_520 : IEnumerable<IEnumerable<V>>
		{
			public _IEnumerable_520(IEnumerable<V> iterable)
			{
				this.iterable = iterable;
			}

			public IEnumerator<IEnumerable<V>> GetEnumerator()
			{
				return new _IEnumerator_522(iterable);
			}

			private sealed class _IEnumerator_522 : IEnumerator<IEnumerable<V>>
			{
				public _IEnumerator_522(IEnumerable<V> iterable)
				{
					this.iterable = iterable;
					this.it = iterable.GetEnumerator();
				}

				/// <summary>Actual iterator</summary>
				internal IEnumerator<V> it;

				/// <summary>Next element to return</summary>
				internal V next;

				public bool MoveNext()
				{
					return this.next != null || this.it.MoveNext();
				}

				public IEnumerable<V> Current
				{
					get
					{
						return null;
					}
				}

				// get next if we need to and one is available
				// if next and last both have values, compare them
				// one of them was not null - have more if it was next
				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly IEnumerable<V> iterable;
			}

			private readonly IEnumerable<V> iterable;
		}

		/// <summary>
		/// Returns a string representation of the contents of calling toString
		/// on each element of the given iterable, joining the elements together
		/// with the given glue.
		/// </summary>
		public static string ToString<E>(IEnumerable<E> iter, string glue)
		{
			StringBuilder builder = new StringBuilder();
			for (IEnumerator<E> it = iter.GetEnumerator(); it.MoveNext(); )
			{
				builder.Append(it.Current);
				if (it.MoveNext())
				{
					builder.Append(glue);
				}
			}
			return builder.ToString();
		}

		/// <summary>Sample k items uniformly from an Iterable of size n (without replacement).</summary>
		/// <param name="items">The items from which to sample.</param>
		/// <param name="n">The total number of items in the Iterable.</param>
		/// <param name="k">The number of items to sample.</param>
		/// <param name="random">The random number generator.</param>
		/// <returns>An Iterable of k items, chosen randomly from the original n items.</returns>
		public static IEnumerable<T> Sample<T>(IEnumerable<T> items, int n, int k, Random random)
		{
			// assemble a list of all indexes
			IList<int> indexes = new List<int>();
			for (int i = 0; i < n; ++i)
			{
				indexes.Add(i);
			}
			// shuffle the indexes and select the first k
			Java.Util.Collections.Shuffle(indexes, random);
			ICollection<int> indexSet = Generics.NewHashSet(indexes.SubList(0, k));
			// filter down to only the items at the selected indexes
			return Iterables.Filter(items, new _IPredicate_614(indexSet));
		}

		private sealed class _IPredicate_614 : IPredicate<T>
		{
			public _IPredicate_614(ICollection<int> indexSet)
			{
				this.indexSet = indexSet;
				this.index = -1;
			}

			private int index;

			public bool Test(T item)
			{
				++this.index;
				return indexSet.Contains(this.index);
			}

			private readonly ICollection<int> indexSet;
		}

		//  /**
		//   * Returns a dummy collection wrapper for the Iterable that iterates
		//   * it once to get the size if requested.  If the underlying iterable
		//   * cannot be iterated more than once, you're out of luck.
		//   */
		//  public static <E> Collection<E> toCollection(final Iterable<E> iter) {
		//    return new AbstractCollection<E>() {
		//      int size = -1;
		//
		//      @Override
		//      public Iterator<E> iterator() {
		//        return iter.iterator();
		//      }
		//
		//      @Override
		//      public int size() {
		//        if (size < 0) {
		//          size = 0;
		//          for (E elem : iter) { size++; }
		//        }
		//        return size;
		//      }
		//    };
		//  }
		//
		//  public static <E,L extends List<E>> L toList(Iterable<E> iter, Class<L> type) {
		//    try {
		//      type.newInstance();
		//    } catch (InstantiationException e) {
		//      e.printStackTrace();
		//    } catch (IllegalAccessException e) {
		//      e.printStackTrace();
		//    }
		//  }
		/// <summary>Creates an ArrayList containing all of the Objects returned by the given Iterator.</summary>
		public static List<T> AsArrayList<T, _T1>(IEnumerator<_T1> iter)
			where _T1 : T
		{
			List<T> al = new List<T>();
			return (List<T>)AddAll(iter, al);
		}

		/// <summary>Creates a HashSet containing all of the Objects returned by the given Iterator.</summary>
		public static HashSet<T> AsHashSet<T, _T1>(IEnumerator<_T1> iter)
			where _T1 : T
		{
			HashSet<T> hs = new HashSet<T>();
			return (HashSet<T>)AddAll(iter, hs);
		}

		/// <summary>
		/// Creates a new Collection from the given CollectionFactory, and adds all of the Objects
		/// returned by the given Iterator.
		/// </summary>
		public static ICollection<E> AsCollection<E, _T1>(IEnumerator<_T1> iter, CollectionFactory<E> cf)
			where _T1 : E
		{
			ICollection<E> c = cf.NewCollection();
			return AddAll(iter, c);
		}

		/// <summary>Adds all of the Objects returned by the given Iterator into the given Collection.</summary>
		/// <returns>the given Collection</returns>
		public static ICollection<T> AddAll<T, _T1>(IEnumerator<_T1> iter, ICollection<T> c)
			where _T1 : T
		{
			while (iter.MoveNext())
			{
				c.Add(iter.Current);
			}
			return c;
		}

		/// <summary>For internal debugging purposes only.</summary>
		public static void Main(string[] args)
		{
			string[] test = new string[] { "a", "b", "c" };
			IList<string> l = Arrays.AsList(test);
			System.Console.Out.WriteLine(AsArrayList(l.GetEnumerator()));
			System.Console.Out.WriteLine(AsHashSet(l.GetEnumerator()));
			System.Console.Out.WriteLine(AsCollection(l.GetEnumerator(), CollectionFactory.HashSetFactory<string>()));
			List<string> al = new List<string>();
			al.Add("d");
			System.Console.Out.WriteLine(AddAll(l.GetEnumerator(), al));
		}
	}
}
