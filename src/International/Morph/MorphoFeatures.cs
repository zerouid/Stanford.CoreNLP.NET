using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.International.Morph
{
	/// <summary>Holds a set of morphosyntactic features for a given surface form.</summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class MorphoFeatures
	{
		private const long serialVersionUID = -3893316324305154940L;

		public const string KeyValDelim = ":";

		protected internal readonly IDictionary<MorphoFeatureSpecification.MorphoFeatureType, string> fSpec;

		protected internal string altTag;

		public MorphoFeatures()
		{
			fSpec = Generics.NewHashMap();
		}

		public MorphoFeatures(Edu.Stanford.Nlp.International.Morph.MorphoFeatures other)
			: this()
		{
			foreach (KeyValuePair<MorphoFeatureSpecification.MorphoFeatureType, string> entry in other.fSpec)
			{
				this.fSpec[entry.Key] = entry.Value;
			}
			this.altTag = other.altTag;
		}

		public virtual void AddFeature(MorphoFeatureSpecification.MorphoFeatureType feat, string val)
		{
			fSpec[feat] = val;
		}

		public virtual bool HasFeature(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			return fSpec.Contains(feat);
		}

		public virtual string GetValue(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			return HasFeature(feat) ? fSpec[feat] : string.Empty;
		}

		public virtual int NumFeatureMatches(Edu.Stanford.Nlp.International.Morph.MorphoFeatures other)
		{
			int nMatches = 0;
			foreach (KeyValuePair<MorphoFeatureSpecification.MorphoFeatureType, string> fPair in fSpec)
			{
				if (other.HasFeature(fPair.Key) && other.GetValue(fPair.Key).Equals(fPair.Value))
				{
					nMatches++;
				}
			}
			return nMatches;
		}

		public virtual int NumActiveFeatures()
		{
			return fSpec.Keys.Count;
		}

		/// <summary>Build a POS tag consisting of a base category plus inflectional features.</summary>
		/// <param name="baseTag"/>
		/// <returns>the tag</returns>
		public virtual string GetTag(string baseTag)
		{
			return baseTag + ToString();
		}

		public virtual void SetAltTag(string tag)
		{
			altTag = tag;
		}

		/// <summary>An alternate tag form than the one produced by getTag().</summary>
		/// <remarks>
		/// An alternate tag form than the one produced by getTag(). Subclasses
		/// may want to use this form to implement someone else's tagset (e.g., CC, ERTS, etc.)
		/// </remarks>
		/// <returns>the tag</returns>
		public virtual string GetAltTag()
		{
			return altTag;
		}

		/// <summary>Assumes that the tag string has been formed using a call to getTag().</summary>
		/// <remarks>
		/// Assumes that the tag string has been formed using a call to getTag(). As such,
		/// it removes the basic category from the feature string.
		/// <p>
		/// Note that this method returns a <b>new</b> MorphoFeatures object. As a result, it
		/// behaves like a static method, but is non-static so that subclasses can override
		/// this method.
		/// </remarks>
		/// <param name="str"/>
		public virtual Edu.Stanford.Nlp.International.Morph.MorphoFeatures FromTagString(string str)
		{
			IList<string> feats = Arrays.AsList(str.Split("\\-"));
			Edu.Stanford.Nlp.International.Morph.MorphoFeatures mFeats = new Edu.Stanford.Nlp.International.Morph.MorphoFeatures();
			foreach (string fPair in feats)
			{
				string[] keyValue = fPair.Split(KeyValDelim);
				if (keyValue.Length != 2)
				{
					//Manual state split annotations
					continue;
				}
				MorphoFeatureSpecification.MorphoFeatureType fName = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(keyValue[0].Trim());
				mFeats.AddFeature(fName, keyValue[1].Trim());
			}
			return mFeats;
		}

		/// <summary>values() returns the values in the order in which they are declared.</summary>
		/// <remarks>
		/// values() returns the values in the order in which they are declared. Thus we will not have
		/// the case where two feature types can yield two strings:
		/// -feat1:A-feat2:B
		/// -feat2:B-feat1:A
		/// </remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (MorphoFeatureSpecification.MorphoFeatureType feat in MorphoFeatureSpecification.MorphoFeatureType.Values())
			{
				if (fSpec.Contains(feat))
				{
					sb.Append(string.Format("-%s%s%s", feat.ToString(), KeyValDelim, fSpec[feat]));
				}
			}
			return sb.ToString();
		}
	}
}
