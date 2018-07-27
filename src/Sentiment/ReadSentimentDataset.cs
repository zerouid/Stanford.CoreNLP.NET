using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>Reads the sentiment dataset and writes it to the appropriate files.</summary>
	/// <author>John Bauer</author>
	public class ReadSentimentDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.ReadSentimentDataset));

		internal static readonly IFunction<Tree, string> TransformTreeToWord = null;

		internal static readonly IFunction<string, string> TransformParens = null;

		internal static readonly TregexPattern[] tregexPatterns = new TregexPattern[] { TregexPattern.Compile("__=single <1 (__ < /^-LRB-$/) <2 (__ <... { (__ < /^[a-zA-Z]$/=letter) ; (__ < /^-RRB-$/) }) > (__ <2 =single <1 (__=useless <<- (__=word !< __)))"
			), TregexPattern.Compile("__=single <1 (__ < /^-LRB-$/) <2 (__ <... { (__ < /^[aA]$/=letter) ; (__ < /^-RRB-$/) }) > (__ <1 =single <2 (__=useless <<, /^n$/=word))"), TregexPattern.Compile("__=single <1 (__ < /^-LRB-$/) <2 (__=A <... { (__ < /^[aA]$/=letter) ; (__=paren < /^-RRB-$/) })"
			), TregexPattern.Compile("__ <1 (__ <<- (/^(?i:provide)$/=provide !<__)) <2 (__ <<, (__=s > __=useless <... { (__ <: -LRB-) ; (__ <1 (__ <: s)) } ))"), TregexPattern.Compile("__=single <1 (__ < /^-LRB-$/) <2 (__ <... { (__ < /^[a-zA-Z]$/=letter) ; (__ < /^-RRB-$/) }) > (__ <1 =single <2 (__=useless <<, (__=word !< __)))"
			), TregexPattern.Compile("-LRB-=lrb !, __ : (__=ltop > __ <<, =lrb <<- (-RRB-=rrb > (__ > __=rtop)) !<< (-RRB- !== =rrb))"), TregexPattern.Compile("__=top <1 (__=f1 < f) <2 (__=f2 <... { (__ < /^[*\\\\]+$/) ; (__ < ed) })"), TregexPattern.Compile
			("__=top <1 (__=f1 <1 (__ < don=do) <2 (__ < /^[\']$/=apos)) <2 (__=wrong < t)"), TregexPattern.Compile("-LRB-=lrb !, __ .. (-RRB-=rrb !< __ !.. -RRB-)"), TregexPattern.Compile("-LRB-=lrb . and|Haneke|is|Evans|Harmon|Harris|its|it|Aniston|headbanger|Testud|but|frames|yet|Denis|DeNiro|sinks|screenwriter|Cho|meditation|Watts|that|the|this|Madonna|Ahola|Franco|Hopkins|Crudup|writer-director|Diggs|very|Crane|Frei|Reno|Jones|Quills|Bobby|Hill|Kim|subjects|Wang|Jaglom|Vega|Sabara|Sade|Goldbacher|too|being|opening=last : (=last . -RRB-=rrb)"
			), TregexPattern.Compile("-LRB-=lrb . (__=n1 !< __ . (__=n2 !< __ . -RRB-=rrb)) : (=n1 (== Besson|Kissinger|Godard|Seagal|jaglon|It|it|Tsai|Nelson|Rifkan|Shakespeare|Solondz|Madonna|Herzog|Witherspoon|Woo|Eyre|there|Moore|Ricci|Seinfeld . (=n2 == /^\'s$/)) | (== Denis|Skins|Spears|Assayas . (=n2 == /^\'$/)) | (== Je-Gyu . (=n2 == is)) | (== the . (=n2 == leads|film|story|characters)) | (== Monsoon . (=n2 == Wedding)) | (== De . (=n2 == Niro)) | (== Roman . (=n2 == Coppola)) | (== than . (=n2 == Leon)) | (==Colgate . (=n2 == /^U.$/)) | (== teen . (=n2 == comedy)) | (== a . (=n2 == remake)) | (== Powerpuff . (=n2 == Girls)) | (== Woody . (=n2 == Allen)))"
			), TregexPattern.Compile("-LRB-=lrb . (__=n1 !< __ . (__=n2 !< __ . (__=n3 !< __ . -RRB-=rrb))) : (=n1 [ (== the . (=n2 == characters . (=n3 == /^\'$/))) | (== the . (=n2 == movie . (=n3 == /^\'s$/))) | (== of . (=n2 == middle-aged . (=n3 == romance))) | (== Jack . (=n2 == Nicholson . (=n3 == /^\'s$/))) | (== De . (=n2 == Palma . (=n3 == /^\'s$/))) | (== Clara . (=n2 == and . (=n3 == Paul))) | (== Sex . (=n2 == and . (=n3 == Lucía))) ])"
			), TregexPattern.Compile("/^401$/ > (__ > __=top)"), TregexPattern.Compile("by . (all > (__=all > __=allgp) . (means > (__=means > __=meansgp))) : (=allgp !== =meansgp)"), TregexPattern.Compile("/^(?:20th|21st)$/ . Century=century"), TregexPattern
			.Compile("__ <: (__=unitary < __)"), TregexPattern.Compile("/^[1]$/=label <: /^(?i:protagonist)$/") };

		internal static readonly TsurgeonPattern[] tsurgeonPatterns = new TsurgeonPattern[] { Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel word /^.*$/={word}={letter}/] [prune single] [excise useless useless]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon
			.ParseOperation("[relabel word /^.*$/={letter}n/] [prune single] [excise useless useless]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[excise single A] [prune paren]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.
			ParseOperation("[relabel provide /^.*$/={provide}s/] [prune s] [excise useless useless]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel word /^.*$/={letter}={word}/] [prune single] [excise useless useless]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon
			.ParseOperation("[prune lrb] [prune rrb] [excise ltop ltop] [excise rtop rtop]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace top (0 fucked)"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[prune wrong] [relabel do do] [relabel apos /^.*$/n={apos}t/] [excise top top]"
			), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[prune rrb] [prune lrb]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[prune rrb] [prune lrb]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation
			("[prune rrb] [prune lrb]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[prune rrb] [prune lrb]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace top (2 (2 401k) (2 statement))"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon
			.ParseOperation("[move means $- all] [excise meansgp meansgp] [createSubtree 2 all means]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel century century"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation
			("[excise unitary unitary]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel label /^.*$/2/") };

		static ReadSentimentDataset()
		{
			// A bunch of trees have some funky tokenization which we can
			// somewhat correct using these tregex / tsurgeon expressions.
			// uncensor "fucked"
			// fix don ' t
			// parens at the start of a sentence - always appears wrong
			// parens with a single word that we can drop
			// parens with two word expressions
			// parens with three word expressions
			// only one of these, so can be very general
			// 20th century, 21st century
			// Fix any stranded unitary nodes
			// relabel some nodes where punctuation changes the score for no apparent reason
			// TregexPattern.compile("__=node <2 (__ < /^[!.?,;]$/) !<1 ~node <1 __=child > ~child"),
			// TODO: relabel words in some less expensive way?
			// Note: the next couple leave unitary nodes, so we then fix them at the end
			// Fix any stranded unitary nodes
			//Tsurgeon.parseOperation("relabel node /^.*$/={child}/"),
			if (tregexPatterns.Length != tsurgeonPatterns.Length)
			{
				throw new Exception("Expected the same number of tregex and tsurgeon when initializing");
			}
		}

		private ReadSentimentDataset()
		{
		}

		// static class
		public static Tree ConvertTree(IList<int> parentPointers, IList<string> sentence, IDictionary<IList<string>, int> phraseIds, IDictionary<int, double> sentimentScores, PTBEscapingProcessor escaper, int numClasses)
		{
			int maxNode = 0;
			foreach (int parent in parentPointers)
			{
				maxNode = Math.Max(maxNode, parent);
			}
			Tree[] subtrees = new Tree[maxNode + 1];
			for (int i = 0; i < sentence.Count; ++i)
			{
				CoreLabel word = new CoreLabel();
				word.SetValue(sentence[i]);
				Tree leaf = new LabeledScoredTreeNode(word);
				subtrees[i] = new LabeledScoredTreeNode(new CoreLabel());
				subtrees[i].AddChild(leaf);
			}
			for (int i_1 = sentence.Count; i_1 <= maxNode; ++i_1)
			{
				subtrees[i_1] = new LabeledScoredTreeNode(new CoreLabel());
			}
			bool[] connected = new bool[maxNode + 1];
			Tree root = null;
			for (int index = 0; index < parentPointers.Count; ++index)
			{
				if (parentPointers[index] == -1)
				{
					if (root != null)
					{
						throw new Exception("Found two roots for sentence " + sentence);
					}
					root = subtrees[index];
				}
				else
				{
					// Walk up the tree structure to make sure that leftmost
					// phrases are added first.  Otherwise, if the numbers are
					// inverted, we might get the right phrase added to a parent
					// first, resulting in "case zero in this", for example,
					// instead of "in this case zero"
					// Note that because we keep track of which ones are already
					// connected, we process this at most once per parent, so the
					// overall construction time is still efficient.
					Connect(parentPointers, subtrees, connected, index);
				}
			}
			for (int i_2 = 0; i_2 <= maxNode; ++i_2)
			{
				IList<Tree> leaves = subtrees[i_2].GetLeaves();
				IList<string> words = CollectionUtils.TransformAsList(leaves, TransformTreeToWord);
				// First we look for a copy of the phrase with -LRB- -RRB-
				// instead of ().  The sentiment trees sometimes have both, and
				// the escaped versions seem to have more reasonable scores.
				// If a particular phrase doesn't have -LRB- -RRB- we fall back
				// to the unescaped versions.
				int phraseId = phraseIds[CollectionUtils.TransformAsList(words, TransformParens)];
				if (phraseId == null)
				{
					phraseId = phraseIds[words];
				}
				if (phraseId == null)
				{
					throw new Exception("Could not find phrase id for phrase " + sentence);
				}
				// TODO: should we make this an option?  Perhaps we want cases
				// where the trees have the phrase id and not their class
				double score = sentimentScores[phraseId];
				if (score == null)
				{
					throw new Exception("Could not find sentiment score for phrase id " + phraseId);
				}
				// TODO: make this a numClasses option
				int classLabel = Math.Round((float)Math.Floor(score * (float)numClasses));
				if (classLabel > numClasses - 1)
				{
					classLabel = numClasses - 1;
				}
				subtrees[i_2].Label().SetValue(int.ToString(classLabel));
			}
			for (int i_3 = 0; i_3 < sentence.Count; ++i_3)
			{
				Tree leaf = subtrees[i_3].Children()[0];
				leaf.Label().SetValue(escaper.EscapeString(leaf.Label().Value()));
			}
			for (int i_4 = 0; i_4 < tregexPatterns.Length; ++i_4)
			{
				root = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(tregexPatterns[i_4], tsurgeonPatterns[i_4], root);
			}
			return root;
		}

		private static void Connect(IList<int> parentPointers, Tree[] subtrees, bool[] connected, int index)
		{
			if (connected[index])
			{
				return;
			}
			if (parentPointers[index] < 0)
			{
				return;
			}
			subtrees[parentPointers[index]].AddChild(subtrees[index]);
			connected[index] = true;
			Connect(parentPointers, subtrees, connected, parentPointers[index]);
		}

		private static void WriteTrees(string filename, IList<Tree> trees, IList<int> treeIds)
		{
			try
			{
				FileOutputStream fos = new FileOutputStream(filename);
				BufferedWriter bout = new BufferedWriter(new OutputStreamWriter(fos));
				foreach (int id in treeIds)
				{
					bout.Write(trees[id].ToString());
					bout.Write("\n");
				}
				bout.Flush();
				fos.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// This program converts the format of the Sentiment data set
		/// prepared by Richard, Jean, etc.
		/// </summary>
		/// <remarks>
		/// This program converts the format of the Sentiment data set
		/// prepared by Richard, Jean, etc. into trees readable with the
		/// normal TreeReaders.
		/// <br />
		/// An example command line is
		/// <br />
		/// <code>java edu.stanford.nlp.sentiment.ReadSentimentDataset -dictionary stanfordSentimentTreebank/dictionary.txt -sentiment stanfordSentimentTreebank/sentiment_labels.txt -tokens stanfordSentimentTreebank/SOStr.txt -parse stanfordSentimentTreebank/STree.txt  -split stanfordSentimentTreebank/datasetSplit.txt  -train train.txt -dev dev.txt -test test.txt</code>
		/// <br />
		/// The arguments are as follows: <br />
		/// <code>-dictionary</code>, <code>-sentiment</code>,
		/// <code>-tokens</code>, <code>-parse</code>, <code>-split</code>
		/// Path to the corresponding files from the dataset <br />
		/// <code>-train</code>, <code>-dev</code>, <code>-test</code>
		/// Paths for saving the corresponding output files <br />
		/// Each of these arguments is required.
		/// <br />
		/// Macro arguments exist in -inputDir and -outputDir, so you can for example run <br />
		/// <code>java edu.stanford.nlp.sentiment.ReadSentimentDataset -inputDir ../data/sentiment/stanfordSentimentTreebank  -outputDir .</code>
		/// </remarks>
		public static void Main(string[] args)
		{
			string dictionaryFilename = null;
			string sentimentFilename = null;
			string tokensFilename = null;
			string parseFilename = null;
			string splitFilename = null;
			string trainFilename = null;
			string devFilename = null;
			string testFilename = null;
			int numClasses = 5;
			int argIndex = 0;
			while (argIndex < args.Length)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-dictionary"))
				{
					dictionaryFilename = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-sentiment"))
					{
						sentimentFilename = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tokens"))
						{
							tokensFilename = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parse"))
							{
								parseFilename = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-split"))
								{
									splitFilename = args[argIndex + 1];
									argIndex += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-inputDir") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-inputDirectory"))
									{
										dictionaryFilename = args[argIndex + 1] + "/dictionary.txt";
										sentimentFilename = args[argIndex + 1] + "/sentiment_labels.txt";
										tokensFilename = args[argIndex + 1] + "/SOStr.txt";
										parseFilename = args[argIndex + 1] + "/STree.txt";
										splitFilename = args[argIndex + 1] + "/datasetSplit.txt";
										argIndex += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train"))
										{
											trainFilename = args[argIndex + 1];
											argIndex += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-dev"))
											{
												devFilename = args[argIndex + 1];
												argIndex += 2;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-test"))
												{
													testFilename = args[argIndex + 1];
													argIndex += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-outputDir") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-outputDirectory"))
													{
														trainFilename = args[argIndex + 1] + "/train.txt";
														devFilename = args[argIndex + 1] + "/dev.txt";
														testFilename = args[argIndex + 1] + "/test.txt";
														argIndex += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-numClasses"))
														{
															numClasses = System.Convert.ToInt32(args[argIndex + 1]);
															argIndex += 2;
														}
														else
														{
															log.Info("Unknown argument " + args[argIndex]);
															System.Environment.Exit(2);
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
			// Sentence file is formatted
			//   w1|w2|w3...
			IList<IList<string>> sentences = Generics.NewArrayList();
			foreach (string line in IOUtils.ReadLines(tokensFilename, "utf-8"))
			{
				string[] sentence = line.Split("\\|");
				sentences.Add(Arrays.AsList(sentence));
			}
			// Split and read the phrase ids file.  This file is in the format
			//   w1 w2 w3 ... | id
			IDictionary<IList<string>, int> phraseIds = Generics.NewHashMap();
			foreach (string line_1 in IOUtils.ReadLines(dictionaryFilename, "utf-8"))
			{
				string[] pieces = line_1.Split("\\|");
				string[] sentence = pieces[0].Split(" ");
				int id = int.Parse(pieces[1]);
				phraseIds[Arrays.AsList(sentence)] = id;
			}
			// Split and read the sentiment scores file.  Each line of this
			// file is of the format:
			//   phrasenum | score
			IDictionary<int, double> sentimentScores = Generics.NewHashMap();
			foreach (string line_2 in IOUtils.ReadLines(sentimentFilename, "utf-8"))
			{
				if (line_2.StartsWith("phrase"))
				{
					continue;
				}
				string[] pieces = line_2.Split("\\|");
				int id = int.Parse(pieces[0]);
				double score = double.ValueOf(pieces[1]);
				sentimentScores[id] = score;
			}
			// Read lines from the tree structure file.  This is a file of parent pointers for each tree.
			int index = 0;
			PTBEscapingProcessor escaper = new PTBEscapingProcessor();
			IList<Tree> trees = Generics.NewArrayList();
			foreach (string line_3 in IOUtils.ReadLines(parseFilename, "utf-8"))
			{
				string[] pieces = line_3.Split("\\|");
				IList<int> parentPointers = CollectionUtils.TransformAsList(Arrays.AsList(pieces), null);
				Tree tree = ConvertTree(parentPointers, sentences[index], phraseIds, sentimentScores, escaper, numClasses);
				++index;
				trees.Add(tree);
			}
			IDictionary<int, IList<int>> splits = Generics.NewHashMap();
			splits[1] = Generics.NewArrayList<int>();
			splits[2] = Generics.NewArrayList<int>();
			splits[3] = Generics.NewArrayList<int>();
			foreach (string line_4 in IOUtils.ReadLines(splitFilename, "utf-8"))
			{
				if (line_4.StartsWith("sentence_index"))
				{
					continue;
				}
				string[] pieces = line_4.Split(",");
				int treeId = int.Parse(pieces[0]) - 1;
				int fileId = int.Parse(pieces[1]);
				splits[fileId].Add(treeId);
			}
			WriteTrees(trainFilename, trees, splits[1]);
			WriteTrees(testFilename, trees, splits[2]);
			WriteTrees(devFilename, trees, splits[3]);
		}
	}
}
