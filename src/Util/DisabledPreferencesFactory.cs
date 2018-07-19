using System.Security;
using Java.Util.Prefs;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Returns do-nothing Preferences implementation.</summary>
	/// <remarks>
	/// Returns do-nothing Preferences implementation.  We don't use this
	/// facility, so we want to avoid the hassles that come with the JVM's
	/// implementation.
	/// Taken from: http://www.allaboutbalance.com/disableprefs/index.html
	/// </remarks>
	/// <author>Robert Slifka</author>
	/// <author>Christopher Manning</author>
	/// <version>2003/03/24</version>
	public class DisabledPreferencesFactory : IPreferencesFactory
	{
		public virtual Preferences SystemRoot()
		{
			return new DisabledPreferences();
		}

		public virtual Preferences UserRoot()
		{
			return new DisabledPreferences();
		}

		public static void Install()
		{
			try
			{
				Runtime.SetProperty("java.util.prefs.PreferencesFactory", "edu.stanford.nlp.util.DisabledPreferencesFactory");
			}
			catch (SecurityException)
			{
			}
		}
		// oh well we couldn't do it...
	}
}
