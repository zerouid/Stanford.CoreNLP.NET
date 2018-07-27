using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>
	/// A tool which takes all of the n-grams of a certain length and looks
	/// for other n-grams which are close using distance between word vectors.
	/// </summary>
	/// <remarks>
	/// A tool which takes all of the n-grams of a certain length and looks
	/// for other n-grams which are close using distance between word vectors.
	/// Useful for coming up with interesting analysis of how the word vectors
	/// help the parsing task.
	/// </remarks>
	/// <author>John Bauer</author>
	public class FindNearestNeighbors
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(FindNearestNeighbors));

		internal const int numNeighbors = 5;

		internal const int maxLength = 8;

		public class ParseRecord
		{
			internal readonly IList<Word> sentence;

			internal readonly Tree goldTree;

			internal readonly Tree parse;

			internal readonly SimpleMatrix rootVector;

			internal readonly IdentityHashMap<Tree, SimpleMatrix> nodeVectors;

			public ParseRecord(IList<Word> sentence, Tree goldTree, Tree parse, SimpleMatrix rootVector, IdentityHashMap<Tree, SimpleMatrix> nodeVectors)
			{
				// TODO: parameter?
				this.sentence = sentence;
				this.goldTree = goldTree;
				this.parse = parse;
				this.rootVector = rootVector;
				this.nodeVectors = nodeVectors;
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			string modelPath = null;
			string outputPath = null;
			string testTreebankPath = null;
			IFileFilter testTreebankFilter = null;
			IList<string> unusedArgs = new List<string>();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					modelPath = args[argIndex + 1];
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
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
						{
							outputPath = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							unusedArgs.Add(args[argIndex++]);
						}
					}
				}
			}
			if (modelPath == null)
			{
				throw new ArgumentException("Need to specify -model");
			}
			if (testTreebankPath == null)
			{
				throw new ArgumentException("Need to specify -testTreebank");
			}
			if (outputPath == null)
			{
				throw new ArgumentException("Need to specify -output");
			}
			string[] newArgs = Sharpen.Collections.ToArray(unusedArgs, new string[unusedArgs.Count]);
			LexicalizedParser lexparser = ((LexicalizedParser)LexicalizedParser.LoadModel(modelPath, newArgs));
			Treebank testTreebank = null;
			if (testTreebankPath != null)
			{
				log.Info("Reading in trees from " + testTreebankPath);
				if (testTreebankFilter != null)
				{
					log.Info("Filtering on " + testTreebankFilter);
				}
				testTreebank = lexparser.GetOp().tlpParams.MemoryTreebank();
				testTreebank.LoadPath(testTreebankPath, testTreebankFilter);
				log.Info("Read in " + testTreebank.Count + " trees for testing");
			}
			FileWriter @out = new FileWriter(outputPath);
			BufferedWriter bout = new BufferedWriter(@out);
			log.Info("Parsing " + testTreebank.Count + " trees");
			int count = 0;
			IList<FindNearestNeighbors.ParseRecord> records = Generics.NewArrayList();
			foreach (Tree goldTree in testTreebank)
			{
				IList<Word> tokens = goldTree.YieldWords();
				IParserQuery parserQuery = lexparser.ParserQuery();
				if (!parserQuery.Parse(tokens))
				{
					throw new AssertionError("Could not parse: " + tokens);
				}
				if (!(parserQuery is RerankingParserQuery))
				{
					throw new ArgumentException("Expected a LexicalizedParser with a Reranker attached");
				}
				RerankingParserQuery rpq = (RerankingParserQuery)parserQuery;
				if (!(rpq.RerankerQuery() is DVModelReranker.Query))
				{
					throw new ArgumentException("Expected a LexicalizedParser with a DVModel attached");
				}
				DeepTree tree = ((DVModelReranker.Query)rpq.RerankerQuery()).GetDeepTrees()[0];
				SimpleMatrix rootVector = null;
				foreach (KeyValuePair<Tree, SimpleMatrix> entry in tree.GetVectors())
				{
					if (entry.Key.Label().Value().Equals("ROOT"))
					{
						rootVector = entry.Value;
						break;
					}
				}
				if (rootVector == null)
				{
					throw new AssertionError("Could not find root nodevector");
				}
				@out.Write(tokens + "\n");
				@out.Write(tree.GetTree() + "\n");
				for (int i = 0; i < rootVector.GetNumElements(); ++i)
				{
					@out.Write("  " + rootVector.Get(i));
				}
				@out.Write("\n\n\n");
				count++;
				if (count % 10 == 0)
				{
					log.Info("  " + count);
				}
				records.Add(new FindNearestNeighbors.ParseRecord(tokens, goldTree, tree.GetTree(), rootVector, tree.GetVectors()));
			}
			log.Info("  done parsing");
			IList<Pair<Tree, SimpleMatrix>> subtrees = Generics.NewArrayList();
			foreach (FindNearestNeighbors.ParseRecord record in records)
			{
				foreach (KeyValuePair<Tree, SimpleMatrix> entry in record.nodeVectors)
				{
					if (entry.Key.GetLeaves().Count <= maxLength)
					{
						subtrees.Add(Pair.MakePair(entry.Key, entry.Value));
					}
				}
			}
			log.Info("There are " + subtrees.Count + " subtrees in the set of trees");
			PriorityQueue<ScoredObject<Pair<Tree, Tree>>> bestmatches = new PriorityQueue<ScoredObject<Pair<Tree, Tree>>>(101, ScoredComparator.DescendingComparator);
			for (int i_1 = 0; i_1 < subtrees.Count; ++i_1)
			{
				log.Info(subtrees[i_1].First().YieldWords());
				log.Info(subtrees[i_1].First());
				for (int j = 0; j < subtrees.Count; ++j)
				{
					if (i_1 == j)
					{
						continue;
					}
					// TODO: look at basic category?
					double normF = subtrees[i_1].Second().Minus(subtrees[j].Second()).NormF();
					bestmatches.Add(new ScoredObject<Pair<Tree, Tree>>(Pair.MakePair(subtrees[i_1].First(), subtrees[j].First()), normF));
					if (bestmatches.Count > 100)
					{
						bestmatches.Poll();
					}
				}
				IList<ScoredObject<Pair<Tree, Tree>>> ordered = Generics.NewArrayList();
				while (bestmatches.Count > 0)
				{
					ordered.Add(bestmatches.Poll());
				}
				Java.Util.Collections.Reverse(ordered);
				foreach (ScoredObject<Pair<Tree, Tree>> pair in ordered)
				{
					log.Info(" MATCHED " + pair.Object().second.YieldWords() + " ... " + pair.Object().Second() + " with a score of " + pair.Score());
				}
				log.Info();
				log.Info();
				bestmatches.Clear();
			}
			/*
			for (int i = 0; i < records.size(); ++i) {
			if (i % 10 == 0) {
			log.info("  " + i);
			}
			List<ScoredObject<ParseRecord>> scored = Generics.newArrayList();
			for (int j = 0; j < records.size(); ++j) {
			if (i == j) continue;
			
			double score = 0.0;
			int matches = 0;
			for (Map.Entry<Tree, SimpleMatrix> first : records.get(i).nodeVectors.entrySet()) {
			for (Map.Entry<Tree, SimpleMatrix> second : records.get(j).nodeVectors.entrySet()) {
			String firstBasic = dvparser.dvModel.basicCategory(first.getKey().label().value());
			String secondBasic = dvparser.dvModel.basicCategory(second.getKey().label().value());
			if (firstBasic.equals(secondBasic)) {
			++matches;
			double normF = first.getValue().minus(second.getValue()).normF();
			score += normF * normF;
			}
			}
			}
			if (matches == 0) {
			score = Double.POSITIVE_INFINITY;
			} else {
			score = score / matches;
			}
			//double score = records.get(i).vector.minus(records.get(j).vector).normF();
			scored.add(new ScoredObject<ParseRecord>(records.get(j), score));
			}
			Collections.sort(scored, ScoredComparator.ASCENDING_COMPARATOR);
			
			out.write(records.get(i).sentence.toString() + "\n");
			for (int j = 0; j < numNeighbors; ++j) {
			out.write("   " + scored.get(j).score() + ": " + scored.get(j).object().sentence + "\n");
			}
			out.write("\n\n");
			}
			log.info();
			*/
			bout.Flush();
			@out.Flush();
			@out.Close();
		}
	}
}
