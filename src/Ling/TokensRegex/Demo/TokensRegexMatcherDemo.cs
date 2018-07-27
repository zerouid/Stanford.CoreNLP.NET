using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <author>Christopher Manning</author>
	public class TokensRegexMatcherDemo
	{
		private TokensRegexMatcherDemo()
		{
		}

		// static main only
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,lemma,ner"));
			Annotation annotation = new Annotation("Casey is 21. Sally Atkinson's age is 30.");
			pipeline.Annotate(annotation);
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<TokenSequencePattern> tokenSequencePatterns = new List<TokenSequencePattern>();
			string[] patterns = new string[] { "(?$who [ ner: PERSON]+ ) /is/ (?$age [ pos: CD ] )", "(?$who [ ner: PERSON]+ ) /'s/ /age/ /is/ (?$age [ pos: CD ] )" };
			foreach (string line in patterns)
			{
				TokenSequencePattern pattern = TokenSequencePattern.Compile(line);
				tokenSequencePatterns.Add(pattern);
			}
			MultiPatternMatcher<ICoreMap> multiMatcher = TokenSequencePattern.GetMultiPatternMatcher(tokenSequencePatterns);
			int i = 0;
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				System.Console.Out.WriteLine("Sentence #" + ++i);
				System.Console.Out.Write("  Tokens:");
				foreach (CoreLabel token in tokens)
				{
					System.Console.Out.Write(' ');
					System.Console.Out.Write(token.ToShortString("Text", "PartOfSpeech", "NamedEntityTag"));
				}
				System.Console.Out.WriteLine();
				IList<ISequenceMatchResult<ICoreMap>> answers = multiMatcher.FindNonOverlapping(tokens);
				int j = 0;
				foreach (ISequenceMatchResult<ICoreMap> matched in answers)
				{
					System.Console.Out.WriteLine("  Match #" + ++j);
					System.Console.Out.WriteLine("    match: " + matched.Group(0));
					System.Console.Out.WriteLine("      who: " + matched.Group("$who"));
					System.Console.Out.WriteLine("      age: " + matched.Group("$age"));
				}
			}
		}
	}
}
