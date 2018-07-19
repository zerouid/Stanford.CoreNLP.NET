using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	[System.Serializable]
	public class EntityMentionFactory
	{
		private const long serialVersionUID = 47894791411048523L;

		/// <summary>
		/// Always use this method to construct EntityMentions
		/// Other factories that inherit from this (e.g., NFLEntityMentionFactory) may override this
		/// </summary>
		/// <param name="objectId"/>
		/// <param name="sentence"/>
		/// <param name="extentSpan"/>
		/// <param name="headSpan"/>
		/// <param name="type"/>
		/// <param name="subtype"/>
		/// <param name="mentionType"/>
		public virtual EntityMention ConstructEntityMention(string objectId, ICoreMap sentence, Span extentSpan, Span headSpan, string type, string subtype, string mentionType)
		{
			return new EntityMention(objectId, sentence, extentSpan, headSpan, type, subtype, mentionType);
		}
	}
}
