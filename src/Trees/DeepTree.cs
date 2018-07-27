
using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A tree combined with a map from subtree to SimpleMatrix vectors.</summary>
	/// <author>Richard Socher</author>
	public class DeepTree
	{
		private readonly Tree tree;

		private readonly IdentityHashMap<Tree, SimpleMatrix> vectors;

		private readonly double score;

		public virtual Tree GetTree()
		{
			return tree;
		}

		public virtual IdentityHashMap<Tree, SimpleMatrix> GetVectors()
		{
			return vectors;
		}

		public virtual double GetScore()
		{
			return score;
		}

		public DeepTree(Tree tree, IdentityHashMap<Tree, SimpleMatrix> vectors, double score)
		{
			this.tree = tree;
			this.vectors = vectors;
			this.score = score;
		}

		private sealed class _IComparator_41 : IComparator<Edu.Stanford.Nlp.Trees.DeepTree>
		{
			public _IComparator_41()
			{
			}

			/// <summary>Reverses the score comparison so that we can sort highest score first</summary>
			public int Compare(Edu.Stanford.Nlp.Trees.DeepTree o1, Edu.Stanford.Nlp.Trees.DeepTree o2)
			{
				return -double.Compare(o1.score, o2.score);
			}
		}

		/// <summary>
		/// A comparator that can be used with Collections.sort() that puts
		/// the highest scoring tree first
		/// </summary>
		public static readonly IComparator<Edu.Stanford.Nlp.Trees.DeepTree> DescendingComparator = new _IComparator_41();
	}
}
