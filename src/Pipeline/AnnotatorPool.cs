using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An object for keeping track of Annotators.</summary>
	/// <remarks>
	/// An object for keeping track of Annotators. Typical use is to allow multiple
	/// pipelines to share any Annotators in common.
	/// For example, if multiple pipelines exist, and they both need a
	/// ParserAnnotator, it would be bad to load two such Annotators into memory.
	/// Instead, an AnnotatorPool will only create one Annotator and allow both
	/// pipelines to share it.
	/// </remarks>
	/// <author>bethard</author>
	public class AnnotatorPool
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.AnnotatorPool));

		/// <summary>A cached annotator, including the signature it should cache on.</summary>
		private class CachedAnnotator
		{
			/// <summary>The signature of the annotator.</summary>
			public readonly string signature;

			/// <summary>The cached annotator.</summary>
			public readonly Lazy<IAnnotator> annotator;

			/// <summary>The straightforward constructor.</summary>
			private CachedAnnotator(string signature, Lazy<IAnnotator> annotator)
			{
				if (!annotator.IsCache())
				{
					log.Warn("Cached annotator will never GC -- this can cause OOM exceptions!");
				}
				this.signature = signature;
				this.annotator = annotator;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				AnnotatorPool.CachedAnnotator that = (AnnotatorPool.CachedAnnotator)o;
				return signature != null ? signature.Equals(that.signature) : that.signature == null && (annotator != null ? annotator.Equals(that.annotator) : that.annotator == null);
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override int GetHashCode()
			{
				int result = signature != null ? signature.GetHashCode() : 0;
				result = 31 * result + (annotator != null ? annotator.GetHashCode() : 0);
				return result;
			}
		}

		/// <summary>The set of annotators that we have cached, possibly with garbage collected annotator instances.</summary>
		/// <remarks>
		/// The set of annotators that we have cached, possibly with garbage collected annotator instances.
		/// This is a map from annotator name to cached annotator instances.
		/// </remarks>
		private readonly IDictionary<string, AnnotatorPool.CachedAnnotator> cachedAnnotators;

		/// <summary>Create an empty AnnotatorPool.</summary>
		public AnnotatorPool()
		{
			this.cachedAnnotators = Generics.NewHashMap();
		}

		/// <summary>Register an Annotator that can be created by the pool.</summary>
		/// <remarks>
		/// Register an Annotator that can be created by the pool.
		/// Note that factories are used here so that many possible annotators can
		/// be defined within the AnnotatorPool, but an Annotator is only created
		/// when one is actually needed.
		/// </remarks>
		/// <param name="name">The name to be associated with the Annotator.</param>
		/// <param name="props">The properties we are using to create the annotator</param>
		/// <param name="annotator">
		/// A factory that creates an instance of the desired Annotator.
		/// This should be an instance of
		/// <see cref="Edu.Stanford.Nlp.Util.Lazy{E}.Cache{E}(Java.Util.Function.ISupplier{T})"/>
		/// , if we want
		/// the annotator pool to behave as a cache (i.e., evict old annotators
		/// when the GC requires it).
		/// </param>
		/// <returns>true if a new annotator was created; false if we reuse an existing one</returns>
		public virtual bool Register(string name, Properties props, Lazy<IAnnotator> annotator)
		{
			bool newAnnotator = false;
			string newSig = PropertiesUtils.GetSignature(name, props);
			lock (this.cachedAnnotators)
			{
				AnnotatorPool.CachedAnnotator oldAnnotator = this.cachedAnnotators[name];
				if (oldAnnotator == null || !Objects.Equals(oldAnnotator.signature, newSig))
				{
					// the new annotator uses different properties so we need to update!
					if (oldAnnotator != null)
					{
						// Try to get it from the global cache
						log.Debug("Replacing old annotator \"" + name + "\" with signature [" + oldAnnotator.signature + "] with new annotator with signature [" + newSig + "]");
					}
					// Add the new annotator
					this.cachedAnnotators[name] = new AnnotatorPool.CachedAnnotator(newSig, annotator);
					// Unmount the old annotator
					Optional.OfNullable(oldAnnotator).FlatMap(null).IfPresent(null);
					// Register that we added an annotator
					newAnnotator = true;
				}
			}
			// nothing to do if an annotator with same name and signature already exists
			return newAnnotator;
		}

		/// <summary>Clear this pool, and unmount all the annotators mounted on it.</summary>
		public virtual void Clear()
		{
			lock (this)
			{
				lock (this.cachedAnnotators)
				{
					foreach (KeyValuePair<string, AnnotatorPool.CachedAnnotator> entry in new HashSet<KeyValuePair<string, AnnotatorPool.CachedAnnotator>>(this.cachedAnnotators))
					{
						// Unmount the annotator
						Optional.OfNullable(entry.Value).FlatMap(null).IfPresent(null);
						// Remove the annotator
						Sharpen.Collections.Remove(this.cachedAnnotators, entry.Key);
					}
				}
			}
		}

		/// <summary>Retrieve an Annotator from the pool.</summary>
		/// <remarks>
		/// Retrieve an Annotator from the pool. If the named Annotator has not yet
		/// been requested, it will be created. Otherwise, the existing instance of
		/// the Annotator will be returned.
		/// </remarks>
		/// <param name="name">The annotator to retrieve from the pool</param>
		/// <returns>The annotator</returns>
		/// <exception cref="System.ArgumentException">If the annotator cannot be created</exception>
		public virtual IAnnotator Get(string name)
		{
			lock (this)
			{
				AnnotatorPool.CachedAnnotator factory = this.cachedAnnotators[name];
				if (factory != null)
				{
					return factory.annotator.Get();
				}
				else
				{
					throw new ArgumentException("No annotator named " + name);
				}
			}
		}

		/// <summary>A global singleton annotator pool, so that we can cache globally on a JVM instance.</summary>
		public static readonly AnnotatorPool Singleton = new AnnotatorPool();
	}
}
