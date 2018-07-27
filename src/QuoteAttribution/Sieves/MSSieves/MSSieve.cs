using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;


namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves
{
	/// <summary>Created by mjfang on 7/8/16.</summary>
	/// <remarks>Created by mjfang on 7/8/16. Mention to Speaker Sieve</remarks>
	public abstract class MSSieve : Sieve
	{
		public MSSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacyList)
			: base(doc, characterMap, pronounCorefMap, animacyList)
		{
		}

		public abstract void DoMentionToSpeaker(Annotation doc);
	}
}
