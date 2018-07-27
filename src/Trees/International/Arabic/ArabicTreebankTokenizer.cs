using System.IO;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>
	/// Builds a tokenizer for the Penn Arabic Treebank (ATB) using a
	/// <see cref="Java.IO.StreamTokenizer"/>
	/// .
	/// <p>
	/// This implementation is current as of the following LDC catalog numbers:
	/// LDC2008E61 (ATBp1v4), LDC2008E62 (ATBp2v3), and LDC2008E22 (ATBp3v3.1)
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	public class ArabicTreebankTokenizer : PennTreebankTokenizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreebankTokenizer));

		public ArabicTreebankTokenizer(Reader r)
			: base(r)
		{
			//Required to support comments that appear in ATB3
			st.EolIsSignificant(true);
		}

		/// <summary>Internally fetches the next token.</summary>
		/// <returns>the next token in the token stream, or null if none exists.</returns>
		protected internal override string GetNext()
		{
			try
			{
				while (true)
				{
					st.NextToken();
					int nextToken = st.ttype;
					switch (nextToken)
					{
						case StreamTokenizer.TtWord:
						{
							// ";;" are comments in ATB3
							// ":::" are also escaped for backward compatibility with the
							// old Stanford ATB pipeline
							if (st.sval.Equals(":::") || st.sval.Equals(";;"))
							{
								do
								{
									st.NextToken();
									nextToken = st.ttype;
								}
								while (nextToken != StreamTokenizer.TtEol);
								continue;
							}
							else
							{
								return st.sval;
							}
							goto case StreamTokenizer.TtNumber;
						}

						case StreamTokenizer.TtNumber:
						{
							return double.ToString(st.nval);
						}

						case StreamTokenizer.TtEol:
						{
							continue;
						}

						case StreamTokenizer.TtEof:
						{
							return null;
						}

						default:
						{
							char[] t = new char[] { (char)nextToken };
							// (array initialization)
							return new string(t);
						}
					}
				}
			}
			catch (IOException e)
			{
				System.Console.Error.Printf("%s: Unknown exception in input stream\n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			ITokenizer<string> att = new Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreebankTokenizer(new FileReader(args[0]));
			while (att.MoveNext())
			{
				System.Console.Out.Write(att.Current);
			}
		}
	}
}
