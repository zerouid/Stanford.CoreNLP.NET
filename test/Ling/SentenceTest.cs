using System.Collections.Generic;
using Edu.Stanford.Nlp.Simple;
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Tests the static methods that turn sentences (lists of Labels)
	/// into strings.
	/// </summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	public class SentenceTest
	{
		private static readonly string[] words = new string[] { "This", "is", "a", "test", "." };

		private static readonly string[] tags = new string[] { "A", "B", "C", "D", "E" };

		private const string expectedValueOnly = "This is a test .";

		private const string expectedTagged = "This_A is_B a_C test_D ._E";

		private const string separator = "_";

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
		}

		[Test]
		public virtual void TestCoreLabelListToString()
		{
			IList<CoreLabel> clWords = new List<CoreLabel>();
			IList<CoreLabel> clValues = new List<CoreLabel>();
			IList<CoreLabel> clWordTags = new List<CoreLabel>();
			IList<CoreLabel> clValueTags = new List<CoreLabel>();
			for (int i = 0; i < words.Length; ++i)
			{
				CoreLabel cl = new CoreLabel();
				cl.SetWord(words[i]);
				clWords.Add(cl);
				cl = new CoreLabel();
				cl.SetValue(words[i]);
				clValues.Add(cl);
				cl = new CoreLabel();
				cl.SetWord(words[i]);
				cl.SetTag(tags[i]);
				clWordTags.Add(cl);
				cl = new CoreLabel();
				cl.SetValue(words[i]);
				cl.SetTag(tags[i]);
				clValueTags.Add(cl);
			}
			NUnit.Framework.Assert.AreEqual(expectedValueOnly, SentenceUtils.ListToString(clWords, true));
			NUnit.Framework.Assert.AreEqual(expectedValueOnly, SentenceUtils.ListToString(clValues, true));
			NUnit.Framework.Assert.AreEqual(expectedTagged, SentenceUtils.ListToString(clWordTags, false, separator));
			NUnit.Framework.Assert.AreEqual(expectedTagged, SentenceUtils.ListToString(clValueTags, false, separator));
		}

		[Test]
		public virtual void TestTaggedWordListToString()
		{
			IList<TaggedWord> tagged = new List<TaggedWord>();
			for (int i = 0; i < words.Length; ++i)
			{
				tagged.Add(new TaggedWord(words[i], tags[i]));
			}
			NUnit.Framework.Assert.AreEqual(expectedValueOnly, SentenceUtils.ListToString(tagged, true));
			NUnit.Framework.Assert.AreEqual(expectedTagged, SentenceUtils.ListToString(tagged, false, separator));
		}

		/// <summary>
		/// Serializing a raw sentence shouldn't make it an order of magnitude larger than
		/// the raw text.
		/// </summary>
		[Test]
		public virtual void TestTokenizedSentenceSize()
		{
			string text = "one two three four five";
			byte[] sentenceArray = new Sentence(text).Serialize().ToByteArray();
			byte[] textArray = Sharpen.Runtime.GetBytesForString(text);
			NUnit.Framework.Assert.IsTrue(sentenceArray.Length < textArray.Length * 11, string.Format("Sentence size (%d bytes) shouldn't be more than %d times bigger than text size (%d bytes)", sentenceArray.Length, 11, textArray.Length));
		}
	}
}
