using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Common;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceSentenceSegmenter : DomReader
	{
		private static readonly string[] sentenceFinalPunc = new string[] { ".", "!", "?" };

		private static ICollection<string> sentenceFinalPuncSet = Generics.NewHashSet();

		static AceSentenceSegmenter()
		{
			// list of tokens which mark sentence boundaries
			// set up sentenceFinalPuncSet
			foreach (string aSentenceFinalPunc in sentenceFinalPunc)
			{
				sentenceFinalPuncSet.Add(aSentenceFinalPunc);
			}
		}

		/// <param name="filenamePrefix">path to an ACE .sgm file (but not including the .sgm extension)</param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static IList<IList<AceToken>> TokenizeAndSegmentSentences(string filenamePrefix)
		{
			IList<IList<AceToken>> sentences = new List<IList<AceToken>>();
			File inputFile = new File(filenamePrefix + AceDocument.OrigExt);
			string input = IOUtils.SlurpFile(inputFile);
			// now we can split the text into tokens
			RobustTokenizer<Word> tokenizer = new RobustTokenizer<Word>(input);
			IList<RobustTokenizer.WordToken> tokenList = tokenizer.TokenizeToWordTokens();
			// and group the tokens into sentences
			List<AceToken> currentSentence = new List<AceToken>();
			int quoteCount = 0;
			for (int i = 0; i < tokenList.Count; i++)
			{
				RobustTokenizer.WordToken token = tokenList[i];
				string tokenText = token.GetWord();
				AceToken convertedToken = WordTokenToAceToken(token, sentences.Count);
				// start a new sentence if we skipped 2+ lines (after datelines, etc.)
				// or we hit some SGML
				// if (token.getNewLineCount() > 1 || AceToken.isSgml(tokenText)) {
				if (AceToken.IsSgml(tokenText))
				{
					if (currentSentence.Count > 0)
					{
						sentences.Add(currentSentence);
					}
					currentSentence = new List<AceToken>();
					quoteCount = 0;
				}
				currentSentence.Add(convertedToken);
				if (tokenText.Equals("\""))
				{
					quoteCount++;
				}
				// start a new sentence whenever we hit sentence-final punctuation
				if (sentenceFinalPuncSet.Contains(tokenText))
				{
					// include quotes after EOS
					if (i < tokenList.Count - 1 && quoteCount % 2 == 1 && tokenList[i + 1].GetWord().Equals("\""))
					{
						AceToken quoteToken = WordTokenToAceToken(tokenList[i + 1], sentences.Count);
						currentSentence.Add(quoteToken);
						quoteCount++;
						i++;
					}
					if (currentSentence.Count > 0)
					{
						sentences.Add(currentSentence);
					}
					currentSentence = new List<AceToken>();
					quoteCount = 0;
				}
				else
				{
					// start a new sentence when we hit an SGML tag
					if (AceToken.IsSgml(tokenText))
					{
						if (currentSentence.Count > 0)
						{
							sentences.Add(currentSentence);
						}
						currentSentence = new List<AceToken>();
						quoteCount = 0;
					}
				}
			}
			return sentences;
		}

		public static AceToken WordTokenToAceToken(RobustTokenizer.WordToken wordToken, int sentence)
		{
			return new AceToken(wordToken.GetWord(), string.Empty, string.Empty, string.Empty, string.Empty, int.ToString(wordToken.GetStart()), int.ToString(wordToken.GetEnd()), sentence);
		}

		// simple testing code
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Xml.Sax.SAXException"/>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"/>
		public static void Main(string[] args)
		{
			string testFilename = "/home/mcclosky/data/ACE2005/English/wl/timex2norm/AGGRESSIVEVOICEDAILY_20041101.1144";
			// testFilename =
			// "/home/mcclosky/data/ACE2005/English/bc/timex2norm/CNN_CF_20030303.1900.02";
			// testFilename =
			// "/home/mcclosky/data/ACE2005/English/un/timex2norm/alt.atheism_20041104.2428";
			testFilename = "/home/mcclosky/data/ACE2005/English/nw/timex2norm/AFP_ENG_20030502.0614";
			IList<IList<AceToken>> sentences = TokenizeAndSegmentSentences(testFilename);
			foreach (IList<AceToken> sentence in sentences)
			{
				System.Console.Out.WriteLine("s: [" + sentence + "]");
			}
		}
	}
}
