using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>
	/// A class for reading Spanish AnCora trees that have been converted
	/// from XML to PTB format.
	/// </summary>
	/// <author>Jon Gauthier</author>
	/// <author>Spence Green (original French version)</author>
	[System.Serializable]
	public class SpanishTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 8L;

		// TODO
		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(), new SpanishTreeNormalizer(false, false, false), new PennTreebankTokenizer(@in));
		}
	}
}
