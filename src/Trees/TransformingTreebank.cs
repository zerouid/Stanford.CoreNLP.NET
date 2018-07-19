using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This class wraps another Treebank, and will vend trees that have been through
	/// a TreeTransformer.
	/// </summary>
	/// <remarks>
	/// This class wraps another Treebank, and will vend trees that have been through
	/// a TreeTransformer.  You can access them via requests like <code>apply()</code> or
	/// <code>iterator()</code>.
	/// <p>
	/// <i>Important note</i>: This class will only function properly if the TreeTransformer
	/// used is a function (which doesn't change its argument) rather than if it is a
	/// TreeMunger.
	/// </remarks>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Christopher Manning</author>
	public class TransformingTreebank : Treebank
	{
		private ITreeTransformer transformer;

		private Treebank tb;

		private const bool Verbose = false;

		/// <summary>Create a new TransformingTreebank.</summary>
		/// <remarks>
		/// Create a new TransformingTreebank.
		/// The trees are made with a <code>LabeledScoredTreeReaderFactory</code>.
		/// <p/>
		/// <i>Compatibility note: Until Sep 2004, this used to create a Treebank
		/// with a SimpleTreeReaderFactory, but this was changed as the old
		/// default wasn't very useful, especially to naive users.</i>
		/// </remarks>
		public TransformingTreebank()
			: this(new LabeledScoredTreeReaderFactory())
		{
		}

		/// <summary>Create a new TransformingTreebank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		public TransformingTreebank(ITreeReaderFactory trf)
			: base(trf)
		{
		}

		/// <summary>
		/// Create a new TransformingTreebank from a base Treebank that will
		/// transform trees with the given TreeTransformer.
		/// </summary>
		/// <remarks>
		/// Create a new TransformingTreebank from a base Treebank that will
		/// transform trees with the given TreeTransformer.
		/// This is the constructor that you should use.
		/// </remarks>
		/// <param name="tb">The base Treebank</param>
		/// <param name="transformer">The TreeTransformer applied to each Tree.</param>
		public TransformingTreebank(Treebank tb, ITreeTransformer transformer)
		{
			this.tb = tb;
			this.transformer = transformer;
		}

		/// <summary>Empty a <code>Treebank</code>.</summary>
		public override void Clear()
		{
			tb.Clear();
			transformer = null;
		}

		// public String toString() {
		//   return "TransformingTreebank[transformer=" + transformer + "]\n" + super.toString();
		// }
		/// <summary>Load trees from given path specification.</summary>
		/// <remarks>
		/// Load trees from given path specification.  Not supported for this
		/// type of treebank.
		/// </remarks>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filt">a FilenameFilter of files to load</param>
		public override void LoadPath(File path, IFileFilter filt)
		{
			throw new NotSupportedException();
		}

		/// <summary>Applies the TreeVisitor to to all trees in the Treebank.</summary>
		/// <param name="tv">A class that can process trees.</param>
		public override void Apply(ITreeVisitor tv)
		{
			foreach (Tree t in tb)
			{
				Tree tmpT = t.DeepCopy();
				if (transformer != null)
				{
					tmpT = transformer.TransformTree(tmpT);
				}
				tv.VisitTree(tmpT);
			}
		}

		public override IEnumerator<Tree> GetEnumerator()
		{
			return new TransformingTreebank.TransformingTreebankIterator(tb.GetEnumerator(), transformer);
		}

		/// <summary>Loads treebank grammar from first argument and prints it.</summary>
		/// <remarks>
		/// Loads treebank grammar from first argument and prints it.
		/// Just a demonstration of functionality. <br />
		/// <code>usage: java MemoryTreebank treebankFilesPath</code>
		/// </remarks>
		/// <param name="args">array of command-line arguments</param>
		public static void Main(string[] args)
		{
			Timing.StartTime();
			Treebank treebank = new DiskTreebank(null);
			Treebank treebank2 = new MemoryTreebank(null);
			treebank.LoadPath(args[0]);
			treebank2.LoadPath(args[0]);
			CompositeTreebank c = new CompositeTreebank(treebank, treebank2);
			Timing.EndTime();
			ITreeTransformer myTransformer = new TransformingTreebank.MyTreeTransformer();
			ITreeTransformer myTransformer2 = new TransformingTreebank.MyTreeTransformer2();
			ITreeTransformer myTransformer3 = new TransformingTreebank.MyTreeTransformer3();
			Treebank tf1 = c.Transform(myTransformer).Transform(myTransformer2).Transform(myTransformer3);
			Treebank tf2 = new Edu.Stanford.Nlp.Trees.TransformingTreebank(new Edu.Stanford.Nlp.Trees.TransformingTreebank(new Edu.Stanford.Nlp.Trees.TransformingTreebank(c, myTransformer), myTransformer2), myTransformer3);
			ITreeTransformer[] tta = new ITreeTransformer[] { myTransformer, myTransformer2, myTransformer3 };
			ITreeTransformer tt3 = new CompositeTreeTransformer(Arrays.AsList(tta));
			Treebank tf3 = c.Transform(tt3);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("COMPOSITE (DISK THEN MEMORY REPEATED VERSION OF) INPUT TREEBANK");
			System.Console.Out.WriteLine(c);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("SLOWLY TRANSFORMED TREEBANK, USING TransformingTreebank() CONSTRUCTOR");
			Treebank tx1 = new Edu.Stanford.Nlp.Trees.TransformingTreebank(c, myTransformer);
			System.Console.Out.WriteLine(tx1);
			System.Console.Out.WriteLine("-----");
			Treebank tx2 = new Edu.Stanford.Nlp.Trees.TransformingTreebank(tx1, myTransformer2);
			System.Console.Out.WriteLine(tx2);
			System.Console.Out.WriteLine("-----");
			Treebank tx3 = new Edu.Stanford.Nlp.Trees.TransformingTreebank(tx2, myTransformer3);
			System.Console.Out.WriteLine(tx3);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("TRANSFORMED TREEBANK, USING Treebank.transform()");
			System.Console.Out.WriteLine(tf1);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("PRINTING AGAIN TRANSFORMED TREEBANK, USING Treebank.transform()");
			System.Console.Out.WriteLine(tf1);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("TRANSFORMED TREEBANK, USING TransformingTreebank() CONSTRUCTOR");
			System.Console.Out.WriteLine(tf2);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("TRANSFORMED TREEBANK, USING CompositeTreeTransformer");
			System.Console.Out.WriteLine(tf3);
			System.Console.Out.WriteLine("-------------------------");
			System.Console.Out.WriteLine("COMPOSITE (DISK THEN MEMORY REPEATED VERSION OF) INPUT TREEBANK");
			System.Console.Out.WriteLine(c);
			System.Console.Out.WriteLine("-------------------------");
		}

		private class TransformingTreebankIterator : IEnumerator<Tree>
		{
			private IEnumerator<Tree> iter;

			private ITreeTransformer transformer;

			internal TransformingTreebankIterator(IEnumerator<Tree> iter, ITreeTransformer transformer)
			{
				// end main
				this.iter = iter;
				this.transformer = transformer;
			}

			public virtual bool MoveNext()
			{
				return iter.MoveNext();
			}

			public virtual Tree Current
			{
				get
				{
					// this line will throw NoSuchElement exception if empty base iterator....
					Tree ret = iter.Current;
					if (transformer != null)
					{
						ret = transformer.TransformTree(ret);
					}
					return ret;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}

		private class MyTreeTransformer : ITreeTransformer
		{
			// end static class TransformingTreebankIterator
			public virtual Tree TransformTree(Tree tree)
			{
				Tree treeCopy = tree.DeepCopy();
				foreach (Tree subtree in treeCopy)
				{
					if (subtree.Depth() < 2)
					{
						continue;
					}
					string categoryLabel = subtree.Label().ToString();
					ILabel label = subtree.Label();
					label.SetFromString(categoryLabel + "-t1");
				}
				return treeCopy;
			}
		}

		private class MyTreeTransformer2 : ITreeTransformer
		{
			public virtual Tree TransformTree(Tree tree)
			{
				Tree treeCopy = tree.DeepCopy();
				foreach (Tree subtree in treeCopy)
				{
					if (subtree.Depth() < 1)
					{
						continue;
					}
					string categoryLabel = subtree.Label().ToString();
					ILabel label = subtree.Label();
					label.SetFromString(categoryLabel + "-t2");
				}
				return treeCopy;
			}
		}

		private class MyTreeTransformer3 : ITreeTransformer
		{
			public virtual Tree TransformTree(Tree tree)
			{
				Tree treeCopy = tree.DeepCopy();
				foreach (Tree subtree in treeCopy)
				{
					if (subtree.Depth() < 2)
					{
						continue;
					}
					string categoryLabel = subtree.Label().ToString();
					ILabel label = subtree.Label();
					label.SetFromString(categoryLabel + "-t3");
				}
				return treeCopy;
			}
		}
	}
}
