using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>
	/// A wrapper class which creates a suitable pipeline for the sentiment
	/// model and processes raw text.
	/// </summary>
	/// <remarks>
	/// A wrapper class which creates a suitable pipeline for the sentiment
	/// model and processes raw text.
	/// <p>
	/// The main program has the following options: <br />
	/// <c>-parserModel</c>
	/// Which parser model to use, defaults to englishPCFG.ser.gz <br />
	/// <c>-sentimentModel</c>
	/// Which sentiment model to use, defaults to sentiment.ser.gz <br />
	/// <c>-file</c>
	/// Which file to process. <br />
	/// <c>-fileList</c>
	/// A comma separated list of files to process. <br />
	/// <c>-stdin</c>
	/// Read one line at a time from stdin. <br />
	/// <c>-output</c>
	/// pennTrees: Output trees with scores at each binarized node.  vectors: Number tree nodes and print out the vectors.  probabilities: Output the scores for different labels for each node. Defaults to printing just the root. <br />
	/// <c>-filterUnknown</c>
	/// Remove unknown trees from the input.  Only applies to TREES input, in which case the trees must be binarized with sentiment labels <br />
	/// <c>-help</c>
	/// Print out help <br />
	/// </remarks>
	/// <author>John Bauer</author>
	public class SentimentPipeline
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.SentimentPipeline));

		private static readonly NumberFormat Nf = new DecimalFormat("0.0000");

		internal enum Output
		{
			Penntrees,
			Vectors,
			Root,
			Probabilities
		}

		internal enum Input
		{
			Text,
			Trees
		}

		private SentimentPipeline()
		{
		}

		// static methods
		/// <summary>
		/// Sets the labels on the tree (except the leaves) to be the integer
		/// value of the sentiment prediction.
		/// </summary>
		/// <remarks>
		/// Sets the labels on the tree (except the leaves) to be the integer
		/// value of the sentiment prediction.  Makes it easy to print out
		/// with Tree.toString()
		/// </remarks>
		private static void SetSentimentLabels(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				SetSentimentLabels(child);
			}
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("Required a tree with CoreLabels");
			}
			CoreLabel cl = (CoreLabel)label;
			cl.SetValue(int.ToString(RNNCoreAnnotations.GetPredictedClass(tree)));
		}

		/// <summary>Sets the labels on the tree to be the indices of the nodes.</summary>
		/// <remarks>
		/// Sets the labels on the tree to be the indices of the nodes.
		/// Starts counting at the root and does a postorder traversal.
		/// </remarks>
		private static int SetIndexLabels(Tree tree, int index)
		{
			if (tree.IsLeaf())
			{
				return index;
			}
			tree.Label().SetValue(int.ToString(index));
			index++;
			foreach (Tree child in tree.Children())
			{
				index = SetIndexLabels(child, index);
			}
			return index;
		}

		/// <summary>Outputs the vectors from the tree.</summary>
		/// <remarks>
		/// Outputs the vectors from the tree.  Counts the tree nodes the
		/// same as setIndexLabels.
		/// </remarks>
		private static int OutputTreeVectors(TextWriter @out, Tree tree, int index)
		{
			if (tree.IsLeaf())
			{
				return index;
			}
			@out.Write("  " + index + ':');
			SimpleMatrix vector = RNNCoreAnnotations.GetNodeVector(tree);
			for (int i = 0; i < vector.GetNumElements(); ++i)
			{
				@out.Write("  " + Nf.Format(vector.Get(i)));
			}
			@out.WriteLine();
			index++;
			foreach (Tree child in tree.Children())
			{
				index = OutputTreeVectors(@out, child, index);
			}
			return index;
		}

		/// <summary>Outputs the scores from the tree.</summary>
		/// <remarks>
		/// Outputs the scores from the tree.  Counts the tree nodes the
		/// same as setIndexLabels.
		/// </remarks>
		private static int OutputTreeScores(TextWriter @out, Tree tree, int index)
		{
			if (tree.IsLeaf())
			{
				return index;
			}
			@out.Write("  " + index + ':');
			SimpleMatrix vector = RNNCoreAnnotations.GetPredictions(tree);
			for (int i = 0; i < vector.GetNumElements(); ++i)
			{
				@out.Write("  " + Nf.Format(vector.Get(i)));
			}
			@out.WriteLine();
			index++;
			foreach (Tree child in tree.Children())
			{
				index = OutputTreeScores(@out, child, index);
			}
			return index;
		}

		/// <summary>Outputs a tree using the output style requested.</summary>
		private static void OutputTree(TextWriter @out, ICoreMap sentence, IList<SentimentPipeline.Output> outputFormats)
		{
			Tree tree = sentence.Get(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree));
			foreach (SentimentPipeline.Output output in outputFormats)
			{
				switch (output)
				{
					case SentimentPipeline.Output.Penntrees:
					{
						Tree copy = tree.DeepCopy();
						SetSentimentLabels(copy);
						@out.WriteLine(copy);
						break;
					}

					case SentimentPipeline.Output.Vectors:
					{
						Tree copy = tree.DeepCopy();
						SetIndexLabels(copy, 0);
						@out.WriteLine(copy);
						OutputTreeVectors(@out, tree, 0);
						break;
					}

					case SentimentPipeline.Output.Root:
					{
						@out.WriteLine("  " + sentence.Get(typeof(SentimentCoreAnnotations.SentimentClass)));
						break;
					}

					case SentimentPipeline.Output.Probabilities:
					{
						Tree copy = tree.DeepCopy();
						SetIndexLabels(copy, 0);
						@out.WriteLine(copy);
						OutputTreeScores(@out, tree, 0);
						break;
					}

					default:
					{
						throw new ArgumentException("Unknown output format " + output);
					}
				}
			}
		}

		private const string DefaultTlppClass = "edu.stanford.nlp.parser.lexparser.EnglishTreebankParserParams";

		private static void Help()
		{
			log.Info("Known command line arguments:");
			log.Info("  -sentimentModel <model>: Which model to use");
			log.Info("  -parserModel <model>: Which parser to use");
			log.Info("  -file <filename>: Which file to process");
			log.Info("  -fileList <file>,<file>,...: Comma separated list of files to process.  Output goes to file.out");
			log.Info("  -stdin: Process stdin instead of a file");
			log.Info("  -input <format>: Which format to input, TEXT or TREES.  Will not process stdin as trees.  If trees are not already binarized, they will be binarized with -tlppClass's headfinder, which means they must have labels in that treebank's tagset."
				);
			log.Info("  -output <format>: Which format to output, PENNTREES, VECTORS, PROBABILITIES, or ROOT.  Multiple formats can be specified as a comma separated list.");
			log.Info("  -filterUnknown: remove unknown trees from the input.  Only applies to TREES input, in which case the trees must be binarized with sentiment labels");
			log.Info("  -tlppClass: a class to use for building the binarizer if using non-binarized TREES as input.  Defaults to " + DefaultTlppClass);
		}

		/// <summary>Reads an annotation from the given filename using the requested input.</summary>
		public static IList<Annotation> GetAnnotations(StanfordCoreNLP tokenizer, SentimentPipeline.Input inputFormat, string filename, bool filterUnknown)
		{
			switch (inputFormat)
			{
				case SentimentPipeline.Input.Text:
				{
					string text = IOUtils.SlurpFileNoExceptions(filename);
					Annotation annotation = new Annotation(text);
					tokenizer.Annotate(annotation);
					IList<Annotation> annotations = Generics.NewArrayList();
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						Annotation nextAnnotation = new Annotation(sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
						nextAnnotation.Set(typeof(CoreAnnotations.SentencesAnnotation), Java.Util.Collections.SingletonList(sentence));
						annotations.Add(nextAnnotation);
					}
					return annotations;
				}

				case SentimentPipeline.Input.Trees:
				{
					IList<Tree> trees;
					if (filterUnknown)
					{
						trees = SentimentUtils.ReadTreesWithGoldLabels(filename);
						trees = SentimentUtils.FilterUnknownRoots(trees);
					}
					else
					{
						MemoryTreebank treebank = new MemoryTreebank("utf-8");
						treebank.LoadPath(filename, null);
						trees = new List<Tree>(treebank);
					}
					IList<Annotation> annotations = Generics.NewArrayList();
					foreach (Tree tree in trees)
					{
						ICoreMap sentence = new Annotation(SentenceUtils.ListToString(tree.Yield()));
						sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), tree);
						IList<ICoreMap> sentences = Java.Util.Collections.SingletonList(sentence);
						Annotation annotation = new Annotation(string.Empty);
						annotation.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
						annotations.Add(annotation);
					}
					return annotations;
				}

				default:
				{
					throw new ArgumentException("Unknown format " + inputFormat);
				}
			}
		}

		/// <summary>Runs the tree-based sentiment model on some text.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string parserModel = null;
			string sentimentModel = null;
			string filename = null;
			string fileList = null;
			bool stdin = false;
			bool filterUnknown = false;
			IList<SentimentPipeline.Output> outputFormats = Java.Util.Collections.SingletonList(SentimentPipeline.Output.Root);
			SentimentPipeline.Input inputFormat = SentimentPipeline.Input.Text;
			string tlppClass = DefaultTlppClass;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-sentimentModel"))
				{
					sentimentModel = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parserModel"))
					{
						parserModel = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-file"))
						{
							filename = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-fileList"))
							{
								fileList = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-stdin"))
								{
									stdin = true;
									argIndex++;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
									{
										inputFormat = SentimentPipeline.Input.ValueOf(args[argIndex + 1].ToUpper());
										argIndex += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
										{
											string[] formats = args[argIndex + 1].Split(",");
											outputFormats = new List<SentimentPipeline.Output>();
											foreach (string format in formats)
											{
												outputFormats.Add(SentimentPipeline.Output.ValueOf(format.ToUpper()));
											}
											argIndex += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-filterUnknown"))
											{
												filterUnknown = true;
												argIndex++;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tlppClass"))
												{
													tlppClass = args[argIndex + 1];
													argIndex += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-help"))
													{
														Help();
														System.Environment.Exit(0);
													}
													else
													{
														log.Info("Unknown argument " + args[argIndex + 1]);
														Help();
														throw new ArgumentException("Unknown argument " + args[argIndex + 1]);
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			// We construct two pipelines.  One handles tokenization, if
			// necessary.  The other takes tokenized sentences and converts
			// them to sentiment trees.
			Properties pipelineProps = new Properties();
			Properties tokenizerProps = null;
			if (sentimentModel != null)
			{
				pipelineProps.SetProperty("sentiment.model", sentimentModel);
			}
			if (parserModel != null)
			{
				pipelineProps.SetProperty("parse.model", parserModel);
			}
			if (inputFormat == SentimentPipeline.Input.Trees)
			{
				pipelineProps.SetProperty("annotators", "binarizer, sentiment");
				pipelineProps.SetProperty("customAnnotatorClass.binarizer", "edu.stanford.nlp.pipeline.BinarizerAnnotator");
				pipelineProps.SetProperty("binarizer.tlppClass", tlppClass);
				pipelineProps.SetProperty("enforceRequirements", "false");
			}
			else
			{
				pipelineProps.SetProperty("annotators", "parse, sentiment");
				pipelineProps.SetProperty("parse.binaryTrees", "true");
				pipelineProps.SetProperty("parse.buildgraphs", "false");
				pipelineProps.SetProperty("enforceRequirements", "false");
				tokenizerProps = new Properties();
				tokenizerProps.SetProperty("annotators", "tokenize, ssplit");
			}
			if (stdin && tokenizerProps != null)
			{
				tokenizerProps.SetProperty(StanfordCoreNLP.NewlineSplitterProperty, "true");
			}
			int count = 0;
			if (filename != null)
			{
				count++;
			}
			if (fileList != null)
			{
				count++;
			}
			if (stdin)
			{
				count++;
			}
			if (count > 1)
			{
				throw new ArgumentException("Please only specify one of -file, -fileList or -stdin");
			}
			if (count == 0)
			{
				throw new ArgumentException("Please specify either -file, -fileList or -stdin");
			}
			StanfordCoreNLP tokenizer = (tokenizerProps == null) ? null : new StanfordCoreNLP(tokenizerProps);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(pipelineProps);
			if (filename != null)
			{
				// Process a file.  The pipeline will do tokenization, which
				// means it will split it into sentences as best as possible
				// with the tokenizer.
				IList<Annotation> annotations = GetAnnotations(tokenizer, inputFormat, filename, filterUnknown);
				foreach (Annotation annotation in annotations)
				{
					pipeline.Annotate(annotation);
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						System.Console.Out.WriteLine(sentence);
						OutputTree(System.Console.Out, sentence, outputFormats);
					}
				}
			}
			else
			{
				if (fileList != null)
				{
					// Process multiple files.  The pipeline will do tokenization,
					// which means it will split it into sentences as best as
					// possible with the tokenizer.  Output will go to filename.out
					// for each file.
					foreach (string file in fileList.Split(","))
					{
						IList<Annotation> annotations = GetAnnotations(tokenizer, inputFormat, file, filterUnknown);
						FileOutputStream fout = new FileOutputStream(file + ".out");
						TextWriter pout = new TextWriter(fout);
						foreach (Annotation annotation in annotations)
						{
							pipeline.Annotate(annotation);
							foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
							{
								pout.WriteLine(sentence);
								OutputTree(pout, sentence, outputFormats);
							}
						}
						pout.Flush();
						fout.Close();
					}
				}
				else
				{
					// Process stdin.  Each line will be treated as a single sentence.
					log.Info("Reading in text from stdin.");
					log.Info("Please enter one sentence per line.");
					log.Info("Processing will end when EOF is reached.");
					BufferedReader reader = IOUtils.ReaderFromStdin("utf-8");
					for (string line; (line = reader.ReadLine()) != null; )
					{
						line = line.Trim();
						if (!line.IsEmpty())
						{
							Annotation annotation = tokenizer.Process(line);
							pipeline.Annotate(annotation);
							foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
							{
								OutputTree(System.Console.Out, sentence, outputFormats);
							}
						}
						else
						{
							// Output blank lines for blank lines so the tool can be
							// used for line-by-line text processing
							System.Console.Out.WriteLine();
						}
					}
				}
			}
		}
	}
}
