using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;




namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A sentence running with the German models.</summary>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	public class GermanDocument : Document
	{
		private sealed class _Properties_24 : Properties
		{
			public _Properties_24()
			{
				{
					try
					{
						using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem("edu/stanford/nlp/pipeline/StanfordCoreNLP-german.properties"))
						{
							this.Load(@is);
						}
					}
					catch (IOException e)
					{
						throw new RuntimeIOException(e);
					}
					this.SetProperty("language", "german");
					this.SetProperty("annotators", string.Empty);
				}
			}
		}

		/// <summary>
		/// The empty
		/// <see cref="Java.Util.Properties"/>
		/// object, for use with creating default annotators.
		/// </summary>
		internal static readonly Properties EmptyProps = new _Properties_24();

		/// <summary>Create a new document from the passed in text.</summary>
		/// <param name="text">The text of the document.</param>
		public GermanDocument(string text)
			: base(Edu.Stanford.Nlp.Simple.GermanDocument.EmptyProps, text)
		{
		}

		/// <summary>Convert a CoreNLP Annotation object to a Document.</summary>
		/// <param name="ann">The CoreNLP Annotation object.</param>
		public GermanDocument(Annotation ann)
			: base(Edu.Stanford.Nlp.Simple.GermanDocument.EmptyProps, ann)
		{
		}

		/// <summary>Create a Document object from a read Protocol Buffer.</summary>
		/// <seealso cref="Document.Serialize()"/>
		/// <param name="proto">The protocol buffer representing this document.</param>
		public GermanDocument(CoreNLPProtos.Document proto)
			: base(Edu.Stanford.Nlp.Simple.GermanDocument.EmptyProps, proto)
		{
		}

		/// <summary>Create a new German document from the passed in text and the given properties.</summary>
		/// <param name="text">The text of the document.</param>
		protected internal GermanDocument(Properties props, string text)
			: base(props, text)
		{
		}

		/// <summary>No lemma annotator for German -- set the lemma to be the word.</summary>
		/// <seealso cref="Document.RunLemma(Java.Util.Properties)"/>
		internal override Document RunLemma(Properties props)
		{
			return MockLemma(props);
		}

		internal override Document RunSentiment(Properties props)
		{
			throw new ArgumentException("Sentiment analysis is not implemented for German");
		}

		public override IDictionary<int, CorefChain> Coref(Properties props)
		{
			throw new ArgumentException("Coreference is not implemented for German");
		}
	}
}
