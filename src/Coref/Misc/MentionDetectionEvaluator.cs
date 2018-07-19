using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Misc
{
	/// <summary>Evaluates the accuracy of mention detection.</summary>
	/// <author>Kevin Clark</author>
	public class MentionDetectionEvaluator : ICorefDocumentProcessor
	{
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(MentionDetectionEvaluator));

		private int correctSystemMentions = 0;

		private int systemMentions = 0;

		private int goldMentions = 0;

		public virtual void Process(int id, Document document)
		{
			foreach (CorefCluster gold in document.goldCorefClusters.Values)
			{
				foreach (Mention m in gold.corefMentions)
				{
					if (document.predictedMentionsByID.Contains(m.mentionID))
					{
						correctSystemMentions += 1;
					}
					goldMentions += 1;
				}
			}
			systemMentions += document.predictedMentionsByID.Count;
			double precision = correctSystemMentions / (double)systemMentions;
			double recall = correctSystemMentions / (double)goldMentions;
			log.Info("Precision: " + correctSystemMentions + " / " + systemMentions + " = " + string.Format("%.4f", precision));
			log.Info("Recall: " + correctSystemMentions + " / " + goldMentions + " = " + string.Format("%.4f", recall));
			log.Info(string.Format("F1: %.4f", 2 * precision * recall / (precision + recall)));
		}

		/// <exception cref="System.Exception"/>
		public virtual void Finish()
		{
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(new string[] { "-props", args[0] });
			Dictionaries dictionaries = new Dictionaries(props);
			CorefProperties.SetInput(props, CorefProperties.Dataset.Train);
			new MentionDetectionEvaluator().Run(props, dictionaries);
		}
	}
}
