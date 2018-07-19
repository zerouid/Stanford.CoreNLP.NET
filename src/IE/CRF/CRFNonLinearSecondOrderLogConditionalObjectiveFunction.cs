using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class CRFNonLinearSecondOrderLogConditionalObjectiveFunction : AbstractCachingDiffFunction, IHasCliquePotentialFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFNonLinearSecondOrderLogConditionalObjectiveFunction));

		public const int NoPrior = 0;

		public const int QuadraticPrior = 1;

		public const int HuberPrior = 2;

		public const int QuarticPrior = 3;

		internal bool useOutputLayer;

		internal bool useHiddenLayer;

		internal bool useSigmoid;

		internal SeqClassifierFlags flags;

		internal int count = 0;

		protected internal int prior;

		protected internal double sigma;

		protected internal double epsilon;

		internal Random random = new Random(2147483647L);

		/// <summary>label indices - for all possible label sequences - for each feature</summary>
		internal IList<IIndex<CRFLabel>> labelIndices;

		internal IIndex<string> classIndex;

		internal double[][] Ehat;

		internal double[][] Uhat;

		internal double[][] What;

		internal int window;

		internal int numClasses;

		internal int numHiddenUnits;

		internal int[] map;

		internal int[][][][] data;

		internal int[][] docWindowLabels;

		internal int[][] labels;

		internal int domainDimension = -1;

		internal int inputLayerSize = -1;

		internal int outputLayerSize = -1;

		internal int inputLayerSize4Edge = -1;

		internal int outputLayerSize4Edge = -1;

		internal int edgeParamCount = -1;

		internal int numNodeFeatures = -1;

		internal int numEdgeFeatures = -1;

		internal int beforeOutputWeights = -1;

		internal int originalFeatureCount = -1;

		internal int[][] weightIndices;

		internal string crfType = "maxent";

		internal string backgroundSymbol;

		public static bool Verbose = false;

		/* Use a Huber robust regression penalty (L1 except very near 0) not L2 */
		// didn't have <String> before. Added since that's what is assumed everywhere.
		// empirical counts of all the linear features [feature][class]
		// empirical counts of all the output layer features [num of class][input layer size]
		// empirical counts of all the input layer features [input layer size][featureIndex.size()]
		// hidden layer number of neuron = numHiddenUnits * numClasses
		// data[docIndex][tokenIndex][][]
		// labels[docIndex][tokenIndex]
		// for debugging
		public static int GetPriorType(string priorTypeStr)
		{
			if (priorTypeStr == null)
			{
				return QuadraticPrior;
			}
			// default
			if (Sharpen.Runtime.EqualsIgnoreCase("QUADRATIC", priorTypeStr))
			{
				return QuadraticPrior;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("HUBER", priorTypeStr))
				{
					return HuberPrior;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase("QUARTIC", priorTypeStr))
					{
						return QuarticPrior;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "NONE"))
						{
							return NoPrior;
						}
						else
						{
							throw new ArgumentException("Unknown prior type: " + priorTypeStr);
						}
					}
				}
			}
		}

		internal CRFNonLinearSecondOrderLogConditionalObjectiveFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, SeqClassifierFlags flags, int numNodeFeatures, int numEdgeFeatures
			)
			: this(data, labels, window, classIndex, labelIndices, map, QuadraticPrior, flags, numNodeFeatures, numEdgeFeatures)
		{
		}

		internal CRFNonLinearSecondOrderLogConditionalObjectiveFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, int prior, SeqClassifierFlags flags, int numNodeFeatures
			, int numEdgeFeatures)
		{
			this.window = window;
			this.classIndex = classIndex;
			this.numClasses = classIndex.Size();
			this.labelIndices = labelIndices;
			this.data = data;
			this.flags = flags;
			this.map = map;
			this.labels = labels;
			this.prior = prior;
			this.backgroundSymbol = flags.backgroundSymbol;
			this.sigma = flags.sigma;
			this.outputLayerSize = numClasses;
			this.outputLayerSize4Edge = numClasses * numClasses;
			this.numHiddenUnits = flags.numHiddenUnits;
			this.inputLayerSize = numHiddenUnits * numClasses;
			this.inputLayerSize4Edge = numHiddenUnits * numClasses * numClasses;
			this.numNodeFeatures = numNodeFeatures;
			this.numEdgeFeatures = numEdgeFeatures;
			this.useOutputLayer = flags.useOutputLayer;
			this.useHiddenLayer = flags.useHiddenLayer;
			this.useSigmoid = flags.useSigmoid;
			this.docWindowLabels = new int[data.Length][];
			if (!useOutputLayer)
			{
				log.Info("Output layer not activated, inputLayerSize must be equal to numClasses, setting it to " + numClasses);
				this.inputLayerSize = numClasses;
				this.inputLayerSize4Edge = numClasses * numClasses;
			}
			else
			{
				if (flags.softmaxOutputLayer && !(flags.sparseOutputLayer || flags.tieOutputLayer))
				{
					throw new Exception("flags.softmaxOutputLayer == true, but neither flags.sparseOutputLayer or flags.tieOutputLayer is true");
				}
			}
		}

		// empiricalCounts();
		public override int DomainDimension()
		{
			if (domainDimension < 0)
			{
				originalFeatureCount = 0;
				foreach (int aMap in map)
				{
					int s = labelIndices[aMap].Size();
					originalFeatureCount += s;
				}
				domainDimension = 0;
				domainDimension += inputLayerSize4Edge * numEdgeFeatures;
				domainDimension += inputLayerSize * numNodeFeatures;
				beforeOutputWeights = domainDimension;
				if (useOutputLayer)
				{
					if (flags.sparseOutputLayer)
					{
						domainDimension += outputLayerSize4Edge * numHiddenUnits;
						domainDimension += outputLayerSize * numHiddenUnits;
					}
					else
					{
						if (flags.tieOutputLayer)
						{
							domainDimension += 1 * numHiddenUnits;
							domainDimension += 1 * numHiddenUnits;
						}
						else
						{
							domainDimension += outputLayerSize4Edge * inputLayerSize4Edge;
							domainDimension += outputLayerSize * inputLayerSize;
						}
					}
				}
				log.Info("originalFeatureCount: " + originalFeatureCount);
				log.Info("beforeOutputWeights: " + beforeOutputWeights);
				log.Info("domainDimension: " + domainDimension);
			}
			return domainDimension;
		}

		public override double[] Initial()
		{
			double[] initial = new double[DomainDimension()];
			// randomly initialize weights
			if (useHiddenLayer || useOutputLayer)
			{
				double epsilon = 0.1;
				double twoEpsilon = epsilon * 2;
				int count = 0;
				double val = 0;
				if (flags.blockInitialize)
				{
					int interval4Edge = numEdgeFeatures / numHiddenUnits;
					for (int i = 0; i < numHiddenUnits; i++)
					{
						int lower = i * interval4Edge;
						int upper = (i + 1) * interval4Edge;
						if (i == numHiddenUnits - 1)
						{
							upper = numEdgeFeatures;
						}
						for (int j = 0; j < outputLayerSize4Edge; j++)
						{
							for (int k = 0; k < numEdgeFeatures; k++)
							{
								val = 0;
								if (k >= lower && k < upper)
								{
									val = random.NextDouble() * twoEpsilon - epsilon;
								}
								initial[count++] = val;
							}
						}
					}
					int interval = numNodeFeatures / numHiddenUnits;
					for (int i_1 = 0; i_1 < numHiddenUnits; i_1++)
					{
						int lower = i_1 * interval;
						int upper = (i_1 + 1) * interval;
						if (i_1 == numHiddenUnits - 1)
						{
							upper = numNodeFeatures;
						}
						for (int j = 0; j < outputLayerSize; j++)
						{
							for (int k = 0; k < numNodeFeatures; k++)
							{
								val = 0;
								if (k >= lower && k < upper)
								{
									val = random.NextDouble() * twoEpsilon - epsilon;
								}
								initial[count++] = val;
							}
						}
					}
					if (count != beforeOutputWeights)
					{
						throw new Exception("after blockInitialize, param Index (" + count + ") not equal to beforeOutputWeights (" + beforeOutputWeights + ")");
					}
				}
				else
				{
					for (int i = 0; i < beforeOutputWeights; i++)
					{
						val = random.NextDouble() * twoEpsilon - epsilon;
						initial[count++] = val;
					}
				}
				if (flags.sparseOutputLayer)
				{
					for (int i = 0; i < outputLayerSize4Edge; i++)
					{
						double total = 1;
						for (int j = 0; j < numHiddenUnits - 1; j++)
						{
							val = random.NextDouble() * total;
							initial[count++] = val;
							total -= val;
						}
						initial[count++] = total;
					}
					for (int i_1 = 0; i_1 < outputLayerSize; i_1++)
					{
						double total = 1;
						for (int j = 0; j < numHiddenUnits - 1; j++)
						{
							val = random.NextDouble() * total;
							initial[count++] = val;
							total -= val;
						}
						initial[count++] = total;
					}
				}
				else
				{
					if (flags.tieOutputLayer)
					{
						double total = 1;
						double sum = 0;
						for (int j = 0; j < numHiddenUnits - 1; j++)
						{
							val = random.NextDouble() * total;
							initial[count++] = val;
							total -= val;
						}
						initial[count++] = total;
						total = 1;
						sum = 0;
						for (int j_1 = 0; j_1 < numHiddenUnits - 1; j_1++)
						{
							val = random.NextDouble() * total;
							initial[count++] = val;
							total -= val;
						}
						initial[count++] = total;
					}
					else
					{
						for (int i = beforeOutputWeights; i < DomainDimension(); i++)
						{
							val = random.NextDouble() * twoEpsilon - epsilon;
							initial[count++] = val;
						}
					}
				}
				if (count != DomainDimension())
				{
					throw new Exception("after param initialization, param Index (" + count + ") not equal to domainDimension (" + DomainDimension() + ")");
				}
			}
			return initial;
		}

		private double[][] EmptyU4Edge()
		{
			int innerSize = inputLayerSize4Edge;
			if (flags.sparseOutputLayer || flags.tieOutputLayer)
			{
				innerSize = numHiddenUnits;
			}
			int outerSize = outputLayerSize4Edge;
			if (flags.tieOutputLayer)
			{
				outerSize = 1;
			}
			double[][] temp = new double[outerSize][];
			for (int i = 0; i < outerSize; i++)
			{
				temp[i] = new double[innerSize];
			}
			return temp;
		}

		private double[][] EmptyW4Edge()
		{
			double[][] temp = new double[inputLayerSize4Edge][];
			for (int i = 0; i < inputLayerSize; i++)
			{
				temp[i] = new double[numEdgeFeatures];
			}
			return temp;
		}

		private double[][] EmptyU()
		{
			int innerSize = inputLayerSize;
			if (flags.sparseOutputLayer || flags.tieOutputLayer)
			{
				innerSize = numHiddenUnits;
			}
			int outerSize = outputLayerSize;
			if (flags.tieOutputLayer)
			{
				outerSize = 1;
			}
			double[][] temp = new double[outerSize][];
			for (int i = 0; i < outerSize; i++)
			{
				temp[i] = new double[innerSize];
			}
			return temp;
		}

		private double[][] EmptyW()
		{
			double[][] temp = new double[inputLayerSize][];
			for (int i = 0; i < inputLayerSize; i++)
			{
				temp[i] = new double[numNodeFeatures];
			}
			return temp;
		}

		public virtual Quadruple<double[][], double[][], double[][], double[][]> SeparateWeights(double[] x)
		{
			int index = 0;
			double[][] inputLayerWeights4Edge = EmptyW4Edge();
			for (int i = 0; i < inputLayerWeights4Edge.Length; i++)
			{
				for (int j = 0; j < inputLayerWeights4Edge[i].Length; j++)
				{
					inputLayerWeights4Edge[i][j] = x[index++];
				}
			}
			double[][] inputLayerWeights = EmptyW();
			for (int i_1 = 0; i_1 < inputLayerWeights.Length; i_1++)
			{
				for (int j = 0; j < inputLayerWeights[i_1].Length; j++)
				{
					inputLayerWeights[i_1][j] = x[index++];
				}
			}
			double[][] outputLayerWeights4Edge = EmptyU4Edge();
			for (int i_2 = 0; i_2 < outputLayerWeights4Edge.Length; i_2++)
			{
				for (int j = 0; j < outputLayerWeights4Edge[i_2].Length; j++)
				{
					if (useOutputLayer)
					{
						outputLayerWeights4Edge[i_2][j] = x[index++];
					}
					else
					{
						outputLayerWeights4Edge[i_2][j] = 1;
					}
				}
			}
			double[][] outputLayerWeights = EmptyU();
			for (int i_3 = 0; i_3 < outputLayerWeights.Length; i_3++)
			{
				for (int j = 0; j < outputLayerWeights[i_3].Length; j++)
				{
					if (useOutputLayer)
					{
						outputLayerWeights[i_3][j] = x[index++];
					}
					else
					{
						outputLayerWeights[i_3][j] = 1;
					}
				}
			}
			System.Diagnostics.Debug.Assert((index == x.Length));
			return new Quadruple<double[][], double[][], double[][], double[][]>(inputLayerWeights4Edge, outputLayerWeights4Edge, inputLayerWeights, outputLayerWeights);
		}

		public virtual ICliquePotentialFunction GetCliquePotentialFunction(double[] x)
		{
			Quadruple<double[][], double[][], double[][], double[][]> allParams = SeparateWeights(x);
			double[][] W4Edge = allParams.First();
			// inputLayerWeights4Edge
			double[][] U4Edge = allParams.Second();
			// outputLayerWeights4Edge
			double[][] W = allParams.Third();
			// inputLayerWeights
			double[][] U = allParams.Fourth();
			// outputLayerWeights
			return new NonLinearSecondOrderCliquePotentialFunction(W4Edge, U4Edge, W, U, flags);
		}

		// todo [cdm]: Below data[m] --> docData
		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			double prob = 0.0;
			// the log prob of the sequence given the model, which is the negation of value at this point
			Quadruple<double[][], double[][], double[][], double[][]> allParams = SeparateWeights(x);
			double[][] W4Edge = allParams.First();
			// inputLayerWeights4Edge
			double[][] U4Edge = allParams.Second();
			// outputLayerWeights4Edge
			double[][] W = allParams.Third();
			// inputLayerWeights
			double[][] U = allParams.Fourth();
			// outputLayerWeights
			double[][] Y4Edge = null;
			double[][] Y = null;
			if (flags.softmaxOutputLayer)
			{
				Y4Edge = new double[U4Edge.Length][];
				for (int i = 0; i < U4Edge.Length; i++)
				{
					Y4Edge[i] = ArrayMath.Softmax(U4Edge[i]);
				}
				Y = new double[U.Length][];
				for (int i_1 = 0; i_1 < U.Length; i_1++)
				{
					Y[i_1] = ArrayMath.Softmax(U[i_1]);
				}
			}
			double[][] What4Edge = EmptyW4Edge();
			double[][] Uhat4Edge = EmptyU4Edge();
			double[][] What = EmptyW();
			double[][] Uhat = EmptyU();
			// the expectations over counts
			// first index is feature index, second index is of possible labeling
			double[][] eW4Edge = EmptyW4Edge();
			double[][] eU4Edge = EmptyU4Edge();
			double[][] eW = EmptyW();
			double[][] eU = EmptyU();
			// iterate over all the documents
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] docData = data[m];
				int[] docLabels = labels[m];
				NonLinearSecondOrderCliquePotentialFunction cliquePotentialFunction = new NonLinearSecondOrderCliquePotentialFunction(W4Edge, U4Edge, W, U, flags);
				// make a clique tree for this document
				CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunction, null);
				// compute the log probability of the document given the model with the parameters x
				int[] given = new int[window - 1];
				Arrays.Fill(given, classIndex.IndexOf(backgroundSymbol));
				int[] windowLabels = new int[window];
				Arrays.Fill(windowLabels, classIndex.IndexOf(backgroundSymbol));
				if (docLabels.Length > docData.Length)
				{
					// only true for self-training
					// fill the given array with the extra docLabels
					System.Array.Copy(docLabels, 0, given, 0, given.Length);
					System.Array.Copy(docLabels, 0, windowLabels, 0, windowLabels.Length);
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
					System.Array.Copy(windowLabels, 1, windowLabels, 0, window - 1);
					windowLabels[window - 1] = docLabels[i_1];
					for (int j = 0; j < docData[i_1].Length; j++)
					{
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						// for each possible labeling for that clique
						int[] cliqueFeatures = docData[i_1][j];
						double[] As = null;
						double[] fDeriv = null;
						double[][] yTimesA = null;
						double[] sumOfYTimesA = null;
						int inputSize;
						int outputSize = -1;
						if (j == 0)
						{
							inputSize = inputLayerSize;
							outputSize = outputLayerSize;
							As = cliquePotentialFunction.HiddenLayerOutput(W, cliqueFeatures, flags, null, j + 1);
						}
						else
						{
							inputSize = inputLayerSize4Edge;
							outputSize = outputLayerSize4Edge;
							As = cliquePotentialFunction.HiddenLayerOutput(W4Edge, cliqueFeatures, flags, null, j + 1);
						}
						fDeriv = new double[inputSize];
						double fD = 0;
						for (int q = 0; q < inputSize; q++)
						{
							if (useSigmoid)
							{
								fD = As[q] * (1 - As[q]);
							}
							else
							{
								fD = 1 - As[q] * As[q];
							}
							fDeriv[q] = fD;
						}
						// calculating yTimesA for softmax
						if (flags.softmaxOutputLayer)
						{
							double val = 0;
							yTimesA = new double[outputSize][];
							for (int ii = 0; ii < outputSize; ii++)
							{
								yTimesA[ii] = new double[numHiddenUnits];
							}
							sumOfYTimesA = new double[outputSize];
							for (int k = 0; k < outputSize; k++)
							{
								double[] Yk = null;
								if (flags.tieOutputLayer)
								{
									if (j == 0)
									{
										Yk = Y[0];
									}
									else
									{
										Yk = Y4Edge[0];
									}
								}
								else
								{
									if (j == 0)
									{
										Yk = Y[k];
									}
									else
									{
										Yk = Y4Edge[k];
									}
								}
								double sum = 0;
								for (int q_1 = 0; q_1 < inputSize; q_1++)
								{
									if (q_1 % outputSize == k)
									{
										int hiddenUnitNo = q_1 / outputSize;
										val = As[q_1] * Yk[hiddenUnitNo];
										yTimesA[k][hiddenUnitNo] = val;
										sum += val;
									}
								}
								sumOfYTimesA[k] = sum;
							}
						}
						// calculating Uhat What
						int[] cliqueLabel = new int[j + 1];
						System.Array.Copy(windowLabels, window - 1 - j, cliqueLabel, 0, j + 1);
						CRFLabel crfLabel = new CRFLabel(cliqueLabel);
						int givenLabelIndex = labelIndex.IndexOf(crfLabel);
						double[] Uk = null;
						double[] UhatK = null;
						double[] Yk_1 = null;
						double[] yTimesAK = null;
						double sumOfYTimesAK = 0;
						if (flags.tieOutputLayer)
						{
							if (j == 0)
							{
								Uk = U[0];
								UhatK = Uhat[0];
							}
							else
							{
								Uk = U4Edge[0];
								UhatK = Uhat4Edge[0];
							}
							if (flags.softmaxOutputLayer)
							{
								if (j == 0)
								{
									Yk_1 = Y[0];
								}
								else
								{
									Yk_1 = Y4Edge[0];
								}
							}
						}
						else
						{
							if (j == 0)
							{
								Uk = U[givenLabelIndex];
								UhatK = Uhat[givenLabelIndex];
							}
							else
							{
								Uk = U4Edge[givenLabelIndex];
								UhatK = Uhat4Edge[givenLabelIndex];
							}
							if (flags.softmaxOutputLayer)
							{
								if (j == 0)
								{
									Yk_1 = Y[givenLabelIndex];
								}
								else
								{
									Yk_1 = Y4Edge[givenLabelIndex];
								}
							}
						}
						if (flags.softmaxOutputLayer)
						{
							yTimesAK = yTimesA[givenLabelIndex];
							sumOfYTimesAK = sumOfYTimesA[givenLabelIndex];
						}
						for (int k_1 = 0; k_1 < inputSize; k_1++)
						{
							double deltaK = 1;
							if (flags.sparseOutputLayer || flags.tieOutputLayer)
							{
								if (k_1 % outputSize == givenLabelIndex)
								{
									int hiddenUnitNo = k_1 / outputSize;
									if (flags.softmaxOutputLayer)
									{
										UhatK[hiddenUnitNo] += (yTimesAK[hiddenUnitNo] - Yk_1[hiddenUnitNo] * sumOfYTimesAK);
										deltaK *= Yk_1[hiddenUnitNo];
									}
									else
									{
										UhatK[hiddenUnitNo] += As[k_1];
										deltaK *= Uk[hiddenUnitNo];
									}
								}
							}
							else
							{
								UhatK[k_1] += As[k_1];
								if (useOutputLayer)
								{
									deltaK *= Uk[k_1];
								}
							}
							if (useHiddenLayer)
							{
								deltaK *= fDeriv[k_1];
							}
							if (useOutputLayer)
							{
								if (flags.sparseOutputLayer || flags.tieOutputLayer)
								{
									if (k_1 % outputSize == givenLabelIndex)
									{
										double[] WhatK = null;
										if (j == 0)
										{
											WhatK = What[k_1];
										}
										else
										{
											WhatK = What4Edge[k_1];
										}
										foreach (int cliqueFeature in cliqueFeatures)
										{
											WhatK[cliqueFeature] += deltaK;
										}
									}
								}
								else
								{
									double[] WhatK = null;
									if (j == 0)
									{
										WhatK = What[k_1];
									}
									else
									{
										WhatK = What4Edge[k_1];
									}
									foreach (int cliqueFeature in cliqueFeatures)
									{
										WhatK[cliqueFeature] += deltaK;
									}
								}
							}
							else
							{
								if (k_1 == givenLabelIndex)
								{
									double[] WhatK = null;
									if (j == 0)
									{
										WhatK = What[k_1];
									}
									else
									{
										WhatK = What4Edge[k_1];
									}
									foreach (int cliqueFeature in cliqueFeatures)
									{
										WhatK[cliqueFeature] += deltaK;
									}
								}
							}
						}
						for (int k_2 = 0; k_2 < labelIndex.Size(); k_2++)
						{
							// labelIndex.size() == numClasses
							int[] label = labelIndex.Get(k_2).GetLabel();
							double p = cliqueTree.Prob(i_1, label);
							// probability of these labels occurring in this clique with these features
							double[] Uk2 = null;
							double[] eUK = null;
							double[] Yk2 = null;
							if (flags.tieOutputLayer)
							{
								if (j == 0)
								{
									// for node features
									Uk2 = U[0];
									eUK = eU[0];
								}
								else
								{
									Uk2 = U4Edge[0];
									eUK = eU4Edge[0];
								}
								if (flags.softmaxOutputLayer)
								{
									if (j == 0)
									{
										Yk2 = Y[0];
									}
									else
									{
										Yk2 = Y4Edge[0];
									}
								}
							}
							else
							{
								if (j == 0)
								{
									Uk2 = U[k_2];
									eUK = eU[k_2];
								}
								else
								{
									Uk2 = U4Edge[k_2];
									eUK = eU4Edge[k_2];
								}
								if (flags.softmaxOutputLayer)
								{
									if (j == 0)
									{
										Yk2 = Y[k_2];
									}
									else
									{
										Yk2 = Y4Edge[k_2];
									}
								}
							}
							if (useOutputLayer)
							{
								for (int q_1 = 0; q_1 < inputSize; q_1++)
								{
									double deltaQ = 1;
									if (flags.sparseOutputLayer || flags.tieOutputLayer)
									{
										if (q_1 % outputSize == k_2)
										{
											int hiddenUnitNo = q_1 / outputSize;
											if (flags.softmaxOutputLayer)
											{
												eUK[hiddenUnitNo] += (yTimesA[k_2][hiddenUnitNo] - Yk2[hiddenUnitNo] * sumOfYTimesA[k_2]) * p;
												deltaQ = Yk2[hiddenUnitNo];
											}
											else
											{
												eUK[hiddenUnitNo] += As[q_1] * p;
												deltaQ = Uk2[hiddenUnitNo];
											}
										}
									}
									else
									{
										eUK[q_1] += As[q_1] * p;
										deltaQ = Uk2[q_1];
									}
									if (useHiddenLayer)
									{
										deltaQ *= fDeriv[q_1];
									}
									if (flags.sparseOutputLayer || flags.tieOutputLayer)
									{
										if (q_1 % outputSize == k_2)
										{
											double[] eWq = null;
											if (j == 0)
											{
												eWq = eW[q_1];
											}
											else
											{
												eWq = eW4Edge[q_1];
											}
											foreach (int cliqueFeature in cliqueFeatures)
											{
												eWq[cliqueFeature] += deltaQ * p;
											}
										}
									}
									else
									{
										double[] eWq = null;
										if (j == 0)
										{
											eWq = eW[q_1];
										}
										else
										{
											eWq = eW4Edge[q_1];
										}
										foreach (int cliqueFeature in cliqueFeatures)
										{
											eWq[cliqueFeature] += deltaQ * p;
										}
									}
								}
							}
							else
							{
								double deltaK = 1;
								if (useHiddenLayer)
								{
									deltaK *= fDeriv[k_2];
								}
								double[] eWK = null;
								if (j == 0)
								{
									eWK = eW[k_2];
								}
								else
								{
									eWK = eW4Edge[k_2];
								}
								foreach (int cliqueFeature in cliqueFeatures)
								{
									eWK[cliqueFeature] += deltaK * p;
								}
							}
						}
					}
				}
			}
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFNonLinearSecondOrderLogConditionalObjectiveFunction.calculate()");
			}
			value = -prob;
			if (Verbose)
			{
				log.Info("value is " + value);
			}
			// compute the partial derivative for each feature by comparing expected counts to empirical counts
			int index = 0;
			for (int i_2 = 0; i_2 < eW4Edge.Length; i_2++)
			{
				for (int j = 0; j < eW4Edge[i_2].Length; j++)
				{
					derivative[index++] = (eW4Edge[i_2][j] - What4Edge[i_2][j]);
					if (Verbose)
					{
						log.Info("inputLayerWeights4Edge deriv(" + i_2 + "," + j + ") = " + eW4Edge[i_2][j] + " - " + What4Edge[i_2][j] + " = " + derivative[index - 1]);
					}
				}
			}
			for (int i_3 = 0; i_3 < eW.Length; i_3++)
			{
				for (int j = 0; j < eW[i_3].Length; j++)
				{
					derivative[index++] = (eW[i_3][j] - What[i_3][j]);
					if (Verbose)
					{
						log.Info("inputLayerWeights deriv(" + i_3 + "," + j + ") = " + eW[i_3][j] + " - " + What[i_3][j] + " = " + derivative[index - 1]);
					}
				}
			}
			if (index != beforeOutputWeights)
			{
				throw new Exception("after W derivative, index(" + index + ") != beforeOutputWeights(" + beforeOutputWeights + ")");
			}
			if (useOutputLayer)
			{
				for (int i = 0; i_3 < eU4Edge.Length; i_3++)
				{
					for (int j = 0; j < eU4Edge[i_3].Length; j++)
					{
						derivative[index++] = (eU4Edge[i_3][j] - Uhat4Edge[i_3][j]);
						if (Verbose)
						{
							log.Info("outputLayerWeights4Edge deriv(" + i_3 + "," + j + ") = " + eU4Edge[i_3][j] + " - " + Uhat4Edge[i_3][j] + " = " + derivative[index - 1]);
						}
					}
				}
				for (int i_1 = 0; i_1 < eU.Length; i_1++)
				{
					for (int j = 0; j < eU[i_1].Length; j++)
					{
						derivative[index++] = (eU[i_1][j] - Uhat[i_1][j]);
						if (Verbose)
						{
							log.Info("outputLayerWeights deriv(" + i_1 + "," + j + ") = " + eU[i_1][j] + " - " + Uhat[i_1][j] + " = " + derivative[index - 1]);
						}
					}
				}
			}
			if (index != x.Length)
			{
				throw new Exception("after W derivative, index(" + index + ") != x.length(" + x.Length + ")");
			}
			int regSize = x.Length;
			if (flags.skipOutputRegularization || flags.softmaxOutputLayer)
			{
				regSize = beforeOutputWeights;
			}
			// incorporate priors
			if (prior == QuadraticPrior)
			{
				double sigmaSq = sigma * sigma;
				for (int i = 0; i_3 < regSize; i_3++)
				{
					double k = 1.0;
					double w = x[i_3];
					value += k * w * w / 2.0 / sigmaSq;
					derivative[i_3] += k * w / sigmaSq;
				}
			}
			else
			{
				if (prior == HuberPrior)
				{
					double sigmaSq = sigma * sigma;
					for (int i = 0; i_3 < regSize; i_3++)
					{
						double w = x[i_3];
						double wabs = System.Math.Abs(w);
						if (wabs < epsilon)
						{
							value += w * w / 2.0 / epsilon / sigmaSq;
							derivative[i_3] += w / epsilon / sigmaSq;
						}
						else
						{
							value += (wabs - epsilon / 2) / sigmaSq;
							derivative[i_3] += ((w < 0.0) ? -1.0 : 1.0) / sigmaSq;
						}
					}
				}
				else
				{
					if (prior == QuarticPrior)
					{
						double sigmaQu = sigma * sigma * sigma * sigma;
						for (int i = 0; i_3 < regSize; i_3++)
						{
							double k = 1.0;
							double w = x[i_3];
							value += k * w * w * w * w / 2.0 / sigmaQu;
							derivative[i_3] += k * w / sigmaQu;
						}
					}
				}
			}
		}

		public virtual double[][] EmptyFull2D()
		{
			double[][] d = new double[map.Length][];
			// int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				d[i] = new double[labelIndices[map[i]].Size()];
			}
			// cdm july 2005: below array initialization isn't necessary: JLS (3rd ed.) 4.12.5
			// Arrays.fill(d[i], 0.0);
			// index += labelIndices.get(map[i]).size();
			return d;
		}
	}
}
