using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <summary>Demo of how to use TokenSequence{Pattern,Matcher}.</summary>
	/// <author>Christopher Manning</author>
	public class TokensRegexMatcher
	{
		private TokensRegexMatcher()
		{
		}

		// static demo class
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				System.Console.Error.WriteLine("TokensRegexMatcher rules file [outFile]");
				return;
			}
			string rules = args[0];
			PrintWriter @out;
			if (args.Length > 2)
			{
				@out = new PrintWriter(args[2]);
			}
			else
			{
				@out = new PrintWriter(System.Console.Out);
			}
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,lemma,ner"));
			Annotation annotation = new Annotation(IOUtils.SlurpFileNoExceptions(args[1]));
			pipeline.Annotate(annotation);
			// Load lines of file as TokenSequencePatterns
			IList<TokenSequencePattern> tokenSequencePatterns = new List<TokenSequencePattern>();
			foreach (string line in ObjectBank.GetLineIterator(rules))
			{
				TokenSequencePattern pattern = TokenSequencePattern.Compile(line);
				tokenSequencePatterns.Add(pattern);
			}
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int i = 0;
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				@out.Println("Sentence #" + ++i);
				@out.Print("  Tokens:");
				foreach (CoreLabel token in tokens)
				{
					@out.Print(' ');
					@out.Print(token.ToShortString("Text", "PartOfSpeech", "NamedEntityTag"));
				}
				@out.Println();
				MultiPatternMatcher<ICoreMap> multiMatcher = TokenSequencePattern.GetMultiPatternMatcher(tokenSequencePatterns);
				IList<ISequenceMatchResult<ICoreMap>> answers = multiMatcher.FindNonOverlapping(tokens);
				int j = 0;
				foreach (ISequenceMatchResult<ICoreMap> matched in answers)
				{
					@out.Println("  Match #" + ++j);
					for (int k = 0; k <= matched.GroupCount(); k++)
					{
						@out.Println("    group " + k + " = " + matched.Group(k));
					}
				}
			}
			@out.Flush();
		}
	}
}
