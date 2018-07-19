using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	public abstract class GrammarCompactor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.GrammarCompactor));

		internal ICollection<TransducerGraph> compactedGraphs;

		public static readonly object RawCounts = new object();

		public static readonly object NormalizedLogProbabilities = new object();

		public object outputType = RawCounts;

		protected internal IIndex<string> stateIndex;

		protected internal IIndex<string> newStateIndex;

		protected internal Distribution<string> inputPrior;

		private const string End = "END";

		private const string Epsilon = "EPSILON";

		protected internal bool verbose = false;

		protected internal readonly Options op;

		public GrammarCompactor(Options op)
		{
			// so that the grammar remembers its graphs after compacting them
			// default value
			// String rawBaseDir = "raw";
			// String compactedBaseDir = "compacted";
			// boolean writeToFile = false;
			this.op = op;
		}

		protected internal abstract TransducerGraph DoCompaction(TransducerGraph graph, IList<IList<string>> trainPaths, IList<IList<string>> testPaths);

		public virtual Triple<IIndex<string>, UnaryGrammar, BinaryGrammar> CompactGrammar(Pair<UnaryGrammar, BinaryGrammar> grammar, IIndex<string> originalStateIndex)
		{
			return CompactGrammar(grammar, Generics.NewHashMap<string, IList<IList<string>>>(), Generics.NewHashMap<string, IList<IList<string>>>(), originalStateIndex);
		}

		/// <summary>Compacts the grammar specified by the Pair.</summary>
		/// <param name="grammar">a Pair of grammars, ordered UnaryGrammar BinaryGrammar.</param>
		/// <param name="allTrainPaths">a Map from String passive constituents to Lists of paths</param>
		/// <param name="allTestPaths">a Map from String passive constituents to Lists of paths</param>
		/// <returns>a Pair of grammars, ordered UnaryGrammar BinaryGrammar.</returns>
		public virtual Triple<IIndex<string>, UnaryGrammar, BinaryGrammar> CompactGrammar(Pair<UnaryGrammar, BinaryGrammar> grammar, IDictionary<string, IList<IList<string>>> allTrainPaths, IDictionary<string, IList<IList<string>>> allTestPaths, IIndex
			<string> originalStateIndex)
		{
			inputPrior = ComputeInputPrior(allTrainPaths);
			// computed once for the whole grammar
			// BinaryGrammar bg = grammar.second;
			this.stateIndex = originalStateIndex;
			IList<IList<string>> trainPaths;
			IList<IList<string>> testPaths;
			ICollection<UnaryRule> unaryRules = Generics.NewHashSet();
			ICollection<BinaryRule> binaryRules = Generics.NewHashSet();
			IDictionary<string, TransducerGraph> graphs = ConvertGrammarToGraphs(grammar, unaryRules, binaryRules);
			compactedGraphs = Generics.NewHashSet();
			if (verbose)
			{
				System.Console.Out.WriteLine("There are " + graphs.Count + " categories to compact.");
			}
			int i = 0;
			for (IEnumerator<KeyValuePair<string, TransducerGraph>> graphIter = graphs.GetEnumerator(); graphIter.MoveNext(); )
			{
				KeyValuePair<string, TransducerGraph> entry = graphIter.Current;
				string cat = entry.Key;
				TransducerGraph graph = entry.Value;
				if (verbose)
				{
					System.Console.Out.WriteLine("About to compact grammar for " + cat + " with numNodes=" + graph.GetNodes().Count);
				}
				trainPaths = Sharpen.Collections.Remove(allTrainPaths, cat);
				// to save memory
				if (trainPaths == null)
				{
					trainPaths = new List<IList<string>>();
				}
				testPaths = Sharpen.Collections.Remove(allTestPaths, cat);
				// to save memory
				if (testPaths == null)
				{
					testPaths = new List<IList<string>>();
				}
				TransducerGraph compactedGraph = DoCompaction(graph, trainPaths, testPaths);
				i++;
				if (verbose)
				{
					System.Console.Out.WriteLine(i + ". Compacted grammar for " + cat + " from " + graph.GetArcs().Count + " arcs to " + compactedGraph.GetArcs().Count + " arcs.");
				}
				graphIter.Remove();
				// to save memory, remove the last thing
				compactedGraphs.Add(compactedGraph);
			}
			Pair<UnaryGrammar, BinaryGrammar> ugbg = ConvertGraphsToGrammar(compactedGraphs, unaryRules, binaryRules);
			return new Triple<IIndex<string>, UnaryGrammar, BinaryGrammar>(newStateIndex, ugbg.First(), ugbg.Second());
		}

		protected internal static Distribution<string> ComputeInputPrior(IDictionary<string, IList<IList<string>>> allTrainPaths)
		{
			ClassicCounter<string> result = new ClassicCounter<string>();
			foreach (IList<IList<string>> pathList in allTrainPaths.Values)
			{
				foreach (IList<string> path in pathList)
				{
					foreach (string input in path)
					{
						result.IncrementCount(input);
					}
				}
			}
			return Distribution.LaplaceSmoothedDistribution(result, result.Size() * 2, 0.5);
		}

		private double SmartNegate(double output)
		{
			if (outputType == NormalizedLogProbabilities)
			{
				return -output;
			}
			return output;
		}

		public static bool WriteFile(TransducerGraph graph, string dir, string name)
		{
			try
			{
				File baseDir = new File(dir);
				if (baseDir.Exists())
				{
					if (!baseDir.IsDirectory())
					{
						return false;
					}
				}
				else
				{
					if (!baseDir.Mkdirs())
					{
						return false;
					}
				}
				File file = new File(baseDir, name + ".dot");
				PrintWriter w;
				try
				{
					w = new PrintWriter(new FileWriter(file));
					string dotString = graph.AsDOTString();
					w.Print(dotString);
					w.Flush();
					w.Close();
				}
				catch (FileNotFoundException)
				{
					log.Info("Failed to open file in writeToDOTfile: " + file);
					return false;
				}
				catch (IOException)
				{
					log.Info("Failed to open file in writeToDOTfile: " + file);
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return false;
			}
		}

		protected internal virtual IDictionary<string, TransducerGraph> ConvertGrammarToGraphs(Pair<UnaryGrammar, BinaryGrammar> grammar, ICollection<UnaryRule> unaryRules, ICollection<BinaryRule> binaryRules)
		{
			int numRules = 0;
			UnaryGrammar ug = grammar.first;
			BinaryGrammar bg = grammar.second;
			IDictionary<string, TransducerGraph> graphs = Generics.NewHashMap();
			// go through the BinaryGrammar and add everything
			foreach (BinaryRule rule in bg)
			{
				numRules++;
				bool wasAdded = AddOneBinaryRule(rule, graphs);
				if (!wasAdded)
				{
					// add it for later, since we don't make graphs for these
					binaryRules.Add(rule);
				}
			}
			// now we need to use the UnaryGrammar to
			// add start and end Arcs to the graphs
			foreach (UnaryRule rule_1 in ug)
			{
				numRules++;
				bool wasAdded = AddOneUnaryRule(rule_1, graphs);
				if (!wasAdded)
				{
					// add it for later, since we don't make graphs for these
					unaryRules.Add(rule_1);
				}
			}
			if (verbose)
			{
				System.Console.Out.WriteLine("Number of raw rules: " + numRules);
				System.Console.Out.WriteLine("Number of raw states: " + stateIndex.Size());
			}
			return graphs;
		}

		protected internal static TransducerGraph GetGraphFromMap(IDictionary<string, TransducerGraph> m, string o)
		{
			TransducerGraph graph = m[o];
			if (graph == null)
			{
				graph = new TransducerGraph();
				graph.SetEndNode(o);
				m[o] = graph;
			}
			return graph;
		}

		protected internal static string GetTopCategoryOfSyntheticState(string s)
		{
			if (s[0] != '@')
			{
				return null;
			}
			int bar = s.IndexOf('|');
			if (bar < 0)
			{
				throw new Exception("Grammar format error. Expected bar in state name: " + s);
			}
			string topcat = Sharpen.Runtime.Substring(s, 1, bar);
			return topcat;
		}

		protected internal virtual bool AddOneUnaryRule(UnaryRule rule, IDictionary<string, TransducerGraph> graphs)
		{
			string parentString = stateIndex.Get(rule.parent);
			string childString = stateIndex.Get(rule.child);
			if (IsSyntheticState(parentString))
			{
				string topcat = GetTopCategoryOfSyntheticState(parentString);
				TransducerGraph graph = GetGraphFromMap(graphs, topcat);
				double output = SmartNegate(rule.Score());
				graph.AddArc(graph.GetStartNode(), parentString, childString, output);
				return true;
			}
			else
			{
				if (IsSyntheticState(childString))
				{
					// need to add Arc from synthetic state to endState
					TransducerGraph graph = GetGraphFromMap(graphs, parentString);
					double output = SmartNegate(rule.Score());
					graph.AddArc(childString, parentString, End, output);
					// parentString should the the same as endState
					graph.SetEndNode(parentString);
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		protected internal virtual bool AddOneBinaryRule(BinaryRule rule, IDictionary<string, TransducerGraph> graphs)
		{
			// parent has to be synthetic in BinaryRule
			string parentString = stateIndex.Get(rule.parent);
			string leftString = stateIndex.Get(rule.leftChild);
			string rightString = stateIndex.Get(rule.rightChild);
			string source;
			string target;
			string input;
			string bracket = null;
			if (op.trainOptions.markFinalStates)
			{
				bracket = Sharpen.Runtime.Substring(parentString, parentString.Length - 1, parentString.Length);
			}
			// the below test is not necessary with left to right grammars
			if (IsSyntheticState(leftString))
			{
				source = leftString;
				input = rightString + (bracket == null ? ">" : bracket);
			}
			else
			{
				if (IsSyntheticState(rightString))
				{
					source = rightString;
					input = leftString + (bracket == null ? "<" : bracket);
				}
				else
				{
					// we don't know what to do with this rule
					return false;
				}
			}
			target = parentString;
			double output = SmartNegate(rule.Score());
			// makes it a real  0 <= k <= infty
			string topcat = GetTopCategoryOfSyntheticState(source);
			if (topcat == null)
			{
				throw new Exception("can't have null topcat");
			}
			TransducerGraph graph = GetGraphFromMap(graphs, topcat);
			graph.AddArc(source, target, input, output);
			return true;
		}

		protected internal static bool IsSyntheticState(string state)
		{
			return state[0] == '@';
		}

		/// <param name="graphs">a Map from String categories to TransducerGraph objects</param>
		/// <param name="unaryRules">is a Set of UnaryRule objects that we need to add</param>
		/// <param name="binaryRules">is a Set of BinaryRule objects that we need to add</param>
		/// <returns>a new Pair of UnaryGrammar, BinaryGrammar</returns>
		protected internal virtual Pair<UnaryGrammar, BinaryGrammar> ConvertGraphsToGrammar(ICollection<TransducerGraph> graphs, ICollection<UnaryRule> unaryRules, ICollection<BinaryRule> binaryRules)
		{
			// first go through all the existing rules and number them with new numberer
			newStateIndex = new HashIndex<string>();
			foreach (UnaryRule rule in unaryRules)
			{
				string parent = stateIndex.Get(rule.parent);
				rule.parent = newStateIndex.AddToIndex(parent);
				string child = stateIndex.Get(rule.child);
				rule.child = newStateIndex.AddToIndex(child);
			}
			foreach (BinaryRule rule_1 in binaryRules)
			{
				string parent = stateIndex.Get(rule_1.parent);
				rule_1.parent = newStateIndex.AddToIndex(parent);
				string leftChild = stateIndex.Get(rule_1.leftChild);
				rule_1.leftChild = newStateIndex.AddToIndex(leftChild);
				string rightChild = stateIndex.Get(rule_1.rightChild);
				rule_1.rightChild = newStateIndex.AddToIndex(rightChild);
			}
			// now go through the graphs and add the rules
			foreach (TransducerGraph graph in graphs)
			{
				object startNode = graph.GetStartNode();
				foreach (TransducerGraph.Arc arc in graph.GetArcs())
				{
					// TODO: make sure these are the strings we're looking for
					string source = arc.GetSourceNode().ToString();
					string target = arc.GetTargetNode().ToString();
					object input = arc.GetInput();
					string inputString = input.ToString();
					double output = ((double)arc.GetOutput());
					if (source.Equals(startNode))
					{
						// make a UnaryRule
						UnaryRule ur = new UnaryRule(newStateIndex.AddToIndex(target), newStateIndex.AddToIndex(inputString), SmartNegate(output));
						unaryRules.Add(ur);
					}
					else
					{
						if (inputString.Equals(End) || inputString.Equals(Epsilon))
						{
							// make a UnaryRule
							UnaryRule ur = new UnaryRule(newStateIndex.AddToIndex(target), newStateIndex.AddToIndex(source), SmartNegate(output));
							unaryRules.Add(ur);
						}
						else
						{
							// make a BinaryRule
							// figure out whether the input was generated on the left or right
							int length = inputString.Length;
							char leftOrRight = inputString[length - 1];
							inputString = Sharpen.Runtime.Substring(inputString, 0, length - 1);
							BinaryRule br;
							if (leftOrRight == '<' || leftOrRight == '[')
							{
								br = new BinaryRule(newStateIndex.AddToIndex(target), newStateIndex.AddToIndex(inputString), newStateIndex.AddToIndex(source), SmartNegate(output));
							}
							else
							{
								if (leftOrRight == '>' || leftOrRight == ']')
								{
									br = new BinaryRule(newStateIndex.AddToIndex(target), newStateIndex.AddToIndex(source), newStateIndex.AddToIndex(inputString), SmartNegate(output));
								}
								else
								{
									throw new Exception("Arc input is in unexpected format: " + arc);
								}
							}
							binaryRules.Add(br);
						}
					}
				}
			}
			// by now, the unaryRules and binaryRules Sets have old untouched and new rules with scores
			ClassicCounter<string> symbolCounter = new ClassicCounter<string>();
			if (outputType == RawCounts)
			{
				// now we take the sets of rules and turn them into grammars
				// the scores of the rules we are given are actually counts
				// so we count parent symbol occurrences
				foreach (UnaryRule rule_2 in unaryRules)
				{
					symbolCounter.IncrementCount(newStateIndex.Get(rule_2.parent), rule_2.score);
				}
				foreach (BinaryRule rule_3 in binaryRules)
				{
					symbolCounter.IncrementCount(newStateIndex.Get(rule_3.parent), rule_3.score);
				}
			}
			// now we put the rules in the grammars
			int numStates = newStateIndex.Size();
			// this should be smaller than last one
			int numRules = 0;
			UnaryGrammar ug = new UnaryGrammar(newStateIndex);
			BinaryGrammar bg = new BinaryGrammar(newStateIndex);
			foreach (UnaryRule rule_4 in unaryRules)
			{
				if (outputType == RawCounts)
				{
					double count = symbolCounter.GetCount(newStateIndex.Get(rule_4.parent));
					rule_4.score = (float)Math.Log(rule_4.score / count);
				}
				ug.AddRule(rule_4);
				numRules++;
			}
			foreach (BinaryRule rule_5 in binaryRules)
			{
				if (outputType == RawCounts)
				{
					double count = symbolCounter.GetCount(newStateIndex.Get(rule_5.parent));
					rule_5.score = (float)Math.Log((rule_5.score - op.trainOptions.ruleDiscount) / count);
				}
				bg.AddRule(rule_5);
				numRules++;
			}
			if (verbose)
			{
				System.Console.Out.WriteLine("Number of minimized rules: " + numRules);
				System.Console.Out.WriteLine("Number of minimized states: " + newStateIndex.Size());
			}
			ug.PurgeRules();
			bg.SplitRules();
			return new Pair<UnaryGrammar, BinaryGrammar>(ug, bg);
		}
	}
}
