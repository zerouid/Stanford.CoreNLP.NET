using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This annotator takes a properties file that was used to
	/// train a ColumnDataClassifier and creates an annotator that
	/// classifies the text by loading the classifier and running it.
	/// </summary>
	/// <remarks>
	/// This annotator takes a properties file that was used to
	/// train a ColumnDataClassifier and creates an annotator that
	/// classifies the text by loading the classifier and running it.
	/// So you must have the properties that were used to train the classifier.
	/// </remarks>
	/// <author>joberant</author>
	/// <version>9/8/14</version>
	public class ColumnDataClassifierAnnotator : IAnnotator
	{
		private readonly ColumnDataClassifier cdcClassifier;

		private readonly bool verbose;

		private const string DummyLabelColumn = "DUMMY\t";

		public ColumnDataClassifierAnnotator(string propFile)
		{
			cdcClassifier = new ColumnDataClassifier(propFile);
			verbose = false;
		}

		public ColumnDataClassifierAnnotator(Properties props)
		{
			// todo [cdm 2016]: Should really set from properties in propFile
			cdcClassifier = new ColumnDataClassifier(props);
			verbose = PropertiesUtils.GetBool(props, "classify.verbose", false);
		}

		public ColumnDataClassifierAnnotator(string propFile, bool verbose)
		{
			cdcClassifier = new ColumnDataClassifier(propFile);
			this.verbose = verbose;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (verbose)
			{
				System.Console.Out.WriteLine("Adding column data classifier annotation...");
			}
			string text = DummyLabelColumn + annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			if (verbose)
			{
				System.Console.Out.WriteLine("Dummy column: " + text);
			}
			// todo [cdm 2016]: At the moment this is hardwired to only work with answer = col 0, datum = col 1 classifier
			IDatum<string, string> datum = cdcClassifier.MakeDatumFromLine(text);
			if (verbose)
			{
				System.Console.Out.WriteLine("Datum: " + datum.ToString());
			}
			string label = cdcClassifier.ClassOf(datum);
			annotation.Set(typeof(CoreAnnotations.ColumnDataClassifierAnnotation), label);
			if (verbose)
			{
				System.Console.Out.WriteLine(string.Format("annotation=%s", annotation.Get(typeof(CoreAnnotations.ColumnDataClassifierAnnotation))));
			}
			if (verbose)
			{
				System.Console.Out.WriteLine("Done.");
			}
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.EmptySet();
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.EmptySet();
		}

		//test - run from your top javanlp directory to get the files etc.
		public static void Main(string[] args)
		{
			Properties props = StringUtils.PropFileToProperties("projects/core/src/edu/stanford/nlp/classify/mood.prop");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation happyAnnotation = new Annotation("I am so glad this is awesome");
			pipeline.Annotate(happyAnnotation);
			Annotation sadAnnotation = new Annotation("I am so gloomy and depressed");
			pipeline.Annotate(sadAnnotation);
			Annotation bothAnnotation = new Annotation("I am so gloomy gloomy gloomy gloomy glad");
			pipeline.Annotate(bothAnnotation);
		}
	}
}
