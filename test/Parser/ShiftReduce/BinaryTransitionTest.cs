using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Test a couple transition operations and their effects</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class BinaryTransitionTest
	{
		// TODO: add tests for isLegal
		// test states where BinaryTransition could not apply (eg stack too small)
		// test compound transitions
		public static State BuildState(int shifts)
		{
			string[] words = new string[] { "This", "is", "a", "short", "test", "." };
			string[] tags = new string[] { "DT", "VBZ", "DT", "JJ", "NN", "." };
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
			IList<TaggedWord> sentence = SentenceUtils.ToTaggedList(Arrays.AsList(words), Arrays.AsList(tags));
			State state = ShiftReduceParser.InitialStateFromTaggedSentence(sentence);
			ShiftTransition shift = new ShiftTransition();
			for (int i = 0; i < shifts; ++i)
			{
				state = shift.Apply(state);
			}
			NUnit.Framework.Assert.AreEqual(shifts, state.tokenPosition);
			return state;
		}

		[NUnit.Framework.Test]
		public virtual void TestLeftTransition()
		{
			State state = BuildState(2);
			BinaryTransition transition = new BinaryTransition("NP", BinaryTransition.Side.Left);
			state = transition.Apply(state);
			NUnit.Framework.Assert.AreEqual(2, state.tokenPosition);
			NUnit.Framework.Assert.AreEqual(1, state.stack.Size());
			NUnit.Framework.Assert.AreEqual(2, state.stack.Peek().Children().Length);
			NUnit.Framework.Assert.AreEqual("NP", state.stack.Peek().Value());
			CheckHeads(state.stack.Peek(), state.stack.Peek().Children()[0]);
		}

		[NUnit.Framework.Test]
		public virtual void TestRightTransition()
		{
			State state = BuildState(2);
			BinaryTransition transition = new BinaryTransition("NP", BinaryTransition.Side.Right);
			state = transition.Apply(state);
			NUnit.Framework.Assert.AreEqual(2, state.tokenPosition);
			NUnit.Framework.Assert.AreEqual(1, state.stack.Size());
			NUnit.Framework.Assert.AreEqual(2, state.stack.Peek().Children().Length);
			NUnit.Framework.Assert.AreEqual("NP", state.stack.Peek().Value());
			CheckHeads(state.stack.Peek(), state.stack.Peek().Children()[1]);
		}

		public virtual void CheckHeads(Tree t1, Tree t2)
		{
			NUnit.Framework.Assert.IsTrue(t1.Label() is CoreLabel);
			NUnit.Framework.Assert.IsTrue(t2.Label() is CoreLabel);
			CoreLabel l1 = (CoreLabel)t1.Label();
			CoreLabel l2 = (CoreLabel)t2.Label();
			NUnit.Framework.Assert.AreEqual(l1.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)), l2.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)));
			NUnit.Framework.Assert.AreEqual(l1.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)), l2.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)));
		}
	}
}
