using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class adds coref information to an Annotation.</summary>
	/// <remarks>
	/// This class adds coref information to an Annotation.
	/// A Map from id to CorefChain is put under the annotation
	/// <see cref="Edu.Stanford.Nlp.Coref.CorefCoreAnnotations.CorefChainAnnotation"/>
	/// .
	/// </remarks>
	/// <author>heeyoung</author>
	/// <author>Jason Bolton</author>
	public class CorefAnnotator : TextAnnotationCreator, IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.CorefAnnotator));

		private readonly CorefSystem corefSystem;

		private bool performMentionDetection;

		private CorefMentionAnnotator mentionAnnotator;

		private readonly Properties props;

		public CorefAnnotator(Properties props)
		{
			this.props = props;
			try
			{
				// if user tries to run with coref.language = ENGLISH and coref.algorithm = hybrid, throw Exception
				// we do not support those settings at this time
				if (CorefProperties.Algorithm(props).Equals(CorefProperties.CorefAlgorithmType.Hybrid) && CorefProperties.GetLanguage(props).Equals(Locale.English))
				{
					log.Error("Error: coref.algorithm=hybrid is not supported for English, " + "please change coref.algorithm or coref.language");
					throw new Exception();
				}
				// suppress
				props.SetProperty("coref.printConLLLoadingMessage", "false");
				corefSystem = new CorefSystem(props);
				props.Remove("coref.printConLLLoadingMessage");
			}
			catch (Exception e)
			{
				log.Error("Error creating CorefAnnotator...terminating pipeline construction!");
				log.Error(e);
				throw new Exception(e);
			}
			// unless custom mention detection is set, just use the default coref mention detector
			performMentionDetection = !PropertiesUtils.GetBool(props, "coref.useCustomMentionDetection", false);
			if (performMentionDetection)
			{
				mentionAnnotator = new CorefMentionAnnotator(props);
			}
		}

		// flip which granularity of ner tag is primary
		public virtual void SetNamedEntityTagGranularity(Annotation annotation, string granularity)
		{
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			Type sourceNERTagClass;
			if (granularity.Equals("fine"))
			{
				sourceNERTagClass = typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation);
			}
			else
			{
				if (granularity.Equals("coarse"))
				{
					sourceNERTagClass = typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation);
				}
				else
				{
					sourceNERTagClass = typeof(CoreAnnotations.NamedEntityTagAnnotation);
				}
			}
			// switch tags
			foreach (CoreLabel token in tokens)
			{
				if (token.Get(sourceNERTagClass) != null && !token.Get(sourceNERTagClass).Equals(string.Empty))
				{
					token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), token.Get(sourceNERTagClass));
				}
			}
		}

		/// <summary>
		/// helper method to find the longest entity mention that is coreferent to an entity mention
		/// after coref has been run...match an entity mention to a coref mention, go through all of
		/// the coref mentions and find the one with the longest matching entity mention, return
		/// that entity mention
		/// </summary>
		/// <param name="em">the entity mention of interest</param>
		/// <param name="ann">the annotation, after coreference has been run</param>
		/// <returns/>
		public virtual Optional<ICoreMap> FindBestCoreferentEntityMention(ICoreMap em, Annotation ann)
		{
			// helper lambda
			IFunction<Optional<ICoreMap>, int> lengthOfOptionalEntityMention = null;
			// initialize return value as empty Optional
			Optional<ICoreMap> bestCoreferentEntityMention = Optional.Empty();
			// look for matching coref mention
			int entityMentionIndex = em.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation));
			Optional<int> matchingCorefMentionIndex = Optional.OfNullable(ann.Get(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation))[entityMentionIndex]);
			Optional<Mention> matchingCorefMention = matchingCorefMentionIndex.IsPresent() ? Optional.Of(ann.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation))[matchingCorefMentionIndex.Get()]) : Optional.Empty();
			// if there is a matching coref mention, look at all of the coref mentions in its coref chain
			if (matchingCorefMention.IsPresent())
			{
				Optional<CorefChain> matchingCorefChain = Optional.OfNullable(ann.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation))[matchingCorefMention.Get().corefClusterID]);
				IList<CorefChain.CorefMention> corefMentionsInTextualOrder = matchingCorefChain.IsPresent() ? matchingCorefChain.Get().GetMentionsInTextualOrder() : new List<CorefChain.CorefMention>();
				foreach (CorefChain.CorefMention cm in corefMentionsInTextualOrder)
				{
					Optional<int> candidateCoreferentEntityMentionIndex = Optional.OfNullable(ann.Get(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation))[cm.mentionID]);
					Optional<ICoreMap> candidateCoreferentEntityMention = candidateCoreferentEntityMentionIndex.IsPresent() ? Optional.OfNullable(ann.Get(typeof(CoreAnnotations.MentionsAnnotation))[candidateCoreferentEntityMentionIndex.Get()]) : Optional.Empty(
						);
					if (lengthOfOptionalEntityMention.Apply(candidateCoreferentEntityMention) > lengthOfOptionalEntityMention.Apply(bestCoreferentEntityMention))
					{
						bestCoreferentEntityMention = candidateCoreferentEntityMention;
					}
				}
			}
			return bestCoreferentEntityMention;
		}

		public virtual void Annotate(Annotation annotation)
		{
			// check if mention detection should be performed by this annotator
			if (performMentionDetection)
			{
				mentionAnnotator.Annotate(annotation);
			}
			// temporarily set the primary named entity tag to the coarse tag
			SetNamedEntityTagGranularity(annotation, "coarse");
			try
			{
				if (!annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					log.Error("this coreference resolution system requires SentencesAnnotation!");
					return;
				}
				if (HasSpeakerAnnotations(annotation))
				{
					annotation.Set(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation), true);
				}
				corefSystem.Annotate(annotation);
			}
			catch (Exception e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			finally
			{
				// restore to the fine-grained
				SetNamedEntityTagGranularity(annotation, "fine");
			}
			// attempt to link ner derived entity mentions to representative entity mentions
			foreach (ICoreMap entityMention in annotation.Get(typeof(CoreAnnotations.MentionsAnnotation)))
			{
				Optional<ICoreMap> bestCoreferentEntityMention = FindBestCoreferentEntityMention(entityMention, annotation);
				if (bestCoreferentEntityMention.IsPresent())
				{
					entityMention.Set(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation), bestCoreferentEntityMention.Get().Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation)));
				}
			}
		}

		public static IList<Pair<IntTuple, IntTuple>> GetLinks(IDictionary<int, CorefChain> result)
		{
			IList<Pair<IntTuple, IntTuple>> links = new List<Pair<IntTuple, IntTuple>>();
			CorefChain.CorefMentionComparator comparator = new CorefChain.CorefMentionComparator();
			foreach (CorefChain c in result.Values)
			{
				IList<CorefChain.CorefMention> s = c.GetMentionsInTextualOrder();
				foreach (CorefChain.CorefMention m1 in s)
				{
					foreach (CorefChain.CorefMention m2 in s)
					{
						if (comparator.Compare(m1, m2) == 1)
						{
							links.Add(new Pair<IntTuple, IntTuple>(m1.position, m2.position));
						}
					}
				}
			}
			return links;
		}

		private static bool HasSpeakerAnnotations(Annotation annotation)
		{
			foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel t in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					if (t.Get(typeof(CoreAnnotations.SpeakerAnnotation)) != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		public virtual ICollection<Type> Requires()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation
				), typeof(CoreAnnotations.NamedEntityTagAnnotation), typeof(CoreAnnotations.EntityTypeAnnotation), typeof(CoreAnnotations.MentionsAnnotation), typeof(CoreAnnotations.EntityMentionIndexAnnotation), typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation
				), typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)));
			if (CorefProperties.MdType(this.props) != CorefProperties.MentionDetectionType.Dependency)
			{
				requirements.Add(typeof(TreeCoreAnnotations.TreeAnnotation));
				requirements.Add(typeof(CoreAnnotations.CategoryAnnotation));
			}
			if (!performMentionDetection)
			{
				requirements.Add(typeof(CorefCoreAnnotations.CorefMentionsAnnotation));
			}
			return Java.Util.Collections.UnmodifiableSet(requirements);
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CorefCoreAnnotations.CorefChainAnnotation), typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation)));
			return requirements;
		}
	}
}
