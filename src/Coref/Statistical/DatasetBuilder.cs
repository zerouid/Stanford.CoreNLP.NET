using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Produces train/dev/test sets for training coreference models with (optionally) sampling.</summary>
	/// <author>Kevin Clark</author>
	public class DatasetBuilder : ICorefDocumentProcessor
	{
		private readonly int maxExamplesPerDocument;

		private readonly double minClassImbalancedPerDocument;

		private readonly IDictionary<int, IDictionary<Pair<int, int>, bool>> mentionPairs;

		private readonly Random random;

		public DatasetBuilder()
			: this(0, int.MaxValue)
		{
		}

		public DatasetBuilder(double minClassImbalancedPerDocument, int maxExamplesPerDocument)
		{
			this.maxExamplesPerDocument = maxExamplesPerDocument;
			this.minClassImbalancedPerDocument = minClassImbalancedPerDocument;
			mentionPairs = new Dictionary<int, IDictionary<Pair<int, int>, bool>>();
			random = new Random(0);
		}

		public virtual void Process(int id, Document document)
		{
			IDictionary<Pair<int, int>, bool> labeledPairs = CorefUtils.GetLabeledMentionPairs(document);
			long numP = labeledPairs.Keys.Stream().Filter(null).Count();
			IList<Pair<int, int>> negative = labeledPairs.Keys.Stream().Filter(null).Collect(Collectors.ToList());
			int numN = negative.Count;
			if (numP / (float)(numP + numN) < minClassImbalancedPerDocument)
			{
				numN = (int)(numP / minClassImbalancedPerDocument - numP);
				Java.Util.Collections.Shuffle(negative);
				for (int i = numN; i < negative.Count; i++)
				{
					Sharpen.Collections.Remove(labeledPairs, negative[i]);
				}
			}
			IDictionary<int, IList<int>> mentionToCandidateAntecedents = new Dictionary<int, IList<int>>();
			foreach (Pair<int, int> pair in labeledPairs.Keys)
			{
				IList<int> candidateAntecedents = mentionToCandidateAntecedents[pair.second];
				if (candidateAntecedents == null)
				{
					candidateAntecedents = new List<int>();
					mentionToCandidateAntecedents[pair.second] = candidateAntecedents;
				}
				candidateAntecedents.Add(pair.first);
			}
			IList<int> mentions = new List<int>(mentionToCandidateAntecedents.Keys);
			while (labeledPairs.Count > maxExamplesPerDocument)
			{
				int mention = mentions.Remove(random.NextInt(mentions.Count));
				foreach (int candidateAntecedent in mentionToCandidateAntecedents[mention])
				{
					Sharpen.Collections.Remove(labeledPairs, new Pair<int, int>(candidateAntecedent, mention));
				}
			}
			mentionPairs[id] = labeledPairs;
		}

		/// <exception cref="System.Exception"/>
		public virtual void Finish()
		{
			IOUtils.WriteObjectToFile(mentionPairs, StatisticalCorefTrainer.datasetFile);
		}
	}
}
