using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>A common base class for annotators that talk to other web servers.</summary>
	/// <remarks>
	/// A common base class for annotators that talk to other web servers.
	/// The important things to do to implement this is:
	/// <ol>
	/// <li>Implement
	/// <see cref="AnnotateImpl(Annotation)"/>
	/// with the code to actually call the server.</li>
	/// <li>Implement
	/// <see cref="Ready(bool)"/>
	/// with code to check if the server is available.
	/// <see cref="Ping(string)"/>
	/// may be useful for this.</li>
	/// <li>Optionally implement
	/// <see cref="StartCommand()"/>
	/// with a command to start a local server. If this is specified, we will start
	/// a local server before we start checking for readiness.
	/// Note that the
	/// <see cref="Ready(bool)"/>
	/// endpoint does still have to point to this local server in that case, or else
	/// lifecycle won't be managed properly.
	/// </li>
	/// </ol>
	/// </remarks>
	/// <author><a href="mailto:gabor@eloquent.ai">Gabor Angeli</a></author>
	public abstract class WebServiceAnnotator : IAnnotator
	{
		/// <summary>A logger from this class.</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(WebServiceAnnotator));

		/// <summary>A timeout to wait for a server to boot up.</summary>
		/// <remarks>A timeout to wait for a server to boot up. Beyond this, we simply give up and throw an exception.</remarks>
		private static long ConnectTimeout = Duration.OfMinutes(15).ToMillis();

		/// <summary>Thrown if we could not annotate, but there's hope to either reconnect or restart the server.</summary>
		/// <remarks>
		/// Thrown if we could not annotate, but there's hope to either reconnect or restart the server.
		/// Will still only try to connect 3 times.
		/// This is the usual exception.
		/// </remarks>
		[System.Serializable]
		public class ShouldRetryException : Exception
		{
			private const long serialVersionUID = -4292922700733296864L;

			public ShouldRetryException()
			{
			}
		}

		/// <summary>An exception thrown if we could not connect to the server, and shouldn't retry / recreate the server.</summary>
		[System.Serializable]
		public class PermanentlyFailedException : Exception
		{
			private const long serialVersionUID = 6812811056236924923L;

			public PermanentlyFailedException()
			{
			}

			public PermanentlyFailedException(Exception t)
				: base(t)
			{
			}
		}

		/// <summary>A class encapsulating a running server process.</summary>
		private class RunningProcess
		{
			/// <summary>The actual running process.</summary>
			public readonly Process process;

			/// <summary>The output stream gobbler, redirecting the stream to stdout.</summary>
			public readonly StreamGobbler stdout;

			/// <summary>The error stream gobbler, redirecting the stream to stderr.</summary>
			public readonly StreamGobbler stderr;

			/// <summary>If true, the server is presumed ready to accept connections.</summary>
			public bool ready = false;

			/// <summary>A shutdown hook to clean up this process on shutdown.</summary>
			private readonly Thread shutdownHoook;

			/// <summary>A straightforward constructor.</summary>
			private RunningProcess(WebServiceAnnotator _enclosing, Process process)
			{
				this._enclosing = _enclosing;
				this.process = process;
				TextWriter errWriter = new BufferedWriter(new OutputStreamWriter(System.Console.Error));
				this.stderr = new StreamGobbler(process.GetErrorStream(), errWriter);
				this.stderr.Start();
				TextWriter outWriter = new BufferedWriter(new OutputStreamWriter(System.Console.Out));
				this.stdout = new StreamGobbler(process.GetErrorStream(), outWriter);
				this.stdout.Start();
				this.shutdownHoook = new Thread(null);
				Runtime.GetRuntime().AddShutdownHook(this.shutdownHoook);
			}

			/// <summary>Kills this process, and kills the stream gobblers waiting on it.</summary>
			public virtual void Kill()
			{
				Runtime.GetRuntime().RemoveShutdownHook(this.shutdownHoook);
				this.shutdownHoook.Run();
			}

			~RunningProcess()
			{
				try
				{
					base.Finalize();
				}
				finally
				{
					this.Kill();
				}
			}

			private readonly WebServiceAnnotator _enclosing;
		}

		/// <summary>If true, we have connected to the server at some point.</summary>
		protected internal bool everLive = false;

		/// <summary>If true, the server was active last time checked</summary>
		protected internal bool serverWasActive = false;

		/// <summary>The running server, if any.</summary>
		private Optional<WebServiceAnnotator.RunningProcess> server = Optional.Empty();

		/// <summary>The command to run to start the server, if any.</summary>
		/// <remarks>
		/// The command to run to start the server, if any.
		/// If no command is given, we assume it's being managed by someone else (e.g., an external
		/// running service).
		/// </remarks>
		/// <returns>
		/// The command we should start, or
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// if we don't want CoreNLP
		/// to manage the server.
		/// </returns>
		protected internal abstract Optional<string[]> StartCommand();

		/// <summary>An optional command provided to run to shut down the server.</summary>
		protected internal abstract Optional<string[]> StopCommand();

		/// <summary>Check if the server is ready to accept annotations.</summary>
		/// <remarks>
		/// Check if the server is ready to accept annotations.
		/// This client will wait until the ready endpoint returns true.
		/// </remarks>
		/// <param name="initialTest">testing a server that has just been started?</param>
		/// <returns>True if the server is ready to accept documents to annotate.</returns>
		protected internal abstract bool Ready(bool initialTest);

		/// <summary>Actually annotate a document with the server.</summary>
		/// <param name="ann">The document to annotate.</param>
		/// <exception cref="ShouldRetryException">Thrown if we could not annotate the document, but we could plausibly retry.</exception>
		/// <exception cref="PermanentlyFailedException">Thrown if we could not annotate the document and should not retry.</exception>
		/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.ShouldRetryException"/>
		/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.PermanentlyFailedException"/>
		protected internal abstract void AnnotateImpl(Annotation ann);

		/// <summary>Check if the server is live.</summary>
		/// <remarks>
		/// Check if the server is live. Can be overwritten if it differs from
		/// <see cref="Ready(bool)"/>
		/// .
		/// </remarks>
		/// <returns>True if the server is live.</returns>
		protected internal virtual bool Live()
		{
			return true;
		}

		/// <summary>A utility to ping an endpoint.</summary>
		/// <remarks>
		/// A utility to ping an endpoint. Useful for
		/// <see cref="Live()"/>
		/// and
		/// <see cref="Ready(bool)"/>
		/// .
		/// </remarks>
		/// <param name="uri">The URL we are trying to ping.</param>
		/// <returns>True if we got any non-5XX response from the endpoint.</returns>
		protected internal virtual bool Ping(string uri)
		{
			try
			{
				URL url = new URL(uri);
				HttpURLConnection connection = (HttpURLConnection)url.OpenConnection();
				connection.SetRequestProperty("Accept-Charset", "UTF-8");
				connection.SetRequestMethod("GET");
				connection.Connect();
				int code = connection.GetResponseCode();
				return code < 500 || code >= 600;
			}
			catch (MalformedURLException)
			{
				log.Warn("Could not parse URL: " + uri);
				return false;
			}
			catch (InvalidCastException)
			{
				log.Warn("Not an HTTP URI");
				return false;
			}
			catch (IOException)
			{
				return false;
			}
		}

		/// <summary>Start the actual server.</summary>
		/// <param name="command">the command we are using to start the sever.</param>
		/// <returns>True if the server was started; false otherwise.</returns>
		private bool StartServer(string[] command)
		{
			ProcessBuilder proc = new ProcessBuilder(command);
			try
			{
				lock (this)
				{
					this.server = Optional.Of(new WebServiceAnnotator.RunningProcess(this, proc.Start()));
				}
				log.Info("Started server " + StringUtils.Join(command));
				return true;
			}
			catch (IOException)
			{
				log.Error("Could not start process: " + StringUtils.Join(command));
				return false;
			}
		}

		/// <summary>Ensure that the server we're trying to connect to exists.</summary>
		/// <remarks>
		/// Ensure that the server we're trying to connect to exists.
		/// This is certainly called from
		/// <see cref="Annotate(Annotation)"/>
		/// , but can also
		/// be called from the constructor of the annotator to cache startup times.
		/// </remarks>
		/// <exception cref="Java.Util.Concurrent.TimeoutException">Thrown if we could not connect to the server for the timeout period.</exception>
		/// <exception cref="System.IO.IOException">Thrown if we could not start the server process.</exception>
		protected internal virtual void EnsureServer()
		{
			long startTime = Runtime.CurrentTimeMillis();
			// if the server was active last time we checked, see if the server is still active
			if (serverWasActive)
			{
				if (Ready(false))
				{
					return;
				}
			}
			// 1. Start a server, if applicable
			bool serverStarted = StartCommand().Map(null).OrElse(true);
			if (!serverStarted)
			{
				throw new IOException("Could not start a local server!");
			}
			// 2. Wait for the target server to come online
			while (!everLive)
			{
				if (Runtime.CurrentTimeMillis() > startTime + ConnectTimeout)
				{
					throw new TimeoutException("Could not connect to annotator: " + this);
				}
				if (!Live())
				{
					try
					{
						Thread.Sleep(1000);
					}
					catch (Exception)
					{
					}
				}
				else
				{
					everLive = true;
				}
			}
			log.Info("Got liveness from server for " + this);
			// 3. Wait for the target server to become ready
			lock (this)
			{
				if (this.server.IsPresent())
				{
					while (!this.server.Get().ready)
					{
						if (Runtime.CurrentTimeMillis() > startTime + ConnectTimeout)
						{
							throw new TimeoutException("Never got readiness from annotator: " + this);
						}
						if (!Ready(true))
						{
							try
							{
								Thread.Sleep(1000);
							}
							catch (Exception)
							{
							}
						}
						else
						{
							this.server.Get().ready = true;
						}
					}
				}
				else
				{
					if (!Ready(false))
					{
						// The server is not ready
						throw new IOException("Server is not ready and can not start it!");
					}
				}
			}
			log.Info("Got readiness from server for " + this);
			serverWasActive = true;
		}

		// 4. Server is ensured! We can continue
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void Unmount()
		{
			log.Info("Unmounting server: " + this);
			lock (this)
			{
				if (this.server.IsPresent())
				{
					this.server.Get().Kill();
					this.server = Optional.Empty();
				}
				// run optional stop script
				try
				{
					if (StopCommand().IsPresent())
					{
						ProcessBuilder proc = new ProcessBuilder(StopCommand().Get());
						proc.Start();
					}
				}
				catch (Exception)
				{
					log.Error("Error: problem with running stop command for WebServiceAnnotator");
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void Annotate(Annotation annotation)
		{
			Annotate(annotation, 0);
		}

		/// <summary>
		/// The actual implementation of
		/// <see cref="IAnnotator.Annotate(Annotation)"/>
		/// .
		/// This calls
		/// <see cref="AnnotateImpl(Annotation)"/>
		/// , which should actually make the server calls.
		/// This method just handles starting/stopping the server, and waiting for readiness
		/// </summary>
		/// <param name="annotation">The annotation to annotate.</param>
		/// <param name="tries">The number of times we have tried to annotate this document.</param>
		private void Annotate(Annotation annotation, int tries)
		{
			try
			{
				// 1. Ensure that we have a server to annotate against
				lock (this)
				{
					EnsureServer();
				}
				try
				{
					// 2. Annotate the document
					AnnotateImpl(annotation);
				}
				catch (WebServiceAnnotator.PermanentlyFailedException e)
				{
					// 3A. We've failed to annotate. Give up
					// 3A.1. Stop the server
					lock (this)
					{
						if (this.server.IsPresent())
						{
							this.server.Get().Kill();
							this.server = Optional.Empty();
						}
					}
					// 3A.1. Throw an exception
					Exception cause = e.InnerException;
					if (cause != null && cause is Exception)
					{
						throw (Exception)cause;
					}
					else
					{
						if (cause != null)
						{
							throw new Exception(cause);
						}
						else
						{
							throw new Exception(e);
						}
					}
				}
				catch (WebServiceAnnotator.ShouldRetryException e)
				{
					// 3B. We've failed to annotate, but should maybe retry
					// 3B.1. Stop the server, if this is our third try
					lock (this)
					{
						if (tries >= 2 && this.server.IsPresent())
						{
							this.server.Get().Kill();
							this.server = Optional.Empty();
						}
					}
					// 3B.2. Retry
					if (tries < 3)
					{
						Annotate(annotation, tries + 1);
					}
					else
					{
						throw new Exception("Could not annotate document after 3 tries:", e);
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception("Could not ensure a server:", e);
			}
		}

		/// <summary>A quick script to debug server lifecycle.</summary>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			WebServiceAnnotator annotator = new _WebServiceAnnotator_394();
			Annotation ann = new Annotation(string.Empty);
			annotator.Annotate(ann);
		}

		private sealed class _WebServiceAnnotator_394 : WebServiceAnnotator
		{
			public _WebServiceAnnotator_394()
			{
			}

			public override ICollection<Type> RequirementsSatisfied()
			{
				return Java.Util.Collections.EmptySet();
			}

			public override ICollection<Type> Requires()
			{
				return Java.Util.Collections.EmptySet();
			}

			protected internal override Optional<string[]> StartCommand()
			{
				return Optional.Of(new string[] { "bash", "script.sh" });
			}

			protected internal override Optional<string[]> StopCommand()
			{
				return Optional.Empty();
			}

			protected internal override bool Ready(bool initialTest)
			{
				return this.Ping("http://localhost:8000");
			}

			/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.ShouldRetryException"/>
			/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.PermanentlyFailedException"/>
			protected internal override void AnnotateImpl(Annotation ann)
			{
				WebServiceAnnotator.log.Info("Fake annotated! ping=" + this.Ping("http://localhost:8000"));
			}

			public override string ToString()
			{
				return "<test WebServiceAnnotator>";
			}
		}

		public abstract ICollection<Type> RequirementsSatisfied();

		public abstract ICollection<Type> Requires();
	}
}
