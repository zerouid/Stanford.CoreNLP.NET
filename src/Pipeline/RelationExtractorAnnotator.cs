using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading;
using Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Annotating relations between entities produced by the NER system.</summary>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class RelationExtractorAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.RelationExtractorAnnotator));

		internal MachineReading mr;

		private static bool verbose = false;

		public RelationExtractorAnnotator(Properties props)
		{
			verbose = bool.ParseBoolean(props.GetProperty("sup.relation.verbose", "false"));
			string relationModel = props.GetProperty("sup.relation.model", DefaultPaths.DefaultSupRelationExRelationModel);
			try
			{
				IExtractor entityExtractor = new RothEntityExtractor();
				BasicRelationExtractor relationExtractor = BasicRelationExtractor.Load(relationModel);
				log.Info("Loading relation model from " + relationModel);
				mr = MachineReading.MakeMachineReadingForAnnotation(new RothCONLL04Reader(), entityExtractor, relationExtractor, null, null, null, true, verbose);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception(e);
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			// extract entities and relations
			Annotation output = mr.Annotate(annotation);
			// transfer entities/relations back to the original annotation
			IList<ICoreMap> outputSentences = output.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<ICoreMap> origSentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < outputSentences.Count; i++)
			{
				ICoreMap outSent = outputSentences[i];
				ICoreMap origSent = origSentences[i];
				// set entities
				IList<EntityMention> entities = outSent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				origSent.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), entities);
				if (verbose && entities != null)
				{
					log.Info("Extracted the following entities:");
					foreach (EntityMention e in entities)
					{
						log.Info("\t" + e);
					}
				}
				// set relations
				IList<RelationMention> relations = outSent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
				origSent.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), relations);
				if (verbose && relations != null)
				{
					log.Info("Extracted the following relations:");
					foreach (RelationMention r in relations)
					{
						if (!r.GetType().Equals(RelationMention.Unrelated))
						{
							log.Info(r);
						}
					}
				}
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation
				), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation
				))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), typeof(MachineReadingAnnotations.RelationMentionsAnnotation))));
		}

		public static void Main(string[] args)
		{
			try
			{
				Properties props = StringUtils.ArgsToProperties(args);
				props.SetProperty("annotators", "tokenize,ssplit,lemma,pos,parse,ner");
				StanfordCoreNLP pipeline = new StanfordCoreNLP();
				string sentence = "Barack Obama lives in America. Obama works for the Federal Goverment.";
				Annotation doc = new Annotation(sentence);
				pipeline.Annotate(doc);
				Edu.Stanford.Nlp.Pipeline.RelationExtractorAnnotator r = new Edu.Stanford.Nlp.Pipeline.RelationExtractorAnnotator(props);
				r.Annotate(doc);
				foreach (ICoreMap s in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					System.Console.Out.WriteLine("For sentence " + s.Get(typeof(CoreAnnotations.TextAnnotation)));
					IList<RelationMention> rls = s.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
					foreach (RelationMention rl in rls)
					{
						System.Console.Out.WriteLine(rl.ToString());
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
