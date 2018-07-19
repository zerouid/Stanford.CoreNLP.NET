using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>Tests the any kind of IOB-style notation processing.</summary>
	/// <remarks>
	/// Tests the any kind of IOB-style notation processing.
	/// In particular, this tests the IOB encoding results counting.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class IOBUtilsTest
	{
		private static readonly string[] words = new string[] { "Deportivo", "scored", "when", "AJ", "Auxerre", "playmaker", "Corentine", "Angelo", "Martins", "tripped", "on", "Brazilian-born", "Spanish", "Donato", "." };

		private static readonly string[] iob1 = new string[] { "I-ORG", "O", "O", "I-ORG", "I-ORG", "O", "I-PER", "I-PER", "I-PER", "O", "O", "I-MISC", "B-MISC", "I-PER", "O" };

		private static readonly string[] iob2 = new string[] { "B-ORG", "O", "O", "B-ORG", "I-ORG", "O", "B-PER", "I-PER", "I-PER", "O", "O", "B-MISC", "B-MISC", "B-PER", "O" };

		private static readonly string[] iobes = new string[] { "S-ORG", "O", "O", "B-ORG", "E-ORG", "O", "B-PER", "I-PER", "E-PER", "O", "O", "S-MISC", "S-MISC", "S-PER", "O" };

		private static readonly string[] io = new string[] { "I-ORG", "O", "O", "I-ORG", "I-ORG", "O", "I-PER", "I-PER", "I-PER", "O", "O", "I-MISC", "I-MISC", "I-PER", "O" };

		private static readonly string[] noprefix = new string[] { "ORG", "O", "O", "ORG", "ORG", "O", "PER", "PER", "PER", "O", "O", "MISC", "MISC", "PER", "O" };

		private static readonly string[] bilou = new string[] { "U-ORG", "O", "O", "B-ORG", "L-ORG", "O", "B-PER", "I-PER", "L-PER", "O", "O", "U-MISC", "U-MISC", "U-PER", "O" };

		[NUnit.Framework.Test]
		public virtual void TestIOB1IOB2()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob1);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iob2", true);
			CheckAnswers(testInput, words, iob2);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB1IOB1()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob1);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iob1", true);
			CheckAnswers(testInput, words, iob1);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB2IOB1()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob2);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iob1", true);
			CheckAnswers(testInput, words, iob1);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB2IOBES()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob2);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iobes", true);
			CheckAnswers(testInput, words, iobes);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOBESIOB1()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iobes);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iob1", true);
			CheckAnswers(testInput, words, iob1);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB1IO()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob1);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "io", true);
			CheckAnswers(testInput, words, io);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB1NoPrefix()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob1);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "noprefix", true);
			CheckAnswers(testInput, words, noprefix);
		}

		[NUnit.Framework.Test]
		public virtual void TestNoPrefixIO()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, noprefix);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "io", true);
			CheckAnswers(testInput, words, io);
		}

		[NUnit.Framework.Test]
		public virtual void TestBILOUIOBES()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, bilou);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "iobes", true);
			CheckAnswers(testInput, words, iobes);
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB2BILOU()
		{
			IList<CoreLabel> testInput = LoadCoreLabelList(words, iob2);
			IOBUtils.EntitySubclassify(testInput, typeof(CoreAnnotations.AnswerAnnotation), "O", "BILOU", true);
			CheckAnswers(testInput, words, bilou);
		}

		private static IList<CoreLabel> LoadCoreLabelList(string[] words, string[] answers)
		{
			IList<CoreLabel> testInput = new List<CoreLabel>();
			string[] fields = new string[] { "word", "answer" };
			string[] values = new string[2];
			NUnit.Framework.Assert.AreEqual(words.Length, answers.Length);
			for (int i = 0; i < words.Length; i++)
			{
				values[0] = words[i];
				values[1] = answers[i];
				CoreLabel c = new CoreLabel(fields, values);
				testInput.Add(c);
			}
			return testInput;
		}

		private static void CheckAnswers(IList<CoreLabel> testInput, string[] words, string[] answers)
		{
			for (int i = 0; i < testInput.Count; i++)
			{
				NUnit.Framework.Assert.AreEqual("Wrong for " + words[i], answers[i], testInput[i].Get(typeof(CoreAnnotations.AnswerAnnotation)));
			}
		}

		private const string Bg = "O";

		private static readonly string[][] labelsIOB2 = new string[][] { new string[] { Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "I-A", Bg, Bg, Bg, Bg
			 }, new string[] { Bg, Bg, Bg, "I-A", "I-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "I-B", Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "B-A", Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, 
			Bg, "I-A" } };

		// 0
		// 1
		// 2
		// 3
		// 4
		// 5
		// 6
		private static void RunIOBResultsTest(string[] gold, string[] guess, double tp, double fp, double fn)
		{
			IList<CoreLabel> sentence = MakeListCoreLabel(gold, guess);
			ICounter<string> entityTP = new ClassicCounter<string>();
			ICounter<string> entityFP = new ClassicCounter<string>();
			ICounter<string> entityFN = new ClassicCounter<string>();
			IOBUtils.CountEntityResults(sentence, entityTP, entityFP, entityFN, Bg);
			NUnit.Framework.Assert.AreEqual("For true positives", tp, entityTP.TotalCount(), 0.0001);
			NUnit.Framework.Assert.AreEqual("For false positives", fp, entityFP.TotalCount(), 0.0001);
			NUnit.Framework.Assert.AreEqual("For false negatives", fn, entityFN.TotalCount(), 0.0001);
		}

		private static IList<CoreLabel> MakeListCoreLabel(string[] gold, string[] guess)
		{
			NUnit.Framework.Assert.AreEqual("Cannot run test on lists of different length", gold.Length, guess.Length);
			IList<CoreLabel> sentence = new List<CoreLabel>();
			for (int i = 0; i < gold.Length; ++i)
			{
				CoreLabel word = new CoreLabel();
				word.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), gold[i]);
				word.Set(typeof(CoreAnnotations.AnswerAnnotation), guess[i]);
				sentence.Add(word);
			}
			return sentence;
		}

		[NUnit.Framework.Test]
		public virtual void TestIOB2Results()
		{
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[0], 0, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[1], 0, 1, 0);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[1], 1, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[2], 0, 1, 0);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[2], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[1], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[2], 1, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[3], 0, 1, 0);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[3], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[1], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[3], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[2], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[3], 1, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[4], 0, 2, 0);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[0], 0, 0, 2);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[4], 1, 1, 0);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[1], 1, 0, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[4], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[2], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[4], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[3], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[4], 2, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[5], 0, 2, 0);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[0], 0, 0, 2);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[5], 1, 1, 0);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[1], 1, 0, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[5], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[2], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[5], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[3], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[5], 1, 1, 1);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[4], 1, 1, 1);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[5], 2, 0, 0);
			RunIOBResultsTest(labelsIOB2[0], labelsIOB2[6], 0, 1, 0);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB2[1], labelsIOB2[6], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[1], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[2], labelsIOB2[6], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[2], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[3], labelsIOB2[6], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[3], 0, 1, 1);
			RunIOBResultsTest(labelsIOB2[4], labelsIOB2[6], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[4], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[5], labelsIOB2[6], 0, 1, 2);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[5], 0, 2, 1);
			RunIOBResultsTest(labelsIOB2[6], labelsIOB2[6], 1, 0, 0);
		}

		private static readonly string[][] labelsIOB = new string[][] { new string[] { Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "B-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "B-A", "I-A", Bg, Bg, Bg, Bg }
			, new string[] { Bg, Bg, Bg, "B-A", "I-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "B-A", "B-A", Bg, Bg, Bg, Bg } };

		// 0
		// 1
		// 2
		// 3
		// 4
		[NUnit.Framework.Test]
		public virtual void TestIOBResults()
		{
			// gold, guess, tp, fp, fn
			RunIOBResultsTest(labelsIOB[0], labelsIOB[0], 0, 0, 0);
			RunIOBResultsTest(labelsIOB[0], labelsIOB[1], 0, 1, 0);
			RunIOBResultsTest(labelsIOB[1], labelsIOB[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB[1], labelsIOB[1], 1, 0, 0);
			RunIOBResultsTest(labelsIOB[0], labelsIOB[2], 0, 1, 0);
			RunIOBResultsTest(labelsIOB[2], labelsIOB[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB[2], labelsIOB[2], 1, 0, 0);
			RunIOBResultsTest(labelsIOB[0], labelsIOB[3], 0, 1, 0);
			RunIOBResultsTest(labelsIOB[3], labelsIOB[0], 0, 0, 1);
			RunIOBResultsTest(labelsIOB[1], labelsIOB[3], 0, 1, 1);
			RunIOBResultsTest(labelsIOB[3], labelsIOB[1], 0, 1, 1);
			RunIOBResultsTest(labelsIOB[2], labelsIOB[3], 0, 1, 1);
			RunIOBResultsTest(labelsIOB[3], labelsIOB[2], 0, 1, 1);
			RunIOBResultsTest(labelsIOB[3], labelsIOB[3], 1, 0, 0);
			RunIOBResultsTest(labelsIOB[2], labelsIOB[4], 0, 2, 1);
			RunIOBResultsTest(labelsIOB[4], labelsIOB[2], 0, 1, 2);
		}

		private static readonly string[][] labelsIOE = new string[][] { new string[] { Bg, Bg, Bg, Bg, "I-A", "E-A", "I-A", Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "L-A", "I-A", Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "I-A", 
			"I-A", Bg, Bg, Bg } };

		// 0
		// 1
		// 2
		[NUnit.Framework.Test]
		public virtual void TestIOEResults()
		{
			// gold, guess, tp, fp, fn
			RunIOBResultsTest(labelsIOE[0], labelsIOE[1], 2, 0, 0);
			RunIOBResultsTest(labelsIOE[0], labelsIOE[2], 0, 1, 2);
			RunIOBResultsTest(labelsIOE[2], labelsIOE[0], 0, 2, 1);
			RunIOBResultsTest(labelsIOE[0], labelsIOB[2], 1, 0, 1);
		}

		private static readonly string[][] labelsIO = new string[][] { new string[] { Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, Bg, "I-A", "I-A", Bg, Bg, Bg, Bg }
			, new string[] { Bg, Bg, Bg, "I-A", "I-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "I-A", "I-A", "I-A", "I-A", Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "I-A", "I-B", "I-B", "I-A", Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "I-A", "I-A"
			, "I-B", "I-A", Bg, Bg, Bg } };

		// 0
		// 1
		// 2
		// 3
		// 4
		// 5
		// 6
		[NUnit.Framework.Test]
		public virtual void TestIOResults()
		{
			// gold, guess, tp, fp, fn
			RunIOBResultsTest(labelsIOB[2], labelsIO[2], 1, 0, 0);
			RunIOBResultsTest(labelsIOB[4], labelsIO[2], 0, 1, 2);
			RunIOBResultsTest(labelsIO[2], labelsIOB[2], 1, 0, 0);
			RunIOBResultsTest(labelsIO[2], labelsIOB[4], 0, 2, 1);
			RunIOBResultsTest(labelsIO[4], labelsIO[5], 0, 3, 1);
			RunIOBResultsTest(labelsIO[4], labelsIO[6], 0, 3, 1);
			RunIOBResultsTest(labelsIO[5], labelsIO[6], 1, 2, 2);
		}

		private static readonly string[][] labelsIOBES = new string[][] { new string[] { Bg, Bg, Bg, "B-A", "E-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "B-A", "L-A", Bg, Bg, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "B-A", "I-A", "I-A", "E-A"
			, Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "B-A", "I-A", "I-A", "L-A", Bg, Bg, Bg }, new string[] { Bg, Bg, Bg, "B-A", "L-A", "U-A", "U-A", Bg, Bg, Bg } };

		// 0
		// 1
		// 2
		// 3
		// 4
		[NUnit.Framework.Test]
		public virtual void TestIOBESResults()
		{
			// gold, guess, tp, fp, fn
			RunIOBResultsTest(labelsIOBES[0], labelsIOBES[1], 1, 0, 0);
			RunIOBResultsTest(labelsIOBES[4], labelsIOBES[0], 1, 0, 2);
			RunIOBResultsTest(labelsIOBES[2], labelsIOBES[3], 1, 0, 0);
			RunIOBResultsTest(labelsIOBES[2], labelsIOBES[4], 0, 3, 1);
		}
	}
}
