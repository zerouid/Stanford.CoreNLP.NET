using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A class which encapsulates configuration settings for Redwood.</summary>
	/// <remarks>
	/// A class which encapsulates configuration settings for Redwood.
	/// The class operates on the builder model; that is, you can chain method
	/// calls.
	/// <p>
	/// If you wish to turn off Redwood logging messages altogether you can use:
	/// <c>RedwoodConfiguration.current().clear().apply();</c>
	/// .
	/// <p>
	/// If you need to suppress messages to stderr in a block, you can use:
	/// <pre>
	/// <c>
	/// // shut off annoying messages to stderr
	/// RedwoodConfiguration.empty().capture(System.err).apply();
	/// // block of code that does stuff
	/// // enable stderr again
	/// RedwoodConfiguration.current().clear().apply();
	/// </c>
	/// </pre>
	/// <p>
	/// Alternatively, if Redwood is logging via slf4j (this is the default, if slf4j is present on your classpath),
	/// then you can configure logging using the usual slf4j configuration methods. See, for example,
	/// <a href="https://stackoverflow.com/questions/41761099/mute-stanford-corenlp-logging">this StackOverflow
	/// question</a>. For example, you can add a Properties file
	/// <c>simplelogger.properties</c>
	/// to your classpath
	/// with the line
	/// <c>org.slf4j.simpleLogger.defaultLogLevel=error</c>
	/// and then only ERROR messages will be
	/// printed.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	public class RedwoodConfiguration
	{
		/// <summary>A list of tasks to run when the configuration is applied</summary>
		private LinkedList<IRunnable> tasks = new LinkedList<IRunnable>();

		private OutputHandler outputHandler = Redwood.ConsoleHandler.Out();

		private File defaultFile = new File("/dev/null");

		private int channelWidth = 0;

		/// <summary>Private constructor to prevent use of "new RedwoodConfiguration()"</summary>
		protected internal RedwoodConfiguration()
		{
		}

		/// <summary>Apply this configuration to Redwood</summary>
		public virtual void Apply()
		{
			foreach (IRunnable task in tasks)
			{
				task.Run();
			}
		}

		/// <summary>Capture a system stream.</summary>
		/// <param name="stream">The stream to capture; one of System.out or System.err</param>
		/// <returns>this</returns>
		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration Capture(OutputStream stream)
		{
			// Capture the stream
			if (stream == System.Console.Out)
			{
				tasks.Add(null);
			}
			else
			{
				if (stream == System.Console.Error)
				{
					tasks.Add(null);
				}
				else
				{
					throw new ArgumentException("Must capture one of stderr or stdout");
				}
			}
			return this;
		}

		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration Restore(OutputStream stream)
		{
			if (stream == System.Console.Out)
			{
				tasks.Add(null);
			}
			else
			{
				if (stream == System.Console.Error)
				{
					tasks.Add(null);
				}
				else
				{
					throw new ArgumentException("Must capture one of stderr or stdout");
				}
			}
			return this;
		}

		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration ListenOnChannels(IConsumer<Redwood.Record> listener, params object[] channels)
		{
			return this.Handlers(RedwoodConfiguration.Handlers.Chain(new FilterHandler(Java.Util.Collections.SingletonList(new _ILogFilter_94(channels)), true), null));
		}

		private sealed class _ILogFilter_94 : ILogFilter
		{
			public _ILogFilter_94(object[] channels)
			{
				this.channels = channels;
				this.matchAgainst = new HashSet<object>(Arrays.AsList(channels));
			}

			internal ICollection<object> matchAgainst;

			public bool Matches(Redwood.Record message)
			{
				foreach (object channel in message.Channels())
				{
					if (this.matchAgainst.Contains(channel))
					{
						return true;
					}
				}
				return false;
			}

			private readonly object[] channels;
		}

		/// <summary>Determine where, in the end, console output should go.</summary>
		/// <remarks>
		/// Determine where, in the end, console output should go.
		/// The default is stdout.
		/// </remarks>
		/// <param name="method">An output, one of: stdout, stderr, or java.util.logging</param>
		/// <returns>this</returns>
		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration Output(string method)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(method, "stdout") || Sharpen.Runtime.EqualsIgnoreCase(method, "out"))
			{
				JavaUtilLoggingAdaptor.Adapt();
				this.outputHandler = Redwood.ConsoleHandler.Out();
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(method, "stderr") || Sharpen.Runtime.EqualsIgnoreCase(method, "err"))
				{
					JavaUtilLoggingAdaptor.Adapt();
					this.outputHandler = Redwood.ConsoleHandler.Err();
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(method, "java.util.logging"))
					{
						JavaUtilLoggingAdaptor.Adapt();
						this.outputHandler = RedirectOutputHandler.FromJavaUtilLogging(Logger.GetLogger("``error``"));
					}
					else
					{
						throw new ArgumentException("Unknown value for log.method");
					}
				}
			}
			return this;
		}

		/// <summary>Set the width of the channels (or 0 to not show channels).</summary>
		/// <param name="width">The left margin in which to show channels</param>
		/// <returns>this</returns>
		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration ChannelWidth(int width)
		{
			tasks.AddFirst(null);
			return this;
		}

		/// <summary>Clear any custom configurations to Redwood</summary>
		/// <returns>this</returns>
		public virtual Edu.Stanford.Nlp.Util.Logging.RedwoodConfiguration Clear()
		{
			this.tasks = new LinkedList<IRunnable>();
			this.tasks.Add(null);
			this.outputHandler = Redwood.ConsoleHandler.Out();
			return this;
		}

		public interface IThunk
		{
			void Apply(RedwoodConfiguration config, Redwood.RecordHandlerTree root);
		}

		public class Handlers
		{
			//
			// Leaf destinations
			//
			/// <summary>Output to a file.</summary>
			/// <remarks>
			/// Output to a file. This is a leaf node.
			/// Consider using "defaultFile" instead.
			/// </remarks>
			/// <param name="path">The file to write to</param>
			public static RedwoodConfiguration.IThunk File(string path)
			{
				return new _IThunk_182(path);
			}

			private sealed class _IThunk_182 : RedwoodConfiguration.IThunk
			{
				public _IThunk_182(string path)
				{
					this.path = path;
				}

				public void Apply(RedwoodConfiguration config, Redwood.RecordHandlerTree root)
				{
					root.AddChild(new _FileHandler_185(config, path));
				}

				private sealed class _FileHandler_185 : Redwood.FileHandler
				{
					public _FileHandler_185(RedwoodConfiguration config, string baseArg1)
						: base(baseArg1)
					{
						this.config = config;
						{
							this.leftMargin = config.channelWidth;
						}
					}

					private readonly RedwoodConfiguration config;
				}

				private readonly string path;
			}

			/// <summary>Output to a file.</summary>
			/// <remarks>
			/// Output to a file. This is a leaf node.
			/// Consider using "defaultFile" instead.
			/// </remarks>
			/// <param name="path">The file to write to</param>
			public static RedwoodConfiguration.IThunk File(Java.IO.File path)
			{
				return File(path.GetPath());
			}

			private sealed class _IThunk_199 : RedwoodConfiguration.IThunk
			{
				public _IThunk_199()
				{
				}

				public void Apply(RedwoodConfiguration config, Redwood.RecordHandlerTree root)
				{
					root.AddChild(new _FileHandler_202(config, config.defaultFile.GetPath()));
				}

				private sealed class _FileHandler_202 : Redwood.FileHandler
				{
					public _FileHandler_202(RedwoodConfiguration config, string baseArg1)
						: base(baseArg1)
					{
						this.config = config;
						{
							this.leftMargin = config.channelWidth;
						}
					}

					private readonly RedwoodConfiguration config;
				}
			}

			/// <summary>Output to a file.</summary>
			/// <remarks>
			/// Output to a file. This is a leaf node.
			/// Consider using this instead of specifying a custom path.
			/// </remarks>
			public static readonly RedwoodConfiguration.IThunk defaultFile = new _IThunk_199();

			/// <summary>Output to a standard output.</summary>
			/// <remarks>
			/// Output to a standard output. This is a leaf node.
			/// Consider using "output" instead, unless you really
			/// want to log only to stdout now and forever in the future.
			/// </remarks>
			public static readonly RedwoodConfiguration.IThunk stdout = null;

			/// <summary>Output to a standard error.</summary>
			/// <remarks>
			/// Output to a standard error. This is a leaf node.
			/// Consider using "output" instead, unless you really
			/// want to log only to stderr now and forever in the future.
			/// </remarks>
			public static readonly RedwoodConfiguration.IThunk stderr = null;

			/// <summary>Output to slf4j.</summary>
			/// <remarks>Output to slf4j. This is a leaf node.</remarks>
			public static readonly RedwoodConfiguration.IThunk slf4j = null;

			/// <summary>Output to java.util.Logging.</summary>
			/// <remarks>Output to java.util.Logging. This is a leaf node.</remarks>
			public static readonly RedwoodConfiguration.IThunk javaUtil = null;

			/// <summary>Output to the default location specified by the output() method.</summary>
			/// <remarks>
			/// Output to the default location specified by the output() method.
			/// Consider using this rather than stderr or stdout.
			/// </remarks>
			public static readonly RedwoodConfiguration.IThunk output = null;

			private sealed class _VisibilityHandler_268 : VisibilityHandler
			{
				public _VisibilityHandler_268()
				{
					{
						//
						// Filters
						//
						this.AlsoHide(Redwood.Dbg);
					}
				}
			}

			/// <summary>Hide the debug channel only.</summary>
			public static readonly LogRecordHandler hideDebug = new _VisibilityHandler_268();

			private sealed class _VisibilityHandler_275 : VisibilityHandler
			{
				public _VisibilityHandler_275()
				{
					{
						this.HideAll();
						this.AlsoShow(Redwood.Err);
					}
				}
			}

			/// <summary>Show only errors (e.g., to send them to an error file)</summary>
			public static readonly LogRecordHandler showOnlyError = new _VisibilityHandler_275();

			/// <summary>Hide these channels, in addition to anything already hidden by upstream handlers.</summary>
			public static LogRecordHandler HideChannels(params object[] channelsToHide)
			{
				return new _VisibilityHandler_284(channelsToHide);
			}

			private sealed class _VisibilityHandler_284 : VisibilityHandler
			{
				public _VisibilityHandler_284(object[] channelsToHide)
				{
					this.channelsToHide = channelsToHide;
					{
						foreach (object channel in channelsToHide)
						{
							this.AlsoHide(channel);
						}
					}
				}

				private readonly object[] channelsToHide;
			}

			/// <summary>Show all channels (with this handler, there may be upstream handlers).</summary>
			public static LogRecordHandler ShowAllChannels()
			{
				return new VisibilityHandler();
			}

			/// <summary>Show only these channels, as far as downstream handlers are concerned.</summary>
			public static LogRecordHandler ShowOnlyChannels(params object[] channelsToShow)
			{
				return new _VisibilityHandler_302(channelsToShow);
			}

			private sealed class _VisibilityHandler_302 : VisibilityHandler
			{
				public _VisibilityHandler_302(object[] channelsToShow)
				{
					this.channelsToShow = channelsToShow;
					{
						this.HideAll();
						foreach (object channel in channelsToShow)
						{
							this.AlsoShow(channel);
						}
					}
				}

				private readonly object[] channelsToShow;
			}

			/// <summary>Rename a channel to be something else</summary>
			public static LogRecordHandler Reroute(object src, object dst)
			{
				return new RerouteChannel(src, dst);
			}

			/// <summary>Collapse records in a heuristic way to make reading easier.</summary>
			/// <remarks>
			/// Collapse records in a heuristic way to make reading easier. This is particularly relevant to branches which
			/// go to a physical console, or a file which you'd like to keep small.
			/// </remarks>
			public static readonly LogRecordHandler collapseApproximate = new RepeatedRecordHandler(RepeatedRecordHandler.Approximate);

			/// <summary>
			/// Collapse records which are duplicates into a single message, followed by a message detailing how many times
			/// it was repeated.
			/// </summary>
			public static readonly LogRecordHandler collapseExact = new RepeatedRecordHandler(RepeatedRecordHandler.Exact);

			//
			// Combinators
			//
			/// <summary>Send any incoming messages multiple ways.</summary>
			/// <remarks>
			/// Send any incoming messages multiple ways.
			/// For example, you may want to send the same output to console and a file.
			/// </remarks>
			/// <param name="destinations">The destinations for log messages coming into this node.</param>
			public static RedwoodConfiguration.IThunk Branch(params RedwoodConfiguration.IThunk[] destinations)
			{
				return null;
			}

			/// <summary>Apply each of the handlers to incoming log messages, in sequence.</summary>
			/// <param name="handlers">The handlers to apply</param>
			/// <param name="destination">The final destination of the messages, after processing</param>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler[] handlers, RedwoodConfiguration.IThunk destination)
			{
				return new _IThunk_349(destination, handlers);
			}

			private sealed class _IThunk_349 : RedwoodConfiguration.IThunk
			{
				public _IThunk_349(RedwoodConfiguration.IThunk destination, LogRecordHandler[] handlers)
				{
					this.destination = destination;
					this.handlers = handlers;
				}

				private Redwood.RecordHandlerTree BuildChain(RedwoodConfiguration config, LogRecordHandler[] handlers, int i)
				{
					Redwood.RecordHandlerTree rtn = new Redwood.RecordHandlerTree(handlers[i]);
					if (i < handlers.Length - 1)
					{
						rtn.AddChildTree(this.BuildChain(config, handlers, i + 1));
					}
					else
					{
						destination.Apply(config, rtn);
					}
					return rtn;
				}

				public void Apply(RedwoodConfiguration config, Redwood.RecordHandlerTree root)
				{
					if (handlers.Length == 0)
					{
						destination.Apply(config, root);
					}
					else
					{
						root.AddChildTree(this.BuildChain(config, handlers, 0));
					}
				}

				private readonly RedwoodConfiguration.IThunk destination;

				private readonly LogRecordHandler[] handlers;
			}

			/// <seealso cref="Chain(LogRecordHandler[], IThunk)"></seealso>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler handler1, RedwoodConfiguration.IThunk destination)
			{
				return Chain(new LogRecordHandler[] { handler1 }, destination);
			}

			/// <seealso cref="Chain(LogRecordHandler[], IThunk)"></seealso>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler handler1, LogRecordHandler handler2, RedwoodConfiguration.IThunk destination)
			{
				return Chain(new LogRecordHandler[] { handler1, handler2 }, destination);
			}

			/// <seealso cref="Chain(LogRecordHandler[], IThunk)"></seealso>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler handler1, LogRecordHandler handler2, LogRecordHandler handler3, RedwoodConfiguration.IThunk destination)
			{
				return Chain(new LogRecordHandler[] { handler1, handler2, handler3 }, destination);
			}

			/// <seealso cref="Chain(LogRecordHandler[], IThunk)"></seealso>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler handler1, LogRecordHandler handler2, LogRecordHandler handler3, LogRecordHandler handler4, RedwoodConfiguration.IThunk destination)
			{
				return Chain(new LogRecordHandler[] { handler1, handler2, handler3, handler4 }, destination);
			}

			/// <seealso cref="Chain(LogRecordHandler[], IThunk)"></seealso>
			public static RedwoodConfiguration.IThunk Chain(LogRecordHandler handler1, LogRecordHandler handler2, LogRecordHandler handler3, LogRecordHandler handler4, LogRecordHandler handler5, RedwoodConfiguration.IThunk destination)
			{
				return Chain(new LogRecordHandler[] { handler1, handler2, handler3, handler4, handler5 }, destination);
			}

			/// <summary>A NOOP, as the name implies.</summary>
			/// <remarks>A NOOP, as the name implies. Useful for appending to the end of lists to make commas match.</remarks>
			public static RedwoodConfiguration.IThunk noop = null;
		}

		/// <summary>Add handlers to Redwood.</summary>
		/// <remarks>
		/// Add handlers to Redwood. This is the main way to tell Redwood to do stuff.
		/// Use this by calling a combination of methods in Handlers. It may be useful
		/// to "import static RedwoodConfiguration.Handlers.*"
		/// For example:
		/// <pre>
		/// handlers(branch(
		/// chain( hideDebug, collapseApproximate, branch( output, file("stderr.log") ),
		/// chain( showOnlyError, file("err.log") ).
		/// chain( showOnlyChannels("results", "evaluate"), file("results.log") ),
		/// chain( file("redwood.log") ),
		/// noop))
		/// </pre>
		/// </remarks>
		/// <param name="paths">A number of paths to add.</param>
		/// <returns>this</returns>
		public virtual RedwoodConfiguration Handlers(params RedwoodConfiguration.IThunk[] paths)
		{
			foreach (RedwoodConfiguration.IThunk thunk in paths)
			{
				tasks.Add(null);
			}
			return this;
		}

		/// <summary>Close tracks when the JVM shuts down.</summary>
		/// <returns>this</returns>
		public virtual RedwoodConfiguration NeatExit()
		{
			tasks.Add(null);
			return this;
		}

		/// <summary>An empty Redwood configuration.</summary>
		/// <remarks>
		/// An empty Redwood configuration.
		/// Note that without a Console Handler, Redwood will not print anything
		/// </remarks>
		/// <returns>An empty Redwood Configuration object.</returns>
		public static RedwoodConfiguration Empty()
		{
			return new RedwoodConfiguration().Clear();
		}

		/// <summary>A standard  Redwood configuration, which prints to the console with channels.</summary>
		/// <remarks>
		/// A standard  Redwood configuration, which prints to the console with channels.
		/// It does not show debug level messages (but shows warning and error messages).
		/// This is the usual starting point for new configurations.
		/// </remarks>
		/// <returns>A basic Redwood Configuration.</returns>
		public static RedwoodConfiguration Standard()
		{
			return new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.hideDebug, RedwoodConfiguration.Handlers.stderr));
		}

		/// <summary>The default Redwood configuration, which prints to the console without channels.</summary>
		/// <remarks>
		/// The default Redwood configuration, which prints to the console without channels.
		/// It does not show debug level messages (but shows warning and error messages).
		/// This is the usual starting point for new configurations.
		/// </remarks>
		/// <returns>A basic Redwood Configuration.</returns>
		public static RedwoodConfiguration Minimal()
		{
			return new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.HideChannels(), RedwoodConfiguration.Handlers.hideDebug, RedwoodConfiguration.Handlers.stderr));
		}

		/// <summary>Run Redwood with SLF4J as the console backend</summary>
		/// <returns>
		/// A redwood configuration. Remember to call
		/// <see cref="Apply()"/>
		/// .
		/// </returns>
		public static RedwoodConfiguration Slf4j()
		{
			return new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.HideChannels(), RedwoodConfiguration.Handlers.slf4j));
		}

		/// <summary>Run Redwood with SLF4J if available, otherwise with stderr logging at the debug (everything) level.</summary>
		/// <returns>
		/// A redwood configuration. Remember to call
		/// <see cref="Apply()"/>
		/// .
		/// </returns>
		public static RedwoodConfiguration DebugLevel()
		{
			RedwoodConfiguration config;
			try
			{
				MetaClass.Create("org.slf4j.LoggerFactory").CreateInstance();
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.ShowAllChannels(), RedwoodConfiguration.Handlers.slf4j));
			}
			catch (Exception)
			{
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.ShowAllChannels(), RedwoodConfiguration.Handlers.stderr));
			}
			return config;
		}

		/// <summary>Run Redwood with SLF4J if available, otherwise with stderr logging at the warning (and error) level.</summary>
		/// <returns>
		/// A redwood configuration. Remember to call
		/// <see cref="Apply()"/>
		/// .
		/// </returns>
		public static RedwoodConfiguration InfoLevel()
		{
			RedwoodConfiguration config;
			try
			{
				MetaClass.Create("org.slf4j.LoggerFactory").CreateInstance();
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.HideChannels(Redwood.Dbg), RedwoodConfiguration.Handlers.slf4j));
			}
			catch (Exception)
			{
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.HideChannels(Redwood.Dbg), RedwoodConfiguration.Handlers.stderr));
			}
			return config;
		}

		/// <summary>Run Redwood with SLF4J if available, otherwise with stderr logging at the error only level.</summary>
		/// <returns>
		/// A redwood configuration. Remember to call
		/// <see cref="Apply()"/>
		/// .
		/// </returns>
		public static RedwoodConfiguration ErrorLevel()
		{
			RedwoodConfiguration config;
			try
			{
				MetaClass.Create("org.slf4j.LoggerFactory").CreateInstance();
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.showOnlyError, RedwoodConfiguration.Handlers.slf4j));
			}
			catch (Exception)
			{
				config = new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.showOnlyError, RedwoodConfiguration.Handlers.stderr));
			}
			return config;
		}

		/// <summary>Run Redwood with java.util.logging</summary>
		/// <returns>
		/// A redwood configuration. Remember to call
		/// <see cref="Apply()"/>
		/// .
		/// </returns>
		public static RedwoodConfiguration JavaUtilLogging()
		{
			return new RedwoodConfiguration().Clear().Handlers(RedwoodConfiguration.Handlers.Chain(RedwoodConfiguration.Handlers.HideChannels(), RedwoodConfiguration.Handlers.javaUtil));
		}

		/// <summary>
		/// The current Redwood configuration; this is used to make incremental changes
		/// to an existing custom configuration.
		/// </summary>
		/// <returns>The current Redwood configuration.</returns>
		public static RedwoodConfiguration Current()
		{
			return new RedwoodConfiguration();
		}

		/// <summary>Helper for parsing properties.</summary>
		/// <param name="p">The Properties object</param>
		/// <param name="key">The key to retrieve</param>
		/// <param name="defaultValue">The default value if the key does not exist</param>
		/// <param name="used">The set of keys we have seen</param>
		/// <returns>The value of the property at the key</returns>
		private static string Get(Properties p, string key, string defaultValue, ICollection<string> used)
		{
			string rtn = p.GetProperty(key, defaultValue);
			used.Add(key);
			return rtn;
		}

		/// <summary>Configure Redwood (from scratch) based on a Properties file.</summary>
		/// <remarks>
		/// Configure Redwood (from scratch) based on a Properties file.
		/// Currently recognized properties are:
		/// <ul>
		/// <li>log.captureStreams = {true,false}: Capture stdout and stderr and route them through Redwood</li>
		/// <li>log.captureStdout = {true,false}: Capture stdout and route it through Redwood</li>
		/// <li>log.captureStderr = {true,false}: Capture stdout and route it through Redwood</li>
		/// <li>log.channels.width = {number}: Show the channels being logged to, at this width (default: 0; recommended: 20)</li>
		/// <li>log.channels.debug = {true,false}: Show the debugging channel</li>
		/// <li>log.file = By default, write to this file.
		/// <li>log.neatExit = {true,false}: Clean up logs on exception or regular system exit</li>
		/// <li>log.output = {stderr,stdout,java.util.logging}: Output messages to either stderr or stdout by default.</li>
		/// </ul>
		/// </remarks>
		/// <param name="props">The properties to use in configuration</param>
		/// <returns>A new Redwood Configuration based on the passed properties, ignoring any existing custom configuration</returns>
		public static RedwoodConfiguration Parse(Properties props)
		{
			RedwoodConfiguration config = new RedwoodConfiguration().Clear();
			ICollection<string> used = Generics.NewHashSet();
			//--Capture Streams
			if (Sharpen.Runtime.EqualsIgnoreCase(Get(props, "log.captureStreams", "false", used), "true"))
			{
				config = config.Capture(System.Console.Out).Capture(System.Console.Error);
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(Get(props, "log.captureStdout", "false", used), "true"))
			{
				config = config.Capture(System.Console.Out);
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(Get(props, "log.captureStderr", "false", used), "true"))
			{
				config = config.Capture(System.Console.Error);
			}
			//--Collapse
			string collapse = Get(props, "log.collapse", "none", used);
			IList<LogRecordHandler> chain = new LinkedList<LogRecordHandler>();
			if (Sharpen.Runtime.EqualsIgnoreCase(collapse, "exact"))
			{
				chain.Add(new RepeatedRecordHandler(RepeatedRecordHandler.Exact));
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(collapse, "approximate"))
				{
					chain.Add(new RepeatedRecordHandler(RepeatedRecordHandler.Approximate));
				}
				else
				{
					if (!Sharpen.Runtime.EqualsIgnoreCase(collapse, "none"))
					{
						throw new ArgumentException("Unknown collapse mode (Redwood): " + collapse);
					}
				}
			}
			//--Channels.Debug
			bool debug = bool.ParseBoolean(Get(props, "log.channels.debug", "true", used));
			if (!debug)
			{
				chain.Add(RedwoodConfiguration.Handlers.hideDebug);
			}
			//--Channels.Width
			config.ChannelWidth(System.Convert.ToInt32(Get(props, "log.channels.width", "0", used)));
			//--Neat exit
			if (Sharpen.Runtime.EqualsIgnoreCase(Get(props, "log.neatExit", "false", used), "true"))
			{
				config = config.NeatExit();
			}
			//--File
			string outputFile = Get(props, "log.file", null, used);
			if (outputFile != null)
			{
				config.defaultFile = new Java.IO.File(outputFile);
				config = config.Handlers(RedwoodConfiguration.Handlers.defaultFile);
			}
			//--Console
			config = config.Output(Get(props, "log.output", "stdout", used));
			//--Console
			config = config.Handlers(RedwoodConfiguration.Handlers.Chain(Sharpen.Collections.ToArray(chain, new LogRecordHandler[chain.Count]), RedwoodConfiguration.Handlers.output));
			//--Error Check
			foreach (object propAsObj in props.Keys)
			{
				string prop = propAsObj.ToString();
				if (prop.StartsWith("log.") && !used.Contains(prop))
				{
					throw new ArgumentException("Could not find Redwood log property: " + prop);
				}
			}
			//--Return
			return config;
		}

		/// <summary>Parses a properties file and applies it immediately to Redwood</summary>
		/// <param name="props">The properties to apply</param>
		public static void Apply(Properties props)
		{
			Parse(props).Apply();
		}
		/*
		public static void main(String[] args) {
		RedwoodConfiguration.empty().neatExit().capture(System.out).capture(System.err)
		.channelWidth(20)
		.handlers(
		Handlers.chain(Handlers.hideDebug, Handlers.output),
		Handlers.file("/tmp/redwood.log"))
		.apply();
		Redwood.log("foo");
		Redwood.log(Redwood.DBG, "debug");
		System.out.println("Bar");
		log.info("Baz");
		}
		*/
	}
}
