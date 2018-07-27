

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// If you have a TreeTransformer which is not threadsafe, and you need
	/// to call it from multiple threads, this will wrap it in a
	/// synchronized manner.
	/// </summary>
	/// <author>John Bauer</author>
	public class SynchronizedTreeTransformer : ITreeTransformer
	{
		internal readonly ITreeTransformer threadUnsafe;

		public SynchronizedTreeTransformer(ITreeTransformer threadUnsafe)
		{
			this.threadUnsafe = threadUnsafe;
		}

		public virtual Tree TransformTree(Tree t)
		{
			lock (threadUnsafe)
			{
				return threadUnsafe.TransformTree(t);
			}
		}
	}
}
