using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves
{
	/// <summary>Created by mjfang on 7/8/16.</summary>
	public class LooseConversationalSpeakerSieve : MSSieve
	{
		public LooseConversationalSpeakerSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet)
		{
		}

		public override void DoMentionToSpeaker(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<IList<Pair<int, int>>> skipChains = new List<IList<Pair<int, int>>>();
			IList<Pair<int, int>> currChain = new List<Pair<int, int>>();
			//Pairs are (pred_idx, paragraph_idx)
			for (int quote_idx = 0; quote_idx < quotes.Count; quote_idx++)
			{
				ICoreMap quote = quotes[quote_idx];
				if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) != null)
				{
					int para_idx = GetQuoteParagraph(quote);
					if (currChain.Count == 0)
					{
						currChain.Add(new Pair<int, int>(quote_idx, para_idx));
					}
					else
					{
						if (currChain[currChain.Count - 1].second == para_idx - 2)
						{
							currChain.Add(new Pair<int, int>(quote_idx, para_idx));
						}
						else
						{
							skipChains.Add(currChain);
							currChain = new List<Pair<int, int>>();
							currChain.Add(new Pair<int, int>(quote_idx, para_idx));
						}
					}
				}
			}
			if (currChain.Count != 0)
			{
				skipChains.Add(currChain);
			}
			foreach (IList<Pair<int, int>> skipChain in skipChains)
			{
				Pair<int, int> firstPair = skipChain[0];
				int firstParagraph = firstPair.second;
				//look for conversational chain candidate
				for (int prev_idx = firstPair.first - 1; prev_idx >= 0; prev_idx--)
				{
					ICoreMap quote = quotes[prev_idx + 1];
					ICoreMap prevQuote = quotes[prev_idx];
					if (GetQuoteParagraph(prevQuote) == firstParagraph - 2)
					{
						quote.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), prevQuote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)));
						quote.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), "Loose Conversational Speaker");
					}
				}
			}
		}
	}
}
