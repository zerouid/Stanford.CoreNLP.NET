using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Ref;
using Java.Nio.Channels;
using Java.Text;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// <p>
	/// A Map backed by the filesystem.
	/// </summary>
	/// <remarks>
	/// <p>
	/// A Map backed by the filesystem.
	/// The primary use-case for this class is in reading a large cache which is convenient to store on disk.
	/// The class will load subsets of data on demand; if the JVM is in danger of running out of memory, these will
	/// be dropped from memory, and re-queried from disk if requested again.
	/// For best results, make sure to set a maximum number of files (by default, any number of files can be created);
	/// and, make sure this number is the same when reading and writing to the database.
	/// </p>
	/// <p>
	/// The keys should have a consistent hash code.
	/// That is, the value of the hash code of an object should be consistent between runs of the JVM.
	/// Note that this is <b>not</b> enforced in the specification of a hash code; in fact, in Java 7
	/// the hash code of a String may change between JVM invocations. The user is advised to be wary.
	/// </p>
	/// <p>
	/// Furthermore, note that many of the operations on this class are expensive, as they require traversing
	/// a potentially large portion of disk, reading it into memory.
	/// Some operations, such as those requiring all the values to be enumerated, may cause a spike in memory
	/// usage.
	/// </p>
	/// <p>
	/// This class is thread-safe, but not necessarily process-safe.
	/// If two processes write to the same block, there is no guarantee that both values will actually be written.
	/// This is very important -- <b>this class is a cache and not a database</b>.
	/// If you care about data integrity, you should use a real database.
	/// </p>
	/// <p>
	/// The values in this map should not be modified once read -- the cache has no reliable way to pick up this change
	/// and synchronize it with the disk.
	/// To enforce this, the cache will cast collections to their unmodifiable counterparts -- to avoid class cast exceptions,
	/// you should not parameterize the class with a particular type of collection
	/// (e.g., use
	/// <see cref="System.Collections.IDictionary{K, V}"/>
	/// rather than
	/// <see cref="System.Collections.Hashtable{K, V}"/>
	/// ).
	/// </p>
	/// <p>
	/// The serialization behavior can be safely changed by overwriting:
	/// </p>
	/// <ul>
	/// <li>@See FileBackedCache#newInputStream</li>
	/// <li>@See FileBackedCache#newOutputStream</li>
	/// <li>@See FileBackedCache#writeNextObject</li>
	/// <li>@See FileBackedCache#readNextObject</li>
	/// </ul>
	/// </remarks>
	/// <?/>
	/// <?/>
	/// <author>Gabor Angeli (angeli at cs)</author>
	public class FileBackedCache<Key, T> : IDictionary<KEY, T>, IEnumerable<KeyValuePair<KEY, T>>
		where Key : ISerializable
	{
		/// <summary>The directory the cached elements are being written to</summary>
		public readonly File cacheDir;

		/// <summary>The maximum number of files to create in that directory ('buckets' in the hash map)</summary>
		public readonly int maxFiles;

		/// <summary>The implementation of the mapping</summary>
		private readonly IDictionary<KEY, SoftReference<T>> mapping = new ConcurrentHashMap<KEY, SoftReference<T>>();

		/// <summary>A reaper for soft references, to save memory on storing the keys</summary>
		private readonly ReferenceQueue<T> reaper = new ReferenceQueue<T>();

		/// <summary>A file canonicalizer, so that we can synchronize on blocks -- static, as it should work between instances.</summary>
		/// <remarks>
		/// A file canonicalizer, so that we can synchronize on blocks -- static, as it should work between instances.
		/// In particular, an exception is thrown if the JVM attempts to take out two locks on a file.
		/// </remarks>
		private static readonly Interner<File> canonicalFile = new Interner<File>();

		/// <summary>A map indicating whether the JVM holds a file lock on the given file</summary>
		private static readonly IdentityHashMap<File, FileBackedCache.FileSemaphore> fileLocks = Generics.NewIdentityHashMap();

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		public FileBackedCache(File directoryToCacheIn)
			: this(directoryToCacheIn, -1)
		{
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="maxFiles">The maximum number of files to store on disk</param>
		public FileBackedCache(File directoryToCacheIn, int maxFiles)
		{
			//
			// Variables
			//
			//
			// Constructors
			//
			// Ensure directory exists
			if (!directoryToCacheIn.Exists())
			{
				if (!directoryToCacheIn.Mkdirs())
				{
					throw new ArgumentException("Could not create cache directory: " + directoryToCacheIn);
				}
			}
			// Ensure directory is directory
			if (!directoryToCacheIn.IsDirectory())
			{
				throw new ArgumentException("Cache directory must be a directory: " + directoryToCacheIn);
			}
			// Ensure directory is writable
			if (!directoryToCacheIn.CanRead())
			{
				throw new ArgumentException("Cannot read cache directory: " + directoryToCacheIn);
			}
			// Save cache directory
			this.cacheDir = directoryToCacheIn;
			this.maxFiles = maxFiles;
			// Start cache cleaner
			/*
			Occasionally clean up the cache, removing keys which have been garbage collected.
			*/
			Thread mappingCleaner = new _Thread_138(this);
			// Clear reference queue
			// GC stale cache entries
			// Remove stale SoftReference
			// Do nothing --
			// Actually remove entries
			// Sleep a bit
			mappingCleaner.SetDaemon(true);
			mappingCleaner.Start();
		}

		private sealed class _Thread_138 : Thread
		{
			public _Thread_138(FileBackedCache<Key, T> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void Run()
			{
				while (true)
				{
					try
					{
						if (this._enclosing.reaper.Poll() != null)
						{
							while (this._enclosing.reaper.Poll() != null)
							{
							}
							IList<KEY> toRemove = Generics.NewLinkedList();
							try
							{
								foreach (KeyValuePair<KEY, SoftReference<T>> entry in this._enclosing.mapping)
								{
									if (entry.Value.Get() == null)
									{
										toRemove.Add(entry.Key);
									}
								}
							}
							catch (ConcurrentModificationException)
							{
							}
							foreach (KEY key in toRemove)
							{
								Sharpen.Collections.Remove(this._enclosing.mapping, key);
							}
						}
						Thread.Sleep(100);
					}
					catch (Exception e)
					{
						throw new RuntimeInterruptedException(e);
					}
				}
			}

			private readonly FileBackedCache<Key, T> _enclosing;
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="initialMapping">The initial elements to place into the cache.</param>
		public FileBackedCache(File directoryToCacheIn, IDictionary<KEY, T> initialMapping)
			: this(directoryToCacheIn, -1)
		{
			PutAll(initialMapping);
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="maxFiles">The maximum number of files to store on disk</param>
		/// <param name="initialMapping">The initial elements to place into the cache.</param>
		public FileBackedCache(File directoryToCacheIn, IDictionary<KEY, T> initialMapping, int maxFiles)
			: this(directoryToCacheIn, maxFiles)
		{
			PutAll(initialMapping);
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		public FileBackedCache(string directoryToCacheIn)
			: this(new File(directoryToCacheIn), -1)
		{
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// or starting with an empty cache.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="maxFiles">The maximum number of files to store on disk</param>
		public FileBackedCache(string directoryToCacheIn, int maxFiles)
			: this(new File(directoryToCacheIn), maxFiles)
		{
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="initialMapping">The initial elements to place into the cache.</param>
		public FileBackedCache(string directoryToCacheIn, IDictionary<KEY, T> initialMapping)
			: this(new File(directoryToCacheIn), initialMapping)
		{
		}

		/// <summary>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// </summary>
		/// <remarks>
		/// Create a file backed cache in a particular directory; either inheriting the elements in the directory
		/// with the initial mapping added, or starting with only the initial mapping.
		/// This constructor may exception, and will create the directory in question if it does not exist.
		/// </remarks>
		/// <param name="directoryToCacheIn">The directory to create the cache in</param>
		/// <param name="initialMapping">The initial elements to place into the cache.</param>
		/// <param name="maxFiles">The maximum number of files to store on disk</param>
		public FileBackedCache(string directoryToCacheIn, IDictionary<KEY, T> initialMapping, int maxFiles)
			: this(new File(directoryToCacheIn), initialMapping, maxFiles)
		{
		}

		/// <summary>Gets the size of the cache, in terms of elements on disk.</summary>
		/// <remarks>
		/// Gets the size of the cache, in terms of elements on disk.
		/// Note that this is an expensive operation, as it reads the entire cache in from disk.
		/// </remarks>
		/// <returns>The size of the cache on disk.</returns>
		public virtual int Count
		{
			get
			{
				//
				// Interface
				//
				return ReadCache();
			}
		}

		/// <summary>Gets the size of the cache, in terms of elements in memory.</summary>
		/// <remarks>
		/// Gets the size of the cache, in terms of elements in memory.
		/// In a multithreaded environment this is on a best-effort basis.
		/// This method makes no disk accesses.
		/// </remarks>
		/// <returns>The size of the cache in memory.</returns>
		public virtual int SizeInMemory()
		{
			return mapping.Count;
		}

		/// <summary>Gets whether the cache is empty, including elements on disk.</summary>
		/// <remarks>
		/// Gets whether the cache is empty, including elements on disk.
		/// Note that this returns true if the cache is empty.
		/// </remarks>
		public virtual bool IsEmpty()
		{
			return Count == 0;
		}

		/// <summary>
		/// Returns true if the specified key exists in the mapping (on a best-effort basis in a multithreaded
		/// environment).
		/// </summary>
		/// <remarks>
		/// Returns true if the specified key exists in the mapping (on a best-effort basis in a multithreaded
		/// environment).
		/// This method may require some disk access, up to a maximum of one file read (of unknown size a priori).
		/// </remarks>
		/// <param name="key">The key to query.</param>
		/// <returns>True if this key is in the cache.</returns>
		public virtual bool Contains(object key)
		{
			// Early exits
			if (mapping.Contains(key))
			{
				return true;
			}
			if (!TryFile(key))
			{
				return false;
			}
			// Read the block for this key
			ICollection<Pair<KEY, T>> elementsRead = ReadBlock(key);
			foreach (Pair<KEY, T> pair in elementsRead)
			{
				if (pair.first.Equals(key))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns true if the specified value is contained.</summary>
		/// <remarks>
		/// Returns true if the specified value is contained.
		/// It is nearly (if not always) a bad idea to call this method.
		/// </remarks>
		/// <param name="value">The value being queried for</param>
		/// <returns>True if the specified value exists in the cache.</returns>
		public virtual bool ContainsValue(object value)
		{
			// Try to short circuit and save the use from their stupidity
			if (mapping.ContainsValue(new SoftReference(value)))
			{
				return true;
			}
			// Do an exhaustive check over the values
			return Values.Contains(value);
		}

		/// <summary>Get a cached value based on a key.</summary>
		/// <remarks>
		/// Get a cached value based on a key.
		/// If the key is in memory, this is a constant time operation.
		/// Else, this requires a single disk access, of undeterminable size but roughly correlated with the
		/// quality of the key's hash code.
		/// </remarks>
		public virtual T Get(object key)
		{
			SoftReference<T> likelyReferenceOrNull = mapping[key];
			T referenceOrNull = likelyReferenceOrNull == null ? null : likelyReferenceOrNull.Get();
			if (likelyReferenceOrNull == null)
			{
				// Case: We don't know about this element being in the cache
				if (!TryFile(key))
				{
					return null;
				}
				// Case: there's no hope of finding this element
				ICollection<Pair<KEY, T>> elemsRead = ReadBlock(key);
				// Read the block for this key
				foreach (Pair<KEY, T> pair in elemsRead)
				{
					if (pair.first.Equals(key))
					{
						return pair.second;
					}
				}
				return null;
			}
			else
			{
				if (referenceOrNull == null)
				{
					// Case: This element once was in the cache
					Sharpen.Collections.Remove(mapping, key);
					return this[key];
				}
				else
				{
					// try again
					if (referenceOrNull is ICollection)
					{
						return (T)Java.Util.Collections.UnmodifiableCollection((ICollection)referenceOrNull);
					}
					else
					{
						if (referenceOrNull is IDictionary)
						{
							return (T)Java.Util.Collections.UnmodifiableMap((IDictionary)referenceOrNull);
						}
						else
						{
							return referenceOrNull;
						}
					}
				}
			}
		}

		public virtual T Put(KEY key, T value)
		{
			T existing = this[key];
			if (existing == value || (existing != null && existing.Equals(value)))
			{
				// Make sure we flush objects which have changed
				if (existing != null && !existing.Equals(value))
				{
					UpdateBlockOrDelete(key, value);
				}
				// Return the same object back
				return existing;
			}
			else
			{
				// In-memory
				SoftReference<T> @ref = new SoftReference<T>(value, this.reaper);
				mapping[key] = @ref;
				// On Disk
				if (existing == null)
				{
					AppendBlock(key, value);
				}
				else
				{
					UpdateBlockOrDelete(key, value);
				}
				// Return
				return existing;
			}
		}

		public virtual T Remove(object key)
		{
			if (!TryFile(key))
			{
				return null;
			}
			try
			{
				return UpdateBlockOrDelete((KEY)key, null);
			}
			catch (InvalidCastException)
			{
				return null;
			}
		}

		public virtual void PutAll<_T0>(IDictionary<_T0> m)
			where _T0 : KEY
		{
			foreach (KeyValuePair<KEY, T> entry in m)
			{
				try
				{
					this[entry.Key] = entry.Value;
				}
				catch (Exception e)
				{
					Redwood.Util.Err(e);
				}
			}
		}

		/// <summary>Clear the IN-MEMORY portion of the cache.</summary>
		/// <remarks>Clear the IN-MEMORY portion of the cache. This does not delete any files.</remarks>
		public virtual void Clear()
		{
			mapping.Clear();
		}

		/// <summary>Returns all the keys for this cache that are found ON DISK.</summary>
		/// <remarks>
		/// Returns all the keys for this cache that are found ON DISK.
		/// This is an expensive operation.
		/// </remarks>
		/// <returns>The set of keys for this cache as found on disk.</returns>
		public virtual ICollection<KEY> Keys
		{
			get
			{
				ReadCache();
				return mapping.Keys;
			}
		}

		/// <summary>Returns all the values for this cache that are found ON DISK.</summary>
		/// <remarks>
		/// Returns all the values for this cache that are found ON DISK.
		/// This is an expensive operation, both in terms of disk access time,
		/// and in terms of memory used.
		/// Furthermore, the memory used in this function cannot be GC collected -- you are loading the
		/// entire cache into memory.
		/// </remarks>
		/// <returns>The set of values for this cache as found on disk.</returns>
		public virtual ICollection<T> Values
		{
			get
			{
				ICollection<KeyValuePair<KEY, T>> entries = this;
				List<T> values = Generics.NewArrayList(entries.Count);
				foreach (KeyValuePair<KEY, T> entry in entries)
				{
					values.Add(entry.Value);
				}
				return values;
			}
		}

		/// <summary>Returns all the (key,value) pairs for this cache that are found ON DISK.</summary>
		/// <remarks>
		/// Returns all the (key,value) pairs for this cache that are found ON DISK.
		/// This is an expensive operation, both in terms of disk access time,
		/// and in terms of memory used.
		/// Furthermore, the memory used in this function cannot be GC collected -- you are loading the
		/// entire cache into memory.
		/// </remarks>
		/// <returns>The set of keys and associated values for this cache as found on disk.</returns>
		public virtual ICollection<KeyValuePair<KEY, T>> EntrySet()
		{
			ReadCache();
			ICollection<KeyValuePair<KEY, SoftReference<T>>> entries = mapping;
			ICollection<KeyValuePair<KEY, T>> rtn = Generics.NewHashSet();
			foreach (KeyValuePair<KEY, SoftReference<T>> entry in entries)
			{
				T value = entry.Value.Get();
				if (value == null)
				{
					value = this[entry.Key];
				}
				T valueFinal = value;
				rtn.Add(new _KeyValuePair_449(valueFinal, entry));
			}
			return rtn;
		}

		private sealed class _KeyValuePair_449 : KeyValuePair<KEY, T>
		{
			public _KeyValuePair_449(T valueFinal, KeyValuePair<KEY, SoftReference<T>> entry)
			{
				this.valueFinal = valueFinal;
				this.entry = entry;
				this.valueImpl = valueFinal;
			}

			private T valueImpl;

			public KEY Key
			{
				get
				{
					return entry.Key;
				}
			}

			public T Value
			{
				get
				{
					return this.valueImpl;
				}
			}

			public T SetValue(T value)
			{
				T oldValue = this.valueImpl;
				this.valueImpl = value;
				return oldValue;
			}

			private readonly T valueFinal;

			private readonly KeyValuePair<KEY, SoftReference<T>> entry;
		}

		/// <summary>Iterates over the entries of the cache.</summary>
		/// <remarks>
		/// Iterates over the entries of the cache.
		/// In the end, this loads the entire cache, but it can do it incrementally.
		/// </remarks>
		/// <returns>An iterator over the entries in the cache.</returns>
		public virtual IEnumerator<KeyValuePair<KEY, T>> GetEnumerator()
		{
			File[] files = cacheDir.ListFiles();
			if (files == null || files.Length == 0)
			{
				return Generics.NewLinkedList<KeyValuePair<KEY, T>>().GetEnumerator();
			}
			for (int i = 0; i < files.Length; ++i)
			{
				try
				{
					files[i] = canonicalFile.Intern(files[i].GetCanonicalFile());
				}
				catch (IOException e)
				{
					throw ThrowSafe(e);
				}
			}
			return new _IEnumerator_487(this, files);
		}

		private sealed class _IEnumerator_487 : IEnumerator<KeyValuePair<KEY, T>>
		{
			public _IEnumerator_487(FileBackedCache<Key, T> _enclosing, File[] files)
			{
				this._enclosing = _enclosing;
				this.files = files;
				this.elements = this._enclosing.ReadBlock(files[0]).GetEnumerator();
				this.index = 1;
			}

			internal IEnumerator<Pair<KEY, T>> elements;

			internal int index;

			public bool MoveNext()
			{
				// Still have elements in this block
				if (this.elements.MoveNext())
				{
					return true;
				}
				// Still have files to traverse
				this.elements = null;
				while (this.index < files.Length && this.elements == null)
				{
					try
					{
						this.elements = this._enclosing.ReadBlock(files[this.index]).GetEnumerator();
					}
					catch (OutOfMemoryException e)
					{
						Redwood.Util.Warn("FileBackedCache", "Caught out of memory error (clearing cache): " + e.Message);
						this._enclosing._enclosing.Clear();
						//noinspection EmptyCatchBlock
						try
						{
							Thread.Sleep(1000);
						}
						catch (Exception e2)
						{
							throw new RuntimeInterruptedException(e2);
						}
						this.elements = this._enclosing.ReadBlock(files[this.index]).GetEnumerator();
					}
					catch (Exception e)
					{
						Redwood.Util.Err(e);
					}
					this.index += 1;
				}
				// No more elements
				return this.elements != null && this.MoveNext();
			}

			public KeyValuePair<KEY, T> Current
			{
				get
				{
					if (!this.MoveNext())
					{
						throw new NoSuchElementException();
					}
					// Convert a pair to an entry
					Pair<KEY, T> pair = this.elements.Current;
					return new _KeyValuePair_521(pair);
				}
			}

			private sealed class _KeyValuePair_521 : KeyValuePair<KEY, T>
			{
				public _KeyValuePair_521(Pair<KEY, T> pair)
				{
					this.pair = pair;
				}

				public KEY Key
				{
					get
					{
						return pair.first;
					}
				}

				public T Value
				{
					get
					{
						return pair.second;
					}
				}

				public T SetValue(T value)
				{
					throw new Exception("Cannot set entry");
				}

				private readonly Pair<KEY, T> pair;
			}

			public void Remove()
			{
				throw new Exception("Remove not implemented");
			}

			private readonly FileBackedCache<Key, T> _enclosing;

			private readonly File[] files;
		}

		/// <summary>Remove a given key from memory, not removing it from the disk.</summary>
		/// <param name="key">The key to remove from memory.</param>
		public virtual bool RemoveFromMemory(KEY key)
		{
			return Sharpen.Collections.Remove(mapping, key) != null;
		}

		/// <summary>Get the list of files on which this JVM holds a lock.</summary>
		/// <returns>A collection of files on which the JVM holds a file lock.</returns>
		public static ICollection<File> LocksHeld()
		{
			List<File> files = Generics.NewArrayList();
			foreach (KeyValuePair<File, FileBackedCache.FileSemaphore> entry in fileLocks)
			{
				if (entry.Value.IsActive())
				{
					files.Add(entry.Key);
				}
			}
			return files;
		}

		//
		// Daemons
		//
		//
		// Implementation
		// These are directly called by the interface methods
		//
		/// <summary>Reads the cache in its entirely -- this is potentially very slow</summary>
		private int ReadCache()
		{
			File[] files = cacheDir.ListFiles();
			if (files == null)
			{
				return 0;
			}
			for (int i = 0; i < files.Length; ++i)
			{
				try
				{
					files[i] = canonicalFile.Intern(files[i].GetCanonicalFile());
				}
				catch (IOException e)
				{
					throw ThrowSafe(e);
				}
			}
			int count = 0;
			foreach (File f in files)
			{
				try
				{
					ICollection<Pair<KEY, T>> block = ReadBlock(f);
					count += block.Count;
				}
				catch (Exception e)
				{
					throw ThrowSafe(e);
				}
			}
			return count;
		}

		/// <summary>Checks for the existence of the block associated with the key</summary>
		private bool TryFile(object key)
		{
			try
			{
				return Hash2file(key.GetHashCode(), false).Exists();
			}
			catch (IOException e)
			{
				throw ThrowSafe(e);
			}
		}

		/// <summary>Reads the block specified by the key in its entirety</summary>
		private ICollection<Pair<KEY, T>> ReadBlock(object key)
		{
			try
			{
				return ReadBlock(Hash2file(key.GetHashCode(), true));
			}
			catch (IOException e)
			{
				Redwood.Util.Err("Could not read file: " + cacheDir.GetPath() + File.separator + FileRoot(key.GetHashCode()));
				throw ThrowSafe(e);
			}
		}

		/// <summary>Appends a value to the block specified by the key</summary>
		private void AppendBlock(KEY key, T value)
		{
			bool haveTakenLock = false;
			Pair<OutputStream, FileBackedCache.ICloseAction> writer = null;
			try
			{
				// Get File
				File toWrite = Hash2file(key.GetHashCode(), false);
				bool exists = toWrite.Exists();
				RobustCreateFile(toWrite);
				lock (toWrite)
				{
					System.Diagnostics.Debug.Assert(canonicalFile.Intern(toWrite.GetCanonicalFile()) == toWrite);
					// Write Object
					writer = NewOutputStream(toWrite, exists);
					haveTakenLock = true;
					WriteNextObject(writer.first, Pair.MakePair(key, value));
					writer.second.Apply();
					haveTakenLock = false;
				}
			}
			catch (IOException e)
			{
				try
				{
					if (haveTakenLock)
					{
						writer.second.Apply();
					}
				}
				catch (IOException e2)
				{
					throw ThrowSafe(e2);
				}
				throw ThrowSafe(e);
			}
		}

		/// <summary>Updates a block with the specified value; or deletes the block if the value is null</summary>
		private T UpdateBlockOrDelete(KEY key, T valueOrNull)
		{
			Pair<InputStream, FileBackedCache.ICloseAction> reader = null;
			Pair<OutputStream, FileBackedCache.ICloseAction> writer = null;
			bool haveClosedReader = false;
			bool haveClosedWriter = false;
			try
			{
				// Variables
				File blockFile = Hash2file(key.GetHashCode(), true);
				lock (blockFile)
				{
					System.Diagnostics.Debug.Assert(canonicalFile.Intern(blockFile.GetCanonicalFile()) == blockFile);
					reader = NewInputStream(blockFile);
					writer = NewOutputStream(blockFile, false);
					// Get write lock before reading
					IList<Pair<KEY, T>> block = Generics.NewLinkedList();
					T existingValue = null;
					// Read
					Pair<KEY, T> element;
					while ((element = ReadNextObjectOrNull(reader.first)) != null)
					{
						if (element.first.Equals(key))
						{
							if (valueOrNull != null)
							{
								// Update
								existingValue = element.second;
								element.second = valueOrNull;
								block.Add(element);
							}
						}
						else
						{
							// Spurious read
							block.Add(element);
						}
					}
					reader.second.Apply();
					haveClosedReader = true;
					// Write
					foreach (Pair<KEY, T> elem in block)
					{
						WriteNextObject(writer.first, elem);
					}
					writer.second.Apply();
					haveClosedWriter = true;
					// Return
					return existingValue;
				}
			}
			catch (Exception e)
			{
				Redwood.Util.Err(e);
				throw ThrowSafe(e);
			}
			finally
			{
				try
				{
					if (reader != null && !haveClosedReader)
					{
						reader.second.Apply();
					}
					if (writer != null && !haveClosedWriter)
					{
						writer.second.Apply();
					}
				}
				catch (IOException e)
				{
					Redwood.Util.Warn(e);
				}
			}
		}

		//
		// Implementation Helpers
		// These are factored bits of the implementation
		//
		/// <summary>Completely reads a block into local memory</summary>
		private ICollection<Pair<KEY, T>> ReadBlock(File block)
		{
			bool haveClosed = false;
			Pair<InputStream, FileBackedCache.ICloseAction> input = null;
			try
			{
				lock (block)
				{
					System.Diagnostics.Debug.Assert(canonicalFile.Intern(block.GetCanonicalFile()) == block);
					IList<Pair<KEY, T>> read = Generics.NewLinkedList();
					// Get the reader
					input = NewInputStream(block);
					// Get each object in the block
					Pair<KEY, T> element;
					while ((element = ReadNextObjectOrNull(input.first)) != null)
					{
						read.Add(element);
					}
					input.second.Apply();
					haveClosed = true;
					// Add elements
					foreach (Pair<KEY, T> elem in read)
					{
						SoftReference<T> @ref = new SoftReference<T>(elem.second, this.reaper);
						mapping[elem.first] = @ref;
					}
					return read;
				}
			}
			catch (StreamCorruptedException)
			{
				Redwood.Util.Warn("Stream corrupted reading " + block);
				// Case: corrupted write
				if (!block.Delete())
				{
					throw new InvalidOperationException("File corrupted, and cannot delete it: " + block.GetPath());
				}
				return Generics.NewLinkedList();
			}
			catch (EOFException)
			{
				Redwood.Util.Warn("Empty file (someone else is preparing to write to it?) " + block);
				return Generics.NewLinkedList();
			}
			catch (IOException e)
			{
				// Case: General IO Error
				Redwood.Util.Err("Could not read file: " + block + ": " + e.Message);
				return Generics.NewLinkedList();
			}
			catch (TypeLoadException e)
			{
				// Case: Couldn't read class
				Redwood.Util.Err("Could not read a class in file: " + block + ": " + e.Message);
				return Generics.NewLinkedList();
			}
			catch (Exception e)
			{
				// Case: Unknown error -- see if it's caused by StreamCorrupted
				if (e.InnerException != null && typeof(StreamCorruptedException).IsAssignableFrom(e.InnerException.GetType()))
				{
					// Yes -- caused by StreamCorrupted
					if (!block.Delete())
					{
						throw new InvalidOperationException("File corrupted, and cannot delete it: " + block.GetPath());
					}
					return Generics.NewLinkedList();
				}
				else
				{
					// No -- random error (pass up)
					throw;
				}
			}
			finally
			{
				if (input != null && !haveClosed)
				{
					try
					{
						input.second.Apply();
					}
					catch (IOException e)
					{
						Redwood.Util.Warn(e);
					}
				}
			}
		}

		/// <summary>Returns a file corresponding to a hash code, ensuring it exists first</summary>
		/// <exception cref="System.IO.IOException"/>
		private File Hash2file(int hashCode, bool create)
		{
			File candidate = canonicalFile.Intern(new File(cacheDir.GetCanonicalPath() + File.separator + FileRoot(hashCode) + ".block.ser.gz").GetCanonicalFile());
			if (create)
			{
				RobustCreateFile(candidate);
			}
			return candidate;
		}

		private int FileRoot(int hashCode)
		{
			if (this.maxFiles < 0)
			{
				return hashCode;
			}
			else
			{
				return Math.Abs(hashCode) % this.maxFiles;
			}
		}

		/// <summary>Turns out, an ObjectOutputStream cannot append to a file.</summary>
		/// <remarks>Turns out, an ObjectOutputStream cannot append to a file. This is dumb.</remarks>
		public class AppendingObjectOutputStream : ObjectOutputStream
		{
			/// <exception cref="System.IO.IOException"/>
			public AppendingObjectOutputStream(OutputStream @out)
				: base(@out)
			{
			}

			//
			// Java Hacks
			//
			/// <exception cref="System.IO.IOException"/>
			protected override void WriteStreamHeader()
			{
				// do not write a header, but reset
				Reset();
			}
		}

		private static Exception ThrowSafe(Exception e)
		{
			if (e is Exception)
			{
				return (Exception)e;
			}
			else
			{
				if (e.InnerException == null)
				{
					return new Exception(e);
				}
				else
				{
					return ThrowSafe(e.InnerException);
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static void RobustCreateFile(File candidate)
		{
			int tries = 0;
			while (!candidate.Exists())
			{
				if (tries > 30)
				{
					throw new IOException("Could not create file: " + candidate);
				}
				if (candidate.CreateNewFile())
				{
					break;
				}
				tries++;
				try
				{
					Thread.Sleep(1000);
				}
				catch (Exception e)
				{
					Redwood.Util.Log(e);
					throw new RuntimeInterruptedException(e);
				}
			}
		}

		public interface ICloseAction
		{
			/// <exception cref="System.IO.IOException"/>
			void Apply();
		}

		public class FileSemaphore
		{
			private int licenses = 1;

			private readonly FileLock Lock;

			private readonly FileChannel channel;

			public FileSemaphore(FileLock Lock, FileChannel channel)
			{
				this.Lock = Lock;
				this.channel = channel;
			}

			public virtual bool IsActive()
			{
				lock (this)
				{
					if (licenses == 0)
					{
						System.Diagnostics.Debug.Assert(Lock == null || !Lock.IsValid());
					}
					if (licenses != 0 && Lock != null)
					{
						System.Diagnostics.Debug.Assert(Lock.IsValid());
					}
					return licenses != 0;
				}
			}

			public virtual void Take()
			{
				lock (this)
				{
					if (!IsActive())
					{
						throw new InvalidOperationException("Taking a file license when the licenses have all been released");
					}
					licenses += 1;
				}
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Release()
			{
				lock (this)
				{
					if (licenses <= 0)
					{
						throw new InvalidOperationException("Already released all semaphore licenses");
					}
					licenses -= 1;
					if (licenses <= 0)
					{
						if (Lock != null)
						{
							Lock.Release();
						}
						channel.Close();
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual FileBackedCache.FileSemaphore AcquireFileLock(File f)
		{
			System.Diagnostics.Debug.Assert(canonicalFile.Intern(f.GetCanonicalFile()) == f);
			lock (f)
			{
				// Check semaphore
				lock (fileLocks)
				{
					if (fileLocks.Contains(f))
					{
						FileBackedCache.FileSemaphore sem = fileLocks[f];
						if (sem.IsActive())
						{
							sem.Take();
							return sem;
						}
						else
						{
							Sharpen.Collections.Remove(fileLocks, f);
						}
					}
				}
				// Get the channel
				FileChannel channel = new RandomAccessFile(f, "rw").GetChannel();
				FileLock lockOrNull = null;
				// Try the lock
				for (int i = 0; i < 1000; ++i)
				{
					lockOrNull = channel.TryLock();
					if (lockOrNull == null || !lockOrNull.IsValid())
					{
						try
						{
							Thread.Sleep(1000);
						}
						catch (Exception e)
						{
							Redwood.Util.Log(e);
							throw new RuntimeInterruptedException(e);
						}
						if (i % 60 == 59)
						{
							Redwood.Util.Warn("FileBackedCache", "Lock still busy after " + ((i + 1) / 60) + " minutes");
						}
						//noinspection UnnecessaryContinue
						continue;
					}
					else
					{
						break;
					}
				}
				if (lockOrNull == null)
				{
					Redwood.Util.Warn("FileBackedCache", "Could not acquire file lock! Continuing without lock");
				}
				// Return
				FileBackedCache.FileSemaphore sem_1 = new FileBackedCache.FileSemaphore(lockOrNull, channel);
				lock (fileLocks)
				{
					fileLocks[f] = sem_1;
				}
				return sem_1;
			}
		}

		//
		//  POSSIBLE OVERRIDES
		//
		/// <summary>Create a new input stream, along with the code to close it and clean up.</summary>
		/// <remarks>
		/// Create a new input stream, along with the code to close it and clean up.
		/// This code may be overridden, but should match nextObjectOrNull().
		/// IMPORTANT NOTE: acquiring a lock (well, semaphore) with FileBackedCache#acquireFileLock(File)
		/// is generally a good idea. Make sure to release() it in the close action as well.
		/// </remarks>
		/// <param name="f">The file to read from</param>
		/// <returns>A pair, corresponding to the stream and the code to close it.</returns>
		/// <exception cref="System.IO.IOException"/>
		protected internal virtual Pair<InputStream, FileBackedCache.ICloseAction> NewInputStream(File f)
		{
			FileBackedCache.FileSemaphore Lock = AcquireFileLock(f);
			ObjectInputStream rtn = new ObjectInputStream(new GZIPInputStream(new BufferedInputStream(new FileInputStream(f))));
			return new Pair<ObjectInputStream, FileBackedCache.ICloseAction>(rtn, null);
		}

		/// <summary>Create a new output stream, along with the code to close it and clean up.</summary>
		/// <remarks>
		/// Create a new output stream, along with the code to close it and clean up.
		/// This code may be overridden, but should match nextObjectOrNull()
		/// IMPORTANT NOTE: acquiring a lock (well, semaphore) with FileBackedCache#acquireFileLock(File)
		/// is generally a good idea. Make sure to release() it in the close action as well.
		/// </remarks>
		/// <param name="f">The file to write to</param>
		/// <param name="isAppend">Signals whether the file we are writing to exists, and we are appending to it.</param>
		/// <returns>A pair, corresponding to the stream and the code to close it.</returns>
		/// <exception cref="System.IO.IOException"/>
		protected internal virtual Pair<OutputStream, FileBackedCache.ICloseAction> NewOutputStream(File f, bool isAppend)
		{
			FileOutputStream stream = new FileOutputStream(f, isAppend);
			FileBackedCache.FileSemaphore Lock = AcquireFileLock(f);
			ObjectOutputStream rtn = isAppend ? new FileBackedCache.AppendingObjectOutputStream(new GZIPOutputStream(new BufferedOutputStream(stream))) : new ObjectOutputStream(new GZIPOutputStream(new BufferedOutputStream(stream)));
			return new Pair<ObjectOutputStream, FileBackedCache.ICloseAction>(rtn, null);
		}

		/// <summary>Return the next object in the given stream, or null if there is no such object.</summary>
		/// <remarks>
		/// Return the next object in the given stream, or null if there is no such object.
		/// This method may be overwritten, but should match the implementation of newInputStream
		/// </remarks>
		/// <param name="input">The input stream to read the object from</param>
		/// <returns>A (key, value) pair corresponding to the read object</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		protected internal virtual Pair<KEY, T> ReadNextObjectOrNull(InputStream input)
		{
			try
			{
				return (Pair<KEY, T>)((ObjectInputStream)input).ReadObject();
			}
			catch (EOFException)
			{
				return null;
			}
		}

		// I hate java
		/// <summary>
		/// Write an object to a stream
		/// This method may be overwritten, but should match the implementation of newOutputStream()
		/// </summary>
		/// <param name="output">The output stream to write the object to.</param>
		/// <param name="value">The value to write to the stream, as a (key, value) pair.</param>
		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void WriteNextObject(OutputStream output, Pair<KEY, T> value)
		{
			((ObjectOutputStream)output).WriteObject(value);
		}

		/// <summary><p>Merge a number of caches together.</summary>
		/// <remarks>
		/// <p>Merge a number of caches together. This could be useful for creating large caches,
		/// as (1) it can bypass NFS for local caching, and (2) it can allow for many small caches
		/// that are then merged together, which is more efficient as the number of entries in a bucket
		/// increases (e.g., if the cache becomes very large).</p>
		/// <p>If there are collision, they are broken by accepting the entry in destination (if applicable),
		/// and then by accepting the entry in the last constituent.</p>
		/// <p><b>IMPORTANT NOTE:</b>: This method requires quite a bit of memory, and there is a brief time
		/// when it deletes all the files in destination, storing the data entirely in memory. If the program
		/// crashes in this state, THE DATA IN |destination| MAY BE LOST</p>
		/// </remarks>
		/// <param name="destination">
		/// The cache to append to. This might not be empty, in which case all entries
		/// in the destination are preserved.
		/// </param>
		/// <param name="constituents">
		/// The constituent caches. All entries in each of these caches are added to
		/// the destination.
		/// </param>
		public static void Merge<Key, T>(FileBackedCache<KEY, T> destination, FileBackedCache<KEY, T>[] constituents)
			where Key : ISerializable
			where T : ISerializable
		{
			Redwood.Util.StartTrack("Merging Caches");
			// (1) Read everything into memory
			Redwood.Util.ForceTrack("Reading Constituents");
			IDictionary<string, IDictionary<KEY, T>> combinedMapping = Generics.NewHashMap();
			try
			{
				// Accumulate constituents
				for (int i = 0; i < constituents.Length; ++i)
				{
					FileBackedCache<KEY, T> constituent = constituents[i];
					foreach (KeyValuePair<KEY, T> entry in constituent)
					{
						string fileToWriteTo = destination.Hash2file(entry.Key.GetHashCode(), false).GetName();
						if (!combinedMapping.Contains(fileToWriteTo))
						{
							combinedMapping[fileToWriteTo] = Generics.NewHashMap<KEY, T>();
						}
						combinedMapping[fileToWriteTo][entry.Key] = entry.Value;
					}
					Redwood.Util.Log("[" + new DecimalFormat("0000").Format(i) + "/" + constituents.Length + "] read " + constituent.cacheDir + " [" + (Runtime.GetRuntime().FreeMemory() / 1000000) + "MB free memory]");
					constituent.Clear();
				}
				// Accumulate destination
				foreach (KeyValuePair<KEY, T> entry_1 in destination)
				{
					string fileToWriteTo = destination.Hash2file(entry_1.Key.GetHashCode(), false).GetName();
					if (!combinedMapping.Contains(fileToWriteTo))
					{
						combinedMapping[fileToWriteTo] = Generics.NewHashMap<KEY, T>();
					}
					combinedMapping[fileToWriteTo][entry_1.Key] = entry_1.Value;
				}
			}
			catch (IOException e)
			{
				Redwood.Util.Err("Found exception in merge() -- all data is intact (but passing exception up)");
				throw new Exception(e);
			}
			Redwood.Util.EndTrack("Reading Constituents");
			// (2) Clear out Destination
			Redwood.Util.ForceTrack("Clearing Destination");
			if (!destination.cacheDir.Exists() && !destination.cacheDir.Mkdirs())
			{
				throw new Exception("Could not create cache dir for destination (data is intact): " + destination.cacheDir);
			}
			File[] filesInDestination = destination.cacheDir.ListFiles();
			if (filesInDestination == null)
			{
				throw new Exception("Cannot list files in destination's cache dir (data is intact): " + destination.cacheDir);
			}
			foreach (File block in filesInDestination)
			{
				if (!block.Delete())
				{
					Redwood.Util.Warn("FileBackedCache", "could not delete block: " + block);
				}
			}
			Redwood.Util.EndTrack("Clearing Destination");
			// (3) Write new files
			Redwood.Util.ForceTrack("Writing New Files");
			try
			{
				foreach (KeyValuePair<string, IDictionary<KEY, T>> blockEntry in combinedMapping)
				{
					// Get File
					File toWrite = canonicalFile.Intern(new File(destination.cacheDir + File.separator + blockEntry.Key).GetCanonicalFile());
					bool exists = toWrite.Exists();
					// should really be false;
					// Write Objects
					Pair<OutputStream, FileBackedCache.ICloseAction> writer = destination.NewOutputStream(toWrite, exists);
					foreach (KeyValuePair<KEY, T> entry in blockEntry.Value)
					{
						destination.WriteNextObject(writer.first, Pair.MakePair(entry.Key, entry.Value));
					}
					writer.second.Apply();
				}
			}
			catch (IOException e)
			{
				Redwood.Util.Err("Could not write constituent files to combined cache (DATA IS LOST)!");
				throw new Exception(e);
			}
			Redwood.Util.EndTrack("Writing New Files");
			Redwood.Util.EndTrack("Merging Caches");
		}

		public static void Merge<Key, T>(FileBackedCache<KEY, T> destination, ICollection<FileBackedCache<KEY, T>> constituents)
			where Key : ISerializable
			where T : ISerializable
		{
			Merge(destination, Sharpen.Collections.ToArray(constituents, (FileBackedCache<KEY, T>[])new FileBackedCache[constituents.Count]));
		}
	}
}
