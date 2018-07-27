using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Tagger.Maxent.Documentation
{
	public class TaggerDemo
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Documentation.TaggerDemo));

		private TaggerDemo()
		{
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				log.Info("usage: java TaggerDemo modelFile fileToTag");
				return;
			}
			MaxentTagger tagger = new MaxentTagger(args[0]);
			IList<IList<IHasWord>> sentences = MaxentTagger.TokenizeText(new BufferedReader(new FileReader(args[1])));
			foreach (IList<IHasWord> sentence in sentences)
			{
				IList<TaggedWord> tSentence = tagger.TagSentence(sentence);
				System.Console.Out.WriteLine(SentenceUtils.ListToString(tSentence, false));
			}
		}
	}
}
