using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Maintains a map from words to tags and their counts.</summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class Dictionary
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Dictionary));

		private readonly IDictionary<string, TagCount> dict = Generics.NewHashMap();

		private readonly IDictionary<int, CountWrapper> partTakingVerbs = Generics.NewHashMap();

		private const string naWord = "NA";

		private const bool Verbose = false;

		public Dictionary()
		{
		}

		internal virtual void FillWordTagCounts(IDictionary<string, IntCounter<string>> wordTagCounts)
		{
			foreach (string word in wordTagCounts.Keys)
			{
				TagCount count = new TagCount(wordTagCounts[word]);
				dict[word] = count;
			}
		}

		/*
		public void release() {
		dict.clear();
		}
		
		public void addVPTaking(String verb, String tag, String partWord) {
		int h = verb.hashCode();
		Integer i = Integer.valueOf(h);
		if (tag.startsWith("RP")) {
		if (this.partTakingVerbs.containsKey(i)) {
		this.partTakingVerbs.get(i).incPart(partWord);
		} else {
		this.partTakingVerbs.put(i, new CountWrapper(verb, 0, 0, 0, 0));
		this.partTakingVerbs.get(i).incPart(partWord);
		}
		} else if (tag.startsWith("RB")) {
		if (this.partTakingVerbs.containsKey(i)) {
		this.partTakingVerbs.get(i).incRB(partWord);
		} else {
		this.partTakingVerbs.put(i, new CountWrapper(verb, 0, 0, 0, 0));
		this.partTakingVerbs.get(i).incRB(partWord);
		}
		} else if (tag.startsWith("IN")) {
		if (this.partTakingVerbs.containsKey(i)) {
		this.partTakingVerbs.get(i).incIn(partWord);
		} else {
		this.partTakingVerbs.put(i, new CountWrapper(verb, 0, 0, 0, 0));
		this.partTakingVerbs.get(i).incIn(partWord);
		}
		}
		}
		*/
		protected internal virtual void AddVThatTaking(string verb)
		{
			int i = verb.GetHashCode();
			if (this.partTakingVerbs.Contains(i))
			{
				this.partTakingVerbs[i].IncThat();
			}
			else
			{
				this.partTakingVerbs[i] = new CountWrapper(verb, 0, 1, 0, 0);
			}
		}

		protected internal virtual int GetCountPart(string verb)
		{
			int i = verb.GetHashCode();
			if (this.partTakingVerbs.Contains(i))
			{
				return this.partTakingVerbs[i].GetCountPart();
			}
			return 0;
		}

		protected internal virtual int GetCountThat(string verb)
		{
			int i = verb.GetHashCode();
			if (this.partTakingVerbs.Contains(i))
			{
				return this.partTakingVerbs[i].GetCountThat();
			}
			return 0;
		}

		protected internal virtual int GetCountIn(string verb)
		{
			int i = verb.GetHashCode();
			if (this.partTakingVerbs.Contains(i))
			{
				return this.partTakingVerbs[i].GetCountIn();
			}
			return 0;
		}

		protected internal virtual int GetCountRB(string verb)
		{
			int i = verb.GetHashCode();
			if (this.partTakingVerbs.Contains(i))
			{
				return this.partTakingVerbs[i].GetCountRB();
			}
			return 0;
		}

		protected internal virtual int GetCount(string word, string tag)
		{
			TagCount count = dict[word];
			if (count == null)
			{
				return 0;
			}
			else
			{
				return count.Get(tag);
			}
		}

		protected internal virtual string[] GetTags(string word)
		{
			TagCount count = Get(word);
			if (count == null)
			{
				return null;
			}
			return count.GetTags();
		}

		protected internal virtual TagCount Get(string word)
		{
			return dict[word];
		}

		internal virtual string GetFirstTag(string word)
		{
			TagCount count = dict[word];
			if (count != null)
			{
				return count.GetFirstTag();
			}
			return null;
		}

		protected internal virtual int Sum(string word)
		{
			TagCount count = dict[word];
			if (count != null)
			{
				return count.Sum();
			}
			return 0;
		}

		internal virtual bool IsUnknown(string word)
		{
			return !dict.Contains(word);
		}

		/*
		public void save(String filename) {
		try {
		DataOutputStream rf = IOUtils.getDataOutputStream(filename);
		save(rf);
		rf.close();
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		*/
		internal virtual void Save(DataOutputStream file)
		{
			string[] arr = Sharpen.Collections.ToArray(dict.Keys, new string[dict.Keys.Count]);
			try
			{
				file.WriteInt(arr.Length);
				log.Info("Saving dictionary of " + arr.Length + " words ...");
				foreach (string word in arr)
				{
					TagCount count = Get(word);
					file.WriteUTF(word);
					count.Save(file);
				}
				int[] arrverbs = Sharpen.Collections.ToArray(this.partTakingVerbs.Keys, new int[partTakingVerbs.Keys.Count]);
				file.WriteInt(arrverbs.Length);
				foreach (int iO in arrverbs)
				{
					CountWrapper tC = this.partTakingVerbs[iO];
					file.WriteInt(iO);
					tC.Save(file);
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void Read(DataInputStream rf, string filename)
		{
			// Object[] arr=dict.keySet().toArray();
			int maxNumTags = 0;
			int len = rf.ReadInt();
			for (int i = 0; i < len; i++)
			{
				string word = rf.ReadUTF();
				TagCount count = TagCount.ReadTagCount(rf);
				int numTags = count.NumTags();
				if (numTags > maxNumTags)
				{
					maxNumTags = numTags;
				}
				this.dict[word] = count;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void ReadTags(DataInputStream rf)
		{
			// Object[] arr=dict.keySet().toArray();
			int maxNumTags = 0;
			int len = rf.ReadInt();
			for (int i = 0; i < len; i++)
			{
				string word = rf.ReadUTF();
				TagCount count = TagCount.ReadTagCount(rf);
				int numTags = count.NumTags();
				if (numTags > maxNumTags)
				{
					maxNumTags = numTags;
				}
				this.dict[word] = count;
			}
		}

		protected internal virtual void Read(string filename)
		{
			try
			{
				DataInputStream rf = IOUtils.GetDataInputStream(filename);
				Read(rf, filename);
				int len1 = rf.ReadInt();
				for (int i = 0; i < len1; i++)
				{
					int iO = rf.ReadInt();
					CountWrapper tC = new CountWrapper();
					tC.Read(rf);
					this.partTakingVerbs[iO] = tC;
				}
				rf.Close();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		protected internal virtual void Read(DataInputStream file)
		{
			try
			{
				ReadTags(file);
				int len1 = file.ReadInt();
				for (int i = 0; i < len1; i++)
				{
					int iO = file.ReadInt();
					CountWrapper tC = new CountWrapper();
					tC.Read(file);
					this.partTakingVerbs[iO] = tC;
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/*
		public void printAmbiguous() {
		String[] arr = dict.keySet().toArray(new String[dict.keySet().size()]);
		try {
		int countAmbiguous = 0;
		int countUnAmbiguous = 0;
		int countAmbDisamb = 0;
		for (String word : arr) {
		if (word.indexOf('|') == -1) {
		continue;
		}
		TagCount count = get(word);
		if (count.numTags() > 1) {
		System.out.print(word);
		countAmbiguous++;
		tC.print();
		System.out.println();
		} else {
		String wordA = word.substring(0, word.indexOf('|'));
		if (get(wordA).numTags() > 1) {
		System.out.print(word);
		countAmbDisamb++;
		countUnAmbiguous++;
		tC.print();
		System.out.println();
		} else {
		countUnAmbiguous++;
		}
		}// else
		}
		System.out.println(" ambg " + countAmbiguous + " unambg " + countUnAmbiguous + " disamb " + countAmbDisamb);
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		*/
		/// <summary>
		/// This makes ambiguity classes from all words in the dictionary and remembers
		/// their classes in the TagCounts
		/// </summary>
		protected internal virtual void SetAmbClasses(AmbiguityClasses ambClasses, int veryCommonWordThresh, TTags ttags)
		{
			foreach (KeyValuePair<string, TagCount> entry in dict)
			{
				string w = entry.Key;
				TagCount count = entry.Value;
				int ambClassId = ambClasses.GetClass(w, this, veryCommonWordThresh, ttags);
				count.SetAmbClassId(ambClassId);
			}
		}

		protected internal virtual int GetAmbClass(string word)
		{
			if (word.Equals(naWord))
			{
				return -2;
			}
			if (Get(word) == null)
			{
				return -1;
			}
			return Get(word).GetAmbClassId();
		}

		public static void Main(string[] args)
		{
			string s = "word";
			string tag = "tag";
			Edu.Stanford.Nlp.Tagger.Maxent.Dictionary d = new Edu.Stanford.Nlp.Tagger.Maxent.Dictionary();
			System.Console.Out.WriteLine(d.GetCount(s, tag));
			System.Console.Out.WriteLine(d.GetFirstTag(s));
		}
	}
}
