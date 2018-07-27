using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Grace Muzny</author>
	public class TrigramSieve : QMSieve
	{
		public TrigramSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, string.Empty)
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			TrigramPatterns(doc);
			OneSpeakerSentence(doc);
		}

		public virtual void TrigramPatterns(Annotation doc)
		{
			IList<CoreLabel> docTokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> docQuotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			foreach (ICoreMap quote in docQuotes)
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					continue;
				}
				int quoteBeginTokenIndex = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				int quoteEndTokenIndex = quote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
				int quoteEndSentenceIndex = quote.Get(typeof(CoreAnnotations.SentenceEndAnnotation));
				Pair<int, int> precedingTokenRange = QuoteAttributionUtils.GetTokenRangePrecedingQuote(doc, quote);
				//get tokens before and after
				if (precedingTokenRange != null)
				{
					Pair<List<string>, List<Pair<int, int>>> namesAndNameIndices = ScanForNames(precedingTokenRange);
					List<string> names = namesAndNameIndices.first;
					List<Pair<int, int>> nameIndices = namesAndNameIndices.second;
					if (names.Count > 0)
					{
						int offset = 0;
						if (beforeQuotePunctuation.Contains(docTokens[quoteBeginTokenIndex - 1].Word()))
						{
							offset = 1;
						}
						Pair<int, int> lastNameIndex = nameIndices[nameIndices.Count - 1];
						CoreLabel prevToken = docTokens[quoteBeginTokenIndex - 1 - offset];
						//CVQ
						if (prevToken.Tag() != null && prevToken.Tag().StartsWith("V") && lastNameIndex.second.Equals(quoteBeginTokenIndex - 2 - offset))
						{
							// verb!
							FillInMention(quote, names[names.Count - 1], lastNameIndex.first, lastNameIndex.second, "trigram CVQ", Name);
							continue;
						}
						//VCQ
						if (lastNameIndex.second.Equals(quoteBeginTokenIndex - 1 - offset))
						{
							CoreLabel secondPrevToken = docTokens[lastNameIndex.first - 1];
							if (secondPrevToken.Tag().StartsWith("V"))
							{
								FillInMention(quote, names[names.Count - 1], lastNameIndex.first, lastNameIndex.second, "trigram VCQ", Name);
								continue;
							}
						}
					}
					List<int> pronounsIndices = ScanForPronouns(precedingTokenRange);
					if (pronounsIndices.Count > 0)
					{
						int offset = 0;
						if (beforeQuotePunctuation.Contains(docTokens[quoteBeginTokenIndex - 1].Word()))
						{
							offset = 1;
						}
						CoreLabel prevToken = docTokens[quoteBeginTokenIndex - 1 - offset];
						int lastPronounIndex = pronounsIndices[pronounsIndices.Count - 1];
						//PVQ
						if (prevToken.Tag().StartsWith("V") && lastPronounIndex == quoteBeginTokenIndex - 2 - offset)
						{
							// verb!
							FillInMention(quote, TokenRangeToString(lastPronounIndex), lastPronounIndex, lastPronounIndex, "trigram PVQ", Pronoun);
							continue;
						}
						//VPQ
						if (lastPronounIndex == quoteBeginTokenIndex - 1 - offset && docTokens[quoteBeginTokenIndex - 2 - offset].Tag().StartsWith("V"))
						{
							FillInMention(quote, TokenRangeToString(lastPronounIndex), lastPronounIndex, lastPronounIndex, "trigram VPQ", Pronoun);
							continue;
						}
					}
				}
				Pair<int, int> followingTokenRange = QuoteAttributionUtils.GetTokenRangeFollowingQuote(doc, quote);
				if (followingTokenRange != null)
				{
					Pair<List<string>, List<Pair<int, int>>> namesAndNameIndices = ScanForNames(followingTokenRange);
					List<string> names = namesAndNameIndices.first;
					List<Pair<int, int>> nameIndices = namesAndNameIndices.second;
					if (names.Count > 0)
					{
						Pair<int, int> firstNameIndex = nameIndices[0];
						CoreLabel nextToken = docTokens[quoteEndTokenIndex + 1];
						//QVC
						if (nextToken.Tag().StartsWith("V") && firstNameIndex.first.Equals(quoteEndTokenIndex + 2))
						{
							// verb!
							FillInMention(quote, names[0], firstNameIndex.first, firstNameIndex.second, "trigram QVC", Name);
							continue;
						}
						//QCV
						if (firstNameIndex.first.Equals(quoteEndTokenIndex + 1))
						{
							CoreLabel secondNextToken = docTokens[firstNameIndex.second + 1];
							if (secondNextToken.Tag().StartsWith("V"))
							{
								FillInMention(quote, names[0], firstNameIndex.first, firstNameIndex.second, "trigram QCV", Name);
								continue;
							}
						}
					}
					List<int> pronounsIndices = ScanForPronouns(followingTokenRange);
					if (pronounsIndices.Count > 0)
					{
						CoreLabel nextToken = docTokens[quoteEndTokenIndex + 1];
						int firstPronounIndex = pronounsIndices[0];
						//QVP
						if (nextToken.Tag().StartsWith("V") && firstPronounIndex == quoteEndTokenIndex + 2)
						{
							// verb!
							FillInMention(quote, TokenRangeToString(pronounsIndices[0]), firstPronounIndex, firstPronounIndex, "trigram QVP", Pronoun);
							continue;
						}
						//QPV
						if (firstPronounIndex == quoteEndTokenIndex + 1 && docTokens[quoteEndTokenIndex + 2].Tag().StartsWith("V"))
						{
							FillInMention(quote, TokenRangeToString(pronounsIndices[pronounsIndices.Count - 1]), firstPronounIndex, firstPronounIndex, "trigram QPV", Pronoun);
							continue;
						}
					}
				}
			}
		}
	}
}
