using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Grace Muzny</author>
	public class ParagraphEndQuoteClosestSieve : QMSieve
	{
		public ParagraphEndQuoteClosestSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "Deterministic endQuoteClosestBefore")
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			ParagraphEndQuoteClosestBefore(doc);
			OneSpeakerSentence(doc);
		}

		//select nearest mention to the left if: the quote is ending a paragraph.
		public virtual void ParagraphEndQuoteClosestBefore(Annotation doc)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					continue;
				}
				Pair<int, int> range = QuoteAttributionUtils.GetRemainderInSentence(doc, quote);
				if (range == null)
				{
					continue;
				}
				//search for mentions in the first run
				Pair<List<string>, List<Pair<int, int>>> namesAndNameIndices = ScanForNames(range);
				List<string> names = namesAndNameIndices.first;
				int quoteBeginTokenIndex = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				bool isBefore = range.second.Equals(quoteBeginTokenIndex - 1);
				//check if the range is preceding the quote or after it.
				int quoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, quote);
				int quoteIndex = quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation));
				bool isOnlyQuoteInParagraph = true;
				if (quoteIndex > 0)
				{
					ICoreMap prevQuote = quotes[quoteIndex - 1];
					int prevQuoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, prevQuote);
					if (prevQuoteParagraph == quoteParagraph)
					{
						isOnlyQuoteInParagraph = false;
					}
				}
				if (quoteIndex < quotes.Count - 1)
				{
					ICoreMap nextQuote = quotes[quoteIndex + 1];
					int nextQuoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, nextQuote);
					if (nextQuoteParagraph == quoteParagraph)
					{
						isOnlyQuoteInParagraph = false;
					}
				}
				if (isBefore && tokens[range.second].Word().Equals(",") && isOnlyQuoteInParagraph)
				{
					Sieve.MentionData closestMention = FindClosestMentionInSpanBackward(range);
					if (closestMention != null && !closestMention.type.Equals("animate noun"))
					{
						FillInMention(quote, closestMention, sieveName);
					}
				}
			}
		}
	}
}
