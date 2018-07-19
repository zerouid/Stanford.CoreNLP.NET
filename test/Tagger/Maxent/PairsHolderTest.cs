using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class PairsHolderTest
	{
		[NUnit.Framework.Test]
		public virtual void TestPairsHolder()
		{
			PairsHolder pairsHolder = new PairsHolder();
			for (int i = 0; i < 10; i++)
			{
				pairsHolder.Add(new WordTag("girl", "NN"));
			}
			MaxentTagger maxentTagger = new MaxentTagger();
			maxentTagger.Init(null);
			//maxentTagger.pairs = pairsHolder;
			History h = new History(0, 5, 3, pairsHolder, maxentTagger.extractors);
			TaggerExperiments te = new TaggerExperiments(maxentTagger);
			int x = te.GetHistoryTable().Add(h);
			//int x = maxentTagger.tHistories.add(h);
			int y = te.GetHistoryTable().GetIndex(h);
			//int y = maxentTagger.tHistories.getIndex(h);
			NUnit.Framework.Assert.AreEqual("Failing to get same index for history", x, y);
			Extractor e = new Extractor(0, false);
			string k = e.Extract(h);
			NUnit.Framework.Assert.AreEqual("Extractor didn't find stored word", k, "girl");
		}
	}
}
