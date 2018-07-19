using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Parsesegment
{
	/// <author>Spence Green</author>
	public sealed class JointParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Parsesegment.JointParser));

		private JointParser()
		{
		}

		private const int MinArgs = 1;

		private static string Usage()
		{
			string cmdLineUsage = string.Format("Usage: java %s [OPTS] trainFile < lattice_file > trees%n", typeof(Edu.Stanford.Nlp.International.Arabic.Parsesegment.JointParser).FullName);
			StringBuilder classUsage = new StringBuilder(cmdLineUsage);
			string nl = Runtime.GetProperty("line.separator");
			classUsage.Append(" -v        : Verbose output").Append(nl);
			classUsage.Append(" -t file   : Test on input trees").Append(nl);
			classUsage.Append(" -l num    : Max (gold) sentence length to evaluate (in interstices)").Append(nl);
			classUsage.Append(" -o        : Input is a serialized list of lattices").Append(nl);
			return classUsage.ToString();
		}

		private static IDictionary<string, int> OptionArgDefs()
		{
			IDictionary<string, int> optionArgDefs = Generics.NewHashMap();
			optionArgDefs["v"] = 0;
			optionArgDefs["t"] = 1;
			optionArgDefs["l"] = 1;
			optionArgDefs["o"] = 0;
			return optionArgDefs;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, OptionArgDefs());
			bool Verbose = PropertiesUtils.GetBool(options, "v", false);
			File testTreebank = options.Contains("t") ? new File(options.GetProperty("t")) : null;
			int maxGoldSentLen = PropertiesUtils.GetInt(options, "l", int.MaxValue);
			bool SerInput = PropertiesUtils.GetBool(options, "o", false);
			string[] parsedArgs = options.GetProperty(string.Empty, string.Empty).Split("\\s+");
			if (parsedArgs.Length != MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			File trainTreebank = new File(parsedArgs[0]);
			DateTime startTime = new DateTime();
			log.Info("###################################");
			log.Info("### Joint Segmentation / Parser ###");
			log.Info("###################################");
			System.Console.Error.Printf("Start time: %s\n", startTime);
			JointParsingModel parsingModel = new JointParsingModel();
			parsingModel.SetVerbose(Verbose);
			parsingModel.SetMaxEvalSentLen(maxGoldSentLen);
			parsingModel.SetSerInput(SerInput);
			//WSGDEBUG -- Some stuff for eclipse debugging
			InputStream inputStream = null;
			try
			{
				if (Runtime.GetProperty("eclipse") == null)
				{
					inputStream = (SerInput) ? new ObjectInputStream(new GZIPInputStream(Runtime.@in)) : Runtime.@in;
				}
				else
				{
					FileInputStream fileStream = new FileInputStream(new File("debug.2.xml"));
					inputStream = (SerInput) ? new ObjectInputStream(new GZIPInputStream(fileStream)) : fileStream;
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				System.Environment.Exit(-1);
			}
			finally
			{
				if (inputStream != null)
				{
					try
					{
						inputStream.Close();
					}
					catch (IOException)
					{
					}
				}
			}
			if (!trainTreebank.Exists())
			{
				log.Info("Training treebank does not exist!\n  " + trainTreebank.GetPath());
			}
			else
			{
				if (testTreebank != null && !testTreebank.Exists())
				{
					log.Info("Test treebank does not exist!\n  " + testTreebank.GetPath());
				}
				else
				{
					if (parsingModel.Run(trainTreebank, testTreebank, inputStream))
					{
						log.Info("Successful shutdown!");
					}
					else
					{
						log.Error("Parsing model failure.");
					}
				}
			}
			DateTime stopTime = new DateTime();
			long elapsedTime = stopTime.GetTime() - startTime.GetTime();
			log.Info();
			log.Info();
			System.Console.Error.Printf("Completed processing at %s\n", stopTime);
			System.Console.Error.Printf("Elapsed time: %d seconds\n", (int)(elapsedTime / 1000F));
		}
	}
}
