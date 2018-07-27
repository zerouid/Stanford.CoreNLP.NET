//
// StanfordCoreNLP -- a suite of NLP tools
// Copyright (c) 2009-2011 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Dcoref.Sievepasses;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Multi-pass Sieve coreference resolution system (see EMNLP 2010 paper).</summary>
	/// <remarks>
	/// Multi-pass Sieve coreference resolution system (see EMNLP 2010 paper).
	/// <p>
	/// The main entry point for API is coref(Document document).
	/// The output is a map from CorefChain ID to corresponding CorefChain.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Mihai Surdeanu</author>
	/// <author>Karthik Raghunathan</author>
	/// <author>Heeyoung Lee</author>
	/// <author>Sudarshan Rangarajan</author>
	public class SieveCoreferenceSystem
	{
		/// <summary>A logger for this class.</summary>
		/// <remarks>A logger for this class. Still uses j.u.l currently.</remarks>
		public static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.Dcoref.SieveCoreferenceSystem).FullName);

		/// <summary>
		/// If true, we score the output of the given test document
		/// Assumes gold annotations are available
		/// </summary>
		private readonly bool doScore;

		/// <summary>If true, we do post processing.</summary>
		private readonly bool doPostProcessing;

		/// <summary>maximum sentence distance between two mentions for resolution (-1: no constraint on distance)</summary>
		private readonly int maxSentDist;

		/// <summary>automatically set by looking at sieves</summary>
		private readonly bool useSemantics;

		/// <summary>Singleton predictor from Recasens, de Marneffe, and Potts (NAACL 2013)</summary>
		private readonly bool useSingletonPredictor;

		/// <summary>flag for replicating CoNLL result</summary>
		private readonly bool replicateCoNLL;

		/// <summary>Path for the official CoNLL scorer</summary>
		public readonly string conllMentionEvalScript;

		/// <summary>flag for optimizing ordering of sieves</summary>
		private readonly bool optimizeSieves;

		/// <summary>Constraints on sieve order</summary>
		private IList<Pair<int, int>> sievesKeepOrder;

		/// <summary>Final score to use for sieve optimization (default is pairwise.Precision)</summary>
		private readonly string optimizeScoreType;

		/// <summary>More useful break down of optimizeScoreType</summary>
		private readonly bool optimizeConllScore;

		private readonly string optimizeMetricType;

		private readonly CorefScorer.SubScoreType optimizeSubScoreType;

		/// <summary>Not final because may change when running optimize sieve ordering but otherwise should stay fixed</summary>
		private DeterministicCorefSieve[] sieves;

		private string[] sieveClassNames;

		/// <summary>Dictionaries of all the useful goodies (gender, animacy, number etc.</summary>
		/// <remarks>Dictionaries of all the useful goodies (gender, animacy, number etc. lists)</remarks>
		private readonly Edu.Stanford.Nlp.Dcoref.Dictionaries dictionaries;

		/// <summary>Semantic knowledge: WordNet</summary>
		private readonly Edu.Stanford.Nlp.Dcoref.Semantics semantics;

		private LogisticClassifier<string, string> singletonPredictor;

		/// <summary>Current sieve index</summary>
		private int currentSieve;

		/// <summary>
		/// counter for links in passes (
		/// <c>Pair&lt;correct links, total links&gt;</c>
		/// )
		/// </summary>
		private IList<Pair<int, int>> linksCountInPass;

		/// <summary>Scores for each pass</summary>
		private IList<CorefScorer> scorePairwise;

		private IList<CorefScorer> scoreBcubed;

		private IList<CorefScorer> scoreMUC;

		private IList<CorefScorer> scoreSingleDoc;

		/// <summary>Additional scoring stats</summary>
		private int additionalCorrectLinksCount;

		private int additionalLinksCount;

		/// <exception cref="System.Exception"/>
		public SieveCoreferenceSystem(Properties props)
		{
			/*final */
			/*final*/
			// Below are member variables used for scoring (not thread safe)
			// initialize required fields
			currentSieve = -1;
			//
			// construct the sieve passes
			//
			string sievePasses = props.GetProperty(Constants.SievesProp, Constants.Sievepasses);
			sieveClassNames = sievePasses.Trim().Split(",\\s*");
			sieves = new DeterministicCorefSieve[sieveClassNames.Length];
			for (int i = 0; i < sieveClassNames.Length; i++)
			{
				sieves[i] = (DeterministicCorefSieve)Sharpen.Runtime.GetType("edu.stanford.nlp.dcoref.sievepasses." + sieveClassNames[i]).GetConstructor().NewInstance();
				sieves[i].Init(props);
			}
			//
			// create scoring framework
			//
			doScore = bool.ParseBoolean(props.GetProperty(Constants.ScoreProp, "false"));
			//
			// setting post processing
			//
			doPostProcessing = bool.ParseBoolean(props.GetProperty(Constants.PostprocessingProp, "false"));
			//
			// setting singleton predictor
			//
			useSingletonPredictor = bool.ParseBoolean(props.GetProperty(Constants.SingletonProp, "true"));
			//
			// setting maximum sentence distance between two mentions for resolution (-1: no constraint on distance)
			//
			maxSentDist = System.Convert.ToInt32(props.GetProperty(Constants.MaxdistProp, "-1"));
			//
			// set useWordNet
			//
			useSemantics = sievePasses.Contains("AliasMatch") || sievePasses.Contains("LexicalChainMatch");
			// flag for replicating CoNLL result
			replicateCoNLL = bool.ParseBoolean(props.GetProperty(Constants.ReplicateconllProp, "false"));
			conllMentionEvalScript = props.GetProperty(Constants.ConllScorer, Constants.conllMentionEvalScript);
			// flag for optimizing sieve ordering
			optimizeSieves = bool.ParseBoolean(props.GetProperty(Constants.OptimizeSievesProp, "false"));
			optimizeScoreType = props.GetProperty(Constants.OptimizeSievesScoreProp, "pairwise.Precision");
			// Break down of the optimize score type
			string[] validMetricTypes = new string[] { "muc", "pairwise", "bcub", "ceafe", "ceafm", "combined" };
			string[] parts = optimizeScoreType.Split("\\.");
			optimizeConllScore = parts.Length > 2 && Sharpen.Runtime.EqualsIgnoreCase("conll", parts[2]);
			optimizeMetricType = parts[0];
			bool optimizeMetricTypeOk = false;
			foreach (string validMetricType in validMetricTypes)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(validMetricType, optimizeMetricType))
				{
					optimizeMetricTypeOk = true;
					break;
				}
			}
			if (!optimizeMetricTypeOk)
			{
				throw new ArgumentException("Invalid metric type for " + Constants.OptimizeSievesScoreProp + " property: " + optimizeScoreType);
			}
			optimizeSubScoreType = CorefScorer.SubScoreType.ValueOf(parts[1]);
			if (optimizeSieves)
			{
				string keepSieveOrder = props.GetProperty(Constants.OptimizeSievesKeepOrderProp);
				if (keepSieveOrder != null)
				{
					string[] orderings = keepSieveOrder.Split("\\s*,\\s*");
					sievesKeepOrder = new List<Pair<int, int>>();
					string firstSieveConstraint = null;
					string lastSieveConstraint = null;
					foreach (string ordering in orderings)
					{
						// Convert ordering constraints from string
						Pair<int, int> p = FromSieveOrderConstraintString(ordering, sieveClassNames);
						// Do initial check of sieves order, can only have one where the first is ANY (< 0), and one where second is ANY (< 0)
						if (p.First() < 0 && p.Second() < 0)
						{
							throw new ArgumentException("Invalid ordering constraint: " + ordering);
						}
						else
						{
							if (p.First() < 0)
							{
								if (lastSieveConstraint != null)
								{
									throw new ArgumentException("Cannot have these two ordering constraints: " + lastSieveConstraint + "," + ordering);
								}
								lastSieveConstraint = ordering;
							}
							else
							{
								if (p.Second() < 0)
								{
									if (firstSieveConstraint != null)
									{
										throw new ArgumentException("Cannot have these two ordering constraints: " + firstSieveConstraint + "," + ordering);
									}
									firstSieveConstraint = ordering;
								}
							}
						}
						sievesKeepOrder.Add(p);
					}
				}
			}
			if (doScore)
			{
				InitScorers();
			}
			//
			// load all dictionaries
			//
			dictionaries = new Edu.Stanford.Nlp.Dcoref.Dictionaries(props);
			semantics = (useSemantics) ? new Edu.Stanford.Nlp.Dcoref.Semantics(dictionaries) : null;
			if (useSingletonPredictor)
			{
				singletonPredictor = GetSingletonPredictorFromSerializedFile(props.GetProperty(Constants.SingletonModelProp, DefaultPaths.DefaultDcorefSingletonModel));
			}
		}

		public virtual void InitScorers()
		{
			linksCountInPass = new List<Pair<int, int>>();
			scorePairwise = new List<CorefScorer>();
			scoreBcubed = new List<CorefScorer>();
			scoreMUC = new List<CorefScorer>();
			foreach (string sieveClassName in sieveClassNames)
			{
				scorePairwise.Add(new ScorerPairwise());
				scoreBcubed.Add(new ScorerBCubed(ScorerBCubed.BCubedType.Bconll));
				scoreMUC.Add(new ScorerMUC());
				linksCountInPass.Add(new Pair<int, int>(0, 0));
			}
		}

		public virtual bool DoScore()
		{
			return doScore;
		}

		public virtual Edu.Stanford.Nlp.Dcoref.Dictionaries Dictionaries()
		{
			return dictionaries;
		}

		public virtual Edu.Stanford.Nlp.Dcoref.Semantics Semantics()
		{
			return semantics;
		}

		public virtual string SieveClassName(int sieveIndex)
		{
			return (sieveIndex >= 0 && sieveIndex < sieveClassNames.Length) ? sieveClassNames[sieveIndex] : null;
		}

		/// <summary>
		/// Needs the following properties:
		/// -props 'Location of coref.properties'
		/// </summary>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			InitializeAndRunCoref(props);
		}

		/// <summary>Returns the name of the log file that this method writes.</summary>
		/// <exception cref="System.Exception"/>
		public static string InitializeAndRunCoref(Properties props)
		{
			string timeStamp = Calendar.GetInstance().GetTime().ToString().ReplaceAll("\\s", "-").ReplaceAll(":", "-");
			//
			// initialize logger
			//
			string logFileName = props.GetProperty(Constants.LogProp, "log.txt");
			if (logFileName.EndsWith(".txt"))
			{
				logFileName = Sharpen.Runtime.Substring(logFileName, 0, logFileName.Length - 4) + "_" + timeStamp + ".txt";
			}
			else
			{
				logFileName = logFileName + "_" + timeStamp + ".txt";
			}
			try
			{
				FileHandler fh = new FileHandler(logFileName, false);
				logger.AddHandler(fh);
				logger.SetLevel(Level.Fine);
				fh.SetFormatter(new NewlineLogFormatter());
			}
			catch (Exception e)
			{
				throw new Exception("Cannot initialize logger!", e);
			}
			logger.Fine(timeStamp);
			logger.Fine(props.ToString());
			Constants.PrintConstants(logger);
			// initialize coref system
			Edu.Stanford.Nlp.Dcoref.SieveCoreferenceSystem corefSystem = new Edu.Stanford.Nlp.Dcoref.SieveCoreferenceSystem(props);
			// MentionExtractor extracts MUC, ACE, or CoNLL documents
			MentionExtractor mentionExtractor;
			if (props.Contains(Constants.MucProp))
			{
				mentionExtractor = new MUCMentionExtractor(corefSystem.dictionaries, props, corefSystem.semantics, corefSystem.singletonPredictor);
			}
			else
			{
				if (props.Contains(Constants.Ace2004Prop) || props.Contains(Constants.Ace2005Prop))
				{
					mentionExtractor = new ACEMentionExtractor(corefSystem.dictionaries, props, corefSystem.semantics, corefSystem.singletonPredictor);
				}
				else
				{
					if (props.Contains(Constants.Conll2011Prop))
					{
						mentionExtractor = new CoNLLMentionExtractor(corefSystem.dictionaries, props, corefSystem.semantics, corefSystem.singletonPredictor);
					}
					else
					{
						throw new Exception("No input file specified!");
					}
				}
			}
			// Set mention finder
			string mentionFinderClass = props.GetProperty(Constants.MentionFinderProp);
			if (mentionFinderClass != null)
			{
				string mentionFinderPropFilename = props.GetProperty(Constants.MentionFinderPropfileProp);
				ICorefMentionFinder mentionFinder;
				if (mentionFinderPropFilename != null)
				{
					Properties mentionFinderProps = new Properties();
					using (FileInputStream fis = new FileInputStream(mentionFinderPropFilename))
					{
						mentionFinderProps.Load(fis);
					}
					mentionFinder = (ICorefMentionFinder)Sharpen.Runtime.GetType(mentionFinderClass).GetConstructor(typeof(Properties)).NewInstance(mentionFinderProps);
				}
				else
				{
					mentionFinder = (ICorefMentionFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(mentionFinderClass));
				}
				mentionExtractor.SetMentionFinder(mentionFinder);
			}
			if (mentionExtractor.mentionFinder == null)
			{
				logger.Warning("No mention finder specified, but not using gold mentions");
			}
			if (corefSystem.optimizeSieves && corefSystem.sieves.Length > 1)
			{
				corefSystem.OptimizeSieveOrdering(mentionExtractor, props, timeStamp);
			}
			try
			{
				RunAndScoreCoref(corefSystem, mentionExtractor, props, timeStamp);
			}
			catch (Exception ex)
			{
				logger.Log(Level.Severe, "ERROR in running coreference", ex);
			}
			logger.Info("done");
			string endTimeStamp = Calendar.GetInstance().GetTime().ToString().ReplaceAll("\\s", "-");
			logger.Fine(endTimeStamp);
			return logFileName;
		}

		/// <exception cref="System.Exception"/>
		public static double RunAndScoreCoref(Edu.Stanford.Nlp.Dcoref.SieveCoreferenceSystem corefSystem, MentionExtractor mentionExtractor, Properties props, string timeStamp)
		{
			// prepare conll output
			PrintWriter writerGold = null;
			PrintWriter writerPredicted = null;
			PrintWriter writerPredictedCoref = null;
			string conllOutputMentionGoldFile = null;
			string conllOutputMentionPredictedFile = null;
			string conllOutputMentionCorefPredictedFile = null;
			string conllMentionEvalFile = null;
			string conllMentionEvalErrFile = null;
			string conllMentionCorefEvalFile = null;
			string conllMentionCorefEvalErrFile = null;
			if (Constants.PrintConllOutput || corefSystem.replicateCoNLL)
			{
				string conllOutput = props.GetProperty(Constants.ConllOutputProp, "conlloutput");
				conllOutputMentionGoldFile = conllOutput + "-" + timeStamp + ".gold.txt";
				conllOutputMentionPredictedFile = conllOutput + "-" + timeStamp + ".predicted.txt";
				conllOutputMentionCorefPredictedFile = conllOutput + "-" + timeStamp + ".coref.predicted.txt";
				conllMentionEvalFile = conllOutput + "-" + timeStamp + ".eval.txt";
				conllMentionEvalErrFile = conllOutput + "-" + timeStamp + ".eval.err.txt";
				conllMentionCorefEvalFile = conllOutput + "-" + timeStamp + ".coref.eval.txt";
				conllMentionCorefEvalErrFile = conllOutput + "-" + timeStamp + ".coref.eval.err.txt";
				logger.Info("CONLL MENTION GOLD FILE: " + conllOutputMentionGoldFile);
				logger.Info("CONLL MENTION PREDICTED FILE: " + conllOutputMentionPredictedFile);
				logger.Info("CONLL MENTION EVAL FILE: " + conllMentionEvalFile);
				logger.Info("CONLL MENTION PREDICTED WITH COREF FILE: " + conllOutputMentionCorefPredictedFile);
				logger.Info("CONLL MENTION WITH COREF EVAL FILE: " + conllMentionCorefEvalFile);
				writerGold = new PrintWriter(new FileOutputStream(conllOutputMentionGoldFile));
				writerPredicted = new PrintWriter(new FileOutputStream(conllOutputMentionPredictedFile));
				writerPredictedCoref = new PrintWriter(new FileOutputStream(conllOutputMentionCorefPredictedFile));
			}
			mentionExtractor.ResetDocs();
			if (corefSystem.DoScore())
			{
				corefSystem.InitScorers();
			}
			//
			// Parse one document at a time, and do single-doc coreference resolution in each.
			//
			// In one iteration, orderedMentionsBySentence contains a list of all
			// mentions in one document. Each mention has properties (annotations):
			// its surface form (Word), NER Tag, POS Tag, Index, etc.
			//
			while (true)
			{
				Document document = mentionExtractor.NextDoc();
				if (document == null)
				{
					break;
				}
				if (!props.Contains(Constants.MucProp))
				{
					PrintRawDoc(document, true);
					PrintRawDoc(document, false);
				}
				PrintDiscourseStructure(document);
				if (corefSystem.DoScore())
				{
					document.ExtractGoldCorefClusters();
				}
				if (Constants.PrintConllOutput || corefSystem.replicateCoNLL)
				{
					// Not doing coref - print conll output here
					PrintConllOutput(document, writerGold, true);
					PrintConllOutput(document, writerPredicted, false);
				}
				// run mention detection only
				corefSystem.Coref(document);
				// Do Coreference Resolution
				if (corefSystem.DoScore())
				{
					//Identifying possible coreferring mentions in the corpus along with any recall/precision errors with gold corpus
					corefSystem.PrintTopK(logger, document, corefSystem.semantics);
					logger.Fine("pairwise score for this doc: ");
					corefSystem.scoreSingleDoc[corefSystem.sieves.Length - 1].PrintF1(logger);
					logger.Fine("accumulated score: ");
					corefSystem.PrintF1(true);
					logger.Fine("\n");
				}
				if (Constants.PrintConllOutput || corefSystem.replicateCoNLL)
				{
					PrintConllOutput(document, writerPredictedCoref, false, true);
				}
			}
			double finalScore = 0;
			if (Constants.PrintConllOutput || corefSystem.replicateCoNLL)
			{
				writerGold.Close();
				writerPredicted.Close();
				writerPredictedCoref.Close();
				//if(props.containsKey(Constants.CONLL_SCORER)) {
				if (corefSystem.conllMentionEvalScript != null)
				{
					//        runConllEval(corefSystem.conllMentionEvalScript, conllOutputMentionGoldFile, conllOutputMentionPredictedFile, conllMentionEvalFile, conllMentionEvalErrFile);
					string summary = GetConllEvalSummary(corefSystem.conllMentionEvalScript, conllOutputMentionGoldFile, conllOutputMentionPredictedFile);
					logger.Info("\nCONLL EVAL SUMMARY (Before COREF)");
					PrintScoreSummary(summary, logger, false);
					//          runConllEval(corefSystem.conllMentionEvalScript, conllOutputMentionGoldFile, conllOutputMentionCorefPredictedFile, conllMentionCorefEvalFile, conllMentionCorefEvalErrFile);
					summary = GetConllEvalSummary(corefSystem.conllMentionEvalScript, conllOutputMentionGoldFile, conllOutputMentionCorefPredictedFile);
					logger.Info("\nCONLL EVAL SUMMARY (After COREF)");
					PrintScoreSummary(summary, logger, true);
					PrintFinalConllScore(summary);
					if (corefSystem.optimizeConllScore)
					{
						finalScore = GetFinalConllScore(summary, corefSystem.optimizeMetricType, corefSystem.optimizeSubScoreType.ToString());
					}
				}
			}
			if (!corefSystem.optimizeConllScore && corefSystem.DoScore())
			{
				finalScore = corefSystem.GetFinalScore(corefSystem.optimizeMetricType, corefSystem.optimizeSubScoreType);
			}
			string scoresFile = props.GetProperty(Constants.ScoreFileProp);
			if (scoresFile != null)
			{
				PrintWriter pw = IOUtils.GetPrintWriter(scoresFile);
				pw.Println((new DecimalFormat("#.##")).Format(finalScore));
				pw.Close();
			}
			if (corefSystem.optimizeSieves)
			{
				logger.Info("Final reported score for sieve optimization " + corefSystem.optimizeScoreType + " : " + finalScore);
			}
			return finalScore;
		}

		/// <summary>Run and score coref distributed</summary>
		/// <exception cref="System.Exception"/>
		public static void RunAndScoreCorefDist(string runDistCmd, Properties props, string propsFile)
		{
			PrintWriter pw = IOUtils.GetPrintWriter(propsFile);
			props.Store(pw, null);
			pw.Close();
			/* Run coref job in a distributed manner, score is written to file */
			IList<string> cmd = new List<string>();
			Sharpen.Collections.AddAll(cmd, Arrays.AsList(runDistCmd.Split("\\s+")));
			cmd.Add("-props");
			cmd.Add(propsFile);
			ProcessBuilder pb = new ProcessBuilder(cmd);
			// Copy environment variables over
			IDictionary<string, string> curEnv = Runtime.Getenv();
			IDictionary<string, string> pbEnv = pb.Environment();
			pbEnv.PutAll(curEnv);
			logger.Info("Running distributed coref:" + StringUtils.Join(pb.Command(), " "));
			StringWriter outSos = new StringWriter();
			StringWriter errSos = new StringWriter();
			PrintWriter @out = new PrintWriter(new BufferedWriter(outSos));
			PrintWriter err = new PrintWriter(new BufferedWriter(errSos));
			SystemUtils.Run(pb, @out, err);
			@out.Close();
			err.Close();
			string outStr = outSos.ToString();
			string errStr = errSos.ToString();
			logger.Info("Finished distributed coref: " + runDistCmd + ", props=" + propsFile);
			logger.Info("Output: " + outStr);
			if (errStr.Length > 0)
			{
				logger.Info("Error: " + errStr);
			}
		}

		/// <exception cref="System.Exception"/>
		internal static bool WaitForFiles(File workDir, IFileFilter fileFilter, int howMany)
		{
			logger.Info("Waiting until we see " + howMany + " " + fileFilter + " files in directory " + workDir + "...");
			int seconds = 0;
			while (true)
			{
				File[] checkFiles = workDir.ListFiles(fileFilter);
				// we found the required number of .check files
				if (checkFiles != null && checkFiles.Length >= howMany)
				{
					logger.Info("Found " + checkFiles.Length + " " + fileFilter + " files. Continuing execution.");
					break;
				}
				// sleep for while before the next check
				Thread.Sleep(Constants.MonitorDistCmdFinishedWaitMillis);
				seconds += Constants.MonitorDistCmdFinishedWaitMillis / 1000;
				if (seconds % 600 == 0)
				{
					double minutes = seconds / 60;
					logger.Info("Still waiting... " + minutes + " minutes have passed.");
				}
			}
			return true;
		}

		private static int FromSieveNameToIndex(string sieveName, string[] sieveNames)
		{
			if ("*".Equals(sieveName))
			{
				return -1;
			}
			for (int i = 0; i < sieveNames.Length; i++)
			{
				if (sieveNames[i].Equals(sieveName))
				{
					return i;
				}
			}
			throw new ArgumentException("Invalid sieve name: " + sieveName);
		}

		private static Pair<int, int> FromSieveOrderConstraintString(string s, string[] sieveNames)
		{
			string[] parts = s.Split("<");
			if (parts.Length == 2)
			{
				string first = parts[0].Trim();
				string second = parts[1].Trim();
				int a = FromSieveNameToIndex(first, sieveNames);
				int b = FromSieveNameToIndex(second, sieveNames);
				return new Pair<int, int>(a, b);
			}
			else
			{
				throw new ArgumentException("Invalid sieve ordering constraint: " + s);
			}
		}

		private static string ToSieveOrderConstraintString(Pair<int, int> orderedSieveIndices, string[] sieveNames)
		{
			string first = (orderedSieveIndices.First() < 0) ? "*" : sieveNames[orderedSieveIndices.First()];
			string second = (orderedSieveIndices.Second() < 0) ? "*" : sieveNames[orderedSieveIndices.Second()];
			return first + " < " + second;
		}

		/// <summary>
		/// Given a set of sieves, select an optimal ordering for the sieves
		/// by iterating over sieves, and selecting the one that gives the best score and
		/// adding sieves one at a time until no more sieves left
		/// </summary>
		/// <exception cref="System.Exception"/>
		public virtual void OptimizeSieveOrdering(MentionExtractor mentionExtractor, Properties props, string timestamp)
		{
			logger.Info("=============SIEVE OPTIMIZATION START ====================");
			logger.Info("Optimize sieves using score: " + optimizeScoreType);
			IFileFilter scoreFilesFilter = new _IFileFilter_634();
			Pattern scoreFilePattern = Pattern.Compile(".*sieves\\.(\\d+)\\.(\\d+).score");
			string runDistributedCmd = props.GetProperty(Constants.RunDistCmdProp);
			string mainWorkDirPath = props.GetProperty(Constants.RunDistCmdWorkDir, "workdir") + "-" + timestamp + File.separator;
			DeterministicCorefSieve[] origSieves = sieves;
			string[] origSieveNames = sieveClassNames;
			ICollection<int> remainingSieveIndices = Generics.NewHashSet();
			for (int i = 0; i < origSieves.Length; i++)
			{
				remainingSieveIndices.Add(i);
			}
			IList<int> optimizedOrdering = new List<int>();
			while (!remainingSieveIndices.IsEmpty())
			{
				// initialize array of current sieves
				int curSievesNumber = optimizedOrdering.Count;
				sieves = new DeterministicCorefSieve[curSievesNumber + 1];
				sieveClassNames = new string[curSievesNumber + 1];
				for (int i_1 = 0; i_1 < curSievesNumber; i_1++)
				{
					sieves[i_1] = origSieves[optimizedOrdering[i_1]];
					sieveClassNames[i_1] = origSieveNames[optimizedOrdering[i_1]];
				}
				logger.Info("*** Optimizing Sieve ordering for pass " + curSievesNumber + " ***");
				// Get list of sieves that we can pick from for the next sieve
				ICollection<int> selectableSieveIndices = new TreeSet<int>(remainingSieveIndices);
				// Based on ordering constraints remove sieves from options
				if (sievesKeepOrder != null)
				{
					foreach (Pair<int, int> ko in sievesKeepOrder)
					{
						if (ko.Second() < 0)
						{
							if (remainingSieveIndices.Contains(ko.First()))
							{
								logger.Info("Restrict selection to " + origSieveNames[ko.First()] + " because of constraint " + ToSieveOrderConstraintString(ko, origSieveNames));
								selectableSieveIndices = Generics.NewHashSet(1);
								selectableSieveIndices.Add(ko.First());
								break;
							}
						}
						else
						{
							if (ko.First() < 0 && remainingSieveIndices.Count > 1)
							{
								if (remainingSieveIndices.Contains(ko.Second()))
								{
									logger.Info("Remove selection " + origSieveNames[ko.Second()] + " because of constraint " + ToSieveOrderConstraintString(ko, origSieveNames));
									selectableSieveIndices.Remove(ko.Second());
								}
							}
							else
							{
								if (remainingSieveIndices.Contains(ko.First()))
								{
									if (remainingSieveIndices.Contains(ko.Second()))
									{
										logger.Info("Remove selection " + origSieveNames[ko.Second()] + " because of constraint " + ToSieveOrderConstraintString(ko, origSieveNames));
										selectableSieveIndices.Remove(ko.Second());
									}
								}
							}
						}
					}
				}
				if (selectableSieveIndices.IsEmpty())
				{
					throw new Exception("Unable to find sieve ordering to satisfy all ordering constraints!!!!");
				}
				int selected = -1;
				if (selectableSieveIndices.Count > 1)
				{
					// Go through remaining sieves and see how well they do
					IList<Pair<double, int>> scores = new List<Pair<double, int>>();
					if (runDistributedCmd != null)
					{
						string workDirPath = mainWorkDirPath + curSievesNumber + File.separator;
						File workDir = new File(workDirPath);
						workDir.Mkdirs();
						workDirPath = workDir.GetAbsolutePath() + File.separator;
						// Start jobs
						foreach (int potentialSieveIndex in selectableSieveIndices)
						{
							string sieveSelectionId = curSievesNumber + "." + potentialSieveIndex;
							string jobDirPath = workDirPath + sieveSelectionId + File.separator;
							File jobDir = new File(jobDirPath);
							jobDir.Mkdirs();
							Properties newProps = new Properties();
							foreach (string key in props.StringPropertyNames())
							{
								string value = props.GetProperty(key);
								value = value.ReplaceAll("\\$\\{JOBDIR\\}", jobDirPath);
								newProps.SetProperty(key, value);
							}
							// try this sieve and see how well it works
							sieves[curSievesNumber] = origSieves[potentialSieveIndex];
							sieveClassNames[curSievesNumber] = origSieveNames[potentialSieveIndex];
							newProps.SetProperty(Constants.OptimizeSievesProp, "false");
							newProps.SetProperty(Constants.ScoreProp, "true");
							newProps.SetProperty(Constants.SievesProp, StringUtils.Join(sieveClassNames, ","));
							newProps.SetProperty(Constants.LogProp, jobDirPath + "sieves." + sieveSelectionId + ".log");
							newProps.SetProperty(Constants.ScoreFileProp, workDirPath + "sieves." + sieveSelectionId + ".score");
							if (Constants.PrintConllOutput || replicateCoNLL)
							{
								newProps.SetProperty(Constants.ConllOutputProp, jobDirPath + "sieves." + sieveSelectionId + ".conlloutput");
							}
							string distCmd = newProps.GetProperty(Constants.RunDistCmdProp, runDistributedCmd);
							RunAndScoreCorefDist(distCmd, newProps, workDirPath + "sieves." + sieveSelectionId + ".props");
						}
						// Wait for jobs to finish and collect scores
						WaitForFiles(workDir, scoreFilesFilter, selectableSieveIndices.Count);
						// Get scores
						File[] scoreFiles = workDir.ListFiles(scoreFilesFilter);
						foreach (File file in scoreFiles)
						{
							Matcher m = scoreFilePattern.Matcher(file.GetName());
							if (m.Matches())
							{
								int potentialSieveIndex_1 = System.Convert.ToInt32(m.Group(2));
								string text = IOUtils.SlurpFile(file);
								double score = double.ParseDouble(text);
								// keeps scores so we can select best score and log them
								scores.Add(new Pair<double, int>(score, potentialSieveIndex_1));
							}
							else
							{
								throw new Exception("Bad score file name: " + file);
							}
						}
					}
					else
					{
						foreach (int potentialSieveIndex in selectableSieveIndices)
						{
							// try this sieve and see how well it works
							sieves[curSievesNumber] = origSieves[potentialSieveIndex];
							sieveClassNames[curSievesNumber] = origSieveNames[potentialSieveIndex];
							logger.Info("Trying sieve " + curSievesNumber + "=" + sieveClassNames[curSievesNumber] + ": ");
							logger.Info(" Trying sieves: " + StringUtils.Join(sieveClassNames, ","));
							double score = RunAndScoreCoref(this, mentionExtractor, props, timestamp);
							// keeps scores so we can select best score and log them
							scores.Add(new Pair<double, int>(score, potentialSieveIndex));
							logger.Info(" Trying sieves: " + StringUtils.Join(sieveClassNames, ","));
							logger.Info(" Trying sieves score: " + score);
						}
					}
					// Select bestScore
					double bestScore = -1;
					foreach (Pair<double, int> p in scores)
					{
						if (selected < 0 || p.First() > bestScore)
						{
							bestScore = p.First();
							selected = p.Second();
						}
					}
					// log ordered scores
					scores.Sort();
					Java.Util.Collections.Reverse(scores);
					logger.Info("Ordered sieves");
					foreach (Pair<double, int> p_1 in scores)
					{
						logger.Info("Sieve optimization pass " + curSievesNumber + " scores: Sieve=" + origSieveNames[p_1.Second()] + ", score=" + p_1.First());
					}
				}
				else
				{
					// Only one sieve
					logger.Info("Only one choice for next sieve");
					selected = selectableSieveIndices.GetEnumerator().Current;
				}
				// log sieve we are adding
				sieves[curSievesNumber] = origSieves[selected];
				sieveClassNames[curSievesNumber] = origSieveNames[selected];
				logger.Info("Adding sieve " + curSievesNumber + "=" + sieveClassNames[curSievesNumber] + " to existing sieves: ");
				logger.Info(" Current Sieves: " + StringUtils.Join(sieveClassNames, ","));
				// select optimal sieve and add it to our optimized ordering
				optimizedOrdering.Add(selected);
				remainingSieveIndices.Remove(selected);
			}
			logger.Info("Final Sieve Ordering: " + StringUtils.Join(sieveClassNames, ","));
			logger.Info("=============SIEVE OPTIMIZATION DONE ====================");
		}

		private sealed class _IFileFilter_634 : IFileFilter
		{
			public _IFileFilter_634()
			{
			}

			public bool Accept(File file)
			{
				return file.GetAbsolutePath().EndsWith(".score");
			}

			public override string ToString()
			{
				return ".score";
			}
		}

		/// <summary>Extracts coreference clusters.</summary>
		/// <remarks>
		/// Extracts coreference clusters.
		/// This is the main API entry point for coreference resolution.
		/// Return a map from CorefChain ID to corresponding CorefChain.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual IDictionary<int, CorefChain> Coref(Document document)
		{
			// Multi-pass sieve coreference resolution
			for (int i = 0; i < sieves.Length; i++)
			{
				currentSieve = i;
				DeterministicCorefSieve sieve = sieves[i];
				// Do coreference resolution using this pass
				Coreference(document, sieve);
			}
			// post processing (e.g., removing singletons, appositions for conll)
			if ((!Constants.UseGoldMentions && doPostProcessing) || replicateCoNLL)
			{
				PostProcessing(document);
			}
			// coref system output: CorefChain
			IDictionary<int, CorefChain> result = Generics.NewHashMap();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				result[c.clusterID] = new CorefChain(c, document.positions);
			}
			return result;
		}

		/// <exception cref="System.Exception"/>
		public virtual IDictionary<int, CorefChain> CorefReturnHybridOutput(Document document)
		{
			// Multi-pass sieve coreference resolution
			for (int i = 0; i < sieves.Length; i++)
			{
				currentSieve = i;
				DeterministicCorefSieve sieve = sieves[i];
				// Do coreference resolution using this pass
				Coreference(document, sieve);
			}
			// post processing (e.g., removing singletons, appositions for conll)
			if ((!Constants.UseGoldMentions && doPostProcessing) || replicateCoNLL)
			{
				PostProcessing(document);
			}
			// coref system output: edu.stanford.nlp.hcoref.data.CorefChain
			IDictionary<int, CorefChain> result = Generics.NewHashMap();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				// build mentionsMap and represents
				IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentionsMap = Generics.NewHashMap();
				IntPair keyPair = new IntPair(0, 0);
				mentionsMap[keyPair] = new HashSet<CorefChain.CorefMention>();
				Mention represents = null;
				CorefChain.CorefMention representsHybridVersion = null;
				foreach (Mention mention in c.GetCorefMentions())
				{
					// convert dcoref CorefMention to hcoref CorefMention
					//IntPair mentionPosition = new IntPair(mention.sentNum, mention.headIndex);
					IntTuple mentionPosition = document.positions[mention];
					CorefChain.CorefMention dcorefMention = new CorefChain.CorefMention(mention, mentionPosition);
					// tokens need the hcoref version of CorefClusterIdAnnotation
					mention.headWord.Set(typeof(CorefCoreAnnotations.CorefClusterIdAnnotation), mention.corefClusterID);
					// drop the dcoref version of CorefClusterIdAnnotation
					mention.headWord.Remove(typeof(CorefCoreAnnotations.CorefClusterIdAnnotation));
					// make the hcoref mention
					CorefChain.CorefMention hcorefMention = new CorefChain.CorefMention(Dictionaries.MentionType.ValueOf(dcorefMention.mentionType.ToString()), Dictionaries.Number.ValueOf(dcorefMention.number.ToString()), Dictionaries.Gender.ValueOf(dcorefMention
						.gender.ToString()), Dictionaries.Animacy.ValueOf(dcorefMention.animacy.ToString()), dcorefMention.startIndex, dcorefMention.endIndex, dcorefMention.headIndex, dcorefMention.corefClusterID, dcorefMention.mentionID, dcorefMention.sentNum, dcorefMention
						.position, dcorefMention.mentionSpan);
					mentionsMap[keyPair].Add(hcorefMention);
					if (mention.MoreRepresentativeThan(represents))
					{
						represents = mention;
						representsHybridVersion = hcorefMention;
					}
				}
				CorefChain hybridCorefChain = new CorefChain(c.clusterID, mentionsMap, representsHybridVersion);
				result[c.clusterID] = hybridCorefChain;
			}
			return result;
		}

		/// <summary>Do coreference resolution using one sieve pass.</summary>
		/// <param name="document">An extracted document</param>
		/// <exception cref="System.Exception"/>
		private void Coreference(Document document, DeterministicCorefSieve sieve)
		{
			//Redwood.forceTrack("Coreference: sieve " + sieve.getClass().getSimpleName());
			logger.Finer("Coreference: sieve " + sieve.GetType().GetSimpleName());
			IList<IList<Mention>> orderedMentionsBySentence = document.GetOrderedMentions();
			IDictionary<int, CorefCluster> corefClusters = document.corefClusters;
			ICollection<Mention> roleSet = document.roleSet;
			logger.Finest("ROLE SET (Skip exact string match): ------------------");
			foreach (Mention m in roleSet)
			{
				logger.Finest("\t" + m.SpanToString());
			}
			logger.Finest("-------------------------------------------------------");
			additionalCorrectLinksCount = 0;
			additionalLinksCount = 0;
			for (int sentI = 0; sentI < orderedMentionsBySentence.Count; sentI++)
			{
				IList<Mention> orderedMentions = orderedMentionsBySentence[sentI];
				for (int mentionI = 0; mentionI < orderedMentions.Count; mentionI++)
				{
					Mention m1 = orderedMentions[mentionI];
					// check for skip: first mention only, discourse salience
					if (sieve.SkipThisMention(document, m1, corefClusters[m1.corefClusterID], dictionaries))
					{
						continue;
					}
					for (int sentJ = sentI; sentJ >= 0; sentJ--)
					{
						IList<Mention> l = sieve.GetOrderedAntecedents(sentJ, sentI, orderedMentions, orderedMentionsBySentence, m1, mentionI, corefClusters, dictionaries);
						if (maxSentDist != -1 && sentI - sentJ > maxSentDist)
						{
							continue;
						}
						// Sort mentions by length whenever we have two mentions beginning at the same position and having the same head
						for (int i = 0; i < l.Count; i++)
						{
							for (int j = 0; j < l.Count; j++)
							{
								if (l[i].headString.Equals(l[j].headString) && l[i].startIndex == l[j].startIndex && l[i].SameSentence(l[j]) && j > i && l[i].SpanToString().Length > l[j].SpanToString().Length)
								{
									logger.Finest("FLIPPED: " + l[i].SpanToString() + "(" + i + "), " + l[j].SpanToString() + "(" + j + ")");
									l.Set(j, l.Set(i, l[j]));
								}
							}
						}
						foreach (Mention m2 in l)
						{
							// m2 - antecedent of m1                   l
							// Skip singletons according to the singleton predictor
							// (only for non-NE mentions)
							// Recasens, de Marneffe, and Potts (NAACL 2013)
							if (m1.isSingleton && m1.mentionType != Dictionaries.MentionType.Proper && m2.isSingleton && m2.mentionType != Dictionaries.MentionType.Proper)
							{
								continue;
							}
							if (m1.corefClusterID == m2.corefClusterID)
							{
								continue;
							}
							CorefCluster c1 = corefClusters[m1.corefClusterID];
							CorefCluster c2 = corefClusters[m2.corefClusterID];
							if (c2 == null)
							{
								logger.Warning("NO corefcluster id " + m2.corefClusterID);
							}
							System.Diagnostics.Debug.Assert((c1 != null));
							System.Diagnostics.Debug.Assert((c2 != null));
							if (sieve.UseRoleSkip())
							{
								if (m1.IsRoleAppositive(m2, dictionaries))
								{
									roleSet.Add(m1);
								}
								else
								{
									if (m2.IsRoleAppositive(m1, dictionaries))
									{
										roleSet.Add(m2);
									}
								}
								continue;
							}
							if (sieve.Coreferent(document, c1, c2, m1, m2, dictionaries, roleSet, semantics))
							{
								// print logs for analysis
								if (DoScore())
								{
									PrintLogs(c1, c2, m1, m2, document, currentSieve);
								}
								int removeID = c1.clusterID;
								CorefCluster.MergeClusters(c2, c1);
								document.MergeIncompatibles(c2, c1);
								document.MergeAcronymCache(c2, c1);
								//                logger.warning("Removing cluster " + removeID + ", merged with " + c2.getClusterID());
								Sharpen.Collections.Remove(corefClusters, removeID);
								goto LOOP_break;
							}
						}
LOOP_continue: ;
					}
LOOP_break: ;
				}
			}
			// End of "LOOP"
			// scoring
			if (DoScore())
			{
				scoreMUC[currentSieve].CalculateScore(document);
				scoreBcubed[currentSieve].CalculateScore(document);
				scorePairwise[currentSieve].CalculateScore(document);
				if (currentSieve == 0)
				{
					scoreSingleDoc = new List<CorefScorer>();
					scoreSingleDoc.Add(new ScorerPairwise());
					scoreSingleDoc[currentSieve].CalculateScore(document);
					additionalCorrectLinksCount = (int)scoreSingleDoc[currentSieve].precisionNumSum;
					additionalLinksCount = (int)scoreSingleDoc[currentSieve].precisionDenSum;
				}
				else
				{
					scoreSingleDoc.Add(new ScorerPairwise());
					scoreSingleDoc[currentSieve].CalculateScore(document);
					additionalCorrectLinksCount = (int)(scoreSingleDoc[currentSieve].precisionNumSum - scoreSingleDoc[currentSieve - 1].precisionNumSum);
					additionalLinksCount = (int)(scoreSingleDoc[currentSieve].precisionDenSum - scoreSingleDoc[currentSieve - 1].precisionDenSum);
				}
				linksCountInPass[currentSieve].SetFirst(linksCountInPass[currentSieve].First() + additionalCorrectLinksCount);
				linksCountInPass[currentSieve].SetSecond(linksCountInPass[currentSieve].Second() + additionalLinksCount);
				PrintSieveScore(document, sieve);
			}
		}

		//Redwood.endTrack("Coreference: sieve " + sieve.getClass().getSimpleName());
		/// <summary>Remove singletons, appositive, predicate nominatives, relative pronouns</summary>
		private static void PostProcessing(Document document)
		{
			ICollection<Mention> removeSet = Generics.NewHashSet();
			ICollection<int> removeClusterSet = Generics.NewHashSet();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				ICollection<Mention> removeMentions = Generics.NewHashSet();
				foreach (Mention m in c.GetCorefMentions())
				{
					if (Constants.RemoveAppositionPredicatenominatives && ((m.appositions != null && m.appositions.Count > 0) || (m.predicateNominatives != null && m.predicateNominatives.Count > 0) || (m.relativePronouns != null && m.relativePronouns.Count > 0)
						))
					{
						removeMentions.Add(m);
						removeSet.Add(m);
						m.corefClusterID = m.mentionID;
					}
				}
				c.corefMentions.RemoveAll(removeMentions);
				if (Constants.RemoveSingletons && c.GetCorefMentions().Count == 1)
				{
					removeClusterSet.Add(c.clusterID);
				}
			}
			foreach (int removeId in removeClusterSet)
			{
				Sharpen.Collections.Remove(document.corefClusters, removeId);
			}
			foreach (Mention m_1 in removeSet)
			{
				Sharpen.Collections.Remove(document.positions, m_1);
			}
		}

		public static LogisticClassifier<string, string> GetSingletonPredictorFromSerializedFile(string serializedFile)
		{
			try
			{
				ObjectInputStream ois = IOUtils.ReadStreamFromString(serializedFile);
				object o = ois.ReadObject();
				if (o is LogisticClassifier<object, object>)
				{
					return (LogisticClassifier<string, string>)o;
				}
				throw new InvalidCastException("Wanted SingletonPredictor, got " + o.GetType());
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
		}

		/// <summary>Remove singleton clusters</summary>
		public static IList<IList<Mention>> FilterMentionsWithSingletonClusters(Document document, IList<IList<Mention>> mentions)
		{
			IList<IList<Mention>> res = new List<IList<Mention>>(mentions.Count);
			foreach (IList<Mention> ml in mentions)
			{
				IList<Mention> filtered = new List<Mention>();
				foreach (Mention m in ml)
				{
					CorefCluster cluster = document.corefClusters[m.corefClusterID];
					if (cluster != null && cluster.GetCorefMentions().Count > 1)
					{
						filtered.Add(m);
					}
				}
				res.Add(filtered);
			}
			return res;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void RunConllEval(string conllMentionEvalScript, string goldFile, string predictFile, string evalFile, string errFile)
		{
			ProcessBuilder process = new ProcessBuilder(conllMentionEvalScript, "all", goldFile, predictFile);
			PrintWriter @out = new PrintWriter(new FileOutputStream(evalFile));
			PrintWriter err = new PrintWriter(new FileOutputStream(errFile));
			SystemUtils.Run(process, @out, err);
			@out.Close();
			err.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static string GetConllEvalSummary(string conllMentionEvalScript, string goldFile, string predictFile)
		{
			ProcessBuilder process = new ProcessBuilder(conllMentionEvalScript, "all", goldFile, predictFile, "none");
			StringOutputStream errSos = new StringOutputStream();
			StringOutputStream outSos = new StringOutputStream();
			PrintWriter @out = new PrintWriter(outSos);
			PrintWriter err = new PrintWriter(errSos);
			SystemUtils.Run(process, @out, err);
			@out.Close();
			err.Close();
			string summary = outSos.ToString();
			string errStr = errSos.ToString();
			if (!errStr.IsEmpty())
			{
				summary += "\nERROR: " + errStr;
			}
			Pattern pattern = Pattern.Compile("\\d+\\.\\d\\d\\d+");
			DecimalFormat df = new DecimalFormat("#.##");
			Matcher matcher = pattern.Matcher(summary);
			while (matcher.Find())
			{
				string number = matcher.Group();
				summary = summary.ReplaceFirst(number, df.Format(double.ParseDouble(number)));
			}
			return summary;
		}

		/// <summary>Print logs for error analysis</summary>
		public virtual void PrintTopK(Logger logger, Document document, Edu.Stanford.Nlp.Dcoref.Semantics semantics)
		{
			IList<IList<Mention>> orderedMentionsBySentence = document.GetOrderedMentions();
			IDictionary<int, CorefCluster> corefClusters = document.corefClusters;
			IDictionary<Mention, IntTuple> positions = document.allPositions;
			IDictionary<int, Mention> golds = document.allGoldMentions;
			logger.Fine("=======ERROR ANALYSIS=========================================================");
			// Temporary sieve for getting ordered antecedents
			DeterministicCorefSieve tmpSieve = new ExactStringMatch();
			for (int i = 0; i < orderedMentionsBySentence.Count; i++)
			{
				IList<Mention> orderedMentions = orderedMentionsBySentence[i];
				for (int j = 0; j < orderedMentions.Count; j++)
				{
					Mention m = orderedMentions[j];
					logger.Fine("=========Line: " + i + "\tmention: " + j + "=======================================================");
					logger.Fine(m.SpanToString() + "\tmentionID: " + m.mentionID + "\tcorefClusterID: " + m.corefClusterID + "\tgoldCorefClusterID: " + m.goldCorefClusterID);
					CorefCluster corefCluster = corefClusters[m.corefClusterID];
					if (corefCluster != null)
					{
						corefCluster.PrintCorefCluster(logger);
					}
					else
					{
						logger.Finer("CANNOT find coref cluster for cluster " + m.corefClusterID);
					}
					logger.Fine("-------------------------------------------------------");
					bool oneRecallErrorPrinted = false;
					bool onePrecisionErrorPrinted = false;
					bool alreadyChoose = false;
					for (int sentJ = i; sentJ >= 0; sentJ--)
					{
						IList<Mention> l = tmpSieve.GetOrderedAntecedents(sentJ, i, orderedMentions, orderedMentionsBySentence, m, j, corefClusters, dictionaries);
						// Sort mentions by length whenever we have two mentions beginning at the same position and having the same head
						for (int ii = 0; ii < l.Count; ii++)
						{
							for (int jj = 0; jj < l.Count; jj++)
							{
								if (l[ii].headString.Equals(l[jj].headString) && l[ii].startIndex == l[jj].startIndex && l[ii].SameSentence(l[jj]) && jj > ii && l[ii].SpanToString().Length > l[jj].SpanToString().Length)
								{
									logger.Finest("FLIPPED: " + l[ii].SpanToString() + "(" + ii + "), " + l[jj].SpanToString() + "(" + jj + ")");
									l.Set(jj, l.Set(ii, l[jj]));
								}
							}
						}
						logger.Finest("Candidates in sentence #" + sentJ + " for mention: " + m.SpanToString());
						for (int ii_1 = 0; ii_1 < l.Count; ii_1++)
						{
							logger.Finest("\tCandidate #" + ii_1 + ": " + l[ii_1].SpanToString());
						}
						foreach (Mention antecedent in l)
						{
							bool chosen = (m.corefClusterID == antecedent.corefClusterID);
							IntTuple src = new IntTuple(2);
							src.Set(0, i);
							src.Set(1, j);
							IntTuple ant = positions[antecedent];
							if (ant == null)
							{
								continue;
							}
							//correct=(chosen==goldLinks.contains(new Pair<IntTuple, IntTuple>(src,ant)));
							bool coreferent = golds.Contains(m.mentionID) && golds.Contains(antecedent.mentionID) && (golds[m.mentionID].goldCorefClusterID == golds[antecedent.mentionID].goldCorefClusterID);
							bool correct = (chosen == coreferent);
							string chosenness = chosen ? "Chosen" : "Not Chosen";
							string correctness = correct ? "Correct" : "Incorrect";
							logger.Fine("\t" + correctness + "\t\t" + chosenness + "\t" + antecedent.SpanToString());
							CorefCluster mC = corefClusters[m.corefClusterID];
							CorefCluster aC = corefClusters[antecedent.corefClusterID];
							if (chosen && !correct && !onePrecisionErrorPrinted && !alreadyChoose)
							{
								onePrecisionErrorPrinted = true;
								PrintLinkWithContext(logger, "\nPRECISION ERROR ", src, ant, document, semantics);
								logger.Fine("END of PRECISION ERROR LOG");
							}
							if (!chosen && !correct && !oneRecallErrorPrinted && (!alreadyChoose || (alreadyChoose && onePrecisionErrorPrinted)))
							{
								oneRecallErrorPrinted = true;
								PrintLinkWithContext(logger, "\nRECALL ERROR ", src, ant, document, semantics);
								logger.Finer("cluster info: ");
								if (mC != null)
								{
									mC.PrintCorefCluster(logger);
								}
								else
								{
									logger.Finer("CANNOT find coref cluster for cluster " + m.corefClusterID);
								}
								logger.Finer("----------------------------------------------------------");
								if (aC != null)
								{
									aC.PrintCorefCluster(logger);
								}
								else
								{
									logger.Finer("CANNOT find coref cluster for cluster " + m.corefClusterID);
								}
								logger.Finer(string.Empty);
								logger.Fine("END of RECALL ERROR LOG");
							}
							if (chosen)
							{
								alreadyChoose = true;
							}
						}
					}
					logger.Fine("\n");
				}
			}
			logger.Fine("===============================================================================");
		}

		public virtual void PrintF1(bool printF1First)
		{
			scoreMUC[sieveClassNames.Length - 1].PrintF1(logger, printF1First);
			scoreBcubed[sieveClassNames.Length - 1].PrintF1(logger, printF1First);
			scorePairwise[sieveClassNames.Length - 1].PrintF1(logger, printF1First);
		}

		private void PrintSieveScore(Document document, DeterministicCorefSieve sieve)
		{
			logger.Fine("===========================================");
			logger.Fine("pass" + currentSieve + ": " + sieve.FlagsToString());
			scoreMUC[currentSieve].PrintF1(logger);
			scoreBcubed[currentSieve].PrintF1(logger);
			scorePairwise[currentSieve].PrintF1(logger);
			logger.Fine("# of Clusters: " + document.corefClusters.Count + ",\t# of additional links: " + additionalLinksCount + ",\t# of additional correct links: " + additionalCorrectLinksCount + ",\tprecision of new links: " + 1.0 * additionalCorrectLinksCount
				 / additionalLinksCount);
			logger.Fine("# of total additional links: " + linksCountInPass[currentSieve].Second() + ",\t# of total additional correct links: " + linksCountInPass[currentSieve].First() + ",\taccumulated precision of this pass: " + 1.0 * linksCountInPass[
				currentSieve].First() / linksCountInPass[currentSieve].Second());
			logger.Fine("--------------------------------------");
		}

		/// <summary>Print coref link info</summary>
		private static void PrintLink(Logger logger, string header, IntTuple src, IntTuple dst, IList<IList<Mention>> orderedMentionsBySentence)
		{
			Mention srcMention = orderedMentionsBySentence[src.Get(0)][src.Get(1)];
			Mention dstMention = orderedMentionsBySentence[dst.Get(0)][dst.Get(1)];
			if (src.Get(0) == dst.Get(0))
			{
				logger.Fine(header + ": [" + srcMention.SpanToString() + "](id=" + srcMention.mentionID + ") in sent #" + src.Get(0) + " => [" + dstMention.SpanToString() + "](id=" + dstMention.mentionID + ") in sent #" + dst.Get(0) + " Same Sentence");
			}
			else
			{
				logger.Fine(header + ": [" + srcMention.SpanToString() + "](id=" + srcMention.mentionID + ") in sent #" + src.Get(0) + " => [" + dstMention.SpanToString() + "](id=" + dstMention.mentionID + ") in sent #" + dst.Get(0));
			}
		}

		protected internal static void PrintList(Logger logger, params string[] args)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string arg in args)
			{
				sb.Append(arg);
				sb.Append('\t');
			}
			logger.Fine(sb.ToString());
		}

		/// <summary>print a coref link information including context and parse tree</summary>
		private static void PrintLinkWithContext(Logger logger, string header, IntTuple src, IntTuple dst, Document document, Edu.Stanford.Nlp.Dcoref.Semantics semantics)
		{
			IList<IList<Mention>> orderedMentionsBySentence = document.GetOrderedMentions();
			IList<IList<Mention>> goldOrderedMentionsBySentence = document.goldOrderedMentionsBySentence;
			Mention srcMention = orderedMentionsBySentence[src.Get(0)][src.Get(1)];
			Mention dstMention = orderedMentionsBySentence[dst.Get(0)][dst.Get(1)];
			IList<CoreLabel> srcSentence = srcMention.sentenceWords;
			IList<CoreLabel> dstSentence = dstMention.sentenceWords;
			PrintLink(logger, header, src, dst, orderedMentionsBySentence);
			PrintList(logger, "Mention:" + srcMention.SpanToString(), "Gender:" + srcMention.gender.ToString(), "Number:" + srcMention.number.ToString(), "Animacy:" + srcMention.animacy.ToString(), "Person:" + srcMention.person.ToString(), "NER:" + srcMention
				.nerString, "Head:" + srcMention.headString, "Type:" + srcMention.mentionType.ToString(), "utter: " + srcMention.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)), "speakerID: " + srcMention.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation
				)), "twinless:" + srcMention.twinless);
			logger.Fine("Context:");
			string p = string.Empty;
			for (int i = 0; i < srcSentence.Count; i++)
			{
				if (i == srcMention.startIndex)
				{
					p += "[";
				}
				if (i == srcMention.endIndex)
				{
					p += "]";
				}
				p += srcSentence[i].Word() + " ";
			}
			logger.Fine(p);
			StringBuilder golds = new StringBuilder();
			golds.Append("Gold mentions in the sentence:\n");
			ICounter<int> mBegin = new ClassicCounter<int>();
			ICounter<int> mEnd = new ClassicCounter<int>();
			foreach (Mention m in goldOrderedMentionsBySentence[src.Get(0)])
			{
				mBegin.IncrementCount(m.startIndex);
				mEnd.IncrementCount(m.endIndex);
			}
			IList<CoreLabel> l = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[src.Get(0)].Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i_1 = 0; i_1 < l.Count; i_1++)
			{
				for (int j = 0; j < mEnd.GetCount(i_1); j++)
				{
					golds.Append("]");
				}
				for (int j_1 = 0; j_1 < mBegin.GetCount(i_1); j_1++)
				{
					golds.Append("[");
				}
				golds.Append(l[i_1].Get(typeof(CoreAnnotations.TextAnnotation)));
				golds.Append(" ");
			}
			logger.Fine(golds.ToString());
			PrintList(logger, "\nAntecedent:" + dstMention.SpanToString(), "Gender:" + dstMention.gender.ToString(), "Number:" + dstMention.number.ToString(), "Animacy:" + dstMention.animacy.ToString(), "Person:" + dstMention.person.ToString(), "NER:" +
				 dstMention.nerString, "Head:" + dstMention.headString, "Type:" + dstMention.mentionType.ToString(), "utter: " + dstMention.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)), "speakerID: " + dstMention.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation
				)), "twinless:" + dstMention.twinless);
			logger.Fine("Context:");
			p = string.Empty;
			for (int i_2 = 0; i_2 < dstSentence.Count; i_2++)
			{
				if (i_2 == dstMention.startIndex)
				{
					p += "[";
				}
				if (i_2 == dstMention.endIndex)
				{
					p += "]";
				}
				p += dstSentence[i_2].Word() + " ";
			}
			logger.Fine(p);
			golds = new StringBuilder();
			golds.Append("Gold mentions in the sentence:\n");
			mBegin = new ClassicCounter<int>();
			mEnd = new ClassicCounter<int>();
			foreach (Mention m_1 in goldOrderedMentionsBySentence[dst.Get(0)])
			{
				mBegin.IncrementCount(m_1.startIndex);
				mEnd.IncrementCount(m_1.endIndex);
			}
			l = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[dst.Get(0)].Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i_3 = 0; i_3 < l.Count; i_3++)
			{
				for (int j = 0; j < mEnd.GetCount(i_3); j++)
				{
					golds.Append("]");
				}
				for (int j_1 = 0; j_1 < mBegin.GetCount(i_3); j_1++)
				{
					golds.Append("[");
				}
				golds.Append(l[i_3].Get(typeof(CoreAnnotations.TextAnnotation)));
				golds.Append(" ");
			}
			logger.Fine(golds.ToString());
			logger.Finer("\nMention:: --------------------------------------------------------");
			try
			{
				logger.Finer(srcMention.dependency.ToString());
			}
			catch (Exception)
			{
			}
			//throw new RuntimeException(e);}
			logger.Finer("Parse:");
			logger.Finer(FormatPennTree(srcMention.contextParseTree));
			logger.Finer("\nAntecedent:: -----------------------------------------------------");
			try
			{
				logger.Finer(dstMention.dependency.ToString());
			}
			catch (Exception)
			{
			}
			//throw new RuntimeException(e);}
			logger.Finer("Parse:");
			logger.Finer(FormatPennTree(dstMention.contextParseTree));
		}

		/// <summary>For printing tree in a better format</summary>
		private static string FormatPennTree(Tree parseTree)
		{
			string treeString = parseTree.PennString();
			treeString = treeString.ReplaceAll("\\[TextAnnotation=", string.Empty);
			treeString = treeString.ReplaceAll("(NamedEntityTag|Value|Index|PartOfSpeech)Annotation.+?\\)", ")");
			treeString = treeString.ReplaceAll("\\[.+?\\]", string.Empty);
			return treeString;
		}

		/// <summary>Print pass results</summary>
		private static void PrintLogs(CorefCluster c1, CorefCluster c2, Mention m1, Mention m2, Document document, int sieveIndex)
		{
			IDictionary<Mention, IntTuple> positions = document.positions;
			IList<IList<Mention>> orderedMentionsBySentence = document.GetOrderedMentions();
			IList<Pair<IntTuple, IntTuple>> goldLinks = document.GetGoldLinks();
			IntTuple p1 = positions[m1];
			System.Diagnostics.Debug.Assert((p1 != null));
			IntTuple p2 = positions[m2];
			System.Diagnostics.Debug.Assert((p2 != null));
			int menDist = 0;
			for (int i = p2.Get(0); i <= p1.Get(0); i++)
			{
				if (p1.Get(0) == p2.Get(0))
				{
					menDist = p1.Get(1) - p2.Get(1);
					break;
				}
				if (i == p2.Get(0))
				{
					menDist += orderedMentionsBySentence[p2.Get(0)].Count - p2.Get(1);
					continue;
				}
				if (i == p1.Get(0))
				{
					menDist += p1.Get(1);
					continue;
				}
				if (p2.Get(0) < i && i < p1.Get(0))
				{
					menDist += orderedMentionsBySentence[i].Count;
				}
			}
			string correct = (goldLinks.Contains(new Pair<IntTuple, IntTuple>(p1, p2))) ? "\tCorrect" : "\tIncorrect";
			logger.Finest("\nsentence distance: " + (p1.Get(0) - p2.Get(0)) + "\tmention distance: " + menDist + correct);
			if (!goldLinks.Contains(new Pair<IntTuple, IntTuple>(p1, p2)))
			{
				logger.Finer("-------Incorrect merge in pass" + sieveIndex + "::--------------------");
				c1.PrintCorefCluster(logger);
				logger.Finer("--------------------------------------------");
				c2.PrintCorefCluster(logger);
				logger.Finer("--------------------------------------------");
			}
			logger.Finer("antecedent: " + m2.SpanToString() + "(" + m2.mentionID + ")\tmention: " + m1.SpanToString() + "(" + m1.mentionID + ")\tsentDistance: " + Math.Abs(m1.sentNum - m2.sentNum) + "\t" + correct + " Pass" + sieveIndex + ":");
		}

		private static void PrintDiscourseStructure(Document document)
		{
			logger.Finer("DISCOURSE STRUCTURE==============================");
			logger.Finer("doc type: " + document.docType);
			int previousUtterIndex = -1;
			string previousSpeaker = string.Empty;
			StringBuilder sb = new StringBuilder();
			foreach (ICoreMap s in document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel l in s.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					int utterIndex = l.Get(typeof(CoreAnnotations.UtteranceAnnotation));
					string speaker = l.Get(typeof(CoreAnnotations.SpeakerAnnotation));
					string word = l.Get(typeof(CoreAnnotations.TextAnnotation));
					if (previousUtterIndex != utterIndex)
					{
						try
						{
							int previousSpeakerID = System.Convert.ToInt32(previousSpeaker);
							logger.Finer("\n<utter>: " + previousUtterIndex + " <speaker>: " + document.allPredictedMentions[previousSpeakerID].SpanToString());
						}
						catch (Exception)
						{
							logger.Finer("\n<utter>: " + previousUtterIndex + " <speaker>: " + previousSpeaker);
						}
						logger.Finer(sb.ToString());
						sb.Length = 0;
						previousUtterIndex = utterIndex;
						previousSpeaker = speaker;
					}
					sb.Append(" ").Append(word);
				}
				sb.Append("\n");
			}
			try
			{
				int previousSpeakerID = System.Convert.ToInt32(previousSpeaker);
				logger.Finer("\n<utter>: " + previousUtterIndex + " <speaker>: " + document.allPredictedMentions[previousSpeakerID].SpanToString());
			}
			catch (Exception)
			{
				logger.Finer("\n<utter>: " + previousUtterIndex + " <speaker>: " + previousSpeaker);
			}
			logger.Finer(sb.ToString());
			logger.Finer("END OF DISCOURSE STRUCTURE==============================");
		}

		private static void PrintScoreSummary(string summary, Logger logger, bool afterPostProcessing)
		{
			string[] lines = summary.Split("\n");
			if (!afterPostProcessing)
			{
				foreach (string line in lines)
				{
					if (line.StartsWith("Identification of Mentions"))
					{
						logger.Info(line);
						return;
					}
				}
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				foreach (string line in lines)
				{
					if (line.StartsWith("METRIC"))
					{
						sb.Append(line);
					}
					if (!line.StartsWith("Identification of Mentions") && line.Contains("Recall"))
					{
						sb.Append(line).Append("\n");
					}
				}
				logger.Info(sb.ToString());
			}
		}

		/// <summary>Print average F1 of MUC, B^3, CEAF_E</summary>
		private static void PrintFinalConllScore(string summary)
		{
			Pattern f1 = Pattern.Compile("Coreference:.*F1: (.*)%");
			Matcher f1Matcher = f1.Matcher(summary);
			double[] F1s = new double[5];
			int i = 0;
			while (f1Matcher.Find())
			{
				F1s[i++] = double.ParseDouble(f1Matcher.Group(1));
			}
			double finalScore = (F1s[0] + F1s[1] + F1s[3]) / 3;
			logger.Info("Final conll score ((muc+bcub+ceafe)/3) = " + (new DecimalFormat("#.##")).Format(finalScore));
		}

		private static double GetFinalConllScore(string summary, string metricType, string scoreType)
		{
			// metricType can be muc, bcub, ceafm, ceafe or combined
			// Expects to match metricType muc, bcub, ceafm, ceafe
			// Will not match the BLANC metrics (coref links, noncoref links, overall)
			Pattern pattern = Pattern.Compile("METRIC\\s+(.*):Coreference:.*" + scoreType + ":\\s*(\\([ 0-9./]*\\))?\\s*(\\d+(\\.\\d+)?)%");
			Matcher matcher = pattern.Matcher(summary);
			double[] scores = new double[5];
			string[] names = new string[5];
			int i = 0;
			while (matcher.Find())
			{
				names[i] = matcher.Group(1);
				scores[i] = double.ParseDouble(matcher.Group(3));
				i++;
			}
			metricType = metricType.ToLower();
			if ("combined".Equals(metricType))
			{
				double finalScore = (scores[0] + scores[1] + scores[3]) / 3;
				logger.Info("Final conll score ((muc+bcub+ceafe)/3) " + scoreType + " = " + finalScore);
				return finalScore;
			}
			else
			{
				if ("bcubed".Equals(metricType))
				{
					metricType = "bcub";
				}
				for (i = 0; i < names.Length; i++)
				{
					if (names[i] != null && names[i].Equals(metricType))
					{
						double finalScore = scores[i];
						logger.Info("Final conll score (" + metricType + ") " + scoreType + " = " + finalScore);
						return finalScore;
					}
				}
				throw new ArgumentException("Invalid metricType:" + metricType);
			}
		}

		/// <summary>Returns final selected score</summary>
		private double GetFinalScore(string metricType, CorefScorer.SubScoreType subScoreType)
		{
			metricType = metricType.ToLower();
			int passIndex = sieveClassNames.Length - 1;
			string scoreDesc = metricType;
			double finalScore;
			switch (metricType)
			{
				case "combined":
				{
					finalScore = (scoreMUC[passIndex].GetScore(subScoreType) + scoreBcubed[passIndex].GetScore(subScoreType) + scorePairwise[passIndex].GetScore(subScoreType)) / 3;
					scoreDesc = "(muc + bcub + pairwise)/3";
					break;
				}

				case "muc":
				{
					finalScore = scoreMUC[passIndex].GetScore(subScoreType);
					break;
				}

				case "bcub":
				case "bcubed":
				{
					finalScore = scoreBcubed[passIndex].GetScore(subScoreType);
					break;
				}

				case "pairwise":
				{
					finalScore = scorePairwise[passIndex].GetScore(subScoreType);
					break;
				}

				default:
				{
					throw new ArgumentException("Invalid sub score type:" + subScoreType);
				}
			}
			logger.Info("Final score (" + scoreDesc + ") " + subScoreType + " = " + (new DecimalFormat("#.##")).Format(finalScore));
			return finalScore;
		}

		public static void PrintConllOutput(Document document, StreamWriter writer, bool gold)
		{
			PrintConllOutput(document, writer, gold, false);
		}

		public static void PrintConllOutput(Document document, StreamWriter writer, bool gold, bool filterSingletons)
		{
			IList<IList<Mention>> orderedMentions;
			if (gold)
			{
				orderedMentions = document.goldOrderedMentionsBySentence;
			}
			else
			{
				orderedMentions = document.predictedOrderedMentionsBySentence;
			}
			if (filterSingletons)
			{
				orderedMentions = FilterMentionsWithSingletonClusters(document, orderedMentions);
			}
			PrintConllOutput(document, writer, orderedMentions, gold);
		}

		private static void PrintConllOutput(Document document, StreamWriter writer, IList<IList<Mention>> orderedMentions, bool gold)
		{
			Annotation anno = document.annotation;
			IList<IList<string[]>> conllDocSentences = document.conllDoc.sentenceWordLists;
			string docID = anno.Get(typeof(CoreAnnotations.DocIDAnnotation));
			StringBuilder sb = new StringBuilder();
			sb.Append("#begin document ").Append(docID).Append("\n");
			IList<ICoreMap> sentences = anno.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int sentNum = 0; sentNum < sentences.Count; sentNum++)
			{
				IList<CoreLabel> sentence = sentences[sentNum].Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<string[]> conllSentence = conllDocSentences[sentNum];
				IDictionary<int, ICollection<Mention>> mentionBeginOnly = Generics.NewHashMap();
				IDictionary<int, ICollection<Mention>> mentionEndOnly = Generics.NewHashMap();
				IDictionary<int, ICollection<Mention>> mentionBeginEnd = Generics.NewHashMap();
				for (int i = 0; i < sentence.Count; i++)
				{
					mentionBeginOnly[i] = new LinkedHashSet<Mention>();
					mentionEndOnly[i] = new LinkedHashSet<Mention>();
					mentionBeginEnd[i] = new LinkedHashSet<Mention>();
				}
				foreach (Mention m in orderedMentions[sentNum])
				{
					if (m.startIndex == m.endIndex - 1)
					{
						mentionBeginEnd[m.startIndex].Add(m);
					}
					else
					{
						mentionBeginOnly[m.startIndex].Add(m);
						mentionEndOnly[m.endIndex - 1].Add(m);
					}
				}
				for (int i_1 = 0; i_1 < sentence.Count; i_1++)
				{
					StringBuilder sb2 = new StringBuilder();
					foreach (Mention m_1 in mentionBeginOnly[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
						sb2.Append("(").Append(corefClusterId);
					}
					foreach (Mention m_2 in mentionBeginEnd[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_2.goldCorefClusterID : m_2.corefClusterID;
						sb2.Append("(").Append(corefClusterId).Append(")");
					}
					foreach (Mention m_3 in mentionEndOnly[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_3.goldCorefClusterID : m_3.corefClusterID;
						sb2.Append(corefClusterId).Append(")");
					}
					if (sb2.Length == 0)
					{
						sb2.Append("-");
					}
					string[] columns = conllSentence[i_1];
					for (int j = 0; j < columns.Length - 1; j++)
					{
						string column = columns[j];
						sb.Append(column).Append("\t");
					}
					sb.Append(sb2).Append("\n");
				}
				sb.Append("\n");
			}
			sb.Append("#end document").Append("\n");
			//    sb.append("#end document ").append(docID).append("\n");
			writer.Print(sb.ToString());
			writer.Flush();
		}

		/// <summary>Print raw document for analysis</summary>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		private static void PrintRawDoc(Document document, bool gold)
		{
			IList<ICoreMap> sentences = document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<IList<Mention>> allMentions;
			if (gold)
			{
				allMentions = document.goldOrderedMentionsBySentence;
			}
			else
			{
				allMentions = document.predictedOrderedMentionsBySentence;
			}
			//    String filename = document.annotation.get()
			StringBuilder doc = new StringBuilder();
			int previousOffset = 0;
			for (int i = 0; i < sentences.Count; i++)
			{
				ICoreMap sentence = sentences[i];
				IList<Mention> mentions = allMentions[i];
				IList<CoreLabel> t = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				string[] tokens = new string[t.Count];
				foreach (CoreLabel c in t)
				{
					tokens[c.Index() - 1] = c.Word();
				}
				if (previousOffset + 2 < t[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
				{
					doc.Append("\n");
				}
				previousOffset = t[t.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				ICounter<int> startCounts = new ClassicCounter<int>();
				ICounter<int> endCounts = new ClassicCounter<int>();
				IDictionary<int, ICollection<Mention>> endMentions = Generics.NewHashMap();
				foreach (Mention m in mentions)
				{
					startCounts.IncrementCount(m.startIndex);
					endCounts.IncrementCount(m.endIndex);
					if (!endMentions.Contains(m.endIndex))
					{
						endMentions[m.endIndex] = Generics.NewHashSet<Mention>();
					}
					endMentions[m.endIndex].Add(m);
				}
				for (int j = 0; j < tokens.Length; j++)
				{
					if (endMentions.Contains(j))
					{
						foreach (Mention m_1 in endMentions[j])
						{
							int corefChainId = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
							doc.Append("]_").Append(corefChainId);
						}
					}
					for (int k = 0; k < startCounts.GetCount(j); k++)
					{
						if (doc.Length > 0 && doc[doc.Length - 1] != '[')
						{
							doc.Append(" ");
						}
						doc.Append("[");
					}
					if (doc.Length > 0 && doc[doc.Length - 1] != '[')
					{
						doc.Append(" ");
					}
					doc.Append(tokens[j]);
				}
				if (endMentions.Contains(tokens.Length))
				{
					foreach (Mention m_1 in endMentions[tokens.Length])
					{
						int corefChainId = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
						doc.Append("]_").Append(corefChainId);
					}
				}
				//append("_").append(m.mentionID);
				doc.Append("\n");
			}
			logger.Fine(document.annotation.Get(typeof(CoreAnnotations.DocIDAnnotation)));
			if (gold)
			{
				logger.Fine("New DOC: (GOLD MENTIONS) ==================================================");
			}
			else
			{
				logger.Fine("New DOC: (Predicted Mentions) ==================================================");
			}
			logger.Fine(doc.ToString());
		}

		public static IList<Pair<IntTuple, IntTuple>> GetLinks(IDictionary<int, CorefChain> result)
		{
			IList<Pair<IntTuple, IntTuple>> links = new List<Pair<IntTuple, IntTuple>>();
			CorefChain.CorefMentionComparator comparator = new CorefChain.CorefMentionComparator();
			foreach (CorefChain c in result.Values)
			{
				IList<CorefChain.CorefMention> s = c.GetMentionsInTextualOrder();
				foreach (CorefChain.CorefMention m1 in s)
				{
					foreach (CorefChain.CorefMention m2 in s)
					{
						if (comparator.Compare(m1, m2) == 1)
						{
							links.Add(new Pair<IntTuple, IntTuple>(m1.position, m2.position));
						}
					}
				}
			}
			return links;
		}

		public static void DebugPrintMentions(TextWriter @out, string tag, IList<IList<Mention>> mentions)
		{
			for (int i = 0; i < mentions.Count; i++)
			{
				@out.WriteLine(tag + " SENTENCE " + i);
				for (int j = 0; j < mentions[i].Count; j++)
				{
					Mention m = mentions[i][j];
					string ms = "(" + m.mentionID + "," + m.originalRef + "," + m.corefClusterID + ",[" + m.startIndex + "," + m.endIndex + "]" + ") ";
					@out.Write(ms);
				}
				@out.WriteLine();
			}
		}

		public static bool CheckClusters(Logger logger, string tag, Document document)
		{
			IList<IList<Mention>> mentions = document.GetOrderedMentions();
			bool clustersOk = true;
			foreach (IList<Mention> mentionCluster in mentions)
			{
				foreach (Mention m in mentionCluster)
				{
					string ms = "(" + m.mentionID + "," + m.originalRef + "," + m.corefClusterID + ",[" + m.startIndex + "," + m.endIndex + "]" + ") ";
					CorefCluster cluster = document.corefClusters[m.corefClusterID];
					if (cluster == null)
					{
						logger.Warning(tag + ": Cluster not found for mention: " + ms);
						clustersOk = false;
					}
					else
					{
						if (!cluster.GetCorefMentions().Contains(m))
						{
							logger.Warning(tag + ": Cluster does not contain mention: " + ms);
							clustersOk = false;
						}
					}
				}
			}
			return clustersOk;
		}
	}
}
