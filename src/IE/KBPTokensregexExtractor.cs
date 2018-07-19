using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>A tokensregex extractor for KBP.</summary>
	/// <remarks>
	/// A tokensregex extractor for KBP.
	/// IMPORTANT: Don't rename this class without updating the rules defs file.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class KBPTokensregexExtractor : IKBPRelationExtractor
	{
		protected internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.KBPTokensregexExtractor));

		public static string Dir = DefaultPaths.DefaultKbpTokensregexDir;

		public static File TestFile = new File("test.conll");

		public static Optional<string> Predictions = Optional.Empty();

		private readonly IDictionary<KBPRelationExtractor.RelationType, CoreMapExpressionExtractor> rules = new Dictionary<KBPRelationExtractor.RelationType, CoreMapExpressionExtractor>();

		/// <summary>IMPORTANT: Don't rename this class without updating the rules defs file.</summary>
		public class Subject : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>IMPORTANT: Don't rename this class without updating the rules defs file.</summary>
		public class Object : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public KBPTokensregexExtractor(string tokensregexDir)
			: this(tokensregexDir, false)
		{
		}

		public KBPTokensregexExtractor(string tokensregexDir, bool verbose)
		{
			if (verbose)
			{
				logger.Log("Creating TokensRegexExtractor");
			}
			// Create extractors
			foreach (KBPRelationExtractor.RelationType rel in KBPRelationExtractor.RelationType.Values())
			{
				string relFileNameComponent = rel.canonicalName.ReplaceAll(":", "_");
				string path = tokensregexDir + File.separator + relFileNameComponent.ReplaceAll("/", "SLASH") + ".rules";
				if (IOUtils.ExistsInClasspathOrFileSystem(path))
				{
					IList<string> listFiles = new List<string>();
					listFiles.Add(tokensregexDir + File.separator + "defs.rules");
					listFiles.Add(path);
					if (verbose)
					{
						logger.Log("Rule files for relation " + rel + " is " + path);
					}
					Env env = TokenSequencePattern.GetNewEnv();
					env.Bind("collapseExtractionRules", true);
					env.Bind("verbose", verbose);
					CoreMapExpressionExtractor extr = CoreMapExpressionExtractor.CreateExtractorFromFiles(env, listFiles).KeepTemporaryTags();
					rules[rel] = extr;
				}
			}
		}

		public virtual Pair<string, double> Classify(KBPRelationExtractor.KBPInput input)
		{
			// Annotate Sentence
			ICoreMap sentenceAsMap = input.sentence.AsCoreMap(null);
			IList<CoreLabel> tokens = sentenceAsMap.Get(typeof(CoreAnnotations.TokensAnnotation));
			// Annotate where the subject is
			foreach (int i in input.subjectSpan)
			{
				tokens[i].Set(typeof(KBPTokensregexExtractor.Subject), "true");
				if ("O".Equals(tokens[i].Ner()))
				{
					tokens[i].SetNER(input.subjectType.name);
				}
			}
			// Annotate where the object is
			foreach (int i_1 in input.objectSpan)
			{
				tokens[i_1].Set(typeof(KBPTokensregexExtractor.Object), "true");
				if ("O".Equals(tokens[i_1].Ner()))
				{
					tokens[i_1].SetNER(input.objectType.name);
				}
			}
			// Run Rules
			foreach (KBPRelationExtractor.RelationType rel in KBPRelationExtractor.RelationType.Values())
			{
				if (rules.Contains(rel) && rel.entityType == input.subjectType && rel.validNamedEntityLabels.Contains(input.objectType))
				{
					CoreMapExpressionExtractor extractor = rules[rel];
					IList<MatchedExpression> extractions = extractor.ExtractExpressions(sentenceAsMap);
					if (extractions != null && extractions.Count > 0)
					{
						MatchedExpression best = MatchedExpression.GetBestMatched(extractions, MatchedExpression.ExprWeightScorer);
						// Un-Annotate Sentence
						foreach (CoreLabel token in tokens)
						{
							token.Remove(typeof(KBPTokensregexExtractor.Subject));
							token.Remove(typeof(KBPTokensregexExtractor.Object));
						}
						return Pair.MakePair(rel.canonicalName, best.GetWeight());
					}
				}
			}
			// Un-Annotate Sentence
			foreach (CoreLabel token_1 in tokens)
			{
				token_1.Remove(typeof(KBPTokensregexExtractor.Subject));
				token_1.Remove(typeof(KBPTokensregexExtractor.Object));
			}
			return Pair.MakePair(KBPRelationExtractorConstants.NoRelation, 1.0);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			RedwoodConfiguration.Standard().Apply();
			// Disable SLF4J crap.
			ArgumentParser.FillOptions(typeof(KBPTokensregexExtractor), args);
			KBPTokensregexExtractor extractor = new KBPTokensregexExtractor(Dir);
			IList<Pair<KBPRelationExtractor.KBPInput, string>> testExamples = IKBPRelationExtractor.ReadDataset(TestFile);
			extractor.ComputeAccuracy(testExamples.Stream(), Predictions.Map(null));
		}
	}
}
