


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A do-nothing Preferences implementation so that we can avoid the hassles
	/// of the JVM Preference implementations.
	/// </summary>
	/// <remarks>
	/// A do-nothing Preferences implementation so that we can avoid the hassles
	/// of the JVM Preference implementations.
	/// Taken from: http://www.allaboutbalance.com/disableprefs/index.html
	/// </remarks>
	/// <author>Robert Slifka</author>
	/// <version>2003/03/24</version>
	public class DisabledPreferences : AbstractPreferences
	{
		public DisabledPreferences()
			: base(null, string.Empty)
		{
		}

		protected override void PutSpi(string key, string value)
		{
		}

		protected override string GetSpi(string key)
		{
			return null;
		}

		protected override void RemoveSpi(string key)
		{
		}

		/// <exception cref="Java.Util.Prefs.BackingStoreException"/>
		protected override void RemoveNodeSpi()
		{
		}

		/// <exception cref="Java.Util.Prefs.BackingStoreException"/>
		protected override string[] KeysSpi()
		{
			return new string[0];
		}

		/// <exception cref="Java.Util.Prefs.BackingStoreException"/>
		protected override string[] ChildrenNamesSpi()
		{
			return new string[0];
		}

		protected override AbstractPreferences ChildSpi(string name)
		{
			return null;
		}

		/// <exception cref="Java.Util.Prefs.BackingStoreException"/>
		protected override void SyncSpi()
		{
		}

		/// <exception cref="Java.Util.Prefs.BackingStoreException"/>
		protected override void FlushSpi()
		{
		}
	}
}
