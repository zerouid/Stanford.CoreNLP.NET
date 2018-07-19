using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Evaluate based on the ParserQuery rather than the Tree produced</summary>
	/// <author>John Bauer</author>
	public interface IParserQueryEval
	{
		void Evaluate(IParserQuery query, Tree gold, PrintWriter pw);

		/// <summary>Called after the evaluation is finished.</summary>
		/// <remarks>
		/// Called after the evaluation is finished.  While that generally
		/// means you want to display final stats here, you can also use this
		/// as a chance to close open files, etc
		/// </remarks>
		void Display(bool verbose, PrintWriter pw);
	}
}
