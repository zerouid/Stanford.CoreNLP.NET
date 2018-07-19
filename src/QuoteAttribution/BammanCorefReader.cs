using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by michaelf on 12/30/15.</summary>
	/// <remarks>Created by michaelf on 12/30/15. Adapted from Grace Muzny's codebase</remarks>
	public class BammanCorefReader
	{
		/// <summary>The main output here is data/tokens/dickens.oliver.tokens, which contains the original book, one token per line, with part of speech, syntax, NER, coreference and other annotations.</summary>
		/// <remarks>
		/// The main output here is data/tokens/dickens.oliver.tokens, which contains the original book, one token per line, with part of speech, syntax, NER, coreference and other annotations. The (tab-separated) format is:
		/// Paragraph id
		/// Sentence id
		/// Token id
		/// Byte start
		/// Byte end
		/// Whitespace following the token (useful for pretty-printing the original text)
		/// Syntactic head id (-1 for the sentence root)
		/// Original token
		/// Normalized token (for quotes etc.)
		/// Lemma
		/// Penn Treebank POS tag
		/// NER tag (PERSON, NUMBER, DATE, DURATION, MISC, TIME, LOCATION, ORDINAL, MONEY, ORGANIZATION, SET, O)
		/// Stanford basic dependency label
		/// Within-quotation flag
		/// Character id (all coreferent tokens share the same character id)
		/// </remarks>
		/// <param name="filename"/>
		public static IDictionary<int, IList<CoreLabel>> ReadTokenFile(string filename, Annotation novel)
		{
			IList<string> lines = IOUtils.LinesFromFile(filename);
			IDictionary<int, IList<CoreLabel>> charsToTokens = new Dictionary<int, IList<CoreLabel>>();
			bool first = true;
			int tokenOffset = 0;
			foreach (string line in lines)
			{
				if (first)
				{
					first = false;
					continue;
				}
				string[] pieces = line.Split("\t");
				int tokenId = System.Convert.ToInt32(pieces[2]) + tokenOffset;
				string token = pieces[7];
				string normalizedTok = pieces[8];
				int characterId = System.Convert.ToInt32(pieces[14]);
				CoreLabel novelTok = novel.Get(typeof(CoreAnnotations.TokensAnnotation))[tokenId];
				// CoreNLP sometimes splits ". . . ." as ". . ." and "." and sometimes lemmatizes it. (The Steppe)
				if (pieces[7].Equals(". . . .") && !novelTok.Get(typeof(CoreAnnotations.OriginalTextAnnotation)).Equals(". . . ."))
				{
					tokenOffset++;
				}
				if (characterId != -1)
				{
					if (!novelTok.Get(typeof(CoreAnnotations.TextAnnotation)).Equals(normalizedTok))
					{
						System.Console.Error.WriteLine(token + " != " + novelTok.Get(typeof(CoreAnnotations.TextAnnotation)));
					}
					else
					{
						if (!charsToTokens.Contains(characterId))
						{
							charsToTokens[characterId] = new List<CoreLabel>();
						}
						charsToTokens[characterId].Add(novelTok);
					}
				}
			}
			return charsToTokens;
		}
	}
}
