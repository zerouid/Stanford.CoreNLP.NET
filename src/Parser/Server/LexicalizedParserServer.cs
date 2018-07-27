using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Server
{
	/// <summary>Serves requests to the given parser model on the given port.</summary>
	/// <remarks>
	/// Serves requests to the given parser model on the given port.
	/// See processRequest for a description of the query formats that are
	/// handled.
	/// </remarks>
	public class LexicalizedParserServer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Server.LexicalizedParserServer));

		internal readonly int port;

		internal readonly ServerSocket serverSocket;

		internal readonly ParserGrammar parser;

		internal readonly TreeBinarizer binarizer;

		internal bool stillRunning = true;

		/// <exception cref="System.IO.IOException"/>
		public LexicalizedParserServer(int port, string parserModel)
			: this(port, LoadModel(parserModel, null))
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public LexicalizedParserServer(int port, string parserModel, string taggerModel)
			: this(port, LoadModel(parserModel, taggerModel))
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public LexicalizedParserServer(int port, ParserGrammar parser)
		{
			//static final Charset utf8Charset = Charset.forName("utf-8");
			this.port = port;
			this.serverSocket = new ServerSocket(port);
			this.parser = parser;
			this.binarizer = TreeBinarizer.SimpleTreeBinarizer(parser.GetTLPParams().HeadFinder(), parser.TreebankLanguagePack());
		}

		private static ParserGrammar LoadModel(string parserModel, string taggerModel)
		{
			ParserGrammar model;
			if (taggerModel == null)
			{
				model = ParserGrammar.LoadModel(parserModel);
			}
			else
			{
				model = ParserGrammar.LoadModel(parserModel, "-preTag", "-taggerSerializedFile", taggerModel);
				// preload tagger so the first query doesn't take forever
				model.LoadTagger();
			}
			model.SetOptionFlags(model.DefaultCoreNLPFlags());
			return model;
		}

		/// <summary>
		/// Runs in a loop, getting requests from new clients until a client
		/// tells us to exit.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Listen()
		{
			while (stillRunning)
			{
				Socket clientSocket = null;
				try
				{
					clientSocket = serverSocket.Accept();
					log.Info("Got a connection");
					ProcessRequest(clientSocket);
					log.Info("Goodbye!");
					log.Info();
				}
				catch (IOException e)
				{
					// accidental multiple closes don't seem to have any bad effect
					clientSocket.Close();
					log.Info(e);
					continue;
				}
			}
			serverSocket.Close();
		}

		// TODO: handle multiple requests in one connection?  why not?
		/// <summary>
		/// Possible commands are of the form: <br />
		/// quit <br />
		/// parse query: returns a String of the parsed query <br />
		/// tree query: returns a serialized Tree of the parsed query <br />
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ProcessRequest(Socket clientSocket)
		{
			BufferedReader reader = new BufferedReader(new InputStreamReader(clientSocket.GetInputStream(), "utf-8"));
			string line = reader.ReadLine();
			log.Info(line);
			if (line == null)
			{
				return;
			}
			line = line.Trim();
			string[] pieces = line.Split(" ", 2);
			string[] commandPieces = pieces[0].Split(":", 2);
			string command = commandPieces[0];
			string commandArgs = string.Empty;
			if (commandPieces.Length > 1)
			{
				commandArgs = commandPieces[1];
			}
			string arg = null;
			if (pieces.Length > 1)
			{
				arg = pieces[1];
			}
			log.Info("Got the command " + command);
			if (arg != null)
			{
				log.Info(" ... with argument " + arg);
			}
			switch (command)
			{
				case "quit":
				{
					HandleQuit();
					break;
				}

				case "parse":
				{
					HandleParse(arg, clientSocket.GetOutputStream(), commandArgs.Equals("binarized"));
					break;
				}

				case "dependencies":
				{
					HandleDependencies(arg, clientSocket.GetOutputStream(), commandArgs);
					break;
				}

				case "tree":
				{
					HandleTree(arg, clientSocket.GetOutputStream());
					break;
				}

				case "tokenize":
				{
					HandleTokenize(arg, clientSocket.GetOutputStream());
					break;
				}

				case "lemma":
				{
					HandleLemma(arg, clientSocket.GetOutputStream());
					break;
				}
			}
			log.Info("Handled request");
			clientSocket.Close();
		}

		/// <summary>Tells the server to exit.</summary>
		public virtual void HandleQuit()
		{
			stillRunning = false;
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void HandleTokenize(string arg, OutputStream outStream)
		{
			if (arg == null)
			{
				return;
			}
			IList<IHasWord> tokens = parser.Tokenize(arg);
			OutputStreamWriter osw = new OutputStreamWriter(outStream, "utf-8");
			for (int i = 0; i < tokens.Count; ++i)
			{
				IHasWord word = tokens[i];
				if (i > 0)
				{
					osw.Write(" ");
				}
				osw.Write(word.ToString());
			}
			osw.Write("\n");
			osw.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void HandleLemma(string arg, OutputStream outStream)
		{
			if (arg == null)
			{
				return;
			}
			IList<CoreLabel> tokens = parser.Lemmatize(arg);
			OutputStreamWriter osw = new OutputStreamWriter(outStream, "utf-8");
			for (int i = 0; i < tokens.Count; ++i)
			{
				CoreLabel word = tokens[i];
				if (i > 0)
				{
					osw.Write(" ");
				}
				osw.Write(word.Lemma());
			}
			osw.Write("\n");
			osw.Flush();
		}

		// TODO: when this method throws an exception (for whatever reason)
		// a waiting client might hang.  There should be some graceful
		// handling of that.
		/// <exception cref="System.IO.IOException"/>
		public virtual void HandleDependencies(string arg, OutputStream outStream, string commandArgs)
		{
			Tree tree = Parse(arg, false);
			if (tree == null)
			{
				return;
			}
			// TODO: this might throw an exception if the parser doesn't support dependencies.  Handle that cleaner?
			GrammaticalStructure gs = parser.GetTLPParams().GetGrammaticalStructure(tree, parser.TreebankLanguagePack().PunctuationWordRejectFilter(), parser.GetTLPParams().TypedDependencyHeadFinder());
			ICollection<TypedDependency> deps = null;
			switch (commandArgs.ToUpper())
			{
				case "COLLAPSED_TREE":
				{
					deps = gs.TypedDependenciesCollapsedTree();
					break;
				}

				default:
				{
					throw new NotSupportedException("Dependencies type not implemented: " + commandArgs);
				}
			}
			OutputStreamWriter osw = new OutputStreamWriter(outStream, "utf-8");
			foreach (TypedDependency dep in deps)
			{
				osw.Write(dep.ToString());
				osw.Write("\n");
			}
			osw.Flush();
		}

		/// <summary>Returns the result of applying the parser to arg as a serialized tree.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void HandleTree(string arg, OutputStream outStream)
		{
			Tree tree = Parse(arg, false);
			if (tree == null)
			{
				return;
			}
			log.Info(tree);
			if (tree != null)
			{
				ObjectOutputStream oos = new ObjectOutputStream(outStream);
				oos.WriteObject(tree);
				oos.Flush();
			}
		}

		/// <summary>Returns the result of applying the parser to arg as a string.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void HandleParse(string arg, OutputStream outStream, bool binarized)
		{
			Tree tree = Parse(arg, binarized);
			if (tree == null)
			{
				return;
			}
			log.Info(tree);
			if (tree != null)
			{
				OutputStreamWriter osw = new OutputStreamWriter(outStream, "utf-8");
				osw.Write(tree.ToString());
				osw.Write("\n");
				osw.Flush();
			}
		}

		private Tree Parse(string arg, bool binarized)
		{
			if (arg == null)
			{
				return null;
			}
			Tree tree = parser.Parse(arg);
			if (binarized)
			{
				tree = binarizer.TransformTree(tree);
			}
			return tree;
		}

		private static void Help()
		{
			log.Info("-help:   display this message");
			log.Info("-model:  load this parser (default englishPCFG.ser.gz)");
			log.Info("-tagger: pretag with this tagger model");
			log.Info("-port:   run on this port (default 4466)");
		}

		internal const int DefaultPort = 4466;

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Runtime.SetOut(new TextWriter(System.Console.Out, true, "utf-8"));
			Runtime.SetErr(new TextWriter(System.Console.Error, true, "utf-8"));
			int port = DefaultPort;
			string model = LexicalizedParser.DefaultParserLoc;
			string tagger = null;
			// TODO: rewrite this a bit to allow for passing flags to the parser
			for (int i = 0; i < args.Length; i += 2)
			{
				if (i + 1 >= args.Length)
				{
					log.Info("Unspecified argument " + args[i]);
					System.Environment.Exit(2);
				}
				string arg = args[i];
				if (arg.StartsWith("--"))
				{
					arg = Sharpen.Runtime.Substring(arg, 2);
				}
				else
				{
					if (arg.StartsWith("-"))
					{
						arg = Sharpen.Runtime.Substring(arg, 1);
					}
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(arg, "model"))
				{
					model = args[i + 1];
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(arg, "port"))
					{
						port = System.Convert.ToInt32(args[i + 1]);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(arg, "tagger"))
						{
							tagger = args[i + 1];
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(arg, "help"))
							{
								Help();
								System.Environment.Exit(0);
							}
						}
					}
				}
			}
			Edu.Stanford.Nlp.Parser.Server.LexicalizedParserServer server = new Edu.Stanford.Nlp.Parser.Server.LexicalizedParserServer(port, model, tagger);
			log.Info("Server ready!");
			server.Listen();
		}
	}
}
