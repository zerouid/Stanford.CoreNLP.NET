using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public abstract class AbstractUnknownWordModelTrainer : IUnknownWordModelTrainer
	{
		internal double treesRead;

		internal double totalTrees;

		internal IIndex<string> wordIndex;

		internal IIndex<string> tagIndex;

		internal Options op;

		internal ILexicon lex;

		public virtual void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees)
		{
			this.totalTrees = totalTrees;
			this.treesRead = 0;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			this.op = op;
			this.lex = lex;
		}

		public void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		public void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		public void Train(Tree tree, double weight)
		{
			IncrementTreesRead(weight);
			int loc = 0;
			IList<TaggedWord> yield = tree.TaggedYield();
			foreach (TaggedWord tw in yield)
			{
				Train(tw, loc, weight);
				++loc;
			}
		}

		public virtual void IncrementTreesRead(double weight)
		{
			treesRead += weight;
		}

		public abstract IUnknownWordModel FinishTraining();

		public abstract void Train(TaggedWord arg1, int arg2, double arg3);
	}
}
