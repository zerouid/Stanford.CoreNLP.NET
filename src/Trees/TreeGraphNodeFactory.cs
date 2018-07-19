using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>TreeGraphNodeFactory</c>
	/// acts as a factory for creating
	/// tree nodes of type
	/// <see cref="TreeGraphNode"/>
	/// .  Unless
	/// another
	/// <see cref="Edu.Stanford.Nlp.Ling.ILabelFactory"/>
	/// is supplied, it will use a CoreLabelFactory
	/// by default.
	/// </summary>
	/// <author>Bill MacCartney</author>
	public class TreeGraphNodeFactory : ITreeFactory
	{
		private readonly ILabelFactory mlf;

		/// <summary>
		/// Make a
		/// <c>TreeFactory</c>
		/// that produces
		/// <c>TreeGraphNode</c>
		/// s.  The labels are of class
		/// <c>CoreLabel</c>
		/// .
		/// </summary>
		public TreeGraphNodeFactory()
			: this(CoreLabel.Factory())
		{
		}

		/// <summary>
		/// Make a
		/// <c>TreeFactory</c>
		/// that produces
		/// <c>TreeGraphNode</c>
		/// s.  The labels depend on the
		/// <c>LabelFactory</c>
		/// .
		/// </summary>
		/// <param name="mlf">The LabelFactory to use for node labels</param>
		public TreeGraphNodeFactory(ILabelFactory mlf)
		{
			this.mlf = mlf;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual Tree NewLeaf(string word)
		{
			return NewLeaf(mlf.NewLabel(word));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual Tree NewLeaf(ILabel label)
		{
			return new TreeGraphNode(label);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual Tree NewTreeNode(string parent, IList<Tree> children)
		{
			return NewTreeNode(mlf.NewLabel(parent), children);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual Tree NewTreeNode(ILabel parentLabel, IList<Tree> children)
		{
			return new TreeGraphNode(parentLabel, children);
		}
	}
}
