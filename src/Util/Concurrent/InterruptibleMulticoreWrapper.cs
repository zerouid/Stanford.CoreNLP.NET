using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	public class InterruptibleMulticoreWrapper<I, O> : MulticoreWrapper<I, O>
	{
		private readonly long timeout;

		public InterruptibleMulticoreWrapper(int numThreads, IThreadsafeProcessor<I, O> processor, bool orderResults, long timeout)
			: base(numThreads, processor, orderResults)
		{
			this.timeout = timeout;
		}

		protected internal override ThreadPoolExecutor BuildThreadPool(int nThreads)
		{
			return new InterruptibleMulticoreWrapper.FixedNamedThreadPoolExecutor(nThreads);
		}

		internal override int GetProcessor()
		{
			try
			{
				return (timeout < 0) ? idleProcessors.Take() : idleProcessors.Poll(timeout, TimeUnit.Milliseconds);
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
		}

		/// <summary>Shuts down the thread pool, returns when finished.</summary>
		/// <remarks>
		/// Shuts down the thread pool, returns when finished.
		/// <br />
		/// If <code>timeout</code> was set, then <code>join</code> waits at
		/// most <code>timeout</code> milliseconds for threads to finish.  If
		/// any fail to finish in that time, the threadpool is shutdownNow.
		/// After that, <code>join</code> continues to wait for the
		/// interrupted threads to finish, so if job do not obey
		/// interruptions, they can continue indefinitely regardless of the
		/// timeout.
		/// </remarks>
		/// <returns>
		/// a list of jobs which had never been started if
		/// <code>timeout</code> was reached, or an empty list if that did not
		/// happen.
		/// </returns>
		public virtual IList<I> JoinWithTimeout()
		{
			if (timeout < 0)
			{
				Join();
				return new List<I>();
			}
			// Make blocking calls to the last processes that are running
			if (!threadPool.IsShutdown())
			{
				try
				{
					IList<I> leftover = null;
					int i;
					for (i = nThreads; i > 0; --i)
					{
						if (idleProcessors.Poll(timeout, TimeUnit.Milliseconds) == null)
						{
							leftover = ShutdownNow();
							break;
						}
					}
					// if the poll hit a timeout, retake the remaining processors
					// so join() can guarantee the threads are finished
					if (i > 0)
					{
						for (; i > leftover.Count; --i)
						{
							idleProcessors.Take();
						}
						return leftover;
					}
					else
					{
						threadPool.Shutdown();
						// Sanity check. The threadpool should be done after iterating over
						// the processors.
						threadPool.AwaitTermination(10, TimeUnit.Seconds);
					}
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
			}
			return new List<I>();
		}

		/// <summary>Calls shutdownNow on the underlying ThreadPool.</summary>
		/// <remarks>
		/// Calls shutdownNow on the underlying ThreadPool.  In order for
		/// this to be useful, the jobs need to look for their thread being
		/// interrupted.  The job the thread is running needs to occasionally
		/// check Thread.interrupted() and throw an exception or otherwise
		/// clean up.
		/// </remarks>
		private IList<I> ShutdownNow()
		{
			IList<I> orphans = new List<I>();
			IList<IRunnable> runnables = threadPool.ShutdownNow();
			foreach (IRunnable runnable in runnables)
			{
				if (!(runnable is InterruptibleMulticoreWrapper.NamedTask))
				{
					throw new AssertionError("Should have gotten NamedTask");
				}
				InterruptibleMulticoreWrapper.NamedTask<I, O, object> task = (InterruptibleMulticoreWrapper.NamedTask<I, O, object>)runnable;
				orphans.Add(task.item);
			}
			return orphans;
		}

		/// <summary>
		/// After a shutdown request, await for the final termination of all
		/// threads.
		/// </summary>
		/// <remarks>
		/// After a shutdown request, await for the final termination of all
		/// threads.  Note that if the threads don't actually obey the
		/// interruption, this may take some time.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual bool AwaitTermination(long timeout, TimeUnit unit)
		{
			return threadPool.AwaitTermination(timeout, unit);
		}

		/// <summary>
		/// Internal class for a FutureTask which happens to know the input
		/// it represents.
		/// </summary>
		/// <remarks>
		/// Internal class for a FutureTask which happens to know the input
		/// it represents.  Useful for if the queue is interrupted with
		/// future jobs unallocated.  Since it is always created with
		/// CallableJob, we assume that is what it has been created with and
		/// extract the input.
		/// </remarks>
		private class NamedTask<I, O, V> : FutureTask<V>
		{
			internal readonly I item;

			internal NamedTask(ICallable<V> c)
				: base(c)
			{
				if (!(c is MulticoreWrapper.CallableJob))
				{
					throw new AssertionError("Should have gotten a CallableJob");
				}
				MulticoreWrapper.CallableJob<I, O> callable = (MulticoreWrapper.CallableJob<I, O>)c;
				item = callable.item;
			}
		}

		/// <summary>
		/// Internal class which represents a ThreadPoolExecutor whose future
		/// jobs know what their input was.
		/// </summary>
		/// <remarks>
		/// Internal class which represents a ThreadPoolExecutor whose future
		/// jobs know what their input was.  That way, if the ThreadPool is
		/// interrupted, it can return the jobs that were killed.
		/// <br />
		/// We know this will never be asked to provide new tasks for
		/// Runnable, just for Callable, so we throw an exception if asked to
		/// provide a new task for a Runnable.
		/// </remarks>
		private class FixedNamedThreadPoolExecutor<I, O> : ThreadPoolExecutor
		{
			internal FixedNamedThreadPoolExecutor(int nThreads)
				: base(nThreads, nThreads, 0L, TimeUnit.Milliseconds, new LinkedBlockingQueue<IRunnable>())
			{
			}

			protected override IRunnableFuture<T> NewTaskFor<V>(ICallable<V> c)
			{
				return new InterruptibleMulticoreWrapper.NamedTask<I, O, V>(c);
			}

			protected override IRunnableFuture<T> NewTaskFor<V>(IRunnable r, V v)
			{
				throw new NotSupportedException();
			}
		}
	}
}
