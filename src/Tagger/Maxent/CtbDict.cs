using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	public class CtbDict
	{
		private const string defaultFilename = "ctb_dict.txt";

		private static Edu.Stanford.Nlp.Tagger.Maxent.CtbDict ctbDictSingleton;

		private static Edu.Stanford.Nlp.Tagger.Maxent.CtbDict GetInstance()
		{
			lock (typeof(CtbDict))
			{
				if (ctbDictSingleton == null)
				{
					ctbDictSingleton = new Edu.Stanford.Nlp.Tagger.Maxent.CtbDict();
				}
				return ctbDictSingleton;
			}
		}

		private CtbDict()
		{
			try
			{
				ReadCtbDict("/u/nlp/data/pos-tagger/dictionary" + '/' + defaultFilename);
			}
			catch (IOException e)
			{
				throw new Exception("can't open file: " + e.Message);
			}
		}

		public IDictionary<string, ICollection<string>> ctb_pre_dict;

		public IDictionary<string, ICollection<string>> ctb_suf_dict;

		/* java sucks */
		/// <exception cref="System.IO.IOException"/>
		private void ReadCtbDict(string filename)
		{
			BufferedReader ctbDetectorReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "GB18030"));
			string ctbDetectorLine;
			ctb_pre_dict = Generics.NewHashMap();
			ctb_suf_dict = Generics.NewHashMap();
			while ((ctbDetectorLine = ctbDetectorReader.ReadLine()) != null)
			{
				string[] fields = ctbDetectorLine.Split("	");
				string tag = fields[0];
				ICollection<string> pres = ctb_pre_dict[tag];
				ICollection<string> sufs = ctb_suf_dict[tag];
				if (pres == null)
				{
					pres = Generics.NewHashSet();
					ctb_pre_dict[tag] = pres;
				}
				pres.Add(fields[1]);
				if (sufs == null)
				{
					sufs = Generics.NewHashSet();
					ctb_suf_dict[tag] = sufs;
				}
				sufs.Add(fields[2]);
			}
		}

		//try
		protected internal static string GetTagPre(string a1, string a2)
		{
			Edu.Stanford.Nlp.Tagger.Maxent.CtbDict dict = Edu.Stanford.Nlp.Tagger.Maxent.CtbDict.GetInstance();
			if (dict.Getpre(a1) == null)
			{
				return "0";
			}
			if (dict.Getpre(a1).Contains(a2))
			{
				return "1";
			}
			return "0";
		}

		protected internal static string GetTagSuf(string a1, string a2)
		{
			Edu.Stanford.Nlp.Tagger.Maxent.CtbDict dict = Edu.Stanford.Nlp.Tagger.Maxent.CtbDict.GetInstance();
			if (dict.Getsuf(a1) == null)
			{
				return "0";
			}
			if (dict.Getsuf(a1).Contains(a2))
			{
				return "1";
			}
			return "0";
		}

		private ICollection<string> Getpre(string a)
		{
			return ctb_pre_dict[a];
		}

		private ICollection<string> Getsuf(string a)
		{
			return ctb_suf_dict[a];
		}
		//class
	}
}
