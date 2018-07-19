using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Test methods in MultiWordStringMatcher.</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class MultiWordStringMatcherTest
	{
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestExctWsMatching()
		{
			MultiWordStringMatcher entityMatcher = new MultiWordStringMatcher(MultiWordStringMatcher.MatchType.Exctws);
			string targetString = "Al-Ahram";
			string context = "the government Al-Ahram newspaper";
			IList<IntPair> offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 23]", "[" + StringUtils.Join(offsets, ",") + "]");
			context = "the government Al- Ahram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 24]", "[" + StringUtils.Join(offsets, ",") + "]");
			targetString = "Al -Ahram";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.IsTrue("entityOffsets", offsets == null || offsets.IsEmpty());
			context = "the government Al-Ahramnewspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.IsTrue("entityOffsets", offsets == null || offsets.IsEmpty());
			context = "the government AlAhram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.IsTrue("entityOffsets", offsets == null || offsets.IsEmpty());
			context = "the government alahram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.IsTrue("entityOffsets", offsets == null || offsets.IsEmpty());
			context = "NZ Oil &amp;amp; Gas";
			targetString = "NZ Oil &amp;amp; Gas";
			offsets = entityMatcher.FindTargetStringOffsets(context, targetString);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[0 20]", "[" + StringUtils.Join(offsets, ",") + "]");
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestLnrmMatching()
		{
			MultiWordStringMatcher entityMatcher = new MultiWordStringMatcher(MultiWordStringMatcher.MatchType.Lnrm);
			string entityName = "Al-Ahram";
			string context = "the government Al-Ahram newspaper";
			IList<IntPair> offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 23]", "[" + StringUtils.Join(offsets, ",") + "]");
			context = "the government Al- Ahram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 24]", "[" + StringUtils.Join(offsets, ",") + "]");
			entityName = "Al -Ahram";
			offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 24]", "[" + StringUtils.Join(offsets, ",") + "]");
			context = "the government Al-Ahramnewspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.IsTrue("entityOffsets", offsets == null || offsets.IsEmpty());
			context = "the government AlAhram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 22]", "[" + StringUtils.Join(offsets, ",") + "]");
			context = "the government alahram newspaper";
			offsets = entityMatcher.FindTargetStringOffsets(context, entityName);
			NUnit.Framework.Assert.AreEqual("entityOffsets", "[15 22]", "[" + StringUtils.Join(offsets, ",") + "]");
		}
	}
}
