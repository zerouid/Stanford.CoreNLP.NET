using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// Interface for rules/patterns for transforming
	/// time related natural language expressions
	/// into temporal representations.
	/// </summary>
	/// <remarks>
	/// Interface for rules/patterns for transforming
	/// time related natural language expressions
	/// into temporal representations.
	/// Patterns use the TokensRegex language.
	/// </remarks>
	/// <author>Angel Chang</author>
	public interface ITimeExpressionPatterns
	{
		/// <summary>
		/// Creates a CoreMapExpressionExtractor that knows how
		/// to extract time related expressions from text into CoreMaps
		/// </summary>
		/// <returns>CoreMapExpressionExtractor</returns>
		CoreMapExpressionExtractor CreateExtractor();

		/// <summary>
		/// Determine how date/times should be resolved for the given temporal
		/// expression and its context
		/// </summary>
		/// <param name="annotation">Annotation from which the temporal express was extracted (context)</param>
		/// <param name="te">Temporal expression</param>
		/// <returns>flag indicating what resolution scheme to use</returns>
		int DetermineRelFlags(ICoreMap annotation, TimeExpression te);
	}
}
