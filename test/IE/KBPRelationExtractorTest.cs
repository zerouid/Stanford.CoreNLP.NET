
using NUnit.Framework;


namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// A test for the
	/// <see cref="IKBPRelationExtractor"/>
	/// base class.
	/// Also tests various nested classes.
	/// </summary>
	[NUnit.Framework.TestFixture]
	public class KBPRelationExtractorTest
	{
		[Test]
		public virtual void TestAccuracySimple()
		{
			KBPRelationExtractor.Accuracy accuracy = new KBPRelationExtractor.Accuracy();
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList("a")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList()));
			accuracy.Predict(new HashSet<string>(Arrays.AsList()), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList()));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("a"), 1e-10, 0.5);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("a"), 1e-10, 1.0);
			NUnit.Framework.Assert.AreEqual(accuracy.F1("a"), 1e-10, 2.0 * 1.0 * 0.5 / (1.0 + 0.5));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMicro(), 1e-10, 3.0 / 5.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMacro(), 1e-10, 7.0 / 12.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMicro(), 1e-10, 3.0 / 4.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMacro(), 1e-10, 5.0 / 6.0);
		}

		[Test]
		public virtual void TestAccuracyNoRelation()
		{
			KBPRelationExtractor.Accuracy accuracy = new KBPRelationExtractor.Accuracy();
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList("a")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("no_relation")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("a"), 1e-10, 0.5);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("a"), 1e-10, 1.0);
			NUnit.Framework.Assert.AreEqual(accuracy.F1("a"), 1e-10, 2.0 * 1.0 * 0.5 / (1.0 + 0.5));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMicro(), 1e-10, 3.0 / 5.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMacro(), 1e-10, 7.0 / 12.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMicro(), 1e-10, 3.0 / 4.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMacro(), 1e-10, 5.0 / 6.0);
		}

		[Test]
		public virtual void TestAccuracyTrueNegatives()
		{
			KBPRelationExtractor.Accuracy accuracy = new KBPRelationExtractor.Accuracy();
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList("a")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("a")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("no_relation")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("b")), new HashSet<string>(Arrays.AsList("b")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("no_relation")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("no_relation")), new HashSet<string>(Arrays.AsList("no_relation")));
			accuracy.Predict(new HashSet<string>(Arrays.AsList("no_relation")), new HashSet<string>(Arrays.AsList("no_relation")));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("a"), 1e-10, 0.5);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("a"), 1e-10, 1.0);
			NUnit.Framework.Assert.AreEqual(accuracy.F1("a"), 1e-10, 2.0 * 1.0 * 0.5 / (1.0 + 0.5));
			NUnit.Framework.Assert.AreEqual(accuracy.Precision("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.Recall("b"), 1e-10, 2.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMicro(), 1e-10, 3.0 / 5.0);
			NUnit.Framework.Assert.AreEqual(accuracy.PrecisionMacro(), 1e-10, 7.0 / 12.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMicro(), 1e-10, 3.0 / 4.0);
			NUnit.Framework.Assert.AreEqual(accuracy.RecallMacro(), 1e-10, 5.0 / 6.0);
		}
	}
}
