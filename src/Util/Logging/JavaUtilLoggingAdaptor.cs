


namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Reroutes java.util.logging messages to the Redwood logging system.</summary>
	/// <author>David McClosky</author>
	public class JavaUtilLoggingAdaptor
	{
		private static bool addedRedwoodHandler;

		private JavaUtilLoggingAdaptor()
		{
		}

		// = false;
		public static void Adapt()
		{
			// get the top Logger:
			Logger topLogger = Logger.GetLogger(string.Empty);
			Handler oldConsoleHandler = null;
			// see if there is already a console handler
			// hopefully reasonable assumption: there's only one ConsoleHandler
			// TODO confirm that this will always give us all handlers (i.e. do we need to loop over all Loggers in java.util.LogManager and do this for each one?)
			foreach (Handler handler in topLogger.GetHandlers())
			{
				if (handler is ConsoleHandler && !(handler is JavaUtilLoggingAdaptor.RedwoodHandler))
				{
					// found the console handler
					oldConsoleHandler = handler;
					break;
				}
			}
			if (oldConsoleHandler != null)
			{
				// it's safe to call this after it's been removed
				topLogger.RemoveHandler(oldConsoleHandler);
			}
			if (!addedRedwoodHandler)
			{
				Handler redwoodHandler = new JavaUtilLoggingAdaptor.RedwoodHandler();
				topLogger.AddHandler(redwoodHandler);
				addedRedwoodHandler = true;
			}
		}

		/// <summary>This is the bridge class which actually adapts java.util.logging calls to Redwood calls.</summary>
		public class RedwoodHandler : ConsoleHandler
		{
			/// <summary>This is a no-op since Redwood doesn't have this.</summary>
			/// <exception cref="System.Security.SecurityException"/>
			public override void Close()
			{
			}

			/// <summary>This is a no-op since Redwood doesn't have this.</summary>
			public override void Flush()
			{
			}

			/// <summary>Convert a java.util.logging call to its equivalent Redwood logging call.</summary>
			/// <remarks>
			/// Convert a java.util.logging call to its equivalent Redwood logging call.
			/// Currently, the WARNING log level becomes Redwood WARNING flag, the SEVERE log level becomes Redwood.ERR, and anything at FINE or lower becomes Redwood.DBG
			/// CONFIG and INFO don't map to a Redwood tag.
			/// </remarks>
			public override void Publish(LogRecord record)
			{
				string message = record.GetMessage();
				Level level = record.GetLevel();
				object tag = null;
				if (level == Level.Warning)
				{
					tag = Redwood.Warn;
				}
				else
				{
					if (level == Level.Severe)
					{
						tag = Redwood.Err;
					}
					else
					{
						if (level.IntValue() <= Level.Fine.IntValue())
						{
							tag = Redwood.Dbg;
						}
					}
				}
				if (tag == null)
				{
					Redwood.Log(message);
				}
				else
				{
					Redwood.Log(tag, message);
				}
			}
		}

		/// <summary>Simple test case.</summary>
		public static void Main(string[] args)
		{
			if (args.Length > 0 && args[0].Equals("redwood"))
			{
				Redwood.Log(Redwood.Dbg, "at the top");
				Redwood.StartTrack("Adaptor test controlled by redwood");
				Logger topLogger = Logger.GetLogger(Logger.GlobalLoggerName);
				topLogger.Warning("I'm warning you!");
				topLogger.Severe("Now I'm using my severe voice.");
				topLogger.Info("FYI");
				Redwood.Log(Redwood.Dbg, "adapting");
				JavaUtilLoggingAdaptor.Adapt();
				topLogger.Warning("I'm warning you in Redwood!");
				JavaUtilLoggingAdaptor.Adapt();
				// should be safe to call this twice
				topLogger.Severe("Now I'm using my severe voice in Redwood!");
				topLogger.Info("FYI: Redwood rocks");
				// make sure original java.util.logging levels are respected
				topLogger.SetLevel(Level.Off);
				topLogger.Severe("We shouldn't see this message.");
				Redwood.Log(Redwood.Dbg, "at the bottom");
				Redwood.EndTrack("Adaptor test controlled by redwood");
			}
			else
			{
				// Reverse mapping
				Logger topLogger = Logger.GetLogger(Logger.GlobalLoggerName);
				// Can be Logger.getGlobal() in jdk1.7
				// topLogger.addHandler(new ConsoleHandler());
				Logger logger = Logger.GetLogger(typeof(JavaUtilLoggingAdaptor).FullName);
				topLogger.Info("Starting test");
				logger.Log(Level.Info, "Hello from the class logger");
				Redwood.Log("Hello from Redwood!");
				Redwood.RootHandler().AddChild(RedirectOutputHandler.FromJavaUtilLogging(topLogger));
				Redwood.Log("Hello from Redwood -> Java!");
				Redwood.Log("Hello from Redwood -> Java again!");
				logger.Log(Level.Info, "Hello again from the class logger");
				Redwood.StartTrack("a track");
				Redwood.Log("Inside a track");
				logger.Log(Level.Info, "Hello a third time from the class logger");
				Redwood.EndTrack("a track");
				logger.Log(Level.Info, "Hello a fourth time from the class logger");
			}
		}
	}
}
