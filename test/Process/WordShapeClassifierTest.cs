using System;
using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Process
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class WordShapeClassifierTest
	{
		private static string[] inputs = new string[] { "fabulous", "Jørgensen", "--", "beta-carotene", "x-ray", "A.", "supercalifragilisticexpialadocious", "58", "59,000", "NF-kappa", "Exxon-Mobil", "a", "A4", "IFN-gamma-inducible", "PPARgamma", "NF-kappaB"
			, "CBF1/RBP-Jkappa", string.Empty, "It's", "A-4", "congrès", "3,35%", "6€", "}", "《", "０-９", "四千", "五亿◯", "ＰＱ", "الحرازي", "2008", "427891", "A.B.C.", "22-34", "Ak47", "frEaKy", "美方称", "alphabeta", "betaalpha", "betalpha", "alpha-beta", "beta-alpha"
			, "zalphabeta", "zbetaalpha", "zbetalpha", "zalpha-beta", "zbeta-alpha" };

		private static string[] chris1outputs = new string[] { "LOWERCASE", "CAPITALIZED", "SYMBOL", "LOWERCASE-DASH", "LOWERCASE-DASH", "ACRONYM1", "LOWERCASE", "CARDINAL13", "NUMBER", "CAPITALIZED-DASH", "CAPITALIZED-DASH", "LOWERCASE", "ALLCAPS-DIGIT"
			, "CAPITALIZED-DASH", "CAPITALIZED", "CAPITALIZED-DASH", "CAPITALIZED-DIGIT-DASH", "SYMBOL", "CAPITALIZED", "ALLCAPS-DIGIT-DASH", "LOWERCASE", "SYMBOL-DIGIT", "SYMBOL-DIGIT", "SYMBOL", "SYMBOL", "DIGIT-DASH", "LOWERCASE", "LOWERCASE", "ALLCAPS"
			, "LOWERCASE", "CARDINAL4", "CARDINAL5PLUS", "ACRONYM", "DIGIT-DASH", "CAPITALIZED-DIGIT", "MIXEDCASE", "LOWERCASE", "LOWERCASE", "LOWERCASE", "LOWERCASE", "LOWERCASE-DASH", "LOWERCASE-DASH", "LOWERCASE", "LOWERCASE", "LOWERCASE", "LOWERCASE-DASH"
			, "LOWERCASE-DASH" };

		private static string[] chris2outputs = new string[] { "xxxxx", "Xxxxx", "--", "g-xxx", "x-xxx", "X.", "xxxxx", "dd", "dd,ddd", "XX-g", "Xx-Xxxx", "x", "Xd", "XX-Xgxxx", "XXXg", "XX-gX", "XX-/Xdg", string.Empty, "Xx'x", "X-d", "xxxxx", "d,dd%"
			, "d€", "}", "《", "d-d", "四千", "五亿◯", "XX", "الاحرزي", "dddd", "ddddd", "X..XX.", "dd-dd", "Xxdd", "xxXxXx", "美方称", "gg", "gg", "gxxx", "g-g", "g-g", "xgg", "xgg", "xgxxx", "xg-g", "xg-g" };

		private static string[] chris2KnownLCoutputs = new string[] { "xxxxxk", "Xxxxx", "--", "g-xxx", "x-xxx", "X.", "xxxxx", "dd", "dd,ddd", "XX-g", "Xx-Xxxx", "xk", "Xd", "XX-Xgxxx", "XXXg", "XX-gX", "XX-/Xdg", string.Empty, "Xx'x", "X-d", "xxxxx"
			, "d,dd%", "d€", "}", "《", "d-d", "四千", "五亿◯", "XX", "الاحرزي", "dddd", "ddddd", "X..XX.", "dd-dd", "Xxdd", "xxXxXx", "美方称", "gg", "gg", "gxxx", "g-g", "g-g", "xgg", "xgg", "xgxxx", "xg-g", "xg-g" };

		private static string[] chris3outputs = new string[] { "xxxx", "Xxxx", "--", "g-xx", "x-xx", "X.", "xxxx", "dd", "dd,dd", "XX-g", "Xx-xx", "x", "Xd", "XX-gxx", "XXg", "XX-gX", "XX-/dg", string.Empty, "Xx'x", "X-d", "xxxx", "d,d%", "d€", "}", 
			"《", "d-d", "四千", "五亿◯", "XX", "الحرزي", "dddd", "dddd", "X.X.", "dd-dd", "Xxdd", "xxXx", "美方称", "g", "g", "gxx", "g-", "g-", "xg", "xg", "xgxx", "xg-", "xg-" };

		private static string[] chris3KnownLCoutputs = new string[] { "xxxxk", "Xxxx", "--", "g-xx", "x-xx", "X.", "xxxx", "dd", "dd,dd", "XX-g", "Xx-xx", "xk", "Xd", "XX-gxx", "XXg", "XX-gX", "XX-/dg", string.Empty, "Xx'x", "X-d", "xxxx", "d,d%", "d€"
			, "}", "《", "d-d", "四千", "五亿◯", "XX", "الحرزي", "dddd", "dddd", "X.X.", "dd-dd", "Xxdd", "xxXx", "美方称", "g", "g", "gxx", "g-", "g-", "xg", "xg", "xgxx", "xg-", "xg-" };

		private static string[] chris4outputs = new string[] { "xxxxx", "Xxxxx", "--", "g-xxx", "x-xxx", "X.", "xxxxx", "dd", "dd.ddd", "XX-g", "Xx-Xxxx", "x", "Xd", "XX-Xgxxx", "XXXg", "XX-gX", "XX-.Xdg", string.Empty, "Xx'x", "X-d", "xxxxx", "d.dd%"
			, "d$", ")", "(", "d-d", "dd", "ddd", "XX", "ccccc", "dddd", "ddddd", "X..XX.", "dd-dd", "Xxdd", "xxXxXx", "ccc", "gg", "gg", "gxxx", "g-g", "g-g", "xgg", "xgg", "xgxxx", "xg-g", "xg-g" };

		private static string[] chris4KnownLCoutputs = new string[] { "xxxxxk", "Xxxxx", "--", "g-xxx", "x-xxx", "X.", "xxxxx", "dd", "dd.ddd", "XX-g", "Xx-Xxxx", "xk", "Xd", "XX-Xgxxx", "XXXg", "XX-gX", "XX-.Xdg", string.Empty, "Xx'x", "X-d", "xxxxx"
			, "d.dd%", "d$", ")", "(", "d-d", "dd", "ddd", "XX", "ccccc", "dddd", "ddddd", "X..XX.", "dd-dd", "Xxdd", "xxXxXx", "ccc", "gg", "gg", "gxxx", "g-g", "g-g", "xgg", "xgg", "xgxxx", "xg-g", "xg-g" };

		private static string[] digitsOutputs = new string[] { "fabulous", "Jørgensen", "--", "beta-carotene", "x-ray", "A.", "supercalifragilisticexpialadocious", "99", "99,999", "NF-kappa", "Exxon-Mobil", "a", "A9", "IFN-gamma-inducible", "PPARgamma"
			, "NF-kappaB", "CBF9/RBP-Jkappa", string.Empty, "It's", "A-9", "congrès", "9,99%", "9€", "}", "《", "9-9", "四千", "五亿◯", "ＰＱ", "الحرازي", "9999", "999999", "A.B.C.", "99-99", "Ak99", "frEaKy", "美方称", "alphabeta", "betaalpha", "betalpha", "alpha-beta"
			, "beta-alpha", "zalphabeta", "zbetaalpha", "zbetalpha", "zalpha-beta", "zbeta-alpha" };

		private static string[] knownLC = new string[] { "house", "fabulous", "octopus", "a" };

		public static void GenericCheck(int wordshape, string[] @in, string[] shape, string[] knownLCWords)
		{
			NUnit.Framework.Assert.AreEqual("WordShapeClassifierTest is bung: array sizes differ", @in.Length, shape.Length);
			ICollection<string> knownLCset = null;
			if (knownLCWords != null)
			{
				knownLCset = new HashSet<string>(Arrays.AsList(knownLC));
			}
			for (int i = 0; i < @in.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("WordShape " + wordshape + " for " + @in[i] + " with " + (knownLCset == null ? "null" : "non-null") + " knownLCwords is not correct!", shape[i], WordShapeClassifier.WordShape(@in[i], wordshape, knownLCset));
			}
			try
			{
				WordShapeClassifier.WordShape(null, wordshape);
				Fail("WordShapeClassifier threw no exception on null");
			}
			catch (ArgumentNullException)
			{
			}
			catch (Exception)
			{
				// this is the good answer
				Fail("WordShapeClassifier didn't throw NullPointerException on null");
			}
		}

		public static void OutputResults(int wordshape, string[] @in, string[] shape, string[] knownLCWords)
		{
			System.Console.Out.WriteLine("======================");
			System.Console.Out.WriteLine(" Classifier " + wordshape);
			System.Console.Out.WriteLine("======================");
			ICollection<string> knownLCset = null;
			if (knownLCWords != null)
			{
				knownLCset = new HashSet<string>(Arrays.AsList(knownLC));
			}
			for (int i = 0; i < @in.Length; ++i)
			{
				string result = WordShapeClassifier.WordShape(@in[i], wordshape, knownLCset);
				System.Console.Out.Write("  " + @in[i] + ": " + result);
				if (i < shape.Length)
				{
					System.Console.Out.Write("  (" + shape[i] + ")");
				}
				System.Console.Out.WriteLine();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestChris1()
		{
			GenericCheck(WordShapeClassifier.Wordshapechris1, inputs, chris1outputs, null);
		}

		[NUnit.Framework.Test]
		public virtual void TestChris2()
		{
			GenericCheck(WordShapeClassifier.Wordshapechris2, inputs, chris2outputs, null);
			GenericCheck(WordShapeClassifier.Wordshapechris2uselc, inputs, chris2KnownLCoutputs, knownLC);
		}

		[NUnit.Framework.Test]
		public virtual void TestChris3()
		{
			GenericCheck(WordShapeClassifier.Wordshapechris3, inputs, chris3outputs, null);
			GenericCheck(WordShapeClassifier.Wordshapechris3uselc, inputs, chris3KnownLCoutputs, knownLC);
		}

		[NUnit.Framework.Test]
		public virtual void TestChris4()
		{
			GenericCheck(WordShapeClassifier.Wordshapechris4, inputs, chris4outputs, null);
			GenericCheck(WordShapeClassifier.Wordshapechris4, inputs, chris4KnownLCoutputs, knownLC);
		}

		[NUnit.Framework.Test]
		public virtual void TestDigits()
		{
			GenericCheck(WordShapeClassifier.Wordshapedigits, inputs, digitsOutputs, null);
		}
	}
}
