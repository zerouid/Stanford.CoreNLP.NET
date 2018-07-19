using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Spence Green</author>
	public class FactoredLexiconEvent
	{
		private readonly int wordId;

		private readonly int lemmaId;

		private readonly int tagId;

		private readonly int morphId;

		private readonly int loc;

		private readonly string word;

		private readonly string featureStr;

		public FactoredLexiconEvent(int wordId, int tagId, int lemmaId, int morphId, int loc, string word, string featureStr)
		{
			this.wordId = wordId;
			this.tagId = tagId;
			this.lemmaId = lemmaId;
			this.morphId = morphId;
			this.loc = loc;
			this.word = word;
			this.featureStr = featureStr;
		}

		public virtual int WordId()
		{
			return wordId;
		}

		public virtual int TagId()
		{
			return tagId;
		}

		public virtual int MorphId()
		{
			return morphId;
		}

		public virtual int LemmaId()
		{
			return lemmaId;
		}

		public virtual int GetLoc()
		{
			return loc;
		}

		public virtual string Word()
		{
			return word;
		}

		public virtual string FeatureStr()
		{
			return featureStr;
		}

		public override string ToString()
		{
			return word;
		}
	}
}
