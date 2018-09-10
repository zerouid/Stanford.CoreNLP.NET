using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Applies a Function to the labels in a tree.</summary>
	/// <author>John Bauer</author>
	public class TreeLeafLabelTransformer : ITreeTransformer
	{
		internal Func<string, string> transform;

		public TreeLeafLabelTransformer(Func<string, string> transform)
		{
			this.transform = transform;
		}

		public virtual Tree TransformTree(Tree tree)
		{
			foreach (Tree leaf in tree.GetLeaves())
			{
				ILabel label = leaf.Label();
				label.SetValue(transform.Apply(label.Value()));
			}
			return tree;
		}
	}
}
