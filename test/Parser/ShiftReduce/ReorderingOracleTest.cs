using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>
	/// Test the results that come back when you run the ReorderingOracle
	/// on various inputs
	/// </summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ReorderingOracleTest
	{
		internal FinalizeTransition finalize = new FinalizeTransition(Collections.Singleton("ROOT"));

		internal ShiftTransition shift = new ShiftTransition();

		internal BinaryTransition rightNP = new BinaryTransition("NP", BinaryTransition.Side.Right);

		internal BinaryTransition tempRightNP = new BinaryTransition("@NP", BinaryTransition.Side.Right);

		internal BinaryTransition leftNP = new BinaryTransition("NP", BinaryTransition.Side.Left);

		internal BinaryTransition tempLeftNP = new BinaryTransition("@NP", BinaryTransition.Side.Left);

		internal BinaryTransition rightVP = new BinaryTransition("VP", BinaryTransition.Side.Right);

		internal BinaryTransition tempRightVP = new BinaryTransition("@VP", BinaryTransition.Side.Right);

		internal BinaryTransition leftVP = new BinaryTransition("VP", BinaryTransition.Side.Left);

		internal BinaryTransition tempLeftVP = new BinaryTransition("@VP", BinaryTransition.Side.Left);

		internal BinaryTransition rightS = new BinaryTransition("S", BinaryTransition.Side.Right);

		internal BinaryTransition tempRightS = new BinaryTransition("@S", BinaryTransition.Side.Right);

		internal BinaryTransition leftS = new BinaryTransition("S", BinaryTransition.Side.Left);

		internal BinaryTransition tempLeftS = new BinaryTransition("@S", BinaryTransition.Side.Left);

		internal UnaryTransition unaryADVP = new UnaryTransition("ADVP", false);

		internal string[] Words = new string[] { "My", "dog", "also", "likes", "eating", "sausage" };

		internal string[] Tags = new string[] { "PRP$", "NN", "RB", "VBZ", "VBZ", "NN" };

		internal IList<TaggedWord> sentence;

		internal Tree[] correctTrees = new Tree[] { Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))"), Tree.ValueOf("(NP (NP (NN A) (NN B)) (NN C))"), Tree.ValueOf("(ROOT (S (NP (PRP$ My) (JJ small) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))"
			) };

		internal IList<Tree> binarizedTrees;

		internal Tree[] incorrectShiftTrees = new Tree[] { Tree.ValueOf("(ROOT (S (PRP$ My) (NN dog) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))"), Tree.ValueOf("(NP (NN A) (NN B) (NN C))"), Tree.ValueOf("(ROOT (S (PRP$ My) (JJ small) (NN dog) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))"
			) };

		internal Debinarizer debinarizer = new Debinarizer(false);

		// doesn't have to make sense
		// initialized in setUp
		// doesn't have to make sense
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			Options op = new Options();
			Treebank treebank = op.tlpParams.MemoryTreebank();
			Sharpen.Collections.AddAll(treebank, Arrays.AsList(correctTrees));
			binarizedTrees = ShiftReduceParser.BinarizeTreebank(treebank, op);
		}

		public virtual IList<ITransition> BuildTransitionList(params ITransition[] transitions)
		{
			return Generics.NewLinkedList(Arrays.AsList(transitions));
		}

		[NUnit.Framework.Test]
		public virtual void TestReorderIncorrectBinaryTransition()
		{
			IList<ITransition> transitions = BuildTransitionList(shift, rightNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectBinaryTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, rightVP, finalize), transitions);
			transitions = BuildTransitionList(shift, unaryADVP, rightNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectBinaryTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(shift, rightNP, unaryADVP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectBinaryTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, rightVP, finalize), transitions);
		}

		[NUnit.Framework.Test]
		public virtual void TestReorderIncorrectShiftResultingTree()
		{
			for (int testcase = 0; testcase < correctTrees.Length; ++testcase)
			{
				State state = ShiftReduceParser.InitialStateFromGoldTagTree(correctTrees[testcase]);
				IList<ITransition> gold = CreateTransitionSequence.CreateTransitionSequence(binarizedTrees[testcase]);
				// System.err.println(correctTrees[testcase]);
				// System.err.println(gold);
				int tnum = 0;
				for (; tnum < gold.Count; ++tnum)
				{
					if (gold[tnum] is BinaryTransition)
					{
						break;
					}
					state = gold[tnum].Apply(state);
				}
				state = shift.Apply(state);
				IList<ITransition> reordered = Generics.NewLinkedList(gold.SubList(tnum, gold.Count));
				NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(reordered));
				// System.err.println(reordered);
				foreach (ITransition transition in reordered)
				{
					state = transition.Apply(state);
				}
				Tree debinarized = debinarizer.TransformTree(state.stack.Peek());
				// System.err.println(debinarized);
				NUnit.Framework.Assert.AreEqual(incorrectShiftTrees[testcase].ToString(), debinarized.ToString());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestReorderIncorrectShift()
		{
			IList<ITransition> transitions = BuildTransitionList(rightNP, shift, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(rightNP, shift, shift, leftNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, leftNP, tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(rightNP, shift, unaryADVP, shift, leftNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(unaryADVP, shift, leftNP, tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(rightNP, shift, shift, unaryADVP, leftNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(leftNP, shift, shift, unaryADVP, leftNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(leftNP, shift, shift, unaryADVP, leftNP, leftVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempLeftVP, leftVP, finalize), transitions);
			transitions = BuildTransitionList(rightNP, shift, shift, unaryADVP, leftNP, leftVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempLeftVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(leftNP, leftNP, shift, shift, unaryADVP, leftNP, rightVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempRightVP, tempRightVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(leftNP, rightNP, shift, shift, unaryADVP, leftNP, leftVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempLeftVP, tempLeftVP, rightVP, finalize), transitions);
			transitions = BuildTransitionList(leftNP, leftNP, shift, shift, unaryADVP, leftNP, leftVP, finalize);
			NUnit.Framework.Assert.IsTrue(ReorderingOracle.ReorderIncorrectShiftTransition(transitions));
			NUnit.Framework.Assert.AreEqual(BuildTransitionList(shift, unaryADVP, leftNP, tempLeftVP, tempLeftVP, leftVP, finalize), transitions);
		}

		public ReorderingOracleTest()
		{
			sentence = SentenceUtils.ToTaggedList(Arrays.AsList(Words), Arrays.AsList(Tags));
		}
	}
}
