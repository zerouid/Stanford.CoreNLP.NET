using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid.Sieve;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid
{
	public class HybridCorefSystem : ICorefAlgorithm
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem));

		public Properties props;

		public IList<Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve> sieves;

		public Edu.Stanford.Nlp.Coref.Data.Dictionaries dictionaries;

		public DocumentMaker docMaker = null;

		/// <exception cref="System.Exception"/>
		public HybridCorefSystem(Properties props, Edu.Stanford.Nlp.Coref.Data.Dictionaries dictionaries)
		{
			this.props = props;
			this.dictionaries = dictionaries;
			sieves = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.LoadSieves(props);
			// set semantics loading
			foreach (Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve sieve in sieves)
			{
				if (sieve.classifierType == Sieve.ClassifierType.Rule)
				{
					continue;
				}
				if (HybridCorefProperties.UseWordEmbedding(props, sieve.sievename))
				{
					props.SetProperty(HybridCorefProperties.LoadWordEmbeddingProp, "true");
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public HybridCorefSystem(Properties props)
		{
			this.props = props;
			sieves = Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.LoadSieves(props);
			// set semantics loading
			foreach (Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve sieve in sieves)
			{
				if (sieve.classifierType == Sieve.ClassifierType.Rule)
				{
					continue;
				}
				if (HybridCorefProperties.UseWordEmbedding(props, sieve.sievename))
				{
					props.SetProperty(HybridCorefProperties.LoadWordEmbeddingProp, "true");
				}
			}
			dictionaries = new Edu.Stanford.Nlp.Coref.Data.Dictionaries(props);
			docMaker = new DocumentMaker(props, dictionaries);
		}

		public virtual Edu.Stanford.Nlp.Coref.Data.Dictionaries Dictionaries()
		{
			return dictionaries;
		}

		/// <exception cref="System.Exception"/>
		public static void RunCoref(string[] args)
		{
			RunCoref(StringUtils.ArgsToProperties(args));
		}

		/// <exception cref="System.Exception"/>
		public static void RunCoref(Properties props)
		{
			/*
			* property, environment setting
			*/
			Redwood.HideChannelsEverywhere("debug-cluster", "debug-mention", "debug-preprocessor", "debug-docreader", "debug-mergethres", "debug-featureselection", "debug-md");
			int nThreads = HybridCorefProperties.GetThreadCounts(props);
			string timeStamp = Calendar.GetInstance().GetTime().ToString().ReplaceAll("\\s", "-").ReplaceAll(":", "-");
			Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem).FullName);
			// set log file path
			if (props.Contains(HybridCorefProperties.LogProp))
			{
				File logFile = new File(props.GetProperty(HybridCorefProperties.LogProp));
				RedwoodConfiguration.Current().Handlers(RedwoodConfiguration.Handlers.File(logFile)).Apply();
				Redwood.Log("Starting coref log");
			}
			log.Info(props.ToString());
			if (HybridCorefProperties.CheckMemory(props))
			{
				CheckMemoryUsage();
			}
			Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem cs = new Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem(props);
			/*
			output setting
			*/
			// prepare conll output
			string goldOutput = null;
			string beforeCorefOutput = null;
			string afterCorefOutput = null;
			PrintWriter writerGold = null;
			PrintWriter writerBeforeCoref = null;
			PrintWriter writerAfterCoref = null;
			if (HybridCorefProperties.DoScore(props))
			{
				string pathOutput = CorefProperties.ConllOutputPath(props);
				(new File(pathOutput)).Mkdir();
				goldOutput = pathOutput + "output-" + timeStamp + ".gold.txt";
				beforeCorefOutput = pathOutput + "output-" + timeStamp + ".predicted.txt";
				afterCorefOutput = pathOutput + "output-" + timeStamp + ".coref.predicted.txt";
				writerGold = new PrintWriter(new FileOutputStream(goldOutput));
				writerBeforeCoref = new PrintWriter(new FileOutputStream(beforeCorefOutput));
				writerAfterCoref = new PrintWriter(new FileOutputStream(afterCorefOutput));
			}
			// run coref
			MulticoreWrapper<Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem>, StringBuilder[]> wrapper = new MulticoreWrapper<Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem>, StringBuilder[]>(nThreads, new _IThreadsafeProcessor_134
				());
			// conll output and logs
			DateTime startTime = null;
			if (HybridCorefProperties.CheckTime(props))
			{
				startTime = new DateTime();
				System.Console.Error.Printf("END-TO-END COREF Start time: %s\n", startTime);
			}
			// run processes
			int docCnt = 0;
			while (true)
			{
				Document document = cs.docMaker.NextDoc();
				if (document == null)
				{
					break;
				}
				wrapper.Put(Pair.MakePair(document, cs));
				docCnt = LogOutput(wrapper, writerGold, writerBeforeCoref, writerAfterCoref, docCnt);
			}
			// Finished reading the input. Wait for jobs to finish
			wrapper.Join();
			docCnt = LogOutput(wrapper, writerGold, writerBeforeCoref, writerAfterCoref, docCnt);
			IOUtils.CloseIgnoringExceptions(writerGold);
			IOUtils.CloseIgnoringExceptions(writerBeforeCoref);
			IOUtils.CloseIgnoringExceptions(writerAfterCoref);
			if (HybridCorefProperties.CheckTime(props))
			{
				System.Console.Error.Printf("END-TO-END COREF Elapsed time: %.3f seconds\n", (((new DateTime()).GetTime() - startTime.GetTime()) / 1000F));
			}
			//      System.err.printf("CORENLP PROCESS TIME TOTAL: %.3f seconds\n", cs.mentionExtractor.corenlpProcessTime);
			if (HybridCorefProperties.CheckMemory(props))
			{
				CheckMemoryUsage();
			}
			// scoring
			if (HybridCorefProperties.DoScore(props))
			{
				string summary = CorefScorer.GetEvalSummary(CorefProperties.GetScorerPath(props), goldOutput, beforeCorefOutput);
				CorefScorer.PrintScoreSummary(summary, logger, false);
				summary = CorefScorer.GetEvalSummary(CorefProperties.GetScorerPath(props), goldOutput, afterCorefOutput);
				CorefScorer.PrintScoreSummary(summary, logger, true);
				CorefScorer.PrintFinalConllScore(summary);
			}
		}

		private sealed class _IThreadsafeProcessor_134 : IThreadsafeProcessor<Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem>, StringBuilder[]>
		{
			public _IThreadsafeProcessor_134()
			{
			}

			public StringBuilder[] Process(Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem> input)
			{
				try
				{
					Document document = input.first;
					Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem cs = input.second;
					StringBuilder[] outputs = new StringBuilder[4];
					cs.Coref(document, outputs);
					return outputs;
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}

			public IThreadsafeProcessor<Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem>, StringBuilder[]> NewInstance()
			{
				return this;
			}
		}

		/// <summary>Write output of coref system in conll format, and log.</summary>
		private static int LogOutput(MulticoreWrapper<Pair<Document, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefSystem>, StringBuilder[]> wrapper, PrintWriter writerGold, PrintWriter writerBeforeCoref, PrintWriter writerAfterCoref, int docCnt)
		{
			while (wrapper.Peek())
			{
				StringBuilder[] output = wrapper.Poll();
				writerGold.Print(output[0]);
				writerBeforeCoref.Print(output[1]);
				writerAfterCoref.Print(output[2]);
				if (output[3].Length > 0)
				{
					log.Info(output[3]);
				}
				if ((++docCnt) % 10 == 0)
				{
					log.Info(docCnt + " document(s) processed");
				}
			}
			return docCnt;
		}

		public virtual void RunCoref(Document document)
		{
			try
			{
				Coref(document);
			}
			catch (Exception e)
			{
				throw new Exception("Error running hybrid coref system", e);
			}
		}

		/// <summary>main entry of coreference system.</summary>
		/// <param name="document">Input document for coref format (Annotation and optional information)</param>
		/// <param name="output">For output of coref system (conll format and log. list size should be 4.)</param>
		/// <returns>Map of coref chain ID and corresponding chain</returns>
		/// <exception cref="System.Exception"/>
		public virtual IDictionary<int, CorefChain> Coref(Document document, StringBuilder[] output)
		{
			if (HybridCorefProperties.PrintMDLog(props))
			{
				Redwood.Log(HybridCorefPrinter.PrintMentionDetectionLog(document));
			}
			if (HybridCorefProperties.DoScore(props))
			{
				output[0] = (new StringBuilder()).Append(CorefPrinter.PrintConllOutput(document, true));
				// gold
				output[1] = (new StringBuilder()).Append(CorefPrinter.PrintConllOutput(document, false));
			}
			// before coref
			output[3] = new StringBuilder();
			// log from sieves
			foreach (Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve sieve in sieves)
			{
				CorefUtils.CheckForInterrupt();
				output[3].Append(sieve.ResolveMention(document, dictionaries, props));
			}
			// post processing
			if (HybridCorefProperties.DoPostProcessing(props))
			{
				PostProcessing(document);
			}
			if (HybridCorefProperties.DoScore(props))
			{
				output[2] = (new StringBuilder()).Append(CorefPrinter.PrintConllOutput(document, false, true));
			}
			// after coref
			return MakeCorefOutput(document);
		}

		/// <summary>main entry of coreference system.</summary>
		/// <param name="document">Input document for coref format (Annotation and optional information)</param>
		/// <returns>Map of coref chain ID and corresponding chain</returns>
		/// <exception cref="System.Exception"/>
		public virtual IDictionary<int, CorefChain> Coref(Document document)
		{
			return Coref(document, new StringBuilder[4]);
		}

		/// <summary>main entry of coreference system.</summary>
		/// <param name="anno">Input annotation.</param>
		/// <returns>Map of coref chain ID and corresponding chain</returns>
		/// <exception cref="System.Exception"/>
		public virtual IDictionary<int, CorefChain> Coref(Annotation anno)
		{
			return Coref(docMaker.MakeDocument(anno));
		}

		/// <summary>Extract final coreference output from coreference document format.</summary>
		private static IDictionary<int, CorefChain> MakeCorefOutput(Document document)
		{
			IDictionary<int, CorefChain> result = Generics.NewHashMap();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				result[c.clusterID] = new CorefChain(c, document.positions);
			}
			return result;
		}

		/// <summary>Remove singletons, appositive, predicate nominatives, relative pronouns.</summary>
		private static void PostProcessing(Document document)
		{
			ICollection<Mention> removeSet = Generics.NewHashSet();
			ICollection<int> removeClusterSet = Generics.NewHashSet();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				ICollection<Mention> removeMentions = Generics.NewHashSet();
				foreach (Mention m in c.GetCorefMentions())
				{
					if (HybridCorefProperties.RemoveAppositionPredicatenominatives && ((m.appositions != null && m.appositions.Count > 0) || (m.predicateNominatives != null && m.predicateNominatives.Count > 0) || (m.relativePronouns != null && m.relativePronouns
						.Count > 0)))
					{
						removeMentions.Add(m);
						removeSet.Add(m);
						m.corefClusterID = m.mentionID;
					}
				}
				c.corefMentions.RemoveAll(removeMentions);
				if (HybridCorefProperties.RemoveSingletons && c.GetCorefMentions().Count == 1)
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

		private static void CheckMemoryUsage()
		{
			Runtime runtime = Runtime.GetRuntime();
			runtime.Gc();
			long memory = runtime.TotalMemory() - runtime.FreeMemory();
			log.Info("USED MEMORY (bytes): " + memory);
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			DateTime startTime = new DateTime();
			System.Console.Error.Printf("Start time: %s\n", startTime);
			RunCoref(args);
			System.Console.Error.Printf("Elapsed time: %.3f seconds\n", (((new DateTime()).GetTime() - startTime.GetTime()) / 1000F));
		}
	}
}
