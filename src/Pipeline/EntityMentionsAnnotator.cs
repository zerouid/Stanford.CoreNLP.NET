using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Annotator that marks entity mentions in a document.</summary>
	/// <remarks>
	/// Annotator that marks entity mentions in a document.
	/// Entity mentions are:
	/// <ul>
	/// <li> Named entities (identified by NER) </li>
	/// <li> Quantifiable entities
	/// <ul>
	/// <li> Times (identified by TimeAnnotator) </li>
	/// <li> Measurements (identified by ???) </li>
	/// </ul>
	/// </li>
	/// </ul>
	/// Each sentence is annotated with a list of the mentions
	/// (MentionsAnnotation is a list of CoreMap).
	/// </remarks>
	/// <author>Angel Chang</author>
	public class EntityMentionsAnnotator : IAnnotator
	{
		private readonly LabeledChunkIdentifier chunkIdentifier;

		/// <summary>
		/// If true, heuristically search for organization acronyms, even if they are not marked
		/// explicitly by an NER tag.
		/// </summary>
		/// <remarks>
		/// If true, heuristically search for organization acronyms, even if they are not marked
		/// explicitly by an NER tag.
		/// This is super useful (+20% recall) for KBP.
		/// </remarks>
		private readonly bool doAcronyms;

		private LanguageInfo.HumanLanguage entityMentionsLanguage;

		public static PropertiesUtils.Property[] SupportedProperties = new PropertiesUtils.Property[] {  };

		/// <summary>The CoreAnnotation keys to use for this entity mentions annotator.</summary>
		private Type nerCoreAnnotationClass = typeof(CoreAnnotations.NamedEntityTagAnnotation);

		private Type nerNormalizedCoreAnnotationClass = typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation);

		private Type mentionsCoreAnnotationClass = typeof(CoreAnnotations.MentionsAnnotation);

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.EntityMentionsAnnotator));

		public EntityMentionsAnnotator()
		{
			// Currently relies on NER annotations being okay
			// - Replace with calling NER classifiers and timeAnnotator directly
			// TODO: Provide properties
			// defaults
			chunkIdentifier = new LabeledChunkIdentifier();
			doAcronyms = false;
		}

		public EntityMentionsAnnotator(string name, Properties props)
		{
			// note: used in annotate.properties
			// if the user has supplied custom CoreAnnotations for the ner tags and entity mentions override the default keys
			try
			{
				if (props.Contains(name + ".nerCoreAnnotation"))
				{
					nerCoreAnnotationClass = (Type)Sharpen.Runtime.GetType(props.GetProperty(name + ".nerCoreAnnotation"));
				}
				if (props.Contains(name + ".nerNormalizedCoreAnnotation"))
				{
					nerNormalizedCoreAnnotationClass = (Type)Sharpen.Runtime.GetType(props.GetProperty(name + ".nerNormalizedCoreAnnotation"));
				}
				if (props.Contains(name + ".mentionsCoreAnnotation"))
				{
					mentionsCoreAnnotationClass = (Type)Sharpen.Runtime.GetType(props.GetProperty(name + ".mentionsCoreAnnotation"));
				}
			}
			catch (TypeLoadException e)
			{
				log.Error(e.Message);
			}
			chunkIdentifier = new LabeledChunkIdentifier();
			doAcronyms = bool.ParseBoolean(props.GetProperty(name + ".acronyms", props.GetProperty("acronyms", "false")));
			// set up language info, this is needed for handling creating pronominal mentions
			entityMentionsLanguage = LanguageInfo.GetLanguageFromString(props.GetProperty(name + ".language", "en"));
		}

		private static IList<CoreLabel> TokensForCharacters(IList<CoreLabel> tokens, int charBegin, int charEnd)
		{
			System.Diagnostics.Debug.Assert(charBegin >= 0);
			IList<CoreLabel> segment = Generics.NewArrayList();
			foreach (CoreLabel token in tokens)
			{
				if (token.EndPosition() < charBegin || token.BeginPosition() >= charEnd)
				{
					continue;
				}
				segment.Add(token);
			}
			return segment;
		}

		private readonly IPredicate<Pair<CoreLabel, CoreLabel>> IsTokensCompatible = null;

		// First argument is the current token
		// Second argument the previous token
		// Get NormalizedNamedEntityTag and say two entities are incompatible if they are different
		// This duplicates logic in the QuantifiableEntityNormalizer (but maybe we will get rid of that class)
		// Get NumericCompositeValueAnnotation and say two entities are incompatible if they are different
		// Check timex...
		private static Optional<ICoreMap> OverlapsWithMention(ICoreMap needle, IList<ICoreMap> haystack)
		{
			IList<CoreLabel> tokens = needle.Get(typeof(CoreAnnotations.TokensAnnotation));
			int charBegin = tokens[0].BeginPosition();
			int charEnd = tokens[tokens.Count - 1].EndPosition();
			return (haystack.Stream().Filter(null).FindFirst());
		}

		// Check overlap
		/// <summary>Returns whether the given token counts as a valid pronominal mention for KBP.</summary>
		/// <remarks>
		/// Returns whether the given token counts as a valid pronominal mention for KBP.
		/// This method (at present) works for either Chinese or English.
		/// </remarks>
		/// <param name="word">The token to classify.</param>
		/// <returns>true if this token is a pronoun that KBP should recognize.</returns>
		private static bool KbpIsPronominalMention(CoreLabel word)
		{
			return WordLists.IsKbpPronominalMention(word.Word());
		}

		/// <summary>Annotate all the pronominal mentions in the document.</summary>
		/// <param name="ann">The document.</param>
		/// <returns>The list of pronominal mentions in the document.</returns>
		private static IList<ICoreMap> AnnotatePronominalMentions(Annotation ann)
		{
			IList<ICoreMap> pronouns = new List<ICoreMap>();
			IList<ICoreMap> sentences = ann.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int sentenceIndex = 0; sentenceIndex < sentences.Count; sentenceIndex++)
			{
				ICoreMap sentence = sentences[sentenceIndex];
				int annoTokenBegin = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				if (annoTokenBegin == null)
				{
					annoTokenBegin = 0;
				}
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
				{
					CoreLabel token = tokens[tokenIndex];
					if (KbpIsPronominalMention(token))
					{
						ICoreMap pronoun = ChunkAnnotationUtils.GetAnnotatedChunk(tokens, tokenIndex, tokenIndex + 1, annoTokenBegin, null, typeof(CoreAnnotations.TextAnnotation), null);
						pronoun.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
						pronoun.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), KBPRelationExtractor.NERTag.Person.name);
						pronoun.Set(typeof(CoreAnnotations.EntityTypeAnnotation), KBPRelationExtractor.NERTag.Person.name);
						// set gender
						string pronounGender = null;
						if (pronoun.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower().Equals("she"))
						{
							pronounGender = "FEMALE";
							pronoun.Set(typeof(CoreAnnotations.GenderAnnotation), pronounGender);
						}
						else
						{
							if (pronoun.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower().Equals("he"))
							{
								pronounGender = "MALE";
								pronoun.Set(typeof(CoreAnnotations.GenderAnnotation), pronounGender);
							}
						}
						if (pronounGender != null)
						{
							foreach (CoreLabel pronounToken in pronoun.Get(typeof(CoreAnnotations.TokensAnnotation)))
							{
								pronounToken.Set(typeof(CoreAnnotations.GenderAnnotation), pronounGender);
							}
						}
						sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)).Add(pronoun);
						pronouns.Add(pronoun);
					}
				}
			}
			return pronouns;
		}

		public virtual void Annotate(Annotation annotation)
		{
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int sentenceIndex = 0;
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				int annoTokenBegin = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				if (annoTokenBegin == null)
				{
					annoTokenBegin = 0;
				}
				IList<ICoreMap> chunks = chunkIdentifier.GetAnnotatedChunks(tokens, annoTokenBegin, typeof(CoreAnnotations.TextAnnotation), nerCoreAnnotationClass, IsTokensCompatible);
				sentence.Set(mentionsCoreAnnotationClass, chunks);
				// By now entity mentions have been annotated and TextAnnotation and NamedEntityAnnotation marked
				// Some additional annotations
				IList<ICoreMap> mentions = sentence.Get(mentionsCoreAnnotationClass);
				if (mentions != null)
				{
					foreach (ICoreMap mention in mentions)
					{
						IList<CoreLabel> mentionTokens = mention.Get(typeof(CoreAnnotations.TokensAnnotation));
						string name = (string)CoreMapAttributeAggregator.FirstNonNil.Aggregate(nerNormalizedCoreAnnotationClass, mentionTokens);
						if (name == null)
						{
							name = mention.Get(typeof(CoreAnnotations.TextAnnotation));
						}
						else
						{
							mention.Set(nerNormalizedCoreAnnotationClass, name);
						}
						//mention.set(CoreAnnotations.EntityNameAnnotation.class, name);
						string type = mention.Get(nerCoreAnnotationClass);
						mention.Set(typeof(CoreAnnotations.EntityTypeAnnotation), type);
						// set sentence index annotation for mention
						mention.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
						// Take first non nil as timex for the mention
						Timex timex = (Timex)CoreMapAttributeAggregator.FirstNonNil.Aggregate(typeof(TimeAnnotations.TimexAnnotation), mentionTokens);
						if (timex != null)
						{
							mention.Set(typeof(TimeAnnotations.TimexAnnotation), timex);
						}
						// Set the entity link from the tokens
						if (mention.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)) == null)
						{
							foreach (CoreLabel token in mentionTokens)
							{
								if ((mention.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)) == null || "O".Equals(mention.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)))) && (token.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)) != null && !"O".Equals
									(token.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)))))
								{
									mention.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), token.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)));
								}
							}
						}
					}
				}
				sentenceIndex++;
			}
			// Post-process with acronyms
			if (doAcronyms)
			{
				AddAcronyms(annotation);
			}
			// Post-process add in KBP pronominal mentions, (English only for now)
			if (entityMentionsLanguage.Equals(LanguageInfo.HumanLanguage.English))
			{
				AnnotatePronominalMentions(annotation);
			}
			// build document wide entity mentions list
			IList<ICoreMap> allEntityMentions = new List<ICoreMap>();
			int entityMentionIndex = 0;
			foreach (ICoreMap sentence_1 in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (ICoreMap entityMention in sentence_1.Get(typeof(CoreAnnotations.MentionsAnnotation)))
				{
					entityMention.Set(typeof(CoreAnnotations.EntityMentionIndexAnnotation), entityMentionIndex);
					entityMention.Set(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation), entityMentionIndex);
					foreach (CoreLabel entityMentionToken in entityMention.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						entityMentionToken.Set(typeof(CoreAnnotations.EntityMentionIndexAnnotation), entityMentionIndex);
					}
					allEntityMentions.Add(entityMention);
					entityMentionIndex++;
				}
			}
			annotation.Set(mentionsCoreAnnotationClass, allEntityMentions);
		}

		private void AddAcronyms(Annotation ann)
		{
			// Find all the organizations in a document
			IList<ICoreMap> allMentionsSoFar = new List<ICoreMap>();
			foreach (ICoreMap sentence in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				Sharpen.Collections.AddAll(allMentionsSoFar, sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)));
			}
			IList<IList<CoreLabel>> organizations = new List<IList<CoreLabel>>();
			foreach (ICoreMap mention in allMentionsSoFar)
			{
				if ("ORGANIZATION".Equals(mention.Get(nerCoreAnnotationClass)))
				{
					organizations.Add(mention.Get(typeof(CoreAnnotations.TokensAnnotation)));
				}
			}
			// Skip very long documents
			if (organizations.Count > 100)
			{
				return;
			}
			// Iterate over tokens...
			foreach (ICoreMap sentence_1 in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<ICoreMap> sentenceMentions = new List<ICoreMap>();
				IList<CoreLabel> tokens = sentence_1.Get(typeof(CoreAnnotations.TokensAnnotation));
				int totalTokensOffset = sentence_1.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				for (int i = 0; i < tokens.Count; ++i)
				{
					// ... that look like they might be an acronym and are not already a mention
					CoreLabel token = tokens[i];
					if ("O".Equals(token.Ner()) && token.Word().ToUpper().Equals(token.Word()) && token.Word().Length >= 3)
					{
						foreach (IList<CoreLabel> org in organizations)
						{
							// ... and actually are an acronym
							if (AcronymMatcher.IsAcronym(token.Word(), org))
							{
								// ... and add them.
								// System.out.println("found ACRONYM ORG");
								token.SetNER("ORGANIZATION");
								ICoreMap chunk = ChunkAnnotationUtils.GetAnnotatedChunk(tokens, i, i + 1, totalTokensOffset, null, null, null);
								chunk.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "ORGANIZATION");
								sentenceMentions.Add(chunk);
							}
						}
					}
				}
			}
		}

		public virtual ICollection<Type> Requires()
		{
			//TODO(jb) for now not fully enforcing pipeline if user customizes keys
			if (!nerCoreAnnotationClass.GetCanonicalName().Equals(typeof(CoreAnnotations.NamedEntityTagAnnotation).GetCanonicalName()))
			{
				return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation))));
			}
			else
			{
				return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation))));
			}
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(mentionsCoreAnnotationClass);
		}
	}
}
