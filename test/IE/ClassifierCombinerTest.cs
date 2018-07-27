using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.IE
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ClassifierCombinerTest
	{
		internal string[] words = new string[] { "Joe", "Smith", "drank", "44", "Budweiser", "cans", "at", "Monaco", "Brewing", "." };

		internal string[] tags = new string[] { "NNP", "NNP", "VBD", "CD", "NNP", "NNS", "IN", "NNP", "NNP", "." };

		internal string[] ans1 = new string[] { "PER", "PER", "O", "O", "ORG", "O", "O", "ORG", "ORG", "O" };

		internal string[] ans2 = new string[] { "O", "O", "O", "NUM", "O", "O", "O", "O", "O", "O" };

		internal string[] ans3 = new string[] { "O", "O", "O", "NUM", "PROD", "PROD", "O", "O", "O", "O" };

		internal string[] ans4 = new string[] { "PER", "PER", "O", "O", "O", "O", "O", "O", "O", "O" };

		internal string[] ans5 = new string[] { "O", "O", "O", "NUM", "PROD", "PROD", "O", "ORG", "ORG", "ORG" };

		internal string[] ans6 = new string[] { "O", "O", "O", "O", "O", "O", "O", "O", "O", "O" };

		internal string[] ans7 = new string[] { "PER", "PER", "O", "NUM", "PROD", "PROD", "O", "ORG", "ORG", "ORG" };

		internal string[] ans8 = new string[] { "O", "O", "O", "PROD", "PROD", "O", "O", "O", "O", "O" };

		internal string[] ans9 = new string[] { "O", "O", "O", "O", "O", "O", "O", "O", "O", "NUM" };

		internal string[] ans10 = new string[] { "O", "O", "O", "O", "O", "O", "O", "O", "NUM", "NUM" };

		internal string[] ans11 = new string[] { "O", "O", "O", "O", "PROD", "PROD", "O", "O", "O", "O" };

		internal string[] ans12 = new string[] { "O", "O", "O", "O", "O", "O", "O", "O", "NUM", "NUM" };

		internal string[] ans13 = new string[] { "O", "O", "O", "O", "O", "O", "NUM", "NUM", "O", "O" };

		internal string[] ans14 = new string[] { "O", "O", "O", "O", "O", "O", "FOO", "FOO", "O", "O" };

		internal string[] ans15 = new string[] { "O", "O", "PER", "PER", "O", "O", "O", "O", "O", "O" };

		internal string[] ans16 = new string[] { "O", "O", "FOO", "FOO", "O", "O", "O", "O", "O", "O" };

		internal string[] out1 = new string[] { "PER", "PER", "O", "NUM", "ORG", "O", "O", "ORG", "ORG", "O" };

		internal string[] out2 = new string[] { "PER", "PER", "O", "NUM", "PROD", "PROD", "O", "ORG", "ORG", "ORG" };

		internal string[] out3 = new string[] { "O", "O", "O", "NUM", "PROD", "PROD", "O", "ORG", "ORG", "ORG" };

		internal string[] out4 = new string[] { "O", "O", "O", "NUM", "O", "O", "O", "O", "O", "NUM" };

		internal string[] out5 = new string[] { "O", "O", "O", "NUM", "O", "O", "O", "O", "NUM", "NUM" };

		internal string[] out6 = new string[] { "O", "O", "O", "O", "O", "O", "NUM", "NUM", "NUM", "NUM" };

		internal string[] out7 = new string[] { "O", "O", "O", "O", "O", "O", "FOO", "FOO", "NUM", "NUM" };

		internal string[] out8 = new string[] { "PER", "PER", "PER", "PER", "O", "O", "O", "O", "O", "O" };

		internal string[] out9 = new string[] { "PER", "PER", "FOO", "FOO", "O", "O", "O", "O", "O", "O" };

		internal string[] out10 = new string[] { "PER", "PER", "O", "NUM", "PROD", "PROD", "O", "O", "O", "O" };

		[NUnit.Framework.Test]
		public virtual void TestCombination()
		{
			// test that a non-conflicting label can be added
			RunTest(ans1, ans2, out1, "NUM");
			// test that a conflicting label isn't added
			RunTest(ans1, ans3, out1, "NUM", "PROD");
			// test that a sequence final label is added (didn't used to work...)
			RunTest(ans4, ans5, out2, "NUM", "PROD", "ORG");
			RunTest(ans5, ans4, out2, "PER");
			// test that a label not in the auxLabels set isn't added
			RunTest(ans6, ans7, out3, "NUM", "PROD", "ORG");
			// test that a sequence initial label is added
			RunTest(ans6, ans7, out2, "NUM", "PROD", "ORG", "PER");
			// test that a label segment that conflicts later on isn't added
			RunTest(ans1, ans8, ans1, "NUM", "PROD", "ORG", "PER");
			// Test that labels that are already in the first sequence are
			// still added if they are present in later sequences
			RunTest(ans2, ans9, out4, "NUM");
			RunTest(ans9, ans2, out4, "NUM");
			RunTest(ans2, ans10, out5, "NUM");
			RunTest(ans10, ans2, out5, "NUM");
			// Test neighbors overlapping
			RunTest(ans8, ans11, ans8, "PROD");
			RunTest(ans11, ans8, ans11, "PROD");
			// Test non-overlapping neighbors at the end of a sequence
			RunTest(ans12, ans13, out6, "NUM");
			RunTest(ans13, ans12, out6, "NUM");
			RunTest(ans12, ans14, out7, "FOO");
			RunTest(ans14, ans12, out7, "NUM");
			// Test non-overlapping neighbors at the start of a sequence
			RunTest(ans4, ans15, out8, "PER");
			RunTest(ans15, ans4, out8, "PER");
			RunTest(ans4, ans16, out9, "FOO");
			RunTest(ans16, ans4, out9, "PER");
			// test consecutive labels
			RunTest(ans3, ans4, out10, "PER", "NUM", "PROD");
			// test consecutive labels
			RunTest(ans4, ans3, out10, "PER", "NUM", "PROD");
			// test a label that conflicted with a main label, followed by a
			// label that doesn't conflict
			RunTest(ans2, ans3, ans3, "NUM", "PROD");
		}

		public virtual void OutputResults(string[] firstInput, string[] secondInput, string[] expectedOutput, params string[] labels)
		{
			IList<CoreLabel> input1 = CoreUtilities.ToCoreLabelList(words, tags, firstInput);
			IList<CoreLabel> input2 = CoreUtilities.ToCoreLabelList(words, tags, secondInput);
			IList<CoreLabel> result = CoreUtilities.ToCoreLabelList(words, tags, expectedOutput);
			ICollection<string> auxLabels = new HashSet<string>();
			foreach (string label in labels)
			{
				auxLabels.Add(label);
			}
			ClassifierCombiner.MergeTwoDocuments(input1, input2, auxLabels, "O");
			foreach (CoreLabel word in input1)
			{
				System.Console.Out.WriteLine(word.Word() + " " + word.Tag() + " " + word.Get(typeof(CoreAnnotations.AnswerAnnotation)));
			}
		}

		public virtual void RunTest(string[] firstInput, string[] secondInput, string[] expectedOutput, params string[] labels)
		{
			IList<CoreLabel> input1 = CoreUtilities.ToCoreLabelList(words, tags, firstInput);
			IList<CoreLabel> input2 = CoreUtilities.ToCoreLabelList(words, tags, secondInput);
			IList<CoreLabel> result = CoreUtilities.ToCoreLabelList(words, tags, expectedOutput);
			ICollection<string> auxLabels = new HashSet<string>();
			foreach (string label in labels)
			{
				auxLabels.Add(label);
			}
			ClassifierCombiner.MergeTwoDocuments(input1, input2, auxLabels, "O");
			NUnit.Framework.Assert.AreEqual(result, input1);
		}
	}
}
