using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>
	/// Mengqiu Wang
	/// TODO(mengqiu) currently only works with disjoint feature sets
	/// for non-disjoint feature sets, need to recompute EHat each iteration, and multiply in the scale
	/// in EHat and E calculations for each lopExpert
	/// </author>
	public class CRFLogConditionalObjectiveFunctionForLOP : AbstractCachingDiffFunction, IHasCliquePotentialFunction
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFLogConditionalObjectiveFunctionForLOP));

		/// <summary>label indices - for all possible label sequences - for each feature</summary>
		internal IList<IIndex<CRFLabel>> labelIndices;

		internal IIndex<string> classIndex;

		internal double[][][] Ehat;

		internal double[] sumOfObservedLogPotential;

		internal double[][][][][] sumOfExpectedLogPotential;

		internal IList<ICollection<int>> featureIndicesSetArray;

		internal IList<IList<int>> featureIndicesListArray;

		internal int window;

		internal int numClasses;

		internal int[] map;

		internal int[][][][] data;

		internal double[][] lopExpertWeights;

		internal double[][][] lopExpertWeights2D;

		internal int[][] labels;

		internal int[][] learnedParamsMapping;

		internal int numLopExpert;

		internal bool backpropTraining;

		internal int domainDimension = -1;

		internal string crfType = "maxent";

		internal string backgroundSymbol;

		public static bool Verbose = false;

		internal CRFLogConditionalObjectiveFunctionForLOP(int[][][][] data, int[][] labels, double[][] lopExpertWeights, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string backgroundSymbol, int numLopExpert
			, IList<ICollection<int>> featureIndicesSetArray, IList<IList<int>> featureIndicesListArray, bool backpropTraining)
		{
			// didn't have <String> before. Added since that's what is assumed everywhere.
			// empirical counts of all the features [lopIter][feature][class]
			// empirical sum of all log potentials [lopIter]
			// sumOfExpectedLogPotential[m][i][j][lopIter][k] m-docNo;i-position;j-cliqueNo;k-label 
			// data[docIndex][tokenIndex][][]
			// lopExpertWeights[expertIter][weightIndex]
			// labels[docIndex][tokenIndex]
			this.window = window;
			this.classIndex = classIndex;
			this.numClasses = classIndex.Size();
			this.labelIndices = labelIndices;
			this.map = map;
			this.data = data;
			this.lopExpertWeights = lopExpertWeights;
			this.labels = labels;
			this.backgroundSymbol = backgroundSymbol;
			this.numLopExpert = numLopExpert;
			this.featureIndicesSetArray = featureIndicesSetArray;
			this.featureIndicesListArray = featureIndicesListArray;
			this.backpropTraining = backpropTraining;
			Initialize2DWeights();
			if (backpropTraining)
			{
				ComputeEHat();
			}
			else
			{
				LogPotential(lopExpertWeights2D);
			}
		}

		public override int DomainDimension()
		{
			if (domainDimension < 0)
			{
				domainDimension = numLopExpert;
				if (backpropTraining)
				{
					// for (int i = 0; i < map.length; i++) {
					//   domainDimension += labelIndices[map[i]].size();
					// }
					for (int i = 0; i < numLopExpert; i++)
					{
						IList<int> featureIndicesList = featureIndicesListArray[i];
						double[][] expertWeights2D = lopExpertWeights2D[i];
						foreach (int fIndex in featureIndicesList)
						{
							int len = expertWeights2D[fIndex].Length;
							domainDimension += len;
						}
					}
				}
			}
			return domainDimension;
		}

		public override double[] Initial()
		{
			double[] initial = new double[DomainDimension()];
			if (backpropTraining)
			{
				learnedParamsMapping = new int[][] {  };
				int index = 0;
				for (; index < numLopExpert; index++)
				{
					initial[index] = 1.0;
				}
				for (int i = 0; i < numLopExpert; i++)
				{
					IList<int> featureIndicesList = featureIndicesListArray[i];
					double[][] expertWeights2D = lopExpertWeights2D[i];
					foreach (int fIndex in featureIndicesList)
					{
						for (int j = 0; j < expertWeights2D[fIndex].Length; j++)
						{
							initial[index] = expertWeights2D[fIndex][j];
							learnedParamsMapping[index] = new int[] { i, fIndex, j };
							index++;
						}
					}
				}
			}
			else
			{
				Arrays.Fill(initial, 1.0);
			}
			return initial;
		}

		public virtual double[][][] Empty2D()
		{
			double[][][] d2 = new double[numLopExpert][][];
			for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
			{
				double[][] d = new double[map.Length][];
				// int index = 0;
				for (int i = 0; i < map.Length; i++)
				{
					d[i] = new double[labelIndices[map[i]].Size()];
				}
				// cdm july 2005: below array initialization isn't necessary: JLS (3rd ed.) 4.12.5
				// Arrays.fill(d[i], 0.0);
				// index += labelIndices[map[i]].size();
				d2[lopIter] = d;
			}
			return d2;
		}

		private void Initialize2DWeights()
		{
			lopExpertWeights2D = new double[numLopExpert][][];
			for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
			{
				lopExpertWeights2D[lopIter] = To2D(lopExpertWeights[lopIter], labelIndices, map);
			}
		}

		public virtual double[][] To2D(double[] weights, IList<IIndex<CRFLabel>> labelIndices, int[] map)
		{
			double[][] newWeights = new double[map.Length][];
			int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				newWeights[i] = new double[labelIndices[map[i]].Size()];
				System.Array.Copy(weights, index, newWeights[i], 0, labelIndices[map[i]].Size());
				index += labelIndices[map[i]].Size();
			}
			return newWeights;
		}

		private void ComputeEHat()
		{
			Ehat = Empty2D();
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] docData = data[m];
				int[] docLabels = labels[m];
				int[] windowLabels = new int[window];
				Arrays.Fill(windowLabels, classIndex.IndexOf(backgroundSymbol));
				if (docLabels.Length > docData.Length)
				{
					// only true for self-training
					// fill the windowLabel array with the extra docLabels
					System.Array.Copy(docLabels, 0, windowLabels, 0, windowLabels.Length);
					// shift the docLabels array left
					int[] newDocLabels = new int[docData.Length];
					System.Array.Copy(docLabels, docLabels.Length - newDocLabels.Length, newDocLabels, 0, newDocLabels.Length);
					docLabels = newDocLabels;
				}
				for (int i = 0; i < docData.Length; i++)
				{
					System.Array.Copy(windowLabels, 1, windowLabels, 0, window - 1);
					windowLabels[window - 1] = docLabels[i];
					int[][] docDataI = docData[i];
					for (int j = 0; j < docDataI.Length; j++)
					{
						// j iterates over cliques
						int[] docDataIJ = docDataI[j];
						int[] cliqueLabel = new int[j + 1];
						System.Array.Copy(windowLabels, window - 1 - j, cliqueLabel, 0, j + 1);
						CRFLabel crfLabel = new CRFLabel(cliqueLabel);
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						int observedLabelIndex = labelIndex.IndexOf(crfLabel);
						//log.info(crfLabel + " " + observedLabelIndex);
						for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
						{
							double[][] ehatOfIter = Ehat[lopIter];
							ICollection<int> indicesSet = featureIndicesSetArray[lopIter];
							foreach (int featureIdx in docDataIJ)
							{
								// k iterates over features
								if (indicesSet.Contains(featureIdx))
								{
									ehatOfIter[featureIdx][observedLabelIndex]++;
								}
							}
						}
					}
				}
			}
		}

		private void LogPotential(double[][][] learnedLopExpertWeights2D)
		{
			sumOfExpectedLogPotential = new double[data.Length][][][][];
			sumOfObservedLogPotential = new double[numLopExpert];
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] docData = data[m];
				int[] docLabels = labels[m];
				int[] windowLabels = new int[window];
				Arrays.Fill(windowLabels, classIndex.IndexOf(backgroundSymbol));
				double[][][][] sumOfELPm = new double[docData.Length][][][];
				if (docLabels.Length > docData.Length)
				{
					// only true for self-training
					// fill the windowLabel array with the extra docLabels
					System.Array.Copy(docLabels, 0, windowLabels, 0, windowLabels.Length);
					// shift the docLabels array left
					int[] newDocLabels = new int[docData.Length];
					System.Array.Copy(docLabels, docLabels.Length - newDocLabels.Length, newDocLabels, 0, newDocLabels.Length);
					docLabels = newDocLabels;
				}
				for (int i = 0; i < docData.Length; i++)
				{
					System.Array.Copy(windowLabels, 1, windowLabels, 0, window - 1);
					windowLabels[window - 1] = docLabels[i];
					double[][][] sumOfELPmi = new double[docData[i].Length][][];
					int[][] docDataI = docData[i];
					for (int j = 0; j < docDataI.Length; j++)
					{
						// j iterates over cliques
						int[] docDataIJ = docDataI[j];
						int[] cliqueLabel = new int[j + 1];
						System.Array.Copy(windowLabels, window - 1 - j, cliqueLabel, 0, j + 1);
						CRFLabel crfLabel = new CRFLabel(cliqueLabel);
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						double[][] sumOfELPmij = new double[numLopExpert][];
						int observedLabelIndex = labelIndex.IndexOf(crfLabel);
						//log.info(crfLabel + " " + observedLabelIndex);
						for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
						{
							double[] sumOfELPmijIter = new double[labelIndex.Size()];
							ICollection<int> indicesSet = featureIndicesSetArray[lopIter];
							foreach (int featureIdx in docDataIJ)
							{
								// k iterates over features
								if (indicesSet.Contains(featureIdx))
								{
									sumOfObservedLogPotential[lopIter] += learnedLopExpertWeights2D[lopIter][featureIdx][observedLabelIndex];
									// sum over potential of this clique over all possible labels, used later in calculating expected counts
									for (int l = 0; l < labelIndex.Size(); l++)
									{
										sumOfELPmijIter[l] += learnedLopExpertWeights2D[lopIter][featureIdx][l];
									}
								}
							}
							sumOfELPmij[lopIter] = sumOfELPmijIter;
						}
						sumOfELPmi[j] = sumOfELPmij;
					}
					sumOfELPm[i] = sumOfELPmi;
				}
				sumOfExpectedLogPotential[m] = sumOfELPm;
			}
		}

		public static double[] CombineAndScaleLopWeights(int numLopExpert, double[][] lopExpertWeights, double[] lopScales)
		{
			double[] newWeights = new double[lopExpertWeights[0].Length];
			for (int i = 0; i < newWeights.Length; i++)
			{
				double tempWeight = 0;
				for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
				{
					tempWeight += lopExpertWeights[lopIter][i] * lopScales[lopIter];
				}
				newWeights[i] = tempWeight;
			}
			return newWeights;
		}

		public static double[][] CombineAndScaleLopWeights2D(int numLopExpert, double[][][] lopExpertWeights2D, double[] lopScales)
		{
			double[][] newWeights = new double[lopExpertWeights2D[0].Length][];
			for (int i = 0; i < newWeights.Length; i++)
			{
				int innerDim = lopExpertWeights2D[0][i].Length;
				double[] innerWeights = new double[innerDim];
				for (int j = 0; j < innerDim; j++)
				{
					double tempWeight = 0;
					for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
					{
						tempWeight += lopExpertWeights2D[lopIter][i][j] * lopScales[lopIter];
					}
					innerWeights[j] = tempWeight;
				}
				newWeights[i] = innerWeights;
			}
			return newWeights;
		}

		public virtual double[][][] SeparateLopExpertWeights2D(double[] learnedParams)
		{
			double[][][] learnedWeights2D = Empty2D();
			for (int paramIndex = numLopExpert; paramIndex < learnedParams.Length; paramIndex++)
			{
				int[] mapping = learnedParamsMapping[paramIndex];
				learnedWeights2D[mapping[0]][mapping[1]][mapping[2]] = learnedParams[paramIndex];
			}
			return learnedWeights2D;
		}

		public virtual double[][] SeparateLopExpertWeights(double[] learnedParams)
		{
			double[][] learnedWeights = new double[numLopExpert][];
			double[][][] learnedWeights2D = SeparateLopExpertWeights2D(learnedParams);
			for (int i = 0; i < numLopExpert; i++)
			{
				learnedWeights[i] = CRFLogConditionalObjectiveFunction.To1D(learnedWeights2D[i], lopExpertWeights[i].Length);
			}
			return learnedWeights;
		}

		public virtual double[] SeparateLopScales(double[] learnedParams)
		{
			double[] rawScales = new double[numLopExpert];
			System.Array.Copy(learnedParams, 0, rawScales, 0, numLopExpert);
			return rawScales;
		}

		public virtual ICliquePotentialFunction GetCliquePotentialFunction(double[] x)
		{
			double[] rawScales = SeparateLopScales(x);
			double[] scales = ArrayMath.Softmax(rawScales);
			double[][][] learnedLopExpertWeights2D = lopExpertWeights2D;
			if (backpropTraining)
			{
				learnedLopExpertWeights2D = SeparateLopExpertWeights2D(x);
			}
			double[][] combinedWeights2D = CombineAndScaleLopWeights2D(numLopExpert, learnedLopExpertWeights2D, scales);
			return new LinearCliquePotentialFunction(combinedWeights2D);
		}

		// todo [cdm]: Below data[m] --> docData
		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			double prob = 0.0;
			// the log prob of the sequence given the model, which is the negation of value at this point
			double[][][] E = Empty2D();
			double[] eScales = new double[numLopExpert];
			double[] rawScales = SeparateLopScales(x);
			double[] scales = ArrayMath.Softmax(rawScales);
			double[][][] learnedLopExpertWeights2D = lopExpertWeights2D;
			if (backpropTraining)
			{
				learnedLopExpertWeights2D = SeparateLopExpertWeights2D(x);
				LogPotential(learnedLopExpertWeights2D);
			}
			double[][] combinedWeights2D = CombineAndScaleLopWeights2D(numLopExpert, learnedLopExpertWeights2D, scales);
			// iterate over all the documents
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] docData = data[m];
				int[] docLabels = labels[m];
				double[][][][] sumOfELPm = sumOfExpectedLogPotential[m];
				// sumOfExpectedLogPotential[m][i][j][lopIter][k] m-docNo;i-position;j-cliqueNo;k-label 
				// make a clique tree for this document
				ICliquePotentialFunction cliquePotentialFunc = new LinearCliquePotentialFunction(combinedWeights2D);
				CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunc, null);
				// compute the log probability of the document given the model with the parameters x
				int[] given = new int[window - 1];
				Arrays.Fill(given, classIndex.IndexOf(backgroundSymbol));
				if (docLabels.Length > docData.Length)
				{
					// only true for self-training
					// fill the given array with the extra docLabels
					System.Array.Copy(docLabels, 0, given, 0, given.Length);
					// shift the docLabels array left
					int[] newDocLabels = new int[docData.Length];
					System.Array.Copy(docLabels, docLabels.Length - newDocLabels.Length, newDocLabels, 0, newDocLabels.Length);
					docLabels = newDocLabels;
				}
				// iterate over the positions in this document
				for (int i = 0; i < docData.Length; i++)
				{
					int label = docLabels[i];
					double p = cliqueTree.CondLogProbGivenPrevious(i, label, given);
					if (Verbose)
					{
						log.Info("P(" + label + "|" + ArrayMath.ToString(given) + ")=" + p);
					}
					prob += p;
					System.Array.Copy(given, 1, given, 0, given.Length - 1);
					given[given.Length - 1] = label;
				}
				// compute the expected counts for this document, which we will need to compute the derivative
				// iterate over the positions in this document
				for (int i_1 = 0; i_1 < docData.Length; i_1++)
				{
					// for each possible clique at this position
					double[][][] sumOfELPmi = sumOfELPm[i_1];
					for (int j = 0; j < docData[i_1].Length; j++)
					{
						double[][] sumOfELPmij = sumOfELPmi[j];
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						// for each possible labeling for that clique
						for (int l = 0; l < labelIndex.Size(); l++)
						{
							int[] label = labelIndex.Get(l).GetLabel();
							double p = cliqueTree.Prob(i_1, label);
							// probability of these labels occurring in this clique with these features
							for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
							{
								ICollection<int> indicesSet = featureIndicesSetArray[lopIter];
								double scale = scales[lopIter];
								double expected = sumOfELPmij[lopIter][l];
								for (int innerLopIter = 0; innerLopIter < numLopExpert; innerLopIter++)
								{
									expected -= scales[innerLopIter] * sumOfELPmij[innerLopIter][l];
								}
								expected *= scale;
								eScales[lopIter] += (p * expected);
								double[][] eOfIter = E[lopIter];
								if (backpropTraining)
								{
									for (int k = 0; k < docData[i_1][j].Length; k++)
									{
										// k iterates over features
										int featureIdx = docData[i_1][j][k];
										if (indicesSet.Contains(featureIdx))
										{
											eOfIter[featureIdx][l] += p;
										}
									}
								}
							}
						}
					}
				}
			}
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunctionForLOP.calculate()");
			}
			value = -prob;
			if (Verbose)
			{
				log.Info("value is " + value);
			}
			// compute the partial derivative for each feature by comparing expected counts to empirical counts
			for (int lopIter_1 = 0; lopIter_1 < numLopExpert; lopIter_1++)
			{
				double scale = scales[lopIter_1];
				double observed = sumOfObservedLogPotential[lopIter_1];
				for (int j = 0; j < numLopExpert; j++)
				{
					observed -= scales[j] * sumOfObservedLogPotential[j];
				}
				observed *= scale;
				double expected = eScales[lopIter_1];
				derivative[lopIter_1] = (expected - observed);
				if (Verbose)
				{
					log.Info("deriv(" + lopIter_1 + ") = " + expected + " - " + observed + " = " + derivative[lopIter_1]);
				}
			}
			if (backpropTraining)
			{
				int dIndex = numLopExpert;
				for (int lopIter = 0; lopIter_1 < numLopExpert; lopIter_1++)
				{
					double scale = scales[lopIter_1];
					double[][] eOfExpert = E[lopIter_1];
					double[][] ehatOfExpert = Ehat[lopIter_1];
					IList<int> featureIndicesList = featureIndicesListArray[lopIter_1];
					foreach (int fIndex in featureIndicesList)
					{
						for (int j = 0; j < eOfExpert[fIndex].Length; j++)
						{
							derivative[dIndex++] = scale * (eOfExpert[fIndex][j] - ehatOfExpert[fIndex][j]);
							if (Verbose)
							{
								log.Info("deriv[" + lopIter_1 + "](" + fIndex + "," + j + ") = " + scale + " * (" + eOfExpert[fIndex][j] + " - " + ehatOfExpert[fIndex][j] + ") = " + derivative[dIndex - 1]);
							}
						}
					}
				}
				System.Diagnostics.Debug.Assert((dIndex == DomainDimension()));
			}
		}
	}
}
