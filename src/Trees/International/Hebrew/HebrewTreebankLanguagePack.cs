using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.Hebrew
{
	/// <author>Spence Green</author>
	[System.Serializable]
	public class HebrewTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		private const long serialVersionUID = 4787589385598144401L;

		private static readonly string[] pennPunctTags = new string[] { "yyCLN", "yyCM", "yyDASH", "yyDOT", "yyEXCL", "yyLRB", "yyQM", "yyQUOT", "yyRRB", "yySCLN" };

		private static readonly string[] pennSFPunctTags = new string[] { "yyDOT", "yyEXCL", "yyQM" };

		private static readonly string[] collinsPunctTags = new string[] { "-NONE-", "yyCLN", "yyCM", "yyDASH", "yyDOT", "yyEXCL", "yyLRB", "yyQM", "yyQUOT", "yyRRB", "yySCLN" };

		private static readonly char[] annotationIntroducingChars = new char[] { '-', '=', '|', '#', '^', '~' };

		/// <summary>wsg: This is the convention in Reut's preprocessed version of the treebank, and the Collins stuff.</summary>
		/// <remarks>
		/// wsg: This is the convention in Reut's preprocessed version of the treebank, and the Collins stuff.
		/// But we could change it to ROOT....
		/// </remarks>
		private static readonly string[] pennStartSymbols = new string[] { "TOP" };

		public override string[] PunctuationTags()
		{
			return pennPunctTags;
		}

		public override string[] PunctuationWords()
		{
			return pennPunctTags;
		}

		//Same as PTB
		public override string[] SentenceFinalPunctuationTags()
		{
			return pennSFPunctTags;
		}

		public override string[] StartSymbols()
		{
			return pennStartSymbols;
		}

		//TODO: Need to add Reut's rules
		public override IHeadFinder HeadFinder()
		{
			return new LeftHeadFinder();
		}

		//TODO: Need to add Reut's rules
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new LeftHeadFinder();
		}

		public override string[] SentenceFinalPunctuationWords()
		{
			return pennSFPunctTags;
		}

		public override string[] EvalBIgnoredPunctuationTags()
		{
			return collinsPunctTags;
		}

		public override string TreebankFileExtension()
		{
			return "tree";
		}

		public override char[] LabelAnnotationIntroducingCharacters()
		{
			return annotationIntroducingChars;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new HebrewTreeReaderFactory();
		}
	}
}
