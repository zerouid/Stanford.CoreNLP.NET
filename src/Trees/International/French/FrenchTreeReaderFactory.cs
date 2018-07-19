using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>
	/// A class for reading French Treebank trees that have been converted
	/// from XML to PTB format.
	/// </summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FrenchTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 8943534517L;

		public FrenchTreeReaderFactory()
		{
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(), new FrenchTreeNormalizer(false), new PennTreebankTokenizer(@in));
		}
	}
}
