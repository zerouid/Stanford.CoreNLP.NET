// Stanford Classifier - a multiclass maxent classifier
// LinearClassifierFactory
// Copyright (c) 2003-2016 The Board of Trustees of
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
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/classifier.html
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// Builds various types of linear classifiers, with functionality for
	/// setting objective function, optimization method, and other parameters.
	/// </summary>
	/// <remarks>
	/// Builds various types of linear classifiers, with functionality for
	/// setting objective function, optimization method, and other parameters.
	/// Classifiers can be defined with passed constructor arguments or using setter methods.
	/// Defaults to Quasi-newton optimization of a
	/// <c>LogConditionalObjectiveFunction</c>
	/// .
	/// (Merges old classes: CGLinearClassifierFactory, QNLinearClassifierFactory, and MaxEntClassifierFactory.)
	/// Note that a bias term is not assumed, and so if you want to learn
	/// a bias term you should add an "always-on" feature to your examples.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Chris Cox (merged factories, 8/11/04)</author>
	/// <author>Dan Klein (CGLinearClassifierFactory, MaxEntClassifierFactory)</author>
	/// <author>Galen Andrew (tuneSigma),</author>
	/// <author>Marie-Catherine de Marneffe (CV in tuneSigma)</author>
	/// <author>Sarah Spikes (Templatization, though I don't know what to do with the Minimizer)</author>
	/// <author>
	/// Ramesh Nallapati (nmramesh@cs.stanford.edu)
	/// <see cref="LinearClassifierFactory{L, F}.TrainSemiSupGE(GeneralDataset{L, F}, System.Collections.IList{E})"/>
	/// methods
	/// </author>
	[System.Serializable]
	public class LinearClassifierFactory<L, F> : AbstractLinearClassifierFactory<L, F>
	{
		private const long serialVersionUID = 7893768984379107397L;

		private double Tol;

		private int mem = 15;

		private bool verbose = false;

		private LogPrior logPrior;

		private bool tuneSigmaHeldOut = false;

		private bool tuneSigmaCV = false;

		private int folds;

		private double min = 0.1;

		private double max = 10.0;

		private bool retrainFromScratchAfterSigmaTuning = false;

		private IFactory<IMinimizer<IDiffFunction>> minimizerCreator = null;

		private int evalIters = -1;

		private IEvaluator[] evaluators;

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.LinearClassifierFactory));

		/// <summary>
		/// This is the
		/// <c>Factory&lt;Minimizer&lt;DiffFunction&gt;&gt;</c>
		/// that we use over and over again.
		/// </summary>
		[System.Serializable]
		private class QNFactory : IFactory<IMinimizer<IDiffFunction>>
		{
			private const long serialVersionUID = 9028306475652690036L;

			//public double sigma;
			//private int prior;
			//private double epsilon = 0.0;
			//private Minimizer<DiffFunction> minimizer;
			//private boolean useSum = false;
			//private boolean resetWeight = true;
			// range of values to tune sigma across
			// = null;
			public virtual IMinimizer<IDiffFunction> Create()
			{
				QNMinimizer qnMinimizer = new QNMinimizer(this._enclosing._enclosing.mem);
				if (!this._enclosing.verbose)
				{
					qnMinimizer.ShutUp();
				}
				return qnMinimizer;
			}

			internal QNFactory(LinearClassifierFactory<L, F> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;
		}

		public LinearClassifierFactory()
			: this((IFactory<IMinimizer<IDiffFunction>>)null)
		{
		}

		/// <summary>
		/// NOTE: Constructors that take in a Minimizer create a LinearClassifierFactory that will reuse the minimizer
		/// and will not be threadsafe (unless the Minimizer itself is ThreadSafe, which is probably not the case).
		/// </summary>
		public LinearClassifierFactory(IMinimizer<IDiffFunction> min)
			: this(min, 1e-4, false)
		{
		}

		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> min)
			: this(min, 1e-4, false)
		{
		}

		public LinearClassifierFactory(IMinimizer<IDiffFunction> min, double tol, bool useSum)
			: this(min, tol, useSum, 1.0)
		{
		}

		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> min, double tol, bool useSum)
			: this(min, tol, useSum, 1.0)
		{
		}

		public LinearClassifierFactory(double tol, bool useSum, double sigma)
			: this((IFactory<IMinimizer<IDiffFunction>>)null, tol, useSum, sigma)
		{
		}

		public LinearClassifierFactory(IMinimizer<IDiffFunction> min, double tol, bool useSum, double sigma)
			: this(min, tol, useSum, (int)(LogPrior.LogPriorType.Quadratic), sigma)
		{
		}

		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> min, double tol, bool useSum, double sigma)
			: this(min, tol, useSum, (int)(LogPrior.LogPriorType.Quadratic), sigma)
		{
		}

		public LinearClassifierFactory(IMinimizer<IDiffFunction> min, double tol, bool useSum, int prior, double sigma)
			: this(min, tol, useSum, prior, sigma, 0.0)
		{
		}

		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> min, double tol, bool useSum, int prior, double sigma)
			: this(min, tol, useSum, prior, sigma, 0.0)
		{
		}

		public LinearClassifierFactory(double tol, bool useSum, int prior, double sigma, double epsilon)
			: this((IFactory<IMinimizer<IDiffFunction>>)null, tol, useSum, new LogPrior(prior, sigma, epsilon))
		{
		}

		public LinearClassifierFactory(double tol, bool useSum, int prior, double sigma, double epsilon, int mem)
			: this((IFactory<IMinimizer<IDiffFunction>>)null, tol, useSum, new LogPrior(prior, sigma, epsilon))
		{
			// end class QNFactory
			this.mem = mem;
		}

		/// <summary>Create a factory that builds linear classifiers from training data.</summary>
		/// <param name="min">
		/// The method to be used for optimization (minimization) (default:
		/// <see cref="Edu.Stanford.Nlp.Optimization.QNMinimizer"/>
		/// )
		/// </param>
		/// <param name="tol">The convergence threshold for the minimization (default: 1e-4)</param>
		/// <param name="useSum">
		/// Asks to the optimizer to minimize the sum of the
		/// likelihoods of individual data items rather than their product (default: false)
		/// NOTE: this is currently ignored!!!
		/// </param>
		/// <param name="prior">
		/// What kind of prior to use, as an enum constant from class
		/// LogPrior
		/// </param>
		/// <param name="sigma">
		/// The strength of the prior (smaller is stronger for most
		/// standard priors) (default: 1.0)
		/// </param>
		/// <param name="epsilon">
		/// A second parameter to the prior (currently only used
		/// by the Huber prior)
		/// </param>
		public LinearClassifierFactory(IMinimizer<IDiffFunction> min, double tol, bool useSum, int prior, double sigma, double epsilon)
			: this(min, tol, useSum, new LogPrior(prior, sigma, epsilon))
		{
		}

		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> min, double tol, bool useSum, int prior, double sigma, double epsilon)
			: this(min, tol, useSum, new LogPrior(prior, sigma, epsilon))
		{
		}

		public LinearClassifierFactory(IMinimizer<IDiffFunction> min, double tol, bool useSum, LogPrior logPrior)
		{
			this.minimizerCreator = new _IFactory_188(min);
			this.Tol = tol;
			//this.useSum = useSum;
			this.logPrior = logPrior;
		}

		private sealed class _IFactory_188 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_188(IMinimizer<IDiffFunction> min)
			{
				this.min = min;
				this.serialVersionUID = -6439748445540743949L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				return min;
			}

			private readonly IMinimizer<IDiffFunction> min;
		}

		/// <summary>Create a factory that builds linear classifiers from training data.</summary>
		/// <remarks>
		/// Create a factory that builds linear classifiers from training data. This is the recommended constructor to
		/// bottom out with. Use of a minimizerCreator makes the classifier threadsafe.
		/// </remarks>
		/// <param name="minimizerCreator">
		/// A Factory for creating minimizers. If this is null, a standard quasi-Newton minimizer
		/// factory will be used.
		/// </param>
		/// <param name="tol">The convergence threshold for the minimization (default: 1e-4)</param>
		/// <param name="useSum">
		/// Asks to the optimizer to minimize the sum of the
		/// likelihoods of individual data items rather than their product (Klein and Manning 2001 WSD.)
		/// NOTE: this is currently ignored!!! At some point support for this option was deleted
		/// </param>
		/// <param name="logPrior">What kind of prior to use, this class specifies its type and hyperparameters.</param>
		public LinearClassifierFactory(IFactory<IMinimizer<IDiffFunction>> minimizerCreator, double tol, bool useSum, LogPrior logPrior)
		{
			if (minimizerCreator == null)
			{
				this.minimizerCreator = new LinearClassifierFactory.QNFactory(this);
			}
			else
			{
				this.minimizerCreator = minimizerCreator;
			}
			this.Tol = tol;
			//this.useSum = useSum;
			this.logPrior = logPrior;
		}

		/// <summary>Set the tolerance.</summary>
		/// <remarks>Set the tolerance.  1e-4 is the default.</remarks>
		public virtual void SetTol(double tol)
		{
			this.Tol = tol;
		}

		/// <summary>Set the prior.</summary>
		/// <param name="logPrior">
		/// One of the priors defined in
		/// <c>LogConditionalObjectiveFunction</c>
		/// .
		/// <c>LogPrior.QUADRATIC</c>
		/// is the default.
		/// </param>
		public virtual void SetPrior(LogPrior logPrior)
		{
			this.logPrior = logPrior;
		}

		/// <summary>
		/// Set the verbose flag for
		/// <see cref="Edu.Stanford.Nlp.Optimization.CGMinimizer"/>
		/// .
		/// <see langword="false"/>
		/// is the default.
		/// </summary>
		public virtual void SetVerbose(bool verbose)
		{
			this.verbose = verbose;
		}

		/// <summary>Sets the minimizer.</summary>
		/// <remarks>
		/// Sets the minimizer.
		/// <see cref="Edu.Stanford.Nlp.Optimization.QNMinimizer"/>
		/// is the default.
		/// </remarks>
		public virtual void SetMinimizerCreator(IFactory<IMinimizer<IDiffFunction>> minimizerCreator)
		{
			this.minimizerCreator = minimizerCreator;
		}

		/// <summary>
		/// Sets the epsilon value for
		/// <see cref="LogConditionalObjectiveFunction{L, F}"/>
		/// .
		/// </summary>
		public virtual void SetEpsilon(double eps)
		{
			logPrior.SetEpsilon(eps);
		}

		public virtual void SetSigma(double sigma)
		{
			logPrior.SetSigma(sigma);
		}

		public virtual double GetSigma()
		{
			return logPrior.GetSigma();
		}

		/// <summary>Sets the minimizer to QuasiNewton.</summary>
		/// <remarks>
		/// Sets the minimizer to QuasiNewton.
		/// <see cref="Edu.Stanford.Nlp.Optimization.QNMinimizer"/>
		/// is the default.
		/// </remarks>
		public virtual void UseQuasiNewton()
		{
			this.minimizerCreator = new LinearClassifierFactory.QNFactory(this);
		}

		public virtual void UseQuasiNewton(bool useRobust)
		{
			this.minimizerCreator = new _IFactory_280(this, useRobust);
		}

		private sealed class _IFactory_280 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_280(LinearClassifierFactory<L, F> _enclosing, bool useRobust)
			{
				this._enclosing = _enclosing;
				this.useRobust = useRobust;
				this.serialVersionUID = -9108222058357693242L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				QNMinimizer qnMinimizer = new QNMinimizer(this._enclosing._enclosing.mem, useRobust);
				if (!this._enclosing.verbose)
				{
					qnMinimizer.ShutUp();
				}
				return qnMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly bool useRobust;
		}

		public virtual void UseStochasticQN(double initialSMDGain, int stochasticBatchSize)
		{
			this.minimizerCreator = new _IFactory_294(this, initialSMDGain, stochasticBatchSize);
		}

		private sealed class _IFactory_294 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_294(LinearClassifierFactory<L, F> _enclosing, double initialSMDGain, int stochasticBatchSize)
			{
				this._enclosing = _enclosing;
				this.initialSMDGain = initialSMDGain;
				this.stochasticBatchSize = stochasticBatchSize;
				this.serialVersionUID = -7760753348350678588L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				SQNMinimizer<IDiffFunction> sqnMinimizer = new SQNMinimizer<IDiffFunction>(this._enclosing._enclosing.mem, initialSMDGain, stochasticBatchSize, false);
				if (!this._enclosing.verbose)
				{
					sqnMinimizer.ShutUp();
				}
				return sqnMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double initialSMDGain;

			private readonly int stochasticBatchSize;
		}

		public virtual void UseStochasticMetaDescent()
		{
			UseStochasticMetaDescent(0.1, 15, StochasticCalculateMethods.ExternalFiniteDifference, 20);
		}

		public virtual void UseStochasticMetaDescent(double initialSMDGain, int stochasticBatchSize, StochasticCalculateMethods stochasticMethod, int passes)
		{
			this.minimizerCreator = new _IFactory_313(this, initialSMDGain, stochasticBatchSize, stochasticMethod, passes);
		}

		private sealed class _IFactory_313 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_313(LinearClassifierFactory<L, F> _enclosing, double initialSMDGain, int stochasticBatchSize, StochasticCalculateMethods stochasticMethod, int passes)
			{
				this._enclosing = _enclosing;
				this.initialSMDGain = initialSMDGain;
				this.stochasticBatchSize = stochasticBatchSize;
				this.stochasticMethod = stochasticMethod;
				this.passes = passes;
				this.serialVersionUID = 6860437108371914482L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				SMDMinimizer<IDiffFunction> smdMinimizer = new SMDMinimizer<IDiffFunction>(initialSMDGain, stochasticBatchSize, stochasticMethod, passes);
				if (!this._enclosing.verbose)
				{
					smdMinimizer.ShutUp();
				}
				return smdMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double initialSMDGain;

			private readonly int stochasticBatchSize;

			private readonly StochasticCalculateMethods stochasticMethod;

			private readonly int passes;
		}

		public virtual void UseStochasticGradientDescent()
		{
			UseStochasticGradientDescent(0.1, 15);
		}

		public virtual void UseStochasticGradientDescent(double gainSGD, int stochasticBatchSize)
		{
			this.minimizerCreator = new _IFactory_331(this, gainSGD, stochasticBatchSize);
		}

		private sealed class _IFactory_331 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_331(LinearClassifierFactory<L, F> _enclosing, double gainSGD, int stochasticBatchSize)
			{
				this._enclosing = _enclosing;
				this.gainSGD = gainSGD;
				this.stochasticBatchSize = stochasticBatchSize;
				this.serialVersionUID = 2564615420955196299L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				InefficientSGDMinimizer<IDiffFunction> sgdMinimizer = new InefficientSGDMinimizer<IDiffFunction>(gainSGD, stochasticBatchSize);
				if (!this._enclosing.verbose)
				{
					sgdMinimizer.ShutUp();
				}
				return sgdMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double gainSGD;

			private readonly int stochasticBatchSize;
		}

		public virtual void UseInPlaceStochasticGradientDescent()
		{
			UseInPlaceStochasticGradientDescent(-1, -1, 1.0);
		}

		public virtual void UseInPlaceStochasticGradientDescent(int SGDPasses, int tuneSampleSize, double sigma)
		{
			this.minimizerCreator = new _IFactory_349(this, sigma, SGDPasses, tuneSampleSize);
		}

		private sealed class _IFactory_349 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_349(LinearClassifierFactory<L, F> _enclosing, double sigma, int SGDPasses, int tuneSampleSize)
			{
				this._enclosing = _enclosing;
				this.sigma = sigma;
				this.SGDPasses = SGDPasses;
				this.tuneSampleSize = tuneSampleSize;
				this.serialVersionUID = -5319225231759162616L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				SGDMinimizer<IDiffFunction> sgdMinimizer = new SGDMinimizer<IDiffFunction>(sigma, SGDPasses, tuneSampleSize);
				if (!this._enclosing.verbose)
				{
					sgdMinimizer.ShutUp();
				}
				return sgdMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double sigma;

			private readonly int SGDPasses;

			private readonly int tuneSampleSize;
		}

		public virtual void UseHybridMinimizerWithInPlaceSGD(int SGDPasses, int tuneSampleSize, double sigma)
		{
			this.minimizerCreator = new _IFactory_363(this, sigma, SGDPasses, tuneSampleSize);
		}

		private sealed class _IFactory_363 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_363(LinearClassifierFactory<L, F> _enclosing, double sigma, int SGDPasses, int tuneSampleSize)
			{
				this._enclosing = _enclosing;
				this.sigma = sigma;
				this.SGDPasses = SGDPasses;
				this.tuneSampleSize = tuneSampleSize;
				this.serialVersionUID = -3042400543337763144L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				SGDMinimizer<IDiffFunction> firstMinimizer = new SGDMinimizer<IDiffFunction>(sigma, SGDPasses, tuneSampleSize);
				QNMinimizer secondMinimizer = new QNMinimizer(this._enclosing.mem);
				if (!this._enclosing.verbose)
				{
					firstMinimizer.ShutUp();
					secondMinimizer.ShutUp();
				}
				return new HybridMinimizer(firstMinimizer, secondMinimizer, SGDPasses);
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double sigma;

			private readonly int SGDPasses;

			private readonly int tuneSampleSize;
		}

		public virtual void UseStochasticGradientDescentToQuasiNewton(double SGDGain, int batchSize, int sgdPasses, int qnPasses, int hessSamples, int QNMem, bool outputToFile)
		{
			this.minimizerCreator = new _IFactory_381(this, SGDGain, batchSize, sgdPasses, qnPasses, hessSamples, QNMem, outputToFile);
		}

		private sealed class _IFactory_381 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_381(LinearClassifierFactory<L, F> _enclosing, double SGDGain, int batchSize, int sgdPasses, int qnPasses, int hessSamples, int QNMem, bool outputToFile)
			{
				this._enclosing = _enclosing;
				this.SGDGain = SGDGain;
				this.batchSize = batchSize;
				this.sgdPasses = sgdPasses;
				this.qnPasses = qnPasses;
				this.hessSamples = hessSamples;
				this.QNMem = QNMem;
				this.outputToFile = outputToFile;
				this.serialVersionUID = 5823852936137599566L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				SGDToQNMinimizer sgdToQNMinimizer = new SGDToQNMinimizer(SGDGain, batchSize, sgdPasses, qnPasses, hessSamples, QNMem, outputToFile);
				if (!this._enclosing.verbose)
				{
					sgdToQNMinimizer.ShutUp();
				}
				return sgdToQNMinimizer;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;

			private readonly double SGDGain;

			private readonly int batchSize;

			private readonly int sgdPasses;

			private readonly int qnPasses;

			private readonly int hessSamples;

			private readonly int QNMem;

			private readonly bool outputToFile;
		}

		public virtual void UseHybridMinimizer()
		{
			UseHybridMinimizer(0.1, 15, StochasticCalculateMethods.ExternalFiniteDifference, 0);
		}

		public virtual void UseHybridMinimizer(double initialSMDGain, int stochasticBatchSize, StochasticCalculateMethods stochasticMethod, int cutoffIteration)
		{
			this.minimizerCreator = null;
		}

		/// <summary>
		/// Set the mem value for
		/// <see cref="Edu.Stanford.Nlp.Optimization.QNMinimizer"/>
		/// .
		/// Only used with quasi-newton minimization.  15 is the default.
		/// </summary>
		/// <param name="mem">
		/// Number of previous function/derivative evaluations to store
		/// to estimate second derivative.  Storing more previous evaluations
		/// improves training convergence speed.  This number can be very
		/// small, if memory conservation is the priority.  For large
		/// optimization systems (of 100,000-1,000,000 dimensions), setting this
		/// to 15 produces quite good results, but setting it to 50 can
		/// decrease the iteration count by about 20% over a value of 15.
		/// </param>
		public virtual void SetMem(int mem)
		{
			this.mem = mem;
		}

		/// <summary>
		/// Sets the minimizer to
		/// <see cref="Edu.Stanford.Nlp.Optimization.CGMinimizer"/>
		/// , with the passed
		/// <paramref name="verbose"/>
		/// flag.
		/// </summary>
		public virtual void UseConjugateGradientAscent(bool verbose)
		{
			this.verbose = verbose;
			UseConjugateGradientAscent();
		}

		/// <summary>
		/// Sets the minimizer to
		/// <see cref="Edu.Stanford.Nlp.Optimization.CGMinimizer"/>
		/// .
		/// </summary>
		public virtual void UseConjugateGradientAscent()
		{
			this.minimizerCreator = new _IFactory_440(this);
		}

		private sealed class _IFactory_440 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_440(LinearClassifierFactory<L, F> _enclosing)
			{
				this._enclosing = _enclosing;
				this.serialVersionUID = -561168861131879990L;
			}

			private const long serialVersionUID;

			public IMinimizer<IDiffFunction> Create()
			{
				return new CGMinimizer(!this._enclosing._enclosing.verbose);
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;
		}

		/// <summary>
		/// NOTE: nothing is actually done with this value!
		/// SetUseSum sets the
		/// <paramref name="useSum"/>
		/// flag: when turned on,
		/// the Summed Conditional Objective Function is used.  Otherwise, the
		/// LogConditionalObjectiveFunction is used.  The default is false.
		/// </summary>
		public virtual void SetUseSum(bool useSum)
		{
		}

		//this.useSum = useSum;
		private IMinimizer<IDiffFunction> GetMinimizer()
		{
			// Create a new minimizer
			IMinimizer<IDiffFunction> minimizer = minimizerCreator.Create();
			if (minimizer is IHasEvaluators)
			{
				((IHasEvaluators)minimizer).SetEvaluators(evalIters, evaluators);
			}
			return minimizer;
		}

		/// <summary>Adapt classifier (adjust the mean of Gaussian prior).</summary>
		/// <remarks>
		/// Adapt classifier (adjust the mean of Gaussian prior).
		/// Under construction -pichuan
		/// </remarks>
		/// <param name="origWeights">the original weights trained from the training data</param>
		/// <param name="adaptDataset">the Dataset used to adapt the trained weights</param>
		/// <returns>adapted weights</returns>
		public virtual double[][] AdaptWeights(double[][] origWeights, GeneralDataset<L, F> adaptDataset)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			logger.Info("adaptWeights in LinearClassifierFactory. increase weight dim only");
			double[][] newWeights = new double[][] {  };
			lock (typeof(Runtime))
			{
				System.Array.Copy(origWeights, 0, newWeights, 0, origWeights.Length);
			}
			AdaptedGaussianPriorObjectiveFunction<L, F> objective = new AdaptedGaussianPriorObjectiveFunction<L, F>(adaptDataset, logPrior, newWeights);
			double[] initial = objective.Initial();
			double[] weights = minimizer.Minimize(objective, Tol, initial);
			return objective.To2D(weights);
		}

		//Question: maybe the adaptWeights can be done just in LinearClassifier ?? (pichuan)
		protected internal override double[][] TrainWeights(GeneralDataset<L, F> dataset)
		{
			return TrainWeights(dataset, null);
		}

		public virtual double[][] TrainWeights(GeneralDataset<L, F> dataset, double[] initial)
		{
			return TrainWeights(dataset, initial, false);
		}

		public virtual double[][] TrainWeights(GeneralDataset<L, F> dataset, double[] initial, bool bypassTuneSigma)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			if (dataset is RVFDataset)
			{
				((RVFDataset<L, F>)dataset).EnsureRealValues();
			}
			double[] interimWeights = null;
			if (!bypassTuneSigma)
			{
				if (tuneSigmaHeldOut)
				{
					interimWeights = HeldOutSetSigma(dataset);
				}
				else
				{
					// the optimum interim weights from held-out training data have already been found.
					if (tuneSigmaCV)
					{
						CrossValidateSetSigma(dataset, folds);
					}
				}
			}
			// TODO: assign optimum interim weights as part of this process.
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(dataset, logPrior);
			if (initial == null && interimWeights != null && !retrainFromScratchAfterSigmaTuning)
			{
				//logger.info("## taking advantage of interim weights as starting point.");
				initial = interimWeights;
			}
			if (initial == null)
			{
				initial = objective.Initial();
			}
			double[] weights = minimizer.Minimize(objective, Tol, initial);
			return objective.To2D(weights);
		}

		/// <summary>IMPORTANT: dataset and biasedDataset must have same featureIndex, labelIndex</summary>
		public virtual IClassifier<L, F> TrainClassifierSemiSup(GeneralDataset<L, F> data, GeneralDataset<L, F> biasedData, double[][] confusionMatrix, double[] initial)
		{
			double[][] weights = TrainWeightsSemiSup(data, biasedData, confusionMatrix, initial);
			LinearClassifier<L, F> classifier = new LinearClassifier<L, F>(weights, data.FeatureIndex(), data.LabelIndex());
			return classifier;
		}

		public virtual double[][] TrainWeightsSemiSup(GeneralDataset<L, F> data, GeneralDataset<L, F> biasedData, double[][] confusionMatrix, double[] initial)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(data, new LogPrior(LogPrior.LogPriorType.Null));
			BiasedLogConditionalObjectiveFunction biasedObjective = new BiasedLogConditionalObjectiveFunction(biasedData, confusionMatrix, new LogPrior(LogPrior.LogPriorType.Null));
			SemiSupervisedLogConditionalObjectiveFunction semiSupObjective = new SemiSupervisedLogConditionalObjectiveFunction(objective, biasedObjective, logPrior);
			if (initial == null)
			{
				initial = objective.Initial();
			}
			double[] weights = minimizer.Minimize(semiSupObjective, Tol, initial);
			return objective.To2D(weights);
		}

		/// <summary>
		/// Trains the linear classifier using Generalized Expectation criteria as described in
		/// <tt>Generalized Expectation Criteria for Semi Supervised Learning of Conditional Random Fields</tt>, Mann and McCallum, ACL 2008.
		/// </summary>
		/// <remarks>
		/// Trains the linear classifier using Generalized Expectation criteria as described in
		/// <tt>Generalized Expectation Criteria for Semi Supervised Learning of Conditional Random Fields</tt>, Mann and McCallum, ACL 2008.
		/// The original algorithm is proposed for CRFs but has been adopted to LinearClassifier (which is a simpler special case of a CRF).
		/// IMPORTANT: the labeled features that are passed as an argument are assumed to be binary valued, although
		/// other features are allowed to be real valued.
		/// </remarks>
		public virtual LinearClassifier<L, F> TrainSemiSupGE<_T0>(GeneralDataset<L, F> labeledDataset, IList<_T0> unlabeledDataList, IList<F> GEFeatures, double convexComboCoeff)
			where _T0 : IDatum<L, F>
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(labeledDataset, new LogPrior(LogPrior.LogPriorType.Null));
			GeneralizedExpectationObjectiveFunction<L, F> geObjective = new GeneralizedExpectationObjectiveFunction<L, F>(labeledDataset, unlabeledDataList, GEFeatures);
			SemiSupervisedLogConditionalObjectiveFunction semiSupObjective = new SemiSupervisedLogConditionalObjectiveFunction(objective, geObjective, null, convexComboCoeff);
			double[] initial = objective.Initial();
			double[] weights = minimizer.Minimize(semiSupObjective, Tol, initial);
			return new LinearClassifier<L, F>(objective.To2D(weights), labeledDataset.FeatureIndex(), labeledDataset.LabelIndex());
		}

		/// <summary>
		/// Trains the linear classifier using Generalized Expectation criteria as described in
		/// <tt>Generalized Expectation Criteria for Semi Supervised Learning of Conditional Random Fields</tt>, Mann and McCallum, ACL 2008.
		/// </summary>
		/// <remarks>
		/// Trains the linear classifier using Generalized Expectation criteria as described in
		/// <tt>Generalized Expectation Criteria for Semi Supervised Learning of Conditional Random Fields</tt>, Mann and McCallum, ACL 2008.
		/// The original algorithm is proposed for CRFs but has been adopted to LinearClassifier (which is a simpler, special case of a CRF).
		/// Automatically discovers high precision, high frequency labeled features to be used as GE constraints.
		/// IMPORTANT: the current feature selector assumes the features are binary. The GE constraints assume the constraining features are binary anyway, although
		/// it doesn't make such assumptions about other features.
		/// </remarks>
		public virtual LinearClassifier<L, F> TrainSemiSupGE<_T0>(GeneralDataset<L, F> labeledDataset, IList<_T0> unlabeledDataList)
			where _T0 : IDatum<L, F>
		{
			IList<F> GEFeatures = GetHighPrecisionFeatures(labeledDataset, 0.9, 10);
			return TrainSemiSupGE(labeledDataset, unlabeledDataList, GEFeatures, 0.5);
		}

		public virtual LinearClassifier<L, F> TrainSemiSupGE<_T0>(GeneralDataset<L, F> labeledDataset, IList<_T0> unlabeledDataList, double convexComboCoeff)
			where _T0 : IDatum<L, F>
		{
			IList<F> GEFeatures = GetHighPrecisionFeatures(labeledDataset, 0.9, 10);
			return TrainSemiSupGE(labeledDataset, unlabeledDataList, GEFeatures, convexComboCoeff);
		}

		/// <summary>Returns a list of featured thresholded by minPrecision and sorted by their frequency of occurrence.</summary>
		/// <remarks>
		/// Returns a list of featured thresholded by minPrecision and sorted by their frequency of occurrence.
		/// precision in this case, is defined as the frequency of majority label over total frequency for that feature.
		/// </remarks>
		/// <returns>list of high precision features.</returns>
		private IList<F> GetHighPrecisionFeatures(GeneralDataset<L, F> dataset, double minPrecision, int maxNumFeatures)
		{
			int[][] feature2label = new int[][] {  };
			// shouldn't be necessary as Java zero fills arrays
			// for(int f = 0; f < dataset.numFeatures(); f++)
			//   Arrays.fill(feature2label[f],0);
			int[][] data = dataset.data;
			int[] labels = dataset.labels;
			for (int d = 0; d < data.Length; d++)
			{
				int label = labels[d];
				//System.out.println("datum id:"+d+" label id: "+label);
				if (data[d] != null)
				{
					//System.out.println(" number of features:"+data[d].length);
					for (int n = 0; n < data[d].Length; n++)
					{
						feature2label[data[d][n]][label]++;
					}
				}
			}
			ICounter<F> feature2freq = new ClassicCounter<F>();
			for (int f = 0; f < dataset.NumFeatures(); f++)
			{
				int maxF = ArrayMath.Max(feature2label[f]);
				int total = ArrayMath.Sum(feature2label[f]);
				double precision = ((double)maxF) / total;
				F feature = dataset.featureIndex.Get(f);
				if (precision >= minPrecision)
				{
					feature2freq.IncrementCount(feature, total);
				}
			}
			if (feature2freq.Size() > maxNumFeatures)
			{
				Counters.RetainTop(feature2freq, maxNumFeatures);
			}
			//for(F feature : feature2freq.keySet())
			//System.out.println(feature+" "+feature2freq.getCount(feature));
			//System.exit(0);
			return Counters.ToSortedList(feature2freq);
		}

		/// <summary>Train a classifier with a sigma tuned on a validation set.</summary>
		/// <returns>The constructed classifier</returns>
		public virtual LinearClassifier<L, F> TrainClassifierV(GeneralDataset<L, F> train, GeneralDataset<L, F> validation, double min, double max, bool accuracy)
		{
			labelIndex = train.LabelIndex();
			featureIndex = train.FeatureIndex();
			this.min = min;
			this.max = max;
			HeldOutSetSigma(train, validation);
			double[][] weights = TrainWeights(train);
			return new LinearClassifier<L, F>(weights, train.FeatureIndex(), train.LabelIndex());
		}

		/// <summary>Train a classifier with a sigma tuned on a validation set.</summary>
		/// <remarks>
		/// Train a classifier with a sigma tuned on a validation set.
		/// In this case we are fitting on the last 30% of the training data.
		/// </remarks>
		/// <param name="train">The data to train (and validate) on.</param>
		/// <returns>The constructed classifier</returns>
		public virtual LinearClassifier<L, F> TrainClassifierV(GeneralDataset<L, F> train, double min, double max, bool accuracy)
		{
			labelIndex = train.LabelIndex();
			featureIndex = train.FeatureIndex();
			tuneSigmaHeldOut = true;
			this.min = min;
			this.max = max;
			HeldOutSetSigma(train);
			double[][] weights = TrainWeights(train);
			return new LinearClassifier<L, F>(weights, train.FeatureIndex(), train.LabelIndex());
		}

		/// <summary>
		/// setTuneSigmaHeldOut sets the
		/// <c>tuneSigmaHeldOut</c>
		/// flag: when turned on,
		/// the sigma is tuned by means of held-out (70%-30%). Otherwise no tuning on sigma is done.
		/// The default is false.
		/// </summary>
		public virtual void SetTuneSigmaHeldOut()
		{
			tuneSigmaHeldOut = true;
			tuneSigmaCV = false;
		}

		/// <summary>
		/// setTuneSigmaCV sets the
		/// <c>tuneSigmaCV</c>
		/// flag: when turned on,
		/// the sigma is tuned by cross-validation. The number of folds is the parameter.
		/// If there is less data than the number of folds, leave-one-out is used.
		/// The default is false.
		/// </summary>
		public virtual void SetTuneSigmaCV(int folds)
		{
			tuneSigmaCV = true;
			tuneSigmaHeldOut = false;
			this.folds = folds;
		}

		/// <summary>NOTE: Nothing is actually done with this value.</summary>
		/// <remarks>
		/// NOTE: Nothing is actually done with this value.
		/// resetWeight sets the
		/// <c>restWeight</c>
		/// flag. This flag makes sense only if sigma is tuned:
		/// when turned on, the weights output by the tuneSigma method will be reset to zero when training the
		/// classifier.
		/// The default is false.
		/// </remarks>
		public virtual void ResetWeight()
		{
		}

		protected internal static readonly double[] sigmasToTry = new double[] { 0.5, 1.0, 2.0, 4.0, 10.0, 20.0, 100.0 };

		//resetWeight = true;
		/// <summary>
		/// Calls the method
		/// <see cref="LinearClassifierFactory{L, F}.CrossValidateSetSigma(GeneralDataset{L, F}, int)"/>
		/// with 5-fold cross-validation.
		/// </summary>
		/// <param name="dataset">the data set to optimize sigma on.</param>
		public virtual void CrossValidateSetSigma(GeneralDataset<L, F> dataset)
		{
			CrossValidateSetSigma(dataset, 5);
		}

		/// <summary>
		/// Calls the method
		/// <see cref="LinearClassifierFactory{L, F}.CrossValidateSetSigma(GeneralDataset{L, F}, int, Edu.Stanford.Nlp.Stats.IScorer{L}, Edu.Stanford.Nlp.Optimization.ILineSearcher)"/>
		/// with
		/// multi-class log-likelihood scoring (see
		/// <see cref="Edu.Stanford.Nlp.Stats.MultiClassAccuracyStats{L}"/>
		/// ) and golden-section line search
		/// (see
		/// <see cref="Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch"/>
		/// ).
		/// </summary>
		/// <param name="dataset">the data set to optimize sigma on.</param>
		public virtual void CrossValidateSetSigma(GeneralDataset<L, F> dataset, int kfold)
		{
			logger.Info("##you are here.");
			CrossValidateSetSigma(dataset, kfold, new MultiClassAccuracyStats<L>(MultiClassAccuracyStats.UseLoglikelihood), new GoldenSectionLineSearch(true, 1e-2, min, max));
		}

		public virtual void CrossValidateSetSigma(GeneralDataset<L, F> dataset, int kfold, IScorer<L> scorer)
		{
			CrossValidateSetSigma(dataset, kfold, scorer, new GoldenSectionLineSearch(true, 1e-2, min, max));
		}

		public virtual void CrossValidateSetSigma(GeneralDataset<L, F> dataset, int kfold, ILineSearcher minimizer)
		{
			CrossValidateSetSigma(dataset, kfold, new MultiClassAccuracyStats<L>(MultiClassAccuracyStats.UseLoglikelihood), minimizer);
		}

		/// <summary>
		/// Sets the sigma parameter to a value that optimizes the cross-validation score given by
		/// <paramref name="scorer"/>
		/// .  Search for an optimal value
		/// is carried out by
		/// <paramref name="minimizer"/>
		/// .
		/// </summary>
		/// <param name="dataset">the data set to optimize sigma on.</param>
		public virtual void CrossValidateSetSigma(GeneralDataset<L, F> dataset, int kfold, IScorer<L> scorer, ILineSearcher minimizer)
		{
			logger.Info("##in Cross Validate, folds = " + kfold);
			logger.Info("##Scorer is " + scorer);
			featureIndex = dataset.featureIndex;
			labelIndex = dataset.labelIndex;
			CrossValidator<L, F> crossValidator = new CrossValidator<L, F>(dataset, kfold);
			IToDoubleFunction<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>> scoreFn = null;
			// must of course bypass sigma tuning here.
			//System.out.println("score: "+score);
			IDoubleUnaryOperator negativeScorer = null;
			//sigma = sigmaToTry;
			double bestSigma = minimizer.Minimize(negativeScorer);
			logger.Info("##best sigma: " + bestSigma);
			SetSigma(bestSigma);
		}

		/// <summary>
		/// Set the
		/// <see cref="Edu.Stanford.Nlp.Optimization.ILineSearcher"/>
		/// to be used in
		/// <see cref="LinearClassifierFactory{L, F}.HeldOutSetSigma(GeneralDataset{L, F}, GeneralDataset{L, F})"/>
		/// .
		/// </summary>
		public virtual void SetHeldOutSearcher(ILineSearcher heldOutSearcher)
		{
			this.heldOutSearcher = heldOutSearcher;
		}

		private ILineSearcher heldOutSearcher;

		// = null;
		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> train)
		{
			Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> data = train.Split(0.3);
			return HeldOutSetSigma(data.First(), data.Second());
		}

		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> train, IScorer<L> scorer)
		{
			Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> data = train.Split(0.3);
			return HeldOutSetSigma(data.First(), data.Second(), scorer);
		}

		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> train, GeneralDataset<L, F> dev)
		{
			return HeldOutSetSigma(train, dev, new MultiClassAccuracyStats<L>(MultiClassAccuracyStats.UseLoglikelihood), heldOutSearcher == null ? new GoldenSectionLineSearch(true, 1e-2, min, max) : heldOutSearcher);
		}

		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> train, GeneralDataset<L, F> dev, IScorer<L> scorer)
		{
			return HeldOutSetSigma(train, dev, scorer, new GoldenSectionLineSearch(true, 1e-2, min, max));
		}

		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> train, GeneralDataset<L, F> dev, ILineSearcher minimizer)
		{
			return HeldOutSetSigma(train, dev, new MultiClassAccuracyStats<L>(MultiClassAccuracyStats.UseLoglikelihood), minimizer);
		}

		/// <summary>
		/// Sets the sigma parameter to a value that optimizes the held-out score given by
		/// <paramref name="scorer"/>
		/// .  Search for an
		/// optimal value is carried out by
		/// <paramref name="minimizer"/>
		/// dataset the data set to optimize sigma on. kfold
		/// </summary>
		/// <returns>an interim set of optimal weights: the weights</returns>
		public virtual double[] HeldOutSetSigma(GeneralDataset<L, F> trainSet, GeneralDataset<L, F> devSet, IScorer<L> scorer, ILineSearcher minimizer)
		{
			featureIndex = trainSet.featureIndex;
			labelIndex = trainSet.labelIndex;
			//double[] resultWeights = null;
			Timing timer = new Timing();
			LinearClassifierFactory.NegativeScorer negativeScorer = new LinearClassifierFactory.NegativeScorer(this, trainSet, devSet, scorer, timer);
			timer.Start();
			double bestSigma = minimizer.Minimize(negativeScorer);
			logger.Info("##best sigma: " + bestSigma);
			SetSigma(bestSigma);
			return ArrayUtils.Flatten(TrainWeights(trainSet, negativeScorer.weights, true));
		}

		internal class NegativeScorer : IDoubleUnaryOperator
		{
			public double[] weights;

			internal GeneralDataset<L, F> trainSet;

			internal GeneralDataset<L, F> devSet;

			internal IScorer<L> scorer;

			internal Timing timer;

			public NegativeScorer(LinearClassifierFactory<L, F> _enclosing, GeneralDataset<L, F> trainSet, GeneralDataset<L, F> devSet, IScorer<L> scorer, Timing timer)
				: base()
			{
				this._enclosing = _enclosing;
				// make sure it's actually the interim weights from best sigma
				// = null;
				this.trainSet = trainSet;
				this.devSet = devSet;
				this.scorer = scorer;
				this.timer = timer;
			}

			public virtual double ApplyAsDouble(double sigmaToTry)
			{
				double[][] weights2D;
				this._enclosing.SetSigma(sigmaToTry);
				weights2D = this._enclosing.TrainWeights(this.trainSet, this.weights, true);
				//bypass.
				this.weights = ArrayUtils.Flatten(weights2D);
				LinearClassifier<L, F> classifier = new LinearClassifier<L, F>(weights2D, this.trainSet.featureIndex, this.trainSet.labelIndex);
				double score = this.scorer.Score(classifier, this.devSet);
				//System.out.println("score: "+score);
				//System.out.print(".");
				LinearClassifierFactory.logger.Info("##sigma = " + this._enclosing.GetSigma() + " -> average Score: " + score);
				LinearClassifierFactory.logger.Info("##time elapsed: " + this.timer.Stop() + " milliseconds.");
				this.timer.Restart();
				return -score;
			}

			private readonly LinearClassifierFactory<L, F> _enclosing;
		}

		/// <summary>
		/// If set to true, then when training a classifier, after an optimal sigma is chosen a model is relearned from
		/// scratch.
		/// </summary>
		/// <remarks>
		/// If set to true, then when training a classifier, after an optimal sigma is chosen a model is relearned from
		/// scratch. If set to false (the default), then the model is updated from wherever it wound up in the sigma-tuning process.
		/// The latter is likely to be faster, but it's not clear which model will wind up better.
		/// </remarks>
		public virtual void SetRetrainFromScratchAfterSigmaTuning(bool retrainFromScratchAfterSigmaTuning)
		{
			this.retrainFromScratchAfterSigmaTuning = retrainFromScratchAfterSigmaTuning;
		}

		public virtual IClassifier<L, F> TrainClassifier(IEnumerable<IDatum<L, F>> dataIterable)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			IIndex<F> featureIndex = Generics.NewIndex();
			IIndex<L> labelIndex = Generics.NewIndex();
			foreach (IDatum<L, F> d in dataIterable)
			{
				labelIndex.Add(d.Label());
				featureIndex.AddAll(d.AsFeatures());
			}
			//If there are duplicates, it doesn't add them again.
			logger.Info(string.Format("Training linear classifier with %d features and %d labels", featureIndex.Size(), labelIndex.Size()));
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(dataIterable, logPrior, featureIndex, labelIndex);
			// [cdm 2014] Commented out next line. Why not use the logPrior set up previously and used at creation???
			// objective.setPrior(new LogPrior(LogPrior.LogPriorType.QUADRATIC));
			double[] initial = objective.Initial();
			double[] weights = minimizer.Minimize(objective, Tol, initial);
			LinearClassifier<L, F> classifier = new LinearClassifier<L, F>(objective.To2D(weights), featureIndex, labelIndex);
			return classifier;
		}

		public virtual IClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset, float[] dataWeights, LogPrior prior)
		{
			IMinimizer<IDiffFunction> minimizer = GetMinimizer();
			if (dataset is RVFDataset)
			{
				((RVFDataset<L, F>)dataset).EnsureRealValues();
			}
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(dataset, dataWeights, prior);
			double[] initial = objective.Initial();
			double[] weights = minimizer.Minimize(objective, Tol, initial);
			LinearClassifier<L, F> classifier = new LinearClassifier<L, F>(objective.To2D(weights), dataset.FeatureIndex(), dataset.LabelIndex());
			return classifier;
		}

		public override LinearClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset)
		{
			return TrainClassifier(dataset, null);
		}

		public virtual LinearClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset, double[] initial)
		{
			// Sanity check
			if (dataset is RVFDataset)
			{
				((RVFDataset<L, F>)dataset).EnsureRealValues();
			}
			if (initial != null)
			{
				foreach (double weight in initial)
				{
					if (double.IsNaN(weight) || double.IsInfinite(weight))
					{
						throw new ArgumentException("Initial weights are invalid!");
					}
				}
			}
			// Train classifier
			double[][] weights = TrainWeights(dataset, initial, false);
			LinearClassifier<L, F> classifier = new LinearClassifier<L, F>(weights, dataset.FeatureIndex(), dataset.LabelIndex());
			return classifier;
		}

		public virtual LinearClassifier<L, F> TrainClassifierWithInitialWeights(GeneralDataset<L, F> dataset, double[][] initialWeights2D)
		{
			double[] initialWeights = (initialWeights2D != null) ? ArrayUtils.Flatten(initialWeights2D) : null;
			return TrainClassifier(dataset, initialWeights);
		}

		public virtual LinearClassifier<L, F> TrainClassifierWithInitialWeights(GeneralDataset<L, F> dataset, LinearClassifier<L, F> initialClassifier)
		{
			double[][] initialWeights2D = (initialClassifier != null) ? initialClassifier.Weights() : null;
			return TrainClassifierWithInitialWeights(dataset, initialWeights2D);
		}

		/// <summary>
		/// Given the path to a file representing the text based serialization of a
		/// Linear Classifier, reconstitutes and returns that LinearClassifier.
		/// </summary>
		/// <remarks>
		/// Given the path to a file representing the text based serialization of a
		/// Linear Classifier, reconstitutes and returns that LinearClassifier.
		/// TODO: Leverage Index
		/// </remarks>
		public static LinearClassifier<string, string> LoadFromFilename(string file)
		{
			try
			{
				BufferedReader @in = IOUtils.ReaderFromString(file);
				// Format: read indices first, weights, then thresholds
				IIndex<string> labelIndex = HashIndex.LoadFromReader(@in);
				IIndex<string> featureIndex = HashIndex.LoadFromReader(@in);
				double[][] weights = new double[][] {  };
				int currLine = 1;
				string line = @in.ReadLine();
				while (line != null && line.Length > 0)
				{
					string[] tuples = line.Split(LinearClassifier.TextSerializationDelimiter);
					if (tuples.Length != 3)
					{
						throw new Exception("Error: incorrect number of tokens in weight specifier, line=" + currLine + " in file " + file);
					}
					currLine++;
					int feature = System.Convert.ToInt32(tuples[0]);
					int label = System.Convert.ToInt32(tuples[1]);
					double value = double.ParseDouble(tuples[2]);
					weights[feature][label] = value;
					line = @in.ReadLine();
				}
				// First line in thresholds is the number of thresholds
				int numThresholds = System.Convert.ToInt32(@in.ReadLine());
				double[] thresholds = new double[numThresholds];
				int curr = 0;
				while ((line = @in.ReadLine()) != null)
				{
					double tval = double.ParseDouble(line.Trim());
					thresholds[curr++] = tval;
				}
				@in.Close();
				LinearClassifier<string, string> classifier = new LinearClassifier<string, string>(weights, featureIndex, labelIndex);
				return classifier;
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Error in LinearClassifierFactory, loading from file=" + file, e);
			}
		}

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			this.evalIters = iters;
			this.evaluators = evaluators;
		}

		public virtual LinearClassifierFactory.LinearClassifierCreator<L, F> GetClassifierCreator(GeneralDataset<L, F> dataset)
		{
			//    LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(dataset, logPrior);
			return new LinearClassifierFactory.LinearClassifierCreator<L, F>(dataset.featureIndex, dataset.labelIndex);
		}

		public class LinearClassifierCreator<L, F> : IClassifierCreator, IProbabilisticClassifierCreator
		{
			internal LogConditionalObjectiveFunction objective;

			internal IIndex<F> featureIndex;

			internal IIndex<L> labelIndex;

			public LinearClassifierCreator(LogConditionalObjectiveFunction objective, IIndex<F> featureIndex, IIndex<L> labelIndex)
			{
				this.objective = objective;
				this.featureIndex = featureIndex;
				this.labelIndex = labelIndex;
			}

			public LinearClassifierCreator(IIndex<F> featureIndex, IIndex<L> labelIndex)
			{
				this.featureIndex = featureIndex;
				this.labelIndex = labelIndex;
			}

			public virtual LinearClassifier CreateLinearClassifier(double[] weights)
			{
				double[][] weights2D;
				if (objective != null)
				{
					weights2D = objective.To2D(weights);
				}
				else
				{
					weights2D = ArrayUtils.To2D(weights, featureIndex.Size(), labelIndex.Size());
				}
				return new LinearClassifier<L, F>(weights2D, featureIndex, labelIndex);
			}

			public virtual IClassifier CreateClassifier(double[] weights)
			{
				return CreateLinearClassifier(weights);
			}

			public virtual IProbabilisticClassifier CreateProbabilisticClassifier(double[] weights)
			{
				return CreateLinearClassifier(weights);
			}
		}
	}
}
