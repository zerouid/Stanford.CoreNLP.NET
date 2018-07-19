using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Dcoref;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Implements the Annotator for the new deterministic coreference resolution system.</summary>
	/// <remarks>
	/// Implements the Annotator for the new deterministic coreference resolution system.
	/// In other words, this depends on: POSTaggerAnnotator, NERCombinerAnnotator (or equivalent), and ParserAnnotator.
	/// </remarks>
	/// <author>Mihai Surdeanu, based on the CorefAnnotator written by Marie-Catherine de Marneffe</author>
	public class DeterministicCorefAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.DeterministicCorefAnnotator));

		private const bool Verbose = false;

		private readonly MentionExtractor mentionExtractor;

		private readonly SieveCoreferenceSystem corefSystem;

		private bool performMentionDetection;

		private CorefMentionAnnotator mentionAnnotator;

		private readonly bool OldFormat;

		private readonly bool allowReparsing;

		public DeterministicCorefAnnotator(Properties props)
		{
			// for backward compatibility
			try
			{
				corefSystem = new SieveCoreferenceSystem(props);
				mentionExtractor = new MentionExtractor(corefSystem.Dictionaries(), corefSystem.Semantics());
				OldFormat = bool.ParseBoolean(props.GetProperty("oldCorefFormat", "false"));
				allowReparsing = PropertiesUtils.GetBool(props, Constants.AllowReparsingProp, Constants.AllowReparsing);
				// unless custom mention detection is set, just use the default coref mention detector
				performMentionDetection = !PropertiesUtils.GetBool(props, "dcoref.useCustomMentionDetection", false);
				if (performMentionDetection)
				{
					mentionAnnotator = new CorefMentionAnnotator(props);
				}
			}
			catch (Exception e)
			{
				log.Error("cannot create DeterministicCorefAnnotator!");
				log.Error(e);
				throw new Exception(e);
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
				if (!token.Get(sourceNERTagClass).Equals(string.Empty) && token.Get(sourceNERTagClass) != null)
				{
					token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), token.Get(sourceNERTagClass));
				}
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			// temporarily set the primary named entity tag to the coarse tag
			SetNamedEntityTagGranularity(annotation, "coarse");
			if (performMentionDetection)
			{
				mentionAnnotator.Annotate(annotation);
			}
			try
			{
				IList<Tree> trees = new List<Tree>();
				IList<IList<CoreLabel>> sentences = new List<IList<CoreLabel>>();
				// extract trees and sentence words
				// we are only supporting the new annotation standard for this Annotator!
				bool hasSpeakerAnnotations = false;
				if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					// int sentNum = 0;
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
						sentences.Add(tokens);
						Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
						trees.Add(tree);
						SemanticGraph dependencies = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Collapsed, GrammaticalStructure.Extras.None, null, true);
						// locking here is crucial for correct threading!
						sentence.Set(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation), dependencies);
						if (!hasSpeakerAnnotations)
						{
							// check for speaker annotations
							foreach (CoreLabel t in tokens)
							{
								if (t.Get(typeof(CoreAnnotations.SpeakerAnnotation)) != null)
								{
									hasSpeakerAnnotations = true;
									break;
								}
							}
						}
						MentionExtractor.MergeLabels(tree, tokens);
						MentionExtractor.InitializeUtterance(tokens);
					}
				}
				else
				{
					log.Error("this coreference resolution system requires SentencesAnnotation!");
					return;
				}
				if (hasSpeakerAnnotations)
				{
					annotation.Set(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation), true);
				}
				// extract all possible mentions
				// this is created for each new annotation because it is not threadsafe
				RuleBasedCorefMentionFinder finder = new RuleBasedCorefMentionFinder(allowReparsing);
				IList<IList<Mention>> allUnprocessedMentions = finder.ExtractPredictedMentions(annotation, 0, corefSystem.Dictionaries());
				// add the relevant info to mentions and order them for coref
				Document document = mentionExtractor.Arrange(annotation, sentences, trees, allUnprocessedMentions);
				IList<IList<Mention>> orderedMentions = document.GetOrderedMentions();
				IDictionary<int, CorefChain> result = corefSystem.CorefReturnHybridOutput(document);
				annotation.Set(typeof(CorefCoreAnnotations.CorefChainAnnotation), result);
				if (OldFormat)
				{
					IDictionary<int, CorefChain> oldResult = corefSystem.Coref(document);
					AddObsoleteCoreferenceAnnotations(annotation, orderedMentions, oldResult);
				}
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
		}

		// for backward compatibility with a few old things
		// TODO: Aim to get rid of this entirely
		private static void AddObsoleteCoreferenceAnnotations(Annotation annotation, IList<IList<Mention>> orderedMentions, IDictionary<int, CorefChain> result)
		{
			IList<Pair<IntTuple, IntTuple>> links = SieveCoreferenceSystem.GetLinks(result);
			//
			// save the coref output as CorefGraphAnnotation
			//
			// cdm 2013: this block didn't seem to be doing anything needed....
			// List<List<CoreLabel>> sents = new ArrayList<List<CoreLabel>>();
			// for (CoreMap sentence: annotation.get(CoreAnnotations.SentencesAnnotation.class)) {
			//   List<CoreLabel> tokens = sentence.get(CoreAnnotations.TokensAnnotation.class);
			//   sents.add(tokens);
			// }
			// this graph is stored in CorefGraphAnnotation -- the raw links found by the coref system
			IList<Pair<IntTuple, IntTuple>> graph = new List<Pair<IntTuple, IntTuple>>();
			foreach (Pair<IntTuple, IntTuple> link in links)
			{
				//
				// Note: all offsets in the graph start at 1 (not at 0!)
				//       we do this for consistency reasons, as indices for syntactic dependencies start at 1
				//
				int srcSent = link.first.Get(0);
				int srcTok = orderedMentions[srcSent - 1][link.first.Get(1) - 1].headIndex + 1;
				int dstSent = link.second.Get(0);
				int dstTok = orderedMentions[dstSent - 1][link.second.Get(1) - 1].headIndex + 1;
				IntTuple dst = new IntTuple(2);
				dst.Set(0, dstSent);
				dst.Set(1, dstTok);
				IntTuple src = new IntTuple(2);
				src.Set(0, srcSent);
				src.Set(1, srcTok);
				graph.Add(new Pair<IntTuple, IntTuple>(src, dst));
			}
			annotation.Set(typeof(CorefCoreAnnotations.CorefGraphAnnotation), graph);
			foreach (CorefChain corefChain in result.Values)
			{
				if (corefChain.GetMentionsInTextualOrder().Count < 2)
				{
					continue;
				}
				ICollection<CoreLabel> coreferentTokens = Generics.NewHashSet();
				foreach (CorefChain.CorefMention mention in corefChain.GetMentionsInTextualOrder())
				{
					ICoreMap sentence = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[mention.sentNum - 1];
					CoreLabel token = sentence.Get(typeof(CoreAnnotations.TokensAnnotation))[mention.headIndex - 1];
					coreferentTokens.Add(token);
				}
				foreach (CoreLabel token_1 in coreferentTokens)
				{
					token_1.Set(typeof(CorefCoreAnnotations.CorefClusterAnnotation), coreferentTokens);
				}
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CorefCoreAnnotations.CorefChainAnnotation));
		}
	}
}
