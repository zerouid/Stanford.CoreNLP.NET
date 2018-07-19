using Edu.Stanford.Nlp.Process;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Builds a tokenizer for English PennTreebank (release 2) trees.</summary>
	/// <remarks>
	/// Builds a tokenizer for English PennTreebank (release 2) trees.
	/// This is currently internally implemented via a java.io.StreamTokenizer.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Roger Levy</author>
	/// <version>2003/01/15</version>
	public class PennTreebankTokenizer : TokenizerAdapter
	{
		/// <summary>A StreamTokenizer for PennTreebank trees.</summary>
		private class EnglishTreebankStreamTokenizer : StreamTokenizer
		{
			/// <summary>Create a StreamTokenizer for PennTreebank trees.</summary>
			/// <remarks>
			/// Create a StreamTokenizer for PennTreebank trees.
			/// This sets up all the character meanings for treebank trees
			/// </remarks>
			/// <param name="r">The reader steam</param>
			private EnglishTreebankStreamTokenizer(Reader r)
				: base(r)
			{
				// start with new tokenizer syntax -- everything ordinary
				ResetSyntax();
				// treat parens as symbols themselves -- done by reset
				// ordinaryChar(')');
				// ordinaryChar('(');
				// treat chars in words as words, like a-zA-Z
				// treat all the typewriter keyboard symbols as parts of words
				// You need to look at an ASCII table to understand this!
				WordChars('!', '\'');
				// non-space non-ctrl symbols before '('
				WordChars('*', '/');
				// after ')' till before numbers
				WordChars('0', '9');
				// numbers
				WordChars(':', '@');
				// symbols between numbers, letters
				WordChars('A', 'Z');
				// uppercase letters
				WordChars('[', '`');
				// symbols between ucase and lcase
				WordChars('a', 'z');
				// lowercase letters
				WordChars('{', '~');
				// symbols before DEL
				WordChars(128, 255);
				// C.Thompson, added 11/02
				// take the normal white space charaters, including tab, return,
				// space
				WhitespaceChars(0, ' ');
			}
		}

		public PennTreebankTokenizer(Reader r)
			: base(new PennTreebankTokenizer.EnglishTreebankStreamTokenizer(r))
		{
		}
	}
}
