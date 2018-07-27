using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>
	/// Given a list of input DVParser models, this tool will output a new
	/// DVParser which is the average of all of those models.
	/// </summary>
	/// <remarks>
	/// Given a list of input DVParser models, this tool will output a new
	/// DVParser which is the average of all of those models.  Sadly, this
	/// does not actually seem to help; the resulting model is generally
	/// worse than the input models, and definitely worse than the models
	/// used in combination.
	/// </remarks>
	/// <author>John Bauer</author>
	public class AverageDVModels
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AverageDVModels));

		public static TwoDimensionalSet<string, string> GetBinaryMatrixNames(IList<TwoDimensionalMap<string, string, SimpleMatrix>> maps)
		{
			TwoDimensionalSet<string, string> matrixNames = new TwoDimensionalSet<string, string>();
			foreach (TwoDimensionalMap<string, string, SimpleMatrix> map in maps)
			{
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in map)
				{
					matrixNames.Add(entry.GetFirstKey(), entry.GetSecondKey());
				}
			}
			return matrixNames;
		}

		public static ICollection<string> GetUnaryMatrixNames(IList<IDictionary<string, SimpleMatrix>> maps)
		{
			ICollection<string> matrixNames = Generics.NewHashSet();
			foreach (IDictionary<string, SimpleMatrix> map in maps)
			{
				foreach (KeyValuePair<string, SimpleMatrix> entry in map)
				{
					matrixNames.Add(entry.Key);
				}
			}
			return matrixNames;
		}

		public static TwoDimensionalMap<string, string, SimpleMatrix> AverageBinaryMatrices(IList<TwoDimensionalMap<string, string, SimpleMatrix>> maps)
		{
			TwoDimensionalMap<string, string, SimpleMatrix> averages = TwoDimensionalMap.TreeMap();
			foreach (Pair<string, string> binary in GetBinaryMatrixNames(maps))
			{
				int count = 0;
				SimpleMatrix matrix = null;
				foreach (TwoDimensionalMap<string, string, SimpleMatrix> map in maps)
				{
					if (!map.Contains(binary.First(), binary.Second()))
					{
						continue;
					}
					SimpleMatrix original = map.Get(binary.First(), binary.Second());
					++count;
					if (matrix == null)
					{
						matrix = original;
					}
					else
					{
						matrix = matrix.Plus(original);
					}
				}
				matrix = matrix.Divide(count);
				averages.Put(binary.First(), binary.Second(), matrix);
			}
			return averages;
		}

		public static IDictionary<string, SimpleMatrix> AverageUnaryMatrices(IList<IDictionary<string, SimpleMatrix>> maps)
		{
			IDictionary<string, SimpleMatrix> averages = Generics.NewTreeMap();
			foreach (string name in GetUnaryMatrixNames(maps))
			{
				int count = 0;
				SimpleMatrix matrix = null;
				foreach (IDictionary<string, SimpleMatrix> map in maps)
				{
					if (!map.Contains(name))
					{
						continue;
					}
					SimpleMatrix original = map[name];
					++count;
					if (matrix == null)
					{
						matrix = original;
					}
					else
					{
						matrix = matrix.Plus(original);
					}
				}
				matrix = matrix.Divide(count);
				averages[name] = matrix;
			}
			return averages;
		}

		/// <summary>
		/// Command line arguments for this program:
		/// <br />
		/// -output: the model file to output
		/// -input: a list of model files to input
		/// </summary>
		public static void Main(string[] args)
		{
			string outputModelFilename = null;
			IList<string> inputModelFilenames = Generics.NewArrayList();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
				{
					outputModelFilename = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
					{
						for (++argIndex; argIndex < args.Length && !args[argIndex].StartsWith("-"); ++argIndex)
						{
							Sharpen.Collections.AddAll(inputModelFilenames, Arrays.AsList(args[argIndex].Split(",")));
						}
					}
					else
					{
						throw new Exception("Unknown argument " + args[argIndex]);
					}
				}
			}
			if (outputModelFilename == null)
			{
				log.Info("Need to specify output model name with -output");
				System.Environment.Exit(2);
			}
			if (inputModelFilenames.Count == 0)
			{
				log.Info("Need to specify input model names with -input");
				System.Environment.Exit(2);
			}
			log.Info("Averaging " + inputModelFilenames);
			log.Info("Outputting result to " + outputModelFilename);
			LexicalizedParser lexparser = null;
			IList<DVModel> models = Generics.NewArrayList();
			foreach (string filename in inputModelFilenames)
			{
				LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(filename));
				if (lexparser == null)
				{
					lexparser = parser;
				}
				models.Add(DVParser.GetModelFromLexicalizedParser(parser));
			}
			IList<TwoDimensionalMap<string, string, SimpleMatrix>> binaryTransformMaps = CollectionUtils.TransformAsList(models, null);
			IList<TwoDimensionalMap<string, string, SimpleMatrix>> binaryScoreMaps = CollectionUtils.TransformAsList(models, null);
			IList<IDictionary<string, SimpleMatrix>> unaryTransformMaps = CollectionUtils.TransformAsList(models, null);
			IList<IDictionary<string, SimpleMatrix>> unaryScoreMaps = CollectionUtils.TransformAsList(models, null);
			IList<IDictionary<string, SimpleMatrix>> wordMaps = CollectionUtils.TransformAsList(models, null);
			TwoDimensionalMap<string, string, SimpleMatrix> binaryTransformAverages = AverageBinaryMatrices(binaryTransformMaps);
			TwoDimensionalMap<string, string, SimpleMatrix> binaryScoreAverages = AverageBinaryMatrices(binaryScoreMaps);
			IDictionary<string, SimpleMatrix> unaryTransformAverages = AverageUnaryMatrices(unaryTransformMaps);
			IDictionary<string, SimpleMatrix> unaryScoreAverages = AverageUnaryMatrices(unaryScoreMaps);
			IDictionary<string, SimpleMatrix> wordAverages = AverageUnaryMatrices(wordMaps);
			DVModel newModel = new DVModel(binaryTransformAverages, unaryTransformAverages, binaryScoreAverages, unaryScoreAverages, wordAverages, lexparser.GetOp());
			DVParser newParser = new DVParser(newModel, lexparser);
			newParser.SaveModel(outputModelFilename);
		}
	}
}
