// CRFClassifier -- a probabilistic (CRF) sequence model, mainly used for NER.
// Copyright (c) 2002-2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>
	/// Subclass of
	/// <see cref="CRFClassifier{IN}"/>
	/// for learning Logarithmic Opinion Pools.
	/// </summary>
	/// <author>Mengqiu Wang</author>
	public class CRFClassifierWithLOP<In> : CRFClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierWithLOP));

		private IList<ICollection<int>> featureIndicesSetArray;

		private IList<IList<int>> featureIndicesListArray;

		protected internal CRFClassifierWithLOP()
			: base(new SeqClassifierFlags())
		{
		}

		public CRFClassifierWithLOP(Properties props)
			: base(props)
		{
		}

		public CRFClassifierWithLOP(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		private int[][][][] CreatePartialDataForLOP(int lopIter, int[][][][] data)
		{
			List<int> newFeatureList = new List<int>(1000);
			ICollection<int> featureIndicesSet = featureIndicesSetArray[lopIter];
			int[][][][] newData = new int[data.Length][][][];
			for (int i = 0; i < data.Length; i++)
			{
				newData[i] = new int[data[i].Length][][];
				for (int j = 0; j < data[i].Length; j++)
				{
					newData[i][j] = new int[data[i][j].Length][];
					for (int k = 0; k < data[i][j].Length; k++)
					{
						int[] oldFeatures = data[i][j][k];
						newFeatureList.Clear();
						foreach (int oldFeatureIndex in oldFeatures)
						{
							if (featureIndicesSet.Contains(oldFeatureIndex))
							{
								newFeatureList.Add(oldFeatureIndex);
							}
						}
						newData[i][j][k] = new int[newFeatureList.Count];
						for (int l = 0; l < newFeatureList.Count; ++l)
						{
							newData[i][j][k][l] = newFeatureList[l];
						}
					}
				}
			}
			return newData;
		}

		private void GetFeatureBoundaryIndices(int numFeatures, int numLopExpert)
		{
			// first find begin/end feature index for each expert
			int interval = numFeatures / numLopExpert;
			featureIndicesSetArray = new List<ICollection<int>>(numLopExpert);
			featureIndicesListArray = new List<IList<int>>(numLopExpert);
			for (int i = 0; i < numLopExpert; i++)
			{
				featureIndicesSetArray.Add(Generics.NewHashSet<int>(interval));
				featureIndicesListArray.Add(Generics.NewArrayList<int>(interval));
			}
			if (flags.randomLopFeatureSplit)
			{
				for (int fIndex = 0; fIndex < numFeatures; fIndex++)
				{
					int lopIter = random.NextInt(numLopExpert);
					featureIndicesSetArray[lopIter].Add(fIndex);
					featureIndicesListArray[lopIter].Add(fIndex);
				}
			}
			else
			{
				for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
				{
					int beginIndex = lopIter * interval;
					int endIndex = (lopIter + 1) * interval;
					if (lopIter == numLopExpert - 1)
					{
						endIndex = numFeatures;
					}
					for (int fIndex = beginIndex; fIndex < endIndex; fIndex++)
					{
						featureIndicesSetArray[lopIter].Add(fIndex);
						featureIndicesListArray[lopIter].Add(fIndex);
					}
				}
			}
			for (int lopIter_1 = 0; lopIter_1 < numLopExpert; lopIter_1++)
			{
				featureIndicesListArray[lopIter_1].Sort();
			}
		}

		protected internal override double[] TrainWeights(int[][][][] data, int[][] labels, IEvaluator[] evaluators, int pruneFeatureItr, double[][][][] featureVals)
		{
			int numFeatures = featureIndex.Size();
			int numLopExpert = flags.numLopExpert;
			double[][] lopExpertWeights = new double[numLopExpert][];
			GetFeatureBoundaryIndices(numFeatures, numLopExpert);
			if (flags.initialLopWeights != null)
			{
				try
				{
					using (BufferedReader br = IOUtils.ReaderFromString(flags.initialLopWeights))
					{
						log.Info("Reading initial LOP weights from file " + flags.initialLopWeights + " ...");
						IList<double[]> listOfWeights = new List<double[]>(numLopExpert);
						for (string line; (line = br.ReadLine()) != null; )
						{
							line = line.Trim();
							string[] parts = line.Split("\t");
							double[] wArr = new double[parts.Length];
							for (int i = 0; i < parts.Length; i++)
							{
								wArr[i] = double.ParseDouble(parts[i]);
							}
							listOfWeights.Add(wArr);
						}
						System.Diagnostics.Debug.Assert((listOfWeights.Count == numLopExpert));
						log.Info("Done!");
						for (int i_1 = 0; i_1 < numLopExpert; i_1++)
						{
							lopExpertWeights[i_1] = listOfWeights[i_1];
						}
					}
				}
				catch (IOException)
				{
					// DataInputStream dis = new DataInputStream(new BufferedInputStream(new GZIPInputStream(new FileInputStream(
					//     flags.initialLopWeights))));
					// initialScales = Convert.readDoubleArr(dis);
					throw new Exception("Could not read from double initial LOP weights file " + flags.initialLopWeights);
				}
			}
			else
			{
				for (int lopIter = 0; lopIter < numLopExpert; lopIter++)
				{
					int[][][][] partialData = CreatePartialDataForLOP(lopIter, data);
					if (flags.randomLopWeights)
					{
						lopExpertWeights[lopIter] = base.GetObjectiveFunction(partialData, labels).Initial();
					}
					else
					{
						lopExpertWeights[lopIter] = base.TrainWeights(partialData, labels, evaluators, pruneFeatureItr, null);
					}
				}
				if (flags.includeFullCRFInLOP)
				{
					double[][] newLopExpertWeights = new double[numLopExpert + 1][];
					System.Array.Copy(lopExpertWeights, 0, newLopExpertWeights, 0, lopExpertWeights.Length);
					if (flags.randomLopWeights)
					{
						newLopExpertWeights[numLopExpert] = base.GetObjectiveFunction(data, labels).Initial();
					}
					else
					{
						newLopExpertWeights[numLopExpert] = base.TrainWeights(data, labels, evaluators, pruneFeatureItr, null);
					}
					ICollection<int> newSet = Generics.NewHashSet(numFeatures);
					IList<int> newList = new List<int>(numFeatures);
					for (int fIndex = 0; fIndex < numFeatures; fIndex++)
					{
						newSet.Add(fIndex);
						newList.Add(fIndex);
					}
					featureIndicesSetArray.Add(newSet);
					featureIndicesListArray.Add(newList);
					numLopExpert += 1;
					lopExpertWeights = newLopExpertWeights;
				}
			}
			// Dumb scales
			// double[] lopScales = new double[numLopExpert];
			// Arrays.fill(lopScales, 1.0);
			CRFLogConditionalObjectiveFunctionForLOP func = new CRFLogConditionalObjectiveFunctionForLOP(data, labels, lopExpertWeights, windowSize, classIndex, labelIndices, map, flags.backgroundSymbol, numLopExpert, featureIndicesSetArray, featureIndicesListArray
				, flags.backpropLopTraining);
			cliquePotentialFunctionHelper = func;
			IMinimizer<IDiffFunction> minimizer = GetMinimizer(0, evaluators);
			double[] initialScales;
			//TODO(mengqiu) clean this part up when backpropLogTraining == true
			if (flags.initialLopScales == null)
			{
				initialScales = func.Initial();
			}
			else
			{
				log.Info("Reading initial LOP scales from file " + flags.initialLopScales);
				try
				{
					using (DataInputStream dis = new DataInputStream(new BufferedInputStream(new GZIPInputStream(new FileInputStream(flags.initialLopScales)))))
					{
						initialScales = ConvertByteArray.ReadDoubleArr(dis);
					}
				}
				catch (IOException)
				{
					throw new Exception("Could not read from double initial LOP scales file " + flags.initialLopScales);
				}
			}
			double[] learnedParams = minimizer.Minimize(func, flags.tolerance, initialScales);
			double[] rawScales = func.SeparateLopScales(learnedParams);
			double[] lopScales = ArrayMath.Softmax(rawScales);
			log.Info("After SoftMax Transformation, learned scales are:");
			for (int lopIter_1 = 0; lopIter_1 < numLopExpert; lopIter_1++)
			{
				log.Info("lopScales[" + lopIter_1 + "] = " + lopScales[lopIter_1]);
			}
			double[][] learnedLopExpertWeights = lopExpertWeights;
			if (flags.backpropLopTraining)
			{
				learnedLopExpertWeights = func.SeparateLopExpertWeights(learnedParams);
			}
			return CRFLogConditionalObjectiveFunctionForLOP.CombineAndScaleLopWeights(numLopExpert, learnedLopExpertWeights, lopScales);
		}
	}
}
