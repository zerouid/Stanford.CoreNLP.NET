using Edu.Stanford.Nlp.Process;



namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <summary>
	/// Produces a tokenizer for the NEGRA corpus in context-free Penn
	/// Treebank format.
	/// </summary>
	/// <author>Roger Levy</author>
	public class NegraPennTokenizer : LexerTokenizer
	{
		public NegraPennTokenizer(Reader r)
			: base(new NegraPennLexer(r))
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Reader @in = new FileReader(args[0]);
			ITokenizer st = new Edu.Stanford.Nlp.Trees.International.Negra.NegraPennTokenizer(@in);
			while (st.MoveNext())
			{
				string s = (string)st.Current;
				System.Console.Out.WriteLine(s);
			}
		}
	}
}
