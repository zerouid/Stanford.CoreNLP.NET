using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>
	/// A class to redirect the output of Redwood to another logging mechanism,
	/// e.g., java.util.logging.
	/// </summary>
	/// <author>Gabor Angeli</author>
	public class RedirectOutputHandler<LoggerClass, ChannelEquivalent> : OutputHandler
	{
		public readonly LoggerClass logger;

		public readonly MethodInfo loggingMethod;

		private readonly IDictionary<object, ChannelEquivalent> channelMapping;

		private readonly ChannelEquivalent defaultChannel;

		/// <summary>
		/// Create a redirect handler, with a logging class, ignoring logging
		/// levels.
		/// </summary>
		/// <param name="logger">The class to use for logging. For example, java.util.logging.Logger</param>
		/// <param name="loggingMethod">
		/// A method which takes a *single* String argument
		/// and logs that string using the |logger| class.
		/// </param>
		public RedirectOutputHandler(LoggerClass logger, MethodInfo loggingMethod)
			: this(logger, loggingMethod, null, null)
		{
		}

		/// <summary>
		/// Create a redirect handler, with a logging class, redirecting both the logging
		/// message, and the channel that it came from
		/// </summary>
		/// <param name="logger">
		/// The class to use for logging. For example,
		/// java.util.logging.Logger
		/// </param>
		/// <param name="loggingMethod">
		/// A method which takes a *single* String argument
		/// and logs that string using the |logger| class.
		/// </param>
		/// <param name="channelMapping">The mapping from Redwood channels, to the native Channel equivalent.</param>
		public RedirectOutputHandler(LoggerClass logger, MethodInfo loggingMethod, IDictionary<object, ChannelEquivalent> channelMapping, ChannelEquivalent defaultChannel)
		{
			this.logger = logger;
			this.loggingMethod = loggingMethod;
			this.channelMapping = channelMapping;
			this.defaultChannel = defaultChannel;
		}

		private bool ShouldLogChannels()
		{
			return channelMapping != null;
		}

		public override void Print(object[] channels, string line)
		{
			if (line.EndsWith("\n"))
			{
				line = Sharpen.Runtime.Substring(line, 0, line.Length - 1);
			}
			if (ShouldLogChannels())
			{
				// -- Case: log with channel
				// (get channel to publish on)
				ChannelEquivalent channel = null;
				if (channels == null)
				{
					// (case: no channel provided)
					channel = defaultChannel;
				}
				else
				{
					foreach (object candidate in channels)
					{
						if (channel == null)
						{
							// (case: channel found in mapping)
							channel = channelMapping[candidate];
						}
					}
					if (channel == null)
					{
						// (case: no channel found in mapping)
						channel = this.defaultChannel;
					}
				}
				// (publish message)
				try
				{
					this.loggingMethod.Invoke(this.logger, channel, line);
				}
				catch (MemberAccessException e)
				{
					throw new InvalidOperationException(e);
				}
				catch (TargetInvocationException e)
				{
					throw new InvalidOperationException(e.InnerException);
				}
			}
			else
			{
				// -- Case: log without channel
				try
				{
					this.loggingMethod.Invoke(this.logger, line);
				}
				catch (MemberAccessException e)
				{
					throw new InvalidOperationException(e);
				}
				catch (TargetInvocationException e)
				{
					throw new InvalidOperationException(e.InnerException);
				}
			}
		}

		/// <summary>Ensure that we don't print duplicate channels when adapting to another logging framework.</summary>
		/// <inheritDoc/>
		protected internal override bool FormatChannel(StringBuilder b, string channelStr, object channel)
		{
			return !(channelMapping != null && channelMapping.Contains(channel));
		}

		//
		// LOGGER IMPLEMENTATIONS
		//
		public static Edu.Stanford.Nlp.Util.Logging.RedirectOutputHandler<Logger, Level> FromJavaUtilLogging(Logger logger)
		{
			IDictionary<object, Level> channelMapping = Generics.NewHashMap();
			channelMapping[Redwood.Warn] = Level.Warning;
			channelMapping[Redwood.Dbg] = Level.Fine;
			channelMapping[Redwood.Err] = Level.Severe;
			try
			{
				return new Edu.Stanford.Nlp.Util.Logging.RedirectOutputHandler<Logger, Level>(logger, typeof(Logger).GetMethod("log", typeof(Level), typeof(string)), channelMapping, Level.Info);
			}
			catch (MissingMethodException e)
			{
				throw new InvalidOperationException(e);
			}
		}
	}
}
