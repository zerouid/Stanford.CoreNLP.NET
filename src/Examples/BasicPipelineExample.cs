using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Examples
{
	public class BasicPipelineExample
	{
		public static string text = "Joe Smith was born in California. " + "In 2017, he went to Paris, France in the summer. " + "His flight left at 3:00pm on July 10th, 2017. " + "After eating some escargot for the first time, Joe said, \"That was delicious!\" "
			 + "He sent a postcard to his sister Jane Smith. " + "After hearing about Joe's trip, Jane decided she might go to France one day.";

		public static void Main(string[] args)
		{
			// set up pipeline properties
			Properties props = new Properties();
			// set the list of annotators to run
			props.SetProperty("annotators", "tokenize,ssplit,pos,lemma,ner,parse,depparse,coref,kbp,quote");
			// set a property for an annotator, in this case the coref annotator is being set to use the neural algorithm
			props.SetProperty("coref.algorithm", "neural");
			// build pipeline
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// create a document object
			CoreDocument document = new CoreDocument(text);
			// annnotate the document
			pipeline.Annotate(document);
			// examples
			// 10th token of the document
			CoreLabel token = document.Tokens()[10];
			System.Console.Out.WriteLine("Example: token");
			System.Console.Out.WriteLine(token);
			System.Console.Out.WriteLine();
			// text of the first sentence
			string sentenceText = document.Sentences()[0].Text();
			System.Console.Out.WriteLine("Example: sentence");
			System.Console.Out.WriteLine(sentenceText);
			System.Console.Out.WriteLine();
			// second sentence
			CoreSentence sentence = document.Sentences()[1];
			// list of the part-of-speech tags for the second sentence
			IList<string> posTags = sentence.PosTags();
			System.Console.Out.WriteLine("Example: pos tags");
			System.Console.Out.WriteLine(posTags);
			System.Console.Out.WriteLine();
			// list of the ner tags for the second sentence
			IList<string> nerTags = sentence.NerTags();
			System.Console.Out.WriteLine("Example: ner tags");
			System.Console.Out.WriteLine(nerTags);
			System.Console.Out.WriteLine();
			// constituency parse for the second sentence
			Tree constituencyParse = sentence.ConstituencyParse();
			System.Console.Out.WriteLine("Example: constituency parse");
			System.Console.Out.WriteLine(constituencyParse);
			System.Console.Out.WriteLine();
			// dependency parse for the second sentence
			SemanticGraph dependencyParse = sentence.DependencyParse();
			System.Console.Out.WriteLine("Example: dependency parse");
			System.Console.Out.WriteLine(dependencyParse);
			System.Console.Out.WriteLine();
			// kbp relations found in fifth sentence
			IList<RelationTriple> relations = document.Sentences()[4].Relations();
			System.Console.Out.WriteLine("Example: relation");
			System.Console.Out.WriteLine(relations[0]);
			System.Console.Out.WriteLine();
			// entity mentions in the second sentence
			IList<CoreEntityMention> entityMentions = sentence.EntityMentions();
			System.Console.Out.WriteLine("Example: entity mentions");
			System.Console.Out.WriteLine(entityMentions);
			System.Console.Out.WriteLine();
			// coreference between entity mentions
			CoreEntityMention originalEntityMention = document.Sentences()[3].EntityMentions()[1];
			System.Console.Out.WriteLine("Example: original entity mention");
			System.Console.Out.WriteLine(originalEntityMention);
			System.Console.Out.WriteLine("Example: canonical entity mention");
			System.Console.Out.WriteLine(originalEntityMention.CanonicalEntityMention().Get());
			System.Console.Out.WriteLine();
			// get document wide coref info
			IDictionary<int, CorefChain> corefChains = document.CorefChains();
			System.Console.Out.WriteLine("Example: coref chains for document");
			System.Console.Out.WriteLine(corefChains);
			System.Console.Out.WriteLine();
			// get quotes in document
			IList<CoreQuote> quotes = document.Quotes();
			CoreQuote quote = quotes[0];
			System.Console.Out.WriteLine("Example: quote");
			System.Console.Out.WriteLine(quote);
			System.Console.Out.WriteLine();
			// original speaker of quote
			// note that quote.speaker() returns an Optional
			System.Console.Out.WriteLine("Example: original speaker of quote");
			System.Console.Out.WriteLine(quote.Speaker().Get());
			System.Console.Out.WriteLine();
			// canonical speaker of quote
			System.Console.Out.WriteLine("Example: canonical speaker of quote");
			System.Console.Out.WriteLine(quote.CanonicalSpeaker().Get());
			System.Console.Out.WriteLine();
		}
	}
}
