using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Sample code that illustrates the training and use of a linear classifier.</summary>
	/// <remarks>
	/// Sample code that illustrates the training and use of a linear classifier.
	/// This toy example is taken from the slides:
	/// Christopher Manning and Dan Klein. 2003. Optimization, Maxent Models,
	/// and Conditional Estimation without Magic.
	/// Tutorial at HLT-NAACL 2003 and ACL 2003.
	/// </remarks>
	/// <author>Dan Klein</author>
	public class ClassifierExample
	{
		protected internal const string Green = "green";

		protected internal const string Red = "red";

		protected internal const string Working = "working";

		protected internal const string Broken = "broken";

		private ClassifierExample()
		{
		}

		// not instantiable
		protected internal static IDatum<string, string> MakeStopLights(string ns, string ew)
		{
			IList<string> features = new List<string>();
			// Create the north-south light feature
			features.Add("NS=" + ns);
			// Create the east-west light feature
			features.Add("EW=" + ew);
			// Create the label
			string label = (ns.Equals(ew) ? Broken : Working);
			return new BasicDatum<string, string>(features, label);
		}

		public static void Main(string[] args)
		{
			// Create a training set
			IList<IDatum<string, string>> trainingData = new List<IDatum<string, string>>();
			trainingData.Add(MakeStopLights(Green, Red));
			trainingData.Add(MakeStopLights(Green, Red));
			trainingData.Add(MakeStopLights(Green, Red));
			trainingData.Add(MakeStopLights(Red, Green));
			trainingData.Add(MakeStopLights(Red, Green));
			trainingData.Add(MakeStopLights(Red, Green));
			trainingData.Add(MakeStopLights(Red, Red));
			// Create a test set
			IDatum<string, string> workingLights = MakeStopLights(Green, Red);
			IDatum<string, string> brokenLights = MakeStopLights(Red, Red);
			// Build a classifier factory
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			factory.UseConjugateGradientAscent();
			// Turn on per-iteration convergence updates
			factory.SetVerbose(true);
			//Small amount of smoothing
			factory.SetSigma(10.0);
			// Build a classifier
			LinearClassifier<string, string> classifier = factory.TrainClassifier(trainingData);
			// Check out the learned weights
			classifier.Dump();
			// Test the classifier
			System.Console.Out.WriteLine("Working instance got: " + classifier.ClassOf(workingLights));
			classifier.JustificationOf(workingLights);
			System.Console.Out.WriteLine("Broken instance got: " + classifier.ClassOf(brokenLights));
			classifier.JustificationOf(brokenLights);
		}
	}
}
