using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>
	/// Escapes an Arabic string by replacing ATB reserved words with the appropriate
	/// escape sequences.
	/// </summary>
	/// <remarks>
	/// Escapes an Arabic string by replacing ATB reserved words with the appropriate
	/// escape sequences. This class is appropriate for use in
	/// <see cref="Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParser"/>
	/// using the <code>-escaper</code> command-line parameter.
	/// </remarks>
	/// <author>Spence Green</author>
	public class ATBEscaper : Func<IList<IHasWord>, IList<IHasWord>>
	{
		public virtual IList<IHasWord> Apply(IList<IHasWord> @in)
		{
			IList<IHasWord> escaped = new List<IHasWord>(@in);
			foreach (IHasWord word in escaped)
			{
				word.SetWord(ATBTreeUtils.Escape(word.Word()));
			}
			return escaped;
		}
	}
}
