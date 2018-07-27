using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Classify
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class GeneralDatasetTest
	{
		[NUnit.Framework.Test]
		public static void TestCreateFolds()
		{
			GeneralDataset<string, string> data = new Dataset<string, string>();
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "cough", "congestion" }), "cold"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "cough", "nausea" }), "flu"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "cough", "congestion" }), "cold"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "cough", "congestion" }), "cold"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "nausea" }), "flu"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "cough", "sore throat" }), "cold"));
			Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> devTrainTest = data.Split(3, 5);
			NUnit.Framework.Assert.AreEqual(4, devTrainTest.First().Size());
			NUnit.Framework.Assert.AreEqual(2, devTrainTest.Second().Size());
			NUnit.Framework.Assert.AreEqual("cold", devTrainTest.First().GetDatum(devTrainTest.First().Size() - 1).Label());
			NUnit.Framework.Assert.AreEqual("flu", devTrainTest.Second().GetDatum(devTrainTest.Second().Size() - 1).Label());
			Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> devTrainTest2 = data.Split(0, 2);
			NUnit.Framework.Assert.AreEqual(4, devTrainTest2.First().Size());
			NUnit.Framework.Assert.AreEqual(2, devTrainTest2.Second().Size());
			Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> devTrainTest3 = data.Split(1.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(devTrainTest2.First().Size(), devTrainTest3.First().Size());
			NUnit.Framework.Assert.AreEqual(devTrainTest2.First().LabelIndex(), devTrainTest3.First().LabelIndex());
			NUnit.Framework.Assert.AreEqual(devTrainTest2.Second().Size(), devTrainTest3.Second().Size());
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(devTrainTest2.First().labels, devTrainTest2.First().labels));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(devTrainTest2.Second().labels, devTrainTest2.Second().labels));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "nausea" }), "flu"));
			Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> devTrainTest4 = data.Split(1.0 / 3.0);
			NUnit.Framework.Assert.AreEqual(5, devTrainTest4.First().Size());
			NUnit.Framework.Assert.AreEqual(2, devTrainTest4.Second().Size());
			Pair<GeneralDataset<string, string>, GeneralDataset<string, string>> devTrainTest5 = data.Split(1.0 / 8.0);
			NUnit.Framework.Assert.AreEqual(7, devTrainTest5.First().Size());
			NUnit.Framework.Assert.AreEqual(0, devTrainTest5.Second().Size());
		}
		// Sonal did this, but I think she got it wrong and either should have past in test ratio or have taken p.second()
		// double trainRatio = 0.9;
		// Pair<GeneralDataset<String,String>,GeneralDataset<String,String>> p = data.split(0, (int) Math.floor(data.size() * trainRatio));
		// assertEquals(6, p.first().size());
	}
}
