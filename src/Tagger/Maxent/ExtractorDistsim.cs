using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Extractor for adding distsim information.</summary>
	/// <author>rafferty</author>
	[System.Serializable]
	public class ExtractorDistsim : Extractor
	{
		private const long serialVersionUID = 2L;

		private readonly Distsim lexicon;

		internal override string Extract(History h, PairsHolder pH)
		{
			string word = base.Extract(h, pH);
			return lexicon.GetMapping(word);
		}

		internal ExtractorDistsim(string distSimPath, int position)
			: base(position, false)
		{
			lexicon = Distsim.InitLexicon(distSimPath);
		}

		public override bool IsLocal()
		{
			return position == 0;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}
}
