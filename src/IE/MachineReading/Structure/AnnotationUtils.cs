using System;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Utilities to manipulate Annotations storing datasets or sentences with Machine Reading info</summary>
	/// <author>Mihai</author>
	public class AnnotationUtils
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Structure.AnnotationUtils));

		private AnnotationUtils()
		{
		}

		// only static methods
		/// <summary>Given a list of sentences (as CoreMaps), wrap it in a new Annotation.</summary>
		public static Annotation CreateDataset(IList<ICoreMap> sentences)
		{
			Annotation dataset = new Annotation(string.Empty);
			AddSentences(dataset, sentences);
			return dataset;
		}

		/// <summary>Randomized shuffle of all sentences int this dataset</summary>
		/// <param name="dataset"/>
		public static void ShuffleSentences(ICoreMap dataset)
		{
			IList<ICoreMap> sentences = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// we use a constant seed for replicability of experiments
			Java.Util.Collections.Shuffle(sentences, new Random(0));
			dataset.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
		}

		/// <summary>Converts the labels of all entity mentions in this dataset to sequences of CoreLabels</summary>
		/// <param name="dataset"/>
		/// <param name="annotationsToSkip"/>
		/// <param name="useSubTypes"/>
		public static IList<IList<CoreLabel>> EntityMentionsToCoreLabels(ICoreMap dataset, ICollection<string> annotationsToSkip, bool useSubTypes, bool useBIO)
		{
			IList<IList<CoreLabel>> retVal = new List<IList<CoreLabel>>();
			IList<ICoreMap> sentences = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> labeledSentence = SentenceEntityMentionsToCoreLabels(sentence, true, annotationsToSkip, null, useSubTypes, useBIO);
				System.Diagnostics.Debug.Assert((labeledSentence != null));
				retVal.Add(labeledSentence);
			}
			return retVal;
		}

		/// <summary>Converts the labels of all entity mentions in this sentence to sequences of CoreLabels</summary>
		/// <param name="sentence"/>
		/// <param name="addAnswerAnnotation"/>
		/// <param name="annotationsToSkip"/>
		/// <param name="useSubTypes"/>
		public static IList<CoreLabel> SentenceEntityMentionsToCoreLabels(ICoreMap sentence, bool addAnswerAnnotation, ICollection<string> annotationsToSkip, ICollection<string> mentionTypesToUse, bool useSubTypes, bool useBIO)
		{
			/*
			Tree completeTree = sentence.get(TreeCoreAnnotations.TreeAnnotation.class);
			if(completeTree == null){
			throw new RuntimeException("ERROR: TreeAnnotation MUST be set before calling this method!");
			}
			*/
			//
			// Set TextAnnotation and PartOfSpeechAnnotation (using the parser data)
			//
			/*
			List<CoreLabel> labels = new ArrayList<CoreLabel>();
			List<Tree> tokenList = completeTree.getLeaves();
			for (Tree tree : tokenList) {
			Word word = new Word(tree.label());
			CoreLabel label = new CoreLabel();
			label.set(CoreAnnotations.TextAnnotation.class, word.value());
			if (addAnswerAnnotation) {
			label.set(CoreAnnotations.AnswerAnnotation.class,
			SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL);
			}
			label.set(CoreAnnotations.PartOfSpeechAnnotation.class, tree.parent(completeTree).label().value());
			labels.add(label);
			}
			*/
			// use the token CoreLabels not the parser data => more robust
			IList<CoreLabel> labels = new List<CoreLabel>();
			foreach (CoreLabel l in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				CoreLabel nl = new CoreLabel(l);
				if (addAnswerAnnotation)
				{
					nl.Set(typeof(CoreAnnotations.AnswerAnnotation), SeqClassifierFlags.DefaultBackgroundSymbol);
				}
				labels.Add(nl);
			}
			// Add AnswerAnnotation from the types of the entity mentions
			if (addAnswerAnnotation)
			{
				IList<EntityMention> entities = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				if (entities != null)
				{
					foreach (EntityMention entity in entities)
					{
						// is this a type that we should skip?
						if (annotationsToSkip != null && annotationsToSkip.Contains(entity.GetType()))
						{
							continue;
						}
						// is this a valid mention type?
						if (mentionTypesToUse != null && !mentionTypesToUse.Contains(entity.GetMentionType()))
						{
							continue;
						}
						// ignore entities without head span
						if (entity.GetHead() != null)
						{
							for (int i = entity.GetHeadTokenStart(); i < entity.GetHeadTokenEnd(); i++)
							{
								string tag = entity.GetType();
								if (useSubTypes && entity.GetSubType() != null)
								{
									tag += "-" + entity.GetSubType();
								}
								if (useBIO)
								{
									if (i == entity.GetHeadTokenStart())
									{
										tag = "B-" + tag;
									}
									else
									{
										tag = "I-" + tag;
									}
								}
								labels[i].Set(typeof(CoreAnnotations.AnswerAnnotation), tag);
							}
						}
					}
				}
			}
			/*
			// Displaying the CoreLabels generated for this sentence
			log.info("sentence to core labels:");
			for(CoreLabel l: labels){
			log.info(" " + l.word() + "/" + l.getString(CoreAnnotations.PartOfSpeechAnnotation.class));
			String tag = l.getString(CoreAnnotations.AnswerAnnotation.class);
			if(tag != null && ! tag.equals(SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL)){
			log.info("/" + tag);
			}
			}
			log.info();
			*/
			return labels;
		}

		public static ICoreMap GetSentence(ICoreMap dataset, int i)
		{
			return dataset.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
		}

		public static int SentenceCount(ICoreMap dataset)
		{
			IList<ICoreMap> sents = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sents != null)
			{
				return sents.Count;
			}
			return 0;
		}

		public static void AddSentence(ICoreMap dataset, ICoreMap sentence)
		{
			IList<ICoreMap> sents = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sents == null)
			{
				sents = new List<ICoreMap>();
				dataset.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			}
			sents.Add(sentence);
		}

		public static void AddSentences(ICoreMap dataset, IList<ICoreMap> sentences)
		{
			IList<ICoreMap> sents = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sents == null)
			{
				sents = new List<ICoreMap>();
				dataset.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			}
			foreach (ICoreMap sentence in sentences)
			{
				sents.Add(sentence);
			}
		}

		/// <summary>Creates a deep copy of the given dataset with new lists for all mentions (entity, relation, event)</summary>
		/// <param name="dataset"/>
		public static Annotation DeepMentionCopy(ICoreMap dataset)
		{
			Annotation newDataset = new Annotation(string.Empty);
			IList<ICoreMap> sents = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<ICoreMap> newSents = new List<ICoreMap>();
			if (sents != null)
			{
				foreach (ICoreMap sent in sents)
				{
					if (!(sent is Annotation))
					{
						throw new Exception("ERROR: Sentences must instantiate Annotation!");
					}
					ICoreMap newSent = SentenceDeepMentionCopy((Annotation)sent);
					newSents.Add(newSent);
				}
			}
			AddSentences(newDataset, newSents);
			return newDataset;
		}

		/// <summary>Deep copy of the sentence: we create new entity/relation/event lists here.</summary>
		/// <remarks>
		/// Deep copy of the sentence: we create new entity/relation/event lists here.
		/// However,  we do not deep copy the ExtractionObjects themselves!
		/// </remarks>
		/// <param name="sentence"/>
		public static Annotation SentenceDeepMentionCopy(Annotation sentence)
		{
			Annotation newSent = new Annotation(sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
			newSent.Set(typeof(CoreAnnotations.TokensAnnotation), sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
			newSent.Set(typeof(TreeCoreAnnotations.TreeAnnotation), sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
			newSent.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation)));
			newSent.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)));
			newSent.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation)));
			newSent.Set(typeof(CoreAnnotations.DocIDAnnotation), sentence.Get(typeof(CoreAnnotations.DocIDAnnotation)));
			// deep copy of all mentions lists
			IList<EntityMention> ents = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			if (ents != null)
			{
				newSent.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), new List<EntityMention>(ents));
			}
			IList<RelationMention> rels = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			if (rels != null)
			{
				newSent.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), new List<RelationMention>(rels));
			}
			IList<EventMention> evs = sentence.Get(typeof(MachineReadingAnnotations.EventMentionsAnnotation));
			if (evs != null)
			{
				newSent.Set(typeof(MachineReadingAnnotations.EventMentionsAnnotation), new List<EventMention>(evs));
			}
			return newSent;
		}

		/// <summary>Return the relation that holds between the given entities.</summary>
		/// <remarks>
		/// Return the relation that holds between the given entities.
		/// Return a relation of type UNRELATED if this sentence contains no relation between the entities.
		/// </remarks>
		public static RelationMention GetRelation(RelationMentionFactory factory, ICoreMap sentence, params ExtractionObject[] args)
		{
			return GetRelations(factory, sentence, args)[0];
		}

		/// <summary>Return all the relations that holds between the given entities.</summary>
		/// <remarks>
		/// Return all the relations that holds between the given entities.
		/// Returns a list containing a relation of type UNRELATED if this sentence contains no relation between the entities.
		/// </remarks>
		public static IList<RelationMention> GetRelations(RelationMentionFactory factory, ICoreMap sentence, params ExtractionObject[] args)
		{
			IList<RelationMention> relationMentions = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			IList<RelationMention> matchingRelationMentions = new List<RelationMention>();
			if (relationMentions != null)
			{
				foreach (RelationMention rel in relationMentions)
				{
					if (rel.ArgsMatch(args))
					{
						matchingRelationMentions.Add(rel);
					}
				}
			}
			if (matchingRelationMentions.Count == 0)
			{
				matchingRelationMentions.Add(RelationMention.CreateUnrelatedRelation(factory, args));
			}
			return matchingRelationMentions;
		}

		/// <summary>
		/// Get list of all relations and non-relations between EntityMentions in this sentence
		/// Use with care.
		/// </summary>
		/// <remarks>
		/// Get list of all relations and non-relations between EntityMentions in this sentence
		/// Use with care. This is an expensive call due to getAllUnrelatedRelations, which creates all non-existing relations between all entity mentions
		/// </remarks>
		public static IList<RelationMention> GetAllRelations(RelationMentionFactory factory, ICoreMap sentence, bool createUnrelatedRelations)
		{
			IList<RelationMention> relationMentions = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			IList<RelationMention> allRelations = new List<RelationMention>();
			if (relationMentions != null)
			{
				Sharpen.Collections.AddAll(allRelations, relationMentions);
			}
			if (createUnrelatedRelations)
			{
				Sharpen.Collections.AddAll(allRelations, GetAllUnrelatedRelations(factory, sentence, true));
			}
			return allRelations;
		}

		public static IList<RelationMention> GetAllUnrelatedRelations(RelationMentionFactory factory, ICoreMap sentence, bool checkExisting)
		{
			IList<RelationMention> relationMentions = (checkExisting ? sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation)) : null);
			IList<EntityMention> entityMentions = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			IList<RelationMention> nonRelations = new List<RelationMention>();
			//
			// scan all possible arguments
			//
			if (entityMentions != null)
			{
				for (int i = 0; i < entityMentions.Count; i++)
				{
					for (int j = 0; j < entityMentions.Count; j++)
					{
						if (i == j)
						{
							continue;
						}
						EntityMention arg1 = entityMentions[i];
						EntityMention arg2 = entityMentions[j];
						bool match = false;
						if (relationMentions != null)
						{
							foreach (RelationMention rel in relationMentions)
							{
								if (rel.ArgsMatch(arg1, arg2))
								{
									match = true;
									break;
								}
							}
						}
						if (match == false)
						{
							nonRelations.Add(RelationMention.CreateUnrelatedRelation(factory, arg1, arg2));
						}
					}
				}
			}
			return nonRelations;
		}

		public static void AddEntityMention(ICoreMap sentence, EntityMention arg)
		{
			IList<EntityMention> l = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			if (l == null)
			{
				l = new List<EntityMention>();
				sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), l);
			}
			l.Add(arg);
		}

		public static void AddEntityMentions(ICoreMap sentence, ICollection<EntityMention> args)
		{
			IList<EntityMention> l = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			if (l == null)
			{
				l = new List<EntityMention>();
				sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), l);
			}
			Sharpen.Collections.AddAll(l, args);
		}

		public virtual IList<EntityMention> GetEntityMentions(ICoreMap sent)
		{
			return Java.Util.Collections.UnmodifiableList(sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)));
		}

		public static void AddRelationMention(ICoreMap sentence, RelationMention arg)
		{
			IList<RelationMention> l = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			if (l == null)
			{
				l = new List<RelationMention>();
				sentence.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), l);
			}
			l.Add(arg);
		}

		public static void AddRelationMentions(ICoreMap sentence, ICollection<RelationMention> args)
		{
			IList<RelationMention> l = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			if (l == null)
			{
				l = new List<RelationMention>();
				sentence.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), l);
			}
			Sharpen.Collections.AddAll(l, args);
		}

		public virtual IList<RelationMention> GetRelationMentions(ICoreMap sent)
		{
			return Java.Util.Collections.UnmodifiableList(sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation)));
		}

		public static void AddEventMention(ICoreMap sentence, EventMention arg)
		{
			IList<EventMention> l = sentence.Get(typeof(MachineReadingAnnotations.EventMentionsAnnotation));
			if (l == null)
			{
				l = new List<EventMention>();
				sentence.Set(typeof(MachineReadingAnnotations.EventMentionsAnnotation), l);
			}
			l.Add(arg);
		}

		public static void AddEventMentions(ICoreMap sentence, ICollection<EventMention> args)
		{
			IList<EventMention> l = sentence.Get(typeof(MachineReadingAnnotations.EventMentionsAnnotation));
			if (l == null)
			{
				l = new List<EventMention>();
				sentence.Set(typeof(MachineReadingAnnotations.EventMentionsAnnotation), l);
			}
			Sharpen.Collections.AddAll(l, args);
		}

		public virtual IList<EventMention> GetEventMentions(ICoreMap sent)
		{
			return Java.Util.Collections.UnmodifiableList(sent.Get(typeof(MachineReadingAnnotations.EventMentionsAnnotation)));
		}

		/// <summary>Prepare a string for printing in a spreadsheet for Mechanical Turk input.</summary>
		/// <param name="s">String to be formatted</param>
		/// <returns>String string enclosed in quotes with other quotes escaped, and with better formatting for readability by Turkers.</returns>
		public static string Prettify(string s)
		{
			if (s == null)
			{
				return string.Empty;
			}
			return s.Replace(" ,", ",").Replace(" .", ".").Replace(" :", ":").Replace("( ", "(").Replace("[ ", "[").Replace(" )", ")").Replace(" ]", "]").Replace(" - ", "-").Replace(" '", "'").Replace("-LRB- ", "(").Replace(" -RRB-", ")").Replace("` ` "
				, "\"").Replace(" ' '", "\"").Replace(" COMMA", ",");
		}

		/// <summary>Fetches the sentence text in a given token span</summary>
		/// <param name="span"/>
		public static string GetTextContent(ICoreMap sent, Span span)
		{
			IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
			StringBuilder buf = new StringBuilder();
			System.Diagnostics.Debug.Assert((span != null));
			for (int i = span.Start(); i < span.End(); i++)
			{
				if (i > span.Start())
				{
					buf.Append(" ");
				}
				buf.Append(tokens[i].Word());
			}
			return buf.ToString();
		}

		public static string SentenceToString(ICoreMap sent)
		{
			StringBuilder sb = new StringBuilder(512);
			IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
			sb.Append("\"" + StringUtils.Join(tokens, " ") + "\"");
			sb.Append("\n");
			IList<RelationMention> relationMentions = sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
			if (relationMentions != null)
			{
				foreach (RelationMention rel in relationMentions)
				{
					sb.Append("\n");
					sb.Append(rel);
				}
			}
			// TODO: add entity and event mentions
			return sb.ToString();
		}

		public static string TokensAndNELabelsToString(ICoreMap sentence)
		{
			StringBuilder os = new StringBuilder();
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tokens != null)
			{
				bool first = true;
				foreach (CoreLabel token in tokens)
				{
					if (!first)
					{
						os.Append(" ");
					}
					os.Append(token.Word());
					if (token.Ner() != null && !token.Ner().Equals("O"))
					{
						os.Append("/" + token.Ner());
					}
					first = false;
				}
			}
			return os.ToString();
		}

		public static string DatasetToString(ICoreMap dataset)
		{
			IList<ICoreMap> sents = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			StringBuilder b = new StringBuilder();
			if (sents != null)
			{
				foreach (ICoreMap sent in sents)
				{
					b.Append(SentenceToString(sent));
				}
			}
			return b.ToString();
		}

		/*
		public static List<CoreLabel> wordsToCoreLabels(List<Word> words) {
		List<CoreLabel> labels = new ArrayList<CoreLabel>();
		for(Word word: words){
		CoreLabel l = new CoreLabel();
		l.setWord(word.word());
		l.set(CoreAnnotations.TextAnnotation.class, word.word());
		l.setBeginPosition(word.beginPosition());
		l.setEndPosition(word.endPosition());
		labels.add(l);
		}
		return labels;
		}
		*/
		public static string TokensToString(IList<CoreLabel> tokens)
		{
			StringBuilder os = new StringBuilder();
			bool first = true;
			foreach (CoreLabel t in tokens)
			{
				if (!first)
				{
					os.Append(" ");
				}
				os.Append(t.Word() + "{" + t.BeginPosition() + ", " + t.EndPosition() + "}");
				first = false;
			}
			return os.ToString();
		}

		/*
		public static boolean sentenceContainsSpan(CoreMap sentence, Span span) {
		List<CoreLabel> tokens = sentence.get(CoreAnnotations.TokensAnnotation.class);
		int sentenceStart = tokens.get(0).beginPosition();
		int sentenceEnd = tokens.get(tokens.size() - 1).endPosition();
		return sentenceStart <= span.start() && sentenceEnd >= span.end();
		}
		*/
		/*
		* Shift the character offsets of all tokens by offset.
		*/
		public static void UpdateOffsets(IList<Word> tokens, int offset)
		{
			foreach (Word l in tokens)
			{
				l.SetBeginPosition(l.BeginPosition() + offset);
				l.SetEndPosition(l.EndPosition() + offset);
			}
		}

		/*
		* Shift the character offsets of all tokens by offset.
		*/
		public static void UpdateOffsetsInCoreLabels(IList<CoreLabel> tokens, int offset)
		{
			foreach (CoreLabel l in tokens)
			{
				l.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), l.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) + offset);
				l.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), l.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) + offset);
			}
		}

		/// <summary>Process string to be a cell in Excel file.</summary>
		/// <remarks>
		/// Process string to be a cell in Excel file.
		/// Escape any quotes in the string and enclose the whole string with quotes.
		/// </remarks>
		public static string Excelify(string s)
		{
			return '"' + s.Replace("\"", "\"\"") + '"';
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static IList<ICoreMap> ReadSentencesFromFile(string path)
		{
			Annotation doc = (Annotation)IOUtils.ReadObjectFromFile(path);
			return doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
		}
	}
}
