using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Stats;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Class for filtering out input features and producing feature conjunctions.</summary>
	/// <author>Kevin Clark</author>
	public class MetaFeatureExtractor
	{
		public enum PairConjunction
		{
			First,
			Last,
			Both
		}

		public enum SingleConjunction
		{
			Index,
			IndexCurrent,
			IndexOther,
			IndexBoth,
			IndexLast
		}

		private readonly bool neTypeConjuntion;

		private readonly bool anaphoricityClassifier;

		private readonly ICollection<MetaFeatureExtractor.PairConjunction> pairConjunctions;

		private readonly ICollection<MetaFeatureExtractor.SingleConjunction> singleConjunctions;

		private readonly IList<string> disallowedPrefixes;

		private readonly string str;

		public class Builder
		{
			private bool anaphoricityClassifier = false;

			private IList<MetaFeatureExtractor.PairConjunction> pairConjunctions = Arrays.AsList(new MetaFeatureExtractor.PairConjunction[] { MetaFeatureExtractor.PairConjunction.Last, MetaFeatureExtractor.PairConjunction.First, MetaFeatureExtractor.PairConjunction
				.Both });

			private IList<MetaFeatureExtractor.SingleConjunction> singleConjunctions = Arrays.AsList(new MetaFeatureExtractor.SingleConjunction[] { MetaFeatureExtractor.SingleConjunction.Index, MetaFeatureExtractor.SingleConjunction.IndexCurrent, MetaFeatureExtractor.SingleConjunction
				.IndexBoth });

			private IList<string> disallowedPrefixes = new List<string>();

			private bool useNEType = true;

			public virtual MetaFeatureExtractor.Builder AnaphoricityClassifier(bool anaphoricityClassifier)
			{
				this.anaphoricityClassifier = anaphoricityClassifier;
				return this;
			}

			public virtual MetaFeatureExtractor.Builder PairConjunctions(MetaFeatureExtractor.PairConjunction[] pairConjunctions)
			{
				this.pairConjunctions = Arrays.AsList(pairConjunctions);
				return this;
			}

			public virtual MetaFeatureExtractor.Builder SingleConjunctions(MetaFeatureExtractor.SingleConjunction[] singleConjunctions)
			{
				this.singleConjunctions = Arrays.AsList(singleConjunctions);
				return this;
			}

			public virtual MetaFeatureExtractor.Builder DisallowedPrefixes(string[] disallowedPrefixes)
			{
				this.disallowedPrefixes = Arrays.AsList(disallowedPrefixes);
				return this;
			}

			public virtual MetaFeatureExtractor.Builder UseNEType(bool useNEType)
			{
				this.useNEType = useNEType;
				return this;
			}

			public virtual MetaFeatureExtractor Build()
			{
				return new MetaFeatureExtractor(this);
			}
		}

		public static MetaFeatureExtractor.Builder NewBuilder()
		{
			return new MetaFeatureExtractor.Builder();
		}

		public MetaFeatureExtractor(MetaFeatureExtractor.Builder builder)
		{
			anaphoricityClassifier = builder.anaphoricityClassifier;
			if (anaphoricityClassifier)
			{
				pairConjunctions = new HashSet<MetaFeatureExtractor.PairConjunction>();
			}
			else
			{
				pairConjunctions = new HashSet<MetaFeatureExtractor.PairConjunction>(builder.pairConjunctions);
			}
			singleConjunctions = new HashSet<MetaFeatureExtractor.SingleConjunction>(builder.singleConjunctions);
			disallowedPrefixes = builder.disallowedPrefixes;
			neTypeConjuntion = builder.useNEType;
			str = StatisticalCorefTrainer.FieldValues(builder);
		}

		public static MetaFeatureExtractor AnaphoricityMFE()
		{
			return MetaFeatureExtractor.NewBuilder().SingleConjunctions(new MetaFeatureExtractor.SingleConjunction[] { MetaFeatureExtractor.SingleConjunction.Index, MetaFeatureExtractor.SingleConjunction.IndexLast }).DisallowedPrefixes(new string[] { "parent-word"
				 }).AnaphoricityClassifier(true).Build();
		}

		public static ICounter<string> FilterOut(ICounter<string> c, IList<string> disallowedPrefixes)
		{
			ICounter<string> c2 = new ClassicCounter<string>();
			foreach (KeyValuePair<string, double> e in c.EntrySet())
			{
				bool allowed = true;
				foreach (string prefix in disallowedPrefixes)
				{
					allowed &= !e.Key.StartsWith(prefix);
				}
				if (allowed)
				{
					c2.IncrementCount(e.Key, e.Value);
				}
			}
			return c2;
		}

		public virtual ICounter<string> GetFeatures(Example example, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor)
		{
			ICounter<string> features = new ClassicCounter<string>();
			ICounter<string> pairFeatures = new ClassicCounter<string>();
			ICounter<string> features1 = new ClassicCounter<string>();
			ICounter<string> features2 = compressor.Uncompress(mentionFeatures[example.mentionId2]);
			if (!example.IsNewLink())
			{
				System.Diagnostics.Debug.Assert((!anaphoricityClassifier));
				pairFeatures = compressor.Uncompress(example.pairwiseFeatures);
				features1 = compressor.Uncompress(mentionFeatures[example.mentionId1]);
			}
			else
			{
				features2.IncrementCount("bias");
			}
			if (!disallowedPrefixes.IsEmpty())
			{
				features1 = FilterOut(features1, disallowedPrefixes);
				features2 = FilterOut(features2, disallowedPrefixes);
				pairFeatures = FilterOut(pairFeatures, disallowedPrefixes);
			}
			IList<string> ids1 = example.IsNewLink() ? new List<string>() : Identifiers(features1, example.mentionType1);
			IList<string> ids2 = Identifiers(features2, example.mentionType2);
			features.AddAll(pairFeatures);
			foreach (string id1 in ids1)
			{
				foreach (string id2 in ids2)
				{
					if (pairConjunctions.Contains(MetaFeatureExtractor.PairConjunction.First))
					{
						features.AddAll(GetConjunction(pairFeatures, "_m1=" + id1));
					}
					if (pairConjunctions.Contains(MetaFeatureExtractor.PairConjunction.Last))
					{
						features.AddAll(GetConjunction(pairFeatures, "_m2=" + id2));
					}
					if (pairConjunctions.Contains(MetaFeatureExtractor.PairConjunction.Both))
					{
						features.AddAll(GetConjunction(pairFeatures, "_ms=" + id1 + "_" + id2));
					}
					if (singleConjunctions.Contains(MetaFeatureExtractor.SingleConjunction.Index))
					{
						features.AddAll(GetConjunction(features1, "_1"));
						features.AddAll(GetConjunction(features2, "_2"));
					}
					if (singleConjunctions.Contains(MetaFeatureExtractor.SingleConjunction.IndexCurrent))
					{
						features.AddAll(GetConjunction(features1, "_1" + "_m=" + id1));
						features.AddAll(GetConjunction(features2, "_2" + "_m=" + id2));
					}
					if (singleConjunctions.Contains(MetaFeatureExtractor.SingleConjunction.IndexLast))
					{
						features.AddAll(GetConjunction(features1, "_1" + "_m2=" + id2));
						features.AddAll(GetConjunction(features2, "_2" + "_m2=" + id2));
					}
					if (singleConjunctions.Contains(MetaFeatureExtractor.SingleConjunction.IndexOther))
					{
						features.AddAll(GetConjunction(features1, "_1" + "_m=" + id2));
						features.AddAll(GetConjunction(features2, "_2" + "_m=" + id1));
					}
					if (singleConjunctions.Contains(MetaFeatureExtractor.SingleConjunction.IndexBoth))
					{
						features.AddAll(GetConjunction(features1, "_1" + "_ms=" + id1 + "_" + id2));
						features.AddAll(GetConjunction(features2, "_2" + "_ms=" + id1 + "_" + id2));
					}
				}
			}
			if (example.IsNewLink())
			{
				features.AddAll(features2);
				features.AddAll(GetConjunction(features2, "_m=" + ids2[0]));
				ICounter<string> newFeatures = new ClassicCounter<string>();
				foreach (KeyValuePair<string, double> e in features.EntrySet())
				{
					newFeatures.IncrementCount(e.Key + "_NEW", e.Value);
				}
				features = newFeatures;
			}
			return features;
		}

		private IList<string> Identifiers(ICounter<string> features, Dictionaries.MentionType mentionType)
		{
			IList<string> identifiers = new List<string>();
			if (mentionType == Dictionaries.MentionType.Pronominal)
			{
				foreach (string feature in features.KeySet())
				{
					if (feature.StartsWith("head-word="))
					{
						identifiers.Add(feature.Replace("head-word=", string.Empty));
						return identifiers;
					}
				}
			}
			else
			{
				if (neTypeConjuntion && mentionType == Dictionaries.MentionType.Proper)
				{
					foreach (string feature in features.KeySet())
					{
						if (feature.StartsWith("head-ne-type="))
						{
							identifiers.Add(mentionType.ToString() + "_" + feature.Replace("head-ne-type=", string.Empty));
							return identifiers;
						}
					}
				}
			}
			identifiers.Add(mentionType.ToString());
			return identifiers;
		}

		private static ICounter<string> GetConjunction(ICounter<string> original, string suffix)
		{
			ICounter<string> conjuction = new ClassicCounter<string>();
			foreach (KeyValuePair<string, double> e in original.EntrySet())
			{
				conjuction.IncrementCount(e.Key + suffix, e.Value);
			}
			return conjuction;
		}

		public override string ToString()
		{
			return str;
		}
	}
}
