using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Runs feature extraction over coreference documents.</summary>
	/// <author>Kevin Clark</author>
	public class FeatureExtractorRunner : ICorefDocumentProcessor
	{
		private readonly FeatureExtractor extractor;

		private readonly Compressor<string> compressor;

		private readonly IDictionary<int, IDictionary<Pair<int, int>, bool>> dataset;

		private readonly IList<DocumentExamples> documents;

		public FeatureExtractorRunner(Properties props, Dictionaries dictionaries)
		{
			documents = new List<DocumentExamples>();
			compressor = new Compressor<string>();
			extractor = new FeatureExtractor(props, dictionaries, compressor);
			try
			{
				dataset = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.datasetFile);
			}
			catch (Exception e)
			{
				throw new Exception("Error initializing FeatureExtractorRunner", e);
			}
		}

		public virtual void Process(int id, Document document)
		{
			if (dataset.Contains(id))
			{
				documents.Add(extractor.Extract(id, document, dataset[id]));
			}
		}

		/// <exception cref="System.Exception"/>
		public virtual void Finish()
		{
			IOUtils.WriteObjectToFile(documents, StatisticalCorefTrainer.extractedFeaturesFile);
			IOUtils.WriteObjectToFile(compressor, StatisticalCorefTrainer.compressorFile);
		}
	}
}
