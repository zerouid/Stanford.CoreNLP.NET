using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Nndep;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class adds dependency parse information to an Annotation.</summary>
	/// <remarks>
	/// This class adds dependency parse information to an Annotation.
	/// Dependency parses are added to each sentence under the annotation
	/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraphCoreAnnotations.BasicDependenciesAnnotation"/>
	/// .
	/// </remarks>
	/// <author>Jon Gauthier</author>
	public class DependencyParseAnnotator : SentenceAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.DependencyParseAnnotator));

		private readonly DependencyParser parser;

		private readonly int nThreads;

		private const int DefaultNthreads = 1;

		/// <summary>Maximum parse time (in milliseconds) for a sentence</summary>
		private readonly long maxTime;

		/// <summary>The default maximum parse time.</summary>
		private const long DefaultMaxtime = -1;

		/// <summary>If true, include the extra arcs in the dependency representation.</summary>
		private readonly GrammaticalStructure.Extras extraDependencies;

		public DependencyParseAnnotator()
			: this(new Properties())
		{
		}

		public DependencyParseAnnotator(Properties properties)
		{
			string modelPath = PropertiesUtils.GetString(properties, "model", DependencyParser.DefaultModel);
			parser = DependencyParser.LoadFromModelFile(modelPath, properties);
			nThreads = PropertiesUtils.GetInt(properties, "testThreads", DefaultNthreads);
			maxTime = PropertiesUtils.GetLong(properties, "sentenceTimeout", DefaultMaxtime);
			extraDependencies = MetaClass.Cast(properties.GetProperty("extradependencies", "NONE"), typeof(GrammaticalStructure.Extras));
		}

		protected internal override int NThreads()
		{
			return nThreads;
		}

		protected internal override long MaxTime()
		{
			return maxTime;
		}

		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			GrammaticalStructure gs = parser.Predict(sentence);
			SemanticGraph deps = SemanticGraphFactory.MakeFromTree(gs, SemanticGraphFactory.Mode.Collapsed, extraDependencies, null);
			SemanticGraph uncollapsedDeps = SemanticGraphFactory.MakeFromTree(gs, SemanticGraphFactory.Mode.Basic, extraDependencies, null);
			SemanticGraph ccDeps = SemanticGraphFactory.MakeFromTree(gs, SemanticGraphFactory.Mode.Ccprocessed, extraDependencies, null);
			SemanticGraph enhancedDeps = SemanticGraphFactory.MakeFromTree(gs, SemanticGraphFactory.Mode.Enhanced, extraDependencies, null);
			SemanticGraph enhancedPlusPlusDeps = SemanticGraphFactory.MakeFromTree(gs, SemanticGraphFactory.Mode.EnhancedPlusPlus, extraDependencies, null);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), deps);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), uncollapsedDeps);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), ccDeps);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), enhancedDeps);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), enhancedPlusPlusDeps);
		}

		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
			// TODO
			log.Info("fail");
		}

		public override ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.TokensAnnotation), 
				typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation))));
		}

		public override ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation
				), typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
		}
	}
}
