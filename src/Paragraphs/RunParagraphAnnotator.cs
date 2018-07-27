using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Paragraphs
{
	/// <summary>Created by Grace Muzny on 05/11/2016.</summary>
	public class RunParagraphAnnotator
	{
		public static string Test1 = "Easy Peasy. Lemon squeezy.";

		public static string Test2 = "Easy Peasy. \nLemon squeezy.\n\n Blop dop bop.";

		public static string Test3 = "Easy Peasy. \n\nLemon squeezy. \n\n Bam! \n Not this one.";

		public static void Main(string[] args)
		{
			RunTest(Test1, "one");
			RunTest(Test1, "two");
			RunTest(Test2, "one");
			RunTest(Test2, "two");
			RunTest(Test3, "one");
			RunTest(Test3, "two");
		}

		public static void RunTest(string test, string num)
		{
			System.Console.Out.WriteLine("Testing: " + test + " : num newline breaks: " + num);
			Annotation ann = new Annotation(test);
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize,ssplit");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(ann);
			Properties propsPara = new Properties();
			propsPara.SetProperty("paragraphBreak", num);
			ParagraphAnnotator para = new ParagraphAnnotator(propsPara, true);
			para.Annotate(ann);
			foreach (ICoreMap sent in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				System.Console.Out.WriteLine(sent);
				System.Console.Out.WriteLine(sent.Get(typeof(CoreAnnotations.ParagraphIndexAnnotation)));
			}
		}
	}
}
