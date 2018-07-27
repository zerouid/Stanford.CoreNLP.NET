using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Paragraphs;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.Training
{
	/// <summary>Created by mjfang on 12/1/16.</summary>
	public class SupervisedSieveTraining
	{
		private static Sieve sieve;

		public static readonly ICollection<string> punctuation = new HashSet<string>(Arrays.AsList(new string[] { ",", ".", "\"", "\n" }));

		public static readonly ICollection<string> punctuationForFeatures = new HashSet<string>(Arrays.AsList(new string[] { ",", ".", "!", "?" }));

		// use to access functions
		// Take in a training Annotated document:
		// convert document to dataset & featurize
		// train classifier
		// output model
		// report training F1/accuracy
		//TODO: in original iteration: maybe can combine these!?
		//given a sentence, return the begin token of the paragraph it's in
		private static int GetParagraphBeginToken(ICoreMap sentence, IList<ICoreMap> sentences)
		{
			int paragraphId = sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
			int paragraphBeginToken = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			for (int i = sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) - 1; i >= 0; i--)
			{
				ICoreMap currSentence = sentences[i];
				if (currSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == paragraphId)
				{
					paragraphBeginToken = currSentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				}
				else
				{
					break;
				}
			}
			return paragraphBeginToken;
		}

		private static int GetParagraphEndToken(ICoreMap sentence, IList<ICoreMap> sentences)
		{
			int quoteParagraphId = sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
			int paragraphEndToken = sentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1;
			for (int i = sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)); i < sentences.Count; i++)
			{
				ICoreMap currSentence = sentences[i];
				if (currSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == quoteParagraphId)
				{
					paragraphEndToken = currSentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1;
				}
				else
				{
					break;
				}
			}
			return paragraphEndToken;
		}

		private static IDictionary<int, IList<ICoreMap>> GetQuotesInParagraph(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IDictionary<int, IList<ICoreMap>> paragraphToQuotes = new Dictionary<int, IList<ICoreMap>>();
			foreach (ICoreMap quote in quotes)
			{
				ICoreMap sentence = sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))];
				paragraphToQuotes.PutIfAbsent(sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)), new List<ICoreMap>());
				paragraphToQuotes[sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation))].Add(quote);
			}
			return paragraphToQuotes;
		}

		//assumption: exclusion list is non-overlapping, ordered
		private static IList<Pair<int, int>> GetRangeExclusion(Pair<int, int> originalRange, IList<Pair<int, int>> exclusionList)
		{
			IList<Pair<int, int>> leftoverRanges = new List<Pair<int, int>>();
			Pair<int, int> currRange = originalRange;
			foreach (Pair<int, int> exRange in exclusionList)
			{
				Pair<int, int> leftRange = new Pair<int, int>(currRange.first, exRange.first - 1);
				if (leftRange.second - leftRange.first >= 0)
				{
					leftoverRanges.Add(leftRange);
				}
				if (currRange.second == exRange.second)
				{
					break;
				}
				else
				{
					currRange = new Pair<int, int>(exRange.second + 1, currRange.second);
				}
			}
			if (currRange.first < currRange.second)
			{
				leftoverRanges.Add(currRange);
			}
			return leftoverRanges;
		}

		public class FeaturesData
		{
			public GeneralDataset<string, string> dataset;

			public IDictionary<int, Pair<int, int>> mapQuoteToDataRange;

			public IDictionary<int, Sieve.MentionData> mapDatumToMention;

			public FeaturesData(IDictionary<int, Pair<int, int>> mapQuoteToDataRange, IDictionary<int, Sieve.MentionData> mapDatumToMention, GeneralDataset<string, string> dataset)
			{
				this.mapQuoteToDataRange = mapQuoteToDataRange;
				this.mapDatumToMention = mapDatumToMention;
				this.dataset = dataset;
			}
		}

		public class SieveData
		{
			internal Annotation doc;

			internal IDictionary<string, IList<Person>> characterMap;

			internal IDictionary<int, string> pronounCorefMap;

			internal ICollection<string> animacyList;

			public SieveData(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacyList)
			{
				this.doc = doc;
				this.characterMap = characterMap;
				this.pronounCorefMap = pronounCorefMap;
				this.animacyList = animacyList;
			}
		}

		//goldList null if not training
		public static SupervisedSieveTraining.FeaturesData Featurize(SupervisedSieveTraining.SieveData sd, IList<XMLToAnnotation.GoldQuoteInfo> goldList, bool isTraining)
		{
			Annotation doc = sd.doc;
			sieve = new Sieve(doc, sd.characterMap, sd.pronounCorefMap, sd.animacyList);
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IDictionary<int, IList<ICoreMap>> paragraphToQuotes = GetQuotesInParagraph(doc);
			GeneralDataset<string, string> dataset = new RVFDataset<string, string>();
			//necessary for 'ScoreBestMention'
			IDictionary<int, Pair<int, int>> mapQuoteToDataRange = new Dictionary<int, Pair<int, int>>();
			//maps quote to corresponding indices in the dataset
			IDictionary<int, Sieve.MentionData> mapDatumToMention = new Dictionary<int, Sieve.MentionData>();
			if (isTraining && goldList.Count != quotes.Count)
			{
				throw new Exception("Gold Quote List size doesn't match quote list size!");
			}
			for (int quoteIdx = 0; quoteIdx < quotes.Count; quoteIdx++)
			{
				int initialSize = dataset.Size();
				ICoreMap quote = quotes[quoteIdx];
				XMLToAnnotation.GoldQuoteInfo gold = null;
				if (isTraining)
				{
					gold = goldList[quoteIdx];
					if (gold.speaker == string.Empty)
					{
						continue;
					}
				}
				ICoreMap quoteFirstSentence = sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))];
				Pair<int, int> quoteRun = new Pair<int, int>(quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
				//      int quoteChapter = quoteFirstSentence.get(ChapterAnnotator.ChapterAnnotation.class);
				int quoteParagraphIdx = quoteFirstSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
				//add mentions before quote up to the previous paragraph
				int rightValue = quoteRun.first - 1;
				int leftValue = quoteRun.first - 1;
				//move left value to be the first token idx of the previous paragraph
				for (int sentIdx = quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation)); sentIdx >= 0; sentIdx--)
				{
					ICoreMap sentence = sentences[sentIdx];
					if (sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == quoteParagraphIdx)
					{
						continue;
					}
					if (sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == quoteParagraphIdx - 1)
					{
						//quoteParagraphIdx - 1 for this and prev
						leftValue = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					}
					else
					{
						break;
					}
				}
				IList<Sieve.MentionData> mentionsInPreviousParagraph = new List<Sieve.MentionData>();
				if (leftValue > -1 && rightValue > -1)
				{
					mentionsInPreviousParagraph = EliminateDuplicates(sieve.FindClosestMentionsInSpanBackward(new Pair<int, int>(leftValue, rightValue)));
				}
				//mentions in next paragraph
				leftValue = quoteRun.second + 1;
				rightValue = quoteRun.second + 1;
				for (int sentIdx_1 = quote.Get(typeof(CoreAnnotations.SentenceEndAnnotation)); sentIdx_1 < sentences.Count; sentIdx_1++)
				{
					ICoreMap sentence = sentences[sentIdx_1];
					//        if(sentence.get(CoreAnnotations.ParagraphIndexAnnotation.class) == quoteParagraphIdx) {
					//          continue;
					//        }
					if (sentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == quoteParagraphIdx)
					{
						//quoteParagraphIdx + 1
						rightValue = sentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1;
					}
					else
					{
						break;
					}
				}
				IList<Sieve.MentionData> mentionsInNextParagraph = new List<Sieve.MentionData>();
				if (leftValue < tokens.Count && rightValue < tokens.Count)
				{
					mentionsInNextParagraph = sieve.FindClosestMentionsInSpanForward(new Pair<int, int>(leftValue, rightValue));
				}
				IList<Sieve.MentionData> candidateMentions = new List<Sieve.MentionData>();
				Sharpen.Collections.AddAll(candidateMentions, mentionsInPreviousParagraph);
				Sharpen.Collections.AddAll(candidateMentions, mentionsInNextParagraph);
				//      System.out.println(candidateMentions.size());
				int rankedDistance = 1;
				int numBackwards = mentionsInPreviousParagraph.Count;
				foreach (Sieve.MentionData mention in candidateMentions)
				{
					IList<CoreLabel> mentionCandidateTokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation)).SubList(mention.begin, mention.end + 1);
					ICoreMap mentionCandidateSentence = sentences[mentionCandidateTokens[0].SentIndex()];
					//        if (mentionCandidateSentence.get(ChapterAnnotator.ChapterAnnotation.class) != quoteChapter) {
					//          continue;
					//        }
					ICounter<string> features = new ClassicCounter<string>();
					bool isLeft = true;
					int distance = quoteRun.first - mention.end;
					if (distance < 0)
					{
						isLeft = false;
						distance = mention.begin - quoteRun.second;
					}
					if (distance < 0)
					{
						continue;
					}
					//disregard mention-in-quote cases.
					features.SetCount("wordDistance", distance);
					IList<CoreLabel> betweenTokens;
					if (isLeft)
					{
						betweenTokens = tokens.SubList(mention.end + 1, quoteRun.first);
					}
					else
					{
						betweenTokens = tokens.SubList(quoteRun.second + 1, mention.begin);
					}
					//Punctuation in between
					foreach (CoreLabel token in betweenTokens)
					{
						if (punctuation.Contains(token.Word()))
						{
							features.SetCount("punctuationPresence:" + token.Word(), 1);
						}
					}
					// number of mentions away
					features.SetCount("rankedDistance", rankedDistance);
					rankedDistance++;
					if (rankedDistance == numBackwards)
					{
						//reset for the forward
						rankedDistance = 1;
					}
					//        int quoteParagraphIdx = quoteFirstSentence.get(CoreAnnotations.ParagraphIndexAnnotation.class);
					//third distance: # of paragraphs away
					int mentionParagraphIdx = -1;
					ICoreMap sentenceInMentionParagraph = null;
					int quoteParagraphBeginToken = GetParagraphBeginToken(quoteFirstSentence, sentences);
					int quoteParagraphEndToken = GetParagraphEndToken(quoteFirstSentence, sentences);
					if (isLeft)
					{
						if (quoteParagraphBeginToken <= mention.begin && mention.end <= quoteParagraphEndToken)
						{
							features.SetCount("leftParagraphDistance", 0);
							mentionParagraphIdx = quoteParagraphIdx;
							sentenceInMentionParagraph = quoteFirstSentence;
						}
						else
						{
							int paragraphDistance = 1;
							int currParagraphIdx = quoteParagraphIdx - paragraphDistance;
							ICoreMap currSentence = quoteFirstSentence;
							int currSentenceIdx = currSentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
							while (currParagraphIdx >= 0)
							{
								//              Paragraph prevParagraph = paragraphs.get(prevParagraphIndex);
								//extract begin and end tokens of
								while (currSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) != currParagraphIdx)
								{
									currSentenceIdx--;
									currSentence = sentences[currSentenceIdx];
								}
								int prevParagraphBegin = GetParagraphBeginToken(currSentence, sentences);
								int prevParagraphEnd = GetParagraphEndToken(currSentence, sentences);
								if (prevParagraphBegin <= mention.begin && mention.end <= prevParagraphEnd)
								{
									mentionParagraphIdx = currParagraphIdx;
									sentenceInMentionParagraph = currSentence;
									features.SetCount("leftParagraphDistance", paragraphDistance);
									if (paragraphDistance % 2 == 0)
									{
										features.SetCount("leftParagraphDistanceEven", 1);
									}
									break;
								}
								paragraphDistance++;
								currParagraphIdx--;
							}
						}
					}
					else
					{
						//right
						if (quoteParagraphBeginToken <= mention.begin && mention.end <= quoteParagraphEndToken)
						{
							features.SetCount("rightParagraphDistance", 0);
							sentenceInMentionParagraph = quoteFirstSentence;
							mentionParagraphIdx = quoteParagraphIdx;
						}
						else
						{
							int paragraphDistance = 1;
							int nextParagraphIndex = quoteParagraphIdx + paragraphDistance;
							ICoreMap currSentence = quoteFirstSentence;
							int currSentenceIdx = currSentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
							while (currSentenceIdx < sentences.Count)
							{
								while (currSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) != nextParagraphIndex)
								{
									currSentenceIdx++;
									currSentence = sentences[currSentenceIdx];
								}
								int nextParagraphBegin = GetParagraphBeginToken(currSentence, sentences);
								int nextParagraphEnd = GetParagraphEndToken(currSentence, sentences);
								if (nextParagraphBegin <= mention.begin && mention.end <= nextParagraphEnd)
								{
									sentenceInMentionParagraph = currSentence;
									features.SetCount("rightParagraphDistance", paragraphDistance);
									break;
								}
								paragraphDistance++;
								nextParagraphIndex++;
							}
						}
					}
					//2. mention features
					if (sentenceInMentionParagraph != null)
					{
						int mentionParagraphBegin = GetParagraphBeginToken(sentenceInMentionParagraph, sentences);
						int mentionParagraphEnd = GetParagraphEndToken(sentenceInMentionParagraph, sentences);
						if (!(mentionParagraphBegin == quoteParagraphBeginToken && mentionParagraphEnd == quoteParagraphEndToken))
						{
							IList<ICoreMap> quotesInMentionParagraph = paragraphToQuotes.GetOrDefault(mentionParagraphIdx, new List<ICoreMap>());
							Pair<List<string>, List<Pair<int, int>>> namesInMentionParagraph = sieve.ScanForNames(new Pair<int, int>(mentionParagraphBegin, mentionParagraphEnd));
							features.SetCount("quotesInMentionParagraph", quotesInMentionParagraph.Count);
							features.SetCount("wordsInMentionParagraph", mentionParagraphEnd - mentionParagraphBegin + 1);
							features.SetCount("namesInMentionParagraph", namesInMentionParagraph.first.Count);
							//mention ordering in paragraph it is in
							for (int i = 0; i < namesInMentionParagraph.second.Count; i++)
							{
								if (ExtractQuotesUtil.RangeContains(new Pair<int, int>(mention.begin, mention.end), namesInMentionParagraph.second[i]))
								{
									features.SetCount("orderInParagraph", i);
								}
							}
							//if mention paragraph is all one quote
							if (quotesInMentionParagraph.Count == 1)
							{
								ICoreMap qInMentionParagraph = quotesInMentionParagraph[0];
								if (qInMentionParagraph.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) == mentionParagraphBegin && qInMentionParagraph.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1 == mentionParagraphEnd)
								{
									features.SetCount("mentionParagraphIsInConversation", 1);
								}
								else
								{
									features.SetCount("mentionParagraphIsInConversation", -1);
								}
							}
							foreach (ICoreMap quoteIMP in quotesInMentionParagraph)
							{
								if (ExtractQuotesUtil.RangeContains(new Pair<int, int>(quoteIMP.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quoteIMP.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - 1), new Pair<int, int>(mention.begin, mention.end)))
								{
									features.SetCount("mentionInQuote", 1);
								}
							}
							if (features.GetCount("mentionInQuote") != 1)
							{
								features.SetCount("mentionNotInQuote", 1);
							}
						}
					}
					// nearby word syntax types...make sure to check if there are previous or next words
					// or there will be an array index crash
					if (mention.begin > 0)
					{
						CoreLabel prevWord = tokens[mention.begin - 1];
						features.SetCount("prevWordType:" + prevWord.Tag(), 1);
						if (punctuationForFeatures.Contains(prevWord.Lemma()))
						{
							features.SetCount("prevWordPunct:" + prevWord.Lemma(), 1);
						}
					}
					if (mention.end + 1 < tokens.Count)
					{
						CoreLabel nextWord = tokens[mention.end + 1];
						features.SetCount("nextWordType:" + nextWord.Tag(), 1);
						if (punctuationForFeatures.Contains(nextWord.Lemma()))
						{
							features.SetCount("nextWordPunct:" + nextWord.Lemma(), 1);
						}
					}
					//                    features.setCount("prevAndNext:" + prevWord.tag()+ ";" + nextWord.tag(), 1);
					//quote paragraph features
					IList<ICoreMap> quotesInQuoteParagraph = paragraphToQuotes[quoteParagraphIdx];
					features.SetCount("QuotesInQuoteParagraph", quotesInQuoteParagraph.Count);
					features.SetCount("WordsInQuoteParagraph", quoteParagraphEndToken - quoteParagraphBeginToken + 1);
					features.SetCount("NamesInQuoteParagraph", sieve.ScanForNames(new Pair<int, int>(quoteParagraphBeginToken, quoteParagraphEndToken)).first.Count);
					//quote features
					features.SetCount("quoteLength", quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) + 1);
					for (int i_1 = 0; i_1 < quotesInQuoteParagraph.Count; i_1++)
					{
						if (quotesInQuoteParagraph[i_1].Equals(quote))
						{
							features.SetCount("quotePosition", i_1 + 1);
						}
					}
					if (features.GetCount("quotePosition") == 0)
					{
						throw new Exception("Check this (equality not working)");
					}
					Pair<List<string>, List<Pair<int, int>>> namesData = sieve.ScanForNames(quoteRun);
					foreach (string name in namesData.first)
					{
						features.SetCount("charactersInQuote:" + sd.characterMap[name][0].name, 1);
					}
					//if quote encompasses entire paragraph
					if (quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) == quoteParagraphBeginToken && quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)) == quoteParagraphEndToken)
					{
						features.SetCount("isImplicitSpeaker", 1);
					}
					else
					{
						features.SetCount("isImplicitSpeaker", -1);
					}
					//Vocative detection
					if (mention.type.Equals("name"))
					{
						IList<Person> pList = sd.characterMap[sieve.TokenRangeToString(new Pair<int, int>(mention.begin, mention.end))];
						Person p = null;
						if (pList != null)
						{
							p = pList[0];
						}
						else
						{
							Pair<List<string>, List<Pair<int, int>>> scanForNamesResultPair = sieve.ScanForNames(new Pair<int, int>(mention.begin, mention.end));
							if (scanForNamesResultPair.first.Count != 0)
							{
								string scanForNamesResultString = scanForNamesResultPair.first[0];
								if (scanForNamesResultString != null && sd.characterMap.Contains(scanForNamesResultString))
								{
									p = sd.characterMap[scanForNamesResultString][0];
								}
							}
						}
						if (p != null)
						{
							foreach (string name_1 in namesData.first)
							{
								if (p.aliases.Contains(name_1))
								{
									features.SetCount("nameInQuote", 1);
								}
							}
							if (quoteParagraphIdx > 0)
							{
								//            Paragraph prevParagraph = paragraphs.get(ex.paragraph_idx - 1);
								IList<ICoreMap> quotesInPrevParagraph = paragraphToQuotes.GetOrDefault(quoteParagraphIdx - 1, new List<ICoreMap>());
								IList<Pair<int, int>> exclusionList = new List<Pair<int, int>>();
								foreach (ICoreMap quoteIPP in quotesInPrevParagraph)
								{
									Pair<int, int> quoteRange = new Pair<int, int>(quoteIPP.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quoteIPP.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
									exclusionList.Add(quoteRange);
									foreach (string name_2 in sieve.ScanForNames(quoteRange).first)
									{
										if (p.aliases.Contains(name_2))
										{
											features.SetCount("nameInPrevParagraphQuote", 1);
										}
									}
								}
								int sentenceIdx = quoteFirstSentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
								ICoreMap sentenceInPrevParagraph = null;
								for (int i = sentenceIdx - 1; i_1 >= 0; i_1--)
								{
									ICoreMap currSentence = sentences[i_1];
									if (currSentence.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)) == quoteParagraphIdx - 1)
									{
										sentenceInPrevParagraph = currSentence;
										break;
									}
								}
								int prevParagraphBegin = GetParagraphBeginToken(sentenceInPrevParagraph, sentences);
								int prevParagraphEnd = GetParagraphEndToken(sentenceInPrevParagraph, sentences);
								IList<Pair<int, int>> prevParagraphNonQuoteRuns = GetRangeExclusion(new Pair<int, int>(prevParagraphBegin, prevParagraphEnd), exclusionList);
								foreach (Pair<int, int> nonQuoteRange in prevParagraphNonQuoteRuns)
								{
									foreach (string name_2 in sieve.ScanForNames(nonQuoteRange).first)
									{
										if (p.aliases.Contains(name_2))
										{
											features.SetCount("nameInPrevParagraphNonQuote", 1);
										}
									}
								}
							}
						}
					}
					if (isTraining)
					{
						if (QuoteAttributionUtils.RangeContains(new Pair<int, int>(gold.mentionStartTokenIndex, gold.mentionEndTokenIndex), new Pair<int, int>(mention.begin, mention.end)))
						{
							RVFDatum<string, string> datum = new RVFDatum<string, string>(features, "isMention");
							datum.SetID(int.ToString(dataset.Size()));
							mapDatumToMention[dataset.Size()] = mention;
							dataset.Add(datum);
						}
						else
						{
							RVFDatum<string, string> datum = new RVFDatum<string, string>(features, "isNotMention");
							datum.SetID(int.ToString(dataset.Size()));
							dataset.Add(datum);
							mapDatumToMention[dataset.Size()] = mention;
						}
					}
					else
					{
						RVFDatum<string, string> datum = new RVFDatum<string, string>(features, "none");
						datum.SetID(int.ToString(dataset.Size()));
						mapDatumToMention[dataset.Size()] = mention;
						dataset.Add(datum);
					}
				}
				mapQuoteToDataRange[quoteIdx] = new Pair<int, int>(initialSize, dataset.Size() - 1);
			}
			return new SupervisedSieveTraining.FeaturesData(mapQuoteToDataRange, mapDatumToMention, dataset);
		}

		//TODO: potential bug in previous iteration: not implementing order reversal in eliminateDuplicates
		private static IList<Sieve.MentionData> EliminateDuplicates(IList<Sieve.MentionData> mentionCandidates)
		{
			IList<Sieve.MentionData> newList = new List<Sieve.MentionData>();
			ICollection<string> seenText = new HashSet<string>();
			for (int i = 0; i < mentionCandidates.Count; i++)
			{
				Sieve.MentionData mentionCandidate = mentionCandidates[i];
				string text = mentionCandidate.text;
				if (!seenText.Contains(text) || mentionCandidate.type.Equals("Pronoun"))
				{
					newList.Add(mentionCandidate);
				}
				seenText.Add(text);
			}
			return newList;
		}

		public static void OutputModel(string fileName, IClassifier<string, string> clf)
		{
			FileOutputStream fo = null;
			try
			{
				fo = new FileOutputStream(fileName);
				ObjectOutputStream so = new ObjectOutputStream(fo);
				so.WriteObject(clf);
				so.Flush();
				so.Close();
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public static void Train(XMLToAnnotation.Data data, Properties props)
		{
			IDictionary<string, IList<Person>> characterMap = QuoteAttributionUtils.ReadPersonMap(props.GetProperty("charactersPath"));
			IDictionary<int, string> pronounCorefMap = QuoteAttributionUtils.SetupCoref(props.GetProperty("booknlpCoref"), characterMap, data.doc);
			ICollection<string> animacyList = QuoteAttributionUtils.ReadAnimacyList(QuoteAttributionAnnotator.AnimacyWordList);
			SupervisedSieveTraining.FeaturesData fd = Featurize(new SupervisedSieveTraining.SieveData(data.doc, characterMap, pronounCorefMap, animacyList), data.goldList, true);
			ExtractQuotesClassifier quotesClassifier = new ExtractQuotesClassifier(fd.dataset);
			OutputModel(props.GetProperty("modelPath"), quotesClassifier.GetClassifier());
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			string home = "/home/mjfang/action_grammars/";
			// make the first argument one for a base directory
			string specificFile = "1PPDevUncollapsed.props";
			if (args.Length >= 1)
			{
				home = args[0];
			}
			if (args.Length >= 2)
			{
				specificFile = args[1];
			}
			System.Console.Out.WriteLine("Base directory: " + home);
			Properties props = StringUtils.PropFileToProperties(home + "ExtractQuotesXMLScripts/" + specificFile);
			XMLToAnnotation.Data data = XMLToAnnotation.ReadXMLFormat(props.GetProperty("file"));
			Properties propsPara = new Properties();
			propsPara.SetProperty("paragraphBreak", "one");
			ParagraphAnnotator pa = new ParagraphAnnotator(propsPara, false);
			pa.Annotate(data.doc);
			Properties annotatorProps = new Properties();
			annotatorProps.SetProperty("charactersPath", props.GetProperty("charactersPath"));
			//"characterList.txt"
			annotatorProps.SetProperty("booknlpCoref", props.GetProperty("booknlpCoref"));
			annotatorProps.SetProperty("modelPath", props.GetProperty("modelPath"));
			//"model.ser");
			QuoteAttributionAnnotator qaa = new QuoteAttributionAnnotator(annotatorProps);
			qaa.Annotate(data.doc);
			ChapterAnnotator ca = new ChapterAnnotator();
			ca.Annotate(data.doc);
			Train(data, annotatorProps);
		}
	}
}
