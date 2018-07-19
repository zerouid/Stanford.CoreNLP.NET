using System.Collections.Generic;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 10/20/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This is a wrapper function to keep a namespace of namespace of recognized features, so that building a set of
	/// ConcatVectors for featurizing a model is easier and more intuitive. It's actually quite simple, and threadsafe.
	/// </author>
	public class ConcatVectorNamespace
	{
		internal readonly IDictionary<string, int> featureToIndex = new Dictionary<string, int>();

		internal readonly IDictionary<string, IDictionary<string, int>> sparseFeatureIndex = new Dictionary<string, IDictionary<string, int>>();

		internal readonly IDictionary<string, IDictionary<int, string>> reverseSparseFeatureIndex = new Dictionary<string, IDictionary<int, string>>();

		/// <summary>Creates a new vector that is appropriately sized to accommodate all the features that have been named so far.</summary>
		/// <returns>a new, empty ConcatVector</returns>
		public virtual ConcatVector NewVector()
		{
			return new ConcatVector(featureToIndex.Count);
		}

		/// <summary>
		/// This constructs a fresh vector that is sized correctly to accommodate all the known sparse values for vectors
		/// that are possibly sparse.
		/// </summary>
		/// <returns>
		/// a new, internally correctly sized ConcatVector that will work correctly as weights for features from
		/// this namespace;
		/// </returns>
		public virtual ConcatVector NewWeightsVector()
		{
			ConcatVector vector = new ConcatVector(featureToIndex.Count);
			foreach (string s in sparseFeatureIndex.Keys)
			{
				int size = sparseFeatureIndex[s].Count;
				vector.SetDenseComponent(EnsureFeature(s), new double[size]);
			}
			return vector;
		}

		/// <summary>
		/// An optimization, this lets clients inform the ConcatVectorNamespace of how many features to expect, so
		/// that we can avoid resizing ConcatVectors.
		/// </summary>
		/// <param name="featureName">the feature to add to our index</param>
		public virtual int EnsureFeature(string featureName)
		{
			lock (featureToIndex)
			{
				if (!featureToIndex.Contains(featureName))
				{
					featureToIndex[featureName] = featureToIndex.Count;
				}
				return featureToIndex[featureName];
			}
		}

		/// <summary>
		/// An optimization, this lets clients inform the ConcatVectorNamespace of how many sparse feature components to
		/// expect, again so that we can avoid resizing ConcatVectors.
		/// </summary>
		/// <param name="featureName">the feature to use in our index</param>
		/// <param name="index">the sparse value to ensure is available</param>
		public virtual int EnsureSparseFeature(string featureName, string index)
		{
			EnsureFeature(featureName);
			lock (sparseFeatureIndex)
			{
				if (!sparseFeatureIndex.Contains(featureName))
				{
					sparseFeatureIndex[featureName] = new Dictionary<string, int>();
					reverseSparseFeatureIndex[featureName] = new Dictionary<int, string>();
				}
			}
			IDictionary<string, int> sparseIndex = sparseFeatureIndex[featureName];
			IDictionary<int, string> reverseSparseIndex = reverseSparseFeatureIndex[featureName];
			lock (sparseIndex)
			{
				if (!sparseIndex.Contains(index))
				{
					reverseSparseIndex[sparseIndex.Count] = index;
					sparseIndex[index] = sparseIndex.Count;
				}
				return sparseIndex[index];
			}
		}

		/// <summary>
		/// This adds a dense feature to a vector, setting the appropriate component of the given vector to the passed in
		/// value.
		/// </summary>
		/// <param name="vector">the vector</param>
		/// <param name="featureName">the feature whose value to set</param>
		/// <param name="value">the value we want to set this vector to</param>
		public virtual void SetDenseFeature(ConcatVector vector, string featureName, double[] value)
		{
			vector.SetDenseComponent(EnsureFeature(featureName), value);
		}

		/// <summary>
		/// This adds a sparse feature to a vector, setting the appropriate component of the given vector to the passed in
		/// value.
		/// </summary>
		/// <param name="vector">the vector</param>
		/// <param name="featureName">the feature whose value to set</param>
		/// <param name="index">the index of the one-hot vector to set, as a string, which we will translate into a mapping</param>
		/// <param name="value">the value we want to set this one-hot index to</param>
		public virtual void SetSparseFeature(ConcatVector vector, string featureName, string index, double value)
		{
			vector.SetSparseComponent(EnsureFeature(featureName), EnsureSparseFeature(featureName, index), value);
		}

		/// <summary>This prints out a ConcatVector by mapping to the namespace, to make debugging learning algorithms easier.</summary>
		/// <param name="vector">the vector to print</param>
		/// <param name="bw">the output stream to write to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void DebugVector(ConcatVector vector, BufferedWriter bw)
		{
			foreach (string key in featureToIndex.Keys)
			{
				bw.Write(key);
				bw.Write(":\n");
				int i = featureToIndex[key];
				if (vector.IsComponentSparse(i))
				{
					DebugFeatureValue(key, vector.GetSparseIndex(i), vector, bw);
				}
				else
				{
					double[] arr = vector.GetDenseComponent(i);
					for (int j = 0; j < arr.Length; j++)
					{
						DebugFeatureValue(key, j, vector, bw);
					}
				}
			}
		}

		/// <summary>This writes a feature's individual value, using the human readable name if possible, to a StringBuilder</summary>
		/// <exception cref="System.IO.IOException"/>
		private void DebugFeatureValue(string feature, int index, ConcatVector vector, BufferedWriter bw)
		{
			bw.Write("\t");
			if (sparseFeatureIndex.Contains(feature) && sparseFeatureIndex[feature].Values.Contains(index))
			{
				// we can map this index to an interpretable string, so we do
				bw.Write(reverseSparseFeatureIndex[feature][index]);
			}
			else
			{
				// we can't map this to a useful string, so we default to the number
				bw.Write(int.ToString(index));
			}
			bw.Write(": ");
			bw.Write(double.ToString(vector.GetValueAt(featureToIndex[feature], index)));
			bw.Write("\n");
		}
	}
}
