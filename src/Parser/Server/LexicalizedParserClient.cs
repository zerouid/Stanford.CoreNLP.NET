using System;
using System.IO;
using Edu.Stanford.Nlp.Trees;





namespace Edu.Stanford.Nlp.Parser.Server
{
	/// <summary>The sister class to LexicalizedParserServer.</summary>
	/// <remarks>
	/// The sister class to LexicalizedParserServer.  This class connects
	/// to the given host and port.  It can then either return a Tree or a
	/// string with the output of the Tree, depending on the method called.
	/// getParse gets the string output, getTree returns a Tree.
	/// </remarks>
	public class LexicalizedParserClient
	{
		internal readonly string host;

		internal readonly int port;

		/// <exception cref="System.IO.IOException"/>
		public LexicalizedParserClient(string host, int port)
		{
			this.host = host;
			this.port = port;
		}

		/// <summary>Reads a text result from the given socket</summary>
		/// <exception cref="System.IO.IOException"/>
		private static string ReadResult(Socket socket)
		{
			BufferedReader reader = new BufferedReader(new InputStreamReader(socket.GetInputStream(), "utf-8"));
			StringBuilder result = new StringBuilder();
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (result.Length > 0)
				{
					result.Append("\n");
				}
				result.Append(line);
			}
			return result.ToString();
		}

		/// <summary>
		/// Tokenize the text according to the parser's tokenizer,
		/// return it as whitespace tokenized text.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual string GetTokenizedText(string query)
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("tokenize " + query + "\n");
			@out.Flush();
			string result = ReadResult(socket);
			socket.Close();
			return result;
		}

		/// <summary>
		/// Get the lemmas for the text according to the parser's lemmatizer
		/// (only applies to English), return it as whitespace tokenized text.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual string GetLemmas(string query)
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("lemma " + query + "\n");
			@out.Flush();
			string result = ReadResult(socket);
			socket.Close();
			return result;
		}

		/// <summary>Returns the String output of the dependencies.</summary>
		/// <remarks>
		/// Returns the String output of the dependencies.
		/// <br />
		/// TODO: use some form of Mode enum (such as the one in SemanticGraphFactory)
		/// instead of a String
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual string GetDependencies(string query, string mode)
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("dependencies:" + mode + " " + query + "\n");
			@out.Flush();
			string result = ReadResult(socket);
			socket.Close();
			return result;
		}

		/// <summary>Returns the String output of the parse of the given query.</summary>
		/// <remarks>
		/// Returns the String output of the parse of the given query.
		/// <br />
		/// The "parse" method in the server is mostly useful for clients
		/// using a language other than Java who don't want to import or wrap
		/// Tree in any way.  However, it is useful to provide getParse to
		/// test that functionality in the server.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual string GetParse(string query, bool binarized)
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("parse" + (binarized ? ":binarized " : " ") + query + "\n");
			@out.Flush();
			string result = ReadResult(socket);
			socket.Close();
			return result;
		}

		/// <summary>Returs a Tree from the server connected to at host:port.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Tree GetTree(string query)
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("tree " + query + "\n");
			@out.Flush();
			ObjectInputStream ois = new ObjectInputStream(socket.GetInputStream());
			object o;
			try
			{
				o = ois.ReadObject();
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
			if (!(o is Tree))
			{
				throw new ArgumentException("Expected a tree");
			}
			Tree tree = (Tree)o;
			socket.Close();
			return tree;
		}

		/// <summary>Tell the server to exit</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void SendQuit()
		{
			Socket socket = new Socket(host, port);
			TextWriter @out = new OutputStreamWriter(socket.GetOutputStream(), "utf-8");
			@out.Write("quit\n");
			@out.Flush();
			socket.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Runtime.SetOut(new TextWriter(System.Console.Out, true, "utf-8"));
			Runtime.SetErr(new TextWriter(System.Console.Error, true, "utf-8"));
			Edu.Stanford.Nlp.Parser.Server.LexicalizedParserClient client = new Edu.Stanford.Nlp.Parser.Server.LexicalizedParserClient("localhost", LexicalizedParserServer.DefaultPort);
			string query = "John Bauer works at Stanford.";
			System.Console.Out.WriteLine(query);
			Tree tree = client.GetTree(query);
			System.Console.Out.WriteLine(tree);
			string results = client.GetParse(query, false);
			System.Console.Out.WriteLine(results);
		}
	}
}
