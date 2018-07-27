using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>
	/// A class for reading French Treebank trees that have been converted
	/// from XML to PTB format.
	/// </summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FrenchXMLTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 578942679136874L;

		private readonly bool ccTagset;

		public FrenchXMLTreeReaderFactory(bool ccTagset)
		{
			this.ccTagset = ccTagset;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new FrenchXMLTreeReader(@in, ccTagset);
		}
	}
}
