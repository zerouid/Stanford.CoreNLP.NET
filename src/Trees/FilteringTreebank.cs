using System;
using System.Collections.Generic;
using Java.IO;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This class wraps another Treebank, and will vend trees that passed
	/// a Filter<Tree>.
	/// </summary>
	/// <author>John Bauer</author>
	public class FilteringTreebank : Treebank
	{
		private IPredicate<Tree> filter;

		private Treebank treebank;

		private const bool Verbose = false;

		public FilteringTreebank(Treebank treebank, IPredicate<Tree> filter)
		{
			this.filter = filter;
			this.treebank = treebank;
		}

		/// <summary>Empty a <code>Treebank</code>.</summary>
		public override void Clear()
		{
			treebank.Clear();
			filter = null;
		}

		/// <summary>Load trees from given path specification.</summary>
		/// <remarks>
		/// Load trees from given path specification.  Passes the path and
		/// filter to the underlying treebank.
		/// </remarks>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filt">a FilenameFilter of files to load</param>
		public override void LoadPath(File path, IFileFilter filt)
		{
			treebank.LoadPath(path, filt);
		}

		/// <summary>
		/// Applies the TreeVisitor, but only to the trees that pass the
		/// filter.
		/// </summary>
		/// <remarks>
		/// Applies the TreeVisitor, but only to the trees that pass the
		/// filter.  Applies the visitor to a copy of the tree.
		/// </remarks>
		/// <param name="tv">A class that can process trees.</param>
		public override void Apply(ITreeVisitor tv)
		{
			foreach (Tree t in treebank)
			{
				if (!filter.Test(t))
				{
					continue;
				}
				Tree tmpT = t.DeepCopy();
				tv.VisitTree(tmpT);
			}
		}

		public override IEnumerator<Tree> GetEnumerator()
		{
			return new FilteringTreebank.FilteringTreebankIterator(treebank.GetEnumerator(), filter);
		}

		private class FilteringTreebankIterator : IEnumerator<Tree>
		{
			private IEnumerator<Tree> iter;

			private IPredicate<Tree> filter;

			internal Tree next;

			internal FilteringTreebankIterator(IEnumerator<Tree> iter, IPredicate<Tree> filter)
			{
				this.iter = iter;
				this.filter = filter;
				PrimeNext();
			}

			public virtual bool MoveNext()
			{
				return (next != null);
			}

			public virtual Tree Current
			{
				get
				{
					Tree answer = next;
					PrimeNext();
					return answer;
				}
			}

			public virtual void PrimeNext()
			{
				while (iter.MoveNext())
				{
					next = iter.Current;
					if (filter.Test(next))
					{
						return;
					}
				}
				next = null;
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}
	}
}
