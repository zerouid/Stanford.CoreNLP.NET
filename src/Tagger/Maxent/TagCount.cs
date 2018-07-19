using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class was created to store the possible tags of a word along with how many times
	/// the word appeared with each tag.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	internal class TagCount
	{
		private IDictionary<string, int> map = Generics.NewHashMap();

		private int ambClassId = -1;

		private string[] getTagsCache;

		private int sumCache;

		private TagCount()
		{
		}

		internal TagCount(IntCounter<string> tagCounts)
		{
			/* This is a numeric ID shared by all words that have the same set of possible tags. */
			// = null;
			// used internally
			foreach (string tag in tagCounts.KeySet())
			{
				map[tag] = tagCounts.GetIntCount(tag);
			}
			getTagsCache = Sharpen.Collections.ToArray(map.Keys, new string[map.Keys.Count]);
			sumCache = CalculateSumCache();
		}

		private const string NullSymbol = "<<NULL>>";

		/// <summary>Saves the object to the file.</summary>
		/// <param name="rf">
		/// is a file handle
		/// Supposedly other objects will be written after this one in the file. The method does not close the file. The TagCount is saved at the current position.
		/// </param>
		protected internal virtual void Save(DataOutputStream rf)
		{
			try
			{
				rf.WriteInt(map.Count);
				foreach (string tag in map.Keys)
				{
					if (tag == null)
					{
						rf.WriteUTF(NullSymbol);
					}
					else
					{
						rf.WriteUTF(tag);
					}
					rf.WriteInt(map[tag]);
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public virtual void SetAmbClassId(int ambClassId)
		{
			this.ambClassId = ambClassId;
		}

		public virtual int GetAmbClassId()
		{
			return ambClassId;
		}

		/// <summary>A TagCount object's fields are read from the file.</summary>
		/// <remarks>
		/// A TagCount object's fields are read from the file. They are read from
		/// the current position and the file is not closed afterwards.
		/// </remarks>
		public static Edu.Stanford.Nlp.Tagger.Maxent.TagCount ReadTagCount(DataInputStream rf)
		{
			try
			{
				Edu.Stanford.Nlp.Tagger.Maxent.TagCount tc = new Edu.Stanford.Nlp.Tagger.Maxent.TagCount();
				int numTags = rf.ReadInt();
				tc.map = Generics.NewHashMap(numTags);
				for (int i = 0; i < numTags; i++)
				{
					string tag = rf.ReadUTF();
					int count = rf.ReadInt();
					if (tag.Equals(NullSymbol))
					{
						tag = null;
					}
					tc.map[tag] = count;
				}
				tc.getTagsCache = Sharpen.Collections.ToArray(tc.map.Keys, new string[tc.map.Keys.Count]);
				tc.sumCache = tc.CalculateSumCache();
				return tc;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <returns>the number of total occurrences of the word .</returns>
		protected internal virtual int Sum()
		{
			return sumCache;
		}

		// Returns the number of occurrence of a particular tag.
		protected internal virtual int Get(string tag)
		{
			int count = map[tag];
			if (count == null)
			{
				return 0;
			}
			return count;
		}

		private int CalculateSumCache()
		{
			int s = 0;
			foreach (int i in map.Values)
			{
				s += i;
			}
			return s;
		}

		/// <returns>an array of the tags the word has had.</returns>
		public virtual string[] GetTags()
		{
			return getTagsCache;
		}

		//map.keySet().toArray(new String[0]);
		protected internal virtual int NumTags()
		{
			return map.Count;
		}

		/// <returns>the most frequent tag.</returns>
		public virtual string GetFirstTag()
		{
			string maxTag = null;
			int max = 0;
			foreach (string tag in map.Keys)
			{
				int count = map[tag];
				if (count > max)
				{
					maxTag = tag;
					max = count;
				}
			}
			return maxTag;
		}

		public override string ToString()
		{
			return map.ToString();
		}
	}
}
