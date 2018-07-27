using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A multinomial logistic regression classifier.</summary>
	/// <remarks>
	/// A multinomial logistic regression classifier. Please see FlippingProbsLogisticClassifierFactory
	/// or ShiftParamsLogisticClassifierFactory for example use cases.
	/// This is classic multinomial logistic regression where you have one reference class (the last one) and
	/// (numClasses - 1) times numFeatures weights, unlike the maxent/softmax regression we more normally use.
	/// </remarks>
	/// <author>jtibs</author>
	[System.Serializable]
	public class MultinomialLogisticClassifier<L, F> : IProbabilisticClassifier<L, F>, IRVFClassifier<L, F>
	{
		private const long serialVersionUID = 1L;

		private readonly double[][] weights;

		private readonly IIndex<F> featureIndex;

		private readonly IIndex<L> labelIndex;

		/// <param name="weights">
		/// A (numClasses - 1) by numFeatures matrix that holds the weight array for each
		/// class. Note that only (numClasses - 1) rows are needed, as the probability for last class is
		/// uniquely determined by the others.
		/// </param>
		public MultinomialLogisticClassifier(double[][] weights, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			this.featureIndex = featureIndex;
			this.labelIndex = labelIndex;
			this.weights = weights;
		}

		public virtual ICollection<L> Labels()
		{
			return labelIndex.ObjectsList();
		}

		public virtual L ClassOf(IDatum<L, F> example)
		{
			return Counters.Argmax(ScoresOf(example));
		}

		public virtual ICounter<L> ScoresOf(IDatum<L, F> example)
		{
			return LogProbabilityOf(example);
		}

		public virtual L ClassOf(RVFDatum<L, F> example)
		{
			return ClassOf((IDatum<L, F>)example);
		}

		public virtual ICounter<L> ScoresOf(RVFDatum<L, F> example)
		{
			return ScoresOf((IDatum<L, F>)example);
		}

		public virtual ICounter<L> ProbabilityOf(IDatum<L, F> example)
		{
			// calculate the feature indices and feature values
			int[] featureIndices = LogisticUtils.IndicesOf(example.AsFeatures(), featureIndex);
			double[] featureValues;
			if (example is RVFDatum<object, object>)
			{
				ICollection<double> featureValuesCollection = ((RVFDatum<object, object>)example).AsFeaturesCounter().Values();
				featureValues = LogisticUtils.ConvertToArray(featureValuesCollection);
			}
			else
			{
				featureValues = new double[example.AsFeatures().Count];
				Arrays.Fill(featureValues, 1.0);
			}
			// calculate probability of each class
			ICounter<L> result = new ClassicCounter<L>();
			int numClasses = labelIndex.Size();
			double[] sigmoids = LogisticUtils.CalculateSigmoids(weights, featureIndices, featureValues);
			for (int c = 0; c < numClasses; c++)
			{
				L label = labelIndex.Get(c);
				result.IncrementCount(label, sigmoids[c]);
			}
			return result;
		}

		public virtual ICounter<L> LogProbabilityOf(IDatum<L, F> example)
		{
			ICounter<L> result = ProbabilityOf(example);
			Counters.LogInPlace(result);
			return result;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private static Edu.Stanford.Nlp.Classify.MultinomialLogisticClassifier<LL, FF> Load<Ll, Ff>(string path)
		{
			System.Console.Error.Write("Loading classifier from " + path + "... ");
			ObjectInputStream @in = new ObjectInputStream(new FileInputStream(path));
			double[][] myWeights = ErasureUtils.UncheckedCast(@in.ReadObject());
			IIndex<FF> myFeatureIndex = ErasureUtils.UncheckedCast(@in.ReadObject());
			IIndex<LL> myLabelIndex = ErasureUtils.UncheckedCast(@in.ReadObject());
			@in.Close();
			System.Console.Error.WriteLine("done.");
			return new Edu.Stanford.Nlp.Classify.MultinomialLogisticClassifier<LL, FF>(myWeights, myFeatureIndex, myLabelIndex);
		}

		/// <exception cref="System.IO.IOException"/>
		private void Save(string path)
		{
			System.Console.Out.Write("Saving classifier to " + path + "... ");
			// make sure the directory specified by path exists
			int lastSlash = path.LastIndexOf(File.separator);
			if (lastSlash > 0)
			{
				File dir = new File(Sharpen.Runtime.Substring(path, 0, lastSlash));
				if (!dir.Exists())
				{
					dir.Mkdirs();
				}
			}
			ObjectOutputStream @out = new ObjectOutputStream(new FileOutputStream(path));
			@out.WriteObject(weights);
			@out.WriteObject(featureIndex);
			@out.WriteObject(labelIndex);
			@out.Close();
			System.Console.Out.WriteLine("done.");
		}

		public virtual IDictionary<L, ICounter<F>> WeightsAsGenericCounter()
		{
			IDictionary<L, ICounter<F>> allweights = new Dictionary<L, ICounter<F>>();
			for (int i = 0; i < weights.Length; i++)
			{
				ICounter<F> c = new ClassicCounter<F>();
				L label = labelIndex.Get(i);
				double[] w = weights[i];
				foreach (F f in featureIndex)
				{
					int indexf = featureIndex.IndexOf(f);
					if (w[indexf] != 0.0)
					{
						c.SetCount(f, w[indexf]);
					}
				}
				allweights[label] = c;
			}
			return allweights;
		}
	}
}
