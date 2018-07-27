using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Jenny Finkel</author>
	public class CRFLogConditionalObjectiveFloatFunction : AbstractCachingDiffFloatFunction, IHasCliquePotentialFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFLogConditionalObjectiveFloatFunction));

		public const int NoPrior = 0;

		public const int QuadraticPrior = 1;

		public const int HuberPrior = 2;

		public const int QuarticPrior = 3;

		protected internal int prior;

		protected internal float sigma;

		protected internal float epsilon;

		internal IList<IIndex<CRFLabel>> labelIndices;

		internal IIndex<string> classIndex;

		internal float[][] Ehat;

		internal int window;

		internal int numClasses;

		internal int[] map;

		internal int[][][][] data;

		internal int[][] labels;

		internal int domainDimension = -1;

		internal string backgroundSymbol;

		public static bool Verbose = false;

		internal CRFLogConditionalObjectiveFloatFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string backgroundSymbol)
			: this(data, labels, window, classIndex, labelIndices, map, QuadraticPrior, backgroundSymbol)
		{
		}

		internal CRFLogConditionalObjectiveFloatFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string backgroundSymbol, double sigma)
			: this(data, labels, window, classIndex, labelIndices, map, QuadraticPrior, backgroundSymbol, sigma)
		{
		}

		internal CRFLogConditionalObjectiveFloatFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, int prior, string backgroundSymbol)
			: this(data, labels, window, classIndex, labelIndices, map, prior, backgroundSymbol, 1.0f)
		{
		}

		internal CRFLogConditionalObjectiveFloatFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, int prior, string backgroundSymbol, double sigma)
		{
			/* Use a Huber robust regression penalty (L1 except very near 0) not L2 */
			// empirical counts of all the features [feature][class]
			this.window = window;
			this.classIndex = classIndex;
			this.numClasses = classIndex.Size();
			this.labelIndices = labelIndices;
			this.map = map;
			this.data = data;
			this.labels = labels;
			this.prior = prior;
			this.backgroundSymbol = backgroundSymbol;
			this.sigma = (float)sigma;
			EmpiricalCounts(data, labels);
		}

		public override int DomainDimension()
		{
			if (domainDimension < 0)
			{
				domainDimension = 0;
				foreach (int aMap in map)
				{
					domainDimension += labelIndices[aMap].Size();
				}
			}
			return domainDimension;
		}

		public virtual ICliquePotentialFunction GetCliquePotentialFunction(double[] x)
		{
			throw new NotSupportedException("CRFLogConditionalObjectiveFloatFunction is not clique potential compatible yet");
		}

		public virtual float[][] To2D(float[] weights)
		{
			float[][] newWeights = new float[map.Length][];
			int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				newWeights[i] = new float[labelIndices[map[i]].Size()];
				System.Array.Copy(weights, index, newWeights[i], 0, labelIndices[map[i]].Size());
				index += labelIndices[map[i]].Size();
			}
			return newWeights;
		}

		public virtual float[] To1D(float[][] weights)
		{
			float[] newWeights = new float[DomainDimension()];
			int index = 0;
			foreach (float[] weight in weights)
			{
				System.Array.Copy(weight, 0, newWeights, index, weight.Length);
				index += weight.Length;
			}
			return newWeights;
		}

		public virtual float[][] Empty2D()
		{
			float[][] d = new float[map.Length][];
			// int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				d[i] = new float[labelIndices[map[i]].Size()];
			}
			// Arrays.fill(d[i], 0);  // not needed; Java arrays zero initialized
			// index += labelIndices.get(map[i]).size();
			return d;
		}

		private void EmpiricalCounts(int[][][][] data, int[][] labels)
		{
			Ehat = Empty2D();
			for (int m = 0; m < data.Length; m++)
			{
				int[][][] dataDoc = data[m];
				int[] labelsDoc = labels[m];
				int[] label = new int[window];
				//Arrays.fill(label, classIndex.indexOf("O"));
				Arrays.Fill(label, classIndex.IndexOf(backgroundSymbol));
				for (int i = 0; i < dataDoc.Length; i++)
				{
					System.Array.Copy(label, 1, label, 0, window - 1);
					label[window - 1] = labelsDoc[i];
					for (int j = 0; j < dataDoc[i].Length; j++)
					{
						int[] cliqueLabel = new int[j + 1];
						System.Array.Copy(label, window - 1 - j, cliqueLabel, 0, j + 1);
						CRFLabel crfLabel = new CRFLabel(cliqueLabel);
						int labelIndex = labelIndices[j].IndexOf(crfLabel);
						//log.info(crfLabel + " " + labelIndex);
						for (int k = 0; k < dataDoc[i][j].Length; k++)
						{
							Ehat[dataDoc[i][j][k]][labelIndex]++;
						}
					}
				}
			}
		}

		public static FloatFactorTable GetFloatFactorTable(float[][] weights, int[][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses)
		{
			FloatFactorTable factorTable = null;
			for (int j = 0; j < labelIndices.Count; j++)
			{
				IIndex<CRFLabel> labelIndex = labelIndices[j];
				FloatFactorTable ft = new FloatFactorTable(numClasses, j + 1);
				// ...and each possible labeling for that clique
				for (int k = 0; k < labelIndex.Size(); k++)
				{
					int[] label = labelIndex.Get(k).GetLabel();
					float weight = 0.0f;
					for (int m = 0; m < data[j].Length; m++)
					{
						//log.info("**"+weights[data[j][m]][k]);
						weight += weights[data[j][m]][k];
					}
					ft.SetValue(label, weight);
				}
				//log.info(">>"+ft);
				//log.info("::"+ft);
				if (j > 0)
				{
					ft.MultiplyInEnd(factorTable);
				}
				//log.info("::"+ft);
				factorTable = ft;
			}
			return factorTable;
		}

		public static FloatFactorTable[] GetCalibratedCliqueTree(float[][] weights, int[][][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses)
		{
			//       for (int i = 0; i < weights.length; i++) {
			//         for (int j = 0; j < weights[i].length; j++) {
			//           log.info(i+" "+j+": "+weights[i][j]);
			//         }
			//       }
			//log.info("calibrating clique tree");
			FloatFactorTable[] factorTables = new FloatFactorTable[data.Length];
			FloatFactorTable[] messages = new FloatFactorTable[data.Length - 1];
			for (int i = 0; i < data.Length; i++)
			{
				factorTables[i] = GetFloatFactorTable(weights, data[i], labelIndices, numClasses);
				if (Verbose)
				{
					log.Info(i + ": " + factorTables[i]);
				}
				if (i > 0)
				{
					messages[i - 1] = factorTables[i - 1].SumOutFront();
					if (Verbose)
					{
						log.Info(messages[i - 1]);
					}
					factorTables[i].MultiplyInFront(messages[i - 1]);
					if (Verbose)
					{
						log.Info(factorTables[i]);
						if (i == data.Length - 1)
						{
							log.Info(i + ": " + factorTables[i].ToProbString());
						}
					}
				}
			}
			for (int i_1 = factorTables.Length - 2; i_1 >= 0; i_1--)
			{
				FloatFactorTable summedOut = factorTables[i_1 + 1].SumOutEnd();
				if (Verbose)
				{
					log.Info((i_1 + 1) + "-->" + i_1 + ": " + summedOut);
				}
				summedOut.DivideBy(messages[i_1]);
				if (Verbose)
				{
					log.Info((i_1 + 1) + "-->" + i_1 + ": " + summedOut);
				}
				factorTables[i_1].MultiplyInEnd(summedOut);
				if (Verbose)
				{
					log.Info(i_1 + ": " + factorTables[i_1]);
					log.Info(i_1 + ": " + factorTables[i_1].ToProbString());
				}
			}
			return factorTables;
		}

		protected internal override void Calculate(float[] x)
		{
			// if (crfType.equalsIgnoreCase("weird")) {
			//   calculateWeird(x);
			//   return;
			// }
			float[][] weights = To2D(x);
			float prob = 0;
			float[][] E = Empty2D();
			for (int m = 0; m < data.Length; m++)
			{
				FloatFactorTable[] factorTables = GetCalibratedCliqueTree(weights, data[m], labelIndices, numClasses);
				//             log.info("calibrated:");
				//             for (int i = 0; i < factorTables.length; i++) {
				//               System.out.println(factorTables[i]);
				//               System.out.println("+++++++++++++++++++++++++++++");
				//             }
				//             System.exit(0);
				float z = factorTables[0].TotalMass();
				int[] given = new int[window - 1];
				Arrays.Fill(given, classIndex.IndexOf(backgroundSymbol));
				for (int i = 0; i < data[m].Length; i++)
				{
					float p = factorTables[i].ConditionalLogProb(given, labels[m][i]);
					if (Verbose)
					{
						log.Info("P(" + labels[m][i] + "|" + Arrays.ToString(given) + ")=" + p);
					}
					prob += p;
					System.Array.Copy(given, 1, given, 0, given.Length - 1);
					given[given.Length - 1] = labels[m][i];
				}
				// get predicted count
				for (int i_1 = 0; i_1 < data[m].Length; i_1++)
				{
					// go through each clique...
					for (int j = 0; j < data[m][i_1].Length; j++)
					{
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						// ...and each possible labeling for that clique
						for (int k = 0; k < labelIndex.Size(); k++)
						{
							int[] label = labelIndex.Get(k).GetLabel();
							// float p = Math.pow(Math.E, factorTables[i].logProbEnd(label));
							float p = (float)Math.Exp(factorTables[i_1].UnnormalizedLogProbEnd(label) - z);
							for (int n = 0; n < data[m][i_1][j].Length; n++)
							{
								E[data[m][i_1][j][n]][k] += p;
							}
						}
					}
				}
			}
			if (float.IsNaN(prob))
			{
				System.Environment.Exit(0);
			}
			value = -prob;
			// compute the partial derivative for each feature
			int index = 0;
			for (int i_2 = 0; i_2 < E.Length; i_2++)
			{
				for (int j = 0; j < E[i_2].Length; j++)
				{
					derivative[index++] = (E[i_2][j] - Ehat[i_2][j]);
					if (Verbose)
					{
						log.Info("deriv(" + i_2 + "," + j + ") = " + E[i_2][j] + " - " + Ehat[i_2][j] + " = " + derivative[index - 1]);
					}
				}
			}
			// priors
			if (prior == QuadraticPrior)
			{
				float sigmaSq = sigma * sigma;
				for (int i = 0; i_2 < x.Length; i_2++)
				{
					float k = 1.0f;
					float w = x[i_2];
					value += k * w * w / 2.0 / sigmaSq;
					derivative[i_2] += k * w / sigmaSq;
				}
			}
			else
			{
				if (prior == HuberPrior)
				{
					float sigmaSq = sigma * sigma;
					for (int i = 0; i_2 < x.Length; i_2++)
					{
						float w = x[i_2];
						float wabs = Math.Abs(w);
						if (wabs < epsilon)
						{
							value += w * w / 2.0 / epsilon / sigmaSq;
							derivative[i_2] += w / epsilon / sigmaSq;
						}
						else
						{
							value += (wabs - epsilon / 2) / sigmaSq;
							derivative[i_2] += ((w < 0.0) ? -1.0 : 1.0) / sigmaSq;
						}
					}
				}
				else
				{
					if (prior == QuarticPrior)
					{
						float sigmaQu = sigma * sigma * sigma * sigma;
						for (int i = 0; i_2 < x.Length; i_2++)
						{
							float k = 1.0f;
							float w = x[i_2];
							value += k * w * w * w * w / 2.0 / sigmaQu;
							derivative[i_2] += k * w / sigmaQu;
						}
					}
				}
			}
		}

		public virtual void CalculateWeird1(float[] x)
		{
			float[][] weights = To2D(x);
			float[][] E = Empty2D();
			value = 0.0f;
			Arrays.Fill(derivative, 0.0f);
			float[][] sums = new float[labelIndices.Count][];
			float[][] probs = new float[labelIndices.Count][];
			float[][] counts = new float[labelIndices.Count][];
			for (int i = 0; i < sums.Length; i++)
			{
				int size = labelIndices[i].Size();
				sums[i] = new float[size];
				probs[i] = new float[size];
				counts[i] = new float[size];
			}
			// Arrays.fill(counts[i], 0.0f); // not needed; Java arrays zero initialized
			for (int d = 0; d < data.Length; d++)
			{
				int[] llabels = labels[d];
				for (int e = 0; e < data[d].Length; e++)
				{
					int[][] ddata = this.data[d][e];
					for (int cl = 0; cl < ddata.Length; cl++)
					{
						int[] features = ddata[cl];
						// activation
						Arrays.Fill(sums[cl], 0.0f);
						int numClasses = labelIndices[cl].Size();
						for (int c = 0; c < numClasses; c++)
						{
							foreach (int feature in features)
							{
								sums[cl][c] += weights[feature][c];
							}
						}
					}
					for (int cl_1 = 0; cl_1 < ddata.Length; cl_1++)
					{
						int[] label = new int[cl_1 + 1];
						//Arrays.fill(label, classIndex.indexOf("O"));
						Arrays.Fill(label, classIndex.IndexOf(backgroundSymbol));
						int index1 = label.Length - 1;
						for (int pos = e; pos >= 0 && index1 >= 0; pos--)
						{
							//log.info(index1+" "+pos);
							label[index1--] = llabels[pos];
						}
						CRFLabel crfLabel = new CRFLabel(label);
						int labelIndex = labelIndices[cl_1].IndexOf(crfLabel);
						float total = ArrayMath.LogSum(sums[cl_1]);
						// 		    int[] features = ddata[cl];
						int numClasses = labelIndices[cl_1].Size();
						for (int c = 0; c < numClasses; c++)
						{
							probs[cl_1][c] = (float)System.Math.Exp(sums[cl_1][c] - total);
						}
						// 		    for (int f=0; f<features.length; f++) {
						// 			for (int c=0; c<numClasses; c++) {
						// 			    //probs[cl][c] = Math.exp(sums[cl][c]-total);
						// 			    derivative[index] += probs[cl][c];
						// 			    if (c == labelIndex) {
						// 				derivative[index]--;
						// 			    }
						// 			    index++;
						// 			}
						// 		    }
						value -= sums[cl_1][labelIndex] - total;
					}
					// 		    // observed
					// 		    for (int f=0; f<features.length; f++) {
					// 		        //int i = indexOf(features[f], labels[d]);
					// 		        derivative[index+labelIndex] -= 1.0;
					// 		    }
					// go through each clique...
					for (int j = 0; j < data[d][e].Length; j++)
					{
						IIndex<CRFLabel> labelIndex = labelIndices[j];
						// ...and each possible labeling for that clique
						for (int k = 0; k < labelIndex.Size(); k++)
						{
							//int[] label = ((CRFLabel) labelIndex.get(k)).getLabel();
							// float p = Math.pow(Math.E, factorTables[i].logProbEnd(label));
							float p = probs[j][k];
							for (int n = 0; n < data[d][e][j].Length; n++)
							{
								E[data[d][e][j][n]][k] += p;
							}
						}
					}
				}
			}
			// compute the partial derivative for each feature
			int index = 0;
			for (int i_1 = 0; i_1 < E.Length; i_1++)
			{
				for (int j = 0; j < E[i_1].Length; j++)
				{
					derivative[index++] = (E[i_1][j] - Ehat[i_1][j]);
				}
			}
			// observed
			// 	int index = 0;
			// 	for (int i = 0; i < Ehat.length; i++) {
			// 	    for (int j = 0; j < Ehat[i].length; j++) {
			// 		derivative[index++] -= Ehat[i][j];
			// 	    }
			// 	}
			// priors
			if (prior == QuadraticPrior)
			{
				float sigmaSq = sigma * sigma;
				for (int i_2 = 0; i_2 < x.Length; i_2++)
				{
					float k = 1.0f;
					float w = x[i_2];
					value += k * w * w / 2.0 / sigmaSq;
					derivative[i_2] += k * w / sigmaSq;
				}
			}
			else
			{
				if (prior == HuberPrior)
				{
					float sigmaSq = sigma * sigma;
					for (int i_2 = 0; i_2 < x.Length; i_2++)
					{
						float w = x[i_2];
						float wabs = System.Math.Abs(w);
						if (wabs < epsilon)
						{
							value += w * w / 2.0 / epsilon / sigmaSq;
							derivative[i_2] += w / epsilon / sigmaSq;
						}
						else
						{
							value += (wabs - epsilon / 2) / sigmaSq;
							derivative[i_2] += ((w < 0.0) ? -1.0 : 1.0) / sigmaSq;
						}
					}
				}
				else
				{
					if (prior == QuarticPrior)
					{
						float sigmaQu = sigma * sigma * sigma * sigma;
						for (int i_2 = 0; i_2 < x.Length; i_2++)
						{
							float k = 1.0f;
							float w = x[i_2];
							value += k * w * w * w * w / 2.0 / sigmaQu;
							derivative[i_2] += k * w / sigmaQu;
						}
					}
				}
			}
		}
		/*
		// TODO(mengqiu) verify this is useless and remove
		public void calculateWeird(float[] x) {
		
		float[][] weights = to2D(x);
		float[][] E = empty2D();
		
		value = 0.0f;
		Arrays.fill(derivative, 0.0f);
		
		int size = labelIndices.get(labelIndices.size() - 1).size();
		
		float[] sums = new float[size];
		float[] probs = new float[size];
		
		Index labelIndex = labelIndices.get(labelIndices.size() - 1);
		
		for (int d = 0; d < data.length; d++) {
		int[] llabels = labels[d];
		
		int[] label = new int[window];
		//Arrays.fill(label, classIndex.indexOf("O"));
		Arrays.fill(label, classIndex.indexOf(backgroundSymbol));
		
		for (int e = 0; e < data[d].length; e++) {
		
		Arrays.fill(sums, 0.0f);
		
		System.arraycopy(label, 1, label, 0, window - 1);
		label[window - 1] = llabels[e];
		CRFLabel crfLabel = new CRFLabel(label);
		int maxCliqueLabelIndex = labelIndex.indexOf(crfLabel);
		
		int[][] ddata = this.data[d][e];
		
		//Iterator labelIter = labelIndices.get(labelIndices.size()-1).iterator();
		//while (labelIter.hasNext()) {
		
		for (int i = 0; i < labelIndex.size(); i++) {
		CRFLabel c = (CRFLabel) labelIndex.get(i);
		
		for (int cl = 0; cl < ddata.length; cl++) {
		
		CRFLabel cliqueLabel = c.getSmallerLabel(cl + 1);
		int clIndex = labelIndices.get(cl).indexOf(cliqueLabel);
		
		int[] features = ddata[cl];
		for (int f = 0; f < features.length; f++) {
		sums[i] += weights[features[f]][clIndex];
		}
		}
		}
		
		float total = ArrayMath.logSum(sums);
		for (int i = 0; i < probs.length; i++) {
		probs[i] = (float) Math.exp(sums[i] - total);
		}
		value -= sums[maxCliqueLabelIndex] - total;
		
		for (int i = 0; i < labelIndex.size(); i++) {
		CRFLabel c = (CRFLabel) labelIndex.get(i);
		
		for (int cl = 0; cl < ddata.length; cl++) {
		
		CRFLabel cliqueLabel = c.getSmallerLabel(cl + 1);
		int clIndex = labelIndices.get(cl).indexOf(cliqueLabel);
		int[] features = ddata[cl];
		
		for (int f = 0; f < features.length; f++) {
		E[features[f]][clIndex] += probs[i];
		if (i == maxCliqueLabelIndex) {
		E[features[f]][clIndex] -= 1.0f;
		}
		//sums[i] += weights[features[f]][cl];
		}
		}
		}
		}
		}
		
		
		// compute the partial derivative for each feature
		int index = 0;
		for (int i = 0; i < E.length; i++) {
		for (int j = 0; j < E[i].length; j++) {
		//derivative[index++] = (E[i][j] - Ehat[i][j]);
		derivative[index++] = E[i][j];
		}
		}
		
		// priors
		if (prior == QUADRATIC_PRIOR) {
		float sigmaSq = sigma * sigma;
		for (int i = 0; i < x.length; i++) {
		float k = 1.0f;
		float w = x[i];
		value += k * w * w / 2.0 / sigmaSq;
		derivative[i] += k * w / sigmaSq;
		}
		} else if (prior == HUBER_PRIOR) {
		float sigmaSq = sigma * sigma;
		for (int i = 0; i < x.length; i++) {
		float w = x[i];
		float wabs = Math.abs(w);
		if (wabs < epsilon) {
		value += w * w / 2.0 / epsilon / sigmaSq;
		derivative[i] += w / epsilon / sigmaSq;
		} else {
		value += (wabs - epsilon / 2) / sigmaSq;
		derivative[i] += ((w < 0.0f ? -1.0f : 1.0f) / sigmaSq);
		}
		}
		} else if (prior == QUARTIC_PRIOR) {
		float sigmaQu = sigma * sigma * sigma * sigma;
		for (int i = 0; i < x.length; i++) {
		float k = 1.0f;
		float w = x[i];
		value += k * w * w * w * w / 2.0 / sigmaQu;
		derivative[i] += k * w / sigmaQu;
		}
		}
		}
		*/
	}
}
