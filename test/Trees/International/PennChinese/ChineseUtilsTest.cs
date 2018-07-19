using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ChineseUtilsTest
	{
		[NUnit.Framework.Test]
		public virtual void TestNormalize()
		{
			string input = "Hello  Ｅｎｇｌｉｓｈ - 你好\u3000班汉·西巴阿差\u3000Chris•Manning \uD83E\uDD16\uD83E\uDD16robot";
			string outputLLL = "Hello  Ｅｎｇｌｉｓｈ - 你好\u3000班汉·西巴阿差\u3000Chris•Manning \uD83E\uDD16\uD83E\uDD16robot";
			string outputAAN = "Hello  English - 你好 班汉·西巴阿差 Chris·Manning \uD83E\uDD16\uD83E\uDD16robot";
			string outputFFF = "Ｈｅｌｌｏ\u3000\u3000Ｅｎｇｌｉｓｈ\u3000－\u3000你好　班汉・西巴阿差　Ｃｈｒｉｓ・Ｍａｎｎｉｎｇ\u3000\uD83E\uDD16\uD83E\uDD16ｒｏｂｏｔ";
			NUnit.Framework.Assert.AreEqual(outputLLL, ChineseUtils.Normalize(input, ChineseUtils.Leave, ChineseUtils.Leave, ChineseUtils.Leave));
			NUnit.Framework.Assert.AreEqual(outputAAN, ChineseUtils.Normalize(input, ChineseUtils.Ascii, ChineseUtils.Ascii, ChineseUtils.Normalize));
			NUnit.Framework.Assert.AreEqual(outputFFF, ChineseUtils.Normalize(input, ChineseUtils.Fullwidth, ChineseUtils.Fullwidth, ChineseUtils.Fullwidth));
		}
	}
}
