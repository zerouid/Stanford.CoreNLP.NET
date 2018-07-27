using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Generates trees based on simple grammars.</summary>
	/// <remarks>
	/// Generates trees based on simple grammars.
	/// <br />
	/// To run this script, run with an input file, an output file, and a
	/// number of trees specified.
	/// <br />
	/// A more complete example is as following:
	/// <code><pre>
	/// # This grammar produces trees that look like
	/// # (S A (V B C)) -&gt; (S X (V Y Z))
	/// # (S D E F) -&gt; (S X Y Z)
	/// nonterminals
	/// ROOT S
	/// S A V
	/// V B C
	/// S D E F
	/// terminals
	/// A avocet albatross artichoke
	/// B barium baseball brontosaurus
	/// C canary cardinal crow
	/// D delphinium dolphin dragon
	/// E egret emu estuary
	/// F finch flock finglonger
	/// tsurgeon
	/// S &lt;&lt; /A|D/=n1 &lt;&lt; /B|E/=n2 &lt;&lt; /C|F/=n3
	/// relabel n1 X
	/// relabel n2 Y
	/// relabel n3 Z
	/// </pre></code>
	/// <br />
	/// You then run the problem with
	/// <br />
	/// <code>java edu.stanford.nlp.trees.GenerateTrees input.txt output.txt 100</code>
	/// </remarks>
	/// <author>John Bauer</author>
	public class GenerateTrees
	{
		internal enum Section
		{
			Terminals,
			Nonterminals,
			Tsurgeon
		}

		internal IDictionary<string, ICounter<IList<string>>> nonTerminals = Generics.NewHashMap();

		internal IDictionary<string, ICounter<string>> terminals = Generics.NewHashMap();

		internal IList<Pair<TregexPattern, TsurgeonPattern>> tsurgeons = new List<Pair<TregexPattern, TsurgeonPattern>>();

		internal Random random = new Random();

		internal LabeledScoredTreeFactory tf = new LabeledScoredTreeFactory();

		internal TregexPatternCompiler compiler = new TregexPatternCompiler();

		internal TreePrint tp = new TreePrint("penn");

		public virtual void ReadGrammar(string filename)
		{
			try
			{
				FileReader fin = new FileReader(filename);
				BufferedReader bin = new BufferedReader(fin);
				ReadGrammar(bin);
				bin.Close();
				fin.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void ReadGrammar(BufferedReader bin)
		{
			try
			{
				string line;
				GenerateTrees.Section section = GenerateTrees.Section.Terminals;
				while ((line = bin.ReadLine()) != null)
				{
					line = line.Trim();
					if (line.Equals(string.Empty))
					{
						continue;
					}
					if (line.Length > 0 && line[0] == '#')
					{
						// skip comments
						continue;
					}
					try
					{
						GenerateTrees.Section newSection = GenerateTrees.Section.ValueOf(line.ToUpper());
						section = newSection;
						if (section == GenerateTrees.Section.Tsurgeon)
						{
							// this will tregex pattern until it has eaten a blank
							// line, then read tsurgeon until it has eaten another
							// blank line.
							Pair<TregexPattern, TsurgeonPattern> operation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.GetOperationFromReader(bin, compiler);
							tsurgeons.Add(operation);
						}
						continue;
					}
					catch (ArgumentException)
					{
					}
					// never mind, not an enum
					string[] pieces = line.Split(" +");
					switch (section)
					{
						case GenerateTrees.Section.Tsurgeon:
						{
							throw new Exception("Found a non-empty line in a tsurgeon section after reading the operation");
						}

						case GenerateTrees.Section.Terminals:
						{
							ICounter<string> productions = terminals[pieces[0]];
							if (productions == null)
							{
								productions = new ClassicCounter<string>();
								terminals[pieces[0]] = productions;
							}
							for (int i = 1; i < pieces.Length; ++i)
							{
								productions.IncrementCount(pieces[i]);
							}
							break;
						}

						case GenerateTrees.Section.Nonterminals:
						{
							ICounter<IList<string>> productions = nonTerminals[pieces[0]];
							if (productions == null)
							{
								productions = new ClassicCounter<IList<string>>();
								nonTerminals[pieces[0]] = productions;
							}
							string[] sublist = Arrays.CopyOfRange(pieces, 1, pieces.Length);
							productions.IncrementCount(Arrays.AsList(sublist));
							break;
						}
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void ProduceTrees(string filename, int numTrees)
		{
			try
			{
				FileWriter fout = new FileWriter(filename);
				BufferedWriter bout = new BufferedWriter(fout);
				PrintWriter pout = new PrintWriter(bout);
				ProduceTrees(pout, numTrees);
				pout.Close();
				bout.Close();
				fout.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void ProduceTrees(PrintWriter pout, int numTrees)
		{
			for (int i = 0; i < numTrees; ++i)
			{
				Tree tree = ProduceTree("ROOT");
				Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(tsurgeons, tree);
				tp.PrintTree(tree, pout);
			}
		}

		public virtual Tree ProduceTree(string state)
		{
			ICounter<string> terminal = terminals[state];
			if (terminal != null)
			{
				// found a terminal production.  make a leaf with a randomly
				// chosen expansion and make a preterminal with that one leaf
				// as a child.
				string label = Counters.Sample(terminal, random);
				Tree child = tf.NewLeaf(label);
				IList<Tree> children = Java.Util.Collections.SingletonList(child);
				Tree root = tf.NewTreeNode(state, children);
				return root;
			}
			ICounter<IList<string>> nonTerminal = nonTerminals[state];
			if (nonTerminal != null)
			{
				// found a nonterminal production.  produce a list of
				// recursive expansions, then attach them all to a node with
				// the expected state
				IList<string> labels = Counters.Sample(nonTerminal, random);
				IList<Tree> children = new List<Tree>();
				foreach (string childLabel in labels)
				{
					children.Add(ProduceTree(childLabel));
				}
				Tree root = tf.NewTreeNode(state, children);
				return root;
			}
			throw new Exception("Unknown state " + state);
		}

		public static void Help()
		{
			System.Console.Out.WriteLine("Command line should be ");
			System.Console.Out.WriteLine("  edu.stanford.nlp.trees.GenerateTrees <input> <output> <numtrees>");
		}

		public static void Main(string[] args)
		{
			if (args.Length == 0 || args[0].Equals("-h"))
			{
				Help();
				System.Environment.Exit(0);
			}
			GenerateTrees grammar = new GenerateTrees();
			grammar.ReadGrammar(args[0]);
			int numTrees = int.Parse(args[2]);
			grammar.ProduceTrees(args[1], numTrees);
		}
	}
}
