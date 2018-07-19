using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// A simple k-NN classifier, with the options of using unit votes, or weighted votes (by
	/// similarity value).
	/// </summary>
	/// <remarks>
	/// A simple k-NN classifier, with the options of using unit votes, or weighted votes (by
	/// similarity value).  Use the <code>KNNClassifierFactory</code> class to train and instantiate
	/// a new classifier.
	/// NOTE: partially generified, waiting for final generification of classifiers package.
	/// </remarks>
	/// <author>Eric Yeh</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class KNNClassifier<K, V> : IClassifier<K, V>
	{
		private const long serialVersionUID = 7115357548209007944L;

		private bool weightedVotes = false;

		private CollectionValuedMap<K, ICounter<V>> instances = new CollectionValuedMap<K, ICounter<V>>();

		private IDictionary<ICounter<V>, K> classLookup = Generics.NewHashMap();

		private bool l2Normalize = false;

		internal int k = 0;

		// whether this is a weighted vote (by sim), or not
		public virtual ICollection<K> Labels()
		{
			return classLookup.Values;
		}

		protected internal KNNClassifier(int k, bool weightedVotes, bool l2Normalize)
		{
			this.k = k;
			this.weightedVotes = weightedVotes;
			this.l2Normalize = l2Normalize;
		}

		protected internal virtual void AddInstances(ICollection<RVFDatum<K, V>> datums)
		{
			foreach (RVFDatum<K, V> datum in datums)
			{
				K label = datum.Label();
				ICounter<V> vec = datum.AsFeaturesCounter();
				instances.Add(label, vec);
				classLookup[vec] = label;
			}
		}

		/// <summary>NOTE: currently does not support standard Datums, only RVFDatums.</summary>
		public virtual K ClassOf(IDatum<K, V> example)
		{
			if (example is RVFDatum<object, object>)
			{
				ClassicCounter<K> scores = ScoresOf(example);
				return Counters.ToSortedList(scores)[0];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Given an instance to classify, scores and returns
		/// score by class.
		/// </summary>
		/// <remarks>
		/// Given an instance to classify, scores and returns
		/// score by class.
		/// NOTE: supports only RVFDatums
		/// </remarks>
		public virtual ClassicCounter<K> ScoresOf(IDatum<K, V> datum)
		{
			if (datum is RVFDatum<object, object>)
			{
				RVFDatum<K, V> vec = (RVFDatum<K, V>)datum;
				if (l2Normalize)
				{
					ClassicCounter<V> featVec = new ClassicCounter<V>(vec.AsFeaturesCounter());
					Counters.Normalize(featVec);
					vec = new RVFDatum<K, V>(featVec);
				}
				ClassicCounter<ICounter<V>> scores = new ClassicCounter<ICounter<V>>();
				foreach (ICounter<V> instance in instances.AllValues())
				{
					scores.SetCount(instance, Counters.Cosine(vec.AsFeaturesCounter(), instance));
				}
				// set entry, for given instance and score
				IList<ICounter<V>> sorted = Counters.ToSortedList(scores);
				ClassicCounter<K> classScores = new ClassicCounter<K>();
				for (int i = 0; i < k && i < sorted.Count; i++)
				{
					K label = classLookup[sorted[i]];
					double count = 1.0;
					if (weightedVotes)
					{
						count = scores.GetCount(sorted[i]);
					}
					classScores.IncrementCount(label, count);
				}
				return classScores;
			}
			else
			{
				return null;
			}
		}

		// Quick little sanity check
		public static void Main(string[] args)
		{
			ICollection<RVFDatum<string, string>> trainingInstances = new List<RVFDatum<string, string>>();
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 5.0);
				f1.SetCount("temperature", 35.0);
				trainingInstances.Add(new RVFDatum<string, string>(f1, "rain"));
			}
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 4.0);
				f1.SetCount("temperature", 32.0);
				trainingInstances.Add(new RVFDatum<string, string>(f1, "rain"));
			}
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 6.0);
				f1.SetCount("temperature", 30.0);
				trainingInstances.Add(new RVFDatum<string, string>(f1, "rain"));
			}
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 2.0);
				f1.SetCount("temperature", 33.0);
				trainingInstances.Add(new RVFDatum<string, string>(f1, "dry"));
			}
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 1.0);
				f1.SetCount("temperature", 34.0);
				trainingInstances.Add(new RVFDatum<string, string>(f1, "dry"));
			}
			Edu.Stanford.Nlp.Classify.KNNClassifier<string, string> classifier = new KNNClassifierFactory<string, string>(3, false, true).Train(trainingInstances);
			{
				ClassicCounter<string> f1 = new ClassicCounter<string>();
				f1.SetCount("humidity", 2.0);
				f1.SetCount("temperature", 33.0);
				RVFDatum<string, string> testVec = new RVFDatum<string, string>(f1);
				System.Console.Out.WriteLine(classifier.ScoresOf(testVec));
				System.Console.Out.WriteLine(classifier.ClassOf(testVec));
			}
		}
	}
}
