using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	public class ParseAndPrintMatrices
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ParseAndPrintMatrices));

		/// <exception cref="System.IO.IOException"/>
		public static void OutputMatrix(BufferedWriter bout, SimpleMatrix matrix)
		{
			for (int i = 0; i < matrix.GetNumElements(); ++i)
			{
				bout.Write("  " + matrix.Get(i));
			}
			bout.NewLine();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void OutputTreeMatrices(BufferedWriter bout, Tree tree, IdentityHashMap<Tree, SimpleMatrix> vectors)
		{
			if (tree.IsPreTerminal() || tree.IsLeaf())
			{
				return;
			}
			for (int i = tree.Children().Length - 1; i >= 0; i--)
			{
				OutputTreeMatrices(bout, tree.Children()[i], vectors);
			}
			OutputMatrix(bout, vectors[tree]);
		}

		public static Tree FindRootTree(IdentityHashMap<Tree, SimpleMatrix> vectors)
		{
			foreach (Tree tree in vectors.Keys)
			{
				if (tree.Label().Value().Equals("ROOT"))
				{
					return tree;
				}
			}
			throw new Exception("Could not find root");
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string modelPath = null;
			string outputPath = null;
			string inputPath = null;
			string testTreebankPath = null;
			IFileFilter testTreebankFilter = null;
			IList<string> unusedArgs = Generics.NewArrayList();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					modelPath = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
					{
						outputPath = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
						{
							inputPath = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-testTreebank"))
							{
								Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-testTreebank");
								argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
								testTreebankPath = treebankDescription.First();
								testTreebankFilter = treebankDescription.Second();
							}
							else
							{
								unusedArgs.Add(args[argIndex++]);
							}
						}
					}
				}
			}
			string[] newArgs = Sharpen.Collections.ToArray(unusedArgs, new string[unusedArgs.Count]);
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(modelPath, newArgs));
			DVModel model = DVParser.GetModelFromLexicalizedParser(parser);
			File outputFile = new File(outputPath);
			FileSystem.CheckNotExistsOrFail(outputFile);
			FileSystem.MkdirOrFail(outputFile);
			int count = 0;
			if (inputPath != null)
			{
				Reader input = new BufferedReader(new FileReader(inputPath));
				DocumentPreprocessor processor = new DocumentPreprocessor(input);
				foreach (IList<IHasWord> sentence in processor)
				{
					count++;
					// index from 1
					IParserQuery pq = parser.ParserQuery();
					if (!(pq is RerankingParserQuery))
					{
						throw new ArgumentException("Expected a RerankingParserQuery");
					}
					RerankingParserQuery rpq = (RerankingParserQuery)pq;
					if (!rpq.Parse(sentence))
					{
						throw new Exception("Unparsable sentence: " + sentence);
					}
					IRerankerQuery reranker = rpq.RerankerQuery();
					if (!(reranker is DVModelReranker.Query))
					{
						throw new ArgumentException("Expected a DVModelReranker");
					}
					DeepTree deepTree = ((DVModelReranker.Query)reranker).GetDeepTrees()[0];
					IdentityHashMap<Tree, SimpleMatrix> vectors = deepTree.GetVectors();
					foreach (KeyValuePair<Tree, SimpleMatrix> entry in vectors)
					{
						log.Info(entry.Key + "   " + entry.Value);
					}
					FileWriter fout = new FileWriter(outputPath + File.separator + "sentence" + count + ".txt");
					BufferedWriter bout = new BufferedWriter(fout);
					bout.Write(SentenceUtils.ListToString(sentence));
					bout.NewLine();
					bout.Write(deepTree.GetTree().ToString());
					bout.NewLine();
					foreach (IHasWord word in sentence)
					{
						OutputMatrix(bout, model.GetWordVector(word.Word()));
					}
					Tree rootTree = FindRootTree(vectors);
					OutputTreeMatrices(bout, rootTree, vectors);
					bout.Flush();
					fout.Close();
				}
			}
		}
	}
}
