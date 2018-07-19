using Edu.Stanford.Nlp.Util.Logging;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Look for verbs selecting a VBN verb.</summary>
	/// <remarks>
	/// Look for verbs selecting a VBN verb.
	/// This is now a zeroeth order observed data only feature.
	/// But reminiscent of what was done in Toutanova and Manning 2000.
	/// It doesn't seem to help tagging performance any more.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class ExtractorVerbalVBNZero : DictionaryExtractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.ExtractorVerbalVBNZero));

		private const string vbnTag = "VBN";

		private const string vbdTag = "VBD";

		private const string jjTag = "JJ";

		private const string edSuff = "ed";

		private const string enSuff = "en";

		private const string oneSt = "1";

		private const string naWord = "NA";

		private readonly int bound;

		private static readonly Pattern stopper = Pattern.Compile("(?i:and|or|but|,|;|-|--)");

		private static readonly Pattern vbnWord = Pattern.Compile("(?i:have|has|having|had|is|am|are|was|were|be|being|been|'ve|'s|s|'d|'re|'m|gotten|got|gets|get|getting)");

		public ExtractorVerbalVBNZero(int bound)
		{
			// cf. list in EnglishPTBTreebankCorrector
			this.bound = bound;
		}

		public override bool Precondition(string tag)
		{
			log.Info("VBN: Testing precondition on " + tag + ": " + (tag.Equals(vbnTag) || tag.Equals(vbdTag) || tag.Equals(jjTag)));
			return tag.Equals(vbnTag) || tag.Equals(vbdTag) || tag.Equals(jjTag);
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string cword = pH.GetWord(h, 0);
			int allCount = dict.Sum(cword);
			int vBNCount = dict.GetCount(cword, vbnTag);
			int vBDCount = dict.GetCount(cword, vbdTag);
			// Conditions for deciding inapplicable
			if ((allCount == 0) && (!(cword.EndsWith(edSuff) || cword.EndsWith(enSuff))))
			{
				return zeroSt;
			}
			if ((allCount > 0) && (vBNCount + vBDCount <= allCount / 100))
			{
				return zeroSt;
			}
			string lastverb = naWord;
			//String lastvtag = zeroSt; // mg: written but never read
			for (int index = -1; index >= -bound; index--)
			{
				string word2 = pH.GetWord(h, index);
				if ("NA".Equals(word2))
				{
					break;
				}
				if (stopper.Matcher(word2).Matches())
				{
					break;
				}
				if (vbnWord.Matcher(word2).Matches())
				{
					lastverb = word2;
					break;
				}
				index--;
			}
			if (!lastverb.Equals(naWord))
			{
				log.Info("VBN: For " + cword + ", found preceding VBN cue " + lastverb);
				return oneSt;
			}
			return zeroSt;
		}

		public override string ToString()
		{
			return "ExtractorVerbalVBNZero(bound=" + bound + ')';
		}

		private const long serialVersionUID = -5881204185400060636L;
	}
}
