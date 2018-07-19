using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <summary>Created by mjfang on 7/7/16.</summary>
	public class ConversationalSieve : QMSieve
	{
		public ConversationalSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "conv")
		{
		}

		//attribute conversational mentions: assign the mention to the same quote as the
		//if quote X has not been labelled, has no add'l text, and quote X-2 has been labelled, and quotes X-2, X-1, and X are consecutive in paragraph,
		//and X-1's quote does not refer to a name:
		//give quote X the same mention as X-2.
		public override void DoQuoteToMention(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int index = 2; index < quotes.Count; index++)
			{
				ICoreMap currQuote = quotes[index];
				ICoreMap prevQuote = quotes[index - 1];
				ICoreMap twoPrevQuote = quotes[index - 2];
				int twoPrevPara = GetQuoteParagraph(twoPrevQuote);
				//default to first in quote that begins n-2
				for (int i = index - 3; i >= 0; i--)
				{
					if (GetQuoteParagraph(quotes[i]) == twoPrevPara)
					{
						twoPrevQuote = quotes[i];
					}
					else
					{
						break;
					}
				}
				int tokenBeginIdx = currQuote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				int tokenEndIdx = currQuote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
				ICoreMap currQuoteBeginSentence = sentences[currQuote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))];
				bool isAloneInParagraph = true;
				if (tokenBeginIdx > 0)
				{
					CoreLabel prevToken = tokens[tokenBeginIdx - 1];
					ICoreMap prevSentence = sentences[prevToken.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))];
					if (prevSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)).Equals(currQuoteBeginSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation))))
					{
						isAloneInParagraph = false;
					}
				}
				if (tokenEndIdx < tokens.Count - 1)
				{
					// if the next token is *NL*, it won't be in a sentence (if newlines have been tokenized)
					// so advance to the next non *NL* toke
					CoreLabel currToken = tokens[tokenEndIdx + 1];
					while (currToken.IsNewline() && tokenEndIdx + 1 < tokens.Count - 1)
					{
						tokenEndIdx++;
						currToken = tokens[tokenEndIdx + 1];
					}
					if (!currToken.IsNewline())
					{
						ICoreMap nextSentence = sentences[currToken.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))];
						if (nextSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)).Equals(currQuoteBeginSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation))))
						{
							isAloneInParagraph = false;
						}
					}
				}
				if (twoPrevQuote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) == null || !isAloneInParagraph || currQuote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null || twoPrevQuote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation
					)).Equals(Sieve.Pronoun))
				{
					continue;
				}
				if (GetQuoteParagraph(currQuote) == GetQuoteParagraph(prevQuote) + 1 && GetQuoteParagraph(prevQuote) == GetQuoteParagraph(twoPrevQuote) + 1)
				{
					FillInMention(currQuote, GetMentionData(twoPrevQuote), sieveName);
				}
			}
		}
	}
}
