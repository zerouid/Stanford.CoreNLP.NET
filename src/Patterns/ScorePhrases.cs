using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Javax.Json;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	public class ScorePhrases<E>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.ScorePhrases));

		private IDictionary<string, bool> writtenInJustification = new Dictionary<string, bool>();

		internal ConstantsAndVariables constVars = null;

		internal Type phraseScorerClass = typeof(ScorePhrasesAverageFeatures);

		internal PhraseScorer phraseScorer = null;

		public ScorePhrases(Properties props, ConstantsAndVariables cv)
		{
			ArgumentParser.FillOptions(this, props);
			this.constVars = cv;
			try
			{
				phraseScorer = phraseScorerClass.GetConstructor(typeof(ConstantsAndVariables)).NewInstance(constVars);
			}
			catch (ReflectiveOperationException e)
			{
				throw new Exception(e);
			}
			ArgumentParser.FillOptions(phraseScorer, props);
		}

		public virtual ICounter<CandidatePhrase> ChooseTopWords(ICounter<CandidatePhrase> newdt, TwoDimensionalCounter<CandidatePhrase, E> terms, ICounter<CandidatePhrase> useThresholdNumPatternsForTheseWords, ICollection<CandidatePhrase> ignoreWords
			, double thresholdWordExtract)
		{
			IEnumerator<CandidatePhrase> termIter = Counters.ToPriorityQueue(newdt).GetEnumerator();
			ICounter<CandidatePhrase> finalwords = new ClassicCounter<CandidatePhrase>();
			while (termIter.MoveNext())
			{
				if (finalwords.Size() >= constVars.numWordsToAdd)
				{
					break;
				}
				CandidatePhrase w = termIter.Current;
				if (newdt.GetCount(w) < thresholdWordExtract)
				{
					Redwood.Log(ConstantsAndVariables.extremedebug, "not adding word " + w + " and any later words because the score " + newdt.GetCount(w) + " is less than the threshold of  " + thresholdWordExtract);
					break;
				}
				System.Diagnostics.Debug.Assert((newdt.GetCount(w) != double.PositiveInfinity));
				if (useThresholdNumPatternsForTheseWords.ContainsKey(w) && NumNonRedundantPatterns(terms, w) < constVars.thresholdNumPatternsApplied)
				{
					Redwood.Log("extremePatDebug", "Not adding " + w + " because the number of non redundant patterns are below threshold of " + constVars.thresholdNumPatternsApplied + ":" + terms.GetCounter(w).KeySet());
					continue;
				}
				CandidatePhrase matchedFuzzy = null;
				if (constVars.minLen4FuzzyForPattern > 0 && ignoreWords != null)
				{
					matchedFuzzy = ConstantsAndVariables.ContainsFuzzy(ignoreWords, w, constVars.minLen4FuzzyForPattern);
				}
				if (matchedFuzzy == null)
				{
					Redwood.Log("extremePatDebug", "adding word " + w);
					finalwords.SetCount(w, newdt.GetCount(w));
				}
				else
				{
					Redwood.Log("extremePatDebug", "not adding " + w + " because it matched " + matchedFuzzy + " in common English word");
					ignoreWords.Add(w);
				}
			}
			string nextTen = string.Empty;
			int n = 0;
			while (termIter.MoveNext())
			{
				n++;
				if (n > 10)
				{
					break;
				}
				CandidatePhrase w = termIter.Current;
				nextTen += ";\t" + w + ":" + newdt.GetCount(w);
			}
			Redwood.Log(Redwood.Dbg, "Next ten phrases were " + nextTen);
			return finalwords;
		}

		public static void RemoveKeys<E, F>(TwoDimensionalCounter<E, F> counter, ICollection<E> removeKeysCollection)
		{
			foreach (E key in removeKeysCollection)
			{
				counter.Remove(key);
			}
		}

		private double NumNonRedundantPatterns(TwoDimensionalCounter<CandidatePhrase, E> terms, CandidatePhrase w)
		{
			object[] pats = Sharpen.Collections.ToArray(terms.GetCounter(w).KeySet());
			int numPat = 0;
			for (int i = 0; i < pats.Length; i++)
			{
				//String pati = constVars.getPatternIndex().get(pats[i]).toString();
				string pati = pats[i].ToString();
				bool contains = false;
				for (int j = i + 1; j < pats.Length; j++)
				{
					//String patj = constVars.getPatternIndex().get(pats[j]).toString();
					string patj = pats[j].ToString();
					if (patj.Contains(pati) || pati.Contains(patj))
					{
						contains = true;
						break;
					}
				}
				if (!contains)
				{
					numPat++;
				}
			}
			return numPat;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual ICounter<CandidatePhrase> LearnNewPhrases(string label, PatternsForEachToken patternsForEachToken, ICounter<E> patternsLearnedThisIter, ICounter<E> allSelectedPatterns, CollectionValuedMap<E, Triple<string, int, int>> tokensMatchedPatterns
			, ICounter<CandidatePhrase> scoreForAllWordsThisIteration, TwoDimensionalCounter<CandidatePhrase, E> terms, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, TwoDimensionalCounter<E, CandidatePhrase> patternsAndWords4Label, string
			 identifier, ICollection<CandidatePhrase> ignoreWords)
		{
			bool computeProcDataFreq = false;
			if (Data.processedDataFreq == null)
			{
				computeProcDataFreq = true;
				Data.processedDataFreq = new ClassicCounter<CandidatePhrase>();
				System.Diagnostics.Debug.Assert(Data.rawFreq != null);
			}
			ICollection<CandidatePhrase> alreadyIdentifiedWords = new HashSet<CandidatePhrase>(constVars.GetLearnedWords(label).KeySet());
			Sharpen.Collections.AddAll(alreadyIdentifiedWords, constVars.GetSeedLabelDictionary()[label]);
			ICounter<CandidatePhrase> words = LearnNewPhrasesPrivate(label, patternsForEachToken, patternsLearnedThisIter, allSelectedPatterns, alreadyIdentifiedWords, tokensMatchedPatterns, scoreForAllWordsThisIteration, terms, wordsPatExtracted, patternsAndWords4Label
				, identifier, ignoreWords, computeProcDataFreq);
			//constVars.addLabelDictionary(label, words.keySet());
			return words;
		}

		private void RunParallelApplyPats(IDictionary<string, DataInstance> sents, string label, E pattern, TwoDimensionalCounter<CandidatePhrase, E> wordsandLemmaPatExtracted, CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat, ICollection
			<CandidatePhrase> alreadyLabeledWords)
		{
			Redwood.Log(Redwood.Dbg, "Applying pattern " + pattern + " to a total of " + sents.Count + " sentences ");
			IList<string> notAllowedClasses = new List<string>();
			IList<string> sentids = CollectionUtils.ToList(sents.Keys);
			if (constVars.doNotExtractPhraseAnyWordLabeledOtherClass)
			{
				foreach (string l in constVars.GetAnswerClass().Keys)
				{
					if (!l.Equals(label))
					{
						notAllowedClasses.Add(l);
					}
				}
				notAllowedClasses.Add("OTHERSEM");
			}
			IDictionary<TokenSequencePattern, E> surfacePatternsLearnedThisIterConverted = null;
			IDictionary<SemgrexPattern, E> depPatternsLearnedThisIterConverted = null;
			if (constVars.patternType.Equals(PatternFactory.PatternType.Surface))
			{
				surfacePatternsLearnedThisIterConverted = new Dictionary<TokenSequencePattern, E>();
				string patternStr = null;
				try
				{
					patternStr = pattern.ToString(notAllowedClasses);
					TokenSequencePattern pat = ((TokenSequencePattern)TokenSequencePattern.Compile(constVars.env[label], patternStr));
					surfacePatternsLearnedThisIterConverted[pat] = pattern;
				}
				catch (Exception e)
				{
					log.Info("Error applying pattern " + patternStr + ". Probably an ill formed pattern (can be because of special symbols in label names). Contact the software developer.");
					throw;
				}
			}
			else
			{
				if (constVars.patternType.Equals(PatternFactory.PatternType.Dep))
				{
					depPatternsLearnedThisIterConverted = new Dictionary<SemgrexPattern, E>();
					SemgrexPattern pat = SemgrexPattern.Compile(pattern.ToString(notAllowedClasses), new Env(constVars.env[label].GetVariables()));
					depPatternsLearnedThisIterConverted[pat] = pattern;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
			//Apply the patterns and extract candidate phrases
			int num;
			int numThreads = constVars.numThreads;
			//If number of sentences is less, do not create so many threads
			if (sents.Count < 50)
			{
				numThreads = 1;
			}
			if (numThreads == 1)
			{
				num = sents.Count;
			}
			else
			{
				num = sents.Count / (numThreads - 1);
			}
			IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
			IList<IFuture<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>>> list = new List<IFuture<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E
				, Triple<string, int, int>>, ICollection<CandidatePhrase>>>>();
			for (int i = 0; i < numThreads; i++)
			{
				ICallable<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>> task = null;
				if (pattern.type.Equals(PatternFactory.PatternType.Surface))
				{
					//Redwood.log(Redwood.DBG, "Applying pats: assigning sentences " + i*num + " to " +Math.min(sentids.size(), (i + 1) * num) + " to thread " + (i+1));
					task = new ApplyPatterns(sents, num == sents.Count ? sentids : sentids.SubList(i * num, Math.Min(sentids.Count, (i + 1) * num)), surfacePatternsLearnedThisIterConverted, label, constVars.removeStopWordsFromSelectedPhrases, constVars.removePhrasesWithStopWords
						, constVars);
				}
				else
				{
					task = new ApplyDepPatterns(sents, num == sents.Count ? sentids : sentids.SubList(i * num, Math.Min(sentids.Count, (i + 1) * num)), depPatternsLearnedThisIterConverted, label, constVars.removeStopWordsFromSelectedPhrases, constVars.removePhrasesWithStopWords
						, constVars);
				}
				IFuture<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>> submit = executor.Submit(task);
				list.Add(submit);
			}
			// Now retrieve the result
			foreach (IFuture<Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>>> future in list)
			{
				try
				{
					Triple<TwoDimensionalCounter<CandidatePhrase, E>, CollectionValuedMap<E, Triple<string, int, int>>, ICollection<CandidatePhrase>> result = future.Get();
					Redwood.Log(ConstantsAndVariables.extremedebug, "Pattern " + pattern + " extracted phrases " + result.First());
					wordsandLemmaPatExtracted.AddAll(result.First());
					matchedTokensByPat.AddAll(result.Second());
					Sharpen.Collections.AddAll(alreadyLabeledWords, result.Third());
				}
				catch (Exception e)
				{
					executor.ShutdownNow();
					throw new Exception(e);
				}
			}
			executor.Shutdown();
		}

		/*
		void runParallelApplyPats(Map<String, List<CoreLabel>> sents, Set<String> sentIds, String label, Counter<E> patternsLearnedThisIter,  TwoDimensionalCounter<Pair<String, String>, Integer> wordsandLemmaPatExtracted,
		CollectionValuedMap<Integer, Triple<String, Integer, Integer>> matchedTokensByPat) throws InterruptedException, ExecutionException {
		List<String> keyset = new ArrayList<String>(sentIds);
		List<String> notAllowedClasses = new ArrayList<String>();
		
		if(constVars.doNotExtractPhraseAnyWordLabeledOtherClass){
		for(String l: constVars.getAnswerClass().keySet()){
		if(!l.equals(label)){
		notAllowedClasses.add(l+":"+l);
		}
		}
		notAllowedClasses.add("OTHERSEM:OTHERSEM");
		}
		
		//Apply the patterns and extract candidate phrases
		int num = 0;
		if (constVars.numThreads == 1)
		num = keyset.size();
		else
		num = keyset.size() / (constVars.numThreads - 1);
		ExecutorService executor = Executors.newFixedThreadPool(constVars.numThreads);
		List<Future<Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>>>> list = new ArrayList<Future<Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>>>>();
		for (int i = 0; i < constVars.numThreads; i++) {
		
		Callable<Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>>> task = null;
		Map<TokenSequencePattern, Integer> patternsLearnedThisIterConverted = new HashMap<TokenSequencePattern , Integer>();
		for (Integer pindex : patternsLearnedThisIter.keySet()){
		SurfacePattern p = constVars.getPatternIndex().get(pindex);
		TokenSequencePattern pat = TokenSequencePattern.compile(constVars.env.get(label), p.toString(notAllowedClasses));
		patternsLearnedThisIterConverted.put(pat, pindex);
		}
		
		task = new ApplyPatternsMulti(sents, keyset.subList(i * num,
		Math.min(keyset.size(), (i + 1) * num)), patternsLearnedThisIterConverted, label,
		constVars.removeStopWordsFromSelectedPhrases,
		constVars.removePhrasesWithStopWords, constVars);
		
		Future<Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>>> submit = executor
		.submit(task);
		list.add(submit);
		}
		
		// Now retrieve the result
		for (Future<Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>>> future : list) {
		try{
		Pair<TwoDimensionalCounter<Pair<String, String>, Integer>, CollectionValuedMap<Integer, Triple<String, Integer, Integer>>> result = future
		.get();
		wordsandLemmaPatExtracted.addAll(result.first());
		matchedTokensByPat.addAll(result.second());
		}catch(Exception e){
		executor.shutdownNow();
		throw new RuntimeException(e);
		}
		}
		executor.shutdown();
		}
		*/
		protected internal virtual IDictionary<E, IDictionary<string, DataInstance>> GetSentences(IDictionary<E, ICollection<string>> sentids)
		{
			try
			{
				ICollection<File> files = new HashSet<File>();
				IDictionary<E, IDictionary<string, DataInstance>> sentsAll = new Dictionary<E, IDictionary<string, DataInstance>>();
				CollectionValuedMap<string, E> sentIds2Pats = new CollectionValuedMap<string, E>();
				foreach (KeyValuePair<E, ICollection<string>> setEn in sentids)
				{
					if (!sentsAll.Contains(setEn.Key))
					{
						sentsAll[setEn.Key] = new Dictionary<string, DataInstance>();
					}
					foreach (string s in setEn.Value)
					{
						sentIds2Pats.Add(s, setEn.Key);
						if (constVars.batchProcessSents)
						{
							File f = Data.sentId2File[s];
							System.Diagnostics.Debug.Assert(f != null, "How come no file for sentence " + s);
							files.Add(f);
						}
					}
				}
				if (constVars.batchProcessSents)
				{
					foreach (File f in files)
					{
						IDictionary<string, DataInstance> sentsf = IOUtils.ReadObjectFromFile(f);
						foreach (KeyValuePair<string, DataInstance> s in sentsf)
						{
							foreach (E pat in sentIds2Pats[s.Key])
							{
								sentsAll[pat][s.Key] = s.Value;
							}
						}
					}
				}
				else
				{
					foreach (KeyValuePair<string, DataInstance> s in Data.sents)
					{
						foreach (E pat in sentIds2Pats[s.Key])
						{
							sentsAll[pat][s.Key] = s.Value;
						}
					}
				}
				//      /System.out.println("All sentences are " + sentsAll.entrySet().stream().map( x -> constVars.patternIndex.get(x.getKey())+":"+x.getValue()).collect(Collectors.toList()));
				return sentsAll;
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
			catch (IOException e1)
			{
				throw new Exception(e1);
			}
		}

		public virtual void ApplyPats(ICounter<E> patterns, string label, TwoDimensionalCounter<CandidatePhrase, E> wordsandLemmaPatExtracted, CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat, ICollection<CandidatePhrase> alreadyLabeledWords
			)
		{
			//   Counter<E> patternsLearnedThisIterConsistsOnlyGeneralized = new ClassicCounter<E>();
			//   Counter<E> patternsLearnedThisIterRest = new ClassicCounter<E>();
			//    Set<String> specialWords = constVars.invertedIndex.getSpecialWordsList();
			foreach (KeyValuePair<string, Env> en in constVars.env)
			{
				en.Value.GetVariables().PutAll(ConstantsAndVariables.globalEnv.GetVariables());
			}
			IDictionary<E, IDictionary<string, DataInstance>> sentencesForPatterns = GetSentences(constVars.invertedIndex.QueryIndex(patterns.KeySet()));
			foreach (KeyValuePair<E, IDictionary<string, DataInstance>> en_1 in sentencesForPatterns)
			{
				RunParallelApplyPats(en_1.Value, label, en_1.Key, wordsandLemmaPatExtracted, matchedTokensByPat, alreadyLabeledWords);
			}
			Redwood.Log(Redwood.Dbg, "# words/lemma and pattern pairs are " + wordsandLemmaPatExtracted.Size());
		}

		/*
		public void applyPats(Counter<E> patterns, String label, boolean computeDataFreq,  TwoDimensionalCounter<Pair<String, String>, Integer> wordsandLemmaPatExtracted,
		CollectionValuedMap<Integer, Triple<String, Integer, Integer>> matchedTokensByPat) throws ClassNotFoundException, IOException, InterruptedException, ExecutionException{
		Counter<E> patternsLearnedThisIterConsistsOnlyGeneralized = new ClassicCounter<E>();
		Counter<E> patternsLearnedThisIterRest = new ClassicCounter<E>();
		Set<String> specialWords = constVars.invertedIndex.getSpecialWordsList();
		List<String> extremelySmallStopWordsList = Arrays.asList(".",",","in","on","of","a","the","an");
		
		for(Entry<Integer, Double> en: patterns.entrySet()){
		Integer pindex = en.getKey();
		SurfacePattern p = constVars.getPatternIndex().get(pindex);
		String[] n = p.getSimplerTokensNext();
		String[] pr = p.getSimplerTokensPrev();
		boolean rest = false;
		if(n!=null){
		for(String e: n){
		if(!specialWords.contains(e)){
		rest = true;
		break;
		}
		}
		}
		if(rest == false && pr!=null){
		for(String e: pr){
		if(!specialWords.contains(e) && !extremelySmallStopWordsList.contains(e)){
		rest = true;
		break;
		}
		}
		}
		if(rest)
		patternsLearnedThisIterRest.setCount(en.getKey(), en.getValue());
		else
		patternsLearnedThisIterConsistsOnlyGeneralized.setCount(en.getKey(), en.getValue());
		}
		
		
		
		Map<String, Set<String>> sentidswithfilerest = constVars.invertedIndex.getFileSentIdsFromPats(patternsLearnedThisIterRest.keySet(), constVars.getPatternIndex());
		
		if (constVars.batchProcessSents) {
		List<File> filesToLoad;
		if(patternsLearnedThisIterConsistsOnlyGeneralized.size() > 0)
		filesToLoad = Data.sentsFiles;
		else{
		filesToLoad = new ArrayList<File>();
		for (String fname : sentidswithfilerest.keySet()) {
		String filename;
		//          if(!constVars.usingDirForSentsInIndex)
		//            filename = constVars.saveSentencesSerDir+"/"+fname;
		//          else
		filename = fname;
		filesToLoad.add(new File(filename));
		}
		}
		
		for (File fname : filesToLoad) {
		Redwood.log(Redwood.DBG, "Applying patterns to sents from " + fname);
		Map<String, List<CoreLabel>> sents = IOUtils.readObjectFromFile(fname);
		
		if(sentidswithfilerest != null && !sentidswithfilerest.isEmpty()){
		
		String filename;
		//          if(constVars.usingDirForSentsInIndex)
		//            filename = constVars.saveSentencesSerDir+"/"+fname.getName();
		//          else
		filename = fname.getAbsolutePath();
		
		Set<String> sentIDs = sentidswithfilerest.get(filename);
		if (sentIDs != null){
		this.runParallelApplyPats(sents, sentIDs, label, patternsLearnedThisIterRest, wordsandLemmaPatExtracted, matchedTokensByPat);
		} else
		Redwood.log(Redwood.DBG, "No sentIds for " + filename  + " in the index for the keywords from the patterns! The index came up with these files: " + sentidswithfilerest.keySet());
		}
		if(patternsLearnedThisIterConsistsOnlyGeneralized.size() > 0){
		this.runParallelApplyPats(sents, sents.keySet(), label, patternsLearnedThisIterConsistsOnlyGeneralized, wordsandLemmaPatExtracted, matchedTokensByPat);
		}
		
		if (computeDataFreq){
		Data.computeRawFreqIfNull(sents, constVars.numWordsCompound);
		Data.fileNamesUsedToComputeRawFreq.add(fname.getName());
		}
		}
		
		//Compute Frequency from the files not loaded using the invertedindex query. otherwise, later on there is an error.
		if(computeDataFreq){
		for(File f: Data.sentsFiles){
		if(!Data.fileNamesUsedToComputeRawFreq.contains(f.getName())){
		Map<String, List<CoreLabel>> sents = IOUtils.readObjectFromFile(f);
		Data.computeRawFreqIfNull(sents, constVars.numWordsCompound);
		Data.fileNamesUsedToComputeRawFreq.add(f.getName());
		}
		}
		}
		
		} else {
		
		if (sentidswithfilerest != null && !sentidswithfilerest.isEmpty()) {
		String filename = CollectionUtils.toList(sentidswithfilerest.keySet()).get(0);
		Set<String> sentids = sentidswithfilerest.get(filename);
		if (sentids != null) {
		this.runParallelApplyPats(Data.sents, sentids, label, patternsLearnedThisIterRest, wordsandLemmaPatExtracted, matchedTokensByPat);
		} else
		throw new RuntimeException("How come no sentIds for " + filename  + ". Index keyset is " + constVars.invertedIndex.getKeySet());
		}
		if(patternsLearnedThisIterConsistsOnlyGeneralized.size() > 0){
		this.runParallelApplyPats(Data.sents, Data.sents.keySet(), label, patternsLearnedThisIterConsistsOnlyGeneralized, wordsandLemmaPatExtracted, matchedTokensByPat);
		}
		Data.computeRawFreqIfNull(Data.sents, constVars.numWordsCompound);
		}
		Redwood.log(Redwood.DBG, "# words/lemma and pattern pairs are " + wordsandLemmaPatExtracted.size());
		}
		*/
		private void StatsWithoutApplyingPatterns(IDictionary<string, DataInstance> sents, PatternsForEachToken patternsForEachToken, ICounter<E> patternsLearnedThisIter, TwoDimensionalCounter<CandidatePhrase, E> wordsandLemmaPatExtracted)
		{
			foreach (KeyValuePair<string, DataInstance> sentEn in sents)
			{
				IDictionary<int, ICollection<E>> pat4Sent = patternsForEachToken.GetPatternsForAllTokens(sentEn.Key);
				if (pat4Sent == null)
				{
					throw new Exception("How come there are no patterns for " + sentEn.Key);
				}
				foreach (KeyValuePair<int, ICollection<E>> en in pat4Sent)
				{
					CoreLabel token = null;
					ICollection<E> p1 = en.Value;
					//        Set<Integer> p1 = en.getValue().first();
					//        Set<Integer> p2 = en.getValue().second();
					//        Set<Integer> p3 = en.getValue().third();
					foreach (E index in patternsLearnedThisIter.KeySet())
					{
						if (p1.Contains(index))
						{
							if (token == null)
							{
								token = sentEn.Value.GetTokens()[en.Key];
							}
							wordsandLemmaPatExtracted.IncrementCount(CandidatePhrase.CreateOrGet(token.Word(), token.Lemma()), index);
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private ICounter<CandidatePhrase> LearnNewPhrasesPrivate(string label, PatternsForEachToken patternsForEachToken, ICounter<E> patternsLearnedThisIter, ICounter<E> allSelectedPatterns, ICollection<CandidatePhrase> alreadyIdentifiedWords, CollectionValuedMap
			<E, Triple<string, int, int>> matchedTokensByPat, ICounter<CandidatePhrase> scoreForAllWordsThisIteration, TwoDimensionalCounter<CandidatePhrase, E> terms, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, TwoDimensionalCounter<E
			, CandidatePhrase> patternsAndWords4Label, string identifier, ICollection<CandidatePhrase> ignoreWords, bool computeProcDataFreq)
		{
			ICollection<CandidatePhrase> alreadyLabeledWords = new HashSet<CandidatePhrase>();
			if (constVars.doNotApplyPatterns)
			{
				// if want to get the stats by the lossy way of just counting without
				// applying the patterns
				ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
				while (sentsIter.MoveNext())
				{
					Pair<IDictionary<string, DataInstance>, File> sentsf = sentsIter.Current;
					this.StatsWithoutApplyingPatterns(sentsf.First(), patternsForEachToken, patternsLearnedThisIter, wordsPatExtracted);
				}
			}
			else
			{
				if (patternsLearnedThisIter.Size() > 0)
				{
					this.ApplyPats(patternsLearnedThisIter, label, wordsPatExtracted, matchedTokensByPat, alreadyLabeledWords);
				}
			}
			if (computeProcDataFreq)
			{
				if (!phraseScorer.wordFreqNorm.Equals(PhraseScorer.Normalization.None))
				{
					Redwood.Log(Redwood.Dbg, "computing processed freq");
					foreach (KeyValuePair<CandidatePhrase, double> fq in Data.rawFreq.EntrySet())
					{
						double @in = fq.Value;
						if (phraseScorer.wordFreqNorm.Equals(PhraseScorer.Normalization.Sqrt))
						{
							@in = Math.Sqrt(@in);
						}
						else
						{
							if (phraseScorer.wordFreqNorm.Equals(PhraseScorer.Normalization.Log))
							{
								@in = 1 + Math.Log(@in);
							}
							else
							{
								throw new Exception("can't understand the normalization");
							}
						}
						System.Diagnostics.Debug.Assert(!double.IsNaN(@in), "Why is processed freq nan when rawfreq is " + @in);
						Data.processedDataFreq.SetCount(fq.Key, @in);
					}
				}
				else
				{
					Data.processedDataFreq = Data.rawFreq;
				}
			}
			if (constVars.wordScoring.Equals(GetPatternsFromDataMultiClass.WordScoring.Weightednorm))
			{
				foreach (CandidatePhrase en in wordsPatExtracted.FirstKeySet())
				{
					if (!constVars.GetOtherSemanticClassesWords().Contains(en) && (en.GetPhraseLemma() == null || !constVars.GetOtherSemanticClassesWords().Contains(CandidatePhrase.CreateOrGet(en.GetPhraseLemma()))) && !alreadyLabeledWords.Contains(en))
					{
						terms.AddAll(en, wordsPatExtracted.GetCounter(en));
					}
				}
				RemoveKeys(terms, ConstantsAndVariables.GetStopWords());
				ICounter<CandidatePhrase> phraseScores = phraseScorer.ScorePhrases(label, terms, wordsPatExtracted, allSelectedPatterns, alreadyIdentifiedWords, false);
				System.Console.Out.WriteLine("count for word U.S. is " + phraseScores.GetCount(CandidatePhrase.CreateOrGet("U.S.")));
				ICollection<CandidatePhrase> ignoreWordsAll;
				if (ignoreWords != null && !ignoreWords.IsEmpty())
				{
					ignoreWordsAll = CollectionUtils.UnionAsSet(ignoreWords, constVars.GetOtherSemanticClassesWords());
				}
				else
				{
					ignoreWordsAll = new HashSet<CandidatePhrase>(constVars.GetOtherSemanticClassesWords());
				}
				Sharpen.Collections.AddAll(ignoreWordsAll, constVars.GetSeedLabelDictionary()[label]);
				Sharpen.Collections.AddAll(ignoreWordsAll, constVars.GetLearnedWords(label).KeySet());
				System.Console.Out.WriteLine("ignoreWordsAll contains word U.S. is " + ignoreWordsAll.Contains(CandidatePhrase.CreateOrGet("U.S.")));
				ICounter<CandidatePhrase> finalwords = ChooseTopWords(phraseScores, terms, phraseScores, ignoreWordsAll, constVars.thresholdWordExtract);
				phraseScorer.PrintReasonForChoosing(finalwords);
				scoreForAllWordsThisIteration.Clear();
				Counters.AddInPlace(scoreForAllWordsThisIteration, phraseScores);
				Redwood.Log(ConstantsAndVariables.minimaldebug, "\n\n## Selected Words for " + label + " : " + Counters.ToSortedString(finalwords, finalwords.Size(), "%1$s:%2$.2f", "\t"));
				if (constVars.goldEntities != null)
				{
					IDictionary<string, bool> goldEntities4Label = constVars.goldEntities[label];
					if (goldEntities4Label != null)
					{
						StringBuilder s = new StringBuilder();
						finalwords.KeySet().Stream().ForEach(null);
						Redwood.Log(ConstantsAndVariables.minimaldebug, "\n\n## Gold labels for selected words for label " + label + " : " + s.ToString());
					}
					else
					{
						Redwood.Log(Redwood.Dbg, "No gold entities provided for label " + label);
					}
				}
				if (constVars.outDir != null && !constVars.outDir.IsEmpty())
				{
					string outputdir = constVars.outDir + "/" + identifier + "/" + label;
					IOUtils.EnsureDir(new File(outputdir));
					TwoDimensionalCounter<CandidatePhrase, CandidatePhrase> reasonForWords = new TwoDimensionalCounter<CandidatePhrase, CandidatePhrase>();
					foreach (CandidatePhrase word in finalwords.KeySet())
					{
						foreach (E l in wordsPatExtracted.GetCounter(word).KeySet())
						{
							foreach (CandidatePhrase w2 in patternsAndWords4Label.GetCounter(l))
							{
								reasonForWords.IncrementCount(word, w2);
							}
						}
					}
					Redwood.Log(ConstantsAndVariables.minimaldebug, "Saving output in " + outputdir);
					string filename = outputdir + "/words.json";
					// the json object is an array corresponding to each iteration - of list
					// of objects,
					// each of which is a bean of entity and reasons
					IJsonArrayBuilder obj = Javax.Json.Json.CreateArrayBuilder();
					if (writtenInJustification.Contains(label) && writtenInJustification[label])
					{
						IJsonReader jsonReader = Javax.Json.Json.CreateReader(new BufferedInputStream(new FileInputStream(filename)));
						IJsonArray objarr = jsonReader.ReadArray();
						foreach (IJsonValue o in objarr)
						{
							obj.Add(o);
						}
						jsonReader.Close();
					}
					IJsonArrayBuilder objThisIter = Javax.Json.Json.CreateArrayBuilder();
					foreach (CandidatePhrase w in reasonForWords.FirstKeySet())
					{
						IJsonObjectBuilder objinner = Javax.Json.Json.CreateObjectBuilder();
						IJsonArrayBuilder l = Javax.Json.Json.CreateArrayBuilder();
						foreach (CandidatePhrase w2 in reasonForWords.GetCounter(w).KeySet())
						{
							l.Add(w2.GetPhrase());
						}
						IJsonArrayBuilder pats = Javax.Json.Json.CreateArrayBuilder();
						foreach (E p in wordsPatExtracted.GetCounter(w))
						{
							pats.Add(p.ToStringSimple());
						}
						objinner.Add("reasonwords", l);
						objinner.Add("patterns", pats);
						objinner.Add("score", finalwords.GetCount(w));
						objinner.Add("entity", w.GetPhrase());
						objThisIter.Add(objinner.Build());
					}
					obj.Add(objThisIter);
					// Redwood.log(ConstantsAndVariables.minimaldebug, channelNameLogger,
					// "Writing justification at " + filename);
					IOUtils.WriteStringToFile(StringUtils.Normalize(StringUtils.ToAscii(obj.Build().ToString())), filename, "ASCII");
					writtenInJustification[label] = true;
				}
				if (constVars.justify)
				{
					Redwood.Log(Redwood.Dbg, "\nJustification for phrases:\n");
					foreach (CandidatePhrase word in finalwords.KeySet())
					{
						Redwood.Log(Redwood.Dbg, "Phrase " + word + " extracted because of patterns: \t" + Counters.ToSortedString(wordsPatExtracted.GetCounter(word), wordsPatExtracted.GetCounter(word).Size(), "%1$s:%2$f", "\n"));
					}
				}
				// if (usePatternResultAsLabel)
				// if (answerLabel != null)
				// labelWords(sents, commonEngWords, finalwords.keySet(),
				// patterns.keySet(), outFile);
				// else
				// throw new RuntimeException("why is the answer label null?");
				return finalwords;
			}
			else
			{
				if (constVars.wordScoring.Equals(GetPatternsFromDataMultiClass.WordScoring.Bpb))
				{
					Counters.AddInPlace(terms, wordsPatExtracted);
					ICounter<CandidatePhrase> maxPatWeightTerms = new ClassicCounter<CandidatePhrase>();
					IDictionary<CandidatePhrase, E> wordMaxPat = new Dictionary<CandidatePhrase, E>();
					foreach (KeyValuePair<CandidatePhrase, ClassicCounter<E>> en in terms.EntrySet())
					{
						ICounter<E> weights = new ClassicCounter<E>();
						foreach (E k in en.Value.KeySet())
						{
							weights.SetCount(k, patternsLearnedThisIter.GetCount(k));
						}
						maxPatWeightTerms.SetCount(en.Key, Counters.Max(weights));
						wordMaxPat[en.Key] = Counters.Argmax(weights);
					}
					Counters.RemoveKeys(maxPatWeightTerms, alreadyIdentifiedWords);
					double maxvalue = Counters.Max(maxPatWeightTerms);
					ICollection<CandidatePhrase> words = Counters.KeysAbove(maxPatWeightTerms, maxvalue - 1e-10);
					CandidatePhrase bestw = null;
					if (words.Count > 1)
					{
						double max = double.NegativeInfinity;
						foreach (CandidatePhrase w in words)
						{
							if (terms.GetCount(w, wordMaxPat[w]) > max)
							{
								max = terms.GetCount(w, wordMaxPat[w]);
								bestw = w;
							}
						}
					}
					else
					{
						if (words.Count == 1)
						{
							bestw = words.GetEnumerator().Current;
						}
						else
						{
							return new ClassicCounter<CandidatePhrase>();
						}
					}
					Redwood.Log(ConstantsAndVariables.minimaldebug, "Selected Words: " + bestw);
					return Counters.AsCounter(Arrays.AsList(bestw));
				}
				else
				{
					throw new Exception("wordscoring " + constVars.wordScoring + " not identified");
				}
			}
		}

		// private void combineExternalFeatures(Counter<String> words) {
		//
		// for (Entry<String, Double> en : words.entrySet()) {
		// Integer num = constVars.distSimClusters.get(en.getKey());
		// if (num == null)
		// num = -1;
		// // Double score = externalWeights.getCount(num);
		// // if not present in the clusters, take minimum of the scores of the
		// // individual words
		// // if (num == null) {
		// // for (String w : en.getKey().split("\\s+")) {
		// // Integer n = constVars.distSimClusters.get(w);
		// // if (n == null)
		// // continue;
		// // score = Math.min(score, externalWeights.getCount(n));
		// // }
		// // }
		// words.setCount(en.getKey(), en.getValue() *
		// constVars.distSimWeights.getCount(num));
		// }
		// }
		internal virtual ICounter<string> GetLearnedScores()
		{
			return phraseScorer.GetLearnedScores();
		}
		// private Counter<String> getLookAheadWeights(Counter<String> words,
		// Counter<String> externalWordWeights, Set<String> alreadyIdentifiedWords,
		// String label,
		// Counter<SurfacePattern> currentAllPatternWeights,
		// TwoDimensionalCounter<SurfacePattern, String> allPatternsandWords) throws
		// IOException {
		// System.out.println("size of patterns weight counter is " +
		// currentAllPatternWeights.size());
		//
		// DirectedWeightedMultigraph<String, DefaultWeightedEdge> graph = new
		// DirectedWeightedMultigraph<String,
		// DefaultWeightedEdge>(org.jgrapht.graph.DefaultWeightedEdge.class);
		//
		// if (Data.googleNGram.size() == 0) {
		// Data.loadGoogleNGrams();
		// }
		//
		// TwoDimensionalCounter<String, SurfacePattern> allPatsAndWords =
		// TwoDimensionalCounter.reverseIndexOrder(allPatternsandWords);
		// System.out.println("We have patterns for " + allPatsAndWords.size() +
		// " words ");
		// TwoDimensionalCounter<String, String> lookaheadweights = new
		// TwoDimensionalCounter<String, String>();
		// // Counter<String> weights = new ClassicCounter<String>();
		//
		// for (Entry<String, Double> en : words.entrySet()) {
		// Counter<SurfacePattern> pats = new
		// ClassicCounter<SurfacePattern>(allPatsAndWords.getCounter(en.getKey()));
		// for (SurfacePattern p : pats.keySet()) {
		// pats.setCount(p, pats.getCount(p) * currentAllPatternWeights.getCount(p));
		// }
		//
		// for (Pair<SurfacePattern, Double> p : Counters.topKeysWithCounts(pats, 10))
		// {
		//
		// for (Entry<String, Double> pen :
		// allPatternsandWords.getCounter(p.first()).entrySet()) {
		// if (pen.getKey().equals(en.getKey()) ||
		// alreadyIdentifiedWords.contains(pen.getKey()) ||
		// constVars.otherSemanticClasses.contains(pen.getKey()))
		// continue;
		//
		// double ngramWt = 1.0;
		// if (Data.googleNGram.containsKey(pen.getKey())) {
		// assert (Data.rawFreq.containsKey(pen.getKey()));
		// ngramWt = (1 + Data.rawFreq.getCount(pen.getKey())) / (Data.rawFreq.size()
		// + Data.googleNGram.getCount(pen.getKey()));
		// }
		// double wordweight = ngramWt;// (minExternalWordWeight +
		// // externalWordWeights.getCount(pen.getKey()))
		// // * p.second() * (0.1 +
		// // currentAllPatternWeights.getCount(p.first()))
		// // * ;
		// // if (wordweight != 0)
		// if (wordweight == 0) {
		// // System.out.println("word weight is zero for " + pen.getKey() +
		// // " and the weights were " +
		// // externalWordWeights.getCount(pen.getKey()) + ";" + p.second() +
		// // ";"
		// // + (0.1 + currentPatternWeights.getCount(p.first())) + ";" +
		// // ngramWt);
		// } else {
		// lookaheadweights.setCount(en.getKey(), pen.getKey(), Math.log(wordweight));
		// graph.addVertex(en.getKey());
		// graph.addVertex(pen.getKey());
		// DefaultWeightedEdge e = graph.addEdge(en.getKey(), pen.getKey());
		// graph.setEdgeWeight(e, lookaheadweights.getCount(en.getKey(),
		// pen.getKey()));
		// }
		//
		// }
		//
		// }
		// // weights.setCount(en.getKey(),
		// // Math.exp(Counters(lookaheadweights.getCounter(en.getKey()))));
		//
		// }
		// Counter<String> weights = new ClassicCounter<String>();
		// for (Entry<String, ClassicCounter<String>> en :
		// lookaheadweights.entrySet()) {
		// // List<Pair<String, Double>> sorted =
		// // Counters.toSortedListWithCounts(en.getValue());
		// // double val = sorted.get((int) Math.floor(sorted.size() / 2)).second();
		// double wt = Math.exp(en.getValue().totalCount() / en.getValue().size());
		//
		// weights.setCount(en.getKey(), wt);
		// }
		// // Counters.expInPlace(weights);
		// // List<String> tk = Counters.topKeys(weights, 50);
		// // BufferedWriter w = new BufferedWriter(new FileWriter("lookahead_" +
		// // answerLabel, true));
		// // for (String s : tk) {
		// // w.write(s + "\t" + weights.getCount(s) + "\t" +
		// // lookaheadweights.getCounter(s) + "\n");
		// // }
		// // w.close();
		// // BufferedWriter writer = new BufferedWriter(new FileWriter("graph.gdf"));
		// // writeGraph(writer, graph);
		// System.out.println("done writing graph");
		// Redwood.log(ConstantsAndVariables.minimaldebug, "calculated look ahead weights for " +
		// weights.size() + " words");
		//
		// return weights;
		// }
		// void writeGraph(BufferedWriter w, DirectedWeightedMultigraph<String,
		// DefaultWeightedEdge> g) throws IOException {
		// w.write("nodedef>name VARCHAR\n");
		// for (String n : g.vertexSet()) {
		// w.write(n + "\n");
		// }
		// w.write("edgedef>node1 VARCHAR,node2 VARCHAR, weight DOUBLE\n");
		// for (DefaultWeightedEdge e : g.edgeSet()) {
		// w.write(g.getEdgeSource(e) + "," + g.getEdgeTarget(e) + "," +
		// g.getEdgeWeight(e) + "\n");
		// }
		// w.close();
		// }
	}
}
