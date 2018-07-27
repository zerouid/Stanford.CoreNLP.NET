
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A small test for the
	/// <see cref="ArgumentParser"/>
	/// class for loading command line options
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class ArgumentParserTest
	{
		public class StaticClass
		{
			public static int staticOption = -1;

			private StaticClass()
			{
			}
		}

		public class NonstaticClass
		{
			public int staticOption = -1;
		}

		public class MixedClass
		{
			public static int staticOption = -1;

			public int nonstaticOption = -1;
		}

		[SetUp]
		public virtual void SetUp()
		{
			ArgumentParserTest.StaticClass.staticOption = -1;
			ArgumentParserTest.MixedClass.staticOption = -1;
		}

		[Test]
		public virtual void TestFillStaticField()
		{
			NUnit.Framework.Assert.AreEqual(-1, ArgumentParserTest.StaticClass.staticOption);
			ArgumentParser.FillOptions(typeof(ArgumentParserTest.StaticClass), "-option.static", "42");
			NUnit.Framework.Assert.AreEqual(42, ArgumentParserTest.StaticClass.staticOption);
		}

		[Test]
		public virtual void TestFillStaticFieldFromProperties()
		{
			NUnit.Framework.Assert.AreEqual(-1, ArgumentParserTest.StaticClass.staticOption);
			Properties props = new Properties();
			props.SetProperty("option.static", "42");
			ArgumentParser.FillOptions(typeof(ArgumentParserTest.StaticClass), props);
			NUnit.Framework.Assert.AreEqual(42, ArgumentParserTest.StaticClass.staticOption);
		}

		[Test]
		public virtual void FillNonstaticField()
		{
			ArgumentParserTest.NonstaticClass x = new ArgumentParserTest.NonstaticClass();
			NUnit.Framework.Assert.AreEqual(-1, x.staticOption);
			ArgumentParser.FillOptions(x, "-option.nonstatic", "42");
			NUnit.Framework.Assert.AreEqual(42, x.staticOption);
		}

		[Test]
		public virtual void FillNonstaticFieldFromProperties()
		{
			ArgumentParserTest.NonstaticClass x = new ArgumentParserTest.NonstaticClass();
			NUnit.Framework.Assert.AreEqual(-1, x.staticOption);
			Properties props = new Properties();
			props.SetProperty("option.nonstatic", "42");
			ArgumentParser.FillOptions(x, props);
			NUnit.Framework.Assert.AreEqual(42, x.staticOption);
		}

		[Test]
		public virtual void FillMixedFieldsInstanceGiven()
		{
			ArgumentParserTest.MixedClass x = new ArgumentParserTest.MixedClass();
			NUnit.Framework.Assert.AreEqual(-1, ArgumentParserTest.MixedClass.staticOption);
			NUnit.Framework.Assert.AreEqual(-1, x.nonstaticOption);
			ArgumentParser.FillOptions(x, "-option.nonstatic", "42", "-option.static", "43");
			NUnit.Framework.Assert.AreEqual(43, ArgumentParserTest.MixedClass.staticOption);
			NUnit.Framework.Assert.AreEqual(42, x.nonstaticOption);
		}

		[Test]
		public virtual void FillMixedFieldsNoInstanceGiven()
		{
			ArgumentParserTest.MixedClass x = new ArgumentParserTest.MixedClass();
			NUnit.Framework.Assert.AreEqual(-1, ArgumentParserTest.MixedClass.staticOption);
			NUnit.Framework.Assert.AreEqual(-1, x.nonstaticOption);
			ArgumentParser.FillOptions(typeof(ArgumentParserTest.MixedClass), "-option.nonstatic", "42", "-option.static", "43");
			NUnit.Framework.Assert.AreEqual(43, ArgumentParserTest.MixedClass.staticOption);
			NUnit.Framework.Assert.AreEqual(-1, x.nonstaticOption);
		}

		/// <summary>Check that command-line arguments override properties.</summary>
		[Test]
		public virtual void CheckOptionsOverrideProperties()
		{
			ArgumentParserTest.NonstaticClass x = new ArgumentParserTest.NonstaticClass();
			NUnit.Framework.Assert.AreEqual(-1, x.staticOption);
			Properties props = new Properties();
			props.SetProperty("option.nonstatic", "78");
			ArgumentParser.FillOptions(x, props, "-option.nonstatic", "42");
			NUnit.Framework.Assert.AreEqual(42, x.staticOption);
		}
	}
}
