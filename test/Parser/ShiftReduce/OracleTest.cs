using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class OracleTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBuildParentMap()
		{
			Tree tree = Tree.ValueOf("(A (B foo) (C bar))");
			IDictionary<Tree, Tree> parents = Oracle.BuildParentMap(tree);
			int total = RecursiveTestBuildParentMap(tree, parents);
			NUnit.Framework.Assert.AreEqual(total, parents.Count);
		}

		public static int RecursiveTestBuildParentMap(Tree tree, IDictionary<Tree, Tree> parents)
		{
			int children = tree.Children().Length;
			foreach (Tree child in tree.Children())
			{
				NUnit.Framework.Assert.AreEqual(tree, parents[child]);
				children += RecursiveTestBuildParentMap(child, parents);
			}
			return children;
		}

		internal string[] TestTrees = new string[] { "(ROOT (S (S (NP (PRP I)) (VP (VBP like) (NP (JJ big) (NNS butts)))) (CC and) (S (NP (PRP I)) (VP (MD can) (RB not) (VP (VB lie)))) (. .)))", "(ROOT (S (NP (NP (RB Not) (PDT all) (DT those)) (SBAR (WHNP (WP who)) (S (VP (VBD wrote))))) (VP (VBP oppose) (NP (DT the) (NNS changes))) (. .)))"
			, "(ROOT (S (NP (NP (DT The) (NNS anthers)) (PP (IN in) (NP (DT these) (NNS plants)))) (VP (VBP are) (ADJP (JJ difficult) (SBAR (S (VP (TO to) (VP (VB clip) (PRT (RP off)))))))) (. .)))" };

		// A small variety of trees to test on, especially with different depths of unary transitions
		public virtual IList<Tree> BuildTestTreebank()
		{
			MemoryTreebank treebank = new MemoryTreebank();
			foreach (string text in TestTrees)
			{
				Tree tree = Tree.ValueOf(text);
				treebank.Add(tree);
			}
			IList<Tree> binarizedTrees = ShiftReduceParser.BinarizeTreebank(treebank, new Options());
			return binarizedTrees;
		}

		/// <summary>
		/// Tests that if you give the Oracle a tree and ask it for a
		/// sequence of transitions, applying the given transition each time,
		/// it produces the original tree again.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestEndToEndCompoundUnaries()
		{
			IList<Tree> binarizedTrees = BuildTestTreebank();
			Oracle oracle = new Oracle(binarizedTrees, true, Java.Util.Collections.Singleton("ROOT"));
			RunEndToEndTest(binarizedTrees, oracle);
		}

		[NUnit.Framework.Test]
		public virtual void TestEndToEndSingleUnaries()
		{
			IList<Tree> binarizedTrees = BuildTestTreebank();
			Oracle oracle = new Oracle(binarizedTrees, false, Java.Util.Collections.Singleton("ROOT"));
			RunEndToEndTest(binarizedTrees, oracle);
		}

		public static void RunEndToEndTest(IList<Tree> binarizedTrees, Oracle oracle)
		{
			for (int index = 0; index < binarizedTrees.Count; ++index)
			{
				State state = ShiftReduceParser.InitialStateFromGoldTagTree(binarizedTrees[index]);
				while (!state.IsFinished())
				{
					OracleTransition gold = oracle.GoldTransition(index, state);
					NUnit.Framework.Assert.IsTrue(gold.transition != null);
					state = gold.transition.Apply(state);
				}
				NUnit.Framework.Assert.AreEqual(binarizedTrees[index], state.stack.Peek());
			}
		}
	}
}
