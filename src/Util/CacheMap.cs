using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A fixed size hash map with LRU replacement.</summary>
	/// <remarks>
	/// A fixed size hash map with LRU replacement.  Can optionally automatically
	/// dump itself out to a file as the cache grows.
	/// </remarks>
	/// <author>Ari Steinberg (ari.steinberg@stanford.edu)</author>
	[System.Serializable]
	public class CacheMap<K, V> : LinkedHashMap<K, V>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.CacheMap));

		private const long serialVersionUID = 1L;

		private string backingFile;

		private int CacheEntries;

		private int entriesSinceLastWritten;

		private int frequencyToWrite;

		private int hits;

		private int misses;

		private int puts;

		/// <summary>Constructor.</summary>
		/// <param name="numEntries">
		/// is the number of entries you want to store in the
		/// CacheMap.  This is not the same as the number of
		/// buckets - that is effected by this and the target
		/// loadFactor.
		/// </param>
		/// <param name="accessOrder">is the same as in LinkedHashMap.</param>
		/// <param name="backingFile">is the name of the file to dump this to, if desired.</param>
		/// <seealso cref="Java.Util.LinkedHashMap{K, V}"/>
		public CacheMap(int numEntries, float loadFactor, bool accessOrder, string backingFile)
			: base((int)Math.Ceil((numEntries + 1) / loadFactor), loadFactor, accessOrder)
		{
			// Make sure its capacity is big enough so that we don't have to resize it
			// even if it gets one more element than we are expecting.  Round up the
			// division
			CacheEntries = numEntries;
			this.backingFile = backingFile;
			entriesSinceLastWritten = 0;
			this.frequencyToWrite = numEntries / 128 + 1;
			hits = misses = puts = 0;
		}

		public CacheMap(int numEntries, float loadFactor, bool accessOrder)
			: this(numEntries, loadFactor, accessOrder, null)
		{
		}

		public CacheMap(int numEntries, float loadFactor)
			: this(numEntries, loadFactor, false, null)
		{
		}

		public CacheMap(int numEntries)
			: this(numEntries, 0.75f, false, null)
		{
		}

		/// <summary>
		/// Creates a new file-backed CacheMap or loads it in from the specified file
		/// if it already exists.
		/// </summary>
		/// <remarks>
		/// Creates a new file-backed CacheMap or loads it in from the specified file
		/// if it already exists.  The parameters passed in are the same as the
		/// constructor.  If useFileParams is true and the file exists, all of your
		/// parameters will be ignored (replaced with those stored in the file
		/// itself).  If useFileParams is false then we override the settings in the
		/// file with the ones you specify (except loadFactor and accessOrder) and
		/// reset the stats.
		/// </remarks>
		public static Edu.Stanford.Nlp.Util.CacheMap<K, V> Create<K, V>(int numEntries, float loadFactor, bool accessOrder, string file, bool useFileParams)
		{
			try
			{
				using (ObjectInputStream ois = new ObjectInputStream(new FileInputStream(file)))
				{
					Edu.Stanford.Nlp.Util.CacheMap<K, V> c = ErasureUtils.UncheckedCast(ois.ReadObject());
					log.Info("Read cache from " + file + ", contains " + c.Count + " entries.  Backing file is " + c.backingFile);
					if (!useFileParams)
					{
						c.backingFile = file;
						c.hits = c.misses = c.puts = 0;
						c.CacheEntries = numEntries;
					}
					return c;
				}
			}
			catch (FileNotFoundException)
			{
				log.Info("Cache file " + file + " has not been created yet.  Making new one.");
				return new Edu.Stanford.Nlp.Util.CacheMap<K, V>(numEntries, loadFactor, accessOrder, file);
			}
			catch (Exception)
			{
				log.Info("Error reading cache file " + file + ".  Making a new cache and NOT backing to file.");
				return new Edu.Stanford.Nlp.Util.CacheMap<K, V>(numEntries, loadFactor, accessOrder);
			}
		}

		public static Edu.Stanford.Nlp.Util.CacheMap<K, V> Create<K, V>(int numEntries, float loadFactor, string file, bool useFileParams)
		{
			return Create(numEntries, loadFactor, false, file, useFileParams);
		}

		public static Edu.Stanford.Nlp.Util.CacheMap<K, V> Create<K, V>(int numEntries, string file, bool useFileParams)
		{
			return Create(numEntries, .75f, false, file, useFileParams);
		}

		public static Edu.Stanford.Nlp.Util.CacheMap<K, V> Create<K, V>(string file, bool useFileParams)
		{
			return Create(1000, .75f, false, file, useFileParams);
		}

		/// <summary>Dump out the contents of the cache to the backing file.</summary>
		public virtual void Write()
		{
			// Do this even if not writing so we printStats() at good times
			entriesSinceLastWritten = 0;
			if (frequencyToWrite < CacheEntries / 4)
			{
				frequencyToWrite *= 2;
			}
			if (backingFile == null)
			{
				return;
			}
			try
			{
				using (ObjectOutputStream oos = new ObjectOutputStream(new FileOutputStream(backingFile)))
				{
					log.Info("Writing cache (size: " + Count + ") to " + backingFile);
					oos.WriteObject(this);
				}
			}
			catch (Exception ex)
			{
				log.Info("Error writing cache to file: " + backingFile + '!');
				log.Info(ex);
			}
		}

		/// <seealso cref="Java.Util.LinkedHashMap{K, V}.RemoveEldestEntry(System.Collections.DictionaryEntry{K, V})"/>
		protected override bool RemoveEldestEntry(KeyValuePair<K, V> eldest)
		{
			return Count > CacheEntries;
		}

		/// <seealso cref="System.Collections.Hashtable{K, V}.Get(object)"/>
		public override V Get(object key)
		{
			V result = base.Get;
			if (result == null)
			{
				misses++;
			}
			else
			{
				hits++;
			}
			return result;
		}

		/// <summary>
		/// Add the entry to the map, and dump the map to a file if it's been a while
		/// since we last did.
		/// </summary>
		/// <seealso cref="System.Collections.Hashtable{K, V}.Put(object, object)"/>
		public override V Put(K key, V value)
		{
			V result = base.Put;
			puts++;
			if (++entriesSinceLastWritten >= frequencyToWrite)
			{
				Write();
			}
			// okay if backingFile is null
			//      printStats(System.err);
			return result;
		}

		/// <summary>Print out cache stats to the specified stream.</summary>
		/// <remarks>
		/// Print out cache stats to the specified stream.  Note that in many cases
		/// treating puts as misses gives a better version of hit percentage than
		/// actually using misses, since it's possible that some of your misses are
		/// because you wind up choosing not to cache the particular value (we output
		/// both versions).  Stats are reset when the cache is loaded in from disk
		/// but are otherwise cumulative.
		/// </remarks>
		public virtual void PrintStats(TextWriter @out)
		{
			@out.WriteLine("cache stats: size: " + Count + ", hits: " + hits + ", misses: " + misses + ", puts: " + puts + ", hit % (using misses): " + ((float)hits) / (hits + misses) + ", hit % (using puts): " + ((float)hits) / (hits + puts));
		}
	}
}
