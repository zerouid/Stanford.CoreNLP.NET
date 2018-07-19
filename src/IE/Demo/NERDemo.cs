using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Demo
{
	/// <summary>This is a demo of calling CRFClassifier programmatically.</summary>
	/// <remarks>
	/// This is a demo of calling CRFClassifier programmatically.
	/// <p>
	/// Usage:
	/// <c>java -mx400m -cp "*" NERDemo [serializedClassifier [fileName]]</c>
	/// <p>
	/// If arguments aren't specified, they default to
	/// classifiers/english.all.3class.distsim.crf.ser.gz and some hardcoded sample text.
	/// If run with arguments, it shows some of the ways to get k-best labelings and
	/// probabilities out with CRFClassifier. If run without arguments, it shows some of
	/// the alternative output formats that you can get.
	/// <p>
	/// To use CRFClassifier from the command line:
	/// </p><blockquote>
	/// <c>java -mx400m edu.stanford.nlp.ie.crf.CRFClassifier -loadClassifier [classifier] -textFile [file]</c>
	/// </blockquote><p>
	/// Or if the file is already tokenized and one word per line, perhaps in
	/// a tab-separated value format with extra columns for part-of-speech tag,
	/// etc., use the version below (note the 's' instead of the 'x'):
	/// </p><blockquote>
	/// <c>java -mx400m edu.stanford.nlp.ie.crf.CRFClassifier -loadClassifier [classifier] -testFile [file]</c>
	/// </blockquote>
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	public class NERDemo
	{
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			string serializedClassifier = "classifiers/english.all.3class.distsim.crf.ser.gz";
			if (args.Length > 0)
			{
				serializedClassifier = args[0];
			}
			AbstractSequenceClassifier<CoreLabel> classifier = CRFClassifier.GetClassifier(serializedClassifier);
			/* For either a file to annotate or for the hardcoded text example, this
			demo file shows several ways to process the input, for teaching purposes.
			*/
			if (args.Length > 1)
			{
				/* For the file, it shows (1) how to run NER on a String, (2) how
				to get the entities in the String with character offsets, and
				(3) how to run NER on a whole file (without loading it into a String).
				*/
				string fileContents = IOUtils.SlurpFile(args[1]);
				IList<IList<CoreLabel>> @out = classifier.Classify(fileContents);
				foreach (IList<CoreLabel> sentence in @out)
				{
					foreach (CoreLabel word in sentence)
					{
						System.Console.Out.Write(word.Word() + '/' + word.Get(typeof(CoreAnnotations.AnswerAnnotation)) + ' ');
					}
					System.Console.Out.WriteLine();
				}
				System.Console.Out.WriteLine("---");
				@out = classifier.ClassifyFile(args[1]);
				foreach (IList<CoreLabel> sentence_1 in @out)
				{
					foreach (CoreLabel word in sentence_1)
					{
						System.Console.Out.Write(word.Word() + '/' + word.Get(typeof(CoreAnnotations.AnswerAnnotation)) + ' ');
					}
					System.Console.Out.WriteLine();
				}
				System.Console.Out.WriteLine("---");
				IList<Triple<string, int, int>> list = classifier.ClassifyToCharacterOffsets(fileContents);
				foreach (Triple<string, int, int> item in list)
				{
					System.Console.Out.WriteLine(item.First() + ": " + Sharpen.Runtime.Substring(fileContents, item.Second(), item.Third()));
				}
				System.Console.Out.WriteLine("---");
				System.Console.Out.WriteLine("Ten best entity labelings");
				IDocumentReaderAndWriter<CoreLabel> readerAndWriter = classifier.MakePlainTextReaderAndWriter();
				classifier.ClassifyAndWriteAnswersKBest(args[1], 10, readerAndWriter);
				System.Console.Out.WriteLine("---");
				System.Console.Out.WriteLine("Per-token marginalized probabilities");
				classifier.PrintProbs(args[1], readerAndWriter);
			}
			else
			{
				// -- This code prints out the first order (token pair) clique probabilities.
				// -- But that output is a bit overwhelming, so we leave it commented out by default.
				// System.out.println("---");
				// System.out.println("First Order Clique Probabilities");
				// ((CRFClassifier) classifier).printFirstOrderProbs(args[1], readerAndWriter);
				/* For the hard-coded String, it shows how to run it on a single
				sentence, and how to do this and produce several formats, including
				slash tags and an inline XML output format. It also shows the full
				contents of the {@code CoreLabel}s that are constructed by the
				classifier. And it shows getting out the probabilities of different
				assignments and an n-best list of classifications with probabilities.
				*/
				string[] example = new string[] { "Good afternoon Rajat Raina, how are you today?", "I go to school at Stanford University, which is located in California." };
				foreach (string str in example)
				{
					System.Console.Out.WriteLine(classifier.ClassifyToString(str));
				}
				System.Console.Out.WriteLine("---");
				foreach (string str_1 in example)
				{
					// This one puts in spaces and newlines between tokens, so just print not println.
					System.Console.Out.Write(classifier.ClassifyToString(str_1, "slashTags", false));
				}
				System.Console.Out.WriteLine("---");
				foreach (string str_2 in example)
				{
					// This one is best for dealing with the output as a TSV (tab-separated column) file.
					// The first column gives entities, the second their classes, and the third the remaining text in a document
					System.Console.Out.Write(classifier.ClassifyToString(str_2, "tabbedEntities", false));
				}
				System.Console.Out.WriteLine("---");
				foreach (string str_3 in example)
				{
					System.Console.Out.WriteLine(classifier.ClassifyWithInlineXML(str_3));
				}
				System.Console.Out.WriteLine("---");
				foreach (string str_4 in example)
				{
					System.Console.Out.WriteLine(classifier.ClassifyToString(str_4, "xml", true));
				}
				System.Console.Out.WriteLine("---");
				foreach (string str_5 in example)
				{
					System.Console.Out.Write(classifier.ClassifyToString(str_5, "tsv", false));
				}
				System.Console.Out.WriteLine("---");
				// This gets out entities with character offsets
				int j = 0;
				foreach (string str_6 in example)
				{
					j++;
					IList<Triple<string, int, int>> triples = classifier.ClassifyToCharacterOffsets(str_6);
					foreach (Triple<string, int, int> trip in triples)
					{
						System.Console.Out.Printf("%s over character offsets [%d, %d) in sentence %d.%n", trip.First(), trip.Second(), trip.third, j);
					}
				}
				System.Console.Out.WriteLine("---");
				// This prints out all the details of what is stored for each token
				int i = 0;
				foreach (string str_7 in example)
				{
					foreach (IList<CoreLabel> lcl in classifier.Classify(str_7))
					{
						foreach (CoreLabel cl in lcl)
						{
							System.Console.Out.Write(i++ + ": ");
							System.Console.Out.WriteLine(cl.ToShorterString());
						}
					}
				}
				System.Console.Out.WriteLine("---");
			}
		}
	}
}
