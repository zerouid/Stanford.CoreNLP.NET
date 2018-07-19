using System;
using System.Collections.Generic;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <author>Galen Andrew</author>
	public class CompositeTreebank : Treebank
	{
		private Treebank t1;

		private Treebank t2;

		public CompositeTreebank(Treebank t1, Treebank t2)
		{
			this.t1 = t1;
			this.t2 = t2;
		}

		public override void Clear()
		{
			t1.Clear();
			t2.Clear();
		}

		public override void LoadPath(File path, IFileFilter filt)
		{
			throw new NotSupportedException();
		}

		public override void Apply(ITreeVisitor tp)
		{
			foreach (Tree tree in this)
			{
				tp.VisitTree(tree);
			}
		}

		public override IEnumerator<Tree> GetEnumerator()
		{
			return new CompositeTreebank.CompositeTreebankIterator(t1, t2);
		}

		private class CompositeTreebankIterator : IEnumerator<Tree>
		{
			private readonly IEnumerator<Tree> it1;

			private readonly IEnumerator<Tree> it2;

			public CompositeTreebankIterator(ICollection<Tree> c1, ICollection<Tree> c2)
			{
				it1 = c1.GetEnumerator();
				it2 = c2.GetEnumerator();
			}

			public virtual bool MoveNext()
			{
				return (it1.MoveNext() || it2.MoveNext());
			}

			public virtual Tree Current
			{
				get
				{
					Tree tree = it1.MoveNext() ? it1.Current : it2.Current;
					return tree;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}
		// end static class CompositeTreebankIterator
	}
}
