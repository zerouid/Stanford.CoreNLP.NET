using System;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Wordseg
{
	public class NonDict2
	{
		public string corporaDict = "/u/nlp/data/gale/segtool/stanford-seg/data/";

		private static CorpusDictionary cd = null;

		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.NonDict2));

		public NonDict2(SeqClassifierFlags flags)
		{
			//public String sighanCorporaDict = "/u/nlp/data/chinese-segmenter/";
			if (cd == null)
			{
				if (flags.sighanCorporaDict != null)
				{
					corporaDict = flags.sighanCorporaDict;
				}
				// use the same flag for Sighan 2005,
				// but our list is extracted from ctb
				string path;
				if (flags.useAs || flags.useHk || flags.useMsr)
				{
					throw new Exception("only support settings for CTB and PKU now.");
				}
				else
				{
					if (flags.usePk)
					{
						path = corporaDict + "/dict/pku.non";
					}
					else
					{
						// CTB
						path = corporaDict + "/dict/ctb.non";
					}
				}
				cd = new CorpusDictionary(path);
				// just output the msg...
				if (flags.useAs || flags.useHk || flags.useMsr)
				{
				}
				else
				{
					if (flags.usePk)
					{
						logger.Info("INFO: flags.usePk=true | building NonDict2 from " + path);
					}
					else
					{
						// CTB
						logger.Info("INFO: flags.usePk=false | building NonDict2 from " + path);
					}
				}
			}
		}

		public virtual string CheckDic(string c2, SeqClassifierFlags flags)
		{
			if (cd.GetW(c2).Equals("1"))
			{
				return "1";
			}
			return "0";
		}
	}
}
