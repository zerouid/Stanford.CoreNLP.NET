using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Process
{
	/// <summary>A tokenizer that works by calling a WordSegmenter.</summary>
	/// <remarks>
	/// A tokenizer that works by calling a WordSegmenter.
	/// This is used for Chinese and Arabic.
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Spence Green</author>
	public class WordSegmentingTokenizer : AbstractTokenizer<IHasWord>
	{
		private IEnumerator<IHasWord> wordIter;

		private ITokenizer<CoreLabel> tok;

		private IWordSegmenter wordSegmenter;

		public WordSegmentingTokenizer(IWordSegmenter segmenter, Reader r)
			: this(segmenter, WhitespaceTokenizer.NewCoreLabelWhitespaceTokenizer(r))
		{
		}

		public WordSegmentingTokenizer(IWordSegmenter segmenter, ITokenizer<CoreLabel> tokenizer)
		{
			wordSegmenter = segmenter;
			tok = tokenizer;
		}

		protected internal override IHasWord GetNext()
		{
			while (wordIter == null || !wordIter.MoveNext())
			{
				if (!tok.MoveNext())
				{
					return null;
				}
				CoreLabel token = tok.Current;
				string s = token.Word();
				if (s == null)
				{
					return null;
				}
				if (s.Equals(WhitespaceLexer.Newline))
				{
					// if newlines were significant, we should make sure to return
					// them when we see them
					IList<IHasWord> se = Java.Util.Collections.SingletonList<IHasWord>(token);
					wordIter = se.GetEnumerator();
				}
				else
				{
					IList<IHasWord> se = wordSegmenter.Segment(s);
					wordIter = se.GetEnumerator();
				}
			}
			return wordIter.Current;
		}

		public static ITokenizerFactory<IHasWord> Factory(IWordSegmenter wordSegmenter)
		{
			return new WordSegmentingTokenizer.WordSegmentingTokenizerFactory(wordSegmenter);
		}

		[System.Serializable]
		private class WordSegmentingTokenizerFactory : ITokenizerFactory<IHasWord>
		{
			private const long serialVersionUID = -4697961121607489828L;

			internal bool tokenizeNLs = false;

			private IWordSegmenter segmenter;

			public WordSegmentingTokenizerFactory(IWordSegmenter wordSegmenter)
			{
				segmenter = wordSegmenter;
			}

			public virtual IEnumerator<IHasWord> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			public virtual ITokenizer<IHasWord> GetTokenizer(Reader r)
			{
				return GetTokenizer(r, null);
			}

			public virtual ITokenizer<IHasWord> GetTokenizer(Reader r, string extraOptions)
			{
				bool tokenizeNewlines = this.tokenizeNLs;
				if (extraOptions != null)
				{
					Properties prop = StringUtils.StringToProperties(extraOptions);
					tokenizeNewlines = PropertiesUtils.GetBool(prop, "tokenizeNLs", this.tokenizeNLs);
				}
				return new WordSegmentingTokenizer(segmenter, WhitespaceTokenizer.NewCoreLabelWhitespaceTokenizer(r, tokenizeNewlines));
			}

			public virtual void SetOptions(string options)
			{
				Properties prop = StringUtils.StringToProperties(options);
				tokenizeNLs = PropertiesUtils.GetBool(prop, "tokenizeNLs", tokenizeNLs);
			}
		}
	}
}
