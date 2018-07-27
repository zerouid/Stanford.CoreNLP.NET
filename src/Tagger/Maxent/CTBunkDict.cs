using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	public class CTBunkDict
	{
		private const string defaultFilename = "ctb_amb";

		private static Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict CTBunkDictSingleton = null;

		private static IDictionary<string, ICollection<string>> CTBunk_dict;

		private static Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict GetInstance()
		{
			if (CTBunkDictSingleton == null)
			{
				CTBunkDictSingleton = new Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict();
			}
			return CTBunkDictSingleton;
		}

		private CTBunkDict()
		{
			ReadCTBunkDict("/u/nlp/data/pos-tagger/dictionary" + "/" + defaultFilename);
		}

		private static void ReadCTBunkDict(string filename)
		{
			CTBunk_dict = Generics.NewHashMap();
			try
			{
				BufferedReader CTBunkDetectorReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "GB18030"));
				for (string CTBunkDetectorLine; (CTBunkDetectorLine = CTBunkDetectorReader.ReadLine()) != null; )
				{
					string[] fields = CTBunkDetectorLine.Split(" ");
					string tag = fields[1];
					ICollection<string> words = CTBunk_dict[tag];
					if (words == null)
					{
						words = Generics.NewHashSet();
						CTBunk_dict[tag] = words;
					}
					words.Add(fields[0]);
				}
			}
			catch (FileNotFoundException e)
			{
				throw new RuntimeIOException("CTBunk file not found: " + filename, e);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("CTBunk I/O error: " + filename, e);
			}
		}

		/// <summary>
		/// Returns "1" as true if the dictionary listed this word with this tag,
		/// and "0" otherwise.
		/// </summary>
		/// <param name="tag">The POS tag</param>
		/// <param name="word">The word</param>
		/// <returns>
		/// "1" as true if the dictionary listed this word with this tag,
		/// and "0" otherwise.
		/// </returns>
		protected internal static string GetTag(string tag, string word)
		{
			Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict dict = Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict.GetInstance();
			ICollection<string> words = Edu.Stanford.Nlp.Tagger.Maxent.CTBunkDict.Get(tag);
			if (words != null && words.Contains(word))
			{
				return "1";
			}
			else
			{
				return "0";
			}
		}

		private static ICollection<string> Get(string a)
		{
			return CTBunk_dict[a];
		}
	}
}
