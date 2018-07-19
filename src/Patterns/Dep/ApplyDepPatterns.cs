using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Function;
using Java.Util.Regex;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Dep
{
	/// <summary>Applying Dependency patterns to sentences.</summary>
	/// <author>sonalg</author>
	/// <version>11/1/14</version>
	public class ApplyDepPatterns<E> : ICallable<Pair<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>>>
		where E : Pattern
	{
		private string label;

		private IDictionary<SemgrexPattern, E> patterns;

		private IList<string> sentids;

		private bool removeStopWordsFromSelectedPhrases;

		private bool removePhrasesWithStopWords;

		private ConstantsAndVariables constVars;

		private IDictionary<string, DataInstance> sents;

		public ApplyDepPatterns(IDictionary<string, DataInstance> sents, IList<string> sentids, IDictionary<SemgrexPattern, E> patterns, string label, bool removeStopWordsFromSelectedPhrases, bool removePhrasesWithStopWords, ConstantsAndVariables cv
			)
		{
			matchingWordRestriction = new _IPredicate_183(this);
			// = null;
			this.sents = sents;
			this.patterns = patterns;
			this.sentids = sentids;
			this.label = label;
			this.removeStopWordsFromSelectedPhrases = removeStopWordsFromSelectedPhrases;
			this.removePhrasesWithStopWords = removePhrasesWithStopWords;
			this.constVars = cv;
		}

		/// <exception cref="System.Exception"/>
		public virtual Pair<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>> Call()
		{
			// CollectionValuedMap<String, Integer> tokensMatchedPattern = new
			// CollectionValuedMap<String, Integer>();
			TwoDimensionalCounter<CandidatePhrase, E> allFreq = new TwoDimensionalCounter<CandidatePhrase, E>();
			CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat = new CollectionValuedMap<E, Triple<string, int, int>>();
			foreach (string sentid in sentids)
			{
				DataInstance sent = sents[sentid];
				IList<CoreLabel> tokens = sent.GetTokens();
				foreach (KeyValuePair<SemgrexPattern, E> pEn in patterns)
				{
					if (pEn.Key == null)
					{
						throw new Exception("why is the pattern " + pEn + " null?");
					}
					SemanticGraph graph = ((DataInstanceDep)sent).GetGraph();
					//SemgrexMatcher m = pEn.getKey().matcher(graph);
					//TokenSequenceMatcher m = pEn.getKey().matcher(sent);
					//        //Setting this find type can save time in searching - greedy and reluctant quantifiers are not enforced
					//        m.setFindType(SequenceMatcher.FindType.FIND_ALL);
					//Higher branch values makes the faster but uses more memory
					//m.setBranchLimit(5);
					ICollection<ExtractedPhrase> matched = GetMatchedTokensIndex(graph, pEn.Key, sent, label);
					foreach (ExtractedPhrase match in matched)
					{
						int s = match.startIndex;
						int e = match.endIndex + 1;
						string phrase = string.Empty;
						string phraseLemma = string.Empty;
						bool useWordNotLabeled = false;
						bool doNotUse = false;
						//find if the neighboring words are labeled - if so - club them together
						if (constVars.clubNeighboringLabeledWords)
						{
							for (int i = s - 1; i >= 0; i--)
							{
								if (tokens[i].Get(constVars.GetAnswerClass()[label]).Equals(label) && (e - i + 1) <= PatternFactory.numWordsCompoundMapped[label])
								{
									s = i;
								}
								else
								{
									//System.out.println("for phrase " + match + " clubbing earlier word. new s is " + s);
									break;
								}
							}
							for (int i_1 = e; i_1 < tokens.Count; i_1++)
							{
								if (tokens[i_1].Get(constVars.GetAnswerClass()[label]).Equals(label) && (i_1 - s + 1) <= PatternFactory.numWordsCompoundMapped[label])
								{
									e = i_1;
								}
								else
								{
									//System.out.println("for phrase " + match + " clubbing next word. new e is " + e);
									break;
								}
							}
						}
						//to make sure we discard phrases with stopwords in between, but include the ones in which stop words were removed at the ends if removeStopWordsFromSelectedPhrases is true
						bool[] addedindices = new bool[e - s];
						// Arrays.fill(addedindices, false); // get for free on array initialization
						for (int i_2 = s; i_2 < e; i_2++)
						{
							CoreLabel l = tokens[i_2];
							l.Set(typeof(PatternsAnnotations.MatchedPattern), true);
							if (!l.ContainsKey(typeof(PatternsAnnotations.MatchedPatterns)) || l.Get(typeof(PatternsAnnotations.MatchedPatterns)) == null)
							{
								l.Set(typeof(PatternsAnnotations.MatchedPatterns), new HashSet<Pattern>());
							}
							Pattern pSur = pEn.Value;
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
						if (!doNotUse && useWordNotLabeled)
						{
							matchedTokensByPat.Add(pEn.Value, new Triple<string, int, int>(sentid, s, e - 1));
							if (useWordNotLabeled)
							{
								phrase = phrase.Trim();
								phraseLemma = phraseLemma.Trim();
								allFreq.IncrementCount(CandidatePhrase.CreateOrGet(phrase, phraseLemma, match.GetFeatures()), pEn.Value, 1.0);
							}
						}
					}
				}
			}
			return new Pair<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>>(allFreq, matchedTokensByPat);
		}

		private sealed class _IPredicate_183 : IPredicate<CoreLabel>
		{
			public _IPredicate_183(ApplyDepPatterns<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public bool Test(CoreLabel coreLabel)
			{
				return this._enclosing.MatchedRestriction(coreLabel, this._enclosing.label);
			}

			private readonly ApplyDepPatterns<E> _enclosing;
		}

		private IPredicate<CoreLabel> matchingWordRestriction;

		private ICollection<ExtractedPhrase> GetMatchedTokensIndex(SemanticGraph graph, SemgrexPattern pattern, DataInstance sent, string label)
		{
			//TODO: look at the ignoreCommonTags flag
			ExtractPhraseFromPattern extract = new ExtractPhraseFromPattern(false, PatternFactory.numWordsCompoundMapped[label]);
			ICollection<IntPair> outputIndices = new List<IntPair>();
			bool findSubTrees = true;
			IList<CoreLabel> tokensC = sent.GetTokens();
			//TODO: see if you can get rid of this (only used for matchedGraphs)
			IList<string> tokens = tokensC.Stream().Map(null).Collect(Collectors.ToList());
			IList<string> outputPhrases = new List<string>();
			IList<ExtractedPhrase> extractedPhrases = new List<ExtractedPhrase>();
			IFunction<Pair<IndexedWord, SemanticGraph>, ICounter<string>> extractFeatures = new _IFunction_206();
			//TODO: make features;
			extract.GetSemGrexPatternNodes(graph, tokens, outputPhrases, outputIndices, pattern, findSubTrees, extractedPhrases, constVars.matchLowerCaseContext, matchingWordRestriction);
			/*
			//TODO: probably a bad idea to add ALL ngrams
			Collection<ExtractedPhrase> outputIndicesMaxPhraseLen = new ArrayList<ExtractedPhrase>();
			for(IntPair o: outputIndices){
			int min = o.get(0);
			int max = o.get(1);
			
			for (int i = min; i <= max ; i++) {
			
			CoreLabel t = tokensC.get(i);
			String phrase = t.word();
			if(!matchedRestriction(t, label))
			continue;
			for (int ngramSize = 1; ngramSize < PatternFactory.numWordsCompound; ++ngramSize) {
			int j = i + ngramSize - 1;
			if(j > max)
			break;
			
			CoreLabel tokenj = tokensC.get(j);
			
			if(ngramSize > 1)
			phrase += " " + tokenj.word();
			
			if (matchedRestriction(tokenj, label)) {
			outputIndicesMaxPhraseLen.add(new ExtractedPhrase(i, j, phrase));
			//outputIndicesMaxPhraseLen.add(new IntPair(i, j));
			}
			}
			}
			}*/
			//System.out.println("extracted phrases are " + extractedPhrases + " and output indices are " + outputIndices);
			return extractedPhrases;
		}

		private sealed class _IFunction_206 : IFunction<Pair<IndexedWord, SemanticGraph>, ICounter<string>>
		{
			public _IFunction_206()
			{
			}

			public ICounter<string> Apply(Pair<IndexedWord, SemanticGraph> indexedWordSemanticGraphPair)
			{
				ICounter<string> feat = new ClassicCounter<string>();
				IndexedWord vertex = indexedWordSemanticGraphPair.First();
				SemanticGraph graph = indexedWordSemanticGraphPair.Second();
				IList<Pair<GrammaticalRelation, IndexedWord>> pt = graph.ParentPairs(vertex);
				foreach (Pair<GrammaticalRelation, IndexedWord> en in pt)
				{
					feat.IncrementCount("PARENTREL-" + en.First());
				}
				return feat;
			}
		}

		private bool MatchedRestriction(CoreLabel coreLabel, string label)
		{
			bool use = false;
			if (PatternFactory.useTargetNERRestriction)
			{
				foreach (string s in constVars.allowedNERsforLabels[label])
				{
					if (coreLabel.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Matches(s))
					{
						use = true;
						break;
					}
				}
			}
			else
			{
				//System.out.println("not matching NER");
				use = true;
			}
			if (use)
			{
				string tag = coreLabel.Tag();
				if (constVars.allowedTagsInitials != null && constVars.allowedTagsInitials.Contains(label))
				{
					foreach (string allowed in constVars.allowedTagsInitials[label])
					{
						if (tag.StartsWith(allowed))
						{
							use = true;
							break;
						}
						use = false;
					}
				}
			}
			if (constVars.debug >= 4)
			{
				if (use)
				{
					System.Console.Out.WriteLine(coreLabel.Word() + " matched restriction " + (PatternFactory.useTargetNERRestriction ? constVars.allowedNERsforLabels[label] : string.Empty) + "and" + PatternFactory.useTargetNERRestriction + " and " + (constVars
						.allowedTagsInitials != null ? constVars.allowedTagsInitials[label] : string.Empty));
				}
				else
				{
					System.Console.Out.WriteLine(coreLabel.Word() + " did not matched restrict " + (PatternFactory.useTargetNERRestriction ? constVars.allowedNERsforLabels[label] : string.Empty) + "and" + PatternFactory.useTargetNERRestriction + " and " + (constVars
						.allowedTagsInitials != null ? constVars.allowedTagsInitials[label] : string.Empty));
				}
			}
			return use;
		}

		private static bool ContainsStopWord(CoreLabel l, ICollection<string> commonEngWords, Pattern ignoreWordRegex)
		{
			// if(useWordResultCache.containsKey(l.word()))
			// return useWordResultCache.get(l.word());
			if ((commonEngWords != null && (commonEngWords.Contains(l.Lemma()) || commonEngWords.Contains(l.Word()))) || (ignoreWordRegex != null && ignoreWordRegex.Matcher(l.Lemma()).Matches()))
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
