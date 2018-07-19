using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Grace Muzny</author>
	public class VocativeSieve : QMSieve
	{
		public VocativeSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, string.Empty)
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			VocativeQuoteToMention(doc);
			OneSpeakerSentence(doc);
		}

		public virtual void VocativeQuoteToMention(Annotation doc)
		{
			// Start of utterance
			// before period
			// between commas
			// between comman & period
			// before exclamation
			// before question
			// Dear, oh!
			IList<CoreLabel> toks = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					continue;
				}
				int currQuoteIndex = quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation));
				int currParagraph = sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
				IList<ICoreMap> quotesInPrevParagraph = new List<ICoreMap>();
				for (int i = currQuoteIndex - 1; i >= 0; i--)
				{
					ICoreMap prevQuote = quotes[i];
					int prevParagraph = sentences[prevQuote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
					if (prevParagraph + 1 == currParagraph)
					{
						quotesInPrevParagraph.Add(prevQuote);
					}
					else
					{
						break;
					}
				}
				if (quotesInPrevParagraph.Count == 0)
				{
					continue;
				}
				bool vocativeFound = false;
				foreach (ICoreMap prevQuote_1 in quotesInPrevParagraph)
				{
					Pair<int, int> quoteRun = new Pair<int, int>(prevQuote_1.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), prevQuote_1.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
					Pair<List<string>, List<Pair<int, int>>> nameAndIndices = ScanForNames(quoteRun);
					IList<Pair<string, Pair<int, int>>> vocativeIndices = new List<Pair<string, Pair<int, int>>>();
					for (int i_1 = 0; i_1 < nameAndIndices.first.Count; i_1++)
					{
						string name = nameAndIndices.first[i_1];
						Pair<int, int> nameIndex = nameAndIndices.second[i_1];
						string prevToken = toks[nameIndex.first - 1].Word();
						string prevPrevToken = toks[nameIndex.first - 2].Word();
						string nextToken = toks[nameIndex.second + 1].Word();
						if ((prevToken.Equals(",") && nextToken.Equals("!")) || (prevToken.Equals(",") && nextToken.Equals("?")) || (prevToken.Equals(",") && nextToken.Equals(".")) || (prevToken.Equals(",") && nextToken.Equals(",")) || (prevToken.Equals(",") && nextToken
							.Equals(";")) || (prevToken.Equals("``") && nextToken.Equals(",")) || (nextToken.Equals("''") && prevToken.Equals(",")) || Sharpen.Runtime.EqualsIgnoreCase(prevToken, "dear") || (prevToken.Equals("!") && Sharpen.Runtime.EqualsIgnoreCase(prevPrevToken
							, "oh")))
						{
							vocativeIndices.Add(new Pair<string, Pair<int, int>>(name, nameIndex));
						}
					}
					if (vocativeIndices.Count > 0)
					{
						FillInMention(quote, vocativeIndices[0].first, vocativeIndices[0].second.first, vocativeIndices[0].second.second, "Deterministic Vocative -- name", Name);
						vocativeFound = true;
						break;
					}
				}
				if (vocativeFound)
				{
					continue;
				}
				foreach (ICoreMap prevQuote_2 in quotesInPrevParagraph)
				{
					Pair<int, int> quoteRun = new Pair<int, int>(prevQuote_2.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), prevQuote_2.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
					IList<int> animates = ScanForAnimates(quoteRun);
					IList<Pair<string, int>> animateVocatives = new List<Pair<string, int>>();
					for (int i_1 = 0; i_1 < animates.Count; i_1++)
					{
						int animateIndex = animates[i_1];
						string prevToken = toks[animateIndex - 1].Word();
						string prevPrevToken = toks[animateIndex - 2].Word();
						string nextToken = toks[animateIndex + 1].Word();
						if ((prevToken.Equals(",") && nextToken.Equals("!")) || (prevToken.Equals(",") && nextToken.Equals("?")) || (prevToken.Equals(",") && nextToken.Equals(".")) || (prevToken.Equals(",") && nextToken.Equals(",")) || (prevToken.Equals(",") && nextToken
							.Equals(";")) || (prevToken.Equals("``") && nextToken.Equals(",")) || (nextToken.Equals("''") && prevToken.Equals(",")) || Sharpen.Runtime.EqualsIgnoreCase(prevToken, "dear") || (prevToken.Equals("!") && Sharpen.Runtime.EqualsIgnoreCase(prevPrevToken
							, "oh")))
						{
							animateVocatives.Add(new Pair<string, int>(toks[animateIndex].Word(), animateIndex));
						}
					}
					if (animateVocatives.Count > 0)
					{
						FillInMention(quote, animateVocatives[0].first, animateVocatives[0].second, animateVocatives[0].second, "Deterministic Vocative -- animate noun", AnimateNoun);
						break;
					}
				}
			}
		}
	}
}
