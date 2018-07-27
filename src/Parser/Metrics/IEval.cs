using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>
	/// An interface which can be implemented by anything that evaluates
	/// one tree at a time and then prints out a summary when done.
	/// </summary>
	/// <remarks>
	/// An interface which can be implemented by anything that evaluates
	/// one tree at a time and then prints out a summary when done.  This
	/// interface is convenient for eval types that do not want the p/r/f1
	/// tools built in to AbstractEval.
	/// <br />
	/// <seealso>edu.stanford.nlp.parser.metrics.BestOfTopKEval</seealso>
	/// for a similar
	/// data type that works on multiple trees.
	/// <br />
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IEval
	{
		void Evaluate(Tree guess, Tree gold);

		void Evaluate(Tree guess, Tree gold, PrintWriter pw);

		void Evaluate(Tree guess, Tree gold, PrintWriter pw, double weight);

		void Display(bool verbose);

		void Display(bool verbose, PrintWriter pw);
	}
}
