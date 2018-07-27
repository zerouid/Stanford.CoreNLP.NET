using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Pipeline.Demo
{
	/// <author>Christopher Manning</author>
	public class StanfordCoreNlpDemoChinese
	{
		private StanfordCoreNlpDemoChinese()
		{
		}

		// static main
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
			Properties props = new Properties();
			props.Load(IOUtils.ReaderFromString("StanfordCoreNLP-chinese.properties"));
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document;
			if (args.Length > 0)
			{
				document = new Annotation(IOUtils.SlurpFileNoExceptions(args[0]));
			}
			else
			{
				document = new Annotation("克林顿说，华盛顿将逐步落实对韩国的经济援助。金大中对克林顿的讲话报以掌声：克林顿总统在会谈中重申，他坚定地支持韩国摆脱经济危机。");
			}
			pipeline.Annotate(document);
			IList<ICoreMap> sentences = document.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int sentNo = 1;
			foreach (ICoreMap sentence in sentences)
			{
				@out.Println("Sentence #" + sentNo + " tokens are:");
				foreach (ICoreMap token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					@out.Println(token.ToShorterString("Text", "CharacterOffsetBegin", "CharacterOffsetEnd", "Index", "PartOfSpeech", "NamedEntityTag"));
				}
				@out.Println("Sentence #" + sentNo + " basic dependencies are:");
				@out.Println(sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)).ToString(SemanticGraph.OutputFormat.List));
				sentNo++;
			}
			// Access coreference.
			@out.Println("Coreference information");
			IDictionary<int, CorefChain> corefChains = document.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
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
					@out.Println("  " + m + ":[" + tokens[m.startIndex - 1].BeginPosition() + ", " + tokens[m.endIndex - 2].EndPosition() + ')');
				}
			}
			IOUtils.CloseIgnoringExceptions(@out);
		}
	}
}
