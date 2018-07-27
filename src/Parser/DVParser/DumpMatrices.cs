using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>Dump out the matrices in a DVModel to a given directory in text format.</summary>
	/// <remarks>
	/// Dump out the matrices in a DVModel to a given directory in text format.
	/// <br />
	/// Sample command line:
	/// <br />
	/// <code>
	/// java -model &lt;modelname&gt; -output &lt;directory&gt;
	/// </code>
	/// </remarks>
	/// <author>John Bauer</author>
	public class DumpMatrices
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(DumpMatrices));

		/// <summary>Output some help and exit</summary>
		public static void Help()
		{
			log.Info("-model : DVModel to load");
			log.Info("-output : where to dump the matrices");
			System.Environment.Exit(2);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void DumpMatrix(string filename, SimpleMatrix matrix)
		{
			string matrixString = matrix.ToString();
			int newLine = matrixString.IndexOf("\n");
			if (newLine >= 0)
			{
				matrixString = Sharpen.Runtime.Substring(matrixString, newLine + 1);
			}
			FileWriter fout = new FileWriter(filename);
			BufferedWriter bout = new BufferedWriter(fout);
			bout.Write(matrixString);
			bout.Close();
			fout.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string modelPath = null;
			string outputDir = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					modelPath = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
					{
						outputDir = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						log.Info("Unknown argument " + args[argIndex]);
						Help();
					}
				}
			}
			if (outputDir == null || modelPath == null)
			{
				Help();
			}
			File outputFile = new File(outputDir);
			FileSystem.CheckNotExistsOrFail(outputFile);
			FileSystem.MkdirOrFail(outputFile);
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(modelPath));
			DVModel model = DVParser.GetModelFromLexicalizedParser(parser);
			string binaryWDir = outputDir + File.separator + "binaryW";
			FileSystem.MkdirOrFail(binaryWDir);
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in model.binaryTransform)
			{
				string filename = binaryWDir + File.separator + entry.GetFirstKey() + "_" + entry.GetSecondKey() + ".txt";
				DumpMatrix(filename, entry.GetValue());
			}
			string binaryScoreDir = outputDir + File.separator + "binaryScore";
			FileSystem.MkdirOrFail(binaryScoreDir);
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry_1 in model.binaryScore)
			{
				string filename = binaryScoreDir + File.separator + entry_1.GetFirstKey() + "_" + entry_1.GetSecondKey() + ".txt";
				DumpMatrix(filename, entry_1.GetValue());
			}
			string unaryWDir = outputDir + File.separator + "unaryW";
			FileSystem.MkdirOrFail(unaryWDir);
			foreach (KeyValuePair<string, SimpleMatrix> entry_2 in model.unaryTransform)
			{
				string filename = unaryWDir + File.separator + entry_2.Key + ".txt";
				DumpMatrix(filename, entry_2.Value);
			}
			string unaryScoreDir = outputDir + File.separator + "unaryScore";
			FileSystem.MkdirOrFail(unaryScoreDir);
			foreach (KeyValuePair<string, SimpleMatrix> entry_3 in model.unaryScore)
			{
				string filename = unaryScoreDir + File.separator + entry_3.Key + ".txt";
				DumpMatrix(filename, entry_3.Value);
			}
			string embeddingFile = outputDir + File.separator + "embeddings.txt";
			FileWriter fout = new FileWriter(embeddingFile);
			BufferedWriter bout = new BufferedWriter(fout);
			foreach (KeyValuePair<string, SimpleMatrix> entry_4 in model.wordVectors)
			{
				bout.Write(entry_4.Key);
				SimpleMatrix vector = entry_4.Value;
				for (int i = 0; i < vector.NumRows(); ++i)
				{
					bout.Write("  " + vector.Get(i, 0));
				}
				bout.Write("\n");
			}
			bout.Close();
			fout.Close();
		}
	}
}
