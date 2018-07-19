using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <summary>Generic interface for mapping one string to another given some contextual evidence.</summary>
	/// <author>Spence Green</author>
	public interface IMapper
	{
		/// <summary>Perform initialization prior to the first call to <code>map</code>.</summary>
		/// <param name="path">A filename for data on disk used during mapping</param>
		/// <param name="options">
		/// Variable length array of strings for options. Option format may
		/// vary for the particular class instance.
		/// </param>
		void Setup(File path, params string[] options);

		/// <summary>Maps from one string representation to another.</summary>
		/// <param name="parent"><code>element</code>'s context (e.g., the parent node in a parse tree)</param>
		/// <param name="element">The string to be transformed.</param>
		/// <returns>The transformed string</returns>
		string Map(string parent, string element);

		/// <summary>Indicates whether <code>child</code> can be converted to another encoding.</summary>
		/// <remarks>
		/// Indicates whether <code>child</code> can be converted to another encoding. In the ATB, for example,
		/// if a punctuation character is labeled with the "PUNC" POS tag, then that character should not
		/// be converted from Buckwalter to UTF-8.
		/// </remarks>
		/// <param name="parent"><code>element</code>'s context (e.g., the parent node in a parse tree)</param>
		/// <param name="child">The string to be transformed.</param>
		/// <returns>True if the string encoding can be changed. False otherwise.</returns>
		bool CanChangeEncoding(string parent, string child);
	}
}
