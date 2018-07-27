using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// Implementation of Generalized Expectation Objective function for
	/// an I.I.D.
	/// </summary>
	/// <remarks>
	/// Implementation of Generalized Expectation Objective function for
	/// an I.I.D. log-linear model. See Mann and McCallum, ACL 2008 for GE in CRFs.
	/// This code, however, is just for log-linear model
	/// IMPORTANT: the current implementation is only correct as long as
	/// the labeled features passed to GE are binary.
	/// However, other features are allowed to be real valued.
	/// The original paper also discusses GE only for binary features.
	/// </remarks>
	/// <author>Ramesh Nallapati (nmramesh@cs.stanford.edu)</author>
	public class GeneralizedExpectationObjectiveFunction<L, F> : AbstractCachingDiffFunction
	{
		private readonly GeneralDataset<L, F> labeledDataset;

		private readonly IList<IDatum<L, F>> unlabeledDataList;

		private readonly IList<F> geFeatures;

		private readonly LinearClassifier<L, F> classifier;

		private double[][] geFeature2EmpiricalDist;

		private IList<IList<int>> geFeature2DatumList;

		private readonly int numFeatures;

		private readonly int numClasses;

		//empirical label distributions of each feature. Really final but java won't let us.
		//an inverted list of active unlabeled documents for each feature. Really final but java won't let us.
		public override int DomainDimension()
		{
			return numFeatures * numClasses;
		}

		internal virtual int ClassOf(int index)
		{
			return index % numClasses;
		}

		internal virtual int FeatureOf(int index)
		{
			return index / numClasses;
		}

		protected internal virtual int IndexOf(int f, int c)
		{
			return f * numClasses + c;
		}

		public virtual double[][] To2D(double[] x)
		{
			double[][] x2 = new double[numFeatures][];
			for (int i = 0; i < numFeatures; i++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					x2[i][j] = x[IndexOf(i, j)];
				}
			}
			return x2;
		}

		protected internal override void Calculate(double[] x)
		{
			classifier.SetWeights(To2D(x));
			if (derivative == null)
			{
				derivative = new double[x.Length];
			}
			else
			{
				Arrays.Fill(derivative, 0.0);
			}
			ICounter<Triple<int, int, int>> feature2classPairDerivatives = new ClassicCounter<Triple<int, int, int>>();
			value = 0.0;
			for (int n = 0; n < geFeatures.Count; n++)
			{
				//F feature = geFeatures.get(n);
				double[] modelDist = new double[numClasses];
				Arrays.Fill(modelDist, 0);
				//go over the unlabeled active data to compute expectations
				IList<int> activeData = geFeature2DatumList[n];
				foreach (int activeDatum in activeData)
				{
					IDatum<L, F> datum = unlabeledDataList[activeDatum];
					double[] probs = GetModelProbs(datum);
					for (int c = 0; c < numClasses; c++)
					{
						modelDist[c] += probs[c];
					}
					UpdateDerivative(datum, probs, feature2classPairDerivatives);
				}
				//computes p(y_d)*(1-p(y_d))*f_d for all active features.
				//now  compute the value (KL-divergence) and the final value of the derivative.
				if (activeData.Count > 0)
				{
					for (int c = 0; c < numClasses; c++)
					{
						modelDist[c] /= activeData.Count;
					}
					SmoothDistribution(modelDist);
					for (int c_1 = 0; c_1 < numClasses; c_1++)
					{
						value += -geFeature2EmpiricalDist[n][c_1] * Math.Log(modelDist[c_1]);
					}
					for (int f = 0; f < labeledDataset.FeatureIndex().Size(); f++)
					{
						for (int c_2 = 0; c_2 < numClasses; c_2++)
						{
							int wtIndex = IndexOf(f, c_2);
							for (int cPrime = 0; cPrime < numClasses; cPrime++)
							{
								derivative[wtIndex] += feature2classPairDerivatives.GetCount(new Triple<int, int, int>(f, c_2, cPrime)) * geFeature2EmpiricalDist[n][cPrime] / modelDist[cPrime];
							}
							derivative[wtIndex] /= activeData.Count;
						}
					}
				}
			}
		}

		// loop over each feature for derivative computation
		//end of if condition
		//loop over each GE feature
		private void UpdateDerivative(IDatum<L, F> datum, double[] probs, ICounter<Triple<int, int, int>> feature2classPairDerivatives)
		{
			foreach (F feature in datum.AsFeatures())
			{
				int fID = labeledDataset.featureIndex.IndexOf(feature);
				if (fID >= 0)
				{
					for (int c = 0; c < numClasses; c++)
					{
						for (int cPrime = 0; cPrime < numClasses; cPrime++)
						{
							if (cPrime == c)
							{
								feature2classPairDerivatives.IncrementCount(new Triple<int, int, int>(fID, c, cPrime), -probs[c] * (1 - probs[c]) * ValueOfFeature(feature, datum));
							}
							else
							{
								feature2classPairDerivatives.IncrementCount(new Triple<int, int, int>(fID, c, cPrime), probs[c] * probs[cPrime] * ValueOfFeature(feature, datum));
							}
						}
					}
				}
			}
		}

		/*
		* This method assumes the feature already exists in the datum.
		*/
		private double ValueOfFeature(F feature, IDatum<L, F> datum)
		{
			if (datum is RVFDatum)
			{
				return ((RVFDatum<L, F>)datum).AsFeaturesCounter().GetCount(feature);
			}
			else
			{
				return 1.0;
			}
		}

		private void ComputeEmpiricalStatistics(IList<F> geFeatures)
		{
			//allocate memory to the containers and initialize them
			geFeature2EmpiricalDist = new double[][] {  };
			geFeature2DatumList = new List<IList<int>>(geFeatures.Count);
			IDictionary<F, int> geFeatureMap = Generics.NewHashMap();
			ICollection<int> activeUnlabeledExamples = Generics.NewHashSet();
			for (int n = 0; n < geFeatures.Count; n++)
			{
				F geFeature = geFeatures[n];
				geFeature2DatumList.Add(new List<int>());
				Arrays.Fill(geFeature2EmpiricalDist[n], 0);
				geFeatureMap[geFeature] = n;
			}
			//compute the empirical label distribution for each GE feature
			for (int i = 0; i < labeledDataset.Size(); i++)
			{
				IDatum<L, F> datum = labeledDataset.GetDatum(i);
				int labelID = labeledDataset.labelIndex.IndexOf(datum.Label());
				foreach (F feature in datum.AsFeatures())
				{
					if (geFeatureMap.Contains(feature))
					{
						int geFnum = geFeatureMap[feature];
						geFeature2EmpiricalDist[geFnum][labelID]++;
					}
				}
			}
			//now normalize and smooth the label distribution for each feature.
			for (int n_1 = 0; n_1 < geFeatures.Count; n_1++)
			{
				ArrayMath.Normalize(geFeature2EmpiricalDist[n_1]);
				SmoothDistribution(geFeature2EmpiricalDist[n_1]);
			}
			//now build the inverted index from each GE feature to unlabeled datums that contain it.
			for (int i_1 = 0; i_1 < unlabeledDataList.Count; i_1++)
			{
				IDatum<L, F> datum = unlabeledDataList[i_1];
				foreach (F feature in datum.AsFeatures())
				{
					if (geFeatureMap.Contains(feature))
					{
						int geFnum = geFeatureMap[feature];
						geFeature2DatumList[geFnum].Add(i_1);
						activeUnlabeledExamples.Add(i_1);
					}
				}
			}
			System.Console.Out.WriteLine("Number of active unlabeled examples:" + activeUnlabeledExamples.Count);
		}

		private static void SmoothDistribution(double[] dist)
		{
			//perform Laplace smoothing
			double epsilon = 1e-6;
			for (int i = 0; i < dist.Length; i++)
			{
				dist[i] += epsilon;
			}
			ArrayMath.Normalize(dist);
		}

		private double[] GetModelProbs(IDatum<L, F> datum)
		{
			double[] condDist = new double[labeledDataset.NumClasses()];
			ICounter<L> probCounter = classifier.ProbabilityOf(datum);
			foreach (L label in probCounter.KeySet())
			{
				int labelID = labeledDataset.labelIndex.IndexOf(label);
				condDist[labelID] = probCounter.GetCount(label);
			}
			return condDist;
		}

		public GeneralizedExpectationObjectiveFunction(GeneralDataset<L, F> labeledDataset, IList<IDatum<L, F>> unlabeledDataList, IList<F> geFeatures)
		{
			System.Console.Out.WriteLine("Number of labeled examples:" + labeledDataset.size + "\nNumber of unlabeled examples:" + unlabeledDataList.Count);
			System.Console.Out.WriteLine("Number of GE features:" + geFeatures.Count);
			this.numFeatures = labeledDataset.NumFeatures();
			this.numClasses = labeledDataset.NumClasses();
			this.labeledDataset = labeledDataset;
			this.unlabeledDataList = unlabeledDataList;
			this.geFeatures = geFeatures;
			this.classifier = new LinearClassifier<L, F>(null, labeledDataset.featureIndex, labeledDataset.labelIndex);
			ComputeEmpiricalStatistics(geFeatures);
		}
		//empirical distributions don't change with iterations, so compute them only once.
		//model distributions will have to be recomputed every iteration though.
	}
}
