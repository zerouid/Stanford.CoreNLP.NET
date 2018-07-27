
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Some simple tests for the TSVUtils functionalities.</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class TSVUtilsTest
	{
		[Test]
		public virtual void TestParseArrayTrivial()
		{
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("foo", "bar"), TSVUtils.ParseArray("{foo,bar}"));
		}

		[Test]
		public virtual void TestParseArrayQuote()
		{
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("foo", ",", "a,b", "bar"), TSVUtils.ParseArray("{foo,\",\",\"a,b\",bar}"));
		}

		[Test]
		public virtual void TestParseArrayEscape()
		{
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("foo", "\"", "a\"b", "bar"), TSVUtils.ParseArray("{foo,\"\\\"\",\"a\\\"b\",bar}"));
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("foo", "\"", "bar"), TSVUtils.ParseArray("{foo,\\\",bar}"));
			NUnit.Framework.Assert.AreEqual(Collections.SingletonList("aa\\bb"), TSVUtils.ParseArray("{\"aa\\\\\\\\bb\"}"));
			// should really give 2 backslashes in answer but doesn't.
			NUnit.Framework.Assert.AreEqual(Collections.SingletonList("a\"b"), TSVUtils.ParseArray("{\"a\"\"b\"}"));
		}

		// should really give 2 backslashes in answer but doesn't.
		[Test]
		public virtual void TestRealSentence()
		{
			string array = "{\"<ref name=\\\"Dr. Mohmmad Riaz Suddle, Director of the Paksat-IR programme and current executive member of the Suparco's plan and research division \\\"/>\",On,August,11th,\",\",Paksat-1R,|,'',Paksat-IR,'',was,launched,from,Xichang,Satellite,Launch,Center,by,Suparco,\",\",making,it,first,satellite,to,be,launched,under,this,programme,.}";
			NUnit.Framework.Assert.AreEqual(31, TSVUtils.ParseArray(array).Count);
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("<ref name=\"Dr. Mohmmad Riaz Suddle, Director of the Paksat-IR programme and current executive member of the Suparco's plan and research division \"/>", "On", "August", "11th", ",", "Paksat-1R", 
				"|", "''", "Paksat-IR", "''", "was", "launched", "from", "Xichang", "Satellite", "Launch", "Center", "by", "Suparco", ",", "making", "it", "first", "satellite", "to", "be", "launched", "under", "this", "programme", "."), TSVUtils.ParseArray
				(array));
		}

		[Test]
		public virtual void TestRealSentenceDoubleEscaped()
		{
			string array = "{\"<ref name=\\\\\"Dr. Mohmmad Riaz Suddle, Director of the Paksat-IR programme and current executive member of the Suparco's plan and research division \\\\\"/>\",On,August,11th,\",\",Paksat-1R,|,'',Paksat-IR,'',was,launched,from,Xichang,Satellite,Launch,Center,by,Suparco,\",\",making,it,first,satellite,to,be,launched,under,this,programme,.}";
			NUnit.Framework.Assert.AreEqual(31, TSVUtils.ParseArray(array).Count);
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("<ref name=\"Dr. Mohmmad Riaz Suddle, Director of the Paksat-IR programme and current executive member of the Suparco's plan and research division \"/>", "On", "August", "11th", ",", "Paksat-1R", 
				"|", "''", "Paksat-IR", "''", "was", "launched", "from", "Xichang", "Satellite", "Launch", "Center", "by", "Suparco", ",", "making", "it", "first", "satellite", "to", "be", "launched", "under", "this", "programme", "."), TSVUtils.ParseArray
				(array));
		}
	}
}
