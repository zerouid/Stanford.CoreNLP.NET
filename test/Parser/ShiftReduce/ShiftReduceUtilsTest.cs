using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ShiftReduceUtilsTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBinarySide()
		{
			string[] words = new string[] { "This", "is", "a", "short", "test", "." };
			string[] tags = new string[] { "DT", "VBZ", "DT", "JJ", "NN", "." };
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
			IList<TaggedWord> sentence = SentenceUtils.ToTaggedList(Arrays.AsList(words), Arrays.AsList(tags));
			State state = ShiftReduceParser.InitialStateFromTaggedSentence(sentence);
			ShiftTransition shift = new ShiftTransition();
			state = shift.Apply(shift.Apply(state));
			BinaryTransition transition = new BinaryTransition("NP", BinaryTransition.Side.Right);
			State next = transition.Apply(state);
			NUnit.Framework.Assert.AreEqual(BinaryTransition.Side.Right, ShiftReduceUtils.GetBinarySide(next.stack.Peek()));
			transition = new BinaryTransition("NP", BinaryTransition.Side.Left);
			next = transition.Apply(state);
			NUnit.Framework.Assert.AreEqual(BinaryTransition.Side.Left, ShiftReduceUtils.GetBinarySide(next.stack.Peek()));
		}
	}
}
