using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>Mengqiu Wang</author>
	public class CRFLogConditionalObjectiveFunctionNoisyLabel : CRFLogConditionalObjectiveFunction
	{
		protected internal readonly double[][] errorMatrix;

		internal CRFLogConditionalObjectiveFunctionNoisyLabel(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string priorType, string backgroundSymbol, double sigma, double[]
			[][][] featureVal, int multiThreadGrad, double[][] errorMatrix)
			: base(data, labels, window, classIndex, labelIndices, map, priorType, backgroundSymbol, sigma, featureVal, multiThreadGrad, false)
		{
			// protected final double[][][] parallelEhat;
			this.errorMatrix = errorMatrix;
		}

		public virtual ICliquePotentialFunction GetFunc(int docIndex)
		{
			int[] docLabels = labels[docIndex];
			return new NoisyLabelLinearCliquePotentialFunction(weights, docLabels, errorMatrix);
		}

		public override void SetWeights(double[][] weights)
		{
			base.SetWeights(weights);
		}

		protected internal override double ExpectedAndEmpiricalCountsAndValueForADoc(double[][] E, double[][] Ehat, int docIndex)
		{
			int[][][] docData = data[docIndex];
			double[][][] featureVal3DArr = null;
			if (featureVal != null)
			{
				featureVal3DArr = featureVal[docIndex];
			}
			// make a clique tree for this document
			CRFCliqueTree<string> cliqueTreeNoisyLabel = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, GetFunc(docIndex), featureVal3DArr);
			CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunc, featureVal3DArr);
			double prob = cliqueTreeNoisyLabel.TotalMass() - cliqueTree.TotalMass();
			DocumentExpectedCounts(E, docData, featureVal3DArr, cliqueTree);
			DocumentExpectedCounts(Ehat, docData, featureVal3DArr, cliqueTreeNoisyLabel);
			return prob;
		}

		protected internal override double RegularGradientAndValue()
		{
			int totalLen = data.Length;
			IList<int> docIDs = new List<int>(totalLen);
			for (int m = 0; m < totalLen; m++)
			{
				docIDs.Add(m);
			}
			return MultiThreadGradient(docIDs, true);
		}

		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			Clear2D(Ehat);
			base.Calculate(x);
		}
	}
}
