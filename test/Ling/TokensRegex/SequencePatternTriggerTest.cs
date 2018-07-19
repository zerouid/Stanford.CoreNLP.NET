using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Tests triggering of sequence patterns</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SequencePatternTriggerTest
	{
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestSimpleTrigger()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word should be matched"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestOptionalTrigger()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word should? be matched"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("should"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestOptionalTrigger2()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word should? be matched?"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("matched"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("should"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestOptionalTrigger3()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word ( should | would ) be matched?"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("matched"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("should"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestOptionalTrigger4()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word should? be matched{1,2}"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("should"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestOptionalTrigger5()
		{
			IList<TokenSequencePattern> patterns = new List<TokenSequencePattern>();
			patterns.Add(TokenSequencePattern.Compile("which word should? be matched{1,8}"));
			MultiPatternMatcher.ISequencePatternTrigger<ICoreMap> trigger = new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns));
			ICollection<SequencePattern<ICoreMap>> triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("one", "two", "three"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("matched"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("should"));
			NUnit.Framework.Assert.AreEqual(0, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
			triggered = trigger.Apply(SentenceUtils.ToCoreLabelList("which", "word", "should", "be", "matched"));
			NUnit.Framework.Assert.AreEqual(1, triggered.Count);
		}
	}
}
