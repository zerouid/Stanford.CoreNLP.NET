using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Text;
using Java.Time;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Atomic;
using Java.Util.Function;
using Java.Util.Regex;
using Java.Util.Zip;
using Javax.Json;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>
	/// Given text and a seed list, this class gives more words like the seed words
	/// by learning surface word or dependency patterns.
	/// </summary>
	/// <remarks>
	/// Given text and a seed list, this class gives more words like the seed words
	/// by learning surface word or dependency patterns.
	/// The multi-threaded class (
	/// <c>nthread</c>
	/// parameter for number of
	/// threads) takes as input.
	/// To use the default options, run
	/// <c>java -mx1000m edu.stanford.nlp.patterns.GetPatternsFromDataMultiClass -file text_file -seedWordsFiles label1,seedwordlist1;label2,seedwordlist2;... -outDir output_directory (optional)</c>
	/// <c>fileFormat</c>
	/// : (Optional) Default is text. Valid values are text
	/// (or txt) and ser, where the serialized file is of the type
	/// <c>Map&lt;String,List&lt;CoreLabel&gt;&gt;</c>
	/// .
	/// <c>file</c>
	/// : (Required) Input file(s) (default assumed text). Can be
	/// one or more of (concatenated by comma or semi-colon): file, directory, files
	/// with regex in the filename (for example: "mydir/health-.*-processed.txt")
	/// <c>seedWordsFiles</c>
	/// : (Required)
	/// label1,file_seed_words1;label2,file_seed_words2;... where file_seed_words are
	/// files with list of seed words, one in each line
	/// <c>outDir</c>
	/// : (Optional) output directory where visualization/output
	/// files are stored
	/// For other flags, see individual comments for each flag.
	/// To use a properties file, see
	/// projects/core/data/edu/stanford/nlp/patterns/surface/example.properties or patterns/example.properties (depends on which codebase you are using)
	/// as an example for the flags and their brief descriptions. Run the code as:
	/// <c>java -mx1000m -cp classpath edu.stanford.nlp.patterns.GetPatternsFromDataMultiClass -props dir-as-above/example.properties</c>
	/// IMPORTANT: Many flags are described in the classes
	/// <see cref="ConstantsAndVariables"/>
	/// ,
	/// <see cref="Edu.Stanford.Nlp.Patterns.Surface.CreatePatterns{E}"/>
	/// , and
	/// <see cref="PhraseScorer{E}"/>
	/// .
	/// </remarks>
	/// <author>Sonal Gupta (sonal@cs.stanford.edu)</author>
	[System.Serializable]
	public class GetPatternsFromDataMultiClass<E>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.GetPatternsFromDataMultiClass));

		private const long serialVersionUID = 1L;

		private PatternsForEachToken<E> patsForEachToken = null;

		public IDictionary<string, ICollection<string>> wordsForOtherClass = null;

		/// <summary>
		/// RlogF is from Riloff 1996, when R's denominator is (pos+neg+unlabeled)
		/// RlogFPosNeg is when the R's denominator is just (pos+negative) examples
		/// PosNegOdds is just the ratio of number of positive words to number of
		/// negative
		/// PosNegUnlabOdds is just the ratio of number of positive words to number of
		/// negative (unlabeled words + negative)
		/// RatioAll is pos/(neg+pos+unlabeled)
		/// YanGarber02 is the modified version presented in
		/// "Unsupervised Learning of Generalized Names"
		/// LOGREG is learning a logistic regression classifier to combine weights to
		/// score a phrase (Same as PhEvalInPat, except score of an unlabeled phrase is
		/// computed using a logistic regression classifier)
		/// LOGREGlogP is learning a logistic regression classifier to combine weights
		/// to score a phrase (Same as PhEvalInPatLogP, except score of an unlabeled
		/// phrase is computed using a logistic regression classifier)
		/// SqrtAllRatio is the pattern scoring used in Gupta et al.
		/// </summary>
		/// <remarks>
		/// RlogF is from Riloff 1996, when R's denominator is (pos+neg+unlabeled)
		/// RlogFPosNeg is when the R's denominator is just (pos+negative) examples
		/// PosNegOdds is just the ratio of number of positive words to number of
		/// negative
		/// PosNegUnlabOdds is just the ratio of number of positive words to number of
		/// negative (unlabeled words + negative)
		/// RatioAll is pos/(neg+pos+unlabeled)
		/// YanGarber02 is the modified version presented in
		/// "Unsupervised Learning of Generalized Names"
		/// LOGREG is learning a logistic regression classifier to combine weights to
		/// score a phrase (Same as PhEvalInPat, except score of an unlabeled phrase is
		/// computed using a logistic regression classifier)
		/// LOGREGlogP is learning a logistic regression classifier to combine weights
		/// to score a phrase (Same as PhEvalInPatLogP, except score of an unlabeled
		/// phrase is computed using a logistic regression classifier)
		/// SqrtAllRatio is the pattern scoring used in Gupta et al. JAMIA 2014 paper
		/// Below F1SeedPattern and BPB based on paper
		/// "Unsupervised Method for Automatics Construction of a disease dictionary..."
		/// Precision, Recall, and FMeasure (controlled by fbeta flag) is ranking the patterns using
		/// their precision, recall and F_beta measure
		/// </remarks>
		public enum PatternScoring
		{
			F1SeedPattern,
			RlogF,
			RlogFPosNeg,
			RlogFUnlabNeg,
			RlogFNeg,
			PhEvalInPat,
			PhEvalInPatLogP,
			PosNegOdds,
			YanGarber02,
			PosNegUnlabOdds,
			RatioAll,
			Logreg,
			LOGREGlogP,
			SqrtAllRatio,
			LinICML03,
			kNN
		}

		internal enum WordScoring
		{
			Bpb,
			Weightednorm
		}

		private IDictionary<string, bool> writtenPatInJustification = new Dictionary<string, bool>();

		private IDictionary<string, ICounter<E>> learnedPatterns = new Dictionary<string, ICounter<E>>();

		private IDictionary<string, IDictionary<int, ICounter<E>>> learnedPatternsEachIter = new Dictionary<string, IDictionary<int, ICounter<E>>>();

		internal IDictionary<string, ICounter<CandidatePhrase>> matchedSeedWords = new Dictionary<string, ICounter<CandidatePhrase>>();

		public IDictionary<string, TwoDimensionalCounter<CandidatePhrase, E>> wordsPatExtracted = new Dictionary<string, TwoDimensionalCounter<CandidatePhrase, E>>();

		internal Properties props;

		public ScorePhrases scorePhrases;

		public ConstantsAndVariables constVars;

		public CreatePatterns createPats;

		private readonly DecimalFormat df = new DecimalFormat("#.##");

		private bool notComputedAllPatternsYet = true;

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> seedSet, bool labelUsingSeedSets, string answerLabel)
			: this(props, sents, seedSet, labelUsingSeedSets, typeof(PatternsAnnotations.PatternLabel1), answerLabel)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> seedSet, bool labelUsingSeedSets, Type answerClass, string answerLabel)
		{
			//public Map<String, Map<Integer, Set<E>>> patternsForEachToken = null;
			// String channelNameLogger = "patterns";
			//Same as learnedPatterns but with iteration information
			/*
			* when there is only one label
			*/
			this.props = props;
			IDictionary<string, Type> ansCl = new Dictionary<string, Type>();
			ansCl[answerLabel] = answerClass;
			IDictionary<string, Type> generalizeClasses = new Dictionary<string, Type>();
			IDictionary<string, IDictionary<Type, object>> ignoreClasses = new Dictionary<string, IDictionary<Type, object>>();
			ignoreClasses[answerLabel] = new Dictionary<Type, object>();
			IDictionary<string, ICollection<CandidatePhrase>> seedSets = new Dictionary<string, ICollection<CandidatePhrase>>();
			seedSets[answerLabel] = seedSet;
			SetUpConstructor(sents, seedSets, labelUsingSeedSets, ansCl, generalizeClasses, ignoreClasses);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> seedSet, bool labelUsingSeedSets, string answerLabel, IDictionary<string, Type> generalizeClasses, IDictionary<Type, 
			object> ignoreClasses)
			: this(props, sents, seedSet, labelUsingSeedSets, typeof(PatternsAnnotations.PatternLabel1), answerLabel, generalizeClasses, ignoreClasses)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> seedSet, bool labelUsingSeedSets, Type answerClass, string answerLabel, IDictionary<string, Type> generalizeClasses, 
			IDictionary<Type, object> ignoreClasses)
		{
			this.props = props;
			IDictionary<string, Type> ansCl = new Dictionary<string, Type>();
			ansCl[answerLabel] = answerClass;
			IDictionary<string, IDictionary<Type, object>> iC = new Dictionary<string, IDictionary<Type, object>>();
			iC[answerLabel] = ignoreClasses;
			IDictionary<string, ICollection<CandidatePhrase>> seedSets = new Dictionary<string, ICollection<CandidatePhrase>>();
			seedSets[answerLabel] = seedSet;
			SetUpConstructor(sents, seedSets, labelUsingSeedSets, ansCl, generalizeClasses, iC);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, IDictionary<string, ICollection<CandidatePhrase>> seedSets, bool labelUsingSeedSets)
		{
			this.props = props;
			IDictionary<string, Type> ansCl = new Dictionary<string, Type>();
			IDictionary<string, Type> gC = new Dictionary<string, Type>();
			IDictionary<string, IDictionary<Type, object>> iC = new Dictionary<string, IDictionary<Type, object>>();
			int i = 1;
			foreach (string label in seedSets.Keys)
			{
				string ansclstr = "edu.stanford.nlp.patterns.PatternsAnnotations$PatternLabel" + i;
				ansCl[label] = (Type)Sharpen.Runtime.GetType(ansclstr);
				iC[label] = new Dictionary<Type, object>();
				i++;
			}
			SetUpConstructor(sents, seedSets, labelUsingSeedSets, ansCl, gC, iC);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, IDictionary<string, ICollection<CandidatePhrase>> seedSets, bool labelUsingSeedSets, IDictionary<string, Type> answerClass)
			: this(props, sents, seedSets, labelUsingSeedSets, answerClass, new Dictionary<string, Type>(), new Dictionary<string, IDictionary<Type, object>>())
		{
		}

		/// <summary>
		/// Generalize classes basically maps label strings to a map of generalized
		/// strings and the corresponding class ignoreClasses have to be boolean.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="System.TypeLoadException"/>
		public GetPatternsFromDataMultiClass(Properties props, IDictionary<string, DataInstance> sents, IDictionary<string, ICollection<CandidatePhrase>> seedSets, bool labelUsingSeedSets, IDictionary<string, Type> answerClass, IDictionary<string, Type
			> generalizeClasses, IDictionary<string, IDictionary<Type, object>> ignoreClasses)
		{
			this.props = props;
			if (ignoreClasses.IsEmpty())
			{
				foreach (string label in seedSets.Keys)
				{
					ignoreClasses[label] = new Dictionary<Type, object>();
				}
			}
			SetUpConstructor(sents, seedSets, labelUsingSeedSets, answerClass, generalizeClasses, ignoreClasses);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void SetUpConstructor(IDictionary<string, DataInstance> sents, IDictionary<string, ICollection<CandidatePhrase>> seedSets, bool labelUsingSeedSets, IDictionary<string, Type> answerClass, IDictionary<string, Type> generalizeClasses, IDictionary
			<string, IDictionary<Type, object>> ignoreClasses)
		{
			Data.sents = sents;
			ArgumentParser.FillOptions(typeof(Data), props);
			ArgumentParser.FillOptions(typeof(ConstantsAndVariables), props);
			PatternFactory.SetUp(props, PatternFactory.PatternType.ValueOf(props.GetProperty(GetPatternsFromDataMultiClass.Flags.patternType)), seedSets.Keys);
			constVars = new ConstantsAndVariables(props, seedSets, answerClass, generalizeClasses, ignoreClasses);
			if (constVars.writeMatchedTokensFiles && constVars.batchProcessSents)
			{
				throw new Exception("writeMatchedTokensFiles and batchProcessSents cannot be true at the same time (not implemented; also doesn't make sense to save a large sentences json file)");
			}
			if (constVars.debug < 1)
			{
				Redwood.HideChannelsEverywhere(ConstantsAndVariables.minimaldebug);
			}
			if (constVars.debug < 2)
			{
				Redwood.HideChannelsEverywhere(Redwood.Dbg);
			}
			constVars.justify = true;
			if (constVars.debug < 3)
			{
				constVars.justify = false;
			}
			if (constVars.debug < 4)
			{
				Redwood.HideChannelsEverywhere(ConstantsAndVariables.extremedebug);
			}
			Redwood.Log(Redwood.Dbg, "Running with debug output");
			Redwood.Log(ConstantsAndVariables.extremedebug, "Running with extreme debug output");
			wordsPatExtracted = new Dictionary<string, TwoDimensionalCounter<CandidatePhrase, E>>();
			foreach (string label in answerClass.Keys)
			{
				wordsPatExtracted[label] = new TwoDimensionalCounter<CandidatePhrase, E>();
			}
			scorePhrases = new ScorePhrases(props, constVars);
			createPats = new CreatePatterns(props, constVars);
			System.Diagnostics.Debug.Assert(!(constVars.doNotApplyPatterns && (PatternFactory.useStopWordsBeforeTerm || PatternFactory.numWordsCompoundMax > 1)), " Cannot have both doNotApplyPatterns and (useStopWordsBeforeTerm true or numWordsCompound > 1)!"
				);
			if (constVars.invertedIndexDirectory == null)
			{
				File f = File.CreateTempFile("inv", "index");
				f.DeleteOnExit();
				f.Mkdir();
				constVars.invertedIndexDirectory = f.GetAbsolutePath();
			}
			ICollection<string> extremelySmallStopWordsList = CollectionUtils.AsSet(".", ",", "in", "on", "of", "a", "the", "an");
			//Function to use to how to add CoreLabels to index
			IFunction<CoreLabel, IDictionary<string, string>> transformCoreLabelToString = null;
			bool createIndex = false;
			if (constVars.loadInvertedIndex)
			{
				constVars.invertedIndex = SentenceIndex.LoadIndex(constVars.invertedIndexClass, props, extremelySmallStopWordsList, constVars.invertedIndexDirectory, transformCoreLabelToString);
			}
			else
			{
				constVars.invertedIndex = SentenceIndex.CreateIndex(constVars.invertedIndexClass, null, props, extremelySmallStopWordsList, constVars.invertedIndexDirectory, transformCoreLabelToString);
				createIndex = true;
			}
			int totalNumSents = 0;
			bool computeDataFreq = false;
			if (Data.rawFreq == null)
			{
				Data.rawFreq = new ClassicCounter<CandidatePhrase>();
				computeDataFreq = true;
			}
			ConstantsAndVariables.DataSentsIterator iter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
			while (iter.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sentsIter = iter.Current;
				IDictionary<string, DataInstance> sentsf = sentsIter.First();
				if (constVars.batchProcessSents)
				{
					foreach (KeyValuePair<string, DataInstance> en in sentsf)
					{
						Data.sentId2File[en.Key] = sentsIter.Second();
					}
				}
				totalNumSents += sentsf.Count;
				if (computeDataFreq)
				{
					Data.ComputeRawFreqIfNull(sentsf, PatternFactory.numWordsCompoundMax);
				}
				Redwood.Log(Redwood.Dbg, "Initializing sents size " + sentsf.Count + " sentences, either by labeling with the seed set or just setting the right classes");
				foreach (string l in constVars.GetAnswerClass().Keys)
				{
					Redwood.Log(Redwood.Dbg, "labelUsingSeedSets is " + labelUsingSeedSets + " and seed set size for " + l + " is " + (seedSets == null ? "null" : seedSets[l].Count));
					ICollection<CandidatePhrase> seed = seedSets == null || !labelUsingSeedSets ? new HashSet<CandidatePhrase>() : (seedSets.Contains(l) ? seedSets[l] : new HashSet<CandidatePhrase>());
					if (!matchedSeedWords.Contains(l))
					{
						matchedSeedWords[l] = new ClassicCounter<CandidatePhrase>();
					}
					ICounter<CandidatePhrase> matched = RunLabelSeedWords(sentsf, constVars.GetAnswerClass()[l], l, seed, constVars, labelUsingSeedSets);
					System.Console.Out.WriteLine("matched phrases for " + l + " is " + matched);
					matchedSeedWords[l].AddAll(matched);
					if (constVars.addIndvWordsFromPhrasesExceptLastAsNeg)
					{
						Redwood.Log(ConstantsAndVariables.minimaldebug, "adding indv words from phrases except last as neg");
						ICollection<CandidatePhrase> otherseed = new HashSet<CandidatePhrase>();
						if (labelUsingSeedSets)
						{
							foreach (CandidatePhrase s in seed)
							{
								string[] t = s.GetPhrase().Split("\\s+");
								for (int i = 0; i < t.Length - 1; i++)
								{
									if (!seed.Contains(t[i]))
									{
										otherseed.Add(CandidatePhrase.CreateOrGet(t[i]));
									}
								}
							}
						}
						RunLabelSeedWords(sentsf, typeof(PatternsAnnotations.OtherSemanticLabel), "OTHERSEM", otherseed, constVars, labelUsingSeedSets);
					}
				}
				if (labelUsingSeedSets && constVars.GetOtherSemanticClassesWords() != null)
				{
					string l_1 = "OTHERSEM";
					if (!matchedSeedWords.Contains(l_1))
					{
						matchedSeedWords[l_1] = new ClassicCounter<CandidatePhrase>();
					}
					matchedSeedWords[l_1].AddAll(RunLabelSeedWords(sentsf, typeof(PatternsAnnotations.OtherSemanticLabel), l_1, constVars.GetOtherSemanticClassesWords(), constVars, labelUsingSeedSets));
				}
				if (constVars.removeOverLappingLabelsFromSeed)
				{
					RemoveOverLappingLabels(sentsf);
				}
				if (createIndex)
				{
					constVars.invertedIndex.Add(sentsf, true);
				}
				if (sentsIter.Second().Exists())
				{
					Redwood.Log(Redwood.Dbg, "Saving the labeled seed sents (if given the option) to the same file " + sentsIter.Second());
					IOUtils.WriteObjectToFile(sentsf, sentsIter.Second());
				}
			}
			Redwood.Log(Redwood.Dbg, "Done loading/creating inverted index of tokens and labeling data with total of " + constVars.invertedIndex.Size() + " sentences");
			//If the scorer class is LearnFeatWt then individual word class is added as a feature
			if (scorePhrases.phraseScorerClass.Equals(typeof(ScorePhrasesAverageFeatures)) && (constVars.usePatternEvalWordClass || constVars.usePhraseEvalWordClass))
			{
				if (constVars.externalFeatureWeightsDir == null)
				{
					File f = File.CreateTempFile("tempfeat", ".txt");
					f.Delete();
					f.DeleteOnExit();
					constVars.externalFeatureWeightsDir = f.GetAbsolutePath();
				}
				IOUtils.EnsureDir(new File(constVars.externalFeatureWeightsDir));
				foreach (string label_1 in seedSets.Keys)
				{
					string externalFeatureWeightsFileLabel = constVars.externalFeatureWeightsDir + "/" + label_1;
					File f = new File(externalFeatureWeightsFileLabel);
					if (!f.Exists())
					{
						Redwood.Log(Redwood.Dbg, "externalweightsfile for the label " + label_1 + " does not exist: learning weights!");
						LearnImportantFeatures lmf = new LearnImportantFeatures();
						ArgumentParser.FillOptions(lmf, props);
						lmf.answerClass = answerClass[label_1];
						lmf.answerLabel = label_1;
						lmf.SetUp();
						lmf.GetTopFeatures(new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents), constVars.perSelectRand, constVars.perSelectNeg, externalFeatureWeightsFileLabel);
					}
					ICounter<int> distSimWeightsLabel = new ClassicCounter<int>();
					foreach (string line in IOUtils.ReadLines(externalFeatureWeightsFileLabel))
					{
						string[] t = line.Split(":");
						if (!t[0].StartsWith("Cluster"))
						{
							continue;
						}
						string s = t[0].Replace("Cluster-", string.Empty);
						int clusterNum = System.Convert.ToInt32(s);
						distSimWeightsLabel.SetCount(clusterNum, double.ParseDouble(t[1]));
					}
					constVars.distSimWeights[label_1] = distSimWeightsLabel;
				}
			}
			// computing semantic odds values
			if (constVars.usePatternEvalSemanticOdds || constVars.usePhraseEvalSemanticOdds)
			{
				ICounter<CandidatePhrase> dictOddsWeightsLabel = new ClassicCounter<CandidatePhrase>();
				ICounter<CandidatePhrase> otherSemanticClassFreq = new ClassicCounter<CandidatePhrase>();
				foreach (CandidatePhrase s in constVars.GetOtherSemanticClassesWords())
				{
					foreach (string s1 in StringUtils.GetNgrams(Arrays.AsList(s.GetPhrase().Split("\\s+")), 1, PatternFactory.numWordsCompoundMax))
					{
						otherSemanticClassFreq.IncrementCount(CandidatePhrase.CreateOrGet(s1));
					}
				}
				otherSemanticClassFreq = Counters.Add(otherSemanticClassFreq, 1.0);
				// otherSemanticClassFreq.setDefaultReturnValue(1.0);
				IDictionary<string, ICounter<CandidatePhrase>> labelDictNgram = new Dictionary<string, ICounter<CandidatePhrase>>();
				foreach (string label_1 in seedSets.Keys)
				{
					ICounter<CandidatePhrase> classFreq = new ClassicCounter<CandidatePhrase>();
					foreach (CandidatePhrase s_1 in seedSets[label_1])
					{
						foreach (string s1 in StringUtils.GetNgrams(Arrays.AsList(s_1.GetPhrase().Split("\\s+")), 1, PatternFactory.numWordsCompoundMax))
						{
							classFreq.IncrementCount(CandidatePhrase.CreateOrGet(s1));
						}
					}
					classFreq = Counters.Add(classFreq, 1.0);
					labelDictNgram[label_1] = classFreq;
				}
				// classFreq.setDefaultReturnValue(1.0);
				foreach (string label_2 in seedSets.Keys)
				{
					ICounter<CandidatePhrase> otherLabelFreq = new ClassicCounter<CandidatePhrase>();
					foreach (string label2 in seedSets.Keys)
					{
						if (label_2.Equals(label2))
						{
							continue;
						}
						otherLabelFreq.AddAll(labelDictNgram[label2]);
					}
					otherLabelFreq.AddAll(otherSemanticClassFreq);
					dictOddsWeightsLabel = Counters.DivisionNonNaN(labelDictNgram[label_2], otherLabelFreq);
					constVars.dictOddsWeights[label_2] = dictOddsWeightsLabel;
				}
			}
		}

		//Redwood.log(Redwood.DBG, "All options are:" + "\n" + Maps.toString(getAllOptions(), "","","\t","\n"));
		public virtual PatternsForEachToken GetPatsForEachToken()
		{
			return patsForEachToken;
		}

		/// <summary>If a token is labeled for two or more labels, then keep the one that has the longest matching phrase.</summary>
		/// <remarks>
		/// If a token is labeled for two or more labels, then keep the one that has the longest matching phrase. For example, "lung" as BODYPART label and "lung cancer" as DISEASE label,
		/// keep only the DISEASE label for "lung". For this to work, you need to have
		/// <c>PatternsAnnotations.Ln</c>
		/// set, which is already done in runLabelSeedWords function.
		/// </remarks>
		private void RemoveOverLappingLabels(IDictionary<string, DataInstance> sents)
		{
			foreach (KeyValuePair<string, DataInstance> sentEn in sents)
			{
				foreach (CoreLabel l in sentEn.Value.GetTokens())
				{
					IDictionary<string, CandidatePhrase> longestMatchingMap = l.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel));
					string longestMatchingString = string.Empty;
					string longestMatchingLabel = null;
					foreach (KeyValuePair<string, CandidatePhrase> en in longestMatchingMap)
					{
						if (en.Value.GetPhrase().Length > longestMatchingString.Length)
						{
							longestMatchingLabel = en.Key;
							longestMatchingString = en.Value.GetPhrase();
						}
					}
					if (longestMatchingLabel != null)
					{
						if (!"OTHERSEM".Equals(longestMatchingLabel))
						{
							l.Set(typeof(PatternsAnnotations.OtherSemanticLabel), constVars.backgroundSymbol);
						}
						foreach (KeyValuePair<string, Type> en_1 in constVars.GetAnswerClass())
						{
							if (!en_1.Key.Equals(longestMatchingLabel))
							{
								l.Set(en_1.Value, constVars.backgroundSymbol);
							}
							else
							{
								l.Set(en_1.Value, en_1.Key);
							}
						}
					}
				}
			}
		}

		public static IDictionary<string, DataInstance> RunPOSNERParseOnTokens(IDictionary<string, DataInstance> sents, Properties propsoriginal)
		{
			PatternFactory.PatternType type = PatternFactory.PatternType.ValueOf(propsoriginal.GetProperty(GetPatternsFromDataMultiClass.Flags.patternType));
			Properties props = new Properties();
			IList<string> anns = new List<string>();
			anns.Add("pos");
			anns.Add("lemma");
			bool useTargetParserParentRestriction = bool.ParseBoolean(propsoriginal.GetProperty(GetPatternsFromDataMultiClass.Flags.useTargetParserParentRestriction));
			bool useTargetNERRestriction = bool.ParseBoolean(propsoriginal.GetProperty(GetPatternsFromDataMultiClass.Flags.useTargetNERRestriction));
			string posModelPath = props.GetProperty(GetPatternsFromDataMultiClass.Flags.posModelPath);
			string numThreads = propsoriginal.GetProperty(GetPatternsFromDataMultiClass.Flags.numThreads);
			if (useTargetParserParentRestriction)
			{
				anns.Add("parse");
			}
			else
			{
				if (type.Equals(PatternFactory.PatternType.Dep))
				{
					anns.Add("depparse");
				}
			}
			if (useTargetNERRestriction)
			{
				anns.Add("ner");
			}
			props.SetProperty("annotators", StringUtils.Join(anns, ","));
			props.SetProperty("parse.maxlen", "80");
			props.SetProperty("nthreads", numThreads);
			props.SetProperty("threads", numThreads);
			// props.put( "tokenize.options",
			// "ptb3Escaping=false,normalizeParentheses=false,escapeForwardSlashAsterisk=false");
			if (posModelPath != null)
			{
				props.SetProperty("pos.model", posModelPath);
			}
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props, false);
			Redwood.Log(Redwood.Dbg, "Annotating text");
			foreach (KeyValuePair<string, DataInstance> en in sents)
			{
				IList<ICoreMap> temp = new List<ICoreMap>();
				ICoreMap s = new ArrayCoreMap();
				s.Set(typeof(CoreAnnotations.TokensAnnotation), en.Value.GetTokens());
				temp.Add(s);
				Annotation doc = new Annotation(temp);
				try
				{
					pipeline.Annotate(doc);
					if (useTargetParserParentRestriction)
					{
						InferParentParseTag(s.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
					}
				}
				catch (Exception e)
				{
					log.Warn("Ignoring error: for sentence  " + StringUtils.JoinWords(en.Value.GetTokens(), " "));
					log.Warn(e);
				}
			}
			Redwood.Log(Redwood.Dbg, "Done annotating text");
			return sents;
		}

		public static IDictionary<string, DataInstance> RunPOSNEROnTokens(IList<ICoreMap> sentsCM, string posModelPath, bool useTargetNERRestriction, string prefix, bool useTargetParserParentRestriction, string numThreads, PatternFactory.PatternType
			 type)
		{
			Annotation doc = new Annotation(sentsCM);
			Properties props = new Properties();
			IList<string> anns = new List<string>();
			anns.Add("pos");
			anns.Add("lemma");
			if (useTargetParserParentRestriction)
			{
				anns.Add("parse");
			}
			else
			{
				if (type.Equals(PatternFactory.PatternType.Dep))
				{
					anns.Add("depparse");
				}
			}
			if (useTargetNERRestriction)
			{
				anns.Add("ner");
			}
			props.SetProperty("annotators", StringUtils.Join(anns, ","));
			props.SetProperty("parse.maxlen", "80");
			props.SetProperty("nthreads", numThreads);
			props.SetProperty("threads", numThreads);
			// props.put( "tokenize.options",
			// "ptb3Escaping=false,normalizeParentheses=false,escapeForwardSlashAsterisk=false");
			if (posModelPath != null)
			{
				props.SetProperty("pos.model", posModelPath);
			}
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props, false);
			Redwood.Log(Redwood.Dbg, "Annotating text");
			pipeline.Annotate(doc);
			Redwood.Log(Redwood.Dbg, "Done annotating text");
			IDictionary<string, DataInstance> sents = new Dictionary<string, DataInstance>();
			foreach (ICoreMap s in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				if (useTargetParserParentRestriction)
				{
					InferParentParseTag(s.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
				}
				DataInstance d = DataInstance.GetNewInstance(type, s);
				sents[prefix + s.Get(typeof(CoreAnnotations.DocIDAnnotation))] = d;
			}
			return sents;
		}

		internal static StanfordCoreNLP pipeline = null;

		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.IO.IOException"/>
		public static int Tokenize(IEnumerator<string> textReader, string posModelPath, bool lowercase, bool useTargetNERRestriction, string sentIDPrefix, bool useTargetParserParentRestriction, string numThreads, bool batchProcessSents, int numMaxSentencesPerBatchFile
			, File saveSentencesSerDirFile, IDictionary<string, DataInstance> sents, int numFilesTillNow, PatternFactory.PatternType type)
		{
			if (pipeline == null)
			{
				Properties props = new Properties();
				IList<string> anns = new List<string>();
				anns.Add("tokenize");
				anns.Add("ssplit");
				anns.Add("pos");
				anns.Add("lemma");
				if (useTargetParserParentRestriction)
				{
					anns.Add("parse");
				}
				if (type.Equals(PatternFactory.PatternType.Dep))
				{
					anns.Add("depparse");
				}
				if (useTargetNERRestriction)
				{
					anns.Add("ner");
				}
				props.SetProperty("annotators", StringUtils.Join(anns, ","));
				props.SetProperty("parse.maxlen", "80");
				if (numThreads != null)
				{
					props.SetProperty("threads", numThreads);
				}
				props.SetProperty("tokenize.options", "ptb3Escaping=false,normalizeParentheses=false,escapeForwardSlashAsterisk=false");
				if (posModelPath != null)
				{
					props.SetProperty("pos.model", posModelPath);
				}
				pipeline = new StanfordCoreNLP(props);
			}
			string text = string.Empty;
			int numLines = 0;
			while (textReader.MoveNext())
			{
				string line = textReader.Current;
				numLines++;
				if (batchProcessSents && numLines > numMaxSentencesPerBatchFile)
				{
					break;
				}
				if (lowercase)
				{
					line = line.ToLower();
				}
				text += line + "\n";
			}
			Annotation doc = new Annotation(text);
			pipeline.Annotate(doc);
			int i = -1;
			foreach (ICoreMap s in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				i++;
				if (useTargetParserParentRestriction)
				{
					InferParentParseTag(s.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
				}
				DataInstance d = DataInstance.GetNewInstance(type, s);
				sents[sentIDPrefix + i] = d;
			}
			//      if (batchProcessSents && sents.size() >= numMaxSentencesPerBatchFile) {
			//        numFilesTillNow++;
			//        File file = new File(saveSentencesSerDirFile + "/sents_" + numFilesTillNow);
			//        IOUtils.writeObjectToFile(sents, file);
			//        sents = new HashMap<String, DataInstance>();
			//        Data.sentsFiles.add(file);
			//      }
			Redwood.Log(Redwood.Dbg, "Done annotating text with " + i + " sentences");
			if (sents.Count > 0 && batchProcessSents)
			{
				numFilesTillNow++;
				File file = new File(saveSentencesSerDirFile + "/sents_" + numFilesTillNow);
				IOUtils.WriteObjectToFile(sents, file);
				Data.sentsFiles.Add(file);
				foreach (string sentid in sents.Keys)
				{
					System.Diagnostics.Debug.Assert(!Data.sentId2File.Contains(sentid), "Data.sentId2File already contains " + sentid + ". Make sure sentIds are unique!");
					Data.sentId2File[sentid] = file;
				}
				sents.Clear();
			}
			// not lugging around sents if batch processing
			if (batchProcessSents)
			{
				sents = null;
			}
			return numFilesTillNow;
		}

		/*
		public static int tokenize(String text, String posModelPath, boolean lowercase, boolean useTargetNERRestriction, String sentIDPrefix,
		boolean useTargetParserParentRestriction, String numThreads, boolean batchProcessSents, int numMaxSentencesPerBatchFile,
		File saveSentencesSerDirFile, Map<String, DataInstance> sents, int numFilesTillNow) throws InterruptedException, ExecutionException,
		IOException {
		if (pipeline == null) {
		Properties props = new Properties();
		List<String> anns = new ArrayList<String>();
		anns.add("tokenize");
		anns.add("ssplit");
		anns.add("pos");
		anns.add("lemma");
		
		if (useTargetParserParentRestriction) {
		anns.add("parse");
		}
		if (useTargetNERRestriction) {
		anns.add("ner");
		}
		
		props.setProperty("annotators", StringUtils.join(anns, ","));
		props.setProperty("parse.maxlen", "80");
		props.setProperty("threads", numThreads);
		
		props.put("tokenize.options", "ptb3Escaping=false,normalizeParentheses=false,escapeForwardSlashAsterisk=false");
		
		if (posModelPath != null) {
		props.setProperty("pos.model", posModelPath);
		}
		pipeline = new StanfordCoreNLP(props);
		}
		if (lowercase)
		text = text.toLowerCase();
		
		Annotation doc = new Annotation(text);
		pipeline.annotate(doc);
		Redwood.log(Redwood.DBG, "Done annotating text");
		
		int i = -1;
		for (CoreMap s : doc.get(CoreAnnotations.SentencesAnnotation.class)) {
		i++;
		if (useTargetParserParentRestriction)
		inferParentParseTag(s.get(TreeAnnotation.class));
		sents.put(sentIDPrefix + i, s.get(CoreAnnotations.TokensAnnotation.class));
		if (batchProcessSents && sents.size() >= numMaxSentencesPerBatchFile) {
		numFilesTillNow++;
		File file = new File(saveSentencesSerDirFile + "/sents_" + numFilesTillNow);
		IOUtils.writeObjectToFile(sents, file);
		sents = new HashMap<String, DataInstance>();
		Data.sentsFiles.add(file);
		}
		
		}
		if (sents.size() > 0 && batchProcessSents) {
		numFilesTillNow++;
		File file = new File(saveSentencesSerDirFile + "/sents_" + numFilesTillNow);
		IOUtils.writeObjectToFile(sents, file);
		Data.sentsFiles.add(file);
		sents.clear();
		}
		// not lugging around sents if batch processing
		if (batchProcessSents)
		sents = null;
		return numFilesTillNow;
		}
		*/
		private static void InferParentParseTag(Tree tree)
		{
			string grandstr = tree.Value();
			foreach (Tree child in tree.Children())
			{
				foreach (Tree grand in child.Children())
				{
					if (grand.IsLeaf())
					{
						((CoreLabel)grand.Label()).Set(typeof(CoreAnnotations.GrandparentAnnotation), grandstr);
					}
				}
				InferParentParseTag(child);
			}
		}

		/// <summary>
		/// If l1 is a part of l2, it finds the starting index of l1 in l2 If l1 is not
		/// a sub-array of l2, then it returns -1 note that l2 should have the exact
		/// elements and order as in l1
		/// </summary>
		/// <param name="l1">array you want to find in l2</param>
		/// <param name="l2"/>
		/// <returns>starting index of the sublist</returns>
		public static IList<int> GetSubListIndex(string[] l1, string[] l2, string[] subl2, ICollection<string> doNotLabelTheseWords, HashSet<string> seenFuzzyMatches, int minLen4Fuzzy, bool fuzzyMatch, bool ignoreCaseSeedMatch)
		{
			if (l1.Length > l2.Length)
			{
				return null;
			}
			EditDistance editDistance = new EditDistance(true);
			IList<int> allIndices = new List<int>();
			bool matched = false;
			int index = -1;
			int lastUnmatchedIndex = 0;
			for (int i = 0; i < l2.Length; )
			{
				for (int j = 0; j < l1.Length; )
				{
					bool d1 = false;
					bool d2 = false;
					bool compareFuzzy = true;
					if (!fuzzyMatch || doNotLabelTheseWords.Contains(l2[i]) || doNotLabelTheseWords.Contains(subl2[i]) || l2[i].Length <= minLen4Fuzzy || subl2[i].Length <= minLen4Fuzzy)
					{
						compareFuzzy = false;
					}
					if (compareFuzzy == false || l1[j].Length <= minLen4Fuzzy)
					{
						d1 = (ignoreCaseSeedMatch && Sharpen.Runtime.EqualsIgnoreCase(l1[j], l2[i])) || l1[j].Equals(l2[i]);
						if (!d1 && fuzzyMatch)
						{
							d2 = (ignoreCaseSeedMatch && Sharpen.Runtime.EqualsIgnoreCase(subl2[i], l1[j])) || subl2[i].Equals(l1[j]);
						}
					}
					else
					{
						string combo = l1[j] + "#" + l2[i];
						if ((ignoreCaseSeedMatch && Sharpen.Runtime.EqualsIgnoreCase(l1[j], l2[i])) || l1[j].Equals(l2[i]) || seenFuzzyMatches.Contains(combo))
						{
							d1 = true;
						}
						else
						{
							d1 = editDistance.Score(l1[j], l2[i]) <= 1;
							if (!d1)
							{
								string combo2 = l1[j] + "#" + subl2[i];
								if ((ignoreCaseSeedMatch && Sharpen.Runtime.EqualsIgnoreCase(l1[j], subl2[i])) || l1[j].Equals(subl2[i]) || seenFuzzyMatches.Contains(combo2))
								{
									d2 = true;
								}
								else
								{
									d2 = editDistance.Score(l1[j], subl2[i]) <= 1;
									if (d2)
									{
										// System.out.println(l1[j] + " matched with " + subl2[i]);
										seenFuzzyMatches.Add(combo2);
									}
								}
							}
							else
							{
								if (d1)
								{
									// System.out.println(l1[j] + " matched with " + l2[i]);
									seenFuzzyMatches.Add(combo);
								}
							}
						}
					}
					// if (l1[j].equals(l2[i]) || subl2[i].equals(l1[j])) {
					if (d1 || d2)
					{
						index = i;
						i++;
						j++;
						if (j == l1.Length)
						{
							matched = true;
							break;
						}
					}
					else
					{
						j = 0;
						i = lastUnmatchedIndex + 1;
						lastUnmatchedIndex = i;
						index = -1;
						if (lastUnmatchedIndex == l2.Length)
						{
							break;
						}
					}
					if (i >= l2.Length)
					{
						index = -1;
						break;
					}
				}
				if (i == l2.Length || matched)
				{
					if (index >= 0)
					{
						// index = index - l1.length + 1;
						allIndices.Add(index - l1.Length + 1);
					}
					matched = false;
					lastUnmatchedIndex = index;
				}
			}
			// break;
			// get starting point
			return allIndices;
		}

		private sealed class _IFunction_915 : IFunction<CoreLabel, string>
		{
			public _IFunction_915()
			{
			}

			//if matchcontextlowercase is on, transform that. escape the word etc. Useful for pattern matching later on
			public string Apply(CoreLabel l)
			{
				string s;
				if (PatternFactory.useLemmaContextTokens)
				{
					s = l.Lemma();
					System.Diagnostics.Debug.Assert(s != null, "Lemma is null and useLemmaContextTokens is true");
				}
				else
				{
					s = l.Word();
				}
				if (ConstantsAndVariables.matchLowerCaseContext)
				{
					s = s.ToLower();
				}
				System.Diagnostics.Debug.Assert(s != null);
				return s;
			}
		}

		private static IFunction<CoreLabel, string> stringTransformationFunction = new _IFunction_915();

		public static IList<IList<E>> GetThreadBatches<E>(IList<E> keyset, int numThreads)
		{
			int num;
			if (numThreads == 1)
			{
				num = keyset.Count;
			}
			else
			{
				num = keyset.Count / (numThreads - 1);
			}
			Redwood.Log(ConstantsAndVariables.extremedebug, "keyset size is " + keyset.Count);
			IList<IList<E>> threadedSentIds = new List<IList<E>>();
			for (int i = 0; i < numThreads; i++)
			{
				IList<E> keys = keyset.SubList(i * num, Math.Min(keyset.Count, (i + 1) * num));
				threadedSentIds.Add(keys);
				Redwood.Log(ConstantsAndVariables.extremedebug, "assigning from " + i * num + " till " + Math.Min(keyset.Count, (i + 1) * num));
			}
			return threadedSentIds;
		}

		/// <summary>Warning: sets labels of words that are not in the given seed set as O!!!</summary>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.IO.IOException"/>
		public static ICounter<CandidatePhrase> RunLabelSeedWords(IDictionary<string, DataInstance> sents, Type answerclass, string label, ICollection<CandidatePhrase> seedWords, ConstantsAndVariables constVars, bool overwriteExistingLabels)
		{
			Redwood.Log(Redwood.Dbg, "ignoreCaseSeedMatch is " + constVars.ignoreCaseSeedMatch);
			IList<IList<string>> threadedSentIds = GetThreadBatches(new List<string>(sents.Keys), constVars.numThreads);
			IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
			IList<IFuture<Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>>>> list = new List<IFuture<Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>>>>();
			ICounter<CandidatePhrase> matchedPhrasesCounter = new ClassicCounter<CandidatePhrase>();
			foreach (IList<string> keys in threadedSentIds)
			{
				ICallable<Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>>> task = new GetPatternsFromDataMultiClass.LabelWithSeedWords(seedWords, sents, keys, answerclass, label, constVars.fuzzyMatch, constVars.minLen4FuzzyForPattern, constVars
					.backgroundSymbol, constVars.GetEnglishWords(), stringTransformationFunction, constVars.writeMatchedTokensIdsForEachPhrase, overwriteExistingLabels, constVars.patternType, constVars.ignoreCaseSeedMatch);
				Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>> sentsi = executor.Submit(task).Get();
				sents.PutAll(sentsi.First());
				matchedPhrasesCounter.AddAll(sentsi.Second());
			}
			executor.Shutdown();
			Redwood.Log("extremedebug", "Matched phrases freq is " + matchedPhrasesCounter);
			return matchedPhrasesCounter;
		}

		public static void GetFeatures(SemanticGraph graph, IndexedWord vertex, bool isHead, ICollection<string> features, GrammaticalRelation reln)
		{
			if (isHead)
			{
				IList<Pair<GrammaticalRelation, IndexedWord>> pt = graph.ParentPairs(vertex);
				foreach (Pair<GrammaticalRelation, IndexedWord> en in pt)
				{
					features.Add("PARENTREL-" + en.First());
				}
			}
			else
			{
				//find the relation to the parent
				if (reln == null)
				{
					IList<SemanticGraphEdge> parents = graph.GetOutEdgesSorted(vertex);
					if (parents.Count > 0)
					{
						reln = parents[0].GetRelation();
					}
				}
				if (reln != null)
				{
					features.Add("REL-" + reln.GetShortName());
				}
			}
		}

		/// <summary>Warning: sets labels of words that are not in the given seed set as O!!!</summary>
		public class LabelWithSeedWords : ICallable<Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>>>
		{
			internal IDictionary<CandidatePhrase, string[]> seedwordsTokens = new Dictionary<CandidatePhrase, string[]>();

			internal IDictionary<string, DataInstance> sents;

			internal IList<string> keyset;

			internal Type labelClass;

			internal HashSet<string> seenFuzzyMatches = new HashSet<string>();

			internal string label;

			internal int minLen4FuzzyForPattern;

			internal string backgroundSymbol = "O";

			internal ICollection<string> doNotLabelDictWords = null;

			internal IFunction<CoreLabel, string> stringTransformation;

			internal bool writeMatchedTokensIdsForEachPhrase = false;

			internal bool overwriteExistingLabels;

			internal PatternFactory.PatternType patternType;

			internal bool fuzzyMatch = false;

			internal IDictionary<string, string> ignoreCaseSeedMatch;

			public LabelWithSeedWords(ICollection<CandidatePhrase> seedwords, IDictionary<string, DataInstance> sents, IList<string> keyset, Type labelclass, string label, bool fuzzyMatch, int minLen4FuzzyForPattern, string backgroundSymbol, ICollection
				<string> doNotLabelDictWords, IFunction<CoreLabel, string> stringTransformation, bool writeMatchedTokensIdsForEachPhrase, bool overwriteExistingLabels, PatternFactory.PatternType type, IDictionary<string, string> ignoreCaseSeedMatch)
			{
				//System.out.println("For graph " + graph.toFormattedString() + " and vertex " + vertex + " the features are " + features);
				foreach (CandidatePhrase s in seedwords)
				{
					this.seedwordsTokens[s] = s.GetPhrase().Split("\\s+");
				}
				this.sents = sents;
				this.keyset = keyset;
				this.labelClass = labelclass;
				this.label = label;
				this.minLen4FuzzyForPattern = minLen4FuzzyForPattern;
				this.backgroundSymbol = backgroundSymbol;
				this.doNotLabelDictWords = doNotLabelDictWords;
				this.stringTransformation = stringTransformation;
				this.writeMatchedTokensIdsForEachPhrase = writeMatchedTokensIdsForEachPhrase;
				this.overwriteExistingLabels = overwriteExistingLabels;
				this.patternType = type;
				this.fuzzyMatch = fuzzyMatch;
				this.ignoreCaseSeedMatch = ignoreCaseSeedMatch;
			}

			public virtual Pair<IDictionary<string, DataInstance>, ICounter<CandidatePhrase>> Call()
			{
				IDictionary<string, DataInstance> newsent = new Dictionary<string, DataInstance>();
				ICounter<CandidatePhrase> matchedPhrasesCounter = new ClassicCounter<CandidatePhrase>();
				foreach (string k in keyset)
				{
					DataInstance sent = sents[k];
					IList<CoreLabel> tokensCore = sent.GetTokens();
					SemanticGraph graph = null;
					if (patternType.Equals(PatternFactory.PatternType.Dep))
					{
						graph = ((DataInstanceDep)sent).GetGraph();
					}
					string[] tokens = new string[tokensCore.Count];
					string[] tokenslemma = new string[tokensCore.Count];
					int num = 0;
					foreach (CoreLabel l in tokensCore)
					{
						//Setting the processedTextAnnotation, used in indexing and pattern matching
						l.Set(typeof(PatternsAnnotations.ProcessedTextAnnotation), stringTransformation.Apply(l));
						tokens[num] = l.Word();
						if (fuzzyMatch && l.Lemma() == null)
						{
							throw new Exception("how come lemma is null");
						}
						tokenslemma[num] = l.Lemma();
						num++;
					}
					bool[] labels = new bool[tokens.Length];
					CollectionValuedMap<int, CandidatePhrase> matchedPhrases = new CollectionValuedMap<int, CandidatePhrase>();
					IDictionary<int, CandidatePhrase> longestMatchedPhrases = new Dictionary<int, CandidatePhrase>();
					foreach (KeyValuePair<CandidatePhrase, string[]> sEn in seedwordsTokens)
					{
						string[] s = sEn.Value;
						CandidatePhrase sc = sEn.Key;
						IList<int> indices = GetSubListIndex(s, tokens, tokenslemma, doNotLabelDictWords, seenFuzzyMatches, minLen4FuzzyForPattern, fuzzyMatch, (ignoreCaseSeedMatch.Contains(label) ? bool.ValueOf(ignoreCaseSeedMatch[label]) : false));
						if (indices != null && !indices.IsEmpty())
						{
							string ph = StringUtils.Join(s, " ");
							sc.AddFeature("LENGTH-" + s.Length, 1.0);
							ICollection<string> features = new List<string>();
							foreach (int index in indices)
							{
								if (graph != null)
								{
									GetPatternsFromDataMultiClass.GetFeatures(graph, graph.GetNodeByIndex(index + 1), true, features, null);
								}
								if (writeMatchedTokensIdsForEachPhrase)
								{
									AddToMatchedTokensByPhrase(ph, k, index, s.Length);
								}
								for (int i = 0; i < s.Length; i++)
								{
									matchedPhrases.Add(index + i, sc);
									if (graph != null)
									{
										try
										{
											GetPatternsFromDataMultiClass.GetFeatures(graph, graph.GetNodeByIndex(index + i + 1), false, features, null);
										}
										catch (Exception e)
										{
											log.Warn(e);
										}
									}
									CandidatePhrase longPh = longestMatchedPhrases[index + i];
									longPh = longPh != null && longPh.GetPhrase().Length > sc.GetPhrase().Length ? longPh : sc;
									longestMatchedPhrases[index + i] = longPh;
									labels[index + i] = true;
								}
							}
							sc.AddFeatures(features);
						}
					}
					int i_1 = -1;
					foreach (CoreLabel l_1 in sent.GetTokens())
					{
						i_1++;
						//The second clause is for old sents ser files compatibility reason
						if (!l_1.ContainsKey(typeof(PatternsAnnotations.MatchedPhrases)) || !(typeof(PatternsAnnotations.MatchedPhrases).IsInstanceOfType(l_1.Get(typeof(PatternsAnnotations.MatchedPhrases)))))
						{
							l_1.Set(typeof(PatternsAnnotations.MatchedPhrases), new CollectionValuedMap<string, CandidatePhrase>());
						}
						if (!l_1.ContainsKey(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel)))
						{
							l_1.Set(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel), new Dictionary<string, CandidatePhrase>());
						}
						if (labels[i_1])
						{
							l_1.Set(labelClass, label);
							//set whether labeled by the seeds or not
							if (!l_1.ContainsKey(typeof(PatternsAnnotations.SeedLabeledOrNot)))
							{
								l_1.Set(typeof(PatternsAnnotations.SeedLabeledOrNot), new Dictionary<Type, bool>());
							}
							l_1.Get(typeof(PatternsAnnotations.SeedLabeledOrNot))[labelClass] = true;
							CandidatePhrase longestMatchingPh = l_1.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel))[label];
							System.Diagnostics.Debug.Assert(longestMatchedPhrases.Contains(i_1));
							longestMatchingPh = (longestMatchingPh != null && (longestMatchingPh.GetPhrase().Length > longestMatchedPhrases[i_1].GetPhrase().Length)) ? longestMatchingPh : longestMatchedPhrases[i_1];
							l_1.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel))[label] = longestMatchingPh;
							matchedPhrasesCounter.IncrementCount(longestMatchingPh, 1.0);
							l_1.Get(typeof(PatternsAnnotations.MatchedPhrases)).AddAll(label, matchedPhrases[i_1]);
							Redwood.Log(ConstantsAndVariables.extremedebug, "labeling " + l_1.Word() + " or its lemma " + l_1.Lemma() + " as " + label + " because of the dict phrases " + matchedPhrases[i_1]);
						}
						else
						{
							if (overwriteExistingLabels)
							{
								l_1.Set(labelClass, backgroundSymbol);
							}
						}
					}
					newsent[k] = sent;
				}
				return new Pair(newsent, matchedPhrasesCounter);
			}
		}

		private static void AddToMatchedTokensByPhrase(string ph, string sentid, int index, int length)
		{
			if (!Data.matchedTokensForEachPhrase.Contains(ph))
			{
				Data.matchedTokensForEachPhrase[ph] = new Dictionary<string, IList<int>>();
			}
			IDictionary<string, IList<int>> matcheds = Data.matchedTokensForEachPhrase[ph];
			if (!matcheds.Contains(sentid))
			{
				matcheds[sentid] = new List<int>();
			}
			for (int i = 0; i < length; i++)
			{
				matcheds[sentid].Add(index + i);
			}
		}

		public IDictionary<string, TwoDimensionalCounter<E, CandidatePhrase>> patternsandWords = null;

		public IDictionary<string, ICounter<E>> currentPatternWeights = null;

		//public Map<String, TwoDimensionalCounter<E, String>> allPatternsandWords = null;
		//deleteExistingIndex is def false for the second call to this function
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void ProcessSents(IDictionary<string, DataInstance> sents, bool deleteExistingIndex)
		{
			if (constVars.computeAllPatterns)
			{
				props.SetProperty("createTable", deleteExistingIndex.ToString());
				props.SetProperty("deleteExisting", deleteExistingIndex.ToString());
				props.SetProperty("createPatLuceneIndex", deleteExistingIndex.ToString());
				Redwood.Log(Redwood.Dbg, "Computing all patterns");
				createPats.GetAllPatterns(sents, props, constVars.storePatsForEachToken);
			}
			else
			{
				Redwood.Log(Redwood.Dbg, "Reading patterns from existing dir");
			}
			props.SetProperty("createTable", "false");
			props.SetProperty("deleteExisting", "false");
			props.SetProperty("createPatLuceneIndex", "false");
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadSavedPatternsAndIndex()
		{
			if (!constVars.computeAllPatterns)
			{
				System.Diagnostics.Debug.Assert(constVars.allPatternsDir != null, "allPatternsDir flag cannot be empty if computeAllPatterns is false!");
				//constVars.setPatternIndex(PatternIndex.load(constVars.allPatternsDir, constVars.storePatsIndex));
				if (constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Memory))
				{
					patsForEachToken.Load(constVars.allPatternsDir);
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual ICounter<E> GetPatterns(string label, ICollection<E> alreadyIdentifiedPatterns, E p0, ICounter<CandidatePhrase> p0Set, ICollection<E> ignorePatterns)
		{
			TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();
			TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();
			//TwoDimensionalCounter<E, String> posnegPatternsandWords4Label = new TwoDimensionalCounter<E, String>();
			TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();
			//TwoDimensionalCounter<E, String> negandUnLabeledPatternsandWords4Label = new TwoDimensionalCounter<E, String>();
			//TwoDimensionalCounter<E, String> allPatternsandWords4Label = new TwoDimensionalCounter<E, String>();
			ICollection<string> allCandidatePhrases = new HashSet<string>();
			ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
			bool firstCallToProcessSents = true;
			while (sentsIter.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sentsPair = sentsIter.Current;
				if (notComputedAllPatternsYet)
				{
					//in the first iteration
					ProcessSents(sentsPair.First(), firstCallToProcessSents);
					firstCallToProcessSents = false;
					if (patsForEachToken == null)
					{
						//in the first iteration, for the first file
						patsForEachToken = PatternsForEachToken.GetPatternsInstance(props, constVars.storePatsForEachToken);
						ReadSavedPatternsAndIndex();
					}
				}
				this.CalculateSufficientStats(sentsPair.First(), patsForEachToken, label, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, allCandidatePhrases);
			}
			notComputedAllPatternsYet = false;
			if (constVars.computeAllPatterns)
			{
				if (constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Db))
				{
					patsForEachToken.CreateIndexIfUsingDBAndNotExists();
				}
				//        String systemdir = System.getProperty("java.io.tmpdir");
				//        File tempFile= File.createTempFile("patterns", ".tmp", new File(systemdir));
				//        tempFile.deleteOnExit();
				//        tempFile.delete();
				//        constVars.allPatternsDir = tempFile.getAbsolutePath();
				if (constVars.allPatternsDir != null)
				{
					IOUtils.EnsureDir(new File(constVars.allPatternsDir));
					patsForEachToken.Save(constVars.allPatternsDir);
				}
			}
			//savePatternIndex(constVars.allPatternsDir);
			patsForEachToken.Close();
			//This is important. It makes sure that we don't recompute patterns in every iteration!
			constVars.computeAllPatterns = false;
			if (patternsandWords == null)
			{
				patternsandWords = new Dictionary<string, TwoDimensionalCounter<E, CandidatePhrase>>();
			}
			if (currentPatternWeights == null)
			{
				currentPatternWeights = new Dictionary<string, ICounter<E>>();
			}
			ICounter<E> currentPatternWeights4Label = new ClassicCounter<E>();
			ICollection<E> removePats = EnforceMinSupportRequirements(patternsandWords4Label, unLabeledPatternsandWords4Label);
			Counters.RemoveKeys(patternsandWords4Label, removePats);
			Counters.RemoveKeys(unLabeledPatternsandWords4Label, removePats);
			Counters.RemoveKeys(negPatternsandWords4Label, removePats);
			ScorePatterns scorePatterns;
			Type patternscoringclass = GetPatternScoringClass(constVars.patternScoring);
			if (patternscoringclass != null && patternscoringclass.Equals(typeof(ScorePatternsF1)))
			{
				scorePatterns = new ScorePatternsF1(constVars, constVars.patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, props, p0Set, p0);
				ICounter<E> finalPat = scorePatterns.Score();
				Counters.RemoveKeys(finalPat, alreadyIdentifiedPatterns);
				Counters.RetainNonZeros(finalPat);
				Counters.RetainTop(finalPat, constVars.numPatterns);
				if (double.IsNaN(Counters.Max(finalPat)))
				{
					throw new Exception("how is the value NaN");
				}
				Redwood.Log(ConstantsAndVariables.minimaldebug, "Selected Patterns: " + finalPat);
				return finalPat;
			}
			else
			{
				if (patternscoringclass != null && patternscoringclass.Equals(typeof(ScorePatternsRatioModifiedFreq)))
				{
					scorePatterns = new ScorePatternsRatioModifiedFreq(constVars, constVars.patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, phInPatScoresCache, scorePhrases, props);
				}
				else
				{
					if (patternscoringclass != null && patternscoringclass.Equals(typeof(ScorePatternsFreqBased)))
					{
						scorePatterns = new ScorePatternsFreqBased(constVars, constVars.patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, props);
					}
					else
					{
						if (constVars.patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.kNN))
						{
							try
							{
								Type clazz = (Type)Sharpen.Runtime.GetType("edu.stanford.nlp.patterns.ScorePatternsKNN");
								Constructor<ScorePatterns> ctor = clazz.GetConstructor(typeof(ConstantsAndVariables), typeof(GetPatternsFromDataMultiClass.PatternScoring), typeof(string), typeof(ISet), typeof(TwoDimensionalCounter), typeof(TwoDimensionalCounter), typeof(TwoDimensionalCounter
									), typeof(ScorePhrases), typeof(Properties));
								scorePatterns = ctor.NewInstance(constVars, constVars.patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, scorePhrases, props);
							}
							catch (TypeLoadException)
							{
								throw new Exception("kNN pattern scoring is not released yet. Stay tuned.");
							}
							catch (ReflectiveOperationException e)
							{
								throw new Exception("newinstance of kNN not created", e);
							}
						}
						else
						{
							throw new Exception(constVars.patternScoring + " is not implemented (check spelling?). ");
						}
					}
				}
			}
			scorePatterns.SetUp(props);
			currentPatternWeights4Label = scorePatterns.Score();
			Redwood.Log(ConstantsAndVariables.extremedebug, "patterns counter size is " + currentPatternWeights4Label.Size());
			if (ignorePatterns != null && !ignorePatterns.IsEmpty())
			{
				Counters.RemoveKeys(currentPatternWeights4Label, ignorePatterns);
				Redwood.Log(ConstantsAndVariables.extremedebug, "Removing patterns from ignorePatterns of size  " + ignorePatterns.Count + ". New patterns size " + currentPatternWeights4Label.Size());
			}
			if (alreadyIdentifiedPatterns != null && !alreadyIdentifiedPatterns.IsEmpty())
			{
				Redwood.Log(ConstantsAndVariables.extremedebug, "Patterns size is " + currentPatternWeights4Label.Size());
				Counters.RemoveKeys(currentPatternWeights4Label, alreadyIdentifiedPatterns);
				Redwood.Log(ConstantsAndVariables.extremedebug, "Removing already identified patterns of size  " + alreadyIdentifiedPatterns.Count + ". New patterns size " + currentPatternWeights4Label.Size());
			}
			IPriorityQueue<E> q = Counters.ToPriorityQueue(currentPatternWeights4Label);
			int num = 0;
			ICounter<E> chosenPat = new ClassicCounter<E>();
			ICollection<E> removePatterns = new HashSet<E>();
			ICollection<E> removeIdentifiedPatterns = null;
			while (num < constVars.numPatterns && !q.IsEmpty())
			{
				E pat = q.RemoveFirst();
				//E pat = constVars.getPatternIndex().get(patindex);
				if (currentPatternWeights4Label.GetCount(pat) < constVars.thresholdSelectPattern)
				{
					Redwood.Log(Redwood.Dbg, "The max weight of candidate patterns is " + df.Format(currentPatternWeights4Label.GetCount(pat)) + " so not adding anymore patterns");
					break;
				}
				bool notchoose = false;
				if (!unLabeledPatternsandWords4Label.ContainsFirstKey(pat) || unLabeledPatternsandWords4Label.GetCounter(pat).IsEmpty())
				{
					Redwood.Log(ConstantsAndVariables.extremedebug, "Removing pattern " + pat + " because it has no unlab support; pos words: " + patternsandWords4Label.GetCounter(pat));
					notchoose = true;
					continue;
				}
				ICollection<E> removeChosenPats = null;
				if (!notchoose)
				{
					if (alreadyIdentifiedPatterns != null)
					{
						foreach (E p in alreadyIdentifiedPatterns)
						{
							if (Pattern.Subsumes(constVars.patternType, pat, p))
							{
								// if (pat.getNextContextStr().contains(p.getNextContextStr()) &&
								// pat.getPrevContextStr().contains(p.getPrevContextStr())) {
								Redwood.Log(ConstantsAndVariables.extremedebug, "Not choosing pattern " + pat + " because it is contained in or contains the already chosen pattern " + p);
								notchoose = true;
								break;
							}
							int rest = pat.EqualContext(p);
							// the contexts dont match
							if (rest == int.MaxValue)
							{
								continue;
							}
							// if pat is less restrictive, remove p and add pat!
							if (rest < 0)
							{
								if (removeIdentifiedPatterns == null)
								{
									removeIdentifiedPatterns = new HashSet<E>();
								}
								removeIdentifiedPatterns.Add(p);
							}
							else
							{
								notchoose = true;
								break;
							}
						}
					}
				}
				// In this iteration:
				if (!notchoose)
				{
					foreach (Pattern p in chosenPat.KeySet())
					{
						//E p = constVars.getPatternIndex().get(pindex);
						bool removeChosenPatFlag = false;
						if (Pattern.SameGenre(constVars.patternType, pat, p))
						{
							if (Pattern.Subsumes(constVars.patternType, pat, p))
							{
								Redwood.Log(ConstantsAndVariables.extremedebug, "Not choosing pattern " + pat + " because it is contained in or contains the already chosen pattern " + p);
								notchoose = true;
								break;
							}
							else
							{
								if (E.Subsumes(constVars.patternType, p, pat))
								{
									//subsume is true even if equal context
									//check if equal context
									int rest = pat.EqualContext(p);
									// the contexts do not match
									if (rest == int.MaxValue)
									{
										Redwood.Log(ConstantsAndVariables.extremedebug, "Not choosing pattern " + p + " because it is contained in or contains another chosen pattern in this iteration " + pat);
										removeChosenPatFlag = true;
									}
									else
									{
										// if pat is less restrictive, remove p from chosen patterns and
										// add pat!
										if (rest < 0)
										{
											removeChosenPatFlag = true;
										}
										else
										{
											notchoose = true;
											break;
										}
									}
								}
							}
							if (removeChosenPatFlag)
							{
								if (removeChosenPats == null)
								{
									removeChosenPats = new HashSet<E>();
								}
								removeChosenPats.Add(pat);
								num--;
							}
						}
					}
				}
				if (notchoose)
				{
					Redwood.Log(Redwood.Dbg, "Not choosing " + pat + " for whatever reason!");
					continue;
				}
				if (removeChosenPats != null)
				{
					Redwood.Log(ConstantsAndVariables.extremedebug, "Removing already chosen patterns in this iteration " + removeChosenPats + " in favor of " + pat);
					Counters.RemoveKeys(chosenPat, removeChosenPats);
				}
				if (removeIdentifiedPatterns != null)
				{
					Redwood.Log(ConstantsAndVariables.extremedebug, "Removing already identified patterns " + removeIdentifiedPatterns + " in favor of " + pat);
					Sharpen.Collections.AddAll(removePatterns, removeIdentifiedPatterns);
				}
				chosenPat.SetCount(pat, currentPatternWeights4Label.GetCount(pat));
				num++;
			}
			this.RemoveLearnedPatterns(label, removePatterns);
			Redwood.Log(Redwood.Dbg, "final size of the patterns is " + chosenPat.Size());
			Redwood.Log(ConstantsAndVariables.minimaldebug, "\n\n## Selected Patterns for " + label + "##\n");
			IList<Pair<E, double>> chosenPatSorted = Counters.ToSortedListWithCounts(chosenPat);
			foreach (Pair<E, double> en in chosenPatSorted)
			{
				Redwood.Log(ConstantsAndVariables.minimaldebug, en.First() + ":" + df.Format(en.second) + "\n");
			}
			if (constVars.outDir != null && !constVars.outDir.IsEmpty())
			{
				CollectionValuedMap<E, CandidatePhrase> posWords = new CollectionValuedMap<E, CandidatePhrase>();
				foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_1 in patternsandWords4Label.EntrySet())
				{
					posWords.AddAll(en_1.Key, en_1.Value.KeySet());
				}
				CollectionValuedMap<E, CandidatePhrase> negWords = new CollectionValuedMap<E, CandidatePhrase>();
				foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_2 in negPatternsandWords4Label.EntrySet())
				{
					negWords.AddAll(en_2.Key, en_2.Value.KeySet());
				}
				CollectionValuedMap<E, CandidatePhrase> unlabWords = new CollectionValuedMap<E, CandidatePhrase>();
				foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_3 in unLabeledPatternsandWords4Label.EntrySet())
				{
					unlabWords.AddAll(en_3.Key, en_3.Value.KeySet());
				}
				if (constVars.outDir != null)
				{
					string outputdir = constVars.outDir + "/" + constVars.identifier + "/" + label;
					Redwood.Log(ConstantsAndVariables.minimaldebug, "Saving output in " + outputdir);
					IOUtils.EnsureDir(new File(outputdir));
					string filename = outputdir + "/patterns" + ".json";
					IJsonArrayBuilder obj = Javax.Json.Json.CreateArrayBuilder();
					if (writtenPatInJustification.Contains(label) && writtenPatInJustification[label])
					{
						IJsonReader jsonReader = Javax.Json.Json.CreateReader(new BufferedInputStream(new FileInputStream(filename)));
						IJsonArray objarr = jsonReader.ReadArray();
						jsonReader.Close();
						foreach (IJsonValue o in objarr)
						{
							obj.Add(o);
						}
					}
					else
					{
						obj = Javax.Json.Json.CreateArrayBuilder();
					}
					IJsonObjectBuilder objThisIter = Javax.Json.Json.CreateObjectBuilder();
					foreach (Pair<E, double> pat in chosenPatSorted)
					{
						IJsonObjectBuilder o = Javax.Json.Json.CreateObjectBuilder();
						IJsonArrayBuilder pos = Javax.Json.Json.CreateArrayBuilder();
						IJsonArrayBuilder neg = Javax.Json.Json.CreateArrayBuilder();
						IJsonArrayBuilder unlab = Javax.Json.Json.CreateArrayBuilder();
						foreach (CandidatePhrase w in posWords[pat.First()])
						{
							pos.Add(w.GetPhrase());
						}
						foreach (CandidatePhrase w_1 in negWords[pat.First()])
						{
							neg.Add(w_1.GetPhrase());
						}
						foreach (CandidatePhrase w_2 in unlabWords[pat.First()])
						{
							unlab.Add(w_2.GetPhrase());
						}
						o.Add("Positive", pos);
						o.Add("Negative", neg);
						o.Add("Unlabeled", unlab);
						o.Add("Score", pat.Second());
						objThisIter.Add(pat.First().ToStringSimple(), o);
					}
					obj.Add(objThisIter.Build());
					IOUtils.EnsureDir(new File(filename).GetParentFile());
					IOUtils.WriteStringToFile(StringUtils.Normalize(StringUtils.ToAscii(obj.Build().ToString())), filename, "ASCII");
					writtenPatInJustification[label] = true;
				}
			}
			if (constVars.justify)
			{
				Redwood.Log(Redwood.Dbg, "Justification for Patterns:");
				foreach (E key in chosenPat.KeySet())
				{
					Redwood.Log(Redwood.Dbg, "\nPattern: " + key);
					Redwood.Log(Redwood.Dbg, "Positive Words:" + Counters.ToSortedString(patternsandWords4Label.GetCounter(key), patternsandWords4Label.GetCounter(key).Size(), "%1$s:%2$f", ";"));
					Redwood.Log(Redwood.Dbg, "Negative Words:" + Counters.ToSortedString(negPatternsandWords4Label.GetCounter(key), negPatternsandWords4Label.GetCounter(key).Size(), "%1$s:%2$f", ";"));
					Redwood.Log(Redwood.Dbg, "Unlabeled Words:" + Counters.ToSortedString(unLabeledPatternsandWords4Label.GetCounter(key), unLabeledPatternsandWords4Label.GetCounter(key).Size(), "%1$s:%2$f", ";"));
				}
			}
			//allPatternsandWords.put(label, allPatternsandWords4Label);
			patternsandWords[label] = patternsandWords4Label;
			currentPatternWeights[label] = currentPatternWeights4Label;
			return chosenPat;
		}

		//  private void savePatternIndex(String dir ) throws IOException {
		//    if(dir != null) {
		//      IOUtils.ensureDir(new File(dir));
		//      constVars.getPatternIndex().save(dir);
		//    }
		//    //patsForEachToken.savePatternIndex(constVars.getPatternIndex(), dir);
		//
		//  }
		public static Type GetPatternScoringClass(GetPatternsFromDataMultiClass.PatternScoring patternScoring)
		{
			if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.F1SeedPattern))
			{
				return typeof(ScorePatternsF1);
			}
			else
			{
				if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PosNegUnlabOdds) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PosNegOdds) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RatioAll
					) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPat) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPatLogP) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring
					.Logreg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LOGREGlogP) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.SqrtAllRatio))
				{
					return typeof(ScorePatternsRatioModifiedFreq);
				}
				else
				{
					if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogF) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFPosNeg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFUnlabNeg
						) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFNeg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.YanGarber02) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LinICML03
						))
					{
						return typeof(ScorePatternsFreqBased);
					}
					else
					{
						return null;
					}
				}
			}
		}

		private static AtomicInteger numCallsToCalStats = new AtomicInteger();

		private static IList<IList<E>> SplitIntoNumThreadsWithSampling<E>(IList<E> c, int n, int numThreads)
		{
			if (n < 0)
			{
				throw new ArgumentException("n < 0: " + n);
			}
			if (n > c.Count)
			{
				throw new ArgumentException("n > size of collection: " + n + ", " + c.Count);
			}
			IList<IList<E>> resultAll = new List<IList<E>>(numThreads);
			int num;
			if (numThreads == 1)
			{
				num = n;
			}
			else
			{
				num = n / (numThreads - 1);
			}
			System.Console.Out.WriteLine("shuffled " + c.Count + " sentences and selecting " + num + " sentences per thread");
			IList<E> result = new List<E>(num);
			int totalitems = 0;
			int nitem = 0;
			Random r = new Random(numCallsToCalStats.IncrementAndGet());
			bool[] added = new bool[c.Count];
			// Arrays.fill(added, false);  // not needed; get false by default
			while (totalitems < n)
			{
				//find the new sample index
				int index;
				do
				{
					index = r.NextInt(c.Count);
				}
				while (added[index]);
				added[index] = true;
				E c1 = c[index];
				if (nitem == num)
				{
					resultAll.Add(result);
					result = new List<E>(num);
					nitem = 0;
				}
				result.Add(c1);
				totalitems++;
				nitem++;
			}
			if (!result.IsEmpty())
			{
				resultAll.Add(result);
			}
			return resultAll;
		}

		//for each pattern, it calculates positive, negative, and unlabeled words
		private void CalculateSufficientStats(IDictionary<string, DataInstance> sents, PatternsForEachToken patternsForEachToken, string label, TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase
			> negPatternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label, ICollection<string> allCandidatePhrases)
		{
			Redwood.Log(Redwood.Dbg, "calculating sufficient stats");
			patternsForEachToken.SetupSearch();
			// calculating the sufficient statistics
			Type answerClass4Label = constVars.GetAnswerClass()[label];
			int sampleSize = constVars.sampleSentencesForSufficientStats == 1.0 ? sents.Count : (int)Math.Round(constVars.sampleSentencesForSufficientStats * sents.Count);
			IList<IList<string>> sampledSentIds = SplitIntoNumThreadsWithSampling(CollectionUtils.ToList(sents.Keys), sampleSize, constVars.numThreads);
			Redwood.Log(Redwood.Dbg, "sampled " + sampleSize + " sentences (" + constVars.sampleSentencesForSufficientStats * 100 + "%)");
			IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
			IList<IFuture<Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>>>> list = new List<IFuture<Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase
				>>>>>();
			foreach (IList<string> sampledSents in sampledSentIds)
			{
				ICallable<Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>>> task = new GetPatternsFromDataMultiClass.CalculateSufficientStatsThreads(this, patternsForEachToken, sampledSents, sents, label
					, answerClass4Label);
				IFuture<Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>>> submit = executor.Submit(task);
				list.Add(submit);
			}
			// Now retrieve the result
			foreach (IFuture<Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>>> future in list)
			{
				try
				{
					Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>> stats = future.Get();
					AddStats(patternsandWords4Label, stats.First());
					AddStats(negPatternsandWords4Label, stats.Second());
					AddStats(unLabeledPatternsandWords4Label, stats.Third());
				}
				catch (Exception e)
				{
					executor.ShutdownNow();
					throw new Exception(e);
				}
			}
			executor.Shutdown();
		}

		private void AddStats(TwoDimensionalCounter<E, CandidatePhrase> pw, IList<Pair<E, CandidatePhrase>> v)
		{
			foreach (Pair<E, CandidatePhrase> w in v)
			{
				pw.IncrementCount(w.First(), w.Second());
			}
		}

		private class CalculateSufficientStatsThreads : ICallable
		{
			private readonly IDictionary<string, DataInstance> sents;

			private readonly PatternsForEachToken patternsForEachToken;

			private readonly ICollection<string> sentIds;

			private readonly string label;

			private readonly Type answerClass4Label;

			public CalculateSufficientStatsThreads(GetPatternsFromDataMultiClass<E> _enclosing, PatternsForEachToken patternsForEachToken, ICollection<string> sentIds, IDictionary<string, DataInstance> sents, string label, Type answerClass4Label)
			{
				this._enclosing = _enclosing;
				this.patternsForEachToken = patternsForEachToken;
				this.sentIds = sentIds;
				this.sents = sents;
				this.label = label;
				this.answerClass4Label = answerClass4Label;
			}

			/// <exception cref="System.Exception"/>
			public virtual Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>> Call()
			{
				IList<Pair<E, CandidatePhrase>> posWords = new List<Pair<E, CandidatePhrase>>();
				IList<Pair<E, CandidatePhrase>> negWords = new List<Pair<E, CandidatePhrase>>();
				IList<Pair<E, CandidatePhrase>> unlabWords = new List<Pair<E, CandidatePhrase>>();
				foreach (string sentId in this.sentIds)
				{
					IDictionary<int, ICollection<E>> pat4Sent = this.patternsForEachToken.GetPatternsForAllTokens(sentId);
					if (pat4Sent == null)
					{
						throw new Exception("How come there are no patterns for " + sentId);
					}
					DataInstance sent = this.sents[sentId];
					IList<CoreLabel> tokens = sent.GetTokens();
					for (int i = 0; i < tokens.Count; i++)
					{
						CoreLabel token = tokens[i];
						//Map<String, Set<String>> matchedPhrases = token.get(PatternsAnnotations.MatchedPhrases.class);
						CandidatePhrase tokenWordOrLemma = CandidatePhrase.CreateOrGet(token.Word());
						CandidatePhrase longestMatchingPhrase;
						if (this._enclosing.constVars.useMatchingPhrase)
						{
							IDictionary<string, CandidatePhrase> longestMatchingPhrases = token.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel));
							longestMatchingPhrase = longestMatchingPhrases[this.label];
							longestMatchingPhrase = (longestMatchingPhrase != null && (longestMatchingPhrase.GetPhrase().Length > tokenWordOrLemma.GetPhrase().Length)) ? longestMatchingPhrase : tokenWordOrLemma;
						}
						else
						{
							/*if (matchedPhrases != null && !matchedPhrases.isEmpty()) {
							for (String s : matchedPhrases) {
							if (s.equals(tokenWordOrLemma)) {
							longestMatchingPhrase = tokenWordOrLemma;
							break;
							}
							if (longestMatchingPhrase == null || longestMatchingPhrase.length() > s.length()) {
							longestMatchingPhrase = s;
							}
							}
							} else {
							longestMatchingPhrase = tokenWordOrLemma;
							}*/
							longestMatchingPhrase = tokenWordOrLemma;
						}
						ICollection<E> pats = pat4Sent[i];
						//make a copy of pats because we are changing numwordscompound etc.
						ISet newpats = new HashSet<E>();
						bool changedpats = false;
						// cdm added null test 2018-01-17 to fix NPE, but more needs to be changed to get DEPS option working,
						// apparently including adding more code currently in research package.
						if (pats != null)
						{
							foreach (E s in pats)
							{
								if (s is SurfacePattern)
								{
									changedpats = true;
									SurfacePattern snew = ((SurfacePattern)s).CopyNewToken();
									snew.SetNumWordsCompound(PatternFactory.numWordsCompoundMapped[this.label]);
									newpats.Add(snew);
								}
							}
						}
						if (changedpats)
						{
							pats = newpats;
						}
						//This happens when dealing with the collapseddependencies
						if (pats == null)
						{
							if (!this._enclosing.constVars.patternType.Equals(PatternFactory.PatternType.Dep))
							{
								throw new Exception("Why are patterns null for sentence " + sentId + " and token " + i + "(" + tokens[i] + "). pat4Sent has token ids " + pat4Sent.Keys + (this._enclosing.constVars.batchProcessSents ? string.Empty : ". The sentence is " + Data
									.sents[sentId]) + ". If you have changed parameters, recompute all patterns.");
							}
							continue;
						}
						//        Set<E> prevPat = pat.first();
						//        Set<E> nextPat = pat.second();
						//        Set<E> prevnextPat = pat.third();
						if (PatternFactory.ignoreWordRegex.Matcher(token.Word()).Matches())
						{
							continue;
						}
						// if the target word/phrase does not satisfy the POS requirement
						string tag = token.Tag();
						if (this._enclosing.constVars.allowedTagsInitials != null && this._enclosing.constVars.allowedTagsInitials.Contains(this.label))
						{
							bool use = false;
							foreach (string allowed in this._enclosing.constVars.allowedTagsInitials[this.label])
							{
								if (tag.StartsWith(allowed))
								{
									use = true;
									break;
								}
							}
							if (!use)
							{
								continue;
							}
						}
						// if the target word/phrase does not satisfy the NER requirements
						string nertag = token.Ner();
						if (this._enclosing.constVars.allowedNERsforLabels != null && this._enclosing.constVars.allowedNERsforLabels.Contains(this.label))
						{
							if (!this._enclosing.constVars.allowedNERsforLabels[this.label].Contains(nertag))
							{
								continue;
							}
						}
						if (token.Get(this.answerClass4Label).Equals(this.label))
						{
							// Positive
							foreach (E s in pats)
							{
								posWords.Add(new Pair<E, CandidatePhrase>(s, longestMatchingPhrase));
							}
						}
						else
						{
							// Negative or unlabeled
							bool negToken = false;
							IDictionary<Type, object> ignore = this._enclosing.constVars.GetIgnoreWordswithClassesDuringSelection()[this.label];
							foreach (Type igCl in ignore.Keys)
							{
								if ((bool)token.Get(igCl))
								{
									negToken = true;
									break;
								}
							}
							if (!negToken)
							{
								if (this._enclosing.constVars.GetOtherSemanticClassesWords().Contains(token.Word()) || this._enclosing.constVars.GetOtherSemanticClassesWords().Contains(token.Lemma()))
								{
									negToken = true;
								}
							}
							if (!negToken)
							{
								foreach (string labelA in this._enclosing.constVars.GetLabels())
								{
									if (!labelA.Equals(this.label))
									{
										if (this._enclosing.constVars.GetSeedLabelDictionary()[labelA].Contains(longestMatchingPhrase) || this._enclosing.constVars.GetSeedLabelDictionary()[labelA].Contains(tokenWordOrLemma) || this._enclosing.constVars.GetLearnedWords(labelA).ContainsKey
											(longestMatchingPhrase) || this._enclosing.constVars.GetLearnedWords(labelA).ContainsKey(tokenWordOrLemma))
										{
											negToken = true;
											break;
										}
									}
								}
							}
							foreach (E sindex in pats)
							{
								if (negToken)
								{
									negWords.Add(new Pair<E, CandidatePhrase>(sindex, longestMatchingPhrase));
								}
								else
								{
									unlabWords.Add(new Pair<E, CandidatePhrase>(sindex, longestMatchingPhrase));
								}
							}
						}
					}
				}
				return new Triple<IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>, IList<Pair<E, CandidatePhrase>>>(posWords, negWords, unlabWords);
			}

			private readonly GetPatternsFromDataMultiClass<E> _enclosing;
		}

		private ICollection<E> EnforceMinSupportRequirements(TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label)
		{
			ICollection<E> remove = new HashSet<E>();
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en in patternsandWords4Label.EntrySet())
			{
				if (en.Value.Size() < constVars.minPosPhraseSupportForPat)
				{
					remove.Add(en.Key);
				}
			}
			int numRemoved = remove.Count;
			Redwood.Log(Redwood.Dbg, "Removing " + numRemoved + " patterns that do not meet minPosPhraseSupportForPat requirement of >= " + constVars.minPosPhraseSupportForPat);
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_1 in unLabeledPatternsandWords4Label.EntrySet())
			{
				if (en_1.Value.Size() < constVars.minUnlabPhraseSupportForPat)
				{
					remove.Add(en_1.Key);
				}
			}
			Redwood.Log(Redwood.Dbg, "Removing " + (remove.Count - numRemoved) + " patterns that do not meet minUnlabPhraseSupportForPat requirement of >= " + constVars.minUnlabPhraseSupportForPat);
			return remove;
		}

		//  void removeLearnedPattern(String label, E p) {
		//    this.learnedPatterns.get(label).remove(p);
		//    if (wordsPatExtracted.containsKey(label))
		//      for (Entry<String, ClassicCounter<E>> en : this.wordsPatExtracted.get(label).entrySet()) {
		//        en.getValue().remove(p);
		//      }
		//  }
		private void RemoveLearnedPatterns(string label, ICollection<E> pats)
		{
			Counters.RemoveKeys(this.learnedPatterns[label], pats);
			foreach (KeyValuePair<int, ICounter<E>> en in this.learnedPatternsEachIter[label])
			{
				Counters.RemoveKeys(en.Value, pats);
			}
			if (wordsPatExtracted.Contains(label))
			{
				foreach (KeyValuePair<CandidatePhrase, ClassicCounter<E>> en_1 in this.wordsPatExtracted[label].EntrySet())
				{
					Counters.RemoveKeys(en_1.Value, pats);
				}
			}
		}

		public static ICounter<E> NormalizeSoftMaxMinMaxScores<E>(ICounter<E> scores, bool minMaxNorm, bool softmax, bool oneMinusSoftMax)
		{
			double minScore = double.MaxValue;
			double maxScore = double.MinValue;
			ICounter<E> newscores = new ClassicCounter<E>();
			if (softmax)
			{
				foreach (KeyValuePair<E, double> en in scores.EntrySet())
				{
					double score = null;
					if (oneMinusSoftMax)
					{
						score = (1 / (1 + Math.Exp(Math.Min(7, en.Value))));
					}
					else
					{
						score = (1 / (1 + Math.Exp(-1 * Math.Min(7, en.Value))));
					}
					if (score < minScore)
					{
						minScore = score;
					}
					if (score > maxScore)
					{
						maxScore = score;
					}
					newscores.SetCount(en.Key, score);
				}
			}
			else
			{
				newscores.AddAll(scores);
				minScore = Counters.Min(newscores);
				maxScore = Counters.Max(newscores);
			}
			if (minMaxNorm)
			{
				foreach (KeyValuePair<E, double> en in newscores.EntrySet())
				{
					double score;
					if (minScore == maxScore)
					{
						score = minScore;
					}
					else
					{
						score = (en.Value - minScore + 1e-10) / (maxScore - minScore);
					}
					newscores.SetCount(en.Key, score);
				}
			}
			return newscores;
		}

		public TwoDimensionalCounter<string, ConstantsAndVariables.ScorePhraseMeasures> phInPatScoresCache = new TwoDimensionalCounter<string, ConstantsAndVariables.ScorePhraseMeasures>();

		/// <exception cref="System.IO.IOException"/>
		public virtual void LabelWords(string label, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> identifiedWords)
		{
			CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat = new CollectionValuedMap<E, Triple<string, int, int>>();
			LabelWords(label, sents, identifiedWords, null, matchedTokensByPat);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void LabelWords(string label, IDictionary<string, DataInstance> sents, ICollection<CandidatePhrase> identifiedWords, string outFile, CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat)
		{
			DateTime startTime = new DateTime();
			Redwood.Log(Redwood.Dbg, "Labeling " + sents.Count + " sentences with " + identifiedWords.Count + " phrases for label " + label);
			int numTokensLabeled = 0;
			CollectionValuedMap<string, int> tokensMatchedPatterns = null;
			if (constVars.restrictToMatched)
			{
				tokensMatchedPatterns = new CollectionValuedMap<string, int>();
				foreach (KeyValuePair<E, ICollection<Triple<string, int, int>>> en in matchedTokensByPat)
				{
					foreach (Triple<string, int, int> en2 in en.Value)
					{
						for (int i = en2.Second(); i <= en2.Third(); i++)
						{
							tokensMatchedPatterns.Add(en2.First(), i);
						}
					}
				}
			}
			IDictionary<string, IDictionary<int, ICollection<E>>> tempPatsForSents = new Dictionary<string, IDictionary<int, ICollection<E>>>();
			foreach (KeyValuePair<string, DataInstance> sentEn in sents)
			{
				IList<CoreLabel> tokens = sentEn.Value.GetTokens();
				bool sentenceChanged = false;
				IDictionary<CandidatePhrase, string[]> identifiedWordsTokens = new Dictionary<CandidatePhrase, string[]>();
				foreach (CandidatePhrase s in identifiedWords)
				{
					string[] toks = s.GetPhrase().Split("\\s+");
					identifiedWordsTokens[s] = toks;
				}
				string[] sent = new string[tokens.Count];
				int i = 0;
				ICollection<int> contextWordsRecalculatePats = new HashSet<int>();
				foreach (CoreLabel l in tokens)
				{
					sent[i] = l.Word();
					i++;
				}
				foreach (KeyValuePair<CandidatePhrase, string[]> phEn in identifiedWordsTokens)
				{
					string[] ph = phEn.Value;
					IList<int> ints = ArrayUtils.GetSubListIndex(ph, sent, null);
					if (ints == null)
					{
						continue;
					}
					foreach (int idx in ints)
					{
						bool donotuse = false;
						if (constVars.restrictToMatched)
						{
							for (int j = 0; j < ph.Length; j++)
							{
								if (!tokensMatchedPatterns[sentEn.Key].Contains(idx + j))
								{
									Redwood.Log(ConstantsAndVariables.extremedebug, "not labeling " + tokens[idx + j].Word());
									donotuse = true;
									break;
								}
							}
						}
						if (donotuse == false)
						{
							string phStr = StringUtils.Join(ph, " ");
							if (constVars.writeMatchedTokensIdsForEachPhrase)
							{
								AddToMatchedTokensByPhrase(phStr, sentEn.Key, idx, ph.Length);
							}
							Redwood.Log(ConstantsAndVariables.extremedebug, "Labeling because of phrase " + phStr);
							for (int j = 0; j < ph.Length; j++)
							{
								int index = idx + j;
								CoreLabel l_1 = tokens[index];
								if (constVars.usePatternResultAsLabel)
								{
									sentenceChanged = true;
									l_1.Set(constVars.GetAnswerClass()[label], label);
									numTokensLabeled++;
									//set the matched and the longest phrases
									CollectionValuedMap<string, CandidatePhrase> matched = new CollectionValuedMap<string, CandidatePhrase>();
									matched.Add(label, phEn.Key);
									if (!l_1.ContainsKey(typeof(PatternsAnnotations.MatchedPhrases)))
									{
										l_1.Set(typeof(PatternsAnnotations.MatchedPhrases), matched);
									}
									else
									{
										l_1.Get(typeof(PatternsAnnotations.MatchedPhrases)).AddAll(matched);
									}
									CandidatePhrase longest = l_1.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel))[label];
									longest = longest != null && longest.GetPhrase().Length > phEn.Key.GetPhrase().Length ? longest : phEn.Key;
									l_1.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel))[label] = longest;
									for (int k = Math.Max(0, index - PatternFactory.numWordsCompoundMapped[label]); k < tokens.Count && k <= index + PatternFactory.numWordsCompoundMapped[label] + 1; k++)
									{
										contextWordsRecalculatePats.Add(k);
									}
								}
							}
						}
					}
				}
				if (patsForEachToken != null)
				{
					//&& patsForEachToken.containsSentId(sentEn.getKey()))
					foreach (int index in contextWordsRecalculatePats)
					{
						if (!tempPatsForSents.Contains(sentEn.Key))
						{
							tempPatsForSents[sentEn.Key] = new Dictionary<int, ICollection<E>>();
						}
						tempPatsForSents[sentEn.Key][index] = Pattern.GetContext(constVars.patternType, sentEn.Value, index, ConstantsAndVariables.GetStopWords());
					}
				}
				//patsForEachToken.addPatterns(sentEn.getKey(), index, createPats.getContext(sentEn.getValue(), index));
				if (sentenceChanged)
				{
					constVars.invertedIndex.Update(sentEn.Value.GetTokens(), sentEn.Key);
				}
			}
			if (patsForEachToken != null)
			{
				patsForEachToken.UpdatePatterns(tempPatsForSents);
			}
			//sentEn.getKey(), index, createPats.getContext(sentEn.getValue(), index));
			constVars.invertedIndex.FinishUpdating();
			if (outFile != null)
			{
				Redwood.Log(ConstantsAndVariables.minimaldebug, "Writing results to " + outFile);
				IOUtils.WriteObjectToFile(sents, outFile);
			}
			DateTime endTime = new DateTime();
			Redwood.Log(Redwood.Dbg, "Done labeling provided sents in " + ElapsedTime(startTime, endTime) + ". Total # of tokens labeled: " + numTokensLabeled);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void IterateExtractApply()
		{
			IterateExtractApply(null, null, null);
		}

		/// <param name="p0">Null in most cases. only used for BPB</param>
		/// <param name="p0Set">Null in most cases</param>
		/// <param name="ignorePatterns"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void IterateExtractApply(IDictionary<string, E> p0, IDictionary<string, ICounter<CandidatePhrase>> p0Set, IDictionary<string, ICollection<E>> ignorePatterns)
		{
			IDictionary<string, CollectionValuedMap<E, Triple<string, int, int>>> matchedTokensByPatAllLabels = new Dictionary<string, CollectionValuedMap<E, Triple<string, int, int>>>();
			//Map<String, Collection<Triple<String, Integer, Integer>>> matchedTokensForPhrases = new HashMap<String, Collection<Triple<String, Integer, Integer>>>();
			IDictionary<string, TwoDimensionalCounter<CandidatePhrase, E>> termsAllLabels = new Dictionary<string, TwoDimensionalCounter<CandidatePhrase, E>>();
			IDictionary<string, ICollection<CandidatePhrase>> ignoreWordsAll = new Dictionary<string, ICollection<CandidatePhrase>>();
			foreach (string label in constVars.GetSeedLabelDictionary().Keys)
			{
				matchedTokensByPatAllLabels[label] = new CollectionValuedMap<E, Triple<string, int, int>>();
				termsAllLabels[label] = new TwoDimensionalCounter<CandidatePhrase, E>();
				if (constVars.useOtherLabelsWordsasNegative)
				{
					ICollection<CandidatePhrase> w = new HashSet<CandidatePhrase>();
					foreach (KeyValuePair<string, ICollection<CandidatePhrase>> en in constVars.GetSeedLabelDictionary())
					{
						if (en.Key.Equals(label))
						{
							continue;
						}
						Sharpen.Collections.AddAll(w, en.Value);
					}
					ignoreWordsAll[label] = w;
				}
			}
			Redwood.Log(ConstantsAndVariables.minimaldebug, "Iterating " + constVars.numIterationsForPatterns + " times.");
			IDictionary<string, BufferedWriter> wordsOutput = new Dictionary<string, BufferedWriter>();
			IDictionary<string, BufferedWriter> patternsOutput = new Dictionary<string, BufferedWriter>();
			foreach (string label_1 in constVars.GetLabels())
			{
				if (constVars.outDir != null)
				{
					IOUtils.EnsureDir(new File(constVars.outDir + "/" + constVars.identifier + "/" + label_1));
					string wordsOutputFileLabel = constVars.outDir + "/" + constVars.identifier + "/" + label_1 + "/learnedwords.txt";
					wordsOutput[label_1] = new BufferedWriter(new FileWriter(wordsOutputFileLabel));
					Redwood.Log(ConstantsAndVariables.minimaldebug, "Saving the learned words for label " + label_1 + " in " + wordsOutputFileLabel);
				}
				if (constVars.outDir != null)
				{
					string patternsOutputFileLabel = constVars.outDir + "/" + constVars.identifier + "/" + label_1 + "/learnedpatterns.txt";
					patternsOutput[label_1] = new BufferedWriter(new FileWriter(patternsOutputFileLabel));
					Redwood.Log(ConstantsAndVariables.minimaldebug, "Saving the learned patterns for label " + label_1 + " in " + patternsOutputFileLabel);
				}
			}
			for (int i = 0; i < constVars.numIterationsForPatterns; i++)
			{
				Redwood.Log(ConstantsAndVariables.minimaldebug, "\n\n################################ Iteration " + (i + 1) + " ##############################");
				bool keepRunning = false;
				IDictionary<string, ICounter<CandidatePhrase>> learnedWordsThisIter = new Dictionary<string, ICounter<CandidatePhrase>>();
				foreach (string label_2 in constVars.GetLabels())
				{
					Redwood.Log(ConstantsAndVariables.minimaldebug, "\n###Learning for label " + label_2 + " ######");
					string sentout = constVars.sentsOutFile == null ? null : constVars.sentsOutFile + "_" + label_2;
					Pair<ICounter<E>, ICounter<CandidatePhrase>> learnedPatWords4label = IterateExtractApply4Label(label_2, p0 != null ? p0[label_2] : null, p0Set != null ? p0Set[label_2] : null, wordsOutput[label_2], sentout, patternsOutput[label_2], ignorePatterns
						 != null ? ignorePatterns[label_2] : null, ignoreWordsAll[label_2], matchedTokensByPatAllLabels[label_2], termsAllLabels[label_2], i + numIterationsLoadedModel);
					learnedWordsThisIter[label_2] = learnedPatWords4label.Second();
					if (learnedPatWords4label.First().Size() > 0 && constVars.GetLearnedWords(label_2).Size() < constVars.maxExtractNumWords)
					{
						keepRunning = true;
					}
				}
				if (constVars.useOtherLabelsWordsasNegative)
				{
					foreach (string label_3 in constVars.GetLabels())
					{
						foreach (KeyValuePair<string, ICounter<CandidatePhrase>> en in learnedWordsThisIter)
						{
							if (en.Key.Equals(label_3))
							{
								continue;
							}
							Sharpen.Collections.AddAll(ignoreWordsAll[label_3], en.Value.KeySet());
						}
					}
				}
				if (!keepRunning)
				{
					if (!constVars.tuneThresholdKeepRunning)
					{
						Redwood.Log(ConstantsAndVariables.minimaldebug, "No patterns learned for all labels. Ending iterations.");
						break;
					}
					else
					{
						constVars.thresholdSelectPattern = 0.8 * constVars.thresholdSelectPattern;
						Redwood.Log(ConstantsAndVariables.minimaldebug, "\n\nTuning thresholds to keep running. New Pattern threshold is  " + constVars.thresholdSelectPattern);
					}
				}
			}
			if (constVars.outDir != null && !constVars.outDir.IsEmpty())
			{
				Redwood.Log(ConstantsAndVariables.minimaldebug, "Writing justification files");
				foreach (string label_2 in constVars.GetLabels())
				{
					IOUtils.EnsureDir(new File(constVars.outDir + "/" + constVars.identifier + "/" + label_2));
					if (constVars.writeMatchedTokensFiles)
					{
						ConstantsAndVariables.DataSentsIterator iter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
						int i_1 = 0;
						string suffix = string.Empty;
						while (iter.MoveNext())
						{
							i_1++;
							if (constVars.batchProcessSents)
							{
								suffix = "_" + i_1;
							}
							WriteMatchedTokensAndSents(label_2, iter.Current.First(), suffix, matchedTokensByPatAllLabels[label_2]);
						}
					}
				}
				if (constVars.writeMatchedTokensIdsForEachPhrase && constVars.outDir != null)
				{
					string matchedtokensfilename = constVars.outDir + "/" + constVars.identifier + "/tokenids4matchedphrases" + ".json";
					IOUtils.WriteStringToFile(MatchedTokensByPhraseJsonString(), matchedtokensfilename, "utf8");
				}
			}
			System.Console.Out.WriteLine("\n\nAll patterns learned:");
			foreach (KeyValuePair<string, IDictionary<int, ICounter<E>>> en2 in this.learnedPatternsEachIter)
			{
				System.Console.Out.WriteLine(en2.Key + ":");
				foreach (KeyValuePair<int, ICounter<E>> en in en2.Value)
				{
					System.Console.Out.WriteLine("Iteration " + en.Key);
					System.Console.Out.WriteLine(StringUtils.Join(en.Value.KeySet(), "\n"));
				}
			}
			System.Console.Out.WriteLine("\n\nAll words learned:");
			foreach (string label_4 in constVars.GetLabels())
			{
				System.Console.Out.WriteLine("\nLabel " + label_4 + "\n");
				foreach (KeyValuePair<int, ICounter<CandidatePhrase>> en in this.constVars.GetLearnedWordsEachIter(label_4))
				{
					System.Console.Out.WriteLine("Iteration " + en.Key + ":\t\t" + en.Value.KeySet());
				}
			}
			// close all the writers
			foreach (string label_5 in constVars.GetLabels())
			{
				if (wordsOutput.Contains(label_5) && wordsOutput[label_5] != null)
				{
					wordsOutput[label_5].Close();
				}
				if (patternsOutput.Contains(label_5) && patternsOutput[label_5] != null)
				{
					patternsOutput[label_5].Close();
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void WriteMatchedTokensAndSents(string label, IDictionary<string, DataInstance> sents, string suffix, CollectionValuedMap<E, Triple<string, int, int>> tokensMatchedPat)
		{
			if (constVars.outDir != null)
			{
				ICollection<string> allMatchedSents = new HashSet<string>();
				string matchedtokensfilename = constVars.outDir + "/" + constVars.identifier + "/" + label + "/tokensmatchedpatterns" + suffix + ".json";
				IJsonObjectBuilder pats = Javax.Json.Json.CreateObjectBuilder();
				foreach (KeyValuePair<E, ICollection<Triple<string, int, int>>> en in tokensMatchedPat)
				{
					CollectionValuedMap<string, Pair<int, int>> matchedStrs = new CollectionValuedMap<string, Pair<int, int>>();
					foreach (Triple<string, int, int> en2 in en.Value)
					{
						allMatchedSents.Add(en2.First());
						matchedStrs.Add(en2.First(), new Pair<int, int>(en2.Second(), en2.Third()));
					}
					IJsonObjectBuilder senttokens = Javax.Json.Json.CreateObjectBuilder();
					foreach (KeyValuePair<string, ICollection<Pair<int, int>>> sen in matchedStrs)
					{
						IJsonArrayBuilder obj = Javax.Json.Json.CreateArrayBuilder();
						foreach (Pair<int, int> sen2 in sen.Value)
						{
							IJsonArrayBuilder startend = Javax.Json.Json.CreateArrayBuilder();
							startend.Add(sen2.First());
							startend.Add(sen2.Second());
							obj.Add(startend);
						}
						senttokens.Add(sen.Key, obj);
					}
					pats.Add(en.Key.ToStringSimple(), senttokens);
				}
				IOUtils.WriteStringToFile(pats.Build().ToString(), matchedtokensfilename, "utf8");
				// Writing the sentence json file -- tokens for each sentence
				IJsonObjectBuilder senttokens_1 = Javax.Json.Json.CreateObjectBuilder();
				foreach (string sentId in allMatchedSents)
				{
					IJsonArrayBuilder sent = Javax.Json.Json.CreateArrayBuilder();
					foreach (CoreLabel l in sents[sentId].GetTokens())
					{
						sent.Add(l.Word());
					}
					senttokens_1.Add(sentId, sent);
				}
				string sentfilename = constVars.outDir + "/" + constVars.identifier + "/sentences" + suffix + ".json";
				IOUtils.WriteStringToFile(senttokens_1.Build().ToString(), sentfilename, "utf8");
			}
		}

		public static string MatchedTokensByPhraseJsonString(string phrase)
		{
			if (!Data.matchedTokensForEachPhrase.Contains(phrase))
			{
				return string.Empty;
			}
			IJsonArrayBuilder arrobj = JsonArrayBuilderFromMapCounter(Data.matchedTokensForEachPhrase[phrase]);
			return arrobj.Build().ToString();
		}

		public static string MatchedTokensByPhraseJsonString()
		{
			IJsonObjectBuilder pats = Javax.Json.Json.CreateObjectBuilder();
			foreach (KeyValuePair<string, IDictionary<string, IList<int>>> en in Data.matchedTokensForEachPhrase)
			{
				IJsonArrayBuilder arrobj = JsonArrayBuilderFromMapCounter(en.Value);
				pats.Add(en.Key, arrobj);
			}
			return pats.Build().ToString();
		}

		private static IJsonArrayBuilder JsonArrayBuilderFromMapCounter(IDictionary<string, IList<int>> mapCounter)
		{
			IJsonArrayBuilder arrobj = Javax.Json.Json.CreateArrayBuilder();
			foreach (KeyValuePair<string, IList<int>> sen in mapCounter)
			{
				IJsonObjectBuilder obj = Javax.Json.Json.CreateObjectBuilder();
				IJsonArrayBuilder tokens = Javax.Json.Json.CreateArrayBuilder();
				foreach (int i in sen.Value)
				{
					tokens.Add(i);
				}
				obj.Add(sen.Key, tokens);
				arrobj.Add(obj);
			}
			return arrobj;
		}

		//numIterTotal = numIter + iterations from previously loaded model!
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private Pair<ICounter<E>, ICounter<CandidatePhrase>> IterateExtractApply4Label(string label, E p0, ICounter<CandidatePhrase> p0Set, BufferedWriter wordsOutput, string sentsOutFile, BufferedWriter patternsOut, ICollection<E> ignorePatterns, ICollection
			<CandidatePhrase> ignoreWords, CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat, TwoDimensionalCounter<CandidatePhrase, E> terms, int numIterTotal)
		{
			if (!learnedPatterns.Contains(label))
			{
				learnedPatterns[label] = new ClassicCounter<E>();
			}
			if (!learnedPatternsEachIter.Contains(label))
			{
				learnedPatternsEachIter[label] = new Dictionary<int, ICounter<E>>();
			}
			if (!constVars.GetLearnedWordsEachIter().Contains(label))
			{
				constVars.GetLearnedWordsEachIter()[label] = new SortedDictionary<int, ICounter<CandidatePhrase>>();
			}
			//    if (!constVars.getLearnedWords().containsKey(label)) {
			//      constVars.getLearnedWords().put(label, new ClassicCounter<CandidatePhrase>());
			//    }
			ICounter<CandidatePhrase> identifiedWords = new ClassicCounter<CandidatePhrase>();
			ICounter<E> patterns = new ClassicCounter<E>();
			ICounter<E> patternThisIter = GetPatterns(label, learnedPatterns[label].KeySet(), p0, p0Set, ignorePatterns);
			patterns.AddAll(patternThisIter);
			learnedPatterns[label].AddAll(patterns);
			System.Diagnostics.Debug.Assert(!learnedPatternsEachIter[label].Contains(numIterTotal), "How come learned patterns already have a key for " + numIterTotal + " keys are " + learnedPatternsEachIter[label].Keys);
			learnedPatternsEachIter[label][numIterTotal] = patterns;
			if (sentsOutFile != null)
			{
				sentsOutFile = sentsOutFile + "_" + numIterTotal + "iter.ser";
			}
			ICounter<string> scoreForAllWordsThisIteration = new ClassicCounter<string>();
			identifiedWords.AddAll(scorePhrases.LearnNewPhrases(label, this.patsForEachToken, patterns, learnedPatterns[label], matchedTokensByPat, scoreForAllWordsThisIteration, terms, wordsPatExtracted[label], this.patternsandWords[label], constVars.identifier
				, ignoreWords));
			if (identifiedWords.Size() > 0)
			{
				if (constVars.usePatternResultAsLabel)
				{
					if (constVars.GetLabels().Contains(label))
					{
						ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
						while (sentsIter.MoveNext())
						{
							Pair<IDictionary<string, DataInstance>, File> sentsf = sentsIter.Current;
							Redwood.Log(Redwood.Dbg, "labeling sentences from " + sentsf.Second());
							LabelWords(label, sentsf.First(), identifiedWords.KeySet(), sentsOutFile, matchedTokensByPat);
							//write only for batch sentences
							//TODO: make this clean!
							if (sentsf.Second().Exists() && constVars.batchProcessSents)
							{
								IOUtils.WriteObjectToFile(sentsf.First(), sentsf.Second());
							}
						}
					}
					else
					{
						throw new Exception("why is the answer label null?");
					}
					System.Diagnostics.Debug.Assert(!constVars.GetLearnedWordsEachIter()[label].Contains(numIterTotal), "How come learned words already have a key for " + numIterTotal);
					constVars.GetLearnedWordsEachIter()[label][numIterTotal] = identifiedWords;
				}
				if (wordsOutput != null)
				{
					wordsOutput.Write("\n" + Counters.ToSortedString(identifiedWords, identifiedWords.Size(), "%1$s", "\n"));
					wordsOutput.Flush();
				}
			}
			//}
			if (patternsOut != null)
			{
				this.WritePatternsToFile(patterns, patternsOut);
			}
			return new Pair<ICounter<E>, ICounter<CandidatePhrase>>(patterns, identifiedWords);
		}

		/// <exception cref="System.IO.IOException"/>
		private void WritePatternsToFile(ICounter<E> pattern, BufferedWriter outFile)
		{
			foreach (KeyValuePair<E, double> en in pattern.EntrySet())
			{
				outFile.Write(en.Key + "\t" + en.Value + "\n");
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void WriteWordsToFile(IDictionary<int, ICounter<CandidatePhrase>> words, BufferedWriter outFile)
		{
			foreach (KeyValuePair<int, ICounter<CandidatePhrase>> en2 in words)
			{
				outFile.Write("###Iteration " + en2.Key + "\n");
				foreach (KeyValuePair<CandidatePhrase, double> en in en2.Value.EntrySet())
				{
					outFile.Write(en.Key + "\t" + en.Value + "\n");
				}
			}
		}

		private static SortedDictionary<int, ICounter<CandidatePhrase>> ReadLearnedWordsFromFile(File file)
		{
			SortedDictionary<int, ICounter<CandidatePhrase>> learned = new SortedDictionary<int, ICounter<CandidatePhrase>>();
			ICounter<CandidatePhrase> words = null;
			int numIter = -1;
			foreach (string line in IOUtils.ReadLines(file))
			{
				if (line.StartsWith("###"))
				{
					if (words != null)
					{
						learned[numIter] = words;
					}
					numIter++;
					words = new ClassicCounter<CandidatePhrase>();
					continue;
				}
				string[] t = line.Split("\t");
				words.SetCount(CandidatePhrase.CreateOrGet(t[0]), double.ParseDouble(t[1]));
			}
			if (words != null)
			{
				learned[numIter] = words;
			}
			return learned;
		}

		public virtual ICounter<E> GetLearnedPatterns(string label)
		{
			return this.learnedPatterns[label];
		}

		//  public Counter<E> getLearnedPatternsSurfaceForm(String label) {
		//    return this.learnedPatterns.get(label);
		//  }
		public virtual IDictionary<string, ICounter<E>> GetLearnedPatterns()
		{
			return this.learnedPatterns;
		}

		public virtual IDictionary<string, IDictionary<int, ICounter<E>>> GetLearnedPatternsEachIter()
		{
			return this.learnedPatternsEachIter;
		}

		public virtual IDictionary<int, ICounter<E>> GetLearnedPatternsEachIter(string label)
		{
			return this.learnedPatternsEachIter[label];
		}

		public virtual void SetLearnedPatterns(ICounter<E> patterns, string label)
		{
			this.learnedPatterns[label] = patterns;
		}

		/// <summary>
		/// COPIED from CRFClassifier: Count the successes and failures of the model on
		/// the given document.
		/// </summary>
		/// <remarks>
		/// COPIED from CRFClassifier: Count the successes and failures of the model on
		/// the given document. Fills numbers in to counters for true positives, false
		/// positives, and false negatives, and also keeps track of the entities seen. <br />
		/// Returns false if we ever encounter null for gold or guess. NOTE: The
		/// current implementation of counting wordFN/FP is incorrect.
		/// </remarks>
		private static bool CountResultsPerEntity(IList<CoreLabel> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN, string background, ICounter<string> wordTP, ICounter<string> wordTN, ICounter<string> wordFP, ICounter
			<string> wordFN, Type whichClassToCompare)
		{
			int index = 0;
			int goldIndex = 0;
			int guessIndex = 0;
			string lastGold = background;
			string lastGuess = background;
			// As we go through the document, there are two events we might be
			// interested in. One is when a gold entity ends, and the other
			// is when a guessed entity ends. If the gold and guessed
			// entities end at the same time, started at the same time, and
			// match entity type, we have a true positive. Otherwise we
			// either have a false positive or a false negative.
			string str = string.Empty;
			string s = string.Empty;
			foreach (CoreLabel l in doc)
			{
				s += " " + l.Word() + ":" + l.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) + ":" + l.Get(whichClassToCompare);
			}
			foreach (CoreLabel line in doc)
			{
				string gold = line.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				string guess = line.Get(whichClassToCompare);
				if (gold == null || guess == null)
				{
					return false;
				}
				if (lastGold != null && !lastGold.Equals(gold) && !lastGold.Equals(background))
				{
					if (lastGuess.Equals(lastGold) && !lastGuess.Equals(guess) && goldIndex == guessIndex)
					{
						wordTP.IncrementCount(str);
						entityTP.IncrementCount(lastGold, 1.0);
					}
					else
					{
						// System.out.println("false negative: " + str);
						wordFN.IncrementCount(str);
						entityFN.IncrementCount(lastGold, 1.0);
						str = string.Empty;
					}
				}
				if (lastGuess != null && !lastGuess.Equals(guess) && !lastGuess.Equals(background))
				{
					if (lastGuess.Equals(lastGold) && !lastGuess.Equals(guess) && goldIndex == guessIndex && !lastGold.Equals(gold))
					{
					}
					else
					{
						// correct guesses already tallied
						// str = "";
						// only need to tally false positives
						// System.out.println("false positive: " + str);
						entityFP.IncrementCount(lastGuess, 1.0);
						wordFP.IncrementCount(str);
					}
					str = string.Empty;
				}
				if (lastGuess != null && lastGold != null && lastGold.Equals(background) && lastGuess.Equals(background))
				{
					str = string.Empty;
				}
				if (lastGold == null || !lastGold.Equals(gold))
				{
					lastGold = gold;
					goldIndex = index;
				}
				if (lastGuess == null || !lastGuess.Equals(guess))
				{
					lastGuess = guess;
					guessIndex = index;
				}
				++index;
				if (str.IsEmpty())
				{
					str = line.Word();
				}
				else
				{
					str += " " + line.Word();
				}
			}
			// We also have to account for entities at the very end of the
			// document, since the above logic only occurs when we see
			// something that tells us an entity has ended
			if (lastGold != null && !lastGold.Equals(background))
			{
				if (lastGold.Equals(lastGuess) && goldIndex == guessIndex)
				{
					entityTP.IncrementCount(lastGold, 1.0);
					wordTP.IncrementCount(str);
				}
				else
				{
					entityFN.IncrementCount(lastGold, 1.0);
					wordFN.IncrementCount(str);
				}
				str = string.Empty;
			}
			if (lastGuess != null && !lastGuess.Equals(background))
			{
				if (lastGold.Equals(lastGuess) && goldIndex == guessIndex)
				{
				}
				else
				{
					// correct guesses already tallied
					entityFP.IncrementCount(lastGuess, 1.0);
					wordFP.IncrementCount(str);
				}
				str = string.Empty;
			}
			return true;
		}

		/// <summary>
		/// Count the successes and failures of the model on the given document
		/// ***token-based***.
		/// </summary>
		/// <remarks>
		/// Count the successes and failures of the model on the given document
		/// ***token-based***. Fills numbers in to counters for true positives, false
		/// positives, and false negatives, and also keeps track of the entities seen. <br />
		/// Returns false if we ever encounter null for gold or guess.
		/// this currently is only for testing one label at a time
		/// </remarks>
		public static void CountResultsPerToken(IList<CoreLabel> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN, string background, ICounter<string> wordTP, ICounter<string> wordTN, ICounter<string> wordFP, ICounter
			<string> wordFN, Type whichClassToCompare)
		{
			IOBUtils.CountEntityResults(doc, entityTP, entityFP, entityFN, background);
			// int index = 0;
			// int goldIndex = 0, guessIndex = 0;
			// String lastGold = background, lastGuess = background;
			// As we go through the document, there are two events we might be
			// interested in. One is when a gold entity ends, and the other
			// is when a guessed entity ends. If the gold and guessed
			// entities end at the same time, started at the same time, and
			// match entity type, we have a true positive. Otherwise we
			// either have a false positive or a false negative.
			foreach (CoreLabel line in doc)
			{
				string gold = line.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				string guess = line.Get(whichClassToCompare);
				System.Diagnostics.Debug.Assert((gold != null), "gold is null");
				System.Diagnostics.Debug.Assert((guess != null), "guess is null");
				if (gold.Equals(guess) && !Sharpen.Runtime.EqualsIgnoreCase(gold, background))
				{
					entityTP.IncrementCount(gold);
					wordTP.IncrementCount(line.Word());
				}
				else
				{
					if (!gold.Equals(guess) && !Sharpen.Runtime.EqualsIgnoreCase(gold, background) && Sharpen.Runtime.EqualsIgnoreCase(guess, background))
					{
						entityFN.IncrementCount(gold);
						wordFN.IncrementCount(line.Word());
					}
					else
					{
						if (!gold.Equals(guess) && !Sharpen.Runtime.EqualsIgnoreCase(guess, background) && Sharpen.Runtime.EqualsIgnoreCase(gold, background))
						{
							wordFP.IncrementCount(line.Word());
							entityFP.IncrementCount(guess);
						}
						else
						{
							if (gold.Equals(guess) && !Sharpen.Runtime.EqualsIgnoreCase(gold, background))
							{
								wordTN.IncrementCount(line.Word());
							}
							else
							{
								if (!(Sharpen.Runtime.EqualsIgnoreCase(gold, background) && Sharpen.Runtime.EqualsIgnoreCase(guess, background)))
								{
									throw new Exception("don't know reached here. not meant for more than one entity label: " + gold + " and " + guess);
								}
							}
						}
					}
				}
			}
		}

		public static void CountResults(IList<CoreLabel> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN, string background, ICounter<string> wordTP, ICounter<string> wordTN, ICounter<string> wordFP, ICounter<string
			> wordFN, Type whichClassToCompare, bool evalPerEntity)
		{
			if (evalPerEntity)
			{
				CountResultsPerEntity(doc, entityTP, entityFP, entityFN, background, wordTP, wordTN, wordFP, wordFN, whichClassToCompare);
			}
			else
			{
				CountResultsPerToken(doc, entityTP, entityFP, entityFN, background, wordTP, wordTN, wordFP, wordFN, whichClassToCompare);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void WriteLabelDataSents(IDictionary<string, DataInstance> sents, BufferedWriter writer)
		{
			foreach (KeyValuePair<string, DataInstance> sent in sents)
			{
				writer.Write(sent.Key + "\t");
				IDictionary<string, bool> lastWordLabeled = new Dictionary<string, bool>();
				foreach (string label in constVars.GetLabels())
				{
					lastWordLabeled[label] = false;
				}
				foreach (CoreLabel s in sent.Value.GetTokens())
				{
					string str = string.Empty;
					//write them in reverse order
					IList<string> listEndedLabels = new List<string>();
					//to first finish labels before starting
					IList<string> startingLabels = new List<string>();
					foreach (KeyValuePair<string, Type> @as in constVars.GetAnswerClass())
					{
						string label_1 = @as.Key;
						bool lastwordlabeled = lastWordLabeled[label_1];
						if (s.Get(@as.Value).Equals(label_1))
						{
							if (!lastwordlabeled)
							{
								startingLabels.Add(label_1);
							}
							lastWordLabeled[label_1] = true;
						}
						else
						{
							if (lastwordlabeled)
							{
								listEndedLabels.Add(label_1);
							}
							lastWordLabeled[label_1] = false;
						}
					}
					for (int i = listEndedLabels.Count - 1; i >= 0; i--)
					{
						str += " </" + listEndedLabels[i] + ">";
					}
					foreach (string label_2 in startingLabels)
					{
						str += " <" + label_2 + "> ";
					}
					str += " " + s.Word();
					writer.Write(str.Trim() + " ");
				}
				writer.Write("\n");
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void WriteLabeledData(string outFile)
		{
			BufferedWriter writer = new BufferedWriter(new FileWriter(outFile));
			ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
			while (sentsIter.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sentsf = sentsIter.Current;
				this.WriteLabelDataSents(sentsf.First(), writer);
			}
			writer.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void WriteColumnOutput(string outFile, bool batchProcessSents, IDictionary<string, Type> answerclasses)
		{
			BufferedWriter writer = new BufferedWriter(new FileWriter(outFile));
			ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(batchProcessSents);
			while (sentsIter.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sentsf = sentsIter.Current;
				WriteColumnOutputSents(sentsf.First(), writer, answerclasses);
			}
			writer.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		private static void WriteColumnOutputSents(IDictionary<string, DataInstance> sents, BufferedWriter writer, IDictionary<string, Type> answerclasses)
		{
			foreach (KeyValuePair<string, DataInstance> sent in sents)
			{
				writer.Write("\n\n" + sent.Key + "\n");
				foreach (CoreLabel s in sent.Value.GetTokens())
				{
					writer.Write(s.Word() + "\t");
					ICollection<string> labels = new HashSet<string>();
					foreach (KeyValuePair<string, Type> @as in answerclasses)
					{
						string label = @as.Key;
						if (s.Get(@as.Value).Equals(label))
						{
							labels.Add(label);
						}
					}
					if (labels.IsEmpty())
					{
						writer.Write("O\n");
					}
					else
					{
						writer.Write(StringUtils.Join(labels, ",") + "\n");
					}
				}
				writer.Write("\n");
			}
		}

		// public Map<String, DataInstance> loadJavaNLPAnnotatorLabeledFile(String
		// labeledFile, Properties props) throws FileNotFoundException {
		// System.out.println("Loading evaluate file " + labeledFile);
		// Map<String, DataInstance> sents = new HashMap<String,
		// DataInstance>();
		// JavaNLPAnnotatorReaderAndWriter j = new JavaNLPAnnotatorReaderAndWriter();
		// j.init(props);
		// Iterator<DataInstance> iter = j.getIterator(new BufferedReader(new
		// FileReader(labeledFile)));
		// int i = 0;
		// while (iter.hasNext()) {
		// i++;
		// DataInstance s = iter.next();
		// String id = s.get(0).get(CoreAnnotations.DocIDAnnotation.class);
		// if (id == null) {
		// id = Integer.toString(i);
		// }
		// sents.put(id, s);
		// }
		// System.out.println("Read " + sents.size() + " eval sentences");
		// return sents;
		// }
		// private void evaluate(String label, Map<String, DataInstance> sents)
		// throws IOException, InterruptedException, ExecutionException {
		// Redwood.log(Redwood.DBG, "labeling " + learnedWords.get(label));
		// CollectionValuedMap<String, Integer> tokensMatchedPatterns = new
		// CollectionValuedMap<String, Integer>();
		//
		// if (restrictToMatched) {
		// if (!alreadySetUp)
		// setUp();
		// List<String> keyset = new ArrayList<String>(sents.keySet());
		// int num = 0;
		// if (constVars.numThreads == 1)
		// num = keyset.size();
		// else
		// num = keyset.size() / (constVars.numThreads - 1);
		// ExecutorService executor = Executors
		// .newFixedThreadPool(constVars.numThreads);
		// // Redwood.log(ConstantsAndVariables.minimaldebug, "keyset size is " +
		// // keyset.size());
		// List<Future<Pair<TwoDimensionalCounter<Pair<String, String>,
		// SurfaceE>, CollectionValuedMap<String, Integer>>>> list = new
		// ArrayList<Future<Pair<TwoDimensionalCounter<Pair<String, String>,
		// SurfaceE>, CollectionValuedMap<String, Integer>>>>();
		// for (int i = 0; i < constVars.numThreads; i++) {
		// // Redwood.log(ConstantsAndVariables.minimaldebug, "assigning from " + i *
		// // num + " till " + Math.min(keyset.size(), (i + 1) * num));
		//
		// Callable<Pair<TwoDimensionalCounter<Pair<String, String>, SurfaceE>,
		// CollectionValuedMap<String, Integer>>> task = null;
		// task = new ApplyPatterns(keyset.subList(i * num,
		// Math.min(keyset.size(), (i + 1) * num)),
		// this.learnedPatterns.get(label), constVars.commonEngWords,
		// usePatternResultAsLabel, this.learnedWords.get(label).keySet(),
		// restrictToMatched, label,
		// constVars.removeStopWordsFromSelectedPhrases,
		// constVars.removePhrasesWithStopWords, constVars);
		// Future<Pair<TwoDimensionalCounter<Pair<String, String>, SurfaceE>,
		// CollectionValuedMap<String, Integer>>> submit = executor
		// .submit(task);
		// list.add(submit);
		// }
		// for (Future<Pair<TwoDimensionalCounter<Pair<String, String>,
		// SurfaceE>, CollectionValuedMap<String, Integer>>> future : list) {
		// Pair<TwoDimensionalCounter<Pair<String, String>, SurfaceE>,
		// CollectionValuedMap<String, Integer>> res = future
		// .get();
		// tokensMatchedPatterns.addAll(res.second());
		// }
		// executor.shutdown();
		// }
		//
		// this.labelWords(label, sents, this.learnedWords.get(label).keySet(),
		// this.learnedPatterns.get(label).keySet(), null, tokensMatchedPatterns);
		// Counter<String> entityTP = new ClassicCounter<String>();
		// Counter<String> entityFP = new ClassicCounter<String>();
		// Counter<String> entityFN = new ClassicCounter<String>();
		// for (Entry<String, DataInstance> sent : sents.entrySet()) {
		// for (CoreLabel l : sent.getValue()) {
		// if (l.containsKey(constVars.answerClass.get(label))
		// && l.get(constVars.answerClass.get(label)) != null)
		// l.set(CoreAnnotations.AnswerAnnotation.class,
		// l.get(constVars.answerClass.get(label)).toString());
		// if (!l.containsKey(CoreAnnotations.AnswerAnnotation.class)
		// || l.get(CoreAnnotations.AnswerAnnotation.class) == null) {
		// l.set(CoreAnnotations.AnswerAnnotation.class,
		// SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL);
		//
		// }
		//
		// }
		// CRFClassifier.countResults(sent.getValue(), entityTP, entityFP, entityFN,
		// SeqClassifierFlags.DEFAULT_BACKGROUND_SYMBOL);
		// }
		//
		// Counter<String> precision = Counters.division(entityTP,
		// Counters.add(entityTP, entityFP));
		// Counter<String> recall = Counters.division(entityTP,
		// Counters.add(entityTP, entityFN));
		// Counter<String> fscore = Counters.getFCounter(precision, recall, 1.0);
		// System.out.println("Precision: " + precision);
		// System.out.println("Recall: " + recall);
		// System.out.println("FScore: " + fscore);
		// }
		/// <exception cref="System.IO.IOException"/>
		public virtual void Evaluate(IDictionary<string, DataInstance> testSentences, bool evalPerEntity)
		{
			foreach (KeyValuePair<string, Type> anscl in constVars.GetAnswerClass())
			{
				string label = anscl.Key;
				ICounter<string> entityTP = new ClassicCounter<string>();
				ICounter<string> entityFP = new ClassicCounter<string>();
				ICounter<string> entityFN = new ClassicCounter<string>();
				ICounter<string> wordTP = new ClassicCounter<string>();
				ICounter<string> wordTN = new ClassicCounter<string>();
				ICounter<string> wordFP = new ClassicCounter<string>();
				ICounter<string> wordFN = new ClassicCounter<string>();
				foreach (KeyValuePair<string, DataInstance> docEn in testSentences)
				{
					DataInstance doc = docEn.Value;
					IList<CoreLabel> doceval = new List<CoreLabel>();
					foreach (CoreLabel l in doc.GetTokens())
					{
						CoreLabel l2 = new CoreLabel();
						l2.SetWord(l.Word());
						if (l.Get(anscl.Value).Equals(label))
						{
							l2.Set(typeof(CoreAnnotations.AnswerAnnotation), label);
						}
						else
						{
							l2.Set(typeof(CoreAnnotations.AnswerAnnotation), constVars.backgroundSymbol);
						}
						// If the gold label is not the label we are calculating the scores
						// for, set it to the background symbol
						if (!l.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)).Equals(label))
						{
							l2.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), constVars.backgroundSymbol);
						}
						else
						{
							l2.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), label);
						}
						doceval.Add(l2);
					}
					CountResults(doceval, entityTP, entityFP, entityFN, constVars.backgroundSymbol, wordTP, wordTN, wordFP, wordFN, typeof(CoreAnnotations.AnswerAnnotation), evalPerEntity);
				}
				//
				System.Console.Out.WriteLine("False Positives: " + Counters.ToSortedString(wordFP, wordFP.Size(), "%s:%.2f", ";"));
				System.Console.Out.WriteLine("False Negatives: " + Counters.ToSortedString(wordFN, wordFN.Size(), "%s:%.2f", ";"));
				Redwood.Log(Redwood.Dbg, "\nFor label " + label + " True Positives: " + entityTP + "\tFalse Positives: " + entityFP + "\tFalse Negatives: " + entityFN);
				ICounter<string> precision = Counters.Division(entityTP, Counters.Add(entityTP, entityFP));
				ICounter<string> recall = Counters.Division(entityTP, Counters.Add(entityTP, entityFN));
				Redwood.Log(ConstantsAndVariables.minimaldebug, "\nFor label " + label + " Precision: " + precision + ", Recall: " + recall + ", F1 score:  " + FScore(precision, recall, 1));
			}
		}

		// Redwood.log(ConstantsAndVariables.minimaldebug, "Total: " +
		// Counters.add(entityFP, entityTP));
		public static ICounter<D> FScore<D>(ICounter<D> precision, ICounter<D> recall, double beta)
		{
			double betasq = beta * beta;
			return Counters.DivisionNonNaN(Counters.Scale(Counters.Product(precision, recall), (1 + betasq)), (Counters.Add(Counters.Scale(precision, betasq), recall)));
		}

		private static IList<File> GetAllFiles(string file)
		{
			IList<File> allFiles = new List<File>();
			foreach (string tokfile in file.Split("[,;]"))
			{
				File filef = new File(tokfile);
				if (filef.IsDirectory())
				{
					Redwood.Log(Redwood.Dbg, "Will read from directory " + filef);
					string path = ".*";
					File dir = filef;
					foreach (File f in IOUtils.IterFilesRecursive(dir, Pattern.Compile(path)))
					{
						Redwood.Log(ConstantsAndVariables.extremedebug, "Will read from file " + f);
						allFiles.Add(f);
					}
				}
				else
				{
					if (filef.Exists())
					{
						Redwood.Log(Redwood.Dbg, "Will read from file " + filef);
						allFiles.Add(filef);
					}
					else
					{
						Redwood.Log(Redwood.Dbg, "trying to read from file " + filef);
						//Is this a pattern?
						RegExFileFilter fileFilter = new RegExFileFilter(Pattern.Compile(filef.GetName()));
						File dir = new File(Sharpen.Runtime.Substring(tokfile, 0, tokfile.LastIndexOf("/")));
						File[] files = dir.ListFiles(fileFilter);
						Sharpen.Collections.AddAll(allFiles, Arrays.AsList(files));
					}
				}
			}
			return allFiles;
		}

		private Pair<double, double> GetPrecisionRecall(string label, IDictionary<string, bool> goldWords4Label)
		{
			ICollection<CandidatePhrase> learnedWords = constVars.GetLearnedWords(label).KeySet();
			int numcorrect = 0;
			int numincorrect = 0;
			int numgoldcorrect = 0;
			foreach (KeyValuePair<string, bool> en in goldWords4Label)
			{
				if (en.Value)
				{
					numgoldcorrect++;
				}
			}
			ICollection<string> assumedNeg = new HashSet<string>();
			foreach (CandidatePhrase e in learnedWords)
			{
				if (!goldWords4Label.Contains(e.GetPhrase()))
				{
					assumedNeg.Add(e.GetPhrase());
					numincorrect++;
					continue;
				}
				if (goldWords4Label[e.GetPhrase()])
				{
					numcorrect++;
				}
				else
				{
					numincorrect++;
				}
			}
			if (!assumedNeg.IsEmpty())
			{
				log.Info("\nGold entity list does not contain words " + assumedNeg + " for label " + label + ". *****Assuming them as negative.******");
			}
			double precision = numcorrect / (double)(numcorrect + numincorrect);
			double recall = numcorrect / (double)(numgoldcorrect);
			return new Pair<double, double>(precision, recall);
		}

		private static double FScore(double precision, double recall, double beta)
		{
			double betasq = beta * beta;
			return (1 + betasq) * precision * recall / (betasq * precision + recall);
		}

		public virtual ICollection<string> GetNonBackgroundLabels(CoreLabel l)
		{
			ICollection<string> labels = new HashSet<string>();
			foreach (KeyValuePair<string, Type> en in constVars.GetAnswerClass())
			{
				if (!l.Get(en.Value).Equals(constVars.backgroundSymbol))
				{
					labels.Add(en.Key);
				}
			}
			return labels;
		}

		public static IDictionary<string, ICollection<CandidatePhrase>> ReadSeedWordsFromJSONString(string str)
		{
			IDictionary<string, ICollection<CandidatePhrase>> seedWords = new Dictionary<string, ICollection<CandidatePhrase>>();
			IJsonReader jsonReader = Javax.Json.Json.CreateReader(new StringReader(str));
			IJsonObject obj = jsonReader.ReadObject();
			jsonReader.Close();
			foreach (string o in obj.Keys)
			{
				seedWords[o] = new HashSet<CandidatePhrase>();
				IJsonArray arr = obj.GetJsonArray(o);
				foreach (IJsonValue v in arr)
				{
					seedWords[o].Add(CandidatePhrase.CreateOrGet(v.ToString()));
				}
			}
			return seedWords;
		}

		public static IDictionary<string, ICollection<CandidatePhrase>> ReadSeedWords(Properties props)
		{
			string seedWordsFile = props.GetProperty("seedWordsFiles");
			if (seedWordsFile != null)
			{
				return ReadSeedWords(seedWordsFile);
			}
			else
			{
				Redwood.Log(Redwood.Force, "NO SEED WORDS FILES PROVIDED!!");
				return Java.Util.Collections.EmptyMap();
			}
		}

		public static IDictionary<string, ICollection<CandidatePhrase>> ReadSeedWords(string seedWordsFiles)
		{
			IDictionary<string, ICollection<CandidatePhrase>> seedWords = new Dictionary<string, ICollection<CandidatePhrase>>();
			if (seedWordsFiles == null)
			{
				throw new Exception("Needs both seedWordsFiles and file parameters to run this class!\nseedWordsFiles has format: label1,filewithlistofwords1;label2,filewithlistofwords2;...");
			}
			foreach (string seedFile in seedWordsFiles.Split(";"))
			{
				string[] t = seedFile.Split(",");
				string label = t[0];
				ICollection<CandidatePhrase> seedWords4Label = new HashSet<CandidatePhrase>();
				for (int i = 1; i < t.Length; i++)
				{
					string seedWordsFile = t[i];
					foreach (File fin in ConstantsAndVariables.ListFileIncludingItself(seedWordsFile))
					{
						Redwood.Log(Redwood.Dbg, "Reading seed words from " + fin + " for label " + label);
						foreach (string line in IOUtils.ReadLines(fin))
						{
							line = line.Trim();
							if (line.IsEmpty() || line.StartsWith("#"))
							{
								continue;
							}
							line = line.Split("\t")[0];
							seedWords4Label.Add(CandidatePhrase.CreateOrGet(line));
						}
					}
				}
				seedWords[label] = seedWords4Label;
				Redwood.Log(ConstantsAndVariables.minimaldebug, "Number of seed words for label " + label + " is " + seedWords4Label.Count);
			}
			return seedWords;
		}

		internal virtual void RemoveLabelings(string label, ICollection<string> removeLabeledPhrases)
		{
		}

		internal static Type[] printOptionClass = new Type[] { typeof(string), typeof(bool), typeof(int), typeof(long), typeof(double), typeof(float) };

		//TODO: write this up when appropriate
		public virtual IDictionary<string, string> GetAllOptions()
		{
			IDictionary<string, string> values = new Dictionary<string, string>();
			props.ForEach(null);
			values.PutAll(constVars.GetAllOptions());
			//StringBuilder sb = new StringBuilder();
			Type thisClass;
			try
			{
				thisClass = Sharpen.Runtime.GetType(this.GetType().FullName);
				FieldInfo[] aClassFields = Sharpen.Runtime.GetDeclaredFields(thisClass);
				//sb.append(this.getClass().getSimpleName() + " [ ");
				foreach (FieldInfo f in aClassFields)
				{
					if (f.GetGenericType().GetType().IsPrimitive || Arrays.BinarySearch(printOptionClass, f.GetType().GetType()) >= 0)
					{
						string fName = f.Name;
						object fvalue = f.GetValue(this);
						values[fName] = fvalue == null ? "null" : fvalue.ToString();
					}
				}
			}
			catch (Exception e)
			{
				//sb.append("(" + f.getType() + ") " + fName + " = " + f.get(this) + ", ");
				log.Warn(e);
			}
			return values;
		}

		public class Flags
		{
			public static string useTargetParserParentRestriction = "useTargetParserParentRestriction";

			public static string useTargetNERRestriction = "useTargetNERRestriction";

			public static string posModelPath = "posModelPath";

			public static string numThreads = "numThreads";

			public static string patternType = "patternType";

			public static string numIterationsOfSavedPatternsToLoad = "numIterationsOfSavedPatternsToLoad";

			public static string patternsWordsDir = "patternsWordsDir";

			public static string loadModelForLabels = "loadModelForLabels";
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Pair<IDictionary<string, DataInstance>, IDictionary<string, DataInstance>> ProcessSents(Properties props, ICollection<string> labels)
		{
			string fileFormat = props.GetProperty("fileFormat");
			IDictionary<string, DataInstance> sents = null;
			bool batchProcessSents = bool.ParseBoolean(props.GetProperty("batchProcessSents", "false"));
			int numMaxSentencesPerBatchFile = System.Convert.ToInt32(props.GetProperty("numMaxSentencesPerBatchFile", int.MaxValue.ToString()));
			//works only for non-batch processing!
			bool preserveSentenceSequence = bool.ParseBoolean(props.GetProperty("preserveSentenceSequence", "false"));
			if (!batchProcessSents)
			{
				if (preserveSentenceSequence)
				{
					sents = new LinkedHashMap<string, DataInstance>();
				}
				else
				{
					sents = new Dictionary<string, DataInstance>();
				}
			}
			else
			{
				Data.sentsFiles = new List<File>();
				Data.sentId2File = new ConcurrentHashMap<string, File>();
			}
			string file = props.GetProperty("file");
			string posModelPath = props.GetProperty("posModelPath");
			bool lowercase = bool.ParseBoolean(props.GetProperty("lowercaseText"));
			bool useTargetNERRestriction = bool.ParseBoolean(props.GetProperty("useTargetNERRestriction"));
			bool useTargetParserParentRestriction = bool.ParseBoolean(props.GetProperty(GetPatternsFromDataMultiClass.Flags.useTargetParserParentRestriction));
			bool useContextNERRestriction = bool.ParseBoolean(props.GetProperty("useContextNERRestriction"));
			bool addEvalSentsToTrain = bool.ParseBoolean(props.GetProperty("addEvalSentsToTrain", "true"));
			string evalFileWithGoldLabels = props.GetProperty("evalFileWithGoldLabels");
			if (file == null && (evalFileWithGoldLabels == null || addEvalSentsToTrain == false))
			{
				throw new Exception("No training data! file is " + file + " and evalFileWithGoldLabels is " + evalFileWithGoldLabels + " and addEvalSentsToTrain is " + addEvalSentsToTrain);
			}
			if (props.GetProperty(GetPatternsFromDataMultiClass.Flags.patternType) == null)
			{
				throw new Exception("PatternType not specified. Options are SURFACE and DEP");
			}
			PatternFactory.PatternType patternType = PatternFactory.PatternType.ValueOf(props.GetProperty(GetPatternsFromDataMultiClass.Flags.patternType));
			// Read training file
			if (file != null)
			{
				string saveSentencesSerDirstr = props.GetProperty("saveSentencesSerDir");
				File saveSentencesSerDir = null;
				if (saveSentencesSerDirstr != null)
				{
					saveSentencesSerDir = new File(saveSentencesSerDirstr);
					if (saveSentencesSerDir.Exists() && !Sharpen.Runtime.EqualsIgnoreCase(fileFormat, "ser"))
					{
						IOUtils.DeleteDirRecursively(saveSentencesSerDir);
					}
					IOUtils.EnsureDir(saveSentencesSerDir);
				}
				string systemdir = Runtime.GetProperty("java.io.tmpdir");
				File tempSaveSentencesDir = File.CreateTempFile("sents", ".tmp", new File(systemdir));
				tempSaveSentencesDir.DeleteOnExit();
				tempSaveSentencesDir.Delete();
				tempSaveSentencesDir.Mkdir();
				int numFilesTillNow = 0;
				if (fileFormat == null || Sharpen.Runtime.EqualsIgnoreCase(fileFormat, "text") || Sharpen.Runtime.EqualsIgnoreCase(fileFormat, "txt"))
				{
					IDictionary<string, DataInstance> sentsthis;
					if (preserveSentenceSequence)
					{
						sentsthis = new LinkedHashMap<string, DataInstance>();
					}
					else
					{
						sentsthis = new Dictionary<string, DataInstance>();
					}
					foreach (File f in GetPatternsFromDataMultiClass.GetAllFiles(file))
					{
						Redwood.Log(Redwood.Dbg, "Annotating text in " + f);
						//String text = IOUtils.stringFromFile(f.getAbsolutePath());
						IEnumerator<string> reader = IOUtils.ReadLines(f).GetEnumerator();
						while (reader.MoveNext())
						{
							numFilesTillNow = Tokenize(reader, posModelPath, lowercase, useTargetNERRestriction || useContextNERRestriction, f.GetName() + "-" + numFilesTillNow + "-", useTargetParserParentRestriction, props.GetProperty(GetPatternsFromDataMultiClass.Flags
								.numThreads), batchProcessSents, numMaxSentencesPerBatchFile, saveSentencesSerDir == null ? tempSaveSentencesDir : saveSentencesSerDir, sentsthis, numFilesTillNow, patternType);
						}
						if (!batchProcessSents)
						{
							sents.PutAll(sentsthis);
						}
					}
					if (!batchProcessSents)
					{
						//          for(Map.Entry<String, DataInstance> d: sents.entrySet()){
						//            for(CoreLabel l : d.getValue().getTokens()){
						//              for(String label: labels) {
						//                if(l.containsKey(PatternsAnnotations.LongestMatchedPhraseForEachLabel.class)){
						//                  CandidatePhrase p = l.get(PatternsAnnotations.LongestMatchedPhraseForEachLabel.class).get(label);
						//                }
						//              }
						//            }
						//          }
						string outfilename = (saveSentencesSerDir == null ? tempSaveSentencesDir : saveSentencesSerDir) + "/sents_" + numFilesTillNow;
						if (saveSentencesSerDir != null)
						{
							Data.inMemorySaveFileLocation = outfilename;
						}
						Redwood.Log(Redwood.Force, "Saving sentences in " + outfilename);
						IOUtils.WriteObjectToFile(sents, outfilename);
					}
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(fileFormat, "ser"))
					{
						foreach (File f in GetPatternsFromDataMultiClass.GetAllFiles(file))
						{
							Redwood.Log(Redwood.Dbg, "reading from ser file " + f);
							if (!batchProcessSents)
							{
								sents.PutAll((IDictionary<string, DataInstance>)IOUtils.ReadObjectFromFile(f));
							}
							else
							{
								File newf = new File(tempSaveSentencesDir.GetAbsolutePath() + "/" + f.GetAbsolutePath().ReplaceAll(Pattern.Quote("/"), "_"));
								IOUtils.Cp(f, newf);
								Data.sentsFiles.Add(newf);
							}
						}
					}
					else
					{
						throw new Exception("Cannot identify the file format. Valid values are text (or txt) and ser, where the serialized file is of the type Map<String, DataInstance>.");
					}
				}
			}
			IDictionary<string, DataInstance> evalsents = new Dictionary<string, DataInstance>();
			bool evaluate = bool.ParseBoolean(props.GetProperty("evaluate"));
			// Read Evaluation File
			if (evaluate)
			{
				if (evalFileWithGoldLabels != null)
				{
					string saveEvalSentencesSerFile = props.GetProperty("saveEvalSentencesSerFile");
					File saveEvalSentencesSerFileFile = null;
					if (saveEvalSentencesSerFile == null)
					{
						string systemdir = Runtime.GetProperty("java.io.tmpdir");
						saveEvalSentencesSerFileFile = File.CreateTempFile("evalsents", ".tmp", new File(systemdir));
					}
					else
					{
						saveEvalSentencesSerFileFile = new File(saveEvalSentencesSerFile);
					}
					IDictionary setClassForTheseLabels = new Dictionary<string, Type>();
					//boolean splitOnPunct = Boolean.parseBoolean(props.getProperty("splitOnPunct", "true"));
					IList<File> allFiles = GetPatternsFromDataMultiClass.GetAllFiles(evalFileWithGoldLabels);
					int numFile = 0;
					string evalFileFormat = props.GetProperty("evalFileFormat");
					if (evalFileFormat == null || Sharpen.Runtime.EqualsIgnoreCase(evalFileFormat, "text") || Sharpen.Runtime.EqualsIgnoreCase(evalFileFormat, "txt") || evalFileFormat.StartsWith("text"))
					{
						foreach (File f in allFiles)
						{
							numFile++;
							Redwood.Log(Redwood.Dbg, "Annotating text in " + f + ". Num file " + numFile);
							if (Sharpen.Runtime.EqualsIgnoreCase(evalFileFormat, "textCoNLLStyle"))
							{
								IDictionary<string, DataInstance> sentsEval = AnnotatedTextReader.ParseColumnFile(new BufferedReader(new FileReader(f)), labels, setClassForTheseLabels, true, f.GetName());
								evalsents.PutAll(RunPOSNERParseOnTokens(sentsEval, props));
							}
							else
							{
								IList<ICoreMap> sentsCMs = AnnotatedTextReader.ParseFile(new BufferedReader(new FileReader(f)), labels, setClassForTheseLabels, true, f.GetName());
								evalsents.PutAll(RunPOSNEROnTokens(sentsCMs, posModelPath, useTargetNERRestriction || useContextNERRestriction, string.Empty, useTargetParserParentRestriction, props.GetProperty(GetPatternsFromDataMultiClass.Flags.numThreads), patternType));
							}
						}
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(fileFormat, "ser"))
						{
							foreach (File f in allFiles)
							{
								evalsents.PutAll((IDictionary<string, DataInstance>)IOUtils.ReadObjectFromFile(f));
							}
						}
					}
					if (addEvalSentsToTrain)
					{
						Redwood.Log(Redwood.Dbg, "Adding " + evalsents.Count + " eval sents to the training set");
					}
					IOUtils.WriteObjectToFile(evalsents, saveEvalSentencesSerFileFile);
					if (batchProcessSents)
					{
						Data.sentsFiles.Add(saveEvalSentencesSerFileFile);
						foreach (string k in evalsents.Keys)
						{
							Data.sentId2File[k] = saveEvalSentencesSerFileFile;
						}
					}
					else
					{
						sents.PutAll(evalsents);
					}
				}
			}
			return new Pair<IDictionary<string, DataInstance>, IDictionary<string, DataInstance>>(sents, evalsents);
		}

		/// <exception cref="System.IO.IOException"/>
		private void SaveModel()
		{
			string patternsWordsDirValue = props.GetProperty("patternsWordsDir");
			string patternsWordsDir;
			if (patternsWordsDirValue.EndsWith(".zip"))
			{
				File temp = File.CreateTempFile("patswords", "dir");
				temp.DeleteOnExit();
				temp.Delete();
				temp.Mkdirs();
				patternsWordsDir = temp.GetAbsolutePath();
			}
			else
			{
				patternsWordsDir = patternsWordsDirValue;
			}
			Redwood.Log(Redwood.Force, "Saving output in " + patternsWordsDir);
			IOUtils.EnsureDir(new File(patternsWordsDir));
			//writing properties file
			string outPropertiesFile = patternsWordsDir + "model.properties";
			props.Store(new BufferedWriter(new FileWriter(outPropertiesFile)), "trained model properties file");
			foreach (string label in constVars.GetLabels())
			{
				IOUtils.EnsureDir(new File(patternsWordsDir + "/" + label));
				BufferedWriter seedW = new BufferedWriter(new FileWriter(patternsWordsDir + "/" + label + "/seedwords.txt"));
				foreach (CandidatePhrase p in constVars.GetSeedLabelDictionary()[label])
				{
					seedW.Write(p.GetPhrase() + "\n");
				}
				seedW.Close();
				IDictionary<int, ICounter<E>> pats = GetLearnedPatternsEachIter(label);
				IOUtils.WriteObjectToFile(pats, patternsWordsDir + "/" + label + "/patternsEachIter.ser");
				BufferedWriter w = new BufferedWriter(new FileWriter(patternsWordsDir + "/" + label + "/phrases.txt"));
				WriteWordsToFile(constVars.GetLearnedWordsEachIter(label), w);
				//Write env
				WriteClassesInEnv(constVars.env, ConstantsAndVariables.globalEnv, patternsWordsDir + "/env.txt");
				//Write the token mapping
				if (constVars.patternType.Equals(PatternFactory.PatternType.Surface))
				{
					IOUtils.WriteStringToFile(Token.ToStringClass2KeyMapping(), patternsWordsDir + "/tokenenv.txt", "utf8");
				}
				w.Close();
			}
		}

		//    if (patternsWordsDirValue.endsWith(".zip")) {
		//      Redwood.log("Saving the zipped model to " + patternsWordsDirValue);
		//      zip(patternsWordsDir, patternsWordsDirValue);
		//    }
		/// <exception cref="System.IO.IOException"/>
		private void Evaluate(IDictionary<string, DataInstance> evalsents)
		{
			if (constVars.goldEntitiesEvalFiles != null)
			{
				foreach (string label in constVars.GetLabels())
				{
					if (constVars.goldEntities.Contains(label))
					{
						Pair<double, double> pr = GetPrecisionRecall(label, constVars.goldEntities[label]);
						Redwood.Log(ConstantsAndVariables.minimaldebug, "\nFor label " + label + ": Number of gold entities is " + constVars.goldEntities[label].Count + ", Precision is " + df.Format(pr.First() * 100) + ", Recall is " + df.Format(pr.Second() * 100) 
							+ ", F1 is " + df.Format(FScore(pr.First(), pr.Second(), 1.0) * 100) + "\n\n");
					}
				}
			}
			if (evalsents.Count > 0)
			{
				bool evalPerEntity = bool.ParseBoolean(props.GetProperty("evalPerEntity", "true"));
				Evaluate(evalsents, evalPerEntity);
			}
			if (evalsents.Count == 0 && constVars.goldEntitiesEvalFiles == null)
			{
				log.Info("No eval sentences or list of gold entities provided to evaluate! Make sure evalFileWithGoldLabels or goldEntitiesEvalFiles is set, or turn off the evaluate flag");
			}
		}

		/// <summary>Execute the system give a properties file or object.</summary>
		/// <remarks>Execute the system give a properties file or object. Returns the model created</remarks>
		/// <param name="props"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="Java.Sql.SQLException"/>
		public static GetPatternsFromDataMultiClass<E> Run<E>(Properties props)
			where E : Pattern
		{
			IDictionary<string, ICollection<CandidatePhrase>> seedWords = ReadSeedWords(props);
			IDictionary<string, Type> answerClasses = new Dictionary<string, Type>();
			string ansClasses = props.GetProperty("answerClasses");
			if (ansClasses != null)
			{
				foreach (string l in ansClasses.Split(";"))
				{
					string[] t = l.Split(",");
					string label = t[0];
					string cl = t[1];
					Type answerClass = ClassLoader.GetSystemClassLoader().LoadClass(cl);
					answerClasses[label] = answerClass;
				}
			}
			//process all the sentences here!
			Pair<IDictionary<string, DataInstance>, IDictionary<string, DataInstance>> sentsPair = ProcessSents(props, seedWords.Keys);
			bool labelUsingSeedSets = bool.ParseBoolean(props.GetProperty("labelUsingSeedSets", "true"));
			GetPatternsFromDataMultiClass<E> model = new GetPatternsFromDataMultiClass<E>(props, sentsPair.First(), seedWords, labelUsingSeedSets);
			return RunNineYards(model, props, sentsPair.Second());
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private static GetPatternsFromDataMultiClass<E> RunNineYards<E>(GetPatternsFromDataMultiClass<E> model, Properties props, IDictionary<string, DataInstance> evalsents)
			where E : Pattern
		{
			ArgumentParser.FillOptions(model, props);
			// If you want to reuse patterns and words learned previously (may be on another dataset etc)
			bool loadSavedPatternsWordsDir = bool.ParseBoolean(props.GetProperty("loadSavedPatternsWordsDir"));
			//#################### Load already save pattersn and phrases
			if (loadSavedPatternsWordsDir)
			{
				LoadFromSavedPatternsWordsDir(model, props);
			}
			if (model.constVars.learn)
			{
				IDictionary<string, E> p0 = new Dictionary<string, E>();
				IDictionary<string, ICounter<CandidatePhrase>> p0Set = new Dictionary<string, ICounter<CandidatePhrase>>();
				IDictionary<string, ICollection<E>> ignorePatterns = new Dictionary<string, ICollection<E>>();
				model.IterateExtractApply(p0, p0Set, ignorePatterns);
			}
			//############ Write Output files
			if (model.constVars.markedOutputTextFile != null)
			{
				model.WriteLabeledData(model.constVars.markedOutputTextFile);
			}
			if (model.constVars.columnOutputFile != null)
			{
				WriteColumnOutput(model.constVars.columnOutputFile, model.constVars.batchProcessSents, model.constVars.GetAnswerClass());
			}
			//###################### SAVE MODEL
			if (model.constVars.savePatternsWordsDir)
			{
				model.SaveModel();
			}
			//######## EVALUATE ###########################3
			bool evaluate = bool.ParseBoolean(props.GetProperty("evaluate"));
			if (evaluate && evalsents != null)
			{
				model.Evaluate(evalsents);
			}
			if (model.constVars.saveInvertedIndex)
			{
				model.constVars.invertedIndex.SaveIndex(model.constVars.invertedIndexDirectory);
			}
			if (model.constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Lucene))
			{
				model.patsForEachToken.Close();
			}
			return model;
		}

		internal static int numIterationsLoadedModel = 0;

		//  static void unzip(String file, String outputDir) throws IOException {
		//    ZipFile zipFile = new ZipFile(file);
		//    Enumeration<? extends ZipEntry> entries = zipFile.entries();
		//    while (entries.hasMoreElements()) {
		//      ZipEntry entry = entries.nextElement();
		//      Path entryDestination = new File(outputDir,  entry.getName()).toPath();
		//      entryDestination.toFile().getParentFile().mkdirs();
		//      if (entry.isDirectory())
		//        entryDestination.toFile().mkdirs();
		//      else {
		//        InputStream in = zipFile.getInputStream(entry);
		//        Files.copy(in, entryDestination);
		//        in.close();
		//      }
		//    }
		//  }
		//
		//  static void zip(String directory, String outputFileName) throws IOException {
		//    FileOutputStream fos = new FileOutputStream(outputFileName);
		//    ZipOutputStream zos = new ZipOutputStream(fos);
		//    //level - the compression level (0-9)
		//    zos.setLevel(9);
		//    addFolder(zos, directory, directory);
		//    zos.close();
		//  }
		/// <summary>copied from http://www.justexample.com/wp/compress-folder-into-zip-file-using-java/</summary>
		/// <exception cref="System.IO.IOException"/>
		private static void AddFolder(ZipOutputStream zos, string folderName, string baseFolderName)
		{
			File f = new File(folderName);
			if (f.Exists())
			{
				if (f.IsDirectory())
				{
					if (!Sharpen.Runtime.EqualsIgnoreCase(folderName, baseFolderName))
					{
						string entryName = Sharpen.Runtime.Substring(folderName, baseFolderName.Length + 1, folderName.Length) + File.separatorChar;
						System.Console.Out.WriteLine("Adding folder entry " + entryName);
						ZipEntry ze = new ZipEntry(entryName);
						zos.PutNextEntry(ze);
					}
					File[] f2 = f.ListFiles();
					foreach (File aF2 in f2)
					{
						AddFolder(zos, aF2.GetAbsolutePath(), baseFolderName);
					}
				}
				else
				{
					//add file
					//extract the relative name for entry purpose
					string entryName = Sharpen.Runtime.Substring(folderName, baseFolderName.Length + 1, folderName.Length);
					ZipEntry ze = new ZipEntry(entryName);
					zos.PutNextEntry(ze);
					FileInputStream @in = new FileInputStream(folderName);
					int len;
					byte[] buffer = new byte[1024];
					while ((len = @in.Read(buffer)) < 0)
					{
						zos.Write(buffer, 0, len);
					}
					@in.Close();
					zos.CloseEntry();
					System.Console.Out.WriteLine("OK!");
				}
			}
			else
			{
				System.Console.Out.WriteLine("File or directory not found " + folderName);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static IDictionary<E, string> LoadFromSavedPatternsWordsDir<E>(GetPatternsFromDataMultiClass<E> model, Properties props)
			where E : Pattern
		{
			bool labelSentsUsingModel = bool.ParseBoolean(props.GetProperty("labelSentsUsingModel", "true"));
			bool applyPatsUsingModel = bool.ParseBoolean(props.GetProperty("applyPatsUsingModel", "true"));
			int numIterationsOfSavedPatternsToLoad = System.Convert.ToInt32(props.GetProperty(GetPatternsFromDataMultiClass.Flags.numIterationsOfSavedPatternsToLoad, int.MaxValue.ToString()));
			IDictionary<E, string> labelsForPattterns = new Dictionary<E, string>();
			string patternsWordsDirValue = props.GetProperty(GetPatternsFromDataMultiClass.Flags.patternsWordsDir);
			string patternsWordsDir;
			//    if(patternsWordsDirValue.endsWith(".zip")){
			//      File tempdir = File.createTempFile("patternswordsdir","dir");
			//      tempdir.deleteOnExit();
			//      tempdir.delete();
			//      tempdir.mkdirs();
			//      patternsWordsDir = tempdir.getAbsolutePath();
			//      unzip(patternsWordsDirValue, patternsWordsDir);
			//    }else
			patternsWordsDir = patternsWordsDirValue;
			string sentsOutFile = props.GetProperty("sentsOutFile");
			string loadModelForLabels = props.GetProperty(GetPatternsFromDataMultiClass.Flags.loadModelForLabels);
			IList<string> loadModelForLabelsList = null;
			if (loadModelForLabels != null)
			{
				loadModelForLabelsList = Arrays.AsList(loadModelForLabels.Split("[,;]"));
			}
			foreach (string label in model.constVars.GetLabels())
			{
				if (loadModelForLabels != null && !loadModelForLabelsList.Contains(label))
				{
					continue;
				}
				System.Diagnostics.Debug.Assert((new File(patternsWordsDir + "/" + label).Exists()), "Why does the directory " + patternsWordsDir + "/" + label + " not exist?");
				ReadClassesInEnv(patternsWordsDir + "/env.txt", model.constVars.env, ConstantsAndVariables.globalEnv);
				//Read the token mapping
				if (model.constVars.patternType.Equals(PatternFactory.PatternType.Surface))
				{
					Token.SetClass2KeyMapping(new File(patternsWordsDir + "/tokenenv.txt"));
				}
				//Load Patterns
				File patf = new File(patternsWordsDir + "/" + label + "/patternsEachIter.ser");
				if (patf.Exists())
				{
					IDictionary<int, ICounter<E>> patterns = IOUtils.ReadObjectFromFile(patf);
					if (numIterationsOfSavedPatternsToLoad < int.MaxValue)
					{
						ICollection<int> toremove = new HashSet<int>();
						foreach (int i in patterns.Keys)
						{
							if (i >= numIterationsOfSavedPatternsToLoad)
							{
								System.Console.Out.WriteLine("Removing patterns from iteration " + i);
								toremove.Add(i);
							}
						}
						foreach (int i_1 in toremove)
						{
							Sharpen.Collections.Remove(patterns, i_1);
						}
					}
					ICounter<E> pats = Counters.Flatten(patterns);
					foreach (E p in pats.KeySet())
					{
						labelsForPattterns[p] = label;
					}
					numIterationsLoadedModel = Math.Max(numIterationsLoadedModel, patterns.Count);
					model.SetLearnedPatterns(pats, label);
					model.SetLearnedPatternsEachIter(patterns, label);
					Redwood.Log(Redwood.Dbg, "Loaded " + model.GetLearnedPatterns()[label].Size() + " patterns from " + patf);
				}
				//Load Words
				File wordf = new File(patternsWordsDir + "/" + label + "/phrases.txt");
				if (wordf.Exists())
				{
					SortedDictionary<int, ICounter<CandidatePhrase>> words = GetPatternsFromDataMultiClass.ReadLearnedWordsFromFile(wordf);
					model.constVars.SetLearnedWordsEachIter(words, label);
					if (numIterationsOfSavedPatternsToLoad < int.MaxValue)
					{
						ICollection<int> toremove = new HashSet<int>();
						foreach (int i in words.Keys)
						{
							if (i >= numIterationsOfSavedPatternsToLoad)
							{
								System.Console.Out.WriteLine("Removing patterns from iteration " + i);
								toremove.Add(i);
							}
						}
						foreach (int i_1 in toremove)
						{
							Sharpen.Collections.Remove(words, i_1);
						}
					}
					numIterationsLoadedModel = Math.Max(numIterationsLoadedModel, words.Count);
					Redwood.Log(Redwood.Dbg, "Loaded " + words.Count + " phrases from " + wordf);
				}
				CollectionValuedMap<E, Triple<string, int, int>> matchedTokensByPat = new CollectionValuedMap<E, Triple<string, int, int>>();
				IEnumerator<Pair<IDictionary<string, DataInstance>, File>> sentsIter = new ConstantsAndVariables.DataSentsIterator(model.constVars.batchProcessSents);
				TwoDimensionalCounter<CandidatePhrase, E> wordsandLemmaPatExtracted = new TwoDimensionalCounter<CandidatePhrase, E>();
				ICollection<CandidatePhrase> alreadyLabeledWords = new HashSet<CandidatePhrase>();
				while (sentsIter.MoveNext())
				{
					Pair<IDictionary<string, DataInstance>, File> sents = sentsIter.Current;
					if (labelSentsUsingModel)
					{
						Redwood.Log(Redwood.Dbg, "labeling sentences from " + sents.Second() + " with the already learned words");
						System.Diagnostics.Debug.Assert(sents.First() != null, "Why are sents null");
						model.LabelWords(label, sents.First(), model.constVars.GetLearnedWords(label).KeySet(), sentsOutFile, matchedTokensByPat);
						if (sents.Second().Exists())
						{
							IOUtils.WriteObjectToFile(sents, sents.Second());
						}
					}
					if (model.constVars.restrictToMatched || applyPatsUsingModel)
					{
						Redwood.Log(Redwood.Dbg, "Applying patterns to " + sents.First().Count + " sentences");
						model.constVars.invertedIndex.Add(sents.First(), true);
						model.constVars.invertedIndex.Add(sents.First(), true);
						model.scorePhrases.ApplyPats(model.GetLearnedPatterns(label), label, wordsandLemmaPatExtracted, matchedTokensByPat, alreadyLabeledWords);
					}
				}
				Counters.AddInPlace(model.wordsPatExtracted[label], wordsandLemmaPatExtracted);
				System.Console.Out.WriteLine("All Extracted phrases are " + wordsandLemmaPatExtracted.FirstKeySet());
			}
			System.Console.Out.Flush();
			System.Console.Error.Flush();
			return labelsForPattterns;
		}

		private void SetLearnedPatternsEachIter(IDictionary<int, ICounter<E>> patterns, string label)
		{
			this.learnedPatternsEachIter[label] = patterns;
		}

		/// <exception cref="System.TypeLoadException"/>
		private static void ReadClassesInEnv(string s, IDictionary<string, Env> env, Env globalEnv)
		{
			foreach (string line in IOUtils.ReadLines(s))
			{
				string[] toks = line.Split("###");
				if (toks.Length == 3)
				{
					string label = toks[0];
					string name = toks[1];
					Type c = Sharpen.Runtime.GetType(toks[2]);
					if (!env.Contains(label))
					{
						env[label] = TokenSequencePattern.GetNewEnv();
					}
					env[label].Bind(name, c);
				}
				else
				{
					if (toks.Length == 2)
					{
						string name = toks[0];
						Type c = Sharpen.Runtime.GetType(toks[1]);
						System.Diagnostics.Debug.Assert(c != null, " Why is name for " + toks[1] + " null");
						globalEnv.Bind(name, c);
					}
					else
					{
						throw new Exception("Ill formed env file!");
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static void WriteClassesInEnv(IDictionary<string, Env> env, Env globalEnv, string file)
		{
			BufferedWriter w = new BufferedWriter(new FileWriter(file));
			foreach (KeyValuePair<string, Env> en in env)
			{
				foreach (KeyValuePair<string, object> en2 in en.Value.GetVariables())
				{
					if (en2.Value is Type)
					{
						w.Write(en.Key + "###" + en2.Key + "###" + ((Type)en2.Value).FullName + "\n");
					}
				}
			}
			foreach (KeyValuePair<string, object> en2_1 in globalEnv.GetVariables())
			{
				if (en2_1.Value is Type)
				{
					w.Write(en2_1.Key + "###" + ((Type)en2_1.Value).FullName + "\n");
				}
			}
			w.Close();
		}

		public static string ElapsedTime(DateTime d1, DateTime d2)
		{
			try
			{
				Duration period = Duration.Between(d1.ToInstant(), d2.ToInstant());
				// Note: this will become easier with Java 9, using toDaysPart() etc.
				long days = period.ToDays();
				period = period.MinusDays(days);
				long hours = period.ToHours();
				period = period.MinusHours(hours);
				long minutes = period.ToMinutes();
				period = period.MinusMinutes(minutes);
				long seconds = period.GetSeconds();
				return days + " days, " + hours + " hours, " + minutes + " minutes, " + seconds + " seconds";
			}
			catch (ArgumentException e)
			{
				log.Warn(e);
			}
			return string.Empty;
		}

		public static void Main(string[] args)
		{
			try
			{
				Properties props = StringUtils.ArgsToPropertiesWithResolve(args);
				GetPatternsFromDataMultiClass.Run<SurfacePattern>(props);
			}
			catch (OutOfMemoryException e)
			{
				System.Console.Out.WriteLine("Out of memory! Either change the memory allotted by running as java -mx20g ... for example if you want to allocate 20G. Or consider using batchProcessSents and numMaxSentencesPerBatchFile flags");
				log.Warn(e);
			}
			catch (Exception e)
			{
				log.Warn(e);
			}
		}
	}
}
