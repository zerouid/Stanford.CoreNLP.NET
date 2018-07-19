using System.Collections.Generic;
using Java.Lang;
using Java.Nio.File;
using Java.Util;
using Java.Util.Regex;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	public class StringUtilsTest
	{
		[Test]
		public virtual void TestTr()
		{
			NUnit.Framework.Assert.AreEqual(StringUtils.Tr("chris", "irs", "mop"), "chomp");
		}

		[Test]
		public virtual void TestGetBaseName()
		{
			NUnit.Framework.Assert.AreEqual(StringUtils.GetBaseName("/u/wcmac/foo.txt"), "foo.txt");
			NUnit.Framework.Assert.AreEqual(StringUtils.GetBaseName("/u/wcmac/foo.txt", string.Empty), "foo.txt");
			NUnit.Framework.Assert.AreEqual(StringUtils.GetBaseName("/u/wcmac/foo.txt", ".txt"), "foo");
			NUnit.Framework.Assert.AreEqual(StringUtils.GetBaseName("/u/wcmac/foo.txt", ".pdf"), "foo.txt");
		}

		[Test]
		public virtual void TestArgsToProperties()
		{
			Properties p1 = new Properties();
			p1.SetProperty("fred", "-2");
			p1.SetProperty(string.Empty, "joe");
			Properties p2 = new Properties();
			p2.SetProperty("fred", "true");
			p2.SetProperty("2", "joe");
			IDictionary<string, int> argNums = new Dictionary<string, int>();
			argNums["fred"] = 1;
			NUnit.Framework.Assert.AreEqual(p2, StringUtils.ArgsToProperties("-fred", "-2", "joe"));
			NUnit.Framework.Assert.AreEqual(StringUtils.ArgsToProperties(new string[] { "-fred", "-2", "joe" }, argNums), p1);
		}

		[Test]
		public virtual void TestValueSplit()
		{
			IList<string> vals1 = StringUtils.ValueSplit("arg(a,b),foo(d,e,f)", "[a-z]*(?:\\([^)]*\\))?", "\\s*,\\s*");
			IList<string> ans1 = Arrays.AsList("arg(a,b)", "foo(d,e,f)");
			NUnit.Framework.Assert.AreEqual(ans1, vals1, "Split failed");
			vals1 = StringUtils.ValueSplit("arg(a,b) , foo(d,e,f) , ", "[a-z]*(?:\\([^)]*\\))?", "\\s*,\\s*");
			NUnit.Framework.Assert.AreEqual(ans1, vals1, "Split failed");
			vals1 = StringUtils.ValueSplit(",arg(a,b),foo(d,e,f)", "[a-z]*(?:\\([^)]*\\))?", "\\s*,\\s*");
			IList<string> ans2 = Arrays.AsList(string.Empty, "arg(a,b)", "foo(d,e,f)");
			NUnit.Framework.Assert.AreEqual(ans2, vals1, "Split failed");
			IList<string> vals3 = StringUtils.ValueSplit("\"quoted,comma\",\"with \\\"\\\" quote\" , \"stuff\",or not,quoted,", "\"(?:[^\"\\\\]+|\\\\\")*\"|[^,\"]+", "\\s*,\\s*");
			IList<string> ans3 = Arrays.AsList("\"quoted,comma\"", "\"with \\\"\\\" quote\"", "\"stuff\"", "or not", "quoted");
			NUnit.Framework.Assert.AreEqual(ans3, vals3, "Split failed");
		}

		[Test]
		public virtual void TestLongestCommonSubstring()
		{
			NUnit.Framework.Assert.AreEqual(12, StringUtils.LongestCommonSubstring("Jo3seph Smarr!", "Joseph R Smarr"));
			NUnit.Framework.Assert.AreEqual(12, StringUtils.LongestCommonSubstring("Joseph R Smarr", "Jo3seph Smarr!"));
		}

		[Test]
		public virtual void TestEditDistance()
		{
			// test insert
			NUnit.Framework.Assert.AreEqual(4, StringUtils.EditDistance("Hi!", "Hi you!"));
			NUnit.Framework.Assert.AreEqual(5, StringUtils.EditDistance("Hi!", "Hi you!?"));
			NUnit.Framework.Assert.AreEqual(1, StringUtils.EditDistance("sdf", "asdf"));
			NUnit.Framework.Assert.AreEqual(1, StringUtils.EditDistance("asd", "asdf"));
			// test delete
			NUnit.Framework.Assert.AreEqual(4, StringUtils.EditDistance("Hi you!", "Hi!"));
			NUnit.Framework.Assert.AreEqual(5, StringUtils.EditDistance("Hi you!?", "Hi!"));
			NUnit.Framework.Assert.AreEqual(1, StringUtils.EditDistance("asdf", "asd"));
			NUnit.Framework.Assert.AreEqual(1, StringUtils.EditDistance("asdf", "sdf"));
			// test modification
			NUnit.Framework.Assert.AreEqual(3, StringUtils.EditDistance("Hi you!", "Hi Sir!"));
			NUnit.Framework.Assert.AreEqual(5, StringUtils.EditDistance("Hi you!", "Hi Sir!!!"));
			// test transposition
			NUnit.Framework.Assert.AreEqual(2, StringUtils.EditDistance("hello", "hlelo"));
			NUnit.Framework.Assert.AreEqual(2, StringUtils.EditDistance("asdf", "adsf"));
			NUnit.Framework.Assert.AreEqual(2, StringUtils.EditDistance("asdf", "sadf"));
			NUnit.Framework.Assert.AreEqual(2, StringUtils.EditDistance("asdf", "asfd"));
			// test empty
			NUnit.Framework.Assert.AreEqual(0, StringUtils.EditDistance(string.Empty, string.Empty));
			NUnit.Framework.Assert.AreEqual(3, StringUtils.EditDistance(string.Empty, "bar"));
			NUnit.Framework.Assert.AreEqual(3, StringUtils.EditDistance("foo", string.Empty));
		}

		[Test]
		public virtual void TestSplitOnChar()
		{
			NUnit.Framework.Assert.AreEqual(3, StringUtils.SplitOnChar("hello\tthere\tworld", '\t').Length);
			NUnit.Framework.Assert.AreEqual(2, StringUtils.SplitOnChar("hello\tworld", '\t').Length);
			NUnit.Framework.Assert.AreEqual(1, StringUtils.SplitOnChar("hello", '\t').Length);
			NUnit.Framework.Assert.AreEqual("hello", StringUtils.SplitOnChar("hello\tthere\tworld", '\t')[0]);
			NUnit.Framework.Assert.AreEqual("there", StringUtils.SplitOnChar("hello\tthere\tworld", '\t')[1]);
			NUnit.Framework.Assert.AreEqual("world", StringUtils.SplitOnChar("hello\tthere\tworld", '\t')[2]);
			NUnit.Framework.Assert.AreEqual(1, StringUtils.SplitOnChar("hello\tthere\tworld\n", ' ').Length);
			NUnit.Framework.Assert.AreEqual("hello\tthere\tworld\n", StringUtils.SplitOnChar("hello\tthere\tworld\n", ' ')[0]);
			NUnit.Framework.Assert.AreEqual(5, StringUtils.SplitOnChar("a\tb\tc\td\te", '\t').Length);
			NUnit.Framework.Assert.AreEqual(5, StringUtils.SplitOnChar("\t\t\t\t", '\t').Length);
			NUnit.Framework.Assert.AreEqual(string.Empty, StringUtils.SplitOnChar("\t\t\t\t", '\t')[0]);
			NUnit.Framework.Assert.AreEqual(string.Empty, StringUtils.SplitOnChar("\t\t\t\t", '\t')[1]);
			NUnit.Framework.Assert.AreEqual(string.Empty, StringUtils.SplitOnChar("\t\t\t\t", '\t')[4]);
		}

		/*
		public void testSplitOnCharSpeed() {
		String line = "1;2;3;4;5;678;901;234567;1";
		int runs = 1000000;
		
		for (int gcIter = 0; gcIter < 10; ++gcIter) {
		long start = System.currentTimeMillis();
		for (int i = 0; i < runs; ++i) {
		StringUtils.split(line, ";");
		}
		System.err.println("Old: " + Redwood.formatTimeDifference(System.currentTimeMillis() - start) + " for " + runs + " splits");
		
		start = System.currentTimeMillis();
		for (int i = 0; i < runs; ++i) {
		StringUtils.splitOnChar(line, ';');
		}
		System.err.println("New: " + Redwood.formatTimeDifference(System.currentTimeMillis() - start) + " for " + runs + " splits");
		System.err.println();
		}
		}
		*/
		[Test]
		public virtual void TestStringIsNullOrEmpty()
		{
			NUnit.Framework.Assert.IsTrue(StringUtils.IsNullOrEmpty(null));
			NUnit.Framework.Assert.IsTrue(StringUtils.IsNullOrEmpty(string.Empty));
			NUnit.Framework.Assert.IsFalse(StringUtils.IsNullOrEmpty(" "));
			NUnit.Framework.Assert.IsFalse(StringUtils.IsNullOrEmpty("foo"));
		}

		[Test]
		public virtual void TestNormalize()
		{
			NUnit.Framework.Assert.AreEqual("can't", StringUtils.Normalize("can't"));
			NUnit.Framework.Assert.AreEqual("Beyonce", StringUtils.Normalize("Beyoncé"));
			NUnit.Framework.Assert.AreEqual("krouzek", StringUtils.Normalize("kroužek"));
			NUnit.Framework.Assert.AreEqual("office", StringUtils.Normalize("o\uFB03ce"));
			NUnit.Framework.Assert.AreEqual("DZ", StringUtils.Normalize("Ǆ"));
			NUnit.Framework.Assert.AreEqual("1⁄4", StringUtils.Normalize("¼"));
			NUnit.Framework.Assert.AreEqual("한국어", StringUtils.Normalize("한국어"));
			NUnit.Framework.Assert.AreEqual("조선말", StringUtils.Normalize("조선말"));
			NUnit.Framework.Assert.AreEqual("が", StringUtils.Normalize("が"));
			NUnit.Framework.Assert.AreEqual("か", StringUtils.Normalize("か"));
		}

		private static readonly char[] escapeInputs = new char[] { '\\', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '"', '"', '"' };

		private static readonly string[] csvInputs = new string[] { string.Empty, ",", "foo", "foo,bar", "foo,    bar", ",foo,bar,", "foo,\"bar\"", "\"foo,foo2\"", "1997, \"Ford\" ,E350", "foo,\"\",bar", "1999,Chevy,\"Venture \"\"Extended Edition, Large\"\"\",,5000.00"
			, "\"\"\",foo,\"", "\"\"\"\",foo" };

		private static readonly string[][] csvOutputs = new string[][] { new string[] {  }, new string[] { string.Empty }, new string[] { "foo" }, new string[] { "foo", "bar" }, new string[] { "foo", "    bar" }, new string[] { string.Empty, "foo", 
			"bar" }, new string[] { "foo", "bar" }, new string[] { "foo,foo2" }, new string[] { "1997", " Ford ", "E350" }, new string[] { "foo", string.Empty, "bar" }, new string[] { "1999", "Chevy", "Venture \"Extended Edition, Large\"", string.Empty
			, "5000.00" }, new string[] { "\",foo," }, new string[] { "\"", "foo" } };

		[Test]
		public virtual void TestCSV()
		{
			NUnit.Framework.Assert.AreEqual(csvInputs.Length, csvOutputs.Length, "Bung test");
			for (int i = 0; i < csvInputs.Length; i++)
			{
				string[] answer = StringUtils.SplitOnCharWithQuoting(csvInputs[i], ',', '"', escapeInputs[i]);
				NUnit.Framework.Assert.IsTrue(Arrays.Equals(csvOutputs[i], answer), "Bad CSV line handling of ex " + i + ": " + Arrays.ToString(csvOutputs[i]) + " vs. " + Arrays.ToString(answer));
			}
		}

		[Test]
		public virtual void TestGetCharacterNgrams()
		{
			TestCharacterNgram("abc", 0, 0);
			TestCharacterNgram("abc", 1, 1, "a", "b", "c");
			TestCharacterNgram("abc", 2, 2, "ab", "bc");
			TestCharacterNgram("abc", 1, 2, "a", "b", "c", "ab", "bc");
			TestCharacterNgram("abc", 1, 3, "a", "b", "c", "ab", "bc", "abc");
			TestCharacterNgram("abc", 1, 4, "a", "b", "c", "ab", "bc", "abc");
		}

		private static void TestCharacterNgram(string @string, int min, int max, params string[] expected)
		{
			System.Console.Out.WriteLine(MakeSet(expected));
			System.Console.Out.WriteLine(StringUtils.GetCharacterNgrams(@string, min, max));
			NUnit.Framework.Assert.AreEqual(MakeSet(expected), new HashSet<string>(StringUtils.GetCharacterNgrams(@string, min, max)));
		}

		[SafeVarargs]
		private static ICollection<T> MakeSet<T>(params T[] elems)
		{
			return new HashSet<T>(Arrays.AsList(elems));
		}

		[Test]
		public virtual void TestExpandEnvironmentVariables()
		{
			IDictionary<string, string> env = new _Dictionary_218();
			NUnit.Framework.Assert.AreEqual("xxx [outA] xxx", StringUtils.ExpandEnvironmentVariables("xxx $A xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx[outA] xxx", StringUtils.ExpandEnvironmentVariables("xxx$A xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx[outA]xxx", StringUtils.ExpandEnvironmentVariables("xxx${A}xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx [outA_B] xxx", StringUtils.ExpandEnvironmentVariables("xxx $A_B xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx [outa_B] xxx", StringUtils.ExpandEnvironmentVariables("xxx $a_B xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx [outa_B45] xxx", StringUtils.ExpandEnvironmentVariables("xxx $a_B45 xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx [out_A] xxx", StringUtils.ExpandEnvironmentVariables("xxx $_A xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx $3A xxx", StringUtils.ExpandEnvironmentVariables("xxx $3A xxx", env));
			NUnit.Framework.Assert.AreEqual("xxx  xxx", StringUtils.ExpandEnvironmentVariables("xxx $UNDEFINED xxx", env));
		}

		private sealed class _Dictionary_218 : Dictionary<string, string>
		{
			public _Dictionary_218()
			{
				{
					this["A"] = "[outA]";
					this["A_B"] = "[outA_B]";
					this["a_B"] = "[outa_B]";
					this["a_B45"] = "[outa_B45]";
					this["_A"] = "[out_A]";
					this["3A"] = "[out_3A]";
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestDecodeArray()
		{
			string tempFile1 = Files.CreateTempFile("test", "tmp").ToString();
			string tempFile2 = Files.CreateTempFile("test", "tmp").ToString();
			string[] decodedArray = StringUtils.DecodeArray("'" + tempFile1 + "','" + tempFile2 + "'");
			NUnit.Framework.Assert.AreEqual(2, decodedArray.Length);
			NUnit.Framework.Assert.AreEqual(tempFile1, decodedArray[0]);
			NUnit.Framework.Assert.AreEqual(tempFile2, decodedArray[1]);
			string[] test10 = new string[] { "\"C:\\Users\\BELLCH~1\\AppData\\Local\\Temp\\bill-ie5804201486895318826regex_rules.txt\"", "[\"C:\\Users\\BELLCH~1\\AppData\\Local\\Temp\\bill-ie5804201486895318826regex_rules.txt\"]" };
			string[] ans10 = new string[] { "C:\\Users\\BELLCH~1\\AppData\\Local\\Temp\\bill-ie5804201486895318826regex_rules.txt" };
			string[] test11 = new string[] { "C:\\Users\\BELLCH~1\\AppData\\Local\\Temp\\bill-ie5804201486895318826regex_rules.txt", "[C:\\Users\\BELLCH~1\\AppData\\Local\\Temp\\bill-ie5804201486895318826regex_rules.txt]" };
			string[] ans11 = new string[] { "C:UsersBELLCH~1AppDataLocalTempbill-ie5804201486895318826regex_rules.txt" };
			foreach (string s in test10)
			{
				NUnit.Framework.Assert.AreEqual(Arrays.AsList(ans10), Arrays.AsList(StringUtils.DecodeArray(s)));
			}
			foreach (string s_1 in test11)
			{
				NUnit.Framework.Assert.AreEqual(Arrays.AsList(ans11), Arrays.AsList(StringUtils.DecodeArray(s_1)));
			}
		}

		[Test]
		public virtual void TestRegexGroups()
		{
			IList<string> ans = Arrays.AsList("42", "123", "1965");
			NUnit.Framework.Assert.AreEqual(ans, StringUtils.RegexGroups(Pattern.Compile("(\\d+)\\D*(\\d+)\\D*(\\d+)"), "abc-x42!123   -1965."));
		}

		[Test]
		public virtual void TestEscapeJsonString()
		{
			NUnit.Framework.Assert.AreEqual("\\u0001\\b\\r\\u001D\\u001Fz", StringUtils.EscapeJsonString("\u0001\b\r\u001d\u001fz"));
			NUnit.Framework.Assert.AreEqual("food", StringUtils.EscapeJsonString("food"));
			NUnit.Framework.Assert.AreEqual("\\\\\\\"here\\u0000goes\\b\\u000B", StringUtils.EscapeJsonString("\\\"here\u0000goes\b\u000B"));
		}
	}
}
