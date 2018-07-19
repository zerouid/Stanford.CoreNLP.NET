using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Entity holds a map from entity to entity mentions.</summary>
	/// <remarks>Entity holds a map from entity to entity mentions. Assumes a single dataset.</remarks>
	public class Entity
	{
		private IDictionary<string, IList<EntityMention>> entityToEntityMentions = new Dictionary<string, IList<EntityMention>>();

		/// <param name="entity">
		/// - identifier for entity, could be entity id or common string that
		/// all entity mentions of this entity share
		/// </param>
		/// <param name="em">- entity mention</param>
		public virtual void AddEntity(string entity, EntityMention em)
		{
			IList<EntityMention> mentions = this.entityToEntityMentions[entity];
			if (mentions == null)
			{
				mentions = new List<EntityMention>();
				this.entityToEntityMentions[entity] = mentions;
			}
			mentions.Add(em);
		}

		public virtual IList<EntityMention> GetEntityMentions(string entity)
		{
			IList<EntityMention> retVal = this.entityToEntityMentions[entity];
			return retVal != null ? retVal : Java.Util.Collections.EmptyList<EntityMention>();
		}
	}
}
