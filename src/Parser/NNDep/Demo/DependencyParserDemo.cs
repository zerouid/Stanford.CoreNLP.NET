using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Nndep;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep.Demo
{
	/// <summary>
	/// Demonstrates how to first use the tagger, then use the NN dependency
	/// parser.
	/// </summary>
	/// <remarks>
	/// Demonstrates how to first use the tagger, then use the NN dependency
	/// parser. Note that the parser will not work on untagged text.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	public class DependencyParserDemo
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.Demo.DependencyParserDemo));

		private DependencyParserDemo()
		{
		}

		// static main method only
		public static void Main(string[] args)
		{
			string modelPath = DependencyParser.DefaultModel;
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
			string text = "I can almost always tell when movies use fake dinosaurs.";
			MaxentTagger tagger = new MaxentTagger(taggerPath);
			DependencyParser parser = DependencyParser.LoadFromModelFile(modelPath);
			DocumentPreprocessor tokenizer = new DocumentPreprocessor(new StringReader(text));
			foreach (IList<IHasWord> sentence in tokenizer)
			{
				IList<TaggedWord> tagged = tagger.TagSentence(sentence);
				GrammaticalStructure gs = parser.Predict(tagged);
				// Print typed dependencies
				log.Info(gs);
			}
		}
	}
}
