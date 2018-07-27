using System;
using System.IO;






namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 10/30/14.</summary>
	public class TextAnnotationPatternsInterface
	{
		private ServerSocket server;

		/// <exception cref="System.IO.IOException"/>
		public TextAnnotationPatternsInterface(int portnum)
		{
			server = new ServerSocket(portnum);
		}

		[System.Serializable]
		public sealed class Actions
		{
			public static readonly TextAnnotationPatternsInterface.Actions Newphrases = new TextAnnotationPatternsInterface.Actions("adds new phrases, that is, phrase X is of label l");

			public static readonly TextAnnotationPatternsInterface.Actions Removephrases = new TextAnnotationPatternsInterface.Actions("removes phrases");

			public static readonly TextAnnotationPatternsInterface.Actions Newannotations = new TextAnnotationPatternsInterface.Actions("adds new annotations, that is, when is the feedback is token x, y, z of sentence w are label l");

			public static readonly TextAnnotationPatternsInterface.Actions Processfile = new TextAnnotationPatternsInterface.Actions("the first command to run to process the sentences and write back the tokenized/labeled file");

			public static readonly TextAnnotationPatternsInterface.Actions Removeannotations = new TextAnnotationPatternsInterface.Actions("opposite of NEWANNOTATIONS");

			public static readonly TextAnnotationPatternsInterface.Actions Suggest = new TextAnnotationPatternsInterface.Actions("ask for suggestions. Runs GetPatternsFromDataMultiClass");

			public static readonly TextAnnotationPatternsInterface.Actions Matchedtokensbyall = new TextAnnotationPatternsInterface.Actions("Sentence and token ids (starting at 0) matched by all the phrases");

			public static readonly TextAnnotationPatternsInterface.Actions Matchedtokensbyphrase = new TextAnnotationPatternsInterface.Actions("Sentence and token ids (starting at 0) matched by the given phrase");

			public static readonly TextAnnotationPatternsInterface.Actions Allannotations = new TextAnnotationPatternsInterface.Actions("If a token is labeled, it's label. returns for each sentence id, labeled_tokenid -> label. Only for tokens that are labeled."
				);

			public static readonly TextAnnotationPatternsInterface.Actions Annotationsbysent = new TextAnnotationPatternsInterface.Actions("For the given sentence, the labeled token ids and their corresponding labels");

			public static readonly TextAnnotationPatternsInterface.Actions Summary = new TextAnnotationPatternsInterface.Actions("Phrases that have been labeled by humans");

			public static readonly TextAnnotationPatternsInterface.Actions None = new TextAnnotationPatternsInterface.Actions("Nothing happens");

			public static readonly TextAnnotationPatternsInterface.Actions Close = new TextAnnotationPatternsInterface.Actions("Close the socket");

			internal string whatitdoes;

			internal Actions(string whatitdoes)
			{
				//Commands that change the model
				//Commands that ask for an answer
				//Miscellaneous
				this.whatitdoes = whatitdoes;
			}
		}

		/// <summary>
		/// A private thread to handle capitalization requests on a particular
		/// socket.
		/// </summary>
		/// <remarks>
		/// A private thread to handle capitalization requests on a particular
		/// socket.  The client terminates the dialogue by sending a single line
		/// containing only a period.
		/// </remarks>
		private class PerformActionUpdateModel : Thread
		{
			private Socket socket;

			private int clientNumber;

			internal TextAnnotationPatterns annotate;

			/// <exception cref="System.IO.IOException"/>
			public PerformActionUpdateModel(Socket socket, int clientNumber)
			{
				this.socket = socket;
				this.clientNumber = clientNumber;
				this.annotate = new TextAnnotationPatterns();
				Log("New connection with client# " + clientNumber + " at " + socket);
			}

			/// <summary>
			/// Services this thread's client by first sending the
			/// client a welcome message then repeatedly reading strings
			/// and sending back the capitalized version of the string.
			/// </summary>
			public override void Run()
			{
				PrintWriter @out = null;
				string msg = string.Empty;
				// Decorate the streams so we can send characters
				// and not just bytes.  Ensure output is flushed
				// after every newline.
				BufferedReader @in = null;
				try
				{
					@in = new BufferedReader(new InputStreamReader(socket.GetInputStream()));
					@out = new PrintWriter(socket.GetOutputStream(), true);
				}
				catch (IOException e)
				{
					try
					{
						socket.Close();
					}
					catch (IOException e1)
					{
						Sharpen.Runtime.PrintStackTrace(e1);
					}
					Sharpen.Runtime.PrintStackTrace(e);
				}
				// Send a welcome message to the client.
				@out.Println("The possible actions are " + Arrays.ToString(TextAnnotationPatternsInterface.Actions.Values()) + ".Enter a line with only a period to quit");
				TextAnnotationPatternsInterface.Actions nextlineAction = TextAnnotationPatternsInterface.Actions.None;
				// Get messages from the client, line by line; return them
				// capitalized
				while (true)
				{
					try
					{
						string line = @in.ReadLine();
						if (line == null || line.Equals("."))
						{
							break;
						}
						string[] toks = line.Split("###");
						try
						{
							nextlineAction = TextAnnotationPatternsInterface.Actions.ValueOf(toks[0].Trim());
						}
						catch (ArgumentException)
						{
							System.Console.Out.WriteLine("read " + toks[0] + " and cannot understand");
							msg = "Did not understand " + toks[0] + ". POSSIBLE ACTIONS ARE: " + Arrays.ToString(TextAnnotationPatternsInterface.Actions.Values());
						}
						string input = toks.Length == 2 ? toks[1] : null;
						switch (nextlineAction)
						{
							case TextAnnotationPatternsInterface.Actions.Newphrases:
							{
								msg = annotate.DoNewPhrases(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Removephrases:
							{
								msg = annotate.DoRemovePhrases(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Newannotations:
							{
								msg = annotate.DoNewAnnotations(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Processfile:
							{
								annotate.SetUpProperties(input, true, true, null);
								msg = annotate.ProcessText(true);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Removeannotations:
							{
								msg = annotate.DoRemoveAnnotations(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Suggest:
							{
								msg = annotate.SuggestPhrases();
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Matchedtokensbyall:
							{
								msg = annotate.GetMatchedTokensByAllPhrases();
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Matchedtokensbyphrase:
							{
								msg = annotate.GetMatchedTokensByPhrase(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Allannotations:
							{
								msg = annotate.GetAllAnnotations();
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Annotationsbysent:
							{
								msg = annotate.GetAllAnnotations(input);
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Summary:
							{
								msg = annotate.CurrentSummary();
								break;
							}

							case TextAnnotationPatternsInterface.Actions.None:
							{
								break;
							}

							case TextAnnotationPatternsInterface.Actions.Close:
							{
								msg = "bye!";
								break;
							}
						}
						System.Console.Out.WriteLine("sending msg " + msg);
					}
					catch (Exception e)
					{
						msg = "ERROR " + e.ToString().ReplaceAll("\n", "\t") + ". REDO.";
						nextlineAction = TextAnnotationPatternsInterface.Actions.None;
						Log("Error handling client# " + clientNumber);
						Sharpen.Runtime.PrintStackTrace(e);
					}
					finally
					{
						@out.Println(msg);
					}
				}
			}

			/// <summary>Logs a simple message.</summary>
			/// <remarks>
			/// Logs a simple message.  In this case we just write the
			/// message to the server applications standard output.
			/// </remarks>
			private static void Log(string message)
			{
				System.Console.Out.WriteLine(message);
			}
		}

		/// <summary>
		/// Application method to run the server runs in an infinite loop
		/// listening on port 9898.
		/// </summary>
		/// <remarks>
		/// Application method to run the server runs in an infinite loop
		/// listening on port 9898.  When a connection is requested, it
		/// spawns a new thread to do the servicing and immediately returns
		/// to listening.  The server keeps a unique client number for each
		/// client that connects just to show interesting logging
		/// messages.  It is certainly not necessary to do this.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			System.Console.Out.WriteLine("The modeling server is running.");
			int clientNumber = 0;
			ServerSocket listener = new ServerSocket(9898);
			try
			{
				while (true)
				{
					new TextAnnotationPatternsInterface.PerformActionUpdateModel(listener.Accept(), clientNumber++).Start();
				}
			}
			finally
			{
				listener.Close();
			}
		}
	}
}
