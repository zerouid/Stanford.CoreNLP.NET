using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	public class ASBCunkDict
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict));

		private const string defaultFilename = "/u/nlp/data/pos-tagger/asbc_amb.fixed.gb18030";

		private static Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict ASBCunkDictSingleton = null;

		private static Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict GetInstance()
		{
			lock (typeof(ASBCunkDict))
			{
				if (ASBCunkDictSingleton == null)
				{
					ASBCunkDictSingleton = new Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict();
				}
				return ASBCunkDictSingleton;
			}
		}

		private ASBCunkDict()
		{
			ReadASBCunkDict(defaultFilename);
		}

		private static IDictionary<string, ICollection<string>> ASBCunk_dict;

		private static void ReadASBCunkDict(string filename)
		{
			try
			{
				BufferedReader ASBCunkDetectorReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "GB18030"));
				string ASBCunkDetectorLine;
				ASBCunk_dict = Generics.NewHashMap();
				while ((ASBCunkDetectorLine = ASBCunkDetectorReader.ReadLine()) != null)
				{
					string[] fields = ASBCunkDetectorLine.Split(" ");
					string tag = fields[1];
					ICollection<string> words = ASBCunk_dict[tag];
					if (words == null)
					{
						words = Generics.NewHashSet();
						ASBCunk_dict[tag] = words;
					}
					words.Add(fields[0]);
				}
			}
			catch (FileNotFoundException)
			{
				log.Info("ASBCunk not found:");
				System.Environment.Exit(-1);
			}
			catch (IOException)
			{
				log.Info("ASBCunk");
				System.Environment.Exit(-1);
			}
		}

		protected internal static string GetTag(string a1, string a2)
		{
			Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict dict = Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict.GetInstance();
			if (Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict.Get(a1) == null)
			{
				return "0";
			}
			if (Edu.Stanford.Nlp.Tagger.Maxent.ASBCunkDict.Get(a1).Contains(a2))
			{
				return "1";
			}
			return "0";
		}

		private static ICollection<string> Get(string a)
		{
			return ASBCunk_dict[a];
		}
		/*
		public static String getPathPrefix() {
		return pathPrefix;
		}
		
		
		public static void setPathPrefix(String pathPrefix) {
		ASBCunkDict.pathPrefix = pathPrefix;
		}
		*/
		//class
	}
}
