using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>
	/// Applies an AbstractEval to a list of trees to pick the best tree
	/// using F1 measure.
	/// </summary>
	/// <remarks>
	/// Applies an AbstractEval to a list of trees to pick the best tree
	/// using F1 measure.  Then uses a second AbstractEval to tally
	/// statistics for the best tree chosen.  This is useful for
	/// experiments to see how much the parser could improve if you were
	/// able to correctly order the top N trees.
	/// <br />
	/// The comparisonEval will not have any useful statistics, as it will
	/// tested against the top N trees for each parsing.  The countingEval
	/// is the useful AbstractEval, as it is tallied only once per parse.
	/// <br />
	/// One example of this is the pcfgTopK eval, which looks for the best
	/// LP/LR of constituents in the top K trees.
	/// </remarks>
	/// <author>John Bauer</author>
	public class BestOfTopKEval
	{
		private readonly AbstractEval comparisonEval;

		private readonly AbstractEval countingEval;

		public BestOfTopKEval(AbstractEval comparisonEval, AbstractEval countingEval)
		{
			this.comparisonEval = comparisonEval;
			this.countingEval = countingEval;
		}

		public virtual void Evaluate(IList<Tree> guesses, Tree gold, PrintWriter pw)
		{
			double bestF1 = double.NegativeInfinity;
			Tree bestTree = null;
			foreach (Tree tree in guesses)
			{
				comparisonEval.Evaluate(tree, gold, null);
				double f1 = comparisonEval.GetLastF1();
				if (bestTree == null || f1 > bestF1)
				{
					bestTree = tree;
					bestF1 = f1;
				}
			}
			countingEval.Evaluate(bestTree, gold, pw);
		}

		public virtual void Display(bool verbose)
		{
			Display(verbose, new PrintWriter(System.Console.Out, true));
		}

		public virtual void Display(bool verbose, PrintWriter pw)
		{
			countingEval.Display(verbose, pw);
		}
	}
}
