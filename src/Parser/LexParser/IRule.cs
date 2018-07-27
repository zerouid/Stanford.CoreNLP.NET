

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Interface for int-format grammar rules.</summary>
	/// <remarks>
	/// Interface for int-format grammar rules.
	/// This replaces the class that used to be a superclass for UnaryRule and BinaryRule.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IRule
	{
		float Score();

		int Parent();
	}
}
