using System;
using System.Collections.Generic;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Iterator that suppresses items in another iterator based on a filter function.</summary>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public class FilteredIterator<T> : IEnumerator<T>
	{
		internal IEnumerator<T> iterator = null;

		internal IPredicate<T> filter = null;

		internal T current = null;

		internal bool hasCurrent = false;

		internal virtual T CurrentCandidate()
		{
			return current;
		}

		internal virtual void AdvanceCandidate()
		{
			if (!iterator.MoveNext())
			{
				hasCurrent = false;
				current = null;
				return;
			}
			hasCurrent = true;
			current = iterator.Current;
		}

		internal virtual bool HasCurrentCandidate()
		{
			return hasCurrent;
		}

		internal virtual bool CurrentCandidateIsAcceptable()
		{
			return filter.Test(CurrentCandidate());
		}

		internal virtual void SkipUnacceptableCandidates()
		{
			while (HasCurrentCandidate() && !CurrentCandidateIsAcceptable())
			{
				AdvanceCandidate();
			}
		}

		public virtual bool MoveNext()
		{
			return HasCurrentCandidate();
		}

		public virtual T Current
		{
			get
			{
				T result = CurrentCandidate();
				AdvanceCandidate();
				SkipUnacceptableCandidates();
				return result;
			}
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}

		public FilteredIterator(IEnumerator<T> iterator, IPredicate<T> filter)
		{
			this.iterator = iterator;
			this.filter = filter;
			AdvanceCandidate();
			SkipUnacceptableCandidates();
		}

		public static void Main(string[] args)
		{
			ICollection<string> c = Arrays.AsList(new string[] { "a", "aa", "b", "bb", "cc" });
			IEnumerator<string> i = new Edu.Stanford.Nlp.Util.FilteredIterator<string>(c.GetEnumerator(), new _IPredicate_71());
			while (i.MoveNext())
			{
				System.Console.Out.WriteLine("Accepted: " + i.Current);
			}
		}

		private sealed class _IPredicate_71 : IPredicate<string>
		{
			public _IPredicate_71()
			{
				this.serialVersionUID = 1L;
			}

			public bool Test(string o)
			{
				return o.Length == 1;
			}
		}
	}
}
