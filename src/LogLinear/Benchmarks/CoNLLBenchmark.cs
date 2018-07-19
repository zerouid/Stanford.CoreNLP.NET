using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Inference;
using Edu.Stanford.Nlp.Loglinear.Learning;
using Edu.Stanford.Nlp.Loglinear.Model;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Benchmarks
{
	/// <summary>Created on 8/26/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This loads the CoNLL dataset and 300 dimensional google word embeddings and trains a model on the data using binary
	/// and unary factors. This is a nice explanation of why it is key to have ConcatVector as a datastructure, since there
	/// is no need to specify the number of words in advance anywhere, and data structures will happily resize with a minimum
	/// of GCC wastage.
	/// </author>
	public class CoNLLBenchmark
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(CoNLLBenchmark));

		internal const string DataPath = "/u/nlp/data/ner/conll/";

		internal IDictionary<string, double[]> embeddings = new Dictionary<string, double[]>();

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			new CoNLLBenchmark().BenchmarkOptimizer();
		}

		/// <exception cref="System.Exception"/>
		public virtual void BenchmarkOptimizer()
		{
			IList<CoNLLBenchmark.CoNLLSentence> train = GetSentences(DataPath + "conll.iob.4class.train");
			IList<CoNLLBenchmark.CoNLLSentence> testA = GetSentences(DataPath + "conll.iob.4class.testa");
			IList<CoNLLBenchmark.CoNLLSentence> testB = GetSentences(DataPath + "conll.iob.4class.testb");
			IList<CoNLLBenchmark.CoNLLSentence> allData = new List<CoNLLBenchmark.CoNLLSentence>();
			Sharpen.Collections.AddAll(allData, train);
			Sharpen.Collections.AddAll(allData, testA);
			Sharpen.Collections.AddAll(allData, testB);
			ICollection<string> tagsSet = new HashSet<string>();
			foreach (CoNLLBenchmark.CoNLLSentence sentence in allData)
			{
				foreach (string nerTag in sentence.ner)
				{
					tagsSet.Add(nerTag);
				}
			}
			IList<string> tags = new List<string>();
			Sharpen.Collections.AddAll(tags, tagsSet);
			embeddings = GetEmbeddings(DataPath + "google-300-trimmed.ser.gz", allData);
			log.Info("Making the training set...");
			ConcatVectorNamespace @namespace = new ConcatVectorNamespace();
			int trainSize = train.Count;
			GraphicalModel[] trainingSet = new GraphicalModel[trainSize];
			for (int i = 0; i < trainSize; i++)
			{
				if (i % 10 == 0)
				{
					log.Info(i + "/" + trainSize);
				}
				trainingSet[i] = GenerateSentenceModel(@namespace, train[i], tags);
			}
			log.Info("Training system...");
			AbstractBatchOptimizer opt = new BacktrackingAdaGradOptimizer();
			// This training call is basically what we want the benchmark for. It should take 99% of the wall clock time
			ConcatVector weights = opt.Optimize(trainingSet, new LogLikelihoodDifferentiableFunction(), @namespace.NewWeightsVector(), 0.01, 1.0e-5, false);
			log.Info("Testing system...");
			// Evaluation method lifted from the CoNLL 2004 perl script
			IDictionary<string, double> correctChunk = new Dictionary<string, double>();
			IDictionary<string, double> foundCorrect = new Dictionary<string, double>();
			IDictionary<string, double> foundGuessed = new Dictionary<string, double>();
			double correct = 0.0;
			double total = 0.0;
			foreach (CoNLLBenchmark.CoNLLSentence sentence_1 in testA)
			{
				GraphicalModel model = GenerateSentenceModel(@namespace, sentence_1, tags);
				int[] guesses = new CliqueTree(model, weights).CalculateMAP();
				string[] nerGuesses = new string[guesses.Length];
				for (int i_1 = 0; i_1 < guesses.Length; i_1++)
				{
					nerGuesses[i_1] = tags[guesses[i_1]];
					if (nerGuesses[i_1].Equals(sentence_1.ner[i_1]))
					{
						correct++;
						correctChunk[nerGuesses[i_1]] = correctChunk.GetOrDefault(nerGuesses[i_1], 0.) + 1;
					}
					total++;
					foundCorrect[sentence_1.ner[i_1]] = foundCorrect.GetOrDefault(sentence_1.ner[i_1], 0.) + 1;
					foundGuessed[nerGuesses[i_1]] = foundGuessed.GetOrDefault(nerGuesses[i_1], 0.) + 1;
				}
			}
			log.Info("\nSystem results:\n");
			log.Info("Accuracy: " + (correct / total) + "\n");
			foreach (string tag in tags)
			{
				double precision = foundGuessed.GetOrDefault(tag, 0.0) == 0 ? 0.0 : correctChunk.GetOrDefault(tag, 0.0) / foundGuessed[tag];
				double recall = foundCorrect.GetOrDefault(tag, 0.0) == 0 ? 0.0 : correctChunk.GetOrDefault(tag, 0.0) / foundCorrect[tag];
				double f1 = (precision + recall == 0.0) ? 0.0 : (precision * recall * 2) / (precision + recall);
				log.Info(tag + " (" + foundCorrect.GetOrDefault(tag, 0.0) + ")");
				log.Info("\tP:" + precision + " (" + correctChunk.GetOrDefault(tag, 0.0) + "/" + foundGuessed.GetOrDefault(tag, 0.0) + ")");
				log.Info("\tR:" + recall + " (" + correctChunk.GetOrDefault(tag, 0.0) + "/" + foundCorrect.GetOrDefault(tag, 0.0) + ")");
				log.Info("\tF1:" + f1);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////
		// GENERATING MODELS
		////////////////////////////////////////////////////////////////////////////////////////////
		private static string GetWordShape(string @string)
		{
			if (@string.ToUpper().Equals(@string) && @string.ToLower().Equals(@string))
			{
				return "no-case";
			}
			if (@string.ToUpper().Equals(@string))
			{
				return "upper-case";
			}
			if (@string.ToLower().Equals(@string))
			{
				return "lower-case";
			}
			if (@string.Length > 1 && char.IsUpperCase(@string[0]) && Sharpen.Runtime.Substring(@string, 1).ToLower().Equals(Sharpen.Runtime.Substring(@string, 1)))
			{
				return "capitalized";
			}
			return "mixed-case";
		}

		public virtual GraphicalModel GenerateSentenceModel(ConcatVectorNamespace @namespace, CoNLLBenchmark.CoNLLSentence sentence, IList<string> tags)
		{
			GraphicalModel model = new GraphicalModel();
			for (int i = 0; i < sentence.token.Count; i++)
			{
				// Add the training label
				IDictionary<string, string> metadata = model.GetVariableMetaDataByReference(i);
				metadata[LogLikelihoodDifferentiableFunction.VariableTrainingValue] = string.Empty + tags.IndexOf(sentence.ner[i]);
				metadata["TOKEN"] = string.Empty + sentence.token[i];
				metadata["POS"] = string.Empty + sentence.pos[i];
				metadata["CHUNK"] = string.Empty + sentence.npchunk[i];
				metadata["TAG"] = string.Empty + sentence.ner[i];
			}
			CoNLLFeaturizer.Annotate(model, tags, @namespace, embeddings);
			System.Diagnostics.Debug.Assert((model.factors != null));
			foreach (GraphicalModel.Factor f in model.factors)
			{
				System.Diagnostics.Debug.Assert((f != null));
			}
			return model;
		}

		public class CoNLLSentence
		{
			public IList<string> token = new List<string>();

			public IList<string> ner = new List<string>();

			public IList<string> pos = new List<string>();

			public IList<string> npchunk = new List<string>();

			public CoNLLSentence(IList<string> token, IList<string> ner, IList<string> pos, IList<string> npchunk)
			{
				////////////////////////////////////////////////////////////////////////////////////////////
				// LOADING DATA FROM FILES
				////////////////////////////////////////////////////////////////////////////////////////////
				this.token = token;
				this.ner = ner;
				this.pos = pos;
				this.npchunk = npchunk;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual IList<CoNLLBenchmark.CoNLLSentence> GetSentences(string filename)
		{
			IList<CoNLLBenchmark.CoNLLSentence> sentences = new List<CoNLLBenchmark.CoNLLSentence>();
			IList<string> tokens = new List<string>();
			IList<string> ner = new List<string>();
			IList<string> pos = new List<string>();
			IList<string> npchunk = new List<string>();
			BufferedReader br = new BufferedReader(new FileReader(filename));
			string line;
			while ((line = br.ReadLine()) != null)
			{
				string[] parts = line.Split("\t");
				if (parts.Length == 4)
				{
					tokens.Add(parts[0]);
					pos.Add(parts[1]);
					npchunk.Add(parts[2]);
					string tag = parts[3];
					if (tag.Contains("-"))
					{
						ner.Add(tag.Split("-")[1]);
					}
					else
					{
						ner.Add(tag);
					}
					if (parts[0].Equals("."))
					{
						sentences.Add(new CoNLLBenchmark.CoNLLSentence(tokens, ner, pos, npchunk));
						tokens = new List<string>();
						ner = new List<string>();
						pos = new List<string>();
						npchunk = new List<string>();
					}
				}
			}
			return sentences;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual IDictionary<string, double[]> GetEmbeddings(string cacheFilename, IList<CoNLLBenchmark.CoNLLSentence> sentences)
		{
			File f = new File(cacheFilename);
			IDictionary<string, double[]> trimmedSet;
			if (!f.Exists())
			{
				trimmedSet = new Dictionary<string, double[]>();
				IDictionary<string, double[]> massiveSet = LoadEmbeddingsFromFile("../google-300.txt");
				log.Info("Got massive embedding set size " + massiveSet.Count);
				foreach (CoNLLBenchmark.CoNLLSentence sentence in sentences)
				{
					foreach (string token in sentence.token)
					{
						if (massiveSet.Contains(token))
						{
							trimmedSet[token] = massiveSet[token];
						}
					}
				}
				log.Info("Got trimmed embedding set size " + trimmedSet.Count);
				f.CreateNewFile();
				ObjectOutputStream oos = new ObjectOutputStream(new GZIPOutputStream(new FileOutputStream(cacheFilename)));
				oos.WriteObject(trimmedSet);
				oos.Close();
				log.Info("Wrote trimmed set to file");
			}
			else
			{
				ObjectInputStream ois = new ObjectInputStream(new GZIPInputStream(new FileInputStream(cacheFilename)));
				trimmedSet = (IDictionary<string, double[]>)ois.ReadObject();
				ois.Close();
			}
			return trimmedSet;
		}

		/// <exception cref="System.IO.IOException"/>
		private static IDictionary<string, double[]> LoadEmbeddingsFromFile(string filename)
		{
			IDictionary<string, double[]> embeddings = new Dictionary<string, double[]>();
			BufferedReader br = new BufferedReader(new FileReader(filename));
			int readLines = 0;
			string line = br.ReadLine();
			while ((line = br.ReadLine()) != null)
			{
				string[] parts = line.Split(" ");
				if (parts.Length == 302)
				{
					string token = parts[0];
					double[] embedding = new double[300];
					for (int i = 1; i < parts.Length - 1; i++)
					{
						embedding[i - 1] = double.ParseDouble(parts[i]);
					}
					embeddings[token] = embedding;
				}
				readLines++;
				if (readLines % 10000 == 0)
				{
					log.Info("Read " + readLines + " lines");
				}
			}
			return embeddings;
		}
	}
}
