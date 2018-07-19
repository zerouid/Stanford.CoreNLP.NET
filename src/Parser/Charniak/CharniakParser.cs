using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Charniak
{
	/// <summary>Runs the Charniak parser via the command line.</summary>
	/// <author>Angel Chang</author>
	public class CharniakParser
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Charniak.CharniakParser));

		private const string CharniakDir = "/u/nlp/packages/bllip-parser/";

		private const string CharniakBin = "./parse-50best.sh";

		private string dir = CharniakDir;

		private string parserExecutable = CharniakBin;

		/// <summary>Do not parse sentences larger than this sentence length</summary>
		private int maxSentenceLength = 400;

		private int beamSize = 0;

		public CharniakParser()
		{
		}

		public CharniakParser(string dir, string parserExecutable)
		{
			// note: this is actually the parser+reranker (will use 2 CPUs)
			this.parserExecutable = parserExecutable;
			this.dir = dir;
		}

		public virtual int GetBeamSize()
		{
			return beamSize;
		}

		public virtual void SetBeamSize(int beamSize)
		{
			this.beamSize = beamSize;
		}

		public virtual int GetMaxSentenceLength()
		{
			return maxSentenceLength;
		}

		public virtual void SetMaxSentenceLength(int maxSentenceLength)
		{
			this.maxSentenceLength = maxSentenceLength;
		}

		public virtual Tree GetBestParse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			ScoredObject<Tree> scoredParse = GetBestScoredParse(sentence);
			return (scoredParse != null) ? scoredParse.Object() : null;
		}

		public virtual ScoredObject<Tree> GetBestScoredParse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			IList<ScoredObject<Tree>> kBestParses = GetKBestParses(sentence, 1);
			if (kBestParses != null)
			{
				return kBestParses[0];
			}
			return null;
		}

		public virtual IList<ScoredObject<Tree>> GetKBestParses<_T0>(IList<_T0> sentence, int k)
			where _T0 : IHasWord
		{
			return GetKBestParses(sentence, k, true);
		}

		public virtual IList<ScoredObject<Tree>> GetKBestParses<_T0>(IList<_T0> sentence, int k, bool deleteTempFiles)
			where _T0 : IHasWord
		{
			try
			{
				File inFile = File.CreateTempFile("charniak.", ".in");
				if (deleteTempFiles)
				{
					inFile.DeleteOnExit();
				}
				File outFile = File.CreateTempFile("charniak.", ".out");
				if (deleteTempFiles)
				{
					outFile.DeleteOnExit();
				}
				File errFile = File.CreateTempFile("charniak.", ".err");
				if (deleteTempFiles)
				{
					errFile.DeleteOnExit();
				}
				PrintSentence(sentence, inFile.GetAbsolutePath());
				RunCharniak(k, inFile.GetAbsolutePath(), outFile.GetAbsolutePath(), errFile.GetAbsolutePath());
				IEnumerable<IList<ScoredObject<Tree>>> iter = CharniakScoredParsesReaderWriter.ReadScoredTrees(outFile.GetAbsolutePath());
				if (deleteTempFiles)
				{
					inFile.Delete();
					outFile.Delete();
					errFile.Delete();
				}
				return iter.GetEnumerator().Current;
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		public virtual IEnumerable<IList<ScoredObject<Tree>>> GetKBestParses(IEnumerable<IList<IHasWord>> sentences, int k)
		{
			return GetKBestParses(sentences, k, true);
		}

		public virtual IEnumerable<IList<ScoredObject<Tree>>> GetKBestParses(IEnumerable<IList<IHasWord>> sentences, int k, bool deleteTempFiles)
		{
			try
			{
				File inFile = File.CreateTempFile("charniak.", ".in");
				if (deleteTempFiles)
				{
					inFile.DeleteOnExit();
				}
				File outFile = File.CreateTempFile("charniak.", ".out");
				if (deleteTempFiles)
				{
					outFile.DeleteOnExit();
				}
				File errFile = File.CreateTempFile("charniak.", ".err");
				if (deleteTempFiles)
				{
					errFile.DeleteOnExit();
				}
				PrintSentences(sentences, inFile.GetAbsolutePath());
				RunCharniak(k, inFile.GetAbsolutePath(), outFile.GetAbsolutePath(), errFile.GetAbsolutePath());
				IEnumerable<IList<ScoredObject<Tree>>> iter = CharniakScoredParsesReaderWriter.ReadScoredTrees(outFile.GetAbsolutePath());
				if (deleteTempFiles)
				{
					inFile.Delete();
					outFile.Delete();
					errFile.Delete();
				}
				return new IterableIterator<IList<ScoredObject<Tree>>>(iter.GetEnumerator());
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		public virtual void PrintSentence<_T0>(IList<_T0> sentence, string filename)
			where _T0 : IHasWord
		{
			IList<IList<IHasWord>> sentences = new List<IList<IHasWord>>();
			sentences.Add(sentence);
			PrintSentences(sentences, filename);
		}

		public virtual void PrintSentences(IEnumerable<IList<IHasWord>> sentences, string filename)
		{
			try
			{
				PrintWriter pw = IOUtils.GetPrintWriter(filename);
				foreach (IList<IHasWord> sentence in sentences)
				{
					pw.Print("<s> ");
					// Note: Use <s sentence-id > to identify sentences
					string sentString = SentenceUtils.ListToString(sentence);
					if (sentence.Count > maxSentenceLength)
					{
						logger.Warning("Sentence length=" + sentence.Count + " is longer than maximum set length " + maxSentenceLength);
						logger.Warning("Long Sentence: " + sentString);
					}
					pw.Print(sentString);
					pw.Println(" </s>");
				}
				pw.Close();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		public virtual void RunCharniak(int n, string infile, string outfile, string errfile)
		{
			try
			{
				if (n == 1)
				{
					n++;
				}
				// Charniak does not output score if n = 1?
				IList<string> args = new List<string>();
				args.Add(parserExecutable);
				args.Add(infile);
				ProcessBuilder process = new ProcessBuilder(args);
				process.Directory(new File(this.dir));
				PrintWriter @out = IOUtils.GetPrintWriter(outfile);
				PrintWriter err = IOUtils.GetPrintWriter(errfile);
				SystemUtils.Run(process, @out, err);
				@out.Close();
				err.Close();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}
	}
}
