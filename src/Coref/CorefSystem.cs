using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>Class for running coreference algorithms</summary>
	/// <author>Kevin Clark</author>
	public class CorefSystem
	{
		private readonly DocumentMaker docMaker;

		private readonly ICorefAlgorithm corefAlgorithm;

		private readonly bool removeSingletonClusters;

		private readonly bool verbose;

		public CorefSystem(Properties props)
		{
			try
			{
				Dictionaries dictionaries = new Dictionaries(props);
				docMaker = new DocumentMaker(props, dictionaries);
				corefAlgorithm = ICorefAlgorithm.FromProps(props, dictionaries);
				removeSingletonClusters = CorefProperties.RemoveSingletonClusters(props);
				verbose = CorefProperties.Verbose(props);
			}
			catch (Exception e)
			{
				throw new Exception("Error initializing coref system", e);
			}
		}

		public CorefSystem(DocumentMaker docMaker, ICorefAlgorithm corefAlgorithm, bool removeSingletonClusters, bool verbose)
		{
			this.docMaker = docMaker;
			this.corefAlgorithm = corefAlgorithm;
			this.removeSingletonClusters = removeSingletonClusters;
			this.verbose = verbose;
		}

		public virtual void Annotate(Annotation ann)
		{
			Document document;
			try
			{
				document = docMaker.MakeDocument(ann);
			}
			catch (Exception e)
			{
				throw new Exception("Error making document", e);
			}
			CorefUtils.CheckForInterrupt();
			corefAlgorithm.RunCoref(document);
			if (removeSingletonClusters)
			{
				CorefUtils.RemoveSingletonClusters(document);
			}
			CorefUtils.CheckForInterrupt();
			IDictionary<int, CorefChain> result = Generics.NewHashMap();
			foreach (CorefCluster c in document.corefClusters.Values)
			{
				result[c.clusterID] = new CorefChain(c, document.positions);
			}
			ann.Set(typeof(CorefCoreAnnotations.CorefChainAnnotation), result);
		}

		/// <exception cref="System.Exception"/>
		public virtual void RunOnConll(Properties props)
		{
			string baseName = CorefProperties.ConllOutputPath(props) + Calendar.GetInstance().GetTime().ToString().ReplaceAll("\\s", "-").ReplaceAll(":", "-");
			string goldOutput = baseName + ".gold.txt";
			string beforeCorefOutput = baseName + ".predicted.txt";
			string afterCorefOutput = baseName + ".coref.predicted.txt";
			PrintWriter writerGold = new PrintWriter(new FileOutputStream(goldOutput));
			PrintWriter writerBeforeCoref = new PrintWriter(new FileOutputStream(beforeCorefOutput));
			PrintWriter writerAfterCoref = new PrintWriter(new FileOutputStream(afterCorefOutput));
			(new _ICorefDocumentProcessor_82(this, writerGold, writerBeforeCoref, writerAfterCoref)).Run(docMaker);
			Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.Coref.CorefSystem).FullName);
			string summary = CorefScorer.GetEvalSummary(CorefProperties.GetScorerPath(props), goldOutput, beforeCorefOutput);
			CorefScorer.PrintScoreSummary(summary, logger, false);
			summary = CorefScorer.GetEvalSummary(CorefProperties.GetScorerPath(props), goldOutput, afterCorefOutput);
			CorefScorer.PrintScoreSummary(summary, logger, true);
			CorefScorer.PrintFinalConllScore(summary);
			writerGold.Close();
			writerBeforeCoref.Close();
			writerAfterCoref.Close();
		}

		private sealed class _ICorefDocumentProcessor_82 : ICorefDocumentProcessor
		{
			public _ICorefDocumentProcessor_82(CorefSystem _enclosing, PrintWriter writerGold, PrintWriter writerBeforeCoref, PrintWriter writerAfterCoref)
			{
				this._enclosing = _enclosing;
				this.writerGold = writerGold;
				this.writerBeforeCoref = writerBeforeCoref;
				this.writerAfterCoref = writerAfterCoref;
			}

			public void Process(int id, Document document)
			{
				writerGold.Print(CorefPrinter.PrintConllOutput(document, true));
				writerBeforeCoref.Print(CorefPrinter.PrintConllOutput(document, false));
				long time = Runtime.CurrentTimeMillis();
				this._enclosing.corefAlgorithm.RunCoref(document);
				if (this._enclosing.verbose)
				{
					Redwood.Log(this.GetName(), "Coref took " + (Runtime.CurrentTimeMillis() - time) / 1000.0 + "s");
				}
				CorefUtils.RemoveSingletonClusters(document);
				writerAfterCoref.Print(CorefPrinter.PrintConllOutput(document, false, true));
			}

			/// <exception cref="System.Exception"/>
			public void Finish()
			{
			}

			public string GetName()
			{
				return this._enclosing.corefAlgorithm.GetType().FullName;
			}

			private readonly CorefSystem _enclosing;

			private readonly PrintWriter writerGold;

			private readonly PrintWriter writerBeforeCoref;

			private readonly PrintWriter writerAfterCoref;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			Edu.Stanford.Nlp.Coref.CorefSystem coref = new Edu.Stanford.Nlp.Coref.CorefSystem(props);
			coref.RunOnConll(props);
		}
	}
}
