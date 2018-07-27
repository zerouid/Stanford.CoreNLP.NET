using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Tagger.Maxent.Documentation
{
	/// <summary>
	/// This demo shows user-provided sentences (i.e.,
	/// <c>List&lt;HasWord&gt;</c>
	/// )
	/// being tagged by the tagger. The sentences are generated by direct use
	/// of the DocumentPreprocessor class.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class TaggerDemo2
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Documentation.TaggerDemo2));

		private TaggerDemo2()
		{
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				log.Info("usage: java TaggerDemo2 modelFile fileToTag");
				return;
			}
			MaxentTagger tagger = new MaxentTagger(args[0]);
			ITokenizerFactory<CoreLabel> ptbTokenizerFactory = PTBTokenizer.Factory(new CoreLabelTokenFactory(), "untokenizable=noneKeep");
			BufferedReader r = new BufferedReader(new InputStreamReader(new FileInputStream(args[1]), "utf-8"));
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, "utf-8"));
			DocumentPreprocessor documentPreprocessor = new DocumentPreprocessor(r);
			documentPreprocessor.SetTokenizerFactory(ptbTokenizerFactory);
			foreach (IList<IHasWord> sentence in documentPreprocessor)
			{
				IList<TaggedWord> tSentence = tagger.TagSentence(sentence);
				pw.Println(SentenceUtils.ListToString(tSentence, false));
			}
			// print the adjectives in one more sentence. This shows how to get at words and tags in a tagged sentence.
			IList<IHasWord> sent = SentenceUtils.ToWordList("The", "slimy", "slug", "crawled", "over", "the", "long", ",", "green", "grass", ".");
			IList<TaggedWord> taggedSent = tagger.TagSentence(sent);
			foreach (TaggedWord tw in taggedSent)
			{
				if (tw.Tag().StartsWith("JJ"))
				{
					pw.Println(tw.Word());
				}
			}
			pw.Close();
		}
	}
}
