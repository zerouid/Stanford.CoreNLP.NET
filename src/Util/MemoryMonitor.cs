using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Utilities for monitoring memory use, including peak memory use.</summary>
	public class MemoryMonitor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.MemoryMonitor));

		public const int MaxSwaps = 50;

		protected internal long lastPoll;

		protected internal long pollEvery;

		protected internal int freeMem;

		protected internal int usedSwap;

		protected internal int swaps;

		protected internal Runtime r;

		public MemoryMonitor()
			: this(60000)
		{
		}

		public MemoryMonitor(long millis)
		{
			// 1 min default
			lastPoll = 0;
			pollEvery = millis;
			freeMem = 0;
			usedSwap = 0;
			swaps = 0;
			r = Runtime.GetRuntime();
			PollVMstat(true);
		}

		// TODO I don't think anyone uses this
		public virtual void PollAtMostEvery(long millis)
		{
			pollEvery = millis;
		}

		public virtual int GetMaxMemory()
		{
			return (int)(r.MaxMemory() / 1024);
		}

		public virtual int GetMaxAvailableMemory()
		{
			return GetMaxAvailableMemory(false);
		}

		// kilobytes
		public virtual int GetMaxAvailableMemory(bool accurate)
		{
			if (accurate)
			{
				Runtime.Gc();
			}
			return (int)((r.MaxMemory() - r.TotalMemory() + r.FreeMemory()) / 1024);
		}

		public virtual int GetUsedMemory()
		{
			return GetUsedMemory(false);
		}

		public virtual int GetUsedMemory(bool accurate)
		{
			if (accurate)
			{
				Runtime.Gc();
			}
			return (int)((r.TotalMemory() - r.FreeMemory()) / 1024);
		}

		public virtual int GetSystemFreeMemory(bool accurate)
		{
			if (accurate)
			{
				Runtime.Gc();
			}
			PollVMstat(false);
			return freeMem;
		}

		public virtual int GetSystemUsedSwap()
		{
			PollVMstat(false);
			return usedSwap;
		}

		public virtual double GetSystemSwapsPerSec()
		{
			PollVMstat(false);
			return swaps;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal static List<string> ParseFields(BufferedReader br, string splitStr, int[] lineNums, int[] positions)
		{
			int currLine = 0;
			int processed = 0;
			List<string> found = new List<string>();
			while (br.Ready())
			{
				string[] fields = br.ReadLine().Split(splitStr);
				currLine++;
				if (currLine == lineNums[processed])
				{
					int currPosition = 0;
					foreach (string f in fields)
					{
						if (f.Length > 0)
						{
							currPosition++;
							if (currPosition == positions[processed])
							{
								found.Add(f);
								processed++;
								if (processed == positions.Length)
								{
									break;
								}
							}
						}
					}
				}
			}
			return found;
		}

		public virtual void PollFree(bool force)
		{
			if (!force)
			{
				long time = Runtime.CurrentTimeMillis();
				if (time - lastPoll < pollEvery)
				{
					return;
				}
			}
			Process p = null;
			int[] freeLines = new int[] { 2, 4 };
			int[] freePositions = new int[] { 4, 3 };
			lastPoll = Runtime.CurrentTimeMillis();
			try
			{
				p = r.Exec("free");
				p.WaitFor();
				BufferedReader bri = new BufferedReader(new InputStreamReader(p.GetInputStream()));
				List<string> l = ParseFields(bri, " ", freeLines, freePositions);
				freeMem = System.Convert.ToInt32(l[1]);
				usedSwap = System.Convert.ToInt32(l[2]);
			}
			catch (Exception e)
			{
				log.Info(e);
			}
			finally
			{
				if (p != null)
				{
					p.Destroy();
				}
			}
		}

		public virtual void PollVMstat(bool force)
		{
			if (!force)
			{
				long time = Runtime.CurrentTimeMillis();
				if (time - lastPoll < pollEvery)
				{
					return;
				}
			}
			Process p = null;
			int[] lines = new int[] { 4, 4, 4, 4 };
			int[] positions = new int[] { 3, 4, 7, 8 };
			try
			{
				p = r.Exec("vmstat 1 2");
				p.WaitFor();
				long time = Runtime.CurrentTimeMillis();
				BufferedReader bri = new BufferedReader(new InputStreamReader(p.GetInputStream()));
				List<string> l = ParseFields(bri, " ", lines, positions);
				usedSwap = System.Convert.ToInt32(l[0]);
				freeMem = System.Convert.ToInt32(l[1]);
				swaps = System.Convert.ToInt32(l[2]) + System.Convert.ToInt32(l[3]);
				lastPoll = time;
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				if (p != null)
				{
					p.Destroy();
				}
			}
		}

		public virtual bool SystemIsSwapping()
		{
			return (GetSystemSwapsPerSec() > MaxSwaps);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("lastPoll:").Append(lastPoll);
			sb.Append(" pollEvery:").Append(pollEvery);
			sb.Append(" freeMem:").Append(freeMem);
			sb.Append(" usedSwap:").Append(usedSwap);
			sb.Append(" swaps:").Append(swaps);
			sb.Append(" maxAvailable:").Append(GetMaxAvailableMemory(false));
			sb.Append(" used:").Append(GetUsedMemory(false));
			return sb.ToString();
		}

		/// <summary>This class offers a simple way to track the peak memory used by a program.</summary>
		/// <remarks>
		/// This class offers a simple way to track the peak memory used by a program.
		/// Simply launch a <code>PeakMemoryMonitor</code> as
		/// <blockquote><code>
		/// Thread monitor = new Thread(new PeakMemoryMonitor());<br />
		/// monitor.start()
		/// </code></blockquote>
		/// and then when you want to stop monitoring, call
		/// <blockquote><code>
		/// monitor.interrupt();
		/// monitor.join();
		/// </code></blockquote>
		/// You only need the last line if you want to be sure the monitor stops before
		/// you move on in the code; and strictly speaking, you should surround the
		/// <code>monitor.join()</code> call with a <code>try/catch</code> block, as
		/// the <code>Thread</code> you are running could itself be interrupted, so you
		/// should actually have something like
		/// <blockquote><code>
		/// monitor.interrupt();
		/// try {
		/// monitor.join();
		/// } catch (InterruptedException ex) {
		/// // handle the exception
		/// }
		/// </code></blockquote>
		/// or else throw the exception.
		/// </remarks>
		/// <author>ilya</author>
		public class PeakMemoryMonitor : IRunnable
		{
			private const float Gigabyte = 1 << 30;

			private const int DefaultPollFrequency = 1000;

			private const int DefaultLogFrequency = 60000;

			private int pollFrequency;

			private int logFrequency;

			private Timing timer;

			private TextWriter outstream;

			private long peak = 0;

			public PeakMemoryMonitor()
				: this(DefaultPollFrequency, DefaultLogFrequency)
			{
			}

			/// <param name="pollFrequency">frequency, in milliseconds, with which to poll</param>
			/// <param name="logFrequency">
			/// frequency, in milliseconds, with which to log maximum memory
			/// used so far
			/// </param>
			public PeakMemoryMonitor(int pollFrequency, int logFrequency)
				: this(pollFrequency, logFrequency, System.Console.Error)
			{
			}

			public PeakMemoryMonitor(int pollFrequency, int logFrequency, TextWriter @out)
			{
				/* 1 second */
				/* 1 minute */
				this.pollFrequency = pollFrequency;
				this.logFrequency = logFrequency;
				this.outstream = @out;
				this.timer = new Timing();
			}

			public virtual void Run()
			{
				Runtime runtime = Runtime.GetRuntime();
				timer.Start();
				while (true)
				{
					peak = Math.Max(peak, runtime.TotalMemory() - runtime.FreeMemory());
					if (timer.Report() > logFrequency)
					{
						Log();
						timer.Restart();
					}
					try
					{
						Thread.Sleep(pollFrequency);
					}
					catch (Exception e)
					{
						Log();
						throw new RuntimeInterruptedException(e);
					}
				}
			}

			public virtual void Log()
			{
				outstream.WriteLine(string.Format("Maximum memory used: %.1f GB", peak / Gigabyte));
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Thread pmm = new Thread(new MemoryMonitor.PeakMemoryMonitor());
			pmm.Start();
			long time = Runtime.CurrentTimeMillis();
			MemoryMonitor mm = new MemoryMonitor();
			long time2 = Runtime.CurrentTimeMillis();
			System.Console.Out.WriteLine("Created MemoryMonitor.  Took " + (time2 - time) + " milliseconds.");
			System.Console.Out.WriteLine(mm);
			time = Runtime.CurrentTimeMillis();
			mm.PollVMstat(true);
			time2 = Runtime.CurrentTimeMillis();
			System.Console.Out.WriteLine("Second Poll.  Took " + (time2 - time) + " milliseconds.");
			System.Console.Out.WriteLine(mm);
			pmm.Interrupt();
			pmm.Join();
		}
	}
}
