using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Test a couple transition operations and their effects</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ShiftTransitionTest
	{
		// TODO: add test for isLegal
		[NUnit.Framework.Test]
		public virtual void TestTransition()
		{
			string[] words = new string[] { "This", "is", "a", "short", "test", "." };
			string[] tags = new string[] { "DT", "VBZ", "DT", "JJ", "NN", "." };
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
			IList<TaggedWord> sentence = SentenceUtils.ToTaggedList(Arrays.AsList(words), Arrays.AsList(tags));
			State state = ShiftReduceParser.InitialStateFromTaggedSentence(sentence);
			ShiftTransition shift = new ShiftTransition();
			for (int i = 0; i < 3; ++i)
			{
				state = shift.Apply(state);
			}
			NUnit.Framework.Assert.AreEqual(3, state.tokenPosition);
		}
	}
}
