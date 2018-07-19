using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	public class StanfordRedwoodConfiguration : RedwoodConfiguration
	{
		/// <summary>Private constructor to prevent use of "new StanfordRedwoodConfiguration()"</summary>
		private StanfordRedwoodConfiguration()
			: base()
		{
		}

		/// <summary>
		/// Configures the Redwood logger using a reasonable set of defaults,
		/// which can be overruled by the supplied Properties file.
		/// </summary>
		/// <param name="props">The properties file to overrule or augment the default configuration</param>
		public static void Apply(Properties props)
		{
			//--Tweak Properties
			//(output to stderr)
			if (props.GetProperty("log.output") == null)
			{
				props.SetProperty("log.output", "stderr");
			}
			//(capture system streams)
			if (props.GetProperty("log.captureStderr") == null)
			{
				props.SetProperty("log.captureStderr", "true");
			}
			//(apply properties)
			RedwoodConfiguration.Apply(props);
			//--Strange Tweaks
			//(adapt legacy logging systems)
			JavaUtilLoggingAdaptor.Adapt();
		}

		/// <summary>Set up the Redwood logger with Stanford's default configuration</summary>
		public static void Setup()
		{
			Apply(new Properties());
		}

		public static void MinimalSetup()
		{
			Properties props = new Properties();
			props.SetProperty("log.output", "stderr");
			RedwoodConfiguration.Apply(props);
		}
	}
}
