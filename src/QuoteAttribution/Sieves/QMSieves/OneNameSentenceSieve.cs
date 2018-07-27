using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Grace Muzny</author>
	public class OneNameSentenceSieve : QMSieve
	{
		public OneNameSentenceSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "Deterministic oneNameSentence")
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			OneNameSentence(doc);
			OneSpeakerSentence(doc);
		}

		public virtual void OneNameSentence(Annotation doc)
		{
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
				Pair<List<string>, List<Pair<int, int>>> namesAndNameIndices = ScanForNames(range);
				List<string> names = namesAndNameIndices.first;
				List<Pair<int, int>> nameIndices = namesAndNameIndices.second;
				List<int> pronounsIndices = ScanForPronouns(range);
				if (names.Count == 1)
				{
					IList<Person> p = characterMap[names[0]];
					//guess if exactly one name
					if (p.Count == 1 && pronounsIndices.Count == 0)
					{
						FillInMention(quote, TokenRangeToString(nameIndices[0]), nameIndices[0].first, nameIndices[0].second, sieveName, Name);
					}
				}
			}
		}
	}
}
