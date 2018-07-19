using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	[System.Serializable]
	public class CombinedDVModelReranker : IReranker
	{
		private readonly Options op;

		private readonly IList<DVModel> models;

		public CombinedDVModelReranker(Options op, IList<DVModel> models)
		{
			this.op = op;
			this.models = models;
		}

		public virtual CombinedDVModelReranker.Query Process<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			return new CombinedDVModelReranker.Query(this);
		}

		public virtual IList<IEval> GetEvals()
		{
			return Java.Util.Collections.EmptyList();
		}

		public class Query : IRerankerQuery
		{
			private readonly ITreeTransformer transformer;

			private readonly IList<DVParserCostAndGradient> scorers;

			public Query(CombinedDVModelReranker _enclosing)
			{
				this._enclosing = _enclosing;
				this.transformer = LexicalizedParser.BuildTrainTransformer(this._enclosing.op);
				this.scorers = Generics.NewArrayList();
				foreach (DVModel model in this._enclosing.models)
				{
					this.scorers.Add(new DVParserCostAndGradient(null, null, model, this._enclosing.op));
				}
			}

			public virtual double Score(Tree tree)
			{
				double totalScore = 0.0;
				foreach (DVParserCostAndGradient scorer in this.scorers)
				{
					IdentityHashMap<Tree, SimpleMatrix> nodeVectors = Generics.NewIdentityHashMap();
					Tree transformedTree = this.transformer.TransformTree(tree);
					if (this._enclosing.op.trainOptions.useContextWords)
					{
						Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(transformedTree);
						transformedTree.SetSpans();
					}
					double score = scorer.Score(transformedTree, nodeVectors);
					totalScore += score;
				}
				//totalScore = Math.max(totalScore, score);
				return totalScore;
			}

			private readonly CombinedDVModelReranker _enclosing;
		}
	}
}
