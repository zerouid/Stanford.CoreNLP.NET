using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>
	/// This class defines a transition-based dependency parser which makes
	/// use of a classifier powered by a neural network.
	/// </summary>
	/// <remarks>
	/// This class defines a transition-based dependency parser which makes
	/// use of a classifier powered by a neural network. The neural network
	/// accepts distributed representation inputs: dense, continuous
	/// representations of words, their part of speech tags, and the labels
	/// which connect words in a partial dependency parse.
	/// <p>
	/// This is an implementation of the method described in
	/// <blockquote>
	/// Danqi Chen and Christopher Manning. A Fast and Accurate Dependency
	/// Parser Using Neural Networks. In <i>EMNLP 2014</i>.
	/// </blockquote>
	/// <p>
	/// The parser can also be used from the command line to train models and to parse text.
	/// New models can be trained from the command line; see the
	/// <see cref="Main(string[])"/>
	/// method
	/// for details on training options. The parser can parse either plain text files or
	/// CoNLL-X format files and output
	/// CoNLL-X format predictions; again see
	/// <see cref="Main(string[])"/>
	/// for available options.
	/// (The options available for things like tokenization and sentence splitting
	/// in this class are not as extensive as and not necessarily consistent with
	/// the options of other classes like
	/// <c>LexicalizedParser</c>
	/// and
	/// <c>StanfordCoreNLP</c>
	/// .
	/// <p>
	/// This parser can also be used programmatically. The easiest way to
	/// prepare the parser with a pre-trained model is to call
	/// <see cref="LoadFromModelFile(string)"/>
	/// . Then call
	/// <see cref="Predict(Edu.Stanford.Nlp.Util.ICoreMap)"/>
	/// on the returned
	/// parser instance in order to get new parses.
	/// </remarks>
	/// <author>Danqi Chen (danqi@cs.stanford.edu)</author>
	/// <author>Jon Gauthier</author>
	public class DependencyParser
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.DependencyParser));

		public const string DefaultModel = "edu/stanford/nlp/models/parser/nndep/english_UD.gz";

		/// <summary>
		/// Words, parts of speech, and dependency relation labels which were
		/// observed in our corpus / stored in the model
		/// </summary>
		/// <seealso cref="GenDictionaries(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E})"/>
		private IList<string> knownWords;

		/// <summary>
		/// Words, parts of speech, and dependency relation labels which were
		/// observed in our corpus / stored in the model
		/// </summary>
		/// <seealso cref="GenDictionaries(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E})"/>
		private IList<string> knownPos;

		/// <summary>
		/// Words, parts of speech, and dependency relation labels which were
		/// observed in our corpus / stored in the model
		/// </summary>
		/// <seealso cref="GenDictionaries(System.Collections.Generic.IList{E}, System.Collections.Generic.IList{E})"/>
		private IList<string> knownLabels;

		/// <summary>Return the set of part-of-speech tags of this parser.</summary>
		/// <remarks>
		/// Return the set of part-of-speech tags of this parser. We normalize it a bit to help it match what
		/// other parsers use.
		/// </remarks>
		/// <returns>Set of POS tags</returns>
		public virtual ICollection<string> GetPosSet()
		{
			ICollection<string> foo = Generics.NewHashSet(knownPos);
			// Don't really understand why these ones are there, but remove them. [CDM 2016]
			foo.Remove("-NULL-");
			foo.Remove("-UNKNOWN-");
			foo.Remove("-ROOT-");
			// but our other models do include an EOS tag
			foo.Add(".$$.");
			return Java.Util.Collections.UnmodifiableSet(foo);
		}

		/// <summary>Mapping from word / POS / dependency relation label to integer ID</summary>
		private IDictionary<string, int> wordIDs;

		/// <summary>Mapping from word / POS / dependency relation label to integer ID</summary>
		private IDictionary<string, int> posIDs;

		/// <summary>Mapping from word / POS / dependency relation label to integer ID</summary>
		private IDictionary<string, int> labelIDs;

		private IList<int> preComputed;

		/// <summary>
		/// Given a particular parser configuration, this classifier will
		/// predict the best transition to make next.
		/// </summary>
		/// <remarks>
		/// Given a particular parser configuration, this classifier will
		/// predict the best transition to make next.
		/// The
		/// <see cref="Classifier"/>
		/// class
		/// handles both training and inference.
		/// </remarks>
		private Classifier classifier;

		private ParsingSystem system;

		private readonly Config config;

		/// <summary>
		/// Language used to generate
		/// <see cref="Edu.Stanford.Nlp.Trees.GrammaticalRelation"/>
		/// instances.
		/// </summary>
		private readonly Language language;

		internal DependencyParser()
			: this(new Properties())
		{
		}

		public DependencyParser(Properties properties)
		{
			config = new Config(properties);
			// Convert Languages.Language instance to
			// GrammaticalLanguage.Language
			this.language = config.language;
		}

		/// <summary>Get an integer ID for the given word.</summary>
		/// <remarks>
		/// Get an integer ID for the given word. This ID can be used to index
		/// into the embeddings
		/// <see cref="Classifier#E"/>
		/// .
		/// </remarks>
		/// <returns>
		/// An ID for the given word, or an ID referring to a generic
		/// "unknown" word if the word is unknown
		/// </returns>
		public virtual int GetWordID(string s)
		{
			return wordIDs.Contains(s) ? wordIDs[s] : wordIDs[Config.Unknown];
		}

		public virtual int GetPosID(string s)
		{
			return posIDs.Contains(s) ? posIDs[s] : posIDs[Config.Unknown];
		}

		public virtual int GetLabelID(string s)
		{
			return labelIDs[s];
		}

		public virtual IList<int> GetFeatures(Configuration c)
		{
			// Presize the arrays for very slight speed gain. Hardcoded, but so is the current feature list.
			IList<int> fWord = new List<int>(18);
			IList<int> fPos = new List<int>(18);
			IList<int> fLabel = new List<int>(12);
			for (int j = 2; j >= 0; --j)
			{
				int index = c.GetStack(j);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
			}
			for (int j_1 = 0; j_1 <= 2; ++j_1)
			{
				int index = c.GetBuffer(j_1);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
			}
			for (int j_2 = 0; j_2 <= 1; ++j_2)
			{
				int k = c.GetStack(j_2);
				int index = c.GetLeftChild(k);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
				index = c.GetRightChild(k);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
				index = c.GetLeftChild(k, 2);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
				index = c.GetRightChild(k, 2);
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
				index = c.GetLeftChild(c.GetLeftChild(k));
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
				index = c.GetRightChild(c.GetRightChild(k));
				fWord.Add(GetWordID(c.GetWord(index)));
				fPos.Add(GetPosID(c.GetPOS(index)));
				fLabel.Add(GetLabelID(c.GetLabel(index)));
			}
			IList<int> feature = new List<int>(48);
			Sharpen.Collections.AddAll(feature, fWord);
			Sharpen.Collections.AddAll(feature, fPos);
			Sharpen.Collections.AddAll(feature, fLabel);
			return feature;
		}

		private const int PosOffset = 18;

		private const int DepOffset = 36;

		private const int StackOffset = 6;

		private const int StackNumber = 6;

		private int[] GetFeatureArray(Configuration c)
		{
			int[] feature = new int[config.numTokens];
			// positions 0-17 hold fWord, 18-35 hold fPos, 36-47 hold fLabel
			for (int j = 2; j >= 0; --j)
			{
				int index = c.GetStack(j);
				feature[2 - j] = GetWordID(c.GetWord(index));
				feature[PosOffset + (2 - j)] = GetPosID(c.GetPOS(index));
			}
			for (int j_1 = 0; j_1 <= 2; ++j_1)
			{
				int index = c.GetBuffer(j_1);
				feature[3 + j_1] = GetWordID(c.GetWord(index));
				feature[PosOffset + 3 + j_1] = GetPosID(c.GetPOS(index));
			}
			for (int j_2 = 0; j_2 <= 1; ++j_2)
			{
				int k = c.GetStack(j_2);
				int index = c.GetLeftChild(k);
				feature[StackOffset + j_2 * StackNumber] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber] = GetLabelID(c.GetLabel(index));
				index = c.GetRightChild(k);
				feature[StackOffset + j_2 * StackNumber + 1] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber + 1] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber + 1] = GetLabelID(c.GetLabel(index));
				index = c.GetLeftChild(k, 2);
				feature[StackOffset + j_2 * StackNumber + 2] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber + 2] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber + 2] = GetLabelID(c.GetLabel(index));
				index = c.GetRightChild(k, 2);
				feature[StackOffset + j_2 * StackNumber + 3] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber + 3] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber + 3] = GetLabelID(c.GetLabel(index));
				index = c.GetLeftChild(c.GetLeftChild(k));
				feature[StackOffset + j_2 * StackNumber + 4] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber + 4] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber + 4] = GetLabelID(c.GetLabel(index));
				index = c.GetRightChild(c.GetRightChild(k));
				feature[StackOffset + j_2 * StackNumber + 5] = GetWordID(c.GetWord(index));
				feature[PosOffset + StackOffset + j_2 * StackNumber + 5] = GetPosID(c.GetPOS(index));
				feature[DepOffset + j_2 * StackNumber + 5] = GetLabelID(c.GetLabel(index));
			}
			return feature;
		}

		public virtual Dataset GenTrainExamples(IList<ICoreMap> sents, IList<DependencyTree> trees)
		{
			int numTrans = system.NumTransitions();
			Dataset ret = new Dataset(config.numTokens, numTrans);
			ICounter<int> tokPosCount = new IntCounter<int>();
			log.Info(Config.Separator);
			log.Info("Generate training examples...");
			for (int i = 0; i < sents.Count; ++i)
			{
				if (i > 0)
				{
					if (i % 1000 == 0)
					{
						log.Info(i + " ");
					}
					if (i % 10000 == 0 || i == sents.Count - 1)
					{
						log.Info();
					}
				}
				if (trees[i].IsProjective())
				{
					Configuration c = system.InitialConfiguration(sents[i]);
					while (!system.IsTerminal(c))
					{
						string oracle = system.GetOracle(c, trees[i]);
						IList<int> feature = GetFeatures(c);
						IList<int> label = new List<int>();
						for (int j = 0; j < numTrans; ++j)
						{
							string str = system.transitions[j];
							if (str.Equals(oracle))
							{
								label.Add(1);
							}
							else
							{
								if (system.CanApply(c, str))
								{
									label.Add(0);
								}
								else
								{
									label.Add(-1);
								}
							}
						}
						ret.AddExample(feature, label);
						for (int j_1 = 0; j_1 < feature.Count; ++j_1)
						{
							tokPosCount.IncrementCount(feature[j_1] * feature.Count + j_1);
						}
						system.Apply(c, oracle);
					}
				}
			}
			log.Info("#Train Examples: " + ret.n);
			IList<int> sortedTokens = Counters.ToSortedList(tokPosCount, false);
			preComputed = new List<int>(sortedTokens.SubList(0, Math.Min(config.numPreComputed, sortedTokens.Count)));
			return ret;
		}

		/// <summary>
		/// Generate unique integer IDs for all known words / part-of-speech
		/// tags / dependency relation labels.
		/// </summary>
		/// <remarks>
		/// Generate unique integer IDs for all known words / part-of-speech
		/// tags / dependency relation labels.
		/// All three of the aforementioned types are assigned IDs from a
		/// continuous range of integers; all IDs 0 &lt;= ID &lt; n_w are word IDs,
		/// all IDs n_w &lt;= ID &lt; n_w + n_pos are POS tag IDs, and so on.
		/// </remarks>
		private void GenerateIDs()
		{
			wordIDs = new Dictionary<string, int>();
			posIDs = new Dictionary<string, int>();
			labelIDs = new Dictionary<string, int>();
			int index = 0;
			foreach (string word in knownWords)
			{
				wordIDs[word] = (index++);
			}
			foreach (string pos in knownPos)
			{
				posIDs[pos] = (index++);
			}
			foreach (string label in knownLabels)
			{
				labelIDs[label] = (index++);
			}
		}

		/// <summary>
		/// Scan a corpus and store all words, part-of-speech tags, and
		/// dependency relation labels observed.
		/// </summary>
		/// <remarks>
		/// Scan a corpus and store all words, part-of-speech tags, and
		/// dependency relation labels observed. Prepare other structures
		/// which support word / POS / label lookup at train- / run-time.
		/// </remarks>
		private void GenDictionaries(IList<ICoreMap> sents, IList<DependencyTree> trees)
		{
			// Collect all words (!), etc. in lists, tacking on one sentence
			// after the other
			IList<string> word = new List<string>();
			IList<string> pos = new List<string>();
			IList<string> label = new List<string>();
			foreach (ICoreMap sentence in sents)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				foreach (CoreLabel token in tokens)
				{
					word.Add(token.Word());
					pos.Add(token.Tag());
				}
			}
			string rootLabel = null;
			foreach (DependencyTree tree in trees)
			{
				for (int k = 1; k <= tree.n; ++k)
				{
					if (tree.GetHead(k) == 0)
					{
						rootLabel = tree.GetLabel(k);
					}
					else
					{
						label.Add(tree.GetLabel(k));
					}
				}
			}
			// Generate "dictionaries," possibly with frequency cutoff
			knownWords = Edu.Stanford.Nlp.Parser.Nndep.Util.GenerateDict(word, config.wordCutOff);
			knownPos = Edu.Stanford.Nlp.Parser.Nndep.Util.GenerateDict(pos);
			knownLabels = Edu.Stanford.Nlp.Parser.Nndep.Util.GenerateDict(label);
			knownLabels.Add(0, rootLabel);
			// Avoid the case that rootLabel equals to one of the other labels
			for (int k_1 = 1; k_1 < knownLabels.Count; ++k_1)
			{
				if (knownLabels[k_1].Equals(rootLabel))
				{
					knownLabels.Remove(k_1);
					break;
				}
			}
			knownWords.Add(0, Config.Unknown);
			knownWords.Add(1, Config.Null);
			knownWords.Add(2, Config.Root);
			knownPos.Add(0, Config.Unknown);
			knownPos.Add(1, Config.Null);
			knownPos.Add(2, Config.Root);
			knownLabels.Add(0, Config.Null);
			GenerateIDs();
			log.Info(Config.Separator);
			log.Info("#Word: " + knownWords.Count);
			log.Info("#POS:" + knownPos.Count);
			log.Info("#Label: " + knownLabels.Count);
		}

		public virtual void WriteModelFile(string modelFile)
		{
			try
			{
				double[][] W1 = classifier.GetW1();
				double[] b1 = classifier.Getb1();
				double[][] W2 = classifier.GetW2();
				double[][] E = classifier.GetE();
				TextWriter output = IOUtils.GetPrintWriter(modelFile);
				output.Write("language=" + language + "\n");
				output.Write("tlp=" + config.tlp.GetType().GetCanonicalName() + "\n");
				output.Write("dict=" + knownWords.Count + "\n");
				output.Write("pos=" + knownPos.Count + "\n");
				output.Write("label=" + knownLabels.Count + "\n");
				output.Write("embeddingSize=" + E[0].Length + "\n");
				output.Write("hiddenSize=" + b1.Length + "\n");
				output.Write("numTokens=" + (W1[0].Length / E[0].Length) + "\n");
				output.Write("preComputed=" + preComputed.Count + "\n");
				int index = 0;
				// First write word / POS / label embeddings
				foreach (string word in knownWords)
				{
					index = WriteEmbedding(E[index], output, index, word);
				}
				foreach (string pos in knownPos)
				{
					index = WriteEmbedding(E[index], output, index, pos);
				}
				foreach (string label in knownLabels)
				{
					index = WriteEmbedding(E[index], output, index, label);
				}
				// Now write classifier weights
				for (int j = 0; j < W1[0].Length; ++j)
				{
					for (int i = 0; i < W1.Length; ++i)
					{
						output.Write(W1[i][j].ToString());
						if (i == W1.Length - 1)
						{
							output.Write("\n");
						}
						else
						{
							output.Write(" ");
						}
					}
				}
				for (int i_1 = 0; i_1 < b1.Length; ++i_1)
				{
					output.Write(b1[i_1].ToString());
					if (i_1 == b1.Length - 1)
					{
						output.Write("\n");
					}
					else
					{
						output.Write(" ");
					}
				}
				for (int j_1 = 0; j_1 < W2[0].Length; ++j_1)
				{
					for (int i_2 = 0; i_2 < W2.Length; ++i_2)
					{
						output.Write(W2[i_2][j_1].ToString());
						if (i_2 == W2.Length - 1)
						{
							output.Write("\n");
						}
						else
						{
							output.Write(" ");
						}
					}
				}
				// Finish with pre-computation info
				for (int i_3 = 0; i_3 < preComputed.Count; ++i_3)
				{
					output.Write(preComputed[i_3].ToString());
					if ((i_3 + 1) % 100 == 0 || i_3 == preComputed.Count - 1)
					{
						output.Write("\n");
					}
					else
					{
						output.Write(" ");
					}
				}
				output.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static int WriteEmbedding(double[] doubles, TextWriter output, int index, string word)
		{
			output.Write(word);
			foreach (double aDouble in doubles)
			{
				output.Write(" " + aDouble);
			}
			output.Write("\n");
			index = index + 1;
			return index;
		}

		/// <summary>
		/// Convenience method; see
		/// <see cref="LoadFromModelFile(string, Java.Util.Properties)"/>
		/// .
		/// </summary>
		/// <seealso cref="LoadFromModelFile(string, Java.Util.Properties)"/>
		public static Edu.Stanford.Nlp.Parser.Nndep.DependencyParser LoadFromModelFile(string modelFile)
		{
			return LoadFromModelFile(modelFile, null);
		}

		/// <summary>Load a saved parser model.</summary>
		/// <param name="modelFile">Path to serialized model (may be GZipped)</param>
		/// <param name="extraProperties">Extra test-time properties not already associated with model (may be null)</param>
		/// <returns>
		/// Loaded and initialized (see
		/// <see cref="Initialize(bool)"/>
		/// model
		/// </returns>
		public static Edu.Stanford.Nlp.Parser.Nndep.DependencyParser LoadFromModelFile(string modelFile, Properties extraProperties)
		{
			Edu.Stanford.Nlp.Parser.Nndep.DependencyParser parser = extraProperties == null ? new Edu.Stanford.Nlp.Parser.Nndep.DependencyParser() : new Edu.Stanford.Nlp.Parser.Nndep.DependencyParser(extraProperties);
			parser.LoadModelFile(modelFile, false);
			return parser;
		}

		/// <summary>Load a parser model file, printing out some messages about the grammar in the file.</summary>
		/// <param name="modelFile">The file (classpath resource, etc.) to load the model from.</param>
		public virtual void LoadModelFile(string modelFile)
		{
			LoadModelFile(modelFile, true);
		}

		/// <summary>helper to check if the model file is new format or not</summary>
		/// <param name="firstLine">the first line of the model file</param>
		/// <returns>true if this is a new format model file</returns>
		private static bool IsModelNewFormat(string firstLine)
		{
			return firstLine.StartsWith("language=");
		}

		private void LoadModelFile(string modelFile, bool verbose)
		{
			Timing t = new Timing();
			try
			{
				using (BufferedReader input = IOUtils.ReaderFromString(modelFile))
				{
					log.Info("Loading depparse model: " + modelFile + " ... ");
					string s;
					// first line in newer saved models is language, legacy models don't store this
					s = input.ReadLine();
					// check if language was stored
					if (IsModelNewFormat(s))
					{
						// set up language
						config.language = Config.GetLanguage(Sharpen.Runtime.Substring(s, 9, s.Length - 1));
						// set up tlp
						s = input.ReadLine();
						string tlpCanonicalName = Sharpen.Runtime.Substring(s, 4, s.Length);
						try
						{
							config.tlp = ReflectionLoading.LoadByReflection(tlpCanonicalName);
							log.Info("Loaded TreebankLanguagePack: " + tlpCanonicalName);
						}
						catch (Exception)
						{
							log.Warn("Error: Failed to load TreebankLanguagePack: " + tlpCanonicalName);
						}
						s = input.ReadLine();
					}
					int nDict = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int nPOS = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int nLabel = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int eSize = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int hSize = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int nTokens = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					s = input.ReadLine();
					int nPreComputed = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
					knownWords = new List<string>();
					knownPos = new List<string>();
					knownLabels = new List<string>();
					double[][] E = new double[][] {  };
					string[] splits;
					int index = 0;
					for (int k = 0; k < nDict; ++k)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						knownWords.Add(splits[0]);
						for (int i = 0; i < eSize; ++i)
						{
							E[index][i] = double.ParseDouble(splits[i + 1]);
						}
						index = index + 1;
					}
					for (int k_1 = 0; k_1 < nPOS; ++k_1)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						knownPos.Add(splits[0]);
						for (int i = 0; i < eSize; ++i)
						{
							E[index][i] = double.ParseDouble(splits[i + 1]);
						}
						index = index + 1;
					}
					for (int k_2 = 0; k_2 < nLabel; ++k_2)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						knownLabels.Add(splits[0]);
						for (int i = 0; i < eSize; ++i)
						{
							E[index][i] = double.ParseDouble(splits[i + 1]);
						}
						index = index + 1;
					}
					GenerateIDs();
					double[][] W1 = new double[hSize][];
					for (int j = 0; j < W1[0].Length; ++j)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						for (int i = 0; i < W1.Length; ++i)
						{
							W1[i][j] = double.ParseDouble(splits[i]);
						}
					}
					double[] b1 = new double[hSize];
					s = input.ReadLine();
					splits = s.Split(" ");
					for (int i_1 = 0; i_1 < b1.Length; ++i_1)
					{
						b1[i_1] = double.ParseDouble(splits[i_1]);
					}
					double[][] W2 = new double[][] {  };
					for (int j_1 = 0; j_1 < W2[0].Length; ++j_1)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						for (int i = 0; i_1 < W2.Length; ++i_1)
						{
							W2[i_1][j_1] = double.ParseDouble(splits[i_1]);
						}
					}
					preComputed = new List<int>();
					while (preComputed.Count < nPreComputed)
					{
						s = input.ReadLine();
						splits = s.Split(" ");
						foreach (string split in splits)
						{
							preComputed.Add(System.Convert.ToInt32(split));
						}
					}
					config.hiddenSize = hSize;
					config.embeddingSize = eSize;
					classifier = new Classifier(config, E, W1, b1, W2, preComputed);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			// initialize the loaded parser
			Initialize(verbose);
			t.Done(log, "Initializing dependency parser");
		}

		// TODO this should be a function which returns the embeddings array + embedID
		// otherwise the class needlessly carries around the extra baggage of `embeddings`
		// (never again used) for the entire training process
		private double[][] ReadEmbedFile(string embedFile, IDictionary<string, int> embedID)
		{
			double[][] embeddings = null;
			if (embedFile != null)
			{
				try
				{
					using (BufferedReader input = IOUtils.ReaderFromString(embedFile))
					{
						IList<string> lines = new List<string>();
						for (string s; (s = input.ReadLine()) != null; )
						{
							lines.Add(s);
						}
						int nWords = lines.Count;
						string[] splits = lines[0].Split("\\s+");
						int dim = splits.Length - 1;
						embeddings = new double[nWords][];
						log.Info("Embedding File " + embedFile + ": #Words = " + nWords + ", dim = " + dim);
						if (dim != config.embeddingSize)
						{
							throw new ArgumentException("The dimension of embedding file does not match config.embeddingSize");
						}
						for (int i = 0; i < lines.Count; ++i)
						{
							splits = lines[i].Split("\\s+");
							embedID[splits[0]] = i;
							for (int j = 0; j < dim; ++j)
							{
								embeddings[i][j] = double.ParseDouble(splits[j + 1]);
							}
						}
					}
				}
				catch (IOException e)
				{
					throw new RuntimeIOException(e);
				}
				embeddings = Edu.Stanford.Nlp.Parser.Nndep.Util.Scaling(embeddings, 0, 1.0);
			}
			return embeddings;
		}

		/// <summary>Train a new dependency parser model.</summary>
		/// <param name="trainFile">Training data</param>
		/// <param name="devFile">
		/// Development data (used for regular UAS evaluation
		/// of model)
		/// </param>
		/// <param name="modelFile">String to which model should be saved</param>
		/// <param name="embedFile">
		/// File containing word embeddings for words used in
		/// training corpus
		/// </param>
		public virtual void Train(string trainFile, string devFile, string modelFile, string embedFile, string preModel)
		{
			log.Info("Train File: " + trainFile);
			log.Info("Dev File: " + devFile);
			log.Info("Model File: " + modelFile);
			log.Info("Embedding File: " + embedFile);
			log.Info("Pre-trained Model File: " + preModel);
			IList<ICoreMap> trainSents = new List<ICoreMap>();
			IList<DependencyTree> trainTrees = new List<DependencyTree>();
			Edu.Stanford.Nlp.Parser.Nndep.Util.LoadConllFile(trainFile, trainSents, trainTrees, config.unlabeled, config.cPOS);
			Edu.Stanford.Nlp.Parser.Nndep.Util.PrintTreeStats("Train", trainTrees);
			IList<ICoreMap> devSents = new List<ICoreMap>();
			IList<DependencyTree> devTrees = new List<DependencyTree>();
			if (devFile != null)
			{
				Edu.Stanford.Nlp.Parser.Nndep.Util.LoadConllFile(devFile, devSents, devTrees, config.unlabeled, config.cPOS);
				Edu.Stanford.Nlp.Parser.Nndep.Util.PrintTreeStats("Dev", devTrees);
			}
			GenDictionaries(trainSents, trainTrees);
			//NOTE: remove -NULL-, and the pass it to ParsingSystem
			IList<string> lDict = new List<string>(knownLabels);
			lDict.Remove(0);
			system = new ArcStandard(config.tlp, lDict, true);
			// Initialize a classifier; prepare for training
			SetupClassifierForTraining(trainSents, trainTrees, embedFile, preModel);
			log.Info(Config.Separator);
			config.PrintParameters();
			long startTime = Runtime.CurrentTimeMillis();
			// Track the best UAS performance we've seen.
			double bestUAS = 0;
			for (int iter = 0; iter < config.maxIter; ++iter)
			{
				log.Info("##### Iteration " + iter);
				Classifier.Cost cost = classifier.ComputeCostFunction(config.batchSize, config.regParameter, config.dropProb);
				log.Info("Cost = " + cost.GetCost() + ", Correct(%) = " + cost.GetPercentCorrect());
				classifier.TakeAdaGradientStep(cost, config.adaAlpha, config.adaEps);
				log.Info("Elapsed Time: " + (Runtime.CurrentTimeMillis() - startTime) / 1000.0 + " (s)");
				// UAS evaluation
				if (devFile != null && iter % config.evalPerIter == 0)
				{
					// Redo precomputation with updated weights. This is only
					// necessary because we're updating weights -- for normal
					// prediction, we just do this once in #initialize
					classifier.PreCompute();
					IList<DependencyTree> predicted = devSents.Stream().Map(null).Collect(Collectors.ToList());
					double uas = config.noPunc ? system.GetUASnoPunc(devSents, predicted, devTrees) : system.GetUAS(devSents, predicted, devTrees);
					log.Info("UAS: " + uas);
					if (config.saveIntermediate && uas > bestUAS)
					{
						log.Info("Exceeds best previous UAS of %f. Saving model file.%n", bestUAS);
						bestUAS = uas;
						WriteModelFile(modelFile);
					}
				}
				// Clear gradients
				if (config.clearGradientsPerIter > 0 && iter % config.clearGradientsPerIter == 0)
				{
					log.Info("Clearing gradient histories..");
					classifier.ClearGradientHistories();
				}
			}
			classifier.FinalizeTraining();
			if (devFile != null)
			{
				// Do final UAS evaluation and save if final model beats the
				// best intermediate one
				IList<DependencyTree> predicted = devSents.Stream().Map(null).Collect(Collectors.ToList());
				double uas = config.noPunc ? system.GetUASnoPunc(devSents, predicted, devTrees) : system.GetUAS(devSents, predicted, devTrees);
				if (uas > bestUAS)
				{
					log.Info(string.Format("Final model UAS: %f%n", uas));
					log.Info(string.Format("Exceeds best previous UAS of %f. Saving model file..%n", bestUAS));
					WriteModelFile(modelFile);
				}
			}
			else
			{
				WriteModelFile(modelFile);
			}
		}

		/// <seealso cref="Train(string, string, string, string, string)"/>
		public virtual void Train(string trainFile, string devFile, string modelFile, string embedFile)
		{
			Train(trainFile, devFile, modelFile, embedFile, null);
		}

		/// <seealso cref="Train(string, string, string, string)"/>
		public virtual void Train(string trainFile, string devFile, string modelFile)
		{
			Train(trainFile, devFile, modelFile, null);
		}

		/// <seealso cref="Train(string, string, string)"/>
		public virtual void Train(string trainFile, string modelFile)
		{
			Train(trainFile, null, modelFile);
		}

		/// <summary>Prepare a classifier for training with the given dataset.</summary>
		private void SetupClassifierForTraining(IList<ICoreMap> trainSents, IList<DependencyTree> trainTrees, string embedFile, string preModel)
		{
			double[][] E = new double[][] {  };
			double[][] W1 = new double[config.hiddenSize][];
			double[] b1 = new double[config.hiddenSize];
			double[][] W2 = new double[][] {  };
			// Randomly initialize weight matrices / vectors
			Random random = Edu.Stanford.Nlp.Parser.Nndep.Util.GetRandom();
			for (int i = 0; i < W1.Length; ++i)
			{
				for (int j = 0; j < W1[i].Length; ++j)
				{
					W1[i][j] = random.NextDouble() * 2 * config.initRange - config.initRange;
				}
			}
			for (int i_1 = 0; i_1 < b1.Length; ++i_1)
			{
				b1[i_1] = random.NextDouble() * 2 * config.initRange - config.initRange;
			}
			for (int i_2 = 0; i_2 < W2.Length; ++i_2)
			{
				for (int j_1 = 0; j_1 < W2[i_2].Length; ++j_1)
				{
					W2[i_2][j_1] = random.NextDouble() * 2 * config.initRange - config.initRange;
				}
			}
			// Read embeddings into `embedID`, `embeddings`
			IDictionary<string, int> embedID = new Dictionary<string, int>();
			double[][] embeddings = ReadEmbedFile(embedFile, embedID);
			// Try to match loaded embeddings with words in dictionary
			int foundEmbed = 0;
			for (int i_3 = 0; i_3 < E.Length; ++i_3)
			{
				int index = -1;
				if (i_3 < knownWords.Count)
				{
					string str = knownWords[i_3];
					//NOTE: exact match first, and then try lower case..
					if (embedID.Contains(str))
					{
						index = embedID[str];
					}
					else
					{
						if (embedID.Contains(str.ToLower()))
						{
							index = embedID[str.ToLower()];
						}
					}
				}
				if (index >= 0)
				{
					++foundEmbed;
					System.Array.Copy(embeddings[index], 0, E[i_3], 0, E[i_3].Length);
				}
				else
				{
					for (int j_2 = 0; j_2 < E[i_3].Length; ++j_2)
					{
						//E[i][j] = random.nextDouble() * config.initRange * 2 - config.initRange;
						//E[i][j] = random.nextDouble() * 0.2 - 0.1;
						//E[i][j] = random.nextGaussian() * Math.sqrt(0.1);
						E[i_3][j_2] = random.NextDouble() * 0.02 - 0.01;
					}
				}
			}
			log.Info("Found embeddings: " + foundEmbed + " / " + knownWords.Count);
			if (preModel != null)
			{
				try
				{
					using (BufferedReader input = IOUtils.ReaderFromString(preModel))
					{
						log.Info("Loading pre-trained model file: " + preModel + " ... ");
						string s;
						s = input.ReadLine();
						int nDict = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						int nPOS = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						int nLabel = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						int eSize = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						int hSize = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						int nTokens = System.Convert.ToInt32(Sharpen.Runtime.Substring(s, s.IndexOf('=') + 1));
						s = input.ReadLine();
						string[] splits;
						for (int k = 0; k < nDict; ++k)
						{
							s = input.ReadLine();
							splits = s.Split(" ");
							if (wordIDs.Contains(splits[0]) && eSize == config.embeddingSize)
							{
								int index = GetWordID(splits[0]);
								for (int i_4 = 0; i_4 < eSize; ++i_4)
								{
									E[index][i_4] = double.ParseDouble(splits[i_4 + 1]);
								}
							}
						}
						for (int k_1 = 0; k_1 < nPOS; ++k_1)
						{
							s = input.ReadLine();
							splits = s.Split(" ");
							if (posIDs.Contains(splits[0]) && eSize == config.embeddingSize)
							{
								int index = GetPosID(splits[0]);
								for (int i_4 = 0; i_4 < eSize; ++i_4)
								{
									E[index][i_4] = double.ParseDouble(splits[i_4 + 1]);
								}
							}
						}
						for (int k_2 = 0; k_2 < nLabel; ++k_2)
						{
							s = input.ReadLine();
							splits = s.Split(" ");
							if (labelIDs.Contains(splits[0]) && eSize == config.embeddingSize)
							{
								int index = GetLabelID(splits[0]);
								for (int i_4 = 0; i_4 < eSize; ++i_4)
								{
									E[index][i_4] = double.ParseDouble(splits[i_4 + 1]);
								}
							}
						}
						bool copyLayer1 = hSize == config.hiddenSize && config.embeddingSize == eSize && config.numTokens == nTokens;
						if (copyLayer1)
						{
							log.Info("Copying parameters W1 && b1...");
						}
						for (int j_2 = 0; j_2 < eSize * nTokens; ++j_2)
						{
							s = input.ReadLine();
							if (copyLayer1)
							{
								splits = s.Split(" ");
								for (int i_4 = 0; i_4 < hSize; ++i_4)
								{
									W1[i_4][j_2] = double.ParseDouble(splits[i_4]);
								}
							}
						}
						s = input.ReadLine();
						if (copyLayer1)
						{
							splits = s.Split(" ");
							for (int i_4 = 0; i_4 < hSize; ++i_4)
							{
								b1[i_4] = double.ParseDouble(splits[i_4]);
							}
						}
						bool copyLayer2 = (nLabel * 2 - 1 == system.NumTransitions()) && hSize == config.hiddenSize;
						if (copyLayer2)
						{
							log.Info("Copying parameters W2...");
						}
						for (int j_3 = 0; j_3 < hSize; ++j_3)
						{
							s = input.ReadLine();
							if (copyLayer2)
							{
								splits = s.Split(" ");
								for (int i_4 = 0; i_4 < nLabel * 2 - 1; ++i_4)
								{
									W2[i_4][j_3] = double.ParseDouble(splits[i_4]);
								}
							}
						}
					}
				}
				catch (IOException e)
				{
					throw new RuntimeIOException(e);
				}
			}
			Dataset trainSet = GenTrainExamples(trainSents, trainTrees);
			classifier = new Classifier(config, trainSet, E, W1, b1, W2, preComputed);
		}

		/// <summary>Determine the dependency parse of the given sentence.</summary>
		/// <remarks>
		/// Determine the dependency parse of the given sentence.
		/// <p>
		/// This "inner" method returns a structure unique to this package; use
		/// <see cref="Predict(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// for general parsing purposes.
		/// </remarks>
		private DependencyTree PredictInner(ICoreMap sentence)
		{
			int numTrans = system.NumTransitions();
			Configuration c = system.InitialConfiguration(sentence);
			while (!system.IsTerminal(c))
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				double[] scores = classifier.ComputeScores(GetFeatureArray(c));
				double optScore = double.NegativeInfinity;
				string optTrans = null;
				for (int j = 0; j < numTrans; ++j)
				{
					if (scores[j] > optScore)
					{
						string tr = system.transitions[j];
						if (system.CanApply(c, tr))
						{
							optScore = scores[j];
							optTrans = tr;
						}
					}
				}
				system.Apply(c, optTrans);
			}
			return c.tree;
		}

		/// <summary>Determine the dependency parse of the given sentence using the loaded model.</summary>
		/// <remarks>
		/// Determine the dependency parse of the given sentence using the loaded model.
		/// You must first load a parser before calling this method.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// If parser has not yet been loaded and initialized
		/// (see
		/// <see cref="Initialize(bool)"/>
		/// </exception>
		public virtual GrammaticalStructure Predict(ICoreMap sentence)
		{
			if (system == null)
			{
				throw new InvalidOperationException("Parser has not been  " + "loaded and initialized; first load a model.");
			}
			DependencyTree result = PredictInner(sentence);
			// The rest of this method is just busy-work to convert the
			// package-local representation into a CoreNLP-standard
			// GrammaticalStructure.
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<TypedDependency> dependencies = new List<TypedDependency>();
			IndexedWord root = new IndexedWord(new Word("ROOT"));
			root.Set(typeof(CoreAnnotations.IndexAnnotation), 0);
			for (int i = 1; i <= result.n; i++)
			{
				int head = result.GetHead(i);
				string label = result.GetLabel(i);
				IndexedWord thisWord = new IndexedWord(tokens[i - 1]);
				IndexedWord headWord = head == 0 ? root : new IndexedWord(tokens[head - 1]);
				GrammaticalRelation relation = head == 0 ? GrammaticalRelation.Root : MakeGrammaticalRelation(label);
				dependencies.Add(new TypedDependency(relation, headWord, thisWord));
			}
			// Build GrammaticalStructure
			// TODO ideally submodule should just return GrammaticalStructure
			TreeGraphNode rootNode = new TreeGraphNode(root);
			return MakeGrammaticalStructure(dependencies, rootNode);
		}

		private GrammaticalRelation MakeGrammaticalRelation(string label)
		{
			GrammaticalRelation stored;
			switch (language)
			{
				case Language.English:
				{
					stored = EnglishGrammaticalRelations.shortNameToGRel[label];
					if (stored != null)
					{
						return stored;
					}
					break;
				}

				case Language.UniversalEnglish:
				{
					stored = UniversalEnglishGrammaticalRelations.shortNameToGRel[label];
					if (stored != null)
					{
						return stored;
					}
					break;
				}

				case Language.Chinese:
				{
					stored = ChineseGrammaticalRelations.shortNameToGRel[label];
					if (stored != null)
					{
						return stored;
					}
					break;
				}
			}
			return new GrammaticalRelation(language, label, null, GrammaticalRelation.Dependent);
		}

		private GrammaticalStructure MakeGrammaticalStructure(IList<TypedDependency> dependencies, TreeGraphNode rootNode)
		{
			switch (language)
			{
				case Language.English:
				{
					return new EnglishGrammaticalStructure(dependencies, rootNode);
				}

				case Language.UniversalEnglish:
				{
					return new UniversalEnglishGrammaticalStructure(dependencies, rootNode);
				}

				case Language.Chinese:
				{
					return new ChineseGrammaticalStructure(dependencies, rootNode);
				}

				default:
				{
					// TODO suboptimal: default to UniversalEnglishGrammaticalStructure return
					return new UniversalEnglishGrammaticalStructure(dependencies, rootNode);
				}
			}
		}

		/// <summary>
		/// Convenience method for
		/// <see cref="Predict(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// . The tokens of the provided sentence must
		/// also have tag annotations (the parser requires part-of-speech tags).
		/// </summary>
		/// <seealso cref="Predict(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		public virtual GrammaticalStructure Predict<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			CoreLabel sentenceLabel = new CoreLabel();
			IList<CoreLabel> tokens = new List<CoreLabel>();
			int i = 1;
			foreach (IHasWord wd in sentence)
			{
				CoreLabel label;
				if (wd is CoreLabel)
				{
					label = (CoreLabel)wd;
					if (label.Tag() == null)
					{
						throw new ArgumentException("Parser requires words " + "with part-of-speech tag annotations");
					}
				}
				else
				{
					label = new CoreLabel();
					label.SetValue(wd.Word());
					label.SetWord(wd.Word());
					if (!(wd is IHasTag))
					{
						throw new ArgumentException("Parser requires words " + "with part-of-speech tag annotations");
					}
					label.SetTag(((IHasTag)wd).Tag());
				}
				label.SetIndex(i);
				i++;
				tokens.Add(label);
			}
			sentenceLabel.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			return Predict(sentenceLabel);
		}

		//TODO: support sentence-only files as input
		/// <summary>Run the parser in the modelFile on a testFile and perhaps save output.</summary>
		/// <param name="testFile">File to parse. In CoNLL-X format. Assumed to have gold answers included.</param>
		/// <param name="outFile">File to write results to in CoNLL-X format.  If null, no output is written</param>
		/// <returns>The LAS score on the dataset</returns>
		public virtual double TestCoNLL(string testFile, string outFile)
		{
			log.Info("Test File: " + testFile);
			Timing timer = new Timing();
			IList<ICoreMap> testSents = new List<ICoreMap>();
			IList<DependencyTree> testTrees = new List<DependencyTree>();
			Edu.Stanford.Nlp.Parser.Nndep.Util.LoadConllFile(testFile, testSents, testTrees, config.unlabeled, config.cPOS);
			// count how much to parse
			int numWords = 0;
			int numOOVWords = 0;
			int numSentences = 0;
			foreach (ICoreMap testSent in testSents)
			{
				numSentences += 1;
				IList<CoreLabel> tokens = testSent.Get(typeof(CoreAnnotations.TokensAnnotation));
				foreach (CoreLabel token in tokens)
				{
					string word = token.Word();
					numWords += 1;
					if (!wordIDs.Contains(word))
					{
						numOOVWords += 1;
					}
				}
			}
			log.Info(string.Format("OOV Words: %d / %d = %.2f%%\n", numOOVWords, numWords, numOOVWords * 100.0 / numWords));
			IList<DependencyTree> predicted = testSents.Stream().Map(null).Collect(Collectors.ToList());
			IDictionary<string, double> result = system.Evaluate(testSents, predicted, testTrees);
			double uas = config.noPunc ? result["UASnoPunc"] : result["UAS"];
			double las = config.noPunc ? result["LASnoPunc"] : result["LAS"];
			log.Info(string.Format("UAS = %.4f%n", uas));
			log.Info(string.Format("LAS = %.4f%n", las));
			long millis = timer.Stop();
			double wordspersec = numWords / (((double)millis) / 1000);
			double sentspersec = numSentences / (((double)millis) / 1000);
			log.Info(string.Format("%s parsed %d words in %d sentences in %.1fs at %.1f w/s, %.1f sent/s.%n", StringUtils.GetShortClassName(this), numWords, numSentences, millis / 1000.0, wordspersec, sentspersec));
			if (outFile != null)
			{
				Edu.Stanford.Nlp.Parser.Nndep.Util.WriteConllFile(outFile, testSents, predicted);
			}
			return las;
		}

		private void ParseTextFile(BufferedReader input, PrintWriter output)
		{
			DocumentPreprocessor preprocessor = new DocumentPreprocessor(input);
			preprocessor.SetSentenceFinalPuncWords(config.tlp.SentenceFinalPunctuationWords());
			preprocessor.SetEscaper(config.escaper);
			preprocessor.SetSentenceDelimiter(config.sentenceDelimiter);
			preprocessor.SetTokenizerFactory(config.tlp.GetTokenizerFactory());
			Timing timer = new Timing();
			MaxentTagger tagger = new MaxentTagger(config.tagger);
			IList<IList<TaggedWord>> tagged = new List<IList<TaggedWord>>();
			foreach (IList<IHasWord> sentence in preprocessor)
			{
				tagged.Add(tagger.TagSentence(sentence));
			}
			log.Info(string.Format("Tagging completed in %.2f sec.%n", timer.Stop() / 1000.0));
			timer.Start();
			int numSentences = 0;
			foreach (IList<TaggedWord> taggedSentence in tagged)
			{
				GrammaticalStructure parse = Predict(taggedSentence);
				ICollection<TypedDependency> deps = parse.TypedDependencies();
				foreach (TypedDependency dep in deps)
				{
					output.Println(dep);
				}
				output.Println();
				numSentences++;
			}
			long millis = timer.Stop();
			double seconds = millis / 1000.0;
			log.Info(string.Format("Parsed %d sentences in %.2f seconds (%.2f sents/sec).%n", numSentences, seconds, numSentences / seconds));
		}

		/// <summary>Prepare for parsing after a model has been loaded.</summary>
		private void Initialize(bool verbose)
		{
			if (knownLabels == null)
			{
				throw new InvalidOperationException("Model has not been loaded or trained");
			}
			// NOTE: remove -NULL-, and then pass the label set to the ParsingSystem
			IList<string> lDict = new List<string>(knownLabels);
			lDict.Remove(0);
			system = new ArcStandard(config.tlp, lDict, verbose);
			// Pre-compute matrix multiplications
			if (config.numPreComputed > 0)
			{
				classifier.PreCompute();
			}
		}

		/// <summary>
		/// Explicitly specifies the number of arguments expected with
		/// particular command line options.
		/// </summary>
		private static readonly IDictionary<string, int> numArgs = new Dictionary<string, int>();

		static DependencyParser()
		{
			numArgs["textFile"] = 1;
			numArgs["outFile"] = 1;
		}

		/// <summary>A main program for training, testing and using the parser.</summary>
		/// <remarks>
		/// A main program for training, testing and using the parser.
		/// <p>
		/// You can use this program to train new parsers from treebank data,
		/// evaluate on test treebank data, or parse raw text input.
		/// <p>
		/// Sample usages:
		/// <ul>
		/// <li>
		/// <strong>Train a parser with CoNLL treebank data:</strong>
		/// <c>java edu.stanford.nlp.parser.nndep.DependencyParser -trainFile trainPath -devFile devPath -embedFile wordEmbeddingFile -embeddingSize wordEmbeddingDimensionality -model modelOutputFile.txt.gz</c>
		/// </li>
		/// <li>
		/// <strong>Parse raw text from a file:</strong>
		/// <c>java edu.stanford.nlp.parser.nndep.DependencyParser -model modelOutputFile.txt.gz -textFile rawTextToParse -outFile dependenciesOutputFile.txt</c>
		/// </li>
		/// <li>
		/// <strong>Parse raw text from standard input, writing to standard output:</strong>
		/// <c>java edu.stanford.nlp.parser.nndep.DependencyParser -model modelOutputFile.txt.gz -textFile - -outFile -</c>
		/// </li>
		/// </ul>
		/// <p>
		/// See below for more information on all of these training / test options and more.
		/// <p>
		/// Input / output options:
		/// <table>
		/// <tr><th>Option</th><th>Required for training</th><th>Required for testing / parsing</th><th>Description</th></tr>
		/// <tr><td><tt>-devFile</tt></td><td>Optional</td><td>No</td><td>Path to a development-set treebank in <a href="http://ilk.uvt.nl/conll/#dataformat">CoNLL-X format</a>. If provided, the dev set performance is monitored during training.</td></tr>
		/// <tr><td><tt>-embedFile</tt></td><td>Optional (highly recommended!)</td><td>No</td><td>A word embedding file, containing distributed representations of English words. Each line of the provided file should contain a single word followed by the elements of the corresponding word embedding (space-delimited). It is not absolutely necessary that all words in the treebank be covered by this embedding file, though the parser's performance will generally improve if you are able to provide better embeddings for more words.</td></tr>
		/// <tr><td><tt>-model</tt></td><td>Yes</td><td>Yes</td><td>Path to a model file. If the path ends in <tt>.gz</tt>, the model will be read as a Gzipped model file. During training, we write to this path; at test time we read a pre-trained model from this path.</td></tr>
		/// <tr><td><tt>-textFile</tt></td><td>No</td><td>Yes (or <tt>testFile</tt>)</td><td>Path to a plaintext file containing sentences to be parsed.</td></tr>
		/// <tr><td><tt>-testFile</tt></td><td>No</td><td>Yes (or <tt>textFile</tt>)</td><td>Path to a test-set treebank in <a href="http://ilk.uvt.nl/conll/#dataformat">CoNLL-X format</a> for final evaluation of the parser.</td></tr>
		/// <tr><td><tt>-trainFile</tt></td><td>Yes</td><td>No</td><td>Path to a training treebank in <a href="http://ilk.uvt.nl/conll/#dataformat">CoNLL-X format.</a></td></tr>
		/// </table>
		/// Training options:
		/// <table>
		/// <tr><th>Option</th><th>Default</th><th>Description</th></tr>
		/// <tr><td><tt>-adaAlpha</tt></td><td>0.01</td><td>Global learning rate for AdaGrad training</td></tr>
		/// <tr><td><tt>-adaEps</tt></td><td>1e-6</td><td>Epsilon value added to the denominator of AdaGrad update expression for numerical stability</td></tr>
		/// <tr><td><tt>-batchSize</tt></td><td>10000</td><td>Size of mini-batch used for training</td></tr>
		/// <tr><td><tt>-clearGradientsPerIter</tt></td><td>0</td><td>Clear AdaGrad gradient histories every <em>n</em> iterations. If zero, no gradient clearing is performed.</td></tr>
		/// <tr><td><tt>-dropProb</tt></td><td>0.5</td><td>Dropout probability. For each training example we randomly choose some amount of units to disable in the neural network classifier. This parameter controls the proportion of units "dropped out."</td></tr>
		/// <tr><td><tt>-embeddingSize</tt></td><td>50</td><td>Dimensionality of word embeddings provided</td></tr>
		/// <tr><td><tt>-evalPerIter</tt></td><td>100</td><td>Run full UAS (unlabeled attachment score) evaluation every time we finish this number of iterations. (Only valid if a development treebank is provided with <tt>-devFile</tt>.)</td></tr>
		/// <tr><td><tt>-hiddenSize</tt></td><td>200</td><td>Dimensionality of hidden layer in neural network classifier</td></tr>
		/// <tr><td><tt>-initRange</tt></td><td>0.01</td><td>Bounds of range within which weight matrix elements should be initialized. Each element is drawn from a uniform distribution over the range <tt>[-initRange, initRange]</tt>.</td></tr>
		/// <tr><td><tt>-maxIter</tt></td><td>20000</td><td>Number of training iterations to complete before stopping and saving the final model.</td></tr>
		/// <tr><td><tt>-numPreComputed</tt></td><td>100000</td><td>The parser pre-computes hidden-layer unit activations for particular inputs words at both training and testing time in order to speed up feedforward computation in the neural network. This parameter determines how many words for which we should compute hidden-layer activations.</td></tr>
		/// <tr><td><tt>-regParameter</tt></td><td>1e-8</td><td>Regularization parameter for training</td></tr>
		/// <tr><td><tt>-saveIntermediate</tt></td><td><tt>true</tt></td><td>If <tt>true</tt>, continually save the model version which gets the highest UAS value on the dev set. (Only valid if a development treebank is provided with <tt>-devFile</tt>.)</td></tr>
		/// <tr><td><tt>-trainingThreads</tt></td><td>1</td><td>Number of threads to use during training. Note that depending on training batch size, it may be unwise to simply choose the maximum amount of threads for your machine. On our 16-core test machines: a batch size of 10,000 runs fastest with around 6 threads; a batch size of 100,000 runs best with around 10 threads.</td></tr>
		/// <tr><td><tt>-wordCutOff</tt></td><td>1</td><td>The parser can optionally ignore rare words by simply choosing an arbitrary "unknown" feature representation for words that appear with frequency less than <em>n</em> in the corpus. This <em>n</em> is controlled by the <tt>wordCutOff</tt> parameter.</td></tr>
		/// </table>
		/// Runtime parsing options:
		/// <table>
		/// <tr><th>Option</th><th>Default</th><th>Description</th></tr>
		/// <tr><td><tt>-escaper</tt></td><td>N/A</td><td>Only applicable for testing with <tt>-textFile</tt>. If provided, use this word-escaper when parsing raw sentences. Should be a fully-qualified class name like <tt>edu.stanford.nlp.trees.international.arabic.ATBEscaper</tt>.</td></tr>
		/// <tr><td><tt>-numPreComputed</tt></td><td>100000</td><td>The parser pre-computes hidden-layer unit activations for particular inputs words at both training and testing time in order to speed up feedforward computation in the neural network. This parameter determines how many words for which we should compute hidden-layer activations.</td></tr>
		/// <tr><td><tt>-sentenceDelimiter</tt></td><td>N/A</td><td>Only applicable for testing with <tt>-textFile</tt>.  If provided, assume that the given <tt>textFile</tt> has already been sentence-split, and that sentences are separated by this delimiter.</td></tr>
		/// <tr><td><tt>-tagger.model</tt></td><td>edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger</td><td>Only applicable for testing with <tt>-textFile</tt>. Path to a part-of-speech tagger to use to pre-tag the raw sentences before parsing.</td></tr>
		/// </table>
		/// </remarks>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args, numArgs);
			Edu.Stanford.Nlp.Parser.Nndep.DependencyParser parser = new Edu.Stanford.Nlp.Parser.Nndep.DependencyParser(props);
			// Train with CoNLL-X data
			if (props.Contains("trainFile"))
			{
				parser.Train(props.GetProperty("trainFile"), props.GetProperty("devFile"), props.GetProperty("model"), props.GetProperty("embedFile"), props.GetProperty("preModel"));
			}
			bool loaded = false;
			// Test with CoNLL-X data
			if (props.Contains("testFile"))
			{
				parser.LoadModelFile(props.GetProperty("model"));
				loaded = true;
				parser.TestCoNLL(props.GetProperty("testFile"), props.GetProperty("outFile"));
			}
			// Parse raw text data
			if (props.Contains("textFile"))
			{
				if (!loaded)
				{
					parser.LoadModelFile(props.GetProperty("model"));
					loaded = true;
				}
				string encoding = parser.config.tlp.GetEncoding();
				string inputFilename = props.GetProperty("textFile");
				BufferedReader input;
				try
				{
					input = inputFilename.Equals("-") ? IOUtils.ReaderFromStdin(encoding) : IOUtils.ReaderFromString(inputFilename, encoding);
				}
				catch (IOException e)
				{
					throw new RuntimeIOException("No input file provided (use -textFile)", e);
				}
				string outputFilename = props.GetProperty("outFile");
				PrintWriter output;
				try
				{
					output = outputFilename == null || outputFilename.Equals("-") ? IOUtils.EncodedOutputStreamPrintWriter(System.Console.Out, encoding, true) : IOUtils.GetPrintWriter(outputFilename, encoding);
				}
				catch (IOException e)
				{
					throw new RuntimeIOException("Error opening output file", e);
				}
				parser.ParseTextFile(input, output);
			}
		}
	}
}
