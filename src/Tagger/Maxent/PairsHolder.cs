/*
* Title:       StanfordMaxEnt<p>
* Description: A Maximum Entropy Toolkit<p>
* Copyright:   The Board of Trustees of The Leland Stanford Junior University
* Company:     Stanford University<p>
*/
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// A simple class that maintains a list of WordTag pairs which are interned
	/// as they are added.
	/// </summary>
	/// <remarks>
	/// A simple class that maintains a list of WordTag pairs which are interned
	/// as they are added.  This stores a tagged corpus.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class PairsHolder
	{
		private readonly List<WordTag> arr = new List<WordTag>();

		public PairsHolder()
		{
		}

		// todo: In Java 5+, just make this class an ArrayList<WordTag> and be done with it?? Or actually, probably a PaddedList. Or need a WindowedList?
		// todo: This method seems crazy.  Can't we either just do nothing or using ensureCapacity()?
		public virtual void SetSize(int s)
		{
			while (arr.Count < s)
			{
				arr.Add(new WordTag(null, "NN"));
			}
		}

		// todo: remove NN.  NA okay?
		public virtual int GetSize()
		{
			return arr.Count;
		}

		internal virtual void Clear()
		{
			arr.Clear();
		}

		/* -----------------
		CDM May 2008.  This method was unused.  But it also has a bug in it
		in that the equals() test can never succeed (Integer vs WordTag).
		So I'm commenting it out for now....
		public int[] getIndexes(Object wordtag) {
		ArrayList<Integer> arr1 = new ArrayList<Integer>();
		int l = wordtag.hashCode();
		Integer lO = Integer.valueOf(l);
		for (int i = 0; i < arrNum.size(); i++) {
		if (arrNum.get(i).equals(lO)) {
		arr1.add(Integer.valueOf(i));
		}
		}
		int[] ret = new int[arr1.size()];
		for (int i = 0; i < arr1.size(); i++) {
		ret[i] = arr1.get(i).intValue();
		}
		return ret;
		}
		*/
		internal virtual void Add(WordTag wordtag)
		{
			arr.Add(wordtag);
		}

		internal virtual void SetWord(int pos, string word)
		{
			arr[pos].SetWord(word);
		}

		internal virtual void SetTag(int pos, string tag)
		{
			arr[pos].SetTag(tag);
		}

		/* Methods unused. Commented for now:
		public void save(String filename) {
		try {
		DataOutputStream rf = IOUtils.getDataOutputStream(filename);
		int sz = arr.size();
		rf.writeInt(sz);
		for (int i = 0; i < sz; i++) {
		//save the wordtag in the file
		WordTag wT = arr.get(i);
		rf.writeUTF(wT.word());
		rf.writeUTF(wT.tag());
		}
		rf.close();
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		
		public void read(String filename) {
		try {
		InDataStreamFile rf = new InDataStreamFile(filename);
		int len = rf.readInt();
		for (int i = 0; i < len; i++) {
		WordTag wT = new WordTag();
		wT.setWord(rf.readUTF());
		wT.setTag(rf.readUTF());
		add(wT);
		
		}
		rf.close();
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		*/
		internal virtual string GetTag(int position)
		{
			return arr[position].Tag();
		}

		internal virtual string GetWord(int position)
		{
			return arr[position].Word();
		}

		internal virtual string GetWord(History h, int position)
		{
			int p = h.current + position;
			return (p >= h.start && p <= h.end) ? arr[p].Word() : "NA";
		}

		internal virtual string GetTag(History h, int position)
		{
			int p = h.current + position;
			return (p >= h.start && p <= h.end) ? arr[p].Tag() : "NA";
		}
	}
}
