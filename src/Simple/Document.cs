using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;







namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A representation of a Document.</summary>
	/// <remarks>A representation of a Document. Most blobs of raw text should become documents.</remarks>
	/// <author>Gabor Angeli</author>
	public class Document
	{
		/// <summary>
		/// The empty
		/// <see cref="Java.Util.Properties"/>
		/// object, for use with creating default annotators.
		/// </summary>
		internal static readonly Properties EmptyProps = PropertiesUtils.AsProperties("language", "english", "annotators", string.Empty, "tokenize.class", "PTBTokenizer", "tokenize.language", "en", "parse.binaryTrees", "true", "mention.type", "dep", 
			"coref.mode", "statistical", "coref.md.type", "dep");

		/// <summary>
		/// The caseless
		/// <see cref="Java.Util.Properties"/>
		/// object.
		/// </summary>
		/// <seealso cref="Caseless()"/>
		/// <seealso cref="Sentence.Caseless()"/>
		internal static readonly Properties CaselessProps = PropertiesUtils.AsProperties("language", "english", "annotators", string.Empty, "tokenize.class", "PTBTokenizer", "tokenize.language", "en", "parse.binaryTrees", "true", "pos.model", "edu/stanford/nlp/models/pos-tagger/wsj-0-18-caseless-left3words-distsim.tagger"
			, "parse.model", "edu/stanford/nlp/models/lexparser/englishPCFG.caseless.ser.gz", "ner.model", "edu/stanford/nlp/models/ner/english.muc.7class.caseless.distsim.crf.ser.gz," + "edu/stanford/nlp/models/ner/english.conll.4class.caseless.distsim.crf.ser.gz,"
			 + "edu/stanford/nlp/models/ner/english.all.3class.caseless.distsim.crf.ser.gz");

		/// <summary>
		/// The backend to use for constructing
		/// <see cref="Edu.Stanford.Nlp.Pipeline.IAnnotator"/>
		/// s.
		/// </summary>
		private static AnnotatorImplementations backend = new AnnotatorImplementations();

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.TokenizerAnnotator"/>
		/// implementation
		/// </summary>
		private static readonly IAnnotator defaultTokenize = backend.Tokenizer(EmptyProps);

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator"/>
		/// implementation
		/// </summary>
		private static readonly IAnnotator defaultSSplit = backend.WordToSentences(EmptyProps);

		private sealed class _ISupplier_90 : ISupplier<IAnnotator>
		{
			public _ISupplier_90()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordPos, PropertiesUtils.GetSignature(IAnnotator.StanfordPos, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			// Use the new coref
			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.POSTaggerAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultPOS = new _ISupplier_90();

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.MorphaAnnotator"/>
		/// implementation
		/// </summary>
		private static readonly ISupplier<IAnnotator> defaultLemma = null;

		private sealed class _ISupplier_106 : ISupplier<IAnnotator>
		{
			public _ISupplier_106()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordNer, PropertiesUtils.GetSignature(IAnnotator.StanfordNer, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.NERCombinerAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultNER = new _ISupplier_106();

		private sealed class _ISupplier_118 : ISupplier<IAnnotator>
		{
			public _ISupplier_118()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordRegexner, PropertiesUtils.GetSignature(IAnnotator.StanfordRegexner, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.RegexNERAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultRegexner = new _ISupplier_118();

		private sealed class _ISupplier_130 : ISupplier<IAnnotator>
		{
			public _ISupplier_130()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordParse, PropertiesUtils.GetSignature(IAnnotator.StanfordParse, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.ParserAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultParse = new _ISupplier_130();

		private sealed class _ISupplier_142 : ISupplier<IAnnotator>
		{
			public _ISupplier_142()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordDependencies, PropertiesUtils.GetSignature(IAnnotator.StanfordDependencies, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.DependencyParseAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultDepparse = new _ISupplier_142();

		private sealed class _ISupplier_154 : ISupplier<IAnnotator>
		{
			public _ISupplier_154()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordNatlog, PropertiesUtils.GetSignature(IAnnotator.StanfordNatlog, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultNatlog = new _ISupplier_154();

		private sealed class _ISupplier_166 : ISupplier<IAnnotator>
		{
			public _ISupplier_166()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordEntityMentions, PropertiesUtils.GetSignature(IAnnotator.StanfordEntityMentions, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.EntityMentionsAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultEntityMentions = new _ISupplier_166();

		private sealed class _ISupplier_178 : ISupplier<IAnnotator>
		{
			public _ISupplier_178()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordKbp, PropertiesUtils.GetSignature(IAnnotator.StanfordKbp, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.KBPAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultKBP = new _ISupplier_178();

		private sealed class _ISupplier_191 : ISupplier<IAnnotator>
		{
			public _ISupplier_191()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordOpenie, PropertiesUtils.GetSignature(IAnnotator.StanfordOpenie, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Naturalli.OpenIE"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultOpenie = new _ISupplier_191();

		private sealed class _ISupplier_203 : ISupplier<IAnnotator>
		{
			public _ISupplier_203()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordCorefMention, PropertiesUtils.GetSignature(IAnnotator.StanfordCorefMention, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.CorefMentionAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultMention = new _ISupplier_203();

		private sealed class _ISupplier_215 : ISupplier<IAnnotator>
		{
			public _ISupplier_215()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordCoref, PropertiesUtils.GetSignature(IAnnotator.StanfordCoref, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.CorefAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultCoref = new _ISupplier_215();

		private sealed class _ISupplier_227 : ISupplier<IAnnotator>
		{
			public _ISupplier_227()
			{
				this.key = new StanfordCoreNLP.AnnotatorSignature(IAnnotator.StanfordSentiment, PropertiesUtils.GetSignature(IAnnotator.StanfordSentiment, Edu.Stanford.Nlp.Simple.Document.EmptyProps));
			}

			private StanfordCoreNLP.AnnotatorSignature key;

			public IAnnotator Get()
			{
				lock (this)
				{
					return StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(this.key, null).Get();
				}
			}
		}

		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.SentimentAnnotator"/>
		/// implementation
		/// </summary>
		private static ISupplier<IAnnotator> defaultSentiment = new _ISupplier_227();

		/// <summary>Cache the most recently used custom annotators.</summary>
		private static readonly AnnotatorPool customAnnotators = new AnnotatorPool();

		/// <summary>Either get a custom annotator which was recently defined, or create it if it has never been defined.</summary>
		/// <remarks>
		/// Either get a custom annotator which was recently defined, or create it if it has never been defined.
		/// This method is synchronized to avoid race conditions when loading the annotators.
		/// </remarks>
		/// <param name="name">The name of the annotator.</param>
		/// <param name="props">The properties used to create the annotator, if we need to create it.</param>
		/// <param name="annotator">The actual function used to make the annotator, if needed.</param>
		/// <returns>An annotator as specified by the given name and properties.</returns>
		private static ISupplier<IAnnotator> GetOrCreate(string name, Properties props, ISupplier<IAnnotator> annotator)
		{
			lock (typeof(Document))
			{
				StanfordCoreNLP.AnnotatorSignature key = new StanfordCoreNLP.AnnotatorSignature(name, PropertiesUtils.GetSignature(name, props));
				customAnnotators.Register(name, props, StanfordCoreNLP.GlobalAnnotatorCache.ComputeIfAbsent(key, null));
				return null;
			}
		}

		/// <summary>The protocol buffer representing this document</summary>
		protected internal readonly CoreNLPProtos.Document.Builder impl;

		/// <summary>The list of sentences associated with this document</summary>
		protected internal IList<Edu.Stanford.Nlp.Simple.Sentence> sentences = null;

		/// <summary>A serializer to assist in serializing and deserializing from Protocol buffers</summary>
		protected internal readonly ProtobufAnnotationSerializer serializer = new ProtobufAnnotationSerializer(false);

		/// <summary>THIS IS NONSTANDARD.</summary>
		/// <remarks>
		/// THIS IS NONSTANDARD.
		/// An indicator of whether we have run the OpenIE annotator.
		/// Unlike most other annotators, it's quite common for a sentence to not have any extracted triples,
		/// and therefore it's hard to determine whether we should rerun the annotator based solely on the saved
		/// annotation.
		/// At the same time, the proto file should not have this flag in it.
		/// So, here it is.
		/// </remarks>
		private bool haveRunOpenie = false;

		/// <summary>THIS IS NONSTANDARD.</summary>
		/// <remarks>
		/// THIS IS NONSTANDARD.
		/// An indicator of whether we have run the KBP annotator.
		/// Unlike most other annotators, it's quite common for a sentence to not have any extracted triples,
		/// and therefore it's hard to determine whether we should rerun the annotator based solely on the saved
		/// annotation.
		/// At the same time, the proto file should not have this flag in it.
		/// So, here it is.
		/// </remarks>
		private bool haveRunKBP = false;

		/// <summary>The default properties to use for annotating things (e.g., coref for the document level)</summary>
		private Properties defaultProps;

		/// <summary>Set the backend implementations for our CoreNLP pipeline.</summary>
		/// <remarks>
		/// Set the backend implementations for our CoreNLP pipeline.
		/// For example, to a
		/// <see cref="Edu.Stanford.Nlp.Pipeline.ServerAnnotatorImplementations"/>
		/// .
		/// </remarks>
		/// <param name="backend">
		/// The backend to use from now on for annotating
		/// documents.
		/// </param>
		public static void SetBackend(AnnotatorImplementations backend)
		{
			Edu.Stanford.Nlp.Simple.Document.backend = backend;
		}

		/// <summary>
		/// Use the CoreNLP Server (
		/// <see cref="Edu.Stanford.Nlp.Pipeline.StanfordCoreNLPServer"/>
		/// ) for the
		/// heavyweight backend annotation job.
		/// </summary>
		/// <param name="host">The hostname of the server.</param>
		/// <param name="port">The port the server is running on.</param>
		public static void UseServer(string host, int port)
		{
			backend = new ServerAnnotatorImplementations(host, port);
		}

		/// <summary>
		/// Use the CoreNLP Server (
		/// <see cref="Edu.Stanford.Nlp.Pipeline.StanfordCoreNLPServer"/>
		/// ) for the
		/// heavyweight backend annotation job, authenticating with the given
		/// credentials.
		/// </summary>
		/// <param name="host">The hostname of the server.</param>
		/// <param name="port">The port the server is running on.</param>
		/// <param name="apiKey">The api key to use as the username for authentication</param>
		/// <param name="apiSecret">The api secrete to use as the password for authentication</param>
		/// <param name="lazy">
		/// Only run the annotations that are required at this time. If this is
		/// false, we will also run a bunch of standard annotations, to cut down on
		/// expected number of round-trips.
		/// </param>
		public static void UseServer(string host, int port, string apiKey, string apiSecret, bool lazy)
		{
			backend = new ServerAnnotatorImplementations(host, port, apiKey, apiSecret, lazy);
		}

		/// <seealso cref="UseServer(string, int, string, string, bool)"></seealso>
		public static void UseServer(string host, string apiKey, string apiSecret, bool lazy)
		{
			UseServer(host, host.StartsWith("http://") ? 80 : 443, apiKey, apiSecret, lazy);
		}

		/// <seealso cref="UseServer(string, int, string, string, bool)"></seealso>
		public static void UseServer(string host, string apiKey, string apiSecret)
		{
			UseServer(host, host.StartsWith("http://") ? 80 : 443, apiKey, apiSecret, true);
		}

		static Document()
		{
			/*
			* A static block that'll automatically fault in the CoreNLP server, if the appropriate environment
			* variables are set.
			* These are:
			*
			* <ul>
			*     <li>CORENLP_HOST</li> -- this is already sufficient to trigger creating a server
			*     <li>CORENLP_PORT</li>
			*     <li>CORENLP_KEY</li>
			*     <li>CORENLP_SECRET</li>
			*     <li>CORENLP_LAZY</li>  (if true, do as much annotation on a single round-trip as possible)
			* </ul>
			*/
			string host = Runtime.Getenv("CORENLP_HOST");
			string portStr = Runtime.Getenv("CORENLP_PORT");
			string key = Runtime.Getenv("CORENLP_KEY");
			string secret = Runtime.Getenv("CORENLP_SECRET");
			string lazystr = Runtime.Getenv("CORENLP_LAZY");
			if (host != null)
			{
				int port = 443;
				if (portStr == null)
				{
					if (host.StartsWith("http://"))
					{
						port = 80;
					}
				}
				else
				{
					port = System.Convert.ToInt32(portStr);
				}
				bool lazy = true;
				if (lazystr != null)
				{
					lazy = bool.Parse(lazystr);
				}
				if (key != null && secret != null)
				{
					UseServer(host, port, key, secret, lazy);
				}
				else
				{
					UseServer(host, port);
				}
			}
		}

		/// <summary>Create a new document from the passed in text and the given properties.</summary>
		/// <param name="text">The text of the document.</param>
		public Document(Properties props, string text)
		{
			this.defaultProps = props;
			this.impl = CoreNLPProtos.Document.NewBuilder().SetText(text);
		}

		/// <summary>Create a new document from the passed in text.</summary>
		/// <param name="text">The text of the document.</param>
		public Document(string text)
			: this(EmptyProps, text)
		{
		}

		/// <summary>Convert a CoreNLP Annotation object to a Document.</summary>
		/// <param name="ann">The CoreNLP Annotation object.</param>
		public Document(Properties props, Annotation ann)
		{
			this.defaultProps = props;
			StanfordCoreNLP.GetDefaultAnnotatorPool(props, new AnnotatorImplementations());
			// cache the annotator pool
			this.impl = new ProtobufAnnotationSerializer(false).ToProtoBuilder(ann);
			IList<ICoreMap> sentences = ann.Get(typeof(CoreAnnotations.SentencesAnnotation));
			this.sentences = new List<Edu.Stanford.Nlp.Simple.Sentence>(sentences.Count);
			foreach (ICoreMap sentence in sentences)
			{
				this.sentences.Add(new Edu.Stanford.Nlp.Simple.Sentence(this, this.serializer.ToProtoBuilder(sentence), sentence.Get(typeof(CoreAnnotations.TextAnnotation)), this.defaultProps));
			}
		}

		/// <seealso cref="Document(Java.Util.Properties, Edu.Stanford.Nlp.Pipeline.Annotation)"></seealso>
		public Document(Annotation ann)
			: this(Edu.Stanford.Nlp.Simple.Document.EmptyProps, ann)
		{
		}

		/// <summary>Create a Document object from a read Protocol Buffer.</summary>
		/// <seealso cref="Serialize()"/>
		/// <param name="proto">The protocol buffer representing this document.</param>
		public Document(Properties props, CoreNLPProtos.Document proto)
		{
			this.defaultProps = props;
			StanfordCoreNLP.GetDefaultAnnotatorPool(props, new AnnotatorImplementations());
			// cache the annotator pool
			this.impl = ((CoreNLPProtos.Document.Builder)proto.ToBuilder());
			if (proto.GetSentenceCount() > 0)
			{
				this.sentences = new List<Edu.Stanford.Nlp.Simple.Sentence>(proto.GetSentenceCount());
				foreach (CoreNLPProtos.Sentence sentence in proto.GetSentenceList())
				{
					this.sentences.Add(new Edu.Stanford.Nlp.Simple.Sentence(this, ((CoreNLPProtos.Sentence.Builder)sentence.ToBuilder()), this.defaultProps));
				}
			}
		}

		/// <seealso cref="Document(Java.Util.Properties, Edu.Stanford.Nlp.Pipeline.CoreNLPProtos.Document)"></seealso>
		public Document(CoreNLPProtos.Document proto)
			: this(Edu.Stanford.Nlp.Simple.Document.EmptyProps, proto)
		{
		}

		/// <summary>Make this document caseless.</summary>
		/// <remarks>
		/// Make this document caseless. That is, from now on, run the caseless models
		/// on the document by default rather than the standard CoreNLP models.
		/// </remarks>
		/// <returns>This same document, but with the default properties swapped out.</returns>
		public virtual Edu.Stanford.Nlp.Simple.Document Caseless()
		{
			this.defaultProps = CaselessProps;
			return this;
		}

		/// <summary>Make this document case sensitive.</summary>
		/// <remarks>
		/// Make this document case sensitive.
		/// A document is case sensitive by default; this only has an effect if you have previously
		/// called
		/// <see cref="Sentence.Caseless()"/>
		/// .
		/// </remarks>
		/// <returns>This same document, but with the default properties swapped out.</returns>
		public virtual Edu.Stanford.Nlp.Simple.Document Cased()
		{
			this.defaultProps = EmptyProps;
			return this;
		}

		/// <summary>Serialize this Document as a Protocol Buffer.</summary>
		/// <remarks>
		/// Serialize this Document as a Protocol Buffer.
		/// This can be deserialized with the constructor
		/// <see cref="Document(Edu.Stanford.Nlp.Pipeline.CoreNLPProtos.Document)"/>
		/// .
		/// </remarks>
		/// <returns>The document as represented by a Protocol Buffer.</returns>
		public virtual CoreNLPProtos.Document Serialize()
		{
			lock (impl)
			{
				// Ensure we have sentences
				IList<Edu.Stanford.Nlp.Simple.Sentence> sentences = Sentences();
				// Ensure we're saving the newest sentences
				// IMPORTANT NOTE: the clear below must come after we call #sentences()
				this.impl.ClearSentence();
				foreach (Edu.Stanford.Nlp.Simple.Sentence s in sentences)
				{
					this.impl.AddSentence(s.Serialize());
				}
				// Serialize document
				return ((CoreNLPProtos.Document)impl.Build());
			}
		}

		/// <summary>Write this document to an output stream.</summary>
		/// <remarks>
		/// Write this document to an output stream.
		/// Internally, this stores the document as a protocol buffer, and saves that buffer to the output stream.
		/// This method does not close the stream after writing.
		/// </remarks>
		/// <param name="out">The output stream to write to. The stream is not closed after the method returns.</param>
		/// <exception cref="System.IO.IOException">Thrown from the underlying write() implementation.</exception>
		/// <seealso cref="Deserialize(Java.IO.InputStream)"/>
		public virtual void Serialize(OutputStream @out)
		{
			Serialize().WriteDelimitedTo(@out);
			@out.Flush();
		}

		/// <summary>Read a document from an input stream.</summary>
		/// <remarks>
		/// Read a document from an input stream.
		/// This does not close the input stream.
		/// </remarks>
		/// <param name="in">The input stream to deserialize from.</param>
		/// <returns>The next document encoded in the input stream.</returns>
		/// <exception cref="System.IO.IOException">Thrown by the underlying parse() implementation.</exception>
		/// <seealso cref="Serialize(Java.IO.OutputStream)"/>
		public static Edu.Stanford.Nlp.Simple.Document Deserialize(InputStream @in)
		{
			return new Edu.Stanford.Nlp.Simple.Document(CoreNLPProtos.Document.ParseDelimitedFrom(@in));
		}

		/// <summary>
		/// <p>
		/// Write this annotation as a JSON string.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Write this annotation as a JSON string.
		/// Optionally, you can also specify a number of operations to call on the document before
		/// dumping it to JSON.
		/// This allows the user to ensure that certain annotations have been computed before the document
		/// is dumped.
		/// For example:
		/// </p>
		/// <pre>
		/// <c>String json = new Document("Lucy in the sky with diamonds").json(Sentence::parse, Sentence::ner);</c>
		/// </pre>
		/// <p>
		/// will create a JSON dump of the document, ensuring that at least the parse tree and ner tags are populated.
		/// </p>
		/// </remarks>
		/// <param name="functions">
		/// The (possibly empty) list of annotations to populate on the document before dumping it
		/// to JSON.
		/// </param>
		/// <returns>The JSON String for this document.</returns>
		[SafeVarargs]
		public string Json(params Func<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (Func<Edu.Stanford.Nlp.Simple.Sentence, object> f in functions)
			{
				f.Apply(this.Sentence(0));
			}
			try
			{
				return new JSONOutputter().Print(this.AsAnnotation());
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// Like the
		/// <see>Document@json(Function...)</see>
		/// function, but with minified JSON more suitable
		/// for sending over the wire.
		/// </summary>
		/// <param name="functions">
		/// The (possibly empty) list of annotations to populate on the document before dumping it
		/// to JSON.
		/// </param>
		/// <returns>The JSON String for this document, without unnecessary whitespace.</returns>
		[SafeVarargs]
		public string JsonMinified(params Func<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (Func<Edu.Stanford.Nlp.Simple.Sentence, object> f in functions)
			{
				f.Apply(this.Sentence(0));
			}
			try
			{
				AnnotationOutputter.Options options = new AnnotationOutputter.Options();
				options.pretty = false;
				return new JSONOutputter().Print(this.AsAnnotation(), options);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// <p>
		/// Write this annotation as an XML string.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Write this annotation as an XML string.
		/// Optionally, you can also specify a number of operations to call on the document before
		/// dumping it to XML.
		/// This allows the user to ensure that certain annotations have been computed before the document
		/// is dumped.
		/// For example:
		/// </p>
		/// <pre>
		/// <c>String xml = new Document("Lucy in the sky with diamonds").xml(Document::parse, Document::ner);</c>
		/// </pre>
		/// <p>
		/// will create a XML dump of the document, ensuring that at least the parse tree and ner tags are populated.
		/// </p>
		/// </remarks>
		/// <param name="functions">
		/// The (possibly empty) list of annotations to populate on the document before dumping it
		/// to XML.
		/// </param>
		/// <returns>The XML String for this document.</returns>
		[SafeVarargs]
		public string Xml(params Func<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (Func<Edu.Stanford.Nlp.Simple.Sentence, object> f in functions)
			{
				f.Apply(this.Sentence(0));
			}
			try
			{
				return new XMLOutputter().Print(this.AsAnnotation());
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// Like the
		/// <see>Document@xml(Function...)</see>
		/// function, but with minified XML more suitable
		/// for sending over the wire.
		/// </summary>
		/// <param name="functions">
		/// The (possibly empty) list of annotations to populate on the document before dumping it
		/// to XML.
		/// </param>
		/// <returns>The XML String for this document, without unecessary whitespace.</returns>
		[SafeVarargs]
		public string XmlMinified(params Func<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (Func<Edu.Stanford.Nlp.Simple.Sentence, object> f in functions)
			{
				f.Apply(this.Sentence(0));
			}
			try
			{
				AnnotationOutputter.Options options = new AnnotationOutputter.Options();
				options.pretty = false;
				return new XMLOutputter().Print(this.AsAnnotation(false), options);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Get the sentences in this document, as a list.</summary>
		/// <param name="props">
		/// The properties to use in the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator"/>
		/// .
		/// </param>
		/// <returns>A list of Sentence objects representing the sentences in the document.</returns>
		public virtual IList<Edu.Stanford.Nlp.Simple.Sentence> Sentences(Properties props)
		{
			return this.Sentences(props, props == EmptyProps ? defaultTokenize : GetOrCreate(AnnotatorConstants.StanfordTokenize, props, null).Get());
		}

		/// <summary>Get the sentences in this document, as a list.</summary>
		/// <param name="props">
		/// The properties to use in the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator"/>
		/// .
		/// </param>
		/// <returns>A list of Sentence objects representing the sentences in the document.</returns>
		protected internal virtual IList<Edu.Stanford.Nlp.Simple.Sentence> Sentences(Properties props, IAnnotator tokenizer)
		{
			if (sentences == null)
			{
				lock (this)
				{
					if (sentences == null)
					{
						IAnnotator ssplit = props == EmptyProps ? defaultSSplit : GetOrCreate(AnnotatorConstants.StanfordSsplit, props, null).Get();
						// Annotate
						Annotation ann = new Annotation(this.impl.GetText());
						tokenizer.Annotate(ann);
						ssplit.Annotate(ann);
						// Grok results
						// (docid)
						if (ann.ContainsKey(typeof(CoreAnnotations.DocIDAnnotation)))
						{
							impl.SetDocID(ann.Get(typeof(CoreAnnotations.DocIDAnnotation)));
						}
						// (sentences)
						IList<ICoreMap> sentences = ann.Get(typeof(CoreAnnotations.SentencesAnnotation));
						this.sentences = new List<Edu.Stanford.Nlp.Simple.Sentence>(sentences.Count);
						this.impl.ClearSentence();
						foreach (ICoreMap sentence in sentences)
						{
							//Sentence sent = new Sentence(this, sentence);
							Edu.Stanford.Nlp.Simple.Sentence sent = new Edu.Stanford.Nlp.Simple.Sentence(this, this.serializer.ToProtoBuilder(sentence), sentence.Get(typeof(CoreAnnotations.TextAnnotation)), defaultProps);
							this.sentences.Add(sent);
							this.impl.AddSentence(sent.Serialize());
						}
					}
				}
			}
			return sentences;
		}

		/// <seealso cref="Sentences(Java.Util.Properties)"></seealso>
		public virtual IList<Edu.Stanford.Nlp.Simple.Sentence> Sentences()
		{
			return Sentences(EmptyProps);
		}

		/// <seealso cref="Sentences(Java.Util.Properties)"></seealso>
		public virtual Edu.Stanford.Nlp.Simple.Sentence Sentence(int sentenceIndex, Properties props)
		{
			return Sentences(props)[sentenceIndex];
		}

		/// <seealso cref="Sentences(Java.Util.Properties)"></seealso>
		public virtual Edu.Stanford.Nlp.Simple.Sentence Sentence(int sentenceIndex)
		{
			return Sentences()[sentenceIndex];
		}

		/// <summary>
		/// Get the raw text of the document, as input by, e.g.,
		/// <see cref="Document(string)"/>
		/// .
		/// </summary>
		public virtual string Text()
		{
			lock (impl)
			{
				return impl.GetText();
			}
		}

		/// <summary>Returns the coref chains in the document.</summary>
		/// <remarks>
		/// Returns the coref chains in the document. This is a map from coref cluster IDs, to the coref chain
		/// with that ID.
		/// </remarks>
		/// <param name="props">
		/// The properties to use in the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.DeterministicCorefAnnotator"/>
		/// .
		/// </param>
		public virtual IDictionary<int, CorefChain> Coref(Properties props)
		{
			lock (this.impl)
			{
				if (impl.GetCorefChainCount() == 0)
				{
					// Run prerequisites
					this.RunLemma(props).RunNER(props);
					if (CorefProperties.MdType(props) != CorefProperties.MentionDetectionType.Dependency)
					{
						this.RunParse(props);
					}
					else
					{
						this.RunDepparse(props);
					}
					// Run mention
					ISupplier<IAnnotator> mention = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultMention : GetOrCreate(AnnotatorConstants.StanfordCorefMention, props, null);
					// Run coref
					ISupplier<IAnnotator> coref = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultCoref : GetOrCreate(AnnotatorConstants.StanfordCoref, props, null);
					Annotation ann = AsAnnotation(true);
					mention.Get().Annotate(ann);
					coref.Get().Annotate(ann);
					// Convert to proto
					lock (serializer)
					{
						foreach (CorefChain chain in ann.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)).Values)
						{
							impl.AddCorefChain(serializer.ToProto(chain));
						}
					}
				}
				IDictionary<int, CorefChain> corefs = Generics.NewHashMap();
				foreach (CoreNLPProtos.CorefChain chain_1 in impl.GetCorefChainList())
				{
					corefs[chain_1.GetChainID()] = FromProto(chain_1);
				}
				return corefs;
			}
		}

		/// <seealso cref="Coref(Java.Util.Properties)"></seealso>
		public virtual IDictionary<int, CorefChain> Coref()
		{
			return Coref(defaultProps);
		}

		/// <summary>Returns the document id of the document, if one was found</summary>
		public virtual Optional<string> Docid()
		{
			lock (impl)
			{
				if (impl.HasDocID())
				{
					return Optional.Of(impl.GetDocID());
				}
				else
				{
					return Optional.Empty();
				}
			}
		}

		/// <summary>Sets the document id of the document, returning this.</summary>
		public virtual Edu.Stanford.Nlp.Simple.Document SetDocid(string docid)
		{
			lock (impl)
			{
				this.impl.SetDocID(docid);
			}
			return this;
		}

		/// <summary>
		/// <p>
		/// Bypass the tokenizer and sentence splitter -- axiomatically set the sentences for this document.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Bypass the tokenizer and sentence splitter -- axiomatically set the sentences for this document.
		/// This is a VERY dangerous method to call if you don't know what you're doing.
		/// The primary use case is for forcing single-sentence documents, where most of the fields in the document
		/// do not matter.
		/// </p>
		/// </remarks>
		/// <param name="sentences">The sentences to force for the sentence list of this document.</param>
		internal virtual void ForceSentences(IList<Edu.Stanford.Nlp.Simple.Sentence> sentences)
		{
			this.sentences = sentences;
			lock (impl)
			{
				this.impl.ClearSentence();
				foreach (Edu.Stanford.Nlp.Simple.Sentence sent in sentences)
				{
					this.impl.AddSentence(sent.Serialize());
				}
			}
		}

		//
		// Begin helpers
		//
		internal virtual Edu.Stanford.Nlp.Simple.Document RunPOS(Properties props)
		{
			lock (this)
			{
				// Cached result
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawToken(0).HasPos())
				{
					return this;
				}
				// Prerequisites
				Sentences();
				// Run annotator
				ISupplier<IAnnotator> pos = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultPOS : GetOrCreate(AnnotatorConstants.StanfordPos, props, null);
				Annotation ann = AsAnnotation(false);
				pos.Get().Annotate(ann);
				// Update data
				for (int i = 0; i < sentences.Count; ++i)
				{
					sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunLemma(Properties props)
		{
			lock (this)
			{
				// Cached result
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawToken(0).HasLemma())
				{
					return this;
				}
				// Prerequisites
				RunPOS(props);
				// Run annotator
				ISupplier<IAnnotator> lemma = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultLemma : GetOrCreate(AnnotatorConstants.StanfordLemma, props, null);
				Annotation ann = AsAnnotation(true);
				lemma.Get().Annotate(ann);
				// Update data
				for (int i = 0; i < sentences.Count; ++i)
				{
					sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document MockLemma(Properties props)
		{
			lock (this)
			{
				// Cached result
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawToken(0).HasLemma())
				{
					return this;
				}
				// Prerequisites
				RunPOS(props);
				// Mock lemma with word
				Annotation ann = AsAnnotation(true);
				for (int i = 0; i < sentences.Count; ++i)
				{
					sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunNER(Properties props)
		{
			lock (this)
			{
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawToken(0).HasNer())
				{
					return this;
				}
				// Run prerequisites
				RunPOS(props);
				// Run annotator
				ISupplier<IAnnotator> ner = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultNER : GetOrCreate(AnnotatorConstants.StanfordNer, props, null);
				Annotation ann = AsAnnotation(true);
				ner.Get().Annotate(ann);
				// Update data
				for (int i = 0; i < sentences.Count; ++i)
				{
					sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunRegexner(Properties props)
		{
			lock (this)
			{
				// Run prerequisites
				RunNER(props);
				// Run annotator
				ISupplier<IAnnotator> ner = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultRegexner : GetOrCreate(AnnotatorConstants.StanfordRegexner, props, null);
				Annotation ann = AsAnnotation(true);
				ner.Get().Annotate(ann);
				// Update data
				for (int i = 0; i < sentences.Count; ++i)
				{
					sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunParse(Properties props)
		{
			lock (this)
			{
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawSentence().HasParseTree())
				{
					return this;
				}
				// Run annotator
				bool cacheAnnotation = false;
				IAnnotator parse = ((props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultParse : GetOrCreate(AnnotatorConstants.StanfordParse, props, null)).Get();
				if (parse.Requires().Contains(typeof(CoreAnnotations.PartOfSpeechAnnotation)) || Runtime.Getenv("CORENLP_HOST") != null)
				{
					// Run the POS tagger if we are (or may be) using the shift reduce parser
					RunPOS(props);
					cacheAnnotation = true;
				}
				else
				{
					Sentences();
				}
				Annotation ann = AsAnnotation(cacheAnnotation);
				parse.Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
						Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
						Tree binaryTree = sentence.Get(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation));
						sentences[i].UpdateParse(serializer.ToProto(tree), binaryTree == null ? null : serializer.ToProto(binaryTree));
						sentences[i].UpdateDependencies(ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation))), ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation
							))), ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
					}
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunDepparse(Properties props)
		{
			lock (this)
			{
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawSentence().HasBasicDependencies())
				{
					return this;
				}
				// Run prerequisites
				RunPOS(props);
				// Run annotator
				ISupplier<IAnnotator> depparse = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultDepparse : GetOrCreate(AnnotatorConstants.StanfordDependencies, props, null);
				Annotation ann = AsAnnotation(true);
				depparse.Get().Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
						sentences[i].UpdateDependencies(ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation))), ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation
							))), ProtobufAnnotationSerializer.ToProto(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
					}
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunNatlog(Properties props)
		{
			lock (this)
			{
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawToken(0).HasPolarity())
				{
					return this;
				}
				// Run prerequisites
				RunLemma(props);
				RunDepparse(props);
				// Run annotator
				ISupplier<IAnnotator> natlog = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultNatlog : GetOrCreate(AnnotatorConstants.StanfordNatlog, props, null);
				Annotation ann = AsAnnotation(true);
				natlog.Get().Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
						sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
						sentences[i].UpdateTokens(ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i].Get(typeof(CoreAnnotations.TokensAnnotation)), null, null);
					}
				}
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunOpenie(Properties props)
		{
			lock (this)
			{
				if (haveRunOpenie)
				{
					return this;
				}
				// Run prerequisites
				RunNatlog(props);
				// Run annotator
				ISupplier<IAnnotator> openie = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultOpenie : GetOrCreate(AnnotatorConstants.StanfordOpenie, props, null);
				Annotation ann = AsAnnotation(true);
				openie.Get().Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
						ICollection<RelationTriple> triples = sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation));
						sentences[i].UpdateOpenIE(triples.Stream().Map(null));
					}
				}
				// Return
				haveRunOpenie = true;
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunKBP(Properties props)
		{
			lock (this)
			{
				if (haveRunKBP)
				{
					return this;
				}
				// Run prerequisites
				Coref(props);
				ISupplier<IAnnotator> entityMention = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultEntityMentions : GetOrCreate(AnnotatorConstants.StanfordEntityMentions, props, null);
				Annotation ann = AsAnnotation(true);
				entityMention.Get().Annotate(ann);
				// Run annotator
				ISupplier<IAnnotator> kbp = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultKBP : GetOrCreate(AnnotatorConstants.StanfordKbp, props, null);
				kbp.Get().Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
						ICollection<RelationTriple> triples = sentence.Get(typeof(CoreAnnotations.KBPTriplesAnnotation));
						sentences[i].UpdateKBP(triples.Stream().Map(null));
					}
				}
				// Return
				haveRunKBP = true;
				return this;
			}
		}

		internal virtual Edu.Stanford.Nlp.Simple.Document RunSentiment(Properties props)
		{
			lock (this)
			{
				if (this.sentences != null && this.sentences.Count > 0 && this.sentences[0].RawSentence().HasSentiment())
				{
					return this;
				}
				// Run prerequisites
				RunParse(props);
				if (this.sentences != null && this.sentences.Count > 0 && !this.sentences[0].RawSentence().HasBinarizedParseTree())
				{
					throw new InvalidOperationException("No binarized parse tree (perhaps it's not supported in this language?)");
				}
				// Run annotator
				Annotation ann = AsAnnotation(true);
				ISupplier<IAnnotator> sentiment = (props == EmptyProps || props == Edu.Stanford.Nlp.Simple.Sentence.SingleSentenceDocument) ? defaultSentiment : GetOrCreate(AnnotatorConstants.StanfordSentiment, props, null);
				sentiment.Get().Annotate(ann);
				// Update data
				lock (serializer)
				{
					for (int i = 0; i < sentences.Count; ++i)
					{
						ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
						string sentimentClass = sentence.Get(typeof(SentimentCoreAnnotations.SentimentClass));
						sentences[i].UpdateSentiment(sentimentClass);
					}
				}
				// Return
				return this;
			}
		}

		/// <summary>Return this Document as an Annotation object.</summary>
		/// <remarks>
		/// Return this Document as an Annotation object.
		/// Note that, importantly, only the fields which have already been called will be populated in
		/// the Annotation!
		/// <p>Therefore, this method is generally NOT recommended.</p>
		/// </remarks>
		public virtual Annotation AsAnnotation()
		{
			return AsAnnotation(false);
		}

		/// <summary>A cached version of this document as an Annotation.</summary>
		/// <remarks>
		/// A cached version of this document as an Annotation.
		/// This will get garbage collected when necessary.
		/// </remarks>
		private SoftReference<Annotation> cachedAnnotation = null;

		/// <summary>Return this Document as an Annotation object.</summary>
		/// <remarks>
		/// Return this Document as an Annotation object.
		/// Note that, importantly, only the fields which have already been called will be populated in
		/// the Annotation!
		/// <p>Therefore, this method is generally NOT recommended.</p>
		/// </remarks>
		/// <param name="cache">If true, allow retrieving this object from the cache.</param>
		internal virtual Annotation AsAnnotation(bool cache)
		{
			Annotation ann = cachedAnnotation == null ? null : cachedAnnotation.Get();
			if (!cache || ann == null)
			{
				ann = serializer.FromProto(Serialize());
			}
			cachedAnnotation = new SoftReference<Annotation>(ann);
			return ann;
		}

		/// <summary>Read a CorefChain from its serialized representation.</summary>
		/// <remarks>
		/// Read a CorefChain from its serialized representation.
		/// This is private due to the need for an additional partial document. Also, why on Earth are you trying to use
		/// this on its own anyways?
		/// </remarks>
		/// <seealso cref="Edu.Stanford.Nlp.Pipeline.ProtobufAnnotationSerializer.FromProto(Edu.Stanford.Nlp.Pipeline.CoreNLPProtos.CorefChain, Edu.Stanford.Nlp.Pipeline.Annotation)"/>
		/// <param name="proto">The serialized representation of the coref chain, missing information on its mention span string.</param>
		/// <returns>A coreference chain.</returns>
		private CorefChain FromProto(CoreNLPProtos.CorefChain proto)
		{
			// Get chain ID
			int cid = proto.GetChainID();
			// Get mentions
			IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentions = new Dictionary<IntPair, ICollection<CorefChain.CorefMention>>();
			CorefChain.CorefMention representative = null;
			for (int i = 0; i < proto.GetMentionCount(); ++i)
			{
				CoreNLPProtos.CorefChain.CorefMention mentionProto = proto.GetMention(i);
				// Create mention
				StringBuilder mentionSpan = new StringBuilder();
				Edu.Stanford.Nlp.Simple.Sentence sentence = Sentence(mentionProto.GetSentenceIndex());
				for (int k = mentionProto.GetBeginIndex(); k < mentionProto.GetEndIndex(); ++k)
				{
					mentionSpan.Append(' ').Append(sentence.Word(k));
				}
				// Set the coref cluster id for the token
				CorefChain.CorefMention mention = new CorefChain.CorefMention(Dictionaries.MentionType.ValueOf(mentionProto.GetMentionType()), Dictionaries.Number.ValueOf(mentionProto.GetNumber()), Dictionaries.Gender.ValueOf(mentionProto.GetGender()), Dictionaries.Animacy
					.ValueOf(mentionProto.GetAnimacy()), mentionProto.GetBeginIndex() + 1, mentionProto.GetEndIndex() + 1, mentionProto.GetHeadIndex() + 1, cid, mentionProto.GetMentionID(), mentionProto.GetSentenceIndex() + 1, new IntTuple(new int[] { mentionProto
					.GetSentenceIndex() + 1, mentionProto.GetPosition() }), mentionSpan.Substring(mentionSpan.Length > 0 ? 1 : 0));
				// Register mention
				IntPair key = new IntPair(mentionProto.GetSentenceIndex() - 1, mentionProto.GetHeadIndex() - 1);
				if (!mentions.Contains(key))
				{
					mentions[key] = new HashSet<CorefChain.CorefMention>();
				}
				mentions[key].Add(mention);
				// Check for representative
				if (proto.HasRepresentative() && i == proto.GetRepresentative())
				{
					representative = mention;
				}
			}
			// Return
			return new CorefChain(cid, mentions, representative);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Simple.Document))
			{
				return false;
			}
			Edu.Stanford.Nlp.Simple.Document document = (Edu.Stanford.Nlp.Simple.Document)o;
			if (impl.HasText() && !impl.GetText().Equals(document.impl.GetText()))
			{
				return false;
			}
			return ((CoreNLPProtos.Document)impl.Build()).Equals(((CoreNLPProtos.Document)document.impl.Build())) && sentences.Equals(document.sentences);
		}

		public override int GetHashCode()
		{
			if (impl.HasText())
			{
				return impl.GetText().GetHashCode();
			}
			else
			{
				return ((CoreNLPProtos.Document)impl.Build()).GetHashCode();
			}
		}

		public override string ToString()
		{
			return impl.GetText();
		}
	}
}
