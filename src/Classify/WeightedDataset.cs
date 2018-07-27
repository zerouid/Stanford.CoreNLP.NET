using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Classify
{
	/// <author>Galen Andrew</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	[System.Serializable]
	public class WeightedDataset<L, F> : Dataset<L, F>
	{
		private const long serialVersionUID = -5435125789127705430L;

		protected internal float[] weights;

		public WeightedDataset(IIndex<L> labelIndex, int[] labels, IIndex<F> featureIndex, int[][] data, int size, float[] weights)
			: base(labelIndex, labels, featureIndex, data, size)
		{
			this.weights = weights;
		}

		public WeightedDataset()
			: this(10)
		{
		}

		public WeightedDataset(int initSize)
			: base(initSize)
		{
			weights = new float[initSize];
		}

		private float[] TrimToSize(float[] i)
		{
			float[] newI = new float[size];
			lock (typeof(Runtime))
			{
				System.Array.Copy(i, 0, newI, 0, size);
			}
			return newI;
		}

		public virtual float[] GetWeights()
		{
			weights = TrimToSize(weights);
			return weights;
		}

		public override float[] GetFeatureCounts()
		{
			float[] counts = new float[featureIndex.Size()];
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					counts[data[i][j]] += weights[i];
				}
			}
			return counts;
		}

		public override void Add(IDatum<L, F> d)
		{
			Add(d, 1.0f);
		}

		public override void Add(ICollection<F> features, L label)
		{
			Add(features, label, 1.0f);
		}

		public virtual void Add(IDatum<L, F> d, float weight)
		{
			Add(d.AsFeatures(), d.Label(), weight);
		}

		protected internal override void EnsureSize()
		{
			base.EnsureSize();
			if (weights.Length == size)
			{
				float[] newWeights = new float[size * 2];
				lock (typeof(Runtime))
				{
					System.Array.Copy(weights, 0, newWeights, 0, size);
				}
				weights = newWeights;
			}
		}

		public virtual void Add(ICollection<F> features, L label, float weight)
		{
			EnsureSize();
			AddLabel(label);
			AddFeatures(features);
			weights[size++] = weight;
		}

		/// <summary>Set the weight of datum i.</summary>
		/// <param name="i">The index of the datum to change the weight of.</param>
		/// <param name="weight">The weight to set</param>
		public virtual void SetWeight(int i, float weight)
		{
			weights[i] = weight;
		}

		/// <summary>Randomizes (shuffles) the data array in place.</summary>
		/// <remarks>
		/// Randomizes (shuffles) the data array in place.
		/// Needs to be redefined here because we need to randomize the weights as well.
		/// </remarks>
		public override void Randomize(long randomSeed)
		{
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpL = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpL;
				float tmpW = weights[randIndex];
				weights[randIndex] = weights[j];
				weights[j] = tmpW;
			}
		}

		/// <summary>Randomizes (shuffles) the data array in place.</summary>
		/// <remarks>
		/// Randomizes (shuffles) the data array in place.
		/// Needs to be redefined here because we need to randomize the weights as well.
		/// </remarks>
		public override void ShuffleWithSideInformation<E>(long randomSeed, IList<E> sideInformation)
		{
			if (size != sideInformation.Count)
			{
				throw new ArgumentException("shuffleWithSideInformation: sideInformation not of same size as Dataset");
			}
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpL = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpL;
				float tmpW = weights[randIndex];
				weights[randIndex] = weights[j];
				weights[j] = tmpW;
				E tmpE = sideInformation[randIndex];
				sideInformation.Set(randIndex, sideInformation[j]);
				sideInformation.Set(j, tmpE);
			}
		}
	}
}
