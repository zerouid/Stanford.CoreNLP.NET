using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify.Demo
{
	internal class ClassifierDemo
	{
		private static string where = string.Empty;

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				where = args[0] + File.separator;
			}
			System.Console.Out.WriteLine("Training ColumnDataClassifier");
			ColumnDataClassifier cdc = new ColumnDataClassifier(where + "examples/cheese2007.prop");
			cdc.TrainClassifier(where + "examples/cheeseDisease.train");
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Testing predictions of ColumnDataClassifier");
			foreach (string line in ObjectBank.GetLineIterator(where + "examples/cheeseDisease.test", "utf-8"))
			{
				// instead of the method in the line below, if you have the individual elements
				// already you can use cdc.makeDatumFromStrings(String[])
				IDatum<string, string> d = cdc.MakeDatumFromLine(line);
				System.Console.Out.Printf("%s  ==>  %s (%.4f)%n", line, cdc.ClassOf(d), cdc.ScoresOf(d).GetCount(cdc.ClassOf(d)));
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Testing accuracy of ColumnDataClassifier");
			Pair<double, double> performance = cdc.TestClassifier(where + "examples/cheeseDisease.test");
			System.Console.Out.Printf("Accuracy: %.3f; macro-F1: %.3f%n", performance.First(), performance.Second());
			DemonstrateSerialization();
			DemonstrateSerializationColumnDataClassifier();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private static void DemonstrateSerialization()
		{
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Demonstrating working with a serialized classifier");
			ColumnDataClassifier cdc = new ColumnDataClassifier(where + "examples/cheese2007.prop");
			IClassifier<string, string> cl = cdc.MakeClassifier(cdc.ReadTrainingExamples(where + "examples/cheeseDisease.train"));
			// Exhibit serialization and deserialization working. Serialized to bytes in memory for simplicity
			System.Console.Out.WriteLine();
			ByteArrayOutputStream baos = new ByteArrayOutputStream();
			ObjectOutputStream oos = new ObjectOutputStream(baos);
			oos.WriteObject(cl);
			oos.Close();
			byte[] @object = baos.ToByteArray();
			ByteArrayInputStream bais = new ByteArrayInputStream(@object);
			ObjectInputStream ois = new ObjectInputStream(bais);
			LinearClassifier<string, string> lc = ErasureUtils.UncheckedCast(ois.ReadObject());
			ois.Close();
			ColumnDataClassifier cdc2 = new ColumnDataClassifier(where + "examples/cheese2007.prop");
			// We compare the output of the deserialized classifier lc versus the original one cl
			// For both we use a ColumnDataClassifier to convert text lines to examples
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Making predictions with both classifiers");
			foreach (string line in ObjectBank.GetLineIterator(where + "examples/cheeseDisease.test", "utf-8"))
			{
				IDatum<string, string> d = cdc.MakeDatumFromLine(line);
				IDatum<string, string> d2 = cdc2.MakeDatumFromLine(line);
				System.Console.Out.Printf("%s  =origi=>  %s (%.4f)%n", line, cl.ClassOf(d), cl.ScoresOf(d).GetCount(cl.ClassOf(d)));
				System.Console.Out.Printf("%s  =deser=>  %s (%.4f)%n", line, lc.ClassOf(d2), lc.ScoresOf(d).GetCount(lc.ClassOf(d)));
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private static void DemonstrateSerializationColumnDataClassifier()
		{
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Demonstrating working with a serialized classifier using serializeTo");
			ColumnDataClassifier cdc = new ColumnDataClassifier(where + "examples/cheese2007.prop");
			cdc.TrainClassifier(where + "examples/cheeseDisease.train");
			// Exhibit serialization and deserialization working. Serialized to bytes in memory for simplicity
			System.Console.Out.WriteLine();
			ByteArrayOutputStream baos = new ByteArrayOutputStream();
			ObjectOutputStream oos = new ObjectOutputStream(baos);
			cdc.SerializeClassifier(oos);
			oos.Close();
			byte[] @object = baos.ToByteArray();
			ByteArrayInputStream bais = new ByteArrayInputStream(@object);
			ObjectInputStream ois = new ObjectInputStream(bais);
			ColumnDataClassifier cdc2 = ColumnDataClassifier.GetClassifier(ois);
			ois.Close();
			// We compare the output of the deserialized classifier cdc2 versus the original one cl
			// For both we use a ColumnDataClassifier to convert text lines to examples
			System.Console.Out.WriteLine("Making predictions with both classifiers");
			foreach (string line in ObjectBank.GetLineIterator(where + "examples/cheeseDisease.test", "utf-8"))
			{
				IDatum<string, string> d = cdc.MakeDatumFromLine(line);
				IDatum<string, string> d2 = cdc2.MakeDatumFromLine(line);
				System.Console.Out.Printf("%s  =origi=>  %s (%.4f)%n", line, cdc.ClassOf(d), cdc.ScoresOf(d).GetCount(cdc.ClassOf(d)));
				System.Console.Out.Printf("%s  =deser=>  %s (%.4f)%n", line, cdc2.ClassOf(d2), cdc2.ScoresOf(d).GetCount(cdc2.ClassOf(d)));
			}
		}
	}
}
