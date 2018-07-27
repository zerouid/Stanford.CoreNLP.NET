using System;
using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// A set of annotator implementations, backed by the server
	/// (
	/// <see cref="StanfordCoreNLPServer"/>
	/// ).
	/// </summary>
	/// <author><a href="mailto:gabor@eloquent.ai">Gabor Angeli</a></author>
	public class ServerAnnotatorImplementations : AnnotatorImplementations
	{
		/// <summary>The hostname of the server to hit</summary>
		public readonly string host;

		/// <summary>The port to hit on the server</summary>
		public readonly int port;

		/// <summary>The key to use as the username to authenticate with the server, or null.</summary>
		public readonly string key;

		/// <summary>The secret key to use as the username to authenticate with the server, or null.</summary>
		public readonly string secret;

		/// <summary>If false, run many common annotations when we hit the server the first time.</summary>
		public readonly bool lazy;

		/// <summary>
		/// Create a new annotator implementation backed by
		/// <see cref="StanfordCoreNLPServer"/>
		/// .
		/// </summary>
		/// <param name="host">The hostname of the server.</param>
		/// <param name="port">The port of the server.</param>
		public ServerAnnotatorImplementations(string host, int port, string key, string secret, bool lazy)
		{
			this.host = host;
			this.port = port;
			this.key = key;
			this.secret = secret;
			this.lazy = lazy;
		}

		/// <summary>
		/// Create a new annotator implementation backed by
		/// <see cref="StanfordCoreNLPServer"/>
		/// .
		/// </summary>
		/// <param name="host">The hostname of the server.</param>
		/// <param name="port">The port of the server.</param>
		public ServerAnnotatorImplementations(string host, int port)
			: this(host, port, null, null, false)
		{
		}

		private class SingletonAnnotator : IAnnotator
		{
			private readonly StanfordCoreNLPClient client;

			public SingletonAnnotator(ServerAnnotatorImplementations _enclosing, string host, int port, Properties properties, string annotator)
			{
				this._enclosing = _enclosing;
				Properties forClient = new Properties();
				foreach (object o in properties.Keys)
				{
					string key = o.ToString();
					string value = properties.GetProperty(key);
					forClient.SetProperty(key, value);
					forClient.SetProperty(annotator + '.' + key, value);
				}
				if (this._enclosing.lazy)
				{
					forClient.SetProperty("annotators", annotator);
					forClient.SetProperty("enforceRequirements", "false");
				}
				else
				{
					string annotators = "tokenize,ssplit,pos,lemma,ner,parse,mention,coref,natlog,openie,sentiment";
					if (!annotators.Contains(annotator))
					{
						annotators += "," + annotator;
					}
					forClient.SetProperty("annotators", annotators);
				}
				this.client = new StanfordCoreNLPClient(forClient, host, port, this._enclosing.key, this._enclosing.secret);
			}

			public virtual void Annotate(Annotation annotation)
			{
				this.client.Annotate(annotation);
			}

			public virtual ICollection<Type> RequirementsSatisfied()
			{
				return Java.Util.Collections.EmptySet();
			}

			public virtual ICollection<Type> Requires()
			{
				return Java.Util.Collections.EmptySet();
			}

			private readonly ServerAnnotatorImplementations _enclosing;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator PosTagger(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordPos);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Ner(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordNer);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator TokensRegexNER(Properties properties, string name)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordRegexner);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Gender(Properties properties, string name)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordGender);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Parse(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordParse);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator TrueCase(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordTruecase);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator CorefMention(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordCorefMention);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Coref(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordCoref);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Dcoref(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordDeterministicCoref);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Relations(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordRelation);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Sentiment(Properties properties, string name)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordSentiment);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Dependencies(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordDependencies);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Openie(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordOpenie);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Kbp(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordKbp);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IAnnotator Link(Properties properties)
		{
			return new ServerAnnotatorImplementations.SingletonAnnotator(this, host, port, properties, AnnotatorConstants.StanfordLink);
		}
	}
}
