using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Quoteattribution.Sieves.Training;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by michaelf on 3/31/16.</summary>
	public class ExtractQuotesClassifier
	{
		internal bool verbose = true;

		private IClassifier<string, string> quoteToMentionClassifier;

		public ExtractQuotesClassifier(GeneralDataset<string, string> trainingSet)
		{
			LinearClassifierFactory<string, string> lcf = new LinearClassifierFactory<string, string>();
			quoteToMentionClassifier = lcf.TrainClassifier(trainingSet);
		}

		public ExtractQuotesClassifier(string modelPath)
		{
			try
			{
				ObjectInputStream si = IOUtils.ReadStreamFromString(modelPath);
				quoteToMentionClassifier = (IClassifier<string, string>)si.ReadObject();
				si.Close();
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception();
			}
			catch (TypeLoadException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception();
			}
		}

		public virtual IClassifier<string, string> GetClassifier()
		{
			return quoteToMentionClassifier;
		}

		public virtual void ScoreBestMentionNew(SupervisedSieveTraining.FeaturesData fd, Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			for (int i = 0; i < quotes.Count; i++)
			{
				ICoreMap quote = quotes[i];
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					continue;
				}
				double maxConfidence = 0;
				int maxDataIdx = -1;
				int goldDataIdx = -1;
				Pair<int, int> dataRange = fd.mapQuoteToDataRange[i];
				if (dataRange == null)
				{
					continue;
				}
				else
				{
					for (int dataIdx = dataRange.first; dataIdx <= dataRange.second; dataIdx++)
					{
						RVFDatum<string, string> datum = fd.dataset.GetRVFDatum(dataIdx);
						double isMentionConfidence = quoteToMentionClassifier.ScoresOf(datum).GetCount("isMention");
						if (isMentionConfidence > maxConfidence)
						{
							maxConfidence = isMentionConfidence;
							maxDataIdx = dataIdx;
						}
					}
					if (maxDataIdx != -1)
					{
						Sieve.MentionData mentionData = fd.mapDatumToMention[maxDataIdx];
						if (mentionData.type.Equals("animate noun"))
						{
							continue;
						}
						quote.Set(typeof(QuoteAttributionAnnotator.MentionAnnotation), mentionData.text);
						quote.Set(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation), mentionData.begin);
						quote.Set(typeof(QuoteAttributionAnnotator.MentionEndAnnotation), mentionData.end);
						quote.Set(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), mentionData.type);
						quote.Set(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation), "supervised");
					}
				}
			}
		}
	}
}
