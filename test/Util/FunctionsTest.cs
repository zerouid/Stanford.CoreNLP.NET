


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests Functions utility class</summary>
	/// <author>dramage</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class FunctionsTest
	{
		[NUnit.Framework.Test]
		public virtual void TestCompose()
		{
			Func<int, int> plusOne = null;
			Func<int, int> doubler = null;
			Func<int, int> composed = Functions.Compose(plusOne, doubler);
			NUnit.Framework.Assert.AreEqual(composed.Apply(1), 3);
			NUnit.Framework.Assert.AreEqual(composed.Apply(2), 5);
		}
	}
}
