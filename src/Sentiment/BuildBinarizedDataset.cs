using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sentiment
{
	/// <author>John Bauer</author>
	/// <author>Richard Socher</author>
	public class BuildBinarizedDataset
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.BuildBinarizedDataset));

		private BuildBinarizedDataset()
		{
		}

		// static methods only
		/// <summary>Sets all of the labels on a tree to the given default value.</summary>
		public static void SetUnknownLabels(Tree tree, int defaultLabel)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				SetUnknownLabels(child, defaultLabel);
			}
			tree.Label().SetValue(defaultLabel.ToString());
		}

		public static void SetPredictedLabels(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				SetPredictedLabels(child);
			}
			tree.Label().SetValue(int.ToString(RNNCoreAnnotations.GetPredictedClass(tree)));
		}

		public static void ExtractLabels(IDictionary<Pair<int, int>, string> spanToLabels, IList<IHasWord> tokens, string line)
		{
			string[] pieces = line.Trim().Split("\\s+");
			if (pieces.Length == 0)
			{
				return;
			}
			if (pieces.Length == 1)
			{
				string error = "Found line with label " + line + " but no tokens to associate with that line";
				throw new Exception(error);
			}
			//TODO: BUG: The pieces are tokenized differently than the splitting, e.g., on possessive markers as in "actors' expenses"
			for (int i = 0; i < tokens.Count - pieces.Length + 2; ++i)
			{
				bool found = true;
				for (int j = 1; j < pieces.Length; ++j)
				{
					if (!tokens[i + j - 1].Word().Equals(pieces[j]))
					{
						found = false;
						break;
					}
				}
				if (found)
				{
					spanToLabels[new Pair<int, int>(i, i + pieces.Length - 1)] = pieces[0];
				}
			}
		}

		public static bool SetSpanLabel(Tree tree, Pair<int, int> span, string value)
		{
			if (!(tree.Label() is CoreLabel))
			{
				throw new AssertionError("Expected CoreLabels");
			}
			CoreLabel label = (CoreLabel)tree.Label();
			if (label.Get(typeof(CoreAnnotations.BeginIndexAnnotation)).Equals(span.first) && label.Get(typeof(CoreAnnotations.EndIndexAnnotation)).Equals(span.second))
			{
				label.SetValue(value);
				return true;
			}
			if (label.Get(typeof(CoreAnnotations.BeginIndexAnnotation)) > span.first && label.Get(typeof(CoreAnnotations.EndIndexAnnotation)) < span.second)
			{
				return false;
			}
			foreach (Tree child in tree.Children())
			{
				if (SetSpanLabel(child, span, value))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Turns a text file into trees for use in a RNTN classifier such as
		/// the treebank used in the Sentiment project.
		/// </summary>
		/// <remarks>
		/// Turns a text file into trees for use in a RNTN classifier such as
		/// the treebank used in the Sentiment project.
		/// <br />
		/// The expected input file is one sentence per line, with sentences
		/// separated by blank lines. The first line has the main label of the sentence together with the full sentence.
		/// Lines after the first sentence line but before
		/// the blank line will be treated as labeled sub-phrases.  The
		/// labels should start with the label and then contain a list of
		/// tokens the label applies to. All phrases that do not have their own label will take on the main sentence label!
		/// For example:
		/// <br />
		/// <code>
		/// 1 Today is not a good day.<br />
		/// 3 good<br />
		/// 3 good day <br />
		/// 3 a good day <br />
		/// <br />
		/// (next block starts here) <br />
		/// </code>
		/// By default the englishPCFG parser is used.  This can be changed
		/// with the
		/// <c>-parserModel</c>
		/// flag.  Specify an input file
		/// with
		/// <c>-input</c>
		/// .
		/// <br />
		/// If a sentiment model is provided with -sentimentModel, that model
		/// will be used to prelabel the sentences.  Any spans with given
		/// labels will then be used to adjust those labels.
		/// </remarks>
		public static void Main(string[] args)
		{
			CollapseUnaryTransformer transformer = new CollapseUnaryTransformer();
			string parserModel = "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";
			string inputPath = null;
			string sentimentModelPath = null;
			SentimentModel sentimentModel = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
				{
					inputPath = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parserModel"))
					{
						parserModel = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-sentimentModel"))
						{
							sentimentModelPath = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							log.Info("Unknown argument " + args[argIndex]);
							System.Environment.Exit(2);
						}
					}
				}
			}
			if (inputPath == null)
			{
				throw new ArgumentException("Must specify input file with -input");
			}
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(parserModel));
			TreeBinarizer binarizer = TreeBinarizer.SimpleTreeBinarizer(parser.GetTLPParams().HeadFinder(), parser.TreebankLanguagePack());
			if (sentimentModelPath != null)
			{
				sentimentModel = SentimentModel.LoadSerialized(sentimentModelPath);
			}
			string text = IOUtils.SlurpFileNoExceptions(inputPath);
			string[] chunks = text.Split("\\n\\s*\\n+");
			// need blank line to make a new chunk
			foreach (string chunk in chunks)
			{
				if (chunk.Trim().IsEmpty())
				{
					continue;
				}
				// The expected format is that line 0 will be the text of the
				// sentence, and each subsequence line, if any, will be a value
				// followed by the sequence of tokens that get that value.
				// Here we take the first line and tokenize it as one sentence.
				string[] lines = chunk.Trim().Split("\\n");
				string sentence = lines[0];
				StringReader sin = new StringReader(sentence);
				DocumentPreprocessor document = new DocumentPreprocessor(sin);
				document.SetSentenceFinalPuncWords(new string[] { "\n" });
				IList<IHasWord> tokens = document.GetEnumerator().Current;
				int mainLabel = System.Convert.ToInt32(tokens[0].Word());
				//System.out.print("Main Sentence Label: " + mainLabel.toString() + "; ");
				tokens = tokens.SubList(1, tokens.Count);
				//log.info(tokens);
				IDictionary<Pair<int, int>, string> spanToLabels = Generics.NewHashMap();
				for (int i = 1; i < lines.Length; ++i)
				{
					ExtractLabels(spanToLabels, tokens, lines[i]);
				}
				// TODO: add an option which treats the spans as constraints when parsing
				Tree tree = parser.Apply(tokens);
				Tree binarized = binarizer.TransformTree(tree);
				Tree collapsedUnary = transformer.TransformTree(binarized);
				// if there is a sentiment model for use in prelabeling, we
				// label here and then use the user given labels to adjust
				if (sentimentModel != null)
				{
					Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(collapsedUnary);
					SentimentCostAndGradient scorer = new SentimentCostAndGradient(sentimentModel, null);
					scorer.ForwardPropagateTree(collapsedUnary);
					SetPredictedLabels(collapsedUnary);
				}
				else
				{
					SetUnknownLabels(collapsedUnary, mainLabel);
				}
				Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(collapsedUnary);
				collapsedUnary.IndexSpans();
				foreach (KeyValuePair<Pair<int, int>, string> pairStringEntry in spanToLabels)
				{
					SetSpanLabel(collapsedUnary, pairStringEntry.Key, pairStringEntry.Value);
				}
				System.Console.Out.WriteLine(collapsedUnary);
			}
		}
		//System.out.println();
		// end main
	}
}
