using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Patterns
{
	public class Data
	{
		public static double ratioDomainNgramFreqWithDataFreq = 1;

		public static ICounter<CandidatePhrase> rawFreq = null;

		public static IList<File> sentsFiles = null;

		public static IDictionary<string, File> sentId2File = null;

		public static IDictionary<string, DataInstance> sents = null;

		public static string inMemorySaveFileLocation = string.Empty;

		public static ICounter<CandidatePhrase> processedDataFreq = null;

		public static ICounter<string> domainNGramRawFreq = new ClassicCounter<string>();

		public static double ratioGoogleNgramFreqWithDataFreq = 1;

		public static string domainNGramsFile = null;

		internal static bool usingGoogleNgram = false;

		public static IDictionary<string, IDictionary<string, IList<int>>> matchedTokensForEachPhrase = new ConcurrentHashMap<string, IDictionary<string, IList<int>>>();

		//when using batch processing, map from sentid to the file that has that sentence
		//save the in-memory sents to this file
		//  @Option(name = "googleNGramsFile")
		//  public static String googleNGramsFile = null;
		//public static Counter<String> googleNGram = new ClassicCounter<String>();
		public static void ComputeRawFreqIfNull(int numWordsCompound, bool batchProcess)
		{
			ConstantsAndVariables.DataSentsIterator iter = new ConstantsAndVariables.DataSentsIterator(batchProcess);
			while (iter.MoveNext())
			{
				ComputeRawFreqIfNull(iter.Current.First(), numWordsCompound);
			}
		}

		public static void ComputeRawFreqIfNull(IDictionary<string, DataInstance> sents, int numWordsCompound)
		{
			Redwood.Log(Redwood.Dbg, "Computing raw freq for every 1-" + numWordsCompound + " consecutive words");
			foreach (DataInstance l in sents.Values)
			{
				IList<IList<CoreLabel>> ngrams = CollectionUtils.GetNGrams(l.GetTokens(), 1, numWordsCompound);
				foreach (IList<CoreLabel> n in ngrams)
				{
					string s = string.Empty;
					foreach (CoreLabel c in n)
					{
						// if (useWord(c, commonEngWords, ignoreWordRegex)) {
						s += " " + c.Word();
					}
					// }
					s = s.Trim();
					if (!s.IsEmpty())
					{
						Data.rawFreq.IncrementCount(CandidatePhrase.CreateOrGet(s));
					}
				}
			}
			//if (googleNGram != null && googleNGram.size() > 0)
			if (usingGoogleNgram)
			{
				SetRatioGoogleNgramFreqWithDataFreq();
			}
			if (domainNGramRawFreq != null && domainNGramRawFreq.Size() > 0)
			{
				ratioDomainNgramFreqWithDataFreq = domainNGramRawFreq.TotalCount() / Data.rawFreq.TotalCount();
			}
		}

		public static void SetRatioGoogleNgramFreqWithDataFreq()
		{
			ratioGoogleNgramFreqWithDataFreq = GoogleNGramsSQLBacked.GetTotalCount(1) / Data.rawFreq.TotalCount();
			Redwood.Log(ConstantsAndVariables.minimaldebug, "Data", "ratioGoogleNgramFreqWithDataFreq is " + ratioGoogleNgramFreqWithDataFreq);
		}

		//return ratioGoogleNgramFreqWithDataFreq;
		//  public static void loadGoogleNGrams() {
		//    if (googleNGram == null || googleNGram.size() == 0) {
		//      for (String line : IOUtils.readLines(googleNGramsFile)) {
		//        String[] t = line.split("\t");
		//        googleNGram.setCount(t[0], Double.valueOf(t[1]));
		//      }
		//      Redwood.log(ConstantsAndVariables.minimaldebug, "Data", "loading freq from google ngram file " + googleNGramsFile);
		//    }
		//  }
		public static void LoadDomainNGrams()
		{
			System.Diagnostics.Debug.Assert((domainNGramsFile != null));
			if (domainNGramRawFreq == null || domainNGramRawFreq.Size() == 0)
			{
				foreach (string line in IOUtils.ReadLines(domainNGramsFile))
				{
					string[] t = line.Split("\t");
					domainNGramRawFreq.SetCount(t[0], double.ValueOf(t[1]));
				}
				Redwood.Log(ConstantsAndVariables.minimaldebug, "Data", "loading freq from domain ngram file " + domainNGramsFile);
			}
		}
	}
}
