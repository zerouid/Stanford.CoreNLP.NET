using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A TreeTransformer that applies component TreeTransformers in order.</summary>
	/// <remarks>
	/// A TreeTransformer that applies component TreeTransformers in order.
	/// The order in which they are applied is the order in which they are added or
	/// the order in which they appear in the List passed to the constructor.
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class CompositeTreeTransformer : ITreeTransformer
	{
		private readonly IList<ITreeTransformer> transformers = new List<ITreeTransformer>();

		public CompositeTreeTransformer()
		{
		}

		public CompositeTreeTransformer(IList<ITreeTransformer> tt)
		{
			Sharpen.Collections.AddAll(transformers, tt);
		}

		public virtual void AddTransformer(ITreeTransformer tt)
		{
			transformers.Add(tt);
		}

		public virtual Tree TransformTree(Tree t)
		{
			foreach (ITreeTransformer tt in transformers)
			{
				t = tt.TransformTree(t);
			}
			return t;
		}

		public override string ToString()
		{
			return "CompositeTreeTransformer: " + transformers;
		}
	}
}
