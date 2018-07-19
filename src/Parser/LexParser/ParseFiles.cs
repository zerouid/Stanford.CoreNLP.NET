using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Java.IO;
using Java.Net;
using Java.Text;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Runs the parser over a set of files.</summary>
	/// <remarks>
	/// Runs the parser over a set of files.  This is useful for making it
	/// operate in a multithreaded manner.  If you want access to the
	/// various stats it keeps, create the object and call parseFiles;
	/// otherwise, the static parseFiles is a good convenience method.
	/// </remarks>
	/// <author>John Bauer (refactored from existing code)</author>
	public class ParseFiles
	{
		private readonly ITreebankLanguagePack tlp;

		private readonly PrintWriter pwOut;

		private readonly PrintWriter pwErr;

		private int numWords = 0;

		private int numSents = 0;

		private int numUnparsable = 0;

		private int numNoMemory = 0;

		private int numFallback = 0;

		private int numSkipped = 0;

		private bool saidMemMessage = false;

		private readonly bool runningAverages;

		private readonly bool summary;

		private readonly AbstractEval.ScoreEval pcfgLL;

		private readonly AbstractEval.ScoreEval depLL;

		private readonly AbstractEval.ScoreEval factLL;

		private readonly Options op;

		private readonly LexicalizedParser pqFactory;

		private readonly TreePrint treePrint;

		// todo: perhaps the output streams could be passed in
		/// <summary>
		/// Parse the files with names given in the String array args elements from
		/// index argIndex on.
		/// </summary>
		/// <remarks>
		/// Parse the files with names given in the String array args elements from
		/// index argIndex on.  Convenience method which builds and invokes a ParseFiles object.
		/// </remarks>
		public static void ParseFiles<_T0>(string[] args, int argIndex, bool tokenized, ITokenizerFactory<_T0> tokenizerFactory, string elementDelimiter, string sentenceDelimiter, IFunction<IList<IHasWord>, IList<IHasWord>> escaper, string tagDelimiter
			, Options op, TreePrint treePrint, LexicalizedParser pqFactory)
			where _T0 : IHasWord
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ParseFiles pf = new Edu.Stanford.Nlp.Parser.Lexparser.ParseFiles(op, treePrint, pqFactory);
			pf.ParseFiles(args, argIndex, tokenized, tokenizerFactory, elementDelimiter, sentenceDelimiter, escaper, tagDelimiter);
		}

		public ParseFiles(Options op, TreePrint treePrint, LexicalizedParser pqFactory)
		{
			this.op = op;
			this.pqFactory = pqFactory;
			this.treePrint = treePrint;
			this.tlp = op.tlpParams.TreebankLanguagePack();
			this.pwOut = op.tlpParams.Pw();
			this.pwErr = op.tlpParams.Pw(System.Console.Error);
			if (op.testOptions.verbose)
			{
				pwErr.Println("Sentence final words are: " + Arrays.AsList(tlp.SentenceFinalPunctuationWords()));
				pwErr.Println("File encoding is: " + op.tlpParams.GetInputEncoding());
			}
			// evaluation setup
			this.runningAverages = bool.ParseBoolean(op.testOptions.evals.GetProperty("runningAverages"));
			this.summary = bool.ParseBoolean(op.testOptions.evals.GetProperty("summary"));
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgLL")))
			{
				this.pcfgLL = new AbstractEval.ScoreEval("pcfgLL", runningAverages);
			}
			else
			{
				this.pcfgLL = null;
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("depLL")))
			{
				this.depLL = new AbstractEval.ScoreEval("depLL", runningAverages);
			}
			else
			{
				this.depLL = null;
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factLL")))
			{
				this.factLL = new AbstractEval.ScoreEval("factLL", runningAverages);
			}
			else
			{
				this.factLL = null;
			}
		}

		public virtual void ParseFiles<_T0>(string[] args, int argIndex, bool tokenized, ITokenizerFactory<_T0> tokenizerFactory, string elementDelimiter, string sentenceDelimiter, IFunction<IList<IHasWord>, IList<IHasWord>> escaper, string tagDelimiter
			)
			where _T0 : IHasWord
		{
			DocumentPreprocessor.DocType docType = (elementDelimiter == null) ? DocumentPreprocessor.DocType.Plain : DocumentPreprocessor.DocType.Xml;
			if (op.testOptions.verbose)
			{
				if (tokenizerFactory != null)
				{
					pwErr.Println("parseFiles: Tokenizer factory is: " + tokenizerFactory);
				}
			}
			Timing timer = new Timing();
			// timer.start(); // constructor already starts it.
			//Loop over the files
			for (int i = argIndex; i < args.Length; i++)
			{
				string filename = args[i];
				DocumentPreprocessor documentPreprocessor;
				if (filename.Equals("-"))
				{
					try
					{
						documentPreprocessor = new DocumentPreprocessor(IOUtils.ReaderFromStdin(op.tlpParams.GetInputEncoding()), docType);
					}
					catch (IOException e)
					{
						throw new RuntimeIOException(e);
					}
				}
				else
				{
					documentPreprocessor = new DocumentPreprocessor(filename, docType, op.tlpParams.GetInputEncoding());
				}
				//Unused values are null per the main() method invocation below
				//null is the default for these properties
				documentPreprocessor.SetSentenceFinalPuncWords(tlp.SentenceFinalPunctuationWords());
				documentPreprocessor.SetEscaper(escaper);
				documentPreprocessor.SetSentenceDelimiter(sentenceDelimiter);
				documentPreprocessor.SetTagDelimiter(tagDelimiter);
				documentPreprocessor.SetElementDelimiter(elementDelimiter);
				if (tokenizerFactory == null)
				{
					documentPreprocessor.SetTokenizerFactory((tokenized) ? null : tlp.GetTokenizerFactory());
				}
				else
				{
					documentPreprocessor.SetTokenizerFactory(tokenizerFactory);
				}
				//Setup the output
				PrintWriter pwo = pwOut;
				if (op.testOptions.writeOutputFiles)
				{
					string normalizedName = filename;
					try
					{
						new URL(normalizedName);
						// this will exception if not a URL
						normalizedName = normalizedName.ReplaceAll("/", "_");
					}
					catch (MalformedURLException)
					{
					}
					//It isn't a URL, so silently ignore
					string ext = (op.testOptions.outputFilesExtension == null) ? "stp" : op.testOptions.outputFilesExtension;
					string fname = normalizedName + '.' + ext;
					if (op.testOptions.outputFilesDirectory != null && !op.testOptions.outputFilesDirectory.IsEmpty())
					{
						string fseparator = Runtime.GetProperty("file.separator");
						if (fseparator == null || fseparator.IsEmpty())
						{
							fseparator = "/";
						}
						File fnameFile = new File(fname);
						fname = op.testOptions.outputFilesDirectory + fseparator + fnameFile.GetName();
					}
					try
					{
						pwo = op.tlpParams.Pw(new FileOutputStream(fname));
					}
					catch (IOException ioe)
					{
						throw new RuntimeIOException(ioe);
					}
				}
				treePrint.PrintHeader(pwo, op.tlpParams.GetOutputEncoding());
				pwErr.Println("Parsing file: " + filename);
				int num = 0;
				int numProcessed = 0;
				if (op.testOptions.testingThreads != 1)
				{
					MulticoreWrapper<IList<IHasWord>, IParserQuery> wrapper = new MulticoreWrapper<IList<IHasWord>, IParserQuery>(op.testOptions.testingThreads, new ParsingThreadsafeProcessor(pqFactory, pwErr));
					foreach (IList<IHasWord> sentence in documentPreprocessor)
					{
						num++;
						numSents++;
						int len = sentence.Count;
						numWords += len;
						pwErr.Println("Parsing [sent. " + num + " len. " + len + "]: " + SentenceUtils.ListToString(sentence, true));
						wrapper.Put(sentence);
						while (wrapper.Peek())
						{
							IParserQuery pq = wrapper.Poll();
							ProcessResults(pq, numProcessed++, pwo);
						}
					}
					wrapper.Join();
					while (wrapper.Peek())
					{
						IParserQuery pq = wrapper.Poll();
						ProcessResults(pq, numProcessed++, pwo);
					}
				}
				else
				{
					IParserQuery pq = pqFactory.ParserQuery();
					foreach (IList<IHasWord> sentence in documentPreprocessor)
					{
						num++;
						numSents++;
						int len = sentence.Count;
						numWords += len;
						pwErr.Println("Parsing [sent. " + num + " len. " + len + "]: " + SentenceUtils.ListToString(sentence, true));
						pq.ParseAndReport(sentence, pwErr);
						ProcessResults(pq, numProcessed++, pwo);
					}
				}
				treePrint.PrintFooter(pwo);
				if (op.testOptions.writeOutputFiles)
				{
					pwo.Close();
				}
				pwErr.Println("Parsed file: " + filename + " [" + num + " sentences].");
			}
			long millis = timer.Stop();
			if (summary)
			{
				if (pcfgLL != null)
				{
					pcfgLL.Display(false, pwErr);
				}
				if (depLL != null)
				{
					depLL.Display(false, pwErr);
				}
				if (factLL != null)
				{
					factLL.Display(false, pwErr);
				}
			}
			if (saidMemMessage)
			{
				ParserUtils.PrintOutOfMemory(pwErr);
			}
			double wordspersec = numWords / (((double)millis) / 1000);
			double sentspersec = numSents / (((double)millis) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			// easier way!
			pwErr.Println("Parsed " + numWords + " words in " + numSents + " sentences (" + nf.Format(wordspersec) + " wds/sec; " + nf.Format(sentspersec) + " sents/sec).");
			if (numFallback > 0)
			{
				pwErr.Println("  " + numFallback + " sentences were parsed by fallback to PCFG.");
			}
			if (numUnparsable > 0 || numNoMemory > 0 || numSkipped > 0)
			{
				pwErr.Println("  " + (numUnparsable + numNoMemory + numSkipped) + " sentences were not parsed:");
				if (numUnparsable > 0)
				{
					pwErr.Println("    " + numUnparsable + " were not parsable with non-zero probability.");
				}
				if (numNoMemory > 0)
				{
					pwErr.Println("    " + numNoMemory + " were skipped because of insufficient memory.");
				}
				if (numSkipped > 0)
				{
					pwErr.Println("    " + numSkipped + " were skipped as length 0 or greater than " + op.testOptions.maxLength);
				}
			}
		}

		// end parseFiles
		public virtual void ProcessResults(IParserQuery parserQuery, int num, PrintWriter pwo)
		{
			if (parserQuery.ParseSkipped())
			{
				IList<IHasWord> sentence = parserQuery.OriginalSentence();
				if (sentence != null)
				{
					numWords -= sentence.Count;
				}
				numSkipped++;
			}
			if (parserQuery.ParseNoMemory())
			{
				numNoMemory++;
			}
			if (parserQuery.ParseUnparsable())
			{
				numUnparsable++;
			}
			if (parserQuery.ParseFallback())
			{
				numFallback++;
			}
			saidMemMessage = saidMemMessage || parserQuery.SaidMemMessage();
			Tree ansTree = parserQuery.GetBestParse();
			if (ansTree == null)
			{
				pwo.Println("(())");
				return;
			}
			if (pcfgLL != null && parserQuery.GetPCFGParser() != null)
			{
				pcfgLL.RecordScore(parserQuery.GetPCFGParser(), pwErr);
			}
			if (depLL != null && parserQuery.GetDependencyParser() != null)
			{
				depLL.RecordScore(parserQuery.GetDependencyParser(), pwErr);
			}
			if (factLL != null && parserQuery.GetFactoredParser() != null)
			{
				factLL.RecordScore(parserQuery.GetFactoredParser(), pwErr);
			}
			try
			{
				treePrint.PrintTree(ansTree, int.ToString(num), pwo);
			}
			catch (Exception re)
			{
				pwErr.Println("TreePrint.printTree skipped: out of memory (or other error)");
				Sharpen.Runtime.PrintStackTrace(re, pwErr);
				numNoMemory++;
				try
				{
					treePrint.PrintTree(null, int.ToString(num), pwo);
				}
				catch (Exception e)
				{
					pwErr.Println("Sentence skipped: out of memory or error calling TreePrint.");
					pwo.Println("(())");
					Sharpen.Runtime.PrintStackTrace(e, pwErr);
				}
			}
			// crude addition of k-best tree printing
			// TODO: interface with the RerankingParserQuery
			if (op.testOptions.printPCFGkBest > 0 && parserQuery.GetPCFGParser() != null && parserQuery.GetPCFGParser().HasParse())
			{
				IList<ScoredObject<Tree>> trees = parserQuery.GetKBestPCFGParses(op.testOptions.printPCFGkBest);
				treePrint.PrintTrees(trees, int.ToString(num), pwo);
			}
			else
			{
				if (op.testOptions.printFactoredKGood > 0 && parserQuery.GetFactoredParser() != null && parserQuery.GetFactoredParser().HasParse())
				{
					// DZ: debug n best trees
					IList<ScoredObject<Tree>> trees = parserQuery.GetKGoodFactoredParses(op.testOptions.printFactoredKGood);
					treePrint.PrintTrees(trees, int.ToString(num), pwo);
				}
			}
		}
	}
}
