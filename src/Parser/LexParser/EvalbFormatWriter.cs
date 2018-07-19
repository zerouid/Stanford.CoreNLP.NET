using System;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class EvalbFormatWriter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(EvalbFormatWriter));

		public const string DefaultGoldFilename = "parses.gld";

		public const string DefaultTestFilename = "parses.tst";

		private PrintWriter goldWriter;

		private PrintWriter testWriter;

		private int count = 0;

		private static readonly EvalbFormatWriter DefaultWriter = new EvalbFormatWriter();

		public virtual void InitFiles(ITreebankLangParserParams tlpParams, string goldFilename, string testFilename)
		{
			try
			{
				goldWriter = tlpParams.Pw(new FileOutputStream(goldFilename));
				testWriter = tlpParams.Pw(new FileOutputStream(testFilename));
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			count = 0;
		}

		public virtual void InitFiles(ITreebankLangParserParams tlpParams, string testFilename)
		{
			try
			{
				goldWriter = null;
				testWriter = tlpParams.Pw(new FileOutputStream(testFilename));
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			count = 0;
		}

		public virtual void WriteTree(Tree test)
		{
			testWriter.Println((test == null) ? "(())" : test.ToString());
			count++;
		}

		//    log.info("Wrote EVALB lines.");
		public virtual void WriteTrees(Tree gold, Tree test)
		{
			goldWriter.Println((gold == null) ? "(())" : gold.ToString());
			testWriter.Println((test == null) ? "(())" : test.ToString());
			count++;
		}

		//    log.info("Wrote EVALB lines.");
		public virtual void CloseFiles()
		{
			if (goldWriter != null)
			{
				goldWriter.Close();
			}
			if (testWriter != null)
			{
				testWriter.Close();
			}
			log.Info("Wrote " + count + " EVALB lines.");
		}

		public static void InitEVALBfiles(ITreebankLangParserParams tlpParams)
		{
			DefaultWriter.InitFiles(tlpParams, DefaultGoldFilename, DefaultTestFilename);
		}

		public static void CloseEVALBfiles()
		{
			DefaultWriter.CloseFiles();
		}

		public static void WriteEVALBline(Tree gold, Tree test)
		{
			DefaultWriter.WriteTrees(gold, test);
		}
	}
}
