//AmbiguityClasses -- StanfordMaxEnt, A Maximum Entropy Toolkit
//Copyright (c) 2002-2008 Leland Stanford Junior University
//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//For more information, bug reports, fixes, contact:
//Christopher Manning
//Dept of Computer Science, Gates 1A
//Stanford CA 94305-9010
//USA
//Support/Questions: java-nlp-user@lists.stanford.edu
//Licensing: java-nlp-support@lists.stanford.edu
//http://www-nlp.stanford.edu/software/tagger.shtml
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>A collection of Ambiguity Class.</summary>
	/// <remarks>
	/// A collection of Ambiguity Class.
	/// <i>The code currently here is rotted and would need to be revived.</i>
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class AmbiguityClasses
	{
		private readonly IIndex<AmbiguityClass> classes;

		private const string naWord = "NA";

		public AmbiguityClasses(TTags ttags)
		{
			// TODO: if it's rotted and not used anywhere, can we just get rid of it all?  [CDM: It would be nice to keep and revive someday. It is a nice and sometimes useful idea.]
			// TODO: this isn't used anywhere, either
			// protected final AmbiguityClass naClass = new AmbiguityClass(null, false, null, null);
			classes = new HashIndex<AmbiguityClass>();
		}

		// naClass.init(naWord, ttags);
		private int Add(AmbiguityClass a)
		{
			if (classes.Contains(a))
			{
				return classes.IndexOf(a);
			}
			classes.Add(a);
			return classes.IndexOf(a);
		}

		protected internal virtual int GetClass(string word, Dictionary dict, int veryCommonWordThresh, TTags ttags)
		{
			if (word.Equals(naWord))
			{
				return -2;
			}
			if (dict.IsUnknown(word))
			{
				return -1;
			}
			bool veryCommon = dict.Sum(word) > veryCommonWordThresh;
			AmbiguityClass a = new AmbiguityClass(word, veryCommon, dict, ttags);
			// TODO: surely it would be faster and not too expensive to cache
			// the results of creating a whole bunch of these, since we're
			// probably constructing the same AmbiguityClass multiple times
			// for each word.  Furthermore, the separation of having two
			// constructors here is pretty awful, quite frankly.
			return Add(a);
		}
		/*
		public void print() {
		Object[] arrClasses = classes.objectsList().toArray();//s.keySet().toArray();
		System.out.println(arrClasses.length);
		//    System.out.println("Number of ambiguity classes is " + arrClasses.length);
		//    for (int i = 0; i < arrClasses.length; i++) {
		//      ((AmbiguityClass) arrClasses[i]).print();
		//    }
		}
		
		public void save(String filename) {
		try {
		DataOutputStream rf = IOUtils.getDataOutputStream(filename);
		Object[] arrClasses = classes.objectsList().toArray();//s.keySet().toArray();
		//      System.out.println("Number of ambiguity classes is " + arrClasses.length);
		//      rf.writeInt(arrClasses.length);
		// for (int i = 0; i < arrClasses.length; i++) {
		//rf.writeUTF(((AmbiguityClass) (arrClasses[i])).getWord());
		// }
		rf.close();
		} catch (Exception e) {
		e.printStackTrace();
		}
		
		}// save
		
		public void save(DataOutputStream file) {
		try {
		Object[] arrClasses = classes.objectsList().toArray();//s.keySet().toArray();
		//      System.out.println("Number of ambiguity classes is " + arrClasses.length);
		//      file.writeInt(arrClasses.length);
		for (int i = 0; i < arrClasses.length; i++) {
		//rf.writeUTF(((AmbiguityClass) (arrClasses[i])).getWord());
		AmbiguityClass cur = (AmbiguityClass) arrClasses[i];
		file.writeBoolean(cur.single);
		file.writeUTF(cur.getWord());
		}
		} catch (Exception e) {
		e.printStackTrace();
		}
		
		}// save
		
		
		public void read(String filename) {
		try {
		InDataStreamFile rf = new InDataStreamFile(filename);
		int len = rf.readInt();//this is the number of ambiguity classes
		for (int i = 0; i < len; i++) {
		boolean singleton = rf.readBoolean();
		//        int len_buff = rf.readInt();
		//        byte[] buff = new byte[len_buff];
		//        rf.read(buff);
		String word = rf.readUTF();//new String(buff);
		word = TestSentence.toNice(word);
		add(new AmbiguityClass(word, singleton));
		//init();
		}//i
		
		rf.close();
		} catch (IOException e) {
		e.printStackTrace();
		}
		}
		
		public void read(InDataStreamFile file) {
		try {
		int len = file.readInt();//this is the number of ambiguity classes
		for (int i = 0; i < len; i++) {
		boolean singleton = file.readBoolean();
		String word = file.readUTF();//new String(buff);
		word = TestSentence.toNice(word);
		add(new AmbiguityClass(word, singleton));
		}//i
		
		} catch (IOException e) {
		e.printStackTrace();
		}
		}
		*/
	}
}
