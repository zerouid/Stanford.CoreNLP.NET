using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves
{
	/// <summary>Created by mjfang on 7/8/16.</summary>
	public class DeterministicSpeakerSieve : MSSieve
	{
		private IDictionary<string, Person.Gender> genderList;

		public DeterministicSpeakerSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet)
		{
		}

		public override void DoMentionToSpeaker(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				string mention = quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation));
				if (mention == null)
				{
					continue;
				}
				int mentionBegin = quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation));
				int mentionEnd = quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation));
				List<CoreLabel> mentionTokens = new List<CoreLabel>();
				for (int i = mentionBegin; i <= mentionEnd; i++)
				{
					mentionTokens.Add(doc.Get(typeof(CoreAnnotations.TokensAnnotation))[i]);
				}
				string mentionType = quote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation));
				if (mentionType.Equals("name"))
				{
					quote.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), characterMap[mention][0].name);
					quote.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), "automatic name");
				}
				else
				{
					if (mentionType.Equals("pronoun"))
					{
						Person speaker = DoCoreference(mentionTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), quote);
						if (speaker != null)
						{
							quote.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), speaker.name);
							quote.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), "coref");
						}
					}
				}
			}
		}
	}
}
