using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class is a reimplementation of Berkeley's state splitting
	/// grammar.
	/// </summary>
	/// <remarks>
	/// This class is a reimplementation of Berkeley's state splitting
	/// grammar.  This work is experimental and still in progress.  There
	/// are several extremely important pieces to implement:
	/// <ol>
	/// <li> this code should use log probabilities throughout instead of
	/// multiplying tiny numbers
	/// <li> time efficiency of the training code is fawful
	/// <li> there are better ways to extract parses using this grammar than
	/// the method in ExhaustivePCFGParser
	/// <li> we should also implement cascading parsers that let us
	/// shortcircuit low quality parses earlier (which could possibly
	/// benefit non-split parsers as well)
	/// <li> when looping, we should short circuit if we go too many loops
	/// <li> ought to smooth as per page 436
	/// </ol>
	/// </remarks>
	/// <author>John Bauer</author>
	public class SplittingGrammarExtractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.SplittingGrammarExtractor));

		internal const int MinDebugIteration = 0;

		internal const int MaxDebugIteration = 0;

		internal const int MaxIterations = int.MaxValue;

		internal int iteration = 0;

		internal virtual bool Debug()
		{
			return (iteration >= MinDebugIteration && iteration < MaxDebugIteration);
		}

		internal Options op;

		/// <summary>These objects are created and filled in here.</summary>
		/// <remarks>
		/// These objects are created and filled in here.  The caller can get
		/// the data from the extractor once it is finished.
		/// </remarks>
		internal IIndex<string> stateIndex;

		internal IIndex<string> wordIndex;

		internal IIndex<string> tagIndex;

		/// <summary>This is a list gotten from the list of startSymbols in op.langpack()</summary>
		internal IList<string> startSymbols;

		/// <summary>A combined list of all the trees in the training set.</summary>
		internal IList<Tree> trees = new List<Tree>();

		/// <summary>All of the weights associated with the trees in the training set.</summary>
		/// <remarks>
		/// All of the weights associated with the trees in the training set.
		/// In general, this is just the weight of the original treebank.
		/// Note that this uses an identity hash map to map from tree pointer
		/// to weight.
		/// </remarks>
		internal ICounter<Tree> treeWeights = new ClassicCounter<Tree>(MapFactory.IdentityHashMapFactory<Tree, MutableDouble>());

		/// <summary>How many total weighted trees we have</summary>
		internal double trainSize;

		/// <summary>The original states in the trees</summary>
		internal ICollection<string> originalStates = Generics.NewHashSet();

		/// <summary>The current number of times a particular state has been split</summary>
		internal IntCounter<string> stateSplitCounts = new IntCounter<string>();

		/// <summary>The binary betas are weights to go from Ax to By, Cz.</summary>
		/// <remarks>
		/// The binary betas are weights to go from Ax to By, Cz.  This maps
		/// from (A, B, C) to (x, y, z) to beta(Ax, By, Cz).
		/// </remarks>
		internal ThreeDimensionalMap<string, string, string, double[][][]> binaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();

		/// <summary>The unary betas are weights to go from Ax to By.</summary>
		/// <remarks>
		/// The unary betas are weights to go from Ax to By.  This maps
		/// from (A, B) to (x, y) to beta(Ax, By).
		/// </remarks>
		internal TwoDimensionalMap<string, string, double[][]> unaryBetas = new TwoDimensionalMap<string, string, double[][]>();

		/// <summary>The latest lexicon we trained.</summary>
		/// <remarks>
		/// The latest lexicon we trained.  At the end of the process, this
		/// is the lexicon for the parser.
		/// </remarks>
		internal ILexicon lex;

		[System.NonSerialized]
		internal IIndex<string> tempWordIndex;

		[System.NonSerialized]
		internal IIndex<string> tempTagIndex;

		/// <summary>The lexicon we are in the process of building in each iteration.</summary>
		[System.NonSerialized]
		internal ILexicon tempLex;

		/// <summary>The latest pair of unary and binary grammars we trained.</summary>
		internal Pair<UnaryGrammar, BinaryGrammar> bgug;

		internal Random random = new Random(87543875943265L);

		internal const double LexSmooth = 0.0001;

		internal const double StateSmooth = 0.0;

		public SplittingGrammarExtractor(Options op)
		{
			this.op = op;
			startSymbols = Arrays.AsList(op.Langpack().StartSymbols());
		}

		internal virtual double[] NeginfDoubles(int size)
		{
			double[] result = new double[size];
			for (int i = 0; i < size; ++i)
			{
				result[i] = double.NegativeInfinity;
			}
			return result;
		}

		public virtual void OutputTransitions(Tree tree, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions)
		{
			OutputTransitions(tree, 0, unaryTransitions, binaryTransitions);
		}

		public virtual void OutputTransitions(Tree tree, int depth, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions)
		{
			for (int i = 0; i < depth; ++i)
			{
				System.Console.Out.Write(" ");
			}
			if (tree.IsLeaf())
			{
				System.Console.Out.WriteLine(tree.Label().Value());
				return;
			}
			if (tree.Children().Length == 1)
			{
				System.Console.Out.WriteLine(tree.Label().Value() + " -> " + tree.Children()[0].Label().Value());
				if (!tree.IsPreTerminal())
				{
					double[][] transitions = unaryTransitions[tree];
					for (int i_1 = 0; i_1 < transitions.Length; ++i_1)
					{
						for (int j = 0; j < transitions[0].Length; ++j)
						{
							for (int z = 0; z < depth; ++z)
							{
								System.Console.Out.Write(" ");
							}
							System.Console.Out.WriteLine("  " + i_1 + "," + j + ": " + transitions[i_1][j] + " | " + Math.Exp(transitions[i_1][j]));
						}
					}
				}
			}
			else
			{
				System.Console.Out.WriteLine(tree.Label().Value() + " -> " + tree.Children()[0].Label().Value() + " " + tree.Children()[1].Label().Value());
				double[][][] transitions = binaryTransitions[tree];
				for (int i_1 = 0; i_1 < transitions.Length; ++i_1)
				{
					for (int j = 0; j < transitions[0].Length; ++j)
					{
						for (int k = 0; k < transitions[0][0].Length; ++k)
						{
							for (int z = 0; z < depth; ++z)
							{
								System.Console.Out.Write(" ");
							}
							System.Console.Out.WriteLine("  " + i_1 + "," + j + "," + k + ": " + transitions[i_1][j][k] + " | " + Math.Exp(transitions[i_1][j][k]));
						}
					}
				}
			}
			if (tree.IsPreTerminal())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				OutputTransitions(child, depth + 1, unaryTransitions, binaryTransitions);
			}
		}

		public virtual void OutputBetas()
		{
			System.Console.Out.WriteLine("UNARY:");
			foreach (string parent in unaryBetas.FirstKeySet())
			{
				foreach (string child in unaryBetas.Get(parent).Keys)
				{
					System.Console.Out.WriteLine("  " + parent + "->" + child);
					double[][] betas = unaryBetas.Get(parent)[child];
					int parentStates = betas.Length;
					int childStates = betas[0].Length;
					for (int i = 0; i < parentStates; ++i)
					{
						for (int j = 0; j < childStates; ++j)
						{
							System.Console.Out.WriteLine("    " + i + "->" + j + " " + betas[i][j] + " | " + Math.Exp(betas[i][j]));
						}
					}
				}
			}
			System.Console.Out.WriteLine("BINARY:");
			foreach (string parent_1 in binaryBetas.FirstKeySet())
			{
				foreach (string left in binaryBetas.Get(parent_1).FirstKeySet())
				{
					foreach (string right in binaryBetas.Get(parent_1).Get(left).Keys)
					{
						System.Console.Out.WriteLine("  " + parent_1 + "->" + left + "," + right);
						double[][][] betas = binaryBetas.Get(parent_1).Get(left)[right];
						int parentStates = betas.Length;
						int leftStates = betas[0].Length;
						int rightStates = betas[0][0].Length;
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									System.Console.Out.WriteLine("    " + i + "->" + j + "," + k + " " + betas[i][j][k] + " | " + Math.Exp(betas[i][j][k]));
								}
							}
						}
					}
				}
			}
		}

		public virtual string State(string tag, int i)
		{
			if (startSymbols.Contains(tag) || tag.Equals(LexiconConstants.BoundaryTag))
			{
				return tag;
			}
			return tag + "^" + i;
		}

		public virtual int GetStateSplitCount(Tree tree)
		{
			return stateSplitCounts.GetIntCount(tree.Label().Value());
		}

		public virtual int GetStateSplitCount(string label)
		{
			return stateSplitCounts.GetIntCount(label);
		}

		/// <summary>
		/// Count all the internal labels in all the trees, and set their
		/// initial state counts to 1.
		/// </summary>
		public virtual void CountOriginalStates()
		{
			originalStates.Clear();
			foreach (Tree tree in trees)
			{
				CountOriginalStates(tree);
			}
			foreach (string state in originalStates)
			{
				stateSplitCounts.IncrementCount(state, 1);
			}
		}

		/// <summary>Counts the labels in the tree, but not the words themselves.</summary>
		private void CountOriginalStates(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			originalStates.Add(tree.Label().Value());
			foreach (Tree child in tree.Children())
			{
				if (child.IsLeaf())
				{
					continue;
				}
				CountOriginalStates(child);
			}
		}

		private void InitialBetasAndLexicon()
		{
			wordIndex = new HashIndex<string>();
			tagIndex = new HashIndex<string>();
			lex = op.tlpParams.Lex(op, wordIndex, tagIndex);
			lex.InitializeTraining(trainSize);
			foreach (Tree tree in trees)
			{
				double weight = treeWeights.GetCount(tree);
				lex.IncrementTreesRead(weight);
				InitialBetasAndLexicon(tree, 0, weight);
			}
			lex.FinishTraining();
		}

		private int InitialBetasAndLexicon(Tree tree, int position, double weight)
		{
			if (tree.IsLeaf())
			{
				// should never get here, unless a training tree is just one leaf
				return position;
			}
			if (tree.IsPreTerminal())
			{
				// fill in initial lexicon here
				string tag = tree.Label().Value();
				string word = tree.Children()[0].Label().Value();
				TaggedWord tw = new TaggedWord(word, State(tag, 0));
				lex.Train(tw, position, weight);
				return (position + 1);
			}
			if (tree.Children().Length == 2)
			{
				string label = tree.Label().Value();
				string leftLabel = tree.GetChild(0).Label().Value();
				string rightLabel = tree.GetChild(1).Label().Value();
				if (!binaryBetas.Contains(label, leftLabel, rightLabel))
				{
					double[][][] map = new double[][][] { new double[][] { new double[1] } };
					map[0][0][0] = 0.0;
					binaryBetas.Put(label, leftLabel, rightLabel, map);
				}
			}
			else
			{
				if (tree.Children().Length == 1)
				{
					string label = tree.Label().Value();
					string childLabel = tree.GetChild(0).Label().Value();
					if (!unaryBetas.Contains(label, childLabel))
					{
						double[][] map = new double[][] { new double[1] };
						map[0][0] = 0.0;
						unaryBetas.Put(label, childLabel, map);
					}
				}
				else
				{
					// should have been binarized
					throw new Exception("Trees should have been binarized, expected 1 or 2 children");
				}
			}
			foreach (Tree child in tree.Children())
			{
				position = InitialBetasAndLexicon(child, position, weight);
			}
			return position;
		}

		/// <summary>Splits the state counts.</summary>
		/// <remarks>
		/// Splits the state counts.  Root states and the boundary tag do not
		/// get their counts increased, and all others are doubled.  Betas
		/// and transition weights are handled later.
		/// </remarks>
		private void SplitStateCounts()
		{
			// double the count of states...
			IntCounter<string> newStateSplitCounts = new IntCounter<string>();
			newStateSplitCounts.AddAll(stateSplitCounts);
			newStateSplitCounts.AddAll(stateSplitCounts);
			// root states should only have 1
			foreach (string root in startSymbols)
			{
				if (newStateSplitCounts.GetCount(root) > 1)
				{
					newStateSplitCounts.SetCount(root, 1);
				}
			}
			if (newStateSplitCounts.GetCount(LexiconConstants.BoundaryTag) > 1)
			{
				newStateSplitCounts.SetCount(LexiconConstants.BoundaryTag, 1);
			}
			stateSplitCounts = newStateSplitCounts;
		}

		internal const double Epsilon = 0.0001;

		/// <summary>
		/// Before each iteration of splitting states, we have tables of
		/// betas which correspond to the transitions between different
		/// substates.
		/// </summary>
		/// <remarks>
		/// Before each iteration of splitting states, we have tables of
		/// betas which correspond to the transitions between different
		/// substates.  When we resplit the states, we duplicate parent
		/// states and then split their transitions 50/50 with some random
		/// variation between child states.
		/// </remarks>
		public virtual void SplitBetas()
		{
			TwoDimensionalMap<string, string, double[][]> tempUnaryBetas = new TwoDimensionalMap<string, string, double[][]>();
			ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();
			foreach (string parent in unaryBetas.FirstKeySet())
			{
				foreach (string child in unaryBetas.Get(parent).Keys)
				{
					double[][] betas = unaryBetas.Get(parent, child);
					int parentStates = betas.Length;
					int childStates = betas[0].Length;
					double[][] newBetas;
					if (!startSymbols.Contains(parent))
					{
						newBetas = new double[][] {  };
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < childStates; ++j)
							{
								newBetas[i * 2][j] = betas[i][j];
								newBetas[i * 2 + 1][j] = betas[i][j];
							}
						}
						parentStates *= 2;
						betas = newBetas;
					}
					if (!child.Equals(LexiconConstants.BoundaryTag))
					{
						newBetas = new double[parentStates][];
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < childStates; ++j)
							{
								double childWeight = 0.45 + random.NextDouble() * 0.1;
								newBetas[i][j * 2] = betas[i][j] + Math.Log(childWeight);
								newBetas[i][j * 2 + 1] = betas[i][j] + Math.Log(1.0 - childWeight);
							}
						}
						betas = newBetas;
					}
					tempUnaryBetas.Put(parent, child, betas);
				}
			}
			foreach (string parent_1 in binaryBetas.FirstKeySet())
			{
				foreach (string left in binaryBetas.Get(parent_1).FirstKeySet())
				{
					foreach (string right in binaryBetas.Get(parent_1).Get(left).Keys)
					{
						double[][][] betas = binaryBetas.Get(parent_1, left, right);
						int parentStates = betas.Length;
						int leftStates = betas[0].Length;
						int rightStates = betas[0][0].Length;
						double[][][] newBetas;
						if (!startSymbols.Contains(parent_1))
						{
							newBetas = new double[][][] {  };
							for (int i = 0; i < parentStates; ++i)
							{
								for (int j = 0; j < leftStates; ++j)
								{
									for (int k = 0; k < rightStates; ++k)
									{
										newBetas[i * 2][j][k] = betas[i][j][k];
										newBetas[i * 2 + 1][j][k] = betas[i][j][k];
									}
								}
							}
							parentStates *= 2;
							betas = newBetas;
						}
						newBetas = new double[parentStates][][];
						for (int i_1 = 0; i_1 < parentStates; ++i_1)
						{
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									double leftWeight = 0.45 + random.NextDouble() * 0.1;
									newBetas[i_1][j * 2][k] = betas[i_1][j][k] + Math.Log(leftWeight);
									newBetas[i_1][j * 2 + 1][k] = betas[i_1][j][k] + Math.Log(1 - leftWeight);
								}
							}
						}
						leftStates *= 2;
						betas = newBetas;
						if (!right.Equals(LexiconConstants.BoundaryTag))
						{
							newBetas = new double[parentStates][][];
							for (int i = 0; i_1 < parentStates; ++i_1)
							{
								for (int j = 0; j < leftStates; ++j)
								{
									for (int k = 0; k < rightStates; ++k)
									{
										double rightWeight = 0.45 + random.NextDouble() * 0.1;
										newBetas[i_1][j][k * 2] = betas[i_1][j][k] + Math.Log(rightWeight);
										newBetas[i_1][j][k * 2 + 1] = betas[i_1][j][k] + Math.Log(1 - rightWeight);
									}
								}
							}
						}
						tempBinaryBetas.Put(parent_1, left, right, newBetas);
					}
				}
			}
			unaryBetas = tempUnaryBetas;
			binaryBetas = tempBinaryBetas;
		}

		/// <summary>Recalculates the betas for all known transitions.</summary>
		/// <remarks>
		/// Recalculates the betas for all known transitions.  The current
		/// betas are used to produce probabilities, which then are used to
		/// compute new betas.  If splitStates is true, then the
		/// probabilities produced are as if the states were split again from
		/// the last time betas were calculated.
		/// <br />
		/// The return value is whether or not the betas have mostly
		/// converged from the last time this method was called.  Obviously
		/// if splitStates was true, the betas will be entirely different, so
		/// this is false.  Otherwise, the new betas are compared against the
		/// old values, and convergence means they differ by less than
		/// EPSILON.
		/// </remarks>
		public virtual bool RecalculateBetas(bool splitStates)
		{
			if (splitStates)
			{
				if (Debug())
				{
					System.Console.Out.WriteLine("Pre-split betas");
					OutputBetas();
				}
				SplitBetas();
				if (Debug())
				{
					System.Console.Out.WriteLine("Post-split betas");
					OutputBetas();
				}
			}
			TwoDimensionalMap<string, string, double[][]> tempUnaryBetas = new TwoDimensionalMap<string, string, double[][]>();
			ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();
			RecalculateTemporaryBetas(splitStates, null, tempUnaryBetas, tempBinaryBetas);
			bool converged = UseNewBetas(!splitStates, tempUnaryBetas, tempBinaryBetas);
			if (Debug())
			{
				OutputBetas();
			}
			return converged;
		}

		public virtual bool UseNewBetas(bool testConverged, TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas)
		{
			RescaleTemporaryBetas(tempUnaryBetas, tempBinaryBetas);
			// if we just split states, we have obviously not converged
			bool converged = testConverged && TestConvergence(tempUnaryBetas, tempBinaryBetas);
			unaryBetas = tempUnaryBetas;
			binaryBetas = tempBinaryBetas;
			wordIndex = tempWordIndex;
			tagIndex = tempTagIndex;
			lex = tempLex;
			if (Debug())
			{
				System.Console.Out.WriteLine("LEXICON");
				try
				{
					OutputStreamWriter osw = new OutputStreamWriter(System.Console.Out, "utf-8");
					lex.WriteData(osw);
					osw.Flush();
				}
				catch (IOException e)
				{
					throw new RuntimeIOException(e);
				}
			}
			tempWordIndex = null;
			tempTagIndex = null;
			tempLex = null;
			return converged;
		}

		/// <summary>
		/// Creates temporary beta data structures and fills them in by
		/// iterating over the trees.
		/// </summary>
		public virtual void RecalculateTemporaryBetas(bool splitStates, IDictionary<string, double[]> totalStateMass, TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas
			)
		{
			tempWordIndex = new HashIndex<string>();
			tempTagIndex = new HashIndex<string>();
			tempLex = op.tlpParams.Lex(op, tempWordIndex, tempTagIndex);
			tempLex.InitializeTraining(trainSize);
			foreach (Tree tree in trees)
			{
				double weight = treeWeights.GetCount(tree);
				if (Debug())
				{
					System.Console.Out.WriteLine("Incrementing trees read: " + weight);
				}
				tempLex.IncrementTreesRead(weight);
				RecalculateTemporaryBetas(tree, splitStates, totalStateMass, tempUnaryBetas, tempBinaryBetas);
			}
			tempLex.FinishTraining();
		}

		public virtual bool TestConvergence(TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas)
		{
			// now, we check each of the new betas to see if it's close to the
			// old value for the same transition.  if not, we have not yet
			// converged.  if all of them are, we have converged.
			foreach (string parentLabel in unaryBetas.FirstKeySet())
			{
				foreach (string childLabel in unaryBetas.Get(parentLabel).Keys)
				{
					double[][] betas = unaryBetas.Get(parentLabel, childLabel);
					double[][] newBetas = tempUnaryBetas.Get(parentLabel, childLabel);
					int parentStates = betas.Length;
					int childStates = betas[0].Length;
					for (int i = 0; i < parentStates; ++i)
					{
						for (int j = 0; j < childStates; ++j)
						{
							double oldValue = betas[i][j];
							double newValue = newBetas[i][j];
							if (Math.Abs(newValue - oldValue) > Epsilon)
							{
								return false;
							}
						}
					}
				}
			}
			foreach (string parentLabel_1 in binaryBetas.FirstKeySet())
			{
				foreach (string leftLabel in binaryBetas.Get(parentLabel_1).FirstKeySet())
				{
					foreach (string rightLabel in binaryBetas.Get(parentLabel_1).Get(leftLabel).Keys)
					{
						double[][][] betas = binaryBetas.Get(parentLabel_1, leftLabel, rightLabel);
						double[][][] newBetas = tempBinaryBetas.Get(parentLabel_1, leftLabel, rightLabel);
						int parentStates = betas.Length;
						int leftStates = betas[0].Length;
						int rightStates = betas[0][0].Length;
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									double oldValue = betas[i][j][k];
									double newValue = newBetas[i][j][k];
									if (Math.Abs(newValue - oldValue) > Epsilon)
									{
										return false;
									}
								}
							}
						}
					}
				}
			}
			return true;
		}

		public virtual void RecalculateTemporaryBetas(Tree tree, bool splitStates, IDictionary<string, double[]> totalStateMass, TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> 
			tempBinaryBetas)
		{
			if (Debug())
			{
				System.Console.Out.WriteLine("Recalculating temporary betas for tree " + tree);
			}
			double[] stateWeights = new double[] { Math.Log(treeWeights.GetCount(tree)) };
			IdentityHashMap<Tree, double[][]> unaryTransitions = new IdentityHashMap<Tree, double[][]>();
			IdentityHashMap<Tree, double[][][]> binaryTransitions = new IdentityHashMap<Tree, double[][][]>();
			RecountTree(tree, splitStates, unaryTransitions, binaryTransitions);
			if (Debug())
			{
				System.Console.Out.WriteLine("  Transitions:");
				OutputTransitions(tree, unaryTransitions, binaryTransitions);
			}
			RecalculateTemporaryBetas(tree, stateWeights, 0, unaryTransitions, binaryTransitions, totalStateMass, tempUnaryBetas, tempBinaryBetas);
		}

		public virtual int RecalculateTemporaryBetas(Tree tree, double[] stateWeights, int position, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions, IDictionary<string, double[]> totalStateMass
			, TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas)
		{
			if (tree.IsLeaf())
			{
				// possible to get here if we have a tree with no structure
				return position;
			}
			if (totalStateMass != null)
			{
				double[] stateTotal = totalStateMass[tree.Label().Value()];
				if (stateTotal == null)
				{
					stateTotal = new double[stateWeights.Length];
					totalStateMass[tree.Label().Value()] = stateTotal;
				}
				for (int i = 0; i < stateWeights.Length; ++i)
				{
					stateTotal[i] += Math.Exp(stateWeights[i]);
				}
			}
			if (tree.IsPreTerminal())
			{
				// fill in our new lexicon here.
				string tag = tree.Label().Value();
				string word = tree.Children()[0].Label().Value();
				// We smooth by LEX_SMOOTH, if relevant.  We rescale so that sum
				// of the weights being added to the lexicon stays the same.
				double total = 0.0;
				foreach (double stateWeight in stateWeights)
				{
					total += Math.Exp(stateWeight);
				}
				if (total <= 0.0)
				{
					return position + 1;
				}
				double scale = 1.0 / (1.0 + LexSmooth);
				double smoothing = total * LexSmooth / stateWeights.Length;
				for (int state = 0; state < stateWeights.Length; ++state)
				{
					// TODO: maybe optimize all this TaggedWord creation
					TaggedWord tw = new TaggedWord(word, State(tag, state));
					tempLex.Train(tw, position, (Math.Exp(stateWeights[state]) + smoothing) * scale);
				}
				return position + 1;
			}
			if (tree.Children().Length == 1)
			{
				string parentLabel = tree.Label().Value();
				string childLabel = tree.Children()[0].Label().Value();
				double[][] transitions = unaryTransitions[tree];
				int parentStates = transitions.Length;
				int childStates = transitions[0].Length;
				double[][] betas = tempUnaryBetas.Get(parentLabel, childLabel);
				if (betas == null)
				{
					betas = new double[parentStates][];
					for (int i = 0; i < parentStates; ++i)
					{
						for (int j = 0; j < childStates; ++j)
						{
							betas[i][j] = double.NegativeInfinity;
						}
					}
					tempUnaryBetas.Put(parentLabel, childLabel, betas);
				}
				double[] childWeights = NeginfDoubles(childStates);
				for (int i_1 = 0; i_1 < parentStates; ++i_1)
				{
					for (int j = 0; j < childStates; ++j)
					{
						double weight = transitions[i_1][j];
						betas[i_1][j] = SloppyMath.LogAdd(betas[i_1][j], weight + stateWeights[i_1]);
						childWeights[j] = SloppyMath.LogAdd(childWeights[j], weight + stateWeights[i_1]);
					}
				}
				position = RecalculateTemporaryBetas(tree.Children()[0], childWeights, position, unaryTransitions, binaryTransitions, totalStateMass, tempUnaryBetas, tempBinaryBetas);
			}
			else
			{
				// length == 2
				string parentLabel = tree.Label().Value();
				string leftLabel = tree.Children()[0].Label().Value();
				string rightLabel = tree.Children()[1].Label().Value();
				double[][][] transitions = binaryTransitions[tree];
				int parentStates = transitions.Length;
				int leftStates = transitions[0].Length;
				int rightStates = transitions[0][0].Length;
				double[][][] betas = tempBinaryBetas.Get(parentLabel, leftLabel, rightLabel);
				if (betas == null)
				{
					betas = new double[parentStates][][];
					for (int i = 0; i < parentStates; ++i)
					{
						for (int j = 0; j < leftStates; ++j)
						{
							for (int k = 0; k < rightStates; ++k)
							{
								betas[i][j][k] = double.NegativeInfinity;
							}
						}
					}
					tempBinaryBetas.Put(parentLabel, leftLabel, rightLabel, betas);
				}
				double[] leftWeights = NeginfDoubles(leftStates);
				double[] rightWeights = NeginfDoubles(rightStates);
				for (int i_1 = 0; i_1 < parentStates; ++i_1)
				{
					for (int j = 0; j < leftStates; ++j)
					{
						for (int k = 0; k < rightStates; ++k)
						{
							double weight = transitions[i_1][j][k];
							betas[i_1][j][k] = SloppyMath.LogAdd(betas[i_1][j][k], weight + stateWeights[i_1]);
							leftWeights[j] = SloppyMath.LogAdd(leftWeights[j], weight + stateWeights[i_1]);
							rightWeights[k] = SloppyMath.LogAdd(rightWeights[k], weight + stateWeights[i_1]);
						}
					}
				}
				position = RecalculateTemporaryBetas(tree.Children()[0], leftWeights, position, unaryTransitions, binaryTransitions, totalStateMass, tempUnaryBetas, tempBinaryBetas);
				position = RecalculateTemporaryBetas(tree.Children()[1], rightWeights, position, unaryTransitions, binaryTransitions, totalStateMass, tempUnaryBetas, tempBinaryBetas);
			}
			return position;
		}

		public virtual void RescaleTemporaryBetas(TwoDimensionalMap<string, string, double[][]> tempUnaryBetas, ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas)
		{
			foreach (string parent in tempUnaryBetas.FirstKeySet())
			{
				foreach (string child in tempUnaryBetas.Get(parent).Keys)
				{
					double[][] betas = tempUnaryBetas.Get(parent)[child];
					int parentStates = betas.Length;
					int childStates = betas[0].Length;
					for (int i = 0; i < parentStates; ++i)
					{
						double sum = double.NegativeInfinity;
						for (int j = 0; j < childStates; ++j)
						{
							sum = SloppyMath.LogAdd(sum, betas[i][j]);
						}
						if (double.IsInfinite(sum))
						{
							for (int j_1 = 0; j_1 < childStates; ++j_1)
							{
								betas[i][j_1] = -System.Math.Log(childStates);
							}
						}
						else
						{
							for (int j_1 = 0; j_1 < childStates; ++j_1)
							{
								betas[i][j_1] -= sum;
							}
						}
					}
				}
			}
			foreach (string parent_1 in tempBinaryBetas.FirstKeySet())
			{
				foreach (string left in tempBinaryBetas.Get(parent_1).FirstKeySet())
				{
					foreach (string right in tempBinaryBetas.Get(parent_1).Get(left).Keys)
					{
						double[][][] betas = tempBinaryBetas.Get(parent_1).Get(left)[right];
						int parentStates = betas.Length;
						int leftStates = betas[0].Length;
						int rightStates = betas[0][0].Length;
						for (int i = 0; i < parentStates; ++i)
						{
							double sum = double.NegativeInfinity;
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									sum = SloppyMath.LogAdd(sum, betas[i][j][k]);
								}
							}
							if (double.IsInfinite(sum))
							{
								for (int j_1 = 0; j_1 < leftStates; ++j_1)
								{
									for (int k = 0; k < rightStates; ++k)
									{
										betas[i][j_1][k] = -System.Math.Log(leftStates * rightStates);
									}
								}
							}
							else
							{
								for (int j_1 = 0; j_1 < leftStates; ++j_1)
								{
									for (int k = 0; k < rightStates; ++k)
									{
										betas[i][j_1][k] -= sum;
									}
								}
							}
						}
					}
				}
			}
		}

		public virtual void RecountTree(Tree tree, bool splitStates, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions)
		{
			IdentityHashMap<Tree, double[]> probIn = new IdentityHashMap<Tree, double[]>();
			IdentityHashMap<Tree, double[]> probOut = new IdentityHashMap<Tree, double[]>();
			RecountTree(tree, splitStates, probIn, probOut, unaryTransitions, binaryTransitions);
		}

		public virtual void RecountTree(Tree tree, bool splitStates, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions
			)
		{
			RecountInside(tree, splitStates, 0, probIn);
			if (Debug())
			{
				System.Console.Out.WriteLine("ROOT PROBABILITY: " + probIn[tree][0]);
			}
			RecountOutside(tree, probIn, probOut);
			RecountWeights(tree, probIn, probOut, unaryTransitions, binaryTransitions);
		}

		public virtual void RecountWeights(Tree tree, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut, IdentityHashMap<Tree, double[][]> unaryTransitions, IdentityHashMap<Tree, double[][][]> binaryTransitions)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return;
			}
			if (tree.Children().Length == 1)
			{
				Tree child = tree.Children()[0];
				string parentLabel = tree.Label().Value();
				string childLabel = child.Label().Value();
				double[][] betas = unaryBetas.Get(parentLabel, childLabel);
				double[] childInside = probIn[child];
				double[] parentOutside = probOut[tree];
				int parentStates = betas.Length;
				int childStates = betas[0].Length;
				double[][] transitions = new double[parentStates][];
				unaryTransitions[tree] = transitions;
				for (int i = 0; i < parentStates; ++i)
				{
					for (int j = 0; j < childStates; ++j)
					{
						transitions[i][j] = parentOutside[i] + childInside[j] + betas[i][j];
					}
				}
				// Renormalize.  Note that we renormalize to 1, regardless of
				// the original total.
				// TODO: smoothing?
				for (int i_1 = 0; i_1 < parentStates; ++i_1)
				{
					double total = double.NegativeInfinity;
					for (int j = 0; j < childStates; ++j)
					{
						total = SloppyMath.LogAdd(total, transitions[i_1][j]);
					}
					// By subtracting off the log total, we make it so the log sum
					// of the transitions is 0, meaning the sum of the actual
					// transitions is 1.  It works if you do the math...
					if (double.IsInfinite(total))
					{
						double transition = -System.Math.Log(childStates);
						for (int j_1 = 0; j_1 < childStates; ++j_1)
						{
							transitions[i_1][j_1] = transition;
						}
					}
					else
					{
						for (int j_1 = 0; j_1 < childStates; ++j_1)
						{
							transitions[i_1][j_1] = transitions[i_1][j_1] - total;
						}
					}
				}
				RecountWeights(child, probIn, probOut, unaryTransitions, binaryTransitions);
			}
			else
			{
				// length == 2
				Tree left = tree.Children()[0];
				Tree right = tree.Children()[1];
				string parentLabel = tree.Label().Value();
				string leftLabel = left.Label().Value();
				string rightLabel = right.Label().Value();
				double[][][] betas = binaryBetas.Get(parentLabel, leftLabel, rightLabel);
				double[] leftInside = probIn[left];
				double[] rightInside = probIn[right];
				double[] parentOutside = probOut[tree];
				int parentStates = betas.Length;
				int leftStates = betas[0].Length;
				int rightStates = betas[0][0].Length;
				double[][][] transitions = new double[parentStates][][];
				binaryTransitions[tree] = transitions;
				for (int i = 0; i < parentStates; ++i)
				{
					for (int j = 0; j < leftStates; ++j)
					{
						for (int k = 0; k < rightStates; ++k)
						{
							transitions[i][j][k] = parentOutside[i] + leftInside[j] + rightInside[k] + betas[i][j][k];
						}
					}
				}
				// Renormalize.  Note that we renormalize to 1, regardless of
				// the original total.
				// TODO: smoothing?
				for (int i_1 = 0; i_1 < parentStates; ++i_1)
				{
					double total = double.NegativeInfinity;
					for (int j = 0; j < leftStates; ++j)
					{
						for (int k = 0; k < rightStates; ++k)
						{
							total = SloppyMath.LogAdd(total, transitions[i_1][j][k]);
						}
					}
					// By subtracting off the log total, we make it so the log sum
					// of the transitions is 0, meaning the sum of the actual
					// transitions is 1.  It works if you do the math...
					if (double.IsInfinite(total))
					{
						double transition = -System.Math.Log(leftStates * rightStates);
						for (int j_1 = 0; j_1 < leftStates; ++j_1)
						{
							for (int k = 0; k < rightStates; ++k)
							{
								transitions[i_1][j_1][k] = transition;
							}
						}
					}
					else
					{
						for (int j_1 = 0; j_1 < leftStates; ++j_1)
						{
							for (int k = 0; k < rightStates; ++k)
							{
								transitions[i_1][j_1][k] = transitions[i_1][j_1][k] - total;
							}
						}
					}
				}
				RecountWeights(left, probIn, probOut, unaryTransitions, binaryTransitions);
				RecountWeights(right, probIn, probOut, unaryTransitions, binaryTransitions);
			}
		}

		public virtual void RecountOutside(Tree tree, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut)
		{
			double[] rootScores = new double[] { 0.0 };
			probOut[tree] = rootScores;
			RecurseOutside(tree, probIn, probOut);
		}

		public virtual void RecurseOutside(Tree tree, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return;
			}
			if (tree.Children().Length == 1)
			{
				RecountOutside(tree.Children()[0], tree, probIn, probOut);
			}
			else
			{
				// length == 2
				RecountOutside(tree.Children()[0], tree.Children()[1], tree, probIn, probOut);
			}
		}

		public virtual void RecountOutside(Tree child, Tree parent, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut)
		{
			string parentLabel = parent.Label().Value();
			string childLabel = child.Label().Value();
			double[] parentScores = probOut[parent];
			double[][] betas = unaryBetas.Get(parentLabel, childLabel);
			int parentStates = betas.Length;
			int childStates = betas[0].Length;
			double[] scores = NeginfDoubles(childStates);
			probOut[child] = scores;
			for (int i = 0; i < parentStates; ++i)
			{
				for (int j = 0; j < childStates; ++j)
				{
					// TODO: no inside scores here, right?
					scores[j] = SloppyMath.LogAdd(scores[j], betas[i][j] + parentScores[i]);
				}
			}
			RecurseOutside(child, probIn, probOut);
		}

		public virtual void RecountOutside(Tree left, Tree right, Tree parent, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut)
		{
			string parentLabel = parent.Label().Value();
			string leftLabel = left.Label().Value();
			string rightLabel = right.Label().Value();
			double[] leftInsideScores = probIn[left];
			double[] rightInsideScores = probIn[right];
			double[] parentScores = probOut[parent];
			double[][][] betas = binaryBetas.Get(parentLabel, leftLabel, rightLabel);
			int parentStates = betas.Length;
			int leftStates = betas[0].Length;
			int rightStates = betas[0][0].Length;
			double[] leftScores = NeginfDoubles(leftStates);
			probOut[left] = leftScores;
			double[] rightScores = NeginfDoubles(rightStates);
			probOut[right] = rightScores;
			for (int i = 0; i < parentStates; ++i)
			{
				for (int j = 0; j < leftStates; ++j)
				{
					for (int k = 0; k < rightStates; ++k)
					{
						leftScores[j] = SloppyMath.LogAdd(leftScores[j], betas[i][j][k] + parentScores[i] + rightInsideScores[k]);
						rightScores[k] = SloppyMath.LogAdd(rightScores[k], betas[i][j][k] + parentScores[i] + leftInsideScores[j]);
					}
				}
			}
			RecurseOutside(left, probIn, probOut);
			RecurseOutside(right, probIn, probOut);
		}

		public virtual int RecountInside(Tree tree, bool splitStates, int loc, IdentityHashMap<Tree, double[]> probIn)
		{
			if (tree.IsLeaf())
			{
				throw new Exception();
			}
			else
			{
				if (tree.IsPreTerminal())
				{
					int stateCount = GetStateSplitCount(tree);
					string word = tree.Children()[0].Label().Value();
					string tag = tree.Label().Value();
					double[] scores = new double[stateCount];
					probIn[tree] = scores;
					if (splitStates && !tag.Equals(LexiconConstants.BoundaryTag))
					{
						for (int i = 0; i < stateCount / 2; ++i)
						{
							IntTaggedWord tw = new IntTaggedWord(word, State(tag, i), wordIndex, tagIndex);
							double logProb = lex.Score(tw, loc, word, null);
							double wordWeight = 0.45 + random.NextDouble() * 0.1;
							scores[i * 2] = logProb + System.Math.Log(wordWeight);
							scores[i * 2 + 1] = logProb + System.Math.Log(1.0 - wordWeight);
							if (Debug())
							{
								System.Console.Out.WriteLine("Lexicon log prob " + State(tag, i) + "-" + word + ": " + logProb);
								System.Console.Out.WriteLine("  Log Split -> " + scores[i * 2] + "," + scores[i * 2 + 1]);
							}
						}
					}
					else
					{
						for (int i = 0; i < stateCount; ++i)
						{
							IntTaggedWord tw = new IntTaggedWord(word, State(tag, i), wordIndex, tagIndex);
							double prob = lex.Score(tw, loc, word, null);
							if (Debug())
							{
								System.Console.Out.WriteLine("Lexicon log prob " + State(tag, i) + "-" + word + ": " + prob);
							}
							scores[i] = prob;
						}
					}
					loc = loc + 1;
				}
				else
				{
					if (tree.Children().Length == 1)
					{
						loc = RecountInside(tree.Children()[0], splitStates, loc, probIn);
						double[] childScores = probIn[tree.Children()[0]];
						string parentLabel = tree.Label().Value();
						string childLabel = tree.Children()[0].Label().Value();
						double[][] betas = unaryBetas.Get(parentLabel, childLabel);
						int parentStates = betas.Length;
						// size of the first key
						int childStates = betas[0].Length;
						double[] scores = NeginfDoubles(parentStates);
						probIn[tree] = scores;
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < childStates; ++j)
							{
								scores[i] = SloppyMath.LogAdd(scores[i], childScores[j] + betas[i][j]);
							}
						}
						if (Debug())
						{
							System.Console.Out.WriteLine(parentLabel + " -> " + childLabel);
							for (int i_1 = 0; i_1 < parentStates; ++i_1)
							{
								System.Console.Out.WriteLine("  " + i_1 + ":" + scores[i_1]);
								for (int j = 0; j < childStates; ++j)
								{
									System.Console.Out.WriteLine("    " + i_1 + "," + j + ": " + betas[i_1][j] + " | " + System.Math.Exp(betas[i_1][j]));
								}
							}
						}
					}
					else
					{
						// length == 2
						loc = RecountInside(tree.Children()[0], splitStates, loc, probIn);
						loc = RecountInside(tree.Children()[1], splitStates, loc, probIn);
						double[] leftScores = probIn[tree.Children()[0]];
						double[] rightScores = probIn[tree.Children()[1]];
						string parentLabel = tree.Label().Value();
						string leftLabel = tree.Children()[0].Label().Value();
						string rightLabel = tree.Children()[1].Label().Value();
						double[][][] betas = binaryBetas.Get(parentLabel, leftLabel, rightLabel);
						int parentStates = betas.Length;
						int leftStates = betas[0].Length;
						int rightStates = betas[0][0].Length;
						double[] scores = NeginfDoubles(parentStates);
						probIn[tree] = scores;
						for (int i = 0; i < parentStates; ++i)
						{
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									scores[i] = SloppyMath.LogAdd(scores[i], leftScores[j] + rightScores[k] + betas[i][j][k]);
								}
							}
						}
						if (Debug())
						{
							System.Console.Out.WriteLine(parentLabel + " -> " + leftLabel + "," + rightLabel);
							for (int i_1 = 0; i_1 < parentStates; ++i_1)
							{
								System.Console.Out.WriteLine("  " + i_1 + ":" + scores[i_1]);
								for (int j = 0; j < leftStates; ++j)
								{
									for (int k = 0; k < rightStates; ++k)
									{
										System.Console.Out.WriteLine("    " + i_1 + "," + j + "," + k + ": " + betas[i_1][j][k] + " | " + System.Math.Exp(betas[i_1][j][k]));
									}
								}
							}
						}
					}
				}
			}
			return loc;
		}

		public virtual void MergeStates()
		{
			if (op.trainOptions.splitRecombineRate <= 0.0)
			{
				return;
			}
			// we go through the machinery to sum up the temporary betas,
			// counting the total mass
			TwoDimensionalMap<string, string, double[][]> tempUnaryBetas = new TwoDimensionalMap<string, string, double[][]>();
			ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();
			IDictionary<string, double[]> totalStateMass = Generics.NewHashMap();
			RecalculateTemporaryBetas(false, totalStateMass, tempUnaryBetas, tempBinaryBetas);
			// Next, for each tree we count the effect of merging its
			// annotations.  We only consider the most recently split
			// annotations as candidates for merging.
			IDictionary<string, double[]> deltaAnnotations = Generics.NewHashMap();
			foreach (Tree tree in trees)
			{
				CountMergeEffects(tree, totalStateMass, deltaAnnotations);
			}
			// Now we have a map of the (approximate) likelihood loss from
			// merging each state.  We merge the ones that provide the least
			// benefit, up to the splitRecombineRate
			IList<Triple<string, int, double>> sortedDeltas = new List<Triple<string, int, double>>();
			foreach (string state in deltaAnnotations.Keys)
			{
				double[] scores = deltaAnnotations[state];
				for (int i = 0; i < scores.Length; ++i)
				{
					sortedDeltas.Add(new Triple<string, int, double>(state, i * 2, scores[i]));
				}
			}
			sortedDeltas.Sort(new _IComparator_1142());
			// The most useful splits will have a large loss in
			// likelihood if they are merged.  Thus, we want those at
			// the end of the list.  This means we make the comparison
			// "backwards", sorting from high to low.
			// for (Triple<String, Integer, Double> delta : sortedDeltas) {
			//   System.out.println(delta.first() + "-" + delta.second() + ": " + delta.third());
			// }
			// System.out.println("-------------");
			// Only merge a fraction of the splits based on what the user
			// originally asked for
			int splitsToMerge = (int)(sortedDeltas.Count * op.trainOptions.splitRecombineRate);
			splitsToMerge = System.Math.Max(0, splitsToMerge);
			splitsToMerge = System.Math.Min(sortedDeltas.Count - 1, splitsToMerge);
			sortedDeltas = sortedDeltas.SubList(0, splitsToMerge);
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine(sortedDeltas);
			IDictionary<string, int[]> mergeCorrespondence = BuildMergeCorrespondence(sortedDeltas);
			RecalculateMergedBetas(mergeCorrespondence);
			foreach (Triple<string, int, double> delta in sortedDeltas)
			{
				stateSplitCounts.DecrementCount(delta.First(), 1);
			}
		}

		private sealed class _IComparator_1142 : IComparator<Triple<string, int, double>>
		{
			public _IComparator_1142()
			{
			}

			public int Compare(Triple<string, int, double> first, Triple<string, int, double> second)
			{
				return double.Compare(second.Third(), first.Third());
			}

			public override bool Equals(object o)
			{
				return o == this;
			}
		}

		public virtual void RecalculateMergedBetas(IDictionary<string, int[]> mergeCorrespondence)
		{
			TwoDimensionalMap<string, string, double[][]> tempUnaryBetas = new TwoDimensionalMap<string, string, double[][]>();
			ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();
			tempWordIndex = new HashIndex<string>();
			tempTagIndex = new HashIndex<string>();
			tempLex = op.tlpParams.Lex(op, tempWordIndex, tempTagIndex);
			tempLex.InitializeTraining(trainSize);
			foreach (Tree tree in trees)
			{
				double treeWeight = treeWeights.GetCount(tree);
				double[] stateWeights = new double[] { System.Math.Log(treeWeight) };
				tempLex.IncrementTreesRead(treeWeight);
				IdentityHashMap<Tree, double[][]> oldUnaryTransitions = new IdentityHashMap<Tree, double[][]>();
				IdentityHashMap<Tree, double[][][]> oldBinaryTransitions = new IdentityHashMap<Tree, double[][][]>();
				RecountTree(tree, false, oldUnaryTransitions, oldBinaryTransitions);
				IdentityHashMap<Tree, double[][]> unaryTransitions = new IdentityHashMap<Tree, double[][]>();
				IdentityHashMap<Tree, double[][][]> binaryTransitions = new IdentityHashMap<Tree, double[][][]>();
				MergeTransitions(tree, oldUnaryTransitions, oldBinaryTransitions, unaryTransitions, binaryTransitions, stateWeights, mergeCorrespondence);
				RecalculateTemporaryBetas(tree, stateWeights, 0, unaryTransitions, binaryTransitions, null, tempUnaryBetas, tempBinaryBetas);
			}
			tempLex.FinishTraining();
			UseNewBetas(false, tempUnaryBetas, tempBinaryBetas);
		}

		/// <summary>
		/// Given a tree and the original set of transition probabilities
		/// from one state to the next in the tree, along with a list of the
		/// weights in the tree and a count of the mass in each substate at
		/// the current node, this method merges the probabilities as
		/// necessary.
		/// </summary>
		/// <remarks>
		/// Given a tree and the original set of transition probabilities
		/// from one state to the next in the tree, along with a list of the
		/// weights in the tree and a count of the mass in each substate at
		/// the current node, this method merges the probabilities as
		/// necessary.  The results go into newUnaryTransitions and
		/// newBinaryTransitions.
		/// </remarks>
		public virtual void MergeTransitions(Tree parent, IdentityHashMap<Tree, double[][]> oldUnaryTransitions, IdentityHashMap<Tree, double[][][]> oldBinaryTransitions, IdentityHashMap<Tree, double[][]> newUnaryTransitions, IdentityHashMap<Tree, double
			[][][]> newBinaryTransitions, double[] stateWeights, IDictionary<string, int[]> mergeCorrespondence)
		{
			if (parent.IsPreTerminal() || parent.IsLeaf())
			{
				return;
			}
			if (parent.Children().Length == 1)
			{
				double[][] oldTransitions = oldUnaryTransitions[parent];
				string parentLabel = parent.Label().Value();
				int[] parentCorrespondence = mergeCorrespondence[parentLabel];
				int parentStates = parentCorrespondence[parentCorrespondence.Length - 1] + 1;
				string childLabel = parent.Children()[0].Label().Value();
				int[] childCorrespondence = mergeCorrespondence[childLabel];
				int childStates = childCorrespondence[childCorrespondence.Length - 1] + 1;
				// System.out.println("P: " + parentLabel + " " + parentStates +
				//                    " C: " + childLabel + " " + childStates);
				// Add up the probabilities of transitioning to each state,
				// scaled by the probability of being in a given state to begin
				// with.  This accounts for when two states in the parent are
				// collapsed into one state.
				double[][] newTransitions = new double[parentStates][];
				for (int i = 0; i < parentStates; ++i)
				{
					for (int j = 0; j < childStates; ++j)
					{
						newTransitions[i][j] = double.NegativeInfinity;
					}
				}
				newUnaryTransitions[parent] = newTransitions;
				for (int i_1 = 0; i_1 < oldTransitions.Length; ++i_1)
				{
					int ti = parentCorrespondence[i_1];
					for (int j = 0; j < oldTransitions[0].Length; ++j)
					{
						int tj = childCorrespondence[j];
						// System.out.println(i + " " + ti + " " + j + " " + tj);
						newTransitions[ti][tj] = SloppyMath.LogAdd(newTransitions[ti][tj], oldTransitions[i_1][j] + stateWeights[i_1]);
					}
				}
				// renormalize
				for (int i_2 = 0; i_2 < parentStates; ++i_2)
				{
					double total = double.NegativeInfinity;
					for (int j = 0; j < childStates; ++j)
					{
						total = SloppyMath.LogAdd(total, newTransitions[i_2][j]);
					}
					if (double.IsInfinite(total))
					{
						for (int j_1 = 0; j_1 < childStates; ++j_1)
						{
							newTransitions[i_2][j_1] = -System.Math.Log(childStates);
						}
					}
					else
					{
						for (int j_1 = 0; j_1 < childStates; ++j_1)
						{
							newTransitions[i_2][j_1] -= total;
						}
					}
				}
				double[] childWeights = NeginfDoubles(oldTransitions[0].Length);
				for (int i_3 = 0; i_3 < oldTransitions.Length; ++i_3)
				{
					for (int j = 0; j < oldTransitions[0].Length; ++j)
					{
						double weight = oldTransitions[i_3][j];
						childWeights[j] = SloppyMath.LogAdd(childWeights[j], weight + stateWeights[i_3]);
					}
				}
				MergeTransitions(parent.Children()[0], oldUnaryTransitions, oldBinaryTransitions, newUnaryTransitions, newBinaryTransitions, childWeights, mergeCorrespondence);
			}
			else
			{
				double[][][] oldTransitions = oldBinaryTransitions[parent];
				string parentLabel = parent.Label().Value();
				int[] parentCorrespondence = mergeCorrespondence[parentLabel];
				int parentStates = parentCorrespondence[parentCorrespondence.Length - 1] + 1;
				string leftLabel = parent.Children()[0].Label().Value();
				int[] leftCorrespondence = mergeCorrespondence[leftLabel];
				int leftStates = leftCorrespondence[leftCorrespondence.Length - 1] + 1;
				string rightLabel = parent.Children()[1].Label().Value();
				int[] rightCorrespondence = mergeCorrespondence[rightLabel];
				int rightStates = rightCorrespondence[rightCorrespondence.Length - 1] + 1;
				// System.out.println("P: " + parentLabel + " " + parentStates +
				//                    " L: " + leftLabel + " " + leftStates +
				//                    " R: " + rightLabel + " " + rightStates);
				double[][][] newTransitions = new double[parentStates][][];
				for (int i = 0; i < parentStates; ++i)
				{
					for (int j = 0; j < leftStates; ++j)
					{
						for (int k = 0; k < rightStates; ++k)
						{
							newTransitions[i][j][k] = double.NegativeInfinity;
						}
					}
				}
				newBinaryTransitions[parent] = newTransitions;
				for (int i_1 = 0; i_1 < oldTransitions.Length; ++i_1)
				{
					int ti = parentCorrespondence[i_1];
					for (int j = 0; j < oldTransitions[0].Length; ++j)
					{
						int tj = leftCorrespondence[j];
						for (int k = 0; k < oldTransitions[0][0].Length; ++k)
						{
							int tk = rightCorrespondence[k];
							// System.out.println(i + " " + ti + " " + j + " " + tj + " " + k + " " + tk);
							newTransitions[ti][tj][tk] = SloppyMath.LogAdd(newTransitions[ti][tj][tk], oldTransitions[i_1][j][k] + stateWeights[i_1]);
						}
					}
				}
				// renormalize
				for (int i_2 = 0; i_2 < parentStates; ++i_2)
				{
					double total = double.NegativeInfinity;
					for (int j = 0; j < leftStates; ++j)
					{
						for (int k = 0; k < rightStates; ++k)
						{
							total = SloppyMath.LogAdd(total, newTransitions[i_2][j][k]);
						}
					}
					if (double.IsInfinite(total))
					{
						for (int j_1 = 0; j_1 < leftStates; ++j_1)
						{
							for (int k = 0; k < rightStates; ++k)
							{
								newTransitions[i_2][j_1][k] = -System.Math.Log(leftStates * rightStates);
							}
						}
					}
					else
					{
						for (int j_1 = 0; j_1 < leftStates; ++j_1)
						{
							for (int k = 0; k < rightStates; ++k)
							{
								newTransitions[i_2][j_1][k] -= total;
							}
						}
					}
				}
				double[] leftWeights = NeginfDoubles(oldTransitions[0].Length);
				double[] rightWeights = NeginfDoubles(oldTransitions[0][0].Length);
				for (int i_3 = 0; i_3 < oldTransitions.Length; ++i_3)
				{
					for (int j = 0; j < oldTransitions[0].Length; ++j)
					{
						for (int k = 0; k < oldTransitions[0][0].Length; ++k)
						{
							double weight = oldTransitions[i_3][j][k];
							leftWeights[j] = SloppyMath.LogAdd(leftWeights[j], weight + stateWeights[i_3]);
							rightWeights[k] = SloppyMath.LogAdd(rightWeights[k], weight + stateWeights[i_3]);
						}
					}
				}
				MergeTransitions(parent.Children()[0], oldUnaryTransitions, oldBinaryTransitions, newUnaryTransitions, newBinaryTransitions, leftWeights, mergeCorrespondence);
				MergeTransitions(parent.Children()[1], oldUnaryTransitions, oldBinaryTransitions, newUnaryTransitions, newBinaryTransitions, rightWeights, mergeCorrespondence);
			}
		}

		internal virtual IDictionary<string, int[]> BuildMergeCorrespondence(IList<Triple<string, int, double>> deltas)
		{
			IDictionary<string, int[]> mergeCorrespondence = Generics.NewHashMap();
			foreach (string state in originalStates)
			{
				int states = GetStateSplitCount(state);
				int[] correspondence = new int[states];
				for (int i = 0; i < states; ++i)
				{
					correspondence[i] = i;
				}
				mergeCorrespondence[state] = correspondence;
			}
			foreach (Triple<string, int, double> merge in deltas)
			{
				int states = GetStateSplitCount(merge.First());
				int split = merge.Second();
				int[] correspondence = mergeCorrespondence[merge.First()];
				for (int i = split + 1; i < states; ++i)
				{
					correspondence[i] = correspondence[i] - 1;
				}
			}
			return mergeCorrespondence;
		}

		public virtual void CountMergeEffects(Tree tree, IDictionary<string, double[]> totalStateMass, IDictionary<string, double[]> deltaAnnotations)
		{
			IdentityHashMap<Tree, double[]> probIn = new IdentityHashMap<Tree, double[]>();
			IdentityHashMap<Tree, double[]> probOut = new IdentityHashMap<Tree, double[]>();
			IdentityHashMap<Tree, double[][]> unaryTransitions = new IdentityHashMap<Tree, double[][]>();
			IdentityHashMap<Tree, double[][][]> binaryTransitions = new IdentityHashMap<Tree, double[][][]>();
			RecountTree(tree, false, probIn, probOut, unaryTransitions, binaryTransitions);
			// no need to count the root
			foreach (Tree child in tree.Children())
			{
				CountMergeEffects(child, totalStateMass, deltaAnnotations, probIn, probOut);
			}
		}

		public virtual void CountMergeEffects(Tree tree, IDictionary<string, double[]> totalStateMass, IDictionary<string, double[]> deltaAnnotations, IdentityHashMap<Tree, double[]> probIn, IdentityHashMap<Tree, double[]> probOut)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			if (tree.Label().Value().Equals(LexiconConstants.BoundaryTag))
			{
				return;
			}
			string label = tree.Label().Value();
			double totalMass = 0.0;
			double[] stateMass = totalStateMass[label];
			foreach (double mass in stateMass)
			{
				totalMass += mass;
			}
			double[] nodeProbIn = probIn[tree];
			double[] nodeProbOut = probOut[tree];
			double[] nodeDelta = deltaAnnotations[label];
			if (nodeDelta == null)
			{
				nodeDelta = new double[nodeProbIn.Length / 2];
				deltaAnnotations[label] = nodeDelta;
			}
			for (int i = 0; i < nodeProbIn.Length / 2; ++i)
			{
				double probInMerged = SloppyMath.LogAdd(System.Math.Log(stateMass[i * 2] / totalMass) + nodeProbIn[i * 2], System.Math.Log(stateMass[i * 2 + 1] / totalMass) + nodeProbIn[i * 2 + 1]);
				double probOutMerged = SloppyMath.LogAdd(nodeProbOut[i * 2], nodeProbOut[i * 2 + 1]);
				double probMerged = probInMerged + probOutMerged;
				double probUnmerged = SloppyMath.LogAdd(nodeProbIn[i * 2] + nodeProbOut[i * 2], nodeProbIn[i * 2 + 1] + nodeProbOut[i * 2 + 1]);
				nodeDelta[i] = nodeDelta[i] + probMerged - probUnmerged;
			}
			if (tree.IsPreTerminal())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				CountMergeEffects(child, totalStateMass, deltaAnnotations, probIn, probOut);
			}
		}

		public virtual void BuildStateIndex()
		{
			stateIndex = new HashIndex<string>();
			foreach (string key in stateSplitCounts.KeySet())
			{
				for (int i = 0; i < stateSplitCounts.GetIntCount(key); ++i)
				{
					stateIndex.AddToIndex(State(key, i));
				}
			}
		}

		public virtual void BuildGrammars()
		{
			// In order to build the grammars, we first need to fill in the
			// temp betas with the sums of the transitions from Ax to By or Ax
			// to By,Cz.  We also need the sum total of the mass in each state
			// Ax over all the trees.
			// we go through the machinery to sum up the temporary betas,
			// counting the total mass...
			TwoDimensionalMap<string, string, double[][]> tempUnaryBetas = new TwoDimensionalMap<string, string, double[][]>();
			ThreeDimensionalMap<string, string, string, double[][][]> tempBinaryBetas = new ThreeDimensionalMap<string, string, string, double[][][]>();
			IDictionary<string, double[]> totalStateMass = Generics.NewHashMap();
			RecalculateTemporaryBetas(false, totalStateMass, tempUnaryBetas, tempBinaryBetas);
			// ... but note we don't actually rescale the betas.
			// instead we use the temporary betas and the total mass in each
			// state to calculate the grammars
			// First build up a BinaryGrammar.
			// The score for each rule will be the Beta scores found earlier,
			// scaled by the total weight of a transition between unsplit states
			BinaryGrammar bg = new BinaryGrammar(stateIndex);
			foreach (string parent in tempBinaryBetas.FirstKeySet())
			{
				int parentStates = GetStateSplitCount(parent);
				double[] stateTotal = totalStateMass[parent];
				foreach (string left in tempBinaryBetas.Get(parent).FirstKeySet())
				{
					int leftStates = GetStateSplitCount(left);
					foreach (string right in tempBinaryBetas.Get(parent).Get(left).Keys)
					{
						int rightStates = GetStateSplitCount(right);
						double[][][] betas = tempBinaryBetas.Get(parent, left, right);
						for (int i = 0; i < parentStates; ++i)
						{
							if (stateTotal[i] < Epsilon)
							{
								continue;
							}
							for (int j = 0; j < leftStates; ++j)
							{
								for (int k = 0; k < rightStates; ++k)
								{
									int parentIndex = stateIndex.IndexOf(State(parent, i));
									int leftIndex = stateIndex.IndexOf(State(left, j));
									int rightIndex = stateIndex.IndexOf(State(right, k));
									double score = betas[i][j][k] - System.Math.Log(stateTotal[i]);
									BinaryRule br = new BinaryRule(parentIndex, leftIndex, rightIndex, score);
									bg.AddRule(br);
								}
							}
						}
					}
				}
			}
			// Now build up a UnaryGrammar
			UnaryGrammar ug = new UnaryGrammar(stateIndex);
			foreach (string parent_1 in tempUnaryBetas.FirstKeySet())
			{
				int parentStates = GetStateSplitCount(parent_1);
				double[] stateTotal = totalStateMass[parent_1];
				foreach (string child in tempUnaryBetas.Get(parent_1).Keys)
				{
					int childStates = GetStateSplitCount(child);
					double[][] betas = tempUnaryBetas.Get(parent_1, child);
					for (int i = 0; i < parentStates; ++i)
					{
						if (stateTotal[i] < Epsilon)
						{
							continue;
						}
						for (int j = 0; j < childStates; ++j)
						{
							int parentIndex = stateIndex.IndexOf(State(parent_1, i));
							int childIndex = stateIndex.IndexOf(State(child, j));
							double score = betas[i][j] - System.Math.Log(stateTotal[i]);
							UnaryRule ur = new UnaryRule(parentIndex, childIndex, score);
							ug.AddRule(ur);
						}
					}
				}
			}
			bgug = new Pair<UnaryGrammar, BinaryGrammar>(ug, bg);
		}

		public virtual void SaveTrees(ICollection<Tree> trees1, double weight1, ICollection<Tree> trees2, double weight2)
		{
			trainSize = 0.0;
			int treeCount = 0;
			trees.Clear();
			treeWeights.Clear();
			foreach (Tree tree in trees1)
			{
				trees.Add(tree);
				treeWeights.IncrementCount(tree, weight1);
				trainSize += weight1;
			}
			treeCount += trees1.Count;
			if (trees2 != null && weight2 >= 0.0)
			{
				foreach (Tree tree_1 in trees2)
				{
					trees.Add(tree_1);
					treeWeights.IncrementCount(tree_1, weight2);
					trainSize += weight2;
				}
				treeCount += trees2.Count;
			}
			log.Info("Found " + treeCount + " trees with total weight " + trainSize);
		}

		public virtual void Extract(ICollection<Tree> treeList)
		{
			Extract(treeList, 1.0, null, 0.0);
		}

		/// <summary>First, we do a few setup steps.</summary>
		/// <remarks>
		/// First, we do a few setup steps.  We read in all the trees, which
		/// is necessary because we continually reprocess them and use the
		/// object pointers as hash keys rather than hashing the trees
		/// themselves.  We then count the initial states in the treebank.
		/// <br />
		/// Having done that, we then assign initial probabilities to the
		/// trees.  At first, each state has 1.0 of the probability mass for
		/// each Ax-ByCz and Ax-By transition.  We then split the number of
		/// states and the probabilities on each tree.
		/// <br />
		/// We then repeatedly recalculate the betas and reannotate the
		/// weights, going until we converge, which is defined as no betas
		/// move more then epsilon.
		/// <br />
		/// java -mx4g edu.stanford.nlp.parser.lexparser.LexicalizedParser  -PCFG -saveToSerializedFile englishSplit.ser.gz -saveToTextFile englishSplit.txt -maxLength 40 -train ../data/wsj/wsjtwentytrees.mrg    -testTreebank ../data/wsj/wsjtwentytrees.mrg   -evals "factDA,tsv" -uwm 0  -hMarkov 0 -vMarkov 0 -simpleBinarizedLabels -noRebinarization -predictSplits -splitTrainingThreads 1 -splitCount 1 -splitRecombineRate 0.5
		/// <br />
		/// may also need
		/// <br />
		/// -smoothTagsThresh 0
		/// <br />
		/// java -mx8g edu.stanford.nlp.parser.lexparser.LexicalizedParser -evals "factDA,tsv" -PCFG -vMarkov 0 -hMarkov 0 -uwm 0 -saveToSerializedFile wsjS1.ser.gz -maxLength 40 -train /afs/ir/data/linguistic-data/Treebank/3/parsed/mrg/wsj 200-2199 -testTreebank /afs/ir/data/linguistic-data/Treebank/3/parsed/mrg/wsj 2200-2219 -compactGrammar 0 -simpleBinarizedLabels -predictSplits -smoothTagsThresh 0 -splitCount 1 -noRebinarization
		/// </remarks>
		public virtual void Extract(ICollection<Tree> trees1, double weight1, ICollection<Tree> trees2, double weight2)
		{
			SaveTrees(trees1, weight1, trees2, weight2);
			CountOriginalStates();
			// Initial betas will be 1 for all possible unary and binary
			// transitions in our treebank
			InitialBetasAndLexicon();
			for (int cycle = 0; cycle < op.trainOptions.splitCount; ++cycle)
			{
				// All states except the root state get split into 2
				SplitStateCounts();
				// first, recalculate the betas and the lexicon for having split
				// the transitions
				RecalculateBetas(true);
				// now, loop until we converge while recalculating betas
				// TODO: add a loop counter, stop after X iterations
				iteration = 0;
				bool converged = false;
				while (!converged && iteration < MaxIterations)
				{
					if (Debug())
					{
						System.Console.Out.WriteLine();
						System.Console.Out.WriteLine();
						System.Console.Out.WriteLine("-------------------");
						System.Console.Out.WriteLine("Iteration " + iteration);
					}
					converged = RecalculateBetas(false);
					++iteration;
				}
				log.Info("Converged for cycle " + cycle + " in " + iteration + " iterations");
				MergeStates();
			}
			// Build up the state index.  The BG & UG both expect a set count
			// of states.
			BuildStateIndex();
			BuildGrammars();
		}
	}
}
