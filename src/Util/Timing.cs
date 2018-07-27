using System.IO;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A class for measuring how long things take.</summary>
	/// <remarks>
	/// A class for measuring how long things take.  For backward
	/// compatibility, this class contains static methods, but the
	/// preferred usage is to instantiate a Timing object and use instance
	/// methods.
	/// <p>To use, call
	/// <see cref="StartTime()"/>
	/// before running the code in
	/// question. Call
	/// <see cref="Tick()"/>
	/// to print an intermediate update, and
	/// <see cref="EndTime()"/>
	/// to
	/// finish the timing and print the result. You can optionally pass a descriptive
	/// string and
	/// <c>PrintStream</c>
	/// to
	/// <c>tick</c>
	/// and
	/// <c>endTime</c>
	/// for more control over what gets printed where.</p>
	/// <p>Example: time reading in a big file and transforming it:</p>
	/// <p><code>Timing.startTime();<br />
	/// String bigFileContents = IOUtils.slurpFile(bigFile);<br />
	/// Timing.tick(&quot;read in big file&quot;, System.err);<br />
	/// String output = costlyTransform(bigFileContents);<br />
	/// Timing.endTime(&quot;transformed big file&quot;, System.err);</code></p>
	/// </remarks>
	/// <author>Bill MacCartney</author>
	public class Timing
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.Timing));

		private const long MillisecondsToSeconds = 1000L;

		private const long SecondDivisor = 1000000000L;

		private const long MillisecondDivisor = 1000000L;

		/// <summary>Stores the time at which the timer was started.</summary>
		/// <remarks>Stores the time at which the timer was started. Now stored as nanoseconds.</remarks>
		private long start;

		/// <summary>Stores the time at which the (static) timer was started.</summary>
		/// <remarks>Stores the time at which the (static) timer was started. Stored as nanoseconds.</remarks>
		private static long startTime = Runtime.NanoTime();

		/// <summary>Stores a suitable formatter for printing seconds nicely.</summary>
		private static readonly NumberFormat nf = new DecimalFormat("0.0", DecimalFormatSymbols.GetInstance(Locale.Root));

		/// <summary>Constructs new Timing object and starts the timer.</summary>
		public Timing()
		{
			this.Start();
		}

		// start ==========================================================
		/// <summary>Start timer.</summary>
		public virtual void Start()
		{
			start = Runtime.NanoTime();
		}

		// report =========================================================
		/// <summary>Return elapsed time (without stopping timer).</summary>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Report()
		{
			return (Runtime.NanoTime() - start) / MillisecondDivisor;
		}

		/// <summary>Return elapsed time (without stopping timer).</summary>
		/// <returns>Number of nanoseconds elapsed</returns>
		public virtual long ReportNano()
		{
			return Runtime.NanoTime() - start;
		}

		/// <summary>Print elapsed time (without stopping timer).</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="stream">PrintStream on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Report(string str, TextWriter stream)
		{
			long elapsed = this.Report();
			stream.WriteLine(str + " Time elapsed: " + elapsed + " ms");
			return elapsed;
		}

		/// <summary>
		/// Print elapsed time to
		/// <c>System.err</c>
		/// (without stopping timer).
		/// </summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Report(string str)
		{
			return this.Report(str, System.Console.Error);
		}

		/// <summary>Print elapsed time (without stopping timer).</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="writer">PrintWriter on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Report(string str, PrintWriter writer)
		{
			long elapsed = this.Report();
			writer.Println(str + " Time elapsed: " + (elapsed) + " ms");
			return elapsed;
		}

		/// <summary>Returns the number of seconds passed since the timer started in the form "d.d".</summary>
		public virtual string ToSecondsString()
		{
			return ToSecondsString(Report());
		}

		/// <summary>Format with one decimal place elapsed milliseconds in seconds.</summary>
		/// <param name="elapsed">Number of milliseconds elapsed</param>
		/// <returns>Formatted String</returns>
		public static string ToSecondsString(long elapsed)
		{
			return nf.Format(((double)elapsed) / MillisecondsToSeconds);
		}

		/// <summary>Format with one decimal place elapsed milliseconds.</summary>
		/// <param name="elapsed">Number of milliseconds elapsed</param>
		/// <returns>Formatted String</returns>
		public static string ToMilliSecondsString(long elapsed)
		{
			return nf.Format(elapsed);
		}

		// restart ========================================================
		/// <summary>Restart timer.</summary>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Restart()
		{
			long elapsed = this.Report();
			this.Start();
			return elapsed;
		}

		/// <summary>Print elapsed time and restart timer.</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="stream">PrintStream on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Restart(string str, TextWriter stream)
		{
			long elapsed = this.Report(str, stream);
			this.Start();
			return elapsed;
		}

		/// <summary>
		/// Print elapsed time to
		/// <c>System.err</c>
		/// and restart timer.
		/// </summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Restart(string str)
		{
			return this.Restart(str, System.Console.Error);
		}

		/// <summary>Print elapsed time and restart timer.</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="writer">PrintWriter on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Restart(string str, PrintWriter writer)
		{
			long elapsed = this.Report(str, writer);
			this.Start();
			return elapsed;
		}

		/// <summary>Print the timing done message with elapsed time in x.y seconds.</summary>
		/// <remarks>
		/// Print the timing done message with elapsed time in x.y seconds.
		/// Restart the timer too.
		/// </remarks>
		public virtual void End(string msg)
		{
			long elapsed = Runtime.NanoTime() - start;
			log.Info(msg + " done [" + nf.Format(((double)elapsed) / SecondDivisor) + " sec].");
			this.Start();
		}

		// stop ===========================================================
		/// <summary>Stop timer.</summary>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Stop()
		{
			long elapsed = this.Report();
			this.start = 0;
			return elapsed;
		}

		/// <summary>Print elapsed time and stop timer.</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="stream">PrintStream on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Stop(string str, TextWriter stream)
		{
			this.Report(str, stream);
			return this.Stop();
		}

		/// <summary>
		/// Print elapsed time to
		/// <c>System.err</c>
		/// and stop timer.
		/// </summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Stop(string str)
		{
			return Stop(str, System.Console.Error);
		}

		/// <summary>Print elapsed time and stop timer.</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="writer">PrintWriter on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public virtual long Stop(string str, PrintWriter writer)
		{
			this.Report(str, writer);
			return this.Stop();
		}

		// startTime ======================================================
		/// <summary>Start (static) timer.</summary>
		public static void StartTime()
		{
			startTime = Runtime.NanoTime();
		}

		// endTime ========================================================
		/// <summary>Return elapsed time on (static) timer (without stopping timer).</summary>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long EndTime()
		{
			return (Runtime.NanoTime() - startTime) / MillisecondDivisor;
		}

		/// <summary>Print elapsed time on (static) timer (without stopping timer).</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="stream">PrintStream on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long EndTime(string str, TextWriter stream)
		{
			long elapsed = EndTime();
			stream.WriteLine(str + " Time elapsed: " + (elapsed) + " ms");
			return elapsed;
		}

		/// <summary>
		/// Print elapsed time on (static) timer to
		/// <c>System.err</c>
		/// (without stopping timer).
		/// </summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long EndTime(string str)
		{
			return EndTime(str, System.Console.Error);
		}

		// chris' new preferred methods 2006 for loading things etc.
		/// <summary>Print the start of timing message to stderr and start the timer.</summary>
		public virtual void Doing(string str)
		{
			log.Info(str + " ... ");
			Start();
		}

		/// <summary>
		/// Finish the line from doing() with the end of the timing done message
		/// and elapsed time in x.y seconds.
		/// </summary>
		public virtual void Done()
		{
			log.Info("done [" + ToSecondsString() + " sec].");
		}

		/// <summary>Give a line saying that something is " done".</summary>
		public virtual void Done(string msg)
		{
			log.Info(msg + " done [" + ToSecondsString() + " sec].");
		}

		public virtual void Done(StringBuilder msg)
		{
			msg.Append(" done [").Append(ToSecondsString()).Append(" sec].");
			log.Info(msg.ToString());
		}

		/// <summary>This method allows you to show the results of timing according to another class' logger.</summary>
		/// <remarks>
		/// This method allows you to show the results of timing according to another class' logger.
		/// E.g.,
		/// <c>timing.done(logger, "Loading lexicon")</c>
		/// .
		/// </remarks>
		/// <param name="logger">Logger to log a timed operation with</param>
		/// <param name="msg">Message to report.</param>
		public virtual void Done(Redwood.RedwoodChannels logger, StringBuilder msg)
		{
			msg.Append("... done [").Append(ToSecondsString()).Append(" sec].");
			logger.Info(msg.ToString());
		}

		public virtual void Done(Redwood.RedwoodChannels logger, string msg)
		{
			logger.Info(msg + " ... done [" + ToSecondsString() + " sec].");
		}

		/// <summary>Print the start of timing message to stderr and start the timer.</summary>
		public static void StartDoing(string str)
		{
			log.Info(str + " ... ");
			StartTime();
		}

		/// <summary>
		/// Finish the line from startDoing with the end of the timing done message
		/// and elapsed time in x.y seconds.
		/// </summary>
		public static void EndDoing()
		{
			long elapsed = Runtime.NanoTime() - startTime;
			log.Info("done [" + nf.Format(((double)elapsed) / SecondDivisor) + " sec].");
		}

		/// <summary>
		/// Finish the line from startDoing with the end of the timing done message
		/// and elapsed time in x.y seconds.
		/// </summary>
		public static void EndDoing(string msg)
		{
			long elapsed = Runtime.NanoTime() - startTime;
			log.Info(msg + " done [" + nf.Format(((double)elapsed) / SecondDivisor) + " sec].");
		}

		// tick ===========================================================
		/// <summary>Restart (static) timer.</summary>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long Tick()
		{
			long elapsed = (Runtime.NanoTime() - startTime) / MillisecondDivisor;
			StartTime();
			return elapsed;
		}

		/// <summary>Print elapsed time and restart (static) timer.</summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <param name="stream">PrintStream on which to write output</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long Tick(string str, TextWriter stream)
		{
			long elapsed = Tick();
			stream.WriteLine(str + " Time elapsed: " + (elapsed) + " ms");
			return elapsed;
		}

		/// <summary>
		/// Print elapsed time to
		/// <c>System.err</c>
		/// and restart (static) timer.
		/// </summary>
		/// <param name="str">Additional prefix string to be printed</param>
		/// <returns>Number of milliseconds elapsed</returns>
		public static long Tick(string str)
		{
			return Tick(str, System.Console.Error);
		}

		// import java.util.Calendar;
		// import java.util.TimeZone;
		// // Calendar cal = Calendar.getInstance(TimeZone.getTimeZone("EST"));
		//  Calendar cal = Calendar.getInstance(TimeZone.getDefault());
		//  String DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
		//  java.text.SimpleDateFormat sdf = new java.text.SimpleDateFormat(DATE_FORMAT);
		// // sdf.setTimeZone(TimeZone.getTimeZone("EST"));
		// sdf.setTimeZone(TimeZone.getDefault());
		// System.out.println("Now : " + sdf.format(cal.getTime()));
		public override string ToString()
		{
			return "Timing[start=" + startTime + ']';
		}
	}
}
