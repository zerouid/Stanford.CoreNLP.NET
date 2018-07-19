using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <summary>Simple extractor which combines several other Extractors.</summary>
	/// <remarks>
	/// Simple extractor which combines several other Extractors.  Currently only works with RelationMentions.
	/// Also note that this implementation uses Sets and will mangle the original order of RelationMentions.
	/// </remarks>
	/// <author>David McClosky</author>
	[System.Serializable]
	public class ExtractorMerger : IExtractor
	{
		private const long serialVersionUID = 1L;

		private static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.ExtractorMerger).FullName);

		private IExtractor[] extractors;

		public ExtractorMerger(IExtractor[] extractors)
		{
			if (extractors.Length < 2)
			{
				throw new ArgumentException("We need at least 2 extractors for ExtractorMerger to make sense.");
			}
			this.extractors = extractors;
		}

		public virtual void Annotate(Annotation dataset)
		{
			// TODO for now, we only merge RelationMentions
			logger.Info("Extractor 0 annotating dataset.");
			extractors[0].Annotate(dataset);
			// store all the RelationMentions per sentence
			IList<ICollection<RelationMention>> allRelationMentions = new List<ICollection<RelationMention>>();
			foreach (ICoreMap sentence in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<RelationMention> relationMentions = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
				ICollection<RelationMention> uniqueRelationMentions = new HashSet<RelationMention>(relationMentions);
				allRelationMentions.Add(uniqueRelationMentions);
			}
			// skip first extractor since we did it at the top
			for (int extractorIndex = 1; extractorIndex < extractors.Length; extractorIndex++)
			{
				logger.Info("Extractor " + extractorIndex + " annotating dataset.");
				IExtractor extractor = extractors[extractorIndex];
				extractor.Annotate(dataset);
				// walk through all sentences and merge our RelationMentions with the combined set
				int sentenceIndex = 0;
				foreach (ICoreMap sentence_1 in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					IList<RelationMention> relationMentions = sentence_1.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
					Sharpen.Collections.AddAll(allRelationMentions[sentenceIndex], relationMentions);
				}
			}
			// put all merged relations back into the dataset
			int sentenceIndex_1 = 0;
			foreach (ICoreMap sentence_2 in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				ICollection<RelationMention> uniqueRelationMentions = allRelationMentions[sentenceIndex_1];
				IList<RelationMention> relationMentions = new List<RelationMention>(uniqueRelationMentions);
				sentence_2.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), relationMentions);
				sentenceIndex_1++;
			}
		}

		public static IExtractor BuildRelationExtractorMerger(string[] extractorModelNames)
		{
			BasicRelationExtractor[] relationExtractorComponents = new BasicRelationExtractor[extractorModelNames.Length];
			for (int i = 0; i < extractorModelNames.Length; i++)
			{
				string modelName = extractorModelNames[i];
				logger.Info("Loading model " + i + " for model merging from " + modelName);
				try
				{
					relationExtractorComponents[i] = BasicRelationExtractor.Load(modelName);
				}
				catch (Exception e)
				{
					logger.Severe("Error loading model:");
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
			Edu.Stanford.Nlp.IE.Machinereading.ExtractorMerger relationExtractor = new Edu.Stanford.Nlp.IE.Machinereading.ExtractorMerger(relationExtractorComponents);
			return relationExtractor;
		}

		public virtual void SetLoggerLevel(Level level)
		{
			logger.SetLevel(level);
		}

		// stubs required by Extractor interface -- they don't do anything since this model is not trainable or savable
		/// <exception cref="System.IO.IOException"/>
		public virtual void Save(string path)
		{
		}

		public virtual void Train(Annotation dataset)
		{
		}
	}
}
