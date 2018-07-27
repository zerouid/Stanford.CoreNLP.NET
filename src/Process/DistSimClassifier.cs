using System.Collections.Generic;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Maps a String to its distributional similarity class.</summary>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class DistSimClassifier
	{
		private const long serialVersionUID = 3L;

		private readonly IDictionary<string, string> lexicon;

		private readonly bool cased;

		private readonly bool numberEquivalence;

		private readonly string unknownWordClass;

		public DistSimClassifier(string filename, bool cased, bool numberEquivalence)
			: this(filename, "alexClark", "utf-8", -1, cased, numberEquivalence, "NULL")
		{
		}

		public DistSimClassifier(string filename, bool cased, bool numberEquivalence, string unknownWordClass)
			: this(filename, "alexClark", "utf-8", -1, cased, numberEquivalence, unknownWordClass)
		{
		}

		public DistSimClassifier(string filename, string format, string encoding, int distSimMaxBits, bool cased, bool numberEquivalence, string unknownWordClass)
		{
			this.cased = cased;
			this.numberEquivalence = numberEquivalence;
			this.unknownWordClass = unknownWordClass;
			Timing.StartDoing("Loading distsim lexicon from " + filename);
			lexicon = Generics.NewHashMap(1 << 15);
			// make a reasonable starting size
			bool terryKoo = "terryKoo".Equals(format);
			foreach (string line in ObjectBank.GetLineIterator(filename, encoding))
			{
				string word;
				string wordClass;
				if (terryKoo)
				{
					string[] bits = line.Split("\\t");
					word = bits[1];
					wordClass = bits[0];
					if (distSimMaxBits > 0 && wordClass.Length > distSimMaxBits)
					{
						wordClass = Sharpen.Runtime.Substring(wordClass, 0, distSimMaxBits);
					}
				}
				else
				{
					// "alexClark"
					string[] bits = line.Split("\\s+");
					word = bits[0];
					wordClass = bits[1];
				}
				if (!cased)
				{
					word = word.ToLower();
				}
				if (numberEquivalence)
				{
					word = WordShapeClassifier.WordShape(word, WordShapeClassifier.Wordshapedigits);
				}
				lexicon[word] = wordClass;
			}
			Timing.EndDoing();
		}

		public virtual string DistSimClass(string word)
		{
			if (!cased)
			{
				word = word.ToLower();
			}
			if (numberEquivalence)
			{
				word = WordShapeClassifier.WordShape(word, WordShapeClassifier.Wordshapedigits);
			}
			string distSim = lexicon[word];
			if (distSim == null)
			{
				distSim = unknownWordClass;
			}
			return distSim;
		}
	}
}
