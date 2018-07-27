using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <summary>A data preparation pipeline for treebanks.</summary>
	/// <remarks>
	/// A data preparation pipeline for treebanks.
	/// <p>
	/// A simple framework for preparing various kinds of treebank data. The original goal was to prepare the
	/// Penn Arabic Treebank (PATB) trees for parsing. This pipeline arose from the
	/// need to prepare various data sets in a uniform manner for the execution of experiments that require
	/// multiple tools. The design objectives are:
	/// <ul>
	/// <li>Support multiple data input and output types
	/// <li>Allow parameterization of data sets via a plain text file
	/// <li>Support rapid, cheap lexical engineering
	/// <li>End result of processing: a folder with all data sets and a manifest of how the data was prepared
	/// </ul>
	/// <p>
	/// These objectives are realized through three features:
	/// <ul>
	/// <li>
	/// <see cref="ConfigParser"/>
	/// -- reads the plain text configuration file and creates configuration parameter objects for each data set
	/// <li>
	/// <see cref="IDataset"/>
	/// interface -- Generic interface for loading, processing, and writing datasets
	/// <li>
	/// <see cref="IMapper"/>
	/// interface -- Generic interface for applying transformations to strings (usually words and POS tags)
	/// </ul>
	/// <p>
	/// The process for preparing arbitrary data set X is as follows:
	/// <ol>
	/// <li>Add parameters to
	/// <see cref="ConfigParser"/>
	/// as necessary
	/// <li>Implement the
	/// <see cref="IDataset"/>
	/// interface for the new data set (or use one of the existing classes)
	/// <li>Implement
	/// <see cref="IMapper"/>
	/// classes as needed
	/// <li>Specify the data set parameters in a plain text file
	/// <li>Run
	/// <see cref="TreebankPreprocessor"/>
	/// using the plain text file as the argument
	/// </ol>
	/// </remarks>
	/// <author>Spence Green</author>
	public sealed class TreebankPreprocessor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Treebank.TreebankPreprocessor));

		private TreebankPreprocessor()
		{
		}

		private static string Usage()
		{
			string cmdLineFormat = string.Format("java %s [OPTIONS] config_file%n", typeof(Edu.Stanford.Nlp.Trees.Treebank.TreebankPreprocessor).FullName);
			StringBuilder sb = new StringBuilder(cmdLineFormat);
			//Add other parameters here
			sb.Append(string.Format("  -v         : Show verbose output%n"));
			sb.Append(string.Format("  -d <name>  : Make a distributable package with the specified name%n"));
			return sb.ToString();
		}

		private static IDataset GetDatasetClass(Properties dsParams)
		{
			IDataset ds = null;
			string dsType = dsParams.GetProperty(ConfigParser.paramType);
			dsParams.Remove(ConfigParser.paramType);
			try
			{
				if (dsType == null)
				{
					ds = new ATBArabicDataset();
				}
				else
				{
					Type c = ClassLoader.GetSystemClassLoader().LoadClass(dsType);
					ds = (IDataset)System.Activator.CreateInstance(c);
				}
			}
			catch (TypeLoadException)
			{
				System.Console.Error.Printf("Dataset type %s does not exist%n", dsType);
			}
			catch (InstantiationException)
			{
				System.Console.Error.Printf("Unable to instantiate dataset type %s%n", dsType);
			}
			catch (MemberAccessException)
			{
				System.Console.Error.Printf("Unable to access dataset type %s%n", dsType);
			}
			return ds;
		}

		private const int MinArgs = 1;

		private static bool Verbose = false;

		private static bool MakeDistrib = false;

		private static string distribName = null;

		private static string configFile = null;

		private static string outputPath = null;

		public static readonly IDictionary<string, int> optionArgDefs = Generics.NewHashMap();

		static TreebankPreprocessor()
		{
			//Command line options
			optionArgDefs["-d"] = 1;
			optionArgDefs["-v"] = 0;
			optionArgDefs["-p"] = 1;
		}

		private static bool ValidateCommandLine(string[] args)
		{
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, optionArgDefs);
			foreach (KeyValuePair<string, string[]> opt in argsMap)
			{
				string key = opt.Key;
				if (key == null)
				{
				}
				else
				{
					switch (key)
					{
						case "-d":
						{
							// continue;
							MakeDistrib = true;
							distribName = opt.Value[0];
							break;
						}

						case "-v":
						{
							Verbose = true;
							break;
						}

						case "-p":
						{
							outputPath = opt.Value[0];
							break;
						}

						default:
						{
							return false;
						}
					}
				}
			}
			//Regular arguments
			string[] rest = argsMap[null];
			if (rest == null || rest.Length != MinArgs)
			{
				return false;
			}
			else
			{
				configFile = rest[0];
			}
			return true;
		}

		/// <summary>Execute with no arguments for usage.</summary>
		public static void Main(string[] args)
		{
			if (!ValidateCommandLine(args))
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			DateTime startTime = new DateTime();
			System.Console.Out.WriteLine("##################################");
			System.Console.Out.WriteLine("# Stanford Treebank Preprocessor #");
			System.Console.Out.WriteLine("##################################");
			System.Console.Out.Printf("Start time: %s%n", startTime);
			System.Console.Out.Printf("Configuration: %s%n%n", configFile);
			ConfigParser cp = new ConfigParser(configFile);
			cp.Parse();
			DistributionPackage distrib = new DistributionPackage();
			foreach (Properties dsParams in cp)
			{
				string nameOfDataset = PropertiesUtils.HasProperty(dsParams, ConfigParser.paramName) ? dsParams.GetProperty(ConfigParser.paramName) : "UN-NAMED";
				if (outputPath != null)
				{
					dsParams.SetProperty(ConfigParser.paramOutputPath, outputPath);
				}
				IDataset ds = GetDatasetClass(dsParams);
				if (ds == null)
				{
					System.Console.Out.Printf("Unable to instantiate TYPE for dataset %s. Check the javadocs%n", nameOfDataset);
					continue;
				}
				bool shouldDistribute = dsParams.Contains(ConfigParser.paramDistrib) && bool.ParseBoolean(dsParams.GetProperty(ConfigParser.paramDistrib));
				dsParams.Remove(ConfigParser.paramDistrib);
				bool lacksRequiredOptions = !(ds.SetOptions(dsParams));
				if (lacksRequiredOptions)
				{
					System.Console.Out.Printf("Skipping dataset %s as it lacks required parameters. Check the javadocs%n", nameOfDataset);
					continue;
				}
				ds.Build();
				if (shouldDistribute)
				{
					distrib.AddFiles(ds.GetFilenames());
				}
				if (Verbose)
				{
					System.Console.Out.Printf("%s%n", ds.ToString());
				}
			}
			if (MakeDistrib)
			{
				distrib.Make(distribName);
			}
			if (Verbose)
			{
				System.Console.Out.WriteLine("-->configuration details");
				System.Console.Out.WriteLine(cp.ToString());
				if (MakeDistrib)
				{
					System.Console.Out.WriteLine("-->distribution package details");
					System.Console.Out.WriteLine(distrib.ToString());
				}
			}
			DateTime stopTime = new DateTime();
			long elapsedTime = stopTime.GetTime() - startTime.GetTime();
			System.Console.Out.Printf("Completed processing at %s%n", stopTime);
			System.Console.Out.Printf("Elapsed time: %d seconds%n", (int)(elapsedTime / 1000F));
		}
	}
}
