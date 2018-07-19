using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Spanish;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Pipeline
{
	/// <summary>
	/// A utility to build unigram part-of-speech tagging data from XML
	/// corpus files from the AnCora corpus.
	/// </summary>
	/// <remarks>
	/// A utility to build unigram part-of-speech tagging data from XML
	/// corpus files from the AnCora corpus.
	/// The constructed tagger is used to tag the constituent tokens of
	/// multi-word expressions, which have no tags in the AnCora corpus.
	/// For invocation options, run the program with no arguments.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	public class AnCoraPOSStats
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Spanish.Pipeline.AnCoraPOSStats));

		private readonly TwoDimensionalCounter<string, string> unigramTagger;

		private const string AncoraEncoding = "ISO8859_1";

		private IList<File> fileList;

		private string outputPath;

		public AnCoraPOSStats(IList<File> fileList, string outputPath)
		{
			this.fileList = fileList;
			this.outputPath = outputPath;
			unigramTagger = new TwoDimensionalCounter<string, string>();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Process()
		{
			SpanishXMLTreeReaderFactory trf = new SpanishXMLTreeReaderFactory();
			Tree t;
			foreach (File file in fileList)
			{
				Reader @in = new BufferedReader(new InputStreamReader(new FileInputStream(file), AncoraEncoding));
				ITreeReader tr = trf.NewTreeReader(@in);
				// Tree reading will implicitly perform tree normalization for us
				while ((t = tr.ReadTree()) != null)
				{
					// Update tagger with this tree
					IList<CoreLabel> yield = t.TaggedLabeledYield();
					foreach (CoreLabel leafLabel in yield)
					{
						if (leafLabel.Tag().Equals(SpanishTreeNormalizer.MwTag))
						{
							continue;
						}
						unigramTagger.IncrementCount(leafLabel.Word(), leafLabel.Tag());
					}
				}
			}
		}

		public virtual TwoDimensionalCounter<string, string> GetUnigramTagger()
		{
			return unigramTagger;
		}

		private static readonly string usage = string.Format("Usage: java %s -o <output_path> file(s)%n%n", typeof(Edu.Stanford.Nlp.International.Spanish.Pipeline.AnCoraPOSStats).FullName);

		private static readonly IDictionary<string, int> argOptionDefs = new Dictionary<string, int>();

		static AnCoraPOSStats()
		{
			argOptionDefs["o"] = 1;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				log.Info(usage);
				System.Environment.Exit(1);
			}
			Properties options = StringUtils.ArgsToProperties(args, argOptionDefs);
			string outputPath = options.GetProperty("o");
			if (outputPath == null)
			{
				throw new ArgumentException("-o argument (output path for built tagger) is required");
			}
			string[] remainingArgs = options.GetProperty(string.Empty).Split(" ");
			IList<File> fileList = new List<File>();
			foreach (string arg in remainingArgs)
			{
				fileList.Add(new File(arg));
			}
			Edu.Stanford.Nlp.International.Spanish.Pipeline.AnCoraPOSStats stats = new Edu.Stanford.Nlp.International.Spanish.Pipeline.AnCoraPOSStats(fileList, outputPath);
			stats.Process();
			ObjectOutputStream oos = new ObjectOutputStream(new FileOutputStream(outputPath));
			TwoDimensionalCounter<string, string> tagger = stats.GetUnigramTagger();
			oos.WriteObject(tagger);
			System.Console.Out.Printf("Wrote tagger to %s%n", outputPath);
		}
	}
}
