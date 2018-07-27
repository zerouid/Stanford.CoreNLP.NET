using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>A lexicon class for Chinese.</summary>
	/// <remarks>
	/// A lexicon class for Chinese.  Extends the (English) BaseLexicon class,
	/// overriding its score and train methods to include a
	/// ChineseUnknownWordModel.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class ChineseLexicon : BaseLexicon
	{
		private const long serialVersionUID = -7836464391021114960L;

		public readonly bool useCharBasedUnknownWordModel;

		public readonly bool useGoodTuringUnknownWordModel;

		private const int Steps = 1;

		private RandomWalk probRandomWalk;

		public ChineseLexicon(Options op, ChineseTreebankParserParams @params, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(op, wordIndex, tagIndex)
		{
			// public static final boolean useMaxentUnknownWordModel;
			//private ChineseUnknownWordModel unknown;
			// private ChineseMaxentLexicon cml;
			useCharBasedUnknownWordModel = @params.useCharBasedUnknownWordModel;
			useGoodTuringUnknownWordModel = @params.useGoodTuringUnknownWordModel;
		}

		// if (useMaxentUnknownWordModel) {
		//  cml = new ChineseMaxentLexicon();
		// } else {
		//unknown = new ChineseUnknownWordModel();
		//this.setUnknownWordModel(new ChineseUnknownWordModel(op));
		// this.getUnknownWordModel().setLexicon(this);
		// }
		public override float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			double c_W = seenCounter.GetCount(iTW);
			bool seen = (c_W > 0.0);
			if (seen)
			{
				return base.Score(iTW, loc, word, featureSpec);
			}
			else
			{
				float score;
				// if (useMaxentUnknownWordModel) {
				//  score = cml.score(iTW, 0);
				// } else {
				score = this.GetUnknownWordModel().Score(iTW, loc, 0.0, 0.0, 0.0, word);
				// ChineseUnknownWordModel doesn't use the final three params
				// }
				return score;
			}
		}
	}
}
