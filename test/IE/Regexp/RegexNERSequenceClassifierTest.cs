using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Regexp
{
	/// <summary>A simple test for the regex ner.</summary>
	/// <remarks>
	/// A simple test for the regex ner.  Writes out a temporary file with
	/// some patterns.  It then reads in those patterns to a couple regex
	/// ner classifiers, tests them on a couple sentences, and makes sure
	/// it gets the expected results.
	/// </remarks>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	public class RegexNERSequenceClassifierTest
	{
		private static File tempFile;

		private static readonly string[] words = new string[] { "My dog likes to eat sausage : turkey , pork , beef , etc .", "I went to Shoreline Park and saw an avocet and some curlews ( shorebirds ) ." };

		private static readonly string[] tags = new string[] { "PRP$ NN RB VBZ VBG NN : NN , NN , NN , FW .", "PRP VBD TO NNP NNP CC VBD DT NN CC DT NNS -LRB- NNP -RRB- ." };

		private static readonly string[] ner = new string[] { "O O O O O O O O O O O O O O O", "O O O LOCATION LOCATION O O O O O O O O O O O" };

		private static readonly string[] expectedUncased = new string[] { "- - - - - food - - - - - - - - -", "- - - park park - - - shorebird - - shorebird - - - -" };

		private static readonly string[] expectedCased = new string[] { "- - - - - food - - - - - - - - -", "- - - - - - - - shorebird - - shorebird - - - -" };

		private static readonly string[] nerPatterns = new string[] { "Shoreline Park\tPARK\n", "Shoreline Park\tPARK\tLOCATION\n", "Shoreline\tPARK\n", "Shoreline Park and\tPARK\tLOCATION\n", "My\tPOSS\nsausage \\:\tFOO\n", "My\tPOSS\nsausage :\tFOO\n"
			, "My\tPOSS\n\\. \\.\tFOO\n", "\\.\tPERIOD\n", ".\tPERIOD\n", "\\(|\\)\tPAREN\n" };

		private static readonly string[][] expectedNER = new string[][] { new string[] { "- - - - - - - - - - - - - - -", "- - - - - - - - - - - - - - - -" }, new string[] { "- - - - - - - - - - - - - - -", "- - - PARK PARK - - - - - - - - - - -" }, 
			new string[] { "- - - - - - - - - - - - - - -", "- - - - - - - - - - - - - - - -" }, new string[] { "- - - - - - - - - - - - - - -", "- - - PARK PARK PARK - - - - - - - - - -" }, new string[] { "POSS - - - - FOO FOO - - - - - - - -", "- - - - - - - - - - - - - - - -"
			 }, new string[] { "POSS - - - - FOO FOO - - - - - - - -", "- - - - - - - - - - - - - - - -" }, new string[] { "POSS - - - - - - - - - - - - - -", "- - - - - - - - - - - - - - - -" }, new string[] { "- - - - - - - - - - - - - - PERIOD", "- - - - - - - - - - - - - - - PERIOD"
			 }, new string[] { "- - - - - - PERIOD - PERIOD - PERIOD - PERIOD - PERIOD", "PERIOD - - - - - - - - - - - PERIOD - PERIOD PERIOD" }, new string[] { "- - - - - - - - - - - - - - -", "- - - - - - - - - - - - PAREN - PAREN -" } };

		public IList<IList<CoreLabel>> sentences;

		private IList<IList<CoreLabel>> NERsentences;

		// = null;
		// not clear it should do this, but does, as it's only tokenwise compatibility
		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			lock (typeof(RegexNERSequenceClassifierTest))
			{
				if (tempFile == null)
				{
					tempFile = File.CreateTempFile("regexnertest.patterns", "txt");
					FileWriter fout = new FileWriter(tempFile);
					BufferedWriter bout = new BufferedWriter(fout);
					bout.Write("sausage\tfood\n");
					bout.Write("(avocet|curlew)(s?)\tshorebird\n");
					bout.Write("shoreline park\tpark\n");
					bout.Flush();
					fout.Close();
				}
			}
			sentences = new List<IList<CoreLabel>>();
			NERsentences = new List<IList<CoreLabel>>();
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
			NUnit.Framework.Assert.AreEqual(words.Length, ner.Length);
			for (int snum = 0; snum < words.Length; ++snum)
			{
				string[] wordPieces = words[snum].Split(" ");
				string[] tagPieces = tags[snum].Split(" ");
				string[] nerPieces = ner[snum].Split(" ");
				NUnit.Framework.Assert.AreEqual(wordPieces.Length, tagPieces.Length);
				NUnit.Framework.Assert.AreEqual(wordPieces.Length, nerPieces.Length, "Input " + snum + " " + words[snum] + " of different length than " + ner[snum]);
				IList<CoreLabel> sentence = new List<CoreLabel>();
				IList<CoreLabel> NERsentence = new List<CoreLabel>();
				for (int wnum = 0; wnum < wordPieces.Length; ++wnum)
				{
					CoreLabel token = new CoreLabel();
					token.SetWord(wordPieces[wnum]);
					token.SetTag(tagPieces[wnum]);
					sentence.Add(token);
					CoreLabel NERtoken = new CoreLabel();
					NERtoken.SetWord(wordPieces[wnum]);
					NERtoken.SetTag(tagPieces[wnum]);
					NERtoken.SetNER(nerPieces[wnum]);
					NERsentence.Add(NERtoken);
				}
				sentences.Add(sentence);
				NERsentences.Add(NERsentence);
			}
		}

		private static string ListToString(IList<CoreLabel> sentence)
		{
			StringBuilder sb = null;
			foreach (CoreLabel cl in sentence)
			{
				if (sb == null)
				{
					sb = new StringBuilder("[");
				}
				else
				{
					sb.Append(", ");
				}
				sb.Append(cl.ToShortString());
			}
			if (sb == null)
			{
				sb = new StringBuilder("[");
			}
			sb.Append(']');
			return sb.ToString();
		}

		private static IList<CoreLabel> DeepCopy(IList<CoreLabel> @in)
		{
			IList<CoreLabel> cll = new List<CoreLabel>(@in.Count);
			foreach (CoreLabel cl in @in)
			{
				cll.Add(new CoreLabel(cl));
			}
			return cll;
		}

		private static void CompareAnswers(string[] expected, IList<CoreLabel> sentence)
		{
			NUnit.Framework.Assert.AreEqual(expected.Length, sentence.Count, "Lengths different for " + StringUtils.Join(expected) + " and " + SentenceUtils.ListToString(sentence));
			string str = "Comparing " + Arrays.ToString(expected) + " and " + ListToString(sentence);
			for (int i = 0; i < expected.Length; ++i)
			{
				if (expected[i].Equals("-"))
				{
					NUnit.Framework.Assert.AreEqual(null, sentence[i].Get(typeof(CoreAnnotations.AnswerAnnotation)), str);
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(expected[i], sentence[i].Get(typeof(CoreAnnotations.AnswerAnnotation)), str);
				}
			}
		}

		[Test]
		public virtual void TestUncased()
		{
			string tempFilename = tempFile.GetPath();
			RegexNERSequenceClassifier uncased = new RegexNERSequenceClassifier(tempFilename, true, false);
			CheckSentences(sentences, uncased, expectedUncased);
		}

		private static void CheckSentences(IList<IList<CoreLabel>> sentences, RegexNERSequenceClassifier uncased, string[] expectedOutput)
		{
			NUnit.Framework.Assert.AreEqual(expectedOutput.Length, sentences.Count);
			for (int i = 0; i < sentences.Count; ++i)
			{
				IList<CoreLabel> sentence = DeepCopy(sentences[i]);
				uncased.Classify(sentence);
				string[] answers = expectedOutput[i].Split(" ");
				CompareAnswers(answers, sentence);
			}
		}

		[Test]
		public virtual void TestCased()
		{
			string tempFilename = tempFile.GetPath();
			RegexNERSequenceClassifier cased = new RegexNERSequenceClassifier(tempFilename, false, false);
			CheckSentences(sentences, cased, expectedCased);
		}

		[Test]
		public virtual void TestNEROverlaps()
		{
			NUnit.Framework.Assert.AreEqual(nerPatterns.Length, expectedNER.Length);
			for (int k = 0; k < nerPatterns.Length; k++)
			{
				BufferedReader r1 = new BufferedReader(new StringReader(nerPatterns[k]));
				RegexNERSequenceClassifier cased = new RegexNERSequenceClassifier(r1, false, false, null);
				NUnit.Framework.Assert.AreEqual(NERsentences.Count, expectedNER[k].Length);
				for (int i = 0; i < NERsentences.Count; ++i)
				{
					IList<CoreLabel> sentence = DeepCopy(NERsentences[i]);
					cased.Classify(sentence);
					string[] answers = expectedNER[k][i].Split(" ");
					CompareAnswers(answers, sentence);
				}
			}
		}
		// System.err.println("Completed test " + k);
	}
}
