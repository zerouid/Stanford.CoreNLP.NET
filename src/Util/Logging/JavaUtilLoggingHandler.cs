using System;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>An outputter that writes to Java Util Logging logs.</summary>
	/// <author>Gabor Angeli</author>
	public class JavaUtilLoggingHandler : OutputHandler
	{
		public override void Print(object[] channel, string line)
		{
			// Parse the channels
			Pair<string, Redwood.Flag> pair = GetSourceStringAndLevel(channel);
			// Get the logger
			Logger impl = Logger.GetLogger(pair.First());
			switch (pair.Second())
			{
				case Redwood.Flag.Error:
				{
					// Route the signal
					impl.Log(Level.Severe, line);
					break;
				}

				case Redwood.Flag.Warn:
				{
					impl.Log(Level.Warning, line);
					break;
				}

				case Redwood.Flag.Debug:
				{
					impl.Log(Level.Fine, line);
					break;
				}

				case Redwood.Flag.Stdout:
				case Redwood.Flag.Stderr:
				{
					impl.Info(line);
					break;
				}

				case Redwood.Flag.Force:
				{
					throw new InvalidOperationException("Should not reach this switch case");
				}
			}
		}
		// Not possible as now enum
		// default:
		//   throw new IllegalStateException("Unknown Redwood flag for j.u.l integration: " + flag);
	}
}
