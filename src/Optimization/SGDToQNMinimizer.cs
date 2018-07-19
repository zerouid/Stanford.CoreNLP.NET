using System;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// Stochastic Gradient Descent To Quasi Newton Minimizer
	/// An experimental minimizer which takes a stochastic function (one implementing AbstractStochasticCachingDiffFunction)
	/// and executes SGD for the first couple passes.
	/// </summary>
	/// <remarks>
	/// Stochastic Gradient Descent To Quasi Newton Minimizer
	/// An experimental minimizer which takes a stochastic function (one implementing AbstractStochasticCachingDiffFunction)
	/// and executes SGD for the first couple passes.  During the final iterations a series of approximate hessian vector
	/// products are built up.  These are then passed to the QNminimizer so that it can start right up without the typical
	/// delay.
	/// Note [2012] The basic idea here is good, but the original ScaledSGDMinimizer wasn't efficient, and so this would
	/// be much more useful if rewritten to use the good StochasticInPlaceMinimizer instead.
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	[System.Serializable]
	public class SGDToQNMinimizer : IMinimizer<IDiffFunction>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SGDToQNMinimizer));

		private const long serialVersionUID = -7551807670291500396L;

		private readonly int bSize;

		private bool quiet = false;

		public bool outputIterationsToFile = false;

		public double gain = 0.1;

		public int SGDPasses = -1;

		public int QNPasses = -1;

		private readonly int hessSampleSize;

		private readonly int QNMem;

		public SGDToQNMinimizer(double SGDGain, int batchSize, int SGDPasses, int QNPasses)
			: this(SGDGain, batchSize, SGDPasses, QNPasses, 50, 10)
		{
		}

		public SGDToQNMinimizer(double SGDGain, int batchSize, int sgdPasses, int qnPasses, int hessSamples, int QNMem)
			: this(SGDGain, batchSize, sgdPasses, qnPasses, hessSamples, QNMem, false)
		{
		}

		public SGDToQNMinimizer(double SGDGain, int batchSize, int sgdPasses, int qnPasses, int hessSamples, int QNMem, bool outputToFile)
		{
			// private int k;
			// public int outputFrequency = 10;
			// private List<double[]> gradList = null;
			// private List<double[]> yList = null;
			// private List<double[]> sList = null;
			// private List<double[]> tmpYList = null;
			// private List<double[]> tmpSList = null;
			// private int memory = 5;
			this.gain = SGDGain;
			this.bSize = batchSize;
			this.SGDPasses = sgdPasses;
			this.QNPasses = qnPasses;
			this.hessSampleSize = hessSamples;
			this.QNMem = QNMem;
			this.outputIterationsToFile = outputToFile;
		}

		public virtual void ShutUp()
		{
			this.quiet = true;
		}

		protected internal virtual string GetName()
		{
			int g = (int)(gain * 1000);
			return "SGD2QN" + bSize + "_g" + g;
		}

		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial, int maxIterations)
		{
			Sayln("SGDToQNMinimizer called on function of " + function.DomainDimension() + " variables;");
			// check for stochastic derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffFunction dfunction = (AbstractStochasticCachingDiffFunction)function;
			dfunction.method = StochasticCalculateMethods.GradientOnly;
			ScaledSGDMinimizer sgd = new ScaledSGDMinimizer(this.gain, this.bSize, this.SGDPasses, 1, this.outputIterationsToFile);
			QNMinimizer qn = new QNMinimizer(this.QNMem, true);
			double[] x = sgd.Minimize(dfunction, functionTolerance, initial, this.SGDPasses);
			QNMinimizer.QNInfo qnInfo = new QNMinimizer.QNInfo(this, sgd.sList, sgd.yList);
			qnInfo.d = sgd.diag;
			qn.Minimize(dfunction, functionTolerance, x, this.QNPasses, qnInfo);
			log.Info(string.Empty);
			log.Info("Minimization complete.");
			log.Info(string.Empty);
			log.Info("Exiting for Debug");
			return x;
		}

		private void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}

		private void Say(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}
	}
}
