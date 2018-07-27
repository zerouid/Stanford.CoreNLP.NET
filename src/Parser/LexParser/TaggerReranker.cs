using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Gives a score to a Tree based on how well it matches the output of
	/// a tagger.
	/// </summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class TaggerReranker : IReranker
	{
		internal MaxentTagger tagger;

		internal Options op;

		internal double weight = -1.0;

		public TaggerReranker(MaxentTagger tagger, Options op)
		{
			this.tagger = tagger;
			this.op = op;
		}

		public virtual IRerankerQuery Process<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			return new TaggerReranker.Query(this, tagger.TagSentence(sentence));
		}

		public virtual IList<IEval> GetEvals()
		{
			return Java.Util.Collections.EmptyList();
		}

		public class Query : IRerankerQuery
		{
			internal readonly IList<TaggedWord> tagged;

			public Query(TaggerReranker _enclosing, IList<TaggedWord> tagged)
			{
				this._enclosing = _enclosing;
				this.tagged = tagged;
			}

			public virtual double Score(Tree tree)
			{
				IList<TaggedWord> yield = tree.TaggedYield();
				int wrong = 0;
				int len = Math.Min(yield.Count, this.tagged.Count);
				for (int i = 0; i < len; ++i)
				{
					string yieldTag = this._enclosing.op.Langpack().BasicCategory(yield[i].Tag());
					if (!yieldTag.Equals(this.tagged[i].Tag()))
					{
						wrong++;
					}
				}
				return wrong * this._enclosing.weight;
			}

			private readonly TaggerReranker _enclosing;
		}

		private const long serialVersionUID = 1;
	}
}
