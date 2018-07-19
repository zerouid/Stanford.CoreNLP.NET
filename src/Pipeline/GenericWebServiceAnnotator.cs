using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Net;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// A simple web service annotator that can be defined through properties:
	/// annotatorEndpoint: a URL endpoint for annotator the service
	/// annotatorRequires: Requirements for the annotator
	/// annotatorProvides: Annotations provided by the annotator
	/// annotatorStartCommand: command line and arguments to start the service.
	/// </summary>
	/// <remarks>
	/// A simple web service annotator that can be defined through properties:
	/// annotatorEndpoint: a URL endpoint for annotator the service
	/// annotatorRequires: Requirements for the annotator
	/// annotatorProvides: Annotations provided by the annotator
	/// annotatorStartCommand: command line and arguments to start the service.
	/// annotatorStopCommand: command line and arguments to stop the service.
	/// The annotator is expected to provide the following interface:
	/// - ENDPOINT/ping/ : Checks if the service is still alive.
	/// - ENDPOINT/annotate/ : Runs all the annotator.
	/// </remarks>
	/// <author><a href="mailto:chaganty@cs.stanford.edu">Arun Chaganty</a></author>
	public class GenericWebServiceAnnotator : WebServiceAnnotator
	{
		public string annotatorEndpoint = "https://localhost:8000/";

		public ICollection<Type> annotatorRequires;

		public ICollection<Type> annotatorProvides;

		public Optional<string[]> startCommand;

		public Optional<string[]> stopCommand;

		protected internal ProtobufAnnotationSerializer serializer;

		private static ICollection<Type> ParseClasses(string classList)
		{
			ICollection<Type> ret = new HashSet<Type>();
			foreach (string s in classList.Split(","))
			{
				s = s.Trim();
				if (s.Length == 0)
				{
					continue;
				}
				// If s is not fully specified ASSUME edu.stanford.nlp.ling.CoreAnnotations.{s}
				if (!s.Contains("."))
				{
					s = "edu.stanford.nlp.ling.CoreAnnotations$" + s;
				}
				try
				{
					ret.Add((Type)Sharpen.Runtime.GetType(s));
				}
				catch (TypeLoadException e)
				{
					throw new Exception(e);
				}
			}
			return ret;
		}

		public GenericWebServiceAnnotator(Properties props)
		{
			// annotator endpoint
			annotatorEndpoint = props.GetProperty("generic.endpoint");
			annotatorRequires = ParseClasses(props.GetProperty("generic.requires", string.Empty));
			annotatorProvides = ParseClasses(props.GetProperty("generic.provides", string.Empty));
			startCommand = Optional.OfNullable(props.GetProperty("generic.start")).Map(null);
			stopCommand = Optional.OfNullable(props.GetProperty("generic.stop")).Map(null);
			serializer = new ProtobufAnnotationSerializer();
		}

		public override ICollection<Type> RequirementsSatisfied()
		{
			return annotatorProvides;
		}

		public override ICollection<Type> Requires()
		{
			return annotatorRequires;
		}

		protected internal override Optional<string[]> StartCommand()
		{
			return startCommand;
		}

		protected internal override Optional<string[]> StopCommand()
		{
			return stopCommand;
		}

		protected internal override bool Ready(bool initialTest)
		{
			return this.Ping(annotatorEndpoint + "/ping/");
		}

		private static void CopyValue<V>(ICoreMap source, ICoreMap target, Type k)
		{
			Type k_ = (Type)k;
			target.Set(k_, source.Get(k_));
		}

		private static void Copy(Annotation source, Annotation target)
		{
			source.KeySet().ForEach(null);
		}

		/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.ShouldRetryException"/>
		/// <exception cref="Edu.Stanford.Nlp.Pipeline.WebServiceAnnotator.PermanentlyFailedException"/>
		protected internal override void AnnotateImpl(Annotation ann)
		{
			Annotation ann_;
			// New annotaiton
			try
			{
				// Executes the connection from conn
				HttpURLConnection conn;
				conn = (HttpURLConnection)new URL(annotatorEndpoint + "/annotate/").OpenConnection();
				conn.SetRequestMethod("POST");
				conn.SetDoOutput(true);
				conn.SetRequestProperty("Content-Type", "application/octet-stream; charset=UTF-8");
				using (OutputStream outputStream = conn.GetOutputStream())
				{
					serializer.ToProto(ann).WriteDelimitedTo(outputStream);
					outputStream.Flush();
				}
				conn.Connect();
				try
				{
					using (InputStream inputStream = conn.GetInputStream())
					{
						Pair<Annotation, InputStream> pair = serializer.Read(inputStream);
						ann_ = pair.first;
					}
				}
				catch (Exception e)
				{
					throw new WebServiceAnnotator.PermanentlyFailedException(e);
				}
			}
			catch (MalformedURLException e)
			{
				throw new WebServiceAnnotator.PermanentlyFailedException(e);
			}
			catch (IOException)
			{
				throw new WebServiceAnnotator.ShouldRetryException();
			}
			// Copy over annotation.
			Copy(ann_, ann);
		}
	}
}
