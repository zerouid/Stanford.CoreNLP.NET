using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Test some (well, just one at the moment) of the utility methods in Maps</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class MapsTest
	{
		[NUnit.Framework.Test]
		public virtual void TestAddAllWithFunction()
		{
			IDictionary<string, string> stringMap = new Dictionary<string, string>();
			IDictionary<string, int> intMap = new Dictionary<string, int>();
			IFunction<int, string> toString = new _IFunction_18();
			Maps.AddAll(stringMap, intMap, toString);
			NUnit.Framework.Assert.AreEqual(0, stringMap.Count);
			intMap["foo"] = 6;
			Maps.AddAll(stringMap, intMap, toString);
			NUnit.Framework.Assert.AreEqual(1, stringMap.Count);
			NUnit.Framework.Assert.AreEqual("6", stringMap["foo"]);
			intMap.Clear();
			intMap["bar"] = 3;
			Maps.AddAll(stringMap, intMap, toString);
			NUnit.Framework.Assert.AreEqual(2, stringMap.Count);
			NUnit.Framework.Assert.AreEqual("6", stringMap["foo"]);
			NUnit.Framework.Assert.AreEqual("3", stringMap["bar"]);
			intMap.Clear();
			intMap["bar"] = 5;
			intMap["baz"] = 9;
			Maps.AddAll(stringMap, intMap, toString);
			NUnit.Framework.Assert.AreEqual(3, stringMap.Count);
			NUnit.Framework.Assert.AreEqual("6", stringMap["foo"]);
			NUnit.Framework.Assert.AreEqual("5", stringMap["bar"]);
			NUnit.Framework.Assert.AreEqual("9", stringMap["baz"]);
		}

		private sealed class _IFunction_18 : IFunction<int, string>
		{
			public _IFunction_18()
			{
			}

			public string Apply(int i)
			{
				return i.ToString();
			}
		}
	}
}
