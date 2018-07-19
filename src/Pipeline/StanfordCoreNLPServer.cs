using System;
using System.Collections.Generic;
using System.IO;
using Com.Sun.Net.Httpserver;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Ref;
using Java.Math;
using Java.Net;
using Java.Nio;
using Java.Security;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Atomic;
using Java.Util.Function;
using Javax.Net.Ssl;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class creates a server that runs a new Java annotator in each thread.</summary>
	/// <author>Gabor Angeli</author>
	/// <author>Arun Chaganty</author>
	public class StanfordCoreNLPServer : IRunnable
	{
		protected internal HttpServer server;

		protected internal string serverID = null;

		protected internal int serverPort = 9000;

		protected internal int statusPort;

		protected internal string uriContext = string.Empty;

		protected internal int timeoutMilliseconds = 15000;

		protected internal bool strict = false;

		protected internal bool quiet = false;

		protected internal bool ssl = false;

		protected internal static string key = "edu/stanford/nlp/pipeline/corenlp.jks";

		protected internal string username = null;

		protected internal string password = null;

		protected internal static string defaultAnnotators = "tokenize,ssplit,pos,lemma,ner,parse,depparse,coref,natlog,openie,kbp";

		protected internal static string preloadedAnnotators = string.Empty;

		protected internal static string serverPropertiesPath = null;

		protected internal static int maxCharLength = 100000;

		protected internal static string blacklist = null;

		protected internal bool stanford = false;

		protected internal readonly string shutdownKey;

		private readonly Properties defaultProps;

		/// <summary>The thread pool for the HTTP server.</summary>
		private readonly IExecutorService serverExecutor;

		/// <summary>
		/// To prevent grossly wasteful over-creation of pipeline objects, cache the last
		/// one we created.
		/// </summary>
		private SoftReference<Pair<string, StanfordCoreNLP>> lastPipeline = new SoftReference<Pair<string, StanfordCoreNLP>>(null);

		/// <summary>An executor to time out CoreNLP execution with.</summary>
		private readonly IExecutorService corenlpExecutor;

		/// <summary>A list of blacklisted subnets -- these cannot call the server.</summary>
		private readonly IList<Pair<Inet4Address, int>> blacklistSubnets;

		/// <summary>Create a new Stanford CoreNLP Server.</summary>
		/// <param name="props">A list of properties for the server (server_id, ...)</param>
		/// <param name="port">The port to host the server from.</param>
		/// <param name="timeout">The timeout (in milliseconds) for each command.</param>
		/// <param name="strict">If true, conform more strictly to the HTTP spec (e.g., for character encoding).</param>
		/// <exception cref="System.IO.IOException">Thrown from the underlying socket implementation.</exception>
		public StanfordCoreNLPServer(Properties props, int port, int timeout, bool strict)
			: this(props)
		{
			statusPort = serverPort;
			// currently not used
			this.serverPort = port;
			if (props != null && !props.Contains("status_port"))
			{
				this.statusPort = port;
			}
			this.timeoutMilliseconds = timeout;
			this.strict = strict;
		}

		/// <summary>Create a new Stanford CoreNLP Server.</summary>
		/// <param name="port">The port to host the server from.</param>
		/// <param name="timeout">The timeout (in milliseconds) for each command.</param>
		/// <param name="strict">If true, conform more strictly to the HTTP spec (e.g., for character encoding).</param>
		/// <exception cref="System.IO.IOException">Thrown from the underlying socket implementation.</exception>
		public StanfordCoreNLPServer(int port, int timeout, bool strict)
			: this(null, port, timeout, strict)
		{
			statusPort = serverPort;
		}

		/// <summary>Create a new Stanford CoreNLP Server, with the default parameters.</summary>
		/// <exception cref="System.IO.IOException">Thrown if we could not write the shutdown key to the a file.</exception>
		public StanfordCoreNLPServer()
			: this(null)
		{
			statusPort = serverPort;
		}

		/// <summary>
		/// Create a new Stanford CoreNLP Server with the default parameters and
		/// pass in properties (server_id, ...).
		/// </summary>
		/// <exception cref="System.IO.IOException">Thrown if we could not write the shutdown key to the a file.</exception>
		public StanfordCoreNLPServer(Properties props)
		{
			statusPort = serverPort;
			// check if englishSR.ser.gz can be found (standard models jar doesn't have this)
			string defaultParserPath;
			ClassLoader classLoader = GetType().GetClassLoader();
			URL srResource = classLoader.GetResource("edu/stanford/nlp/models/srparser/englishSR.ser.gz");
			Redwood.Util.Log("setting default constituency parser");
			if (srResource != null)
			{
				defaultParserPath = "edu/stanford/nlp/models/srparser/englishSR.ser.gz";
				Redwood.Util.Log("using SR parser: edu/stanford/nlp/models/srparser/englishSR.ser.gz");
			}
			else
			{
				defaultParserPath = "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";
				Redwood.Util.Log("warning: cannot find edu/stanford/nlp/models/srparser/englishSR.ser.gz");
				Redwood.Util.Log("using: edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz instead");
				Redwood.Util.Log("to use shift reduce parser download English models jar from:");
				Redwood.Util.Log("http://stanfordnlp.github.io/CoreNLP/download.html");
			}
			this.defaultProps = PropertiesUtils.AsProperties("annotators", defaultAnnotators, "coref.mention.type", "dep", "coref.mode", "statistical", "coref.language", "en", "inputFormat", "text", "outputFormat", "json", "prettyPrint", "false", "parse.model"
				, defaultParserPath, "parse.binaryTrees", "true", "openie.strip_entailments", "true");
			// Run these annotators by default
			// Use dependency trees with coref by default
			// Use the new coref
			// We're English by default
			// By default, treat the POST data like text
			// By default, return in JSON -- this is a server, after all.
			// Don't bother pretty-printing
			// SR scales linearly with sentence length. Good for a server!
			// needed for the Sentiment annotator
			// these are large to serialize, so ignore them
			// overwrite all default properties with provided server properties
			// for instance you might want to provide a default ner model
			if (serverPropertiesPath != null)
			{
				Properties serverProperties = StringUtils.ArgsToProperties("-props", serverPropertiesPath);
				PropertiesUtils.OverWriteProperties(this.defaultProps, serverProperties);
			}
			this.serverExecutor = Executors.NewFixedThreadPool(ArgumentParser.threads);
			this.corenlpExecutor = Executors.NewFixedThreadPool(ArgumentParser.threads);
			// Generate and write a shutdown key, get optional server_id from passed in properties
			// this way if multiple servers running can shut them all down with different ids
			string shutdownKeyFileName;
			if (props != null && props.GetProperty("server_id") != null)
			{
				shutdownKeyFileName = "corenlp.shutdown." + props.GetProperty("server_id");
			}
			else
			{
				shutdownKeyFileName = "corenlp.shutdown";
			}
			string tmpDir = Runtime.GetProperty("java.io.tmpdir");
			File tmpFile = new File(tmpDir + File.separator + shutdownKeyFileName);
			tmpFile.DeleteOnExit();
			if (tmpFile.Exists())
			{
				if (!tmpFile.Delete())
				{
					throw new InvalidOperationException("Could not delete shutdown key file");
				}
			}
			this.shutdownKey = new BigInteger(130, new Random()).ToString(32);
			IOUtils.WriteStringToFile(shutdownKey, tmpFile.GetPath(), "utf-8");
			// set status port
			if (props != null && props.Contains("status_port"))
			{
				this.statusPort = System.Convert.ToInt32(props.GetProperty("status_port"));
			}
			else
			{
				if (props != null && props.Contains("port"))
				{
					this.statusPort = System.Convert.ToInt32(props.GetProperty("port"));
				}
			}
			// parse blacklist
			if (blacklist == null)
			{
				this.blacklistSubnets = Java.Util.Collections.EmptyList();
			}
			else
			{
				this.blacklistSubnets = new List<Pair<Inet4Address, int>>();
				foreach (string subnet in IOUtils.ReadLines(blacklist))
				{
					try
					{
						this.blacklistSubnets.Add(ParseSubnet(subnet));
					}
					catch (ArgumentException)
					{
						Redwood.Util.Warn("Could not parse subnet: " + subnet);
					}
				}
			}
		}

		/// <summary>Parse the URL parameters into a map of (key, value) pairs.</summary>
		/// <param name="uri">The URL that was requested.</param>
		/// <returns>A map of (key, value) pairs corresponding to the request parameters.</returns>
		/// <exception cref="Java.IO.UnsupportedEncodingException">Thrown if we could not decode the URL with utf8.</exception>
		private static IDictionary<string, string> GetURLParams(URI uri)
		{
			if (uri.GetQuery() != null)
			{
				IDictionary<string, string> urlParams = new Dictionary<string, string>();
				string query = uri.GetQuery();
				string[] queryFields = query.ReplaceAll("\\\\&", "___AMP___").ReplaceAll("\\\\\\+", "___PLUS___").Split("&");
				foreach (string queryField in queryFields)
				{
					int firstEq = queryField.IndexOf('=');
					// Convention uses "+" for spaces.
					string key = URLDecoder.Decode(Sharpen.Runtime.Substring(queryField, 0, firstEq), "utf8").ReplaceAll("___AMP___", "&").ReplaceAll("___PLUS___", "+");
					string value = URLDecoder.Decode(Sharpen.Runtime.Substring(queryField, firstEq + 1), "utf8").ReplaceAll("___AMP___", "&").ReplaceAll("___PLUS___", "+");
					urlParams[key] = value;
				}
				return urlParams;
			}
			else
			{
				return Java.Util.Collections.EmptyMap();
			}
		}

		/// <summary>Reads the POST contents of the request and parses it into an Annotation object, ready to be annotated.</summary>
		/// <remarks>
		/// Reads the POST contents of the request and parses it into an Annotation object, ready to be annotated.
		/// This method can also read a serialized document, if the input format is set to be serialized.
		/// </remarks>
		/// <param name="props">The properties we are annotating with. This is where the input format is retrieved from.</param>
		/// <param name="httpExchange">The exchange we are reading POST data from.</param>
		/// <returns>An Annotation representing the read document.</returns>
		/// <exception cref="System.IO.IOException">Thrown if we cannot read the POST data.</exception>
		/// <exception cref="System.TypeLoadException">Thrown if we cannot load the serializer.</exception>
		private Annotation GetDocument(Properties props, HttpExchange httpExchange)
		{
			string inputFormat = props.GetProperty("inputFormat", "text");
			string date = props.GetProperty("date");
			switch (inputFormat)
			{
				case "text":
				{
					// The default encoding by the HTTP standard is ISO-8859-1, but most
					// real users of CoreNLP would likely assume UTF-8 by default.
					string defaultEncoding = this.strict ? "ISO-8859-1" : "UTF-8";
					// Get the encoding
					Headers h = httpExchange.GetRequestHeaders();
					string encoding;
					if (h.Contains("Content-type"))
					{
						string[] charsetPair = Arrays.Stream(h.GetFirst("Content-type").Split(";")).Map(null).Filter(null).FindFirst().OrElse(new string[] { "charset", defaultEncoding });
						if (charsetPair.Length == 2)
						{
							encoding = charsetPair[1];
						}
						else
						{
							encoding = defaultEncoding;
						}
					}
					else
					{
						encoding = defaultEncoding;
					}
					string text = IOUtils.SlurpReader(IOUtils.EncodedInputStreamReader(httpExchange.GetRequestBody(), encoding));
					// Remove the \ and + characters that mess up the URL decoding.
					text = text.ReplaceAll("%(?![0-9a-fA-F]{2})", "%25");
					text = text.ReplaceAll("\\+", "%2B");
					text = URLDecoder.Decode(text, encoding).Trim();
					// Read the annotation
					Annotation annotation = new Annotation(text);
					// Set the date (if provided)
					if (date != null)
					{
						annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), date);
					}
					return annotation;
				}

				case "serialized":
				{
					string inputSerializerName = props.GetProperty("inputSerializer", typeof(ProtobufAnnotationSerializer).FullName);
					AnnotationSerializer serializer = MetaClass.Create(inputSerializerName).CreateInstance();
					Pair<Annotation, InputStream> pair = serializer.Read(httpExchange.GetRequestBody());
					return pair.first;
				}

				default:
				{
					throw new IOException("Could not parse input format: " + inputFormat);
				}
			}
		}

		/// <summary>Create (or retrieve) a StanfordCoreNLP object corresponding to these properties.</summary>
		/// <param name="props">The properties to create the object with.</param>
		/// <returns>A pipeline parameterized by these properties.</returns>
		private StanfordCoreNLP MkStanfordCoreNLP(Properties props)
		{
			StanfordCoreNLP impl;
			StringBuilder sb = new StringBuilder();
			props.StringPropertyNames().Stream().Filter(null).ForEach(null);
			string cacheKey = sb.ToString();
			lock (this)
			{
				Pair<string, StanfordCoreNLP> lastPipeline = this.lastPipeline.Get();
				if (lastPipeline != null && Objects.Equals(lastPipeline.first, cacheKey))
				{
					return lastPipeline.second;
				}
				else
				{
					// Do some housekeeping on the global cache
					for (IEnumerator<KeyValuePair<StanfordCoreNLP.AnnotatorSignature, Lazy<IAnnotator>>> iter = StanfordCoreNLP.GlobalAnnotatorCache.GetEnumerator(); iter.MoveNext(); )
					{
						KeyValuePair<StanfordCoreNLP.AnnotatorSignature, Lazy<IAnnotator>> entry = iter.Current;
						if (!entry.Value.IsCache())
						{
							Redwood.Util.Error("Entry in global cache is not garbage collectable!");
							iter.Remove();
						}
						else
						{
							if (entry.Value.IsGarbageCollected())
							{
								iter.Remove();
							}
						}
					}
					// Create a CoreNLP
					impl = new StanfordCoreNLP(props);
					this.lastPipeline = new SoftReference<Pair<string, StanfordCoreNLP>>(Pair.MakePair(cacheKey, impl));
				}
			}
			return impl;
		}

		/// <summary>
		/// Parse the parameters of a connection into a CoreNLP properties file that can be passed into
		/// <see cref="StanfordCoreNLP"/>
		/// , and used in the I/O stages.
		/// </summary>
		/// <param name="httpExchange">The http exchange; effectively, the request information.</param>
		/// <returns>
		/// A
		/// <see cref="Java.Util.Properties"/>
		/// object corresponding to a combination of default and passed properties.
		/// </returns>
		/// <exception cref="Java.IO.UnsupportedEncodingException">Thrown if we could not decode the key/value pairs with UTF-8.</exception>
		private Properties GetProperties(HttpExchange httpExchange)
		{
			IDictionary<string, string> urlParams = GetURLParams(httpExchange.GetRequestURI());
			// Load the default properties
			Properties props = new Properties();
			defaultProps.ForEach(null);
			// Add GET parameters as properties
			urlParams.Stream().Filter(null).ForEach(null);
			// Try to get more properties from query string.
			// (get the properties from the URL params)
			IDictionary<string, string> urlProperties = new Dictionary<string, string>();
			if (urlParams.Contains("properties"))
			{
				urlProperties = StringUtils.DecodeMap(URLDecoder.Decode(urlParams["properties"], "UTF-8"));
			}
			else
			{
				if (urlParams.Contains("props"))
				{
					urlProperties = StringUtils.DecodeMap(URLDecoder.Decode(urlParams["props"], "UTF-8"));
				}
			}
			// check to see if a specific language was set, use language specific properties
			string language = urlParams.GetOrDefault("pipelineLanguage", urlProperties.GetOrDefault("pipelineLanguage", "default"));
			if (language != null && !"default".Equals(language))
			{
				string languagePropertiesFile = LanguageInfo.GetLanguagePropertiesFile(language);
				if (languagePropertiesFile != null)
				{
					try
					{
						using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(languagePropertiesFile))
						{
							Properties languageSpecificProperties = new Properties();
							languageSpecificProperties.Load(@is);
							PropertiesUtils.OverWriteProperties(props, languageSpecificProperties);
						}
					}
					catch (IOException)
					{
						Redwood.Util.Err("Failure to load language specific properties: " + languagePropertiesFile + " for " + language);
					}
				}
				else
				{
					try
					{
						RespondError("Invalid language: '" + language + '\'', httpExchange);
					}
					catch (IOException e)
					{
						Redwood.Util.Warn(e);
					}
					return new Properties();
				}
			}
			// (tweak the default properties a bit)
			if (!props.Contains("mention.type"))
			{
				// Set coref head to use dependencies
				props.SetProperty("mention.type", "dep");
				if (urlProperties.Contains("annotators") && urlProperties["annotators"] != null && ArrayUtils.Contains(urlProperties["annotators"].Split(","), "parse"))
				{
					// (case: the properties have a parse annotator --
					//        we don't have to use the dependency mention finder)
					props.Remove("mention.type");
				}
			}
			// (add new properties on top of the default properties)
			urlProperties.ForEach(null);
			// Get the annotators
			string annotators = props.GetProperty("annotators");
			// If the properties contains a custom annotator, then do not enforceRequirements.
			if (!PropertiesUtils.HasPropertyPrefix(props, StanfordCoreNLP.CustomAnnotatorPrefix) && PropertiesUtils.GetBool(props, "enforceRequirements", true))
			{
				annotators = StanfordCoreNLP.EnsurePrerequisiteAnnotators(props.GetProperty("annotators").Split("[, \t]+"), props);
			}
			// Make sure the properties compile
			props.SetProperty("annotators", annotators);
			return props;
		}

		/// <summary>A helper function to respond to a request with an error.</summary>
		/// <param name="response">The description of the error to send to the user.</param>
		/// <param name="httpExchange">The exchange to send the error over.</param>
		/// <exception cref="System.IO.IOException">Thrown if the HttpExchange cannot communicate the error.</exception>
		private static void RespondError(string response, HttpExchange httpExchange)
		{
			httpExchange.GetResponseHeaders().Add("Content-type", "text/plain");
			httpExchange.SendResponseHeaders(HttpInternalError, response.Length);
			httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
			httpExchange.Close();
		}

		/// <summary>
		/// A helper function to respond to a request with an error specifically indicating
		/// bad input from the user.
		/// </summary>
		/// <param name="response">The description of the error to send to the user.</param>
		/// <param name="httpExchange">The exchange to send the error over.</param>
		/// <exception cref="System.IO.IOException">Thrown if the HttpExchange cannot communicate the error.</exception>
		private static void RespondBadInput(string response, HttpExchange httpExchange)
		{
			httpExchange.GetResponseHeaders().Add("Content-type", "text/plain");
			httpExchange.SendResponseHeaders(HttpBadRequest, response.Length);
			httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
			httpExchange.Close();
		}

		/// <summary>
		/// A helper function to respond to a request with an error stating that the user is not authorized
		/// to make this request.
		/// </summary>
		/// <param name="httpExchange">The exchange to send the error over.</param>
		/// <exception cref="System.IO.IOException">Thrown if the HttpExchange cannot communicate the error.</exception>
		private static void RespondUnauthorized(HttpExchange httpExchange)
		{
			Redwood.Util.Log("Responding unauthorized to " + httpExchange.GetRemoteAddress());
			httpExchange.GetResponseHeaders().Add("Content-type", "application/javascript");
			byte[] content = Sharpen.Runtime.GetBytesForString("{\"message\": \"Unauthorized API request\"}", "utf-8");
			httpExchange.SendResponseHeaders(HttpUnauthorized, content.Length);
			httpExchange.GetResponseBody().Write(content);
			httpExchange.Close();
		}

		private static void SetHttpExchangeResponseHeaders(HttpExchange httpExchange)
		{
			// Set common response headers
			httpExchange.GetResponseHeaders().Add("Access-Control-Allow-Origin", "*");
			httpExchange.GetResponseHeaders().Add("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
			httpExchange.GetResponseHeaders().Add("Access-Control-Allow-Headers", "*");
			httpExchange.GetResponseHeaders().Add("Access-Control-Allow-Credentials", "true");
			httpExchange.GetResponseHeaders().Add("Access-Control-Allow-Credentials-Header", "*");
		}

		/// <summary>Adapted from: https://stackoverflow.com/questions/4209760/validate-an-ip-address-with-mask</summary>
		private static Pair<Inet4Address, int> ParseSubnet(string subnet)
		{
			string[] parts = subnet.Split("/");
			string ip = parts[0];
			int prefix;
			if (parts.Length < 2)
			{
				prefix = 0;
			}
			else
			{
				prefix = System.Convert.ToInt32(parts[1]);
			}
			try
			{
				return Pair.MakePair((Inet4Address)InetAddress.GetByName(ip), prefix);
			}
			catch (UnknownHostException)
			{
				throw new ArgumentException("Invalid subnet: " + subnet);
			}
		}

		/// <summary>Adapted from: https://stackoverflow.com/questions/4209760/validate-an-ip-address-with-mask</summary>
		private static bool NetMatch(Pair<Inet4Address, int> subnet, Inet4Address addr)
		{
			byte[] b = subnet.first.GetAddress();
			int ipInt = ((b[0] & unchecked((int)(0xFF))) << 24) | ((b[1] & unchecked((int)(0xFF))) << 16) | ((b[2] & unchecked((int)(0xFF))) << 8) | ((b[3] & unchecked((int)(0xFF))) << 0);
			byte[] b1 = addr.GetAddress();
			int ipInt1 = ((b1[0] & unchecked((int)(0xFF))) << 24) | ((b1[1] & unchecked((int)(0xFF))) << 16) | ((b1[2] & unchecked((int)(0xFF))) << 8) | ((b1[3] & unchecked((int)(0xFF))) << 0);
			int mask = ~((1 << (32 - subnet.second)) - 1);
			return (ipInt & mask) == (ipInt1 & mask);
		}

		/// <summary>Check that the given address is not in the subnet</summary>
		/// <param name="addr">The address to check.</param>
		/// <returns>True if the address is <b>not</b> in any blacklisted subnet. That is, we can accept connections from it.</returns>
		private bool OnBlacklist(Inet4Address addr)
		{
			foreach (Pair<Inet4Address, int> subnet in blacklistSubnets)
			{
				if (NetMatch(subnet, addr))
				{
					return true;
				}
			}
			return false;
		}

		/// <seealso cref="OnBlacklist(Java.Net.Inet4Address)"></seealso>
		private bool OnBlacklist(HttpExchange exchange)
		{
			if (!stanford)
			{
				return false;
			}
			InetAddress addr = exchange.GetRemoteAddress().GetAddress();
			if (addr is Inet4Address)
			{
				return OnBlacklist((Inet4Address)addr);
			}
			else
			{
				Redwood.Util.Log("Not checking IPv6 address against blacklist: " + addr);
				return false;
			}
		}

		/// <summary>A callback object that lets us hook into the result of an annotation request.</summary>
		public class FinishedRequest
		{
			public readonly Properties props;

			public readonly Annotation document;

			public readonly Optional<string> tokensregex;

			public readonly Optional<string> semgrex;

			public FinishedRequest(Properties props, Annotation document)
			{
				// TODO(gabor) we should eventually check ipv6 addresses too
				this.props = props;
				this.document = document;
				this.tokensregex = Optional.Empty();
				this.semgrex = Optional.Empty();
			}

			public FinishedRequest(Properties props, Annotation document, string tokensregex, string semgrex)
			{
				this.props = props;
				this.document = document;
				this.tokensregex = Optional.OfNullable(tokensregex);
				this.semgrex = Optional.OfNullable(semgrex);
			}
		}

		/// <summary>A simple ping test.</summary>
		/// <remarks>A simple ping test. Responds with pong.</remarks>
		protected internal class PingHandler : IHttpHandler
		{
			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				// Return a simple text message that says pong.
				httpExchange.GetResponseHeaders().Set("Content-type", "text/plain");
				string response = "pong\n";
				httpExchange.SendResponseHeaders(HttpOk, Sharpen.Runtime.GetBytesForString(response).Length);
				httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
				httpExchange.Close();
			}
		}

		/// <summary>A handler to let the caller know if the server is alive AND ready to respond to requests.</summary>
		/// <remarks>
		/// A handler to let the caller know if the server is alive AND ready to respond to requests.
		/// The canonical use-case for this is for Kubernetes readiness checks.
		/// </remarks>
		protected internal class ReadyHandler : IHttpHandler
		{
			/// <summary>If true, the server is running and ready for requests.</summary>
			public readonly AtomicBoolean serverReady;

			/// <summary>The creation time of this handler.</summary>
			/// <remarks>The creation time of this handler. This is used to tell the caller how long we've been waiting for.</remarks>
			public readonly long startTime;

			/// <summary>The trivial constructor.</summary>
			public ReadyHandler(AtomicBoolean serverReady)
			{
				this.serverReady = serverReady;
				this.startTime = Runtime.CurrentTimeMillis();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				// Return a simple text message that says pong.
				httpExchange.GetResponseHeaders().Set("Content-type", "text/plain");
				string response;
				int status;
				if (this.serverReady.Get())
				{
					response = "ready\n";
					status = HttpOk;
				}
				else
				{
					response = "server is not ready yet. uptime=" + Redwood.FormatTimeDifference(Runtime.CurrentTimeMillis() - this.startTime) + '\n';
					status = HttpUnavailable;
				}
				httpExchange.SendResponseHeaders(status, Sharpen.Runtime.GetBytesForString(response).Length);
				httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
				httpExchange.Close();
			}
		}

		/// <summary>
		/// A handler to let the caller know if the server is alive,
		/// but not necessarily ready to respond to requests.
		/// </summary>
		/// <remarks>
		/// A handler to let the caller know if the server is alive,
		/// but not necessarily ready to respond to requests.
		/// The canonical use-case for this is for Kubernetes liveness checks.
		/// </remarks>
		protected internal class LiveHandler : IHttpHandler
		{
			// end static class ReadyHandler
			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				// Return a simple text message that says pong.
				httpExchange.GetResponseHeaders().Set("Content-type", "text/plain");
				string response = "live\n";
				httpExchange.SendResponseHeaders(HttpOk, Sharpen.Runtime.GetBytesForString(response).Length);
				httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
				httpExchange.Close();
			}
		}

		/// <summary>Sending the appropriate shutdown key will gracefully shutdown the server.</summary>
		/// <remarks>
		/// Sending the appropriate shutdown key will gracefully shutdown the server.
		/// This key is, by default, saved into the local file /tmp/corenlp.shutdown on the
		/// machine the server was run from.
		/// </remarks>
		protected internal class ShutdownHandler : IHttpHandler
		{
			// end static class LiveHandler
			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				IDictionary<string, string> urlParams = StanfordCoreNLPServer.GetURLParams(httpExchange.GetRequestURI());
				httpExchange.GetResponseHeaders().Set("Content-type", "text/plain");
				bool doExit = false;
				string response = "Invalid shutdown key\n";
				if (urlParams.Contains("key") && urlParams["key"].Equals(this._enclosing.shutdownKey))
				{
					response = "Shutdown successful!\n";
					doExit = true;
				}
				httpExchange.SendResponseHeaders(HttpURLConnection.HttpOk, Sharpen.Runtime.GetBytesForString(response).Length);
				httpExchange.GetResponseBody().Write(Sharpen.Runtime.GetBytesForString(response));
				httpExchange.Close();
				if (doExit)
				{
					System.Environment.Exit(0);
				}
			}

			internal ShutdownHandler(StanfordCoreNLPServer _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly StanfordCoreNLPServer _enclosing;
		}

		/// <summary>Serve a file from the filesystem or classpath</summary>
		public class FileHandler : IHttpHandler
		{
			private readonly string content;

			private readonly string contentType;

			/// <exception cref="System.IO.IOException"/>
			public FileHandler(string fileOrClasspath)
				: this(fileOrClasspath, "text/html")
			{
			}

			/// <exception cref="System.IO.IOException"/>
			public FileHandler(string fileOrClasspath, string contentType)
			{
				// end static class ShutdownHandler
				using (BufferedReader r = IOUtils.ReaderFromString(fileOrClasspath, "utf-8"))
				{
					this.content = IOUtils.SlurpReader(r);
				}
				this.contentType = contentType + "; charset=utf-8";
			}

			// always encode in utf-8
			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				httpExchange.GetResponseHeaders().Set("Content-type", this.contentType);
				ByteBuffer buffer = Java.Nio.Charset.Charset.ForName("UTF-8").Encode(content);
				byte[] bytes = new byte[buffer.Remaining()];
				buffer.Get(bytes);
				httpExchange.SendResponseHeaders(HttpOk, bytes.Length);
				httpExchange.GetResponseBody().Write(bytes);
				httpExchange.Close();
			}
		}

		/// <summary>The main handler for taking an annotation request, and annotating it.</summary>
		protected internal class CoreNLPHandler : IHttpHandler
		{
			/// <summary>The default properties to use in the absence of anything sent by the client.</summary>
			public readonly Properties defaultProps;

			/// <summary>An authenticator to determine if we can perform this API request.</summary>
			private readonly IPredicate<Properties> authenticator;

			/// <summary>A callback to call when an annotation job has finished.</summary>
			private readonly IConsumer<StanfordCoreNLPServer.FinishedRequest> callback;

			private readonly StanfordCoreNLPServer.FileHandler homepage;

			/// <summary>Create a handler for accepting annotation requests.</summary>
			/// <param name="props">The properties file to use as the default if none were sent by the client.</param>
			public CoreNLPHandler(StanfordCoreNLPServer _enclosing, Properties props, IPredicate<Properties> authenticator, IConsumer<StanfordCoreNLPServer.FinishedRequest> callback, StanfordCoreNLPServer.FileHandler homepage)
			{
				this._enclosing = _enclosing;
				// end static class FileHandler
				this.defaultProps = props;
				this.callback = callback;
				this.authenticator = authenticator;
				this.homepage = homepage;
			}

			/// <summary>
			/// Get the response data type to send to the client, based off of the output format requested from
			/// CoreNLP.
			/// </summary>
			/// <param name="props">The properties being used by CoreNLP.</param>
			/// <param name="of">The output format being output by CoreNLP.</param>
			/// <returns>An identifier for the type of the HTTP response (e.g., 'text/json').</returns>
			public virtual string GetContentType(Properties props, StanfordCoreNLP.OutputFormat of)
			{
				switch (of)
				{
					case StanfordCoreNLP.OutputFormat.Json:
					{
						return "application/json";
					}

					case StanfordCoreNLP.OutputFormat.Text:
					case StanfordCoreNLP.OutputFormat.Conll:
					{
						return "text/plain";
					}

					case StanfordCoreNLP.OutputFormat.Xml:
					{
						return "text/xml";
					}

					case StanfordCoreNLP.OutputFormat.Serialized:
					{
						string outputSerializerName = props.GetProperty("outputSerializer");
						if (outputSerializerName != null && outputSerializerName.Equals(typeof(ProtobufAnnotationSerializer).FullName))
						{
							return "application/x-protobuf";
						}
						goto default;
					}

					default:
					{
						//noinspection fallthrough
						return "application/octet-stream";
					}
				}
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				if (this._enclosing.OnBlacklist(httpExchange))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				StanfordCoreNLPServer.SetHttpExchangeResponseHeaders(httpExchange);
				// Get sentence.
				Properties props;
				Annotation ann;
				StanfordCoreNLP.OutputFormat of;
				try
				{
					props = this._enclosing.GetProperties(httpExchange);
					if (Sharpen.Runtime.EqualsIgnoreCase("GET", httpExchange.GetRequestMethod()))
					{
						// Handle direct browser connections (i.e., not a POST request).
						this.homepage.Handle(httpExchange);
						return;
					}
					else
					{
						if (httpExchange.GetRequestMethod().Equals("HEAD"))
						{
							// attempt to handle issue #368; see http://bugs.java.com/bugdatabase/view_bug.do?bug_id=6886723
							httpExchange.GetRequestBody().Close();
							httpExchange.GetResponseHeaders().Add("Transfer-encoding", "chunked");
							httpExchange.SendResponseHeaders(200, -1);
							httpExchange.Close();
							return;
						}
						else
						{
							// Handle API request
							if (this.authenticator != null && !this.authenticator.Test(props))
							{
								StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
								return;
							}
							if (!this._enclosing.quiet)
							{
								Redwood.Util.Log("[" + httpExchange.GetRemoteAddress() + "] API call w/annotators " + props.GetProperty("annotators", "<unknown>"));
							}
							ann = this._enclosing.GetDocument(props, httpExchange);
							of = StanfordCoreNLP.OutputFormat.ValueOf(props.GetProperty("outputFormat", "json").ToUpper());
							string text = ann.Get(typeof(CoreAnnotations.TextAnnotation)).Replace('\n', ' ');
							if (!this._enclosing.quiet)
							{
								System.Console.Out.WriteLine(text);
							}
							if (StanfordCoreNLPServer.maxCharLength > 0 && text.Length > StanfordCoreNLPServer.maxCharLength)
							{
								StanfordCoreNLPServer.RespondBadInput("Request is too long to be handled by server: " + text.Length + " characters. Max length is " + StanfordCoreNLPServer.maxCharLength + " characters.", httpExchange);
								return;
							}
						}
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					StanfordCoreNLPServer.RespondError("Could not handle incoming annotation", httpExchange);
					return;
				}
				IFuture<Annotation> completedAnnotationFuture = null;
				try
				{
					// Annotate
					StanfordCoreNLP pipeline = this._enclosing.MkStanfordCoreNLP(props);
					completedAnnotationFuture = this._enclosing.corenlpExecutor.Submit(null);
					Annotation completedAnnotation;
					int timeoutMilliseconds;
					try
					{
						timeoutMilliseconds = System.Convert.ToInt32(props.GetProperty("timeout", int.ToString(this._enclosing.timeoutMilliseconds)));
						timeoutMilliseconds = this.MaybeAlterStanfordTimeout(httpExchange, timeoutMilliseconds);
					}
					catch (NumberFormatException)
					{
						timeoutMilliseconds = this._enclosing.timeoutMilliseconds;
					}
					completedAnnotation = completedAnnotationFuture.Get(timeoutMilliseconds, TimeUnit.Milliseconds);
					completedAnnotationFuture = null;
					// No longer any need for the future
					// Get output
					ByteArrayOutputStream os = new ByteArrayOutputStream();
					AnnotationOutputter.Options options = AnnotationOutputter.GetOptions(pipeline);
					StanfordCoreNLP.CreateOutputter(props, options).Accept(completedAnnotation, os);
					os.Close();
					byte[] response = os.ToByteArray();
					string contentType = this.GetContentType(props, of);
					if (contentType.Equals("application/json") || contentType.StartsWith("text/"))
					{
						contentType += ";charset=" + options.encoding;
					}
					httpExchange.GetResponseHeaders().Add("Content-type", contentType);
					httpExchange.GetResponseHeaders().Add("Content-length", int.ToString(response.Length));
					httpExchange.SendResponseHeaders(HttpURLConnection.HttpOk, response.Length);
					httpExchange.GetResponseBody().Write(response);
					httpExchange.Close();
					if (completedAnnotation != null && !StringUtils.IsNullOrEmpty(props.GetProperty("annotators")))
					{
						this.callback.Accept(new StanfordCoreNLPServer.FinishedRequest(props, completedAnnotation));
					}
				}
				catch (TimeoutException e)
				{
					// Print the stack trace for debugging
					Sharpen.Runtime.PrintStackTrace(e);
					// Return error message.
					StanfordCoreNLPServer.RespondError("CoreNLP request timed out. Your document may be too long.", httpExchange);
					// Cancel the future if it's alive
					//noinspection ConstantConditions
					if (completedAnnotationFuture != null)
					{
						completedAnnotationFuture.Cancel(true);
					}
				}
				catch (Exception e)
				{
					// Print the stack trace for debugging
					Sharpen.Runtime.PrintStackTrace(e);
					// Return error message.
					StanfordCoreNLPServer.RespondError(e.GetType().FullName + ": " + e.Message, httpExchange);
					// Cancel the future if it's alive
					//noinspection ConstantConditions
					if (completedAnnotationFuture != null)
					{
						// just in case...
						completedAnnotationFuture.Cancel(true);
					}
				}
			}

			private int MaybeAlterStanfordTimeout(HttpExchange httpExchange, int timeoutMilliseconds)
			{
				if (!this._enclosing.stanford)
				{
					return timeoutMilliseconds;
				}
				try
				{
					// Check for too long a timeout from an unauthorized source
					if (timeoutMilliseconds > 15000)
					{
						// If two conditions:
						//   (1) The server is running on corenlp.run (i.e., corenlp.stanford.edu)
						//   (2) The request is not coming from a *.stanford.edu" email address
						// Then force the timeout to be 15 seconds
						if ("corenlp.stanford.edu".Equals(InetAddress.GetLocalHost().GetHostName()) && !httpExchange.GetRemoteAddress().GetHostName().ToLower().EndsWith("stanford.edu"))
						{
							timeoutMilliseconds = 15000;
						}
					}
					return timeoutMilliseconds;
				}
				catch (UnknownHostException)
				{
					return timeoutMilliseconds;
				}
			}

			private readonly StanfordCoreNLPServer _enclosing;
		}

		/// <summary>A handler for matching TokensRegex patterns against text.</summary>
		protected internal class TokensRegexHandler : IHttpHandler
		{
			/// <summary>A callback to call when an annotation job has finished.</summary>
			private readonly IConsumer<StanfordCoreNLPServer.FinishedRequest> callback;

			/// <summary>An authenticator to determine if we can perform this API request.</summary>
			private readonly IPredicate<Properties> authenticator;

			/// <summary>Create a new TokensRegex Handler.</summary>
			/// <param name="callback">The callback to call when annotation has finished.</param>
			public TokensRegexHandler(StanfordCoreNLPServer _enclosing, IPredicate<Properties> authenticator, IConsumer<StanfordCoreNLPServer.FinishedRequest> callback)
			{
				this._enclosing = _enclosing;
				// end class CoreNLPHandler
				this.callback = callback;
				this.authenticator = authenticator;
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				if (this._enclosing.OnBlacklist(httpExchange))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				StanfordCoreNLPServer.SetHttpExchangeResponseHeaders(httpExchange);
				Properties props = this._enclosing.GetProperties(httpExchange);
				if (this.authenticator != null && !this.authenticator.Test(props))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				IDictionary<string, string> @params = StanfordCoreNLPServer.GetURLParams(httpExchange.GetRequestURI());
				IFuture<Pair<string, Annotation>> future = this._enclosing.corenlpExecutor.Submit(null);
				// Get the document
				// Construct the matcher
				// (get the pattern)
				// (get whether to filter / find)
				// (create the matcher)
				// Run TokensRegex
				// Case: just filter sentences
				// Case: find matches
				// Send response
				try
				{
					int tokensRegexTimeOut = (this._enclosing.lastPipeline.Get() == null) ? 75 : 5;
					Pair<string, Annotation> response = future.Get(tokensRegexTimeOut, TimeUnit.Seconds);
					byte[] content = Sharpen.Runtime.GetBytesForString(response.first);
					Annotation completedAnnotation = response.second;
					StanfordCoreNLPServer.SendAndGetResponse(httpExchange, content);
					if (completedAnnotation != null && !StringUtils.IsNullOrEmpty(props.GetProperty("annotators")))
					{
						this.callback.Accept(new StanfordCoreNLPServer.FinishedRequest(props, completedAnnotation, @params["pattern"], null));
					}
				}
				catch (Exception)
				{
					StanfordCoreNLPServer.RespondError("Timeout when executing TokensRegex query", httpExchange);
				}
			}

			private readonly StanfordCoreNLPServer _enclosing;
		}

		/// <summary>A handler for matching semgrex patterns against dependency trees.</summary>
		protected internal class SemgrexHandler : IHttpHandler
		{
			/// <summary>A callback to call when an annotation job has finished.</summary>
			private readonly IConsumer<StanfordCoreNLPServer.FinishedRequest> callback;

			/// <summary>An authenticator to determine if we can perform this API request.</summary>
			private readonly IPredicate<Properties> authenticator;

			/// <summary>Create a new Semgrex Handler.</summary>
			/// <param name="callback">The callback to call when annotation has finished.</param>
			public SemgrexHandler(StanfordCoreNLPServer _enclosing, IPredicate<Properties> authenticator, IConsumer<StanfordCoreNLPServer.FinishedRequest> callback)
			{
				this._enclosing = _enclosing;
				this.callback = callback;
				this.authenticator = authenticator;
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				if (this._enclosing.OnBlacklist(httpExchange))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				StanfordCoreNLPServer.SetHttpExchangeResponseHeaders(httpExchange);
				Properties props = this._enclosing.GetProperties(httpExchange);
				if (this.authenticator != null && !this.authenticator.Test(props))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				IDictionary<string, string> @params = StanfordCoreNLPServer.GetURLParams(httpExchange.GetRequestURI());
				IFuture<Pair<string, Annotation>> response = this._enclosing.corenlpExecutor.Submit(null);
				// Get the document
				// Construct the matcher
				// (get the pattern)
				// (get whether to filter / find)
				// (in case of find, get whether to only keep unique matches)
				// (create the matcher)
				// Run Semgrex
				// Case: just filter sentences
				// Case: find matches
				// Case: find either next node or next unique node
				// Send response
				try
				{
					int semgrexTimeOut = (this._enclosing.lastPipeline.Get() == null) ? 75 : 5;
					Pair<string, Annotation> pair = response.Get(semgrexTimeOut, TimeUnit.Seconds);
					Annotation completedAnnotation = pair.second;
					byte[] content = Sharpen.Runtime.GetBytesForString(pair.first);
					StanfordCoreNLPServer.SendAndGetResponse(httpExchange, content);
					if (completedAnnotation != null && !StringUtils.IsNullOrEmpty(props.GetProperty("annotators")))
					{
						this.callback.Accept(new StanfordCoreNLPServer.FinishedRequest(props, completedAnnotation, @params["pattern"], null));
					}
				}
				catch (Exception)
				{
					StanfordCoreNLPServer.RespondError("Timeout when executing Semgrex query", httpExchange);
				}
			}

			private readonly StanfordCoreNLPServer _enclosing;
		}

		/// <summary>A handler for matching tregrex patterns against dependency trees.</summary>
		protected internal class TregexHandler : IHttpHandler
		{
			/// <summary>A callback to call when an annotation job has finished.</summary>
			private readonly IConsumer<StanfordCoreNLPServer.FinishedRequest> callback;

			/// <summary>An authenticator to determine if we can perform this API request.</summary>
			private readonly IPredicate<Properties> authenticator;

			/// <summary>Create a new Tregex Handler.</summary>
			/// <param name="callback">The callback to call when annotation has finished.</param>
			public TregexHandler(StanfordCoreNLPServer _enclosing, IPredicate<Properties> authenticator, IConsumer<StanfordCoreNLPServer.FinishedRequest> callback)
			{
				this._enclosing = _enclosing;
				this.callback = callback;
				this.authenticator = authenticator;
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Handle(HttpExchange httpExchange)
			{
				if (this._enclosing.OnBlacklist(httpExchange))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				StanfordCoreNLPServer.SetHttpExchangeResponseHeaders(httpExchange);
				Properties props = this._enclosing.GetProperties(httpExchange);
				if (this.authenticator != null && !this.authenticator.Test(props))
				{
					StanfordCoreNLPServer.RespondUnauthorized(httpExchange);
					return;
				}
				IDictionary<string, string> @params = StanfordCoreNLPServer.GetURLParams(httpExchange.GetRequestURI());
				IFuture<Pair<string, Annotation>> response = this._enclosing.corenlpExecutor.Submit(null);
				// Get the document
				// Construct the matcher
				// (get the pattern)
				// (create the matcher)
				// Run Tregex
				//sentWriter.set("tree", tree.pennString());
				// Send response
				try
				{
					int tregexTimeOut = (this._enclosing.lastPipeline.Get() == null) ? 75 : 5;
					Pair<string, Annotation> pair = response.Get(tregexTimeOut, TimeUnit.Seconds);
					Annotation completedAnnotation = pair.second;
					byte[] content = Sharpen.Runtime.GetBytesForString(pair.first);
					StanfordCoreNLPServer.SendAndGetResponse(httpExchange, content);
					if (completedAnnotation != null && !StringUtils.IsNullOrEmpty(props.GetProperty("annotators")))
					{
						this.callback.Accept(new StanfordCoreNLPServer.FinishedRequest(props, completedAnnotation, @params["pattern"], null));
					}
				}
				catch (Exception)
				{
					StanfordCoreNLPServer.RespondError("Timeout when executing Tregex query", httpExchange);
				}
			}

			private readonly StanfordCoreNLPServer _enclosing;
		}

		/// <exception cref="System.IO.IOException"/>
		private static void SendAndGetResponse(HttpExchange httpExchange, byte[] response)
		{
			if (response.Length > 0)
			{
				httpExchange.GetResponseHeaders().Add("Content-type", "application/json");
				httpExchange.GetResponseHeaders().Add("Content-length", int.ToString(response.Length));
				httpExchange.SendResponseHeaders(HttpOk, response.Length);
				httpExchange.GetResponseBody().Write(response);
				httpExchange.Close();
			}
		}

		private static HttpsServer AddSSLContext(HttpsServer server)
		{
			Redwood.Util.Log("Adding SSL context to server; key=" + StanfordCoreNLPServer.key);
			try
			{
				using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(key))
				{
					KeyStore ks = KeyStore.GetInstance("JKS");
					if (StanfordCoreNLPServer.key != null && IOUtils.ExistsInClasspathOrFileSystem(StanfordCoreNLPServer.key))
					{
						ks.Load(@is, "corenlp".ToCharArray());
					}
					else
					{
						throw new ArgumentException("Could not find SSL keystore at " + StanfordCoreNLPServer.key);
					}
					KeyManagerFactory kmf = KeyManagerFactory.GetInstance("SunX509");
					kmf.Init(ks, "corenlp".ToCharArray());
					SSLContext sslContext = SSLContext.GetInstance("TLS");
					sslContext.Init(kmf.GetKeyManagers(), null, null);
					// Add SSL support to the server
					server.SetHttpsConfigurator(new _HttpsConfigurator_1322(sslContext));
					// Return
					return server;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		private sealed class _HttpsConfigurator_1322 : HttpsConfigurator
		{
			public _HttpsConfigurator_1322(SSLContext baseArg1)
				: base(baseArg1)
			{
			}

			public override void Configure(HttpsParameters @params)
			{
				SSLContext context = this.GetSSLContext();
				SSLEngine engine = context.CreateSSLEngine();
				@params.SetNeedClientAuth(false);
				@params.SetCipherSuites(engine.GetEnabledCipherSuites());
				@params.SetProtocols(engine.GetEnabledProtocols());
				@params.SetSSLParameters(context.GetDefaultSSLParameters());
			}
		}

		/// <summary>
		/// If we have a separate liveness port, start a server on a separate thread pool whose only
		/// job is to watch for when the CoreNLP server becomes ready.
		/// </summary>
		/// <remarks>
		/// If we have a separate liveness port, start a server on a separate thread pool whose only
		/// job is to watch for when the CoreNLP server becomes ready.
		/// This will also immediately signal liveness.
		/// </remarks>
		/// <param name="live">
		/// The boolean to track when CoreNLP has initialized and the server is ready
		/// to serve requests.
		/// </param>
		private void LivenessServer(AtomicBoolean live)
		{
			if (this.serverPort != this.statusPort)
			{
				try
				{
					// Create the server
					if (this.ssl)
					{
						server = AddSSLContext(((HttpsServer)HttpsServer.Create(new InetSocketAddress(statusPort), 0)));
					}
					else
					{
						// 0 is the default 'backlog'
						server = HttpServer.Create(new InetSocketAddress(statusPort), 0);
					}
					// 0 is the default 'backlog'
					// Add the two status endpoints
					WithAuth(server.CreateContext("/live", new StanfordCoreNLPServer.LiveHandler()), Optional.Empty());
					WithAuth(server.CreateContext("/ready", new StanfordCoreNLPServer.ReadyHandler(live)), Optional.Empty());
					// Start the server
					server.Start();
					// Server started
					Redwood.Util.Log("Liveness server started at " + server.GetAddress());
				}
				catch (IOException e)
				{
					Redwood.Util.Err("Could not start liveness server. This will probably result in very bad things happening soon.", e);
				}
			}
		}

		/// <summary>Returns the implementing Http server.</summary>
		public virtual Optional<HttpServer> GetServer()
		{
			return Optional.OfNullable(server);
		}

		/// <seealso cref="Run(Java.Util.Optional{T}, Java.Util.Function.IPredicate{T}, Java.Util.Function.IConsumer{T}, FileHandler, bool, Java.Util.Concurrent.Atomic.AtomicBoolean)"></seealso>
		public virtual void Run()
		{
			// Set the static page handler
			try
			{
				AtomicBoolean live = new AtomicBoolean(false);
				this.LivenessServer(live);
				StanfordCoreNLPServer.FileHandler homepage = new StanfordCoreNLPServer.FileHandler("edu/stanford/nlp/pipeline/demo/corenlp-brat.html");
				Run(Optional.Empty(), null, null, homepage, false, live);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Enable authentication for this endpoint</summary>
		/// <param name="context">The context to enable authentication for.</param>
		/// <param name="credentials">The optional credentials to enforce. This is a (key,value) pair</param>
		private static void WithAuth(HttpContext context, Optional<Pair<string, string>> credentials)
		{
			credentials.IfPresent(null);
		}

		/// <summary>Run the server.</summary>
		/// <remarks>
		/// Run the server.
		/// This method registers the handlers, and initializes the HTTP server.
		/// </remarks>
		public virtual void Run(Optional<Pair<string, string>> basicAuth, IPredicate<Properties> authenticator, IConsumer<StanfordCoreNLPServer.FinishedRequest> callback, StanfordCoreNLPServer.FileHandler homepage, bool https, AtomicBoolean live)
		{
			try
			{
				if (https)
				{
					server = AddSSLContext(((HttpsServer)HttpsServer.Create(new InetSocketAddress(serverPort), 0)));
				}
				else
				{
					// 0 is the default 'backlog'
					server = HttpServer.Create(new InetSocketAddress(serverPort), 0);
				}
				// 0 is the default 'backlog'
				string contextRoot = uriContext;
				if (contextRoot.IsEmpty())
				{
					contextRoot = "/";
				}
				WithAuth(server.CreateContext(contextRoot, new StanfordCoreNLPServer.CoreNLPHandler(this, defaultProps, authenticator, callback, homepage)), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/tokensregex", new StanfordCoreNLPServer.TokensRegexHandler(this, authenticator, callback)), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/semgrex", new StanfordCoreNLPServer.SemgrexHandler(this, authenticator, callback)), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/tregex", new StanfordCoreNLPServer.TregexHandler(this, authenticator, callback)), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/corenlp-brat.js", new StanfordCoreNLPServer.FileHandler("edu/stanford/nlp/pipeline/demo/corenlp-brat.js", "application/javascript")), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/corenlp-brat.cs", new StanfordCoreNLPServer.FileHandler("edu/stanford/nlp/pipeline/demo/corenlp-brat.css", "text/css")), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/corenlp-parseviewer.js", new StanfordCoreNLPServer.FileHandler("edu/stanford/nlp/pipeline/demo/corenlp-parseviewer.js", "application/javascript")), basicAuth);
				WithAuth(server.CreateContext(uriContext + "/ping", new StanfordCoreNLPServer.PingHandler()), Optional.Empty());
				WithAuth(server.CreateContext(uriContext + "/shutdown", new StanfordCoreNLPServer.ShutdownHandler(this)), basicAuth);
				if (this.serverPort == this.statusPort)
				{
					WithAuth(server.CreateContext(uriContext + "/live", new StanfordCoreNLPServer.LiveHandler()), Optional.Empty());
					WithAuth(server.CreateContext(uriContext + "/ready", new StanfordCoreNLPServer.ReadyHandler(live)), Optional.Empty());
				}
				server.SetExecutor(serverExecutor);
				server.Start();
				live.Set(true);
				Redwood.Util.Log("StanfordCoreNLPServer listening at " + server.GetAddress());
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>The main method.</summary>
		/// <remarks>
		/// The main method.
		/// Read the command line arguments and run the server.
		/// </remarks>
		/// <param name="args">The command line arguments</param>
		/// <exception cref="System.IO.IOException">Thrown if we could not start / run the server.</exception>
		public static void Main(string[] args)
		{
			// Add a bit of logging
			Redwood.Util.Log("--- " + typeof(StanfordCoreNLPServer).GetSimpleName() + "#main() called ---");
			string build = Runtime.Getenv("BUILD");
			if (build != null)
			{
				Redwood.Util.Log("    Build: " + build);
			}
			Runtime.GetRuntime().AddShutdownHook(new Thread(null));
			// Fill arguments
			ArgumentParser.FillOptions(typeof(StanfordCoreNLPServer), args);
			// get server properties from command line, right now only property used is server_id
			Properties serverProperties = StringUtils.ArgsToProperties(args);
			StanfordCoreNLPServer server = new StanfordCoreNLPServer(serverProperties);
			// must come after filling global options
			ArgumentParser.FillOptions(server, args);
			// align status port and server port in case status port hasn't been set and
			// server port is not the default 9000
			if (serverProperties != null && !serverProperties.Contains("status_port") && serverProperties.Contains("port"))
			{
				server.statusPort = System.Convert.ToInt32(serverProperties.GetProperty("port"));
			}
			Redwood.Util.Log("    Threads: " + ArgumentParser.threads);
			// Start the liveness server
			AtomicBoolean live = new AtomicBoolean(false);
			server.LivenessServer(live);
			// Create the homepage
			StanfordCoreNLPServer.FileHandler homepage;
			try
			{
				homepage = new StanfordCoreNLPServer.FileHandler("edu/stanford/nlp/pipeline/demo/corenlp-brat.html");
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			// Pre-load the models
			if (StanfordCoreNLPServer.preloadedAnnotators != null && !StanfordCoreNLPServer.preloadedAnnotators.Trim().IsEmpty())
			{
				Properties props = new Properties();
				server.defaultProps.ForEach(null);
				props.SetProperty("annotators", StanfordCoreNLPServer.preloadedAnnotators);
				try
				{
					new StanfordCoreNLP(props);
				}
				catch (Exception ignored)
				{
					Redwood.Util.Err("Could not pre-load annotators in server; encountered exception:");
					Sharpen.Runtime.PrintStackTrace(ignored);
				}
			}
			// Credentials
			Optional<Pair<string, string>> credentials = Optional.Empty();
			if (server.username != null && server.password != null)
			{
				credentials = Optional.Of(Pair.MakePair(server.username, server.password));
			}
			// Run the server
			Redwood.Util.Log("Starting server...");
			if (server.ssl)
			{
				server.Run(credentials, null, null, homepage, true, live);
			}
			else
			{
				server.Run(credentials, null, null, homepage, false, live);
			}
		}
		// end main()
	}
}
