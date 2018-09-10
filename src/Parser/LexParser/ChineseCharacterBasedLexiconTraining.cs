using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Includes a main file which trains a ChineseCharacterBasedLexicon.</summary>
	/// <remarks>
	/// Includes a main file which trains a ChineseCharacterBasedLexicon.
	/// Separated from ChineseCharacterBasedLexicon so that packages which
	/// use ChineseCharacterBasedLexicon don't also need all of
	/// LexicalizedParser.
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class ChineseCharacterBasedLexiconTraining
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ChineseCharacterBasedLexiconTraining));

		protected internal static readonly NumberFormat formatter = new DecimalFormat("0.000");

		public static void PrintStats(ICollection<Tree> trees, PrintWriter pw)
		{
			ClassicCounter<int> wordLengthCounter = new ClassicCounter<int>();
			ClassicCounter<TaggedWord> wordCounter = new ClassicCounter<TaggedWord>();
			ClassicCounter<ChineseCharacterBasedLexicon.Symbol> charCounter = new ClassicCounter<ChineseCharacterBasedLexicon.Symbol>();
			int counter = 0;
			foreach (Tree tree in trees)
			{
				counter++;
				IList<TaggedWord> taggedWords = tree.TaggedYield();
				foreach (TaggedWord taggedWord in taggedWords)
				{
					string word = taggedWord.Word();
					if (word.Equals(LexiconConstants.Boundary))
					{
						continue;
					}
					wordCounter.IncrementCount(taggedWord);
					wordLengthCounter.IncrementCount(int.Parse(word.Length));
					for (int j = 0; j < length; j++)
					{
						ChineseCharacterBasedLexicon.Symbol sym = ChineseCharacterBasedLexicon.Symbol.CannonicalSymbol(word[j]);
						charCounter.IncrementCount(sym);
					}
					charCounter.IncrementCount(ChineseCharacterBasedLexicon.Symbol.EndWord);
				}
			}
			ICollection<ChineseCharacterBasedLexicon.Symbol> singletonChars = Counters.KeysBelow(charCounter, 1.5);
			ICollection<TaggedWord> singletonWords = Counters.KeysBelow(wordCounter, 1.5);
			ClassicCounter<string> singletonWordPOSes = new ClassicCounter<string>();
			foreach (TaggedWord taggedWord_1 in singletonWords)
			{
				singletonWordPOSes.IncrementCount(taggedWord_1.Tag());
			}
			Distribution<string> singletonWordPOSDist = Distribution.GetDistribution(singletonWordPOSes);
			ClassicCounter<char> singletonCharRads = new ClassicCounter<char>();
			foreach (ChineseCharacterBasedLexicon.Symbol s in singletonChars)
			{
				singletonCharRads.IncrementCount(char.ValueOf(RadicalMap.GetRadical(s.GetCh())));
			}
			Distribution<char> singletonCharRadDist = Distribution.GetDistribution(singletonCharRads);
			Distribution<int> wordLengthDist = Distribution.GetDistribution(wordLengthCounter);
			NumberFormat percent = new DecimalFormat("##.##%");
			pw.Println("There are " + singletonChars.Count + " singleton chars out of " + (int)charCounter.TotalCount() + " tokens and " + charCounter.Size() + " types found in " + counter + " trees.");
			pw.Println("Thus singletonChars comprise " + percent.Format(singletonChars.Count / charCounter.TotalCount()) + " of tokens and " + percent.Format((double)singletonChars.Count / charCounter.Size()) + " of types.");
			pw.Println();
			pw.Println("There are " + singletonWords.Count + " singleton words out of " + (int)wordCounter.TotalCount() + " tokens and " + wordCounter.Size() + " types.");
			pw.Println("Thus singletonWords comprise " + percent.Format(singletonWords.Count / wordCounter.TotalCount()) + " of tokens and " + percent.Format((double)singletonWords.Count / wordCounter.Size()) + " of types.");
			pw.Println();
			pw.Println("Distribution over singleton word POS:");
			pw.Println(singletonWordPOSDist.ToString());
			pw.Println();
			pw.Println("Distribution over singleton char radicals:");
			pw.Println(singletonCharRadDist.ToString());
			pw.Println();
			pw.Println("Distribution over word length:");
			pw.Println(wordLengthDist);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			IDictionary<string, int> flagsToNumArgs = Generics.NewHashMap();
			flagsToNumArgs["-parser"] = int.Parse(3);
			flagsToNumArgs["-lex"] = int.Parse(3);
			flagsToNumArgs["-test"] = int.Parse(2);
			flagsToNumArgs["-out"] = int.Parse(1);
			flagsToNumArgs["-lengthPenalty"] = int.Parse(1);
			flagsToNumArgs["-penaltyType"] = int.Parse(1);
			flagsToNumArgs["-maxLength"] = int.Parse(1);
			flagsToNumArgs["-stats"] = int.Parse(2);
			IDictionary<string, string[]> argMap = StringUtils.ArgsToMap(args, flagsToNumArgs);
			bool eval = argMap.Contains("-eval");
			PrintWriter pw = null;
			if (argMap.Contains("-out"))
			{
				pw = new PrintWriter(new OutputStreamWriter(new FileOutputStream((argMap["-out"])[0]), "GB18030"), true);
			}
			log.Info("ChineseCharacterBasedLexicon called with args:");
			ChineseTreebankParserParams ctpp = new ChineseTreebankParserParams();
			for (int i = 0; i < args.Length; i++)
			{
				ctpp.SetOptionFlag(args, i);
				log.Info(" " + args[i]);
			}
			log.Info();
			Options op = new Options(ctpp);
			if (argMap.Contains("-stats"))
			{
				string[] statArgs = (argMap["-stats"]);
				MemoryTreebank rawTrainTreebank = op.tlpParams.MemoryTreebank();
				IFileFilter trainFilt = new NumberRangesFileFilter(statArgs[1], false);
				rawTrainTreebank.LoadPath(new File(statArgs[0]), trainFilt);
				log.Info("Done reading trees.");
				MemoryTreebank trainTreebank;
				if (argMap.Contains("-annotate"))
				{
					trainTreebank = new MemoryTreebank();
					TreeAnnotator annotator = new TreeAnnotator(ctpp.HeadFinder(), ctpp, op);
					foreach (Tree tree in rawTrainTreebank)
					{
						trainTreebank.Add(annotator.TransformTree(tree));
					}
					log.Info("Done annotating trees.");
				}
				else
				{
					trainTreebank = rawTrainTreebank;
				}
				PrintStats(trainTreebank, pw);
				System.Environment.Exit(0);
			}
			int maxLength = 1000000;
			//    Test.verbose = true;
			if (argMap.Contains("-norm"))
			{
				op.testOptions.lengthNormalization = true;
			}
			if (argMap.Contains("-maxLength"))
			{
				maxLength = System.Convert.ToInt32((argMap["-maxLength"])[0]);
			}
			op.testOptions.maxLength = 120;
			bool combo = argMap.Contains("-combo");
			if (combo)
			{
				ctpp.useCharacterBasedLexicon = true;
				op.testOptions.maxSpanForTags = 10;
				op.doDep = false;
				op.dcTags = false;
			}
			LexicalizedParser lp = null;
			ILexicon lex = null;
			if (argMap.Contains("-parser"))
			{
				string[] parserArgs = (argMap["-parser"]);
				if (parserArgs.Length > 1)
				{
					IFileFilter trainFilt = new NumberRangesFileFilter(parserArgs[1], false);
					lp = LexicalizedParser.TrainFromTreebank(parserArgs[0], trainFilt, op);
					if (parserArgs.Length == 3)
					{
						string filename = parserArgs[2];
						log.Info("Writing parser in serialized format to file " + filename + " ");
						System.Console.Error.Flush();
						ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
						@out.WriteObject(lp);
						@out.Close();
						log.Info("done.");
					}
				}
				else
				{
					string parserFile = parserArgs[0];
					lp = LexicalizedParser.LoadModel(parserFile, op);
				}
				lex = lp.GetLexicon();
				op = lp.GetOp();
				ctpp = (ChineseTreebankParserParams)op.tlpParams;
			}
			if (argMap.Contains("-rad"))
			{
				ctpp.useUnknownCharacterModel = true;
			}
			if (argMap.Contains("-lengthPenalty"))
			{
				ctpp.lengthPenalty = double.Parse((argMap["-lengthPenalty"])[0]);
			}
			if (argMap.Contains("-penaltyType"))
			{
				ctpp.penaltyType = System.Convert.ToInt32((argMap["-penaltyType"])[0]);
			}
			if (argMap.Contains("-lex"))
			{
				string[] lexArgs = (argMap["-lex"]);
				if (lexArgs.Length > 1)
				{
					IIndex<string> wordIndex = new HashIndex<string>();
					IIndex<string> tagIndex = new HashIndex<string>();
					lex = ctpp.Lex(op, wordIndex, tagIndex);
					MemoryTreebank rawTrainTreebank = op.tlpParams.MemoryTreebank();
					IFileFilter trainFilt = new NumberRangesFileFilter(lexArgs[1], false);
					rawTrainTreebank.LoadPath(new File(lexArgs[0]), trainFilt);
					log.Info("Done reading trees.");
					MemoryTreebank trainTreebank;
					if (argMap.Contains("-annotate"))
					{
						trainTreebank = new MemoryTreebank();
						TreeAnnotator annotator = new TreeAnnotator(ctpp.HeadFinder(), ctpp, op);
						foreach (Tree tree in rawTrainTreebank)
						{
							tree = annotator.TransformTree(tree);
							trainTreebank.Add(tree);
						}
						log.Info("Done annotating trees.");
					}
					else
					{
						trainTreebank = rawTrainTreebank;
					}
					lex.InitializeTraining(trainTreebank.Count);
					lex.Train(trainTreebank);
					lex.FinishTraining();
					log.Info("Done training lexicon.");
					if (lexArgs.Length == 3)
					{
						string filename = lexArgs.Length == 3 ? lexArgs[2] : "parsers/chineseCharLex.ser.gz";
						log.Info("Writing lexicon in serialized format to file " + filename + " ");
						System.Console.Error.Flush();
						ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
						@out.WriteObject(lex);
						@out.Close();
						log.Info("done.");
					}
				}
				else
				{
					string lexFile = lexArgs.Length == 1 ? lexArgs[0] : "parsers/chineseCharLex.ser.gz";
					log.Info("Reading Lexicon from file " + lexFile);
					ObjectInputStream @in = IOUtils.ReadStreamFromString(lexFile);
					try
					{
						lex = (ILexicon)@in.ReadObject();
					}
					catch (TypeLoadException)
					{
						throw new Exception("Bad serialized file: " + lexFile);
					}
					@in.Close();
				}
			}
			if (argMap.Contains("-test"))
			{
				bool segmentWords = ctpp.segment;
				bool parse = lp != null;
				System.Diagnostics.Debug.Assert((parse || segmentWords));
				//      WordCatConstituent.collinizeWords = argMap.containsKey("-collinizeWords");
				//      WordCatConstituent.collinizeTags = argMap.containsKey("-collinizeTags");
				IWordSegmenter seg = null;
				if (segmentWords)
				{
					seg = (IWordSegmenter)lex;
				}
				string[] testArgs = (argMap["-test"]);
				MemoryTreebank testTreebank = op.tlpParams.MemoryTreebank();
				IFileFilter testFilt = new NumberRangesFileFilter(testArgs[1], false);
				testTreebank.LoadPath(new File(testArgs[0]), testFilt);
				ITreeTransformer subcategoryStripper = op.tlpParams.SubcategoryStripper();
				ITreeTransformer collinizer = ctpp.Collinizer();
				WordCatEquivalenceClasser eqclass = new WordCatEquivalenceClasser();
				WordCatEqualityChecker eqcheck = new WordCatEqualityChecker();
				EquivalenceClassEval basicEval = new EquivalenceClassEval(eqclass, eqcheck, "basic");
				EquivalenceClassEval collinsEval = new EquivalenceClassEval(eqclass, eqcheck, "collinized");
				IList<string> evalTypes = new List<string>(3);
				bool goodPOS = false;
				if (segmentWords)
				{
					evalTypes.Add(WordCatConstituent.wordType);
					if (ctpp.segmentMarkov && !parse)
					{
						evalTypes.Add(WordCatConstituent.tagType);
						goodPOS = true;
					}
				}
				if (parse)
				{
					evalTypes.Add(WordCatConstituent.tagType);
					evalTypes.Add(WordCatConstituent.catType);
					if (combo)
					{
						evalTypes.Add(WordCatConstituent.wordType);
						goodPOS = true;
					}
				}
				TreeToBracketProcessor proc = new TreeToBracketProcessor(evalTypes);
				log.Info("Testing...");
				foreach (Tree goldTop in testTreebank)
				{
					Tree gold = goldTop.FirstChild();
					IList<IHasWord> goldSentence = gold.YieldHasWord();
					if (goldSentence.Count > maxLength)
					{
						log.Info("Skipping sentence; too long: " + goldSentence.Count);
						continue;
					}
					else
					{
						log.Info("Processing sentence; length: " + goldSentence.Count);
					}
					IList<IHasWord> s;
					if (segmentWords)
					{
						StringBuilder goldCharBuf = new StringBuilder();
						foreach (IHasWord aGoldSentence in goldSentence)
						{
							StringLabel word = (StringLabel)aGoldSentence;
							goldCharBuf.Append(word.Value());
						}
						string goldChars = goldCharBuf.ToString();
						s = seg.Segment(goldChars);
					}
					else
					{
						s = goldSentence;
					}
					Tree tree;
					if (parse)
					{
						tree = lp.ParseTree(s);
						if (tree == null)
						{
							throw new Exception("PARSER RETURNED NULL!!!");
						}
					}
					else
					{
						tree = Edu.Stanford.Nlp.Trees.Trees.ToFlatTree(s);
						tree = subcategoryStripper.TransformTree(tree);
					}
					if (pw != null)
					{
						if (parse)
						{
							tree.PennPrint(pw);
						}
						else
						{
							IEnumerator sentIter = s.GetEnumerator();
							for (; ; )
							{
								Word word = (Word)sentIter.Current;
								pw.Print(word.Word());
								if (sentIter.MoveNext())
								{
									pw.Print(" ");
								}
								else
								{
									break;
								}
							}
						}
						pw.Println();
					}
					if (eval)
					{
						ICollection ourBrackets;
						ICollection goldBrackets;
						ourBrackets = proc.AllBrackets(tree);
						goldBrackets = proc.AllBrackets(gold);
						if (goodPOS)
						{
							Sharpen.Collections.AddAll(ourBrackets, TreeToBracketProcessor.CommonWordTagTypeBrackets(tree, gold));
							Sharpen.Collections.AddAll(goldBrackets, TreeToBracketProcessor.CommonWordTagTypeBrackets(gold, tree));
						}
						basicEval.Eval(ourBrackets, goldBrackets);
						System.Console.Out.WriteLine("\nScores:");
						basicEval.DisplayLast();
						Tree collinsTree = collinizer.TransformTree(tree);
						Tree collinsGold = collinizer.TransformTree(gold);
						ourBrackets = proc.AllBrackets(collinsTree);
						goldBrackets = proc.AllBrackets(collinsGold);
						if (goodPOS)
						{
							Sharpen.Collections.AddAll(ourBrackets, TreeToBracketProcessor.CommonWordTagTypeBrackets(collinsTree, collinsGold));
							Sharpen.Collections.AddAll(goldBrackets, TreeToBracketProcessor.CommonWordTagTypeBrackets(collinsGold, collinsTree));
						}
						collinsEval.Eval(ourBrackets, goldBrackets);
						System.Console.Out.WriteLine("\nCollinized scores:");
						collinsEval.DisplayLast();
						System.Console.Out.WriteLine();
					}
				}
				if (eval)
				{
					basicEval.Display();
					System.Console.Out.WriteLine();
					collinsEval.Display();
				}
			}
		}
	}
}
