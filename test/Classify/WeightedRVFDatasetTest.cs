using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>TODO(gabor) JavaDoc</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class WeightedRVFDatasetTest
	{
		[NUnit.Framework.Test]
		public virtual void TestWeightingWorks()
		{
			WeightedRVFDataset<string, string> dataset = new WeightedRVFDataset<string, string>();
			RVFDatum<string, string> datum1 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum1, 42.0f);
			RVFDatum<string, string> datum2 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum2, 7.3f);
			NUnit.Framework.Assert.AreEqual(42.0f, dataset.GetWeights()[0], 1e-10);
			NUnit.Framework.Assert.AreEqual(7.3f, dataset.GetWeights()[1], 1e-10);
		}

		[NUnit.Framework.Test]
		public virtual void TestBackwardsCompatibility()
		{
			RVFDataset<string, string> dataset = new WeightedRVFDataset<string, string>();
			RVFDatum<string, string> datum1 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum1);
			RVFDatum<string, string> datum2 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum2);
			NUnit.Framework.Assert.AreEqual(1.0f, ((WeightedRVFDataset<string, string>)dataset).GetWeights()[0], 1e-10);
			NUnit.Framework.Assert.AreEqual(1.0f, ((WeightedRVFDataset<string, string>)dataset).GetWeights()[1], 1e-10);
		}

		[NUnit.Framework.Test]
		public virtual void TestMixedCompatibility()
		{
			WeightedRVFDataset<string, string> dataset = new WeightedRVFDataset<string, string>();
			RVFDatum<string, string> datum1 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum1, 42.0f);
			RVFDatum<string, string> datum2 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum2);
			RVFDatum<string, string> datum3 = NewRVFDatum(null, "a", "b", "a");
			dataset.Add(datum3, 7.3f);
			NUnit.Framework.Assert.AreEqual(42.0f, dataset.GetWeights()[0], 1e-10);
			NUnit.Framework.Assert.AreEqual(1.0f, dataset.GetWeights()[1], 1e-10);
			NUnit.Framework.Assert.AreEqual(7.3f, dataset.GetWeights()[2], 1e-10);
		}

		private static RVFDatum<L, F> NewRVFDatum<L, F>(L label, params F[] items)
		{
			return new RVFDatum<L, F>(Counters.AsCounter(Arrays.AsList(items)), label);
		}
	}
}
