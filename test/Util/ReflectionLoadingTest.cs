

namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ReflectionLoadingTest
	{
		[NUnit.Framework.Test]
		public virtual void TestOneArg()
		{
			string s = ReflectionLoading.LoadByReflection("java.lang.String", "foo");
			NUnit.Framework.Assert.AreEqual("foo", s);
		}

		[NUnit.Framework.Test]
		public virtual void TestNoArgs()
		{
			string s = ReflectionLoading.LoadByReflection("java.lang.String");
			NUnit.Framework.Assert.AreEqual(string.Empty, s);
		}
	}
}
