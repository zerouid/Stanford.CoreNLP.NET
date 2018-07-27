using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Hash the instances on the things that the features look at.</summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	internal class ListInstances
	{
		private readonly List<int> v = new List<int>();

		private int[] positions = null;

		private int num = 0;

		internal ListInstances()
		{
		}

		protected internal virtual void Add(int x)
		{
			v.Add(x);
		}

		protected internal virtual void AddPositions(int s, int e)
		{
			positions = new int[2];
			positions[0] = s;
			positions[1] = e;
		}

		public virtual int[] GetPositions()
		{
			return positions;
		}

		protected internal virtual void Inc()
		{
			num++;
		}

		public virtual int GetNum()
		{
			return num;
		}

		public virtual int[] GetInstances()
		{
			int[] arr = new int[v.Count];
			int[] arr1 = new int[v.Count];
			Sharpen.Collections.ToArray(v, arr1);
			for (int i = 0; i < v.Count; i++)
			{
				arr[i] = arr1[i];
			}
			return arr;
		}
		/*
		Methods unused: commented for now.
		public void save(DataOutputStream rf) {
		try {
		rf.writeInt(v.size());
		int[] arr = getInstances();
		for (int i = 0; i < v.size(); i++) {
		rf.writeInt(arr[i]);
		}
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		
		public void read(DataInputStream rf) {
		try {
		int len = rf.readInt();
		for (int i = 0; i < len; i++) {
		int x = rf.readInt();
		add(x);
		}
		} catch (Exception e) {
		e.printStackTrace();
		}
		
		}// end read
		
		*/
	}

	public class TemplateHash
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.TemplateHash));

		private readonly IDictionary<Pair<int, string>, ListInstances> tempHash = Generics.NewHashMap();

		private readonly MaxentTagger maxentTagger;

		public TemplateHash(MaxentTagger maxentTagger)
		{
			// the positions of the feature extractors
			this.maxentTagger = maxentTagger;
		}

		protected internal virtual void AddPositions(int start, int end, FeatureKey fK)
		{
			Pair<int, string> key = new Pair<int, string>(fK.num, fK.val);
			tempHash[key].AddPositions(start, end);
		}

		protected internal virtual int[] GetPositions(FeatureKey s)
		{
			Pair<int, string> p = new Pair<int, string>(s.num, s.val);
			return tempHash[p].GetPositions();
		}

		//public void init() {
		//    cdm 2008: stringNums isn't used anywhere, so we now don't do any init.
		//    int num = maxentTagger.extractors.getSize() + maxentTagger.extractorsRare.getSize();
		//    //log.info("A total of "+num+" features in TemplateHash");
		//    stringNums = new String[num];
		//    for (int i = 0; i < num; i++) {
		//      stringNums[i] = String.valueOf(i);
		//    }
		//}
		protected internal virtual void Release()
		{
			tempHash.Clear();
		}

		protected internal virtual void Add(int nFeatFrame, History history, int number)
		{
			Pair<int, string> wT;
			int general = maxentTagger.extractors.Size();
			if (nFeatFrame < general)
			{
				wT = new Pair<int, string>(nFeatFrame, maxentTagger.extractors.Extract(nFeatFrame, history));
			}
			else
			{
				wT = new Pair<int, string>(nFeatFrame, maxentTagger.extractorsRare.Extract(nFeatFrame - general, history));
			}
			if (tempHash.Contains(wT))
			{
				ListInstances li = tempHash[wT];
				// TODO: can we clean this call up somehow?  perhaps make the
				// TemplateHash aware of the TaggerExperiments if we need to, or
				// vice-versa?
				if (TaggerExperiments.IsPopulated(nFeatFrame, li.GetNum(), maxentTagger))
				{
					li.Add(number);
				}
			}
			else
			{
				ListInstances li = new ListInstances();
				li.Add(number);
				tempHash[wT] = li;
			}
		}

		protected internal virtual void AddPrev(int nFeatFrame, History history)
		{
			Pair<int, string> wT;
			int general = maxentTagger.extractors.Size();
			if (nFeatFrame < general)
			{
				wT = new Pair<int, string>(nFeatFrame, maxentTagger.extractors.Extract(nFeatFrame, history));
			}
			else
			{
				wT = new Pair<int, string>(nFeatFrame, maxentTagger.extractorsRare.Extract(nFeatFrame - general, history));
			}
			if (tempHash.Contains(wT))
			{
				(tempHash[wT]).Inc();
			}
			else
			{
				ListInstances li = new ListInstances();
				li.Inc();
				tempHash[wT] = li;
			}
		}

		protected internal virtual int[] GetXValues(Pair<int, string> key)
		{
			if (tempHash.Contains(key))
			{
				return tempHash[key].GetInstances();
			}
			return null;
		}
		/* Methods unused. Commented for now.
		public void save(DataOutputStream rf) {
		try {
		Pair[] keys = new Pair[tempHash.keySet().size()];
		tempHash.keySet().toArray(keys);
		rf.writeInt(keys.length);
		for (Pair key : keys) {
		//rf.writeInt(s.length());
		//rf.write(s.getBytes());
		key.save(rf);
		tempHash.get(key).save(rf);
		} // for
		
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		
		private void read(InDataStreamFile rf) {
		try {
		int numElem = rf.readInt();
		for (int i = 0; i < numElem; i++) {
		//int strLen=rf.readInt();
		//byte[] buff=new byte[strLen];
		//rf.read(buff);
		//String s=new String(buff);
		Pair<String,String> sWT = Pair.readStringPair(rf);
		Pair<Integer,String> wT = new Pair<Integer,String>(Integer.parseInt(sWT.first()), sWT.second());
		ListInstances li = new ListInstances();
		li.read(rf);
		tempHash.put(wT, li);
		}// for
		} catch (Exception e) {
		e.printStackTrace();
		}
		}
		
		public void print() {
		Object[] arr = tempHash.keySet().toArray();
		for (int i = 0; i < arr.length; i++) {
		System.out.println(arr[i]);
		}
		}
		
		public static void main(String[] args) {
		TemplateHash hT = new TemplateHash();
		Pair<Integer,String> p = new Pair<Integer,String>(0, "0");
		ListInstances li = new ListInstances();
		li.add(14);
		hT.tempHash.put(p, new ListInstances());
		if (hT.tempHash.containsKey(p)) {
		System.out.println(hT.tempHash.get(p));
		}
		}
		
		// Read a string representation of a Pair from a DataStream.
		// This might not work correctly unless the pair of objects are of type
		// [@code String}.
		//
		public static Pair<String, String> readStringPair(DataInputStream in) {
		Pair<String, String> p = new Pair<>();
		try {
		p.first = in.readUTF();
		p.second = in.readUTF();
		} catch (Exception e) {
		e.printStackTrace();
		}
		return p;
		}
		
		*/
	}
}
