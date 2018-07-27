using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Nndep;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Nndep.Demo
{
	/// <summary>
	/// Demonstrates how to use the NN dependency
	/// parser via a CoreNLP pipeline.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class DependencyParserCoreNLPDemo
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.Demo.DependencyParserCoreNLPDemo));

		private DependencyParserCoreNLPDemo()
		{
		}

		// static main method only
		public static void Main(string[] args)
		{
			string text;
			if (args.Length > 0)
			{
				text = IOUtils.SlurpFileNoExceptions(args[0], "utf-8");
			}
			else
			{
				text = "I can almost always tell when movies use fake dinosaurs.";
			}
			Annotation ann = new Annotation(text);
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,depparse", "depparse.model", DependencyParser.DefaultModel);
			AnnotationPipeline pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(ann);
			foreach (ICoreMap sent in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				SemanticGraph sg = sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
				log.Info(IOUtils.eolChar + sg.ToString(SemanticGraph.OutputFormat.List));
			}
		}
	}
}
