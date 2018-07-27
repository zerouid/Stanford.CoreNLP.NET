using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Org.Slf4j;


namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A handler for outputting to SLF4J rather than stderr.</summary>
	/// <author>Gabor Angeli</author>
	public class SLF4JHandler : OutputHandler
	{
		// Called via reflection from RedwoodConfiguration
		private static Pair<ILogger, Redwood.Flag> GetLoggerAndLevel(object[] channel)
		{
			Pair<string, Redwood.Flag> pair = GetSourceStringAndLevel(channel);
			// Get the logger for slf4j
			ILogger impl = LoggerFactory.GetLogger(pair.First());
			return Pair.MakePair(impl, pair.Second());
		}

		/// <summary>
		/// Override the raw handle method, as potentially we are dropping log levels in SLF4J
		/// and we do not want to render the resulting message.
		/// </summary>
		/// <param name="record">The record to handle.</param>
		/// <returns>Nothing -- this is the leaf of a tree.</returns>
		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			// Get the implementing SLF4J logger
			Pair<ILogger, Redwood.Flag> loggerAndLevel = GetLoggerAndLevel(record.Channels());
			switch (loggerAndLevel.second)
			{
				case Redwood.Flag.Force:
				{
					// Potentially short-circuit
					break;
				}

				case Redwood.Flag.Error:
				{
					// Always pass it on if explicitly forced
					if (!loggerAndLevel.first.IsErrorEnabled())
					{
						return Java.Util.Collections.EmptyList();
					}
					break;
				}

				case Redwood.Flag.Warn:
				{
					if (!loggerAndLevel.first.IsWarnEnabled())
					{
						return Java.Util.Collections.EmptyList();
					}
					break;
				}

				case Redwood.Flag.Debug:
				{
					if (!loggerAndLevel.first.IsDebugEnabled())
					{
						return Java.Util.Collections.EmptyList();
					}
					break;
				}

				default:
				{
					if (!loggerAndLevel.first.IsInfoEnabled())
					{
						return Java.Util.Collections.EmptyList();
					}
					break;
				}
			}
			return base.Handle(record);
		}

		public override void Print(object[] channel, string line)
		{
			// Get the implementing SLF4J logger
			Pair<ILogger, Redwood.Flag> loggerAndLevel = GetLoggerAndLevel(channel);
			// Format the line
			if (line.Length > 0 && line[line.Length - 1] == '\n')
			{
				line = Sharpen.Runtime.Substring(line, 0, line.Length - 1);
			}
			switch (loggerAndLevel.second)
			{
				case Redwood.Flag.Error:
				{
					// Route the signal
					loggerAndLevel.first.Error(line);
					break;
				}

				case Redwood.Flag.Warn:
				{
					loggerAndLevel.first.Warn(line);
					break;
				}

				case Redwood.Flag.Debug:
				{
					loggerAndLevel.first.Debug(line);
					break;
				}

				case Redwood.Flag.Stdout:
				case Redwood.Flag.Stderr:
				{
					loggerAndLevel.first.Info(line);
					break;
				}

				case Redwood.Flag.Force:
				{
					throw new InvalidOperationException("Should not reach this switch case");
				}

				default:
				{
					throw new InvalidOperationException("Unknown Redwood flag for slf4j integration: " + loggerAndLevel.second);
				}
			}
		}
	}
}
