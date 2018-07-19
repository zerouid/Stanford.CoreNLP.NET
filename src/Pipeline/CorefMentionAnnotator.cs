using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.MD;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class adds mention information to an Annotation.</summary>
	/// <remarks>
	/// This class adds mention information to an Annotation.
	/// After annotation each sentence will have a
	/// <c>List&lt;Mention&gt;</c>
	/// representing the Mentions in the sentence.
	/// The
	/// <c>List&lt;Mention&gt;</c>
	/// containing the Mentions will be put under the annotation
	/// <see cref="Edu.Stanford.Nlp.Coref.CorefCoreAnnotations.CorefMentionsAnnotation"/>
	/// .
	/// </remarks>
	/// <author>heeyoung</author>
	/// <author>Jason Bolton</author>
	public class CorefMentionAnnotator : TextAnnotationCreator, IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.CorefMentionAnnotator));

		private IHeadFinder headFinder;

		private CorefMentionFinder md;

		private string mdName;

		private Dictionaries dictionaries;

		private Properties corefProperties;

		private readonly ICollection<Type> mentionAnnotatorRequirements = new HashSet<Type>();

		public CorefMentionAnnotator(Properties props)
		{
			try
			{
				corefProperties = props;
				//System.out.println("corefProperties: "+corefProperties);
				dictionaries = new Dictionaries(props);
				//System.out.println("got dictionaries");
				headFinder = CorefProperties.GetHeadFinder(props);
				//System.out.println("got head finder");
				md = GetMentionFinder(props, headFinder);
				log.Info("Using mention detector type: " + mdName);
				Sharpen.Collections.AddAll(mentionAnnotatorRequirements, Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation
					), typeof(CoreAnnotations.EntityTypeAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(
					SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)));
			}
			catch (Exception e)
			{
				log.Info("Error with building coref mention annotator!");
				log.Info(e);
			}
		}

		/// <summary>
		/// Return true if the coref mention synchs with the entity mention
		/// For instance the em "Joe Smith" synchs with "Joe Smith", "Joe Smith's" and "President Joe Smith's"
		/// It does not synch with "Joe Smith's car" or "President Joe"
		/// </summary>
		/// <param name="cm">the coref mention</param>
		/// <param name="em">the entity mention</param>
		/// <returns>true if the coref mention and entity mention synch</returns>
		public static bool SynchCorefMentionEntityMention(Annotation ann, Mention cm, ICoreMap em)
		{
			int currCMTokenIndex = 0;
			int tokenOverlapCount = 0;
			// get cm tokens
			ICoreMap cmSentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[cm.sentNum];
			IList<CoreLabel> cmTokens = cmSentence.Get(typeof(CoreAnnotations.TokensAnnotation)).SubList(cm.startIndex, cm.endIndex);
			// if trying to synch with a PERSON entity mention, ignore leading TITLE tokens
			if (em.Get(typeof(CoreAnnotations.EntityTypeAnnotation)).Equals("PERSON"))
			{
				while (currCMTokenIndex < cmTokens.Count && cmTokens[currCMTokenIndex].Ner().Equals("TITLE"))
				{
					currCMTokenIndex++;
				}
			}
			// get em tokens
			int currEMTokenIndex = 0;
			IList<CoreLabel> emTokens = em.Get(typeof(CoreAnnotations.TokensAnnotation));
			// search for token mismatch
			while (currEMTokenIndex < emTokens.Count && currCMTokenIndex < cmTokens.Count)
			{
				// if a token mismatch is found, return false
				if (!(emTokens[currEMTokenIndex] == cmTokens[currCMTokenIndex]))
				{
					return false;
				}
				currCMTokenIndex++;
				currEMTokenIndex++;
				tokenOverlapCount += 1;
			}
			// finally allow for a trailing "'s"
			if (currCMTokenIndex < cmTokens.Count && cmTokens[currCMTokenIndex].Word().Equals("'s"))
			{
				currCMTokenIndex++;
			}
			// check that both em and cm tokens have been exhausted, check for token overlap, or return false
			if (currCMTokenIndex < cmTokens.Count || currEMTokenIndex < emTokens.Count || tokenOverlapCount == 0)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// TO DO: be careful, this could introduce a really hard to find bug
			// this is necessary for Chinese coreference
			// removeNested needs to be set to "false" for newswire text or big performance drop
			string docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			if (docID == null)
			{
				docID = string.Empty;
			}
			if (docID.Contains("nw") && (CorefProperties.Conll(corefProperties) || corefProperties.GetProperty("coref.input.type", "raw").Equals("conll")) && CorefProperties.GetLanguage(corefProperties) == Locale.Chinese && PropertiesUtils.GetBool(corefProperties
				, "coref.specialCaseNewswire"))
			{
				corefProperties.SetProperty("removeNestedMentions", "false");
			}
			else
			{
				corefProperties.SetProperty("removeNestedMentions", "true");
			}
			IList<IList<Mention>> mentions = md.FindMentions(annotation, dictionaries, corefProperties);
			// build list of coref mentions in this document
			annotation.Set(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), new List<Mention>());
			// initialize indexes
			int mentionIndex = 0;
			int currIndex = 0;
			// initialize each token with an empty set of corresponding coref mention id's
			foreach (CoreLabel token in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				token.Set(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation), new ArraySet<int>());
			}
			foreach (ICoreMap sentence in sentences)
			{
				IList<Mention> mentionsForThisSentence = mentions[currIndex];
				sentence.Set(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), mentionsForThisSentence);
				Sharpen.Collections.AddAll(annotation.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)), mentionsForThisSentence);
				// set sentNum correctly for each coref mention
				foreach (Mention corefMention in mentionsForThisSentence)
				{
					corefMention.sentNum = currIndex;
				}
				// increment to next list of mentions
				currIndex++;
				// assign latest mentionID, annotate tokens with coref mention info
				foreach (Mention m in mentionsForThisSentence)
				{
					m.mentionID = mentionIndex;
					// go through all the tokens corresponding to this coref mention
					// annotate them with the index into the document wide coref mention list
					for (int corefMentionTokenIndex = m.startIndex; corefMentionTokenIndex < m.endIndex; corefMentionTokenIndex++)
					{
						CoreLabel currToken = sentence.Get(typeof(CoreAnnotations.TokensAnnotation))[corefMentionTokenIndex];
						currToken.Get(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation)).Add(mentionIndex);
					}
					mentionIndex++;
				}
			}
			// synch coref mentions to entity mentions
			Dictionary<int, int> corefMentionToEntityMentionMapping = new Dictionary<int, int>();
			Dictionary<int, int> entityMentionToCorefMentionMapping = new Dictionary<int, int>();
			foreach (CoreLabel token_1 in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (token_1.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation)) != null)
				{
					int tokenEntityMentionIndex = token_1.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation));
					ICoreMap tokenEntityMention = annotation.Get(typeof(CoreAnnotations.MentionsAnnotation))[tokenEntityMentionIndex];
					foreach (int candidateCorefMentionIndex in token_1.Get(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation)))
					{
						Mention candidateTokenCorefMention = annotation.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation))[candidateCorefMentionIndex];
						if (SynchCorefMentionEntityMention(annotation, candidateTokenCorefMention, tokenEntityMention))
						{
							entityMentionToCorefMentionMapping[tokenEntityMentionIndex] = candidateCorefMentionIndex;
							corefMentionToEntityMentionMapping[candidateCorefMentionIndex] = tokenEntityMentionIndex;
						}
					}
				}
			}
			// store mappings between entity mentions and coref mentions in annotation
			annotation.Set(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation), corefMentionToEntityMentionMapping);
			annotation.Set(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation), entityMentionToCorefMentionMapping);
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		private CorefMentionFinder GetMentionFinder(Properties props, IHeadFinder headFinder)
		{
			switch (CorefProperties.MdType(props))
			{
				case CorefProperties.MentionDetectionType.Dependency:
				{
					mdName = "dependency";
					return new DependencyCorefMentionFinder(props);
				}

				case CorefProperties.MentionDetectionType.Hybrid:
				{
					mdName = "hybrid";
					mentionAnnotatorRequirements.Add(typeof(TreeCoreAnnotations.TreeAnnotation));
					mentionAnnotatorRequirements.Add(typeof(CoreAnnotations.BeginIndexAnnotation));
					mentionAnnotatorRequirements.Add(typeof(CoreAnnotations.EndIndexAnnotation));
					return new HybridCorefMentionFinder(headFinder, props);
				}

				case CorefProperties.MentionDetectionType.Rule:
				default:
				{
					mentionAnnotatorRequirements.Add(typeof(TreeCoreAnnotations.TreeAnnotation));
					mentionAnnotatorRequirements.Add(typeof(CoreAnnotations.BeginIndexAnnotation));
					mentionAnnotatorRequirements.Add(typeof(CoreAnnotations.EndIndexAnnotation));
					mdName = "rule";
					return new RuleBasedCorefMentionFinder(headFinder, props);
				}
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return mentionAnnotatorRequirements;
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), typeof(CoreAnnotations.ParagraphAnnotation), typeof(CoreAnnotations.SpeakerAnnotation), typeof(CoreAnnotations.UtteranceAnnotation
				))));
		}
	}
}
