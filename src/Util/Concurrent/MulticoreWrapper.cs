using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>Provides convenient multicore processing for threadsafe objects.</summary>
	/// <remarks>
	/// Provides convenient multicore processing for threadsafe objects. Objects that can
	/// be wrapped by MulticoreWrapper must implement the ThreadsafeProcessor interface.
	/// See edu.stanford.nlp.util.concurrent.MulticoreWrapperTest and
	/// edu.stanford.nlp.tagger.maxent.documentation.MulticoreWrapperDemo for examples of use.
	/// TODO(spenceg): This code does **not** support multiple consumers, i.e., multi-threaded calls
	/// to peek() and poll().
	/// </remarks>
	/// <author>Spence Green</author>
	/// <?/>
	/// <?/>
	public class MulticoreWrapper<I, O>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.Concurrent.MulticoreWrapper));

		internal readonly int nThreads;

		private int submittedItemCounter = 0;

		private int returnedItemCounter = -1;

		private readonly bool orderResults;

		private readonly IDictionary<int, O> outputQueue;

		internal readonly ThreadPoolExecutor threadPool;

		internal readonly IBlockingQueue<int> idleProcessors;

		private readonly IList<IThreadsafeProcessor<I, O>> processorList;

		private readonly MulticoreWrapper.IJobCallback<O> callback;

		/// <summary>Constructor.</summary>
		/// <param name="nThreads">
		/// If less than or equal to 0, then automatically determine the number
		/// of threads. Otherwise, the size of the underlying threadpool.
		/// </param>
		/// <param name="processor"/>
		public MulticoreWrapper(int nThreads, IThreadsafeProcessor<I, O> processor)
			: this(nThreads, processor, true)
		{
		}

		/// <summary>Constructor.</summary>
		/// <param name="numThreads">
		/// -- if less than or equal to 0, then automatically determine the number
		/// of threads. Otherwise, the size of the underlying threadpool.
		/// </param>
		/// <param name="processor"/>
		/// <param name="orderResults">
		/// -- If true, return results in the order submitted. Otherwise, return results
		/// as they become available.
		/// </param>
		public MulticoreWrapper(int numThreads, IThreadsafeProcessor<I, O> processor, bool orderResults)
		{
			// Which id was the last id returned.  Only meaningful in the case
			// of a queue where output order matters.
			//  private final ExecutorCompletionService<Integer> queue;
			nThreads = numThreads <= 0 ? Runtime.GetRuntime().AvailableProcessors() : numThreads;
			this.orderResults = orderResults;
			outputQueue = new ConcurrentHashMap<int, O>(2 * nThreads);
			threadPool = BuildThreadPool(nThreads);
			//    queue = new ExecutorCompletionService<Integer>(threadPool);
			idleProcessors = new ArrayBlockingQueue<int>(nThreads, false);
			callback = null;
			// Sanity check: Fixed thread pool so prevent timeouts.
			// Default should be false
			threadPool.AllowCoreThreadTimeOut(false);
			threadPool.PrestartAllCoreThreads();
			// Setup the processors, one per thread
			IList<IThreadsafeProcessor<I, O>> procList = new List<IThreadsafeProcessor<I, O>>(nThreads);
			procList.Add(processor);
			idleProcessors.Add(0);
			for (int i = 1; i < nThreads; ++i)
			{
				procList.Add(processor.NewInstance());
				idleProcessors.Add(i);
			}
			processorList = Java.Util.Collections.UnmodifiableList(procList);
		}

		protected internal virtual ThreadPoolExecutor BuildThreadPool(int nThreads)
		{
			return (ThreadPoolExecutor)Executors.NewFixedThreadPool(nThreads);
		}

		public virtual int NThreads()
		{
			return nThreads;
		}

		/// <summary>Return status information about the underlying threadpool.</summary>
		public override string ToString()
		{
			return string.Format("active: %d/%d  submitted: %d  completed: %d  input_q: %d  output_q: %d  idle_q: %d", threadPool.GetActiveCount(), threadPool.GetPoolSize(), threadPool.GetTaskCount(), threadPool.GetCompletedTaskCount(), threadPool.GetQueue
				().Count, outputQueue.Count, idleProcessors.Count);
		}

		/// <summary>Allocate instance to a process and return.</summary>
		/// <remarks>
		/// Allocate instance to a process and return. This call blocks until item
		/// can be assigned to a thread.
		/// </remarks>
		/// <param name="item">Input to a Processor</param>
		/// <exception cref="Java.Util.Concurrent.RejectedExecutionException">
		/// -- A RuntimeException when there is an
		/// uncaught exception in the queue. Resolution is for the calling class to shutdown
		/// the wrapper and create a new threadpool.
		/// </exception>
		public virtual void Put(I item)
		{
			lock (this)
			{
				int procId = GetProcessor();
				if (procId == null)
				{
					throw new RejectedExecutionException("Couldn't submit item to threadpool: " + item);
				}
				int itemId = submittedItemCounter++;
				MulticoreWrapper.CallableJob<I, O> job = new MulticoreWrapper.CallableJob<I, O>(item, itemId, processorList[procId], procId, callback);
				threadPool.Submit(job);
			}
		}

		/// <summary>Returns the next available thread id.</summary>
		/// <remarks>
		/// Returns the next available thread id.  Subclasses may wish to
		/// override this, for example if they implement a timeout
		/// </remarks>
		internal virtual int GetProcessor()
		{
			try
			{
				return idleProcessors.Take();
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
		}

		/// <summary>
		/// Wait for all threads to finish, then destroy the pool of
		/// worker threads so that the main thread can shutdown.
		/// </summary>
		public virtual void Join()
		{
			Join(true);
		}

		/// <summary>Wait for all threads to finish.</summary>
		/// <param name="destroyThreadpool">
		/// -- if true, then destroy the worker threads
		/// so that the main thread can shutdown.
		/// </param>
		public virtual void Join(bool destroyThreadpool)
		{
			// Make blocking calls to the last processes that are running
			if (!threadPool.IsShutdown())
			{
				try
				{
					for (int i = nThreads; i > 0; --i)
					{
						idleProcessors.Take();
					}
					if (destroyThreadpool)
					{
						threadPool.Shutdown();
						// Sanity check. The threadpool should be done after iterating over
						// the processors.
						threadPool.AwaitTermination(10, TimeUnit.Seconds);
					}
					else
					{
						// Repopulate the list of processors
						for (int i_1 = 0; i_1 < nThreads; ++i_1)
						{
							idleProcessors.Put(i_1);
						}
					}
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
		}

		/// <summary>Indicates whether or not a new result is available.</summary>
		/// <returns>true if a new result is available, false otherwise.</returns>
		public virtual bool Peek()
		{
			if (outputQueue.IsEmpty())
			{
				return false;
			}
			else
			{
				return orderResults ? outputQueue.Contains(returnedItemCounter + 1) : true;
			}
		}

		/// <summary>Returns the next available result.</summary>
		/// <returns>the next completed result, or null if no result is available</returns>
		public virtual O Poll()
		{
			if (!Peek())
			{
				return null;
			}
			returnedItemCounter++;
			int itemIndex = orderResults ? returnedItemCounter : outputQueue.Keys.GetEnumerator().Current;
			return Sharpen.Collections.Remove(outputQueue, itemIndex);
		}

		/// <summary>Internal class for a result when a CallableJob completes.</summary>
		/// <author>Spence Green</author>
		/// <?/>
		private interface IJobCallback<O>
		{
			void Call(MulticoreWrapper.QueueItem<O> result, int processorId);
		}

		/// <summary>Internal class for adding a job to the thread pool.</summary>
		/// <author>Spence Green</author>
		/// <?/>
		/// <?/>
		internal class CallableJob<I, O> : ICallable<int>
		{
			internal readonly I item;

			private readonly int itemId;

			private readonly IThreadsafeProcessor<I, O> processor;

			private readonly int processorId;

			private readonly MulticoreWrapper.IJobCallback<O> callback;

			public CallableJob(I item, int itemId, IThreadsafeProcessor<I, O> processor, int processorId, MulticoreWrapper.IJobCallback<O> callback)
			{
				this.item = item;
				this.itemId = itemId;
				this.processor = processor;
				this.processorId = processorId;
				this.callback = callback;
			}

			public virtual int Call()
			{
				O result = null;
				try
				{
					result = processor.Process(item);
				}
				catch (Exception e)
				{
					log.Warn(e);
				}
				// Hope that the consumer knows how to handle null!
				MulticoreWrapper.QueueItem<O> output = new MulticoreWrapper.QueueItem<O>(result, itemId);
				callback.Call(output, processorId);
				return itemId;
			}
		}

		/// <summary>Internal class for storing results of type O in a min queue.</summary>
		/// <author>Spence Green</author>
		/// <?/>
		private class QueueItem<O> : IComparable<MulticoreWrapper.QueueItem<O>>
		{
			public readonly int id;

			public readonly O item;

			public QueueItem(O item, int id)
			{
				this.item = item;
				this.id = id;
			}

			public virtual int CompareTo(MulticoreWrapper.QueueItem<O> other)
			{
				return int.Compare(this.id, other.id);
			}

			public override bool Equals(object other)
			{
				if (other == this)
				{
					return true;
				}
				if (!(other is MulticoreWrapper.QueueItem))
				{
					return false;
				}
				MulticoreWrapper.QueueItem<O> otherQueue = (MulticoreWrapper.QueueItem<O>)other;
				return this.id == otherQueue.id;
			}

			public override int GetHashCode()
			{
				return id;
			}
		}
		// end static class QueueItem
	}
}
