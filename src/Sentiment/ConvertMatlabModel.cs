using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>
	/// This tool is of very limited scope: it converts a model built with
	/// the Matlab version of the code to the Java version of the code.
	/// </summary>
	/// <remarks>
	/// This tool is of very limited scope: it converts a model built with
	/// the Matlab version of the code to the Java version of the code.  It
	/// is useful to save this tool in case the format of the Java model
	/// changes, in which case this will let us easily recreate it.
	/// <br />
	/// Another set of matrices is in <br />
	/// /u/nlp/data/sentiment/binary/model_binary_best_asTextFiles/
	/// </remarks>
	/// <author>John Bauer</author>
	public class ConvertMatlabModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.ConvertMatlabModel));

		private ConvertMatlabModel()
		{
		}

		// static class
		/// <summary>Will not overwrite an existing word vector if it is already there</summary>
		public static void CopyWordVector(IDictionary<string, SimpleMatrix> wordVectors, string source, string target)
		{
			if (wordVectors.Contains(target) || !wordVectors.Contains(source))
			{
				return;
			}
			log.Info("Using wordVector " + source + " for " + target);
			wordVectors[target] = new SimpleMatrix(wordVectors[source]);
		}

		/// <summary><br />Will</br> overwrite an existing word vector</summary>
		public static void ReplaceWordVector(IDictionary<string, SimpleMatrix> wordVectors, string source, string target)
		{
			if (!wordVectors.Contains(source))
			{
				return;
			}
			wordVectors[target] = new SimpleMatrix(wordVectors[source]);
		}

		/// <exception cref="System.IO.IOException"/>
		public static SimpleMatrix LoadMatrix(string binaryName, string textName)
		{
			File matrixFile = new File(binaryName);
			if (matrixFile.Exists())
			{
				return SimpleMatrix.LoadBinary(matrixFile.GetPath());
			}
			matrixFile = new File(textName);
			if (matrixFile.Exists())
			{
				return NeuralUtils.LoadTextMatrix(matrixFile);
			}
			throw new Exception("Could not find either " + binaryName + " or " + textName);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string basePath = "/user/socherr/scr/projects/semComp/RNTN/src/params/";
			int numSlices = 25;
			bool useEscapedParens = false;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-slices"))
				{
					numSlices = System.Convert.ToInt32(args[argIndex + 1]);
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-path"))
					{
						basePath = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-useEscapedParens"))
						{
							useEscapedParens = true;
							argIndex += 1;
						}
						else
						{
							log.Info("Unknown argument " + args[argIndex]);
							System.Environment.Exit(2);
						}
					}
				}
			}
			SimpleMatrix[] slices = new SimpleMatrix[numSlices];
			for (int i = 0; i < numSlices; ++i)
			{
				slices[i] = LoadMatrix(basePath + "bin/Wt_" + (i + 1) + ".bin", basePath + "Wt_" + (i + 1) + ".txt");
			}
			SimpleTensor tensor = new SimpleTensor(slices);
			log.Info("W tensor size: " + tensor.NumRows() + "x" + tensor.NumCols() + "x" + tensor.NumSlices());
			SimpleMatrix W = LoadMatrix(basePath + "bin/W.bin", basePath + "W.txt");
			log.Info("W matrix size: " + W.NumRows() + "x" + W.NumCols());
			SimpleMatrix Wcat = LoadMatrix(basePath + "bin/Wcat.bin", basePath + "Wcat.txt");
			log.Info("W cat size: " + Wcat.NumRows() + "x" + Wcat.NumCols());
			SimpleMatrix combinedWV = LoadMatrix(basePath + "bin/Wv.bin", basePath + "Wv.txt");
			log.Info("Word matrix size: " + combinedWV.NumRows() + "x" + combinedWV.NumCols());
			File vocabFile = new File(basePath + "vocab_1.txt");
			if (!vocabFile.Exists())
			{
				vocabFile = new File(basePath + "words.txt");
			}
			IList<string> lines = Generics.NewArrayList();
			foreach (string line in IOUtils.ReadLines(vocabFile))
			{
				lines.Add(line.Trim());
			}
			log.Info("Lines in vocab file: " + lines.Count);
			IDictionary<string, SimpleMatrix> wordVectors = Generics.NewTreeMap();
			for (int i_1 = 0; i_1 < lines.Count && i_1 < combinedWV.NumCols(); ++i_1)
			{
				string[] pieces = lines[i_1].Split(" +");
				if (pieces.Length == 0 || pieces.Length > 1)
				{
					continue;
				}
				wordVectors[pieces[0]] = combinedWV.ExtractMatrix(0, numSlices, i_1, i_1 + 1);
				if (pieces[0].Equals("UNK"))
				{
					wordVectors[SentimentModel.UnknownWord] = wordVectors["UNK"];
				}
			}
			// If there is no ",", we first try to look for an HTML escaping,
			// then fall back to "." as better than just a random word vector.
			// Same for "``" and ";"
			CopyWordVector(wordVectors, "&#44", ",");
			CopyWordVector(wordVectors, ".", ",");
			CopyWordVector(wordVectors, "&#59", ";");
			CopyWordVector(wordVectors, ".", ";");
			CopyWordVector(wordVectors, "&#96&#96", "``");
			CopyWordVector(wordVectors, "''", "``");
			if (useEscapedParens)
			{
				ReplaceWordVector(wordVectors, "(", "-LRB-");
				ReplaceWordVector(wordVectors, ")", "-RRB-");
			}
			RNNOptions op = new RNNOptions();
			op.numHid = numSlices;
			op.lowercaseWordVectors = false;
			if (Wcat.NumRows() == 2)
			{
				op.classNames = new string[] { "Negative", "Positive" };
				op.equivalenceClasses = new int[][] { new int[] { 0 }, new int[] { 1 } };
				// TODO: set to null once old models are updated
				op.numClasses = 2;
			}
			if (!wordVectors.Contains(SentimentModel.UnknownWord))
			{
				wordVectors[SentimentModel.UnknownWord] = SimpleMatrix.Random(numSlices, 1, -0.00001, 0.00001, new Random());
			}
			SentimentModel model = SentimentModel.ModelFromMatrices(W, Wcat, tensor, wordVectors, op);
			model.SaveSerialized("matlab.ser.gz");
		}
	}
}
