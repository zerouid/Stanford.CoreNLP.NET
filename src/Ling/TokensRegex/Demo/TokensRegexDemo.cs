using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <summary>Demo illustrating how to use CoreMapExtractor.</summary>
	/// <remarks>
	/// Demo illustrating how to use CoreMapExtractor.
	/// Usage:
	/// java edu.stanford.nlp.ling.tokensregex.demo.TokensRegexDemo rulesFile [inputFile [outputFile]]
	/// <p>
	/// This is a good example to fun to s
	/// </p>
	/// </remarks>
	public class TokensRegexDemo
	{
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string rules;
			if (args.Length > 0)
			{
				rules = args[0];
			}
			else
			{
				rules = "edu/stanford/nlp/ling/tokensregex/demo/rules/expr.rules.txt";
			}
			PrintWriter @out;
			if (args.Length > 2)
			{
				@out = new PrintWriter(args[2]);
			}
			else
			{
				@out = new PrintWriter(System.Console.Out);
			}
			CoreMapExpressionExtractor<MatchedExpression> extractor = CoreMapExpressionExtractor.CreateExtractorFromFiles(TokenSequencePattern.GetNewEnv(), rules);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,lemma,ner"));
			Annotation annotation;
			if (args.Length > 1)
			{
				annotation = new Annotation(IOUtils.SlurpFileNoExceptions(args[1]));
			}
			else
			{
				annotation = new Annotation("( ( five plus three plus four ) * 2 ) divided by three");
			}
			pipeline.Annotate(annotation);
			// An Annotation is a Map and you can get and use the various analyses individually.
			@out.Println();
			// The toString() method on an Annotation just prints the text of the Annotation
			// But you can see what is in it with other methods like toShorterString()
			@out.Println("The top level annotation");
			@out.Println(annotation.ToShorterString());
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int i = 0;
			foreach (ICoreMap sentence in sentences)
			{
				@out.Println("Sentence #" + ++i);
				foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					@out.Println("  Token: " + "word=" + token.Get(typeof(CoreAnnotations.TextAnnotation)) + ",  pos=" + token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)) + ", ne=" + token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
				}
				IList<MatchedExpression> matchedExpressions = extractor.ExtractExpressions(sentence);
				foreach (MatchedExpression matched in matchedExpressions)
				{
					// Print out matched text and value
					@out.Println("Matched expression: " + matched.GetText() + " with value " + matched.GetValue());
					// Print out token information
					ICoreMap cm = matched.GetAnnotation();
					foreach (CoreLabel token_1 in cm.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						string word = token_1.Get(typeof(CoreAnnotations.TextAnnotation));
						string lemma = token_1.Get(typeof(CoreAnnotations.LemmaAnnotation));
						string pos = token_1.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
						string ne = token_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
						@out.Println("  Matched token: " + "word=" + word + ", lemma=" + lemma + ", pos=" + pos + ", ne=" + ne);
					}
				}
			}
			@out.Flush();
		}
	}
}
