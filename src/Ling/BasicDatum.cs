using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Basic implementation of Datum interface that can be constructed with a
	/// Collection of features and one more more labels.
	/// </summary>
	/// <remarks>
	/// Basic implementation of Datum interface that can be constructed with a
	/// Collection of features and one more more labels. The features must be
	/// specified
	/// at construction, but the labels can be set and/or changed later.
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class BasicDatum<LabelType, FeatureType> : IDatum<LabelType, FeatureType>
	{
		/// <summary>features for this Datum</summary>
		private readonly ICollection<FeatureType> features;

		/// <summary>labels for this Datum.</summary>
		/// <remarks>labels for this Datum. Invariant: always non-null</remarks>
		private readonly IList<LabelType> labels = new List<LabelType>();

		/// <summary>Constructs a new BasicDatum with the given features and labels.</summary>
		public BasicDatum(ICollection<FeatureType> features, ICollection<LabelType> labels)
			: this(features)
		{
			SetLabels(labels);
		}

		/// <summary>Constructs a new BasicDatum with the given features and label.</summary>
		public BasicDatum(ICollection<FeatureType> features, LabelType label)
			: this(features)
		{
			SetLabel(label);
		}

		/// <summary>Constructs a new BasicDatum with the given features and no labels.</summary>
		public BasicDatum(ICollection<FeatureType> features)
		{
			this.features = features;
		}

		/// <summary>Constructs a new BasicDatum with no features or labels.</summary>
		public BasicDatum()
			: this(null)
		{
		}

		/// <summary>Returns the collection that this BasicDatum was constructed with.</summary>
		public virtual ICollection<FeatureType> AsFeatures()
		{
			return (features);
		}

		/// <summary>Returns the first label for this Datum, or null if none have been set.</summary>
		public virtual LabelType Label()
		{
			return ((labels.Count > 0) ? labels[0] : null);
		}

		/// <summary>Returns the complete List of labels for this Datum, which may be empty.</summary>
		public virtual ICollection<LabelType> Labels()
		{
			return labels;
		}

		/// <summary>
		/// Removes all currently assigned Labels for this Datum then adds the
		/// given Label.
		/// </summary>
		/// <remarks>
		/// Removes all currently assigned Labels for this Datum then adds the
		/// given Label.
		/// Calling <tt>setLabel(null)</tt> effectively clears all labels.
		/// </remarks>
		public virtual void SetLabel(LabelType label)
		{
			labels.Clear();
			AddLabel(label);
		}

		/// <summary>
		/// Removes all currently assigned labels for this Datum then adds all
		/// of the given Labels.
		/// </summary>
		public virtual void SetLabels(ICollection<LabelType> labels)
		{
			this.labels.Clear();
			if (labels != null)
			{
				Sharpen.Collections.AddAll(this.labels, labels);
			}
		}

		/// <summary>
		/// Adds the given Label to the List of labels for this Datum if it is not
		/// null.
		/// </summary>
		public virtual void AddLabel(LabelType label)
		{
			if (label != null)
			{
				labels.Add(label);
			}
		}

		/// <summary>Returns a String representation of this BasicDatum (lists features and labels).</summary>
		public override string ToString()
		{
			return ("BasicDatum[features=" + AsFeatures() + ",labels=" + Labels() + "]");
		}

		/// <summary>Returns whether the given Datum contains the same features as this Datum.</summary>
		/// <remarks>
		/// Returns whether the given Datum contains the same features as this Datum.
		/// Doesn't check the labels, should we change this?
		/// </remarks>
		public override bool Equals(object o)
		{
			if (!(o is IDatum))
			{
				return (false);
			}
			IDatum<LabelType, FeatureType> d = (IDatum<LabelType, FeatureType>)o;
			return features.Equals(d.AsFeatures());
		}

		public override int GetHashCode()
		{
			return features.GetHashCode();
		}

		private const long serialVersionUID = -4857004070061779966L;
	}
}
