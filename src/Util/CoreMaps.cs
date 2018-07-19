using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Utility functions for working with
	/// <see cref="ICoreMap"/>
	/// 's.
	/// </summary>
	/// <author>dramage</author>
	/// <author>Gabor Angeli (merge() method)</author>
	public class CoreMaps
	{
		private CoreMaps()
		{
		}

		// static stuff
		/// <summary>
		/// Merge one CoreMap into another -- that is, overwrite and add any keys in
		/// the base CoreMap with those in the one to be merged.
		/// </summary>
		/// <remarks>
		/// Merge one CoreMap into another -- that is, overwrite and add any keys in
		/// the base CoreMap with those in the one to be merged.
		/// This method is functional -- neither of the argument CoreMaps are changed.
		/// </remarks>
		/// <param name="base">The CoreMap to serve as the base (keys in this are lower priority)</param>
		/// <param name="toBeMerged">The CoreMap to merge in (keys in this are higher priority)</param>
		/// <returns>A new CoreMap representing the merge of the two inputs</returns>
		public static ICoreMap Merge(ICoreMap @base, ICoreMap toBeMerged)
		{
			//(variables)
			ICoreMap rtn = new ArrayCoreMap(@base.Size());
			//(copy base)
			foreach (Type key in @base.KeySet())
			{
				rtn.Set(key, @base.Get(key));
			}
			//(merge)
			foreach (Type key_1 in toBeMerged.KeySet())
			{
				rtn.Set(key_1, toBeMerged.Get(key_1));
			}
			//(return)
			return rtn;
		}

		/// <summary>see merge(CoreMap base, CoreMap toBeMerged)</summary>
		public static CoreLabel Merge(CoreLabel @base, CoreLabel toBeMerged)
		{
			//(variables)
			CoreLabel rtn = new CoreLabel(@base.Size());
			//(copy base)
			foreach (Type key in @base.KeySet())
			{
				rtn.Set(key, @base.Get(key));
			}
			//(merge)
			foreach (Type key_1 in toBeMerged.KeySet())
			{
				rtn.Set(key_1, toBeMerged.Get(key_1));
			}
			//(return)
			return rtn;
		}

		/// <summary>
		/// Returns a view of a collection of CoreMaps as a Map from each CoreMap to
		/// the value it stores under valueKey.
		/// </summary>
		/// <remarks>
		/// Returns a view of a collection of CoreMaps as a Map from each CoreMap to
		/// the value it stores under valueKey. Changes to the map are propagated
		/// directly to the coremaps in the collection and to the collection itself in
		/// the case of removal operations.  Keys added or removed from the given
		/// collection by anything other than the returned map will leave the map
		/// in an undefined state.
		/// </remarks>
		public static IDictionary<CM, V> AsMap<V, Cm, Coll>(COLL coremaps, Type valueKey)
			where Cm : ICoreMap
			where Coll : ICollection<CM>
		{
			IdentityHashMap<CM, bool> references = new IdentityHashMap<CM, bool>();
			foreach (CM map in coremaps)
			{
				references[map] = true;
			}
			// an EntrySet view of the elements of coremaps
			ICollection<KeyValuePair<CM, V>> entrySet = new _AbstractSet_77(coremaps, references, valueKey);
			return new _AbstractMap_119(coremaps, references, valueKey, entrySet);
		}

		private sealed class _AbstractSet_77 : AbstractSet<KeyValuePair<CM, V>>
		{
			public _AbstractSet_77(COLL coremaps, IdentityHashMap<CM, bool> references, Type valueKey)
			{
				this.coremaps = coremaps;
				this.references = references;
				this.valueKey = valueKey;
			}

			public override IEnumerator<KeyValuePair<CM, V>> GetEnumerator()
			{
				return new _IEnumerator_80(coremaps, references, valueKey);
			}

			private sealed class _IEnumerator_80 : IEnumerator<KeyValuePair<CM, V>>
			{
				public _IEnumerator_80(COLL coremaps, IdentityHashMap<CM, bool> references, Type valueKey)
				{
					this.coremaps = coremaps;
					this.references = references;
					this.valueKey = valueKey;
					this.it = coremaps.GetEnumerator();
					this.last = null;
				}

				internal IEnumerator<CM> it;

				internal CM last;

				public bool MoveNext()
				{
					return this.it.MoveNext();
				}

				public KeyValuePair<CM, V> Current
				{
					get
					{
						CM next = this.it.Current;
						this.last = next;
						return new _KeyValuePair_91(next, valueKey);
					}
				}

				private sealed class _KeyValuePair_91 : KeyValuePair<CM, V>
				{
					public _KeyValuePair_91(CM next, Type valueKey)
					{
						this.next = next;
						this.valueKey = valueKey;
					}

					public CM Key
					{
						get
						{
							return next;
						}
					}

					public V Value
					{
						get
						{
							return next.Get(valueKey);
						}
					}

					public V SetValue(V value)
					{
						return next.Set(valueKey, value);
					}

					private readonly CM next;

					private readonly Type valueKey;
				}

				public void Remove()
				{
					Sharpen.Collections.Remove(references, this.last);
					this.it.Remove();
				}

				private readonly COLL coremaps;

				private readonly IdentityHashMap<CM, bool> references;

				private readonly Type valueKey;
			}

			public override int Count
			{
				get
				{
					return coremaps.Count;
				}
			}

			private readonly COLL coremaps;

			private readonly IdentityHashMap<CM, bool> references;

			private readonly Type valueKey;
		}

		private sealed class _AbstractMap_119 : AbstractMap<CM, V>
		{
			public _AbstractMap_119(COLL coremaps, IdentityHashMap<CM, bool> references, Type valueKey, ICollection<KeyValuePair<CM, V>> entrySet)
			{
				this.coremaps = coremaps;
				this.references = references;
				this.valueKey = valueKey;
				this.entrySet = entrySet;
			}

			public override int Count
			{
				get
				{
					return coremaps.Count;
				}
			}

			public override bool Contains(object key)
			{
				return coremaps.Contains(key);
			}

			public override V Get(object key)
			{
				if (!references.Contains(key))
				{
					return null;
				}
				return ((ICoreMap)key).Get(valueKey);
			}

			public override V Put(CM key, V value)
			{
				if (!references.Contains(key))
				{
					coremaps.Add(key);
					references[key] = true;
				}
				return key.Set(valueKey, value);
			}

			public override V Remove(object key)
			{
				if (!references.Contains(key))
				{
					return null;
				}
				return coremaps.Remove(key) ? ((ICoreMap)key).Get(valueKey) : null;
			}

			public override ICollection<KeyValuePair<CM, V>> EntrySet()
			{
				return entrySet;
			}

			private readonly COLL coremaps;

			private readonly IdentityHashMap<CM, bool> references;

			private readonly Type valueKey;

			private readonly ICollection<KeyValuePair<CM, V>> entrySet;
		}

		/// <summary>Utility function for dumping all the keys and values of a CoreMap to a String.</summary>
		public static string DumpCoreMap(ICoreMap cm)
		{
			StringBuilder sb = new StringBuilder();
			DumpCoreMapToStringBuilder(cm, sb);
			return sb.ToString();
		}

		public static void DumpCoreMapToStringBuilder(ICoreMap cm, StringBuilder sb)
		{
			foreach (Type rawKey in cm.KeySet())
			{
				Type key = (Type)rawKey;
				string className = key.GetSimpleName();
				object value = cm.Get(key);
				sb.Append(className).Append(": ").Append(value).Append("\n");
			}
		}
	}
}
