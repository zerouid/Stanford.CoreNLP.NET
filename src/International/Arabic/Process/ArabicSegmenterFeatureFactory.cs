using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// Feature factory for the IOB clitic segmentation model described by
	/// Green and DeNero (2012).
	/// </summary>
	/// <author>Spence Green</author>
	/// <?/>
	[System.Serializable]
	public class ArabicSegmenterFeatureFactory<In> : FeatureFactory<IN>
		where In : CoreLabel
	{
		private const long serialVersionUID = -4560226365250020067L;

		private const string DomainMarker = "@";

		private const int MaxBefore = 5;

		private const int MaxAfter = 9;

		private const int MaxLength = 10;

		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
		}

		/// <summary>Extracts all the features from the input data at a certain index.</summary>
		/// <param name="cInfo">The complete data set as a List of WordInfo</param>
		/// <param name="loc">The index at which to extract features.</param>
		public override ICollection<string> GetCliqueFeatures(PaddedList<IN> cInfo, int loc, Clique clique)
		{
			ICollection<string> features = Generics.NewHashSet();
			if (clique == cliqueC)
			{
				AddAllInterningAndSuffixing(features, FeaturesC(cInfo, loc), "C");
			}
			else
			{
				if (clique == cliqueCpC)
				{
					AddAllInterningAndSuffixing(features, FeaturesCpC(cInfo, loc), "CpC");
				}
				else
				{
					if (clique == cliqueCp2C)
					{
						AddAllInterningAndSuffixing(features, FeaturesCp2C(cInfo, loc), "Cp2C");
					}
					else
					{
						if (clique == cliqueCp3C)
						{
							AddAllInterningAndSuffixing(features, FeaturesCp3C(cInfo, loc), "Cp3C");
						}
					}
				}
			}
			string domain = cInfo[loc].Get(typeof(CoreAnnotations.DomainAnnotation));
			if (domain != null)
			{
				ICollection<string> domainFeatures = Generics.NewHashSet();
				foreach (string feature in features)
				{
					domainFeatures.Add(feature + DomainMarker + domain);
				}
				Sharpen.Collections.AddAll(features, domainFeatures);
			}
			return features;
		}

		protected internal virtual ICollection<string> FeaturesC(PaddedList<IN> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel n = cInfo[loc + 1];
			CoreLabel n2 = cInfo[loc + 2];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charn = n.Get(typeof(CoreAnnotations.CharAnnotation));
			string charn2 = n2.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp2 = p2.Get(typeof(CoreAnnotations.CharAnnotation));
			// Default feature set...a 5 character window
			// plus a few other language-independent features
			features.Add(charc + "-c");
			features.Add(charn + "-n1");
			features.Add(charn2 + "-n2");
			features.Add(charp + "-p");
			features.Add(charp2 + "-p2");
			// Length feature 
			if (charc.Length > 1)
			{
				features.Add("length");
			}
			// Character-level class features
			bool seenPunc = false;
			bool seenDigit = false;
			for (int i = 0; i < limit; ++i)
			{
				char charcC = charc[i];
				seenPunc = seenPunc || Characters.IsPunctuation(charcC);
				seenDigit = seenDigit || char.IsDigit(charcC);
				string cuBlock = Characters.UnicodeBlockStringOf(charcC);
				features.Add(cuBlock + "-uBlock");
				string cuType = char.GetType(charcC).ToString();
				features.Add(cuType + "-uType");
			}
			if (seenPunc)
			{
				features.Add("haspunc");
			}
			if (seenDigit)
			{
				features.Add("hasdigit");
			}
			// Token-level features
			string word = c.Word();
			int index = c.Index();
			features.Add(Math.Min(MaxBefore, index) + "-before");
			features.Add(Math.Min(MaxAfter, word.Length - charc.Length - index) + "-after");
			features.Add(Math.Min(MaxLength, word.Length) + "-length");
			// Indicator transition feature
			features.Add("cliqueC");
			return features;
		}

		protected internal virtual ICollection<string> FeaturesCpC(PaddedList<IN> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			features.Add(charc + charp + "-cngram");
			// Indicator transition feature
			features.Add("cliqueCpC");
			return features;
		}

		protected internal virtual ICollection<string> FeaturesCp2C(PaddedList<IN> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp2 = p2.Get(typeof(CoreAnnotations.CharAnnotation));
			features.Add(charc + charp + charp2 + "-cngram");
			// Indicator transition feature
			features.Add("cliqueCp2C");
			return features;
		}

		protected internal virtual ICollection<string> FeaturesCp3C(PaddedList<IN> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp2 = p2.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp3 = p3.Get(typeof(CoreAnnotations.CharAnnotation));
			features.Add(charc + charp + charp2 + charp3 + "-cngram");
			// Indicator transition feature
			features.Add("cliqueCp3C");
			return features;
		}
	}
}
