using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth
{
	[System.Serializable]
	public class RothEntityExtractor : BasicEntityExtractor
	{
		private const long serialVersionUID = 1L;

		public const bool UseSubTypes = false;

		private IDictionary<string, string> entityTagForNer;

		public RothEntityExtractor()
			: base(null, UseSubTypes, null, true, new EntityMentionFactory(), true)
		{
			entityTagForNer = new Dictionary<string, string>();
			//    entityTagForNer.put("person", "Peop");
			//    entityTagForNer.put("organization", "Org");
			//    entityTagForNer.put("location", "Loc");
			entityTagForNer["person"] = "PEOPLE";
			entityTagForNer["organization"] = "ORGANIZATION";
			entityTagForNer["location"] = "LOCATION";
		}

		public override string GetEntityTypeForTag(string ner)
		{
			ner = ner.ToLower();
			if (entityTagForNer.Contains(ner))
			{
				return entityTagForNer[ner];
			}
			else
			{
				return "O";
			}
		}
	}
}
