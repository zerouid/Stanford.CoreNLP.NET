using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>One vs All multiclass classifier</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class OneVsAllClassifier<L, F> : IClassifier<L, F>
	{
		private const long serialVersionUID = -743792054415242776L;

		private const string PosLabel = "+1";

		private const string NegLabel = "-1";

		private static readonly IIndex<string> binaryIndex;

		private static readonly int posIndex;

		static OneVsAllClassifier()
		{
			binaryIndex = new HashIndex<string>();
			binaryIndex.Add(PosLabel);
			binaryIndex.Add(NegLabel);
			posIndex = binaryIndex.IndexOf(PosLabel);
		}

		private IIndex<F> featureIndex;

		private IIndex<L> labelIndex;

		private IDictionary<L, IClassifier<string, F>> binaryClassifiers;

		private L defaultLabel;

		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.OneVsAllClassifier));

		public OneVsAllClassifier(IIndex<F> featureIndex, IIndex<L> labelIndex)
			: this(featureIndex, labelIndex, Generics.NewHashMap(), null)
		{
		}

		public OneVsAllClassifier(IIndex<F> featureIndex, IIndex<L> labelIndex, IDictionary<L, IClassifier<string, F>> binaryClassifiers)
			: this(featureIndex, labelIndex, binaryClassifiers, null)
		{
		}

		public OneVsAllClassifier(IIndex<F> featureIndex, IIndex<L> labelIndex, IDictionary<L, IClassifier<string, F>> binaryClassifiers, L defaultLabel)
		{
			this.featureIndex = featureIndex;
			this.labelIndex = labelIndex;
			this.binaryClassifiers = binaryClassifiers;
			this.defaultLabel = defaultLabel;
		}

		public virtual void AddBinaryClassifier(L label, IClassifier<string, F> classifier)
		{
			binaryClassifiers[label] = classifier;
		}

		protected internal virtual IClassifier<string, F> GetBinaryClassifier(L label)
		{
			return binaryClassifiers[label];
		}

		public virtual L ClassOf(IDatum<L, F> example)
		{
			ICounter<L> scores = ScoresOf(example);
			if (scores != null)
			{
				return Counters.Argmax(scores);
			}
			else
			{
				return defaultLabel;
			}
		}

		public virtual ICounter<L> ScoresOf(IDatum<L, F> example)
		{
			ICounter<L> scores = new ClassicCounter<L>();
			foreach (L label in labelIndex)
			{
				IDictionary<L, string> posLabelMap = new ArrayMap<L, string>();
				posLabelMap[label] = PosLabel;
				IDatum<string, F> binDatum = GeneralDataset.MapDatum(example, posLabelMap, NegLabel);
				IClassifier<string, F> binaryClassifier = GetBinaryClassifier(label);
				ICounter<string> binScores = binaryClassifier.ScoresOf(binDatum);
				double score = binScores.GetCount(PosLabel);
				scores.SetCount(label, score);
			}
			return scores;
		}

		public virtual ICollection<L> Labels()
		{
			return labelIndex.ObjectsList();
		}

		public static Edu.Stanford.Nlp.Classify.OneVsAllClassifier<L, F> Train<L, F>(IClassifierFactory<string, F, IClassifier<string, F>> classifierFactory, GeneralDataset<L, F> dataset)
		{
			IIndex<L> labelIndex = dataset.LabelIndex();
			return Train(classifierFactory, dataset, labelIndex.ObjectsList());
		}

		public static Edu.Stanford.Nlp.Classify.OneVsAllClassifier<L, F> Train<L, F>(IClassifierFactory<string, F, IClassifier<string, F>> classifierFactory, GeneralDataset<L, F> dataset, ICollection<L> trainLabels)
		{
			IIndex<L> labelIndex = dataset.LabelIndex();
			IIndex<F> featureIndex = dataset.FeatureIndex();
			IDictionary<L, IClassifier<string, F>> classifiers = Generics.NewHashMap();
			foreach (L label in trainLabels)
			{
				int i = labelIndex.IndexOf(label);
				logger.Info("Training " + label + " = " + i + ", posIndex = " + posIndex);
				// Create training data for training this classifier
				IDictionary<L, string> posLabelMap = new ArrayMap<L, string>();
				posLabelMap[label] = PosLabel;
				GeneralDataset<string, F> binaryDataset = dataset.MapDataset(dataset, binaryIndex, posLabelMap, NegLabel);
				IClassifier<string, F> binaryClassifier = classifierFactory.TrainClassifier(binaryDataset);
				classifiers[label] = binaryClassifier;
			}
			Edu.Stanford.Nlp.Classify.OneVsAllClassifier<L, F> classifier = new Edu.Stanford.Nlp.Classify.OneVsAllClassifier<L, F>(featureIndex, labelIndex, classifiers);
			return classifier;
		}
	}
}
