using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A simple tokenizer for tokenizing Penn Chinese Treebank files.</summary>
	/// <remarks>
	/// A simple tokenizer for tokenizing Penn Chinese Treebank files.  A
	/// token is any parenthesis, node label, or terminal.  All SGML
	/// content of the files is ignored.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <version>01/17/2003</version>
	public class CHTBTokenizer : AbstractTokenizer<string>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.CHTBTokenizer));

		private readonly CHTBLexer lexer;

		/// <summary>Constructs a new tokenizer from a Reader.</summary>
		/// <remarks>
		/// Constructs a new tokenizer from a Reader.  Note that getting
		/// the bytes going into the Reader into Java-internal Unicode is
		/// not the tokenizer's job.  This can be done by converting the
		/// file with
		/// <c>ConvertEncodingThread</c>
		/// , or by specifying
		/// the files encoding explicitly in the Reader with
		/// java.io.
		/// <c>InputStreamReader</c>
		/// .
		/// </remarks>
		/// <param name="r">Reader</param>
		public CHTBTokenizer(Reader r)
		{
			lexer = new CHTBLexer(r);
		}

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>The next token in the token stream, or null if none exists.</returns>
		protected internal override string GetNext()
		{
			try
			{
				int a;
				while ((a = lexer.Yylex()) == CHTBLexer.Ignore)
				{
				}
				// log.info("#ignored: " + lexer.match());
				if (a == CHTBLexer.Yyeof)
				{
					return null;
				}
				else
				{
					//log.info("#matched: " + lexer.match());
					return lexer.Match();
				}
			}
			catch (IOException)
			{
			}
			// do nothing, return null
			return null;
		}

		/// <summary>
		/// The main() method tokenizes a file in the specified Encoding
		/// and prints it to standard output in the specified Encoding.
		/// </summary>
		/// <remarks>
		/// The main() method tokenizes a file in the specified Encoding
		/// and prints it to standard output in the specified Encoding.
		/// Its arguments are (Infile, Encoding).
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				log.Error("Usage: CHTBTokenizer inputFile encoding");
			}
			string encoding = args[1];
			Reader @in = IOUtils.ReaderFromString(args[0], encoding);
			for (ITokenizer<string> st = new Edu.Stanford.Nlp.Trees.International.Pennchinese.CHTBTokenizer(@in); st.MoveNext(); )
			{
				string s = st.Current;
				EncodingPrintWriter.Out.Println(s, encoding);
			}
		}
		// EncodingPrintWriter.out.println("|" + s + "| (" + s.length() + ")",
		//				encoding);
	}
}
