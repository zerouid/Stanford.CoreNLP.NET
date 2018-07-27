using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>A POS tagger server for the Stanford POS Tagger.</summary>
	/// <remarks>
	/// A POS tagger server for the Stanford POS Tagger.
	/// Runs on a socket and waits for text to tag and returns the
	/// tagged text.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class MaxentTaggerServer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.MaxentTaggerServer));

		/// <summary>Debugging toggle.</summary>
		private bool Debug = false;

		private readonly string charset;

		/// <summary>The listener socket of this server.</summary>
		private readonly ServerSocket listener;

		/// <summary>The classifier that does the actual tagging.</summary>
		private readonly MaxentTagger.TaggerWrapper tagger;

		/// <summary>Creates a new tagger server on the specified port.</summary>
		/// <param name="port">the port this NERServer listens on.</param>
		/// <param name="tagger">The classifier which will do the tagging</param>
		/// <param name="charset">The character set for encoding Strings over the socket stream, e.g., "utf-8"</param>
		/// <exception cref="System.IO.IOException">If there is a problem creating a ServerSocket</exception>
		public MaxentTaggerServer(int port, MaxentTagger.TaggerWrapper tagger, string charset)
		{
			//// Variables
			//// Constructors
			this.tagger = tagger;
			listener = new ServerSocket(port);
			this.charset = charset;
		}

		//// Public Methods
		/// <summary>Runs this tagger server.</summary>
		public virtual void Run()
		{
			if (Debug)
			{
				log.Info("Starting server loop");
			}
			Socket client = null;
			while (true)
			{
				try
				{
					client = listener.Accept();
					if (Debug)
					{
						log.Info("Accepted request from ");
						log.Info(client.GetInetAddress().GetHostName());
					}
					new MaxentTaggerServer.Session(this, client);
				}
				catch (Exception e1)
				{
					log.Info("MaxentTaggerServer: couldn't accept");
					Sharpen.Runtime.PrintStackTrace(e1, System.Console.Error);
					try
					{
						client.Close();
					}
					catch (Exception e2)
					{
						log.Info("MaxentTaggerServer: couldn't close client");
						Sharpen.Runtime.PrintStackTrace(e2, System.Console.Error);
					}
				}
			}
		}

		/// <summary>
		/// A single user session, accepting one request, processing it, and
		/// sending back the results.
		/// </summary>
		private class Session : Thread
		{
			/// <summary>The socket to the client.</summary>
			private readonly Socket client;

			/// <summary>The input stream from the client.</summary>
			private readonly BufferedReader @in;

			/// <summary>The output stream to the client.</summary>
			private PrintWriter @out;

			/// <exception cref="System.IO.IOException"/>
			private Session(MaxentTaggerServer _enclosing, Socket socket)
			{
				this._enclosing = _enclosing;
				//// Inner Classes
				//// Instance Fields
				//// Constructors
				this.client = socket;
				this.@in = new BufferedReader(new InputStreamReader(this.client.GetInputStream(), this._enclosing.charset));
				this.@out = new PrintWriter(new OutputStreamWriter(this.client.GetOutputStream(), this._enclosing.charset));
				this.Start();
			}

			//// Public Methods
			/// <summary>
			/// Runs this session by reading a string, tagging it, and writing
			/// back the result.
			/// </summary>
			/// <remarks>
			/// Runs this session by reading a string, tagging it, and writing
			/// back the result.  The input should be a single line (no embedded
			/// newlines), which represents a whole sentence or document.
			/// </remarks>
			public override void Run()
			{
				if (this._enclosing.Debug)
				{
					MaxentTaggerServer.log.Info("Created new session");
				}
				try
				{
					string input = this.@in.ReadLine();
					if (this._enclosing.Debug)
					{
						EncodingPrintWriter.Err.Println("Receiving: \"" + input + '\"', this._enclosing.charset);
					}
					if (!(input == null))
					{
						string output = this._enclosing.tagger.Apply(input);
						if (this._enclosing.Debug)
						{
							EncodingPrintWriter.Err.Println("Sending: \"" + output + '\"', this._enclosing.charset);
						}
						this.@out.Print(output);
						this.@out.Flush();
					}
					this.Close();
				}
				catch (IOException e)
				{
					MaxentTaggerServer.log.Info("MaxentTaggerServer:Session: couldn't read input or error running POS tagger");
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				catch (ArgumentNullException npe)
				{
					MaxentTaggerServer.log.Info("MaxentTaggerServer:Session: connection closed by peer");
					Sharpen.Runtime.PrintStackTrace(npe, System.Console.Error);
				}
			}

			/// <summary>Terminates this session gracefully.</summary>
			private void Close()
			{
				try
				{
					this.@in.Close();
					this.@out.Close();
					this.client.Close();
				}
				catch (Exception e)
				{
					MaxentTaggerServer.log.Info("MaxentTaggerServer:Session: can't close session");
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}

			private readonly MaxentTaggerServer _enclosing;
		}

		/// <summary>This example sends material to the tagger server one line at a time.</summary>
		/// <remarks>
		/// This example sends material to the tagger server one line at a time.
		/// Each line should be at least a whole sentence, but can be a whole
		/// document.
		/// </remarks>
		private class TaggerClient
		{
			private TaggerClient()
			{
			}

			// end class Session
			/// <exception cref="System.IO.IOException"/>
			private static void CommunicateWithMaxentTaggerServer(string host, int port, string charset)
			{
				if (host == null)
				{
					host = "localhost";
				}
				BufferedReader stdIn = new BufferedReader(new InputStreamReader(Runtime.@in, charset));
				log.Info("Input some text and press RETURN to POS tag it, or just RETURN to finish.");
				for (string userInput; (userInput = stdIn.ReadLine()) != null && !userInput.Matches("\\n?"); )
				{
					try
					{
						Socket socket = new Socket(host, port);
						PrintWriter @out = new PrintWriter(new OutputStreamWriter(socket.GetOutputStream(), charset), true);
						BufferedReader @in = new BufferedReader(new InputStreamReader(socket.GetInputStream(), charset));
						PrintWriter stdOut = new PrintWriter(new OutputStreamWriter(System.Console.Out, charset), true);
						// send material to NER to socket
						@out.Println(userInput);
						// Print the results of NER
						stdOut.Println(@in.ReadLine());
						while (@in.Ready())
						{
							stdOut.Println(@in.ReadLine());
						}
						@in.Close();
						socket.Close();
					}
					catch (UnknownHostException)
					{
						log.Info("Cannot find host: ");
						log.Info(host);
						return;
					}
					catch (IOException)
					{
						log.Info("I/O error in the connection to: ");
						log.Info(host);
						return;
					}
				}
				stdIn.Close();
			}
		}

		private const string Usage = "Usage: MaxentTaggerServer [-model file|-client] -port portNumber [other MaxentTagger options]";

		// end static class NERClient
		/// <summary>Starts this server on the specified port.</summary>
		/// <remarks>
		/// Starts this server on the specified port.  The classifier used can be
		/// either a default one stored in the jar file from which this code is
		/// invoked or you can specify it as a filename or as another classifier
		/// resource name, which must correspond to the name of a resource in the
		/// /classifiers/ directory of the jar file.
		/// <p>
		/// Usage: <code>java edu.stanford.nlp.tagger.maxent.MaxentTaggerServer [-model file|-client] -port portNumber [other MaxentTagger options]</code>
		/// </remarks>
		/// <param name="args">Command-line arguments (described above)</param>
		/// <exception cref="System.Exception">If file or Java class problems with serialized classifier</exception>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				log.Info(Usage);
				return;
			}
			// Use both Properties and TaggerConfig.  It's okay.
			Properties props = StringUtils.ArgsToProperties(args);
			string client = props.GetProperty("client");
			string portStr = props.GetProperty("port");
			if (portStr == null || portStr.Equals(string.Empty))
			{
				log.Info(Usage);
				return;
			}
			int port = 0;
			try
			{
				port = System.Convert.ToInt32(portStr);
			}
			catch (NumberFormatException)
			{
				log.Info("Non-numerical port");
				log.Info(Usage);
				System.Environment.Exit(1);
			}
			if (client != null && !client.Equals(string.Empty))
			{
				// run a test client for illustration/testing
				string host = props.GetProperty("host");
				string encoding = props.GetProperty("encoding");
				if (encoding == null || string.Empty.Equals(encoding))
				{
					encoding = "utf-8";
				}
				MaxentTaggerServer.TaggerClient.CommunicateWithMaxentTaggerServer(host, port, encoding);
			}
			else
			{
				TaggerConfig config = new TaggerConfig(args);
				MaxentTagger tagger = new MaxentTagger(config.GetModel(), config);
				// initializes tagger
				MaxentTagger.TaggerWrapper wrapper = new MaxentTagger.TaggerWrapper(tagger);
				new MaxentTaggerServer(port, wrapper, config.GetEncoding()).Run();
			}
		}
	}
}
