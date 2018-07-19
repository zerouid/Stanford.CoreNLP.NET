using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DatasetTest
	{
		[NUnit.Framework.Test]
		public static void TestDataset()
		{
			Dataset<string, string> data = new Dataset<string, string>();
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "cough", "congestion" }), "cold"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "fever", "cough", "nausea" }), "flu"));
			data.Add(new BasicDatum<string, string>(Arrays.AsList(new string[] { "cough", "congestion" }), "cold"));
			// data.summaryStatistics();
			NUnit.Framework.Assert.AreEqual(4, data.NumFeatures());
			NUnit.Framework.Assert.AreEqual(4, data.NumFeatureTypes());
			NUnit.Framework.Assert.AreEqual(2, data.NumClasses());
			NUnit.Framework.Assert.AreEqual(8, data.NumFeatureTokens());
			NUnit.Framework.Assert.AreEqual(3, data.Size());
			data.ApplyFeatureCountThreshold(2);
			NUnit.Framework.Assert.AreEqual(3, data.NumFeatures());
			NUnit.Framework.Assert.AreEqual(3, data.NumFeatureTypes());
			NUnit.Framework.Assert.AreEqual(2, data.NumClasses());
			NUnit.Framework.Assert.AreEqual(7, data.NumFeatureTokens());
			NUnit.Framework.Assert.AreEqual(3, data.Size());
			//Dataset data = Dataset.readSVMLightFormat(args[0]);
			//double[] scores = data.getInformationGains();
			//System.out.println(ArrayMath.mean(scores));
			//System.out.println(ArrayMath.variance(scores));
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			LinearClassifier<string, string> classifier = factory.TrainClassifier(data);
			IDatum<string, string> d = new BasicDatum<string, string>(Arrays.AsList(new string[] { "cough", "fever" }));
			NUnit.Framework.Assert.AreEqual("Classification incorrect", "flu", classifier.ClassOf(d));
			ICounter<string> probs = classifier.ProbabilityOf(d);
			NUnit.Framework.Assert.AreEqual("Returned probability incorrect", 0.4553, probs.GetCount("cold"), 0.0001);
			NUnit.Framework.Assert.AreEqual("Returned probability incorrect", 0.5447, probs.GetCount("flu"), 0.0001);
			System.Console.Out.WriteLine();
		}
	}
}
