using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class CRFNonLinearLogConditionalObjectiveFunction : AbstractCachingDiffFunction, IHasCliquePotentialFunction, IHasFeatureGrouping, IHasRegularizerParamRange
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFNonLinearLogConditionalObjectiveFunction));

		public const int NoPrior = 0;

		public const int QuadraticPrior = 1;

		public const int HuberPrior = 2;

		public const int QuarticPrior = 3;

		public const int L1Prior = 4;

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

		internal double[][][][] featureVal;

		internal int[][] docWindowLabels;

		internal int[][] labels;

		internal int domainDimension = -1;

		internal int inputLayerSize = -1;

		internal int outputLayerSize = -1;

		internal int edgeParamCount = -1;

		internal int numNodeFeatures = -1;

		internal int numEdgeFeatures = -1;

		internal int beforeOutputWeights = -1;

		internal int originalFeatureCount = -1;

		internal int[][] weightIndices;

		internal string backgroundSymbol;

		private int[][] featureGrouping = null;

		public static bool Verbose = false;

		public static bool Debug = false;

		public bool gradientsOnly = false;

		/* Use a Huber robust regression penalty (L1 except very near 0) not L2 */
		// didn't have <String> before. Added since that's what is assumed everywhere.
		// empirical counts of all the linear features [feature][class]
		// empirical counts of all the output layer features [num of class][input layer size]
		// empirical counts of all the input layer features [input layer size][featureIndex.size()]
		// hidden layer number of neuron = numHiddenUnits * numClasses
		// data[docIndex][tokenIndex][][]
		// featureVal[docIndex][tokenIndex][][]
		// labels[docIndex][tokenIndex]
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
				if (Sharpen.Runtime.EqualsIgnoreCase("L1", priorTypeStr))
				{
					return L1Prior;
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
							if (Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "lasso") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "ridge") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "ae-lasso") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "g-lasso")
								 || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "sg-lasso") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "NONE"))
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
		}

		internal CRFNonLinearLogConditionalObjectiveFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, SeqClassifierFlags flags, int numNodeFeatures, int numEdgeFeatures
			, double[][][][] featureVal)
		{
			this.window = window;
			this.classIndex = classIndex;
			this.numClasses = classIndex.Size();
			this.labelIndices = labelIndices;
			this.data = data;
			this.featureVal = featureVal;
			this.flags = flags;
			this.map = map;
			this.labels = labels;
			this.prior = GetPriorType(flags.priorType);
			this.backgroundSymbol = flags.backgroundSymbol;
			this.sigma = flags.sigma;
			this.outputLayerSize = numClasses;
			this.numHiddenUnits = flags.numHiddenUnits;
			if (flags.arbitraryInputLayerSize != -1)
			{
				this.inputLayerSize = flags.arbitraryInputLayerSize;
			}
			else
			{
				this.inputLayerSize = numHiddenUnits * numClasses;
			}
			this.numNodeFeatures = numNodeFeatures;
			this.numEdgeFeatures = numEdgeFeatures;
			log.Info("numOfEdgeFeatures: " + numEdgeFeatures);
			this.useOutputLayer = flags.useOutputLayer;
			this.useHiddenLayer = flags.useHiddenLayer;
			this.useSigmoid = flags.useSigmoid;
			this.docWindowLabels = new int[data.Length][];
			if (!useOutputLayer)
			{
				log.Info("Output layer not activated, inputLayerSize must be equal to numClasses, setting it to " + numClasses);
				this.inputLayerSize = numClasses;
			}
			else
			{
				if (flags.softmaxOutputLayer && !(flags.sparseOutputLayer || flags.tieOutputLayer))
				{
					throw new Exception("flags.softmaxOutputLayer == true, but neither flags.sparseOutputLayer or flags.tieOutputLayer is true");
				}
			}
			EmpiricalCounts();
		}

		public override int DomainDimension()
		{
			if (domainDimension < 0)
			{
				domainDimension = 0;
				edgeParamCount = numEdgeFeatures * labelIndices[1].Size();
				originalFeatureCount = 0;
				foreach (int aMap in map)
				{
					int s = labelIndices[aMap].Size();
					originalFeatureCount += s;
				}
				domainDimension += edgeParamCount;
				domainDimension += inputLayerSize * numNodeFeatures;
				beforeOutputWeights = domainDimension;
				// TODO(mengqiu) temporary fix for debugging
				if (useOutputLayer)
				{
					if (flags.sparseOutputLayer)
					{
						domainDimension += outputLayerSize * numHiddenUnits;
					}
					else
					{
						if (flags.tieOutputLayer)
						{
							domainDimension += 1 * numHiddenUnits;
						}
						else
						{
							domainDimension += outputLayerSize * inputLayerSize;
						}
					}
				}
				log.Info("edgeParamCount: " + edgeParamCount);
				log.Info("originalFeatureCount: " + originalFeatureCount);
				log.Info("beforeOutputWeights: " + beforeOutputWeights);
				log.Info("domainDimension: " + domainDimension);
			}
			return domainDimension;
		}

		public override double[] Initial()
		{
			//TODO(mengqiu) initialize edge feature weights to be weights from CRF
			double[] initial = new double[DomainDimension()];
			// randomly initialize weights
			if (useHiddenLayer || useOutputLayer)
			{
				double epsilon = 0.1;
				double twoEpsilon = epsilon * 2;
				int count = 0;
				double val = 0;
				// init edge param weights
				for (int i = 0; i < edgeParamCount; i++)
				{
					val = random.NextDouble() * twoEpsilon - epsilon;
					initial[count++] = val;
				}
				if (flags.blockInitialize)
				{
					double fanIn = 1 / Math.Sqrt(numNodeFeatures + 0.0);
					double twoFanIn = 2.0 * fanIn;
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
									val = random.NextDouble() * twoFanIn - fanIn;
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
					double fanIn = 1 / Math.Sqrt(numNodeFeatures + 0.0);
					double twoFanIn = 2.0 * fanIn;
					for (int i_1 = edgeParamCount; i_1 < beforeOutputWeights; i_1++)
					{
						val = random.NextDouble() * twoFanIn - fanIn;
						initial[count++] = val;
					}
				}
				// init output layer weights
				if (flags.sparseOutputLayer)
				{
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
							if (flags.hardcodeSoftmaxOutputWeights)
							{
								val = 1.0 / numHiddenUnits;
							}
							else
							{
								val = random.NextDouble() * total;
								total -= val;
							}
							initial[count++] = val;
						}
						if (flags.hardcodeSoftmaxOutputWeights)
						{
							initial[count++] = 1.0 / numHiddenUnits;
						}
						else
						{
							initial[count++] = total;
						}
					}
					else
					{
						for (int i_1 = beforeOutputWeights; i_1 < DomainDimension(); i_1++)
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

		private void EmpiricalCounts()
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
					// for (int j = 1; j < docData[i].length; j++) { // j starting from 1, skip all node features
					//TODO(mengqiu) generalize this for bigger cliques
					int j = 1;
					int[] cliqueLabel = new int[j + 1];
					System.Array.Copy(windowLabels, window - 1 - j, cliqueLabel, 0, j + 1);
					CRFLabel crfLabel = new CRFLabel(cliqueLabel);
					int labelIndex = labelIndices[j].IndexOf(crfLabel);
					int[] cliqueFeatures = docData[i][j];
					//log.info(crfLabel + " " + labelIndex);
					foreach (int cliqueFeature in cliqueFeatures)
					{
						Ehat[cliqueFeature][labelIndex]++;
					}
				}
			}
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
			// TODO(mengqiu) temporary fix for debugging
			double[][] temp = new double[inputLayerSize][];
			for (int i = 0; i < inputLayerSize; i++)
			{
				temp[i] = new double[numNodeFeatures];
			}
			return temp;
		}

		public virtual Triple<double[][], double[][], double[][]> SeparateWeights(double[] x)
		{
			double[] linearWeights = new double[edgeParamCount];
			System.Array.Copy(x, 0, linearWeights, 0, edgeParamCount);
			double[][] linearWeights2D = To2D(linearWeights);
			int index = edgeParamCount;
			double[][] inputLayerWeights = EmptyW();
			for (int i = 0; i < inputLayerWeights.Length; i++)
			{
				for (int j = 0; j < inputLayerWeights[i].Length; j++)
				{
					inputLayerWeights[i][j] = x[index++];
				}
			}
			double[][] outputLayerWeights = EmptyU();
			for (int i_1 = 0; i_1 < outputLayerWeights.Length; i_1++)
			{
				for (int j = 0; j < outputLayerWeights[i_1].Length; j++)
				{
					if (useOutputLayer)
					{
						if (flags.hardcodeSoftmaxOutputWeights)
						{
							outputLayerWeights[i_1][j] = 1.0 / numHiddenUnits;
						}
						else
						{
							outputLayerWeights[i_1][j] = x[index++];
						}
					}
					else
					{
						outputLayerWeights[i_1][j] = 1;
					}
				}
			}
			System.Diagnostics.Debug.Assert((index == x.Length));
			return new Triple<double[][], double[][], double[][]>(linearWeights2D, inputLayerWeights, outputLayerWeights);
		}

		public virtual ICliquePotentialFunction GetCliquePotentialFunction(double[] x)
		{
			Triple<double[][], double[][], double[][]> allParams = SeparateWeights(x);
			double[][] linearWeights = allParams.First();
			double[][] W = allParams.Second();
			// inputLayerWeights
			double[][] U = allParams.Third();
			// outputLayerWeights
			return new NonLinearCliquePotentialFunction(linearWeights, W, U, flags);
		}

		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			double prob = 0.0;
			// the log prob of the sequence given the model, which is the negation of value at this point
			Triple<double[][], double[][], double[][]> allParams = SeparateWeights(x);
			double[][] linearWeights = allParams.First();
			double[][] W = allParams.Second();
			// inputLayerWeights
			double[][] U = allParams.Third();
			// outputLayerWeights
			double[][] Y = null;
			if (flags.softmaxOutputLayer)
			{
				Y = new double[U.Length][];
				for (int i = 0; i < U.Length; i++)
				{
					Y[i] = ArrayMath.Softmax(U[i]);
				}
			}
			double[][] What = EmptyW();
			double[][] Uhat = EmptyU();
			// the expectations over counts
			// first index is feature index, second index is of possible labeling
			double[][] E = Empty2D();
			double[][] eW = EmptyW();
			double[][] eU = EmptyU();
			// iterate over all the documents
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] docData = data[m];
				int[] docLabels = labels[m];
				double[][][] featureVal3DArr = null;
				if (featureVal != null)
				{
					featureVal3DArr = featureVal[m];
				}
				if (Debug)
				{
					log.Info("processing doc " + m);
				}
				NonLinearCliquePotentialFunction cliquePotentialFunction = new NonLinearCliquePotentialFunction(linearWeights, W, U, flags);
				// make a clique tree for this document
				CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunction, featureVal3DArr);
				// compute the log probability of the document given the model with the parameters x
				int[] given = new int[window - 1];
				if (!gradientsOnly)
				{
					Arrays.Fill(given, classIndex.IndexOf(backgroundSymbol));
				}
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
				if (!gradientsOnly)
				{
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
						if (Debug)
						{
							log.Info("calculating Ehat[" + i_1 + "]");
						}
						// calculating empirical counts of node features
						if (j == 0)
						{
							double[] featureValArr = null;
							if (featureVal3DArr != null)
							{
								featureValArr = featureVal3DArr[i_1][j];
							}
							As = cliquePotentialFunction.HiddenLayerOutput(W, cliqueFeatures, flags, featureValArr);
							fDeriv = new double[inputLayerSize];
							double fD = 0;
							for (int q = 0; q < inputLayerSize; q++)
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
								yTimesA = new double[outputLayerSize][];
								for (int ii = 0; ii < outputLayerSize; ii++)
								{
									yTimesA[ii] = new double[numHiddenUnits];
								}
								sumOfYTimesA = new double[outputLayerSize];
								for (int k = 0; k < outputLayerSize; k++)
								{
									double[] Yk = null;
									if (flags.tieOutputLayer)
									{
										Yk = Y[0];
									}
									else
									{
										Yk = Y[k];
									}
									double sum = 0;
									for (int q_1 = 0; q_1 < inputLayerSize; q_1++)
									{
										if (q_1 % outputLayerSize == k)
										{
											int hiddenUnitNo = q_1 / outputLayerSize;
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
								Uk = U[0];
								UhatK = Uhat[0];
								if (flags.softmaxOutputLayer)
								{
									Yk_1 = Y[0];
								}
							}
							else
							{
								Uk = U[givenLabelIndex];
								UhatK = Uhat[givenLabelIndex];
								if (flags.softmaxOutputLayer)
								{
									Yk_1 = Y[givenLabelIndex];
								}
							}
							if (flags.softmaxOutputLayer)
							{
								yTimesAK = yTimesA[givenLabelIndex];
								sumOfYTimesAK = sumOfYTimesA[givenLabelIndex];
							}
							for (int k_1 = 0; k_1 < inputLayerSize; k_1++)
							{
								double deltaK = 1;
								if (flags.sparseOutputLayer || flags.tieOutputLayer)
								{
									if (k_1 % outputLayerSize == givenLabelIndex)
									{
										int hiddenUnitNo = k_1 / outputLayerSize;
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
										if (k_1 % outputLayerSize == givenLabelIndex)
										{
											double[] WhatK = What[k_1];
											for (int n = 0; n < cliqueFeatures.Length; n++)
											{
												double fVal = 1.0;
												if (featureVal3DArr != null)
												{
													fVal = featureVal3DArr[i_1][j][n];
												}
												WhatK[cliqueFeatures[n]] += deltaK * fVal;
											}
										}
									}
									else
									{
										double[] WhatK = What[k_1];
										double fVal = 1.0;
										for (int n = 0; n < cliqueFeatures.Length; n++)
										{
											fVal = 1.0;
											if (featureVal3DArr != null)
											{
												fVal = featureVal3DArr[i_1][j][n];
											}
											WhatK[cliqueFeatures[n]] += deltaK * fVal;
										}
									}
								}
								else
								{
									if (k_1 == givenLabelIndex)
									{
										double[] WhatK = What[k_1];
										double fVal = 1.0;
										for (int n = 0; n < cliqueFeatures.Length; n++)
										{
											fVal = 1.0;
											if (featureVal3DArr != null)
											{
												fVal = featureVal3DArr[i_1][j][n];
											}
											WhatK[cliqueFeatures[n]] += deltaK * fVal;
										}
									}
								}
							}
						}
						if (Debug)
						{
							log.Info(" done!");
						}
						if (Debug)
						{
							log.Info("calculating E[" + i_1 + "]");
						}
						// calculate expected count of features
						for (int k_2 = 0; k_2 < labelIndex.Size(); k_2++)
						{
							// labelIndex.size() == numClasses
							int[] label = labelIndex.Get(k_2).GetLabel();
							double p = cliqueTree.Prob(i_1, label);
							// probability of these labels occurring in this clique with these features
							if (j == 0)
							{
								// for node features
								double[] Uk = null;
								double[] eUK = null;
								double[] Yk = null;
								if (flags.tieOutputLayer)
								{
									Uk = U[0];
									eUK = eU[0];
									if (flags.softmaxOutputLayer)
									{
										Yk = Y[0];
									}
								}
								else
								{
									Uk = U[k_2];
									eUK = eU[k_2];
									if (flags.softmaxOutputLayer)
									{
										Yk = Y[k_2];
									}
								}
								if (useOutputLayer)
								{
									for (int q = 0; q < inputLayerSize; q++)
									{
										double deltaQ = 1;
										if (flags.sparseOutputLayer || flags.tieOutputLayer)
										{
											if (q % outputLayerSize == k_2)
											{
												int hiddenUnitNo = q / outputLayerSize;
												if (flags.softmaxOutputLayer)
												{
													eUK[hiddenUnitNo] += (yTimesA[k_2][hiddenUnitNo] - Yk[hiddenUnitNo] * sumOfYTimesA[k_2]) * p;
													deltaQ = Yk[hiddenUnitNo];
												}
												else
												{
													eUK[hiddenUnitNo] += As[q] * p;
													deltaQ = Uk[hiddenUnitNo];
												}
											}
										}
										else
										{
											eUK[q] += As[q] * p;
											deltaQ = Uk[q];
										}
										if (useHiddenLayer)
										{
											deltaQ *= fDeriv[q];
										}
										if (flags.sparseOutputLayer || flags.tieOutputLayer)
										{
											if (q % outputLayerSize == k_2)
											{
												double[] eWq = eW[q];
												double fVal = 1.0;
												for (int n = 0; n < cliqueFeatures.Length; n++)
												{
													fVal = 1.0;
													if (featureVal3DArr != null)
													{
														fVal = featureVal3DArr[i_1][j][n];
													}
													eWq[cliqueFeatures[n]] += deltaQ * p * fVal;
												}
											}
										}
										else
										{
											double[] eWq = eW[q];
											double fVal = 1.0;
											for (int n = 0; n < cliqueFeatures.Length; n++)
											{
												fVal = 1.0;
												if (featureVal3DArr != null)
												{
													fVal = featureVal3DArr[i_1][j][n];
												}
												eWq[cliqueFeatures[n]] += deltaQ * p * fVal;
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
									double[] eWK = eW[k_2];
									double fVal = 1.0;
									for (int n = 0; n < cliqueFeatures.Length; n++)
									{
										fVal = 1.0;
										if (featureVal3DArr != null)
										{
											fVal = featureVal3DArr[i_1][j][n];
										}
										eWK[cliqueFeatures[n]] += deltaK * p * fVal;
									}
								}
							}
							else
							{
								// for edge features
								foreach (int cliqueFeature in cliqueFeatures)
								{
									E[cliqueFeature][k_2] += p;
								}
							}
						}
						if (Debug)
						{
							log.Info(" done!");
						}
					}
				}
			}
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFNonLinearLogConditionalObjectiveFunction.calculate()");
			}
			value = -prob;
			if (Verbose)
			{
				log.Info("value is " + value);
			}
			if (Debug)
			{
				log.Info("calculating derivative ");
			}
			// compute the partial derivative for each feature by comparing expected counts to empirical counts
			int index = 0;
			for (int i_2 = 0; i_2 < E.Length; i_2++)
			{
				for (int j = 0; j < E[i_2].Length; j++)
				{
					derivative[index++] = (E[i_2][j] - Ehat[i_2][j]);
					if (Verbose)
					{
						log.Info("linearWeights deriv(" + i_2 + "," + j + ") = " + E[i_2][j] + " - " + Ehat[i_2][j] + " = " + derivative[index - 1]);
					}
				}
			}
			if (index != edgeParamCount)
			{
				throw new Exception("after edge derivative, index(" + index + ") != edgeParamCount(" + edgeParamCount + ")");
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
				for (int i = 0; i_3 < eU.Length; i_3++)
				{
					for (int j = 0; j < eU[i_3].Length; j++)
					{
						if (flags.hardcodeSoftmaxOutputWeights)
						{
							derivative[index++] = 0;
						}
						else
						{
							derivative[index++] = (eU[i_3][j] - Uhat[i_3][j]);
						}
						if (Verbose)
						{
							log.Info("outputLayerWeights deriv(" + i_3 + "," + j + ") = " + eU[i_3][j] + " - " + Uhat[i_3][j] + " = " + derivative[index - 1]);
						}
					}
				}
			}
			if (index != x.Length)
			{
				throw new Exception("after W derivative, index(" + index + ") != x.length(" + x.Length + ")");
			}
			int regSize = x.Length;
			if (flags.skipOutputRegularization || flags.softmaxOutputLayer || flags.hardcodeSoftmaxOutputWeights)
			{
				regSize = beforeOutputWeights;
			}
			if (Debug)
			{
				log.Info("done!");
			}
			if (Debug)
			{
				log.Info("incorporating priors ...");
			}
			// incorporate priors
			if (prior == QuadraticPrior)
			{
				double sigmaSq = sigma * sigma;
				double twoSigmaSq = 2.0 * sigmaSq;
				double w = 0;
				double valueSum = 0;
				for (int i = 0; i_3 < regSize; i_3++)
				{
					w = x[i_3];
					valueSum += w * w;
					derivative[i_3] += w / sigmaSq;
				}
				value += valueSum / twoSigmaSq;
			}
			else
			{
				if (prior == L1Prior)
				{
				}
				else
				{
					// Do nothing, as the prior will be applied in OWL-QN
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
			if (flags.regularizeSoftmaxTieParam && flags.softmaxOutputLayer && !flags.hardcodeSoftmaxOutputWeights)
			{
				// lambda is 1/(2*sigma*sigma)
				double softmaxLambda = flags.softmaxTieLambda;
				double oneDividedByTwoSigmaSq = softmaxLambda * 2;
				double y = 0;
				double mean = 1.0 / numHiddenUnits;
				int count = 0;
				foreach (double[] aU in U)
				{
					for (int j = 0; j < aU.Length; j++)
					{
						y = aU[j];
						value += (y - mean) * (y - mean) * softmaxLambda;
						double grad = (y - mean) * oneDividedByTwoSigmaSq;
						// log.info("U["+i+"]["+j+"]="+x[beforeOutputWeights+count]+", Y["+i+"]["+j+"]="+Y[i][j]+", grad="+grad);
						derivative[beforeOutputWeights + count] += grad;
						count++;
					}
				}
			}
			if (Debug)
			{
				log.Info("done!");
			}
		}

		public virtual ICollection<int> GetRegularizerParamRange(double[] x)
		{
			ICollection<int> paramRange = Generics.NewHashSet(x.Length);
			for (int i = 0; i < beforeOutputWeights; i++)
			{
				paramRange.Add(i);
			}
			return paramRange;
		}

		public virtual double[][] To2D(double[] linearWeights)
		{
			double[][] newWeights = new double[numEdgeFeatures][];
			int index = 0;
			int labelIndicesSize = labelIndices[1].Size();
			for (int i = 0; i < numEdgeFeatures; i++)
			{
				newWeights[i] = new double[labelIndicesSize];
				System.Array.Copy(linearWeights, index, newWeights[i], 0, labelIndicesSize);
				index += labelIndicesSize;
			}
			return newWeights;
		}

		public virtual double[][] Empty2D()
		{
			double[][] d = new double[numEdgeFeatures][];
			// int index = 0;
			int labelIndicesSize = labelIndices[1].Size();
			for (int i = 0; i < numEdgeFeatures; i++)
			{
				d[i] = new double[labelIndicesSize];
			}
			// cdm july 2005: below array initialization isn't necessary: JLS (3rd ed.) 4.12.5
			// Arrays.fill(d[i], 0.0);
			// index += labelIndices.get(map[i]).size();
			return d;
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

		public virtual int[][] GetFeatureGrouping()
		{
			if (featureGrouping != null)
			{
				return featureGrouping;
			}
			else
			{
				IList<ICollection<int>> groups = new List<ICollection<int>>();
				if (flags.groupByInput)
				{
					for (int nodeFeatureIndex = 0; nodeFeatureIndex < numNodeFeatures; nodeFeatureIndex++)
					{
						// for each node feature, we enforce the sparsity
						ICollection<int> newSet = new HashSet<int>();
						for (int outputClassIndex = 0; outputClassIndex < numClasses; outputClassIndex++)
						{
							for (int hiddenUnitIndex = 0; hiddenUnitIndex < numHiddenUnits; hiddenUnitIndex++)
							{
								int firstLayerIndex = hiddenUnitIndex * numClasses + outputClassIndex;
								int oneDIndex = firstLayerIndex * numNodeFeatures + nodeFeatureIndex + edgeParamCount;
								newSet.Add(oneDIndex);
							}
						}
						groups.Add(newSet);
					}
				}
				else
				{
					if (flags.groupByHiddenUnit)
					{
						for (int nodeFeatureIndex = 0; nodeFeatureIndex < numNodeFeatures; nodeFeatureIndex++)
						{
							// for each node feature, we enforce the sparsity
							for (int hiddenUnitIndex = 0; hiddenUnitIndex < numHiddenUnits; hiddenUnitIndex++)
							{
								ICollection<int> newSet = new HashSet<int>();
								for (int outputClassIndex = 0; outputClassIndex < numClasses; outputClassIndex++)
								{
									int firstLayerIndex = hiddenUnitIndex * numClasses + outputClassIndex;
									int oneDIndex = firstLayerIndex * numNodeFeatures + nodeFeatureIndex + edgeParamCount;
									newSet.Add(oneDIndex);
								}
								groups.Add(newSet);
							}
						}
					}
					else
					{
						for (int nodeFeatureIndex = 0; nodeFeatureIndex < numNodeFeatures; nodeFeatureIndex++)
						{
							// for each node feature, we enforce the sparsity
							for (int outputClassIndex = 0; outputClassIndex < numClasses; outputClassIndex++)
							{
								ICollection<int> newSet = new HashSet<int>();
								for (int hiddenUnitIndex = 0; hiddenUnitIndex < numHiddenUnits; hiddenUnitIndex++)
								{
									int firstLayerIndex = hiddenUnitIndex * numClasses + outputClassIndex;
									int oneDIndex = firstLayerIndex * numNodeFeatures + nodeFeatureIndex + edgeParamCount;
									newSet.Add(oneDIndex);
								}
								groups.Add(newSet);
							}
						}
					}
				}
				int[][] fg = new int[groups.Count][];
				for (int i = 0; i < fg.Length; i++)
				{
					ICollection<int> aSet = groups[i];
					fg[i] = new int[aSet.Count];
					int ind = 0;
					foreach (int j in aSet)
					{
						fg[i][ind++] = j;
					}
				}
				featureGrouping = fg;
				return fg;
			}
		}
	}
}
