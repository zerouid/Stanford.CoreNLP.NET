

namespace Edu.Stanford.Nlp.Trees
{
	public interface ITreebankTransformer
	{
		MemoryTreebank TransformTrees(Treebank tb);
	}
}
