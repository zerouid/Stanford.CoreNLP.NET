using System.Collections.Generic;


namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Relation holds a map from relation to relation mentions.</summary>
	/// <remarks>
	/// Relation holds a map from relation to relation mentions. Assumes a single
	/// dataset.
	/// </remarks>
	public class Relation
	{
		private IDictionary<string, IList<RelationMention>> relationToRelationMentions = new Dictionary<string, IList<RelationMention>>();

		public virtual void AddRelation(string relation, RelationMention rm)
		{
			IList<RelationMention> mentions = this.relationToRelationMentions[relation];
			if (mentions == null)
			{
				mentions = new List<RelationMention>();
				this.relationToRelationMentions[relation] = mentions;
			}
			mentions.Add(rm);
		}

		public virtual IList<RelationMention> GetRelationMentions(string relation)
		{
			IList<RelationMention> retVal = this.relationToRelationMentions[relation];
			return retVal != null ? retVal : Java.Util.Collections.EmptyList<RelationMention>();
		}
	}
}
