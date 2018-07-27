

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Allows us to verify that a wordnet connection is available without compile
	/// time errors if the package is not found.
	/// </summary>
	/// <author>Chris Cox</author>
	/// <author>Eric Yeh</author>
	public interface IWordNetConnection
	{
		// Used String arg version, instead of StringBuffer - EY 02/02/07
		bool WordNetContains(string s);
	}
}
