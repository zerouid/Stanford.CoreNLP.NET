using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <author>
	/// Jenny Finkel
	/// Mengqiu Wang
	/// </author>
	public class CRFLogConditionalObjectiveFunction : AbstractStochasticCachingDiffUpdateFunction, IHasCliquePotentialFunction, IHasFeatureGrouping
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFLogConditionalObjectiveFunction));

		public const int NoPrior = 0;

		public const int QuadraticPrior = 1;

		public const int HuberPrior = 2;

		public const int QuarticPrior = 3;

		public const int DropoutPrior = 4;

		public const bool Debug2 = false;

		public const bool Debug3 = false;

		public const bool Timed = false;

		public const bool Condense = true;

		public static bool Verbose = false;

		protected internal readonly int prior;

		protected internal readonly double sigma;

		protected internal readonly double epsilon = 0.1;

		/// <summary>label indices - for all possible label sequences - for each feature</summary>
		protected internal readonly IList<IIndex<CRFLabel>> labelIndices;

		protected internal readonly IIndex<string> classIndex;

		protected internal readonly double[][] Ehat;

		protected internal readonly double[][] E;

		protected internal double[][][] parallelE;

		protected internal double[][][] parallelEhat;

		protected internal readonly int window;

		protected internal readonly int numClasses;

		protected internal readonly int[] map;

		protected internal int[][][][] data;

		protected internal double[][][][] featureVal;

		protected internal int[][] labels;

		protected internal readonly int domainDimension;

		protected internal int[][] weightIndices;

		protected internal readonly string backgroundSymbol;

		protected internal int[][] featureGrouping = null;

		protected internal const double smallConst = 1e-6;

		protected internal Random rand = new Random(2147483647L);

		protected internal readonly int multiThreadGrad;

		protected internal double[][] weights;

		protected internal ICliquePotentialFunction cliquePotentialFunc;

		/* Use a Huber robust regression penalty (L1 except very near 0) not L2 */
		// public static final boolean DEBUG2 = true;
		// public static final boolean TIMED = true;
		// public static final boolean CONDENSE = false;
		// You can't actually set this at present
		// didn't have <String> before. Added since that's what is assumed everywhere.
		// empirical counts of all the features [feature][class]
		// public static Index<String> featureIndex;  // no idea why this was here [cdm 2013]
		// data[docIndex][tokenIndex][][]
		// featureVal[docIndex][tokenIndex][][]
		// labels[docIndex][tokenIndex]
		// protected double[][] eHat4Update, e4Update;
		// protected static final double largeConst = 5;
		// need to ensure the following two objects are only read during multi-threading
		// to ensure thread-safety. It should only be modified in calculate() via setWeights()
		public override double[] Initial()
		{
			return Initial(rand);
		}

		public virtual double[] Initial(bool useRandomSeed)
		{
			Random randToUse = useRandomSeed ? new Random() : rand;
			return Initial(randToUse);
		}

		public virtual double[] Initial(Random randGen)
		{
			double[] initial = new double[DomainDimension()];
			for (int i = 0; i < initial.Length; i++)
			{
				initial[i] = randGen.NextDouble() + smallConst;
			}
			// initial[i] = generator.nextDouble() * largeConst;
			// initial[i] = -1+2*(i);
			// initial[i] = (i == 0 ? 1 : 0);
			return initial;
		}

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
						if (Sharpen.Runtime.EqualsIgnoreCase("DROPOUT", priorTypeStr))
						{
							return DropoutPrior;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase("NONE", priorTypeStr))
							{
								return NoPrior;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "lasso") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "ridge") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "gaussian") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "ae-lasso"
									) || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "sg-lasso") || Sharpen.Runtime.EqualsIgnoreCase(priorTypeStr, "g-lasso"))
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
		}

		internal CRFLogConditionalObjectiveFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string priorType, string backgroundSymbol, double sigma, double[][][][] featureVal
			, int multiThreadGrad)
			: this(data, labels, window, classIndex, labelIndices, map, priorType, backgroundSymbol, sigma, featureVal, multiThreadGrad, true)
		{
			expectedThreadProcessor = new CRFLogConditionalObjectiveFunction.ExpectationThreadsafeProcessor(this);
			expectedAndEmpiricalThreadProcessor = new CRFLogConditionalObjectiveFunction.ExpectationThreadsafeProcessor(this, true);
		}

		internal CRFLogConditionalObjectiveFunction(int[][][][] data, int[][] labels, int window, IIndex<string> classIndex, IList<IIndex<CRFLabel>> labelIndices, int[] map, string priorType, string backgroundSymbol, double sigma, double[][][][] featureVal
			, int multiThreadGrad, bool calcEmpirical)
		{
			expectedThreadProcessor = new CRFLogConditionalObjectiveFunction.ExpectationThreadsafeProcessor(this);
			expectedAndEmpiricalThreadProcessor = new CRFLogConditionalObjectiveFunction.ExpectationThreadsafeProcessor(this, true);
			this.window = window;
			this.classIndex = classIndex;
			this.numClasses = classIndex.Size();
			this.labelIndices = labelIndices;
			this.map = map;
			this.data = data;
			this.featureVal = featureVal;
			this.labels = labels;
			this.prior = GetPriorType(priorType);
			this.backgroundSymbol = backgroundSymbol;
			this.sigma = sigma;
			this.multiThreadGrad = multiThreadGrad;
			// takes docIndex, returns Triple<prob, E, dropoutGrad>
			Ehat = Empty2D();
			E = Empty2D();
			weights = Empty2D();
			if (calcEmpirical)
			{
				EmpiricalCounts(Ehat);
			}
			int myDomainDimension = 0;
			foreach (int dim in map)
			{
				myDomainDimension += labelIndices[dim].Size();
			}
			domainDimension = myDomainDimension;
		}

		protected internal virtual void EmpiricalCounts(double[][] eHat)
		{
			for (int m = 0; m < data.Length; m++)
			{
				EmpiricalCountsForADoc(eHat, m);
			}
		}

		protected internal virtual void EmpiricalCountsForADoc(double[][] eHat, int docIndex)
		{
			int[][][] docData = data[docIndex];
			int[] docLabels = labels[docIndex];
			int[] windowLabels = new int[window];
			Arrays.Fill(windowLabels, classIndex.IndexOf(backgroundSymbol));
			double[][][] featureValArr = null;
			if (featureVal != null)
			{
				featureValArr = featureVal[docIndex];
			}
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
				for (int j = 0; j < docData[i].Length; j++)
				{
					int[] cliqueLabel = new int[j + 1];
					System.Array.Copy(windowLabels, window - 1 - j, cliqueLabel, 0, j + 1);
					CRFLabel crfLabel = new CRFLabel(cliqueLabel);
					int labelIndex = labelIndices[j].IndexOf(crfLabel);
					//log.info(crfLabel + " " + labelIndex);
					for (int n = 0; n < docData[i][j].Length; n++)
					{
						double fVal = 1.0;
						if (featureValArr != null && j == 0)
						{
							// j == 0 because only node features gets feature values
							fVal = featureValArr[i][j][n];
						}
						eHat[docData[i][j][n]][labelIndex] += fVal;
					}
				}
			}
		}

		public virtual ICliquePotentialFunction GetCliquePotentialFunction(double[] x)
		{
			To2D(x, weights);
			return new LinearCliquePotentialFunction(weights);
		}

		protected internal virtual double ExpectedAndEmpiricalCountsAndValueForADoc(double[][] E, double[][] Ehat, int docIndex)
		{
			EmpiricalCountsForADoc(Ehat, docIndex);
			return ExpectedCountsAndValueForADoc(E, docIndex);
		}

		public virtual double ValueForADoc(int docIndex)
		{
			return ExpectedCountsAndValueForADoc(null, docIndex, false, true);
		}

		protected internal virtual double ExpectedCountsAndValueForADoc(double[][] E, int docIndex)
		{
			return ExpectedCountsAndValueForADoc(E, docIndex, true, true);
		}

		protected internal virtual double ExpectedCountsForADoc(double[][] E, int docIndex)
		{
			return ExpectedCountsAndValueForADoc(E, docIndex, true, false);
		}

		protected internal virtual double ExpectedCountsAndValueForADoc(double[][] E, int docIndex, bool doExpectedCountCalc, bool doValueCalc)
		{
			int[][][] docData = data[docIndex];
			double[][][] featureVal3DArr = null;
			if (featureVal != null)
			{
				featureVal3DArr = featureVal[docIndex];
			}
			// make a clique tree for this document
			CRFCliqueTree<string> cliqueTree = CRFCliqueTree.GetCalibratedCliqueTree(docData, labelIndices, numClasses, classIndex, backgroundSymbol, cliquePotentialFunc, featureVal3DArr);
			double prob = 0.0;
			if (doValueCalc)
			{
				prob = DocumentLogProbability(docData, docIndex, cliqueTree);
			}
			if (doExpectedCountCalc)
			{
				DocumentExpectedCounts(E, docData, featureVal3DArr, cliqueTree);
			}
			return prob;
		}

		/// <summary>Compute the expected counts for this document, which we will need to compute the derivative.</summary>
		protected internal virtual void DocumentExpectedCounts(double[][] E, int[][][] docData, double[][][] featureVal3DArr, CRFCliqueTree<string> cliqueTree)
		{
			// iterate over the positions in this document
			for (int i = 0; i < docData.Length; i++)
			{
				// for each possible clique at this position
				for (int j = 0; j < docData[i].Length; j++)
				{
					IIndex<CRFLabel> labelIndex = labelIndices[j];
					// for each possible labeling for that clique
					for (int k = 0; k < liSize; k++)
					{
						int[] label = labelIndex.Get(k).GetLabel();
						double p = cliqueTree.Prob(i, label);
						// probability of these labels occurring in this clique with these features
						for (int n = 0; n < docData[i][j].Length; n++)
						{
							double fVal = 1.0;
							if (j == 0 && featureVal3DArr != null)
							{
								// j == 0 because only node features gets feature values
								fVal = featureVal3DArr[i][j][n];
							}
							E[docData[i][j][n]][k] += p * fVal;
						}
					}
				}
			}
		}

		/// <summary>Compute the log probability of the document given the model with the parameters x.</summary>
		private double DocumentLogProbability(int[][][] docData, int docIndex, CRFCliqueTree<string> cliqueTree)
		{
			int[] docLabels = labels[docIndex];
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
				System.Console.Error.Printf("P_-1(Background) = % 5.3f%n", startPosLogProb);
			}
			double prob = startPosLogProb;
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
			return prob;
		}

		private IThreadsafeProcessor<Pair<int, IList<int>>, Pair<int, double>> expectedThreadProcessor;

		private IThreadsafeProcessor<Pair<int, IList<int>>, Pair<int, double>> expectedAndEmpiricalThreadProcessor;

		internal class ExpectationThreadsafeProcessor : IThreadsafeProcessor<Pair<int, IList<int>>, Pair<int, double>>
		{
			internal bool calculateEmpirical = false;

			public ExpectationThreadsafeProcessor(CRFLogConditionalObjectiveFunction _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public ExpectationThreadsafeProcessor(CRFLogConditionalObjectiveFunction _enclosing, bool calculateEmpirical)
			{
				this._enclosing = _enclosing;
				this.calculateEmpirical = calculateEmpirical;
			}

			public virtual Pair<int, double> Process(Pair<int, IList<int>> threadIDAndDocIndices)
			{
				int tID = threadIDAndDocIndices.First();
				if (tID < 0 || tID >= this._enclosing.multiThreadGrad)
				{
					throw new ArgumentException("threadID must be with in range 0 <= tID < multiThreadGrad(=" + this._enclosing.multiThreadGrad + ")");
				}
				IList<int> docIDs = threadIDAndDocIndices.Second();
				double[][] partE;
				// initialized below
				double[][] partEhat = null;
				// initialized below
				if (this._enclosing.multiThreadGrad == 1)
				{
					partE = this._enclosing.E;
					if (this.calculateEmpirical)
					{
						partEhat = this._enclosing.Ehat;
					}
				}
				else
				{
					partE = this._enclosing.parallelE[tID];
					// TODO: if we put this on the heap, this clearing will be unnecessary
					CRFLogConditionalObjectiveFunction.Clear2D(partE);
					if (this.calculateEmpirical)
					{
						partEhat = this._enclosing.parallelEhat[tID];
						CRFLogConditionalObjectiveFunction.Clear2D(partEhat);
					}
				}
				double probSum = 0;
				foreach (int docIndex in docIDs)
				{
					if (this.calculateEmpirical)
					{
						probSum += this._enclosing.ExpectedAndEmpiricalCountsAndValueForADoc(partE, partEhat, docIndex);
					}
					else
					{
						probSum += this._enclosing.ExpectedCountsAndValueForADoc(partE, docIndex);
					}
				}
				return new Pair<int, double>(tID, probSum);
			}

			public virtual IThreadsafeProcessor<Pair<int, IList<int>>, Pair<int, double>> NewInstance()
			{
				return this;
			}

			private readonly CRFLogConditionalObjectiveFunction _enclosing;
		}

		public virtual void SetWeights(double[][] weights)
		{
			this.weights = weights;
			cliquePotentialFunc = new LinearCliquePotentialFunction(weights);
		}

		protected internal virtual double RegularGradientAndValue()
		{
			int totalLen = data.Length;
			IList<int> docIDs = new List<int>(totalLen);
			for (int m = 0; m < totalLen; m++)
			{
				docIDs.Add(m);
			}
			return MultiThreadGradient(docIDs, false);
		}

		protected internal virtual double MultiThreadGradient(IList<int> docIDs, bool calculateEmpirical)
		{
			double objective = 0.0;
			// TODO: This is a bunch of unnecessary heap traffic, should all be on the stack
			if (multiThreadGrad > 1)
			{
				if (parallelE == null)
				{
					parallelE = new double[multiThreadGrad][][];
					for (int i = 0; i < multiThreadGrad; i++)
					{
						parallelE[i] = Empty2D();
					}
				}
				if (calculateEmpirical)
				{
					if (parallelEhat == null)
					{
						parallelEhat = new double[multiThreadGrad][][];
						for (int i = 0; i < multiThreadGrad; i++)
						{
							parallelEhat[i] = Empty2D();
						}
					}
				}
			}
			// TODO: this is a huge amount of machinery for no discernible reason
			MulticoreWrapper<Pair<int, IList<int>>, Pair<int, double>> wrapper = new MulticoreWrapper<Pair<int, IList<int>>, Pair<int, double>>(multiThreadGrad, (calculateEmpirical ? expectedAndEmpiricalThreadProcessor : expectedThreadProcessor));
			int totalLen = docIDs.Count;
			int partLen = totalLen / multiThreadGrad;
			int currIndex = 0;
			for (int part = 0; part < multiThreadGrad; part++)
			{
				int endIndex = currIndex + partLen;
				if (part == multiThreadGrad - 1)
				{
					endIndex = totalLen;
				}
				// TODO: let's not construct a sub-list of DocIDs, unnecessary object creation, can calculate directly from ThreadID
				IList<int> subList = docIDs.SubList(currIndex, endIndex);
				wrapper.Put(new Pair<int, IList<int>>(part, subList));
				currIndex = endIndex;
			}
			wrapper.Join();
			// This all seems fine. May want to start running this after the joins, in case we have different end-times
			while (wrapper.Peek())
			{
				Pair<int, double> result = wrapper.Poll();
				int tID = result.First();
				objective += result.Second();
				if (multiThreadGrad > 1)
				{
					Combine2DArr(E, parallelE[tID]);
					if (calculateEmpirical)
					{
						Combine2DArr(Ehat, parallelEhat[tID]);
					}
				}
			}
			return objective;
		}

		/// <summary>Calculates both value and partial derivatives at the point x, and save them internally.</summary>
		protected internal override void Calculate(double[] x)
		{
			// final double[][] weights = to2D(x);
			To2D(x, weights);
			SetWeights(weights);
			// the expectations over counts
			// first index is feature index, second index is of possible labeling
			// double[][] E = empty2D();
			Clear2D(E);
			double prob = RegularGradientAndValue();
			// the log prob of the sequence given the model, which is the negation of value at this point
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunction.calculate()" + " - this may well indicate numeric underflow due to overly long documents.");
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
					if (Verbose)
					{
						log.Info("deriv(" + i + "," + j + ") = " + E[i][j] + " - " + Ehat[i][j] + " = " + derivative[index]);
					}
					index++;
				}
			}
			ApplyPrior(x, 1.0);
		}

		// log.info("\nfuncVal: " + value);
		public override int DataDimension()
		{
			return data.Length;
		}

		public override void CalculateStochastic(double[] x, double[] v, int[] batch)
		{
			To2D(x, weights);
			SetWeights(weights);
			double batchScale = ((double)batch.Length) / ((double)this.DataDimension());
			// the expectations over counts
			// first index is feature index, second index is of possible labeling
			// double[][] E = empty2D();
			// iterate over all the documents
			IList<int> docIDs = new List<int>(batch.Length);
			foreach (int item in batch)
			{
				docIDs.Add(item);
			}
			double prob = MultiThreadGradient(docIDs, false);
			// the log prob of the sequence given the model, which is the negation of value at this point
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunction.calculate()");
			}
			value = -prob;
			// compute the partial derivative for each feature by comparing expected counts to empirical counts
			int index = 0;
			for (int i = 0; i < E.Length; i++)
			{
				for (int j = 0; j < E[i].Length; j++)
				{
					// real gradient should be empirical-expected;
					// but since we minimize -L(\theta), the gradient is -(empirical-expected)
					derivative[index++] = (E[i][j] - batchScale * Ehat[i][j]);
					if (Verbose)
					{
						log.Info("deriv(" + i + "," + j + ") = " + E[i][j] + " - " + Ehat[i][j] + " = " + derivative[index - 1]);
					}
				}
			}
			ApplyPrior(x, batchScale);
		}

		// re-initialization is faster than Arrays.fill(arr, 0)
		// private void clearUpdateEs() {
		//   for (int i = 0; i < eHat4Update.length; i++)
		//     eHat4Update[i] = new double[eHat4Update[i].length];
		//   for (int i = 0; i < e4Update.length; i++)
		//     e4Update[i] = new double[e4Update[i].length];
		// }
		/// <summary>
		/// Performs stochastic update of weights x (scaled by xScale) based
		/// on samples indexed by batch.
		/// </summary>
		/// <remarks>
		/// Performs stochastic update of weights x (scaled by xScale) based
		/// on samples indexed by batch.
		/// NOTE: This function does not do regularization (regularization is done by the minimizer).
		/// </remarks>
		/// <param name="x">- unscaled weights</param>
		/// <param name="xScale">- how much to scale x by when performing calculations</param>
		/// <param name="batch">- indices of which samples to compute function over</param>
		/// <param name="gScale">- how much to scale adjustments to x</param>
		/// <returns>value of function at specified x (scaled by xScale) for samples</returns>
		public override double CalculateStochasticUpdate(double[] x, double xScale, int[] batch, double gScale)
		{
			// int[][] wis = getWeightIndices();
			To2D(x, xScale, weights);
			SetWeights(weights);
			// if (eHat4Update == null) {
			//   eHat4Update = empty2D();
			//   e4Update = new double[eHat4Update.length][];
			//   for (int i = 0; i < e4Update.length; i++)
			//     e4Update[i] = new double[eHat4Update[i].length];
			// } else {
			//   clearUpdateEs();
			// }
			// Adjust weight by -gScale*gradient
			// gradient is expected count - empirical count
			// so we adjust by + gScale(empirical count - expected count)
			// iterate over all the documents
			IList<int> docIDs = new List<int>(batch.Length);
			foreach (int item in batch)
			{
				docIDs.Add(item);
			}
			double prob = MultiThreadGradient(docIDs, true);
			// the log prob of the sequence given the model, which is the negation of value at this point
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunction.calculate()");
			}
			value = -prob;
			int index = 0;
			for (int i = 0; i < E.Length; i++)
			{
				for (int j = 0; j < E[i].Length; j++)
				{
					x[index++] += (Ehat[i][j] - E[i][j]) * gScale;
				}
			}
			return value;
		}

		/// <summary>
		/// Performs stochastic gradient update based
		/// on samples indexed by batch, but does not apply regularization.
		/// </summary>
		/// <param name="x">- unscaled weights</param>
		/// <param name="batch">- indices of which samples to compute function over</param>
		public override void CalculateStochasticGradient(double[] x, int[] batch)
		{
			if (derivative == null)
			{
				derivative = new double[DomainDimension()];
			}
			// int[][] wis = getWeightIndices();
			// was: double[][] weights = to2D(x, 1.0); // but 1.0 should be the same as omitting 2nd parameter....
			To2D(x, weights);
			SetWeights(weights);
			// iterate over all the documents
			IList<int> docIDs = new List<int>(batch.Length);
			foreach (int item in batch)
			{
				docIDs.Add(item);
			}
			MultiThreadGradient(docIDs, true);
			int index = 0;
			for (int i = 0; i < E.Length; i++)
			{
				for (int j = 0; j < E[i].Length; j++)
				{
					// real gradient should be empirical-expected;
					// but since we minimize -L(\theta), the gradient is -(empirical-expected)
					derivative[index++] = (E[i][j] - Ehat[i][j]);
				}
			}
		}

		/// <summary>
		/// Computes value of function for specified value of x (scaled by xScale)
		/// only over samples indexed by batch.
		/// </summary>
		/// <remarks>
		/// Computes value of function for specified value of x (scaled by xScale)
		/// only over samples indexed by batch.
		/// NOTE: This function does not do regularization (regularization is done by the minimizer).
		/// </remarks>
		/// <param name="x">- unscaled weights</param>
		/// <param name="xScale">- how much to scale x by when performing calculations</param>
		/// <param name="batch">- indices of which samples to compute function over</param>
		/// <returns>value of function at specified x (scaled by xScale) for samples</returns>
		public override double ValueAt(double[] x, double xScale, int[] batch)
		{
			double prob = 0.0;
			// the log prob of the sequence given the model, which is the negation of value at this point
			// int[][] wis = getWeightIndices();
			To2D(x, xScale, weights);
			SetWeights(weights);
			// iterate over all the documents
			foreach (int ind in batch)
			{
				prob += ValueForADoc(ind);
			}
			if (double.IsNaN(prob))
			{
				// shouldn't be the case
				throw new Exception("Got NaN for prob in CRFLogConditionalObjectiveFunction.calculate()");
			}
			value = -prob;
			return value;
		}

		public virtual int[][] GetFeatureGrouping()
		{
			if (featureGrouping != null)
			{
				return featureGrouping;
			}
			else
			{
				int[][] fg = new int[1][];
				fg[0] = ArrayMath.Range(0, DomainDimension());
				return fg;
			}
		}

		public virtual void SetFeatureGrouping(int[][] fg)
		{
			this.featureGrouping = fg;
		}

		protected internal virtual void ApplyPrior(double[] x, double batchScale)
		{
			// incorporate priors
			if (prior == QuadraticPrior)
			{
				double lambda = 1 / (sigma * sigma);
				for (int i = 0; i < x.Length; i++)
				{
					double w = x[i];
					value += batchScale * w * w * lambda * 0.5;
					derivative[i] += batchScale * w * lambda;
				}
			}
			else
			{
				if (prior == HuberPrior)
				{
					double sigmaSq = sigma * sigma;
					for (int i = 0; i < x.Length; i++)
					{
						double w = x[i];
						double wabs = System.Math.Abs(w);
						if (wabs < epsilon)
						{
							value += batchScale * w * w / 2.0 / epsilon / sigmaSq;
							derivative[i] += batchScale * w / epsilon / sigmaSq;
						}
						else
						{
							value += batchScale * (wabs - epsilon / 2) / sigmaSq;
							derivative[i] += batchScale * ((w < 0.0) ? -1.0 : 1.0) / sigmaSq;
						}
					}
				}
				else
				{
					if (prior == QuarticPrior)
					{
						double sigmaQu = sigma * sigma * sigma * sigma;
						double lambda = 1 / 2.0 / sigmaQu;
						for (int i = 0; i < x.Length; i++)
						{
							double w = x[i];
							value += batchScale * w * w * w * w * lambda;
							derivative[i] += batchScale * w / sigmaQu;
						}
					}
				}
			}
		}

		protected internal virtual Pair<double[][][], double[][][]> GetCondProbs(CRFCliqueTree<string> cTree, int[][][] docData)
		{
			// first index position is curr index, second index curr-class, third index prev-class
			// e.g. [1][2][3] means curr is at position 1 with class 2, prev is at position 0 with class 3
			double[][][] prevGivenCurr = new double[docData.Length][][];
			// first index position is curr index, second index curr-class, third index next-class
			// e.g. [0][2][3] means curr is at position 0 with class 2, next is at position 1 with class 3
			double[][][] nextGivenCurr = new double[docData.Length][][];
			for (int i = 0; i < docData.Length; i++)
			{
				prevGivenCurr[i] = new double[numClasses][];
				nextGivenCurr[i] = new double[numClasses][];
				for (int j = 0; j < numClasses; j++)
				{
					prevGivenCurr[i][j] = new double[numClasses];
					nextGivenCurr[i][j] = new double[numClasses];
				}
			}
			// computing prevGivenCurr and nextGivenCurr
			for (int i_1 = 0; i_1 < docData.Length; i_1++)
			{
				int[] labelPair = new int[2];
				for (int l1 = 0; l1 < numClasses; l1++)
				{
					labelPair[0] = l1;
					for (int l2 = 0; l2 < numClasses; l2++)
					{
						labelPair[1] = l2;
						double prob = cTree.LogProb(i_1, labelPair);
						// log.info(prob);
						if (i_1 - 1 >= 0)
						{
							nextGivenCurr[i_1 - 1][l1][l2] = prob;
						}
						prevGivenCurr[i_1][l2][l1] = prob;
					}
				}
				for (int j = 0; j < numClasses; j++)
				{
					if (i_1 - 1 >= 0)
					{
						// ArrayMath.normalize(nextGivenCurr[i-1][j]);
						ArrayMath.LogNormalize(nextGivenCurr[i_1 - 1][j]);
						for (int k = 0; k < nextGivenCurr[i_1 - 1][j].Length; k++)
						{
							nextGivenCurr[i_1 - 1][j][k] = System.Math.Exp(nextGivenCurr[i_1 - 1][j][k]);
						}
					}
					// ArrayMath.normalize(prevGivenCurr[i][j]);
					ArrayMath.LogNormalize(prevGivenCurr[i_1][j]);
					for (int k_1 = 0; k_1 < prevGivenCurr[i_1][j].Length; k_1++)
					{
						prevGivenCurr[i_1][j][k_1] = System.Math.Exp(prevGivenCurr[i_1][j][k_1]);
					}
				}
			}
			return new Pair<double[][][], double[][][]>(prevGivenCurr, nextGivenCurr);
		}

		protected internal static void Combine2DArr(double[][] combineInto, double[][] toBeCombined, double scale)
		{
			for (int i = 0; i < toBeCombined.Length; i++)
			{
				for (int j = 0; j < toBeCombined[i].Length; j++)
				{
					combineInto[i][j] += toBeCombined[i][j] * scale;
				}
			}
		}

		protected internal static void Combine2DArr(double[][] combineInto, double[][] toBeCombined)
		{
			for (int i = 0; i < toBeCombined.Length; i++)
			{
				for (int j = 0; j < toBeCombined[i].Length; j++)
				{
					combineInto[i][j] += toBeCombined[i][j];
				}
			}
		}

		// TODO(mengqiu) add dimension checks
		protected internal static void Combine2DArr(double[][] combineInto, IDictionary<int, double[]> toBeCombined)
		{
			foreach (KeyValuePair<int, double[]> entry in toBeCombined)
			{
				int key = entry.Key;
				double[] source = entry.Value;
				for (int i = 0; i < source.Length; i++)
				{
					combineInto[key][i] += source[i];
				}
			}
		}

		protected internal static void Combine2DArr(double[][] combineInto, IDictionary<int, double[]> toBeCombined, double scale)
		{
			foreach (KeyValuePair<int, double[]> entry in toBeCombined)
			{
				int key = entry.Key;
				double[] source = entry.Value;
				for (int i = 0; i < source.Length; i++)
				{
					combineInto[key][i] += source[i] * scale;
				}
			}
		}

		// this used to be computed lazily, but that was clearly erroneous for multithreading!
		public override int DomainDimension()
		{
			return domainDimension;
		}

		/// <summary>
		/// Takes a double array of weights and creates a 2D array where:
		/// the first element is the mapped index of the clique size (e.g., node-0, edge-1) matching featuresIndex i
		/// the second element is the number of output classes for that clique size
		/// </summary>
		/// <returns>a 2D weight array</returns>
		public static double[][] To2D(double[] weights, IList<IIndex<CRFLabel>> labelIndices, int[] map)
		{
			double[][] newWeights = new double[map.Length][];
			int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				int labelSize = labelIndices[map[i]].Size();
				newWeights[i] = new double[labelSize];
				try
				{
					System.Array.Copy(weights, index, newWeights[i], 0, labelSize);
				}
				catch (Exception ex)
				{
					log.Info("weights: " + Arrays.ToString(weights));
					log.Info("newWeights[" + i + "]: " + Arrays.ToString(newWeights[i]));
					throw new Exception(ex);
				}
				index += labelSize;
			}
			return newWeights;
		}

		public virtual double[][] To2D(double[] weights)
		{
			return To2D(weights, this.labelIndices, this.map);
		}

		public static void To2D(double[] weights, IList<IIndex<CRFLabel>> labelIndices, int[] map, double[][] newWeights)
		{
			int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				int labelSize = labelIndices[map[i]].Size();
				try
				{
					System.Array.Copy(weights, index, newWeights[i], 0, labelSize);
				}
				catch (Exception ex)
				{
					log.Info("weights: " + Arrays.ToString(weights));
					log.Info("newWeights[" + i + "]: " + Arrays.ToString(newWeights[i]));
					throw new Exception(ex);
				}
				index += labelSize;
			}
		}

		public virtual void To2D(double[] weights1D, double[][] newWeights)
		{
			To2D(weights1D, this.labelIndices, this.map, newWeights);
		}

		/// <summary>Beware: this changes the input weights array in place.</summary>
		public virtual double[][] To2D(double[] weights1D, double wScale)
		{
			for (int i = 0; i < weights1D.Length; i++)
			{
				weights1D[i] = weights1D[i] * wScale;
			}
			return To2D(weights1D, this.labelIndices, this.map);
		}

		/// <summary>Beware: this changes the input weights array in place.</summary>
		public virtual void To2D(double[] weights1D, double wScale, double[][] newWeights)
		{
			for (int i = 0; i < weights1D.Length; i++)
			{
				weights1D[i] = weights1D[i] * wScale;
			}
			To2D(weights1D, this.labelIndices, this.map, newWeights);
		}

		public static void Clear2D(double[][] arr2D)
		{
			for (int i = 0; i < arr2D.Length; i++)
			{
				for (int j = 0; j < arr2D[i].Length; j++)
				{
					arr2D[i][j] = 0.0;
				}
			}
		}

		public static void To1D(double[][] weights, double[] newWeights)
		{
			int index = 0;
			foreach (double[] weightVector in weights)
			{
				System.Array.Copy(weightVector, 0, newWeights, index, weightVector.Length);
				index += weightVector.Length;
			}
		}

		public static double[] To1D(double[][] weights, int domainDimension)
		{
			double[] newWeights = new double[domainDimension];
			int index = 0;
			foreach (double[] weightVector in weights)
			{
				System.Array.Copy(weightVector, 0, newWeights, index, weightVector.Length);
				index += weightVector.Length;
			}
			return newWeights;
		}

		public virtual double[] To1D(double[][] weights)
		{
			return To1D(weights, DomainDimension());
		}

		public virtual int[][] GetWeightIndices()
		{
			if (weightIndices == null)
			{
				weightIndices = new int[map.Length][];
				int index = 0;
				for (int i = 0; i < map.Length; i++)
				{
					weightIndices[i] = new int[labelIndices[map[i]].Size()];
					for (int j = 0; j < labelIndices[map[i]].Size(); j++)
					{
						weightIndices[i][j] = index;
						index++;
					}
				}
			}
			return weightIndices;
		}

		protected internal virtual double[][] Empty2D()
		{
			double[][] d = new double[map.Length][];
			// int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				d[i] = new double[labelIndices[map[i]].Size()];
			}
			return d;
		}

		public virtual int[][] GetLabels()
		{
			return labels;
		}
	}
}
