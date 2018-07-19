using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Locks;
using Java.Util.Function;
using Java.Util.Regex;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// An annotation pipeline in spirit identical to
	/// <see cref="StanfordCoreNLP"/>
	/// , but
	/// with the backend supported by a web server.
	/// </summary>
	/// <author>Gabor Angeli</author>
	public class StanfordCoreNLPClient : AnnotationPipeline
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.StanfordCoreNLPClient));

		/// <summary>A simple URL spec, for parsing backend URLs</summary>
		private static readonly Pattern UrlPattern = Pattern.Compile("(?:(https?)://)?([^:]+)(?::([0-9]+))?");

		/// <summary>Information on how to connect to a backend.</summary>
		/// <remarks>
		/// Information on how to connect to a backend.
		/// The semantics of one of these objects is as follows:
		/// <ul>
		/// <li>It should define a hostname and port to connect to.</li>
		/// <li>This represents ONE thread on the remote server. The client should
		/// treat it as such.</li>
		/// <li>Two backends that are .equals() point to the same endpoint, but there can be
		/// multiple of them if we want to run multiple threads on that endpoint.</li>
		/// </ul>
		/// </remarks>
		private class Backend
		{
			/// <summary>The protocol to connect to the server with.</summary>
			public readonly string protocol;

			/// <summary>The hostname of the server running the CoreNLP annotators</summary>
			public readonly string host;

			/// <summary>The port of the server running the CoreNLP annotators</summary>
			public readonly int port;

			public Backend(string protocol, string host, int port)
			{
				this.protocol = protocol;
				this.host = host;
				this.port = port;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is StanfordCoreNLPClient.Backend))
				{
					return false;
				}
				StanfordCoreNLPClient.Backend backend = (StanfordCoreNLPClient.Backend)o;
				return port == backend.port && protocol.Equals(backend.protocol) && host.Equals(backend.host);
			}

			public override int GetHashCode()
			{
				throw new InvalidOperationException("Hashing backends is dangerous!");
			}

			public override string ToString()
			{
				return protocol + "://" + host + ":" + port;
			}
		}

		/// <summary>
		/// A special type of
		/// <see cref="Java.Lang.Thread"/>
		/// , which is responsible for scheduling jobs
		/// on the backend.
		/// </summary>
		private class BackendScheduler : Thread
		{
			/// <summary>The list of backends that we can schedule on.</summary>
			/// <remarks>
			/// The list of backends that we can schedule on.
			/// This should not generally be called directly from anywhere
			/// </remarks>
			public readonly IList<StanfordCoreNLPClient.Backend> backends;

			/// <summary>The queue on requests for the scheduler to handle.</summary>
			/// <remarks>
			/// The queue on requests for the scheduler to handle.
			/// Each element of this queue is a function: calling the function signals
			/// that this backend is available to perform a task on the passed backend.
			/// It is then obligated to call the passed Consumer to signal that it has
			/// released control of the backend, and it can be used for other things.
			/// Remember to lock access to this object with
			/// <see cref="stateLock"/>
			/// .
			/// </remarks>
			private readonly IQueue<IBiConsumer<StanfordCoreNLPClient.Backend, IConsumer<StanfordCoreNLPClient.Backend>>> queue;

			/// <summary>
			/// The lock on access to
			/// <see cref="queue"/>
			/// .
			/// </summary>
			private readonly ILock stateLock = new ReentrantLock();

			/// <summary>Represents the event that an item has been added to the work queue.</summary>
			/// <remarks>
			/// Represents the event that an item has been added to the work queue.
			/// Linked to
			/// <see cref="stateLock"/>
			/// .
			/// </remarks>
			private readonly ICondition enqueued = stateLock.NewCondition();

			/// <summary>
			/// Represents the event that the queue has become empty, and this schedule is no
			/// longer needed.
			/// </summary>
			public readonly ICondition shouldShutdown = stateLock.NewCondition();

			/// <summary>The queue of annotators (backends) that are free to be run on.</summary>
			/// <remarks>
			/// The queue of annotators (backends) that are free to be run on.
			/// Remember to lock access to this object with
			/// <see cref="stateLock"/>
			/// .
			/// </remarks>
			private readonly IQueue<StanfordCoreNLPClient.Backend> freeAnnotators;

			/// <summary>
			/// Represents the event that an annotator has freed up and is available for
			/// work on the
			/// <see cref="freeAnnotators"/>
			/// queue.
			/// Linked to
			/// <see cref="stateLock"/>
			/// .
			/// </summary>
			private readonly ICondition newlyFree = stateLock.NewCondition();

			/// <summary>While this is true, continue running the scheduler.</summary>
			private bool doRun = true;

			/// <summary>Create a new scheduler from a list of backends.</summary>
			/// <remarks>
			/// Create a new scheduler from a list of backends.
			/// These can contain duplicates -- in that case, that many concurrent
			/// calls can be made to that backend.
			/// </remarks>
			public BackendScheduler(IList<StanfordCoreNLPClient.Backend> backends)
				: base()
			{
				SetDaemon(true);
				this.backends = backends;
				this.freeAnnotators = new LinkedList<StanfordCoreNLPClient.Backend>(backends);
				this.queue = new LinkedList<IBiConsumer<StanfordCoreNLPClient.Backend, IConsumer<StanfordCoreNLPClient.Backend>>>();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public override void Run()
			{
				try
				{
					while (doRun)
					{
						// Wait for a request
						IBiConsumer<StanfordCoreNLPClient.Backend, IConsumer<StanfordCoreNLPClient.Backend>> request;
						StanfordCoreNLPClient.Backend annotator;
						stateLock.Lock();
						try
						{
							while (queue.IsEmpty())
							{
								enqueued.Await();
								if (!doRun)
								{
									return;
								}
							}
							// Get the actual request
							request = queue.Poll();
							// We have a request
							// Find a free annotator
							while (freeAnnotators.IsEmpty())
							{
								newlyFree.Await();
							}
							annotator = freeAnnotators.Poll();
						}
						finally
						{
							stateLock.Unlock();
						}
						// We have an annotator
						// Run the annotation
						request.Accept(annotator, null);
					}
				}
				catch (Exception e)
				{
					// ASYNC: we've freed this annotator
					// add it back to the queue and register it as available
					// If the queue is empty, and all the annotators have returned, we're done
					// Annotator is running (in parallel, most likely)
					throw new Exception(e);
				}
			}

			/// <summary>Schedule a new job on the backend</summary>
			/// <param name="annotate">
			/// A callback, which will be called when a backend is free
			/// to do some processing. The implementation of this callback
			/// MUST CALL the second argument when it is done processing,
			/// to register the backend as free for further work.
			/// </param>
			public virtual void Schedule(IBiConsumer<StanfordCoreNLPClient.Backend, IConsumer<StanfordCoreNLPClient.Backend>> annotate)
			{
				stateLock.Lock();
				try
				{
					queue.Add(annotate);
					enqueued.Signal();
				}
				finally
				{
					stateLock.Unlock();
				}
			}
		}

		/// <summary>The path on the server to connect to.</summary>
		private readonly string path = string.Empty;

		/// <summary>The Properties file to annotate with.</summary>
		private readonly Properties properties;

		/// <summary>The Properties file to send to the server, serialized as JSON.</summary>
		private readonly string propsAsJSON;

		/// <summary>The API key to authenticate with, or null</summary>
		private readonly string apiKey;

		/// <summary>The API secret to authenticate with, or null</summary>
		private readonly string apiSecret;

		/// <summary>The scheduler to use when running on multiple backends at a time</summary>
		private readonly StanfordCoreNLPClient.BackendScheduler scheduler;

		/// <summary>
		/// The annotation serializer responsible for translating between the wire format
		/// (protocol buffers) and the
		/// <see cref="Annotation"/>
		/// classes.
		/// </summary>
		private readonly ProtobufAnnotationSerializer serializer = new ProtobufAnnotationSerializer(true);

		/// <summary>The main constructor.</summary>
		/// <remarks>
		/// The main constructor. Create a client from a properties file and a list of backends.
		/// Note that this creates at least one Daemon thread.
		/// </remarks>
		/// <param name="properties">
		/// The properties file, as would be passed to
		/// <see cref="StanfordCoreNLP"/>
		/// .
		/// </param>
		/// <param name="backends">The backends to run on.</param>
		/// <param name="apiKey">The key to authenticate with as a username</param>
		/// <param name="apiSecret">The key to authenticate with as a password</param>
		private StanfordCoreNLPClient(Properties properties, IList<StanfordCoreNLPClient.Backend> backends, string apiKey, string apiSecret)
		{
			// end static class BackEndScheduler
			// Save the constructor variables
			this.properties = properties;
			Properties serverProperties = new Properties();
			foreach (string key in properties.StringPropertyNames())
			{
				serverProperties.SetProperty(key, properties.GetProperty(key));
			}
			Java.Util.Collections.Shuffle(backends, new Random(Runtime.CurrentTimeMillis()));
			this.scheduler = new StanfordCoreNLPClient.BackendScheduler(backends);
			this.apiKey = apiKey;
			this.apiSecret = apiSecret;
			// Set required serverProperties
			serverProperties.SetProperty("inputFormat", "serialized");
			serverProperties.SetProperty("outputFormat", "serialized");
			serverProperties.SetProperty("inputSerializer", typeof(ProtobufAnnotationSerializer).FullName);
			serverProperties.SetProperty("outputSerializer", typeof(ProtobufAnnotationSerializer).FullName);
			// Create a list of all the properties, as JSON map elements
			IList<string> jsonProperties = serverProperties.StringPropertyNames().Stream().Map(null).Collect(Collectors.ToList());
			// Create the JSON object
			this.propsAsJSON = "{ " + StringUtils.Join(jsonProperties, ", ") + " }";
			// Start 'er up
			this.scheduler.Start();
		}

		/// <summary>The main constructor without credentials.</summary>
		/// <seealso cref="StanfordCoreNLPClient(Java.Util.Properties, System.Collections.Generic.IList{E}, string, string)"/>
		private StanfordCoreNLPClient(Properties properties, IList<StanfordCoreNLPClient.Backend> backends)
			: this(properties, backends, null, null)
		{
		}

		/// <summary>Run the client, pulling credentials from the environment.</summary>
		/// <remarks>
		/// Run the client, pulling credentials from the environment.
		/// Throws an IllegalStateException if the required environment variables aren't set.
		/// These are:
		/// <ul>
		/// <li>CORENLP_HOST</li>
		/// <li>CORENLP_KEY</li>
		/// <li>CORENLP_SECRET</li>
		/// </ul>
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">Thrown if we could not read the required environment variables.</exception>
		public StanfordCoreNLPClient(Properties properties)
			: this(properties, Optional.OfNullable(Runtime.Getenv("CORENLP_HOST")).OrElseThrow(null), Optional.OfNullable(Runtime.Getenv("CORENLP_HOST")).Map(null).OrElse(443), 1, Optional.OfNullable(Runtime.Getenv("CORENLP_KEY")).OrElse(null), Optional
				.OfNullable(Runtime.Getenv("CORENLP_SECRET")).OrElse(null))
		{
		}

		/// <summary>Run on a single backend.</summary>
		/// <seealso cref="StanfordCoreNLPClient">(Properties, List)</seealso>
		public StanfordCoreNLPClient(Properties properties, string host, int port)
			: this(properties, host, port, 1)
		{
		}

		/// <summary>Run on a single backend, with authentication</summary>
		/// <seealso cref="StanfordCoreNLPClient">(Properties, List)</seealso>
		public StanfordCoreNLPClient(Properties properties, string host, int port, string apiKey, string apiSecret)
			: this(properties, host, port, 1, apiKey, apiSecret)
		{
		}

		/// <summary>Run on a single backend, with authentication</summary>
		/// <seealso cref="StanfordCoreNLPClient">(Properties, List)</seealso>
		public StanfordCoreNLPClient(Properties properties, string host, string apiKey, string apiSecret)
			: this(properties, host, host.StartsWith("http://") ? 80 : 443, 1, apiKey, apiSecret)
		{
		}

		/// <summary>Run on a single backend, but with k threads on each backend.</summary>
		/// <seealso cref="StanfordCoreNLPClient">(Properties, List)</seealso>
		public StanfordCoreNLPClient(Properties properties, string host, int port, int threads)
			: this(properties, host, port, threads, null, null)
		{
		}

		/// <summary>Run on a single backend, but with k threads on each backend, and with authentication</summary>
		/// <seealso cref="StanfordCoreNLPClient">(Properties, List)</seealso>
		public StanfordCoreNLPClient(Properties properties, string host, int port, int threads, string apiKey, string apiSecret)
			: this(properties, new _List_369(threads, host, port), apiKey, apiSecret)
		{
		}

		private sealed class _List_369 : List<StanfordCoreNLPClient.Backend>
		{
			public _List_369(int threads, string host, int port)
			{
				this.threads = threads;
				this.host = host;
				this.port = port;
				{
					for (int i = 0; i < threads; ++i)
					{
						this.Add(new StanfordCoreNLPClient.Backend(host.StartsWith("http://") ? "http" : "https", host.StartsWith("http://") ? Sharpen.Runtime.Substring(host, "http://".Length) : (host.StartsWith("https://") ? Sharpen.Runtime.Substring(host, "https://"
							.Length) : host), port));
					}
				}
			}

			private readonly int threads;

			private readonly string host;

			private readonly int port;
		}

		/// <summary>
		/// <inheritDoc/>
		/// This method creates an async call to the server, and blocks until the server
		/// has finished annotating the object.
		/// </summary>
		public override void Annotate(Annotation annotation)
		{
			ILock Lock = new ReentrantLock();
			ICondition annotationDone = Lock.NewCondition();
			Annotate(Java.Util.Collections.Singleton(annotation), 1, null);
			try
			{
				Lock.Lock();
				annotationDone.Await();
			}
			catch (Exception)
			{
				// Only wait for one callback to complete; only annotating one document
				log.Info("Interrupt while waiting for annotation to return");
			}
			finally
			{
				Lock.Unlock();
			}
		}

		/// <summary>This method fires off a request to the server.</summary>
		/// <remarks>
		/// This method fires off a request to the server. Upon returning, it calls the provided
		/// callback method.
		/// </remarks>
		/// <param name="annotations">The input annotations to process</param>
		/// <param name="numThreads">The number of threads to run on. IGNORED in this class.</param>
		/// <param name="callback">A function to be called when an annotation finishes.</param>
		public override void Annotate(IEnumerable<Annotation> annotations, int numThreads, IConsumer<Annotation> callback)
		{
			foreach (Annotation annotation in annotations)
			{
				Annotate(annotation, callback);
			}
		}

		/// <summary>The canonical entry point of the client annotator.</summary>
		/// <remarks>
		/// The canonical entry point of the client annotator.
		/// Create an HTTP request, send this annotation to the server, and await a response.
		/// </remarks>
		/// <param name="annotation">The annotation to annotate.</param>
		/// <param name="callback">
		/// Called when the server has returned an annotated document.
		/// The input to this callback is the same as the passed Annotation object.
		/// </param>
		public virtual void Annotate(Annotation annotation, IConsumer<Annotation> callback)
		{
			scheduler.Schedule(null);
		}

		// 1. Create the input
		// 1.1 Create a protocol buffer
		// 1.2 Create the query params
		// 2. Create a connection
		// 3. Do the annotation
		//    This method has two contracts:
		//    1. It should call the two relevant callbacks
		//    2. It must not throw an exception
		/// <summary>Actually try to perform the annotation on the server side.</summary>
		/// <remarks>
		/// Actually try to perform the annotation on the server side.
		/// This is factored out so that we can retry up to 3 times.
		/// </remarks>
		/// <param name="annotation">The annotation we need to fill.</param>
		/// <param name="backend">The backend we are querying against.</param>
		/// <param name="serverURL">The URL of the server we are hitting.</param>
		/// <param name="message">The message we are sending the server (don't need to recompute each retry).</param>
		/// <param name="tries">The number of times we've tried already.</param>
		private void DoAnnotation(Annotation annotation, StanfordCoreNLPClient.Backend backend, URL serverURL, byte[] message, int tries)
		{
			try
			{
				// 1. Set up the connection
				URLConnection connection = serverURL.OpenConnection();
				// 1.1 Set authentication
				if (apiKey != null && apiSecret != null)
				{
					string userpass = apiKey + ":" + apiSecret;
					string basicAuth = "Basic " + Sharpen.Runtime.GetStringForBytes(Base64.GetEncoder().Encode(Sharpen.Runtime.GetBytesForString(userpass)));
					connection.SetRequestProperty("Authorization", basicAuth);
				}
				// 1.2 Set some protocol-independent properties
				connection.SetDoOutput(true);
				connection.SetRequestProperty("Content-Type", "application/x-protobuf");
				connection.SetRequestProperty("Content-Length", int.ToString(message.Length));
				connection.SetRequestProperty("Accept-Charset", "utf-8");
				connection.SetRequestProperty("User-Agent", typeof(StanfordCoreNLPClient).FullName);
				switch (backend.protocol)
				{
					case "https":
					case "http":
					{
						// 1.3 Set some protocol-dependent properties
						((HttpURLConnection)connection).SetRequestMethod("POST");
						break;
					}

					default:
					{
						throw new InvalidOperationException("Haven't implemented protocol: " + backend.protocol);
					}
				}
				// 2. Annotate
				// 2.1. Fire off the request
				connection.Connect();
				connection.GetOutputStream().Write(message);
				connection.GetOutputStream().Flush();
				// 2.2 Await a response
				// -- It might be possible to send more than one message, but we are not going to do that.
				Annotation response = serializer.Read(connection.GetInputStream()).first;
				// 2.3. Copy response over to original annotation
				foreach (Type key in response.KeySet())
				{
					annotation.Set(key, response.Get(key));
				}
			}
			catch (Exception t)
			{
				// 3. We encountered an error -- retry
				if (tries < 3)
				{
					log.Warn(t);
					DoAnnotation(annotation, backend, serverURL, message, tries + 1);
				}
				else
				{
					throw new Exception(t);
				}
			}
		}

		public virtual bool CheckStatus(URL serverURL)
		{
			try
			{
				// 1. Set up the connection
				HttpURLConnection connection = (HttpURLConnection)serverURL.OpenConnection();
				// 1.1 Set authentication
				if (apiKey != null && apiSecret != null)
				{
					string userpass = apiKey + ":" + apiSecret;
					string basicAuth = "Basic " + Sharpen.Runtime.GetStringForBytes(Base64.GetEncoder().Encode(Sharpen.Runtime.GetBytesForString(userpass)));
					connection.SetRequestProperty("Authorization", basicAuth);
				}
				connection.SetRequestMethod("GET");
				connection.Connect();
				return connection.GetResponseCode() >= 200 && connection.GetResponseCode() <= 400;
			}
			catch (Exception t)
			{
				throw new Exception(t);
			}
		}

		/// <summary>Runs the entire pipeline on the content of the given text passed in.</summary>
		/// <param name="text">The text to process</param>
		/// <returns>An Annotation object containing the output of all annotators</returns>
		public virtual Annotation Process(string text)
		{
			Annotation annotation = new Annotation(text);
			Annotate(annotation);
			return annotation;
		}

		/// <summary>Runs an interactive shell where input text is processed with the given pipeline.</summary>
		/// <param name="pipeline">The pipeline to be used</param>
		/// <exception cref="System.IO.IOException">If IO problem with stdin</exception>
		private static void Shell(StanfordCoreNLPClient pipeline)
		{
			log.Info("Entering interactive shell. Type q RETURN or EOF to quit.");
			StanfordCoreNLP.OutputFormat outputFormat = StanfordCoreNLP.OutputFormat.ValueOf(pipeline.properties.GetProperty("outputFormat", "text").ToUpper());
			IOUtils.Console("NLP> ", null);
		}

		/// <summary>The implementation of what to run on a command-line call of CoreNLPWebClient</summary>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public virtual void Run()
		{
			StanfordRedwoodConfiguration.MinimalSetup();
			StanfordCoreNLP.OutputFormat outputFormat = StanfordCoreNLP.OutputFormat.ValueOf(properties.GetProperty("outputFormat", "text").ToUpper());
			//
			// Process one file or a directory of files
			//
			if (properties.Contains("file") || properties.Contains("textFile"))
			{
				string fileName = properties.GetProperty("file");
				if (fileName == null)
				{
					fileName = properties.GetProperty("textFile");
				}
				ICollection<File> files = new FileSequentialCollection(new File(fileName), properties.GetProperty("extension"), true);
				StanfordCoreNLP.ProcessFiles(null, files, 1, properties, null, StanfordCoreNLP.CreateOutputter(properties, new AnnotationOutputter.Options()), outputFormat, false);
			}
			else
			{
				//
				// Process a list of files
				//
				if (properties.Contains("filelist"))
				{
					string fileName = properties.GetProperty("filelist");
					ICollection<File> inputFiles = StanfordCoreNLP.ReadFileList(fileName);
					ICollection<File> files = new List<File>(inputFiles.Count);
					foreach (File file in inputFiles)
					{
						if (file.IsDirectory())
						{
							Sharpen.Collections.AddAll(files, new FileSequentialCollection(new File(fileName), properties.GetProperty("extension"), true));
						}
						else
						{
							files.Add(file);
						}
					}
					StanfordCoreNLP.ProcessFiles(null, files, 1, properties, null, StanfordCoreNLP.CreateOutputter(properties, new AnnotationOutputter.Options()), outputFormat, false);
				}
				else
				{
					//
					// Run the interactive shell
					//
					Shell(this);
				}
			}
		}

		/// <summary>
		/// <p>
		/// Good practice to call after you are done with this object.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Good practice to call after you are done with this object.
		/// Shuts down the queue of annotations to run and the associated threads.
		/// </p>
		/// <p>
		/// If this is not called, any job which has been scheduled but not run will be
		/// cancelled.
		/// </p>
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual void Shutdown()
		{
			scheduler.stateLock.Lock();
			try
			{
				while (!scheduler.queue.IsEmpty() || scheduler.freeAnnotators.Count != scheduler.backends.Count)
				{
					scheduler.shouldShutdown.Await(5, TimeUnit.Seconds);
				}
				scheduler.doRun = false;
				scheduler.enqueued.SignalAll();
			}
			finally
			{
				// In case the thread's waiting on this condition
				scheduler.stateLock.Unlock();
			}
		}

		/// <summary>Client that runs data through a StanfordCoreNLPServer either just for testing or for command-line text processing.</summary>
		/// <remarks>
		/// Client that runs data through a StanfordCoreNLPServer either just for testing or for command-line text processing.
		/// This runs the pipeline you specify on the
		/// text in the file(s) that you specify (with -file or -filelist) and sends some results to stdout.
		/// The current code in this main method assumes that each line of the file
		/// is to be processed separately as a single sentence.
		/// A site must be specified with a protocol like "https:" in front of it.
		/// Example usage:<br />
		/// java -mx6g edu.stanford.nlp.pipeline.StanfordCoreNLP -props properties -backends site1:port1,site2:port2 <br />
		/// or just -host https://foo.bar.com [-port 9000]
		/// </remarks>
		/// <param name="args">List of required properties</param>
		/// <exception cref="System.IO.IOException">If IO problem</exception>
		/// <exception cref="System.TypeLoadException">If class loading problem</exception>
		public static void Main(string[] args)
		{
			//
			// process the arguments
			//
			// extract all the properties from the command line
			// if cmd line is empty, set the properties to null. The processor will search for the properties file in the classpath
			// if (args.length < 2) {
			//   log.info("Usage: " + StanfordCoreNLPClient.class.getSimpleName() + " -host <hostname> -port <port> ...");
			//   System.exit(1);
			// }
			Properties props = StringUtils.ArgsToProperties(args);
			bool hasH = props.Contains("h");
			bool hasHelp = props.Contains("help");
			if (hasH || hasHelp)
			{
				string helpValue = hasH ? props.GetProperty("h") : props.GetProperty("help");
				StanfordCoreNLP.PrintHelp(System.Console.Error, helpValue);
				return;
			}
			// Create the backends
			IList<StanfordCoreNLPClient.Backend> backends = new List<StanfordCoreNLPClient.Backend>();
			string defaultBack = "http://localhost:9000";
			string backStr = props.GetProperty("backends");
			if (backStr == null)
			{
				string host = props.GetProperty("host");
				string port = props.GetProperty("port");
				if (host != null)
				{
					if (port != null)
					{
						defaultBack = host + ':' + port;
					}
					else
					{
						defaultBack = host;
					}
				}
			}
			foreach (string spec in props.GetProperty("backends", defaultBack).Split(","))
			{
				Matcher matcher = UrlPattern.Matcher(spec.Trim());
				if (matcher.Matches())
				{
					string protocol = matcher.Group(1);
					if (protocol == null)
					{
						protocol = "http";
					}
					string host = matcher.Group(2);
					int port = 80;
					string portStr = matcher.Group(3);
					if (portStr != null)
					{
						port = System.Convert.ToInt32(portStr);
					}
					backends.Add(new StanfordCoreNLPClient.Backend(protocol, host, port));
				}
			}
			log.Info("Using backends: " + backends);
			// Run the pipeline
			StanfordCoreNLPClient client = new StanfordCoreNLPClient(props, backends);
			client.Run();
			try
			{
				client.Shutdown();
			}
			catch (Exception)
			{
			}
		}
		// In case anything is pending on the server
		// end main()
	}
}
