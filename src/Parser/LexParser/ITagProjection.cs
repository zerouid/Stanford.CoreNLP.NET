using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// An interface for projecting POS tags onto a reduced
	/// set for the dependency grammar.
	/// </summary>
	/// <author>Dan Klein</author>
	public interface ITagProjection
	{
		/// <summary>Project more split dependency space onto less split space.</summary>
		/// <param name="tagStr">The full name of the tag</param>
		/// <returns>A name for the  tag in a reduced tag space</returns>
		string Project(string tagStr);
	}
}
