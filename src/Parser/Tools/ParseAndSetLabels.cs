using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>
	/// Given a list of sentences, converts the sentences to trees and then
	/// relabels them using a list of new labels.
	/// </summary>
	/// <remarks>
	/// Given a list of sentences, converts the sentences to trees and then
	/// relabels them using a list of new labels.
	/// This tool processes the text using a given parser model, one
	/// sentence per line.
	/// The labels file is expected to be a tab separated file.  If there
	/// are multiple labels on a line, only the last one is used.
	/// There are a few options for how to handle missing labels:
	/// FAIL, DEFAULT, KEEP_ORIGINAL
	/// The argument for providing the labels is
	/// <c>-labels</c>
	/// The argument for providing the sentences is
	/// <c>-sentences</c>
	/// Alternatively, one can provide the flag
	/// <c>-useLabelKeys</c>
	/// to specify that the keys in the labels file should be treated as
	/// the sentences.  Exactly one of
	/// <c>-useLabelKeys</c>
	/// or
	/// <c>-sentences</c>
	/// must be used.
	/// Example command line:
	/// java edu.stanford.nlp.parser.tools.ParseAndSetLabels -output foo.txt -sentences "C:\Users\JohnBauer\Documents\alphasense\dataset\sentences10.txt" -labels "C:\Users\JohnBauer\Documents\alphasense\dataset\phrases10.tsv" -parser edu/stanford/nlp/models/srparser/englishSR.ser.gz -tagger edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger -remapLabels 0=1,1=2,2=2,3=0,4=0
	/// </remarks>
	public class ParseAndSetLabels
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Tools.ParseAndSetLabels));

		public enum MissingLabels
		{
			Fail,
			Default,
			KeepOriginal
		}

		private ParseAndSetLabels()
		{
		}

		// static methods
		public static void SetLabels(Tree tree, IDictionary<string, string> labelMap, ParseAndSetLabels.MissingLabels missing, string defaultLabel, ICollection<string> unknowns)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			string text = SentenceUtils.ListToString(tree.Yield());
			string label = labelMap[text];
			if (label != null)
			{
				tree.Label().SetValue(label);
			}
			else
			{
				switch (missing)
				{
					case ParseAndSetLabels.MissingLabels.Fail:
					{
						throw new Exception("No label for '" + text + "'");
					}

					case ParseAndSetLabels.MissingLabels.Default:
					{
						tree.Label().SetValue(defaultLabel);
						unknowns.Add(text);
						break;
					}

					case ParseAndSetLabels.MissingLabels.KeepOriginal:
					{
						// do nothing
						break;
					}

					default:
					{
						throw new ArgumentException("Unknown MissingLabels mode " + missing);
					}
				}
			}
			foreach (Tree child in tree.Children())
			{
				SetLabels(child, labelMap, missing, defaultLabel, unknowns);
			}
		}

		public static ICollection<string> SetLabels(IList<Tree> trees, IDictionary<string, string> labelMap, ParseAndSetLabels.MissingLabels missing, string defaultLabel)
		{
			logger.Info("Setting labels");
			ICollection<string> unknowns = new HashSet<string>();
			foreach (Tree tree in trees)
			{
				SetLabels(tree, labelMap, missing, defaultLabel, unknowns);
			}
			return unknowns;
		}

		public static void WriteTrees(IList<Tree> trees, string outputFile)
		{
			logger.Info("Writing new trees to " + outputFile);
			try
			{
				BufferedWriter @out = new BufferedWriter(new FileWriter(outputFile));
				foreach (Tree tree in trees)
				{
					@out.Write(tree.ToString());
					@out.Write("\n");
				}
				@out.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static IDictionary<string, string> ReadLabelMap(string labelsFile, string separator, string remapLabels)
		{
			logger.Info("Reading labels from " + labelsFile);
			IDictionary<string, string> remap = Java.Util.Collections.EmptyMap();
			if (remapLabels != null)
			{
				remap = StringUtils.MapStringToMap(remapLabels);
				logger.Info("Remapping labels using " + remap);
			}
			IDictionary<string, string> labelMap = new Dictionary<string, string>();
			foreach (string phrase in IOUtils.ReadLines(labelsFile))
			{
				string[] pieces = phrase.Split(separator);
				string label = pieces[pieces.Length - 1];
				if (remap.Contains(label))
				{
					label = remap[label];
				}
				labelMap[pieces[0]] = label;
			}
			return labelMap;
		}

		public static IList<string> ReadSentences(string sentencesFile)
		{
			logger.Info("Reading sentences from " + sentencesFile);
			IList<string> sentences = new List<string>();
			foreach (string sentence in IOUtils.ReadLines(sentencesFile))
			{
				sentences.Add(sentence);
			}
			return sentences;
		}

		public static ParserGrammar LoadParser(string parserFile, string taggerFile)
		{
			if (taggerFile != null)
			{
				return ParserGrammar.LoadModel(parserFile, "-preTag", "-taggerSerializedFile", taggerFile);
			}
			else
			{
				return ParserGrammar.LoadModel(parserFile);
			}
		}

		public static IList<Tree> ParseSentences(IList<string> sentences, ParserGrammar parser, TreeBinarizer binarizer)
		{
			logger.Info("Parsing sentences");
			IList<Tree> trees = new List<Tree>();
			foreach (string sentence in sentences)
			{
				Tree tree = parser.Parse(sentence);
				if (binarizer != null)
				{
					tree = binarizer.TransformTree(tree);
				}
				trees.Add(tree);
				if (trees.Count % 1000 == 0)
				{
					logger.Info("  Parsed " + trees.Count + " trees");
				}
			}
			return trees;
		}

		public static void Main(string[] args)
		{
			// TODO: rather than always rolling our own arg parser, we should
			// find a library which does it for us nicely
			string outputFile = null;
			string sentencesFile = null;
			string labelsFile = null;
			string parserFile = LexicalizedParser.DefaultParserLoc;
			string taggerFile = null;
			ParseAndSetLabels.MissingLabels missing = ParseAndSetLabels.MissingLabels.Default;
			string defaultLabel = "-1";
			string separator = "\\t+";
			string saveUnknownsFile = null;
			string remapLabels = null;
			int argIndex = 0;
			bool binarize = true;
			bool useLabelKeys = false;
			while (argIndex < args.Length)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
				{
					outputFile = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-sentences"))
					{
						sentencesFile = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-labels"))
						{
							labelsFile = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parser"))
							{
								parserFile = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tagger"))
								{
									taggerFile = args[argIndex + 1];
									argIndex += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-missing"))
									{
										missing = ParseAndSetLabels.MissingLabels.ValueOf(args[argIndex + 1]);
										argIndex += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-separator"))
										{
											separator = args[argIndex + 1];
											argIndex += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-default"))
											{
												defaultLabel = args[argIndex + 1];
												argIndex += 2;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-saveUnknowns"))
												{
													saveUnknownsFile = args[argIndex + 1];
													argIndex += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-remapLabels"))
													{
														remapLabels = args[argIndex + 1];
														argIndex += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-binarize"))
														{
															binarize = true;
															argIndex += 1;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nobinarize"))
															{
																binarize = false;
																argIndex += 1;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-useLabelKeys"))
																{
																	useLabelKeys = true;
																	argIndex += 1;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nouseLabelKeys"))
																	{
																		useLabelKeys = false;
																		argIndex += 1;
																	}
																	else
																	{
																		throw new ArgumentException("Unknown argument " + args[argIndex]);
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
						}
					}
				}
			}
			if (outputFile == null)
			{
				throw new ArgumentException("-output is required");
			}
			if (sentencesFile == null && !useLabelKeys)
			{
				throw new ArgumentException("-sentences or -useLabelKeys is required");
			}
			if (sentencesFile != null && useLabelKeys)
			{
				throw new ArgumentException("Use only one of -sentences or -useLabelKeys");
			}
			if (labelsFile == null)
			{
				throw new ArgumentException("-labels is required");
			}
			ParserGrammar parser = LoadParser(parserFile, taggerFile);
			TreeBinarizer binarizer = null;
			if (binarize)
			{
				binarizer = TreeBinarizer.SimpleTreeBinarizer(parser.GetTLPParams().HeadFinder(), parser.TreebankLanguagePack());
			}
			IDictionary<string, string> labelMap = ReadLabelMap(labelsFile, separator, remapLabels);
			IList<string> sentences;
			if (sentencesFile != null)
			{
				sentences = ReadSentences(sentencesFile);
			}
			else
			{
				sentences = new List<string>(labelMap.Keys);
			}
			IList<Tree> trees = ParseSentences(sentences, parser, binarizer);
			ICollection<string> unknowns = SetLabels(trees, labelMap, missing, defaultLabel);
			WriteTrees(trees, outputFile);
		}
	}
}
