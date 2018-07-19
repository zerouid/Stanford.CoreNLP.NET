using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Common
{
	public class StringDictionary
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Common.StringDictionary));

		public class IndexAndCount
		{
			public readonly int mIndex;

			public int mCount;

			internal IndexAndCount(int i, int c)
			{
				mIndex = i;
				mCount = c;
			}
		}

		/// <summary>Name of this dictionary</summary>
		private readonly string mName;

		/// <summary>
		/// Access type: If true, create a dictionary entry if the entry does not exist
		/// in get Otherwise, return -1 if the entry does not exist in get
		/// </summary>
		private bool mCreate;

		/// <summary>The actual dictionary</summary>
		private IDictionary<string, StringDictionary.IndexAndCount> mDict;

		/// <summary>Inverse mapping from integer keys to the string values</summary>
		private IDictionary<int, string> mInverse;

		public StringDictionary(string name)
		{
			mName = name;
			mCreate = false;
			mDict = Generics.NewHashMap();
			mInverse = Generics.NewHashMap();
		}

		public virtual void SetMode(bool mode)
		{
			mCreate = mode;
		}

		public virtual int Size()
		{
			return mDict.Count;
		}

		public virtual int Get(string s)
		{
			return Get(s, true);
		}

		public virtual StringDictionary.IndexAndCount GetIndexAndCount(string s)
		{
			StringDictionary.IndexAndCount ic = mDict[s];
			if (mCreate)
			{
				if (ic == null)
				{
					ic = new StringDictionary.IndexAndCount(mDict.Count, 0);
					mDict[s] = ic;
					mInverse[int.Parse(ic.mIndex)] = s;
				}
				ic.mCount++;
			}
			return ic;
		}

		/// <summary>
		/// Fetches the index of this string If mCreate is true, the entry is created
		/// if it does not exist.
		/// </summary>
		/// <remarks>
		/// Fetches the index of this string If mCreate is true, the entry is created
		/// if it does not exist. If mCreate is true, the count of the entry is
		/// incremented for every get If no entry found throws an exception if
		/// shouldThrow == true
		/// </remarks>
		public virtual int Get(string s, bool shouldThrow)
		{
			StringDictionary.IndexAndCount ic = mDict[s];
			if (mCreate)
			{
				if (ic == null)
				{
					ic = new StringDictionary.IndexAndCount(mDict.Count, 0);
					mDict[s] = ic;
					mInverse[int.Parse(ic.mIndex)] = s;
				}
				ic.mCount++;
			}
			if (ic != null)
			{
				return ic.mIndex;
			}
			if (shouldThrow)
			{
				throw new Exception("Unknown entry \"" + s + "\" in dictionary \"" + mName + "\"!");
			}
			else
			{
				return -1;
			}
		}

		public const string NilValue = "nil";

		/// <summary>Reverse mapping from integer key to string value</summary>
		public virtual string Get(int idx)
		{
			if (idx == -1)
			{
				return NilValue;
			}
			string s = mInverse[idx];
			if (s == null)
			{
				throw new Exception("Unknown index \"" + idx + "\" in dictionary \"" + mName + "\"!");
			}
			return s;
		}

		public virtual int GetCount(int idx)
		{
			if (idx == -1)
			{
				return 0;
			}
			string s = mInverse[idx];
			if (s == null)
			{
				throw new Exception("Unknown index \"" + idx + "\" in dictionary \"" + mName + "\"!");
			}
			return GetIndexAndCount(s).mCount;
		}

		/// <summary>
		/// Saves all dictionary entries that appeared
		/// <literal>&gt;</literal>
		/// threshold times Note: feature
		/// indices are changed to contiguous values starting at 0. This is needed in
		/// order to minimize the memory allocated for the expanded feature vectors
		/// (average perceptron).
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Save(string path, string prefix, int threshold)
		{
			string fileName = path + File.separator + prefix + "." + mName;
			TextWriter os = new TextWriter(new FileOutputStream(fileName));
			int index = 0;
			foreach (KeyValuePair<string, StringDictionary.IndexAndCount> entry in mDict)
			{
				StringDictionary.IndexAndCount ic = entry.Value;
				if (ic.mCount > threshold)
				{
					os.WriteLine(entry.Key + ' ' + index + ' ' + ic.mCount);
					index++;
				}
			}
			os.Close();
			log.Info("Saved " + index + "/" + mDict.Count + " entries for dictionary \"" + mName + "\".");
		}

		public virtual void Clear()
		{
			mDict.Clear();
			mInverse.Clear();
		}

		public virtual ICollection<string> KeySet()
		{
			return mDict.Keys;
		}

		/// <summary>Loads all saved dictionary entries from disk</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Load(string path, string prefix)
		{
			string fileName = path + File.separator + prefix + "." + mName;
			using (BufferedReader @is = IOUtils.ReaderFromString(fileName))
			{
				for (string line; (line = @is.ReadLine()) != null; )
				{
					List<string> tokens = SimpleTokenize.Tokenize(line);
					if (tokens.Count != 3)
					{
						throw new Exception("Invalid dictionary line: " + line);
					}
					int index = System.Convert.ToInt32(tokens[1]);
					int count = System.Convert.ToInt32(tokens[2]);
					if (index < 0 || count <= 0)
					{
						throw new Exception("Invalid dictionary line: " + line);
					}
					StringDictionary.IndexAndCount ic = new StringDictionary.IndexAndCount(index, count);
					mDict[tokens[0]] = ic;
					mInverse[int.Parse(index)] = tokens[0];
				}
				log.Info("Loaded " + mDict.Count + " entries for dictionary \"" + mName + "\".");
			}
		}

		public virtual ICollection<string> Keys()
		{
			return mDict.Keys;
		}
	}
}
