/*
* Title:       Stanford JavaNLP.
* Description: A Maximum Entropy Toolkit.
* Copyright:   Copyright (c) 2002. Kristina Toutanova, Stanford University
* Company:     Stanford University, All Rights Reserved.
*/
using System;
using Edu.Stanford.Nlp.Maxent.Iis;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Maxent
{
	/// <summary>
	/// This class will call an optimization method such as Conjugate Gradient or
	/// Quasi-Newton  on a LambdaSolve object to find
	/// optimal parameters, including imposing a Gaussian prior on those
	/// parameters.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <author>Christopher Manning</author>
	public class CGRunner
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Maxent.CGRunner));

		private const bool SaveLambdasRegularly = false;

		private readonly LambdaSolve prob;

		private readonly string filename;

		/// <summary>Error tolerance passed to CGMinimizer</summary>
		private readonly double tol;

		private readonly bool useGaussianPrior;

		private readonly double priorSigmaS;

		private readonly double[] sigmaSquareds;

		private const double DefaultTolerance = 1e-4;

		private const double DefaultSigmasquared = 0.5;

		/// <summary>Set up a LambdaSolve problem for solution by a Minimizer.</summary>
		/// <remarks>
		/// Set up a LambdaSolve problem for solution by a Minimizer.
		/// Uses a Gaussian prior with a sigma<sup>2</sup> of 0.5.
		/// </remarks>
		/// <param name="prob">The problem to solve</param>
		/// <param name="filename">Used (with extension) to save intermediate results.</param>
		public CGRunner(LambdaSolve prob, string filename)
			: this(prob, filename, DefaultSigmasquared)
		{
		}

		/// <summary>
		/// Set up a LambdaSolve problem for solution by a Minimizer,
		/// specifying a value for sigma<sup>2</sup>.
		/// </summary>
		/// <param name="prob">The problem to solve</param>
		/// <param name="filename">Used (with extension) to save intermediate results.</param>
		/// <param name="priorSigmaS">
		/// The prior sigma<sup>2</sup>: this doubled will be
		/// used to divide the lambda<sup>2</sup> values as the
		/// prior penalty in the likelihood.  A value of 0.0
		/// or Double.POSITIVE_INFINITY
		/// indicates to not use regularization.
		/// </param>
		public CGRunner(LambdaSolve prob, string filename, double priorSigmaS)
			: this(prob, filename, DefaultTolerance, priorSigmaS)
		{
		}

		/// <summary>Set up a LambdaSolve problem for solution by a Minimizer.</summary>
		/// <param name="prob">The problem to solve</param>
		/// <param name="filename">Used (with extension) to save intermediate results.</param>
		/// <param name="tol">Tolerance of errors (passed to CG)</param>
		/// <param name="priorSigmaS">
		/// The prior sigma<sup>2</sup>: this doubled will be
		/// used to divide the lambda<sup>2</sup> values as the
		/// prior penalty.  A value of 0.0
		/// or Double.POSITIVE_INFINITY
		/// indicates to not use regularization.
		/// </param>
		public CGRunner(LambdaSolve prob, string filename, double tol, double priorSigmaS)
		{
			// = null;
			this.prob = prob;
			this.filename = filename;
			this.tol = tol;
			this.useGaussianPrior = priorSigmaS != 0.0 && priorSigmaS != double.PositiveInfinity;
			this.priorSigmaS = priorSigmaS;
			this.sigmaSquareds = null;
		}

		/// <summary>Set up a LambdaSolve problem for solution by a Minimizer.</summary>
		/// <param name="prob">The problem to solve</param>
		/// <param name="filename">Used (with extension) to save intermediate results.</param>
		/// <param name="tol">Tolerance of errors (passed to CG)</param>
		/// <param name="sigmaSquareds">
		/// The prior sigma<sup>2</sup> for each feature: this doubled will be
		/// used to divide the lambda<sup>2</sup> values as the
		/// prior penalty. This array must have size the number of features.
		/// If it is null, no regularization will be performed.
		/// </param>
		public CGRunner(LambdaSolve prob, string filename, double tol, double[] sigmaSquareds)
		{
			this.prob = prob;
			this.filename = filename;
			this.tol = tol;
			this.useGaussianPrior = sigmaSquareds != null;
			this.sigmaSquareds = sigmaSquareds;
			this.priorSigmaS = -1.0;
		}

		// not used
		private void PrintOptimizationResults(CGRunner.LikelihoodFunction df, CGRunner.MonitorFunction monitor)
		{
			double negLogLike = df.ValueAt(prob.lambda);
			System.Console.Error.Printf("After optimization neg (penalized) log cond likelihood: %1.2f%n", negLogLike);
			if (monitor != null)
			{
				monitor.ReportMonitoring(negLogLike);
			}
			int numNonZero = 0;
			for (int i = 0; i < prob.lambda.Length; i++)
			{
				if (prob.lambda[i] != 0.0)
				{
					// 0.0 == -0.0 in IEEE math!
					numNonZero++;
				}
			}
			System.Console.Error.Printf("Non-zero parameters: %d/%d (%1.2f%%)%n", numNonZero, prob.lambda.Length, (100.0 * numNonZero) / prob.lambda.Length);
		}

		/// <summary>Solves the problem using a quasi-newton method (L-BFGS).</summary>
		/// <remarks>
		/// Solves the problem using a quasi-newton method (L-BFGS).  The solution
		/// is stored in the
		/// <c>lambda</c>
		/// array of
		/// <c>prob</c>
		/// .
		/// </remarks>
		public virtual void SolveQN()
		{
			CGRunner.LikelihoodFunction df = new CGRunner.LikelihoodFunction(prob, tol, useGaussianPrior, priorSigmaS, sigmaSquareds);
			CGRunner.MonitorFunction monitor = new CGRunner.MonitorFunction(prob, df, filename);
			IMinimizer<IDiffFunction> cgm = new QNMinimizer(monitor, 10);
			// all parameters are started at 0.0
			prob.lambda = cgm.Minimize(df, tol, new double[df.DomainDimension()]);
			PrintOptimizationResults(df, monitor);
		}

		public virtual void SolveOWLQN2(double weight)
		{
			CGRunner.LikelihoodFunction df = new CGRunner.LikelihoodFunction(prob, tol, useGaussianPrior, priorSigmaS, sigmaSquareds);
			CGRunner.MonitorFunction monitor = new CGRunner.MonitorFunction(prob, df, filename);
			IMinimizer<IDiffFunction> cgm = new QNMinimizer(monitor, 10);
			((QNMinimizer)cgm).UseOWLQN(true, weight);
			// all parameters are started at 0.0
			prob.lambda = cgm.Minimize(df, tol, new double[df.DomainDimension()]);
			PrintOptimizationResults(df, monitor);
		}

		/// <summary>Solves the problem using conjugate gradient (CG).</summary>
		/// <remarks>
		/// Solves the problem using conjugate gradient (CG).  The solution
		/// is stored in the
		/// <c>lambda</c>
		/// array of
		/// <c>prob</c>
		/// .
		/// </remarks>
		public virtual void SolveCG()
		{
			CGRunner.LikelihoodFunction df = new CGRunner.LikelihoodFunction(prob, tol, useGaussianPrior, priorSigmaS, sigmaSquareds);
			CGRunner.MonitorFunction monitor = new CGRunner.MonitorFunction(prob, df, filename);
			IMinimizer<IDiffFunction> cgm = new CGMinimizer(monitor);
			// all parameters are started at 0.0
			prob.lambda = cgm.Minimize(df, tol, new double[df.DomainDimension()]);
			PrintOptimizationResults(df, monitor);
		}

		/// <summary>Solves the problem using OWLQN.</summary>
		/// <remarks>
		/// Solves the problem using OWLQN.  The solution
		/// is stored in the
		/// <c>lambda</c>
		/// array of
		/// <c>prob</c>
		/// .  Note that the
		/// likelihood function will be a penalized L2 likelihood function unless you
		/// have turned this off via setting the priorSigmaS to 0.0.
		/// </remarks>
		/// <param name="weight">
		/// Controls the sparseness/regularization of the L1 solution.
		/// The bigger the number the sparser the solution.  Weights between
		/// 0.01 and 1.0 typically give good performance.
		/// </param>
		public virtual void SolveL1(double weight)
		{
			CGRunner.LikelihoodFunction df = new CGRunner.LikelihoodFunction(prob, tol, useGaussianPrior, priorSigmaS, sigmaSquareds);
			IMinimizer<IDiffFunction> owl = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", weight);
			prob.lambda = owl.Minimize(df, tol, new double[df.DomainDimension()]);
			PrintOptimizationResults(df, null);
		}

		/// <summary>This class implements the DiffFunction interface for Minimizer</summary>
		private sealed class LikelihoodFunction : IDiffFunction
		{
			private readonly LambdaSolve model;

			private readonly double tol;

			private readonly bool useGaussianPrior;

			private readonly double[] sigmaSquareds;

			private int valueAtCalls;

			private double likelihood;

			public LikelihoodFunction(LambdaSolve m, double tol, bool useGaussianPrior, double sigmaSquared, double[] sigmaSquareds)
			{
				model = m;
				this.tol = tol;
				this.useGaussianPrior = useGaussianPrior;
				if (useGaussianPrior)
				{
					// keep separate prior on each parameter for flexibility
					this.sigmaSquareds = new double[model.lambda.Length];
					if (sigmaSquareds != null)
					{
						System.Array.Copy(sigmaSquareds, 0, this.sigmaSquareds, 0, sigmaSquareds.Length);
					}
					else
					{
						Arrays.Fill(this.sigmaSquareds, sigmaSquared);
					}
				}
				else
				{
					this.sigmaSquareds = null;
				}
			}

			public int DomainDimension()
			{
				return model.lambda.Length;
			}

			public double Likelihood()
			{
				return likelihood;
			}

			public int NumCalls()
			{
				return valueAtCalls;
			}

			public double ValueAt(double[] lambda)
			{
				valueAtCalls++;
				model.lambda = lambda;
				double lik = model.LogLikelihoodScratch();
				if (useGaussianPrior)
				{
					//double twoSigmaSquared = 2 * sigmaSquared;
					for (int i = 0; i < lambda.Length; i++)
					{
						lik += (lambda[i] * lambda[i]) / (sigmaSquareds[i] + sigmaSquareds[i]);
					}
				}
				// log.info(valueAtCalls + " calls to valueAt;" +
				//		       " penalized log likelihood is " + lik);
				likelihood = lik;
				return lik;
			}

			public double[] DerivativeAt(double[] lambda)
			{
				bool eq = true;
				for (int j = 0; j < lambda.Length; j++)
				{
					if (Math.Abs(lambda[j] - model.lambda[j]) > tol)
					{
						eq = false;
						break;
					}
				}
				if (!eq)
				{
					log.Info("derivativeAt: call with different value");
					ValueAt(lambda);
				}
				double[] drvs = model.GetDerivatives();
				// System.out.println("for lambdas "+lambda[0]+" "+lambda[1] +
				//                   " derivatives "+drvs[0]+" "+drvs[1]);
				if (useGaussianPrior)
				{
					// prior penalty
					for (int j_1 = 0; j_1 < lambda.Length; j_1++)
					{
						// double sign=1;
						// if(lambda[j]<=0){sign=-1;}
						drvs[j_1] += lambda[j_1] / sigmaSquareds[j_1];
					}
				}
				//System.out.println("final derivatives "+drvs[0]+" "+drvs[1]);
				return drvs;
			}
		}

		/// <summary>This one is used in the monitor</summary>
		private sealed class MonitorFunction : IFunction
		{
			private readonly LambdaSolve model;

			private readonly CGRunner.LikelihoodFunction lf;

			private readonly string filename;

			private int iterations;

			public MonitorFunction(LambdaSolve m, CGRunner.LikelihoodFunction lf, string filename)
			{
				// end static class LikelihoodFunction
				// = 0
				this.model = m;
				this.lf = lf;
				this.filename = filename;
			}

			public double ValueAt(double[] lambda)
			{
				double likelihood = lf.Likelihood();
				// this line is printed in the middle of the normal line of QN minimization, so put println at beginning
				log.Info();
				log.Info(ReportMonitoring(likelihood));
				if (SaveLambdasRegularly && iterations > 0 && iterations % 5 == 0)
				{
					model.Save_lambdas(filename + '.' + iterations + ".lam");
				}
				if (iterations > 0 && iterations % 30 == 0)
				{
					model.CheckCorrectness();
				}
				iterations++;
				return 42;
			}

			// never cause premature termination.
			public string ReportMonitoring(double likelihood)
			{
				return "Iter. " + iterations + ": " + "neg. log cond. likelihood = " + likelihood + " [" + lf.NumCalls() + " calls to valueAt]";
			}

			public int DomainDimension()
			{
				return lf.DomainDimension();
			}
		}
		// end static class MonitorFunction
	}
}
