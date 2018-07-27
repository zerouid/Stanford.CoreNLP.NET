using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>The representation of Datums used internally in CRFClassifier.</summary>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class CRFDatum<Feat, Lab>
	{
		/// <summary>Features for this Datum.</summary>
		private readonly IList<FEAT> features;

		private readonly LAB label;

		private readonly IList<double[]> featureVals;

		/// <summary>Constructs a new BasicDatum with the given features and label.</summary>
		/// <param name="features">The features of the CRFDatum</param>
		/// <param name="label">The label of the CRFDatum</param>
		public CRFDatum(IList<FEAT> features, LAB label, IList<double[]> featureVals)
		{
			// featureVals holds the (optional) feature value for non-boolean features
			// such as the ones used in continuous vector space embeddings
			this.features = features;
			this.label = label;
			this.featureVals = featureVals;
		}

		/// <summary>Returns the collection that this BasicDatum was constructed with.</summary>
		/// <returns>the collection that this BasicDatum was constructed with.</returns>
		public virtual IList<FEAT> AsFeatures()
		{
			return features;
		}

		/// <summary>Returns the double array containing the feature values.</summary>
		/// <returns>
		/// The double array that contains the feature values matching each feature as
		/// returned by
		/// <c>asFeatures()</c>
		/// </returns>
		public virtual IList<double[]> AsFeatureVals()
		{
			return featureVals;
		}

		/// <summary>Returns the label for this Datum, or null if none have been set.</summary>
		/// <returns>The label for this Datum, or null if none have been set.</returns>
		public virtual LAB Label()
		{
			return label;
		}

		/// <summary>Returns a String representation of this BasicDatum (lists features and labels).</summary>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("CRFDatum[\n");
			sb.Append("    label=").Append(label).Append('\n');
			for (int i = 0; i < sz; i++)
			{
				sb.Append("    features(").Append(i).Append("):").Append(features[i]);
				sb.Append(", val=").Append(Arrays.ToString(featureVals[i]));
				sb.Append('\n');
			}
			sb.Append(']');
			return sb.ToString();
		}

		/// <summary>Returns whether the given Datum contains the same features as this Datum.</summary>
		/// <remarks>
		/// Returns whether the given Datum contains the same features as this Datum.
		/// Doesn't check the labels, should we change this?
		/// (CDM Feb 2012: Also doesn't correctly respect the contract for equals,
		/// since it gives one way equality with other Datum's.)
		/// </remarks>
		/// <param name="o">The object to test equality with</param>
		/// <returns>Whether it is equal to this CRFDatum in terms of features</returns>
		public override bool Equals(object o)
		{
			if (!(o is IDatum))
			{
				return (false);
			}
			IDatum<object, object> d = (IDatum<object, object>)o;
			return features.Equals(d.AsFeatures());
		}

		public override int GetHashCode()
		{
			return features.GetHashCode();
		}

		private const long serialVersionUID = -8345554365027671190L;
	}
}
