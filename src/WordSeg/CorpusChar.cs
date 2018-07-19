using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>Check tag of each character from 5 different corpora.</summary>
	/// <remarks>
	/// Check tag of each character from 5 different corpora. (4 official training corpora of Sighan bakeoff 2005, plus CTB)
	/// These tags are not external knowledge. They are learned from the training corpora.
	/// </remarks>
	/// <author>Huihsin Tseng</author>
	/// <author>Pichuan Chang</author>
	public class CorpusChar
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.CorpusChar));

		private IDictionary<string, ICollection<string>> charMap;

		public CorpusChar(string charlistFilename)
		{
			charMap = ReadDict(charlistFilename);
		}

		private IDictionary<string, ICollection<string>> GetCharMap()
		{
			return charMap;
		}

		private static IDictionary<string, ICollection<string>> ReadDict(string filename)
		{
			IDictionary<string, ICollection<string>> char_dict;
			try
			{
				BufferedReader detectorReader = IOUtils.ReaderFromString(filename, "UTF-8");
				char_dict = Generics.NewHashMap();
				//logger.debug("DEBUG: in CorpusChar readDict");
				for (string detectorLine; (detectorLine = detectorReader.ReadLine()) != null; )
				{
					string[] fields = detectorLine.Split("	");
					string tag = fields[0];
					ICollection<string> chars = char_dict[tag];
					if (chars == null)
					{
						chars = Generics.NewHashSet();
						char_dict[tag] = chars;
					}
					//logger.debug("DEBUG: CorpusChar: "+filename+" "+fields[1]);
					chars.Add(fields[1]);
				}
				detectorReader.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			logger.Info("Loading character dictionary file from " + filename + " [done].");
			return char_dict;
		}

		public virtual string GetTag(string a1, string a2)
		{
			IDictionary<string, ICollection<string>> h1 = GetCharMap();
			ICollection<string> h2 = h1[a1];
			if (h2 == null)
			{
				return "0";
			}
			if (h2.Contains(a2))
			{
				return "1";
			}
			return "0";
		}
		/*
		public String getCtbTag(String a1, String a2) {
		HashMap h1=dict.getctb();
		Set h2=(Set)h1.get(a1);
		if (h2 == null) return "0";
		if (h2.contains(a2))
		return "1";
		return "0";
		}
		
		public String getAsbcTag(String a1, String a2) {
		HashMap h1=dict.getasbc();
		Set h2=(Set)h1.get(a1);
		if (h2 == null) return "0";
		if (h2.contains(a2))
		return "1";
		return "0";
		}
		
		public String getPkuTag(String a1, String a2) {
		HashMap h1=dict.getpku();
		Set h2=(Set)h1.get(a1);
		if (h2 == null) return "0";
		if (h2.contains(a2))
		return "1";
		return "0";
		}
		
		public String getHkTag(String a1, String a2) {
		HashMap h1=dict.gethk();
		Set h2=(Set)h1.get(a1);
		if (h2 == null) return "0";
		if (h2.contains(a2))
		return "1";
		return "0";
		}
		
		
		public String getMsrTag(String a1, String a2) {
		HashMap h1=dict.getmsr();
		Set h2=(Set)h1.get(a1);
		if (h2 == null) return "0";
		if (h2.contains(a2))
		return "1";
		return "0";
		}*/
		//end of class
	}
}
