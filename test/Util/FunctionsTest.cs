using Java.Util.Function;
using Sharpen;

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
			IFunction<int, int> plusOne = null;
			IFunction<int, int> doubler = null;
			IFunction<int, int> composed = Functions.Compose(plusOne, doubler);
			NUnit.Framework.Assert.AreEqual(composed.Apply(1), 3);
			NUnit.Framework.Assert.AreEqual(composed.Apply(2), 5);
		}
	}
}
