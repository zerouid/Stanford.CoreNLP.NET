using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Nndep;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <author>Grace Muzny, Michael Fang</author>
	public class QuoteAttributionUtils
	{
		//import edu.stanford.nlp.parser.ensemble.maltparser.core.options.option.IntegerOption;
		//TODO: change this to take the nearest (non-quote) sentence (even if not part of it)
		public static Pair<int, int> GetRemainderInSentence(Annotation doc, ICoreMap quote)
		{
			Pair<int, int> range = GetTokenRangePrecedingQuote(doc, quote);
			if (range == null)
			{
				range = GetTokenRangeFollowingQuote(doc, quote);
			}
			return range;
		}

		public static int GetQuoteParagraphIndex(Annotation doc, ICoreMap quote)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
		}

		//taken from WordToSentencesAnnotator
		private static ICoreMap ConstructSentence(IList<CoreLabel> sentenceTokens, ICoreMap prevSentence, ICoreMap sentence)
		{
			// get the sentence text from the first and last character offsets
			int begin = sentenceTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int last = sentenceTokens.Count - 1;
			int end = sentenceTokens[last].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			string sentenceText = prevSentence.Get(typeof(CoreAnnotations.TextAnnotation)) + sentence.Get(typeof(CoreAnnotations.TextAnnotation));
			// create a sentence annotation with text and token offsets
			Annotation newSentence = new Annotation(sentenceText);
			newSentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
			newSentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
			newSentence.Set(typeof(CoreAnnotations.TokensAnnotation), sentenceTokens);
			newSentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), prevSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
			newSentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), sentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
			newSentence.Set(typeof(CoreAnnotations.ParagraphIndexAnnotation), sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)));
			newSentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), GetParse(newSentence));
			return newSentence;
		}

		public class EnhancedSentenceAnnotation : ICoreAnnotation<ICoreMap>
		{
			//    newSentence.set(CoreAnnotations.SentenceIndexAnnotation.class, sentences.size());
			public virtual Type GetType()
			{
				return typeof(ICoreMap);
			}
		}

		public static void AddEnhancedSentences(Annotation doc)
		{
			//for every sentence that begins a paragraph: append this sentence and the previous one and see if sentence splitter would make a single sentence out of it. If so, add as extra sentence.
			//for each sieve that potentially uses augmentedSentences in original:
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			WordToSentenceProcessor wsp = new WordToSentenceProcessor(WordToSentenceProcessor.NewlineIsSentenceBreak.Never);
			//create SentenceSplitter that never splits on newline
			int prevParagraph = 0;
			for (int i = 1; i < sentences.Count; i++)
			{
				ICoreMap sentence = sentences[i];
				ICoreMap prevSentence = sentences[i - 1];
				IList<CoreLabel> tokensConcat = new List<CoreLabel>();
				Sharpen.Collections.AddAll(tokensConcat, prevSentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
				Sharpen.Collections.AddAll(tokensConcat, sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
				IList<IList<CoreLabel>> sentenceTokens = wsp.Process(tokensConcat);
				if (sentenceTokens.Count == 1)
				{
					//wsp would have put them into a single sentence --> add enhanced sentence.
					sentence.Set(typeof(QuoteAttributionUtils.EnhancedSentenceAnnotation), ConstructSentence(sentenceTokens[0], prevSentence, sentence));
				}
			}
		}

		//gets range of tokens that are in the same sentence as the beginning of the quote that precede it, if they exist,
		//or the previous sentence, if it is in the same paragraph.
		//also, ensure that the difference is at least two tokens
		public static Pair<int, int> GetTokenRangePrecedingQuote(Annotation doc, ICoreMap quote)
		{
			IList<ICoreMap> docSentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int quoteBeginTokenIndex = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (quoteBeginTokenIndex <= 2)
			{
				return null;
			}
			int quoteBeginSentenceIndex = quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation));
			ICoreMap beginSentence = docSentences[quoteBeginSentenceIndex];
			if (beginSentence.Get(typeof(QuoteAttributionUtils.EnhancedSentenceAnnotation)) != null)
			{
				beginSentence = beginSentence.Get(typeof(QuoteAttributionUtils.EnhancedSentenceAnnotation));
			}
			int quoteIndex = quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation));
			if (beginSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) < quoteBeginTokenIndex - 1)
			{
				//check previous quote to make sure boundary is okay- modify if necessary.
				if (quoteIndex > 0)
				{
					ICoreMap prevQuote = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation))[quoteIndex - 1];
					int prevQuoteTokenEnd = prevQuote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
					if (prevQuoteTokenEnd > beginSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)))
					{
						if (prevQuoteTokenEnd + 1 == quoteBeginTokenIndex)
						{
							return null;
						}
						return new Pair<int, int>(prevQuoteTokenEnd + 1, quoteBeginTokenIndex - 1);
					}
				}
				return new Pair<int, int>(beginSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quoteBeginTokenIndex - 1);
			}
			else
			{
				if (quoteBeginSentenceIndex > 0)
				{
					//try previous sentence- if it is in the same paragraph.
					int currParagraph = beginSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
					ICoreMap prevSentence = docSentences[quoteBeginSentenceIndex - 1];
					//check if prevSentence is in same paragraph
					if (prevSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == currParagraph)
					{
						//check previous quote boundary
						if (quoteIndex > 0)
						{
							ICoreMap prevQuote = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation))[quoteIndex - 1];
							int prevQuoteTokenEnd = prevQuote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
							if (prevQuoteTokenEnd > prevSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)))
							{
								if (prevQuoteTokenEnd + 1 == quoteBeginTokenIndex)
								{
									return null;
								}
								return new Pair<int, int>(prevQuoteTokenEnd + 1, quoteBeginTokenIndex - 1);
							}
							return new Pair<int, int>(prevSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quoteBeginTokenIndex - 1);
						}
					}
				}
			}
			return null;
		}

		public static Pair<int, int> GetTokenRangeFollowingQuote(Annotation doc, ICoreMap quote)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			int quoteEndTokenIndex = quote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
			if (quoteEndTokenIndex >= doc.Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 2)
			{
				return null;
			}
			int quoteEndSentenceIndex = quote.Get(typeof(CoreAnnotations.SentenceEndAnnotation));
			ICoreMap endSentence = sentences[quoteEndSentenceIndex];
			int quoteIndex = quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation));
			if (quoteEndTokenIndex < endSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 2)
			{
				//quote TokenEndAnnotation is inclusive; sentence TokenEndAnnotation is exclusive
				//check next quote to ensure boundary
				if (quoteIndex < quotes.Count - 1)
				{
					ICoreMap nextQuote = quotes[quoteIndex + 1];
					int nextQuoteTokenBegin = nextQuote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					if (nextQuoteTokenBegin < endSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1)
					{
						if (quoteEndTokenIndex + 1 == nextQuoteTokenBegin)
						{
							return null;
						}
						return new Pair<int, int>(quoteEndTokenIndex + 1, nextQuoteTokenBegin - 1);
					}
				}
				return new Pair<int, int>(quoteEndTokenIndex + 1, endSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1);
			}
			else
			{
				if (quoteEndSentenceIndex < sentences.Count - 1)
				{
					//check next sentence
					ICoreMap nextSentence = sentences[quoteEndSentenceIndex + 1];
					int currParagraph = endSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
					if (nextSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == currParagraph)
					{
						//check next quote boundary
						if (quoteIndex < quotes.Count - 1)
						{
							ICoreMap nextQuote = quotes[quoteIndex + 1];
							int nextQuoteTokenBegin = nextQuote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
							if (nextQuoteTokenBegin < nextSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1)
							{
								if (quoteEndTokenIndex + 1 == nextQuoteTokenBegin)
								{
									return null;
								}
								return new Pair<int, int>(quoteEndTokenIndex + 1, nextQuoteTokenBegin - 1);
							}
							return new Pair<int, int>(quoteEndTokenIndex + 1, nextSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1);
						}
					}
				}
			}
			return null;
		}

		private static ICoreMap ConstructCoreMap(Annotation doc, Pair<int, int> run)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			// check if the second part of the run is a *NL* token, adjust accordingly
			int endTokenIndex = run.second;
			while (endTokenIndex > 0 && tokens[endTokenIndex].Get(typeof(CoreAnnotations.IsNewlineAnnotation)))
			{
				endTokenIndex--;
			}
			// get the sentence text from the first and last character offsets
			int begin = tokens[run.first].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int end = tokens[endTokenIndex].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			string sentenceText = Sharpen.Runtime.Substring(doc.Get(typeof(CoreAnnotations.TextAnnotation)), begin, end);
			IList<CoreLabel> sentenceTokens = tokens.SubList(run.first, endTokenIndex + 1);
			// create a sentence annotation with text and token offsets
			ICoreMap sentence = new Annotation(sentenceText);
			sentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
			sentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
			sentence.Set(typeof(CoreAnnotations.TokensAnnotation), sentenceTokens);
			return sentence;
		}

		internal static DependencyParser parser = DependencyParser.LoadFromModelFile(DependencyParser.DefaultModel, new Properties());

		private static SemanticGraph GetParse(ICoreMap sentence)
		{
			GrammaticalStructure gs = parser.Predict(sentence);
			GrammaticalStructure.Extras maximal = GrammaticalStructure.Extras.Maximal;
			//        SemanticGraph deps = SemanticGraphFactory.makeFromTree(gs, SemanticGraphFactory.Mode.ENHANCED, maximal, true, null),
			//                uncollapsedDeps = SemanticGraphFactory.makeFromTree(gs, SemanticGraphFactory.Mode.BASIC, maximal, true, null),
			//    SemanticGraph ccDeps = SemanticGraphFactory.makeFromTree(gs, SemanticGraphFactory.Mode.ENHANCED_PLUS_PLUS, maximal, true, null);
			SemanticGraph ccDeps = SemanticGraphFactory.GenerateEnhancedPlusPlusDependencies(gs);
			return ccDeps;
		}

		public static void AnnotateForDependencyParse(Annotation doc)
		{
			// for each quote, dependency parse sentences with quote-removed (if it exists).
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				Pair<int, int> range = GetRemainderInSentence(doc, quote);
				if (range != null)
				{
					ICoreMap sentenceQuoteRemoved = ConstructCoreMap(doc, range);
					quote.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), GetParse(sentenceQuoteRemoved));
				}
			}
		}

		public static int GetParagraphRank(Annotation doc, ICoreMap quote)
		{
			int quoteParaBegin = GetParagraphBeginNumber(quote);
			IList<ICoreMap> sents = GetSentsInParagraph(doc, quoteParaBegin);
			IList<ICoreMap> quotesInParagraph = Generics.NewArrayList();
			foreach (ICoreMap q in doc.Get(typeof(CoreAnnotations.QuotationsAnnotation)))
			{
				if (GetParagraphBeginNumber(q) == quoteParaBegin)
				{
					quotesInParagraph.Add(q);
				}
			}
			return quotesInParagraph.IndexOf(quote);
		}

		public static int GetParagraphBeginNumber(ICoreMap quote)
		{
			IList<ICoreMap> sents = quote.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sents[0].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
		}

		public static int GetParagraphEndNumber(ICoreMap quote)
		{
			IList<ICoreMap> sents = quote.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sents[sents.Count - 1].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
		}

		public static IList<ICoreMap> GetSentsInParagraph(Annotation doc, int paragraph)
		{
			IList<ICoreMap> sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<ICoreMap> targets = Generics.NewArrayList();
			foreach (ICoreMap sent in sents)
			{
				if (sent.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == paragraph)
				{
					targets.Add(sent);
				}
			}
			return sents;
		}

		public static IList<ICoreMap> GetSentsForQuoteParagraphs(Annotation doc, ICoreMap quote)
		{
			IList<ICoreMap> sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int paragraphBegin = GetParagraphBeginNumber(quote);
			int paragraphEnd = GetParagraphEndNumber(quote);
			IList<ICoreMap> targets = Generics.NewArrayList();
			foreach (ICoreMap sent in sents)
			{
				if (sent.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) >= paragraphBegin && sent.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) <= paragraphEnd)
				{
					targets.Add(sent);
				}
			}
			return sents;
		}

		public static IDictionary<string, Person.Gender> ReadGenderedNounList(string filename)
		{
			IDictionary<string, Person.Gender> genderMap = Generics.NewHashMap();
			IList<string> lines = IOUtils.LinesFromFile(filename);
			foreach (string line in lines)
			{
				string[] nounAndStats = line.Split("\\t");
				string[] stats = nounAndStats[1].Split(" ");
				Person.Gender gender = (System.Convert.ToInt32(stats[0]) >= System.Convert.ToInt32(stats[1])) ? Person.Gender.Male : Person.Gender.Female;
				genderMap[nounAndStats[0]] = gender;
			}
			return genderMap;
		}

		public static ICollection<string> ReadFamilyRelations(string filename)
		{
			ICollection<string> familyRelations = Generics.NewHashSet();
			IList<string> lines = IOUtils.LinesFromFile(filename);
			foreach (string line in lines)
			{
				if (line.Trim().Length > 0)
				{
					familyRelations.Add(line.ToLower().Trim());
				}
			}
			return familyRelations;
		}

		public static ICollection<string> ReadAnimacyList(string filename)
		{
			ICollection<string> animacyList = Generics.NewHashSet();
			IList<string> lines = IOUtils.LinesFromFile(filename);
			foreach (string line in lines)
			{
				if (!char.IsUpperCase(line[0]))
				{
					//ignore names
					animacyList.Add(line);
				}
			}
			return animacyList;
		}

		//map each alias(i.e. the name of a character) to a character, potentially multiple if ambiguous.
		public static IDictionary<string, IList<Person>> ReadPersonMap(IList<Person> personList)
		{
			IDictionary<string, IList<Person>> personMap = new Dictionary<string, IList<Person>>();
			foreach (Person person in personList)
			{
				foreach (string alias in person.aliases)
				{
					if (personMap[alias] == null)
					{
						personMap[alias] = new List<Person>();
					}
					personMap[alias].Add(person);
				}
			}
			return personMap;
		}

		public static IDictionary<string, IList<Person>> ReadPersonMap(string fileName)
		{
			return ReadPersonMap(ReadCharacterList(fileName));
		}

		public static List<Person> ReadCharacterList(string filename)
		{
			List<Person> characterList = new List<Person>();
			//format: name;Gender(M or F); aliases (everything semi-colon delimited)
			foreach (string line in IOUtils.ReadLines(new File(filename)))
			{
				string[] terms = line.Split(";");
				if (terms.Length == 2)
				{
					characterList.Add(new Person(terms[0], terms[1], null));
				}
				else
				{
					List<string> aliases = new List<string>();
					for (int l = 2; l < terms.Length; l++)
					{
						aliases.Add(terms[l]);
					}
					aliases.Add(terms[0]);
					characterList.Add(new Person(terms[0], terms[1], aliases));
				}
			}
			return characterList;
		}

		public static bool IsPronominal(string potentialPronoun)
		{
			if (potentialPronoun.ToLower().Equals("he") || potentialPronoun.ToLower().Equals("she"))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static IDictionary<int, string> SetupCoref(string bammanFile, IDictionary<string, IList<Person>> characterMap, Annotation doc)
		{
			if (bammanFile != null)
			{
				//TODO: integrate coref
				IDictionary<int, IList<CoreLabel>> bammanTokens = BammanCorefReader.ReadTokenFile(bammanFile, doc);
				IDictionary<int, string> pronounCorefMap = MapBammanToCharacterMap(bammanTokens, characterMap);
				return pronounCorefMap;
			}
			else
			{
				IDictionary<int, string> pronounCorefMap = new Dictionary<int, string>();
				foreach (CorefChain cc in doc.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)).Values)
				{
					string representativeMention = cc.GetRepresentativeMention().mentionSpan;
					foreach (CorefChain.CorefMention cm in cc.GetMentionsInTextualOrder())
					{
						if (IsPronominal(cm.mentionSpan))
						{
							ICoreMap cmSentence = doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[cm.sentNum - 1];
							IList<CoreLabel> cmTokens = cmSentence.Get(typeof(CoreAnnotations.TokensAnnotation));
							int charBegin = cmTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
							pronounCorefMap[charBegin] = representativeMention;
						}
					}
				}
				return pronounCorefMap;
			}
		}

		//return map of index of CharacterOffsetBeginAnnotation to name of character.
		protected internal static IDictionary<int, string> MapBammanToCharacterMap(IDictionary<int, IList<CoreLabel>> BammanTokens, IDictionary<string, IList<Person>> characterMap)
		{
			IDictionary<int, string> indexToCharacterName = new Dictionary<int, string>();
			//first, link the
			foreach (int characterID in BammanTokens.Keys)
			{
				IList<CoreLabel> tokens = BammanTokens[characterID];
				ICounter<string> names = new ClassicCounter<string>();
				int prevEnd = -2;
				string prevName = string.Empty;
				foreach (CoreLabel token in tokens)
				{
					if (token.Tag().Equals("NNP"))
					{
						int beginIndex = token.BeginPosition();
						if (prevEnd + 1 == beginIndex)
						{
							//adjacent to last token
							prevName += " " + token.Word();
						}
						else
						{
							//not adjacent candidate: clear and then
							if (!prevName.Equals(string.Empty))
							{
								names.IncrementCount(prevName, 1);
							}
							prevName = token.Word();
							prevEnd = token.EndPosition();
						}
					}
					else
					{
						if (!prevName.Equals(string.Empty))
						{
							names.IncrementCount(prevName, 1);
						}
						prevName = string.Empty;
						prevEnd = -2;
					}
				}
				//System.out.println();
				bool flag = false;
				//exact match
				foreach (string name in Counters.ToSortedList(names))
				{
					if (characterMap.Keys.Contains(name))
					{
						indexToCharacterName[characterID] = name;
						flag = true;
						break;
					}
				}
				//not exact match: try partial match
				if (!flag)
				{
					foreach (string charName in characterMap.Keys)
					{
						foreach (string name_1 in Counters.ToSortedList(names))
						{
							if (charName.Contains(name_1))
							{
								indexToCharacterName[characterID] = charName;
								flag = true;
								System.Console.Out.WriteLine("contingency name found" + characterID);
								foreach (string n in Counters.ToSortedList(names))
								{
									System.Console.Out.Write(n + "|");
								}
								System.Console.Out.WriteLine();
								break;
							}
						}
						if (flag)
						{
							break;
						}
					}
					System.Console.Out.WriteLine();
				}
				if (!flag)
				{
					System.Console.Error.WriteLine("no name found :( " + characterID);
					foreach (string name_1 in Counters.ToSortedList(names))
					{
						System.Console.Error.Write(name_1 + "| ");
					}
					System.Console.Error.WriteLine();
				}
			}
			IDictionary<int, string> beginIndexToName = new Dictionary<int, string>();
			foreach (int charId in BammanTokens.Keys)
			{
				if (indexToCharacterName[charId] == null)
				{
					continue;
				}
				IList<CoreLabel> tokens = BammanTokens[charId];
				foreach (CoreLabel btoken in tokens)
				{
					if (btoken.Tag().Equals("PRP"))
					{
						beginIndexToName[btoken.BeginPosition()] = indexToCharacterName[charId];
					}
				}
			}
			return beginIndexToName;
		}

		//return true if one is contained in the other.
		public static bool RangeContains(Pair<int, int> r1, Pair<int, int> r2)
		{
			return ((r1.first <= r2.first && r1.second >= r2.first) || (r1.first <= r2.second && r1.second >= r2.second));
		}
	}
}
