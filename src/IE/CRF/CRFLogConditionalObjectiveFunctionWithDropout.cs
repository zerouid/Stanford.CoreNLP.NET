using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class CRFLogConditionalObjectiveFunctionWithDropout : CRFLogConditionalObjectiveFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFLogConditionalObjectiveFunctionWithDropout));

		private readonly double delta;

		private readonly double dropoutScale;

		private double[][] dropoutPriorGradTotal;

		private readonly bool dropoutApprox;

		private double[][] weightSquare;

		private readonly int[][][][] totalData;

		private int unsupDropoutStartIndex;

		private readonly double unsupDropoutScale;

		private IList<IList<ICollection<int>>> dataFeatureHash;

		private IList<IDictionary<int, IList<int>>> condensedMap;

		private int[][] dataFeatureHashByDoc;

		private int edgeLabelIndexSize;

		private int nodeLabelIndexSize;

		private int[][] edgeLabels;

		private IDictionary<int, IList<int>> currPrevLabelsMap;

		private IDictionary<int, IList<int>> currNextLabelsMap;

		private sealed class _IThreadsafeProcessor_42 : IThreadsafeProcessor<Pair<int, bool>, Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>>
		{
			public _IThreadsafeProcessor_42(CRFLogConditionalObjectiveFunctionWithDropout _enclosing)
			{
				this._enclosing = _enclosing;
			}

			// data[docIndex][tokenIndex][][]
			public Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>> Process(Pair<int, bool> docIndexUnsup)
			{
				return this._enclosing.ExpectedCountsAndValueForADoc(docIndexUnsup.First(), false, docIndexUnsup.Second());
			}

			public IThreadsafeProcessor<Pair<int, bool>, Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>> NewInstance()
			{
				return this;
			}

			private readonly CRFLogConditionalObjectiveFunctionWithDropout _enclosing;
		}

		private IThreadsafeProcessor<Pair<int, bool>, Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>> dropoutPriorThreadProcessor;

		internal CRFLogConditionalObjectiveFunctionWithDropout(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string priorType, string backgroundSymbol, double sigma, double[]
			[][][] featureVal, double delta, double dropoutScale, int multiThreadGrad, bool dropoutApprox, double unsupDropoutScale, int[][][][] unsupDropoutData)
			: base(data, labels, window, classIndex, labelIndices, map, priorType, backgroundSymbol, sigma, featureVal, multiThreadGrad)
		{
			dropoutPriorThreadProcessor = new _IThreadsafeProcessor_42(this);
			//TODO(Mengqiu) Need to figure out what to do with dataDimension() in case of
			// mixed supervised+unsupervised data for SGD (AdaGrad)
			this.delta = delta;
			this.dropoutScale = dropoutScale;
			this.dropoutApprox = dropoutApprox;
			dropoutPriorGradTotal = Empty2D();
			this.unsupDropoutStartIndex = data.Length;
			this.unsupDropoutScale = unsupDropoutScale;
			if (unsupDropoutData != null)
			{
				this.totalData = new int[data.Length + unsupDropoutData.Length][][][];
				for (int i = 0; i < data.Length; i++)
				{
					this.totalData[i] = data[i];
				}
				for (int i_1 = 0; i_1 < unsupDropoutData.Length; i_1++)
				{
					this.totalData[i_1 + unsupDropoutStartIndex] = unsupDropoutData[i_1];
				}
			}
			else
			{
				this.totalData = data;
			}
			InitEdgeLabels();
			InitializeDataFeatureHash();
		}

		private void InitEdgeLabels()
		{
			if (labelIndices.Count < 2)
			{
				return;
			}
			IIndex<CRFLabel> edgeLabelIndex = labelIndices[1];
			edgeLabelIndexSize = edgeLabelIndex.Size();
			IIndex<CRFLabel> nodeLabelIndex = labelIndices[0];
			nodeLabelIndexSize = nodeLabelIndex.Size();
			currPrevLabelsMap = new Dictionary<int, IList<int>>();
			currNextLabelsMap = new Dictionary<int, IList<int>>();
			edgeLabels = new int[edgeLabelIndexSize][];
			for (int k = 0; k < edgeLabelIndexSize; k++)
			{
				int[] labelPair = edgeLabelIndex.Get(k).GetLabel();
				edgeLabels[k] = labelPair;
				int curr = labelPair[1];
				int prev = labelPair[0];
				if (!currPrevLabelsMap.Contains(curr))
				{
					currPrevLabelsMap[curr] = new List<int>(numClasses);
				}
				currPrevLabelsMap[curr].Add(prev);
				if (!currNextLabelsMap.Contains(prev))
				{
					currNextLabelsMap[prev] = new List<int>(numClasses);
				}
				currNextLabelsMap[prev].Add(curr);
			}
		}

		private IDictionary<int, double[]> SparseE(ICollection<int> activeFeatures)
		{
			IDictionary<int, double[]> aMap = new Dictionary<int, double[]>(activeFeatures.Count);
			foreach (int f in activeFeatures)
			{
				// System.err.printf("aMap.put(%d, new double[%d])\n", f, map[f]+1);
				aMap[f] = new double[map[f] == 0 ? nodeLabelIndexSize : edgeLabelIndexSize];
			}
			return aMap;
		}

		private IDictionary<int, double[]> SparseE(int[] activeFeatures)
		{
			IDictionary<int, double[]> aMap = new Dictionary<int, double[]>(activeFeatures.Length);
			foreach (int f in activeFeatures)
			{
				// System.err.printf("aMap.put(%d, new double[%d])\n", f, map[f]+1);
				aMap[f] = new double[map[f] == 0 ? nodeLabelIndexSize : edgeLabelIndexSize];
			}
			return aMap;
		}

		private Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>> ExpectedCountsAndValueForADoc(int docIndex, bool skipExpectedCountCalc, bool skipValCalc)
		{
			int[] activeFeatures = dataFeatureHashByDoc[docIndex];
			IList<ICollection<int>> docDataHash = dataFeatureHash[docIndex];
			IDictionary<int, IList<int>> condensedFeaturesMap = condensedMap[docIndex];
			double prob = 0;
			int[][][] docData = totalData[docIndex];
			int[] docLabels = null;
			if (docIndex < labels.Length)
			{
				docLabels = labels[docIndex];
			}
			Timing timer = new Timing();
			double[][][] featureVal3DArr = null;
			if (featureVal != null)
			{
				featureVal3DArr = featureVal[docIndex];
			}
			// make a clique tree for this document
			CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunc, featureVal3DArr);
			if (!skipValCalc)
			{
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
				double startPosLogProb = cliqueTree.LogProbStartPos();
				if (Verbose)
				{
					System.Console.Error.Printf("P_-1(Background) = % 5.3f\n", startPosLogProb);
				}
				prob += startPosLogProb;
				// iterate over the positions in this document
				for (int i = 0; i < docData.Length; i++)
				{
					int label = docLabels[i];
					double p = cliqueTree.CondLogProbGivenPrevious(i, label, given);
					if (Verbose)
					{
						log.Info("P(" + label + "|" + ArrayMath.ToString(given) + ")=" + System.Math.Exp(p));
					}
					prob += p;
					System.Array.Copy(given, 1, given, 0, given.Length - 1);
					given[given.Length - 1] = label;
				}
			}
			IDictionary<int, double[]> EForADoc = SparseE(activeFeatures);
			IList<IDictionary<int, double[]>> EForADocPos = null;
			if (dropoutApprox)
			{
				EForADocPos = new List<IDictionary<int, double[]>>(docData.Length);
			}
			if (!skipExpectedCountCalc)
			{
				// compute the expected counts for this document, which we will need to compute the derivative
				// iterate over the positions in this document
				double fVal = 1.0;
				for (int i = 0; i < docData.Length; i++)
				{
					ICollection<int> docDataHashI = docDataHash[i];
					IDictionary<int, double[]> EForADocPosAtI = null;
					if (dropoutApprox)
					{
						EForADocPosAtI = SparseE(docDataHashI);
					}
					foreach (int fIndex in docDataHashI)
					{
						int j = map[fIndex];
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						// for each possible labeling for that clique
						for (int k = 0; k < labelIndex.Size(); k++)
						{
							int[] label = labelIndex.Get(k).GetLabel();
							double p = cliqueTree.Prob(i, label);
							// probability of these labels occurring in this clique with these features
							if (dropoutApprox)
							{
								IncreScore(EForADocPosAtI, fIndex, k, fVal * p);
							}
							IncreScore(EForADoc, fIndex, k, fVal * p);
						}
					}
					if (dropoutApprox)
					{
						foreach (int fIndex_1 in docDataHashI)
						{
							if (condensedFeaturesMap.Contains(fIndex_1))
							{
								IList<int> aList = condensedFeaturesMap[fIndex_1];
								foreach (int toCopyInto in aList)
								{
									double[] arr = EForADocPosAtI[fIndex_1];
									double[] targetArr = new double[arr.Length];
									for (int q = 0; q < arr.Length; q++)
									{
										targetArr[q] = arr[q];
									}
									EForADocPosAtI[toCopyInto] = targetArr;
								}
							}
						}
						EForADocPos.Add(EForADocPosAtI);
					}
				}
				// copy for condensedFeaturesMap
				foreach (KeyValuePair<int, IList<int>> entry in condensedFeaturesMap)
				{
					int key = entry.Key;
					IList<int> aList = entry.Value;
					foreach (int toCopyInto in aList)
					{
						double[] arr = EForADoc[key];
						double[] targetArr = new double[arr.Length];
						for (int i_1 = 0; i_1 < arr.Length; i_1++)
						{
							targetArr[i_1] = arr[i_1];
						}
						EForADoc[toCopyInto] = targetArr;
					}
				}
			}
			IDictionary<int, double[]> dropoutPriorGrad = null;
			if (prior == DropoutPrior)
			{
				// we can optimize this, this is too large, don't need this big
				dropoutPriorGrad = SparseE(activeFeatures);
				// log.info("computing dropout prior for doc " + docIndex + " ... ");
				prob -= GetDropoutPrior(cliqueTree, docData, EForADoc, docDataHash, activeFeatures, dropoutPriorGrad, condensedFeaturesMap, EForADocPos);
			}
			// log.info(" done!");
			return new Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>(docIndex, prob, EForADoc, dropoutPriorGrad);
		}

		private void IncreScore(IDictionary<int, double[]> aMap, int fIndex, int k, double val)
		{
			aMap[fIndex][k] += val;
		}

		private void IncreScoreAllowNull(IDictionary<int, double[]> aMap, int fIndex, int k, double val)
		{
			if (!aMap.Contains(fIndex))
			{
				aMap[fIndex] = new double[map[fIndex] == 0 ? nodeLabelIndexSize : edgeLabelIndexSize];
			}
			aMap[fIndex][k] += val;
		}

		private void InitializeDataFeatureHash()
		{
			int macroActiveFeatureTotalCount = 0;
			int macroCondensedTotalCount = 0;
			int macroDocPosCount = 0;
			log.Info("initializing data feature hash, sup-data size: " + data.Length + ", unsup data size: " + (totalData.Length - data.Length));
			dataFeatureHash = new List<IList<ICollection<int>>>(totalData.Length);
			condensedMap = new List<IDictionary<int, IList<int>>>(totalData.Length);
			dataFeatureHashByDoc = new int[totalData.Length][];
			for (int m = 0; m < totalData.Length; m++)
			{
				IDictionary<int, int> occurPos = new Dictionary<int, int>();
				int[][][] aDoc = totalData[m];
				IList<ICollection<int>> aList = new List<ICollection<int>>(aDoc.Length);
				ICollection<int> setOfFeatures = new HashSet<int>();
				for (int i = 0; i < aDoc.Length; i++)
				{
					// positions in docI
					ICollection<int> aSet = new HashSet<int>();
					int[][] dataI = aDoc[i];
					for (int j = 0; j < dataI.Length; j++)
					{
						int[] dataJ = dataI[j];
						foreach (int item in dataJ)
						{
							if (j == 0)
							{
								if (occurPos.Contains(item))
								{
									occurPos[item] = -1;
								}
								else
								{
									occurPos[item] = i;
								}
							}
							aSet.Add(item);
						}
					}
					aList.Add(aSet);
					Sharpen.Collections.AddAll(setOfFeatures, aSet);
				}
				macroDocPosCount += aDoc.Length;
				macroActiveFeatureTotalCount += setOfFeatures.Count;
				// examine all singletons, merge ones in the same position
				IDictionary<int, IList<int>> condensedFeaturesMap = new Dictionary<int, IList<int>>();
				int[] representFeatures = new int[aDoc.Length];
				Arrays.Fill(representFeatures, -1);
				foreach (KeyValuePair<int, int> entry in occurPos)
				{
					int key = entry.Key;
					int pos = entry.Value;
					if (pos != -1)
					{
						if (representFeatures[pos] == -1)
						{
							// use this as representFeatures
							representFeatures[pos] = key;
							condensedFeaturesMap[key] = new List<int>();
						}
						else
						{
							// condense this one
							int rep = representFeatures[pos];
							condensedFeaturesMap[rep].Add(key);
							// remove key
							aList[pos].Remove(key);
							setOfFeatures.Remove(key);
						}
					}
				}
				int condensedCount = 0;
				for (IEnumerator<KeyValuePair<int, IList<int>>> it = condensedFeaturesMap.GetEnumerator(); it.MoveNext(); )
				{
					KeyValuePair<int, IList<int>> entry_1 = it.Current;
					if (entry_1.Value.Count == 0)
					{
						it.Remove();
					}
				}
				macroCondensedTotalCount += setOfFeatures.Count;
				condensedMap.Add(condensedFeaturesMap);
				dataFeatureHash.Add(aList);
				int[] arrOfIndex = new int[setOfFeatures.Count];
				int pos2 = 0;
				foreach (int ind in setOfFeatures)
				{
					arrOfIndex[pos2++] = ind;
				}
				dataFeatureHashByDoc[m] = arrOfIndex;
			}
			log.Info("Avg. active features per position: " + (macroActiveFeatureTotalCount / (macroDocPosCount + 0.0)));
			log.Info("Avg. condensed features per position: " + (macroCondensedTotalCount / (macroDocPosCount + 0.0)));
			log.Info("initializing data feature hash done!");
		}

		private double GetDropoutPrior(CRFCliqueTree<string> cliqueTree, int[][][] docData, IDictionary<int, double[]> EForADoc, IList<ICollection<int>> docDataHash, int[] activeFeatures, IDictionary<int, double[]> dropoutPriorGrad, IDictionary<int, 
			IList<int>> condensedFeaturesMap, IList<IDictionary<int, double[]>> EForADocPos)
		{
			IDictionary<int, double[]> dropoutPriorGradFirstHalf = SparseE(activeFeatures);
			Timing timer = new Timing();
			double priorValue = 0;
			long elapsedMs = 0;
			Pair<double[][][], double[][][]> condProbs = GetCondProbs(cliqueTree, docData);
			// first index position is curr index, second index curr-class, third index prev-class
			// e.g. [1][2][3] means curr is at position 1 with class 2, prev is at position 0 with class 3
			double[][][] prevGivenCurr = condProbs.First();
			// first index position is curr index, second index curr-class, third index next-class
			// e.g. [0][2][3] means curr is at position 0 with class 2, next is at position 1 with class 3
			double[][][] nextGivenCurr = condProbs.Second();
			// first dim is doc length (i)
			// second dim is numOfFeatures (fIndex)
			// third dim is numClasses (y)
			// fourth dim is labelIndexSize (matching the clique type of fIndex, for \theta)
			double[][][][] FAlpha = null;
			double[][][][] FBeta = null;
			if (!dropoutApprox)
			{
				FAlpha = new double[docData.Length][][][];
				FBeta = new double[docData.Length][][][];
			}
			for (int i = 0; i < docData.Length; i++)
			{
				if (!dropoutApprox)
				{
					FAlpha[i] = new double[activeFeatures.Length][][];
					FBeta[i] = new double[activeFeatures.Length][][];
				}
			}
			if (!dropoutApprox)
			{
				// computing FAlpha
				int fIndex = 0;
				double aa;
				double bb;
				double cc = 0;
				bool prevFeaturePresent = false;
				for (int i_1 = 1; i_1 < docData.Length; i_1++)
				{
					// for each possible clique at this position
					ICollection<int> docDataHashIMinusOne = docDataHash[i_1 - 1];
					for (int fIndexPos = 0; fIndexPos < activeFeatures.Length; fIndexPos++)
					{
						fIndex = activeFeatures[fIndexPos];
						prevFeaturePresent = docDataHashIMinusOne.Contains(fIndex);
						int j = map[fIndex];
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						int labelIndexSize = labelIndex.Size();
						if (FAlpha[i_1 - 1][fIndexPos] == null)
						{
							FAlpha[i_1 - 1][fIndexPos] = new double[numClasses][];
							for (int q = 0; q < numClasses; q++)
							{
								FAlpha[i_1 - 1][fIndexPos][q] = new double[labelIndexSize];
							}
						}
						foreach (KeyValuePair<int, IList<int>> entry in currPrevLabelsMap)
						{
							int y = entry.Key;
							// value at i-1
							double[] sum = new double[labelIndexSize];
							foreach (int yPrime in entry.Value)
							{
								// value at i-2
								for (int kk = 0; kk < labelIndexSize; kk++)
								{
									int[] prevLabel = labelIndex.Get(kk).GetLabel();
									aa = (prevGivenCurr[i_1 - 1][y][yPrime]);
									bb = (prevFeaturePresent && ((j == 0 && prevLabel[0] == y) || (j == 1 && prevLabel[1] == y && prevLabel[0] == yPrime)) ? 1 : 0);
									cc = 0;
									if (FAlpha[i_1 - 1][fIndexPos][yPrime] != null)
									{
										cc = FAlpha[i_1 - 1][fIndexPos][yPrime][kk];
									}
									sum[kk] += aa * (bb + cc);
								}
							}
							// sum[kk] += (prevGivenCurr[i-1][y][yPrime]) * ((prevFeaturePresent && ((j == 0 && prevLabel[0] == y) || (j == 1 && prevLabel[1] == y && prevLabel[0] == yPrime)) ? 1 : 0) + FAlpha[i-1][fIndexPos][yPrime][kk]);
							if (FAlpha[i_1][fIndexPos] == null)
							{
								FAlpha[i_1][fIndexPos] = new double[numClasses][];
							}
							FAlpha[i_1][fIndexPos][y] = sum;
						}
					}
				}
				// computing FBeta
				int docDataLen = docData.Length;
				for (int i_2 = docDataLen - 2; i_2 >= 0; i_2--)
				{
					ICollection<int> docDataHashIPlusOne = docDataHash[i_2 + 1];
					// for each possible clique at this position
					for (int fIndexPos = 0; fIndexPos < activeFeatures.Length; fIndexPos++)
					{
						fIndex = activeFeatures[fIndexPos];
						bool nextFeaturePresent = docDataHashIPlusOne.Contains(fIndex);
						int j = map[fIndex];
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						int labelIndexSize = labelIndex.Size();
						if (FBeta[i_2 + 1][fIndexPos] == null)
						{
							FBeta[i_2 + 1][fIndexPos] = new double[numClasses][];
							for (int q = 0; q < numClasses; q++)
							{
								FBeta[i_2 + 1][fIndexPos][q] = new double[labelIndexSize];
							}
						}
						foreach (KeyValuePair<int, IList<int>> entry in currNextLabelsMap)
						{
							int y = entry.Key;
							// value at i
							double[] sum = new double[labelIndexSize];
							foreach (int yPrime in entry.Value)
							{
								// value at i+1
								for (int kk = 0; kk < labelIndexSize; kk++)
								{
									int[] nextLabel = labelIndex.Get(kk).GetLabel();
									// log.info("labelIndexSize:"+labelIndexSize+", nextGivenCurr:"+nextGivenCurr+", nextLabel:"+nextLabel+", FBeta["+(i+1)+"]["+ fIndexPos +"]["+yPrime+"] :"+FBeta[i+1][fIndexPos][yPrime]);
									aa = (nextGivenCurr[i_2][y][yPrime]);
									bb = (nextFeaturePresent && ((j == 0 && nextLabel[0] == yPrime) || (j == 1 && nextLabel[0] == y && nextLabel[1] == yPrime)) ? 1 : 0);
									cc = 0;
									if (FBeta[i_2 + 1][fIndexPos][yPrime] != null)
									{
										cc = FBeta[i_2 + 1][fIndexPos][yPrime][kk];
									}
									sum[kk] += aa * (bb + cc);
								}
							}
							// sum[kk] += (nextGivenCurr[i][y][yPrime]) * ( (nextFeaturePresent && ((j == 0 && nextLabel[0] == yPrime) || (j == 1 && nextLabel[0] == y && nextLabel[1] == yPrime)) ? 1 : 0) + FBeta[i+1][fIndexPos][yPrime][kk]);
							if (FBeta[i_2][fIndexPos] == null)
							{
								FBeta[i_2][fIndexPos] = new double[numClasses][];
							}
							FBeta[i_2][fIndexPos][y] = sum;
						}
					}
				}
			}
			// derivative equals: VarU' * PtYYp * (1-PtYYp) + VarU * PtYYp' * (1-PtYYp) + VarU * PtYYp * (1-PtYYp)'
			// derivative equals: VarU' * PtYYp * (1-PtYYp) + VarU * PtYYp' * (1-PtYYp) + VarU * PtYYp * -PtYYp'
			// derivative equals: VarU' * PtYYp * (1-PtYYp) + VarU * PtYYp' * (1 - 2 * PtYYp)
			double deltaDivByOneMinusDelta = delta / (1.0 - delta);
			Timing innerTimer = new Timing();
			long eTiming = 0;
			long dropoutTiming = 0;
			bool containsFeature = false;
			// iterate over the positions in this document
			for (int i_3 = 1; i_3 < docData.Length; i_3++)
			{
				ICollection<int> docDataHashI = docDataHash[i_3];
				IDictionary<int, double[]> EForADocPosAtI = null;
				if (dropoutApprox)
				{
					EForADocPosAtI = EForADocPos[i_3];
				}
				// for each possible clique at this position
				for (int k = 0; k < edgeLabelIndexSize; k++)
				{
					// sum over (y, y')
					int[] label = edgeLabels[k];
					int y = label[0];
					int yP = label[1];
					// important to use label as an int[] for calculating cliqueTree.prob()
					// if it's a node clique, and label index is 2, if we don't use int[]{2} but just pass 2,
					// cliqueTree is going to treat it as index of the edge clique labels, and convert 2
					// into int[]{0,2}, and return the edge prob marginal instead of node marginal
					double PtYYp = cliqueTree.Prob(i_3, label);
					double PtYYpTimesOneMinusPtYYp = PtYYp * (1.0 - PtYYp);
					double oneMinus2PtYYp = (1.0 - 2 * PtYYp);
					double USum = 0;
					int fIndex;
					for (int jjj = 0; jjj < labelIndices.Count; jjj++)
					{
						for (int n = 0; n < docData[i_3][jjj].Length; n++)
						{
							fIndex = docData[i_3][jjj][n];
							int valIndex;
							if (jjj == 1)
							{
								valIndex = k;
							}
							else
							{
								valIndex = yP;
							}
							double theta;
							try
							{
								theta = weights[fIndex][valIndex];
							}
							catch (Exception ex)
							{
								System.Console.Error.Printf("weights[%d][%d], map[%d]=%d, labelIndices.get(map[%d]).size() = %d, weights.length=%d\n", fIndex, valIndex, fIndex, map[fIndex], fIndex, labelIndices[map[fIndex]].Size(), weights.Length);
								throw new Exception(ex);
							}
							USum += weightSquare[fIndex][valIndex];
							// first half of derivative: VarU' * PtYYp * (1-PtYYp)
							double VarUp = deltaDivByOneMinusDelta * theta;
							IncreScoreAllowNull(dropoutPriorGradFirstHalf, fIndex, valIndex, VarUp * PtYYpTimesOneMinusPtYYp);
						}
					}
					double VarU = 0.5 * deltaDivByOneMinusDelta * USum;
					// update function objective
					priorValue += VarU * PtYYpTimesOneMinusPtYYp;
					double VarUTimesOneMinus2PtYYp = VarU * oneMinus2PtYYp;
					// second half of derivative: VarU * PtYYp' * (1 - 2 * PtYYp)
					// boolean prevFeaturePresent = false;
					// boolean nextFeaturePresent = false;
					for (int fIndexPos = 0; fIndexPos < activeFeatures.Length; fIndexPos++)
					{
						fIndex = activeFeatures[fIndexPos];
						containsFeature = docDataHashI.Contains(fIndex);
						// if (!containsFeature) continue;
						int jj = map[fIndex];
						IIndex<CRFLabel> fLabelIndex = labelIndices[jj];
						for (int kk = 0; kk < fLabelIndex.Size(); kk++)
						{
							// for all parameter \theta
							int[] fLabel = fLabelIndex.Get(kk).GetLabel();
							// if (FAlpha[i] != null)
							//   log.info("fIndex: " + fIndex+", FAlpha[i].size:"+FAlpha[i].length);
							double fCount = containsFeature && ((jj == 0 && fLabel[0] == yP) || (jj == 1 && k == kk)) ? 1 : 0;
							double alpha;
							double beta;
							double condE;
							double PtYYpPrime;
							if (!dropoutApprox)
							{
								alpha = ((FAlpha[i_3][fIndexPos] == null || FAlpha[i_3][fIndexPos][y] == null) ? 0 : FAlpha[i_3][fIndexPos][y][kk]);
								beta = ((FBeta[i_3][fIndexPos] == null || FBeta[i_3][fIndexPos][yP] == null) ? 0 : FBeta[i_3][fIndexPos][yP][kk]);
								condE = fCount + alpha + beta;
								PtYYpPrime = PtYYp * (condE - EForADoc[fIndex][kk]);
							}
							else
							{
								double E = 0;
								if (EForADocPosAtI.Contains(fIndex))
								{
									E = EForADocPosAtI[fIndex][kk];
								}
								condE = fCount;
								PtYYpPrime = PtYYp * (condE - E);
							}
							IncreScore(dropoutPriorGrad, fIndex, kk, VarUTimesOneMinus2PtYYp * PtYYpPrime);
						}
					}
				}
			}
			// copy for condensedFeaturesMap
			foreach (KeyValuePair<int, IList<int>> entry in condensedFeaturesMap)
			{
				int key = entry.Key;
				IList<int> aList = entry.Value;
				foreach (int toCopyInto in aList)
				{
					double[] arr = dropoutPriorGrad[key];
					double[] targetArr = new double[arr.Length];
					for (int i_1 = 0; i_1 < arr.Length; i_1++)
					{
						targetArr[i_1] = arr[i_1];
					}
					dropoutPriorGrad[toCopyInto] = targetArr;
				}
			}
			foreach (KeyValuePair<int, double[]> entry_1 in dropoutPriorGrad)
			{
				int key = entry_1.Key;
				double[] target = entry_1.Value;
				if (dropoutPriorGradFirstHalf.Contains(key))
				{
					double[] source = dropoutPriorGradFirstHalf[key];
					for (int i_1 = 0; i_1 < target.Length; i_1++)
					{
						target[i_1] += source[i_1];
					}
				}
			}
			// for (int i=0;i<dropoutPriorGrad.length;i++)
			//   for (int j=0; j<dropoutPriorGrad[i].length;j++) {
			//     if (DEBUG3)
			//       System.err.printf("f=%d, k=%d, dropoutPriorGradFirstHalf[%d][%d]=% 5.3f, dropoutPriorGrad[%d][%d]=% 5.3f\n", i, j, i, j, dropoutPriorGradFirstHalf[i][j], i, j, dropoutPriorGrad[i][j]);
			//     dropoutPriorGrad[i][j] += dropoutPriorGradFirstHalf[i][j];
			//   }
			return dropoutScale * priorValue;
		}

		public override void SetWeights(double[][] weights)
		{
			base.SetWeights(weights);
			if (weightSquare == null)
			{
				weightSquare = new double[weights.Length][];
				for (int i = 0; i < weights.Length; i++)
				{
					weightSquare[i] = new double[weights[i].Length];
				}
			}
			for (int i_1 = 0; i_1 < weights.Length; i_1++)
			{
				for (int j = 0; j < weights[i_1].Length; j++)
				{
					double w = weights[i_1][j];
					weightSquare[i_1][j] = w * w;
				}
			}
		}

		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			double prob = 0.0;
			// the log prob of the sequence given the model, which is the negation of value at this point
			// final double[][] weights = to2D(x);
			To2D(x, weights);
			SetWeights(weights);
			// the expectations over counts
			// first index is feature index, second index is of possible labeling
			// double[][] E = empty2D();
			Clear2D(E);
			Clear2D(dropoutPriorGradTotal);
			MulticoreWrapper<Pair<int, bool>, Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>> wrapper = new MulticoreWrapper<Pair<int, bool>, Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>>>
				(multiThreadGrad, dropoutPriorThreadProcessor);
			// supervised part
			for (int m = 0; m < totalData.Length; m++)
			{
				bool submitIsUnsup = (m >= unsupDropoutStartIndex);
				wrapper.Put(new Pair<int, bool>(m, submitIsUnsup));
				while (wrapper.Peek())
				{
					Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>> result = wrapper.Poll();
					int docIndex = result.First();
					bool isUnsup = docIndex >= unsupDropoutStartIndex;
					if (isUnsup)
					{
						prob += unsupDropoutScale * result.Second();
					}
					else
					{
						prob += result.Second();
					}
					IDictionary<int, double[]> partialDropout = result.Fourth();
					if (partialDropout != null)
					{
						if (isUnsup)
						{
							Combine2DArr(dropoutPriorGradTotal, partialDropout, unsupDropoutScale);
						}
						else
						{
							Combine2DArr(dropoutPriorGradTotal, partialDropout);
						}
					}
					if (!isUnsup)
					{
						IDictionary<int, double[]> partialE = result.Third();
						if (partialE != null)
						{
							Combine2DArr(E, partialE);
						}
					}
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				Quadruple<int, double, IDictionary<int, double[]>, IDictionary<int, double[]>> result = wrapper.Poll();
				int docIndex = result.First();
				bool isUnsup = docIndex >= unsupDropoutStartIndex;
				if (isUnsup)
				{
					prob += unsupDropoutScale * result.Second();
				}
				else
				{
					prob += result.Second();
				}
				IDictionary<int, double[]> partialDropout = result.Fourth();
				if (partialDropout != null)
				{
					if (isUnsup)
					{
						Combine2DArr(dropoutPriorGradTotal, partialDropout, unsupDropoutScale);
					}
					else
					{
						Combine2DArr(dropoutPriorGradTotal, partialDropout);
					}
				}
				if (!isUnsup)
				{
					IDictionary<int, double[]> partialE = result.Third();
					if (partialE != null)
					{
						Combine2DArr(E, partialE);
					}
				}
			}
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunctionWithDropout.calculate()" + " - this may well indicate numeric underflow due to overly long documents.");
			}
			// because we minimize -L(\theta)
			value = -prob;
			if (Verbose)
			{
				log.Info("value is " + System.Math.Exp(-value));
			}
			// compute the partial derivative for each feature by comparing expected counts to empirical counts
			int index = 0;
			for (int i = 0; i < E.Length; i++)
			{
				for (int j = 0; j < E[i].Length; j++)
				{
					// because we minimize -L(\theta)
					derivative[index] = (E[i][j] - Ehat[i][j]);
					derivative[index] += dropoutScale * dropoutPriorGradTotal[i][j];
					if (Verbose)
					{
						log.Info("deriv(" + i + ',' + j + ") = " + E[i][j] + " - " + Ehat[i][j] + " = " + derivative[index]);
					}
					index++;
				}
			}
		}
	}
}
