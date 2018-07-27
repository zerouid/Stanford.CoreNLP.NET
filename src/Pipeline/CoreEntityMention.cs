using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	public class CoreEntityMention
	{
		private ICoreMap entityMentionCoreMap;

		private CoreSentence sentence;

		public CoreEntityMention(CoreSentence mySentence, ICoreMap coreMapEntityMention)
		{
			this.sentence = mySentence;
			this.entityMentionCoreMap = coreMapEntityMention;
		}

		/// <summary>get the underlying CoreMap if need be</summary>
		public virtual ICoreMap CoreMap()
		{
			return entityMentionCoreMap;
		}

		/// <summary>get this entity mention's sentence</summary>
		public virtual CoreSentence Sentence()
		{
			return sentence;
		}

		/// <summary>full text of the mention</summary>
		public virtual string Text()
		{
			return this.entityMentionCoreMap.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>the list of tokens for this entity mention</summary>
		public virtual IList<CoreLabel> Tokens()
		{
			return this.entityMentionCoreMap.Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		/// <summary>char offsets of mention</summary>
		public virtual Pair<int, int> CharOffsets()
		{
			int beginCharOffset = this.entityMentionCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int endCharOffset = this.entityMentionCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			return new Pair<int, int>(beginCharOffset, endCharOffset);
		}

		/// <summary>return the type of the entity mention</summary>
		public virtual string EntityType()
		{
			return this.entityMentionCoreMap.Get(typeof(CoreAnnotations.EntityTypeAnnotation));
		}

		/// <summary>return the entity this entity mention is linked to</summary>
		public virtual string Entity()
		{
			return this.entityMentionCoreMap.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation));
		}

		/// <summary>return the canonical entity mention for this entity mention</summary>
		public virtual Optional<Edu.Stanford.Nlp.Pipeline.CoreEntityMention> CanonicalEntityMention()
		{
			CoreDocument myDocument = sentence.Document();
			Optional<int> canonicalEntityMentionIndex = Optional.OfNullable(CoreMap().Get(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation)));
			return canonicalEntityMentionIndex.IsPresent() ? Optional.Of(sentence.Document().EntityMentions()[canonicalEntityMentionIndex.Get()]) : Optional.Empty();
		}

		public override string ToString()
		{
			return CoreMap().ToString();
		}
	}
}
