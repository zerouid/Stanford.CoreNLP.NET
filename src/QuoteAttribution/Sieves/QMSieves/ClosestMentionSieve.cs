using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <summary>Created by mjfang on 7/7/16.</summary>
	public class ClosestMentionSieve : QMSieve
	{
		public ClosestMentionSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "closestBaseline")
		{
		}

		public virtual Sieve.MentionData GetClosestMention(ICoreMap quote)
		{
			Sieve.MentionData closestBackward = FindClosestMentionInSpanBackward(new Pair<int, int>(0, quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - 1));
			Sieve.MentionData closestForward = FindClosestMentionInSpanForward(new Pair<int, int>(quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)), doc.Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 1));
			int backDistance = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - closestBackward.end;
			int forwardDistance = closestForward.begin - quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)) + 1;
			if (backDistance < forwardDistance)
			{
				return closestBackward;
			}
			else
			{
				return closestForward;
			}
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					Sieve.MentionData md = GetClosestMention(quote);
					FillInMention(quote, md, sieveName);
				}
			}
		}
	}
}
