using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>A tokensregex extractor for KBP.</summary>
	/// <author>Gabor Angeli</author>
	public class KBPSemgrexExtractor : IKBPRelationExtractor
	{
		protected internal readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.KBPSemgrexExtractor));

		public static string Dir = DefaultPaths.DefaultKbpSemgrexDir;

		public static File TestFile = new File("test.conll");

		public static Optional<string> Predictions = Optional.Empty();

		private readonly IDictionary<KBPRelationExtractor.RelationType, ICollection<SemgrexPattern>> rules = new Dictionary<KBPRelationExtractor.RelationType, ICollection<SemgrexPattern>>();

		/// <exception cref="System.IO.IOException"/>
		public KBPSemgrexExtractor(string semgrexdir)
			: this(semgrexdir, false)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public KBPSemgrexExtractor(string semgrexdir, bool verbose)
		{
			if (verbose)
			{
				logger.Log("Creating SemgrexRegexExtractor");
			}
			// Create extractors
			foreach (KBPRelationExtractor.RelationType rel in KBPRelationExtractor.RelationType.Values())
			{
				string relFileNameComponent = rel.canonicalName.ReplaceAll(":", "_");
				string filename = semgrexdir + File.separator + relFileNameComponent.Replace("/", "SLASH") + ".rules";
				if (IOUtils.ExistsInClasspathOrFileSystem(filename))
				{
					IList<SemgrexPattern> rulesforrel = SemgrexBatchParser.CompileStream(IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(filename));
					if (verbose)
					{
						logger.Log("Read " + rulesforrel.Count + " rules from " + filename + " for relation " + rel);
					}
					rules[rel] = rulesforrel;
				}
			}
		}

		public virtual Pair<string, double> Classify(KBPRelationExtractor.KBPInput input)
		{
			foreach (KBPRelationExtractor.RelationType rel in KBPRelationExtractor.RelationType.Values())
			{
				if (rules.Contains(rel) && rel.entityType == input.subjectType && rel.validNamedEntityLabels.Contains(input.objectType))
				{
					ICollection<SemgrexPattern> rulesForRel = rules[rel];
					ICoreMap sentence = input.sentence.AsCoreMap(null, null);
					bool matches = Matches(sentence, rulesForRel, input, sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))) || Matches(sentence, rulesForRel, input, sentence.Get(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation
						)));
					if (matches)
					{
						//logger.log("MATCH for " + rel +  ". " + sentence: + sentence + " with rules for  " + rel);
						return Pair.MakePair(rel.canonicalName, 1.0);
					}
				}
			}
			return Pair.MakePair(KBPRelationExtractorConstants.NoRelation, 1.0);
		}

		/// <summary>Returns whether any of the given patterns match this tree.</summary>
		private bool Matches(ICoreMap sentence, ICollection<SemgrexPattern> rulesForRel, KBPRelationExtractor.KBPInput input, SemanticGraph graph)
		{
			if (graph == null || graph.IsEmpty())
			{
				return false;
			}
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			foreach (int i in input.subjectSpan)
			{
				if ("O".Equals(tokens[i].Ner()))
				{
					tokens[i].SetNER(input.subjectType.name);
				}
			}
			foreach (int i_1 in input.objectSpan)
			{
				if ("O".Equals(tokens[i_1].Ner()))
				{
					tokens[i_1].SetNER(input.objectType.name);
				}
			}
			foreach (SemgrexPattern p in rulesForRel)
			{
				try
				{
					SemgrexMatcher n = p.Matcher(graph);
					while (n.Find())
					{
						IndexedWord entity = n.GetNode("entity");
						IndexedWord slot = n.GetNode("slot");
						bool hasSubject = entity.Index() >= input.subjectSpan.Start() + 1 && entity.Index() <= input.subjectSpan.End();
						bool hasObject = slot.Index() >= input.objectSpan.Start() + 1 && slot.Index() <= input.objectSpan.End();
						if (hasSubject && hasObject)
						{
							return true;
						}
					}
				}
				catch (Exception)
				{
					//Happens when graph has no roots
					return false;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			RedwoodConfiguration.Standard().Apply();
			// Disable SLF4J crap.
			ArgumentParser.FillOptions(typeof(Edu.Stanford.Nlp.IE.KBPSemgrexExtractor), args);
			Edu.Stanford.Nlp.IE.KBPSemgrexExtractor extractor = new Edu.Stanford.Nlp.IE.KBPSemgrexExtractor(Dir);
			IList<Pair<KBPRelationExtractor.KBPInput, string>> testExamples = IKBPRelationExtractor.ReadDataset(TestFile);
			extractor.ComputeAccuracy(testExamples.Stream(), Predictions.Map(null));
		}
	}
}
