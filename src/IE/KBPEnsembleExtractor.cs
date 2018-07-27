using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.IE
{
	/// <summary>An ensemble of other KBP relation extractors.</summary>
	/// <remarks>
	/// An ensemble of other KBP relation extractors.
	/// Currently, this class just takes the union of the given extractors.
	/// That is, it returns the first relation returned by any extractor
	/// (ties broken by the order the extractors are passed to the constructor),
	/// and only returns no_relation if no extractor proposed a relation.
	/// </remarks>
	public class KBPEnsembleExtractor : IKBPRelationExtractor
	{
		protected internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(IKBPRelationExtractor));

		private static string StatisticalModel = DefaultPaths.DefaultKbpClassifier;

		private static string SemgrexDir = DefaultPaths.DefaultKbpSemgrexDir;

		private static string TokensregexDir = DefaultPaths.DefaultKbpTokensregexDir;

		public static Optional<string> Predictions = Optional.Empty();

		public static File TestFile = new File("test.conll");

		/// <summary>The extractors to run, in the order of priority they should be run in.</summary>
		public readonly IKBPRelationExtractor[] extractors;

		/// <summary>Creates a new ensemble extractor from the given argument extractors.</summary>
		/// <param name="extractors">A varargs list of extractors to union together.</param>
		public KBPEnsembleExtractor(params IKBPRelationExtractor[] extractors)
		{
			this.extractors = extractors;
		}

		public virtual Pair<string, double> Classify(KBPRelationExtractor.KBPInput input)
		{
			Pair<string, double> prediction = Pair.MakePair(KBPRelationExtractorConstants.NoRelation, 1.0);
			foreach (IKBPRelationExtractor extractor in extractors)
			{
				Pair<string, double> classifierPrediction = extractor.Classify(input);
				if (prediction.first.Equals(KBPRelationExtractorConstants.NoRelation) || (!classifierPrediction.first.Equals(KBPRelationExtractorConstants.NoRelation) && classifierPrediction.second > prediction.second))
				{
					// The last prediction was NO_RELATION, or this is not NO_RELATION and has a higher score
					prediction = classifierPrediction;
				}
			}
			return prediction;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			RedwoodConfiguration.Standard().Apply();
			// Disable SLF4J crap.
			ArgumentParser.FillOptions(typeof(Edu.Stanford.Nlp.IE.KBPEnsembleExtractor), args);
			object @object = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(StatisticalModel);
			IKBPRelationExtractor statisticalExtractor;
			if (@object is LinearClassifier)
			{
				//noinspection unchecked
				statisticalExtractor = new KBPStatisticalExtractor((IClassifier<string, string>)@object);
			}
			else
			{
				if (@object is KBPStatisticalExtractor)
				{
					statisticalExtractor = (KBPStatisticalExtractor)@object;
				}
				else
				{
					throw new InvalidCastException(@object.GetType() + " cannot be cast into a " + typeof(KBPStatisticalExtractor));
				}
			}
			logger.Info("Read statistical model from " + StatisticalModel);
			IKBPRelationExtractor extractor = new Edu.Stanford.Nlp.IE.KBPEnsembleExtractor(new KBPTokensregexExtractor(TokensregexDir), new KBPSemgrexExtractor(SemgrexDir), statisticalExtractor);
			IList<Pair<KBPRelationExtractor.KBPInput, string>> testExamples = IKBPRelationExtractor.ReadDataset(TestFile);
			extractor.ComputeAccuracy(testExamples.Stream(), Predictions.Map(null));
		}
	}
}
