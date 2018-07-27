//AmbiguityClass -- StanfordMaxEnt, A Maximum Entropy Toolkit
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
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//http://www-nlp.stanford.edu/software/tagger.shtml
using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>An ambiguity class for a word is the word by itself or its set of observed tags.</summary>
	public class AmbiguityClass
	{
		private readonly IList<int> sortedIds;

		private readonly string key;

		private readonly string word;

		private readonly bool single;

		protected internal AmbiguityClass(string word, bool single, Dictionary dict, TTags ttags)
		{
			// import java.util.HashSet;
			// private final HashSet<String> s;
			this.single = single;
			if (single)
			{
				this.word = word;
				sortedIds = Java.Util.Collections.EmptyList();
			}
			else
			{
				this.word = null;
				string[] tags = dict.GetTags(word);
				sortedIds = new List<int>(tags.Length);
				foreach (string tag in tags)
				{
					Add(ttags.GetIndex(tag));
				}
			}
			// s = Generics.newHashSet();
			// for (Integer sortedId : sortedIds) {
			//   s.add(ttags.getTag(sortedId));
			// }
			key = this.ToString();
		}

		public virtual string GetWord()
		{
			return word;
		}

		/*
		public boolean belongs(String word) {
		String[] tags = GlobalHolder.dict.getTags(word);
		if (tags.length != sortedIds.size()) {
		return false;
		}
		for (int i = 0; i < tags.length; i++) {
		if (!s.contains(tags[i])) {
		return false;
		}
		}
		members++;
		return true;
		} // belongs
		*/
		private bool Add(int tagId)
		{
			for (int j = 0; j < sortedIds.Count; j++)
			{
				if (tagId < sortedIds[j])
				{
					sortedIds.Add(j, tagId);
					return true;
				}
				if (tagId == sortedIds[j])
				{
					return false;
				}
			}
			sortedIds.Add(tagId);
			return true;
		}

		public override string ToString()
		{
			if (single)
			{
				return word;
			}
			StringBuilder sb = new StringBuilder();
			foreach (int sID in sortedIds)
			{
				sb.Append(':').Append(sID);
			}
			return sb.ToString();
		}

		/*
		public void print() {
		//System.out.print(word + " ");
		for (Integer sortedId : sortedIds) {
		System.out.print(GlobalHolder.tags.getTag(sortedId.intValue()));
		}
		System.out.println();
		}
		*/
		public override int GetHashCode()
		{
			return key.GetHashCode();
		}

		public override bool Equals(object o)
		{
			return o is Edu.Stanford.Nlp.Tagger.Maxent.AmbiguityClass && key.Equals(((Edu.Stanford.Nlp.Tagger.Maxent.AmbiguityClass)o).key);
		}
	}
}
