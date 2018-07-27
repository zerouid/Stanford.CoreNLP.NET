using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This is just a main method and other static methods for
	/// command-line manipulation, statistics, and testing of
	/// Treebank objects.
	/// </summary>
	/// <remarks>
	/// This is just a main method and other static methods for
	/// command-line manipulation, statistics, and testing of
	/// Treebank objects.  It has been separated out into its
	/// own class so that users of Treebank classes don't have
	/// to inherit all this class' dependencies.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class Treebanks
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Treebanks));

		private Treebanks()
		{
		}

		// static methods
		private static void PrintUsage()
		{
			log.Info("This main method will let you variously manipulate and view a treebank.");
			log.Info("Usage: java Treebanks [-flags]* treebankPath [fileRanges]");
			log.Info("Useful flags include:");
			log.Info("\t-maxLength n\t-suffix ext\t-treeReaderFactory class");
			log.Info("\t-pennPrint\t-encoding enc\t-tlp class\t-sentenceLengths");
			log.Info("\t-summary\t-decimate\t-yield\t-correct\t-punct");
			log.Info("\t-oneLine\t-words\t-taggedWords\t-annotate options");
		}

		/// <summary>Loads treebank and prints it.</summary>
		/// <remarks>
		/// Loads treebank and prints it.
		/// All files below the designated
		/// <c>filePath</c>
		/// within the given
		/// number range if any are loaded.  You can normalize the trees or not
		/// (English-specific) and print trees one per line up to a certain length
		/// (for EVALB).
		/// <p>
		/// Usage:
		/// <c>java edu.stanford.nlp.trees.Treebanks [-maxLength n|-normalize|-treeReaderFactory class] filePath [numberRanges]</c>
		/// </remarks>
		/// <param name="args">Array of command-line arguments</param>
		/// <exception cref="System.IO.IOException">If there is a treebank file access problem</exception>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				PrintUsage();
				return;
			}
			int i = 0;
			int maxLength;
			int minLength;
			int maxL = int.MaxValue;
			int minL = -1;
			bool normalized = false;
			bool decimate = false;
			bool pennPrintTrees = false;
			bool oneLinePrint = false;
			bool printTaggedWords = false;
			bool printWords = false;
			bool correct = false;
			string annotationOptions = null;
			bool summary = false;
			bool timing = false;
			bool yield = false;
			bool punct = false;
			bool sentenceLengths = false;
			bool countTaggings = false;
			bool removeCodeTrees = false;
			string decimatePrefix = null;
			string encoding = TreebankLanguagePackConstants.DefaultEncoding;
			string suffix = Treebank.DefaultTreeFileSuffix;
			ITreeReaderFactory trf = null;
			ITreebankLanguagePack tlp = null;
			IList<IPredicate<Tree>> filters = new List<IPredicate<Tree>>();
			while (i < args.Length && args[i].StartsWith("-"))
			{
				if (args[i].Equals("-maxLength") && i + 1 < args.Length)
				{
					maxL = System.Convert.ToInt32(args[i + 1]);
					i += 2;
				}
				else
				{
					if (args[i].Equals("-minLength") && i + 1 < args.Length)
					{
						minL = System.Convert.ToInt32(args[i + 1]);
						i += 2;
					}
					else
					{
						if (args[i].Equals("-h") || args[i].Equals("-help"))
						{
							PrintUsage();
							i++;
						}
						else
						{
							if (args[i].Equals("-normalized"))
							{
								normalized = true;
								i += 1;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tlp"))
								{
									try
									{
										object o = Sharpen.Runtime.GetType(args[i + 1]).GetDeclaredConstructor().NewInstance();
										tlp = (ITreebankLanguagePack)o;
										trf = tlp.TreeReaderFactory();
									}
									catch (Exception)
									{
										log.Info("Couldn't instantiate as TreebankLanguagePack: " + args[i + 1]);
										return;
									}
									i += 2;
								}
								else
								{
									if (args[i].Equals("-treeReaderFactory") || args[i].Equals("-trf"))
									{
										try
										{
											object o = Sharpen.Runtime.GetType(args[i + 1]).GetDeclaredConstructor().NewInstance();
											trf = (ITreeReaderFactory)o;
										}
										catch (Exception)
										{
											log.Info("Couldn't instantiate as TreeReaderFactory: " + args[i + 1]);
											return;
										}
										i += 2;
									}
									else
									{
										if (args[i].Equals("-suffix"))
										{
											suffix = args[i + 1];
											i += 2;
										}
										else
										{
											if (args[i].Equals("-decimate"))
											{
												decimate = true;
												decimatePrefix = args[i + 1];
												i += 2;
											}
											else
											{
												if (args[i].Equals("-encoding"))
												{
													encoding = args[i + 1];
													i += 2;
												}
												else
												{
													if (args[i].Equals("-correct"))
													{
														correct = true;
														i += 1;
													}
													else
													{
														if (args[i].Equals("-summary"))
														{
															summary = true;
															i += 1;
														}
														else
														{
															if (args[i].Equals("-yield"))
															{
																yield = true;
																i += 1;
															}
															else
															{
																if (args[i].Equals("-punct"))
																{
																	punct = true;
																	i += 1;
																}
																else
																{
																	if (args[i].Equals("-pennPrint"))
																	{
																		pennPrintTrees = true;
																		i++;
																	}
																	else
																	{
																		if (args[i].Equals("-oneLine"))
																		{
																			oneLinePrint = true;
																			i++;
																		}
																		else
																		{
																			if (args[i].Equals("-taggedWords"))
																			{
																				printTaggedWords = true;
																				i++;
																			}
																			else
																			{
																				if (args[i].Equals("-words"))
																				{
																					printWords = true;
																					i++;
																				}
																				else
																				{
																					if (args[i].Equals("-annotate"))
																					{
																						annotationOptions = args[i + 1];
																						i += 2;
																					}
																					else
																					{
																						if (args[i].Equals("-timing"))
																						{
																							timing = true;
																							i++;
																						}
																						else
																						{
																							if (args[i].Equals("-countTaggings"))
																							{
																								countTaggings = true;
																								i++;
																							}
																							else
																							{
																								if (args[i].Equals("-sentenceLengths"))
																								{
																									sentenceLengths = true;
																									i++;
																								}
																								else
																								{
																									if (args[i].Equals("-removeCodeTrees"))
																									{
																										removeCodeTrees = true;
																										i++;
																									}
																									else
																									{
																										if (args[i].Equals("-filter"))
																										{
																											IPredicate<Tree> filter = ReflectionLoading.LoadByReflection(args[i + 1]);
																											filters.Add(filter);
																											i += 2;
																										}
																										else
																										{
																											log.Info("Unknown option: " + args[i]);
																											i++;
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
											}
										}
									}
								}
							}
						}
					}
				}
			}
			maxLength = maxL;
			minLength = minL;
			Treebank treebank;
			if (trf == null)
			{
				trf = null;
			}
			if (normalized)
			{
				treebank = new DiskTreebank();
			}
			else
			{
				treebank = new DiskTreebank(trf, encoding);
			}
			foreach (IPredicate<Tree> filter_1 in filters)
			{
				treebank = new FilteringTreebank(treebank, filter_1);
			}
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
			if (i + 1 < args.Length)
			{
				treebank.LoadPath(args[i], new NumberRangesFileFilter(args[i + 1], true));
			}
			else
			{
				if (i < args.Length)
				{
					treebank.LoadPath(args[i], suffix, true);
				}
				else
				{
					PrintUsage();
					return;
				}
			}
			// log.info("Loaded " + treebank.size() + " trees from " + args[i]);
			if (annotationOptions != null)
			{
				// todo Not yet implemented
				log.Info("annotationOptions not yet implemented");
			}
			if (summary)
			{
				System.Console.Out.WriteLine(treebank.TextualSummary());
			}
			if (sentenceLengths)
			{
				SentenceLengths(treebank, args[i], ((i + 1) < args.Length ? args[i + 1] : null), pw);
			}
			if (punct)
			{
				PrintPunct(treebank, tlp, pw);
			}
			if (correct)
			{
				treebank = new EnglishPTBTreebankCorrector().TransformTrees(treebank);
			}
			if (pennPrintTrees)
			{
				treebank.Apply(null);
			}
			if (oneLinePrint)
			{
				treebank.Apply(null);
			}
			if (printWords)
			{
				TreeNormalizer tn = new BobChrisTreeNormalizer();
				treebank.Apply(null);
			}
			if (printTaggedWords)
			{
				TreeNormalizer tn = new BobChrisTreeNormalizer();
				treebank.Apply(null);
			}
			if (countTaggings)
			{
				CountTaggings(treebank, pw);
			}
			if (yield)
			{
				treebank.Apply(null);
			}
			if (decimate)
			{
				TextWriter w1 = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(decimatePrefix + "-train.txt"), encoding));
				TextWriter w2 = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(decimatePrefix + "-dev.txt"), encoding));
				TextWriter w3 = new BufferedWriter(new OutputStreamWriter(new FileOutputStream(decimatePrefix + "-test.txt"), encoding));
				treebank.Decimate(w1, w2, w3);
			}
			if (timing)
			{
				RunTiming(treebank);
			}
			if (removeCodeTrees)
			{
				// this is a bit of a hack. It only works on an individual file
				if (new File(args[i]).IsDirectory())
				{
					throw new Exception("-removeCodeTrees only works on a single file");
				}
				string treebankStr = IOUtils.SlurpFile(args[i]);
				treebankStr = treebankStr.ReplaceAll("\\( \\(CODE <[^>]+>\\)\\)", string.Empty);
				TextWriter w = new OutputStreamWriter(new FileOutputStream(args[i]), encoding);
				w.Write(treebankStr);
				w.Close();
			}
		}

		// end main()
		private static void PrintPunct(Treebank treebank, ITreebankLanguagePack tlp, PrintWriter pw)
		{
			if (tlp == null)
			{
				log.Info("The -punct option requires you to specify -tlp");
			}
			else
			{
				IPredicate<string> punctTagFilter = tlp.PunctuationTagAcceptFilter();
				foreach (Tree t in treebank)
				{
					IList<TaggedWord> tws = t.TaggedYield();
					foreach (TaggedWord tw in tws)
					{
						if (punctTagFilter.Test(tw.Tag()))
						{
							pw.Println(tw);
						}
					}
				}
			}
		}

		private static void CountTaggings(Treebank tb, PrintWriter pw)
		{
			TwoDimensionalCounter<string, string> wtc = new TwoDimensionalCounter<string, string>();
			tb.Apply(null);
			foreach (string key in wtc.FirstKeySet())
			{
				pw.Print(key);
				pw.Print('\t');
				ICounter<string> ctr = wtc.GetCounter(key);
				foreach (string k2 in ctr.KeySet())
				{
					pw.Print(k2 + '\t' + ctr.GetCount(k2) + '\t');
				}
				pw.Println();
			}
		}

		private static void RunTiming(Treebank treebank)
		{
			System.Console.Out.WriteLine();
			Timing.StartTime();
			int num = 0;
			foreach (Tree t in treebank)
			{
				num += t.Yield().Count;
			}
			Timing.EndTime("traversing corpus, counting words with iterator");
			log.Info("There were " + num + " words in the treebank.");
			treebank.Apply(new _ITreeVisitor_352());
			// = 0;
			log.Info();
			Timing.EndTime("traversing corpus, counting words with TreeVisitor");
			log.Info("There were " + num + " words in the treebank.");
			log.Info();
			Timing.StartTime();
			log.Info("This treebank contains " + treebank.Count + " trees.");
			Timing.EndTime("size of corpus");
		}

		private sealed class _ITreeVisitor_352 : ITreeVisitor
		{
			public _ITreeVisitor_352()
			{
			}

			internal int num;

			public void VisitTree(Tree t)
			{
				this.num += t.Yield().Count;
			}
		}

		private static void SentenceLengths(Treebank treebank, string name, string range, PrintWriter pw)
		{
			int maxleng = 150;
			int[] lengthCounts = new int[maxleng + 2];
			int numSents = 0;
			int longestSeen = 0;
			int totalWords = 0;
			string longSent = string.Empty;
			double median = 0.0;
			NumberFormat nf = new DecimalFormat("0.0");
			bool foundMedian = false;
			foreach (Tree t in treebank)
			{
				numSents++;
				int len = t.Yield().Count;
				if (len <= maxleng)
				{
					lengthCounts[len]++;
				}
				else
				{
					lengthCounts[maxleng + 1]++;
				}
				totalWords += len;
				if (len > longestSeen)
				{
					longestSeen = len;
					longSent = t.ToString();
				}
			}
			System.Console.Out.Write("Files " + name + ' ');
			if (range != null)
			{
				System.Console.Out.Write(range + ' ');
			}
			System.Console.Out.WriteLine("consists of " + numSents + " sentences");
			int runningTotal = 0;
			for (int i = 0; i <= maxleng; i++)
			{
				runningTotal += lengthCounts[i];
				System.Console.Out.WriteLine("  " + lengthCounts[i] + " of length " + i + " (running total: " + runningTotal + ')');
				if (!foundMedian && runningTotal > numSents / 2)
				{
					if (numSents % 2 == 0 && runningTotal == numSents / 2 + 1)
					{
						// right on the boundary
						int j = i - 1;
						while (j > 0 && lengthCounts[j] == 0)
						{
							j--;
						}
						median = ((double)i + j) / 2;
					}
					else
					{
						median = i;
					}
					foundMedian = true;
				}
			}
			if (lengthCounts[maxleng + 1] > 0)
			{
				runningTotal += lengthCounts[maxleng + 1];
				System.Console.Out.WriteLine("  " + lengthCounts[maxleng + 1] + " of length " + (maxleng + 1) + " to " + longestSeen + " (running total: " + runningTotal + ')');
			}
			System.Console.Out.WriteLine("Average length: " + nf.Format(((double)totalWords) / numSents) + "; median length: " + nf.Format(median));
			System.Console.Out.WriteLine("Longest sentence is of length: " + longestSeen);
			pw.Println(longSent);
		}
	}
}
