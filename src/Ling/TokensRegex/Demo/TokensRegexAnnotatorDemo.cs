using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <summary>Demo illustrating how to use TokensRegexAnnotator.</summary>
	/// <remarks>
	/// Demo illustrating how to use TokensRegexAnnotator.
	/// Usage:
	/// java edu.stanford.nlp.ling.tokensregex.demo.TokensRegexAnnotatorDemo rulesFile [inputFile [outputFile]]
	/// </remarks>
	public class TokensRegexAnnotatorDemo
	{
		private TokensRegexAnnotatorDemo()
		{
		}

		// static main method
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
				rules = "edu/stanford/nlp/ling/tokensregex/demo/rules/colors.rules.txt";
			}
			if (args.Length > 2)
			{
				@out = new PrintWriter(args[2]);
			}
			else
			{
				@out = new PrintWriter(System.Console.Out);
			}
			Properties properties = new Properties();
			properties.SetProperty("annotators", "tokenize,ssplit,pos,lemma,ner,tokensregexdemo");
			properties.SetProperty("customAnnotatorClass.tokensregexdemo", "edu.stanford.nlp.pipeline.TokensRegexAnnotator");
			properties.SetProperty("tokensregexdemo.rules", rules);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(properties);
			Annotation annotation;
			if (args.Length > 1)
			{
				annotation = new Annotation(IOUtils.SlurpFileNoExceptions(args[1]));
			}
			else
			{
				annotation = new Annotation("Both blue and light blue are nice colors.");
			}
			pipeline.Annotate(annotation);
			// An Annotation is a Map and you can get and use the various analyses individually.
			// The toString() method on an Annotation just prints the text of the Annotation
			// But you can see what is in it with other methods like toShorterString()
			@out.Println();
			@out.Println("The top level annotation");
			@out.Println(annotation.ToShorterString());
			@out.Println();
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				// NOTE: Depending on what tokensregex rules are specified, there are other annotations
				//       that are of interest other than just the tokens and what we print out here
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
	}
}
