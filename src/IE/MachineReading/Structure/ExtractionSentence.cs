using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>A RelationsSentence contains all the relations for a given sentence</summary>
	/// <author>Mihai</author>
	[System.Serializable]
	public class ExtractionSentence
	{
		private const long serialVersionUID = 87958315651919036L;

		/// <summary>Id of the textual document containing this sentence</summary>
		private readonly string documentId;

		/// <summary>Text of this sentence</summary>
		private string textContent;

		/// <summary>
		/// List of relation mentions in this sentence
		/// There are no ordering guarantees
		/// </summary>
		private readonly IList<RelationMention> relationMentions;

		/// <summary>
		/// List of entity mentions in this sentence
		/// There are no ordering guarantees
		/// </summary>
		private readonly IList<EntityMention> entityMentions;

		/// <summary>
		/// List of event mentions in this sentence
		/// There are no ordering guarantees
		/// </summary>
		private readonly IList<EventMention> eventMentions;

		public ExtractionSentence(string docid, string textContent)
		{
			this.documentId = docid;
			this.textContent = textContent;
			this.entityMentions = new List<EntityMention>();
			this.relationMentions = new List<RelationMention>();
			this.eventMentions = new List<EventMention>();
		}

		public ExtractionSentence(Edu.Stanford.Nlp.IE.Machinereading.Structure.ExtractionSentence original)
		{
			this.documentId = original.documentId;
			this.relationMentions = new List<RelationMention>(original.relationMentions);
			this.entityMentions = new List<EntityMention>(original.entityMentions);
			this.eventMentions = new List<EventMention>(original.eventMentions);
			this.textContent = original.textContent;
		}

		public virtual void AddEntityMention(EntityMention arg)
		{
			this.entityMentions.Add(arg);
		}

		public virtual void AddEntityMentions(ICollection<EntityMention> args)
		{
			Sharpen.Collections.AddAll(this.entityMentions, args);
		}

		public virtual void AddRelationMention(RelationMention rel)
		{
			relationMentions.Add(rel);
		}

		public virtual IList<RelationMention> GetRelationMentions()
		{
			return Java.Util.Collections.UnmodifiableList(relationMentions);
		}

		public virtual void SetRelationMentions(IList<RelationMention> rels)
		{
			relationMentions.Clear();
			Sharpen.Collections.AddAll(relationMentions, rels);
		}

		/// <summary>Return the relation that holds between the given entities.</summary>
		/// <remarks>
		/// Return the relation that holds between the given entities.
		/// Return a relation of type UNRELATED if this sentence contains no relation between the entities.
		/// </remarks>
		public virtual RelationMention GetRelation(RelationMentionFactory factory, params ExtractionObject[] args)
		{
			foreach (RelationMention rel in relationMentions)
			{
				if (rel.ArgsMatch(args))
				{
					return rel;
				}
			}
			return RelationMention.CreateUnrelatedRelation(factory, args);
		}

		/// <summary>
		/// Get list of all relations and non-relations between ArgForRelations in this sentence
		/// Use with care.
		/// </summary>
		/// <remarks>
		/// Get list of all relations and non-relations between ArgForRelations in this sentence
		/// Use with care. This is an expensive call due to getAllUnrelatedRelations, which creates all non-existing relations between all entity mentions
		/// </remarks>
		public virtual IList<RelationMention> GetAllRelations(RelationMentionFactory factory)
		{
			IList<RelationMention> allRelations = new List<RelationMention>(relationMentions);
			Sharpen.Collections.AddAll(allRelations, GetAllUnrelatedRelations(factory));
			return allRelations;
		}

		public virtual IList<RelationMention> GetAllUnrelatedRelations(RelationMentionFactory factory)
		{
			IList<RelationMention> nonRelations = new List<RelationMention>();
			IList<RelationMention> allRelations = new List<RelationMention>(relationMentions);
			//
			// scan all possible arguments
			//
			for (int i = 0; i < GetEntityMentions().Count; i++)
			{
				for (int j = 0; j < GetEntityMentions().Count; j++)
				{
					if (i == j)
					{
						continue;
					}
					EntityMention arg1 = GetEntityMentions()[i];
					EntityMention arg2 = GetEntityMentions()[j];
					bool match = false;
					foreach (RelationMention rel in allRelations)
					{
						if (rel.ArgsMatch(arg1, arg2))
						{
							match = true;
							break;
						}
					}
					if (!match)
					{
						RelationMention nonrel = RelationMention.CreateUnrelatedRelation(factory, arg1, arg2);
						nonRelations.Add(nonrel);
						allRelations.Add(nonrel);
					}
				}
			}
			return nonRelations;
		}

		public virtual void AddEventMention(EventMention @event)
		{
			eventMentions.Add(@event);
		}

		public virtual IList<EventMention> GetEventMentions()
		{
			return Java.Util.Collections.UnmodifiableList(eventMentions);
		}

		public virtual void SetEventMentions(IList<EventMention> events)
		{
			eventMentions.Clear();
			Sharpen.Collections.AddAll(eventMentions, events);
		}

		public virtual string GetTextContent()
		{
			return textContent;
		}

		/*
		public String getTextContent(Span span) {
		StringBuilder buf = new StringBuilder();
		assert(span != null);
		for(int i = span.start(); i < span.end(); i ++){
		if(i > span.start()) buf.append(" ");
		buf.append(tokens[i].word());
		}
		return buf.toString();
		}
		*/
		public virtual void SetTextContent(string textContent)
		{
			this.textContent = textContent;
		}

		// /**
		//  * Returns true if the character offset span is contained within this
		//  * sentence.
		//  * 
		//  * @param span a Span of character offsets
		//  * @return true if the span starts and ends within the sentence
		//  */
		// public boolean containsSpan(Span span) {
		//   int sentenceStart = tokens[0].beginPosition();
		//   int sentenceEnd = tokens[tokens.length - 1].endPosition();
		//   return sentenceStart <= span.start() && sentenceEnd >= span.end();
		// }
		public virtual IList<EntityMention> GetEntityMentions()
		{
			return Java.Util.Collections.UnmodifiableList(entityMentions);
		}

		public virtual void SetEntityMentions(IList<EntityMention> newArgs)
		{
			entityMentions.Clear();
			Sharpen.Collections.AddAll(entityMentions, newArgs);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(512);
			sb.Append("\"" + textContent + "\"");
			sb.Append("\n");
			foreach (RelationMention rel in this.relationMentions)
			{
				sb.Append("\n");
				sb.Append(rel);
			}
			// TODO: add event mentions
			return sb.ToString();
		}

		public static string TokensToString(Word[] tokens)
		{
			StringBuilder sb = new StringBuilder(512);
			for (int i = 0; i < tokens.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(" ");
				}
				Word l = tokens[i];
				sb.Append(l.Word() + "{" + l.BeginPosition() + ", " + l.EndPosition() + "}");
			}
			return sb.ToString();
		}

		// /**
		//  * Converts an ExtractionSentence to the equivalent List of CoreLabels.
		//  *
		//  * @param addAnswerAnnotation
		//  *          whether to annotate with gold NER tags
		//  * @return the sentence as a List<CoreLabel>
		//  */
		// public List<CoreLabel> toCoreLabels(
		//     boolean addAnswerAnnotation,
		//     Set<String> annotationsToSkip,
		//     boolean useSubTypes) {
		//   Tree completeTree = getTree();
		//   List<CoreLabel> labels = new ArrayList<CoreLabel>();
		//   List<Tree> tokenList = getTree().getLeaves();
		//   for (Tree tree : tokenList) {
		//     Word word = new Word(tree.label());
		//     CoreLabel label = new CoreLabel();
		//     label.set(TextAnnotation.class, word.value());
		//     if (addAnswerAnnotation) {
		//       label.set(AnswerAnnotation.class,
		//           SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL);
		//     }
		//     label.set(PartOfSpeechAnnotation.class, tree.parent(completeTree).label().value());
		//     labels.add(label);
		//   }
		//   if (addAnswerAnnotation) {
		//     // reset some annotation with answer types
		//     for (EntityMention entity : getEntityMentions()) {
		//       if (annotationsToSkip == null || ! annotationsToSkip.contains(entity.getType())) {
		//         // ignore entities without indices
		//         //if (entity.getSyntacticHeadTokenPosition() >= 0) {
		//         //  labels.get(entity.getSyntacticHeadTokenPosition()).set(
		//         //      AnswerAnnotation.class, entity.getType());
		//         //}
		//         if(entity.getHead() != null){
		//           for(int i = entity.getHeadTokenStart(); i < entity.getHeadTokenEnd(); i ++){
		//             String tag = entity.getType();
		//             if(useSubTypes && entity.getSubType() != null) tag += "-" + entity.getSubType();
		//             labels.get(i).set(AnswerAnnotation.class, tag);
		//           }
		//         }
		//       }
		//     }
		//   }
		//   return labels;
		// }
		public virtual string GetDocumentId()
		{
			return documentId;
		}
	}
}
