using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.Tuebadz
{
	/// <summary>Language pack for the Tuebingen Treebank of Written German (TueBa-D/Z).</summary>
	/// <remarks>
	/// Language pack for the Tuebingen Treebank of Written German (TueBa-D/Z).
	/// http://www.sfs.nphil.uni-tuebingen.de/en_tuebadz.shtml
	/// This treebank is in utf-8.
	/// </remarks>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	[System.Serializable]
	public class TueBaDZLanguagePack : AbstractTreebankLanguagePack
	{
		private bool limitedGF = false;

		private static string[] gfToKeepArray = new string[] { "ON", "OA", "OD" };

		private static string[] tuebadzPunctTags = new string[] { "$.", "$,", "$-LRB" };

		private static string[] tuebadzSFPunctTags = new string[] { "$." };

		private static string[] tuebadzPunctWords = new string[] { "`", "-", ",", ";", ":", "!", "?", "/", ".", "...", "'", "\"", "[", "]", "*" };

		private static string[] tuebadzSFPunctWords = new string[] { ".", "!", "?" };

		/// <summary>The first one is used by the TueBaDZ Treebank, and the rest are used by Klein's lexparser.</summary>
		private static char[] annotationIntroducingChars = new char[] { ':', '^', '~', '%', '#', '=' };

		/// <summary>Gives a handle to the TreebankLanguagePack</summary>
		public TueBaDZLanguagePack()
			: this(false)
		{
		}

		/// <summary>Make a new language pack with grammatical functions used based on the value of leaveGF</summary>
		public TueBaDZLanguagePack(bool leaveGF)
			: this(leaveGF, AbstractTreebankLanguagePack.DefaultGfChar)
		{
		}

		/// <summary>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.
		/// </summary>
		/// <remarks>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.  gfChar should *not* be an annotation introducing character.
		/// </remarks>
		public TueBaDZLanguagePack(bool leaveGF, char gfChar)
			: this(false, leaveGF, gfChar)
		{
		}

		/// <summary>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.
		/// </summary>
		/// <remarks>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.  gfChar should *not* be an annotation introducing character.
		/// </remarks>
		public TueBaDZLanguagePack(bool useLimitedGF, bool leaveGF, char gfChar)
			: base(gfChar)
		{
			this.leaveGF = leaveGF;
			this.limitedGF = useLimitedGF;
		}

		/// <summary>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// </summary>
		/// <remarks>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// The idea here is that Penn treebank style labels follow a syntactic
		/// category with various functional and crossreferencing information
		/// introduced by special characters (such as "NP-SBJ=1").  This would
		/// be truncated to "NP" by the array containing '-' and "=".
		/// </remarks>
		/// <returns>An array of characters that set off label name suffixes</returns>
		public override char[] LabelAnnotationIntroducingCharacters()
		{
			return annotationIntroducingChars;
		}

		public override string[] PunctuationTags()
		{
			return tuebadzPunctTags;
		}

		public override string[] PunctuationWords()
		{
			return tuebadzPunctWords;
		}

		public override string[] SentenceFinalPunctuationTags()
		{
			return tuebadzSFPunctTags;
		}

		public override string[] StartSymbols()
		{
			return new string[] { "TOP" };
		}

		public override string[] SentenceFinalPunctuationWords()
		{
			return tuebadzSFPunctWords;
		}

		public override string TreebankFileExtension()
		{
			return ".penn";
		}

		private bool leaveGF = false;

		public override string BasicCategory(string category)
		{
			string basicCat = base.BasicCategory(category);
			if (!leaveGF)
			{
				basicCat = StripGF(basicCat);
			}
			return basicCat;
		}

		public override string StripGF(string category)
		{
			if (category == null)
			{
				return null;
			}
			int index = category.LastIndexOf(gfCharacter);
			if (index > 0)
			{
				if (!limitedGF || !ContainsKeptGF(category, index))
				{
					category = Sharpen.Runtime.Substring(category, 0, index);
				}
			}
			return category;
		}

		/// <summary>
		/// Helper method for determining if the gf in category
		/// is one of those in the array gfToKeepArray.
		/// </summary>
		/// <remarks>
		/// Helper method for determining if the gf in category
		/// is one of those in the array gfToKeepArray.  Index is the
		/// index where the gfCharacter appears.
		/// </remarks>
		private static bool ContainsKeptGF(string category, int index)
		{
			foreach (string gf in gfToKeepArray)
			{
				int gfLength = gf.Length;
				if (gfLength < (category.Length - index))
				{
					if (Sharpen.Runtime.Substring(category, index + 1).Equals(gf))
					{
						//category.substring(index+1, index+1+gfLength).equals(gf))
						return true;
					}
				}
			}
			return false;
		}

		public virtual bool IsLeaveGF()
		{
			return leaveGF;
		}

		public virtual void SetLeaveGF(bool leaveGF)
		{
			this.leaveGF = leaveGF;
		}

		/// <summary>Return the input Charset encoding for the Treebank.</summary>
		/// <remarks>
		/// Return the input Charset encoding for the Treebank.
		/// See documentation for the <code>Charset</code> class.
		/// </remarks>
		/// <returns>Name of Charset</returns>
		public override string GetEncoding()
		{
			return "iso-8859-15";
		}

		/// <summary>Prints a few aspects of the TreebankLanguagePack, just for debugging.</summary>
		public static void Main(string[] args)
		{
			ITreebankLanguagePack tlp = new Edu.Stanford.Nlp.Trees.International.Tuebadz.TueBaDZLanguagePack();
			System.Console.Out.WriteLine("Start symbol: " + tlp.StartSymbol());
			string start = tlp.StartSymbol();
			System.Console.Out.WriteLine("Should be true: " + (tlp.IsStartSymbol(start)));
			string[] strs = new string[] { "-", "-LLB-", "NP-2", "NP=3", "NP-LGS", "NP-TMP=3", "CARD-HD" };
			foreach (string str in strs)
			{
				System.Console.Out.WriteLine("String: " + str + " basic: " + tlp.BasicCategory(str) + " basicAndFunc: " + tlp.CategoryAndFunction(str));
			}
		}

		private const long serialVersionUID = 2697418320262700673L;

		public virtual bool IsLimitedGF()
		{
			return limitedGF;
		}

		public virtual void SetLimitedGF(bool limitedGF)
		{
			this.limitedGF = limitedGF;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new TueBaDZTreeReaderFactory(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new TueBaDZHeadFinder();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new TueBaDZHeadFinder();
		}
	}
}
