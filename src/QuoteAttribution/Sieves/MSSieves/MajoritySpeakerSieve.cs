using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves
{
	/// <summary>Created by mjfang on 7/10/16.</summary>
	public class MajoritySpeakerSieve : MSSieve
	{
		private ICounter<string> topSpeakerList;

		public virtual ICounter<string> GetTopSpeakerList()
		{
			ICounter<string> characters = new ClassicCounter<string>();
			List<string> names = ScanForNames(new Pair<int, int>(0, doc.Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 1)).first;
			foreach (string name in names)
			{
				characters.IncrementCount(characterMap[name][0].name);
			}
			return characters;
		}

		public MajoritySpeakerSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet)
		{
			this.topSpeakerList = GetTopSpeakerList();
		}

		public override void DoMentionToSpeaker(Annotation doc)
		{
			foreach (ICoreMap quote in doc.Get(typeof(CoreAnnotations.QuotationsAnnotation)))
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) == null)
				{
					quote.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), characterMap[Counters.ToSortedList(topSpeakerList)[0]][0].name);
					quote.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), "majority speaker baseline");
				}
			}
		}
	}
}
