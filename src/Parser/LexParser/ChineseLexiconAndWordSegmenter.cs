using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>This class lets you train a lexicon and segmenter at the same time.</summary>
	/// <author>Galen Andrew</author>
	/// <author>Pi-Chuan Chang</author>
	[System.Serializable]
	public class ChineseLexiconAndWordSegmenter : ILexicon, IWordSegmenter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter));

		private readonly ChineseLexicon chineseLexicon;

		private readonly IWordSegmenter wordSegmenter;

		public ChineseLexiconAndWordSegmenter(ChineseLexicon lex, IWordSegmenter seg)
		{
			chineseLexicon = lex;
			wordSegmenter = seg;
		}

		public virtual IList<IHasWord> Segment(string s)
		{
			return wordSegmenter.Segment(s);
		}

		public virtual bool IsKnown(int word)
		{
			return chineseLexicon.IsKnown(word);
		}

		public virtual bool IsKnown(string word)
		{
			return chineseLexicon.IsKnown(word);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<string> TagSet(Func<string, string> basicCategoryFunction)
		{
			return chineseLexicon.TagSet(basicCategoryFunction);
		}

		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec)
		{
			return chineseLexicon.RuleIteratorByWord(word, loc, null);
		}

		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc, string featureSpec)
		{
			return chineseLexicon.RuleIteratorByWord(word, loc, null);
		}

		/// <summary>Returns the number of rules (tag rewrites as word) in the Lexicon.</summary>
		/// <remarks>
		/// Returns the number of rules (tag rewrites as word) in the Lexicon.
		/// This method assumes that the lexicon has been initialized.
		/// </remarks>
		public virtual int NumRules()
		{
			return chineseLexicon.NumRules();
		}

		public virtual void InitializeTraining(double numTrees)
		{
			chineseLexicon.InitializeTraining(numTrees);
			wordSegmenter.InitializeTraining(numTrees);
		}

		public virtual void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		public virtual void Train(Tree tree)
		{
			Train(tree, 1.0);
		}

		public virtual void Train(Tree tree, double weight)
		{
			Train(tree.TaggedYield(), weight);
		}

		public virtual void Train(IList<TaggedWord> sentence)
		{
			Train(sentence, 1.0);
		}

		public virtual void Train(IList<TaggedWord> sentence, double weight)
		{
			chineseLexicon.Train(sentence, weight);
			wordSegmenter.Train(sentence);
		}

		public virtual void TrainUnannotated(IList<TaggedWord> sentence, double weight)
		{
			// TODO: for now we just punt on these
			throw new NotSupportedException("This version of the parser does not support non-tree training data");
		}

		public virtual void IncrementTreesRead(double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void Train(TaggedWord tw, int loc, double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void FinishTraining()
		{
			chineseLexicon.FinishTraining();
			wordSegmenter.FinishTraining();
		}

		public virtual float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			return chineseLexicon.Score(iTW, loc, word, null);
		}

		// end score()
		public virtual void LoadSegmenter(string filename)
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadData(BufferedReader @in)
		{
			chineseLexicon.ReadData(@in);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteData(TextWriter w)
		{
			chineseLexicon.WriteData(w);
		}

		private Options op;

		// the data & functions below are for standalone segmenter. -pichuan
		// helper function
		private static int NumSubArgs(string[] args, int index)
		{
			int i = index;
			while (i + 1 < args.Length && args[i + 1][0] != '-')
			{
				i++;
			}
			return i - index;
		}

		private ChineseLexiconAndWordSegmenter(Treebank trainTreebank, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs = GetSegmenterDataFromTreebank(trainTreebank, op, wordIndex, tagIndex);
			chineseLexicon = cs.chineseLexicon;
			wordSegmenter = cs.wordSegmenter;
		}

		private static Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter GetSegmenterDataFromTreebank(Treebank trainTreebank, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			System.Console.Out.WriteLine("Currently " + new DateTime());
			//    printOptions(true, op);
			Timing.StartTime();
			// setup tree transforms
			ITreebankLangParserParams tlpParams = op.tlpParams;
			if (op.testOptions.verbose)
			{
				System.Console.Out.Write("Training ");
				System.Console.Out.WriteLine(trainTreebank.TextualSummary());
			}
			System.Console.Out.Write("Binarizing trees...");
			TreeAnnotatorAndBinarizer binarizer;
			// initialized below
			if (!op.trainOptions.leftToRight)
			{
				binarizer = new TreeAnnotatorAndBinarizer(tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			}
			else
			{
				binarizer = new TreeAnnotatorAndBinarizer(tlpParams.HeadFinder(), new LeftHeadFinder(), tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			}
			CollinsPuncTransformer collinsPuncTransformer = null;
			if (op.trainOptions.collinsPunc)
			{
				collinsPuncTransformer = new CollinsPuncTransformer(tlpParams.TreebankLanguagePack());
			}
			IList<Tree> binaryTrainTrees = new List<Tree>();
			// List<Tree> binaryTuneTrees = new ArrayList<Tree>();
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(trainTreebank, true, 0, op.trainOptions.selectiveSplitCutOff, op.trainOptions.tagSelectiveSplitCutOff, tlpParams.TreebankLanguagePack());
				if (op.testOptions.verbose)
				{
					log.Info("Parent split categories: " + op.trainOptions.splitters);
				}
			}
			if (op.trainOptions.selectivePostSplit)
			{
				ITreeTransformer myTransformer = new TreeAnnotator(tlpParams.HeadFinder(), tlpParams, op);
				Treebank annotatedTB = trainTreebank.Transform(myTransformer);
				op.trainOptions.postSplitters = ParentAnnotationStats.GetSplitCategories(annotatedTB, true, 0, op.trainOptions.selectivePostSplitCutOff, op.trainOptions.tagSelectivePostSplitCutOff, tlpParams.TreebankLanguagePack());
				if (op.testOptions.verbose)
				{
					log.Info("Parent post annotation split categories: " + op.trainOptions.postSplitters);
				}
			}
			if (op.trainOptions.hSelSplit)
			{
				binarizer.SetDoSelectiveSplit(false);
				foreach (Tree tree in trainTreebank)
				{
					if (op.trainOptions.collinsPunc)
					{
						tree = collinsPuncTransformer.TransformTree(tree);
					}
					tree = binarizer.TransformTree(tree);
				}
				binarizer.SetDoSelectiveSplit(true);
			}
			foreach (Tree tree_1 in trainTreebank)
			{
				if (op.trainOptions.collinsPunc)
				{
					tree_1 = collinsPuncTransformer.TransformTree(tree_1);
				}
				tree_1 = binarizer.TransformTree(tree_1);
				binaryTrainTrees.Add(tree_1);
			}
			Timing.Tick("done.");
			if (op.testOptions.verbose)
			{
				binarizer.DumpStats();
			}
			System.Console.Out.Write("Extracting Lexicon...");
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter clex = (Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter)op.tlpParams.Lex(op, wordIndex, tagIndex);
			clex.InitializeTraining(binaryTrainTrees.Count);
			clex.Train(binaryTrainTrees);
			clex.FinishTraining();
			Timing.Tick("done.");
			return clex;
		}

		private static void PrintArgs(string[] args, TextWriter ps)
		{
			ps.Write("ChineseLexiconAndWordSegmenter invoked with arguments:");
			foreach (string arg in args)
			{
				ps.Write(" " + arg);
			}
			ps.WriteLine();
		}

		internal static void SaveSegmenterDataToSerialized(Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs, string filename)
		{
			try
			{
				log.Info("Writing segmenter in serialized format to file " + filename + " ");
				ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
				@out.WriteObject(cs);
				@out.Close();
				log.Info("done.");
			}
			catch (IOException ioe)
			{
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
		}

		internal static void SaveSegmenterDataToText(Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs, string filename)
		{
			try
			{
				log.Info("Writing parser in text grammar format to file " + filename);
				OutputStream os;
				if (filename.EndsWith(".gz"))
				{
					// it's faster to do the buffering _outside_ the gzipping as here
					os = new BufferedOutputStream(new GZIPOutputStream(new FileOutputStream(filename)));
				}
				else
				{
					os = new BufferedOutputStream(new FileOutputStream(filename));
				}
				PrintWriter @out = new PrintWriter(os);
				string prefix = "BEGIN ";
				//      out.println(prefix + "OPTIONS");
				//      if (pd.pt != null) {
				//        pd.pt.writeData(out);
				//      }
				//      out.println();
				//      log.info(".");
				@out.Println(prefix + "LEXICON");
				if (cs != null)
				{
					cs.WriteData(@out);
				}
				@out.Println();
				log.Info(".");
				@out.Flush();
				@out.Close();
				log.Info("done.");
			}
			catch (IOException e)
			{
				log.Info("Trouble saving segmenter data to ASCII format.");
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		private static Treebank MakeTreebank(string treebankPath, Options op, IFileFilter filt)
		{
			log.Info("Training a segmenter from treebank dir: " + treebankPath);
			Treebank trainTreebank = op.tlpParams.MemoryTreebank();
			log.Info("Reading trees...");
			if (filt == null)
			{
				trainTreebank.LoadPath(treebankPath);
			}
			else
			{
				trainTreebank.LoadPath(treebankPath, filt);
			}
			Timing.Tick("done [read " + trainTreebank.Count + " trees].");
			return trainTreebank;
		}

		/// <summary>Construct a new ChineseLexiconAndWordSegmenter.</summary>
		/// <remarks>
		/// Construct a new ChineseLexiconAndWordSegmenter.  This loads a segmenter file that
		/// was previously assembled and stored.
		/// </remarks>
		/// <exception cref="System.ArgumentException">If segmenter data cannot be loaded</exception>
		public ChineseLexiconAndWordSegmenter(string segmenterFileOrUrl, Options op)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs = GetSegmenterDataFromFile(segmenterFileOrUrl, op);
			this.op = cs.op;
			// in case a serialized options was read in
			chineseLexicon = cs.chineseLexicon;
			wordSegmenter = cs.wordSegmenter;
		}

		public static Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter GetSegmenterDataFromFile(string parserFileOrUrl, Options op)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs = GetSegmenterDataFromSerializedFile(parserFileOrUrl);
			if (cs == null)
			{
			}
			//      pd = getSegmenterDataFromTextFile(parserFileOrUrl, op);
			return cs;
		}

		protected internal static Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter GetSegmenterDataFromSerializedFile(string serializedFileOrUrl)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs = null;
			try
			{
				log.Info("Loading segmenter from serialized file " + serializedFileOrUrl + " ...");
				ObjectInputStream @in;
				InputStream @is;
				if (serializedFileOrUrl.StartsWith("http://"))
				{
					URL u = new URL(serializedFileOrUrl);
					URLConnection uc = u.OpenConnection();
					@is = uc.GetInputStream();
				}
				else
				{
					@is = new FileInputStream(serializedFileOrUrl);
				}
				if (serializedFileOrUrl.EndsWith(".gz"))
				{
					// it's faster to do the buffering _outside_ the gzipping as here
					@in = new ObjectInputStream(new BufferedInputStream(new GZIPInputStream(@is)));
				}
				else
				{
					@in = new ObjectInputStream(new BufferedInputStream(@is));
				}
				cs = (Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter)@in.ReadObject();
				@in.Close();
				log.Info(" done.");
				return cs;
			}
			catch (InvalidClassException ice)
			{
				// For this, it's not a good idea to continue and try it as a text file!
				log.Info();
				// as in middle of line from above message
				throw new Exception(ice);
			}
			catch (FileNotFoundException fnfe)
			{
				// For this, it's not a good idea to continue and try it as a text file!
				log.Info();
				// as in middle of line from above message
				throw new Exception(fnfe);
			}
			catch (StreamCorruptedException)
			{
			}
			catch (Exception e)
			{
				// suppress error message, on the assumption that we've really got
				// a text grammar, and that'll be tried next
				log.Info();
				// as in middle of line from above message
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}

		/// <summary>
		/// This method lets you train and test a segmenter relative to a
		/// Treebank.
		/// </summary>
		/// <remarks>
		/// This method lets you train and test a segmenter relative to a
		/// Treebank.
		/// <p>
		/// <i>Implementation note:</i> This method is largely cloned from
		/// LexicalizedParser's main method.  Should we try to have it be able
		/// to train segmenters to stop things going out of sync?
		/// </remarks>
		public static void Main(string[] args)
		{
			bool train = false;
			bool saveToSerializedFile = false;
			bool saveToTextFile = false;
			string serializedInputFileOrUrl = null;
			string textInputFileOrUrl = null;
			string serializedOutputFileOrUrl = null;
			string textOutputFileOrUrl = null;
			string treebankPath = null;
			Treebank testTreebank = null;
			// Treebank tuneTreebank = null;
			string testPath = null;
			IFileFilter testFilter = null;
			IFileFilter trainFilter = null;
			string encoding = null;
			// variables needed to process the files to be parsed
			ITokenizerFactory<Word> tokenizerFactory = null;
			//    DocumentPreprocessor documentPreprocessor = new DocumentPreprocessor();
			bool tokenized = false;
			// whether or not the input file has already been tokenized
			Func<IList<IHasWord>, IList<IHasWord>> escaper = new ChineseEscaper();
			// int tagDelimiter = -1;
			// String sentenceDelimiter = "\n";
			// boolean fromXML = false;
			int argIndex = 0;
			if (args.Length < 1)
			{
				log.Info("usage: java edu.stanford.nlp.parser.lexparser." + "LexicalizedParser parserFileOrUrl filename*");
				return;
			}
			Options op = new Options();
			op.tlpParams = new ChineseTreebankParserParams();
			// while loop through option arguments
			while (argIndex < args.Length && args[argIndex][0] == '-')
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train"))
				{
					train = true;
					saveToSerializedFile = true;
					int numSubArgs = NumSubArgs(args, argIndex);
					argIndex++;
					if (numSubArgs > 1)
					{
						treebankPath = args[argIndex];
						argIndex++;
					}
					else
					{
						throw new Exception("Error: -train option must have treebankPath as first argument.");
					}
					if (numSubArgs == 2)
					{
						trainFilter = new NumberRangesFileFilter(args[argIndex++], true);
					}
					else
					{
						if (numSubArgs >= 3)
						{
							try
							{
								int low = System.Convert.ToInt32(args[argIndex]);
								int high = System.Convert.ToInt32(args[argIndex + 1]);
								trainFilter = new NumberRangeFileFilter(low, high, true);
								argIndex += 2;
							}
							catch (NumberFormatException)
							{
								// maybe it's a ranges expression?
								trainFilter = new NumberRangesFileFilter(args[argIndex], true);
								argIndex++;
							}
						}
					}
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-encoding"))
					{
						// sets encoding for TreebankLangParserParams
						encoding = args[argIndex + 1];
						op.tlpParams.SetInputEncoding(encoding);
						op.tlpParams.SetOutputEncoding(encoding);
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-loadFromSerializedFile"))
						{
							// load the parser from a binary serialized file
							// the next argument must be the path to the parser file
							serializedInputFileOrUrl = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							// doesn't make sense to load from TextFile -pichuan
							//      } else if (args[argIndex].equalsIgnoreCase("-loadFromTextFile")) {
							//        // load the parser from declarative text file
							//        // the next argument must be the path to the parser file
							//        textInputFileOrUrl = args[argIndex + 1];
							//        argIndex += 2;
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveToSerializedFile"))
							{
								saveToSerializedFile = true;
								serializedOutputFileOrUrl = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveToTextFile"))
								{
									// save the parser to declarative text file
									saveToTextFile = true;
									textOutputFileOrUrl = args[argIndex + 1];
									argIndex += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank"))
									{
										// the next argument is the treebank path and range for testing
										int numSubArgs = NumSubArgs(args, argIndex);
										argIndex++;
										if (numSubArgs == 1)
										{
											testFilter = new NumberRangesFileFilter(args[argIndex++], true);
										}
										else
										{
											if (numSubArgs > 1)
											{
												testPath = args[argIndex++];
												if (numSubArgs == 2)
												{
													testFilter = new NumberRangesFileFilter(args[argIndex++], true);
												}
												else
												{
													if (numSubArgs >= 3)
													{
														try
														{
															int low = System.Convert.ToInt32(args[argIndex]);
															int high = System.Convert.ToInt32(args[argIndex + 1]);
															testFilter = new NumberRangeFileFilter(low, high, true);
															argIndex += 2;
														}
														catch (NumberFormatException)
														{
															// maybe it's a ranges expression?
															testFilter = new NumberRangesFileFilter(args[argIndex++], true);
														}
													}
												}
											}
										}
									}
									else
									{
										int j = op.tlpParams.SetOptionFlag(args, argIndex);
										if (j == argIndex)
										{
											log.Info("Unknown option ignored: " + args[argIndex]);
											j++;
										}
										argIndex = j;
									}
								}
							}
						}
					}
				}
			}
			// end while loop through arguments
			ITreebankLangParserParams tlpParams = op.tlpParams;
			// all other arguments are order dependent and
			// are processed in order below
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter cs = null;
			if (!train && op.testOptions.verbose)
			{
				System.Console.Out.WriteLine("Currently " + new DateTime());
				PrintArgs(args, System.Console.Out);
			}
			if (train)
			{
				PrintArgs(args, System.Console.Out);
				// so we train a parser using the treebank
				if (treebankPath == null)
				{
					// the next arg must be the treebank path, since it wasn't give earlier
					treebankPath = args[argIndex];
					argIndex++;
					if (args.Length > argIndex + 1)
					{
						try
						{
							// the next two args might be the range
							int low = System.Convert.ToInt32(args[argIndex]);
							int high = System.Convert.ToInt32(args[argIndex + 1]);
							trainFilter = new NumberRangeFileFilter(low, high, true);
							argIndex += 2;
						}
						catch (NumberFormatException)
						{
							// maybe it's a ranges expression?
							trainFilter = new NumberRangesFileFilter(args[argIndex], true);
							argIndex++;
						}
					}
				}
				Treebank trainTreebank = MakeTreebank(treebankPath, op, trainFilter);
				IIndex<string> wordIndex = new HashIndex<string>();
				IIndex<string> tagIndex = new HashIndex<string>();
				cs = new Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter(trainTreebank, op, wordIndex, tagIndex);
			}
			else
			{
				if (textInputFileOrUrl != null)
				{
				}
				else
				{
					// so we load the segmenter from a text grammar file
					// XXXXX fix later -pichuan
					//cs = new LexicalizedParser(textInputFileOrUrl, true, op);
					// so we load a serialized segmenter
					if (serializedInputFileOrUrl == null)
					{
						// the next argument must be the path to the serialized parser
						serializedInputFileOrUrl = args[argIndex];
						argIndex++;
					}
					try
					{
						cs = new Edu.Stanford.Nlp.Parser.Lexparser.ChineseLexiconAndWordSegmenter(serializedInputFileOrUrl, op);
					}
					catch (ArgumentException)
					{
						log.Info("Error loading segmenter, exiting...");
						System.Environment.Exit(0);
					}
				}
			}
			// the following has to go after reading parser to make sure
			// op and tlpParams are the same for train and test
			TreePrint treePrint = op.testOptions.TreePrint(tlpParams);
			if (testFilter != null)
			{
				if (testPath == null)
				{
					if (treebankPath == null)
					{
						throw new Exception("No test treebank path specified...");
					}
					else
					{
						log.Info("No test treebank path specified.  Using train path: \"" + treebankPath + "\"");
						testPath = treebankPath;
					}
				}
				testTreebank = tlpParams.TestMemoryTreebank();
				testTreebank.LoadPath(testPath, testFilter);
			}
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(tlpParams.SisterSplitters()));
			// at this point we should be sure that op.tlpParams is
			// set appropriately (from command line, or from grammar file),
			// and will never change again.  We also set the tlpParams of the
			// LexicalizedParser instance to be the same object.  This is
			// redundancy that we probably should take out eventually.
			//
			// -- Roger
			if (op.testOptions.verbose)
			{
				log.Info("Lexicon is " + cs.GetType().FullName);
			}
			PrintWriter pwOut = tlpParams.Pw();
			PrintWriter pwErr = tlpParams.Pw(System.Console.Error);
			// Now what do we do with the parser we've made
			if (saveToTextFile)
			{
				// save the parser to textGrammar format
				if (textOutputFileOrUrl != null)
				{
					SaveSegmenterDataToText(cs, textOutputFileOrUrl);
				}
				else
				{
					log.Info("Usage: must specify a text segmenter data output path");
				}
			}
			if (saveToSerializedFile)
			{
				if (serializedOutputFileOrUrl == null && argIndex < args.Length)
				{
					// the next argument must be the path to serialize to
					serializedOutputFileOrUrl = args[argIndex];
					argIndex++;
				}
				if (serializedOutputFileOrUrl != null)
				{
					SaveSegmenterDataToSerialized(cs, serializedOutputFileOrUrl);
				}
				else
				{
					if (textOutputFileOrUrl == null && testTreebank == null)
					{
						// no saving/parsing request has been specified
						log.Info("usage: " + "java edu.stanford.nlp.parser.lexparser.ChineseLexiconAndWordSegmenter" + "-train trainFilesPath [start stop] serializedParserFilename");
					}
				}
			}
			/* --------------------- Testing part!!!! ----------------------- */
			if (op.testOptions.verbose)
			{
			}
			//      printOptions(false, op);
			if (testTreebank != null || (argIndex < args.Length && Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank")))
			{
				// test parser on treebank
				if (testTreebank == null)
				{
					// the next argument is the treebank path and range for testing
					testTreebank = tlpParams.TestMemoryTreebank();
					if (args.Length < argIndex + 4)
					{
						testTreebank.LoadPath(args[argIndex + 1]);
					}
					else
					{
						int testlow = System.Convert.ToInt32(args[argIndex + 2]);
						int testhigh = System.Convert.ToInt32(args[argIndex + 3]);
						testTreebank.LoadPath(args[argIndex + 1], new NumberRangeFileFilter(testlow, testhigh, true));
					}
				}
			}
		}

		private const long serialVersionUID = -6554995189795187918L;

		/* TODO - test segmenting on treebank. -pichuan */
		//      lp.testOnTreebank(testTreebank);
		//    } else if (argIndex >= args.length) {
		//      // no more arguments, so we just parse our own test sentence
		//      if (lp.parse(op.tlpParams.defaultTestSentence())) {
		//        treePrint.printTree(lp.getBestParse(), pwOut);
		//      } else {
		//        pwErr.println("Error. Can't parse test sentence: " +
		//              lp.parse(op.tlpParams.defaultTestSentence()));
		//      }
		//wsg2010: This code block doesn't actually do anything. It appears to read and tokenize a file, and then just print it.
		//         There are easier ways to do that. This code was copied from an old version of LexicalizedParser.
		//    else {
		//      // We parse filenames given by the remaining arguments
		//      int numWords = 0;
		//      Timing timer = new Timing();
		//      // set the tokenizer
		//      if (tokenized) {
		//        tokenizerFactory = WhitespaceTokenizer.factory();
		//      }
		//      TreebankLanguagePack tlp = tlpParams.treebankLanguagePack();
		//      if (tokenizerFactory == null) {
		//        tokenizerFactory = (TokenizerFactory<Word>) tlp.getTokenizerFactory();
		//      }
		//      documentPreprocessor.setTokenizerFactory(tokenizerFactory);
		//      documentPreprocessor.setSentenceFinalPuncWords(tlp.sentenceFinalPunctuationWords());
		//      if (encoding != null) {
		//        documentPreprocessor.setEncoding(encoding);
		//      }
		//      timer.start();
		//      for (int i = argIndex; i < args.length; i++) {
		//        String filename = args[i];
		//        try {
		//          List document = null;
		//          if (fromXML) {
		//            document = documentPreprocessor.getSentencesFromXML(filename, sentenceDelimiter, tokenized);
		//          } else {
		//            document = documentPreprocessor.getSentencesFromText(filename, escaper, sentenceDelimiter, tagDelimiter);
		//          }
		//          log.info("Segmenting file: " + filename + " with " + document.size() + " sentences.");
		//          PrintWriter pwo = pwOut;
		//          if (op.testOptions.writeOutputFiles) {
		//            try {
		//              pwo = tlpParams.pw(new FileOutputStream(filename + ".stp"));
		//            } catch (IOException ioe) {
		//              ioe.printStackTrace();
		//            }
		//          }
		//          int num = 0;
		//          treePrint.printHeader(pwo, tlp.getEncoding());
		//          for (Iterator it = document.iterator(); it.hasNext();) {
		//            num++;
		//            List sentence = (List) it.next();
		//            int len = sentence.size();
		//            numWords += len;
		////            pwErr.println("Parsing [sent. " + num + " len. " + len + "]: " + sentence);
		//            pwo.println(Sentence.listToString(sentence));
		//          }
		//          treePrint.printFooter(pwo);
		//          if (op.testOptions.writeOutputFiles) {
		//            pwo.close();
		//          }
		//        } catch (IOException e) {
		//          pwErr.println("Couldn't find file: " + filename);
		//        }
		//
		//      } // end for each file
		//      long millis = timer.stop();
		//      double wordspersec = numWords / (((double) millis) / 1000);
		//      NumberFormat nf = new DecimalFormat("0.00"); // easier way!
		//      pwErr.println("Segmented " + numWords + " words at " + nf.format(wordspersec) + " words per second.");
		//    }
		public virtual IUnknownWordModel GetUnknownWordModel()
		{
			return chineseLexicon.GetUnknownWordModel();
		}

		public virtual void SetUnknownWordModel(IUnknownWordModel uwm)
		{
			chineseLexicon.SetUnknownWordModel(uwm);
		}

		public virtual void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees)
		{
			Train(trees);
		}
	}
}
