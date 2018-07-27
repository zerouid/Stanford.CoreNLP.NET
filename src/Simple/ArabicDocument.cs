using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;




namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A sentence running with the Arabic models.</summary>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	public class ArabicDocument : Document
	{
		private sealed class _Properties_24 : Properties
		{
			public _Properties_24()
			{
				{
					try
					{
						using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem("edu/stanford/nlp/pipeline/StanfordCoreNLP-arabic.properties"))
						{
							this.Load(@is);
						}
					}
					catch (IOException e)
					{
						throw new RuntimeIOException(e);
					}
					this.SetProperty("language", "arabic");
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
		public ArabicDocument(string text)
			: base(Edu.Stanford.Nlp.Simple.ArabicDocument.EmptyProps, text)
		{
		}

		/// <summary>Convert a CoreNLP Annotation object to a Document.</summary>
		/// <param name="ann">The CoreNLP Annotation object.</param>
		public ArabicDocument(Annotation ann)
			: base(Edu.Stanford.Nlp.Simple.ArabicDocument.EmptyProps, ann)
		{
		}

		/// <summary>Create a Document object from a read Protocol Buffer.</summary>
		/// <seealso cref="Document.Serialize()"/>
		/// <param name="proto">The protocol buffer representing this document.</param>
		public ArabicDocument(CoreNLPProtos.Document proto)
			: base(Edu.Stanford.Nlp.Simple.ArabicDocument.EmptyProps, proto)
		{
		}

		/// <summary>Create a new Arabic document from the passed in text and the given properties.</summary>
		/// <param name="text">The text of the document.</param>
		protected internal ArabicDocument(Properties props, string text)
			: base(props, text)
		{
		}

		/// <summary>No lemma annotator for Arabic -- set the lemma to be the word.</summary>
		/// <seealso cref="Document.RunLemma(Java.Util.Properties)"/>
		internal override Document RunLemma(Properties props)
		{
			return MockLemma(props);
		}

		internal override Document RunSentiment(Properties props)
		{
			throw new ArgumentException("Sentiment analysis is not implemented for Arabic");
		}

		internal override Document RunDepparse(Properties props)
		{
			throw new ArgumentException("Dependency parsing is not implemented for Arabic");
		}

		internal override Document RunNER(Properties props)
		{
			throw new ArgumentException("NER is not implemented for Arabic");
		}

		public override IDictionary<int, CorefChain> Coref(Properties props)
		{
			throw new ArgumentException("Coreference is not implemented for Arabic");
		}
	}
}
