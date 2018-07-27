using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Applying SurfacePattern to sentences.</summary>
	/// <?/>
	/// <author>Sonal Gupta</author>
	public class ApplyPatterns<E> : ICallable<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.Surface.ApplyPatterns));

		private readonly string label;

		private readonly IDictionary<TokenSequencePattern, E> patterns;

		private readonly IList<string> sentids;

		private readonly bool removeStopWordsFromSelectedPhrases;

		private readonly bool removePhrasesWithStopWords;

		private readonly ConstantsAndVariables constVars;

		private readonly IDictionary<string, DataInstance> sents;

		public ApplyPatterns(IDictionary<string, DataInstance> sents, IList<string> sentids, IDictionary<TokenSequencePattern, E> patterns, string label, bool removeStopWordsFromSelectedPhrases, bool removePhrasesWithStopWords, ConstantsAndVariables
			 cv)
		{
			this.sents = sents;
			this.patterns = patterns;
			this.sentids = sentids;
			this.label = label;
			this.removeStopWordsFromSelectedPhrases = removeStopWordsFromSelectedPhrases;
			this.removePhrasesWithStopWords = removePhrasesWithStopWords;
			this.constVars = cv;
		}

		/// <exception cref="System.Exception"/>
		public virtual Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>> Call()
		{
			// CollectionValuedMap<String, Integer> tokensMatchedPattern = new
			// CollectionValuedMap<String, Integer>();
			try
			{
				ICollection<CandidatePhrase> alreadyLabeledPhrases = new HashSet<CandidatePhrase>();
				TwoDimensionalCounter<CandidatePhrase, E> allFreq = new TwoDimensionalCounter<CandidatePhrase, E>();
				CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat = new CollectionValuedMap<E, Triple<string, int, int>>();
				foreach (string sentid in sentids)
				{
					IList<CoreLabel> sent = sents[sentid].GetTokens();
					foreach (KeyValuePair<TokenSequencePattern, E> pEn in patterns)
					{
						if (pEn.Key == null)
						{
							throw new Exception("why is the pattern " + pEn + " null?");
						}
						TokenSequenceMatcher m = ((TokenSequenceMatcher)pEn.Key.GetMatcher(sent));
						//        //Setting this find type can save time in searching - greedy and reluctant quantifiers are not enforced
						//        m.setFindType(SequenceMatcher.FindType.FIND_ALL);
						//Higher branch values makes the faster but uses more memory
						m.SetBranchLimit(5);
						while (m.Find())
						{
							int s = m.Start("$term");
							int e = m.End("$term");
							System.Diagnostics.Debug.Assert(e - s <= PatternFactory.numWordsCompoundMapped[label], "How come the pattern " + pEn.Key + " is extracting phrases longer than numWordsCompound of " + PatternFactory.numWordsCompoundMapped[label] + " for label "
								 + label);
							string phrase = string.Empty;
							string phraseLemma = string.Empty;
							bool useWordNotLabeled = false;
							bool doNotUse = false;
							//find if the neighboring words are labeled - if so - club them together
							if (constVars.clubNeighboringLabeledWords)
							{
								for (int i = s - 1; i >= 0; i--)
								{
									if (!sent[i].Get(constVars.GetAnswerClass()[label]).Equals(label))
									{
										s = i + 1;
										break;
									}
								}
								for (int i_1 = e; i_1 < sent.Count; i_1++)
								{
									if (!sent[i_1].Get(constVars.GetAnswerClass()[label]).Equals(label))
									{
										e = i_1;
										break;
									}
								}
							}
							//to make sure we discard phrases with stopwords in between, but include the ones in which stop words were removed at the ends if removeStopWordsFromSelectedPhrases is true
							bool[] addedindices = new bool[e - s];
							// Arrays.fill(addedindices, false); // not needed as initialized false
							for (int i_2 = s; i_2 < e; i_2++)
							{
								CoreLabel l = sent[i_2];
								l.Set(typeof(PatternsAnnotations.MatchedPattern), true);
								if (!l.ContainsKey(typeof(PatternsAnnotations.MatchedPatterns)) || l.Get(typeof(PatternsAnnotations.MatchedPatterns)) == null)
								{
									l.Set(typeof(PatternsAnnotations.MatchedPatterns), new HashSet<Pattern>());
								}
								SurfacePattern pSur = (SurfacePattern)pEn.Value;
								System.Diagnostics.Debug.Assert(pSur != null, "Why is " + pEn.Value + " not present in the index?!");
								System.Diagnostics.Debug.Assert(l.Get(typeof(PatternsAnnotations.MatchedPatterns)) != null, "How come MatchedPatterns class is null for the token. The classes in the key set are " + l.KeySet());
								l.Get(typeof(PatternsAnnotations.MatchedPatterns)).Add(pSur);
								foreach (KeyValuePair<Type, object> ig in constVars.GetIgnoreWordswithClassesDuringSelection()[label])
								{
									if (l.ContainsKey(ig.Key) && l.Get(ig.Key).Equals(ig.Value))
									{
										doNotUse = true;
									}
								}
								bool containsStop = ContainsStopWord(l, constVars.GetCommonEngWords(), PatternFactory.ignoreWordRegex);
								if (removePhrasesWithStopWords && containsStop)
								{
									doNotUse = true;
								}
								else
								{
									if (!containsStop || !removeStopWordsFromSelectedPhrases)
									{
										if (label == null || l.Get(constVars.GetAnswerClass()[label]) == null || !l.Get(constVars.GetAnswerClass()[label]).Equals(label))
										{
											useWordNotLabeled = true;
										}
										phrase += " " + l.Word();
										phraseLemma += " " + l.Lemma();
										addedindices[i_2 - s] = true;
									}
								}
							}
							for (int i_3 = 0; i_3 < addedindices.Length; i_3++)
							{
								if (i_3 > 0 && i_3 < addedindices.Length - 1 && addedindices[i_3 - 1] == true && addedindices[i_3] == false && addedindices[i_3 + 1] == true)
								{
									doNotUse = true;
									break;
								}
							}
							if (!doNotUse)
							{
								matchedTokensByPat.Add(pEn.Value, new Triple<string, int, int>(sentid, s, e - 1));
								phrase = phrase.Trim();
								if (!phrase.IsEmpty())
								{
									phraseLemma = phraseLemma.Trim();
									CandidatePhrase candPhrase = CandidatePhrase.CreateOrGet(phrase, phraseLemma);
									allFreq.IncrementCount(candPhrase, pEn.Value, 1.0);
									if (!useWordNotLabeled)
									{
										alreadyLabeledPhrases.Add(candPhrase);
									}
								}
							}
						}
					}
				}
				return new Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>(allFreq, matchedTokensByPat, alreadyLabeledPhrases);
			}
			catch (Exception e)
			{
				logger.Error(e);
				throw;
			}
		}

		private static bool LemmaExists(CoreLabel l)
		{
			return l.Lemma() != null && !l.Lemma().IsEmpty();
		}

		private static bool ContainsStopWord(CoreLabel l, ICollection<string> commonEngWords, Pattern ignoreWordRegex)
		{
			// if(useWordResultCache.containsKey(l.word()))
			// return useWordResultCache.get(l.word());
			if ((commonEngWords != null && ((LemmaExists(l) && commonEngWords.Contains(l.Lemma())) || commonEngWords.Contains(l.Word()))) || (ignoreWordRegex != null && ((LemmaExists(l) && ignoreWordRegex.Matcher(l.Lemma()).Matches()) || ignoreWordRegex
				.Matcher(l.Word()).Matches())))
			{
				//|| (ignoreWords !=null && (ignoreWords.contains(l.lemma()) || ignoreWords.contains(l.word())))) {
				// useWordResultCache.putIfAbsent(l.word(), false);
				return true;
			}
			//
			// if (l.word().length() >= minLen4Fuzzy) {
			// try {
			// String matchedFuzzy = NoisyLabelSentences.containsFuzzy(commonEngWords,
			// l.word(), minLen4Fuzzy);
			// if (matchedFuzzy != null) {
			// synchronized (commonEngWords) {
			// commonEngWords.add(l.word());
			// System.out.println("word is " + l.word() + " and matched fuzzy with " +
			// matchedFuzzy);
			// }
			// useWordResultCache.putIfAbsent(l.word(), false);
			// return false;
			// }
			// } catch (Exception e) {
			// e.printStackTrace();
			// System.out.println("Exception " + " while fuzzy matching " + l.word());
			// }
			// }
			// useWordResultCache.putIfAbsent(l.word(), true);
			return false;
		}
	}
}
