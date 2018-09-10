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
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Microsoft.Extensions.Configuration;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>
	/// Subclass of
	/// <see cref="CRFClassifier{IN}"/>
	/// for implementing the nonlinear architecture in [Wang and Manning IJCNLP-2013 Effect of Nonlinear ...].
	/// </summary>
	/// <author>Mengqiu Wang</author>
	public class CRFClassifierNonlinear<In> : CRFClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifierNonlinear<In>));

		/// <summary>Parameter weights of the classifier.</summary>
		private double[][] linearWeights;

		private double[][] inputLayerWeights4Edge;

		private double[][] outputLayerWeights4Edge;

		private double[][] inputLayerWeights;

		private double[][] outputLayerWeights;

		protected internal CRFClassifierNonlinear()
			: base(new SeqClassifierFlags())
		{
		}

		public CRFClassifierNonlinear(IConfiguration props)
			: base(new SeqClassifierFlags(props)
		{
		}

		public CRFClassifierNonlinear(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		public override Triple<int[][][], int[], double[][][]> DocumentToDataAndLabels(IList<In> document)
		{
			Triple<int[][][], int[], double[][][]> result = base.DocumentToDataAndLabels(document);
			int[][][] data = result.First();
			data = TransformDocData(data);
			return new Triple<int[][][], int[], double[][][]>(data, result.Second(), result.Third());
		}

		private int[][][] TransformDocData(int[][][] docData)
		{
			int[][][] transData = new int[docData.Length][][];
			for (int i = 0; i < docData.Length; i++)
			{
				transData[i] = new int[docData[i].Length][];
				for (int j = 0; j < docData[i].Length; j++)
				{
					int[] cliqueFeatures = docData[i][j];
					transData[i][j] = new int[cliqueFeatures.Length];
					for (int n = 0; n < cliqueFeatures.Length; n++)
					{
						int transFeatureIndex = -1;
						if (j == 0)
						{
							transFeatureIndex = nodeFeatureIndicesMap.IndexOf(cliqueFeatures[n]);
							if (transFeatureIndex == -1)
							{
								throw new Exception("node cliqueFeatures[n]=" + cliqueFeatures[n] + " not found, nodeFeatureIndicesMap.size=" + nodeFeatureIndicesMap.Size());
							}
						}
						else
						{
							transFeatureIndex = edgeFeatureIndicesMap.IndexOf(cliqueFeatures[n]);
							if (transFeatureIndex == -1)
							{
								throw new Exception("edge cliqueFeatures[n]=" + cliqueFeatures[n] + " not found, edgeFeatureIndicesMap.size=" + edgeFeatureIndicesMap.Size());
							}
						}
						transData[i][j][n] = transFeatureIndex;
					}
				}
			}
			return transData;
		}

		protected internal override ICliquePotentialFunction GetCliquePotentialFunctionForTest()
		{
			if (cliquePotentialFunction == null)
			{
				if (flags.secondOrderNonLinear)
				{
					cliquePotentialFunction = new NonLinearSecondOrderCliquePotentialFunction(inputLayerWeights4Edge, outputLayerWeights4Edge, inputLayerWeights, outputLayerWeights, flags);
				}
				else
				{
					cliquePotentialFunction = new NonLinearCliquePotentialFunction(linearWeights, inputLayerWeights, outputLayerWeights, flags);
				}
			}
			return cliquePotentialFunction;
		}

		protected internal override double[] TrainWeights(int[][][][] data, int[][] labels, IEvaluator[] evaluators, int pruneFeatureItr, double[][][][] featureVals)
		{
			if (flags.secondOrderNonLinear)
			{
				CRFNonLinearSecondOrderLogConditionalObjectiveFunction func = new CRFNonLinearSecondOrderLogConditionalObjectiveFunction(data, labels, windowSize, classIndex, labelIndices, map, flags, nodeFeatureIndicesMap.Size(), edgeFeatureIndicesMap.Size
					());
				cliquePotentialFunctionHelper = func;
				double[] allWeights = TrainWeightsUsingNonLinearCRF(func, evaluators);
				Quadruple<double[][], double[][], double[][], double[][]> @params = func.SeparateWeights(allWeights);
				this.inputLayerWeights4Edge = @params.First();
				this.outputLayerWeights4Edge = @params.Second();
				this.inputLayerWeights = @params.Third();
				this.outputLayerWeights = @params.Fourth();
			}
			else
			{
				CRFNonLinearLogConditionalObjectiveFunction func = new CRFNonLinearLogConditionalObjectiveFunction(data, labels, windowSize, classIndex, labelIndices, map, flags, nodeFeatureIndicesMap.Size(), edgeFeatureIndicesMap.Size(), featureVals);
				if (flags.useAdaGradFOBOS)
				{
					func.gradientsOnly = true;
				}
				cliquePotentialFunctionHelper = func;
				double[] allWeights = TrainWeightsUsingNonLinearCRF(func, evaluators);
				Triple<double[][], double[][], double[][]> @params = func.SeparateWeights(allWeights);
				this.linearWeights = @params.First();
				this.inputLayerWeights = @params.Second();
				this.outputLayerWeights = @params.Third();
			}
			return null;
		}

		private double[] TrainWeightsUsingNonLinearCRF(AbstractCachingDiffFunction func, IEvaluator[] evaluators)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer(0, evaluators);
			double[] initialWeights;
			if (flags.initialWeights == null)
			{
				initialWeights = func.Initial();
			}
			else
			{
				log.Info("Reading initial weights from file " + flags.initialWeights);
				try
				{
					using (DataInputStream dis = new DataInputStream(new BufferedInputStream(new GZIPInputStream(new FileInputStream(flags.initialWeights)))))
					{
						initialWeights = ConvertByteArray.ReadDoubleArr(dis);
					}
				}
				catch (IOException)
				{
					throw new Exception("Could not read from double initial weight file " + flags.initialWeights);
				}
			}
			log.Info("numWeights: " + initialWeights.Length);
			if (flags.testObjFunction)
			{
				StochasticDiffFunctionTester tester = new StochasticDiffFunctionTester(func);
				if (tester.TestSumOfBatches(initialWeights, 1e-4))
				{
					log.Info("Testing complete... exiting");
					System.Environment.Exit(1);
				}
				else
				{
					log.Info("Testing failed....exiting");
					System.Environment.Exit(1);
				}
			}
			//check gradient
			if (flags.checkGradient)
			{
				if (func.GradientCheck())
				{
					log.Info("gradient check passed");
				}
				else
				{
					throw new Exception("gradient check failed");
				}
			}
			return minimizer.Minimize(func, flags.tolerance, initialWeights);
		}

		/// <exception cref="System.Exception"/>
		protected internal override void SerializeTextClassifier(PrintWriter pw)
		{
			base.SerializeTextClassifier(pw);
			pw.Printf("nodeFeatureIndicesMap.size()=\t%d%n", nodeFeatureIndicesMap.Size());
			for (int i = 0; i < nodeFeatureIndicesMap.Size(); i++)
			{
				pw.Printf("%d\t%d%n", i, nodeFeatureIndicesMap.Get(i));
			}
			pw.Printf("edgeFeatureIndicesMap.size()=\t%d%n", edgeFeatureIndicesMap.Size());
			for (int i_1 = 0; i_1 < edgeFeatureIndicesMap.Size(); i_1++)
			{
				pw.Printf("%d\t%d%n", i_1, edgeFeatureIndicesMap.Get(i_1));
			}
			if (flags.secondOrderNonLinear)
			{
				pw.Printf("inputLayerWeights4Edge.length=\t%d%n", inputLayerWeights4Edge.Length);
				foreach (double[] ws in inputLayerWeights4Edge)
				{
					List<double> list = new List<double>();
					foreach (double w in ws)
					{
						list.Add(w);
					}
					pw.Printf("%d\t%s%n", ws.Length, StringUtils.Join(list, " "));
				}
				pw.Printf("outputLayerWeights4Edge.length=\t%d%n", outputLayerWeights4Edge.Length);
				foreach (double[] ws_1 in outputLayerWeights4Edge)
				{
					List<double> list = new List<double>();
					foreach (double w in ws_1)
					{
						list.Add(w);
					}
					pw.Printf("%d\t%s%n", ws_1.Length, StringUtils.Join(list, " "));
				}
			}
			else
			{
				pw.Printf("linearWeights.length=\t%d%n", linearWeights.Length);
				foreach (double[] ws in linearWeights)
				{
					List<double> list = new List<double>();
					foreach (double w in ws)
					{
						list.Add(w);
					}
					pw.Printf("%d\t%s%n", ws.Length, StringUtils.Join(list, " "));
				}
			}
			pw.Printf("inputLayerWeights.length=\t%d%n", inputLayerWeights.Length);
			foreach (double[] ws_2 in inputLayerWeights)
			{
				List<double> list = new List<double>();
				foreach (double w in ws_2)
				{
					list.Add(w);
				}
				pw.Printf("%d\t%s%n", ws_2.Length, StringUtils.Join(list, " "));
			}
			pw.Printf("outputLayerWeights.length=\t%d%n", outputLayerWeights.Length);
			foreach (double[] ws_3 in outputLayerWeights)
			{
				List<double> list = new List<double>();
				foreach (double w in ws_3)
				{
					list.Add(w);
				}
				pw.Printf("%d\t%s%n", ws_3.Length, StringUtils.Join(list, " "));
			}
		}

		/// <exception cref="System.Exception"/>
		protected internal override void LoadTextClassifier(StreamReader br)
		{
			base.LoadTextClassifier(br);
			string line = br.ReadLine();
			string[] toks = line.Split('\t');
			if (!toks[0].Equals("nodeFeatureIndicesMap.size()="))
			{
				throw new Exception("format error in nodeFeatureIndicesMap");
			}
			int nodeFeatureIndicesMapSize = System.Convert.ToInt32(toks[1]);
			nodeFeatureIndicesMap = new HashIndex<int>();
			int count = 0;
			while (count < nodeFeatureIndicesMapSize)
			{
				line = br.ReadLine();
				toks = line.Split('\t');
				int idx = System.Convert.ToInt32(toks[0]);
				if (count != idx)
				{
					throw new Exception("format error");
				}
				nodeFeatureIndicesMap.Add(System.Convert.ToInt32(toks[1]));
				count++;
			}
			line = br.ReadLine();
			toks = line.Split('\t');
			if (!toks[0].Equals("edgeFeatureIndicesMap.size()="))
			{
				throw new Exception("format error");
			}
			int edgeFeatureIndicesMapSize = System.Convert.ToInt32(toks[1]);
			edgeFeatureIndicesMap = new HashIndex<int>();
			count = 0;
			while (count < edgeFeatureIndicesMapSize)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int idx = System.Convert.ToInt32(toks[0]);
				if (count != idx)
				{
					throw new Exception("format error");
				}
				edgeFeatureIndicesMap.Add(System.Convert.ToInt32(toks[1]));
				count++;
			}
			int weightsLength = -1;
			if (flags.secondOrderNonLinear)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				if (!toks[0].Equals("inputLayerWeights4Edge.length="))
				{
					throw new Exception("format error");
				}
				weightsLength = System.Convert.ToInt32(toks[1]);
				inputLayerWeights4Edge = new double[weightsLength][];
				count = 0;
				while (count < weightsLength)
				{
					line = br.ReadLine();
					toks = line.Split("\\t");
					int weights2Length = System.Convert.ToInt32(toks[0]);
					inputLayerWeights4Edge[count] = new double[weights2Length];
					string[] weightsValue = toks[1].Split(" ");
					if (weights2Length != weightsValue.Length)
					{
						throw new Exception("weights format error");
					}
					for (int i2 = 0; i2 < weights2Length; i2++)
					{
						inputLayerWeights4Edge[count][i2] = double.Parse(weightsValue[i2]);
					}
					count++;
				}
				line = br.ReadLine();
				toks = line.Split("\\t");
				if (!toks[0].Equals("outputLayerWeights4Edge.length="))
				{
					throw new Exception("format error");
				}
				weightsLength = System.Convert.ToInt32(toks[1]);
				outputLayerWeights4Edge = new double[weightsLength][];
				count = 0;
				while (count < weightsLength)
				{
					line = br.ReadLine();
					toks = line.Split("\\t");
					int weights2Length = System.Convert.ToInt32(toks[0]);
					outputLayerWeights4Edge[count] = new double[weights2Length];
					string[] weightsValue = toks[1].Split(" ");
					if (weights2Length != weightsValue.Length)
					{
						throw new Exception("weights format error");
					}
					for (int i2 = 0; i2 < weights2Length; i2++)
					{
						outputLayerWeights4Edge[count][i2] = double.Parse(weightsValue[i2]);
					}
					count++;
				}
			}
			else
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				if (!toks[0].Equals("linearWeights.length="))
				{
					throw new Exception("format error");
				}
				weightsLength = System.Convert.ToInt32(toks[1]);
				linearWeights = new double[weightsLength][];
				count = 0;
				while (count < weightsLength)
				{
					line = br.ReadLine();
					toks = line.Split("\\t");
					int weights2Length = System.Convert.ToInt32(toks[0]);
					linearWeights[count] = new double[weights2Length];
					string[] weightsValue = toks[1].Split(" ");
					if (weights2Length != weightsValue.Length)
					{
						throw new Exception("weights format error");
					}
					for (int i2 = 0; i2 < weights2Length; i2++)
					{
						linearWeights[count][i2] = double.Parse(weightsValue[i2]);
					}
					count++;
				}
			}
			line = br.ReadLine();
			toks = line.Split("\\t");
			if (!toks[0].Equals("inputLayerWeights.length="))
			{
				throw new Exception("format error");
			}
			weightsLength = System.Convert.ToInt32(toks[1]);
			inputLayerWeights = new double[weightsLength][];
			count = 0;
			while (count < weightsLength)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int weights2Length = System.Convert.ToInt32(toks[0]);
				inputLayerWeights[count] = new double[weights2Length];
				string[] weightsValue = toks[1].Split(" ");
				if (weights2Length != weightsValue.Length)
				{
					throw new Exception("weights format error");
				}
				for (int i2 = 0; i2 < weights2Length; i2++)
				{
					inputLayerWeights[count][i2] = double.Parse(weightsValue[i2]);
				}
				count++;
			}
			line = br.ReadLine();
			toks = line.Split("\\t");
			if (!toks[0].Equals("outputLayerWeights.length="))
			{
				throw new Exception("format error");
			}
			weightsLength = System.Convert.ToInt32(toks[1]);
			outputLayerWeights = new double[weightsLength][];
			count = 0;
			while (count < weightsLength)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int weights2Length = System.Convert.ToInt32(toks[0]);
				outputLayerWeights[count] = new double[weights2Length];
				string[] weightsValue = toks[1].Split(" ");
				if (weights2Length != weightsValue.Length)
				{
					throw new Exception("weights format error");
				}
				for (int i2 = 0; i2 < weights2Length; i2++)
				{
					outputLayerWeights[count][i2] = double.Parse(weightsValue[i2]);
				}
				count++;
			}
		}

		public override void SerializeClassifier(ObjectOutputStream oos)
		{
			try
			{
				base.SerializeClassifier(oos);
				oos.WriteObject(nodeFeatureIndicesMap);
				oos.WriteObject(edgeFeatureIndicesMap);
				if (flags.secondOrderNonLinear)
				{
					oos.WriteObject(inputLayerWeights4Edge);
					oos.WriteObject(outputLayerWeights4Edge);
				}
				else
				{
					oos.WriteObject(linearWeights);
				}
				oos.WriteObject(inputLayerWeights);
				oos.WriteObject(outputLayerWeights);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(Stream ois, Properties props)
		{
			// can't have right types in deserialization
			base.LoadClassifier(ois, props);
			nodeFeatureIndicesMap = (IIndex<int>)ois.ReadObject();
			edgeFeatureIndicesMap = (IIndex<int>)ois.ReadObject();
			if (flags.secondOrderNonLinear)
			{
				inputLayerWeights4Edge = (double[][])ois.ReadObject();
				outputLayerWeights4Edge = (double[][])ois.ReadObject();
			}
			else
			{
				linearWeights = (double[][])ois.ReadObject();
			}
			inputLayerWeights = (double[][])ois.ReadObject();
			outputLayerWeights = (double[][])ois.ReadObject();
		}
	}
}
