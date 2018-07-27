using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <summary>Demo illustrating how to use TokensRegexAnnotator for tweaks to tokenization</summary>
	public class TokensRegexRetokenizeDemo
	{
		private static void RunPipeline(StanfordCoreNLP pipeline, string text, PrintWriter @out)
		{
			Annotation annotation = new Annotation(text);
			pipeline.Annotate(annotation);
			// An Annotation is a Map and you can get and use the various analyses individually.
			@out.Println();
			// The toString() method on an Annotation just prints the text of the Annotation
			// But you can see what is in it with other methods like toShorterString()
			@out.Println("The top level annotation");
			@out.Println(annotation.ToShorterString());
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				// Print out token annotations
				foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					// Print out words, lemma, ne, and normalized ne
					string word = token.Get(typeof(CoreAnnotations.TextAnnotation));
					string lemma = token.Get(typeof(CoreAnnotations.LemmaAnnotation));
					string pos = token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
					string ne = token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					string normalized = token.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation));
					@out.Println("token: " + "word=" + word + ", lemma=" + lemma + ", pos=" + pos + ", ne=" + ne + ", normalized=" + normalized);
				}
			}
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			PrintWriter @out;
			string rules;
			if (args.Length > 0)
			{
				rules = args[0];
			}
			else
			{
				rules = "edu/stanford/nlp/ling/tokensregex/demo/rules/retokenize.rules.txt";
			}
			if (args.Length > 2)
			{
				@out = new PrintWriter(args[2]);
			}
			else
			{
				@out = new PrintWriter(System.Console.Out);
			}
			string text;
			if (args.Length > 1)
			{
				text = IOUtils.SlurpFileNoExceptions(args[1]);
			}
			else
			{
				text = "Do we tokenize on hyphens? one-two-three-four.  How about dates? 03-16-2015.";
			}
			Properties propertiesDefaultTokenize = new Properties();
			propertiesDefaultTokenize.SetProperty("annotators", "tokenize,ssplit,pos,lemma,ner");
			StanfordCoreNLP pipelineDefaultRetokenize = new StanfordCoreNLP();
			@out.Println("Default tokenization: ");
			RunPipeline(pipelineDefaultRetokenize, text, @out);
			Properties properties = new Properties();
			properties.SetProperty("annotators", "tokenize,retokenize,ssplit,pos,lemma,ner");
			properties.SetProperty("customAnnotatorClass.retokenize", "edu.stanford.nlp.pipeline.TokensRegexAnnotator");
			properties.SetProperty("retokenize.rules", rules);
			StanfordCoreNLP pipelineWithRetokenize = new StanfordCoreNLP(properties);
			@out.Println();
			@out.Println("Always tokenize hyphens: ");
			RunPipeline(pipelineWithRetokenize, text, @out);
		}
	}
}
