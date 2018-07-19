using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>A max-margin mention-ranking coreference model.</summary>
	/// <author>Kevin Clark</author>
	public class MaxMarginMentionRanker : PairwiseModel
	{
		[System.Serializable]
		public sealed class ErrorType
		{
			public static readonly MaxMarginMentionRanker.ErrorType Fn = new MaxMarginMentionRanker.ErrorType(0);

			public static readonly MaxMarginMentionRanker.ErrorType FnPron = new MaxMarginMentionRanker.ErrorType(1);

			public static readonly MaxMarginMentionRanker.ErrorType Fl = new MaxMarginMentionRanker.ErrorType(2);

			public static readonly MaxMarginMentionRanker.ErrorType Wl = new MaxMarginMentionRanker.ErrorType(3);

			public readonly int id;

			private ErrorType(int id)
			{
				this.id = id;
			}
		}

		private readonly SimpleLinearClassifier.ILoss[] losses = new SimpleLinearClassifier.ILoss[MaxMarginMentionRanker.ErrorType.Values().Length];

		private readonly SimpleLinearClassifier.ILoss loss;

		public readonly double[] costs;

		public readonly bool multiplicativeCost;

		public class Builder : PairwiseModel.Builder
		{
			private double[] costs = new double[] { 1.2, 1.2, 0.5, 1.0 };

			private bool multiplicativeCost = true;

			public Builder(string name, MetaFeatureExtractor meta)
				: base(name, meta)
			{
			}

			public virtual MaxMarginMentionRanker.Builder SetCosts(double fnCost, double fnPronounCost, double faCost, double wlCost)
			{
				this.costs = new double[] { fnCost, fnPronounCost, faCost, wlCost };
				return this;
			}

			public virtual MaxMarginMentionRanker.Builder MultiplicativeCost(bool multiplicativeCost)
			{
				this.multiplicativeCost = multiplicativeCost;
				return this;
			}

			public override PairwiseModel Build()
			{
				return new MaxMarginMentionRanker(this);
			}
		}

		public static PairwiseModel.Builder NewBuilder(string name, MetaFeatureExtractor meta)
		{
			return new MaxMarginMentionRanker.Builder(name, meta);
		}

		public MaxMarginMentionRanker(MaxMarginMentionRanker.Builder builder)
			: base(builder)
		{
			costs = builder.costs;
			multiplicativeCost = builder.multiplicativeCost;
			if (multiplicativeCost)
			{
				foreach (MaxMarginMentionRanker.ErrorType et in MaxMarginMentionRanker.ErrorType.Values())
				{
					losses[et.id] = SimpleLinearClassifier.MaxMargin(builder.costs[et.id]);
				}
			}
			loss = SimpleLinearClassifier.MaxMargin(1.0);
		}

		public virtual void Learn(Example correct, Example incorrect, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor, MaxMarginMentionRanker.ErrorType errorType)
		{
			ICounter<string> cFeatures = meta.GetFeatures(correct, mentionFeatures, compressor);
			ICounter<string> iFeatures = meta.GetFeatures(incorrect, mentionFeatures, compressor);
			foreach (KeyValuePair<string, double> e in cFeatures.EntrySet())
			{
				iFeatures.DecrementCount(e.Key, e.Value);
			}
			if (multiplicativeCost)
			{
				classifier.Learn(iFeatures, 1.0, costs[errorType.id], loss);
			}
			else
			{
				classifier.Learn(iFeatures, 1.0, 1.0, losses[errorType.id]);
			}
		}
	}
}
