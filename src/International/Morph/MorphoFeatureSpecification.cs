using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.International.Morph
{
	/// <summary>Morphological feature specification for surface forms in a given language.</summary>
	/// <remarks>
	/// Morphological feature specification for surface forms in a given language.
	/// Currently supported feature names are the values of MorphFeatureType.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public abstract class MorphoFeatureSpecification
	{
		private const long serialVersionUID = -5720683653931585664L;

		public const string MorphoMark = "~#";

		public const string LemmaMark = "|||";

		public const string NoAnalysis = "XXX";

		public enum MorphoFeatureType
		{
			Tense,
			Def,
			Asp,
			Mood,
			Nnum,
			Num,
			Ngen,
			Gen,
			Case,
			Per,
			Poss,
			Voice,
			Other,
			Prop
		}

		protected internal readonly ICollection<MorphoFeatureSpecification.MorphoFeatureType> activeFeatures;

		public MorphoFeatureSpecification()
		{
			//Delimiter for associating a surface form with a morphological analysis, e.g.,
			//
			//     his~#PRP_3ms
			//
			// WSGDEBUG --
			//   Added NNUM and NGEN for nominals in Arabic
			activeFeatures = Generics.NewHashSet();
		}

		public virtual void Activate(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			activeFeatures.Add(feat);
		}

		public virtual bool IsActive(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			return activeFeatures.Contains(feat);
		}

		public abstract IList<string> GetValues(MorphoFeatureSpecification.MorphoFeatureType feat);

		public abstract MorphoFeatures StrToFeatures(string spec);

		/// <summary>Returns the lemma as pair.first() and the morph analysis as pair.second().</summary>
		public static Pair<string, string> SplitMorphString(string word, string morphStr)
		{
			if (morphStr == null || morphStr.Trim().Equals(string.Empty))
			{
				return new Pair<string, string>(word, NoAnalysis);
			}
			string[] toks = morphStr.Split(Pattern.Quote(LemmaMark));
			if (toks.Length != 2)
			{
				throw new Exception("Invalid morphology string: " + morphStr);
			}
			return new Pair<string, string>(toks[0], toks[1]);
		}

		public override string ToString()
		{
			return activeFeatures.ToString();
		}
	}
}
