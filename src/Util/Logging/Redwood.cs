using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Atomic;
using Java.Util.Concurrent.Locks;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A hierarchical channel-based logger.</summary>
	/// <remarks>
	/// A hierarchical channel-based logger. Log messages are arranged hierarchically by depth
	/// (e.g., main-&gt;tagging-&gt;sentence 2) using the startTrack() and endTrack() methods.
	/// Furthermore, messages can be flagged with a number of channels, which allow filtering by channel.
	/// Log levels are implemented as channels (ERROR, WARNING, etc.).
	/// <p>
	/// Details on the handlers used are documented in their respective classes, which all implement
	/// <see cref="LogRecordHandler"/>
	/// .
	/// New handlers should implement this class.
	/// Details on configuring Redwood can be found in the
	/// <see cref="RedwoodConfiguration"/>
	/// class.
	/// New configuration methods should be implemented in this class, following the standard
	/// builder paradigm.
	/// <p>
	/// There is a <a href="https://nlp.stanford.edu/software/Redwood.pdf">tutorial on Redwood</a> on the
	/// NLP website.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	/// <author>David McClosky</author>
	public class Redwood
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Edu.Stanford.Nlp.Util.Logging.Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.Logging.Redwood));

		public static readonly Redwood.Flag Err = Redwood.Flag.Error;

		public static readonly Redwood.Flag Warn = Redwood.Flag.Warn;

		public static readonly Redwood.Flag Dbg = Redwood.Flag.Debug;

		public static readonly Redwood.Flag Force = Redwood.Flag.Force;

		public static readonly Redwood.Flag Stdout = Redwood.Flag.Stdout;

		public static readonly Redwood.Flag Stderr = Redwood.Flag.Stderr;

		/// <summary>The real System.out stream</summary>
		protected internal static readonly TextWriter realSysOut = System.Console.Out;

		/// <summary>The real System.err stream</summary>
		protected internal static readonly TextWriter realSysErr = System.Console.Error;

		/// <summary>The tree of handlers</summary>
		private static Redwood.RecordHandlerTree handlers = new Redwood.RecordHandlerTree();

		/// <summary>The current depth of the logger</summary>
		private static int depth = 0;

		/// <summary>
		/// The stack of track titles, for consistency checking
		/// the endTrack() call
		/// </summary>
		private static readonly Stack<string> titleStack = new Stack<string>();

		/// <summary>Signals that no more log messages should be accepted by Redwood</summary>
		private static bool isClosed = false;

		/// <summary>Queue of tasks to be run in various threads</summary>
		private static readonly IDictionary<long, IQueue<IRunnable>> threadedLogQueue = new Dictionary<long, IQueue<IRunnable>>();

		/// <summary>Thread id which currently has control of the Redwood</summary>
		private static long currentThread = -1L;

		/// <summary>
		/// Threads which have something they wish to log, but do not yet
		/// have control of Redwood
		/// </summary>
		private static readonly IQueue<long> threadsWaiting = new LinkedList<long>();

		/// <summary>Indicator that messages are coming from multiple threads</summary>
		private static bool isThreaded = false;

		/// <summary>Synchronization</summary>
		private static readonly ReentrantLock control = new ReentrantLock();

		private Redwood()
		{
		}

		/*
		---------------------------------------------------------
		VARIABLES
		---------------------------------------------------------
		*/
		// -- UTILITIES --
		// -- STREAMS --
		// -- BASIC LOGGING --
		// -- THREADED ENVIRONMENT --
		// Don't replace with Generics.newHashMap()! Classloader goes haywire
		// static class
		/*
		---------------------------------------------------------
		HELPER METHODS
		---------------------------------------------------------
		*/
		private static void QueueTask(long threadId, IRunnable toRun)
		{
			System.Diagnostics.Debug.Assert(control.IsHeldByCurrentThread());
			System.Diagnostics.Debug.Assert(threadId != currentThread);
			//(get queue)
			if (!threadedLogQueue.Contains(threadId))
			{
				threadedLogQueue[threadId] = new LinkedList<IRunnable>();
			}
			IQueue<IRunnable> threadLogQueue = threadedLogQueue[threadId];
			//(add to queue)
			threadLogQueue.Offer(toRun);
			//(register this thread as waiting)
			if (!threadsWaiting.Contains(threadId))
			{
				threadsWaiting.Offer(threadId);
				System.Diagnostics.Debug.Assert(threadedLogQueue[threadId] != null && !threadedLogQueue[threadId].IsEmpty());
			}
		}

		private static void ReleaseThreadControl(long threadId)
		{
			System.Diagnostics.Debug.Assert(!isThreaded || control.IsHeldByCurrentThread());
			System.Diagnostics.Debug.Assert(currentThread < 0L || currentThread == threadId);
			//(release control)
			currentThread = -1L;
		}

		private static void AttemptThreadControl(long threadId, IRunnable r)
		{
			//(get lock)
			bool tookLock = false;
			if (!control.IsHeldByCurrentThread())
			{
				control.Lock();
				tookLock = true;
			}
			//(perform action)
			AttemptThreadControlThreadsafe(threadId);
			if (threadId == currentThread)
			{
				r.Run();
			}
			else
			{
				QueueTask(threadId, r);
			}
			//(release lock)
			System.Diagnostics.Debug.Assert(control.IsHeldByCurrentThread());
			if (tookLock)
			{
				control.Unlock();
			}
		}

		private static void AttemptThreadControlThreadsafe(long threadId)
		{
			//--Assertions
			System.Diagnostics.Debug.Assert(control.IsHeldByCurrentThread());
			//--Update Current Thread
			bool hopeless = true;
			if (currentThread < 0L)
			{
				//(case: no one has control)
				if (threadsWaiting.IsEmpty())
				{
					currentThread = threadId;
				}
				else
				{
					currentThread = threadsWaiting.Poll();
					hopeless = false;
					System.Diagnostics.Debug.Assert(threadedLogQueue[currentThread] == null || !threadedLogQueue[currentThread].IsEmpty());
				}
			}
			else
			{
				if (currentThread == threadId)
				{
					//(case: we have control)
					threadsWaiting.Remove(currentThread);
				}
				else
				{
					if (currentThread >= 0L)
					{
						//(case: someone else has control
						threadsWaiting.Remove(currentThread);
					}
					else
					{
						System.Diagnostics.Debug.Assert(false);
					}
				}
			}
			//--Clear Backlog
			long activeThread = currentThread;
			IQueue<IRunnable> backlog = threadedLogQueue[currentThread];
			if (backlog != null)
			{
				//(run backlog)
				while (!backlog.IsEmpty() && currentThread >= 0L)
				{
					backlog.Poll().Run();
				}
				//(requeue, if applicable)
				if (currentThread < 0L && !backlog.IsEmpty())
				{
					threadsWaiting.Offer(activeThread);
					hopeless = false;
				}
			}
			//--Recursive
			if (!hopeless && currentThread != threadId)
			{
				AttemptThreadControlThreadsafe(threadId);
			}
			System.Diagnostics.Debug.Assert(!threadsWaiting.Contains(currentThread));
			System.Diagnostics.Debug.Assert(control.IsHeldByCurrentThread());
		}

		protected internal static Redwood.RecordHandlerTree RootHandler()
		{
			return handlers;
		}

		/// <summary>
		/// Remove all log handlers from Redwood, presumably in order to
		/// construct a custom pipeline afterwards
		/// </summary>
		protected internal static void ClearHandlers()
		{
			handlers = new Redwood.RecordHandlerTree();
		}

		/// <summary>Get a handler based on its class.</summary>
		/// <param name="clazz">
		/// The class of the Handler to return.
		/// If multiple Handlers exist, the first one is returned.
		/// </param>
		/// <?/>
		/// <returns>The handler matching the class name.</returns>
		[Obsolete]
		private static E GetHandler<E>()
			where E : LogRecordHandler
		{
			System.Type clazz = typeof(E);
			foreach (LogRecordHandler cand in handlers)
			{
				if (clazz == cand.GetType())
				{
					return (E)cand;
				}
			}
			return null;
		}

		/// <summary>
		/// Captures System.out and System.err and redirects them
		/// to Redwood logging.
		/// </summary>
		/// <param name="captureOut">True is System.out should be captured</param>
		/// <param name="captureErr">True if System.err should be captured</param>
		protected internal static void CaptureSystemStreams(bool captureOut, bool captureErr)
		{
			if (captureOut)
			{
				Runtime.SetOut(new RedwoodPrintStream(Stdout, realSysOut));
			}
			else
			{
				Runtime.SetOut(realSysOut);
			}
			if (captureErr)
			{
				Runtime.SetErr(new RedwoodPrintStream(Stderr, realSysErr));
			}
			else
			{
				Runtime.SetErr(realSysErr);
			}
		}

		/// <summary>Restores System.out and System.err to their original values</summary>
		protected internal static void RestoreSystemStreams()
		{
			Runtime.SetOut(realSysOut);
			Runtime.SetErr(realSysErr);
		}

		/*
		---------------------------------------------------------
		TRUE PUBLIC FACING METHODS
		---------------------------------------------------------
		*/
		/// <summary>Log a message.</summary>
		/// <remarks>
		/// Log a message. The last argument to this object is the message to log
		/// (usually a String); the first arguments are the channels to log to.
		/// For example:
		/// log(Redwood.ERR,"tag","this message is tagged with ERROR and tag")
		/// </remarks>
		/// <param name="args">The last argument is the message; the first arguments are the channels.</param>
		public static void Log(params object[] args)
		{
			//--Argument Check
			if (args.Length == 0)
			{
				return;
			}
			if (isClosed)
			{
				return;
			}
			//--Create Record
			object content = args[args.Length - 1];
			object[] tags = new object[args.Length - 1];
			System.Array.Copy(args, 0, tags, 0, args.Length - 1);
			long timestamp = Runtime.CurrentTimeMillis();
			//--Handle Record
			if (isThreaded)
			{
				//(case: multithreaded)
				IRunnable log = null;
				long threadId = Thread.CurrentThread().GetId();
				AttemptThreadControl(threadId, log);
			}
			else
			{
				//(case: no threading)
				Redwood.Record toPass = new Redwood.Record(content, tags, depth, timestamp);
				handlers.Process(toPass, Redwood.MessageType.Simple, depth, toPass.timesstamp);
			}
		}

		/// <summary>The Redwood equivalent to printf().</summary>
		/// <param name="format">The format string, as per java's Formatter.format() object.</param>
		/// <param name="args">The arguments to format.</param>
		public static void Logf(string format, params object[] args)
		{
			Log((ISupplier<string>)null);
		}

		/// <summary>The Redwood equivalent to printf(), with a logging level.</summary>
		/// <remarks>
		/// The Redwood equivalent to printf(), with a logging level.
		/// For including more channels, use
		/// <see cref="RedwoodChannels"/>
		/// .
		/// </remarks>
		/// <param name="level">The logging level to log at.</param>
		/// <param name="format">The format string, as per java's Formatter.format() object.</param>
		/// <param name="args">The arguments to format.</param>
		public static void Logf(Redwood.Flag level, string format, params object[] args)
		{
			Log(level, (ISupplier<string>)null);
		}

		/// <summary>Begin a "track;" that is, begin logging at one level deeper.</summary>
		/// <remarks>
		/// Begin a "track;" that is, begin logging at one level deeper.
		/// Channels other than the FORCE channel are ignored.
		/// </remarks>
		/// <param name="args">The title of the track to begin, with an optional FORCE flag.</param>
		public static void StartTrack(params object[] args)
		{
			if (isClosed)
			{
				return;
			}
			//--Create Record
			int len = args.Length == 0 ? 0 : args.Length - 1;
			object content = args.Length == 0 ? string.Empty : args[len];
			object[] tags = new object[len];
			long timestamp = Runtime.CurrentTimeMillis();
			System.Array.Copy(args, 0, tags, 0, len);
			//--Create Task
			IRunnable startTrack = null;
			//--Run Task
			if (isThreaded)
			{
				//(case: multithreaded)
				long threadId = Thread.CurrentThread().GetId();
				AttemptThreadControl(threadId, startTrack);
			}
			else
			{
				//(case: no threading)
				startTrack.Run();
			}
		}

		/// <summary>Helper method to start a track on the FORCE channel.</summary>
		/// <param name="name">The track name to print</param>
		public static void ForceTrack(object name)
		{
			StartTrack(Force, name);
		}

		/// <summary>Helper method to start an anonymous track on the FORCE channel.</summary>
		public static void ForceTrack()
		{
			StartTrack(Force, string.Empty);
		}

		/// <summary>End a "track;" that is, return to logging at one level shallower.</summary>
		/// <param name="title">A title that should match the beginning of this track.</param>
		public static void EndTrack(string title)
		{
			if (isClosed)
			{
				return;
			}
			//--Make Task
			long timestamp = Runtime.CurrentTimeMillis();
			IRunnable endTrack = null;
			//(check name match)
			//        throw new IllegalArgumentException("Track names do not match: expected: " + expected + " found: " + title);
			//(decrement depth)
			//(send signal)
			//--Run Task
			if (isThreaded)
			{
				//(case: multithreaded)
				long threadId = Thread.CurrentThread().GetId();
				AttemptThreadControl(threadId, endTrack);
			}
			else
			{
				//(case: no threading)
				endTrack.Run();
			}
		}

		/// <summary>A utility method for closing calls to the anonymous startTrack() call.</summary>
		public static void EndTrack()
		{
			EndTrack(string.Empty);
		}

		/// <summary>Start a multithreaded logging environment.</summary>
		/// <remarks>
		/// Start a multithreaded logging environment. Log messages will be real time
		/// from one of the threads; as each thread finishes, another thread begins logging,
		/// first by making up the backlog, and then by printing any new log messages.
		/// A thread signals that it has finished logging with the finishThread() function;
		/// the multithreaded environment is ended with the endThreads() function.
		/// </remarks>
		/// <param name="title">The name of the thread group being started</param>
		public static void StartThreads(string title)
		{
			if (isThreaded)
			{
				throw new InvalidOperationException("Cannot nest Redwood threaded environments");
			}
			StartTrack(Force, "Threads( " + title + " )");
			isThreaded = true;
		}

		/// <summary>
		/// Signal that this thread will not log any more messages in the multithreaded
		/// environment
		/// </summary>
		public static void FinishThread()
		{
			//--Create Task
			long threadId = Thread.CurrentThread().GetId();
			IRunnable finish = null;
			//--Run Task
			if (isThreaded)
			{
				//(case: multithreaded)
				AttemptThreadControl(threadId, finish);
			}
			else
			{
				//(case: no threading)
				Edu.Stanford.Nlp.Util.Logging.Redwood.Log(Redwood.Flag.Warn, "finishThreads() called outside of threaded environment");
			}
		}

		/// <summary>
		/// Signal that all threads have run to completion, and the multithreaded
		/// environment is over.
		/// </summary>
		/// <param name="check">The name of the thread group passed to startThreads()</param>
		public static void EndThreads(string check)
		{
			//(error check)
			isThreaded = false;
			if (currentThread != -1L)
			{
				Edu.Stanford.Nlp.Util.Logging.Redwood.Log(Redwood.Flag.Warn, "endThreads() called, but thread " + currentThread + " has not finished (exception in thread?)");
			}
			//(end threaded environment)
			System.Diagnostics.Debug.Assert(!control.IsHeldByCurrentThread());
			//(write remaining threads)
			bool cleanPass = false;
			while (!cleanPass)
			{
				cleanPass = true;
				foreach (long thread in threadedLogQueue.Keys)
				{
					System.Diagnostics.Debug.Assert(currentThread < 0L);
					if (threadedLogQueue[thread] != null && !threadedLogQueue[thread].IsEmpty())
					{
						//(mark queue as unclean)
						cleanPass = false;
						//(variables)
						IQueue<IRunnable> backlog = threadedLogQueue[thread];
						currentThread = thread;
						//(clear buffer)
						while (currentThread >= 0)
						{
							if (backlog.IsEmpty())
							{
								Edu.Stanford.Nlp.Util.Logging.Redwood.Log(Redwood.Flag.Warn, "Forgot to call finishThread() on thread " + currentThread);
							}
							System.Diagnostics.Debug.Assert(!control.IsHeldByCurrentThread());
							backlog.Poll().Run();
						}
						//(unregister thread)
						threadsWaiting.Remove(thread);
					}
				}
			}
			while (threadsWaiting.Count > 0)
			{
				System.Diagnostics.Debug.Assert(currentThread < 0L);
				System.Diagnostics.Debug.Assert(control.TryLock());
				System.Diagnostics.Debug.Assert(!threadsWaiting.IsEmpty());
				control.Lock();
				AttemptThreadControlThreadsafe(-1);
				control.Unlock();
			}
			//(clean up)
			foreach (KeyValuePair<long, IQueue<IRunnable>> longQueueEntry in threadedLogQueue)
			{
				System.Diagnostics.Debug.Assert(longQueueEntry.Value.IsEmpty());
			}
			System.Diagnostics.Debug.Assert(threadsWaiting.IsEmpty());
			System.Diagnostics.Debug.Assert(currentThread == -1L);
			EndTrack("Threads( " + check + " )");
		}

		/// <summary>Create an object representing a group of channels.</summary>
		/// <remarks>
		/// Create an object representing a group of channels.
		/// <see cref="RedwoodChannels"/>
		/// contains a more complete description.
		/// </remarks>
		/// <seealso cref="RedwoodChannels"/>
		public static Redwood.RedwoodChannels Channels(params object[] channelNames)
		{
			return new Redwood.RedwoodChannels(channelNames);
		}

		/// <summary>Hide multiple channels.</summary>
		/// <remarks>Hide multiple channels.  All other channels will be unaffected.</remarks>
		/// <param name="channels">The channels to hide</param>
		public static void HideChannelsEverywhere(params object[] channels)
		{
			foreach (LogRecordHandler handler in handlers)
			{
				if (handler is VisibilityHandler)
				{
					VisibilityHandler visHandler = (VisibilityHandler)handler;
					foreach (object channel in channels)
					{
						visHandler.AlsoHide(channel);
					}
				}
			}
		}

		/// <summary>Stop Redwood, closing all tracks and prohibiting future log messages.</summary>
		public static void Stop()
		{
			//--Close logger
			isClosed = true;
			// <- not a thread-safe boolean
			Thread.Yield();
			//poor man's synchronization attempt (let everything else log that wants to)
			Thread.Yield();
			//--Close Tracks
			while (depth > 0)
			{
				depth -= 1;
				//(send signal to handlers)
				handlers.Process(null, Redwood.MessageType.EndTrack, depth, Runtime.CurrentTimeMillis());
			}
			//--Shutdown
			handlers.Process(null, Redwood.MessageType.Shutdown, 0, Runtime.CurrentTimeMillis());
		}

		/*
		---------------------------------------------------------
		UTILITY METHODS
		---------------------------------------------------------
		*/
		/// <summary>Utility method for formatting a time difference (maybe this should go to a util class?)</summary>
		/// <param name="diff">Time difference in milliseconds</param>
		/// <param name="b">The string builder to append to</param>
		protected internal static void FormatTimeDifference(long diff, StringBuilder b)
		{
			//--Get Values
			int mili = (int)diff % 1000;
			long rest = diff / 1000;
			int sec = (int)rest % 60;
			rest = rest / 60;
			int min = (int)rest % 60;
			rest = rest / 60;
			int hr = (int)rest % 24;
			rest = rest / 24;
			int day = (int)rest;
			//--Make String
			if (day > 0)
			{
				b.Append(day).Append(day > 1 ? " days, " : " day, ");
			}
			if (hr > 0)
			{
				b.Append(hr).Append(hr > 1 ? " hours, " : " hour, ");
			}
			if (min > 0)
			{
				if (min < 10)
				{
					b.Append("0");
				}
				b.Append(min).Append(":");
			}
			if (min > 0 && sec < 10)
			{
				b.Append("0");
			}
			b.Append(sec).Append(".").Append(string.Format("%04d", mili));
			if (min > 0)
			{
				b.Append(" minutes");
			}
			else
			{
				b.Append(" seconds");
			}
		}

		public static string FormatTimeDifference(long diff)
		{
			StringBuilder b = new StringBuilder();
			FormatTimeDifference(diff, b);
			return b.ToString();
		}

		/// <summary>Check if the console supports ANSI escape codes.</summary>
		public static readonly bool supportsAnsi;

		static Redwood()
		{
			string os = Runtime.GetProperty("os.name").ToLower();
			bool isUnix = os.Contains("unix") || os.Contains("linux") || os.Contains("solaris");
			supportsAnsi = bool.GetBoolean("Ansi") || isUnix;
		}

		static Redwood()
		{
			/*
			* Set up the default logger.
			* If SLF4J is in the code's classpath
			*/
			RedwoodConfiguration config = RedwoodConfiguration.Minimal();
			try
			{
				MetaClass.Create("org.slf4j.LoggerFactory").CreateInstance();
				MetaClass.Create("edu.stanford.nlp.util.logging.SLF4JHandler").CreateInstance();
				config = RedwoodConfiguration.Slf4j();
			}
			catch (Exception)
			{
			}
			config.Apply();
		}

		/// <summary>An enumeration of the types of "messages" you can send a handler</summary>
		private enum MessageType
		{
			Simple,
			StartTrack,
			Shutdown,
			EndTrack
		}

		/// <summary>A tree structure of record handlers</summary>
		protected internal class RecordHandlerTree : IEnumerable<LogRecordHandler>
		{
			private readonly bool isRoot;

			private readonly LogRecordHandler head;

			private readonly IList<Redwood.RecordHandlerTree> children = new List<Redwood.RecordHandlerTree>();

			public RecordHandlerTree()
			{
				// -- Overhead --
				isRoot = true;
				head = null;
			}

			public RecordHandlerTree(LogRecordHandler head)
			{
				this.isRoot = false;
				this.head = head;
			}

			// -- Core Tree Methods --
			public virtual LogRecordHandler Head()
			{
				return head;
			}

			public virtual IEnumerator<Redwood.RecordHandlerTree> Children()
			{
				return children.GetEnumerator();
			}

			// -- Utility Methods --
			public virtual void AddChild(LogRecordHandler handler)
			{
				if (Redwood.depth != 0)
				{
					throw new InvalidOperationException("Cannot modify Redwood when within a track");
				}
				children.Add(new Redwood.RecordHandlerTree(handler));
			}

			protected internal virtual void AddChildTree(Redwood.RecordHandlerTree tree)
			{
				if (Redwood.depth != 0)
				{
					throw new InvalidOperationException("Cannot modify Redwood when within a track");
				}
				children.Add(tree);
			}

			public virtual LogRecordHandler RemoveChild(LogRecordHandler handler)
			{
				if (Redwood.depth != 0)
				{
					throw new InvalidOperationException("Cannot modify Redwood when within a track");
				}
				IEnumerator<Redwood.RecordHandlerTree> iter = Children();
				while (iter.MoveNext())
				{
					LogRecordHandler cand = iter.Current.Head();
					if (cand == handler)
					{
						iter.Remove();
						return cand;
					}
				}
				return null;
			}

			public virtual Redwood.RecordHandlerTree Find(LogRecordHandler toFind)
			{
				if (toFind == Head())
				{
					return this;
				}
				else
				{
					IEnumerator<Redwood.RecordHandlerTree> iter = Children();
					while (iter.MoveNext())
					{
						Redwood.RecordHandlerTree cand = iter.Current.Find(toFind);
						if (cand != null)
						{
							return cand;
						}
					}
				}
				return null;
			}

			public virtual IEnumerator<LogRecordHandler> GetEnumerator()
			{
				return new _IEnumerator_687(this);
			}

			private sealed class _IEnumerator_687 : IEnumerator<LogRecordHandler>
			{
				public _IEnumerator_687(RecordHandlerTree _enclosing)
				{
					this._enclosing = _enclosing;
					this.seenHead = this._enclosing.isRoot;
					this.childrenIter = this._enclosing.Children();
					this.childOnPrix = this.childrenIter.MoveNext() ? this.childrenIter.Current : null;
					this.childIter = this.childOnPrix == null ? null : this.childOnPrix.GetEnumerator();
					this.lastReturned = null;
				}

				private bool seenHead;

				private readonly IEnumerator<Redwood.RecordHandlerTree> childrenIter;

				private readonly Redwood.RecordHandlerTree childOnPrix;

				private IEnumerator<LogRecordHandler> childIter;

				private LogRecordHandler lastReturned;

				// -- Variables
				// -- HasNext
				public bool MoveNext()
				{
					while (this.childIter != null && !this.childIter.MoveNext())
					{
						if (!this.childrenIter.MoveNext())
						{
							break;
						}
						else
						{
							this.childIter = this.childrenIter.Current.GetEnumerator();
						}
					}
					return !this.seenHead || (this.childIter != null && this.childIter.MoveNext());
				}

				public LogRecordHandler Current
				{
					get
					{
						// -- Next
						if (!this.seenHead)
						{
							this.seenHead = true;
							return this._enclosing.Head();
						}
						this.lastReturned = this.childIter.Current;
						return this.lastReturned;
					}
				}

				// -- Remove
				public void Remove()
				{
					if (!this.seenHead)
					{
						throw new InvalidOperationException("INTERNAL: this shouldn't happen...");
					}
					if (this.lastReturned == null)
					{
						throw new InvalidOperationException("Called remove() before any elements returned");
					}
					if (this.childOnPrix != null && this.lastReturned == this.childOnPrix.Head())
					{
						this.childrenIter.Remove();
					}
					else
					{
						if (this.childIter != null)
						{
							this.childIter.Remove();
						}
						else
						{
							throw new InvalidOperationException("INTERNAL: not sure what we're removing");
						}
					}
				}

				private readonly RecordHandlerTree _enclosing;
			}

			private static IList<Redwood.Record> Append(IList<Redwood.Record> lst, Redwood.Record toAppend)
			{
				if (lst == LogRecordHandler.Empty)
				{
					lst = new List<Redwood.Record>();
				}
				lst.Add(toAppend);
				return lst;
			}

			private void Process(Redwood.Record toPass, Redwood.MessageType type, int newDepth, long timestamp)
			{
				//--Handle Message
				//(records to pass on)
				IList<Redwood.Record> toPassOn;
				if (head != null)
				{
					switch (type)
					{
						case Redwood.MessageType.Simple:
						{
							//(case: not root)
							//(case: simple log message)
							toPassOn = head.Handle(toPass);
							break;
						}

						case Redwood.MessageType.StartTrack:
						{
							//(case: begin a new track)
							toPassOn = head.SignalStartTrack(toPass);
							break;
						}

						case Redwood.MessageType.EndTrack:
						{
							//case: end a track)
							toPassOn = head.SignalEndTrack(newDepth, timestamp);
							break;
						}

						case Redwood.MessageType.Shutdown:
						{
							//case: end a track)
							toPassOn = head.SignalShutdown();
							break;
						}

						default:
						{
							throw new InvalidOperationException("MessageType was non-exhaustive: " + type);
						}
					}
				}
				else
				{
					//(case: is root)
					toPassOn = new List<Redwood.Record>();
					switch (type)
					{
						case Redwood.MessageType.Simple:
						{
							toPassOn = Append(toPassOn, toPass);
							break;
						}

						case Redwood.MessageType.StartTrack:
						{
							break;
						}

						case Redwood.MessageType.EndTrack:
						{
							break;
						}

						case Redwood.MessageType.Shutdown:
						{
							break;
						}

						default:
						{
							throw new InvalidOperationException("MessageType was non-exhaustive: " + type);
						}
					}
				}
				//--Propagate Children
				IEnumerator<Redwood.RecordHandlerTree> iter = Children();
				while (iter.MoveNext())
				{
					//for each child...
					Redwood.RecordHandlerTree child = iter.Current;
					// (auxiliary records)
					foreach (Redwood.Record r in toPassOn)
					{
						//for each record...
						child.Process(r, Redwood.MessageType.Simple, newDepth, timestamp);
					}
					switch (type)
					{
						case Redwood.MessageType.StartTrack:
						case Redwood.MessageType.EndTrack:
						case Redwood.MessageType.Shutdown:
						{
							// (special record)
							child.Process(toPass, type, newDepth, timestamp);
							break;
						}

						case Redwood.MessageType.Simple:
						{
							break;
						}

						default:
						{
							throw new InvalidOperationException("MessageType was non-exhaustive: " + type);
						}
					}
				}
			}

			private StringBuilder ToStringHelper(StringBuilder b, int depth)
			{
				for (int i = 0; i < depth; i++)
				{
					b.Append("  ");
				}
				b.Append(head == null ? "ROOT" : head).Append("\n");
				foreach (Redwood.RecordHandlerTree child in children)
				{
					child.ToStringHelper(b, depth + 1);
				}
				return b;
			}

			public override string ToString()
			{
				return ToStringHelper(new StringBuilder(), 0).ToString();
			}
		}

		/// <summary>
		/// A log record, which encapsulates the information needed
		/// to eventually display the enclosed message.
		/// </summary>
		public class Record
		{
			public readonly object content;

			private readonly object[] channels;

			public readonly int depth;

			public readonly long timesstamp;

			public readonly long thread = Thread.CurrentThread().GetId();

			private bool channelsSorted = false;

			/// <summary>
			/// Create a new Record, based on the content of the log, the channels, and
			/// the depth
			/// </summary>
			/// <param name="content">An object (usually String) representing the log contents</param>
			/// <param name="channels">A set of channels to publish this record to</param>
			/// <param name="depth">The depth of the log message</param>
			protected internal Record(object content, object[] channels, int depth, long timestamp)
			{
				//(filled in at construction)
				//(known at creation)
				//(state)
				this.content = content;
				this.channels = channels;
				this.depth = depth;
				this.timesstamp = timestamp;
			}

			/// <summary>Sort the channels alphabetically, with the standard channels in front.</summary>
			/// <remarks>
			/// Sort the channels alphabetically, with the standard channels in front.
			/// Note that the special FORCE tag is always first.
			/// </remarks>
			private void Sort()
			{
				//(sort flags)
				if (!channelsSorted && channels.Length == 2)
				{
					// Efficiency tweak for when we only have two channels. More than two, it's worth just sorting.
					if (channels[1] is Redwood.Flag && !(channels[0] is Redwood.Flag))
					{
						// Case: second element is a flag, but first isn't.
						// Action: put the flag first
						object tmp = channels[0];
						channels[0] = channels[1];
						channels[1] = tmp;
					}
					else
					{
						if (!(channels[0] is Redwood.Flag) && !(channels[1] is Redwood.Flag) && string.CompareOrdinal(channels[0].ToString(), channels[1].ToString()) > 0)
						{
							// Case: neither element is a flag, and the second argument comes before the first
							// Action: sort the two arguments
							object tmp = channels[0];
							channels[0] = channels[1];
							channels[1] = tmp;
						}
					}
				}
				else
				{
					// Misc case: both elements are flags, or the flag is already first.
					// In both of these cases, we don't need to do anything
					if (!channelsSorted && channels.Length > 2)
					{
						Arrays.Sort(channels, null);
					}
				}
			}

			/// <summary>Returns whether this log message wants to be forced to be printed</summary>
			/// <returns>true if the FORCE flag is set on this message</returns>
			public virtual bool Force()
			{
				Sort();
				return this.channels.Length > 0 && this.channels[0] == Force;
			}

			/// <summary>Returns the channels for this record, in sorted order (special channels first, then alphabetical)</summary>
			/// <returns>A sorted list of channels</returns>
			public virtual object[] Channels()
			{
				Sort();
				return this.channels;
			}

			public override string ToString()
			{
				return "Record [content=" + content + ", depth=" + depth + ", channels=" + Arrays.ToString(Channels()) + ", thread=" + thread + ", timesstamp=" + timesstamp + "]";
			}
		}

		/// <summary>Default output handler which actually prints things to the real System.out</summary>
		public class ConsoleHandler : OutputHandler
		{
			internal TextWriter stream;

			private ConsoleHandler(TextWriter stream)
			{
				this.stream = stream;
			}

			/// <summary>Print a string to the console, without the trailing newline</summary>
			/// <param name="channels">
			/// The channel this line is being printed to;
			/// not relevant for this handler.
			/// </param>
			/// <param name="line">The string to be printed.</param>
			public override void Print(object[] channels, string line)
			{
				stream.Write(line);
				stream.Flush();
			}

			protected internal override bool SupportsAnsi()
			{
				return true;
			}

			public static Redwood.ConsoleHandler Out()
			{
				return new Redwood.ConsoleHandler(realSysOut);
			}

			public static Redwood.ConsoleHandler Err()
			{
				return new Redwood.ConsoleHandler(realSysErr);
			}
		}

		/// <summary>Handler which prints to a specified file.</summary>
		/// <remarks>
		/// Handler which prints to a specified file.
		/// TODO: make constructors for other ways of describing files (File, for example!)
		/// </remarks>
		public class FileHandler : OutputHandler
		{
			private PrintWriter printWriter;

			public FileHandler(string filename)
			{
				try
				{
					printWriter = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filename), "utf-8")));
				}
				catch (IOException e)
				{
					Redwood.Log(Redwood.Flag.Error, e);
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override void Print(object[] channels, string line)
			{
				printWriter.Write(line == null ? "null" : line);
				printWriter.Flush();
			}
		}

		/// <summary>
		/// A utility class for Redwood intended for static import
		/// (import static edu.stanford.nlp.util.logging.Redwood.Util.*;),
		/// providing a wrapper for Redwood functions and adding utility shortcuts.
		/// </summary>
		public class Util
		{
			private Util()
			{
			}

			// static methods
			private static object[] RevConcat(object[] B, params object[] A)
			{
				// A is empty whenever do info level logging; B is only empty for blank logging line
				if (A.Length == 0)
				{
					return B;
				}
				object[] C = new object[A.Length + B.Length];
				System.Array.Copy(A, 0, C, 0, A.Length);
				System.Array.Copy(B, 0, C, A.Length, B.Length);
				return C;
			}

			public static readonly Redwood.Flag Err = Redwood.Flag.Error;

			public static readonly Redwood.Flag Warn = Redwood.Flag.Warn;

			public static readonly Redwood.Flag Dbg = Redwood.Flag.Debug;

			public static readonly Redwood.Flag Force = Redwood.Flag.Force;

			public static readonly Redwood.Flag Stdout = Redwood.Flag.Stdout;

			public static readonly Redwood.Flag Stderr = Redwood.Flag.Stderr;

			public static void PrettyLog(object obj)
			{
				PrettyLogger.Log(obj);
			}

			public static void PrettyLog(string description, object obj)
			{
				PrettyLogger.Log(description, obj);
			}

			public static void Log(params object[] objs)
			{
				Redwood.Log(objs);
			}

			public static void Logf(string format, params object[] args)
			{
				Redwood.Logf(format, args);
			}

			public static void Warn(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Warn));
			}

			public static void Warning(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Warn));
			}

			public static void Debug(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Dbg));
			}

			public static void Err(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Err, Force));
			}

			public static void Error(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Err, Force));
			}

			public static void Fatal(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Err, Force));
				System.Environment.Exit(1);
			}

			public static void RuntimeException(params object[] objs)
			{
				Redwood.Log(RevConcat(objs, Err, Force));
				throw new Exception(Arrays.ToString(objs));
			}

			public static void Println(object o)
			{
				System.Console.Out.WriteLine(o);
			}

			/// <summary>Exits with a given status code</summary>
			public static void Exit(int exitCode)
			{
				Redwood.Stop();
				System.Environment.Exit(exitCode);
			}

			/// <summary>Exits with status code 0, stopping Redwood first</summary>
			public static void Exit()
			{
				Exit(0);
			}

			/// <summary>Create a RuntimeException with arguments</summary>
			public static Exception Fail(object msg)
			{
				if (msg is string)
				{
					return new Exception((string)msg);
				}
				else
				{
					if (msg is Exception)
					{
						return (Exception)msg;
					}
					else
					{
						if (msg is Exception)
						{
							return new Exception((Exception)msg);
						}
						else
						{
							throw new Exception(msg.ToString());
						}
					}
				}
			}

			/// <summary>Create a new RuntimeException with no arguments</summary>
			public static Exception Fail()
			{
				return new Exception();
			}

			public static void StartTrack(params object[] objs)
			{
				Redwood.StartTrack(objs);
			}

			public static void ForceTrack(string title)
			{
				Redwood.StartTrack(Force, title);
			}

			public static void EndTrack(string check)
			{
				Redwood.EndTrack(check);
			}

			public static void EndTrack()
			{
				Redwood.EndTrack();
			}

			public static void EndTrackIfOpen(string check)
			{
				if (!Redwood.titleStack.Empty() && Redwood.titleStack.Peek().Equals(check))
				{
					Redwood.EndTrack(check);
				}
			}

			public static void EndTracksUntil(string check)
			{
				while (!Redwood.titleStack.Empty() && !Redwood.titleStack.Peek().Equals(check))
				{
					Redwood.EndTrack(Redwood.titleStack.Peek());
				}
			}

			public static void EndTracksTo(string check)
			{
				EndTracksUntil(check);
				EndTrack(check);
			}

			public static void StartThreads(string title)
			{
				Redwood.StartThreads(title);
			}

			public static void FinishThread()
			{
				Redwood.FinishThread();
			}

			public static void EndThreads(string check)
			{
				Redwood.EndThreads(check);
			}

			public static Redwood.RedwoodChannels Channels(params object[] channels)
			{
				return new Redwood.RedwoodChannels(channels);
			}

			/// <summary>Wrap a collection of threads (Runnables) to be logged by Redwood.</summary>
			/// <remarks>
			/// Wrap a collection of threads (Runnables) to be logged by Redwood.
			/// Each thread will be logged as a continuous chunk; concurrent threads will be queued
			/// and logged after the "main" thread has finished.
			/// This means that every Runnable passed to this method will run as a chunk, though in possibly
			/// random order.
			/// The handlers set up will operate on the output as if it were not concurrent -- timing will be preserved
			/// but repeated records will be collapsed as per the order the logs are actually output, rather than based
			/// on timestamp.
			/// </remarks>
			/// <param name="title">A title for the group of threads being run</param>
			/// <param name="runnables">The Runnables representing the tasks being run, without the Redwood overhead</param>
			/// <returns>A new collection of Runnables with the Redwood overhead taken care of</returns>
			public static IEnumerable<IRunnable> Thread(string title, IEnumerable<IRunnable> runnables)
			{
				//--Preparation
				//(variables)
				AtomicBoolean haveStarted = new AtomicBoolean(false);
				ReentrantLock metaInfoLock = new ReentrantLock();
				AtomicInteger numPending = new AtomicInteger(0);
				IEnumerator<IRunnable> iter = runnables.GetEnumerator();
				//--Create Runnables
				return new IterableIterator<IRunnable>(new _IEnumerator_1050(iter, numPending));
			}

			private sealed class _IEnumerator_1050 : IEnumerator<IRunnable>
			{
				public _IEnumerator_1050(IEnumerator<IRunnable> iter, AtomicInteger numPending)
				{
					this.iter = iter;
					this.numPending = numPending;
				}

				public bool MoveNext()
				{
					lock (iter)
					{
						return iter.MoveNext();
					}
				}

				public IRunnable Current
				{
					get
					{
						lock (this)
						{
							IRunnable runnable;
							lock (iter)
							{
								runnable = iter.Current;
							}
							// (don't flood the queue)
							while (numPending.Get() > 100)
							{
								try
								{
									Java.Lang.Thread.Sleep(100);
								}
								catch (Exception e)
								{
									throw new RuntimeInterruptedException(e);
								}
							}
							numPending.IncrementAndGet();
							// (add the job)
							return null;
						}
					}
				}

				//(signal start of threads)
				//<--this must be a blocking operation
				//(run runnable)
				//(signal end of thread)
				//(signal end of threads)
				public void Remove()
				{
					lock (iter)
					{
						iter.Remove();
					}
				}

				private readonly IEnumerator<IRunnable> iter;

				private readonly AtomicInteger numPending;
			}

			public static IEnumerable<IRunnable> Thread(IEnumerable<IRunnable> runnables)
			{
				return Thread(string.Empty, runnables);
			}

			/// <summary>Thread a collection of Runnables, and run them via a java Executor.</summary>
			/// <remarks>
			/// Thread a collection of Runnables, and run them via a java Executor.
			/// This is a utility function; the Redwood-specific changes happen in the
			/// thread() method.
			/// </remarks>
			/// <param name="title">A title for the group of threads being run</param>
			/// <param name="runnables">
			/// The Runnables representing the tasks being run, without the Redwood overhead --
			/// particularly, these should NOT have been passed to thread() yet.
			/// </param>
			/// <param name="numThreads">The number of threads to run on</param>
			public static void ThreadAndRun(string title, IEnumerable<IRunnable> runnables, int numThreads)
			{
				// (short circuit if single thread)
				if (numThreads <= 1 || isThreaded || (runnables is ICollection && ((ICollection<IRunnable>)runnables).Count <= 1))
				{
					StartTrack("Threads (" + title + ")");
					foreach (IRunnable toRun in runnables)
					{
						toRun.Run();
					}
					EndTrack("Threads (" + title + ")");
					return;
				}
				//(create executor)
				IExecutorService exec = Executors.NewFixedThreadPool(numThreads);
				//(add threads)
				foreach (IRunnable toRun_1 in Thread(title, runnables))
				{
					exec.Submit(toRun_1);
				}
				//(await finish)
				exec.Shutdown();
				try
				{
					exec.AwaitTermination(long.MaxValue, TimeUnit.Seconds);
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
			}

			public static void ThreadAndRun(string title, IEnumerable<IRunnable> runnables)
			{
				ThreadAndRun(title, runnables, Runtime.GetRuntime().AvailableProcessors());
			}

			public static void ThreadAndRun(IEnumerable<IRunnable> runnables, int numThreads)
			{
				ThreadAndRun(numThreads.ToString(), runnables, numThreads);
			}

			public static void ThreadAndRun(IEnumerable<IRunnable> runnables)
			{
				ThreadAndRun(runnables, ArgumentParser.threads);
			}

			/// <summary>Print (to console) a margin with the channels of a given log message.</summary>
			/// <remarks>
			/// Print (to console) a margin with the channels of a given log message.
			/// Note that this does not affect File printing.
			/// </remarks>
			/// <param name="width">The width of the margin to print (must be &gt;2)</param>
			public static void PrintChannels(int width)
			{
				foreach (LogRecordHandler handler in handlers)
				{
					if (handler is OutputHandler)
					{
						((OutputHandler)handler).leftMargin = width;
					}
				}
			}

			public static readonly Style Bold = Style.Bold;

			public static readonly Style Dim = Style.Dim;

			public static readonly Style Italic = Style.Italic;

			public static readonly Style Underline = Style.Underline;

			public static readonly Style Blink = Style.Blink;

			public static readonly Style CrossOut = Style.CrossOut;

			public static readonly Color Black = Color.Black;

			public static readonly Color Red = Color.Red;

			public static readonly Color Green = Color.Green;

			public static readonly Color Yellow = Color.Yellow;

			public static readonly Color Blue = Color.Blue;

			public static readonly Color Magenta = Color.Magenta;

			public static readonly Color Cyan = Color.Cyan;

			public static readonly Color White = Color.White;
		}

		/// <summary>Represents a collection of channels.</summary>
		/// <remarks>
		/// Represents a collection of channels. This lets you decouple selecting
		/// channels from logging messages, similar to traditional logging systems.
		/// <see cref="RedwoodChannels"/>
		/// have log and logf methods. Unlike Redwood.log and
		/// Redwood.logf, these do not take channel names since those are specified
		/// inside
		/// <see cref="RedwoodChannels"/>
		/// .
		/// Required if you want to use logf with a channel. This follows the
		/// Builder Pattern so Redwood.channels("chanA", "chanB").log("message") is equivalent to
		/// Redwood.channels("chanA").channels("chanB").log("message")
		/// </remarks>
		public class RedwoodChannels
		{
			private readonly object[] channelNames;

			public RedwoodChannels(params object[] channelNames)
			{
				this.channelNames = channelNames;
			}

			/// <summary>
			/// Creates a new RedwoodChannels object, concatenating the channels from
			/// this RedwoodChannels with some additional channels.
			/// </summary>
			/// <param name="moreChannelNames">The channel names to also include</param>
			/// <returns>A RedwoodChannels representing the current and new channels.</returns>
			public virtual Redwood.RedwoodChannels Channels(params object[] moreChannelNames)
			{
				//(copy array)
				object[] result = new object[channelNames.Length + moreChannelNames.Length];
				System.Array.Copy(channelNames, 0, result, 0, channelNames.Length);
				System.Array.Copy(moreChannelNames, 0, result, channelNames.Length, moreChannelNames.Length);
				//(create channels)
				return new Redwood.RedwoodChannels(result);
			}

			/// <summary>Log a message to the channels specified in this RedwoodChannels object.</summary>
			/// <param name="obj">The object to log</param>
			public virtual void Log(params object[] obj)
			{
				object[] newArgs = new object[channelNames.Length + obj.Length];
				System.Array.Copy(channelNames, 0, newArgs, 0, channelNames.Length);
				System.Array.Copy(obj, 0, newArgs, channelNames.Length, obj.Length);
				Redwood.Log(newArgs);
			}

			/// <summary>Log a printf-style formatted message to the channels specified in this RedwoodChannels object.</summary>
			/// <param name="format">The format string for the printf function</param>
			/// <param name="args">The arguments to the printf function</param>
			public virtual void Logf(string format, params object[] args)
			{
				Log((ISupplier<string>)null);
			}

			/// <summary>Log a printf-style formatted message to the channels specified in this RedwoodChannels object.</summary>
			/// <param name="level">The log level to log with.</param>
			/// <param name="format">The format string for the printf function</param>
			/// <param name="args">The arguments to the printf function</param>
			public virtual void Logf(Redwood.Flag level, string format, params object[] args)
			{
				Log(level, (ISupplier<string>)null);
			}

			/// <summary>Log to the debug channel.</summary>
			/// <remarks>Log to the debug channel. @see RedwoodChannels#logf(Flag, String, Object...)</remarks>
			public virtual void Debugf(string format, params object[] args)
			{
				Debug((ISupplier<string>)null);
			}

			/// <summary>Log to the warn channel.</summary>
			/// <remarks>Log to the warn channel. @see RedwoodChannels#logf(Flag, String, Object...)</remarks>
			public virtual void Warnf(string format, params object[] args)
			{
				Warn((ISupplier<string>)null);
			}

			/// <summary>Log to the error channel.</summary>
			/// <remarks>Log to the error channel. @see RedwoodChannels#logf(Flag, String, Object...)</remarks>
			public virtual void Errf(string format, params object[] args)
			{
				Err((ISupplier<string>)null);
			}

			/// <summary>PrettyLog an object using these channels.</summary>
			/// <remarks>
			/// PrettyLog an object using these channels.  A default description will be created
			/// based on the type of obj.
			/// </remarks>
			public virtual void PrettyLog(object obj)
			{
				PrettyLogger.Log(this, obj);
			}

			/// <summary>PrettyLog an object with a description using these channels.</summary>
			public virtual void PrettyLog(string description, object obj)
			{
				PrettyLogger.Log(this, description, obj);
			}

			public virtual void Info(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs));
			}

			public virtual void Warn(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Warn));
			}

			public virtual void Warning(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Warn));
			}

			public virtual void Debug(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Dbg));
			}

			public virtual void Err(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Err, Force));
			}

			public virtual void Error(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Err, Force));
			}

			public virtual void Fatal(params object[] objs)
			{
				Log(Redwood.Util.RevConcat(objs, Err, Force));
				System.Environment.Exit(1);
			}
		}

		/// <summary>Standard channels; enum for the sake of efficiency</summary>
		protected internal enum Flag
		{
			Error,
			Warn,
			Debug,
			Stdout,
			Stderr,
			Force
		}

		/// <summary>Various informal tests of Redwood functionality.</summary>
		/// <param name="args">Unused</param>
		public static void Main(string[] args)
		{
			// TODO(gabor) update this with the new RedwoodConfiguration
			RedwoodConfiguration.Current().ListenOnChannels(null, Redwood.Err).Apply();
			Redwood.Log("hello world!");
			Redwood.Log(Redwood.Err, "an error!");
			System.Environment.Exit(1);
			// -- STRESS TEST THREADS --
			LinkedList<IRunnable> tasks = new LinkedList<IRunnable>();
			for (int i = 0; i < 1000; i++)
			{
				int fI = i;
				tasks.Add(null);
			}
			StartTrack("Wrapper");
			for (int i_1 = 0; i_1 < 100; i_1++)
			{
				Redwood.Util.ThreadAndRun(tasks, 100);
			}
			EndTrack("Wrapper");
			System.Environment.Exit(1);
			ForceTrack("Track 1");
			Log("tag", Err, "hello world");
			StartTrack("Hidden");
			StartTrack("Subhidden");
			EndTrack("Subhidden");
			EndTrack("Hidden");
			StartTrack(Force, "Shown");
			StartTrack(Force, "Subshown");
			EndTrack("Subshown");
			EndTrack("Shown");
			Log("^shown should have appeared above");
			StartTrack("Track 1.1");
			Log(Warn, "some", "something in 1.1");
			Log("some", Err, "something in 1.1");
			Log(Force, "some", Warn, "something in 1.1");
			Log(Warn, Force, "some", "something in 1.1");
			Logf("format string %s then int %d", "hello", 7);
			EndTrack("Track 1.1");
			StartTrack();
			Log("In an anonymous track");
			EndTrack();
			EndTrack("Track 1");
			Log("outside of a track");
			Log("these", "channels", "should", "be", "in", Dbg, "alphabetical", "order", "a log item with lots of channels");
			Log("these", "channels", "should", "be", "in", Dbg, "alphabetical", "order", "a log item\nthat spans\nmultiple\nlines");
			Log(Dbg, "a last log item");
			Log(Err, null);
			//--Repeated Records
			//    RedwoodConfiguration.current().collapseExact().apply();
			//(simple case)
			ForceTrack("Strict Equality");
			for (int i_2 = 0; i_2 < 100; i_2++)
			{
				Log("this is a message");
			}
			EndTrack("Strict Equality");
			//(in-track change)
			ForceTrack("Change");
			for (int i_3 = 0; i_3 < 10; i_3++)
			{
				Log("this is a message");
			}
			for (int i_4 = 0; i_4 < 10; i_4++)
			{
				Log("this is a another message");
			}
			for (int i_5 = 0; i_5 < 10; i_5++)
			{
				Log("this is a third message");
			}
			for (int i_6 = 0; i_6 < 5; i_6++)
			{
				Log("this is a fourth message");
			}
			Log(Force, "this is a fourth message");
			for (int i_7 = 0; i_7 < 5; i_7++)
			{
				Log("this is a fourth message");
			}
			Log("^middle 'fourth message' was forced");
			EndTrack("Change");
			//(suppress tracks)
			ForceTrack("Repeated Tracks");
			for (int i_8 = 0; i_8 < 100; i_8++)
			{
				StartTrack("Track type 1");
				Log("a message");
				EndTrack("Track type 1");
			}
			for (int i_9 = 0; i_9 < 100; i_9++)
			{
				StartTrack("Track type 2");
				Log("a message");
				EndTrack("Track type 2");
			}
			for (int i_10 = 0; i_10 < 100; i_10++)
			{
				StartTrack("Track type 3");
				Log("a message");
				EndTrack("Track type 3");
			}
			StartTrack("Track type 3");
			StartTrack("nested");
			Log(Force, "this should show up");
			EndTrack("nested");
			EndTrack("Track type 3");
			for (int i_11 = 0; i_11 < 5; i_11++)
			{
				StartTrack("Track type 3");
				Log(Force, "this should show up");
				EndTrack("Track type 3");
			}
			Log(Warn, "The log message 'this should show up' should show up 6 (5+1) times above");
			EndTrack("Repeated Tracks");
			//(tracks with invisible things)
			//    Redwood.hideOnlyChannels(DBG);
			ForceTrack("Hidden Subtracks");
			for (int i_12 = 0; i_12 < 100; i_12++)
			{
				StartTrack("Only has debug messages");
				Log(Dbg, "You shouldn't see me");
				EndTrack("Only has debug messages");
			}
			Log("You shouldn't see any other messages or 'skipped tracks' here");
			EndTrack("Hidden Subtracks");
			//(fuzzy repeats)
			RedwoodConfiguration.Standard().Apply();
			//    RedwoodConfiguration.current().collapseApproximate().apply();
			ForceTrack("Fuzzy Equality");
			for (int i_13 = 0; i_13 < 100; i_13++)
			{
				Log("iter " + i_13 + " ended with value " + (-34587292534.0 + Math.Sqrt(i_13) * 3000000000.0));
			}
			EndTrack("Fuzzy Equality");
			ForceTrack("Fuzzy Equality (timing)");
			for (int i_14 = 0; i_14 < 100; i_14++)
			{
				Log("iter " + i_14 + " ended with value " + (-34587292534.0 + Math.Sqrt(i_14) * 3000000000.0));
				try
				{
					Thread.Sleep(50);
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
			}
			EndTrack("Fuzzy Equality (timing)");
			//--Util Helper
			Redwood.Util.Log("hello world");
			Redwood.Util.Log(Dbg, "hello world");
			Redwood.Util.Debug("hello world");
			Redwood.Util.Debug("atag", "hello world");
			//--Show Name at Track Finish
			Redwood.GetHandler<Redwood.ConsoleHandler>().minLineCountForTrackNameReminder = 5;
			StartTrack("Long Track");
			for (int i_15 = 0; i_15 < 10; i_15++)
			{
				Log(Force, "contents of long track");
			}
			EndTrack("Long TracK");
			StartTrack("Long Track");
			StartTrack("But really this is the long one");
			try
			{
				Thread.Sleep(3000);
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
			for (int i_16 = 0; i_16 < 10; i_16++)
			{
				Log(Force, "contents of long track");
			}
			EndTrack("But really this is the long one");
			EndTrack("Long TracK");
			Redwood.GetHandler<Redwood.ConsoleHandler>().minLineCountForTrackNameReminder = 50;
			//--Multithreading
			IExecutorService exec = Executors.NewFixedThreadPool(10);
			StartThreads("name");
			for (int i_17 = 0; i_17 < 50; i_17++)
			{
				int theI = i_17;
				exec.Execute(null);
			}
			exec.Shutdown();
			try
			{
				exec.AwaitTermination(long.MaxValue, TimeUnit.Seconds);
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
			EndThreads("name");
			//--System Streams
			Redwood.CaptureSystemStreams(true, true);
			System.Console.Out.WriteLine("Hello World");
			log.Info("This is an error!");
			//--Neat Exit
			//    RedwoodConfiguration.standard().collapseExact().apply();
			//(on close)
			for (int i_18 = 0; i_18 < 100; i_18++)
			{
				//      startTrack();
				Log("stuff!");
			}
			//      endTrack();
			Redwood.Util.Exit(0);
			//(on exception)
			System.Console.Out.WriteLine("I'm going to exception soon (on purpose)");
			RedwoodConfiguration.Current().NeatExit().Apply();
			StartTrack("I should close");
			Log(Force, "so I'm nonempty...");
			try
			{
				Thread.Sleep(1000);
			}
			catch (Exception e)
			{
				throw new RuntimeInterruptedException(e);
			}
			throw new ArgumentException();
		}
		// end main()
	}
}
