using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Stochastic Gradient Descent With AdaGrad and FOBOS in batch mode.</summary>
	/// <remarks>
	/// Stochastic Gradient Descent With AdaGrad and FOBOS in batch mode.
	/// Optionally, user can also turn on AdaDelta via option "useAdaDelta"
	/// Similar to SGDMinimizer, regularization is done in the minimizer, not in the objective function.
	/// This version is not efficient for online setting. For online variant, consider implementing SparseAdaGradMinimizer.java
	/// </remarks>
	/// <author>Mengqiu Wang</author>
	public class SGDWithAdaGradAndFOBOS<T> : IMinimizer<T>, IHasEvaluators
		where T : IDiffFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SGDWithAdaGradAndFOBOS));

		protected internal double[] x;

		protected internal double initRate;

		protected internal double lambda;

		protected internal double alpha = 1.0;

		protected internal bool quiet = false;

		private const int DefaultNumPasses = 50;

		protected internal readonly int numPasses;

		protected internal int bSize = 1;

		private const int DefaultTuningSamples = int.MaxValue;

		private const int DefaultBatchSize = 1000;

		private double eps = 1e-3;

		private double Tol = 1e-4;

		public IList<double[]> yList = null;

		public IList<double[]> sList = null;

		public double[] diag;

		private int hessSampleSize = -1;

		private double[] s;

		private double[] y = null;

		protected internal Random gen = new Random(1);

		protected internal long maxTime = long.MaxValue;

		private int evaluateIters = 0;

		private IEvaluator[] evaluators;

		private SGDWithAdaGradAndFOBOS.Prior prior = SGDWithAdaGradAndFOBOS.Prior.Lasso;

		private bool useEvalImprovement = false;

		private bool useAvgImprovement = false;

		private bool suppressTestPrompt = false;

		private int terminateOnEvalImprovementNumOfEpoch = 1;

		private double bestEvalSoFar = double.NegativeInfinity;

		private double[] xBest;

		private int noImproveItrCount = 0;

		private bool useAdaDelta = false;

		private bool useAdaDiff = false;

		private double rho = 0.95;

		private double[] sumGradSquare;

		private double[] prevGrad;

		private double[] prevDeltaX;

		private double[] sumDeltaXSquare;

		// Initial stochastic iteration count
		// when alpha = 1, sg-lasso is just lasso; when alpha = 0, sg-lasso is g-lasso
		//-1;
		// NOTE: If bSize does not divide evenly into total number of samples,
		// some samples may get accounted for twice in one pass
		// fields for approximating Hessian to feed to QN
		// Evaluate every x iterations (0 = no evaluation)
		// separate set of evaluators to check how optimization is going
		public virtual void SetHessSampleSize(int hessSize)
		{
			this.hessSampleSize = hessSize;
		}

		// (TODO) should initialize relevant data structure as well
		public virtual void TerminateOnEvalImprovement(bool toTerminate)
		{
			useEvalImprovement = toTerminate;
		}

		public virtual void TerminateOnAvgImprovement(bool toTerminate, double tolerance)
		{
			useAvgImprovement = toTerminate;
			Tol = tolerance;
		}

		public virtual void SuppressTestPrompt(bool suppressTestPrompt)
		{
			this.suppressTestPrompt = suppressTestPrompt;
		}

		public virtual void SetTerminateOnEvalImprovementNumOfEpoch(int terminateOnEvalImprovementNumOfEpoch)
		{
			this.terminateOnEvalImprovementNumOfEpoch = terminateOnEvalImprovementNumOfEpoch;
		}

		public virtual bool ToContinue(double[] x, double currEval)
		{
			if (currEval >= bestEvalSoFar)
			{
				bestEvalSoFar = currEval;
				noImproveItrCount = 0;
				if (xBest == null)
				{
					xBest = Arrays.CopyOf(x, x.Length);
				}
				else
				{
					System.Array.Copy(x, 0, xBest, 0, x.Length);
				}
				return true;
			}
			else
			{
				noImproveItrCount += 1;
				return noImproveItrCount <= terminateOnEvalImprovementNumOfEpoch;
			}
		}

		public enum Prior
		{
			Lasso,
			Ridge,
			Gaussian,
			aeLASSO,
			gLASSO,
			sgLASSO,
			None
		}

		private static SGDWithAdaGradAndFOBOS.Prior GetPrior(string priorType)
		{
			switch (priorType)
			{
				case "none":
				{
					return SGDWithAdaGradAndFOBOS.Prior.None;
				}

				case "lasso":
				{
					return SGDWithAdaGradAndFOBOS.Prior.Lasso;
				}

				case "ridge":
				{
					return SGDWithAdaGradAndFOBOS.Prior.Ridge;
				}

				case "gaussian":
				{
					return SGDWithAdaGradAndFOBOS.Prior.Gaussian;
				}

				case "ae-lasso":
				{
					return SGDWithAdaGradAndFOBOS.Prior.aeLASSO;
				}

				case "g-lasso":
				{
					return SGDWithAdaGradAndFOBOS.Prior.gLASSO;
				}

				case "sg-lasso":
				{
					return SGDWithAdaGradAndFOBOS.Prior.sgLASSO;
				}

				default:
				{
					throw new ArgumentException("prior type " + priorType + " not recognized; supported priors " + "are: lasso, ridge, gaussian, ae-lasso, g-lasso, and sg-lasso");
				}
			}
		}

		public SGDWithAdaGradAndFOBOS(double initRate, double lambda, int numPasses)
			: this(initRate, lambda, numPasses, -1)
		{
		}

		public SGDWithAdaGradAndFOBOS(double initRate, double lambda, int numPasses, int batchSize)
			: this(initRate, lambda, numPasses, batchSize, "lasso", 1.0, false, false, 1e-3, 0.95)
		{
		}

		public SGDWithAdaGradAndFOBOS(double initRate, double lambda, int numPasses, int batchSize, string priorType, double alpha, bool useAdaDelta, bool useAdaDiff, double adaGradEps, double adaDeltaRho)
		{
			this.initRate = initRate;
			this.prior = GetPrior(priorType);
			this.bSize = batchSize;
			this.lambda = lambda;
			this.eps = adaGradEps;
			this.rho = adaDeltaRho;
			this.useAdaDelta = useAdaDelta;
			this.useAdaDiff = useAdaDiff;
			this.alpha = alpha;
			if (numPasses >= 0)
			{
				this.numPasses = numPasses;
			}
			else
			{
				this.numPasses = DefaultNumPasses;
				Sayln("  SGDWithAdaGradAndFOBOS: numPasses=" + numPasses + ", defaulting to " + this.numPasses);
			}
		}

		public virtual void ShutUp()
		{
			this.quiet = true;
		}

		private static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		protected internal virtual string GetName()
		{
			return "SGDWithAdaGradAndFOBOS" + bSize + "_lambda" + nf.Format(lambda) + "_alpha" + nf.Format(alpha);
		}

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			this.evaluateIters = iters;
			this.evaluators = evaluators;
		}

		// really this is the the L2 norm....
		private static double GetNorm(double[] w)
		{
			double norm = 0;
			foreach (double aW in w)
			{
				norm += aW * aW;
			}
			return Math.Sqrt(norm);
		}

		private double DoEvaluation(double[] x)
		{
			// Evaluate solution
			if (evaluators == null)
			{
				return double.NegativeInfinity;
			}
			double score = double.NegativeInfinity;
			foreach (IEvaluator eval in evaluators)
			{
				if (!suppressTestPrompt)
				{
					Sayln("  Evaluating: " + eval.ToString());
				}
				double aScore = eval.Evaluate(x);
				if (aScore != double.NegativeInfinity)
				{
					score = aScore;
				}
			}
			return score;
		}

		private static double Pospart(double number)
		{
			return number > 0.0 ? number : 0.0;
		}

		/*
		private void approxHessian(double[] newX) {
		for(int i = 0; i < x.length; i++){
		double thisGain = fixedGain*gainSchedule(k,5*numBatches)/(diag[i]);
		newX[i] = x[i] - thisGain*grad[i];
		}
		
		//Get a new pair...
		say(" A ");
		if (hessSampleSize > 0 && sList.size() == hessSampleSize || sList.size() == hessSampleSize) {
		s = sList.remove(0);
		y = yList.remove(0);
		} else {
		s = new double[x.length];
		y = new double[x.length];
		}
		
		s = prevDeltaX;
		
		s = ArrayMath.pairwiseSubtract(newX, x);
		dfunction.recalculatePrevBatch = true;
		System.arraycopy(dfunction.derivativeAt(newX,bSize),0,y,0,grad.length);
		
		ArrayMath.pairwiseSubtractInPlace(y,newGrad);  // newY = newY-newGrad
		double[] comp = new double[x.length];
		
		sList.add(s);
		yList.add(y);
		ScaledSGDMinimizer.updateDiagBFGS(diag,s,y);
		}
		*/
		private double ComputeLearningRate(int index, double grad)
		{
			// double eps2 = 1e-12;
			double currentRate = double.NegativeInfinity;
			double prevG = prevGrad[index];
			double gradDiff = grad - prevG;
			if (useAdaDelta)
			{
				double deltaXt = prevDeltaX[index];
				sumDeltaXSquare[index] = sumDeltaXSquare[index] * rho + (1 - rho) * deltaXt * deltaXt;
				if (useAdaDiff)
				{
					sumGradSquare[index] = sumGradSquare[index] * rho + (1 - rho) * (gradDiff) * (gradDiff);
				}
				else
				{
					sumGradSquare[index] = sumGradSquare[index] * rho + (1 - rho) * grad * grad;
				}
				// double nominator = initRate;
				// if (sumDeltaXSquare[index] > 0) {
				//   nominator = Math.sqrt(sumDeltaXSquare[index]+eps);
				// }
				// currentRate = nominator / Math.sqrt(sumGradSquare[index]+eps);
				currentRate = Math.Sqrt(sumDeltaXSquare[index] + eps) / Math.Sqrt(sumGradSquare[index] + eps);
			}
			else
			{
				// double deltaXt = currentRate * grad;
				// sumDeltaXSquare[index] = sumDeltaXSquare[index] * rho + (1-rho) * deltaXt * deltaXt;
				if (useAdaDiff)
				{
					sumGradSquare[index] += gradDiff * gradDiff;
				}
				else
				{
					sumGradSquare[index] += grad * grad;
				}
				// apply AdaGrad
				currentRate = initRate / Math.Sqrt(sumGradSquare[index] + eps);
			}
			// prevDeltaX[index] = grad * currentRate;
			return currentRate;
		}

		private void UpdateX(double[] x, int index, double realUpdate)
		{
			prevDeltaX[index] = realUpdate - x[index];
			x[index] = realUpdate;
		}

		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		public virtual double[] Minimize(IDiffFunction f, double functionTolerance, double[] initial, int maxIterations)
		{
			int totalSamples = 0;
			Sayln("Using lambda=" + lambda);
			if (f is AbstractStochasticCachingDiffUpdateFunction)
			{
				AbstractStochasticCachingDiffUpdateFunction func = (AbstractStochasticCachingDiffUpdateFunction)f;
				func.sampleMethod = AbstractStochasticCachingDiffFunction.SamplingMethod.Shuffled;
				totalSamples = func.DataDimension();
				if (bSize > totalSamples)
				{
					log.Info("WARNING: Total number of samples=" + totalSamples + " is smaller than requested batch size=" + bSize + "!!!");
					bSize = totalSamples;
					Sayln("Using batch size=" + bSize);
				}
				if (bSize <= 0)
				{
					log.Info("WARNING: Requested batch size=" + bSize + " <= 0 !!!");
					bSize = totalSamples;
					Sayln("Using batch size=" + bSize);
				}
			}
			x = new double[initial.Length];
			double[] testUpdateCache = null;
			double[] currentRateCache = null;
			double[] bCache = null;
			sumGradSquare = new double[initial.Length];
			prevGrad = new double[initial.Length];
			prevDeltaX = new double[initial.Length];
			if (useAdaDelta)
			{
				sumDeltaXSquare = new double[initial.Length];
				if (prior != SGDWithAdaGradAndFOBOS.Prior.None && prior != SGDWithAdaGradAndFOBOS.Prior.Gaussian)
				{
					throw new NotSupportedException("useAdaDelta is currently only supported for Prior.NONE or Prior.GAUSSIAN");
				}
			}
			int[][] featureGrouping = null;
			if (prior != SGDWithAdaGradAndFOBOS.Prior.Lasso && prior != SGDWithAdaGradAndFOBOS.Prior.None)
			{
				testUpdateCache = new double[initial.Length];
				currentRateCache = new double[initial.Length];
			}
			if (prior != SGDWithAdaGradAndFOBOS.Prior.Lasso && prior != SGDWithAdaGradAndFOBOS.Prior.Ridge && prior != SGDWithAdaGradAndFOBOS.Prior.Gaussian)
			{
				if (!(f is IHasFeatureGrouping))
				{
					throw new NotSupportedException("prior is specified to be ae-lasso or g-lasso, but function does not support feature grouping");
				}
				featureGrouping = ((IHasFeatureGrouping)f).GetFeatureGrouping();
			}
			if (prior == SGDWithAdaGradAndFOBOS.Prior.sgLASSO)
			{
				bCache = new double[initial.Length];
			}
			System.Array.Copy(initial, 0, x, 0, x.Length);
			int numBatches = 1;
			if (f is AbstractStochasticCachingDiffUpdateFunction)
			{
				if (totalSamples > 0)
				{
					numBatches = totalSamples / bSize;
				}
			}
			bool have_max = (maxIterations > 0 || numPasses > 0);
			if (!have_max)
			{
				throw new NotSupportedException("No maximum number of iterations has been specified.");
			}
			else
			{
				maxIterations = Math.Max(maxIterations, numPasses * numBatches);
			}
			Sayln("       Batch size of: " + bSize);
			Sayln("       Data dimension of: " + totalSamples);
			Sayln("       Batches per pass through data:  " + numBatches);
			Sayln("       Number of passes is = " + numPasses);
			Sayln("       Max iterations is = " + maxIterations);
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//            Loop
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			Timing total = new Timing();
			Timing current = new Timing();
			total.Start();
			current.Start();
			int iters = 0;
			double gValue = 0;
			double wValue = 0;
			double currentRate = 0;
			double testUpdate = 0;
			double realUpdate = 0;
			IList<double> values = null;
			double oldObjVal = 0;
			for (int pass = 0; pass < numPasses; pass++)
			{
				bool doEval = (pass > 0 && evaluateIters > 0 && pass % evaluateIters == 0);
				double evalScore = double.NegativeInfinity;
				if (doEval)
				{
					evalScore = DoEvaluation(x);
					if (useEvalImprovement && !ToContinue(x, evalScore))
					{
						break;
					}
				}
				// TODO: currently objVal is only updated for GAUSSIAN prior
				// when other priors are used, objVal only reflects the un-regularized obj value
				double objVal = double.NegativeInfinity;
				double objDelta = double.NegativeInfinity;
				Say("Iter: " + iters + " pass " + pass + " batch 1 ... ");
				int numOfNonZero = 0;
				int numOfNonZeroGroup = 0;
				string gSizeStr = string.Empty;
				for (int batch = 0; batch < numBatches; batch++)
				{
					iters++;
					//Get the next gradients
					// log.info("getting gradients");
					double[] gradients = null;
					if (f is AbstractStochasticCachingDiffUpdateFunction)
					{
						AbstractStochasticCachingDiffUpdateFunction func = (AbstractStochasticCachingDiffUpdateFunction)f;
						if (bSize == totalSamples)
						{
							objVal = func.ValueAt(x);
							gradients = func.GetDerivative();
							objDelta = objVal - oldObjVal;
							oldObjVal = objVal;
							if (values == null)
							{
								values = new List<double>();
							}
							values.Add(objVal);
						}
						else
						{
							func.CalculateStochasticGradient(x, bSize);
							gradients = func.GetDerivative();
						}
					}
					else
					{
						if (f is AbstractCachingDiffFunction)
						{
							AbstractCachingDiffFunction func = (AbstractCachingDiffFunction)f;
							gradients = func.DerivativeAt(x);
						}
					}
					// log.info("applying regularization");
					if (prior == SGDWithAdaGradAndFOBOS.Prior.None || prior == SGDWithAdaGradAndFOBOS.Prior.Gaussian)
					{
						// Gaussian prior is also handled in objective
						for (int index = 0; index < x.Length; index++)
						{
							gValue = gradients[index];
							currentRate = ComputeLearningRate(index, gValue);
							// arrive at x(t+1/2)
							wValue = x[index];
							testUpdate = wValue - (currentRate * gValue);
							realUpdate = testUpdate;
							UpdateX(x, index, realUpdate);
						}
					}
					else
					{
						// x[index] = testUpdate;
						if (prior == SGDWithAdaGradAndFOBOS.Prior.Lasso || prior == SGDWithAdaGradAndFOBOS.Prior.Ridge)
						{
							double testUpdateSquaredSum = 0;
							ICollection<int> paramRange = null;
							if (f is IHasRegularizerParamRange)
							{
								paramRange = ((IHasRegularizerParamRange)f).GetRegularizerParamRange(x);
							}
							else
							{
								paramRange = new HashSet<int>();
								for (int i = 0; i < x.Length; i++)
								{
									paramRange.Add(i);
								}
							}
							foreach (int index in paramRange)
							{
								gValue = gradients[index];
								currentRate = ComputeLearningRate(index, gValue);
								// arrive at x(t+1/2)
								wValue = x[index];
								testUpdate = wValue - (currentRate * gValue);
								double currentLambda = currentRate * lambda;
								// apply FOBOS
								if (prior == SGDWithAdaGradAndFOBOS.Prior.Lasso)
								{
									realUpdate = Math.Signum(testUpdate) * Pospart(Math.Abs(testUpdate) - currentLambda);
									UpdateX(x, index, realUpdate);
									if (realUpdate != 0)
									{
										numOfNonZero++;
									}
								}
								else
								{
									if (prior == SGDWithAdaGradAndFOBOS.Prior.Ridge)
									{
										testUpdateSquaredSum += testUpdate * testUpdate;
										testUpdateCache[index] = testUpdate;
										currentRateCache[index] = currentRate;
									}
								}
							}
							// } else if (prior == Prior.GAUSSIAN) { // GAUSSIAN prior is assumed to be handled in the objective directly
							//   realUpdate = testUpdate / (1 + currentLambda);
							//   updateX(x, index, realUpdate);
							//   // update objVal
							//   objVal += currentLambda * wValue * wValue;
							if (prior == SGDWithAdaGradAndFOBOS.Prior.Ridge)
							{
								double testUpdateNorm = Math.Sqrt(testUpdateSquaredSum);
								for (int index_1 = 0; index_1 < testUpdateCache.Length; index_1++)
								{
									realUpdate = testUpdateCache[index_1] * Pospart(1 - currentRateCache[index_1] * lambda / testUpdateNorm);
									UpdateX(x, index_1, realUpdate);
									if (realUpdate != 0)
									{
										numOfNonZero++;
									}
								}
							}
						}
						else
						{
							// log.info("featureGroup.length: " + featureGrouping.length);
							foreach (int[] gFeatureIndices in featureGrouping)
							{
								// if (gIndex % 100 == 0) log.info(gIndex+" ");
								double testUpdateSquaredSum = 0;
								double testUpdateAbsSum = 0;
								double M = gFeatureIndices.Length;
								double dm = Math.Log(M);
								foreach (int index in gFeatureIndices)
								{
									gValue = gradients[index];
									currentRate = ComputeLearningRate(index, gValue);
									// arrive at x(t+1/2)
									wValue = x[index];
									testUpdate = wValue - (currentRate * gValue);
									testUpdateSquaredSum += testUpdate * testUpdate;
									testUpdateAbsSum += Math.Abs(testUpdate);
									testUpdateCache[index] = testUpdate;
									currentRateCache[index] = currentRate;
								}
								if (prior == SGDWithAdaGradAndFOBOS.Prior.gLASSO)
								{
									double testUpdateNorm = Math.Sqrt(testUpdateSquaredSum);
									bool groupHasNonZero = false;
									foreach (int index_1 in gFeatureIndices)
									{
										realUpdate = testUpdateCache[index_1] * Pospart(1 - currentRateCache[index_1] * lambda * dm / testUpdateNorm);
										UpdateX(x, index_1, realUpdate);
										if (realUpdate != 0)
										{
											numOfNonZero++;
											groupHasNonZero = true;
										}
									}
									if (groupHasNonZero)
									{
										numOfNonZeroGroup++;
									}
								}
								else
								{
									if (prior == SGDWithAdaGradAndFOBOS.Prior.aeLASSO)
									{
										int nonZeroCount = 0;
										bool groupHasNonZero = false;
										foreach (int index_1 in gFeatureIndices)
										{
											double tau = currentRateCache[index_1] * lambda / (1 + currentRateCache[index_1] * lambda * M) * testUpdateAbsSum;
											realUpdate = Math.Signum(testUpdateCache[index_1]) * Pospart(Math.Abs(testUpdateCache[index_1]) - tau);
											UpdateX(x, index_1, realUpdate);
											if (realUpdate != 0)
											{
												numOfNonZero++;
												nonZeroCount++;
												groupHasNonZero = true;
											}
										}
										if (groupHasNonZero)
										{
											numOfNonZeroGroup++;
										}
									}
									else
									{
										// gSizeStr += nonZeroCount+",";
										if (prior == SGDWithAdaGradAndFOBOS.Prior.sgLASSO)
										{
											double bSquaredSum = 0;
											double b = 0;
											foreach (int index_1 in gFeatureIndices)
											{
												b = Math.Signum(testUpdateCache[index_1]) * Pospart(Math.Abs(testUpdateCache[index_1]) - currentRateCache[index_1] * alpha * lambda);
												bCache[index_1] = b;
												bSquaredSum += b * b;
											}
											double bNorm = Math.Sqrt(bSquaredSum);
											int nonZeroCount = 0;
											bool groupHasNonZero = false;
											foreach (int index_2 in gFeatureIndices)
											{
												realUpdate = bCache[index_2] * Pospart(1 - currentRateCache[index_2] * (1.0 - alpha) * lambda * dm / bNorm);
												UpdateX(x, index_2, realUpdate);
												if (realUpdate != 0)
												{
													numOfNonZero++;
													nonZeroCount++;
													groupHasNonZero = true;
												}
											}
											if (groupHasNonZero)
											{
												numOfNonZeroGroup++;
											}
										}
									}
								}
							}
						}
					}
					// gSizeStr += nonZeroCount+",";
					// log.info();
					// update gradient and lastX
					for (int index_3 = 0; index_3 < x.Length; index_3++)
					{
						prevGrad[index_3] = gradients[index_3];
					}
				}
				// if (hessSampleSize > 0) {
				//   approxHessian();
				// }
				try
				{
					ArrayMath.AssertFinite(x, "x");
				}
				catch (ArrayMath.InvalidElementException e)
				{
					log.Info(e.ToString());
					for (int i = 0; i < x.Length; i++)
					{
						x[i] = double.NaN;
					}
					break;
				}
				Sayln(numBatches.ToString() + ", n0-fCount:" + numOfNonZero + ((prior != SGDWithAdaGradAndFOBOS.Prior.Lasso && prior != SGDWithAdaGradAndFOBOS.Prior.Ridge) ? ", n0-gCount:" + numOfNonZeroGroup : string.Empty) + ((evalScore != double.NegativeInfinity
					) ? ", evalScore:" + evalScore : string.Empty) + (objVal != double.NegativeInfinity ? ", obj_val:" + nf.Format(objVal) + ", obj_delta:" + objDelta : string.Empty));
				if (values != null && useAvgImprovement && iters > 5)
				{
					int size = values.Count;
					double previousVal = (size >= 10 ? values[size - 10] : values[0]);
					double averageImprovement = (previousVal - objVal) / (size >= 10 ? 10 : size);
					if (System.Math.Abs(averageImprovement / objVal) < Tol)
					{
						Sayln("Online Optmization completed, due to average improvement: | newest_val - previous_val | / |newestVal| < TOL ");
						break;
					}
				}
				if (iters >= maxIterations)
				{
					Sayln("Online Optimization complete.  Stopped after max iterations");
					break;
				}
				if (total.Report() >= maxTime)
				{
					Sayln("Online Optimization complete.  Stopped after max time");
					break;
				}
			}
			if (evaluateIters > 0)
			{
				// do final evaluation
				double evalScore = (useEvalImprovement ? DoEvaluation(xBest) : DoEvaluation(x));
				Sayln("final evalScore is: " + evalScore);
			}
			Sayln("Completed in: " + Timing.ToSecondsString(total.Report()) + " s");
			return (useEvalImprovement ? xBest : x);
		}

		protected internal virtual void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}

		protected internal virtual void Say(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}
	}
}
