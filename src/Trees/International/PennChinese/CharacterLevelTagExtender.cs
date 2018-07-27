using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A transformer to extend tags down to the level of individual characters.</summary>
	/// <remarks>
	/// A transformer to extend tags down to the level of individual characters.
	/// Each word preterminal is split into new preterminals for each character
	/// with tags corresponding to the original preterminal tag plus a suffix
	/// depending on the position of the character in the word: _S for single-char
	/// words, _B for first char of multi-char words, _M for middle chars and _E
	/// for final chars.
	/// <p/>
	/// This is used in combining Chinese parsing and word segmentation using the
	/// method of Luo '03.
	/// <p/>
	/// Note: it implements TreeTransformer because we might want to do away
	/// with TreeNormalizers in favor of TreeTransformers
	/// </remarks>
	/// <author>Galen Andrew (galand@cs.stanford.edu) Date: May 13, 2004</author>
	[System.Serializable]
	public class CharacterLevelTagExtender : BobChrisTreeNormalizer, ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.CharacterLevelTagExtender));

		private const long serialVersionUID = 7893996593626523700L;

		private const bool useTwoCharTags = false;

		public CharacterLevelTagExtender()
			: base(new ChineseTreebankLanguagePack())
		{
		}

		public CharacterLevelTagExtender(ITreebankLanguagePack tlp)
			: base(tlp)
		{
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			return TransformTree(base.NormalizeWholeTree(tree, tf));
		}

		//  static Set preterminals = new HashSet();
		public override Tree TransformTree(Tree tree)
		{
			ITreeFactory tf = tree.TreeFactory();
			string tag = tree.Label().Value();
			if (tree.IsPreTerminal())
			{
				string word = tree.FirstChild().Label().Value();
				IList<Tree> newPreterms = new List<Tree>();
				for (int i = 0; i < size; i++)
				{
					string singleCharLabel = new string(new char[] { word[i] });
					Tree newLeaf = tf.NewLeaf(singleCharLabel);
					string suffix;
					if (word.Length == 1)
					{
						suffix = "_S";
					}
					else
					{
						if (i == 0)
						{
							suffix = "_B";
						}
						else
						{
							if (i == word.Length - 1)
							{
								suffix = "_E";
							}
							else
							{
								suffix = "_M";
							}
						}
					}
					newPreterms.Add(tf.NewTreeNode(tag + suffix, Java.Util.Collections.SingletonList<Tree>(newLeaf)));
				}
				return tf.NewTreeNode(tag, newPreterms);
			}
			else
			{
				IList<Tree> newChildren = new List<Tree>();
				for (int i = 0; i < tree.Children().Length; i++)
				{
					Tree child = tree.Children()[i];
					newChildren.Add(TransformTree(child));
				}
				return tf.NewTreeNode(tag, newChildren);
			}
		}

		public virtual Tree UntransformTree(Tree tree)
		{
			ITreeFactory tf = tree.TreeFactory();
			if (tree.IsPrePreTerminal())
			{
				if (tree.FirstChild().Label().Value().Matches(".*_."))
				{
					StringBuilder word = new StringBuilder();
					for (int i = 0; i < tree.Children().Length; i++)
					{
						Tree child = tree.Children()[i];
						word.Append(child.FirstChild().Label().Value());
					}
					Tree newChild = tf.NewLeaf(word.ToString());
					tree.SetChildren(Java.Util.Collections.SingletonList(newChild));
				}
			}
			else
			{
				for (int i = 0; i < tree.Children().Length; i++)
				{
					Tree child = tree.Children()[i];
					UntransformTree(child);
				}
			}
			return tree;
		}

		private static void TestTransAndUntrans(Edu.Stanford.Nlp.Trees.International.Pennchinese.CharacterLevelTagExtender e, Treebank tb, PrintWriter pw)
		{
			foreach (Tree tree in tb)
			{
				Tree oldTree = tree.TreeSkeletonCopy();
				e.TransformTree(tree);
				e.UntransformTree(tree);
				if (!tree.Equals(oldTree))
				{
					pw.Println("NOT EQUAL AFTER UNTRANSFORMATION!!!");
					pw.Println();
					oldTree.PennPrint(pw);
					pw.Println();
					tree.PennPrint(pw);
					pw.Println("------------------");
				}
			}
		}

		/// <summary>for testing -- CURRENTLY BROKEN!!!</summary>
		/// <param name="args">input dir and output filename</param>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				throw new Exception("args: treebankPath trainNums testNums");
			}
			ChineseTreebankParserParams ctpp = new ChineseTreebankParserParams();
			ctpp.charTags = true;
			// TODO: these options are getting clobbered by reading in the
			// parser object (unless it's a text file parser?)
			Options op = new Options(ctpp);
			op.doDep = false;
			op.testOptions.maxLength = 90;
			LexicalizedParser lp;
			try
			{
				IFileFilter trainFilt = new NumberRangesFileFilter(args[1], false);
				lp = LexicalizedParser.TrainFromTreebank(args[0], trainFilt, op);
				try
				{
					string filename = "chineseCharTagPCFG.ser.gz";
					log.Info("Writing parser in serialized format to file " + filename + " ");
					System.Console.Error.Flush();
					ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
					@out.WriteObject(lp);
					@out.Close();
					log.Info("done.");
				}
				catch (IOException ioe)
				{
					Sharpen.Runtime.PrintStackTrace(ioe);
				}
			}
			catch (ArgumentException)
			{
				lp = LexicalizedParser.LoadModel(args[1], op);
			}
			IFileFilter testFilt = new NumberRangesFileFilter(args[2], false);
			MemoryTreebank testTreebank = ctpp.MemoryTreebank();
			testTreebank.LoadPath(new File(args[0]), testFilt);
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(new FileOutputStream("out.chi"), "GB18030"), true);
			WordCatEquivalenceClasser eqclass = new WordCatEquivalenceClasser();
			WordCatEqualityChecker eqcheck = new WordCatEqualityChecker();
			EquivalenceClassEval eval = new EquivalenceClassEval(eqclass, eqcheck);
			//    System.out.println("Preterminals:" + preterminals);
			System.Console.Out.WriteLine("Testing...");
			foreach (Tree gold in testTreebank)
			{
				Tree tree;
				try
				{
					tree = lp.ParseTree(gold.YieldHasWord());
					if (tree == null)
					{
						System.Console.Out.WriteLine("Failed to parse " + gold.YieldHasWord());
						continue;
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					continue;
				}
				gold = gold.FirstChild();
				pw.Println(SentenceUtils.ListToString(gold.PreTerminalYield()));
				pw.Println(SentenceUtils.ListToString(gold.Yield()));
				gold.PennPrint(pw);
				pw.Println(tree.PreTerminalYield());
				pw.Println(tree.Yield());
				tree.PennPrint(pw);
				//      Collection allBrackets = WordCatConstituent.allBrackets(tree);
				//      Collection goldBrackets = WordCatConstituent.allBrackets(gold);
				//      eval.eval(allBrackets, goldBrackets);
				eval.DisplayLast();
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine();
			eval.Display();
		}
	}
}
