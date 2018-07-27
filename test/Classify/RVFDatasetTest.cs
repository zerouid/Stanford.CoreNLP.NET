using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;



using NUnit.Framework;


namespace Edu.Stanford.Nlp.Classify
{
	/// <author>Steven Bethard</author>
	[NUnit.Framework.TestFixture]
	public class RVFDatasetTest
	{
		[Test]
		public virtual void TestCombiningDatasets()
		{
			RVFDatum<string, string> datum1 = NewRVFDatum(null, "a", "b", "a");
			RVFDatum<string, string> datum2 = NewRVFDatum(null, "c", "c", "b");
			RVFDataset<string, string> data1 = new RVFDataset<string, string>();
			data1.Add(datum1);
			RVFDataset<string, string> data2 = new RVFDataset<string, string>();
			data1.Add(datum2);
			RVFDataset<string, string> data = new RVFDataset<string, string>();
			data.AddAll(data1);
			data.AddAll(data2);
			IEnumerator<RVFDatum<string, string>> iterator = data.GetEnumerator();
			NUnit.Framework.Assert.AreEqual(datum1, iterator.Current);
			NUnit.Framework.Assert.AreEqual(datum2, iterator.Current);
			NUnit.Framework.Assert.IsFalse(iterator.MoveNext());
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestSVMLightIntegerFormat()
		{
			RVFDataset<bool, int> dataset = new RVFDataset<bool, int>();
			dataset.Add(NewRVFDatum(true, 1, 2, 1, 0));
			dataset.Add(NewRVFDatum(false, 2, 2, 0, 0));
			dataset.Add(NewRVFDatum(true, 0, 1, 2, 2));
			File tempFile = File.CreateTempFile("testSVMLightIntegerFormat", ".svm");
			dataset.WriteSVMLightFormat(tempFile);
			RVFDataset<bool, int> newDataset = new RVFDataset<bool, int>();
			try
			{
				newDataset.ReadSVMLightFormat(tempFile);
				NUnit.Framework.Assert.Fail("expected failure with empty indexes");
			}
			catch (Exception)
			{
			}
			newDataset = new RVFDataset<bool, int>(dataset.Size(), dataset.FeatureIndex(), dataset.LabelIndex());
			newDataset.ReadSVMLightFormat(tempFile);
			NUnit.Framework.Assert.AreEqual(CollectionUtils.ToList(dataset), CollectionUtils.ToList(newDataset));
		}

		[SafeVarargs]
		private static RVFDatum<L, F> NewRVFDatum<L, F>(L label, params F[] items)
		{
			return new RVFDatum<L, F>(Counters.AsCounter(Arrays.AsList(items)), label);
		}
	}
}
