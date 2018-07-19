using System;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>A named-entity recognizer server for Stanford's NER.</summary>
	/// <remarks>
	/// A named-entity recognizer server for Stanford's NER.
	/// Runs on a socket and waits for text to annotate and returns the
	/// annotated text.  (Internally, it uses the
	/// <c>classifyString()</c>
	/// method on a classifier, which can be either the default CRFClassifier
	/// which is serialized inside the jar file from which it is called, or another
	/// classifier which is passed as an argument to the main method.
	/// </remarks>
	/// <version>$Id$</version>
	/// <author>
	/// Bjorn Aldag <br />
	/// Copyright &copy; 2000 - 2004 Cycorp, Inc.  All rights reserved.
	/// Permission granted for Stanford to distribute with their NER code
	/// by Bjorn Aldag
	/// </author>
	/// <author>Christopher Manning 2006 (considerably rewritten)</author>
	public class NERServer
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.NERServer));

		/// <summary>Debugging toggle.</summary>
		private static readonly bool EnvDebug = Runtime.Getenv("NERSERVER_DEBUG") != null && bool.ParseBoolean(Runtime.Getenv("NERSERVER_DEBUG"));

		private bool Debug = EnvDebug;

		private readonly string charset;

		/// <summary>The listener socket of this server.</summary>
		private readonly ServerSocket listener;

		/// <summary>The classifier that does the actual tagging.</summary>
		private readonly AbstractSequenceClassifier ner;

		/// <summary>Creates a new named entity recognizer server on the specified port.</summary>
		/// <param name="port">the port this NERServer listens on.</param>
		/// <param name="asc">The classifier which will do the tagging</param>
		/// <param name="charset">The character set for encoding Strings over the socket stream, e.g., "utf-8"</param>
		/// <exception cref="System.IO.IOException">If there is a problem creating a ServerSocket</exception>
		public NERServer(int port, AbstractSequenceClassifier asc, string charset)
		{
			//// Variables
			//// Constructors
			ner = asc;
			listener = new ServerSocket(port);
			this.charset = charset;
		}

		//// Public Methods
		/// <summary>Runs this named entity recognizer server.</summary>
		public virtual void Run()
		{
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
					new NERServer.Session(this, client);
				}
				catch (Exception e1)
				{
					log.Info("NERServer: couldn't accept");
					Sharpen.Runtime.PrintStackTrace(e1, System.Console.Error);
					try
					{
						client.Close();
					}
					catch (Exception e2)
					{
						log.Info("NERServer: couldn't close client");
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
			private Session(NERServer _enclosing, Socket socket)
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
					NERServer.log.Info("Created new session");
				}
				string input = null;
				try
				{
					// TODO: why not allow for multiple lines of input?
					input = this.@in.ReadLine();
					if (this._enclosing.Debug)
					{
						EncodingPrintWriter.Err.Println("Receiving: \"" + input + '\"', this._enclosing.charset);
					}
				}
				catch (IOException e)
				{
					NERServer.log.Info("NERServer:Session: couldn't read input");
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				catch (ArgumentNullException npe)
				{
					NERServer.log.Info("NERServer:Session: connection closed by peer");
					Sharpen.Runtime.PrintStackTrace(npe, System.Console.Error);
				}
				try
				{
					if (!(input == null))
					{
						string output = this._enclosing.ner.ClassifyToString(input, this._enclosing.ner.flags.outputFormat, !"slashTags".Equals(this._enclosing.ner.flags.outputFormat));
						if (this._enclosing.Debug)
						{
							EncodingPrintWriter.Err.Println("Sending: \"" + output + '\"', this._enclosing.charset);
						}
						this.@out.Print(output);
						this.@out.Flush();
					}
				}
				catch (Exception e)
				{
					// ah well, guess they won't be hearing back from us after all
					if (this._enclosing.Debug)
					{
						NERServer.log.Error("NERServer.Session: error classifying string.");
						NERServer.log.Error(e);
					}
				}
				finally
				{
					this.Close();
				}
			}

			/// <summary>Terminates this session gracefully.</summary>
			private void Close()
			{
				try
				{
					this.@in.Close();
					this.@out.Close();
					if (this._enclosing.Debug)
					{
						NERServer.log.Info("Closing connection to client");
						NERServer.log.Info(this.client.GetInetAddress().GetHostName());
					}
					this.client.Close();
				}
				catch (Exception e)
				{
					NERServer.log.Info("NERServer:Session: can't close session");
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
			}

			private readonly NERServer _enclosing;
		}

		/// <summary>This example sends material to the NER server one line at a time.</summary>
		/// <remarks>
		/// This example sends material to the NER server one line at a time.
		/// Each line should be at least a whole sentence, or can be a whole
		/// document.
		/// </remarks>
		public class NERClient
		{
			private NERClient()
			{
			}

			// end class Session
			/// <exception cref="System.IO.IOException"/>
			public static void CommunicateWithNERServer(string host, int port, string charset)
			{
				System.Console.Out.WriteLine("Input some text and press RETURN to NER tag it, " + " or just RETURN to finish.");
				BufferedReader stdIn = new BufferedReader(new InputStreamReader(Runtime.@in, charset));
				CommunicateWithNERServer(host, port, charset, stdIn, null, true);
				stdIn.Close();
			}

			/// <exception cref="System.IO.IOException"/>
			public static void CommunicateWithNERServer(string host, int port, string charset, BufferedReader input, BufferedWriter output, bool closeOnBlank)
			{
				if (host == null)
				{
					host = "localhost";
				}
				for (string userInput; (userInput = input.ReadLine()) != null; )
				{
					if (userInput.Matches("\\n?"))
					{
						if (closeOnBlank)
						{
							break;
						}
						else
						{
							continue;
						}
					}
					try
					{
						// TODO: why not keep the same socket for multiple lines?
						Socket socket = new Socket(host, port);
						PrintWriter @out = new PrintWriter(new OutputStreamWriter(socket.GetOutputStream(), charset), true);
						BufferedReader @in = new BufferedReader(new InputStreamReader(socket.GetInputStream(), charset));
						// send material to NER to socket
						@out.Println(userInput);
						// Print the results of NER
						string result;
						while ((result = @in.ReadLine()) != null)
						{
							if (output == null)
							{
								EncodingPrintWriter.Out.Println(result, charset);
							}
							else
							{
								output.Write(result);
								output.NewLine();
							}
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
			}
		}

		private const string Usage = "Usage: NERServer [-loadClassifier fileOrResource|-client] -port portNumber";

		// end static class NERClient
		/// <summary>Starts this server on the specified port.</summary>
		/// <remarks>
		/// Starts this server on the specified port.  The classifier used can be
		/// either a default one stored in the jar file from which this code is
		/// invoked or you can specify it as a filename or as another classifier
		/// resource name, which must correspond to the name of a resource in the
		/// /classifiers/ directory of the jar file.
		/// Default port is 4465.
		/// When run in server mode, additional properties can be specified
		/// on the command line and will be passed to the model loaded.
		/// Usage:
		/// <c>java edu.stanford.nlp.ie.NERServer [-loadClassifier fileOrResource|-client] -port portNumber</c>
		/// </remarks>
		/// <param name="args">Command-line arguments (described above)</param>
		/// <exception cref="System.Exception">If file or Java class problems with serialized classifier</exception>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			string loadFile = props.GetProperty("loadClassifier");
			string loadJarFile = props.GetProperty("loadJarClassifier");
			string client = props.GetProperty("client");
			string portStr = props.GetProperty("port", "4465");
			props.Remove("port");
			// so later code doesn't complain
			if (portStr == null || portStr.Equals(string.Empty))
			{
				log.Info(Usage);
				return;
			}
			string charset = "utf-8";
			string encoding = props.GetProperty("encoding");
			if (encoding != null && !string.Empty.Equals(encoding))
			{
				charset = encoding;
			}
			int port;
			try
			{
				port = System.Convert.ToInt32(portStr);
			}
			catch (NumberFormatException)
			{
				log.Info("Non-numerical port");
				log.Info(Usage);
				return;
			}
			// default output format for if no output format is specified
			if (props.GetProperty("outputFormat") == null)
			{
				props.SetProperty("outputFormat", "slashTags");
			}
			if (client != null && !client.Equals(string.Empty))
			{
				// run a test client for illustration/testing
				string host = props.GetProperty("host");
				NERServer.NERClient.CommunicateWithNERServer(host, port, charset);
			}
			else
			{
				AbstractSequenceClassifier asc;
				if (!StringUtils.IsNullOrEmpty(loadFile))
				{
					asc = CRFClassifier.GetClassifier(loadFile, props);
				}
				else
				{
					if (!StringUtils.IsNullOrEmpty(loadJarFile))
					{
						asc = CRFClassifier.GetClassifier(loadJarFile, props);
					}
					else
					{
						asc = CRFClassifier.GetDefaultClassifier(props);
					}
				}
				new NERServer(port, asc, charset).Run();
			}
		}
	}
}
