using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Loglinear.Model;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Management;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/26/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Abstract base of all the different kinds of optimizers. This exists to both facilitate sharing test between optimizers
	/// and to share certain basic bits of functionality useful for batch optimizers, like intelligent multi-thread management
	/// and user interrupt handling.
	/// </author>
	public abstract class AbstractBatchOptimizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AbstractBatchOptimizer));

		public virtual ConcatVector Optimize<T>(T[] dataset, AbstractDifferentiableFunction<T> fn)
		{
			return Optimize(dataset, fn, new ConcatVector(0), 0.0, 1.0e-5, false);
		}

		public virtual ConcatVector Optimize<T>(T[] dataset, AbstractDifferentiableFunction<T> fn, ConcatVector initialWeights, double l2regularization, double convergenceDerivativeNorm, bool quiet)
		{
			if (!quiet)
			{
				log.Info("\n**************\nBeginning training\n");
			}
			else
			{
				log.Info("[Beginning quiet training]");
			}
			AbstractBatchOptimizer.TrainingWorker<T> mainWorker = new AbstractBatchOptimizer.TrainingWorker<T>(this, dataset, fn, initialWeights, l2regularization, convergenceDerivativeNorm, quiet);
			new Thread(mainWorker).Start();
			BufferedReader br = new BufferedReader(new InputStreamReader(Runtime.@in));
			if (!quiet)
			{
				log.Info("NOTE: you can press any key (and maybe ENTER afterwards to jog stdin) to terminate learning early.");
				log.Info("The convergence criteria are quite aggressive if left uninterrupted, and will run for a while");
				log.Info("if left to their own devices.\n");
				while (true)
				{
					if (mainWorker.isFinished)
					{
						log.Info("training completed without interruption");
						return mainWorker.weights;
					}
					try
					{
						if (br.Ready())
						{
							log.Info("received quit command: quitting");
							log.Info("training completed by interruption");
							mainWorker.isFinished = true;
							return mainWorker.weights;
						}
					}
					catch (IOException e)
					{
						Sharpen.Runtime.PrintStackTrace(e);
					}
				}
			}
			else
			{
				while (!mainWorker.isFinished)
				{
					lock (mainWorker.naturalTerminationBarrier)
					{
						try
						{
							Sharpen.Runtime.Wait(mainWorker.naturalTerminationBarrier);
						}
						catch (Exception e)
						{
							throw new RuntimeInterruptedException(e);
						}
					}
				}
				log.Info("[Quiet training complete]");
				return mainWorker.weights;
			}
		}

		internal IList<AbstractBatchOptimizer.Constraint> constraints = new List<AbstractBatchOptimizer.Constraint>();

		/// <summary>This adds a constraint on the weight vector, that a certain component must be set to a sparse index=value</summary>
		/// <param name="component">the component to fix</param>
		/// <param name="index">the index of the fixed sparse component</param>
		/// <param name="value">the value to fix at</param>
		public virtual void AddSparseConstraint(int component, int index, double value)
		{
			constraints.Add(new AbstractBatchOptimizer.Constraint(component, index, value));
		}

		/// <summary>This adds a constraint on the weight vector, that a certain component must be set to a dense array</summary>
		/// <param name="component">the component to fix</param>
		/// <param name="arr">the dense array to set</param>
		public virtual void AddDenseConstraint(int component, double[] arr)
		{
			constraints.Add(new AbstractBatchOptimizer.Constraint(component, arr));
		}

		/// <summary>A way to record a constraint on the weight vector</summary>
		private class Constraint
		{
			internal int component;

			internal bool isSparse;

			internal int index;

			internal double value;

			internal double[] arr;

			public Constraint(int component, int index, double value)
			{
				isSparse = true;
				this.component = component;
				this.index = index;
				this.value = value;
			}

			public Constraint(int component, double[] arr)
			{
				isSparse = false;
				this.component = component;
				this.arr = arr;
			}

			public virtual void ApplyToWeights(ConcatVector weights)
			{
				if (isSparse)
				{
					weights.SetSparseComponent(component, index, value);
				}
				else
				{
					weights.SetDenseComponent(component, arr);
				}
			}

			public virtual void ApplyToDerivative(ConcatVector derivative)
			{
				if (isSparse)
				{
					derivative.SetSparseComponent(component, index, 0.0);
				}
				else
				{
					derivative.SetDenseComponent(component, new double[] { 0.0 });
				}
			}
		}

		/// <summary>This is the hook for subclassing batch optimizers to override in order to have their optimizer work.</summary>
		/// <param name="weights">the current weights (update these in place)</param>
		/// <param name="gradient">the gradient at these weights</param>
		/// <param name="logLikelihood">the log likelihood at these weights</param>
		/// <param name="state">any saved state the optimizer wants to keep and pass around during each optimization run</param>
		/// <param name="quiet">whether or not to dump output about progress to the console</param>
		/// <returns>whether or not we've converged</returns>
		public abstract bool UpdateWeights(ConcatVector weights, ConcatVector gradient, double logLikelihood, AbstractBatchOptimizer.OptimizationState state, bool quiet);

		/// <summary>This is subclassed by children to store any state they need to perform optimization</summary>
		protected internal abstract class OptimizationState
		{
			internal OptimizationState(AbstractBatchOptimizer _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly AbstractBatchOptimizer _enclosing;
		}

		/// <summary>This is called at the beginning of each batch optimization.</summary>
		/// <remarks>
		/// This is called at the beginning of each batch optimization. It should return a fresh OptimizationState object that
		/// will then be handed to updateWeights() on each update.
		/// </remarks>
		/// <param name="initialWeights">the initial weights for the optimizer to use</param>
		/// <returns>a fresh OptimizationState</returns>
		protected internal abstract AbstractBatchOptimizer.OptimizationState GetFreshOptimizationState(ConcatVector initialWeights);

		private class GradientWorker<T> : IRunnable
		{
			internal ConcatVector localDerivative;

			internal double localLogLikelihood = 0.0;

			internal AbstractBatchOptimizer.TrainingWorker mainWorker;

			internal int threadIdx;

			internal int numThreads;

			internal IList<T> queue;

			internal AbstractDifferentiableFunction<T> fn;

			internal ConcatVector weights;

			internal long jvmThreadId = 0;

			internal long finishedAtTime = 0;

			internal long cpuTimeRequired = 0;

			public GradientWorker(AbstractBatchOptimizer.TrainingWorker<T> mainWorker, int threadIdx, int numThreads, IList<T> queue, AbstractDifferentiableFunction<T> fn, ConcatVector weights)
			{
				// This is to help the dynamic re-balancing of work queues
				this.mainWorker = mainWorker;
				this.threadIdx = threadIdx;
				this.numThreads = numThreads;
				this.queue = queue;
				this.fn = fn;
				this.weights = weights;
				localDerivative = weights.NewEmptyClone();
			}

			public virtual void Run()
			{
				long startTime = ManagementFactory.GetThreadMXBean().GetThreadCpuTime(jvmThreadId);
				foreach (T datum in queue)
				{
					localLogLikelihood += fn.GetSummaryForInstance(datum, weights, localDerivative);
					// Check for user interrupt
					if (mainWorker.isFinished)
					{
						return;
					}
				}
				finishedAtTime = Runtime.CurrentTimeMillis();
				long endTime = ManagementFactory.GetThreadMXBean().GetThreadCpuTime(jvmThreadId);
				cpuTimeRequired = endTime - startTime;
			}
		}

		private class TrainingWorker<T> : IRunnable
		{
			internal ConcatVector weights;

			internal AbstractBatchOptimizer.OptimizationState optimizationState;

			internal bool isFinished = false;

			internal bool useThreads = Runtime.GetRuntime().AvailableProcessors() > 1;

			internal T[] dataset;

			internal AbstractDifferentiableFunction<T> fn;

			internal double l2regularization;

			internal double convergenceDerivativeNorm;

			internal bool quiet;

			internal readonly object naturalTerminationBarrier = new object();

			public TrainingWorker(AbstractBatchOptimizer _enclosing, T[] dataset, AbstractDifferentiableFunction<T> fn, ConcatVector initialWeights, double l2regularization, double convergenceDerivativeNorm, bool quiet)
			{
				this._enclosing = _enclosing;
				this.optimizationState = this._enclosing.GetFreshOptimizationState(initialWeights);
				this.weights = initialWeights.DeepClone();
				this.dataset = dataset;
				this.fn = fn;
				this.l2regularization = l2regularization;
				this.convergenceDerivativeNorm = convergenceDerivativeNorm;
				this.quiet = quiet;
			}

			/// <summary>
			/// This lets the system allocate work to threads evenly, which reduces the amount of blocking and can improve
			/// runtimes by 20% or more.
			/// </summary>
			/// <param name="datum">the datum to estimate work for</param>
			/// <returns>a work estimate, on a relative scale of single cpu wall time, for getting the gradient and log-likelihood</returns>
			private int EstimateRelativeRuntime(T datum)
			{
				if (datum is GraphicalModel)
				{
					int cost = 0;
					GraphicalModel model = (GraphicalModel)datum;
					foreach (GraphicalModel.Factor f in model.factors)
					{
						cost += f.featuresTable.CombinatorialNeighborStatesCount();
					}
					return cost;
				}
				else
				{
					return 1;
				}
			}

			public virtual void Run()
			{
				// Multithreading stuff
				int numThreads = Math.Max(1, Runtime.GetRuntime().AvailableProcessors());
				IList<T>[] queues = (IList<T>[])(new IList[numThreads]);
				Random r = new Random();
				// Allocate work to make estimated cost of work per thread as even as possible
				if (this.useThreads)
				{
					for (int i = 0; i < numThreads; i++)
					{
						queues[i] = new List<T>();
					}
					int[] queueEstimatedTotalCost = new int[numThreads];
					foreach (T datum in this.dataset)
					{
						int datumEstimatedCost = this.EstimateRelativeRuntime(datum);
						int minCostQueue = 0;
						for (int i_1 = 0; i_1 < numThreads; i_1++)
						{
							if (queueEstimatedTotalCost[i_1] < queueEstimatedTotalCost[minCostQueue])
							{
								minCostQueue = i_1;
							}
						}
						queueEstimatedTotalCost[minCostQueue] += datumEstimatedCost;
						queues[minCostQueue].Add(datum);
					}
				}
				while (!this.isFinished)
				{
					// Collect log-likelihood and derivatives
					long startTime = Runtime.CurrentTimeMillis();
					long threadWaiting = 0;
					ConcatVector derivative = this.weights.NewEmptyClone();
					double logLikelihood = 0.0;
					if (this.useThreads)
					{
						AbstractBatchOptimizer.GradientWorker[] workers = new AbstractBatchOptimizer.GradientWorker[numThreads];
						Thread[] threads = new Thread[numThreads];
						for (int i = 0; i < workers.Length; i++)
						{
							workers[i] = new AbstractBatchOptimizer.GradientWorker(this, i, numThreads, queues[i], this.fn, this.weights);
							threads[i] = new Thread(workers[i]);
							workers[i].jvmThreadId = threads[i].GetId();
							threads[i].Start();
						}
						// This is for logging
						long minFinishTime = long.MaxValue;
						long maxFinishTime = long.MinValue;
						// This is for re-balancing
						long minCPUTime = long.MaxValue;
						long maxCPUTime = long.MinValue;
						int slowestWorker = 0;
						int fastestWorker = 0;
						for (int i_1 = 0; i_1 < workers.Length; i_1++)
						{
							try
							{
								threads[i_1].Join();
							}
							catch (Exception e)
							{
								throw new RuntimeInterruptedException(e);
							}
							logLikelihood += workers[i_1].localLogLikelihood;
							derivative.AddVectorInPlace(workers[i_1].localDerivative, 1.0);
							if (workers[i_1].finishedAtTime < minFinishTime)
							{
								minFinishTime = workers[i_1].finishedAtTime;
							}
							if (workers[i_1].finishedAtTime > maxFinishTime)
							{
								maxFinishTime = workers[i_1].finishedAtTime;
							}
							if (workers[i_1].cpuTimeRequired < minCPUTime)
							{
								fastestWorker = i_1;
								minCPUTime = workers[i_1].cpuTimeRequired;
							}
							if (workers[i_1].cpuTimeRequired > maxCPUTime)
							{
								slowestWorker = i_1;
								maxCPUTime = workers[i_1].cpuTimeRequired;
							}
						}
						threadWaiting = maxFinishTime - minFinishTime;
						// Try to reallocate work dynamically to minimize waiting on subsequent rounds
						// Figure out the percentage of work represented by the waiting
						double waitingPercentage = (double)(maxCPUTime - minCPUTime) / (double)maxCPUTime;
						int needTransferItems = (int)Math.Floor(queues[slowestWorker].Count * waitingPercentage * 0.5);
						for (int i_2 = 0; i_2 < needTransferItems; i_2++)
						{
							int toTransfer = r.NextInt(queues[slowestWorker].Count);
							T datum = queues[slowestWorker][toTransfer];
							queues[slowestWorker].Remove(toTransfer);
							queues[fastestWorker].Add(datum);
						}
						// Check for user interrupt
						if (this.isFinished)
						{
							return;
						}
					}
					else
					{
						foreach (T datum in this.dataset)
						{
							System.Diagnostics.Debug.Assert((datum != null));
							logLikelihood += this.fn.GetSummaryForInstance(datum, this.weights, derivative);
							// Check for user interrupt
							if (this.isFinished)
							{
								return;
							}
						}
					}
					logLikelihood /= this.dataset.Length;
					derivative.MapInPlace(null);
					long gradientComputationTime = Runtime.CurrentTimeMillis() - startTime;
					// Regularization
					logLikelihood = logLikelihood - (this.l2regularization * this.weights.DotProduct(this.weights));
					derivative.AddVectorInPlace(this.weights, -2 * this.l2regularization);
					// Zero out the derivative on the components we're holding fixed
					foreach (AbstractBatchOptimizer.Constraint constraint in this._enclosing.constraints)
					{
						constraint.ApplyToDerivative(derivative);
					}
					// If our derivative is sufficiently small, we've converged
					double derivativeNorm = derivative.DotProduct(derivative);
					if (derivativeNorm < this.convergenceDerivativeNorm)
					{
						if (!this.quiet)
						{
							AbstractBatchOptimizer.log.Info("Derivative norm " + derivativeNorm + " < " + this.convergenceDerivativeNorm + ": quitting");
						}
						break;
					}
					// Do the actual computation
					if (!this.quiet)
					{
						AbstractBatchOptimizer.log.Info("[" + gradientComputationTime + " ms, threads waiting " + threadWaiting + " ms]");
					}
					bool converged = this._enclosing.UpdateWeights(this.weights, derivative, logLikelihood, this.optimizationState, this.quiet);
					// Apply constraints to the weights vector
					foreach (AbstractBatchOptimizer.Constraint constraint_1 in this._enclosing.constraints)
					{
						constraint_1.ApplyToWeights(this.weights);
					}
					if (converged)
					{
						break;
					}
				}
				lock (this.naturalTerminationBarrier)
				{
					Sharpen.Runtime.NotifyAll(this.naturalTerminationBarrier);
				}
				this.isFinished = true;
			}

			private readonly AbstractBatchOptimizer _enclosing;
		}
	}
}
