using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// This constructs trained
	/// <c>KNNClassifier</c>
	/// objects, given
	/// sets of RVFDatums, or Counters (dimensions are identified by the keys).
	/// </summary>
	public class KNNClassifierFactory<K, V>
	{
		private int k;

		private bool weightedVotes;

		private bool l2NormalizeVectors;

		/// <summary>
		/// Creates a new factory that generates K-NN classifiers with the given k-value, and
		/// if the votes are weighted by their similarity score, or unit value.
		/// </summary>
		public KNNClassifierFactory(int k, bool weightedVotes, bool l2NormalizeVectors)
		{
			// = 0;
			// = false;
			// = false;
			this.k = k;
			this.weightedVotes = weightedVotes;
			this.l2NormalizeVectors = l2NormalizeVectors;
		}

		/// <summary>
		/// Given a set of labeled RVFDatums, treats each as an instance vector of that
		/// label and adds it to the examples used for classification.
		/// </summary>
		/// <remarks>
		/// Given a set of labeled RVFDatums, treats each as an instance vector of that
		/// label and adds it to the examples used for classification.
		/// NOTE: l2NormalizeVectors is NOT applied here.
		/// </remarks>
		public virtual KNNClassifier<K, V> Train(ICollection<RVFDatum<K, V>> instances)
		{
			KNNClassifier<K, V> classifier = new KNNClassifier<K, V>(k, weightedVotes, l2NormalizeVectors);
			classifier.AddInstances(instances);
			return classifier;
		}

		/// <summary>
		/// Given a set of vectors, and a mapping from each vector to its class label,
		/// generates the sets of instances used to perform classifications and returns
		/// the corresponding K-NN classifier.
		/// </summary>
		/// <remarks>
		/// Given a set of vectors, and a mapping from each vector to its class label,
		/// generates the sets of instances used to perform classifications and returns
		/// the corresponding K-NN classifier.
		/// NOTE: if l2NormalizeVectors is T, creates a copy and applies L2Normalize to it.
		/// </remarks>
		public virtual KNNClassifier<K, V> Train(ICollection<ICounter<V>> vectors, IDictionary<V, K> labelMap)
		{
			KNNClassifier<K, V> classifier = new KNNClassifier<K, V>(k, weightedVotes, l2NormalizeVectors);
			ICollection<RVFDatum<K, V>> instances = new List<RVFDatum<K, V>>();
			foreach (ICounter<V> vector in vectors)
			{
				K label = labelMap[vector];
				RVFDatum<K, V> datum;
				if (l2NormalizeVectors)
				{
					datum = new RVFDatum<K, V>(Counters.L2Normalize(new ClassicCounter<V>(vector)), label);
				}
				else
				{
					datum = new RVFDatum<K, V>(vector, label);
				}
				instances.Add(datum);
			}
			classifier.AddInstances(instances);
			return classifier;
		}

		/// <summary>
		/// Given a CollectionValued Map of vectors, treats outer key as label for each
		/// set of inner vectors.
		/// </summary>
		/// <remarks>
		/// Given a CollectionValued Map of vectors, treats outer key as label for each
		/// set of inner vectors.
		/// NOTE: if l2NormalizeVectors is T, creates a copy of each vector and applies
		/// l2Normalize to it.
		/// </remarks>
		public virtual KNNClassifier<K, V> Train(CollectionValuedMap<K, ICounter<V>> vecBag)
		{
			KNNClassifier<K, V> classifier = new KNNClassifier<K, V>(k, weightedVotes, l2NormalizeVectors);
			ICollection<RVFDatum<K, V>> instances = new List<RVFDatum<K, V>>();
			foreach (K label in vecBag.Keys)
			{
				RVFDatum<K, V> datum;
				foreach (ICounter<V> vector in vecBag[label])
				{
					if (l2NormalizeVectors)
					{
						datum = new RVFDatum<K, V>(Counters.L2Normalize(new ClassicCounter<V>(vector)), label);
					}
					else
					{
						datum = new RVFDatum<K, V>(vector, label);
					}
					instances.Add(datum);
				}
			}
			classifier.AddInstances(instances);
			return classifier;
		}
	}
}
