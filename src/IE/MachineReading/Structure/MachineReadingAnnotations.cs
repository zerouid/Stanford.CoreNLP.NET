using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Annotations specific to the machinereading data structures</summary>
	/// <author>Mihai</author>
	public class MachineReadingAnnotations
	{
		private MachineReadingAnnotations()
		{
		}

		/// <summary>The CoreMap key for getting the entity mentions corresponding to a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the entity mentions corresponding to a sentence.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class EntityMentionsAnnotation : ICoreAnnotation<IList<EntityMention>>
		{
			// only static members
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the relation mentions corresponding to a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the relation mentions corresponding to a sentence.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class RelationMentionsAnnotation : ICoreAnnotation<IList<RelationMention>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting relation mentions corresponding to a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting relation mentions corresponding to a sentence.  Whereas
		/// RelationMentionsAnnotation gives only relations pertaining to a test entity,
		/// AllRelationMentionsAnnotation gives all pairwise relations.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class AllRelationMentionsAnnotation : ICoreAnnotation<IList<RelationMention>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the event mentions corresponding to a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the event mentions corresponding to a sentence.
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class EventMentionsAnnotation : ICoreAnnotation<IList<EventMention>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the document id of a given sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the document id of a given sentence.
		/// This key is typically set on sentence annotations.
		/// NOTE: This is a trivial subclass of CoreAnnotations.DocIDAnnotation
		/// </remarks>
		public class DocumentIdAnnotation : CoreAnnotations.DocIDAnnotation
		{
			public override Type GetType()
			{
				return typeof(string);
			}
		}

		public class DocumentDirectoryAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the syntactic dependencies of a sentence.</summary>
		/// <remarks>
		/// The CoreMap key for getting the syntactic dependencies of a sentence.
		/// Note: this is no longer used, but it appears in sentences cached during KBP 2010
		/// This key is typically set on sentence annotations.
		/// </remarks>
		public class DependencyAnnotation : ICoreAnnotation<SemanticGraph>
		{
			public virtual Type GetType()
			{
				return typeof(SemanticGraph);
			}
		}

		/// <summary>Marks trigger words for relation extraction</summary>
		/// <author>Mihai</author>
		public class TriggerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Marks words as belonging to a list of either male or female names</summary>
		public class GenderAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}
	}
}
