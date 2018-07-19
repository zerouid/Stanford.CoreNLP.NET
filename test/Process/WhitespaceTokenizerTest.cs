using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class WhitespaceTokenizerTest
	{
		public static readonly string[] Test = new string[] { "This is a test . \n This is a second line .", "A \n B \n \n C", "A. B", "皇后\u3000\u3000後世 and (800)\u00A0326-1456" };

		public static readonly string[][] ResultsNoEol = new string[][] { new string[] { "This", "is", "a", "test", ".", "This", "is", "a", "second", "line", "." }, new string[] { "A", "B", "C" }, new string[] { "A.", "B" }, new string[] { "皇后", "後世"
			, "and", "(800)\u00A0326-1456" } };

		public static readonly string[][] ResultsEol = new string[][] { new string[] { "This", "is", "a", "test", ".", "\n", "This", "is", "a", "second", "line", "." }, new string[] { "A", "\n", "B", "\n", "\n", "C" }, new string[] { "A.", "B" }, new 
			string[] { "皇后", "後世", "and", "(800)\u00A0326-1456" } };

		public virtual void RunTest<_T0>(ITokenizerFactory<_T0> factory, string[] testStrings, string[][] resultsStrings)
			where _T0 : IHasWord
		{
			for (int i = 0; i < testStrings.Length; ++i)
			{
				ITokenizer<IHasWord> tokenizer = factory.GetTokenizer(new StringReader(testStrings[i]));
				IList<IHasWord> tokens = tokenizer.Tokenize();
				NUnit.Framework.Assert.AreEqual(resultsStrings[i].Length, tokens.Count);
				for (int j = 0; j < resultsStrings[i].Length; ++j)
				{
					NUnit.Framework.Assert.AreEqual(resultsStrings[i][j], tokens[j].Word());
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestWordTokenizer()
		{
			RunTest(WhitespaceTokenizer.Factory(false), Test, ResultsNoEol);
			RunTest(WhitespaceTokenizer.Factory(true), Test, ResultsEol);
		}

		[NUnit.Framework.Test]
		public virtual void TestCLTokenizer()
		{
			ILexedTokenFactory<CoreLabel> factory = new CoreLabelTokenFactory();
			RunTest(new WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel>(factory, false), Test, ResultsNoEol);
			RunTest(new WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel>(factory, true), Test, ResultsEol);
		}
	}
}
