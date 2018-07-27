using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	[System.Serializable]
	public class RelationMentionFactory
	{
		private const long serialVersionUID = -662846276208839290L;

		/// <summary>
		/// Always use this method to construct RelationMentions
		/// Other factories that inherit from this (e.g., NFLRelationFactory) may override this
		/// </summary>
		/// <param name="objectId"/>
		/// <param name="sentence"/>
		/// <param name="span"/>
		/// <param name="type"/>
		/// <param name="subtype"/>
		/// <param name="args"/>
		/// <param name="probs"/>
		public virtual RelationMention ConstructRelationMention(string objectId, ICoreMap sentence, Span span, string type, string subtype, IList<ExtractionObject> args, ICounter<string> probs)
		{
			RelationMention relation = new RelationMention(objectId, sentence, span, type, subtype, args);
			relation.SetTypeProbabilities(probs);
			return relation;
		}
	}
}
