using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <summary>Created by mjfang on 7/8/16.</summary>
	public class LooseConversationalSieve : QMSieve
	{
		public LooseConversationalSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "loose")
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<IList<Pair<int, int>>> skipChains = new List<IList<Pair<int, int>>>();
			IList<Pair<int, int>> currChain = new List<Pair<int, int>>();
			//Pairs are (quote_idx, paragraph_idx)
			//same as conversational, but make it less restrictive.
			//look for patterns: are they consecutive in paragraph? group those that are in
			for (int quote_idx = 0; quote_idx < quotes.Count; quote_idx++)
			{
				ICoreMap quote = quotes[quote_idx];
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) == null)
				{
					int para_idx = GetQuoteParagraph(quote);
					if (currChain.Count != 0 && currChain[currChain.Count - 1].second != para_idx - 2)
					{
						skipChains.Add(currChain);
						currChain = new List<Pair<int, int>>();
					}
					currChain.Add(new Pair<int, int>(quote_idx, para_idx));
				}
			}
			if (currChain.Count != 0)
			{
				skipChains.Add(currChain);
			}
			foreach (IList<Pair<int, int>> skipChain in skipChains)
			{
				Pair<int, int> firstQuoteAndParagraphIdx = skipChain[0];
				int firstParagraph = firstQuoteAndParagraphIdx.second;
				bool chainAttributed = false;
				for (int prevQuoteIdx = firstQuoteAndParagraphIdx.first - 1; prevQuoteIdx >= 0; prevQuoteIdx--)
				{
					ICoreMap prevQuote = quotes[prevQuoteIdx];
					if (GetQuoteParagraph(prevQuote) == firstParagraph - 2 && prevQuote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
					{
						foreach (Pair<int, int> quoteAndParagraphIdx in skipChain)
						{
							ICoreMap quote = quotes[quoteAndParagraphIdx.first];
							FillInMention(quote, GetMentionData(prevQuote), sieveName);
						}
					}
				}
			}
		}
	}
}
