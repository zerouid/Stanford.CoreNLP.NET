using System;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <author>Huihsin Tseng</author>
	internal class TagAffixDetector
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.TagAffixDetector));

		private const bool Verbose = false;

		private readonly CorpusChar cc;

		private readonly AffixDictionary aD;

		private const string DefaultCorporaDict = "/u/nlp/data/gale/segtool/stanford-seg/data";

		public TagAffixDetector(SeqClassifierFlags flags)
		{
			// String sighanCorporaDict = "/u/nlp/data/chinese-segmenter/";
			string corporaDict;
			if (flags.sighanCorporaDict != null)
			{
				corporaDict = flags.sighanCorporaDict;
			}
			else
			{
				corporaDict = DefaultCorporaDict;
			}
			if (!corporaDict.IsEmpty() && !corporaDict.EndsWith("/"))
			{
				corporaDict = corporaDict + '/';
			}
			string ccPath;
			string adPath;
			if (flags.useChPos || flags.useCTBChar2 || flags.usePKChar2)
			{
				// if we're using POS information, override the ccPath
				// For now we only have list for CTB and PK
				if (flags.useASBCChar2 || flags.useHKChar2 || flags.useMSRChar2)
				{
					throw new Exception("only support settings for CTB and PK now.");
				}
				else
				{
					if (flags.useCTBChar2)
					{
						ccPath = corporaDict + "dict/character_list";
						adPath = corporaDict + "dict/in.ctb";
					}
					else
					{
						if (flags.usePKChar2)
						{
							ccPath = corporaDict + "dict/pos_open/character_list.pku.utf8";
							adPath = corporaDict + "dict/in.pk";
						}
						else
						{
							throw new Exception("none of flags.useXXXChar2 are on");
						}
					}
				}
			}
			else
			{
				ccPath = corporaDict + "dict/pos_close/char.ctb.list";
				adPath = corporaDict + "dict/in.ctb";
			}
			cc = new CorpusChar(ccPath);
			aD = new AffixDictionary(adPath);
		}

		internal virtual string CheckDic(string t2, string c2)
		{
			if (cc.GetTag(t2, c2).Equals("1"))
			{
				return "1";
			}
			return "0";
		}

		internal virtual string CheckInDic(string c2)
		{
			if (aD.GetInDict(c2).Equals("1"))
			{
				return "1";
			}
			return "0";
		}
	}
}
