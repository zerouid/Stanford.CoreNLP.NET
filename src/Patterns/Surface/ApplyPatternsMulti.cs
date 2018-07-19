using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <author>Sonal Gupta</author>
	public class ApplyPatternsMulti<E> : ICallable<Pair<TwoDimensionalCounter<Pair<string, string>, E>, CollectionValuedMap<E, Triple<string, int, int>>>>
		where E : Pattern
	{
		private readonly string label;

		private readonly IDictionary<TokenSequencePattern, E> patterns;

		private readonly IList<string> sentids;

		private readonly bool removeStopWordsFromSelectedPhrases;

		private readonly bool removePhrasesWithStopWords;

		private readonly ConstantsAndVariables constVars;

		private readonly MultiPatternMatcher<ICoreMap> multiPatternMatcher;

		private readonly IDictionary<string, DataInstance> sents;

		public ApplyPatternsMulti(IDictionary<string, DataInstance> sents, IList<string> sentids, IDictionary<TokenSequencePattern, E> patterns, string label, bool removeStopWordsFromSelectedPhrases, bool removePhrasesWithStopWords, ConstantsAndVariables
			 cv)
		{
			//Set<String> ignoreWords;
			this.sents = sents;
			this.patterns = patterns;
			multiPatternMatcher = TokenSequencePattern.GetMultiPatternMatcher(patterns.Keys);
			this.sentids = sentids;
			this.label = label;
			this.removeStopWordsFromSelectedPhrases = removeStopWordsFromSelectedPhrases;
			this.removePhrasesWithStopWords = removePhrasesWithStopWords;
			this.constVars = cv;
		}

		/// <exception cref="System.Exception"/>
		public virtual Pair<TwoDimensionalCounter<Pair<string, string>, E>, CollectionValuedMap<E, Triple<string, int, int>>> Call()
		{
			//CollectionValuedMap<String, Integer> tokensMatchedPattern = new CollectionValuedMap<String, Integer>();
			CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat = new CollectionValuedMap<E, Triple<string, int, int>>();
			TwoDimensionalCounter<Pair<string, string>, E> allFreq = new TwoDimensionalCounter<Pair<string, string>, E>();
			foreach (string sentid in sentids)
			{
				IList<CoreLabel> sent = sents[sentid].GetTokens();
				//FIND_ALL is faster than FIND_NONOVERLAP
				IEnumerable<ISequenceMatchResult<ICoreMap>> matched = multiPatternMatcher.Find(sent, SequenceMatcher.FindType.FindAll);
				foreach (ISequenceMatchResult<ICoreMap> m in matched)
				{
					int s = m.Start("$term");
					int e = m.End("$term");
					E matchedPat = patterns[m.Pattern()];
					matchedTokensByPat.Add(matchedPat, new Triple<string, int, int>(sentid, s, e));
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
					// Arrays.fill(addedindices, false); // unneeded as done on initialization
					for (int i_2 = s; i_2 < e; i_2++)
					{
						CoreLabel l = sent[i_2];
						l.Set(typeof(PatternsAnnotations.MatchedPattern), true);
						if (!l.ContainsKey(typeof(PatternsAnnotations.MatchedPatterns)))
						{
							l.Set(typeof(PatternsAnnotations.MatchedPatterns), new HashSet<Pattern>());
						}
						l.Get(typeof(PatternsAnnotations.MatchedPatterns)).Add(matchedPat);
						// if (restrictToMatched) {
						// tokensMatchedPattern.add(sentid, i);
						// }
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
								if (label == null || l.Get(constVars.GetAnswerClass()[label]) == null || !l.Get(constVars.GetAnswerClass()[label]).Equals(label.ToString()))
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
					if (!doNotUse && useWordNotLabeled)
					{
						phrase = phrase.Trim();
						phraseLemma = phraseLemma.Trim();
						allFreq.IncrementCount(new Pair<string, string>(phrase, phraseLemma), matchedPat, 1.0);
					}
				}
			}
			//      for (SurfacePattern pat : patterns.keySet()) {
			//        String patternStr = pat.toString();
			//
			//        TokenSequencePattern p = TokenSequencePattern.compile(constVars.env.get(label), patternStr);
			//        if (pat == null || p == null)
			//          throw new RuntimeException("why is the pattern " + pat + " null?");
			//
			//        TokenSequenceMatcher m = p.getMatcher(sent);
			//        while (m.find()) {
			//
			//          int s = m.start("$term");
			//          int e = m.end("$term");
			//
			//          String phrase = "";
			//          String phraseLemma = "";
			//          boolean useWordNotLabeled = false;
			//          boolean doNotUse = false;
			//          for (int i = s; i < e; i++) {
			//            CoreLabel l = sent.get(i);
			//            l.set(PatternsAnnotations.MatchedPattern.class, true);
			//            if (restrictToMatched) {
			//              tokensMatchedPattern.add(sentid, i);
			//            }
			//            for (Entry<Class, Object> ig : constVars.ignoreWordswithClassesDuringSelection.get(label).entrySet()) {
			//              if (l.containsKey(ig.getKey()) && l.get(ig.getKey()).equals(ig.getValue())) {
			//                doNotUse = true;
			//              }
			//            }
			//            boolean containsStop = containsStopWord(l, constVars.getCommonEngWords(), constVars.ignoreWordRegex, ignoreWords);
			//            if (removePhrasesWithStopWords && containsStop) {
			//              doNotUse = true;
			//            } else {
			//              if (!containsStop || !removeStopWordsFromSelectedPhrases) {
			//
			//                if (label == null || l.get(constVars.answerClass.get(label)) == null || !l.get(constVars.answerClass.get(label)).equals(label.toString())) {
			//                  useWordNotLabeled = true;
			//                }
			//                phrase += " " + l.word();
			//                phraseLemma += " " + l.lemma();
			//
			//              }
			//            }
			//          }
			//          if (!doNotUse && useWordNotLabeled) {
			//            phrase = phrase.trim();
			//            phraseLemma = phraseLemma.trim();
			//            allFreq.incrementCount(new Pair<String, String>(phrase, phraseLemma), pat, 1.0);
			//          }
			//        }
			//      }
			return new Pair<TwoDimensionalCounter<Pair<string, string>, E>, CollectionValuedMap<E, Triple<string, int, int>>>(allFreq, matchedTokensByPat);
		}

		private static bool ContainsStopWord(CoreLabel l, ICollection<string> commonEngWords, Pattern ignoreWordRegex)
		{
			// if(useWordResultCache.containsKey(l.word()))
			// return useWordResultCache.get(l.word());
			if ((commonEngWords.Contains(l.Lemma()) || commonEngWords.Contains(l.Word())) || (ignoreWordRegex != null && ignoreWordRegex.Matcher(l.Lemma()).Matches()))
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
