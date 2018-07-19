using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	public interface IAbstractCoreLabel : IAbstractToken, ILabel, ITypesafeMap
	{
		/// <summary>Return a non-null String value for a key.</summary>
		/// <remarks>
		/// Return a non-null String value for a key. This method is included
		/// for backwards compatibility with the removed class AbstractMapLabel.
		/// It is guaranteed to not return null; if the key is not present or
		/// has a null value, it returns the empty string ("").  It is only valid to
		/// call this method when key is paired with a value of type String.
		/// </remarks>
		/// <?/>
		/// <param name="key">The key to return the value of.</param>
		/// <returns>
		/// "" if the key is not in the map or has the value
		/// <see langword="null"/>
		/// and the String value of the key otherwise
		/// </returns>
		string GetString<Key>()
			where Key : TypesafeMap.IKey<string>;

		string GetString<Key>(string def)
			where Key : TypesafeMap.IKey<string>;
	}
}
