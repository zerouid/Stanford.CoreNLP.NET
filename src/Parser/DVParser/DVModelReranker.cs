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
	public class DVModelReranker : IReranker
	{
		private readonly Options op;

		private readonly DVModel model;

		public DVModelReranker(DVModel model)
		{
			this.op = model.op;
			this.model = model;
		}

		internal virtual DVModel GetModel()
		{
			return model;
		}

		public virtual DVModelReranker.Query Process<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			return new DVModelReranker.Query(this);
		}

		public virtual IList<IEval> GetEvals()
		{
			IEval eval = new UnknownWordPrinter(model);
			return Java.Util.Collections.SingletonList(eval);
		}

		public class Query : IRerankerQuery
		{
			private readonly ITreeTransformer transformer;

			private readonly DVParserCostAndGradient scorer;

			private IList<DeepTree> deepTrees;

			public Query(DVModelReranker _enclosing)
			{
				this._enclosing = _enclosing;
				this.transformer = LexicalizedParser.BuildTrainTransformer(this._enclosing.op);
				this.scorer = new DVParserCostAndGradient(null, null, this._enclosing.model, this._enclosing.op);
				this.deepTrees = Generics.NewArrayList();
			}

			public virtual double Score(Tree tree)
			{
				IdentityHashMap<Tree, SimpleMatrix> nodeVectors = Generics.NewIdentityHashMap();
				Tree transformedTree = this.transformer.TransformTree(tree);
				if (this._enclosing.op.trainOptions.useContextWords)
				{
					Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(transformedTree);
					transformedTree.SetSpans();
				}
				double score = this.scorer.Score(transformedTree, nodeVectors);
				this.deepTrees.Add(new DeepTree(tree, nodeVectors, score));
				return score;
			}

			public virtual IList<DeepTree> GetDeepTrees()
			{
				return this.deepTrees;
			}

			private readonly DVModelReranker _enclosing;
		}

		private const long serialVersionUID = 7897546308624261207L;
	}
}
