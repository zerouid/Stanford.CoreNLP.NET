using Java.Lang.Ref;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>An instantiation of a lazy object.</summary>
	/// <author>Gabor Angeli</author>
	public abstract class Lazy<E>
	{
		/// <summary>If this lazy should cache, this is the cached value.</summary>
		private SoftReference<E> implOrNullCache = null;

		/// <summary>If this lazy should not cache, this is the computed value</summary>
		private E implOrNull = null;

		/// <summary>For testing only: simulate a GC event.</summary>
		internal virtual void SimulateGC()
		{
			if (implOrNullCache != null)
			{
				implOrNullCache.Clear();
			}
		}

		/// <summary>
		/// Get the value of this
		/// <see cref="Lazy{E}"/>
		/// , computing it if necessary.
		/// </summary>
		public virtual E Get()
		{
			lock (this)
			{
				E orNull = GetIfDefined();
				if (orNull == null)
				{
					orNull = Compute();
					if (IsCache())
					{
						implOrNullCache = new SoftReference<E>(orNull);
					}
					else
					{
						implOrNull = orNull;
					}
				}
				System.Diagnostics.Debug.Assert(orNull != null);
				return orNull;
			}
		}

		/// <summary>Compute the value of this lazy.</summary>
		protected internal abstract E Compute();

		/// <summary>
		/// Specify whether this lazy should garbage collect its value if needed,
		/// or whether it should force it to be persistent.
		/// </summary>
		public abstract bool IsCache();

		/// <summary>
		/// Get the value of this
		/// <see cref="Lazy{E}"/>
		/// if it's been initialized, or else
		/// return null.
		/// </summary>
		public virtual E GetIfDefined()
		{
			if (implOrNullCache != null)
			{
				System.Diagnostics.Debug.Assert(implOrNull == null);
				return implOrNullCache.Get();
			}
			else
			{
				return implOrNull;
			}
		}

		/// <summary>Check if this lazy has been garbage collected, if it is a cached value.</summary>
		/// <remarks>
		/// Check if this lazy has been garbage collected, if it is a cached value.
		/// Useful for, e.g., clearing keys in a map when the values are already gone.
		/// </remarks>
		public virtual bool IsGarbageCollected()
		{
			return this.IsCache() && (this.implOrNullCache == null || this.implOrNullCache.Get() == null);
		}

		/// <summary>
		/// Create a degenerate
		/// <see cref="Lazy{E}"/>
		/// , which simply returns the given pre-computed
		/// value.
		/// </summary>
		public static Lazy<E> From<E>(E definedElement)
		{
			Lazy<E> rtn = new _Lazy_82(definedElement);
			rtn.implOrNull = definedElement;
			return rtn;
		}

		private sealed class _Lazy_82 : Lazy<E>
		{
			public _Lazy_82(E definedElement)
			{
				this.definedElement = definedElement;
			}

			protected internal override E Compute()
			{
				return definedElement;
			}

			public override bool IsCache()
			{
				return false;
			}

			private readonly E definedElement;
		}

		/// <summary>Create a lazy value from the given provider.</summary>
		/// <remarks>
		/// Create a lazy value from the given provider.
		/// The provider is only called once on initialization.
		/// </remarks>
		public static Lazy<E> Of<E>(ISupplier<E> fn)
		{
			return new _Lazy_103(fn);
		}

		private sealed class _Lazy_103 : Lazy<E>
		{
			public _Lazy_103(ISupplier<E> fn)
			{
				this.fn = fn;
			}

			protected internal override E Compute()
			{
				return fn.Get();
			}

			public override bool IsCache()
			{
				return false;
			}

			private readonly ISupplier<E> fn;
		}

		/// <summary>
		/// Create a lazy value from the given provider, allowing the value
		/// stored in the lazy to be garbage collected if necessary.
		/// </summary>
		/// <remarks>
		/// Create a lazy value from the given provider, allowing the value
		/// stored in the lazy to be garbage collected if necessary.
		/// The value is then re-created by when needed again.
		/// </remarks>
		public static Lazy<E> Cache<E>(ISupplier<E> fn)
		{
			return new _Lazy_123(fn);
		}

		private sealed class _Lazy_123 : Lazy<E>
		{
			public _Lazy_123(ISupplier<E> fn)
			{
				this.fn = fn;
			}

			protected internal override E Compute()
			{
				return fn.Get();
			}

			public override bool IsCache()
			{
				return true;
			}

			private readonly ISupplier<E> fn;
		}
	}
}
