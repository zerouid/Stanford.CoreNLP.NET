using System;
using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Type signature for a class that supports the basic operations required
	/// of a typesafe heterogeneous map.
	/// </summary>
	/// <author>dramage</author>
	public interface ITypesafeMap
	{
		/// <summary>Base type of keys for the map.</summary>
		/// <remarks>
		/// Base type of keys for the map.  The classes that implement Key are
		/// the keys themselves - not instances of those classes.
		/// </remarks>
		/// <?/>
		public interface IKey<Value>
		{
		}

		/// <summary>
		/// Returns the value associated with the given key or null if
		/// none is provided.
		/// </summary>
		VALUE Get<Value>(Type key);

		/// <summary>
		/// Associates the given value with the given type for future calls
		/// to get.
		/// </summary>
		/// <remarks>
		/// Associates the given value with the given type for future calls
		/// to get.  Returns the value removed or null if no value was present.
		/// </remarks>
		VALUE Set<Value>(Type key, VALUE value);

		/// <summary>Removes the given key from the map, returning the value removed.</summary>
		VALUE Remove<Value>(Type key);

		/// <summary>Collection of keys currently held in this map.</summary>
		/// <remarks>
		/// Collection of keys currently held in this map.  Some implementations may
		/// have the returned set be immutable.
		/// </remarks>
		ICollection<Type> KeySet();

		// Set<Class<? extends Key<?>>> keySet();
		/// <summary>Returns true if contains the given key.</summary>
		bool ContainsKey<Value>(Type key);

		/// <summary>Returns the number of keys in the map.</summary>
		int Size();
	}

	public static class TypesafeMapConstants
	{
	}
}
