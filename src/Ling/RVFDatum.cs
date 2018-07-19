using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A basic implementation of the Datum interface that can be constructed with a
	/// Collection of features and one more more labels.
	/// </summary>
	/// <remarks>
	/// A basic implementation of the Datum interface that can be constructed with a
	/// Collection of features and one more more labels. The features must be
	/// specified at construction, but the labels can be set and/or changed later.
	/// </remarks>
	/// <author>
	/// Jenny Finkel
	/// <a href="mailto:jrfinkel@stanford.edu">jrfinkel@stanford.edu</a>
	/// </author>
	/// <author>Sarah Spikes (sdspikes@stanford.edu) [templatized]</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class RVFDatum<L, F> : IDatum<L, F>
	{
		private const long serialVersionUID = -255312811814660438L;

		/// <summary>features for this Datum</summary>
		private readonly ICounter<F> features;

		/// <summary>labels for this Datum.</summary>
		/// <remarks>labels for this Datum. Invariant: always non-null</remarks>
		private L label;

		/// <summary>Id of this instance</summary>
		private string id = null;

		/// <summary>Constructs a new RVFDatum with the given features and label.</summary>
		public RVFDatum(ICounter<F> features, L label)
		{
			// = null;
			this.features = features;
			SetLabel(label);
		}

		/// <summary>Constructs a new RVFDatum taking the data from a Datum.</summary>
		/// <remarks>
		/// Constructs a new RVFDatum taking the data from a Datum. <i>Implementation
		/// note:</i> This constructor allocates its own counter over features, but is
		/// only guaranteed correct if the label and feature names are immutable.
		/// </remarks>
		/// <param name="m">The Datum to copy.</param>
		public RVFDatum(IDatum<L, F> m)
		{
			this.features = new ClassicCounter<F>();
			foreach (F key in m.AsFeatures())
			{
				features.IncrementCount(key, 1.0);
			}
			SetLabel(m.Label());
		}

		/// <summary>Constructs a new RVFDatum with the given features and no labels.</summary>
		public RVFDatum(ICounter<F> features)
		{
			this.features = features;
		}

		/// <summary>Constructs a new RVFDatum with no features or labels.</summary>
		public RVFDatum()
			: this((ClassicCounter<F>)null)
		{
		}

		/// <summary>Returns the Counter of features and values</summary>
		public virtual ICounter<F> AsFeaturesCounter()
		{
			return features;
		}

		/// <summary>Returns the list of features without values</summary>
		public virtual ICollection<F> AsFeatures()
		{
			return features.KeySet();
		}

		/// <summary>
		/// Removes all currently assigned Labels for this Datum then adds the given
		/// Label.
		/// </summary>
		/// <remarks>
		/// Removes all currently assigned Labels for this Datum then adds the given
		/// Label. Calling <tt>setLabel(null)</tt> effectively clears all labels.
		/// </remarks>
		public virtual void SetLabel(L label)
		{
			this.label = label;
		}

		/// <summary>Sets id for this instance</summary>
		/// <param name="id"/>
		public virtual void SetID(string id)
		{
			this.id = id;
		}

		/// <summary>
		/// Returns a String representation of this BasicDatum (lists features and
		/// labels).
		/// </summary>
		public override string ToString()
		{
			return "RVFDatum[id=" + id + ", features=" + AsFeaturesCounter() + ",label=" + Label() + "]";
		}

		public virtual L Label()
		{
			return label;
		}

		public virtual ICollection<L> Labels()
		{
			return Java.Util.Collections.SingletonList(label);
		}

		public virtual double GetFeatureCount(F feature)
		{
			return features.GetCount(feature);
		}

		public virtual string Id()
		{
			return id;
		}

		/// <summary>
		/// Returns whether the given RVFDatum contains the same features with the same
		/// values as this RVFDatum.
		/// </summary>
		/// <remarks>
		/// Returns whether the given RVFDatum contains the same features with the same
		/// values as this RVFDatum. An RVFDatum can only be equal to another RVFDatum.
		/// <i>Implementation note:</i> Doesn't check the labels, should we change
		/// this?
		/// </remarks>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Ling.RVFDatum))
			{
				return (false);
			}
			Edu.Stanford.Nlp.Ling.RVFDatum<L, F> d = (Edu.Stanford.Nlp.Ling.RVFDatum<L, F>)o;
			return features.Equals(d.AsFeaturesCounter());
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override int GetHashCode()
		{
			return features.GetHashCode();
		}
	}
}
