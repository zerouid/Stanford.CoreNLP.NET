using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Shiftreduce;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce.Demo
{
	/// <summary>
	/// Demonstrates how to first use the tagger, then use the
	/// ShiftReduceParser.
	/// </summary>
	/// <remarks>
	/// Demonstrates how to first use the tagger, then use the
	/// ShiftReduceParser.  Note that ShiftReduceParser will not work
	/// on untagged text.
	/// </remarks>
	/// <author>John Bauer</author>
	public class ShiftReduceDemo
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ShiftReduceDemo));

		public static void Main(string[] args)
		{
			string modelPath = "edu/stanford/nlp/models/srparser/englishSR.ser.gz";
			string taggerPath = "edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger";
			for (int argIndex = 0; argIndex < args.Length; )
			{
				switch (args[argIndex])
				{
					case "-tagger":
					{
						taggerPath = args[argIndex + 1];
						argIndex += 2;
						break;
					}

					case "-model":
					{
						modelPath = args[argIndex + 1];
						argIndex += 2;
						break;
					}

					default:
					{
						throw new Exception("Unknown argument " + args[argIndex]);
					}
				}
			}
			string text = "My dog likes to shake his stuffed chickadee toy.";
			MaxentTagger tagger = new MaxentTagger(taggerPath);
			ShiftReduceParser model = ((ShiftReduceParser)ShiftReduceParser.LoadModel(modelPath));
			DocumentPreprocessor tokenizer = new DocumentPreprocessor(new StringReader(text));
			foreach (IList<IHasWord> sentence in tokenizer)
			{
				IList<TaggedWord> tagged = tagger.TagSentence(sentence);
				Tree tree = model.Apply(tagged);
				log.Info(tree);
			}
		}
	}
}
