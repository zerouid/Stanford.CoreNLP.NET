using System;
using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// An extension of
	/// <see cref="ArrayCoreMap"/>
	/// with an immutable set of key,value
	/// pairs that is used for equality and hashcode comparisons.
	/// </summary>
	/// <author>dramage</author>
	[System.Serializable]
	public class HashableCoreMap : ArrayCoreMap
	{
		/// <summary>Set of immutable keys</summary>
		private readonly ICollection<Type> immutableKeys;

		/// <summary>Pre-computed hashcode</summary>
		private readonly int hashcode;

		/// <summary>
		/// Creates an instance of HashableCoreMap with initial key,value pairs
		/// for the immutable, hashable keys as provided in the given map.
		/// </summary>
		public HashableCoreMap(IDictionary<Type, object> hashkey)
		{
			int keyHashcode = 0;
			int valueHashcode = 0;
			foreach (KeyValuePair<Type, object> entry in hashkey)
			{
				// NB it is important to compose these hashcodes in an order-independent
				// way, so we just add them all here.
				keyHashcode += entry.Key.GetHashCode();
				valueHashcode += entry.Value.GetHashCode();
				base.Set((Type)entry.Key, entry.Value);
			}
			this.immutableKeys = hashkey.Keys;
			this.hashcode = keyHashcode * 31 + valueHashcode;
		}

		/// <summary>
		/// Creates an instance by copying values from the given other CoreMap,
		/// using the values it associates with the given set of hashkeys for
		/// the immutable, hashable keys used by hashcode and equals.
		/// </summary>
		public HashableCoreMap(ArrayCoreMap other, ICollection<Type> hashkey)
			: base(other)
		{
			int keyHashcode = 0;
			int valueHashcode = 0;
			foreach (Type key in hashkey)
			{
				// NB it is important to compose these hashcodes in an order-independent
				// way, so we just add them all here.
				keyHashcode += key.GetHashCode();
				valueHashcode += base.Get((Type)key).GetHashCode();
			}
			this.immutableKeys = hashkey;
			this.hashcode = keyHashcode * 31 + valueHashcode;
		}

		/// <summary>
		/// Sets the value associated with the given key; if the the key is one
		/// of the hashable keys, throws an exception.
		/// </summary>
		/// <exception cref="HashableCoreMapException">
		/// Attempting to set the value for an
		/// immutable, hashable key.
		/// </exception>
		public override VALUE Set<Value>(Type key, VALUE value)
		{
			if (immutableKeys.Contains(key))
			{
				throw new HashableCoreMap.HashableCoreMapException("Attempt to change value " + "of immutable field " + key.GetSimpleName());
			}
			return base.Set(key, value);
		}

		/// <summary>
		/// Provides a hash code based on the immutable keys and values provided
		/// to the constructor.
		/// </summary>
		public override int GetHashCode()
		{
			return hashcode;
		}

		/// <summary>
		/// If the provided object is a HashableCoreMap, equality is based only
		/// upon the values of the immutable hashkeys; otherwise, defaults to
		/// behavior of the superclass's equals method.
		/// </summary>
		public override bool Equals(object o)
		{
			if (o is Edu.Stanford.Nlp.Util.HashableCoreMap)
			{
				Edu.Stanford.Nlp.Util.HashableCoreMap other = (Edu.Stanford.Nlp.Util.HashableCoreMap)o;
				if (!other.immutableKeys.Equals(this.immutableKeys))
				{
					return false;
				}
				foreach (Type key in immutableKeys)
				{
					if (!this.Get((Type)key).Equals(other.Get((Type)key)))
					{
						return false;
					}
				}
				return true;
			}
			else
			{
				return base.Equals(o);
			}
		}

		private const long serialVersionUID = 1L;

		/// <summary>
		/// An exception thrown when attempting to change the value associated
		/// with an (immutable) hash key in a HashableCoreMap.
		/// </summary>
		/// <author>dramage</author>
		[System.Serializable]
		public class HashableCoreMapException : Exception
		{
			public HashableCoreMapException(string message)
				: base(message)
			{
			}

			private const long serialVersionUID = 1L;
			//
			// Exception type
			//
		}
	}
}
