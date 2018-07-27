using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Pipeline.Demo
{
	/// <summary>This class demonstrates building and using a Stanford CoreNLP pipeline.</summary>
	public class StanfordCoreNlpDemo
	{
		/// <summary>Usage: java -cp "*" StanfordCoreNlpDemo [inputFile [outputTextFile [outputXmlFile]]]</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			// set up optional output files
			PrintWriter @out;
			if (args.Length > 1)
			{
				@out = new PrintWriter(args[1]);
			}
			else
			{
				@out = new PrintWriter(System.Console.Out);
			}
			PrintWriter xmlOut = null;
			if (args.Length > 2)
			{
				xmlOut = new PrintWriter(args[2]);
			}
			// Create a CoreNLP pipeline. To build the default pipeline, you can just use:
			//   StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// Here's a more complex setup example:
			//   Properties props = new Properties();
			//   props.put("annotators", "tokenize, ssplit, pos, lemma, ner, depparse");
			//   props.put("ner.model", "edu/stanford/nlp/models/ner/english.all.3class.distsim.crf.ser.gz");
			//   props.put("ner.applyNumericClassifiers", "false");
			//   StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// Add in sentiment
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize, ssplit, pos, lemma, ner, parse, dcoref, sentiment");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// Initialize an Annotation with some text to be annotated. The text is the argument to the constructor.
			Annotation annotation;
			if (args.Length > 0)
			{
				annotation = new Annotation(IOUtils.SlurpFileNoExceptions(args[0]));
			}
			else
			{
				annotation = new Annotation("Kosgi Santosh sent an email to Stanford University. He didn't get a reply.");
			}
			// run all the selected Annotators on this text
			pipeline.Annotate(annotation);
			// this prints out the results of sentence analysis to file(s) in good formats
			pipeline.PrettyPrint(annotation, @out);
			if (xmlOut != null)
			{
				pipeline.XmlPrint(annotation, xmlOut);
			}
			// Access the Annotation in code
			// The toString() method on an Annotation just prints the text of the Annotation
			// But you can see what is in it with other methods like toShorterString()
			@out.Println();
			@out.Println("The top level annotation");
			@out.Println(annotation.ToShorterString());
			@out.Println();
			// An Annotation is a Map with Class keys for the linguistic analysis types.
			// You can get and use the various analyses individually.
			// For instance, this gets the parse tree of the first sentence in the text.
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null && !sentences.IsEmpty())
			{
				ICoreMap sentence = sentences[0];
				@out.Println("The keys of the first sentence's CoreMap are:");
				@out.Println(sentence.KeySet());
				@out.Println();
				@out.Println("The first sentence is:");
				@out.Println(sentence.ToShorterString());
				@out.Println();
				@out.Println("The first sentence tokens are:");
				foreach (ICoreMap token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					@out.Println(token.ToShorterString());
				}
				Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				@out.Println();
				@out.Println("The first sentence parse tree is:");
				tree.PennPrint(@out);
				@out.Println();
				@out.Println("The first sentence basic dependencies are:");
				@out.Println(sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)).ToString(SemanticGraph.OutputFormat.List));
				@out.Println("The first sentence collapsed, CC-processed dependencies are:");
				SemanticGraph graph = sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation));
				@out.Println(graph.ToString(SemanticGraph.OutputFormat.List));
				// Access coreference. In the coreference link graph,
				// each chain stores a set of mentions that co-refer with each other,
				// along with a method for getting the most representative mention.
				// Both sentence and token offsets start at 1!
				@out.Println("Coreference information");
				IDictionary<int, CorefChain> corefChains = annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
				if (corefChains == null)
				{
					return;
				}
				foreach (KeyValuePair<int, CorefChain> entry in corefChains)
				{
					@out.Println("Chain " + entry.Key);
					foreach (CorefChain.CorefMention m in entry.Value.GetMentionsInTextualOrder())
					{
						// We need to subtract one since the indices count from 1 but the Lists start from 0
						IList<CoreLabel> tokens = sentences[m.sentNum - 1].Get(typeof(CoreAnnotations.TokensAnnotation));
						// We subtract two for end: one for 0-based indexing, and one because we want last token of mention not one following.
						@out.Println("  " + m + ", i.e., 0-based character offsets [" + tokens[m.startIndex - 1].BeginPosition() + ", " + tokens[m.endIndex - 2].EndPosition() + ")");
					}
				}
				@out.Println();
				@out.Println("The first sentence overall sentiment rating is " + sentence.Get(typeof(SentimentCoreAnnotations.SentimentClass)));
			}
			IOUtils.CloseIgnoringExceptions(@out);
			IOUtils.CloseIgnoringExceptions(xmlOut);
		}
	}
}
